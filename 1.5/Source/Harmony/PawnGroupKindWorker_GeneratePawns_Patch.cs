﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace VFEInsectoids
{
    [HarmonyPatch(typeof(PawnGroupKindWorker), "GeneratePawns", new Type[]
    {
        typeof(PawnGroupMakerParms), typeof(PawnGroupMaker), typeof(bool)
    })]
    public static class PawnGroupKindWorker_GeneratePawns_Patch
    {
        public static bool Prefix(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, ref List<Pawn> __result)
        {
            if (parms.faction == Faction.OfInsects)
            {
                __result = GenerateInsectPawns(parms);
                return false;
            }
            return true;
        }

        public static InsectGenelineDef forcedSecondGeneline;
        public static List<Pawn> GenerateInsectPawns(PawnGroupMakerParms parms)
        {
            var otherGeneline = forcedSecondGeneline ?? DefDatabase<InsectGenelineDef>.AllDefsListForReading
                .Where(x => x != VFEI_DefOf.VFEI_Sorne).RandomElement();
            var points = parms.points;
            var sornePoints = parms.points * 0.7f;
            var otherGenelinePoints = parms.points - sornePoints;
            var insects = new List<Pawn>();
            GenerateInsects(parms, VFEI_DefOf.VFEI_Sorne, insects, sornePoints);
            GenerateInsects(parms, otherGeneline, insects, otherGenelinePoints);
            return insects;
        }

        public static void GenerateInsects(this PawnGroupMakerParms parms, InsectGenelineDef genelineDef, List<Pawn> __result, float points)
        {
            while (true)
            {
                var insect = GenerateInsect(parms.faction, points, genelineDef.insects);
                if (insect != null)
                {
                    points -= insect.kindDef.combatPower;
                    __result.Add(insect);
                }
                else
                {
                    break;
                }
            }
        }

        private static Pawn GenerateInsect(Faction faction, float curPoints, IEnumerable<PawnGenOption> source)
        {
            var chosenKind = RandomPawnKindDef(curPoints, source);
            if (chosenKind != null)
            {
                PawnGenerationRequest request = new PawnGenerationRequest(chosenKind, faction);
                int index = chosenKind.lifeStages.Count - 1;
                request.FixedBiologicalAge = chosenKind.race.race.lifeStageAges[index].minAge;
                return PawnGenerator.GeneratePawn(request);
            }
            return null;
        }

        private static PawnKindDef RandomPawnKindDef(float curPoints, IEnumerable<PawnGenOption> source)
        {
            source = source.Where((PawnGenOption x) => curPoints >= x.kind.combatPower);
            if (source.TryRandomElementByWeight(x => x.selectionWeight, out var result))
            {
                return result.kind;
            }
            return null;
        }
    }
}