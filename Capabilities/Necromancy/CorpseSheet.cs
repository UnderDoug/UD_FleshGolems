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

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public class CorpseSheet : IComposite
    {
        public static UD_FleshGolems_NecromancySystem NecromancySystem => UD_FleshGolems_NecromancySystem.System;

        [SerializeField]
        private CorpseBlueprint Corpse;

        [SerializeField]
        private List<CorpseEntityPair> Pairs;

        [SerializeField]
        private bool _IsCorpseProduct;

        public bool IsCorpseProduct
        {
            get => _IsCorpseProduct = Corpse.GetType().InheritsFrom(typeof(CorpseProduct));
            set
            {
                if (value != _IsCorpseProduct)
                {
                    if (value)
                    {
                        Corpse = new CorpseProduct(Corpse, new(_CorpseProductCorpseBlueprints));
                        _CorpseProductCorpseBlueprints = null;
                    }
                    else
                    {
                        _CorpseProductCorpseBlueprints = new(((CorpseProduct)Corpse).CorpseBlueprints);
                        Corpse = new CorpseBlueprint(Corpse);
                    }
                }
                _IsCorpseProduct = value;
            }
        }

        [SerializeField]
        private List<CorpseBlueprint> _CorpseProductCorpseBlueprints;

        [SerializeField]
        private List<CorpseBlueprint> InheritedCorpses;

        private CorpseSheet()
        {
            _IsCorpseProduct = false;
            Corpse = null;
            Pairs = new();
            InheritedCorpses = new();
            _CorpseProductCorpseBlueprints = new();
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
            Pairs = this.Pairs;
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
            foreach (CorpseEntityPair weightedPairs in Pairs)
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

        public IReadOnlyList<CorpseEntityPair> GetPairs(Predicate<CorpseEntityPair> Filter, Predicate<GameObjectBlueprint> EntityFilter)
            => Pairs
                .Where(cep =>Filter == null || Filter(cep))
                .Where(cep => EntityFilter == null || EntityFilter(cep.GetEntityGameObjectBlueprint()))
                .ToList();

        public IReadOnlyList<CorpseEntityPair> GetPairs() => GetPairs(null, null);

        public IReadOnlyList<CorpseEntityPair> GetPrimaryPairs(Predicate<CorpseEntityPair> Filter, Predicate<GameObjectBlueprint> EntityFilter)
            => GetPairs(Filter, EntityFilter)
                .Where(cep => cep.Relationship == Relationship.PrimaryCorpse)
                .ToList();

        private bool CorpseBluprintPairHasSameBlueprint(CorpseEntityPair Old, CorpseEntityPair New) => Old.Corpse == New.Corpse;
        private bool NewCorpseBluprintPairHasHigherWeight(CorpseEntityPair Old, CorpseEntityPair New) => Old.Corpse == New.Corpse;
        private bool AddUniquePair(List<CorpseEntityPair> Collection, CorpseEntityPair Pair)
            => Collection.AddUniqueObject(
                Item: Pair,
                EqualityComparer: new EqualityComparer(CompareType.Blueprints),
                Comparer: new OrderedComparer(CompareType.Relationship, CompareType.Weight));

        private bool AddUniqueEntity(
            List<CorpseEntityPair> Collection,
            EntityBlueprint Entity,
            int Weight,
            Relationship Relationship = Relationship.None)
            => AddUniquePair(
                Collection: Collection,
                Pair: new(Corpse, Entity, Weight, Relationship));

        private bool AddUniqueEntity(
            List<CorpseEntityPair> Collection,
            GameObjectBlueprint Blueprint,
            int Weight,
            Relationship Relationship = Relationship.None)
            => AddUniqueEntity(
                Collection: Collection,
                Entity: NecromancySystem.RequireEntityBlueprint(Blueprint),
                Weight: Weight,
                Relationship: Relationship);

        private List<CorpseBlueprint> GetInheritedCorpses(CorpseBlueprint CorpseBlueprint)
        {
            Debug.GetIndents(out Indents indent);
            Debug.Log(Debug.GetCallingTypeAndMethod(), CorpseBlueprint.ToString(), indent[1]);

            List<CorpseBlueprint> inheritedCorpses = new();
            GameObjectBlueprint inheritedCorpse = CorpseBlueprint?.GetGameObjectBlueprint()?.Inherits?.GetGameObjectBlueprint();

            while (inheritedCorpse != null
                && inheritedCorpse.IsCorpse()
                && NecromancySystem?.RequireCorpseSheet(inheritedCorpse)?.GetCorpse() is CorpseBlueprint inheritedCorpseBlueprint)
            {
                inheritedCorpses.AddUnique(inheritedCorpseBlueprint);
                inheritedCorpse = inheritedCorpse?.Inherits?.GetGameObjectBlueprint();
            }
            Debug.SetIndent(indent[0]);
            return inheritedCorpses;
        }
        private bool AddInheritedCorpses(EntityWeight WithEntityWeight)
        {
            Debug.GetIndents(out Indents indent);
            Debug.Log(Debug.GetCallingTypeAndMethod(), WithEntityWeight?.GetBlueprint()?.ToString(), indent[1]);
            Pairs ??= new();
            bool any = false;
            foreach (CorpseBlueprint inheritedCorpse in InheritedCorpses)
            {
                Debug.Log(inheritedCorpse?.ToString(), indent[2]);
                any = AddUniquePair(
                    Collection: Pairs, 
                    Pair: new CorpseEntityPair(
                        Corpse: inheritedCorpse,
                        Entity: WithEntityWeight.GetBlueprint(),
                        Weight: WithEntityWeight.Weight,
                        Relationship: Relationship.InheritedCorpse))
                    || any;
            }
            Debug.SetIndent(indent[0]);
            return any;
        }

        public IReadOnlyList<EntityWeight> GetEntityWeights(bool ExcludeProducts, Predicate<GameObjectBlueprint> Filter = null)
            => GetPairs(cep => !ExcludeProducts || cep.Relationship != Relationship.CorpseProduct, Filter)
                .ToList()
                .ConvertAll(cep => cep.GetEntityWeight());

        public IReadOnlyList<EntityWeight> GetEntityWeights(Predicate<GameObjectBlueprint> Filter = null)
            => GetEntityWeights(false, Filter);

        public CorpseSheet AddPair(CorpseEntityPair Pair, bool SkipInherited = false, EqualityComparer<CorpseEntityPair> EqComparer = null)
        {
            Pairs ??= new();
            if (Pair is null)
            {
                throw new ArgumentNullException(nameof(Pair));
            }
            Pairs.AddUnique(Pair, EqComparer);
            if (!SkipInherited)
            {
                AddInheritedCorpses(Pair.GetEntityWeight());
            }
            return this;
        }
        public CorpseSheet AddEntity(
            string EntityBlueprint,
            int Weight,
            Relationship Relationship,
            EqualityComparer<CorpseEntityPair> EqComparer = null)
            => AddPair(
                Pair: new CorpseEntityPair(
                    Corpse: Corpse, 
                    Entity: NecromancySystem?.RequireEntityBlueprint(EntityBlueprint),
                    Weight: Weight,
                    Relationship: Relationship),
                EqComparer: EqComparer);

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
    }
}
