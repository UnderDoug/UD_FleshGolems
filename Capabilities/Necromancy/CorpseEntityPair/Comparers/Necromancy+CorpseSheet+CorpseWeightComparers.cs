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
using XRL;
using XRL.World.Parts;

namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        public enum CompareType : int
        {
            None = 0,
            Blueprints = 1,
            Weight = 2,
            Relationship = 4,
        }

        private static bool HasType(this int Type, CompareType CompareType)
        {
            if (!Type.HasBit((int)CompareType))
            {
                return false;
            }
            return true;
        }

        private static bool HasAllTypes(this int Type, params CompareType[] CompareType)
        {
            if (CompareType.IsNullOrEmpty())
            {
                return false;
            }
            foreach (CompareType type in CompareType)
            {
                if (Type.HasType(type))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool HasAnyTypes(this int Type, params CompareType[] CompareType)
        {
            if (CompareType.IsNullOrEmpty())
            {
                return false;
            }
            foreach (CompareType type in CompareType)
            {
                if (Type.HasType(type))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasOnlyType(this int Type, CompareType CompareType)
        {
            return Type == (int)CompareType;
        }

        public partial class CorpseSheet : IComposite
        {
            public class CorpseEntityPairEqualityComparer : EqualityComparer<CorpseEntityPair>, IEqualityComparer<CorpseEntityPair>
            {
                [UD_FleshGolems_DebugRegistry]
                public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
                {
                    Registry.Register(nameof(EitherNull), false);

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

                private CorpseEntityPairEqualityComparer()
                    : base()
                {
                    Type = (int)CompareType.None;
                    CompareTypes = new();
                }

                public CorpseEntityPairEqualityComparer(params CompareType?[] OrderedTypes)
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

                private bool EitherNull(CorpseEntityPair x, CorpseEntityPair y, out bool AreEqual)
                {
                    Debug.GetIndents(out Indents indent);
                    Debug.Log(Debug.GetCallingTypeAndMethod(), indent);
                    AreEqual = (x is null) == (y is null);
                    if (x is null || y is null)
                    {
                        Debug.Log("Either is indeed null...", Indent: indent[1]);
                        return true;
                    }
                    Debug.Log("Neither is null...", Indent: indent[1]);
                    Debug.SetIndent(indent[0]);
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

            public class CorpseEntityPairComparer : Comparer<CorpseEntityPair>, IComparer<CorpseEntityPair>
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

                private CorpseEntityPairComparer()
                    : base()
                {
                    Type = (int)CompareType.None;
                    TypeOrder = new();
                }

                public CorpseEntityPairComparer(params CompareType?[] OrderedTypes)
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
                    /*
                    if (x == null && y == null)
                    {
                        return 0;
                    }
                    if (x != null && y != null)
                    {
                        return 1;
                    }
                    if (x == null && y != null)
                    {
                        return -1;
                    }
                    */
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

            public class BlueprintWeightComparer : Comparer<BlueprintWeight>, IComparer<BlueprintWeight>
            {
                public override int Compare(BlueprintWeight x, BlueprintWeight y)
                {
                    if (x.Equals(null) && !y.Equals(null))
                    {
                        return -1;
                    }
                    if (!x.Equals(null) && y.Equals(null))
                    {
                        return 1;
                    }
                    if (x.Equals(null) == y.Equals(null)
                        || x.GetType() != y.GetType()
                        || x.Blueprint != y.Blueprint)
                    {
                        return 0;
                    }
                    return x.Weight.CompareTo(y.Weight);
                }
            }
        }
    }
}
