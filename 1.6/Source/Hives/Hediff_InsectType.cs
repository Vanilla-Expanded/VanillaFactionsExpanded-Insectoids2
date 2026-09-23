using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEInsectoids
{

    public class Hediff_InsectWorker : Hediff_InsectType
    {
        public override InsectType InsectType => InsectType.Worker;
    }
    public class Hediff_InsectHunter : Hediff_InsectType
    {
        public override InsectType InsectType => InsectType.Hunter;
    }
    public class Hediff_InsectDefender : Hediff_InsectType
    {
        public override InsectType InsectType => InsectType.Defender;
    }

    public abstract class Hediff_InsectType : HediffWithComps
    {
        public Thing hive;
        private CompHive _compHive;
        public CompHive CompHive =>_compHive ??= hive?.TryGetComp<CompHive>();
        public abstract InsectType InsectType { get; }
        public override bool ShouldRemove => CompHive is null || CompHive.parent.Destroyed;

        public override void PostRemoved()
        {
            base.PostRemoved();
            CompHive?.RemoveInsect(pawn);
        }

        public override void Notify_PawnKilled()
        {
            base.Notify_PawnKilled();
            CompHive?.RemoveInsect(pawn);
        }

        public override void Notify_Spawned()
        {
            base.Notify_Spawned();
            UpdateArea();
        }

        public override void Tick()
        {
            base.Tick();
            if (pawn.IsHashIntervalTick(60))
            {
                if (hive is Pawn pawnHive && pawnHive.GetLord() is Lord lord2
                    && lord2.LordJob is LordJob_FormAndSendCaravan)
                {
                    var lord = pawn.GetLord();
                    if (lord != lord2)
                    {
                        lord?.RemovePawn(pawn);
                        lord2.AddPawn(pawn);
                        if (pawn.jobs.curDriver?.asleep == true)
                        {
                            pawn.jobs.StopAll();
                        }
                    }
                }
                else if (CompHive is { } compHive)
                {
                    if (pawn.MapHeld != null && pawn.MapHeld == compHive.parent.MapHeld)
                    {
                        compHive.EnsureLord();
                        var lord = pawn.GetLord();
                        if (compHive.lord != null && lord != compHive.lord
                            && lord?.LordJob is not LordJob_FormAndSendCaravan)
                        {
                            lord?.RemovePawn(pawn);
                            compHive.lord.AddPawn(pawn);
                        }
                    }
                    else if (pawn.GetLord()?.LordJob is LordJob_PlayerHive)
                    {
                        pawn.GetLord().RemovePawn(pawn);
                    }
                }
            }
        }

        public void UpdateArea()
        {
            var area = pawn.playerSettings.AreaRestrictionInPawnCurrentMap;
            var hiveArea = pawn.Map.areaManager.Get<Area_Hive>();
            if (area != null && hiveArea == area && hiveArea.TrueCount <= 0)
            {
                pawn.playerSettings.AreaRestrictionInPawnCurrentMap = null;
            }
            else if (hiveArea != null && hiveArea != area && hiveArea.TrueCount > 0)
            {
                pawn.playerSettings.AreaRestrictionInPawnCurrentMap = hiveArea;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref hive, "hive");
        }
    }
}
