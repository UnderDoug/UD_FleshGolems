using System;
using System.Collections.Generic;
using System.Linq;

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
            [ModSensitiveStaticCache(CreateEmptyInstance = false)]
            [GameBasedStaticCache(ClearInstance = false)]
            public static StringMap<CorpseSheet> CorpseSheets = new();

            [Serializable] public abstract partial class BlueprintWrapper : IComposite { }
            [Serializable] public partial class CorpseBlueprint : BlueprintWrapper, IComposite { }
            [Serializable] public partial class CorpseProduct : CorpseBlueprint, IComposite { }
            [Serializable] public partial class EntityBlueprint : BlueprintWrapper, IComposite { }
            [Serializable] public partial class CorpseEntityPair : IComposite { }
            [Serializable] public abstract partial class BlueprintWeight : IComposite { }
            [Serializable] public partial class CorpseWeight : BlueprintWeight, IComposite { }
            [Serializable] public partial class EntityWeight : BlueprintWeight, IComposite { }

            [SerializeField]
            private CorpseBlueprint Corpse;

            [SerializeField]
            private List<CorpseEntityPair> Pairs;

            [SerializeField]
            private bool _IsCorpseProduct;

            public bool IsCorpseProduct
            {
                get => _IsCorpseProduct = Corpse.GetType().InheritsFrom(typeof(CorpseProduct));
                private set
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

            private CorpseSheet()
            {
                _IsCorpseProduct = false;
                Corpse = null;
                Pairs = new();
            }

            public CorpseSheet(CorpseBlueprint Corpse)
                : this()
            {
                this.Corpse = Corpse;
            }

            public static bool TryGetCorpseSheet(string Corpse, out CorpseSheet CorpseSheet)
            {
                CorpseSheets ??= new();
                if (CorpseSheets[Corpse] is CorpseSheet cachedCorpseSheet)
                {
                    CorpseSheet = cachedCorpseSheet;
                    return true;
                }
                CorpseSheet = null;
                return false;
            }

            public void Deconstruct(out string Corpse) => Corpse = this.Corpse.Blueprint;
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
            public Dictionary<BlueprintWrapper, int> GetWeightedEntityList()
            {
                Dictionary<BlueprintWrapper, int> weightedList= new();
                foreach (CorpseEntityPair weightedPairs in Pairs)
                {
                    AccumulateBlueprintWeight(ref weightedList, Corpse, weightedPairs);
                }
                return weightedList;
            }
            private static void AccumulateBlueprintWeight(
                ref Dictionary<BlueprintWrapper, int> WeightedList,
                BlueprintWrapper Key,
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
                => Collection.AddUnique(
                    Item: Pair,
                    EqualityComparer: new CorpseEntityPairEqualBlueprints(),
                    Comparer: new CorpseEntityPairCompareRelationshipFirst());

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
                    Entity: new EntityBlueprint(Blueprint),
                    Weight: Weight,
                    Relationship: Relationship);

            private List<CorpseEntityPair> GetInheritedCorpses(CorpseEntityPair CorpseEntityPair)
            {
                List<CorpseEntityPair> inheritedCorpses = new();
                GameObjectBlueprint inheritedCorpse = CorpseEntityPair?.GetCorpseGameObjectBlueprint()?.Inherits?.GetGameObjectBlueprint();
                while (inheritedCorpse != null
                    && inheritedCorpse.IsCorpse()
                    && AddUniquePair(
                        Collection: inheritedCorpses,
                        Pair: new(
                            Corpse: new CorpseBlueprint(inheritedCorpse),
                            Entity: CorpseEntityPair.Entity,
                            Weight: CorpseEntityPair.Weight,
                            Relationship: Relationship.InheritedCorpse))
                    )
                {
                    inheritedCorpse = inheritedCorpse.Inherits?.GetGameObjectBlueprint();
                }
                return inheritedCorpses;
            }
            private bool AddInheritedCorpses(List<CorpseEntityPair> InheritedCorpseEntityPairs)
            {
                bool any = false;
                if (!InheritedCorpseEntityPairs.IsNullOrEmpty())
                {
                    Pairs ??= new();
                    foreach (CorpseEntityPair inheritedCorpseEntityPair in InheritedCorpseEntityPairs)
                    {
                        any = AddUniquePair(Pairs, inheritedCorpseEntityPair) || any;
                    }
                }
                return any;
            }
            public bool AddInheritedCorpses(CorpseEntityPair CorpseEntityPair)
                => AddInheritedCorpses(GetInheritedCorpses(CorpseEntityPair));

            public IReadOnlyList<EntityWeight> GetEntites(bool ExcludeProducts, Predicate<GameObjectBlueprint> Filter = null)
                => GetPairs(cep => !ExcludeProducts || cep.Relationship != Relationship.CorpseProduct, Filter)
                    .ToList()
                    .ConvertAll(cep => (EntityWeight)cep);

            public IReadOnlyList<EntityWeight> GetEntites(Predicate<GameObjectBlueprint> Filter = null)
                => GetEntites(false, Filter);

            public CorpseSheet AddPair(CorpseEntityPair Pair)
            {
                Pairs ??= new();
                Pairs.AddUnique(Pair);
                if (GetInheritedCorpses(Pair) is List<CorpseEntityPair> inheritedCorpses
                    && !inheritedCorpses.IsNullOrEmpty())
                {
                    Pairs.AddRange(inheritedCorpses);
                }
                return this;
            }
            public CorpseSheet AddEntity(string EntityBlueprint, int Weight, Relationship Relationship)
                => AddPair(
                    new CorpseEntityPair(
                        Corpse: Corpse, 
                        Entity: new EntityBlueprint(EntityBlueprint),
                        Weight: Weight,
                        Relationship: Relationship));
            public CorpseSheet AddEntity(GameObjectBlueprint EntityBlueprint, int Weight, Relationship Relationship)
                => AddEntity(EntityBlueprint.Name, Weight, Relationship);

            public CorpseSheet AddPrimaryEntity(GameObjectBlueprint EntityBlueprint, int Weight)
                => AddEntity(EntityBlueprint, Weight, Relationship.PrimaryCorpse);

            public CorpseSheet AddPrimaryEntity(string EntityBlueprint, int Weight)
                => AddEntity(EntityBlueprint, Weight, Relationship.PrimaryCorpse);

            public bool CorpseHasEntity(string Entity, bool CheckAll = true)
                => GetEntites(!CheckAll).Any(c => c.Blueprint == Entity);

            public bool CorpseHasEntity(EntityBlueprint Entity, bool CheckAll = true)
                => CorpseHasEntity(Entity.Blueprint, CheckAll);

            public bool CorpseHasEntityWithWeight(EntityWeight EntityWeight, bool CheckAll = true)
                => GetEntites(!CheckAll).Any(c => c.Blueprint == EntityWeight.Blueprint && c.Weight == EntityWeight.Weight);

            public bool IsPrimaryForEntity(EntityBlueprint EntityBlueprint)
            {
                return GetPairs().Any(c => c.Entity == EntityBlueprint && c.Relationship == Relationship.PrimaryCorpse);
            }
        }
    }
}
