using Verse;
using Verse.AI.Group;

namespace VFEInsectoids
{
    public class LordJob_PlayerHive : LordJob
    {
        public override bool CanBlockHostileVisitors => false;

        public override bool AddFleeToil => false;

        public override bool KeepExistingWhileHasAnyBuilding => true;

        public override bool ShouldExistWithoutPawns => true;

        public Thing hive;

        public LordJob_PlayerHive()
        {

        }

        public LordJob_PlayerHive(Thing hive)
        {
            this.hive = hive;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            stateGraph.AddToil(new LordToil_PlayerHive(hive));
            return stateGraph;
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref hive, "hive");
        }
    }
}
