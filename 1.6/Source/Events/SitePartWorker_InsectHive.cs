﻿using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse.Grammar;

namespace VFEInsectoids
{
    public class SitePartWorker_InsectHive : SitePartWorker
    {
        public override bool FactionCanOwn(Faction faction)
        {
            return faction == Faction.OfInsects;
        }

        public override void Notify_GeneratedByQuestGen(SitePart part, Slate slate, List<Rule> outExtraDescriptionRules, Dictionary<string, string> outExtraDescriptionConstants)
        {
            base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);
            string count = GetInsectoidCount(slate.Get<int>("challengeRating")).ToString();
            outExtraDescriptionRules.Add(new Rule_String("count", count));
        }

        public override SitePartParams GenerateDefaultParams(float myThreatPoints, PlanetTile tile, Faction faction)
        {
            SitePartParams sitePartParams = base.GenerateDefaultParams(myThreatPoints, tile, faction);
            sitePartParams.threatPoints = Mathf.Max(sitePartParams.threatPoints, faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Settlement));
            return sitePartParams;
        }

        public int GetInsectoidCount(int rating)
        {
            switch (rating)
            {
                case 1: return 8;
                case 2: return 12;
                case 3: return 16;
            }
            return -1;
        }
    }
}
