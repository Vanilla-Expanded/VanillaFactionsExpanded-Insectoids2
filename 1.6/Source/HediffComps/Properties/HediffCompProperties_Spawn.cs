﻿using Verse;

namespace VFEInsectoids
{
    public class HediffCompProperties_Spawn : HediffCompProperties
    {


        public PawnKindDef spawn;

        public HediffCompProperties_Spawn()
        {
            this.compClass = typeof(HediffComp_Spawn);
        }
    }
}
