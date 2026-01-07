using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

using XRL.UI;
using XRL.Wish;
using XRL;
using XRL.Collections;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.WorldBuilders;

using UD_FleshGolems;
using UD_FleshGolems.Capabilities.Necromancy;
using static UD_FleshGolems.Utils;

using SerializeField = UnityEngine.SerializeField;

using Debug = UD_FleshGolems.Logging.Debug;

namespace UD_FleshGolems.Capabilities
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    [HasWishCommand]
    [Serializable]
    public class UD_FleshGolems_NecromancySystem : IScribedSystem
    {
        [Serializable]
        public enum CountsAs : int
        {
            Any,
            Blueprint,
            OtherCorpse,
            Subtype,
            Genotype,
            Species,
            Faction,
            Population,
            Keyword,
            None,
        }

        public const string EXCLUDE_FROM_CACHE_PROPTAG = "UD_FleshGolems Necromancy ExcludeFromNecronomicon";
        public const string IGNORE_EXCLUDE_PROPTAG = "UD_FleshGolems PastLife Ignore ExcludeFromDynamicEncounters WhenFinding";
        public const string CORPSE_COUNTS_AS_PROPTAG = "UD_FleshGolems PastLife CountsAs";
        public const string CORPSE_EXCLUDE_COUNTS_AS_PROPTAG = "UD_FleshGolems PastLife ExcludeFrom CountsAs";
        public const string CORPSE_COUNTS_AS_POSTLOAD_PROPTAG = "UD_FleshGolems_PostLoad_CountsAs";
        public const string PASTLIFE_BLUEPRINT_PROPTAG = "UD_FleshGolems_PastLife_Blueprint";

        [ModSensitiveStaticCache( CreateEmptyInstance = false )]
        public static UD_FleshGolems_NecromancySystem System;

        [SerializeField]
        private StringMap<CorpseSheet> Necronomicon;

        [SerializeField]
        private StringMap<EntityBlueprint> EntityBlueprints;

        [ModSensitiveStaticCache( CreateEmptyInstance = false )]
        [SerializeField]
        private bool Initialized;

        [ModSensitiveStaticCache( CreateEmptyInstance = false )]
        [SerializeField]
        private bool CorpseSheetsInitialized;

        [ModSensitiveStaticCache( CreateEmptyInstance = false )]
        [SerializeField]
        private bool EntityPrimaryCorpsesInitialized;

        [ModSensitiveStaticCache( CreateEmptyInstance = false )]
        [SerializeField]
        private bool CorpseProductsInitialized;

        [ModSensitiveStaticCache( CreateEmptyInstance = false )]
        [SerializeField]
        private bool CountsAsCorspesInitialized;

        [ModSensitiveStaticCache( CreateEmptyInstance = false )]
        [SerializeField]
        private bool InheritedCorspesInitialized;

        public GameObject TheMadMonger;

        public UD_FleshGolems_NecromancySystem()
        {
            Necronomicon = new();
            EntityBlueprints = new();
            Initialized = false;
            CorpseSheetsInitialized = false;
            EntityPrimaryCorpsesInitialized = false;
            CorpseProductsInitialized = false;
            CountsAsCorspesInitialized = false;
            InheritedCorspesInitialized = false;
            TheMadMonger = null;
        }

        [ModSensitiveCacheInit]
        [GameBasedCacheInit]
        public static void NecromancySystemInit()
        {
            if (System == null)
                System = The.Game?.RequireSystem(InitializeSystem);
            else
                The.Game?.AddSystem(System);

            bool success = false;
            try
            {
                if (System != null)
                    Loading.LoadTask("Compiling " + nameof(Necronomicon) + "...", System.Init);

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
                    MetricsManager.LogModInfo(ThisMod, nameof(UD_FleshGolems_NecromancySystem) + "." + nameof(NecromancySystemInit) + " Cache finished... " + (success ? "success!" : "failure!"));
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
            if (!Initialized)
            {
                Stopwatch sw = new();
                sw.Start();

                
                InitializeCorpseSheetsCorpses(out CorpseSheetsInitialized);
                InitializeEntityPrimaryCorpses(out EntityPrimaryCorpsesInitialized);
                InititializeCorpseProducts(out CorpseProductsInitialized);
                InitializeCountsAsCorspes(out CountsAsCorspesInitialized);
                
                Loading.SetLoadingStatus(null);

                sw.Stop();
                TimeSpan duration = sw.Elapsed;
                string timeUnit = duration.Minutes > 0 ? "minute" : "second";
                double timeDuration = duration.Minutes > 0 ? duration.TotalMinutes : duration.TotalSeconds;
                string cacheDebugMessage = "Corpse Cache took " + timeDuration.Things(timeUnit) + ".";
                
                UnityEngine.Debug.Log(cacheDebugMessage);

                TheMadMonger = GameObjectFactory.Factory.CreateObject(
                    ObjectBlueprint: "UD_FleshGolems Mad Monger",
                    Context: nameof(UD_FleshGolems_MadMonger_WorldBuilder));

                Initialized = true;
            }
        }

        public bool TryGetCorpseSheet(string Corpse, out CorpseSheet CorpseSheet)
        {
            Necronomicon ??= new();
            return Necronomicon.TryGetValue(Corpse, out CorpseSheet);
        }
        public CorpseSheet RequireCorpseSheet(string Blueprint)
        {
            if (!TryGetCorpseSheet(Blueprint, out CorpseSheet corpseSheet))
            {
                corpseSheet = new CorpseSheet(new CorpseBlueprint(Blueprint));
                Necronomicon[Blueprint] = corpseSheet;
                if (!corpseSheet.InheritedCorpsesInitialized)
                    corpseSheet.InitializeInheritedCorpseList();
            }
            return corpseSheet;
        }
        public CorpseSheet RequireCorpseSheet(GameObjectBlueprint Blueprint)
            => RequireCorpseSheet(Blueprint.Name);

        public CorpseSheet RequireCorpseSheet(CorpseBlueprint Blueprint)
            => RequireCorpseSheet(Blueprint.ToString());

        public bool TryGetEntityBlueprint(string Entity, out EntityBlueprint EntityBlueprint)
        {
            EntityBlueprints ??= new();
            return EntityBlueprints.TryGetValue(Entity, out EntityBlueprint);
        }
        public bool TryGetEntityBlueprint(GameObject Entity, out EntityBlueprint EntityBlueprint)
            => TryGetEntityBlueprint(Entity.Blueprint, out EntityBlueprint);

        public EntityBlueprint RequireEntityBlueprint(string Blueprint)
        {
            if (!TryGetEntityBlueprint(Blueprint, out EntityBlueprint entityBlueprint))
            {
                entityBlueprint = new EntityBlueprint(Blueprint);
                EntityBlueprints[Blueprint] = entityBlueprint;
            }
            return entityBlueprint;
        }
        public EntityBlueprint RequireEntityBlueprint(GameObjectBlueprint Blueprint)
            => RequireEntityBlueprint(Blueprint.Name);

        public EntityBlueprint RequireEntityBlueprint(GameObject Entity)
            => RequireEntityBlueprint(Entity.Blueprint);

        public static bool IsReanimatableCorpse(GameObjectBlueprint Blueprint)
            => UD_FleshGolems_NanoNecroAnimation.IsReanimatableCorpse(Blueprint);

        public static bool IsReanimatableCacheableCorpse(GameObjectBlueprint Blueprint)
            => IsReanimatableCorpse(Blueprint)
            && !Blueprint.HasTagOrProperty(EXCLUDE_FROM_CACHE_PROPTAG);

        public UD_FleshGolems_NecromancySystem InitializeCorpseSheetsCorpses(out bool Initialized)
        {
            if (!CorpseSheetsInitialized)
            {
                int counter = 0;
                foreach (GameObjectBlueprint blueprint in GameObjectFactory.Factory.BlueprintList.Where(IsReanimatableCacheableCorpse))
                {
                    SetLoadingStatusCorpses(counter++ % 100 == 0);
                    RequireCorpseSheet(blueprint.Name);
                }
            }
            Initialized = true;
            return this;
        }

        public IEnumerable<EntityBlueprint> GetEntityBlueprintsWithCorpse(Predicate<GameObjectBlueprint> Filter = null)
        {
            if (!EntityPrimaryCorpsesInitialized)
                yield break;

            List<EntityBlueprint> entityBlueprints = EntityBlueprints
                ?.Where(entry => Filter == null || Filter(entry.Value.GetGameObjectBlueprint()))
                ?.Select(entry => entry.Value)
                ?.ToList();

            foreach (EntityBlueprint entityBlueprint in entityBlueprints)
                yield return entityBlueprint;
        }
        public IEnumerable<GameObjectBlueprint> GetEntityModelsWithCorpse(Predicate<GameObjectBlueprint> Filter = null)
        {
            foreach (EntityBlueprint entityBlueprint in GetEntityBlueprintsWithCorpse(Filter))
                yield return entityBlueprint.GetGameObjectBlueprint();
        }

        public static bool BlueprintHasCorpseThatIsCorpse(GameObjectBlueprint Blueprint)
            => Blueprint.HasPart(nameof(Corpse))
            && Blueprint.TryGetCorpseModel(out GameObjectBlueprint corpseModel)
            && corpseModel.IsCorpse();

        public UD_FleshGolems_NecromancySystem InitializeEntityPrimaryCorpses(out bool Initialized)
        {
            if (!EntityPrimaryCorpsesInitialized)
            {
                int counter = 0;
                foreach (GameObjectBlueprint corpseHavingEntity in GameObjectFactory.Factory.GetBlueprints(BlueprintHasCorpseThatIsCorpse))
                {
                    SetLoadingStatusCorpses(counter++ % 50 == 0);
                    if (!corpseHavingEntity.IsChiliad()
                        && corpseHavingEntity.TryGetCorpseBlueprintAndChance(out string corpseBlueprint, out int corpseChance))
                        RequireCorpseSheet(corpseBlueprint)
                            .AddPrimaryEntity(corpseHavingEntity, corpseChance);
                }
            }
            Initialized = true;
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
                if (corpseSheet.GetCorpseBlueprint() is not null)
                    yield return corpseSheet.GetCorpse();
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
                if (corpseSheet.GetCorpseBlueprint() is GameObjectBlueprint corpseBlueprint)
                    if (Filter == null || Filter(corpseBlueprint))
                        yield return corpseSheet.GetCorpse();
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
                if (Filter == null || Filter(corpseSheet.GetCorpse()))
                    yield return corpseSheet.GetCorpse();
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
                if (Filter == null || Filter(corpseSheet))
                    yield return corpseSheet.GetCorpse();
        }
        public IEnumerable<CorpseSheet> GetCorpseSheets(Predicate<CorpseSheet> Filter)
        {
            if (!CorpseSheetsInitialized)
            {
                string cacheName = nameof(UD_FleshGolems_NecromancySystem) + "." + nameof(Necronomicon);
                MetricsManager.LogModError(ThisMod, "Attempted to iterate " + cacheName + " before it was initialized");
                yield break;
            }
            foreach ((_, CorpseSheet corpseSheet) in Necronomicon)
                if (Filter == null || Filter(corpseSheet))
                    yield return corpseSheet;
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
            if (CorpseBlueprint == null)
                return new();

            if (!CorpseBlueprint.TryGetPartParameter(ProcessableType, OnSuccessFieldName, out string processedProductValue))
                return new();

            if (processedProductValue.IsNullOrEmpty())
                return new();

            List<GameObjectBlueprint> outputList = new();
            if (!processedProductValue.StartsWith('@'))
            {
                if (processedProductValue.GetGameObjectBlueprint() is GameObjectBlueprint processedProductValueBlueprint
                    && (ProductFilter == null
                        || ProductFilter(processedProductValueBlueprint))
                    && processedProductValueBlueprint.IsCorpse())
                    outputList.AddUnique(processedProductValueBlueprint);
            }
            else
                foreach (string populationResult in GetDistinctFromPopulation(processedProductValue[1..], 15))
                    if (populationResult.GetGameObjectBlueprint() is GameObjectBlueprint processedProductBlueprint
                        && processedProductBlueprint.IsCorpse()
                        && (ProductFilter == null
                            || ProductFilter(processedProductBlueprint)))
                        outputList.AddUnique(processedProductBlueprint);

            return outputList;
        }
        public UD_FleshGolems_NecromancySystem InititializeCorpseProducts(out bool Initialized)
        {
            if (!CorpseProductsInitialized)
            {
                foreach (CorpseBlueprint corpseBlueprint in GetCorpseBlueprints(IsProcessableCorpse))
                {
                    List<GameObjectBlueprint> possibleProducts = new();

                    List<GameObjectBlueprint> butcherableProducts = new(GetProcessableCorpsesProducts(
                        CorpseBlueprint: corpseBlueprint.GetGameObjectBlueprint(),
                        ProcessableType: nameof(Butcherable),
                        OnSuccessFieldName: nameof(Butcherable.OnSuccess)));

                    if (!butcherableProducts.IsNullOrEmpty())
                        foreach (GameObjectBlueprint butcherableProduct in butcherableProducts)
                            possibleProducts.Add(butcherableProduct);

                    List<GameObjectBlueprint> harvestableProducts = new(GetProcessableCorpsesProducts(
                        CorpseBlueprint: corpseBlueprint.GetGameObjectBlueprint(),
                        ProcessableType: nameof(Harvestable),
                        OnSuccessFieldName: nameof(Harvestable.OnSuccess)));

                    if (!harvestableProducts.IsNullOrEmpty())
                        foreach (GameObjectBlueprint harvestableProduct in harvestableProducts)
                            possibleProducts.Add(harvestableProduct);

                    int counter = 0;
                    foreach (GameObjectBlueprint possibleProduct in possibleProducts)
                    {
                        SetLoadingStatusCorpses(counter++ % 50 == 0);
                        if (possibleProduct.IsCorpse())
                        {
                            CorpseSheet productCorpseSheet = RequireCorpseSheet(possibleProduct.Name);
                            productCorpseSheet.IsCorpseProduct = true;

                            // add the corpse we got the product from to the corpseProduct's list.
                            productCorpseSheet.AddProductOriginCorpse(corpseBlueprint);

                            // add the entity weight entries for the corpse we got the product from to the product's corpse sheet.
                            CorpseSheet corpseSheet = RequireCorpseSheet(corpseBlueprint.ToString());

                            foreach (EntityWeight entityWeight in corpseSheet.GetEntityWeights())
                                productCorpseSheet.AddProductEntity(entityWeight);
                        }
                    }
                }
            }
            Initialized = true;
            return this;
        }

        public static bool PossiblyExcludedFromDynamicEncounters(GameObjectBlueprint Blueprint)
            => Blueprint != null
            && Blueprint.HasTagOrProperty(IGNORE_EXCLUDE_PROPTAG)
            || !Blueprint.IsExcludedFromDynamicEncounters();

        public bool IsEntityWithCorpse(GameObjectBlueprint Entity)
            => GetEntityModelsWithCorpse().Contains(Entity);

        public bool IsNotExcludedFromCountsAs(GameObjectBlueprint EntityModel, CountsAs CountsAs)
            => CountsAs > CountsAs.OtherCorpse
            || !EntityModel.HasTagOrProperty(CORPSE_EXCLUDE_COUNTS_AS_PROPTAG);

        private List<EntityWeightCountsAs> GetCorpseCountsAsEntityWeights(
            CountsAs CountsAs,
            List<string> CountsAsParamaters)
        {
            List<GameObjectBlueprint> entitiesWithCorpses = new();
            foreach (EntityBlueprint entityBlueprint in EntityBlueprints.Values)
                if (entityBlueprint.GetGameObjectBlueprint() is var entityModel
                    && IsNotExcludedFromCountsAs(entityModel, CountsAs))
                    entitiesWithCorpses.Add(entityBlueprint.GetGameObjectBlueprint());

            List<GameObjectBlueprint> countsAsModels = new();
            void addToCountsAsModels(List<string> countsAsBlueprintList)
            {
                if (countsAsBlueprintList != null)
                    foreach (string countsAsBlueprint in countsAsBlueprintList)
                        if (countsAsBlueprint.GetGameObjectBlueprint() is GameObjectBlueprint countsAsModel
                            && IsEntityWithCorpse(countsAsModel))
                            countsAsModels.Add(countsAsModel);
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
                    addToCountsAsModels(countsAsPropTagBlueprints);
            }
            GameObjectBlueprint toGameObjectBlueprint(string blueprint) => blueprint.GetGameObjectBlueprint();
            bool isNot = false;
            string primaryCountsAsParam = CountsAsParamaters[1];
            if (primaryCountsAsParam.EqualsNoCase("not"))
            {
                primaryCountsAsParam = CountsAsParamaters[2];
                isNot = true;
            }
            switch (CountsAs)
            {
                case CountsAs.Any:
                    countsAsModels = entitiesWithCorpses;
                    break;

                case CountsAs.Keyword:
                    countsAsModels = ProcessCountsAsKeyword(CountsAsParamaters);
                    break;

                case CountsAs.Blueprint:
                    if (primaryCountsAsParam is string countsAsBlueprintParam)
                        countsAsModels = countsAsBlueprintParam
                            .CachedCommaExpansion()
                            .ConvertAll(toGameObjectBlueprint);
                    break;

                case CountsAs.Population:
                    if (primaryCountsAsParam is string countsAsPopulationParam)
                        foreach (string countsAsPopulation in countsAsPopulationParam.CachedCommaExpansion())
                            countsAsModels.AddRange(PopulationManager.GetEach(countsAsPopulationParam).ConvertAll(toGameObjectBlueprint));
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
                                if ((countsAsFactionParamNew && !faction.Old)
                                    || (countsAsFactionParamOld && faction.Old))
                                    countsAsFactions.Add(faction);
                        }
                        else
                        if (countsAsFactionParam.Contains(","))
                        {
                            foreach (string countsAsFaction in countsAsFactionParam.CachedCommaExpansion())
                                if (Factions.Get(countsAsFaction) is Faction faction)
                                {
                                    countsAsFactions.Add(faction);
                                }
                        }
                        else
                        if (Factions.Get(countsAsFactionParam) is Faction faction)
                            countsAsFactions.Add(faction);
                        foreach (Faction countsAsFaction in countsAsFactions)
                            if (countsAsFaction.GetMembers(IsEntityWithCorpse) is List<GameObjectBlueprint> factionMembersWithCorpse)
                                countsAsModels.AddRange(factionMembersWithCorpse);
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
                        foreach (string countsAsOtherCorpse in countsAsOtherCorpseList)
                            if (RequireCorpseSheet(countsAsOtherCorpse)
                                ?.GetEntities()
                                ?.ToList()
                                ?.ConvertAll(eb => eb.GetGameObjectBlueprint())
                                is List<GameObjectBlueprint> countsAsOtherCorpseModels
                                && !countsAsOtherCorpseModels.IsNullOrEmpty())
                                countsAsModels.AddRange(countsAsOtherCorpseModels);
                    break;

                case CountsAs.None:
                default:
                    break;
            }
            if (!countsAsModels.IsNullOrEmpty())
            {
                int weight = 0;
                if (CountsAs == CountsAs.Any)
                {
                    if (CountsAsParamaters.Count > 1
                        && int.TryParse(primaryCountsAsParam, out int countsAsAnyParamWeight))
                        weight = Math.Min(countsAsAnyParamWeight, 100);
                    else
                        weight = 100;
                }
                if (CountsAsParamaters.Count > 2
                    && int.TryParse(CountsAsParamaters[^1], out int countsAsParamWeight))
                    weight = Math.Min(countsAsParamWeight, 100);

                if (isNot)
                    return entitiesWithCorpses.Where(bp => !countsAsModels.Contains(bp))
                        .ToList()
                        .ConvertAll(bpm
                        => new EntityWeightCountsAs(
                            Entity: RequireEntityBlueprint(bpm.Name),
                            Weight: weight,
                            CountsAs: CountsAs));

                return countsAsModels.ConvertAll(bpm 
                    => new EntityWeightCountsAs(
                        Entity: RequireEntityBlueprint(bpm.Name),
                        Weight: weight,
                        CountsAs: CountsAs));
            }
            return new();
        }

        public List<GameObjectBlueprint> ProcessCountsAsKeyword(List<string> CountsAsParamaters)
        {
            List<GameObjectBlueprint> keywordBlueprintList = new();
            if (CountsAsParamaters.IsNullOrEmpty()
                || CountsAsParamaters.Count < 2)
                return keywordBlueprintList;

            List<GameObjectBlueprint> entitiesWithCorpses = new();
            foreach (EntityBlueprint entityBlueprint in EntityBlueprints.Values)
                entitiesWithCorpses.Add(entityBlueprint.GetGameObjectBlueprint());

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
                    keywordBlueprintList = entitiesWithCorpses.Where(Extensions.IsCorpse).ToList();
                    break;
                case "Dead":
                case "Corpse":
                    keywordBlueprintList = entitiesWithCorpses.Where(Extensions.IsCorpse).ToList();
                    break;
            }
            if (isNot)
                return entitiesWithCorpses.Where(bp => !keywordBlueprintList.Contains(bp)).ToList();

            return keywordBlueprintList;
        }

        private UD_FleshGolems_NecromancySystem InitializeCountsAsCorspes(out bool Initialized)
        {
            if (!CountsAsCorspesInitialized)
            {
                int counter = 0;
                List<CorpseCountsAs> corpseCountsAsList = new();
                foreach (CorpseBlueprint corpseBlueprint in GetCorpseBlueprints())
                {
                    if (corpseBlueprint?.GetGameObjectBlueprint() is not GameObjectBlueprint corpseModel
                        || !corpseModel.TryGetStringPropertyOrTag(CORPSE_COUNTS_AS_PROPTAG, out string rawCountsAsParams))
                        continue;

                    if (CorpseCountsAs.GetCorpseCountsAs(corpseBlueprint, rawCountsAsParams) is CorpseCountsAs corpseCountsAs
                        && corpseCountsAs.CountasAs != CountsAs.None)
                        corpseCountsAsList.Add(corpseCountsAs);
                }

                foreach ((string name, string value) in GameObjectFactory.Factory?.GetBlueprintIfExists(CORPSE_COUNTS_AS_POSTLOAD_PROPTAG)?.Tags)
                    if (name.GetGameObjectBlueprint() is var corpseModel
                        && corpseModel.IsCorpse()
                        && RequireCorpseSheet(corpseModel) is CorpseSheet postLoadCorpseSheet
                        && CorpseCountsAs.GetCorpseCountsAs(postLoadCorpseSheet.GetCorpse(), value) is CorpseCountsAs corpseCountsAs
                        && corpseCountsAs.CountasAs != CountsAs.None)
                        corpseCountsAsList.Add(corpseCountsAs);

                foreach (CorpseCountsAs corpseCountsAs in corpseCountsAsList)
                {
                    List<EntityWeightCountsAs> countsAsModels = GetCorpseCountsAsEntityWeights(corpseCountsAs.CountasAs, corpseCountsAs.Paramaters);
                    if (countsAsModels.IsNullOrEmpty())
                        continue;

                    CorpseSheet corpseSheet = RequireCorpseSheet(corpseCountsAs.Blueprint);
                    foreach ((EntityBlueprint entityBlueprint, int weight, CountsAs countsAs) in countsAsModels)
                    {
                        SetLoadingStatusCorpses(counter++ % 50 == 0);
                        corpseSheet.AddCountsAsEntity(new EntityWeight(entityBlueprint, weight));
                    }
                }
            }
            Initialized = true;
            return this;
        }
        private UD_FleshGolems_NecromancySystem InitializeInheritedCorspes(out bool Initialized)
        {
            if (InheritedCorspesInitialized)
            {
                int counter = 0;
                foreach (CorpseBlueprint corpseBlueprint in GetCorpseBlueprints())
                {
                    CorpseSheet corpseSheet = RequireCorpseSheet(corpseBlueprint.ToString());
                    List<EntityWeight> corpseSheetEntityWeightList = corpseSheet.GetEntityWeights().ToList();
                    foreach (CorpseBlueprint inheritedCorpse in corpseSheet.GetInheritedCorpseList())
                    {
                        SetLoadingStatusCorpses(counter++ % 50 == 0);
                        RequireCorpseSheet(inheritedCorpse).AddInheritedEntities(corpseSheetEntityWeightList);
                    }
                }
            }
            Initialized = true;
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

        public Dictionary<string, int> GetWeightedCorpseStringsForEntity(EntityBlueprint EntityBlueprint, Predicate<CorpseSheet> CorpseSheetFilter = null)
        {
            List<CorpseWeight> corpseWeights = new();

            foreach (CorpseSheet corpseSheet in GetCorpseSheets(CorpseSheetFilter))
                if (corpseSheet.GetCorpseWeight(EntityBlueprint) is CorpseWeight corpseWeight)
                    corpseWeights.Add(corpseWeight);

            return corpseWeights?.ToDictionary(cw => cw.GetBlueprint().ToString(), cw => cw.Weight);
        }

        public Dictionary<string, int> GetWeightedCorpseStringsForEntity(string Entity, Predicate<CorpseSheet> CorpseSheetFilter = null)
            => GetWeightedCorpseStringsForEntity(RequireEntityBlueprint(Entity), CorpseSheetFilter);

        public Dictionary<string, int> GetWeightedCorpseStringsForEntity(GameObjectBlueprint EntityModel, Predicate<CorpseSheet> CorpseSheetFilter = null)
            => GetWeightedCorpseStringsForEntity(EntityModel.Name, CorpseSheetFilter);

        /*
         * 
         * Wishes!
         * 
         */

        [WishCommand("UD_FleshGolems gimme cache")]
        public static void Debug_GimmeCache_WishHandler()
        {
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

            string uIFriendlySpace = UIFriendlyNBPS(1);
            UnityEngine.Debug.Log(output.Replace(uIFriendlySpace, " ").Replace(Bullet(), "-"));
            Popup.Show(output);
        }
    }
}
