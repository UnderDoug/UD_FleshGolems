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
using XRL;
using XRL.Collections;
using XRL.World;
using XRL.World.Parts;
using XRL.World.AI;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.Anatomy;
using XRL.World.ObjectBuilders;

using static XRL.World.Parts.UD_FleshGolems_CorpseReanimationHelper;
using static XRL.World.Parts.Mutation.UD_FleshGolems_NanoNecroAnimation;

using UD_FleshGolems;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using SerializeField = UnityEngine.SerializeField;

using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;
namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        [Serializable]
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

        [Serializable] public partial class CorpseSheet : IComposite { }

        public const string CACHE_EMPTY = "empty";

        public const string IGNORE_EXCLUDE_PROPTAG = "UD_FleshGolems PastLife Ignore ExcludeFromDynamicEncounters WhenFinding";
        public const string CORPSE_COUNTS_AS_PROPTAG = "UD_FleshGolems PastLife CountsAs";
        public const string PASTLIFE_BLUEPRINT_PROPTAG = "UD_FleshGolems_PastLife_Blueprint";

        public static bool IsProcessableCorpse(GameObjectBlueprint Corpse, Predicate<GameObjectBlueprint> Filter)
            => Corpse.IsCorpse(Filter)
            && (Corpse.HasPart(nameof(Butcherable)) || Corpse.HasPart(nameof(Harvestable)));

        public static bool IsProcessableCorpse(GameObjectBlueprint Corpse, bool ExcludeBase)
            => IsProcessableCorpse(Corpse, ExcludeBase ? IsNotBaseBlueprint : null);

        public static bool IsProcessableCorpse(GameObjectBlueprint Corpse)
            => IsProcessableCorpse(Corpse, true);

        public static List<GameObjectBlueprint> GetCorpseBlueprintModels(bool ForCache = false)
        {
            List<GameObjectBlueprint> blueprintsList = new();
            CorpseSheets ??= new();
            if (!ForCache && !CorpseSheets.IsNullOrEmpty())
            {
                foreach ((string blueprint, CorpseSheet _) in CorpseSheets)
                {
                    blueprintsList.Add(blueprint.GetGameObjectBlueprint());
                }
            }
            else
            {
                int counter = 0;
                foreach (GameObjectBlueprint blueprint in GameObjectFactory.Factory.BlueprintList)
                {
                    if (ForCache && counter++ % 100 == 0)
                    {
                        Startup.SetLoadingStatusCaching();
                    }
                    if (blueprint.IsCorpse())
                    {
                        CorpseSheets[blueprint.Name] = new CorpseSheet(new CorpseBlueprint(blueprint.Name));
                        blueprintsList.Add(blueprint);
                    }
                }
            }
            return blueprintsList;
        }
        public static List<string> GetCorpseBlueprints(bool ForCache = false)
        {
            return GetCorpseBlueprintModels(ForCache).ConvertAll(bp => bp.Name);
        }
        public static List<GameObjectBlueprint> GetEntitiesWithCorpseBlueprints(bool ForCache = false)
        {
            /*
            List<GameObjectBlueprint> entityBlueprints = new();
            CorpseSheets ??= new();
            if (!ForCache && !EntitiesWithCorpseBlueprints.IsNullOrEmpty())
            {
                foreach (GameObjectBlueprint blueprint in EntitiesWithCorpseBlueprints.Values)
                {
                    entityBlueprints.Add(blueprint);
                }
            }
            else
            {
                int counter = 0;
                foreach (GameObjectBlueprint blueprint in GameObjectFactory.Factory.BlueprintList)
                {
                    if (ForCache && counter++ % 250 == 0)
                    {
                        Startup.SetLoadingStatusCaching();
                    }
                    if (!blueprint.IsBaseBlueprint()
                        && !blueprint.IsChiliad()
                        && blueprint.TryGetCorpseBlueprintAndChance(out string corpseBlueprint, out int corpseChance)
                        && corpseBlueprint.IsCorpse())
                    {
                        if (TryGetCorpseSheet(corpseBlueprint, out CorpseSheet corpseSheet))
                        {
                            corpseSheet.AddEntity(blueprint, corpseChance, )
                        }
                        EntitiesWithCorpseBlueprints[blueprint.Name] = blueprint;
                        CorpseByEntity[blueprint.Name] = corpseBlueprint;
                        CacheEntityWithWeightByCorpse(corpseBlueprint, new BlueprintWeightPair(blueprint.Name, corpseChance));
                        entityBlueprints.Add(blueprint);
                    }
                }
            }
            return entityBlueprints;
            */
            return new();
        }
    }
}
