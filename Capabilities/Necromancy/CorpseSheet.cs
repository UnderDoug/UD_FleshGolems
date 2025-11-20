using System;
using System.Text;
using System.Collections.Generic;
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

using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;
using UD_FleshGolems.Logging;

using Relationship = UD_FleshGolems.Capabilities.Necromancy.CorpseEntityPair.PairRelationship;

using SerializeField = UnityEngine.SerializeField;
using static UD_FleshGolems.Capabilities.Necromancy.CorpseEntityPair;
using System.Diagnostics.CodeAnalysis;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public class CorpseSheet : IComposite
    {
        public static UD_FleshGolems_NecromancySystem NecromancySystem => UD_FleshGolems_NecromancySystem.System;

        [SerializeField]
        protected CorpseBlueprint Corpse;

        // [SerializeField]
        // private List<CorpseEntityPair> Pairs;

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

        protected CorpseSheet()
        {
            _IsCorpseProduct = false;
            Corpse = null;
            // Pairs = new();
            Entities = new();
            InheritedCorpses = new();
            ProductOriginBlueprints = new();
        }

        public CorpseSheet(CorpseBlueprint Corpse)
            : this()
        {
            this.Corpse = Corpse;
            InheritedCorpses = GetInheritedCorpses(Corpse);
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
        public Dictionary<BlueprintBox, int> GetWeightedEntityList()
        {
            Dictionary<BlueprintBox, int> weightedList= new();
            foreach (CorpseEntityPair weightedPairs in GetPairs())
            {
                AccumulateBlueprintWeight(ref weightedList, Corpse, weightedPairs);
            }
            return weightedList;
        }
        public Dictionary<string, int> GetWeightedEntityNameList(bool Include0Chance, Predicate<GameObjectBlueprint> Filter)
        {
            Dictionary<string, int> weightedList= new();
            foreach ((BlueprintBox blueprint, int weight) in GetWeightedEntityList())
            {
                if ((Include0Chance || weight > 0)
                    && (Filter == null || Filter(blueprint.GetGameObjectBlueprint())))
                {
                    weightedList.Add(blueprint.ToString(), weight);
                }
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

        private List<CorpseBlueprint> GetInheritedCorpses(CorpseBlueprint CorpseBlueprint)
        {
            Debug.GetIndents(out Indent indent);
            Debug.Log(Debug.GetCallingTypeAndMethod(), CorpseBlueprint.ToString(), indent[1]);

            List<CorpseBlueprint> inheritedCorpses = new();
            GameObjectBlueprint inheritedCorpse = CorpseBlueprint?.GetGameObjectBlueprint()?.Inherits?.GetGameObjectBlueprint();

            while (inheritedCorpse != null
                && inheritedCorpse.IsCorpse()
                && NecromancySystem?.RequireCorpseSheet(inheritedCorpse)?.GetCorpse() is CorpseBlueprint inheritedCorpseBlueprint)
            {
                if (inheritedCorpses.Contains(inheritedCorpseBlueprint))
                {
                    inheritedCorpses.Add(inheritedCorpseBlueprint);
                }
                // inheritedCorpses.AddUnique(inheritedCorpseBlueprint);
                inheritedCorpse = inheritedCorpse?.Inherits?.GetGameObjectBlueprint();
            }
            Debug.SetIndent(indent[0]);
            return inheritedCorpses;
        }
        private bool AddInheritedCorpses(EntityWeight WithEntityWeight)
        {
            Debug.GetIndents(out Indent indent);
            Debug.Log(Debug.GetCallingTypeAndMethod(), WithEntityWeight?.GetBlueprint()?.ToString(), indent[1]);
            // Pairs ??= new();
            Entities ??= new();
            EntityBlueprint entityBlueprint = WithEntityWeight.GetBlueprint();
            bool any = false;
            foreach (CorpseBlueprint inheritedCorpse in InheritedCorpses)
            {
                Debug.Log(inheritedCorpse?.ToString(), indent[2]);
                if (!Entities.ContainsKey(entityBlueprint))
                {
                    Entities.Add(entityBlueprint, new());
                }
                if (!Entities[entityBlueprint].ContainsKey(Relationship.InheritedCorpse))
                {
                    Entities[entityBlueprint].Add(Relationship.InheritedCorpse, WithEntityWeight);
                    any = true;
                }
                else
                {
                    if (Entities[entityBlueprint][Relationship.InheritedCorpse].Weight < WithEntityWeight.Weight)
                    {
                        Entities[entityBlueprint][Relationship.InheritedCorpse] = WithEntityWeight;
                        any = true;
                    }
                }
            }
            Debug.SetIndent(indent[0]);
            return any;
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
                .ToList()
                .ConvertAll(cep => cep.GetEntityWeight());

        public IReadOnlyList<EntityWeight> GetEntityWeights(bool ExcludeProducts, Predicate<EntityWeight> Filter = null)
            => GetPairs(cep => !ExcludeProducts || cep.Relationship != Relationship.CorpseProduct, Filter)
                .ToList()
                .ConvertAll(cep => cep.GetEntityWeight());

        public IReadOnlyList<EntityWeight> GetEntityWeights(bool ExcludeProducts, Predicate<GameObjectBlueprint> Filter = null)
            => GetPairs(cep => !ExcludeProducts || cep.Relationship != Relationship.CorpseProduct, Filter)
                .ToList()
                .ConvertAll(cep => cep.GetEntityWeight());

        public IReadOnlyList<EntityWeight> GetEntityWeights(Predicate<GameObjectBlueprint> Filter = null)
            => GetEntityWeights(false, Filter);

        public CorpseSheet AddEntity(
            string EntityBlueprint,
            int Weight,
            Relationship Relationship,
            EqualityComparer<CorpseEntityPair> EqComparer = null)
        {
            EntityBlueprint entityBlueprint = new(EntityBlueprint);
            EntityWeight entityWeight = new(entityBlueprint, Weight);
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
            return this;
        }

        public CorpseSheet AddEntity(
            GameObjectBlueprint EntityBlueprint,
            int Weight, Relationship Relationship,
            EqualityComparer<CorpseEntityPair> EqComparer = null)
            => AddEntity(
                EntityBlueprint: EntityBlueprint.Name,
                Weight: Weight,
                Relationship: Relationship,
                EqComparer: EqComparer);

        public CorpseSheet AddPrimaryEntity(GameObjectBlueprint EntityBlueprint, int Weight)
            => AddEntity(
                EntityBlueprint: EntityBlueprint,
                Weight: Weight,
                Relationship: Relationship.PrimaryCorpse,
                EqComparer: new EqualityComparer(CompareType.Blueprints, CompareType.Relationship));

        public CorpseSheet AddPrimaryEntity(string EntityBlueprint, int Weight)
            => AddEntity(
                EntityBlueprint: EntityBlueprint,
                Weight: Weight,
                Relationship: Relationship.PrimaryCorpse,
                EqComparer: new EqualityComparer(CompareType.Blueprints, CompareType.Relationship));

        public CorpseSheet AddProductEntity(EntityWeight EntityWeight)
            => AddEntity(
                EntityBlueprint: EntityWeight.Blueprint.ToString(),
                Weight: EntityWeight.Weight,
                Relationship: Relationship.CorpseProduct,
                EqComparer: new EqualityComparer(CompareType.Blueprints, CompareType.Relationship));

        public CorpseSheet AddCountsAsEntity(EntityWeight EntityWeight)
            => AddEntity(
                EntityBlueprint: EntityWeight.Blueprint.ToString(),
                Weight: EntityWeight.Weight,
                Relationship: Relationship.CorpseCountsAs,
                EqComparer: new EqualityComparer(CompareType.Blueprints, CompareType.Relationship));

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
    }
}
