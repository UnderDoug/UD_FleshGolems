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

namespace XRL.World.Parts
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    [HasWishCommand]
    [Serializable]
    public class UD_FleshGolems_PastLife : IScribedPart
    {
        [UD_FleshGolems_DebugRegistry]
        public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
        {
            /*
            Type thisType = typeof(UD_FleshGolems_PastLife);
            MethodBase GetMethod(string methodName)
            {
                return thisType.GetMethod(methodName);
            }
            Registry.Register(GetMethod(nameof(GetProcessableCorpsesProducts)), false);
            Registry.Register(GetMethod(nameof(GetProcessableCorpsesAndTheirProducts)), true);
            Registry.Register(GetMethod(nameof(GetCorpsesThisProductComesFrom)), true);
            Registry.Register(GetMethod(nameof(EntityCouldHaveComeFromThisCorpse)), true);
            Registry.Register(GetMethod(nameof(GetBlueprintsWhoseCorpseThisCouldBe)), true);
            Registry.Register(GetMethod(nameof(GetALivingBlueprintForCorpseWeighted)), true);
            Registry.Register(GetMethod(nameof(GetALivingBlueprintForCorpse)), true);
            */
            Registry.Register(nameof(GetCorpseBlueprints), false);
            Registry.Register(nameof(GetEntitiesWithCorpseBlueprints), false);
            Registry.Register(nameof(GetProcessableCorpsesProducts), false);
            Registry.Register(nameof(GetProcessableCorpsesAndTheirProducts), false);
            Registry.Register(nameof(GetCorpsesThisProductComesFrom), false);
            Registry.Register(nameof(EntityCouldHaveComeFromThisCorpse), false);
            Registry.Register(nameof(GetBlueprintsWhoseCorpseThisCouldBe), true);
            Registry.Register(nameof(GetALivingBlueprintForCorpseWeighted), true);
            Registry.Register(nameof(GetALivingBlueprintForCorpse), true);
            return Registry;
        }

        [Serializable]
        public class UD_FleshGolems_DeathAddress : IComposite
        {
            public string DeathZone;
            public int X;
            public int Y;

            private UD_FleshGolems_DeathAddress()
            {
                DeathZone = null;
                X = 0;
                Y = 0;
            }
            public UD_FleshGolems_DeathAddress(string DeathZone, int X, int Y)
                : this()
            {
                this.DeathZone = DeathZone;
                this.X = X;
                this.Y = Y;
            }
            public UD_FleshGolems_DeathAddress(string DeathZone, Location2D DeathLocation)
                : this(DeathZone, DeathLocation.X, DeathLocation.Y)
            {
            }

            public Location2D GetLocation() => new(X, Y);

            public ZoneRequest GetZoneRequest() => new(DeathZone);

            public Cell GetCell() => The.ZoneManager?.GetZone(DeathZone)?.GetCell(X, Y);

            public static explicit operator Cell(UD_FleshGolems_DeathAddress Source)
            {
                return Source.GetCell();
            }
        }

        [Serializable]
        public class UD_FleshGolems_InstalledCybernetic : IComposite
        {
            public string ImplantedLimbType;

            [SerializeField]
            private string CyberneticID;

            private GameObject _Cybernetic;
            public GameObject Cybernetic
            {
                get => _Cybernetic ??= GameObject.FindByID(CyberneticID);
                set
                {
                    CyberneticID = value?.ID;
                    _Cybernetic = value;
                }
            }

            protected UD_FleshGolems_InstalledCybernetic()
            {
                ImplantedLimbType = null;
                Cybernetic = null;
            }
            public UD_FleshGolems_InstalledCybernetic(GameObject Cybernetic, string ImplantedPart)
                : this()
            {
                ImplantedLimbType = ImplantedPart;
                this.Cybernetic = Cybernetic;
            }
            public UD_FleshGolems_InstalledCybernetic(GameObject Cybernetic, BodyPart ImplantedPart)
                : this(Cybernetic, ImplantedPart.Type)
            {
            }
            public UD_FleshGolems_InstalledCybernetic(GameObject Cybernetic, Body ImplantedBody)
                : this(Cybernetic, ImplantedBody.FindCybernetics(Cybernetic))
            {
            }
            public UD_FleshGolems_InstalledCybernetic(GameObject Cybernetic)
                : this(Cybernetic, Cybernetic?.Implantee?.Body)
            {
            }
            public void Deconstruct(out GameObject Cybernetic, out string ImplantedLimbType)
            {
                ImplantedLimbType = this.ImplantedLimbType;
                Cybernetic = this.Cybernetic;
            }
            public void Deconstruct(out GameObject Cybernetic)
            {
                Cybernetic = this.Cybernetic;
            }
            public void Deconstruct(out string ImplantedLimbType)
            {
                ImplantedLimbType = this.ImplantedLimbType;
            }

            public static implicit operator KeyValuePair<GameObject, string>(UD_FleshGolems_InstalledCybernetic Source) => new(Source.Cybernetic, Source.ImplantedLimbType);
            public static implicit operator UD_FleshGolems_InstalledCybernetic(KeyValuePair<GameObject, string> Source) => new(Source.Key, Source.Value);
        }

        public class BlueprintWeightPair : IEquatable<KeyValuePair<string, int>>
        {
            public string Blueprint;
            public int Weight;

            public BlueprintWeightPair(string Blueprint, int Weight)
            {
                this.Blueprint = Blueprint;
                this.Weight = Weight;
            }

            public GameObjectBlueprint GetGameObjectBlueprint()
            {
                return Blueprint.GetGameObjectBlueprint();
            }

            public override string ToString()
            {
                return Blueprint + ": " + Weight;
            }

            public void Deconstruct(out string Blueprint) => Blueprint = this.Blueprint;
            public void Deconstruct(out int Weight) => Weight = this.Weight;
            public void Deconstruct(out string Blueprint, out int Weight)
            {
                Deconstruct(out Blueprint);
                Deconstruct(out Weight);
            }
            public void Deconstruct(out GameObjectBlueprint Blueprint) => Blueprint = GetGameObjectBlueprint();

            public static implicit operator KeyValuePair<string, int>(BlueprintWeightPair Operand) => new(Operand.Blueprint, Operand.Weight);
            public static implicit operator BlueprintWeightPair(KeyValuePair<string, int> Operand) => new(Operand.Key, Operand.Value);

            public static explicit operator string(BlueprintWeightPair Operand) => Operand.Blueprint;
            public static explicit operator int(BlueprintWeightPair Operand) => Operand.Weight;
            public static explicit operator GameObjectBlueprint(BlueprintWeightPair Operand) => Operand.GetGameObjectBlueprint();

            public override bool Equals(object obj)
            {
                if (obj is KeyValuePair<string, int> kvpObj)
                {
                    return Equals(kvpObj);
                }
                return base.Equals(obj);
            }

            public override int GetHashCode() => Blueprint.GetHashCode() ^ Weight.GetHashCode();

            public bool Equals(KeyValuePair<string, int> other)
            {
                return Blueprint.Equals(other.Key)
                    && Weight.Equals(other.Value);
            }

            public static bool operator ==(BlueprintWeightPair Operand1, KeyValuePair<string, int> Operand2) => Operand1.Equals(Operand2);
            public static bool operator !=(BlueprintWeightPair Operand1, KeyValuePair<string, int> Operand2) => !(Operand1 == Operand2);

            public static bool operator ==(KeyValuePair<string, int> Operand1, BlueprintWeightPair Operand2) => Operand2 == Operand1;
            public static bool operator !=(KeyValuePair<string, int> Operand1, BlueprintWeightPair Operand2) => Operand2 != Operand1;
        }

        public const string IGNORE_EXCLUDE_TAG = "UD_FleshGolems PastLife Ignore ExcludeFromDynamicEncounters WhenFinding";
        public const string CACHE_EMPTY = "empty";

        [ModSensitiveStaticCache(CreateEmptyInstance = false)]
        [GameBasedStaticCache(ClearInstance = false)]
        public static StringMap<GameObjectBlueprint> CorpseBlueprints = new();

        public static StringMap<GameObjectBlueprint> GetProcessableCorpseBlueprints() =>
            (from bp in CorpseBlueprints 
             where IsProcessableCorpse(bp.Value) 
             select bp) as StringMap<GameObjectBlueprint>;

        [ModSensitiveStaticCache(CreateEmptyInstance = false)]
        [GameBasedStaticCache(ClearInstance = false)]
        public static StringMap<GameObjectBlueprint> CorpseCorpseProductBlueprints = new(); // corpse products that are also corpses.

        [ModSensitiveStaticCache(CreateEmptyInstance = false)]
        [GameBasedStaticCache(ClearInstance = false)]
        public static StringMap<GameObjectBlueprint> EntitiesWithCorpseBlueprints = new();

        [ModSensitiveStaticCache(CreateEmptyInstance = false)]
        [GameBasedStaticCache(ClearInstance = false)]
        public static StringMap<string> CorpseByEntity = new();

        [ModSensitiveStaticCache(CreateEmptyInstance = false)]
        [GameBasedStaticCache(ClearInstance = false)]
        public static StringMap<List<string>> ProcessablesByProduct = new();

        [ModSensitiveStaticCache(CreateEmptyInstance = false)]
        [GameBasedStaticCache(ClearInstance = false)]
        public static StringMap<List<string>> ProductsByProcessable = new();

        [ModSensitiveStaticCache(CreateEmptyInstance = false)]
        [GameBasedStaticCache(ClearInstance = false)]
        public static StringMap<List<string>> EntitiesByCorpse = new();

        [ModSensitiveStaticCache(CreateEmptyInstance = false)]
        [GameBasedStaticCache(ClearInstance = false)]
        public static StringMap<List<BlueprintWeightPair>> EntityWithWeightByCorpse = new();

        public const string PASTLIFE_BLUEPRINT_PROPTAG = "UD_FleshGolems_PastLife_Blueprint";

        public GameObject BrainInAJar;

        public UD_FleshGolems_PastLife PastPastLife => BrainInAJar?.GetPart<UD_FleshGolems_PastLife>();

        public bool Init { get; protected set; }
        public bool WasCorpse => (Blueprint?.IsCorpse()).GetValueOrDefault();

        public bool WasPlayer => (!Blueprint.IsNullOrEmpty() && Blueprint.IsPlayerBlueprint()) 
            || (BrainInAJar != null && BrainInAJar.HasPropertyOrTag("UD_FleshGolems_WasPlayer"));

        public int TimesReanimated;

        public string Blueprint;

        public bool ExcludeFromDynamicEncounters => Blueprint.IsExcludedFromDynamicEncounters();

        [SerializeField]
        private string _BaseDisplayName;
        public string BaseDisplayName => _BaseDisplayName ??= BrainInAJar?.BaseDisplayName;

        [SerializeField]
        private string _RefName;
        public string RefName => _RefName ??= BrainInAJar?.GetReferenceDisplayName(Short: true);
        public bool WasProperlyNamed => (BrainInAJar?.HasProperName).GetValueOrDefault();
        public Titles Titles => BrainInAJar?.GetPart<Titles>();
        public Epithets Epithets => BrainInAJar?.GetPart<Epithets>();
        public Honorifics Honorifics => BrainInAJar?.GetPart<Honorifics>();

        public Render PastRender => BrainInAJar?.Render;

        private string _Description;
        public string Description => _Description ??= BrainInAJar?.GetPart<Description>()?._Short;

        public UD_FleshGolems_DeathAddress DeathAddress;

        public Physics Physics => BrainInAJar?.Physics;

        public Brain Brain => BrainInAJar?.Brain;

        public Gender Gender => BrainInAJar?.GetGender();
        public PronounSet PronounSet => BrainInAJar?.GetPronounSet();

        public string ConversationScriptID => BrainInAJar?.GetPart<ConversationScript>()?.ConversationID;

        public Dictionary<string, Statistic> Stats => BrainInAJar?.Statistics;

        public Body Body => BrainInAJar?.Body;
        public string Species => BrainInAJar?.GetSpecies();
        public string Genotype => BrainInAJar?.GetGenotype();
        public string Subtype => BrainInAJar?.GetSubtype();
        public Corpse Corpse => BrainInAJar?.GetPart<Corpse>();
        public string BleedLiquid => BrainInAJar?.GetBleedLiquid();

        public Mutations Mutations => BrainInAJar?.GetPart<Mutations>();

        public Skills Skills => BrainInAJar?.GetPart<Skills>();

        public Dictionary<string, string> Tags => Blueprint?.GetGameObjectBlueprint()?.Tags;
        public Dictionary<string, string> StringProperties => BrainInAJar?._Property;
        public Dictionary<string, int> IntProperties => BrainInAJar?._IntProperty;

        public EffectRack Effects => BrainInAJar?._Effects;

        public List<UD_FleshGolems_InstalledCybernetic> InstalledCybernetics;

        public UD_FleshGolems_PastLife()
        {
            BrainInAJar = GetNewBrainInAJar();
            Init = false;

            TimesReanimated = 0;

            Blueprint = null;

            _BaseDisplayName = null;
            _RefName = null;
            _Description = null;

            DeathAddress = null;

            InstalledCybernetics = new();
        }
        public UD_FleshGolems_PastLife(GameObject PastLife)
            : this()
        {
            Initialize(PastLife);
        }
        public UD_FleshGolems_PastLife(UD_FleshGolems_PastLife PrevPastLife)
            : this()
        {
            Initialize(PrevPastLife);
        }

        private static void CacheEntityWithWeightByCorpse(string CorpseBlueprint, BlueprintWeightPair Entry)
        {
            EntityWithWeightByCorpse ??= new();
            EntityWithWeightByCorpse[CorpseBlueprint] ??= new();
            if (EntityWithWeightByCorpse[CorpseBlueprint].Find(e => e.Blueprint == Entry.Blueprint) is BlueprintWeightPair entityWithWeight)
            {
                entityWithWeight.Weight += Entry.Weight;
            }
            else
            {
                EntityWithWeightByCorpse[CorpseBlueprint].Add(Entry);
            }
        }
        private static void CacheEntityWithWeightByCorpse(string CorpseBlueprint, List<BlueprintWeightPair> Entries)
        {
            EntityWithWeightByCorpse ??= new();
            foreach (BlueprintWeightPair entry in Entries)
            {
                CacheEntityWithWeightByCorpse(CorpseBlueprint, entry);
            }
        }

        private static void CacheEntitesByCorpse(string CorpseBlueprint, string EntityBlueprint)
        {
            EntitiesByCorpse ??= new();
            EntitiesByCorpse[CorpseBlueprint] ??= new();
            if (!EntitiesByCorpse[CorpseBlueprint].Any(bp => bp == EntityBlueprint))
            {
                EntitiesByCorpse[CorpseBlueprint].Add(EntityBlueprint);
            }
        }
        private static void CacheEntitesByCorpse(string CorpseBlueprint, List<string> EntityBlueprints)
        {
            EntitiesByCorpse ??= new();
            foreach (string entity in EntityBlueprints)
            {
                CacheEntitesByCorpse(CorpseBlueprint, entity);
            }
        }

        private static void CacheStringMapListString(StringMap<List<string>> ListStringCache, string Key, string Value)
        {
            ListStringCache ??= new();
            ListStringCache[Key] ??= new();
            if (!ListStringCache[Key].Any(bp => bp == Value))
            {
                ListStringCache[Key].Add(Value);
            }
        }
        private static void CacheStringMapListString(StringMap<List<string>> ListStringCache, string Key, List<string> ListString)
        {
            ListStringCache ??= new();
            foreach (string @string in ListString)
            {
                CacheStringMapListString(ListStringCache, Key, @string);
            }
        }

        private static GameObject GetNewBrainInAJar()
        {
            return GameObjectFactory.Factory.CreateUnmodifiedObject("UD_FleshGolems Brain In A Jar Widget");
        }

        // Make a few different predicate-likes here to make it easier to understand the mess below.

        public static bool IsProcessableCorpse(GameObjectBlueprint Corpse, Predicate<GameObjectBlueprint> Filter)
        {
            return Corpse.IsCorpse(Filter)
                && (Corpse.HasPart(nameof(Butcherable)) || Corpse.HasPart(nameof(Harvestable)));
        }
        public static bool IsProcessableCorpse(GameObjectBlueprint Corpse)
        {
            return IsProcessableCorpse(Corpse, null);
        }

        public static List<GameObjectBlueprint> GetCorpseBlueprints()
        {
            List<GameObjectBlueprint> blueprintsList = new();
            if (!CorpseBlueprints.IsNullOrEmpty())
            {
                foreach (GameObjectBlueprint blueprint in CorpseBlueprints.Values)
                {
                    blueprintsList.Add(blueprint);
                }
            }
            else
            {
                foreach (GameObjectBlueprint blueprint in GameObjectFactory.Factory.BlueprintList)
                {
                    if (blueprint.IsCorpse())
                    {
                        CorpseBlueprints[blueprint.Name] = blueprint;
                        blueprintsList.Add(blueprint);
                    }
                }
            }
            return blueprintsList;
        }

        public static List<GameObjectBlueprint> GetCorpseBlueprints(Predicate<GameObjectBlueprint> Filter)
        {
            List<GameObjectBlueprint> blueprintsList = new();
            foreach (GameObjectBlueprint blueprint in GetCorpseBlueprints())
            {
                if (Filter == null || Filter(blueprint))
                {
                    blueprintsList.Add(blueprint);
                }
            }
            return blueprintsList;
        }

        public static List<GameObjectBlueprint> GetEntitiesWithCorpseBlueprints()
        {
            List<GameObjectBlueprint> entityBlueprints = new();
            if (!EntitiesWithCorpseBlueprints.IsNullOrEmpty())
            {
                foreach (GameObjectBlueprint blueprint in EntitiesWithCorpseBlueprints.Values)
                {
                    entityBlueprints.Add(blueprint);
                }
            }
            else
            {
                foreach (GameObjectBlueprint blueprint in GameObjectFactory.Factory.BlueprintList)
                {
                    if (!blueprint.IsBaseBlueprint()
                        && !blueprint.IsChiliad()
                        && blueprint.TryGetCorpseBlueprintAndChance(out string corpseBlueprint, out int corpseChance)
                        && corpseBlueprint.IsCorpse())
                    {
                        EntitiesWithCorpseBlueprints[blueprint.Name] = blueprint;
                        CorpseByEntity[blueprint.Name] = corpseBlueprint;
                        CacheEntityWithWeightByCorpse(corpseBlueprint, new BlueprintWeightPair(blueprint.Name, corpseChance));
                        entityBlueprints.Add(blueprint);
                    }
                }
            }
            return entityBlueprints;
        }

        public static IEnumerable<GameObjectBlueprint> GetEntitiesWithCorpseBlueprints(Predicate<GameObjectBlueprint> Filter)
        {
            foreach (GameObjectBlueprint blueprint in GetEntitiesWithCorpseBlueprints())
            {
                if (Filter == null || Filter(blueprint))
                {
                    yield return blueprint;
                }
            }
            yield break;
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
        public static List<string> GetProcessableCorpsesProducts(
            GameObjectBlueprint CorpseBlueprint,
            string ProcessableType,
            string OnSuccessFieldName,
            Predicate<GameObjectBlueprint> ProductFilter = null)
        {
            Debug.GetIndents(out Indents indent);
            Debug.Log(
                Debug.GetCallingTypeAndMethod(true),
                CorpseBlueprint.Name + ", " + ProcessableType + ", " + OnSuccessFieldName,
                Indent: indent[1]);

            if (CorpseBlueprint == null
                || !CorpseBlueprint.TryGetPartParameter(ProcessableType, OnSuccessFieldName, out string processedProductValue))
            {
                Debug.SetIndent(indent[0]);
                return new();
            }

            List<string> outputList = new();
            Debug.Log(processedProductValue, indent[2]);
            if (!processedProductValue.StartsWith('@'))
            {
                if (processedProductValue.GetGameObjectBlueprint() is GameObjectBlueprint processedProductValueBlueprint
                    && (ProductFilter == null || ProductFilter(processedProductValueBlueprint)))
                {
                    Debug.Log(processedProductValue, indent[3]);
                    if (processedProductValueBlueprint.IsCorpse())
                    {
                        CorpseCorpseProductBlueprints[processedProductValue] = processedProductValueBlueprint;
                    }
                    outputList.Add(processedProductValue);
                }
            }
            else
            {
                string tableName = processedProductValue[1..];
                if (PopulationManager.GetEach(tableName) is List<string> populationBlueprints)
                {
                    foreach (string popBlueprint in populationBlueprints)
                    {
                        Debug.Log(popBlueprint, indent[3]);
                        if (popBlueprint.GetGameObjectBlueprint() is GameObjectBlueprint processedProductBlueprint
                            && (ProductFilter == null || ProductFilter(processedProductBlueprint)))
                        {
                            Debug.CheckYeh("Added", indent[4]);
                            if (processedProductBlueprint.IsCorpse())
                            {
                                CorpseCorpseProductBlueprints[popBlueprint] = processedProductBlueprint;
                            }
                            outputList.Add(popBlueprint);
                        }
                    }
                }
                /*
                if (PopulationManager.Populations.ContainsKey(tableName)
                    && PopulationManager.Populations[tableName] is PopulationInfo productsPopInfo)
                {
                    foreach (PopulationItem productPopItem in productsPopInfo.Items)
                    {
                        Debug.Log(productPopItem.Name, indent[3]);
                        if (productPopItem.GetBlueprintIfExists() is GameObjectBlueprint processedProductBlueprint
                            && (ProductFilter == null || ProductFilter(processedProductBlueprint)))
                        {
                            Debug.CheckYeh("Added", indent[4]);
                            if (processedProductBlueprint.IsCorpse())
                            {
                                CorpseCorpseProductBlueprints[productPopItem.Name] = processedProductBlueprint;
                            }
                            outputList.Add(productPopItem.Name);
                        }
                    }
                }
                */
            }
            Debug.SetIndent(indent[0]);
            return outputList;
        }
        public static List<KeyValuePair<string, List<string>>> GetProcessableCorpsesAndTheirProducts(bool ExcludeEmpty = true)
        {
            Debug.GetIndents(out Indents indent);
            Debug.Log(Debug.GetCallingTypeAndMethod(true), indent[1]);
            ProductsByProcessable ??= new();
            ProcessablesByProduct ??= new();
            List<KeyValuePair<string, List<string>>> output = new();
            foreach (GameObjectBlueprint corpseBlueprint in GetCorpseBlueprints(IsProcessableCorpse))
            {
                List<string> possibleProducts = new();
                if (ProductsByProcessable[corpseBlueprint.Name] is List<string> cachedProducts
                    && !cachedProducts.IsNullOrEmpty()
                    && (!ExcludeEmpty || !cachedProducts.All(s => s == CACHE_EMPTY)))
                {
                    output.Add(new(corpseBlueprint.Name, cachedProducts));
                    continue;
                }
                List<string> butcherableProducts = new(GetProcessableCorpsesProducts(
                    CorpseBlueprint: corpseBlueprint,
                    ProcessableType: nameof(Butcherable),
                    OnSuccessFieldName: nameof(Butcherable.OnSuccess)));

                if (!butcherableProducts.IsNullOrEmpty())
                {
                    Debug.Log(nameof(butcherableProducts), indent[2]);
                    foreach (string butcherableProduct in butcherableProducts)
                    {
                        Debug.Log(butcherableProduct, indent[3]);
                        possibleProducts.Add(butcherableProduct);
                    }
                }

                List<string> harvestableProducts = new(GetProcessableCorpsesProducts(
                    CorpseBlueprint: corpseBlueprint,
                    ProcessableType: nameof(Harvestable),
                    OnSuccessFieldName: nameof(Harvestable.OnSuccess)));

                if (!harvestableProducts.IsNullOrEmpty())
                {
                    Debug.Log(nameof(harvestableProducts), indent[2]);
                    foreach (string harvestableProduct in harvestableProducts)
                    {
                        Debug.Log(harvestableProduct, indent[3]);
                        possibleProducts.Add(harvestableProduct);
                    }
                }
                if (!possibleProducts.IsNullOrEmpty())
                {
                    Debug.Log(nameof(possibleProducts), possibleProducts.Count, indent[2]);
                }
                else
                {
                    possibleProducts.Add("empty");
                    Debug.Log("No " + nameof(possibleProducts), indent[2]);
                }
                foreach (string possibleProduct in possibleProducts)
                {
                    if (possibleProduct != CACHE_EMPTY)
                    {
                        CacheStringMapListString(ProcessablesByProduct, possibleProduct, corpseBlueprint.Name);
                    }
                }
                CacheStringMapListString(ProductsByProcessable, corpseBlueprint.Name, possibleProducts);
                output.Add(new(corpseBlueprint.Name, possibleProducts));
            }
            Debug.SetIndent(indent[0]);
            return output;
        }
        public static List<KeyValuePair<string, List<string>>> GetProcessableCorpsesAndTheirProducts(
            Predicate<GameObjectBlueprint> CorpseFilter,
            Predicate<GameObjectBlueprint> ProductFilter,
            bool ExcludeEmpty = true)
        {
            List<KeyValuePair<string, List<string>>> output = new();
            foreach ((string processableCorpseBlueprint, List<string> products) in GetProcessableCorpsesAndTheirProducts(ExcludeEmpty))
            {
                if (CorpseFilter != null && CorpseFilter(processableCorpseBlueprint.GetGameObjectBlueprint()))
                {
                    continue;
                }
                foreach (string product in products)
                {
                    if (ProductFilter != null && ProductFilter(product.GetGameObjectBlueprint()))
                    {
                        continue;
                    }
                    output.Add(new(processableCorpseBlueprint, products));
                }
            }
            return output;
        }

        public static List<string> GetCorpsesThisProductComesFrom(string ProductBlueprint, bool ExcludeEmpty = true)
        {
            Debug.GetIndents(out Indents indent);
            Debug.Log(Debug.GetCallingTypeAndMethod(true), indent[1]);
            if (ProcessablesByProduct[ProductBlueprint] is List<string> cachedProcessables
                && !cachedProcessables.IsNullOrEmpty()
                && (!ExcludeEmpty || !cachedProcessables.All(s => s == CACHE_EMPTY)))
            {
                return cachedProcessables;
            }
            List<string> corpseList = new();
            foreach ((string corpseBlueprint, List<string> productsList) in GetProcessableCorpsesAndTheirProducts(null, ProductFilter: IsCorpse, ExcludeEmpty))
            {
                if (productsList.Contains(ProductBlueprint))
                {
                    Debug.Log(nameof(corpseBlueprint), corpseBlueprint, indent[2]);
                    corpseList.Add(corpseBlueprint);
                }
            }
            if (corpseList.IsNullOrEmpty())
            {
                corpseList.Add("empty");
            }
            CacheStringMapListString(ProcessablesByProduct, ProductBlueprint, corpseList);
            Debug.SetIndent(indent[0]);
            return corpseList;
        }

        public static bool EntityCouldHaveComeFromThisCorpse(
            GameObjectBlueprint Entity,
            string CorpseBlueprint,
            out string EntityCorpseBlueprint,
            out int EntityCorpseChance,
            bool Include0Chance = true,
            bool ExcludeExcludedFromDynamicEnounter = true)
        {
            Debug.GetIndents(out Indents indent);
            Debug.Log(Debug.GetCallingTypeAndMethod(true) + "(" + nameof(Entity) + ": " + Entity.Name + ", " + CorpseBlueprint + ")", indent[1]);
            if (Entity.TryGetCorpseBlueprintAndChance(out EntityCorpseBlueprint, out EntityCorpseChance)
                && (EntityCorpseChance > 0 || Include0Chance))
            {
                if (EntitiesByCorpse[CorpseBlueprint] is List<string> cachedEntities
                    && cachedEntities.Contains(Entity.Name))
                {
                    return true;
                }
                if (Entity.IsBaseBlueprint() || (ExcludeExcludedFromDynamicEnounter && Entity.IsExcludedFromDynamicEncounters()))
                {
                    // We don't want base creatures or (optionally) ones excluded from dynamic encounters.
                    Debug.Log(AppendCross("") + " Entity Blueprint is Base or optionally excluded (ExcludeFromDynamic)", indent[2]);
                    Debug.SetIndent(indent[0]);
                    return true;
                }
                if (CorpseBlueprint == EntityCorpseBlueprint)
                {
                    // this entity has this corpse blueprint.
                    Debug.Log(AppendTick("") + " Same Blueprints", indent[2]);
                    Debug.SetIndent(indent[0]);
                    return true;
                }
                else
                if (CorpseBlueprint.InheritsFrom(EntityCorpseBlueprint))
                {
                    // this corpse blueprint inherits from this entity's corpse blueprint
                    Debug.Log(AppendTick("") + " This Corpse inherits Entity Corpse Blueprint", indent[2]);
                    Debug.SetIndent(indent[0]);
                    return true;
                }
                else
                if (CorpseBlueprint.IsBaseBlueprint() && EntityCorpseBlueprint.InheritsFrom(CorpseBlueprint))
                {
                    // this entity's corpse inherits from this base corpse blueprint
                    Debug.Log(AppendTick("") + " This Base Corpse is inherited by Entity Corpse", indent[2]);
                    Debug.SetIndent(indent[0]);
                    return true;
                }
                else
                if (GetCorpsesThisProductComesFrom(CorpseBlueprint) is List<string> productCorpseList)
                {
                    foreach (string productCorpse in productCorpseList)
                    {
                        if (EntityCouldHaveComeFromThisCorpse(
                            Entity,
                            productCorpse,
                            out EntityCorpseBlueprint,
                            out EntityCorpseChance,
                            Include0Chance,
                            ExcludeExcludedFromDynamicEnounter))
                        {
                            // this entity could have come from one of the corpses thie corpse product can be processed from
                            Debug.Log(AppendTick("") + " Entity could have come from this Corpse Product's Corpse", indent[2]);
                            Debug.SetIndent(indent[0]);
                            return true;
                        }
                    }
                }
            }
            Debug.Log(AppendCross("") + " No Match", indent[2]);
            Debug.SetIndent(indent[0]);
            return false;
        }

        public static List<BlueprintWeightPair> GetBlueprintsWhoseCorpseThisCouldBe(
            string CorpseBlueprint,
            bool Include0Chance = true,
            bool ExcludeExcludedFromDynamicEnounter = true,
            Predicate<GameObjectBlueprint> Filter = null)
        {
            Debug.GetIndents(out Indents indent);
            Debug.Log(Debug.GetCallingTypeAndMethod(true), CorpseBlueprint, indent[1]);
            List<BlueprintWeightPair> blueprintsWeightedList = new();
            if (CorpseBlueprint.IsNullOrEmpty() || !CorpseBlueprint.IsCorpse())
            {
                // if there's no blueprint to test or the corpse being tested is not a corpse then return.
                if (!EntitiesByCorpse[CorpseBlueprint].IsNullOrEmpty())
                {
                    EntitiesByCorpse[CorpseBlueprint] = new();
                }
                if (!EntityWithWeightByCorpse[CorpseBlueprint].IsNullOrEmpty())
                {
                    EntityWithWeightByCorpse[CorpseBlueprint] = new();
                }
                Debug.Log(AppendCross("") + " No Corpse Blueprint or Corpse Blueprint Not Corpse", indent[2]);
                Debug.SetIndent(indent[0]);
                return blueprintsWeightedList;
            }
            if (EntityWithWeightByCorpse[CorpseBlueprint] is List<BlueprintWeightPair> cachedEntityWeightList
                && !cachedEntityWeightList.IsNullOrEmpty())
            {
                if (Filter == null)
                {
                    return cachedEntityWeightList;
                }
                List<BlueprintWeightPair> filteredList = new();
                foreach (BlueprintWeightPair blueprintWeightPair in cachedEntityWeightList)
                {
                    if (Filter((GameObjectBlueprint)blueprintWeightPair))
                    {
                        filteredList.Add(blueprintWeightPair);
                    }
                }
                Debug.Log(AppendTick("") + " Got Cached Value!", indent[2]);
                Debug.SetIndent(indent[0]);
                return filteredList;
            }
            List<string> processableOriginCorpses = GetCorpsesThisProductComesFrom(CorpseBlueprint);

            bool excludeExcludedFromDynamicEncounters(GameObjectBlueprint bp)
            {
                if (bp.HasTagOrProperty(IGNORE_EXCLUDE_TAG))
                {
                    return true;
                }
                return !ExcludeExcludedFromDynamicEnounter
                    || !bp.IsExcludedFromDynamicEncounters();
            }
            foreach (GameObjectBlueprint entityBlueprint in GetEntitiesWithCorpseBlueprints(excludeExcludedFromDynamicEncounters))
            {
                if (Filter != null && !Filter(entityBlueprint))
                {
                    continue;
                }
                if (!processableOriginCorpses.IsNullOrEmpty())
                {
                    Debug.Log("Corpse Blueprint is processing product...", indent[2]);
                    foreach (string processableOriginCorpse in processableOriginCorpses)
                    {
                        Debug.Log(processableOriginCorpse, indent[3]);
                        if (EntityCouldHaveComeFromThisCorpse(
                            Entity: entityBlueprint,
                            CorpseBlueprint: processableOriginCorpse,
                            EntityCorpseBlueprint: out _,
                            EntityCorpseChance: out int processableOriginCorpseChance,
                            Include0Chance: Include0Chance,
                            ExcludeExcludedFromDynamicEnounter: ExcludeExcludedFromDynamicEnounter))
                        {
                            // add this entity for any corpse matches for corpses that can be processed into this corpse.
                            // IE, if the corpse blueprint is an "Ogre Ape Heart", then
                            //     it comes from an "Ogre Ape Corpse", therefore 
                            //     "Ogre Ape" should be a possible outcome.
                            blueprintsWeightedList.TryAdd(new(entityBlueprint.Name, processableOriginCorpseChance));
                        }
                    }
                }
                else
                if (EntityCouldHaveComeFromThisCorpse(
                    Entity: entityBlueprint,
                    CorpseBlueprint: CorpseBlueprint,
                    EntityCorpseBlueprint: out _,
                    EntityCorpseChance: out int corpseChance,
                    Include0Chance: Include0Chance,
                    ExcludeExcludedFromDynamicEnounter: ExcludeExcludedFromDynamicEnounter))
                {
                    // add this entity if their corpse matches the one being tested.
                    blueprintsWeightedList.TryAdd(new(entityBlueprint.Name, corpseChance));
                }
            }
            if (blueprintsWeightedList.IsNullOrEmpty()
                && CorpseBlueprint.GetGameObjectBlueprint().Inherits is string blueprintInherits
                && (blueprintInherits.InheritsFrom("Corpse") || blueprintInherits == "Corpse"))
            {
                // if we got an empty list, then we can broaden the search.
                // run the search again, but for the corpse that the tested one inherits from
                // if it indeed does inherit from a corpse.
                blueprintsWeightedList = GetBlueprintsWhoseCorpseThisCouldBe(blueprintInherits, Include0Chance, ExcludeExcludedFromDynamicEnounter);
            }
            foreach (string blueprint in blueprintsWeightedList.Select(v => (string)v))
            {
                CacheEntitesByCorpse(CorpseBlueprint, blueprint);
            }
            CacheEntityWithWeightByCorpse(CorpseBlueprint, blueprintsWeightedList);
            Debug.SetIndent(indent[0]);
            return blueprintsWeightedList;
        }

        public static string GetALivingBlueprintForCorpseWeighted(string CorpseBlueprint, bool Include0Chance = true, bool GuaranteeBlueprint = true)
        {
            Debug.GetIndents(out Indents indent);
            Dictionary<string, int> weightedBlueprints = GetBlueprintsWhoseCorpseThisCouldBe(CorpseBlueprint, Filter: bp => !bp.IsBaseBlueprint()).ConvertToWeightedList();
            List<string> blueprints = new(weightedBlueprints.Keys);
            int maxWeight = 0;
            foreach (string blueprint in blueprints)
            {
                if (Include0Chance && weightedBlueprints[blueprint] == 0)
                {
                    weightedBlueprints[blueprint]++;
                }
                maxWeight += weightedBlueprints[blueprint];
            }
            int cumulativeWeight = 0;
            int rolledAmount = Stat.RandomCosmetic(0, maxWeight - 1);

            Debug.Log(Debug.GetCallingTypeAndMethod(true) + "(" + CorpseBlueprint + ", " + rolledAmount + "/" + maxWeight + ")", indent[1]);
            foreach ((string blueprint, int weight) in weightedBlueprints)
            {
                if (weight < 1)
                {
                    continue;
                }
                cumulativeWeight += weight;
                if (rolledAmount < cumulativeWeight)
                {
                    return blueprint;
                }
            }

            if (!Include0Chance && GuaranteeBlueprint)
            {
                return GetALivingBlueprintForCorpseWeighted(CorpseBlueprint, true, false);
            }

            Debug.SetIndent(indent[0]);
            return null;
        }
        public static string GetALivingBlueprintForCorpse(string CorpseBlueprint, bool Include0Chance = true)
        {
            return GetBlueprintsWhoseCorpseThisCouldBe(CorpseBlueprint, Include0Chance: Include0Chance)
                ?.ConvertToWeightedList()
                ?.Keys
                ?.GetRandomElementCosmetic();
        }

        public UD_FleshGolems_PastLife Initialize(GameObject PastLife = null)
        {
            Debug.LogHeader(nameof(PastLife) + ": " + (PastLife?.DebugName ?? NULL), out Indents indent);
            if (!Init)
            {
                bool obliteratePastLife = false;
                try
                {
                    BrainInAJar ??= GetNewBrainInAJar();
                    if (BrainInAJar != null)
                    {
                        Blueprint ??= ParentObject?.GetPropertyOrTag(PASTLIFE_BLUEPRINT_PROPTAG)
                            ?? ParentObject?.GetPropertyOrTag("SourceObject")
                            ?? PastLife?.Blueprint
                            ?? GetALivingBlueprintForCorpseWeighted(ParentObject.Blueprint);

                        Debug.Log(nameof(Blueprint), Blueprint ?? NULL, indent[1]);

                        obliteratePastLife = PastLife == null;
                        PastLife ??= GameObject.CreateSample(Blueprint)
                            ?? GameObject.CreateSample(EncountersAPI.GetACreatureBlueprintModel(bp => bp.TryGetCorpseChance(out int bpCorpseChance) && bpCorpseChance > 0).Name)
                            ?? GameObject.CreateSample("Trash Monk");

                        Blueprint ??= PastLife?.Blueprint;

                        Debug.Log(nameof(PastLife), PastLife?.DebugName ?? NULL, indent[1]);

                        if (PastLife.TryGetPart(out UD_FleshGolems_PastLife prevPastLife)
                            && prevPastLife.DeepCopy(BrainInAJar, DeepCopyMapInventory) is UD_FleshGolems_PastLife prevPastLifeCopy)
                        {
                            BrainInAJar.AddPart(prevPastLifeCopy);
                        }

                        if (PastLife.IsPlayer())
                        {
                            BrainInAJar.SetStringProperty("UD_FleshGolems_WasPlayer", "Yep, I used to be the player!");
                        }

                        if (PastLife.GetBlueprint().InheritsFrom("UD_FleshGolems Brain In A Jar Widget")
                            || PastLife.GetBlueprint().InheritsFrom("Corpse"))
                        {
                            TimesReanimated++;
                        }

                        BrainInAJar.HasProperName = PastLife.HasProperName || PastLife.GetBlueprint().GetxTag("Grammar", "Proper").EqualsNoCase("true");

                        BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Titles>());
                        BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Epithets>());
                        BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Honorifics>());

                        BrainInAJar._Property = PastLife._Property;
                        BrainInAJar._IntProperty = PastLife._IntProperty;

                        Render bIAJ_Render = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.Render);
                        BrainInAJar.Render = bIAJ_Render;

                        Description bIAJ_Description = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Description>());

                        if (PastLife.CurrentCell is Cell deathCell
                            && deathCell.ParentZone is Zone deathZone)
                        {
                            DeathAddress = new(deathZone.ZoneID, deathCell.Location);
                        }

                        Physics bIAJ_Physics = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.Physics);
                        BrainInAJar.Physics = bIAJ_Physics;

                        Brain bIAJ_Brain = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.Brain);
                        BrainInAJar.Brain = bIAJ_Brain;
                        try
                        {
                            foreach ((int flags, PartyMember partyMember) in bIAJ_Brain.PartyMembers)
                            {
                                PartyMember partyMemberCopy = new(partyMember.Reference, partyMember.Flags);
                                Brain.PartyMembers.TryAdd(flags, partyMemberCopy);
                            }
                        }
                        catch (Exception x)
                        {
                            MetricsManager.LogException(Name + "." + nameof(Initialize), x, "game_mod_exception");
                            Brain.PartyMembers = new();
                        }
                        try
                        {
                            foreach ((int key, OpinionList opinionList) in bIAJ_Brain.Opinions)
                            {
                                OpinionList opinionsCopy = new();
                                foreach (IOpinion opinionCopy in opinionList)
                                {
                                    opinionsCopy.Add(opinionCopy);
                                }
                                Brain.Opinions.TryAdd(key, opinionsCopy);
                            }
                        }
                        catch (Exception x)
                        {
                            MetricsManager.LogException(Name + "." + nameof(Initialize), x, "game_mod_exception");
                            Brain.Opinions = new();
                        }

                        Body bIAJ_Body = BrainInAJar.RequirePart<Body>();
                        BrainInAJar.Body = bIAJ_Body;
                        Anatomies.GetAnatomy(PastLife?.Body?.Anatomy ?? "Humanoid")?.ApplyTo(bIAJ_Body);
                        RoughlyCopyAdditionalLimbs(bIAJ_Body, PastLife?.Body);

                        BrainInAJar.SetBleedLiquid(PastLife.GetBleedLiquid());

                        Corpse bIAJ_Corpse = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Corpse>());
                        bIAJ_Corpse.CorpseBlueprint = ParentObject.Blueprint;
                        if (bIAJ_Corpse.CorpseBlueprint.IsNullOrEmpty())
                        {
                            if (Blueprint
                                .GetGameObjectBlueprint()
                                .TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseBlueprint), out string corpseBlueprint))
                            {
                                bIAJ_Corpse.CorpseBlueprint = corpseBlueprint;
                            }
                            else
                            if ((PastLife.GetSpecies() + " Corpse").GetGameObjectBlueprint() is GameObjectBlueprint corpseGameObjectBlueprint
                                && corpseGameObjectBlueprint
                                    .TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseBlueprint), out string speciesCorpseBlueprint))
                            {
                                bIAJ_Corpse.CorpseBlueprint = speciesCorpseBlueprint;
                            }
                        }
                        bIAJ_Corpse.CorpseChance = 100;

                        if (!PastLife.GenderName.IsNullOrEmpty())
                        {
                            BrainInAJar.SetGender(PastLife.GenderName);
                        }

                        if (!PastLife.PronounSetName.IsNullOrEmpty())
                        {
                            BrainInAJar.SetPronounSet(PastLife.PronounSetName);
                        }

                        ConversationScript bIAJ_Conversation = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<ConversationScript>());

                        if (PastLife.Statistics.IsNullOrEmpty())
                        {
                            BrainInAJar.Statistics = new();
                            foreach ((string statName, Statistic stat) in PastLife.Statistics)
                            {
                                Statistic newStat = new(stat);
                                if (statName == "Hitpoints")
                                {
                                    newStat.Penalty = 0;
                                }
                                newStat.Owner = BrainInAJar;
                                BrainInAJar.Statistics.Add(statName, newStat);
                            }
                        }

                        BrainInAJar.SetSpecies(PastLife.GetSpecies());
                        BrainInAJar.SetGenotype(PastLife.GetGenotype());
                        BrainInAJar.SetSubtype(PastLife.GetSubtype());

                        Mutations bIAJ_Mutations = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Mutations>());
                        Skills bIAJ_Skills = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Skills>());
                        foreach (BaseSkill baseSkill in PastLife.GetPartsDescendedFrom<BaseSkill>())
                        {
                            // There's a bug in v1.04 with how Skills serializes its BaseSkills
                            // that means the only way to guarantee copying them is via the parts list.
                            if (!bIAJ_Skills.SkillList.Contains(baseSkill))
                            {
                                bIAJ_Skills.AddSkill(baseSkill);
                            }
                        }

                        foreach (Effect pastEffect in PastLife.Effects)
                        {
                            BrainInAJar.Effects.Add(pastEffect.DeepCopy(BrainInAJar, null));
                        }

                        if (PastLife?.Body is Body pastBody
                            && pastBody.GetInstalledCyberneticsReadonly() is List<GameObject> installedCybernetics
                            && InstalledCybernetics.IsNullOrEmpty())
                        {
                            foreach (GameObject installedCybernetic in installedCybernetics)
                            {
                                if (installedCybernetic?.Implantee?.Body is Body implanteeBody)
                                {
                                    InstalledCybernetics.Add(new(installedCybernetic, implanteeBody));
                                }
                            }
                        }
                    }
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(Name + "." + nameof(Initialize), x, "game_mod_exception");
                }
                finally
                {
                    Init = true;
                    if (obliteratePastLife)
                    {
                        PastLife?.Obliterate();
                    }
                    Debug.SetIndent(indent[0]);
                }
            }
            Debug.SetIndent(indent[0]);
            return this;
        }

        public UD_FleshGolems_PastLife Initialize(UD_FleshGolems_PastLife PrevPastLife)
        {
            if (!Init && PrevPastLife != null && PrevPastLife.Init)
            {
                Initialize(PrevPastLife.BrainInAJar);
                TimesReanimated++;
            }
            else
            if (PrevPastLife?.ParentObject is GameObject pastLife)
            {
                Initialize(pastLife);
            }
            return this;
        }

        public override void Attach()
        {
            base.Attach();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public GameObjectBlueprint GetBlueprint()
        {
            return Blueprint.GetGameObjectBlueprint();
        }

        public static bool RestoreBrain(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            bool ExcludedFromDynamicEncounters,
            out Brain FrankenBrain)
        {
            FrankenBrain = null;
            if (FrankenCorpse == null || PastLife == null)
            {
                return false;
            }
            FrankenBrain = FrankenCorpse.Brain;
            if (FrankenBrain != null
                && PastLife?.Brain is Brain pastBrain)
            {
                FrankenCorpse.Brain.Allegiance ??= new();
                FrankenBrain.Allegiance.Hostile = pastBrain.Allegiance.Hostile;
                FrankenBrain.Allegiance.Calm = pastBrain.Allegiance.Calm;
                if ((!UD_FleshGolems_Reanimated.HasWorldGenerated || ExcludedFromDynamicEncounters))
                {
                    FrankenCorpse.Brain.Allegiance.Clear();
                    FrankenCorpse.Brain.Allegiance.Add("Newly Sentient Beings", 75);
                    foreach ((string faction, int rep) in pastBrain.Allegiance)
                    {
                        if (!pastBrain.Allegiance.ContainsKey(faction))
                        {
                            FrankenCorpse.Brain.Allegiance.Add(faction, rep);
                        }
                        else
                        {
                            FrankenCorpse.Brain.Allegiance[faction] += rep;
                        }
                    }
                    if (!FrankenCorpse.HasPropertyOrTag("StartingPet") && !FrankenCorpse.HasPropertyOrTag("Pet"))
                    {
                        FrankenCorpse.Brain.PartyLeader = pastBrain.PartyLeader;
                        FrankenCorpse.Brain.PartyMembers = pastBrain.PartyMembers;

                        FrankenCorpse.Brain.Opinions = pastBrain.Opinions;

                    }
                }
                FrankenBrain.Wanders = pastBrain.Wanders;
                FrankenBrain.WallWalker = pastBrain.WallWalker;
                FrankenBrain.HostileWalkRadius = pastBrain.HostileWalkRadius;

                FrankenBrain.Mobile = pastBrain.Mobile;
            }
            return true;
        }
        public bool RestoreBrain(
            bool ExcludedFromDynamicEncounters,
            out Brain FrankenBrain)
        {
            return RestoreBrain(ParentObject, this, ExcludedFromDynamicEncounters, out FrankenBrain);
        }

        public static string GenerateDisplayName(UD_FleshGolems_PastLife PastLife)
        {
            if (PastLife == null)
            {
                return null;
            }
            string oldIdentity = PastLife.RefName;
            string newIdentity;
            if (PastLife.WasProperlyNamed)
            {
                newIdentity = "corpse of " + oldIdentity;
            }
            else
            {
                newIdentity = oldIdentity + " corpse";
            }
            return newIdentity;
        }
        public string GenerateDisplayName()
        {
            return GenerateDisplayName(this);
        }

        public static string GenerateDescription(UD_FleshGolems_PastLife PastLife)
        {
            if (PastLife == null || PastLife.Description.IsNullOrEmpty())
            {
                return null;
            }
            string oldDescription = PastLife.Description;
            string postDescription = "In life this =subject.uD_xTag:TextFragments:CorpseDescription= was ";
            if (PastLife.WasPlayer)
            {
                postDescription += "you.";
            }
            else
            {
                string whoTheyWere = (PastLife?.RefName ?? PastLife.GetBlueprint().DisplayName());
                if (!PastLife.WasProperlyNamed)
                {
                    whoTheyWere = Grammar.A(whoTheyWere);
                }
                string postDescriptionEnd = PastLife.WasCorpse ? "." : ":\n" + oldDescription;
                postDescription += whoTheyWere + postDescriptionEnd;
            }
            return postDescription;
        }
        public string GenerateDescription()
        {
            return GenerateDescription(this);
        }

        public static bool RestoreGenderIdentity(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            bool WantOldIdentity = false)
        {
            if (FrankenCorpse == null || PastLife == null || !WantOldIdentity)
            {
                return false;
            }
            if (PastLife.Gender?.Name is string pastGenderName)
            {
                FrankenCorpse.SetGender(pastGenderName);
            }
            if (PastLife.PronounSet?.Name is string pastPronounSetName)
            {
                FrankenCorpse.SetPronounSet(pastPronounSetName);
            }
            return true;
        }
        public bool RestoreGenderIdentity(bool WantOldIdentity = true)
        {
            return RestoreGenderIdentity(ParentObject, this, WantOldIdentity);
        }

        public static bool RestoreAnatomy(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out Body FrankenBody)
        {
            FrankenBody = null;
            if (FrankenCorpse == null || PastLife == null || PastLife.Body == null)
            {
                return false;
            }
            if (PastLife.Body is Body pastBody
                && Anatomies.GetAnatomy(pastBody.Anatomy) is Anatomy.Anatomy anatomy)
            {
                if (FrankenCorpse.Body == null)
                {
                    FrankenCorpse.AddPart(new Body()).Anatomy = anatomy.Name;
                }
                else
                {
                    FrankenCorpse.Body.Rebuild(anatomy.Name);
                }
            }
            FrankenBody = FrankenCorpse.Body;
            return true;
        }
        public bool RestoreAnatomy(
            out Body FrankenBody)
        {
            return RestoreAnatomy(ParentObject, this, out FrankenBody);
        }

        public static bool IsIntrinsicAndNative(BodyPart BodyPart)
        {
            return BodyPart.Native
                && !BodyPart.Extrinsic;
        }
        private static bool RoughlyCopyAdditionalLimbs(
            Body DestinationBody,
            Body SourceBody)
        {
            if (DestinationBody == null || SourceBody.ParentObject == null || SourceBody == null)
            {
                return false;
            }

            foreach (BodyPart bodyPart in SourceBody?.LoopParts())
            {
                if (bodyPart.Native
                    || bodyPart.Extrinsic
                    || !bodyPart.ParentPart.Native
                    || bodyPart?.ParentPart is not BodyPart parentPart
                    || parentPart.Type is not string parentPartType
                    || DestinationBody?.GetPart(parentPartType) is not List<BodyPart> destinationBodyPartsOfType
                    || destinationBodyPartsOfType.GetRandomElementCosmetic(IsIntrinsicAndNative) is not BodyPart targetBodyPart)
                {
                    continue;
                }
                targetBodyPart.AddPart(bodyPart?.DeepCopy(DestinationBody?.ParentObject, DestinationBody, null, DeepCopyMapInventory));
            }
            return true;
        }
        public static bool RestoreAdditionalLimbs(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out Body FrankenBody)
        {
            FrankenBody = null;
            if (FrankenCorpse == null || PastLife == null || PastLife.Body == null)
            {
                return false;
            }
            FrankenBody = FrankenCorpse.Body;
            return RoughlyCopyAdditionalLimbs(FrankenBody, PastLife.Body);
        }
        public bool RestoreAdditionalLimbs(
            out Body FrankenBody)
        {
            return RestoreAdditionalLimbs(ParentObject, this, out FrankenBody);
        }
        public bool RestoreAdditionalLimbs()
        {
            return RestoreAdditionalLimbs(out _);
        }

        public static bool RestoreTaxonomy(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife)
        {
            if (FrankenCorpse == null || PastLife == null)
            {
                return false;
            }
            if (PastLife.Species is string pastSpecies)
            {
                FrankenCorpse.SetSpecies(pastSpecies);
            }
            if (PastLife.Genotype is string pastGenotype)
            {
                FrankenCorpse.SetGenotype(pastGenotype);
            }
            if (PastLife.Subtype is string pastSubtype)
            {
                FrankenCorpse.SetSubtype(pastSubtype);
            }
            return true;
        }
        public bool RestoreTaxonomy()
        {
            return RestoreTaxonomy(ParentObject, this);
        }

        public static bool RestoreMutations(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out Mutations FrankenMutations,
            Predicate<BaseMutation> Exclude = null)
        {
            FrankenMutations = null;
            bool any = false;
            if (FrankenCorpse == null
                || PastLife == null
                || PastLife.Mutations == null
                || PastLife.Mutations.ActiveMutationList.IsNullOrEmpty())
            {
                return any;
            }
            FrankenMutations = FrankenCorpse.RequirePart<Mutations>();
            foreach (BaseMutation baseMutation in PastLife.Mutations.MutationList)
            {
                BaseMutation baseMutationToAdd = baseMutation.DeepCopy(FrankenCorpse, null) as BaseMutation;
                bool alreadyHaveMutation = FrankenMutations.HasMutation(baseMutation.Name);
                if (alreadyHaveMutation)
                {
                    baseMutationToAdd = FrankenMutations.GetMutation(baseMutation.Name);
                }
                if (Exclude != null && Exclude(baseMutationToAdd))
                {
                    continue;
                }
                if (baseMutationToAdd.CapOverride == -1)
                {
                    baseMutationToAdd.CapOverride = baseMutation.Level;
                }
                if (!alreadyHaveMutation)
                {
                    FrankenMutations.AddMutation(baseMutationToAdd, baseMutation.Level);
                }
                else
                {
                    baseMutationToAdd.BaseLevel += baseMutation.Level;
                }
                FrankenMutations.AddMutation(baseMutationToAdd, baseMutation.BaseLevel);
                any = true;
            }
            return any;
        }
        public bool RestoreMutations(
            out Mutations FrankenMutations,
            Predicate<BaseMutation> Exclude = null)
        {
            return RestoreMutations(ParentObject, this, out FrankenMutations, Exclude);
        }

        public static bool RestoreSkills(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out Skills FrankenSkills,
            Predicate<BaseSkill> Exclude = null)
        {
            FrankenSkills = null;
            bool any = false;
            if (FrankenCorpse == null
                || PastLife == null
                || PastLife.Skills == null
                || PastLife.Skills.SkillList.IsNullOrEmpty())
            {
                return any;
            }
            FrankenSkills = FrankenCorpse.RequirePart<Skills>();
            foreach (BaseSkill baseSkill in PastLife.Skills.SkillList)
            {
                if ((Exclude != null && Exclude(baseSkill))
                    || FrankenCorpse.HasSkill(baseSkill.Name)
                    || FrankenCorpse.HasPart(baseSkill.Name))
                {
                    continue;
                }
                any = FrankenSkills.AddSkill(baseSkill.DeepCopy(FrankenCorpse, null) as BaseSkill) || any;
            }
            return any;
        }

        public bool RestoreSkills(
            out Skills FrankenSkills,
            Predicate<BaseSkill> Exclude = null)
        {
            return RestoreSkills(ParentObject, this, out FrankenSkills, Exclude);
        }

        public static bool RestoreCybernetics(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out bool WereCyberneticsInstalled)
        {
            WereCyberneticsInstalled = false;
            if (FrankenCorpse == null || PastLife == null)
            {
                return false;
            }
            if (!PastLife.InstalledCybernetics.IsNullOrEmpty())
            {
                Body frankenBody = FrankenCorpse.Body;
                foreach ((GameObject cybernetic, string bodyPartType) in PastLife.InstalledCybernetics)
                {
                    if (frankenBody.FindCybernetics(cybernetic) != null)
                    {
                        continue;
                    }
                    if (cybernetic.DeepCopy() is GameObject newCybernetic
                        && newCybernetic.TryRemoveFromContext())
                    {
                        if (newCybernetic.TryGetPart(out CyberneticsBaseItem cyberneticBasePart))
                        {
                            int cyberneticsCost = cyberneticBasePart.Cost;
                            FrankenCorpse.ModIntProperty(CYBERNETICS_LICENSES, cyberneticsCost);
                            FrankenCorpse.ModIntProperty(CYBERNETICS_LICENSES_FREE, cyberneticsCost);

                            List<BodyPart> bodyParts = frankenBody.GetPart(bodyPartType);
                            bodyParts.ShuffleInPlace();

                            foreach (BodyPart bodyPart in bodyParts)
                            {
                                if (bodyPart.CanReceiveCyberneticImplant()
                                    && !bodyPart.HasInstalledCybernetics())
                                {
                                    bodyPart.Implant(newCybernetic);
                                    WereCyberneticsInstalled = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }
        public bool RestoreCybernetics(out bool WereCyberneticsInstalled)
        {
            return RestoreCybernetics(ParentObject, this, out WereCyberneticsInstalled);
        }

        /*
         * 
         * Wishes!
         * 
         */

        public virtual void DebugOutput()
        {
            Debug.ResetIndent(out Indents indent);
            try
            {
                Debug.Log(nameof(UD_FleshGolems_PastLife), ParentObject.DebugName, indent[0]);

                Debug.Log(nameof(Init), Init, indent[1]);
                Debug.Log(nameof(WasCorpse), WasCorpse, indent);
                Debug.Log(nameof(WasPlayer), WasPlayer, indent);

                Debug.Log(nameof(TimesReanimated), TimesReanimated, indent);

                Debug.Log(nameof(Blueprint), Blueprint, indent);
                Debug.Log(nameof(BaseDisplayName), BaseDisplayName, indent);

                Debug.Log(nameof(Titles), Titles, indent);
                Debug.Log(nameof(Epithets), Epithets, indent);
                Debug.Log(nameof(Honorifics), Honorifics, indent);

                Debug.Log(nameof(PastRender), PastRender, indent);
                Debug.Log(nameof(Description), Description, indent);

                Debug.Log(nameof(DeathAddress), DeathAddress, indent);

                Debug.Log(nameof(Brain), Brain != null, indent);
                Debug.Log(nameof(Brain.Allegiance), indent[2]);
                foreach ((string faction, int rep) in Brain?.Allegiance ?? new())
                {
                    Debug.Log(faction, rep, indent[3]);
                }
                if (Brain != null)
                {
                    Debug.Log("bools", indent[2]);
                    Traverse brainWalk = new(Brain);
                    foreach (string field in brainWalk.Fields() ?? new())
                    {
                        string fieldValue = brainWalk?.Field(field)?.GetValue()?.ToString();
                        Debug.Log(field, fieldValue ?? "??", indent[3]);
                    }
                }
                Debug.Log(nameof(Gender), Gender, indent[1]);
                Debug.Log(nameof(PronounSet), PronounSet, indent);
                Debug.Log(nameof(ConversationScriptID), ConversationScriptID, indent);

                Debug.Log(nameof(Stats), Stats?.Count, indent);
                foreach ((string statName, Statistic stat) in Stats ?? new())
                {
                    Debug.Log(statName, stat.BaseValue, indent[2]);
                }
                Debug.Log(nameof(Species), Species, indent[1]);
                Debug.Log(nameof(Genotype), Genotype, indent);
                Debug.Log(nameof(Subtype), Subtype, indent);

                Debug.Log(nameof(Mutations), Mutations?.ActiveMutationList?.Count, indent);
                foreach (BaseMutation mutation in Mutations?.ActiveMutationList)
                {
                    Debug.Log(mutation.Name, mutation.BaseLevel, indent[2]);
                }
                Debug.Log(nameof(Skills), Skills?.SkillList?.Count, indent[1]);
                foreach (BaseSkill baseSkill in Skills?.SkillList)
                {
                    Debug.Log(baseSkill.Name, indent[2]);
                }
                Debug.Log(nameof(InstalledCybernetics), new List<UD_FleshGolems_InstalledCybernetic>(InstalledCybernetics)?.Count, indent[1]);
                foreach ((GameObject cybernetic, string implantedLimb) in InstalledCybernetics)
                {
                    Debug.Log(implantedLimb, cybernetic.Blueprint, indent[2]);
                }

                Debug.Log(nameof(Tags), Tags?.Count, indent[1]);
                foreach ((string name, string value) in Tags ?? new())
                {
                    Debug.Log(name, value, indent[2]);
                }
                Debug.Log(nameof(StringProperties), StringProperties?.Count, indent[1]);
                foreach ((string name, string value) in StringProperties ?? new())
                {
                    Debug.Log(name, value, indent[2]);
                }
                Debug.Log(nameof(IntProperties), IntProperties?.Count, indent[1]);
                foreach ((string name, int value) in IntProperties ?? new())
                {
                    Debug.Log(name, value, indent[2]);
                }

                Debug.Log(nameof(Effects), Effects?.Count, indent[1]);
                foreach (Effect Effect in Effects)
                {
                    Debug.Log(Effect.ClassName + ",  duration", Effect.Duration, indent[2]);
                }
            }
            catch (Exception x)
            {
                MetricsManager.LogException(Name + "." + nameof(DebugOutput), x, "game_mod_exception");
            }
            finally
            {
                Debug.SetIndent(indent[0]);
            }
        }

        [WishCommand("UD_FleshGolems debug PastLife")]
        public static void Debug_PastLife_WishHandler()
        {
            int startX = 40;
            int startY = 12;
            if (The.Player.CurrentCell is Cell playerCell)
            {
                startX = playerCell.X;
                startY = playerCell.Y;
            }
            if (PickTarget.ShowPicker(
                Style: PickTarget.PickStyle.EmptyCell,
                StartX: startX,
                StartY: startY,
                VisLevel: AllowVis.Any,
                ObjectTest: GO => GO.HasPart<UD_FleshGolems_PastLife>(),
                Label: "debug " + nameof(UD_FleshGolems_PastLife)) is Cell pickCell
                && Popup.PickGameObject(
                    Title: "pick a thing with a past life",
                    Objects: pickCell.GetObjectsWithPart(nameof(UD_FleshGolems_PastLife)),
                    AllowEscape: true,
                    ShortDisplayNames: true) is GameObject pickedObject)
            {
                pickedObject?.GetPart<UD_FleshGolems_PastLife>().DebugOutput();
                Popup.Show(
                    "debug output for " + Grammar.MakePossessive(pickedObject.ShortDisplayNameSingleStripped) + " " +
                    nameof(UD_FleshGolems_PastLife));
            }
            else
            {
                Popup.Show("nothing selected to debug " + nameof(UD_FleshGolems_PastLife));
            }
        }

        [WishCommand("UD_FleshGolems wot creature")]
        public static void Debug_WotDis_WishHandler()
        {
            int startX = 40;
            int startY = 12;
            if (The.Player.CurrentCell is Cell playerCell)
            {
                startX = playerCell.X;
                startY = playerCell.Y;
            }
            if (PickTarget.ShowPicker(
                Style: PickTarget.PickStyle.EmptyCell,
                StartX: startX,
                StartY: startY,
                VisLevel: AllowVis.Any,
                ObjectTest: IsReanimatableCorpse,
                Label: "pick a corpse to get a list of possible creatrues") is Cell pickCell)
            {
                List<GameObject> corpseList = pickCell.GetObjectsViaEventList(Filter: IsReanimatableCorpse);
                GameObject targetCorpse = null;
                if (corpseList.Count == 1)
                {
                    targetCorpse = corpseList[0];
                }
                if (targetCorpse == null
                    && Popup.PickGameObject(
                        Title: "which corpse?",
                        Objects: corpseList,
                        AllowEscape: true,
                        ShortDisplayNames: true) is GameObject pickedObject)
                {
                    targetCorpse = pickedObject;
                }
                if (targetCorpse != null)
                {
                    List<string> possibleBlueprints = new(GetBlueprintsWhoseCorpseThisCouldBe(targetCorpse.Blueprint).ConvertToWeightedList().Keys);

                    string corpseListLabel = 
                        targetCorpse.IndicativeProximal + " " + targetCorpse.GetReferenceDisplayName(Short: true) + 
                        " might have been any of the following:";

                    string corpseListOutput = possibleBlueprints.GenerateBulletList(Label: corpseListLabel);
                    Popup.Show(corpseListOutput);
                }
                return;
            }
            Popup.Show("no corpse selected to get a creature list from");
        }

        [WishCommand("UD_FleshGolems gimme caches")]
        public static void Debug_GimmeCache_WishHandler()
        {
            string output = "PastLife Corpse Caches\n";
            output += nameof(CorpseBlueprints) + "\n";
            List<string> corpseBlueprints = new();
            foreach ((string corpseBlueprint, GameObjectBlueprint _) in CorpseBlueprints)
            {
                corpseBlueprints.Add(corpseBlueprint);
            }
            if (corpseBlueprints.IsNullOrEmpty())
            {
                corpseBlueprints ??= new()
                {
                    "empty",
                };
            }
            output += corpseBlueprints.GenerateBulletList() + "\n\n";

            output += nameof(EntitiesWithCorpseBlueprints) + "\n";
            List<string> entitiesWithCorpseBlueprints = new();
            foreach ((string entityBlueprint, GameObjectBlueprint _) in EntitiesWithCorpseBlueprints)
            {
                entitiesWithCorpseBlueprints.Add(entityBlueprint);
            }
            if (entitiesWithCorpseBlueprints.IsNullOrEmpty())
            {
                entitiesWithCorpseBlueprints ??= new()
                {
                    "empty",
                };
            }
            output += entitiesWithCorpseBlueprints.GenerateBulletList() + "\n\n";

            output += nameof(CorpseByEntity) + "\n";
            output += "Entity,Corpse\n";
            List<string> corpseByEntity = new();
            foreach (string corpseEntityPair in CorpseByEntity.ConvertToStringListWithKeyValue(kvp => kvp.Key + ": " + kvp.Value))
            {
                corpseByEntity.Add(corpseEntityPair);
            }
            if (corpseByEntity.IsNullOrEmpty())
            {
                corpseByEntity ??= new()
                {
                    "empty",
                };
            }
            output += corpseByEntity.GenerateBulletList() + "\n\n";

            output += nameof(EntitiesByCorpse) + "\n";
            output += "Corpse,Entities\n";
            List<string> entitesByCorpse = new();
            foreach (string entityCorpsePair in EntitiesByCorpse.ConvertToStringListWithKeyValue(kvp => kvp.Key + "," + "\"" + kvp.Value.Join() + "\""))
            {
                entitesByCorpse.Add(entityCorpsePair);
            }
            if (entitesByCorpse.IsNullOrEmpty())
            {
                entitesByCorpse ??= new()
                {
                    "empty",
                };
            }
            output += entitesByCorpse.GenerateBulletList() + "\n\n";

            output += nameof(ProcessablesByProduct) + "\n";
            output += "Product,Corpse\n";
            List<string> processablesByProduct = new();
            foreach (string corpseProductPair in ProcessablesByProduct.ConvertToStringListWithKeyValue(list => Grammar.MakeAndList(list)))
            {
                processablesByProduct.Add(corpseProductPair);
            }
            if (processablesByProduct.IsNullOrEmpty())
            {
                processablesByProduct ??= new()
                {
                    "empty",
                };
            }
            output += processablesByProduct.GenerateBulletList() + "\n\n";

            static string andListOrBulletList(List<string> list)
            {
                if (!list.IsNullOrEmpty() & list.Count > 3)
                {
                    string spaces = "=ud_nbsp:4=".StartReplace().ToString();
                    return "\n" + list.GenerateBulletList(ItemPostProc: s => spaces + s);
                }
                return "\"" + Grammar.MakeAndList(list) + "\"";
            }
            output += nameof(ProductsByProcessable) + "\n";
            output += "Corpse,Product\n";
            List<string> productsByProcessable = new();
            foreach (string productCorpsePair in ProductsByProcessable.ConvertToStringListWithKeyValue(andListOrBulletList))
            {
                productsByProcessable.Add(productCorpsePair);
            }
            if (productsByProcessable.IsNullOrEmpty())
            {
                productsByProcessable ??= new()
                {
                    "empty",
                };
            }
            output += productsByProcessable.GenerateBulletList() + "\n\n";

            static string bwpListToStringList(List<BlueprintWeightPair> bwpList)
            {
                if (!bwpList.IsNullOrEmpty() & bwpList.Count > 3)
                {
                    string spaces = "=ud_nbsp:4=".StartReplace().ToString();
                    return "\n" + new List<string>(bwpList.ConvertToStringList(bwp => (string)bwp + ": " + (int)bwp)).GenerateBulletList(ItemPostProc: s => spaces + s);
                }
                return "\"" + new List<string>(bwpList.ConvertToStringList(bwp => (string)bwp + ": " + (int)bwp)).Join() + "\"";
            }
            output += nameof(EntityWithWeightByCorpse) + "\n";
            List<string> entitesByCorpseWithChance = new();
            foreach (string corpsewithChanceEntityPair in EntityWithWeightByCorpse.ConvertToStringListWithKeyValue(bwpListToStringList))
            {
                entitesByCorpseWithChance.Add(corpsewithChanceEntityPair);
            }
            if (entitesByCorpseWithChance.IsNullOrEmpty())
            {
                entitesByCorpseWithChance ??= new()
                {
                    "empty",
                };
            }
            output += entitesByCorpseWithChance.GenerateBulletList() + "\n\n";

            UnityEngine.Debug.Log(output);
            Popup.Show(output);
        }
    }
}
