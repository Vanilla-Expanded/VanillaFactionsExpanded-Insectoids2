﻿using RimWorld;
using Verse;

namespace VFEInsectoids
{
    [DefOf]
    public static class VFEI_DefOf
    {

        static VFEI_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(VFEI_DefOf));
        }

        public static FactionDef VFEI2_Hive;

        public static PawnKindDef VFEI2_Swarmling;
        public static PawnKindDef Megascarab;
        public static PawnKindDef Spelopede;
        public static PawnKindDef Megaspider;
        public static PawnKindDef VFEI2_Queen;
        public static PawnKindDef VFEI2_Megapede;

        public static SoundDef Hive_Spawn;

        public static ThingDef VFEI2_InsectoidCocoon;
        public static ThingDef Filth_RubbleRock;

        public static HediffDef VFEI2_ArmorDegradation;
        public static HediffDef VFEI2_TeramantisStun;

        public static PawnRenderTreeDef VFEI2_Unarmored;

        public static DamageDef VFEI2_AcidSpit;

        public static ThingDef VFEI2_InfestedShipChunk, VFEI2_InfestedShipPart, VFEI2_InfestedShipModule, VFEI_InfestedMeteorIncoming;
        public static InsectGenelineDef VFEI_Sorne;
        public static TerrainDef VFEI2_Creep;
    }
}
