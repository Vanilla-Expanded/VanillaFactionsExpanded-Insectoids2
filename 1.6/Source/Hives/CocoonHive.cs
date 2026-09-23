using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEInsectoids
{
    public class CocoonHive : ThingWithComps, IThingHolder
    {
        public int spawnInTick;
        public Thing hive;
        public ThingOwner<Pawn> innerContainer;

        public CocoonHive()
        {
            innerContainer = new ThingOwner<Pawn>(this, oneStackOnly: false);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref spawnInTick, "spawnInTick");
            Scribe_References.Look(ref hive, "hive");
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public override string GetInspectString()
        {
            return "VFEI_CocoonInsectSpawnIn".Translate(spawnInTick.ToStringTicksToPeriod());
        }

        public override void Tick()
        {
            base.Tick();
            this.spawnInTick--;
            if (this.spawnInTick <= 0)
            {
                var comp = hive.TryGetComp<CompHive>();
                TryRemoveInsect(comp, out var insectAge);
                comp.TrySpawnPawn(Position, insectAge, Map);
                FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Slime);
                innerContainer.ClearAndDestroyContents();
                this.Destroy();
            }
        }

        private void TryRemoveInsect(CompHive comp, out float insectAge)
        {
            insectAge = 0;
            var insect = innerContainer.InnerListForReading.FirstOrDefault() as Pawn;
            if (insect != null)
            {
                insectAge = insect.ageTracker.AgeBiologicalYearsFloat;
                comp.RemoveInsect(insect);
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            var comp = hive.TryGetComp<CompHive>();
            TryRemoveInsect(comp, out _);
        }
    }
}