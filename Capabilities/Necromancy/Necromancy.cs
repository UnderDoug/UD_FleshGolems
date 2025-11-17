using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using Genkit;
using Qud.API;

using XRL.UI;
using XRL.Wish;
using XRL.Rules;
using XRL.Language;
using XRL.Collections;
using XRL.World;
using XRL.World.AI;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.Anatomy;
using XRL.World.ObjectBuilders;

using static XRL.World.Parts.UD_FleshGolems_CorpseReanimationHelper;
using static XRL.World.Parts.Mutation.UD_FleshGolems_NanoNecroAnimation;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using SerializeField = UnityEngine.SerializeField;
using XRL;
using XRL.World.Parts;

namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        public enum CountsAs : int
        {
            None = -1,
            Any = 0,
            Blueprint = 1,
            Population = 2,
            Faction = 3,
            Species = 4,
            Genotype = 5,
            Subtype = 6,
            OtherCorpse = 7,
        }

        public const string CACHE_EMPTY = "empty";

        [ModSensitiveStaticCache(CreateEmptyInstance = false)]
        [GameBasedStaticCache(ClearInstance = false)]
        public static StringMap<CorpseSheet> CorpseSheetByEntity = new();

        public const string IGNORE_EXCLUDE_PROPTAG = "UD_FleshGolems PastLife Ignore ExcludeFromDynamicEncounters WhenFinding";
        public const string CORPSE_COUNTS_AS_PROPTAG = "UD_FleshGolems PastLife CountsAs";
        public const string PASTLIFE_BLUEPRINT_PROPTAG = "UD_FleshGolems_PastLife_Blueprint";

        public static bool IsProcessableCorpse(GameObjectBlueprint Corpse, Predicate<GameObjectBlueprint> Filter)
        {
            return Corpse.IsCorpse(Filter)
                && (Corpse.HasPart(nameof(Butcherable)) || Corpse.HasPart(nameof(Harvestable)));
        }
        public static bool IsProcessableCorpse(GameObjectBlueprint Corpse, bool ExcludeBase)
        {
            return IsProcessableCorpse(Corpse, ExcludeBase ? IsNotBaseBlueprint : null);
        }
        public static bool IsProcessableCorpse(GameObjectBlueprint Corpse)
        {
            return IsProcessableCorpse(Corpse, true);
        }
    }
}
