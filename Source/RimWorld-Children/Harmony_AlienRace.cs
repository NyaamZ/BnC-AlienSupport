﻿using System;
using System.Collections.Generic;
using System.Linq;
using AlienRace;
using Harmony;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorldChildren;

namespace BnC_Alien_Support
{
    [StaticConstructorOnStartup]
    internal static class Harmony_AlienRace
    {
        static Harmony_AlienRace()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("BnC_Alien_Support");

            harmony.Patch(
                        AccessTools.Method(
                            typeof(HarmonyPatches),
                            nameof(AlienRace.HarmonyPatches.DrawAddons)),
                        new HarmonyMethod(typeof(AlienRace_Patches), nameof(AlienRace_Patches.DrawAddons_Prefix)));

            harmony.Patch(
                AccessTools.Method(
                    typeof(PawnGraphicSet),
                    nameof(PawnGraphicSet.HeadMatAt)), null,
                new HarmonyMethod(typeof(AlienRace_Patches), nameof(AlienRace_Patches.AlienChildHeadMatAt)));

            harmony.Patch(
                AccessTools.Method(
                    typeof(Children_Drawing),
                    nameof(Children_Drawing.ModifyChildBodyType)), null,
                new HarmonyMethod(typeof(AlienRace_Patches), nameof(AlienRace_Patches.ModifyChildBodyTypePostfix)));

            //harmony.Patch(
            //    AccessTools.Method(
            //        typeof(PawnGraphicSet),
            //        nameof(PawnGraphicSet.MatsBodyBaseAt)), null,
            //    new HarmonyMethod(typeof(AlienRace_Patches), nameof(AlienRace_Patches.AlienChildMatsBodyBaseAt)));

            AlienRace_Patches.ChangeAliensProperty();

        }
    }

    public static class AlienRace_Patches
    {
        
        //public static bool DrawAddons_Prefix(bool portrait, Pawn pawn, Vector3 vector, Quaternion quat, Rot4 rotation)
        //{
        //    if (pawn.ageTracker.CurLifeStageIndex < AgeStage.Child)
        //    { return false; }
        //    return true;
        //}
       
        public static bool DrawAddons_Prefix(bool portrait, Pawn pawn, Vector3 vector, Quaternion quat, Rot4 rotation)
        {
            if (!(pawn.def is ThingDef_AlienRace alienProps)) return false;

            // don't make addon for baby & toddler 
            if (pawn.ageTracker.CurLifeStageIndex < AgeStage.Child) return false;
            
            List<AlienPartGenerator.BodyAddon> addons = alienProps.alienRace.generalSettings.alienPartGenerator.bodyAddons;
            AlienPartGenerator.AlienComp alienComp = pawn.GetComp<AlienPartGenerator.AlienComp>();
            for (int i = 0; i < addons.Count; i++)
            {
                AlienPartGenerator.BodyAddon ba = addons[index: i];

                if (!ba.CanDrawAddon(pawn: pawn)) continue;

                AlienPartGenerator.RotationOffset offset = rotation == Rot4.South ?
                                                               ba.offsets.south :
                                                               rotation == Rot4.North ?
                                                                   ba.offsets.north :
                                                                   rotation == Rot4.East ?
                                                                    ba.offsets.east :
                                                                    ba.offsets.west;

                Vector2 bodyOffset = (portrait ? offset?.portraitBodyTypes ?? offset?.bodyTypes : offset?.bodyTypes)?.FirstOrDefault(predicate: to => to.bodyType == pawn.story.bodyType)
                                   ?.offset ?? Vector2.zero;
                Vector2 crownOffset = (portrait ? offset?.portraitCrownTypes ?? offset?.crownTypes : offset?.crownTypes)?.FirstOrDefault(predicate: to => to.crownType == alienComp.crownType)
                                    ?.offset ?? Vector2.zero;

                //Defaults for tails 
                //south 0.42f, -0.3f, -0.22f
                //north     0f,  0.3f, -0.55f
                //east -0.42f, -0.3f, -0.22f   

                float moffsetX = 0.42f;
                float moffsetZ = -0.22f;
                float moffsetY = ba.inFrontOfBody ? 0.3f + ba.layerOffset : -0.3f - ba.layerOffset;
                float num = ba.angle;

                if (rotation == Rot4.North)
                {
                    moffsetX = 0f;
                    moffsetY = !ba.inFrontOfBody ? -0.3f - ba.layerOffset : 0.3f + ba.layerOffset;
                    moffsetZ = -0.55f;
                    num = 0;
                }

                moffsetX += bodyOffset.x + crownOffset.x;
                moffsetZ += bodyOffset.y + crownOffset.y;

                if (rotation == Rot4.East)
                {
                    moffsetX = -moffsetX;
                    num = -num; //Angle
                }

                Vector3 offsetVector = new Vector3(x: moffsetX, y: moffsetY, z: moffsetZ);

                Material dmat = alienComp.addonGraphics[index: i].MatAt(rot: rotation);
                Vector3 rootloc = vector;

                // adjust tail scale
                if (pawn.ageTracker.CurLifeStageIndex == AgeStage.Child && !ba.inFrontOfBody)
                {
                    const float TextureScaleX = 1.225f; 
                    const float TextureScaleY = 1.225f;    
                    const float TextureOffsetX = -0.09f;  
                    const float TextureOffsetY = 0f;
                    const float dVectorOffsetX = 0.16f;  
                    const float dVectorOffsetY = 0f;   
                    const float dVectorOffsetZ = 0.32f;                     
                    Material xmat = new Material(dmat);

                    //float TextureScaleX = CnP_Settings.option_debug_scale_X;
                    //float TextureScaleY = CnP_Settings.option_debug_scale_Y;
                    //float TextureOffsetX = CnP_Settings.option_debug_offset_X;
                    //float TextureOffsetY = CnP_Settings.option_debug_offset_Y;
                    //float dVectorOffsetX = 0.16f;  // CnP_Settings.option_debug_loc_X;
                    //float dVectorOffsetY = 0f;   // CnP_Settings.option_debug_loc_Y; 
                    //float dVectorOffsetZ = 0.32f;   // CnP_Settings.option_debug_loc_Z; 

                    if (rotation == Rot4.East) offsetVector.x += dVectorOffsetX; 
                    if (rotation == Rot4.West) offsetVector.x -= dVectorOffsetX;

                    offsetVector.y += dVectorOffsetY;
                    offsetVector.z += dVectorOffsetZ;
                    
                    xmat.mainTextureScale = new Vector2(TextureScaleX, TextureScaleY);
                    xmat.mainTextureOffset = new Vector2(TextureOffsetX, TextureOffsetY);
                    dmat = xmat;
                }
                //////////////////////////////////////////////////////////////////////////////////////////////////

                GenDraw.DrawMeshNowOrLater(mesh: alienComp.addonGraphics[index: i].MeshAt(rot: rotation), loc: rootloc + offsetVector.RotatedBy(angle: Mathf.Acos(f: Quaternion.Dot(a: Quaternion.identity, b: quat)) * 2f * 57.29578f),
                    quat: Quaternion.AngleAxis(angle: num, axis: Vector3.up) * quat, mat: dmat, drawNow: portrait);
            }
            return false;
         }

        //// change head Scale for alien race child
        public static void AlienChildHeadMatAt(PawnGraphicSet __instance, ref Material __result)
        {
            Pawn pawn = __instance.pawn;
            bool isAgechild = pawn.ageTracker.CurLifeStageIndex == 2;
            bool isHumanlikeHead = pawn.def.defName == "Human" || pawn.def.defName == "Ratkin";
            if (pawn.RaceProps.Humanlike && isAgechild && !isHumanlikeHead)
            {
                const float CHeadTextureScaleX = 1.225f;     // BnC_Settings.option_debug_scale_X;
                const float CHeadTextureScaleY = 1.225f;    // BnC_Settings.option_debug_scale_Y;
                const float CHeadTextureOffsetX = -0.105f;  // BnC_Settings.option_debug_offset_X;
                const float CHeadTextureOffsetY = -0.10f;   // BnC_Settings.option_debug_offset_Y; 

                Material xMat = new Material(__result);
                xMat.mainTextureScale = new Vector2(CHeadTextureScaleX, CHeadTextureScaleY);
                xMat.mainTextureOffset = new Vector2(CHeadTextureOffsetX, CHeadTextureOffsetY);
                __result = xMat;
            }
        }

        // if alien race bodytype has thin > skip, else give random bodytype 
        public static void ModifyChildBodyTypePostfix(Pawn pawn, ref BodyTypeDef __result)
        {
            bool IsAgechild = pawn.ageTracker.CurLifeStageIndex == 2;            
            if (IsAgechild)
            {
                if (pawn.def is ThingDef_AlienRace alienProps && !alienProps.alienRace.generalSettings.alienPartGenerator.alienbodytypes.NullOrEmpty() &&
                !alienProps.alienRace.generalSettings.alienPartGenerator.alienbodytypes.Contains(item: BodyTypeDefOf.Thin))
                {
                    __result = alienProps.alienRace.generalSettings.alienPartGenerator.alienbodytypes.RandomElement();
                }
            }            
        }

        ////// change Body Scale for alien race child
        //public static void AlienChildMatsBodyBaseAt(PawnGraphicSet __instance, ref List<Material> __result)
        //{
        //    Pawn pawn = __instance.pawn;
        //    bool isAgechild = pawn.ageTracker.CurLifeStageIndex == 2;
        //    bool isHumanlikeHead = pawn.def.defName == "Human" || pawn.def.defName == "Ratkin";
        //    if (pawn.RaceProps.Humanlike && isAgechild && !isHumanlikeHead)
        //    {
        //        //const float CBodyTextureScaleX = 1.06f;     // BnC_Settings.option_debug_scale_X;
        //        //const float CBodyTextureScaleY = 1.225f;    // BnC_Settings.option_debug_scale_Y;
        //        //const float CBodyTextureOffsetX = -0.024f;  // BnC_Settings.option_debug_offset_X;
        //        //const float CBodyTextureOffsetY = -0.2f;   // BnC_Settings.option_debug_offset_Y; 

        //        float CBodyTextureScaleX = BnC_Settings.option_debug_scale_X;
        //        float CBodyTextureScaleY = BnC_Settings.option_debug_scale_Y;
        //        float CBodyTextureOffsetX = BnC_Settings.option_debug_offset_X;
        //        float CBodyTextureOffsetY = BnC_Settings.option_debug_offset_Y; 

        //        Material xMat = new Material(__result[0]);
        //        xMat.mainTextureScale = new Vector2(CBodyTextureScaleX, CBodyTextureScaleY);
        //        xMat.mainTextureOffset = new Vector2(CBodyTextureOffsetX, CBodyTextureOffsetY);
        //        __result[0] = xMat;
        //    }
        //}

        public static void ChangeAliensProperty()
        {
            List<ThingDef_AlienRace> CurrentAliensdef = new List<ThingDef_AlienRace>();
            string st = "";
            const float Max_lifeExpectancy = 355f;
            ThingDef babycloth = DefDatabase<ThingDef>.GetNamed("Apparel_Baby_Onesie");
            ThingDef babyhat = DefDatabase<ThingDef>.GetNamed("Apparel_Baby_Beanie");
            RaceRestrictionSettings.apparelWhiteDict.Add(key: babycloth, value: new List<ThingDef_AlienRace>());
            RaceRestrictionSettings.apparelWhiteDict.Add(key: babyhat, value: new List<ThingDef_AlienRace>());

            foreach (ThingDef_AlienRace ar in DefDatabase<ThingDef_AlienRace>.AllDefs)
            {
                if ( ar!=null && ChildrenUtility.SupportAlienRaces.Contains(ar.defName))
                {
                    //Log.Message("[From BnC ALSupport] " + ar.defName);
                    st = st + ar.defName + ", ";
                    CurrentAliensdef.Add(ar);
                    ChildrenUtility.CurrentAlienRaces.Add(ar.defName);
                    //set max lifeExpectancy
                    if (ar.race.lifeExpectancy > Max_lifeExpectancy) ar.race.lifeExpectancy = Max_lifeExpectancy;
                    RaceRestrictionSettings.apparelWhiteDict[key: babycloth].Add(item: ar);
                    RaceRestrictionSettings.apparelWhiteDict[key: babyhat].Add(item: ar);
                }
            }

            if (CurrentAliensdef.Count == 0)
            {
                Log.Message("[From BnC ArSupport] CurrentAliens is null");
                return;
            }
            
            Log.Message("[From BnC ArSupport] Using alien races now : " + st);
            ChangeStageAgesCurve(CurrentAliensdef);

            foreach (PawnKindDef pkd in from k in DefDatabase<PawnKindDef>.AllDefs
                                        where k.RaceProps.Humanlike
                                        select k)
            {
                if (pkd.RaceProps.lifeStageAges.Count > 3)
                {
                    if ((float)pkd.minGenerationAge > pkd.RaceProps.lifeStageAges[2].minAge + 2f)
                    {
                        pkd.minGenerationAge = (int)pkd.RaceProps.lifeStageAges[2].minAge + 2;
                        //Log.Message("genAge Change - " + pkd.defName + "  to  >>  " + pkd.minGenerationAge);
                    }

                }
            }
        }

        public static void ChangeStageAgesCurve(List<ThingDef_AlienRace> CurrentAliensdef)
        {
            foreach (ThingDef_AlienRace ar in CurrentAliensdef)
            {
                if (ar.defName != "Ratkin")
                {
                    // change life stage
                    List<LifeStageAge> lifeStageAges = ar.race.lifeStageAges;
                    lifeStageAges[1].minAge *= 0.75f;
                    lifeStageAges[2].minAge *= 0.75f;

                    SimpleCurve newAgeCurve = new SimpleCurve
                    {
                        {
                            new CurvePoint(ar.race.lifeStageAges[2].minAge, 0f),              //3
                            true
                        },
                        {
                            new CurvePoint(ar.race.lifeStageAges[2].minAge+2f, 10f),              //5  
                            true
                        },
                        {
                            new CurvePoint((ar.race.lifeStageAges[2].minAge+ar.race.lifeStageAges[3].minAge)/2f, 40f),              //6 
                            true
                        },
                        {
                            new CurvePoint(ar.race.lifeStageAges[3].minAge, 75f),          //14
                            true
                        },
                        {
                            new CurvePoint(ar.race.lifeStageAges[4].minAge + 3f, 95f),            //23
                            true
                        },
                        {
                            new CurvePoint(ar.race.lifeStageAges[3].minAge + 10f, 85f),           //30
                            true
                        },
                        {
                            new CurvePoint((ar.race.lifeStageAges[3].minAge+ ar.race.lifeExpectancy)/2f, 30f),            //60
                            true
                        },
                        {
                            new CurvePoint(ar.race.lifeExpectancy, 9f),             //80
                            true
                        },
                        {
                            new CurvePoint(ar.race.lifeExpectancy * 100f /80f, 0f),             //100
                            true
                        }
                    };
                    // change curve
                    ar.race.ageGenerationCurve = newAgeCurve;
                }                
            }
        }

    }
}