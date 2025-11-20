using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

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

using UD_FleshGolems.Logging;
using UD_FleshGolems.Capabilities.Necromancy;

using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;
using Relationship = UD_FleshGolems.Capabilities.Necromancy.CorpseEntityPair.PairRelationship;
using Debug = UD_FleshGolems.Logging.Debug;

namespace UD_FleshGolems.Capabilities
{
    [HasGameBasedStaticCache]
    [HasWishCommand]
    [Serializable]
    public class UD_FleshGolems_NecromancySystem : IScribedSystem
    {
        [UD_FleshGolems_DebugRegistry]
        public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
        {
            Registry.Register(nameof(RequireCorpseSheet), false);
            Registry.Register(nameof(RequireEntityBlueprint), false);
            Registry.Register(nameof(GetProcessableCorpsesProducts), true);
            return Registry;
        }

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

        public const string IGNORE_EXCLUDE_PROPTAG = "UD_FleshGolems PastLife Ignore ExcludeFromDynamicEncounters WhenFinding";
        public const string CORPSE_COUNTS_AS_PROPTAG = "UD_FleshGolems PastLife CountsAs";
        public const string PASTLIFE_BLUEPRINT_PROPTAG = "UD_FleshGolems_PastLife_Blueprint";

        public static UD_FleshGolems_NecromancySystem System;

        [SerializeField]
        private StringMap<CorpseSheet> Necronomicon;

        [SerializeField]
        private StringMap<EntityBlueprint> EntityBlueprints;

        [SerializeField]
        private bool CorpseSheetsInitialized;

        [SerializeField]
        private bool EntityPrimaryCorpsesInitialized;

        [SerializeField]
        private bool CorpseProductsInitialized;

        [SerializeField]
        private bool CountsAsCorspesInitialized;

        public UD_FleshGolems_NecromancySystem()
        {
            Necronomicon = new();
            EntityBlueprints = new();
            CorpseSheetsInitialized = false;
            EntityPrimaryCorpsesInitialized = false;
            CorpseProductsInitialized = false;
            CountsAsCorspesInitialized = false;
        }

        [GameBasedCacheInit]
        public static void NecromancySystemInit()
        {
            Debug.GetIndents(out Indent indent);
            System = The.Game?.RequireSystem(InitializeSystem);
            bool success = false;
            try
            {
                System?.Init();
                success = true;
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(NecromancySystemInit) + " -> " + nameof(Init), x, "game_mod_Exception");
                success = false;
            }
            finally
            {
                if (The.Game != null)
                {
                    Debug.Log(Debug.GetCallingTypeAndMethod(), "Cache finished... " + (success ? "success!" : "failure!"), indent[0]);
                }
                Debug.SetIndent(indent[0]);
            }
        }

        private static int CachingPeriods = 0;
        private static string GetPeriods(int Periods, out int NewPeriods)
        {
            NewPeriods = Periods + 1;
            return ".".ThisManyTimes(Math.Abs(3 - (Periods % 7)));
        }
        private static void SetLoadingStatusCorpses(bool OnCondition = true)
        {
            if (OnCondition) Loading.SetLoadingStatus("Assessing Corpses" + GetPeriods(CachingPeriods, out CachingPeriods));
        }

        private static UD_FleshGolems_NecromancySystem InitializeSystem() => new();

        private void Init()
        {
            Stopwatch sw = new();
            sw.Start();

            Debug.LogHeader("Starting Corpse Cache...", out Indent indent);

            InitializeCorpseSheetsCorpses(out CorpseSheetsInitialized);
            InitializeEntityPrimaryCorpses(out EntityPrimaryCorpsesInitialized);
            InititializeCorpseProducts(out CorpseProductsInitialized);
            InitializeCountsAsCorspes(out CountsAsCorspesInitialized);

            sw.Stop();
            TimeSpan duration = sw.Elapsed;
            string timeUnit = duration.Minutes > 0 ? "minutes" : "seconds";
            double timeDuration = duration.Minutes > 0 ? duration.TotalMinutes : duration.TotalSeconds;
            Debug.Log("Corpse Cache took " + timeDuration.Things(timeUnit) + ".", Indent: indent[0]);
            Debug.SetIndent(indent[0]);
        }

        public bool TryGetCorpseSheet(string Corpse, out CorpseSheet CorpseSheet)
        {
            Necronomicon ??= new();
            if (Necronomicon[Corpse] is CorpseSheet cachedCorpseSheet)
            {
                CorpseSheet = cachedCorpseSheet;
                return true;
            }
            CorpseSheet = null;
            return false;
        }
        public CorpseSheet RequireCorpseSheet(string Blueprint)
        {
            Debug.GetIndents(out Indent indent);
            Debug.Log(Debug.GetCallingTypeAndMethod(), Blueprint, indent[1]);
            if (!TryGetCorpseSheet(Blueprint, out CorpseSheet corpseSheet))
            {
                Debug.Log("No existing CorpseSheet entry; creating new one...", indent[2]);
                corpseSheet = new CorpseSheet(new CorpseBlueprint(Blueprint));
                Necronomicon[Blueprint] = corpseSheet;
            }
            else
            {
                Debug.Log("CorpseSheet entry retreived...", indent[2]);
            }
            Debug.SetIndent(indent[0]);
            return corpseSheet;
        }
        public CorpseSheet RequireCorpseSheet(GameObjectBlueprint Blueprint)
        {
            return RequireCorpseSheet(Blueprint.Name);
        }

        public bool TryGetEntityBluprint(string Entity, out EntityBlueprint EntityBlueprint)
        {
            EntityBlueprints ??= new();
            if (EntityBlueprints[Entity] is EntityBlueprint cachedEntityBlueprint)
            {
                EntityBlueprint = cachedEntityBlueprint;
                return true;
            }
            EntityBlueprint = null;
            return false;
        }
        public EntityBlueprint RequireEntityBlueprint(string Blueprint)
        {
            Debug.GetIndents(out Indent indent);
            Debug.Log(Debug.GetCallingTypeAndMethod(), Blueprint, indent[1]);

            if (!TryGetEntityBluprint(Blueprint, out EntityBlueprint entityBlueprint))
            {
                Debug.Log("No existing entity entry; creating new one...", indent[2]);
                entityBlueprint = new EntityBlueprint(Blueprint);
                EntityBlueprints[Blueprint] = entityBlueprint;
            }
            else
            {
                Debug.Log("Entity entry retreived...", indent[2]);
            }
            Debug.SetIndent(indent[0]);
            return entityBlueprint;
        }
        public EntityBlueprint RequireEntityBlueprint(GameObjectBlueprint Blueprint)
        {
            return RequireEntityBlueprint(Blueprint.Name);
        }

        public static bool IsReanimatableCorpse(GameObjectBlueprint Blueprint)
        {
            return UD_FleshGolems_NanoNecroAnimation.IsReanimatableCorpse(Blueprint);
        }

        public UD_FleshGolems_NecromancySystem InitializeCorpseSheetsCorpses(out bool Initialized)
        {
            Debug.GetIndents(out Indent indent);
            Debug.Log(Debug.GetCallingTypeAndMethod(), indent[1]);
            int counter = 0;
            foreach (GameObjectBlueprint blueprint in GameObjectFactory.Factory.BlueprintList.Where(IsReanimatableCorpse))
            {
                Debug.Log(blueprint.Name, indent[2]);
                SetLoadingStatusCorpses(counter++ % 100 == 0);
                RequireCorpseSheet(blueprint.Name);
            }
            Debug.CheckYeh(nameof(CorpseSheetsInitialized), indent[1]);
            Initialized = true;
            Debug.SetIndent(indent[0]);
            return this;
        }

        private static IEnumerable<GameObjectBlueprint> GetEntitiesWithCorpse(Predicate<GameObjectBlueprint> Filter = null)
            => GameObjectFactory.Factory.BlueprintList
                .Where(bp => bp.HasPart(nameof(Corpse)))
                .Where(bp => !bp.IsBaseBlueprint())
                .Where(bp => !bp.IsChiliad())
                .Where(bp => Filter == null || Filter(bp));
        public UD_FleshGolems_NecromancySystem InitializeEntityPrimaryCorpses(out bool Initialized)
        {
            Debug.GetIndents(out Indent indent);
            Debug.Log(Debug.GetCallingTypeAndMethod(), indent[1]);
            int counter = 0;
            foreach (GameObjectBlueprint blueprint in GameObjectFactory.Factory.BlueprintList.Where(bp => bp.HasPart(nameof(Corpse))))
            {
                SetLoadingStatusCorpses(counter++ % 10 == 0);
                if (!blueprint.IsBaseBlueprint()
                    && !blueprint.IsChiliad()
                    && blueprint.TryGetCorpseBlueprintAndChance(out string corpseBlueprint, out int corpseChance)
                    && corpseBlueprint.IsCorpse())
                {
                    Debug.CheckYeh(blueprint.Name + " added " + nameof(corpseBlueprint) + ": " + corpseBlueprint, indent[2]);
                    RequireCorpseSheet(corpseBlueprint)
                        .AddPrimaryEntity(blueprint, corpseChance);
                }
                else
                {
                    Debug.CheckNah(blueprint.Name + " skipped, doesn't pass.", indent[2]);
                }
            }
            Debug.CheckYeh(nameof(EntityPrimaryCorpsesInitialized), indent[1]);
            Initialized = true;
            Debug.SetIndent(indent[0]);
            return this;
        }

        public static bool IsProcessableCorpse(GameObjectBlueprint Corpse, Predicate<GameObjectBlueprint> Filter)
            => Corpse.IsCorpse(Filter)
            && (Corpse.HasPart(nameof(Butcherable)) || Corpse.HasPart(nameof(Harvestable)));

        public static bool IsProcessableCorpse(GameObjectBlueprint Corpse, bool ExcludeBase)
            => IsProcessableCorpse(Corpse, ExcludeBase ? IsNotBaseBlueprint : null);

        public static bool IsProcessableCorpse(GameObjectBlueprint Corpse)
            => IsProcessableCorpse(Corpse, true);

        public IEnumerable<CorpseBlueprint> GetCorpseBlueprints()
        {
            foreach ((_, CorpseSheet corpseSheet) in Necronomicon)
            {
                if (corpseSheet.GetCorpseBlueprint() is GameObjectBlueprint corpseBlueprint)
                {
                    yield return corpseSheet.GetCorpse();
                }
            }
        }

        public IEnumerable<CorpseBlueprint> GetCorpseBlueprints(Predicate<GameObjectBlueprint> Filter)
        {
            if (!CorpseSheetsInitialized)
            {
                string cacheName = nameof(UD_FleshGolems_NecromancySystem) + "." + nameof(Necronomicon);
                MetricsManager.LogModError(ThisMod, "Attempted to iterate " + cacheName + " before it was initialized");
                yield break;
            }
            foreach ((_, CorpseSheet corpseSheet) in Necronomicon)
            {
                if (corpseSheet.GetCorpseBlueprint() is GameObjectBlueprint corpseBlueprint)
                {
                    if (Filter == null || Filter(corpseBlueprint))
                    {
                        yield return corpseSheet.GetCorpse();
                    }
                }
            }
        }

        public IEnumerable<CorpseBlueprint> GetCorpseBlueprints(Predicate<CorpseBlueprint> Filter)
        {
            if (!CorpseSheetsInitialized)
            {
                string cacheName = nameof(UD_FleshGolems_NecromancySystem) + "." + nameof(Necronomicon);
                MetricsManager.LogModError(ThisMod, "Attempted to iterate " + cacheName + " before it was initialized");
                yield break;
            }
            foreach ((_, CorpseSheet corpseSheet) in Necronomicon)
            {
                if (Filter == null || Filter(corpseSheet.GetCorpse()))
                {
                    yield return corpseSheet.GetCorpse();
                }
            }
        }
        public IEnumerable<CorpseBlueprint> GetCorpseBlueprints(Predicate<CorpseSheet> Filter)
        {
            if (!CorpseSheetsInitialized)
            {
                string cacheName = nameof(UD_FleshGolems_NecromancySystem) + "." + nameof(Necronomicon);
                MetricsManager.LogModError(ThisMod, "Attempted to iterate " + cacheName + " before it was initialized");
                yield break;
            }
            foreach ((_, CorpseSheet corpseSheet) in Necronomicon)
            {
                if (Filter == null || Filter(corpseSheet))
                {
                    yield return corpseSheet.GetCorpse();
                }
            }
        }

        /// <summary>
        ///     Attempts to get a <see cref="GameObjectBlueprint"/>'s (designed for a "Corpse"-inheriting blueprint) <see cref="Butcherable"/>, <see cref="Harvestable"/>, 
        ///     or custom "Processable" part's "OnSuccess" field value, compiled into a list.
        /// </summary>
        /// <param name="CorpseBlueprint">The blueprint from which to get the products list.</param>
        /// <param name="ProcessableType">
        ///     The name of the <see cref="IPart"/> responsible for handling the prospective corpse's processing (<see cref="Butcherable"/>, 
        ///     <see cref="Harvestable"/>, or custom "Processable" <see cref="IPart"/>)</param>
        /// <param name="OnSuccessFieldName">The name of the field that contains the processing success result.</param>
        /// <param name="PossibleProducts"></param>
        /// <param name="ProductFilter">Any conditions by which a product should be included, to the exclusion of all others.</param>
        /// <returns>An <see cref="List{string}"/> of the <see cref="GameObject.Blueprint"/> or <see cref="GameObjectBlueprint.Name"/> for each potential "product" from successfully "processing" the <paramref name="CorpseBlueprint"/></returns>
        public static List<GameObjectBlueprint> GetProcessableCorpsesProducts(
            GameObjectBlueprint CorpseBlueprint,
            string ProcessableType,
            string OnSuccessFieldName,
            Predicate<GameObjectBlueprint> ProductFilter = null)
        {
            Debug.GetIndents(out Indent indent);
            Debug.Log(
                Debug.GetCallingTypeAndMethod(true),
                CorpseBlueprint.Name + ", " + ProcessableType + ", " + OnSuccessFieldName,
                Indent: indent[1]);

            if (CorpseBlueprint == null)
            {
                Debug.CheckNah("Corpse null", indent[2]);
                Debug.SetIndent(indent[0]);
                return new();
            }

            if (!CorpseBlueprint.TryGetPartParameter(ProcessableType, OnSuccessFieldName, out string processedProductValue))
            {
                Debug.CheckNah("No Products", indent[2]);
                Debug.SetIndent(indent[0]);
                return new();
            }
            if (processedProductValue.IsNullOrEmpty())
            {
                Debug.CheckNah("Product null or empty.", indent[2]);
                Debug.SetIndent(indent[0]);
                return new();
            }

            List<GameObjectBlueprint> outputList = new();
            Debug.Log(processedProductValue, indent[2]);
            if (!processedProductValue.StartsWith('@'))
            {
                if (processedProductValue.GetGameObjectBlueprint() is GameObjectBlueprint processedProductValueBlueprint
                    && (ProductFilter == null || ProductFilter(processedProductValueBlueprint)))
                {
                    Debug.Log(processedProductValue, indent[3]);
                    if (processedProductValueBlueprint.IsCorpse())
                    {
                        Debug.CheckYeh("Added", indent[4]);
                        outputList.AddUnique(processedProductValueBlueprint);
                    }
                }
            }
            else
            {
                string tableName = processedProductValue[1..];
                foreach (string populationResult in GetDistinctFromPopulation(tableName, 15))
                {
                    if (populationResult.GetGameObjectBlueprint() is GameObjectBlueprint processedProductBlueprint)
                    {
                        if (processedProductBlueprint.IsCorpse())
                        {
                            if (ProductFilter == null || ProductFilter(processedProductBlueprint))
                            {
                                Debug.CheckYeh("Added", indent[5]);
                                outputList.AddUnique(processedProductBlueprint);
                            }
                        }
                    }
                }
            }
            Debug.SetIndent(indent[0]);
            return outputList;
        }
        public UD_FleshGolems_NecromancySystem InititializeCorpseProducts(out bool Initialized)
        {
            Debug.GetIndents(out Indent indent);
            Debug.Log(Debug.GetCallingTypeAndMethod(), indent[1]);

            List<GameObjectBlueprint> possibleProducts = new();
            foreach (CorpseBlueprint corpseBlueprint in GetCorpseBlueprints(IsProcessableCorpse))
            {
                Debug.Log(nameof(corpseBlueprint), corpseBlueprint.ToString(), indent[2]);
                
                List<GameObjectBlueprint> butcherableProducts = new(GetProcessableCorpsesProducts(
                    CorpseBlueprint: corpseBlueprint.GetGameObjectBlueprint(),
                    ProcessableType: nameof(Butcherable),
                    OnSuccessFieldName: nameof(Butcherable.OnSuccess)));

                if (!butcherableProducts.IsNullOrEmpty())
                {
                    Debug.Log(nameof(butcherableProducts), indent[3]);
                    foreach (GameObjectBlueprint butcherableProduct in butcherableProducts)
                    {
                        Debug.Log(butcherableProduct.Name, indent[4]);
                        possibleProducts.Add(butcherableProduct);
                    }
                }

                List<GameObjectBlueprint> harvestableProducts = new(GetProcessableCorpsesProducts(
                    CorpseBlueprint: corpseBlueprint.GetGameObjectBlueprint(),
                    ProcessableType: nameof(Harvestable),
                    OnSuccessFieldName: nameof(Harvestable.OnSuccess)));

                if (!harvestableProducts.IsNullOrEmpty())
                {
                    Debug.Log(nameof(harvestableProducts), indent[3]);
                    foreach (GameObjectBlueprint harvestableProduct in harvestableProducts)
                    {
                        Debug.Log(harvestableProduct.Name, indent[4]);
                        possibleProducts.Add(harvestableProduct);
                    }
                }
                if (!possibleProducts.IsNullOrEmpty())
                {
                    Debug.Log(nameof(possibleProducts), possibleProducts.Count, indent[3]);
                }
                int counter = 0;
                foreach (GameObjectBlueprint possibleProduct in possibleProducts)
                {
                    Debug.Log(nameof(possibleProduct), possibleProduct.Name, indent[4]);
                    SetLoadingStatusCorpses(counter++ % 5 == 0);
                    if (possibleProduct.IsCorpse())
                    {
                        CorpseSheet productCorpseSheet = RequireCorpseSheet(possibleProduct.Name);
                        productCorpseSheet.IsCorpseProduct = true;

                        // add the corpse we got the product from to the corpseProduct's list.
                        if (productCorpseSheet.AddProductOriginCorpse(corpseBlueprint))
                        {
                            Debug.CheckYeh("Added " + corpseBlueprint + " to ProductOriginCorpses list", indent[5]);
                        }
                        else
                        {
                            Debug.CheckNah(corpseBlueprint + " is  already in ProductOriginCorpses list", indent[5]);
                        }

                        // add the entity weight entries for the corpse we got the product from to the product's corpse sheet.
                        CorpseSheet corpseSheet = RequireCorpseSheet(corpseBlueprint.ToString());
                        Debug.Log(
                                "Adding " + corpseBlueprint + " " + nameof(EntityWeight).Pluralize() +
                                " to " + nameof(productCorpseSheet),
                                indent[5]);

                        foreach (EntityWeight entityWeight in corpseSheet.GetEntityWeights())
                        {
                            Debug.CheckYeh("Added " + entityWeight + " to " + nameof(productCorpseSheet), indent[6]);
                            productCorpseSheet.AddProductEntity(entityWeight);
                        }
                    }
                }
            }
            Debug.CheckYeh(nameof(CorpseProductsInitialized), indent[1]);
            Initialized = true;
            Debug.SetIndent(indent[0]);
            return this;
        }

        private static bool PossiblyExcludedFromDynamicEncounters(GameObjectBlueprint bp)
                => bp.HasTagOrProperty(IGNORE_EXCLUDE_PROPTAG)
                || !bp.IsExcludedFromDynamicEncounters();

        public static bool IsEntityWithCorpse(GameObjectBlueprint Entity) => GetEntitiesWithCorpse().Contains(Entity);

        private static CountsAs ProcessCountsAsPropTag(string PropTag, out List<string> CountsAsParamaters)
        {
            CountsAsParamaters = new();
            if (PropTag.EqualsNoCase("any") || PropTag.Equals("*"))
            {
                return CountsAs.Any;
            }
            else
            if (PropTag.Contains(":"))
            {
                CountsAsParamaters = PropTag.Split(":").ToList();
                return CountsAsParamaters[0].ToLower() switch
                {
                    "blueprint" => CountsAs.Blueprint,
                    "population" => CountsAs.Population,
                    "faction" => CountsAs.Faction,
                    "species" => CountsAs.Species,
                    "genotype" => CountsAs.Genotype,
                    "subtype" => CountsAs.Subtype,
                    "otherCorpse" => CountsAs.OtherCorpse,
                    _ => CountsAs.Any,
                };
            }
            else
            {
                return CountsAs.None;
            }
        }

        private List<EntityBlueprint> GetCorpseCountsAsBlueprints(GameObjectBlueprint CorpseBlueprint)
        {
            Debug.GetIndents(out Indent indent);
            Debug.Log(Debug.GetCallingTypeAndMethod(true) + "(" + nameof(CorpseBlueprint) + ": " + CorpseBlueprint.Name + ")", indent[1]);
            List<EntityBlueprint> countsAsBlueprints = new();
            if (CorpseBlueprint == null
                || !CorpseBlueprint.TryGetStringPropertyOrTag(CORPSE_COUNTS_AS_PROPTAG, out string rawValue))
            {
                Debug.Log("Blueprint null or doesn't have tag!", indent[2]);
                Debug.SetIndent(indent[0]);
                return countsAsBlueprints;
            }

            Debug.Log(nameof(rawValue), rawValue, indent[2]);
            CountsAs countsAs = ProcessCountsAsPropTag(rawValue, out List<string> countsAsParamaters);

            Debug.Log(nameof(CountsAs), countsAs, indent[2]);

            List<GameObjectBlueprint> entitiesWithCorpses = GetEntitiesWithCorpse(PossiblyExcludedFromDynamicEncounters).ToList();

            List<GameObjectBlueprint> countsAsModels = new();
            void addToCountsAsModels(List<string> countsAsBlueprintList)
            {
                if (countsAsBlueprintList != null)
                {
                    foreach (string countsAsBlueprint in countsAsBlueprintList)
                    {
                        if (countsAsBlueprint.GetGameObjectBlueprint() is GameObjectBlueprint countsAsModel
                            && IsEntityWithCorpse(countsAsModel))
                        {
                            countsAsModels.Add(countsAsModel);
                        }
                    }
                }
            }
            void addPropTagModels(string PropTag, string Value)
            {
                if (Value is string countsAsPropTagParam
                    && countsAsPropTagParam.CachedCommaExpansion() is List<string> countsAsPropTagList
                    && entitiesWithCorpses
                        .Where(bp
                            => bp.GetPropertyOrTag(PropTag) is string propTag
                            && countsAsPropTagList.Contains(propTag))
                        .Select(bp => bp.Name)
                        .ToList() is List<string> countsAsPropTagBlueprints)
                {
                    addToCountsAsModels(countsAsPropTagBlueprints);
                }
            }
            GameObjectBlueprint toGameObjectBlueprint(string blueprint) => blueprint.GetGameObjectBlueprint();
            switch (countsAs)
            {
                case CountsAs.Any:
                    countsAsModels = GetEntitiesWithCorpse(PossiblyExcludedFromDynamicEncounters).ToList();
                    break;

                case CountsAs.Blueprint:
                    if (countsAsParamaters[1] is string countsAsBlueprintParam)
                    {
                        countsAsModels = countsAsBlueprintParam
                            .CachedCommaExpansion()
                            .ConvertAll(toGameObjectBlueprint);
                    }
                    break;

                case CountsAs.Population:
                    if (countsAsParamaters[1] is string countsAsPopulationParam)
                    {
                        foreach (string countsAsPopulation in countsAsPopulationParam.CachedCommaExpansion())
                        {
                            countsAsModels.AddRange(PopulationManager.GetEach(countsAsPopulationParam).ConvertAll(toGameObjectBlueprint));
                        }
                    }
                    break;

                case CountsAs.Faction:
                    if (countsAsParamaters[1] is string countsAsFactionParam)
                    {
                        List<Faction> countsAsFactions = new();
                        bool countsAsFactionParamNew = countsAsFactionParam.ToLower() == "new";
                        bool countsAsFactionParamOld = countsAsFactionParam.ToLower() == "old";
                        if (countsAsFactionParamNew || countsAsFactionParamOld)
                        {
                            foreach (Faction faction in Factions.GetList())
                            {
                                if ((countsAsFactionParamNew && !faction.Old)
                                    || (countsAsFactionParamOld && faction.Old))
                                {
                                    countsAsFactions.Add(faction);
                                }
                            }
                        }
                        else
                        if (countsAsFactionParam.Contains(","))
                        {
                            foreach (string countsAsFaction in countsAsFactionParam.CachedCommaExpansion())
                            {
                                if (Factions.Get(countsAsFaction) is Faction faction)
                                {
                                    countsAsFactions.Add(faction);
                                }
                            }
                        }
                        else
                        if (Factions.Get(countsAsFactionParam) is Faction faction)
                        {
                            countsAsFactions.Add(faction);
                        }
                        foreach (Faction countsAsFaction in countsAsFactions)
                        {
                            if (countsAsFaction.GetMembers(IsEntityWithCorpse) is List<GameObjectBlueprint> factionMembersWithCorpse)
                            {
                                countsAsModels.AddRange(factionMembersWithCorpse);
                            }
                        }
                    }
                    break;

                case CountsAs.Species:
                    addPropTagModels(CountsAs.Species.ToString(), countsAsParamaters[1]);
                    break;

                case CountsAs.Genotype:
                    addPropTagModels(CountsAs.Genotype.ToString(), countsAsParamaters[1]);
                    break;

                case CountsAs.Subtype:
                    addPropTagModels(CountsAs.Subtype.ToString(), countsAsParamaters[1]);
                    break;

                case CountsAs.OtherCorpse:
                    if (countsAsParamaters[1] is string countsAsOtherCorpseParam
                        && countsAsOtherCorpseParam.CachedCommaExpansion() is List<string> countsAsOtherCorpseList)
                    {
                        bool hasOneOfTheseCorpses(GameObjectBlueprint Entity)
                        {
                            return Entity.TryGetCorpseBlueprint(out string corpseBlueprint)
                                && countsAsOtherCorpseList.Contains(corpseBlueprint);
                        }
                        countsAsModels = GetEntitiesWithCorpse(hasOneOfTheseCorpses).ToList();
                    }
                    break;
            }
            if (countsAsModels.IsNullOrEmpty())
            {
                Debug.Log(nameof(countsAsModels), "empty!", indent[2]);
            }
            Debug.SetIndent(indent[0]);
            return countsAsBlueprints;
        }

        private UD_FleshGolems_NecromancySystem InitializeCountsAsCorspes(out bool Initialized)
        {
            Debug.GetIndents(out Indent indent);
            Debug.Log(Debug.GetCallingTypeAndMethod(true), indent[1]);
            foreach (CorpseBlueprint corpseBlueprint in GetCorpseBlueprints())
            {
                Debug.Log(nameof(corpseBlueprint), corpseBlueprint.ToString(), indent[2]);
                int counter = 0;
                CorpseSheet corpseSheet = RequireCorpseSheet(corpseBlueprint.ToString());
                List<GameObjectBlueprint> countsAsModels = GetCorpseCountsAsBlueprints(corpseBlueprint.GetGameObjectBlueprint())
                    ?.ConvertAll(ew => ew.GetGameObjectBlueprint());

                if (countsAsModels.IsNullOrEmpty())
                {
                    Debug.CheckNah(nameof(countsAsModels) + " empty!", indent[3]);
                    continue;
                }
                Debug.CheckYeh(nameof(countsAsModels) + "." + nameof(countsAsModels.Count) + ": " + countsAsModels.Count, indent[3]);
                foreach (GameObjectBlueprint countsAsModel in countsAsModels)
                {
                    SetLoadingStatusCorpses(counter++ % 5 == 0);
                    Debug.Log("Added " + nameof(countsAsModel), countsAsModel.Name, indent[4]);
                    corpseSheet.AddCountsAsEntity(new EntityWeight(new EntityBlueprint(countsAsModel), 100));
                }
            }
            Debug.CheckYeh(nameof(CountsAsCorspesInitialized), indent[1]);
            Debug.SetIndent(indent[0]);
            Initialized = true;
            return this;
        }

        public Dictionary<string, int> GetWeightedEntityStringsThisCorpseCouldBe(
            string CorpseBlueprint,
            bool Include0CHance = true,
            Predicate<GameObjectBlueprint> Filter = null)
            => RequireCorpseSheet(CorpseBlueprint)
                ?.GetWeightedEntityNameList(Include0CHance, Filter);

        public Dictionary<string, int> GetWeightedEntityStringsThisCorpseCouldBe(
            CorpseBlueprint CorpseBlueprint,
            bool Include0CHance = true,
            Predicate<GameObjectBlueprint> Filter = null)
            => GetWeightedEntityStringsThisCorpseCouldBe(CorpseBlueprint.ToString(), Include0CHance, Filter);

        public Dictionary<string, int> GetWeightedEntityStringsThisCorpseCouldBe(
            GameObjectBlueprint CorpseBlueprint,
            bool Include0CHance = true,
            Predicate<GameObjectBlueprint> Filter = null)
            => GetWeightedEntityStringsThisCorpseCouldBe(CorpseBlueprint.Name, Include0CHance, Filter);

        public Dictionary<string, int> GetWeightedEntityStringsThisCorpseCouldBe(
            GameObject Corpse,
            bool Include0CHance = true,
            Predicate<GameObjectBlueprint> Filter = null)
            => GetWeightedEntityStringsThisCorpseCouldBe(Corpse.Blueprint, Include0CHance, Filter);

        /*
         * 
         * Wishes!
         * 
         */

        [WishCommand("UD_FleshGolems gimme cache")]
        public static void Debug_GimmeCache_WishHandler()
        {
            Debug.SetSilenceLogging(true);
            string output = "NecromancySystem Corpse Caches\n";
            output += nameof(GetCorpseBlueprints) + "\n";
            output += System?.GetCorpseBlueprints()
                        ?.ToList()
                        ?.ConvertAll(cbp => cbp.ToString())
                        ?.GenerateBulletList();
            output += "\n\n";

            output += nameof(GetEntitiesWithCorpse) + "\n";
            output += GetEntitiesWithCorpse()
                        ?.ToList()
                        ?.ConvertAll(cbp => cbp.Name)
                        ?.GenerateBulletList();
            output += "\n\n";

            output += "Entities by Corpse" + "\n";
            output += "Corpse,Entities\n";
            output += System?.Necronomicon
                        ?.Select(cn => cn.Key + ",\"" + cn.Value.GetEntities().Join() + "\"")
                        ?.ToList()
                        ?.GenerateBulletList();
            output += "\n\n";

            /*
            output += "Processables By Product" + "\n";
            output += "Product,Corpse\n";
            output += System?.GetCorpseBlueprints((CorpseSheet cs) => cs.IsCorpseProduct)
                        ?.ToList()
                        ?.ConvertAll(cbp => ((CorpseProduct)cbp).ToDebugString())
                        ?.GenerateBulletList();
            output += "\n\n";
            */

            output += "Full Breakdown" + "\n";
            output += System?.Necronomicon
                        ?.Select(cn 
                            => cn.Key + ":\n" 
                            + cn.Value?.GetEntityWeights()
                                ?.ConvertToWeightedList()
                                ?.ConvertToStringList(kvp => kvp.Key + ": " + kvp.Value)
                                ?.GenerateBulletList(ItemPostProc: s => UIFriendlyNBPS(4) + s))
                        ?.ToList()
                        ?.ConvertAll(cbp => cbp.ToString())
                        ?.GenerateBulletList();
            output += "\n\n";

            Debug.SetSilenceLogging(false);
            string uIFriendlySpace = UIFriendlyNBPS(1);
            UnityEngine.Debug.Log(output.Replace(uIFriendlySpace, " "));
            Popup.Show(output);
        }
    }
}
