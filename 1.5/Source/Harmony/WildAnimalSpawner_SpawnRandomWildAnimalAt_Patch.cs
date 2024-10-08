﻿using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;
using Verse.AI.Group;

namespace VFEInsectoids
{
    [HarmonyPatch(typeof(WildAnimalSpawner), "SpawnRandomWildAnimalAt")]
    public static class WildAnimalSpawner_SpawnRandomWildAnimalAt_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var allWildAnimals = AccessTools.PropertyGetter(typeof(BiomeDef), "AllWildAnimals");
            var spawnPawn = AccessTools.Method(typeof(GenSpawn), "Spawn", new Type[] { typeof(Thing),
                typeof(IntVec3), typeof(Map), typeof(WipeMode)});
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.Calls(allWildAnimals))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(WildAnimalSpawner_SpawnRandomWildAnimalAt_Patch), "TryOverrideWildAnimals"));
                }
                else if (instruction.Calls(spawnPawn))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(WildAnimalSpawner_SpawnRandomWildAnimalAt_Patch), 
                        "TryAddLordJob"));
                }
            }
        }

        public static List<PawnGenOption> royalInsects = new List<PawnGenOption>
        {
            new PawnGenOption{ kind = VFEI_DefOf.VFEI2_RoyalMegascarab, selectionWeight 
                = VFEI_DefOf.VFEI_Sorne.insects.Find(x => x.kind == PawnKindDefOf.Megascarab).selectionWeight},
            new PawnGenOption{ kind = VFEI_DefOf.VFEI2_RoyalSpelopede, selectionWeight
                = VFEI_DefOf.VFEI_Sorne.insects.Find(x => x.kind == PawnKindDefOf.Spelopede).selectionWeight},
            new PawnGenOption{ kind = VFEI_DefOf.VFEI2_RoyalMegaspider, selectionWeight
                = VFEI_DefOf.VFEI_Sorne.insects.Find(x => x.kind == PawnKindDefOf.Megaspider).selectionWeight},
        };

        public static IEnumerable<PawnKindDef> TryOverrideWildAnimals(IEnumerable<PawnKindDef> wildAnimals,
            WildAnimalSpawner spawner)
        {
            if (spawner.map.IsInfested() && Rand.Chance(0.75f))
            {
                var insects = VFEI_DefOf.VFEI_Sorne.insects.Select(x => x.kind);
                if (spawner.map.Parent is Settlement settlement && settlement.Faction == Faction.OfInsects)
                {
                    insects = insects.Concat(royalInsects.Select(x => x.kind));
                }
                return insects;
            }
            return wildAnimals;
        }

        public static Thing TryAddLordJob(Thing thing, WildAnimalSpawner spawner)
        {
            if (thing is Pawn pawn)
            {
                var map = spawner?.map ?? pawn.Map;
                if (map != null && pawn.Spawned && map.IsInfested() && pawn.RaceProps.Insect)
                {
                    var hive = map.listerThings.AllThings.OfType<Hive>().OrderBy(x => x.Position.DistanceTo(pawn.Position)).FirstOrDefault();
                    if (hive != null)
                    {
                        if (pawn.Faction != hive.Faction)
                        {
                            pawn.SetFaction(hive.Faction);
                        }
                        var comp = hive.GetComp<CompSpawnerPawn>();
                        comp.spawnedPawns.Add(pawn);
                        Lord lord = comp.Lord;
                        if (lord == null)
                        {
                            lord = CompSpawnerPawn.CreateNewLord(comp.parent, true, 30, comp.Props.lordJob);
                        }
                        lord.AddPawn(pawn);
                    }
                }
            }
            return thing;
        }
    }
}
