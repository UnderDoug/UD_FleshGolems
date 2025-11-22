using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

using XRL.World;

using UD_FleshGolems.Logging;

using Relationship = UD_FleshGolems.Capabilities.Necromancy.CorpseEntityPair.PairRelationship;
using ArgPair = UD_FleshGolems.Logging.Debug.ArgPair;

using SerializeField = UnityEngine.SerializeField;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public class CorpseSheet : IComposite
    {
        public static UD_FleshGolems_NecromancySystem NecromancySystem => UD_FleshGolems_NecromancySystem.System;

        [SerializeField]
        protected CorpseBlueprint Corpse;

        [SerializeField]
        protected Dictionary<EntityBlueprint, Dictionary<Relationship, EntityWeight>> Entities;

        [SerializeField]
        protected bool _IsCorpseProduct;

        public bool IsCorpseProduct
        {
            get => _IsCorpseProduct = Corpse.GetType().InheritsFrom(typeof(CorpseProduct));
            set
            {
                if (value != _IsCorpseProduct)
                {
                    if (value)
                    {
                        Corpse = new CorpseProduct(Corpse);
                    }
                    else
                    {
                        Corpse = new CorpseBlueprint(Corpse);
                    }
                }
                _IsCorpseProduct = value;
            }
        }

        [SerializeField]
        protected List<CorpseBlueprint> InheritedCorpses;

        [SerializeField]
        protected List<CorpseBlueprint> ProductOriginBlueprints;

        public bool InheritedCorpsesInitialized
            => Corpse?.GetGameObjectBlueprint() is GameObjectBlueprint corpseModel
            && (corpseModel.Name == "Corpse" 
                || (corpseModel.GetBlueprintInheritsList().Any(bp => bp.Name == "Corpse")
                    && !InheritedCorpses.IsNullOrEmpty()));

        protected CorpseSheet()
        {
            _IsCorpseProduct = false;
            Corpse = null;
            Entities = new();
            InheritedCorpses = new();
            ProductOriginBlueprints = new();
        }

        public CorpseSheet(CorpseBlueprint Corpse)
            : this()
        {
            this.Corpse = Corpse;
            InitializeInheritedCorpseList();
        }

        public void Deconstruct(out CorpseBlueprint Corpse, out IReadOnlyList<CorpseEntityPair> Pairs)
        {
            Corpse = this.Corpse;
            Pairs = GetPairs();
        }

        public void Deconstruct(out CorpseBlueprint Corpse, out Dictionary<EntityBlueprint, Dictionary<Relationship, EntityWeight>> Entities)
        {
            Corpse = this.Corpse;
            Entities = this.Entities;
        }

        public Dictionary<Relationship, EntityWeight> this[[AllowNull] EntityBlueprint Entity]
        {
            get
            {
                if (Entity == null
                    || !Entities.ContainsKey(Entity)
                    || Entities[Entity] == null)
                {
                    return null;
                }
                return Entities[Entity];
            }
            private set
            {
                if (Entity != null)
                {
                    if (Entities.ContainsKey(Entity))
                    {
                        Entities[Entity] = value;
                    }
                    else
                    {
                        Entities.Add(Entity, value);
                    }
                }
            }
        }

        public IEnumerable<EntityWeight> this[[AllowNull] Relationship Relationship]
        {
            get
            {
                if (Relationship == Relationship.None)
                {
                    yield break;
                }
                Entities ??= new();
                foreach ((EntityBlueprint _, Dictionary<Relationship, EntityWeight> rew) in Entities)
                {
                    foreach ((Relationship rel, EntityWeight ew) in rew)
                    {
                        if (rel == Relationship)
                        {
                            yield return ew;
                        }
                    }
                }
            }
        }

        public CorpseBlueprint GetCorpse()
        {
            return Corpse;
        }
        public GameObjectBlueprint GetCorpseBlueprint()
        {
            return Corpse.GetGameObjectBlueprint();
        }
        public IReadOnlyList<CorpseBlueprint> GetInheritedCorpseList()
        {
            return InheritedCorpses;
        }
        public Dictionary<BlueprintBox, int> GetWeightedEntityList()
        {
            Dictionary<BlueprintBox, int> weightedList= new();
            foreach (CorpseEntityPair weightedPairs in GetPairs())
            {
                AccumulateBlueprintWeight(ref weightedList, Corpse, weightedPairs);
            }
            return weightedList;
        }
        private static void AccumulateBlueprintWeight(
            ref Dictionary<BlueprintBox, int> WeightedList,
            BlueprintBox Key,
            CorpseEntityPair Pair)
        {
            WeightedList ??= new();
            if (Pair[Key] is BlueprintWeight blueprintWeight)
            {
                if (WeightedList.ContainsKey(blueprintWeight.Blueprint))
                {
                    WeightedList[blueprintWeight.Blueprint] += blueprintWeight.Weight;
                }
                else
                {
                    WeightedList.Add(blueprintWeight.Blueprint, blueprintWeight.Weight);
                }
            }
        }
        public Dictionary<string, int> GetWeightedEntityNameList(bool Include0Chance, Predicate<GameObjectBlueprint> Filter, bool DrillIntoInheritance = true)
        {
            Dictionary<string, int> weightedList= new();
            foreach ((string blueprint, int weight) in GetWeightedList(Filter))
            {
                if (Include0Chance || weight > 0)
                {
                    weightedList.Add(blueprint.ToString(), weight);
                }
            }
            if (DrillIntoInheritance
                && weightedList.IsNullOrEmpty()
                && !InheritedCorpses.IsNullOrEmpty())
            {
                return NecromancySystem
                    ?.RequireCorpseSheet(InheritedCorpses[0])
                    ?.GetWeightedEntityNameList(Include0Chance, Filter, DrillIntoInheritance);
            }
            return weightedList;
        }

        public IReadOnlyList<CorpseEntityPair> GetPairs(Predicate<CorpseEntityPair> Filter, Predicate<GameObjectBlueprint> EntityFilter)
        {
            List<CorpseEntityPair> output = new();
            foreach ((EntityBlueprint eb, Dictionary<Relationship, EntityWeight> rew) in Entities)
            {
                if (EntityFilter == null || EntityFilter(eb.GetGameObjectBlueprint()))
                {
                    foreach ((Relationship rel, EntityWeight ew) in rew)
                    {
                        CorpseEntityPair cep = new(Corpse, eb, ew.Weight, rel);
                        if (Filter == null || Filter (cep))
                        {
                            output.Add(cep);
                        }
                    }
                }
            }
            return output;
        }

        public IReadOnlyList<CorpseEntityPair> GetPairs(Predicate<CorpseEntityPair> Filter, Predicate<EntityWeight> EntityWeightFilter)
            => GetPairs()
                .Where(cep =>Filter == null || Filter(cep))
                .Where(cep => EntityWeightFilter == null || EntityWeightFilter(cep.GetEntityWeight()))
                .ToList();

        public IReadOnlyList<CorpseEntityPair> GetPairs() => GetPairs(null, (Predicate<GameObjectBlueprint>)null);

        public IReadOnlyList<CorpseEntityPair> GetPrimaryPairs(Predicate<CorpseEntityPair> Filter, Predicate<GameObjectBlueprint> EntityFilter)
            => GetPairs(Filter, EntityFilter)
                .Where(cep => cep.Relationship == Relationship.PrimaryCorpse)
                .ToList();

        private IReadOnlyList<CorpseBlueprint> GetInheritedCorpses()
        {
            Debug.LogMethod1(out Indent indent, Debug.LogArg(nameof(Corpse), Corpse.ToString()));
            List<CorpseBlueprint> outputList = new();
            foreach (GameObjectBlueprint inheritedCorpse in Corpse.GetGameObjectBlueprint().GetBlueprintInherits())
            {
                if (inheritedCorpse.IsCorpse()
                    && NecromancySystem?.RequireCorpseSheet(inheritedCorpse)?.Corpse is CorpseBlueprint inheritedCorpseBlueprint)
                {
                    outputList.Add(inheritedCorpseBlueprint);
                    Debug.Log(inheritedCorpseBlueprint.ToString(), indent[1]);
                }
            }
            Debug.DiscardIndent();
            return outputList;
        }
        public CorpseSheet InitializeInheritedCorpseList()
        {
            InheritedCorpses = GetInheritedCorpses().ToList();
            return this;
        }

        public IReadOnlyList<EntityBlueprint> GetEntities(Predicate<GameObjectBlueprint> Filter = null)
        {
            List<EntityBlueprint> output = new();
            foreach (CorpseEntityPair pair in GetPairs(null, Filter))
            {
                if (!output.Contains(pair.Entity))
                {
                    output.AddUnique(pair.Entity);
                }
            }
            return output;
        }

        public IReadOnlyList<EntityWeight> GetEntityWeights(bool ExcludeProducts)
            => GetPairs(cep => !ExcludeProducts || cep.Relationship != Relationship.CorpseProduct, (Predicate<GameObjectBlueprint>)null)
                ?.ToList()
                ?.ConvertAll(cep => cep.GetEntityWeight());

        public IReadOnlyList<EntityWeight> GetEntityWeights(bool ExcludeProducts, Predicate<EntityWeight> Filter = null)
            => GetPairs(cep => !ExcludeProducts || cep.Relationship != Relationship.CorpseProduct, Filter)
                ?.ToList()
                ?.ConvertAll(cep => cep.GetEntityWeight());

        public IReadOnlyList<EntityWeight> GetEntityWeights(bool ExcludeProducts, Predicate<GameObjectBlueprint> Filter = null)
            => GetPairs(cep => !ExcludeProducts || cep.Relationship != Relationship.CorpseProduct, Filter)
                ?.ToList()
                ?.ConvertAll(cep => cep.GetEntityWeight());

        public IReadOnlyList<EntityWeight> GetEntityWeights(Predicate<GameObjectBlueprint> Filter = null)
            => GetEntityWeights(false, Filter);

        public IReadOnlyList<EntityWeightRelationship> GetEntityWeightRelationships(
            bool ExcludeProducts,
            Predicate<GameObjectBlueprint> Filter = null)
            => GetPairs(cep => !ExcludeProducts || cep.Relationship != Relationship.CorpseProduct, Filter)
                ?.ToList()
                ?.ConvertAll(cep => new EntityWeightRelationship(cep));

        public IReadOnlyList<EntityWeightRelationship> GetEntityWeightRelationships(bool ExcludeProducts, Predicate<EntityWeight> Filter = null)
            => GetPairs(cep => !ExcludeProducts || cep.Relationship != Relationship.CorpseProduct, Filter)
                ?.ToList()
                ?.ConvertAll(cep => new EntityWeightRelationship(cep));

        public IReadOnlyList<EntityWeightRelationship> GetEntityWeightRelationships(Predicate<GameObjectBlueprint> Filter = null)
            => GetEntityWeightRelationships(false, Filter);

        public CorpseSheet AddEntity(
            string EntityBlueprint,
            int Weight,
            Relationship Relationship)
        {
            using Indent indent = new();
            Debug.LogMethod("for corpse " + Corpse.ToString(), indent[1], new ArgPair[]
                {
                    Debug.LogArg(EntityBlueprint),
                    Debug.LogArg(Weight),
                    Debug.LogArg(Relationship),
                });
            EntityWeight entityWeight = new(NecromancySystem?.RequireEntityBlueprint(EntityBlueprint), Weight);
            EntityBlueprint entityBlueprint = entityWeight.GetBlueprint();
            Entities ??= new();
            if (!Entities.ContainsKey(entityBlueprint))
            {
                Entities.Add(entityBlueprint, new());
            }
            if (!Entities[entityBlueprint].ContainsKey(Relationship))
            {
                Entities[entityBlueprint].Add(Relationship, entityWeight);
            }
            else
            {
                if (Entities[entityBlueprint][Relationship].Weight < entityWeight.Weight)
                {
                    Entities[entityBlueprint][Relationship] = entityWeight;
                }
            }
            if (Relationship != Relationship.InheritedCorpse && false)
            {
                foreach (CorpseBlueprint inheritedCorpse in InheritedCorpses)
                {
                    CorpseSheet inheritedCorpseSheet = NecromancySystem?.RequireCorpseSheet(inheritedCorpse);
                    inheritedCorpseSheet.AddInheritedEntity(entityWeight);
                }
            }
            return this;
        }

        public CorpseSheet AddEntity(
            GameObjectBlueprint EntityBlueprint,
            int Weight,
            Relationship Relationship)
            => AddEntity(
                EntityBlueprint: EntityBlueprint.Name,
                Weight: Weight,
                Relationship: Relationship);

        public CorpseSheet AddEntity(
            EntityWeight EntityWeight,
            Relationship Relationship)
            => AddEntity(
                EntityBlueprint: EntityWeight.GetBlueprint().ToString(),
                Weight: EntityWeight.Weight,
                Relationship: Relationship);

        public CorpseSheet AddPrimaryEntity(GameObjectBlueprint EntityBlueprint, int Weight)
            => AddEntity(
                EntityBlueprint: EntityBlueprint,
                Weight: Weight,
                Relationship: Relationship.PrimaryCorpse);

        public CorpseSheet AddPrimaryEntity(string EntityBlueprint, int Weight)
            => AddEntity(
                EntityBlueprint: EntityBlueprint,
                Weight: Weight,
                Relationship: Relationship.PrimaryCorpse);

        public CorpseSheet AddInheritedEntity(EntityWeight EntityWeight)
            => AddEntity(
                EntityBlueprint: EntityWeight.Blueprint.ToString(),
                Weight: EntityWeight.Weight,
                Relationship: Relationship.InheritedCorpse);

        public CorpseSheet AddInheritedEntities(List<EntityWeight> EntityWeights)
            => EntityWeights.ForEach(ew => AddInheritedEntity(ew), Return: this);

        public CorpseSheet AddProductEntity(EntityWeight EntityWeight)
            => AddEntity(
                EntityBlueprint: EntityWeight.Blueprint.ToString(),
                Weight: EntityWeight.Weight,
                Relationship: Relationship.CorpseProduct);

        public CorpseSheet AddCountsAsEntity(EntityWeight EntityWeight)
            => AddEntity(
                EntityBlueprint: EntityWeight.Blueprint.ToString(),
                Weight: EntityWeight.Weight,
                Relationship: Relationship.CorpseCountsAs);

        public bool CorpseHasEntity(string Entity, bool CheckAll = true)
            => GetEntityWeights(!CheckAll)
                .Any(c => c?.GetBlueprint().ToString() == Entity);

        public bool CorpseHasEntity(EntityBlueprint Entity, bool CheckAll = true)
            => CorpseHasEntity(Entity.ToString(), CheckAll);

        public bool CorpseHasEntityWithWeight(EntityWeight EntityWeight, bool CheckAll = true)
            => GetEntityWeights(!CheckAll)
                .Any(c => c?.Blueprint == EntityWeight?.Blueprint && c?.Weight == EntityWeight?.Weight);

        public bool IsPrimaryForEntity(EntityBlueprint EntityBlueprint)
            => GetPairs()
                .Any(c => c?.Entity == EntityBlueprint && c?.Relationship == Relationship.PrimaryCorpse);

        public bool AddProductOriginCorpse(CorpseBlueprint Corpse)
        {
            ProductOriginBlueprints ??= new();
            if (!ProductOriginBlueprints.Contains(Corpse))
            {
                ProductOriginBlueprints.Add(Corpse);
                return true;
            }
            return false;
        }

        public Dictionary<string, int> GetWeightedList(Predicate<GameObjectBlueprint> Filter = null)
            => GetEntityWeights(Filter)?.ConvertToWeightedList();

        public List<string> GetWeightedListToString(Predicate<GameObjectBlueprint> Filter = null)
            => GetWeightedList(Filter)?.ConvertToStringList(kvp => kvp.Key + ": " + kvp.Value);
    }
}
