using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace VFEInsectoids
{
    public enum InsectType { Worker, Defender, Hunter};
    public class PawnKindWithType
    {
        public PawnKindDef insect;
        public InsectType insectType;
    }

    public class CompProperties_Hive : CompProperties_SpawnerPawn
    {
        public List<PawnKindWithType> insectTypes;
        public int insectoidRespawnTime;
        public CompProperties_Hive()
        {
            this.compClass = typeof(CompHive);
        }
    }

    [HotSwappable]
    public class CompHive : CompSpawnerPawn
    {
        public CompProperties_Hive Props => base.props as CompProperties_Hive;

        public const int MaxInsectCapacity = 24;
        public Lord lord;

        public Color insectColor;

        public List<Pawn> insects = new List<Pawn>();

        public int additionalCapacity;
        public int InsectCapacity
        {
            get
            {
                var baseValue = VFEInsectoidsSettings.baseArtificalInsectCount;
                if (parent is Pawn)
                {
                    baseValue += 2;
                }
                else
                {
                    if (VFEI_DefOf.VFEI2_StandardHivetech.IsFinished)
                    {
                        baseValue += 1;
                    }
                    if (VFEI_DefOf.VFEI2_ExoticHivetech.IsFinished)
                    {
                        baseValue += 1;
                    }
                }
                return Mathf.Min(baseValue + additionalCapacity, MaxInsectCapacity);
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            DrawArtificialHiveOverlay(parent.Position, parent.def, parent.Map, VFEInsectoidsSettings.minHiveStabilityDistance);
        }

        public static readonly Color ArtificialHiveRingColor = new Color(0.8f, 0.49f, 0.43f);

        public static void DrawArtificialHiveOverlay(IntVec3 pos, ThingDef def, Map map, float radius)
        {
            GenDraw.DrawRadiusRing(pos, radius, ArtificialHiveRingColor);
            foreach (Thing item in GetAllNearbyArtificialHives(pos, map))
            {
                GenDraw.DrawLineBetween(GenThing.TrueCenter(pos, Rot4.North, def.size, def.Altitude), item.TrueCenter(), SimpleColor.Red);
            }
        }

        public float MaintenanceDurationOverride(float duration) => duration / MaintenanceMultiplier();

        public float MaintenanceMultiplier()
        {
            var mult = 1f;
            if (parent.MapHeld != null)
            {
                foreach (var other in GetAllNearbyArtificialHives(parent.Position, parent.MapHeld).Where(x => x != parent))
                {
                    mult += VFEInsectoidsSettings.stabilityHiveMaintenancePenalty;
                }
            }
            return mult;
        }

        public static IEnumerable<Thing> GetAllNearbyArtificialHives(IntVec3 pos, Map map)
        {
            var allNearbyHives = new List<Thing>();
            foreach (var def in Utils.allArtificialHiveDefs)
            {
                allNearbyHives.AddRange(map.listerThings.ThingsOfDef(def).Where(x => x.Position.DistanceTo(pos) < VFEInsectoidsSettings.minHiveStabilityDistance));
            }
            return allNearbyHives;
        }


        public List<PawnKindDef> AllAvailableInsects => Props.insectTypes.Select(x => x.insect).ToList();

        public override void Initialize(CompProperties props)
        {
            this.props = props;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            pawnsLeftToSpawn = 0;
            if (chosenKind is null)
            {
                chosenKind = AllAvailableInsects.First();
                insectColor = VFEI_DefOf.Structure_BrownSubtle.color;
                if (this.parent.def.defName== "VFEI2_AA_ArtificialBlackHive")
                {
                    insectColor = Color.white;
                }
                
                if (CanSpawn())
                {
                    SetNextRespawnTick();
                }
            }

            EnsureLord();
        }

        public void EnsureLord()
        {
            if (lord != null && lord.Map != parent.MapHeld)
            {
                RemoveLord();
            }
            if (lord is null && parent.Spawned)
            {
                lord = LordMaker.MakeNewLord(parent.Faction, new LordJob_PlayerHive(parent), parent.Map);
                if (parent is Building building)
                {
                    lord.AddBuilding(building);
                }
            }
            if (lord != null)
            {
                foreach (var insect in insects)
                {
                    var insectLord = insect.GetLord();
                    if (insect.Spawned && insect.Map == parent.MapHeld && insectLord != lord
                        && insectLord?.LordJob is not LordJob_FormAndSendCaravan)
                    {
                        insectLord?.RemovePawn(insect);
                        lord.AddPawn(insect);
                    }
                }
            }
        }

        private void SetNextRespawnTick()
        {
            nextPawnSpawnTick = Find.TickManager.TicksGame + Props.insectoidRespawnTime;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction == Faction.OfPlayer)
            {
                yield return new Gizmo_Hive
                {
                    compHive = this
                };
                if (DebugSettings.ShowDevGizmos && CanSpawn())
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "DEV: Spawn " + chosenKind.label,
                        action = delegate
                        {
                            DoSpawn();
                        }
                    };
                }
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            foreach (var insect in insects.ToList())
            {
                RemoveInsect(insect);
            }
            if (lord != null)
            {
                RemoveLord();
            }
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map);
            if (lord != null)
            {
                RemoveLord();
            }
        }

        private void RemoveLord()
        {
            lord.RemoveAllBuildings();
            lord.RemoveAllPawns();
            lord.Map?.lordManager.RemoveLord(lord);
            lord = null;
        }

        public override void CompTick()
        {
            if (parent.IsHashIntervalTick(30))
            {
                if (CanSpawn())
                {
                    if (nextPawnSpawnTick == -1)
                    {
                        SetNextRespawnTick();
                    }
                    if (Find.TickManager.TicksGame >= nextPawnSpawnTick)
                    {
                        DoSpawn();
                    }
                }
                else
                {
                    nextPawnSpawnTick = -1;
                }
            }
        }

        public void DoSpawn(bool sendMessage = true)
        {
            TrySpawnPawn(parent.Position, chosenKind.race.race.lifeStageAges[0].minAge);
            if (insects.Count >= InsectCapacity)
            {
                nextPawnSpawnTick = -1;
                if (sendMessage)
                {
                    Messages.Message("VFEI_SpawningStoppedMaxCapacity".Translate(), parent, MessageTypeDefOf.NeutralEvent);
                }
            }
            else
            {
                SetNextRespawnTick();
            }
        }

        public bool CanSpawn()
        {
            return insects.Count < InsectCapacity;
        }

        public void TrySpawnPawn(IntVec3 position, float age)
        {
            TrySpawnPawn(position, age, parent.Map);
        }

        public void TrySpawnPawn(IntVec3 position, float age, Map map)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(chosenKind, parent.Faction);
            request.FixedBiologicalAge = age;
            var pawn = PawnGenerator.GeneratePawn(request);
            AddInsect(pawn);
            GenSpawn.Spawn(pawn, position, map);
            EnsureLord();
            if (Props.spawnSound != null)
            {
                Props.spawnSound.PlayOneShot(pawn);
            }
        }

        public void ChangePawnKind(PawnKindDef def)
        {
            chosenKind = def;
            foreach (var insect in insects.ToList())
            {
                if (insect.Spawned is false)
                {
                    SetToBeCocooned(insect);
                }
                else
                {
                    SpawnCocoon(insect);
                }
            }
        }

        public void SetToBeCocooned(Pawn insect)
        {
            var hediff = insect.health.AddHediff(VFEI_DefOf.VFEI_ToBeCocooned) as Hediff_ToBeCocooned;
            hediff.hive = this.parent;
        }

        public void SpawnCocoon(Pawn insect)
        {
            var pos = insect.Position;
            var cocoon = GenSpawn.Spawn(VFEI_DefOf.VFEI2_InsectoidCocoonHive, pos, insect.Map)
                as CocoonHive;
            insect.DeSpawn();
            cocoon.innerContainer.TryAdd(insect);
            cocoon.hive = this.parent;
            cocoon.spawnInTick = 6000;
        }

        public void AddInsect(Pawn insect)
        {
            if (insect.IsColonyInsect(out var hediff))
            {
                insect.health.RemoveHediff(hediff);
            }
            var hediffDef = Props.insectTypes.First(x => x.insect == chosenKind).insectType.GetInsectTypeHediff();
            hediff = insect.health.AddHediff(hediffDef) as Hediff_InsectType;
            hediff.hive = this.parent;
            insects.Add(insect);
            insect.GetLord()?.RemovePawn(insect);
            EnsureLord();
        }

        public void RemoveInsect(Pawn insect)
        {
            if (insect.IsColonyInsect(out var hediff))
            {
                insect.health.RemoveHediff(hediff);
            }
            insects.Remove(insect);
            var lordJob = insect.GetLord()?.LordJob as LordJob_PlayerHive;
            if (lordJob != null)
            {
                lordJob.lord.RemovePawn(insect);
            }
        }

        public override string CompInspectStringExtra()
        {
            var sb = new StringBuilder();
            if (CanSpawn() && nextPawnSpawnTick != -1)
            {
                var period = nextPawnSpawnTick - Find.TickManager.TicksGame;
                sb.AppendLine("VFEI_InsectoidWillBeSpawnedIn".Translate(chosenKind.LabelCap, 
                    period.ToStringTicksToPeriod()));
            }
            if (parent.GetComp<CompMaintainable>() != null)
            {
                sb.AppendLine("VFEI_MaintenanceLossSpeed".Translate(MaintenanceMultiplier().ToStringPercent()));
            }
            return sb.ToString().TrimEndNewlines();
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref insectColor, "insectColor");
            Scribe_Collections.Look(ref insects, "insects", LookMode.Reference);
            Scribe_References.Look(ref lord, "lord");
            Scribe_Values.Look(ref additionalCapacity, "additionalCapacity");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                insects ??= new List<Pawn>();
            }
        }
    }
}
