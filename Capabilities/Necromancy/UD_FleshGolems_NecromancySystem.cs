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
using ArgPair = UD_FleshGolems.Logging.Debug.ArgPair;
using Debug = UD_FleshGolems.Logging.Debug;

namespace UD_FleshGolems.Capabilities
{
    [HasModSensitiveStaticCache]
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
        public enum CountsAs
        {
            None,
            Any,
            Keyword,
            Blueprint,
            Population,
            Faction,
            Species,
            Genotype,
            Subtype,
            OtherCorpse,
        }

        public const string IGNORE_EXCLUDE_PROPTAG = "UD_FleshGolems PastLife Ignore ExcludeFromDynamicEncounters WhenFinding";
        public const string CORPSE_COUNTS_AS_PROPTAG = "UD_FleshGolems PastLife CountsAs";
        public const string PASTLIFE_BLUEPRINT_PROPTAG = "UD_FleshGolems_PastLife_Blueprint";

        [ModSensitiveStaticCache( CreateEmptyInstance = false )]
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

        [SerializeField]
        private bool InheritedCorspesInitialized;

        public UD_FleshGolems_NecromancySystem()
        {
            Necronomicon = new();
            EntityBlueprints = new();
            CorpseSheetsInitialized = false;
            EntityPrimaryCorpsesInitialized = false;
            CorpseProductsInitialized = false;
            CountsAsCorspesInitialized = false;
            InheritedCorspesInitialized = false;
        }

        [ModSensitiveCacheInit]
        [GameBasedCacheInit]
        public static void NecromancySystemInit()
        {
            using Indent indent = new();
            if (System == null)
            {
                System = The.Game?.RequireSystem(InitializeSystem);
            }
            else
            {
                The.Game?.AddSystem(System);
            }
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
                    Debug.LogCaller("Cache finished... " + (success ? "success!" : "failure!"), indent[0]);
                }
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
            if (OnCondition) Loading.SetLoadingStatus("Compiling " + nameof(Necronomicon) + GetPeriods(CachingPeriods, out CachingPeriods));
        }

        private static UD_FleshGolems_NecromancySystem InitializeSystem() => new();

        private void Init()
        {
            Debug.ResetIndent();

            Stopwatch sw = new();
            sw.Start();

            using Indent indent = new();
            Debug.LogCaller("Starting Corpse Cache...", indent);
            InitializeCorpseSheetsCorpses(out CorpseSheetsInitialized);
            InitializeEntityPrimaryCorpses(out EntityPrimaryCorpsesInitialized);
            InititializeCorpseProducts(out CorpseProductsInitialized);
            InitializeCountsAsCorspes(out CountsAsCorspesInitialized);
            // InitializeInheritedCorspes(out InheritedCorspesInitialized);
            Loading.SetLoadingStatus(null);
            sw.Stop();
            TimeSpan duration = sw.Elapsed;
            string timeUnit = duration.Minutes > 0 ? "minute" : "second";
            double timeDuration = duration.Minutes > 0 ? duration.TotalMinutes : duration.TotalSeconds;
            Debug.Log("Corpse Cache took " + timeDuration.Things(timeUnit) + ".", Indent: indent[0]);
            indent.Dispose();
            Debug.ResetIndent();
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
            using Indent indent = new();
            Debug.LogMethod(indent[1], Debug.Arg(Blueprint));
            if (!TryGetCorpseSheet(Blueprint, out CorpseSheet corpseSheet))
            {
                Debug.Log("No existing CorpseSheet entry; creating new one...", indent[2]);
                corpseSheet = new CorpseSheet(new CorpseBlueprint(Blueprint));
                Necronomicon[Blueprint] = corpseSheet;
                if (!corpseSheet.InheritedCorpsesInitialized)
                {
                    corpseSheet.InitializeInheritedCorpseList();
                }
            }
            else
            {
                Debug.Log("CorpseSheet entry retreived...", indent[2]);
            }
            return corpseSheet;
        }
        public CorpseSheet RequireCorpseSheet(GameObjectBlueprint Blueprint)
        {
            return RequireCorpseSheet(Blueprint.Name);
        }
        public CorpseSheet RequireCorpseSheet(CorpseBlueprint Blueprint)
        {
            return RequireCorpseSheet(Blueprint.ToString());
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
            using Indent indent = new();
            Debug.LogCaller(indent[1], Debug.Arg(Blueprint));

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
            using Indent indent = new(1);
            Debug.LogMethod(indent);
            int counter = 0;
            foreach (GameObjectBlueprint blueprint in GameObjectFactory.Factory.BlueprintList.Where(IsReanimatableCorpse))
            {
                Debug.Log(blueprint.Name, indent[1]);
                SetLoadingStatusCorpses(counter++ % 100 == 0);
                RequireCorpseSheet(blueprint.Name);
            }
            Initialized = true;

            Debug.CheckYeh(nameof(CorpseSheetsInitialized), indent[0]);
            return this;
        }

        private static IEnumerable<GameObjectBlueprint> GetEntitiesWithCorpseForInit(Predicate<GameObjectBlueprint> Filter = null)
            => GameObjectFactory.Factory.BlueprintList
                .Where(bp => bp.HasPart(nameof(Corpse)))
                .Where(bp => !bp.IsBaseBlueprint())
                .Where(bp => !bp.IsChiliad())
                .Where(bp => Filter == null || Filter(bp));

        public IEnumerable<EntityBlueprint> GetEntityBlueprintsWithCorpse(Predicate<GameObjectBlueprint> Filter = null)
        {
            if (!EntityPrimaryCorpsesInitialized)
            {
                yield break;
            }
            foreach (EntityBlueprint entityBlueprint in EntityBlueprints.Values)
            {
                if (Filter == null || Filter(entityBlueprint.GetGameObjectBlueprint()))
                {
                    yield return entityBlueprint;
                }
            }
        }
        public IEnumerable<GameObjectBlueprint> GetEntityModelsWithCorpse(Predicate<GameObjectBlueprint> Filter = null)
        {
            foreach (EntityBlueprint entityBlueprint in GetEntityBlueprintsWithCorpse(Filter))
            {
                yield return entityBlueprint.GetGameObjectBlueprint();
            }
        }

        public static bool BlueprintHasCorpse(GameObjectBlueprint Blueprint) => Blueprint.HasPart(nameof(Corpse));

        public UD_FleshGolems_NecromancySystem InitializeEntityPrimaryCorpses(out bool Initialized)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent);
            int counter = 0;
            foreach (GameObjectBlueprint corpseHavingEntity in GameObjectFactory.Factory.GetBlueprints(BlueprintHasCorpse))
            {
                SetLoadingStatusCorpses(counter++ % 10 == 0);
                if (
                    //!corpseHavingEntity.IsBaseBlueprint()
                    //&& 
                    !corpseHavingEntity.IsChiliad()
                    && corpseHavingEntity.TryGetCorpseBlueprintAndChance(out string corpseBlueprint, out int corpseChance)
                    && corpseBlueprint.IsCorpse())
                {
                    RequireCorpseSheet(corpseBlueprint)
                        .AddPrimaryEntity(corpseHavingEntity, corpseChance);
                    Debug.CheckYeh(corpseHavingEntity.Name + " added " + nameof(corpseBlueprint), corpseBlueprint, indent[1]);
                }
                else
                {
                    Debug.CheckNah(corpseHavingEntity.Name + " skipped, doesn't pass.", indent[1]);
                }
            }
            Initialized = true;

            Debug.CheckYeh(nameof(EntityPrimaryCorpsesInitialized), indent[0]);
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
                Debug.MetricsManager_LogCallingModError("Attempted to iterate " + cacheName + " before it was initialized");
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
            using Indent indent = new();
            Debug.LogMethod(indent[1],
                new ArgPair[] 
                {
                    Debug.Arg(CorpseBlueprint.Name),
                    Debug.Arg(ProcessableType),
                    Debug.Arg(OnSuccessFieldName),
                    Debug.Arg(nameof(ProductFilter), ProductFilter != null),
                });

            if (CorpseBlueprint == null)
            {
                Debug.CheckNah("Corpse null", indent[1]);
                return new();
            }

            if (!CorpseBlueprint.TryGetPartParameter(ProcessableType, OnSuccessFieldName, out string processedProductValue))
            {
                Debug.CheckNah("No Products", indent[1]);
                return new();
            }
            if (processedProductValue.IsNullOrEmpty())
            {
                Debug.CheckNah("Product null or empty.", indent[1]);
                return new();
            }

            List<GameObjectBlueprint> outputList = new();
            Debug.Log(processedProductValue, indent[1]);
            if (!processedProductValue.StartsWith('@'))
            {
                if (processedProductValue.GetGameObjectBlueprint() is GameObjectBlueprint processedProductValueBlueprint
                    && (ProductFilter == null || ProductFilter(processedProductValueBlueprint)))
                {
                    Debug.Log(processedProductValue, indent[2]);
                    if (processedProductValueBlueprint.IsCorpse())
                    {
                        Debug.CheckYeh("Added", indent[3]);
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
                                outputList.AddUnique(processedProductBlueprint);
                                Debug.CheckYeh("Added", indent[2]);
                            }
                        }
                    }
                }
            }
            return outputList;
        }
        public UD_FleshGolems_NecromancySystem InititializeCorpseProducts(out bool Initialized)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent);

            foreach (CorpseBlueprint corpseBlueprint in GetCorpseBlueprints(IsProcessableCorpse))
            {
                List<GameObjectBlueprint> possibleProducts = new();
                Debug.Log(nameof(corpseBlueprint), corpseBlueprint.ToString(), indent[1]);
                
                List<GameObjectBlueprint> butcherableProducts = new(GetProcessableCorpsesProducts(
                    CorpseBlueprint: corpseBlueprint.GetGameObjectBlueprint(),
                    ProcessableType: nameof(Butcherable),
                    OnSuccessFieldName: nameof(Butcherable.OnSuccess)));

                if (!butcherableProducts.IsNullOrEmpty())
                {
                    Debug.Log(nameof(butcherableProducts), indent[2]);
                    foreach (GameObjectBlueprint butcherableProduct in butcherableProducts)
                    {
                        Debug.Log(butcherableProduct.Name, indent[3]);
                        possibleProducts.Add(butcherableProduct);
                    }
                }

                List<GameObjectBlueprint> harvestableProducts = new(GetProcessableCorpsesProducts(
                    CorpseBlueprint: corpseBlueprint.GetGameObjectBlueprint(),
                    ProcessableType: nameof(Harvestable),
                    OnSuccessFieldName: nameof(Harvestable.OnSuccess)));

                if (!harvestableProducts.IsNullOrEmpty())
                {
                    Debug.Log(nameof(harvestableProducts), indent[2]);
                    foreach (GameObjectBlueprint harvestableProduct in harvestableProducts)
                    {
                        Debug.Log(harvestableProduct.Name, indent[3]);
                        possibleProducts.Add(harvestableProduct);
                    }
                }
                if (!possibleProducts.IsNullOrEmpty())
                {
                    Debug.Log(nameof(possibleProducts), possibleProducts.Count, indent[2]);
                }
                int counter = 0;
                foreach (GameObjectBlueprint possibleProduct in possibleProducts)
                {
                    Debug.Log(nameof(possibleProduct), possibleProduct.Name, indent[3]);
                    SetLoadingStatusCorpses(counter++ % 5 == 0);
                    if (possibleProduct.IsCorpse())
                    {
                        CorpseSheet productCorpseSheet = RequireCorpseSheet(possibleProduct.Name);
                        productCorpseSheet.IsCorpseProduct = true;

                        // add the corpse we got the product from to the corpseProduct's list.
                        if (productCorpseSheet.AddProductOriginCorpse(corpseBlueprint))
                        {
                            Debug.CheckYeh("Added " + corpseBlueprint + " to ProductOriginCorpses list", indent[4]);
                        }
                        else
                        {
                            Debug.CheckNah(corpseBlueprint + " is  already in ProductOriginCorpses list", indent[4]);
                        }

                        // add the entity weight entries for the corpse we got the product from to the product's corpse sheet.
                        CorpseSheet corpseSheet = RequireCorpseSheet(corpseBlueprint.ToString());
                        /*
                        Debug.Log(
                            "Adding " + corpseBlueprint + " " + nameof(EntityWeight).Pluralize() +
                            " to " + nameof(productCorpseSheet),
                            indent[5]);
                        */
                        foreach (EntityWeight entityWeight in corpseSheet.GetEntityWeights())
                        {
                            productCorpseSheet.AddProductEntity(entityWeight);
                            Debug.CheckYeh("Added " + entityWeight + " to " + nameof(productCorpseSheet), indent[4]);
                        }
                    }
                }
            }
            Initialized = true;

            Debug.CheckYeh(nameof(CorpseProductsInitialized), indent[0]);
            return this;
        }

        public static bool PossiblyExcludedFromDynamicEncounters(GameObjectBlueprint Blueprint)
            => Blueprint != null
            && Blueprint.HasTagOrProperty(IGNORE_EXCLUDE_PROPTAG)
            || !Blueprint.IsExcludedFromDynamicEncounters();

        public bool IsEntityWithCorpse(GameObjectBlueprint Entity) => GetEntityModelsWithCorpse().Contains(Entity);

        private List<EntityWeightCountsAs> GetCorpseCountsAsEntityWeights(
            CountsAs countsAs,
            List<string> countsAsParamaters)
        {
            using Indent indent = new();
            Debug.LogMethod(indent, 
                new ArgPair[]
                {
                    Debug.Arg(nameof(countsAs), countsAs),
                    Debug.Arg(nameof(countsAsParamaters), "\"" + countsAsParamaters.Join() + "\""),
                });

            List<GameObjectBlueprint> entitiesWithCorpses = new();
            foreach (EntityBlueprint entityBlueprint in EntityBlueprints.Values)
            {
                entitiesWithCorpses.Add(entityBlueprint.GetGameObjectBlueprint());
            }

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
            bool isNot = false;
            string primaryCountsAsParam = countsAsParamaters[1];
            if (primaryCountsAsParam.EqualsNoCase("not"))
            {
                primaryCountsAsParam = countsAsParamaters[2];
                isNot = true;
            }
            switch (countsAs)
            {
                case CountsAs.Any:
                    countsAsModels = entitiesWithCorpses;
                    break;

                case CountsAs.Keyword:
                    countsAsModels = ProcessCountsAsKeyword(countsAsParamaters);
                    break;

                case CountsAs.Blueprint:
                    if (primaryCountsAsParam is string countsAsBlueprintParam)
                    {
                        countsAsModels = countsAsBlueprintParam
                            .CachedCommaExpansion()
                            .ConvertAll(toGameObjectBlueprint);
                    }
                    break;

                case CountsAs.Population:
                    if (primaryCountsAsParam is string countsAsPopulationParam)
                    {
                        foreach (string countsAsPopulation in countsAsPopulationParam.CachedCommaExpansion())
                        {
                            countsAsModels.AddRange(PopulationManager.GetEach(countsAsPopulationParam).ConvertAll(toGameObjectBlueprint));
                        }
                    }
                    break;

                case CountsAs.Faction:
                    if (primaryCountsAsParam is string countsAsFactionParam)
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
                    addPropTagModels(CountsAs.Species.ToString(), primaryCountsAsParam);
                    break;

                case CountsAs.Genotype:
                    addPropTagModels(CountsAs.Genotype.ToString(), primaryCountsAsParam);
                    break;

                case CountsAs.Subtype:
                    addPropTagModels(CountsAs.Subtype.ToString(), primaryCountsAsParam);
                    break;

                case CountsAs.OtherCorpse:
                    if (primaryCountsAsParam is string countsAsOtherCorpseParam
                        && countsAsOtherCorpseParam.CachedCommaExpansion() is List<string> countsAsOtherCorpseList)
                    {
                        foreach (string countsAsOtherCorpse in countsAsOtherCorpseList)
                        {
                            if (RequireCorpseSheet(countsAsOtherCorpse)
                                ?.GetEntities()
                                ?.ToList()
                                ?.ConvertAll(eb => eb.GetGameObjectBlueprint())
                                is List<GameObjectBlueprint> countsAsOtherCorpseModels
                                && !countsAsOtherCorpseModels.IsNullOrEmpty())
                            {
                                countsAsModels.AddRange(countsAsOtherCorpseModels);
                            }
                        }
                    }
                    break;

                case CountsAs.None:
                default:
                    break;
            }
            if (countsAsModels.IsNullOrEmpty())
            {
                Debug.Log(nameof(countsAsModels), "empty!", indent[1]);
            }
            else
            {
                int weight = 0;
                if (countsAs == CountsAs.Any)
                {
                    if (countsAsParamaters.Count > 1
                        && int.TryParse(primaryCountsAsParam, out int countsAsAnyParamWeight))
                    {
                        weight = Math.Min(countsAsAnyParamWeight, 100);
                    }
                    else
                    {
                        weight = 100;
                    }
                }
                if (countsAsParamaters.Count > 2
                    && int.TryParse(countsAsParamaters[^1], out int countsAsParamWeight))
                {
                    weight = Math.Min(countsAsParamWeight, 100);
                }
                if (isNot)
                {
                    return entitiesWithCorpses.Where(bp => !countsAsModels.Contains(bp))
                        .ToList()
                        .ConvertAll(bpm
                        => new EntityWeightCountsAs(
                            Entity: RequireEntityBlueprint(bpm.Name),
                            Weight: weight,
                            CountsAs: countsAs));
                }
                return countsAsModels.ConvertAll(bpm 
                    => new EntityWeightCountsAs(
                        Entity: RequireEntityBlueprint(bpm.Name),
                        Weight: weight,
                        CountsAs: countsAs));
            }
            return new();
        }

        public List<GameObjectBlueprint> ProcessCountsAsKeyword(List<string> CountsAsParamaters)
        {
            List<GameObjectBlueprint> keywordBlueprintList = new();
            if (CountsAsParamaters.IsNullOrEmpty()
                || CountsAsParamaters.Count < 2)
            {
                return keywordBlueprintList;
            }
            List<GameObjectBlueprint> entitiesWithCorpses = new();
            foreach (EntityBlueprint entityBlueprint in EntityBlueprints.Values)
            {
                entitiesWithCorpses.Add(entityBlueprint.GetGameObjectBlueprint());
            }
            bool isNot = false;
            string keyword = CountsAsParamaters[1];

            if (keyword.EqualsNoCase("not"))
            {
                keyword = CountsAsParamaters[2];
                isNot = true;
            }

            switch (keyword)
            {
                case "Living":
                    keywordBlueprintList = entitiesWithCorpses.Where(bp => !bp.IsCorpse()).ToList();
                    break;
                case "Dead":
                case "Corpse":
                    keywordBlueprintList = entitiesWithCorpses.Where(bp => bp.IsCorpse()).ToList();
                    break;
            }
            if (isNot)
            {
                return entitiesWithCorpses.Where(bp => !keywordBlueprintList.Contains(bp)).ToList();
            }
            return keywordBlueprintList;
        }

        private UD_FleshGolems_NecromancySystem InitializeCountsAsCorspes(out bool Initialized)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent);

            int counter = 0;
            List<CorpseCountsAs> corpseCountsAsList = new();
            foreach (CorpseBlueprint corpseBlueprint in GetCorpseBlueprints())
            {
                Debug.Log(nameof(corpseBlueprint), corpseBlueprint.ToString(), indent[1]);

                if (corpseBlueprint?.GetGameObjectBlueprint() is not GameObjectBlueprint corpseModel
                    || !corpseModel.TryGetStringPropertyOrTag(CORPSE_COUNTS_AS_PROPTAG, out string rawCountsAsParams))
                {
                    Debug.CheckNah("Blueprint null or doesn't have tag!", indent[2]);
                    continue;
                }
                Debug.Log(nameof(rawCountsAsParams), rawCountsAsParams, indent[2]);
                if (CorpseCountsAs.GetCorpseCountsAs(corpseBlueprint, rawCountsAsParams) is CorpseCountsAs corpseCoutnasAs
                    && corpseCoutnasAs.CountasAs != CountsAs.None)
                {
                    Debug.CheckYeh(corpseCoutnasAs.ToString(), indent[2]);
                    corpseCountsAsList.Add(corpseCoutnasAs);
                }
            }
            foreach ((string name, string value) in GameObjectFactory.Factory?.GetBlueprintIfExists("UD_FleshGolems_PostLoad_CountsAs")?.Tags)
            {
                if (name.GetGameObjectBlueprint() is var corpseModel
                    && corpseModel.IsCorpse()
                    && RequireCorpseSheet(corpseModel) is CorpseSheet postLoadCorpseSheet
                    && CorpseCountsAs.GetCorpseCountsAs(postLoadCorpseSheet.GetCorpse(), value) is CorpseCountsAs corpseCoutnasAs
                    && corpseCoutnasAs.CountasAs != CountsAs.None)
                {
                    Debug.CheckYeh(corpseCoutnasAs.ToString(), indent[2]);
                    corpseCountsAsList.Add(corpseCoutnasAs);
                }
            }
            foreach (CorpseCountsAs corpseCountsAs in corpseCountsAsList)
            {
                List<EntityWeightCountsAs> countsAsModels = GetCorpseCountsAsEntityWeights(corpseCountsAs.CountasAs, corpseCountsAs.Paramaters);
                if (countsAsModels.IsNullOrEmpty())
                {
                    Debug.CheckNah(nameof(countsAsModels) + " empty!", indent[2]);
                    continue;
                }
                CorpseSheet corpseSheet = RequireCorpseSheet(corpseCountsAs.Blueprint);
                Debug.CheckYeh(nameof(countsAsModels) + "." + nameof(countsAsModels.Count) + ": " + countsAsModels.Count, indent[2]);
                foreach ((EntityBlueprint entityBlueprint, int weight, CountsAs countsAs) in countsAsModels)
                {
                    SetLoadingStatusCorpses(counter++ % 5 == 0);
                    corpseSheet.AddCountsAsEntity(new EntityWeight(entityBlueprint, weight));
                }
            }
            Initialized = true;

            Debug.CheckYeh(nameof(CountsAsCorspesInitialized), indent[0]);
            return this;
        }
        private UD_FleshGolems_NecromancySystem InitializeInheritedCorspes(out bool Initialized)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent);

            int counter = 0;
            foreach (CorpseBlueprint corpseBlueprint in GetCorpseBlueprints())
            {
                Debug.Log(nameof(corpseBlueprint), corpseBlueprint.ToString(), indent[1]);
                CorpseSheet corpseSheet = RequireCorpseSheet(corpseBlueprint.ToString());
                List<EntityWeight> corpseSheetEntityWeightList = corpseSheet.GetEntityWeights().ToList();
                foreach (CorpseBlueprint inheritedCorpse in corpseSheet.GetInheritedCorpseList())
                {
                    SetLoadingStatusCorpses(counter++ % 5 == 0);
                    Debug.Log("Adding EntityWeights for " + nameof(inheritedCorpse) + " " + inheritedCorpse.ToString(), indent[2]);
                    RequireCorpseSheet(inheritedCorpse).AddInheritedEntities(corpseSheetEntityWeightList);
                }
            }
            Initialized = true;

            Debug.CheckYeh(nameof(InheritedCorspesInitialized), indent[0]);
            return this;
        }

        public Dictionary<string, int> GetWeightedEntityStringsThisCorpseCouldBe(
            string CorpseBlueprint,
            bool Include0Weight = true,
            Predicate<GameObjectBlueprint> Filter = null,
            bool DrillIntoInheritance = true)
            => RequireCorpseSheet(CorpseBlueprint)
                ?.GetWeightedEntityNameList(Include0Weight, Filter, DrillIntoInheritance);

        public Dictionary<string, int> GetWeightedEntityStringsThisCorpseCouldBe(
            CorpseBlueprint CorpseBlueprint,
            bool Include0Weight = true,
            Predicate<GameObjectBlueprint> Filter = null)
            => GetWeightedEntityStringsThisCorpseCouldBe(CorpseBlueprint.ToString(), Include0Weight, Filter);

        public Dictionary<string, int> GetWeightedEntityStringsThisCorpseCouldBe(
            GameObjectBlueprint CorpseBlueprint,
            bool Include0Weight = true,
            Predicate<GameObjectBlueprint> Filter = null)
            => GetWeightedEntityStringsThisCorpseCouldBe(CorpseBlueprint.Name, Include0Weight, Filter);

        public Dictionary<string, int> GetWeightedEntityStringsThisCorpseCouldBe(
            GameObject Corpse,
            bool Include0Weight = true,
            Predicate<GameObjectBlueprint> Filter = null)
            => GetWeightedEntityStringsThisCorpseCouldBe(Corpse.Blueprint, Include0Weight, Filter);

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

            output += nameof(GetEntityBlueprintsWithCorpse) + "\n";
            output += System?.GetEntityBlueprintsWithCorpse()
                        ?.ToList()
                        ?.ConvertAll(cbp => cbp.ToString())
                        ?.GenerateBulletList();
            output += "\n\n";

            output += "Entities by Corpse" + "\n";
            output += "Corpse,Entities\n";
            output += System?.Necronomicon
                        ?.Select(page
                            => page.Key + ":\n"
                            + page.Value?.GetEntityWeights()
                                ?.ConvertToWeightedList()
                                ?.ConvertToStringList(kvp => kvp.Key + ": " + kvp.Value)
                                ?.GenerateBulletList(ItemPostProc: s => UIFriendlyNBPS(4) + s))
                        ?.ToList()
                        ?.ConvertAll(cbp => cbp.ToString())
                        ?.GenerateBulletList();
            output += "\n\n";

            output += "Full Breakdown" + "\n";
            output += System?.Necronomicon
                        ?.Select(page 
                            => page.Key + ":\n" 
                            + page.Value?.GetEntityWeightRelationships()
                                ?.ConvertToWeightedList()
                                ?.ConvertToStringList(kvp => kvp.Key + ": " + kvp.Value)
                                ?.GenerateBulletList(ItemPostProc: s => UIFriendlyNBPS(4) + s))
                        ?.ToList()
                        ?.ConvertAll(cbp => cbp.ToString())
                        ?.GenerateBulletList();
            output += "\n\n";

            Debug.SetSilenceLogging(false);
            string uIFriendlySpace = UIFriendlyNBPS(1);
            UnityEngine.Debug.Log(output.Replace(uIFriendlySpace, " ").Replace(Bullet(), "-"));
            Popup.Show(output);
        }
    }
}
