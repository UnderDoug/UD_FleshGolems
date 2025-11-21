using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

using HarmonyLib;

using XRL.World;

using UD_FleshGolems.Logging;

using Relationship = UD_FleshGolems.Capabilities.Necromancy.CorpseEntityPair.PairRelationship;
using SerializeField = UnityEngine.SerializeField;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public class CorpseEntityPair
        : IComposite
        , IEquatable<CorpseEntityPair>
        , IEquatable<BlueprintWeight>
        , IEquatable<KeyValuePair<BlueprintBox, Relationship>>
        , IComparable<CorpseEntityPair>
        , IComparable<Relationship>
        , IComparable<BlueprintWeight>
        , IComparable<KeyValuePair<BlueprintBox, Relationship>>
    {
        public enum CompareType : int
        {
            None = 0,
            Blueprints = 1,
            Weight = 2,
            Relationship = 4,
        }

        public class EqualityComparer : EqualityComparer<CorpseEntityPair>, IEqualityComparer<CorpseEntityPair>
        {
            [UD_FleshGolems_DebugRegistry]
            public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
            {
                foreach (MethodBase method in typeof(EqualityComparer).GetMethods())
                {
                    if (method.Name == nameof(EitherNull))
                    {
                        Registry.Register(method, false);
                    }
                    if (method.Name == nameof(Equals))
                    {
                        Registry.Register(method, false);
                    }
                    if (method.Name == nameof(GetHashCode))
                    {
                        Registry.Register(method, false);
                    }
                }

                Registry.Register(nameof(BlueprintEquals), false);
                Registry.Register(nameof(GetHashCodeBlueprint), false);

                Registry.Register(nameof(WeightEquals), false);
                Registry.Register(nameof(GetHashCodeWeight), false);

                Registry.Register(nameof(RelationshipEquals), false);
                Registry.Register(nameof(GetHashCodeRelationship), false);

                Registry.Register(nameof(EntirelyEquals), false);
                Registry.Register(nameof(GetHashCodeEntirely), false);

                return Registry;
            }

            private int Type;

            private List<CompareType> CompareTypes;

            private EqualityComparer()
                : base()
            {
                Type = (int)CompareType.None;
                CompareTypes = new();
            }

            public EqualityComparer(params CompareType?[] OrderedTypes)
                : this()
            {
                if (!OrderedTypes.IsNullOrEmpty()
                    && !OrderedTypes.All(ot => ot == null))
                {
                    foreach (CompareType? type in OrderedTypes)
                    {
                        if (type is CompareType notNullType)
                        {
                            CompareTypes.Add(notNullType);
                            Type ^= (int)notNullType;
                        }
                    }
                }
            }

            public override bool Equals(CorpseEntityPair x, CorpseEntityPair y)
            {
                Debug.Log(Debug.GetCallingTypeAndMethod(), CompareTypes?.Join() ?? CompareType.None.ToString(), Debug.LastIndent);
                if (EitherNull(x, y, out bool areEqual))
                {
                    return areEqual;
                }
                if (Type.HasAllTypes(CompareType.Blueprints, CompareType.Weight, CompareType.Relationship))
                {
                    return EntirelyEquals(x, y);
                }
                else
                if (Type.HasType(CompareType.Blueprints))
                {
                    if (Type.HasType(CompareType.Weight))
                    {
                        return WeightEquals(x, y);
                    }
                    else
                    if (Type.HasType(CompareType.Relationship))
                    {
                        return RelationshipEquals(x, y);
                    }
                    else
                    {
                        return BlueprintEquals(x, y);
                    }
                }
                else
                if (Type.HasAnyTypes(CompareType.Weight, CompareType.Relationship))
                {
                    return (Type.HasOnlyType(CompareType.Weight) && x?.Weight == y?.Weight)
                        || (Type.HasOnlyType(CompareType.Relationship) && x?.Relationship == y?.Relationship);
                }
                return x.Equals(y);
            }

            public override int GetHashCode(CorpseEntityPair obj)
            {
                Debug.Log(Debug.GetCallingTypeAndMethod(), CompareTypes?.Join() ?? CompareType.None.ToString(), Debug.LastIndent);
                if (obj == null)
                {
                    return default;
                }
                if (Type.HasAllTypes(CompareType.Blueprints, CompareType.Weight, CompareType.Relationship))
                {
                    return GetHashCodeEntirely(obj);
                }
                else
                if (Type.HasType(CompareType.Blueprints))
                {
                    if (Type.HasType(CompareType.Weight))
                    {
                        return GetHashCodeWeight(obj);
                    }
                    else
                    if (Type.HasType(CompareType.Relationship))
                    {
                        return GetHashCodeRelationship(obj);
                    }
                    else
                    {
                        return GetHashCodeBlueprint(obj);
                    }
                }
                else
                if (Type.HasAnyTypes(CompareType.Weight, CompareType.Relationship))
                {
                    if (Type.HasOnlyType(CompareType.Weight))
                    {
                        int? maybeWeight = obj?.Weight.GetHashCode();
                        return maybeWeight.GetValueOrDefault();
                    }
                    else
                    if (Type.HasOnlyType(CompareType.Relationship))
                    {
                        int? maybeRelationship = obj?.Relationship.GetHashCode();
                        return maybeRelationship.GetValueOrDefault();
                    }
                }
                return obj != null
                    ? obj.GetHashCode()
                    : default;
            }

            public bool EitherNull(CorpseEntityPair x, CorpseEntityPair y, out bool AreEqual)
            {
                // Debug.GetIndents(out Indents indent);
                //Debug.Log(Debug.GetCallingTypeAndMethod(), indent);
                AreEqual = (x is null) == (y is null);
                if (x is null || y is null)
                {
                    // Debug.Log("Either is indeed null...", Indent: indent[1]);
                    return true;
                }
                // Debug.Log("Neither is null...", Indent: indent[1]);
                // Debug.DiscardIndent();
                return false;
            }

            public bool BlueprintEquals(CorpseEntityPair x, CorpseEntityPair y)
            {
                Debug.Log(Debug.GetCallingTypeAndMethod(), Debug.LastIndent);
                return (EitherNull(x, y, out bool areEqual) && areEqual)
                    || (x?.Corpse == y?.Corpse && x?.Entity == y?.Entity);
            }

            public int GetHashCodeBlueprint(CorpseEntityPair obj)
            {
                int? maybeCorpse = obj?.Corpse?.GetHashCode();
                int? maybeEntity = obj?.Entity?.GetHashCode();
                return maybeCorpse.GetValueOrDefault() ^ maybeEntity.GetValueOrDefault();
            }

            public bool WeightEquals(CorpseEntityPair x, CorpseEntityPair y)
            {
                Debug.Log(Debug.GetCallingTypeAndMethod(), Debug.LastIndent);
                return (EitherNull(x, y, out bool areEqual) && areEqual)
                    || (BlueprintEquals(x, y) && x?.Weight == y?.Weight);
            }

            public int GetHashCodeWeight(CorpseEntityPair obj)
            {
                int? maybeWeight = obj?.Weight.GetHashCode();
                return GetHashCodeBlueprint(obj)
                    ^ maybeWeight.GetValueOrDefault();
            }

            public bool RelationshipEquals(CorpseEntityPair x, CorpseEntityPair y)
            {
                Debug.Log(Debug.GetCallingTypeAndMethod(), Debug.LastIndent);
                return (EitherNull(x, y, out bool areEqual) && areEqual)
                    || (BlueprintEquals(x, y) && x?.Relationship == y?.Relationship);
            }
            public int GetHashCodeRelationship(CorpseEntityPair obj)
            {
                int? maybeRelationship = obj?.Relationship.GetHashCode();
                return GetHashCodeBlueprint(obj)
                    ^ maybeRelationship.GetValueOrDefault();
            }

            public bool EntirelyEquals(CorpseEntityPair x, CorpseEntityPair y)
            {
                Debug.Log(Debug.GetCallingTypeAndMethod(), Debug.LastIndent);
                return (EitherNull(x, y, out bool areEqual) && areEqual)
                    || (BlueprintEquals(x, y)
                        && x?.Weight == y?.Weight
                        && x?.Relationship == y?.Relationship);
            }

            public int GetHashCodeEntirely(CorpseEntityPair obj)
            {
                int? maybeWeight = obj?.Relationship.GetHashCode();
                int? maybeRelationship = obj?.Relationship.GetHashCode();
                return GetHashCodeBlueprint(obj)
                    ^ maybeWeight.GetValueOrDefault()
                    ^ maybeRelationship.GetValueOrDefault();
            }

        }

        public class OrderedComparer : Comparer<CorpseEntityPair>, IComparer<CorpseEntityPair>
        {
            [UD_FleshGolems_DebugRegistry]
            public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
            {
                Registry.Register(nameof(IsNotComparable), false);

                Registry.Register(nameof(CompareWeight), false);
                Registry.Register(nameof(CompareRelationship), false);

                Registry.Register(nameof(CompareWeightFirst), false);
                Registry.Register(nameof(CompareRelationshipFirst), false);

                return Registry;
            }
            private int Type;

            private List<CompareType> TypeOrder;

            private OrderedComparer()
                : base()
            {
                Type = (int)CompareType.None;
                TypeOrder = new();
            }

            public OrderedComparer(params CompareType?[] OrderedTypes)
                : this()
            {
                if (!OrderedTypes.IsNullOrEmpty()
                    && !OrderedTypes.All(ot => ot == null))
                {
                    foreach (CompareType? type in OrderedTypes)
                    {
                        if (type is CompareType notNullType)
                        {
                            TypeOrder.Add(notNullType);
                            Type ^= (int)notNullType;
                        }
                    }
                }
            }

            public override int Compare(CorpseEntityPair x, CorpseEntityPair y)
            {
                Debug.Log(Debug.GetCallingTypeAndMethod(), TypeOrder?.Join() ?? CompareType.None.ToString(), Debug.LastIndent);
                if (IsNotComparable(x, y, out int Comparison))
                {
                    return Comparison;
                }
                if (Type.HasAllTypes(CompareType.Weight, CompareType.Relationship))
                {
                    if (TypeOrder.IndexOf(CompareType.Weight) < TypeOrder.IndexOf(CompareType.Relationship))
                    {
                        return CompareWeightFirst(x, y);
                    }
                    else
                    if (TypeOrder.IndexOf(CompareType.Relationship) < TypeOrder.IndexOf(CompareType.Weight))
                    {
                        return CompareRelationshipFirst(x, y);
                    }
                }
                else
                if (Type.HasType(CompareType.Weight))
                {
                    return CompareWeight(x, y);
                }
                else
                if (Type.HasType(CompareType.Relationship))
                {
                    return CompareRelationship(x, y);
                }
                return IsNotComparable(x, y, out int comparison) ? comparison : 0;
            }

            public static bool IsNotComparable(CorpseEntityPair x, CorpseEntityPair y, out int Comparison)
            {
                Comparison = 0;
                if (x != null && y == null)
                {
                    Comparison = 1;
                    return true;
                }
                if (x == null && y != null)
                {
                    Comparison = -1;
                    return true;
                }
                if ((x == null) == (y == null)
                    || x.Corpse != y.Corpse
                    || x.Entity != y.Entity)
                {
                    Comparison = 0;
                    return true;
                }
                return false;
            }

            public int CompareWeight(CorpseEntityPair x, CorpseEntityPair y)
                => IsNotComparable(x, y, out int comparison)
                ? comparison
                : x.Weight.CompareTo(y.Weight);

            public int CompareRelationship(CorpseEntityPair x, CorpseEntityPair y)
                => IsNotComparable(x, y, out int comparison)
                ? comparison
                : x.Relationship.CompareTo(y.Relationship);

            public int CompareWeightFirst(CorpseEntityPair x, CorpseEntityPair y)
            {
                if (IsNotComparable(x, y, out int comparison))
                {
                    return comparison;
                }
                if (x.Weight.CompareTo(y.Weight) is int weightCompariosn
                    && weightCompariosn != 0)
                {
                    return weightCompariosn;
                }
                return x.Relationship.CompareTo(y.Relationship);
            }

            public int CompareRelationshipFirst(CorpseEntityPair x, CorpseEntityPair y)
            {
                if (IsNotComparable(x, y, out int comparison))
                {
                    return comparison;
                }
                if (x.Relationship.CompareTo(y.Relationship) is int relationshipComparison
                    && relationshipComparison != 0)
                {
                    return relationshipComparison;
                }
                return x.Weight.CompareTo(y.Weight);
            }
        }

        public enum PairRelationship : int
        {
            None = int.MaxValue,
            PrimaryCorpse = 0,
            InheritedCorpse = 1,
            CorpseProduct = 2,
            CorpseCountsAs = 3,
        }

        public BlueprintWeight this[[AllowNull] BlueprintBox Reference]
        {
            get
            {
                if (Corpse == Reference)
                {
                    return GetEntityWeight();
                }
                else
                if (Entity == Reference)
                {
                    return GetCorpseWeight();
                }
                return null;
            }
            private set
            {
                if (value.Blueprint is not null)
                {
                    if (Corpse == Reference)
                    {
                        Entity = value.Blueprint as EntityBlueprint;
                    }
                    else
                    if (Entity == Reference)
                    {
                        Corpse = value.Blueprint as CorpseBlueprint;
                    }
                    Weight = value.Weight;
                }
            }
        }

        public CorpseBlueprint Corpse;
        public EntityBlueprint Entity;
        public int Weight;
        public Relationship Relationship { get; private set; }

        private CorpseEntityPair()
        {
            Corpse = null;
            Entity = null;
            Weight = 0;
            Relationship = Relationship.None;
        }

        public CorpseEntityPair(
            CorpseBlueprint Corpse,
            EntityBlueprint Entity,
            int Weight,
            Relationship Relationship)
            : this()
        {
            this.Corpse = Corpse;
            this.Entity = Entity;
            this.Weight = Weight;
            this.Relationship = Relationship;
        }

        public CorpseEntityPair(CorpseEntityPair CorpseEntityPair)
            : this(
                  Corpse: CorpseEntityPair.Corpse,
                  Entity: CorpseEntityPair.Entity,
                  Weight: CorpseEntityPair.Weight,
                  Relationship: CorpseEntityPair.Relationship)
        {
        }

        public GameObjectBlueprint GetCorpseGameObjectBlueprint()
        {
            return Corpse.GetGameObjectBlueprint();
        }
        public GameObjectBlueprint GetEntityGameObjectBlueprint()
        {
            return Entity.GetGameObjectBlueprint();
        }

        public CorpseWeight GetCorpseWeight()
        {
            if (Corpse is null)
            {
                return null;
            }
            return new CorpseWeight(Corpse, Weight);
        }
        public EntityWeight GetEntityWeight()
        {
            if (Entity is null)
            {
                return null;
            }
            return new EntityWeight(Entity, Weight);
        }

        public override string ToString()
        {
            return Corpse + ":" + Entity + "::" + Weight + ";" + Relationship;
        }

        public void Deconstruct(out CorpseBlueprint Corpse, out EntityBlueprint Entity, out int Weight, out Relationship Relationship)
        {
            Corpse = this.Corpse;
            Entity = this.Entity;
            Weight = this.Weight;
            Relationship = this.Relationship;
        }

        // Equality
        public override bool Equals(object obj = null)
        {
            if (obj == null)
            {
                return false;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode() 
            => Corpse.GetHashCode()
            ^ Entity.GetHashCode()
            ^ Weight.GetHashCode()
            ^ Relationship.GetHashCode();

        public bool Equals(CorpseEntityPair other)
        {
            return other is not null
                && Corpse.Equals(other.Corpse)
                && Entity.Equals(other.Entity)
                && Weight.Equals(other.Weight)
                && Relationship.Equals(other.Relationship);
        }
        public bool Equals(BlueprintWeight other)
        {
            return other is not null
                && Corpse.Equals(other.Blueprint)
                && Weight.Equals(other.Weight);
        }
        public bool Equals(KeyValuePair<BlueprintBox, Relationship> other)
        {
            return other is KeyValuePair<BlueprintBox, Relationship> otherKVP
                && Corpse.Equals(otherKVP.Key)
                && Relationship.Equals(other.Value);
        }

        // Comparison
        public int CompareTo(CorpseEntityPair other)
        {
            if (other is null)
            {
                return 1;
            }
            if (Entity != other.Entity || Corpse != other.Corpse)
            {
                return 0;
            }
            if (!Relationship.Equals(other.Relationship))
            {
                return -Relationship.CompareTo(other.Relationship);
            }
            return Weight.CompareTo(other.Weight);
        }
        public int CompareTo(Relationship other)
        {
            return -Relationship.CompareTo(other);
        }
        public int CompareTo(BlueprintWeight other)
        {
            if (other.Blueprint is not null)
            {
                return 1;
            }
            if (!Corpse.Equals(other.Blueprint))
            {
                return 0;
            }
            return Weight.CompareTo(other.Weight);
        }
        public int CompareTo(KeyValuePair<BlueprintBox, Relationship> other)
        {
            if (other.Key is not null)
            {
                return 1;
            }
            if (!Corpse.Equals(other.Key))
            {
                return 0;
            }
            return Relationship.CompareTo(other.Value);
        }
    }
}
