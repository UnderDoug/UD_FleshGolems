using System;
using System.Collections.Generic;
using System.Linq;

using UD_FleshGolems.Logging;

using XRL;
using XRL.Collections;
using XRL.World;

using Relationship = UD_FleshGolems.Capabilities.Necromancy.CorpseSheet.CorpseEntityPair.PairRelationship;

using SerializeField = UnityEngine.SerializeField;

namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        public partial class CorpseSheet : IComposite
        {
            [Serializable] public abstract partial class BlueprintBox : IComposite { }
            [Serializable] public partial class CorpseBlueprint : BlueprintBox, IComposite { }
            [Serializable] public partial class CorpseProduct : CorpseBlueprint, IComposite { }
            [Serializable] public partial class EntityBlueprint : BlueprintBox, IComposite { }
            [Serializable] public partial class CorpseEntityPair : IComposite { }
            [Serializable] public abstract partial class BlueprintWeight<T> : IComposite where T : BlueprintBox, new() { }
            [Serializable] public partial class CorpseWeight : BlueprintWeight<CorpseBlueprint>, IComposite { }
            [Serializable] public partial class EntityWeight : BlueprintWeight<EntityBlueprint>, IComposite { }

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

            public void Deconstruct(out string Corpse) => Corpse = this.Corpse.Name;
            public void Deconstruct(out CorpseBlueprint Corpse) => Corpse = this.Corpse;
            public void Deconstruct(out GameObjectBlueprint Corpse) => Corpse = this.Corpse.GetGameObjectBlueprint();
            public void Deconstruct(out IReadOnlyList<CorpseEntityPair> Pairs) => Pairs = this.Pairs;
            public void Deconstruct(out CorpseBlueprint Corpse, out IReadOnlyList<CorpseEntityPair> Pairs)
            {
                Deconstruct(out Corpse);
                Deconstruct(out Pairs);
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
                    EqualityComparer: new CorpseEntityPairEqualityComparer(CompareType.Blueprints),
                    Comparer: new CorpseEntityPairComparer(CompareType.Relationship, CompareType.Weight));

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
                Debug.Log(Debug.GetCallingTypeAndMethod(), CorpseBlueprint.Name, indent[1]);

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
            private bool AddInheritedCorpse(CorpseEntityPair FromCorpse, ref CorpseEntityPair InheritedCorpse)
            {
                Debug.GetIndents(out Indents indent);
                Debug.Log(Debug.GetCallingTypeAndMethod(), FromCorpse.Corpse.Name, indent[1]);
                if (FromCorpse?.GetCorpseGameObjectBlueprint()?.Inherits?.GetGameObjectBlueprint() is GameObjectBlueprint inheritedCorpseModel
                    && inheritedCorpseModel.IsCorpse()
                    && NecromancySystem?.RequireCorpseSheet(inheritedCorpseModel)?.GetCorpse() is CorpseBlueprint inheritedCorpseBlueprint)
                {
                    Pairs ??= new();
                    InheritedCorpse =
                        new CorpseEntityPair(
                            Corpse: inheritedCorpseBlueprint,
                            Entity: FromCorpse.Entity,
                            Weight: FromCorpse.Weight,
                            Relationship: Relationship.InheritedCorpse);

                    Debug.SetIndent(indent[0]);
                    return AddUniquePair(Pairs, InheritedCorpse);
                }
                Debug.SetIndent(indent[0]);
                return false;
            }
            private bool AddInheritedCorpses(CorpseEntityPair FromCorpse, out List<CorpseEntityPair> InheritedCorpses)
            {
                Debug.GetIndents(out Indents indent);
                Debug.Log(Debug.GetCallingTypeAndMethod(), FromCorpse.Corpse.Name, indent[1]);
                Pairs ??= new();
                bool any = false;
                InheritedCorpses = new();
                CorpseEntityPair InheritedCorpse = null;
                while (AddInheritedCorpse(FromCorpse, ref InheritedCorpse))
                {
                    InheritedCorpses.Add(InheritedCorpse);
                    FromCorpse = InheritedCorpse;
                    any = true;
                }
                Debug.SetIndent(indent[0]);
                return any;
            }
            private bool AddInheritedCorpses(EntityWeight WithEntityWeight)
            {
                Debug.GetIndents(out Indents indent);
                Debug.Log(Debug.GetCallingTypeAndMethod(), WithEntityWeight?.GetBlueprint()?.Name, indent[1]);
                Pairs ??= new();
                bool any = false;
                foreach (CorpseBlueprint inheritedCorpse in InheritedCorpses)
                {
                    Debug.Log(inheritedCorpse?.Name, indent[2]);
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
                    .ConvertAll(cep => (EntityWeight)cep);

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
                    AddInheritedCorpses((EntityWeight)Pair);
                }
                return this;
            }
            public CorpseSheet AddEntity(string EntityBlueprint, int Weight, Relationship Relationship, EqualityComparer<CorpseEntityPair> EqComparer = null)
                => AddPair(
                    Pair: new CorpseEntityPair(
                        Corpse: Corpse, 
                        Entity: NecromancySystem?.RequireEntityBlueprint(EntityBlueprint),
                        Weight: Weight,
                        Relationship: Relationship),
                    EqComparer: EqComparer);
            public CorpseSheet AddEntity(GameObjectBlueprint EntityBlueprint, int Weight, Relationship Relationship, EqualityComparer<CorpseEntityPair> EqComparer = null)
                => AddEntity(EntityBlueprint.Name, Weight, Relationship, EqComparer);

            public CorpseSheet AddPrimaryEntity(GameObjectBlueprint EntityBlueprint, int Weight)
                => AddEntity(EntityBlueprint, Weight, Relationship.PrimaryCorpse, new CorpseEntityPairEqualityComparer(CompareType.Blueprints, CompareType.Relationship));

            public CorpseSheet AddPrimaryEntity(string EntityBlueprint, int Weight)
                => AddEntity(EntityBlueprint, Weight, Relationship.PrimaryCorpse, new CorpseEntityPairEqualityComparer(CompareType.Blueprints, CompareType.Relationship));

            public CorpseSheet AddProductEntity(EntityWeight EntityWeight)
                => AddEntity(EntityWeight.Blueprint.Name, EntityWeight.Weight, Relationship.CorpseProduct, new CorpseEntityPairEqualityComparer(CompareType.Blueprints, CompareType.Relationship));

            public CorpseSheet AddCountsAsEntity(EntityWeight EntityWeight)
                => AddEntity(EntityWeight.Blueprint.Name, EntityWeight.Weight, Relationship.CorpseCountsAs, new CorpseEntityPairEqualityComparer(CompareType.Blueprints, CompareType.Relationship));

            public bool CorpseHasEntity(string Entity, bool CheckAll = true)
                => GetEntityWeights(!CheckAll).Any(c => c.Blueprint == Entity);

            public bool CorpseHasEntity(EntityBlueprint Entity, bool CheckAll = true)
                => CorpseHasEntity(Entity.Name, CheckAll);

            public bool CorpseHasEntityWithWeight(EntityWeight EntityWeight, bool CheckAll = true)
                => GetEntityWeights(!CheckAll).Any(c => c.Blueprint == EntityWeight.Blueprint && c.Weight == EntityWeight.Weight);

            public bool IsPrimaryForEntity(EntityBlueprint EntityBlueprint)
            {
                return GetPairs().Any(c => c.Entity == EntityBlueprint && c.Relationship == Relationship.PrimaryCorpse);
            }
        }
    }
}
