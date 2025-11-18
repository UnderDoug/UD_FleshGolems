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
        public partial class CorpseSheet : IComposite
        {
            public class CorpseEntityPairEqualBlueprints : EqualityComparer<CorpseEntityPair>, IEqualityComparer<CorpseEntityPair>
            {
                public override bool Equals(CorpseEntityPair x, CorpseEntityPair y) => x.Corpse != y.Corpse || x.Entity != y.Entity;

                public override int GetHashCode(CorpseEntityPair obj) => obj.Corpse.GetHashCode() ^ obj.Entity.GetHashCode();
            }
            public class CorpseEntityPairEqualBlueprintsAndWeight : EqualityComparer<CorpseEntityPair>, IEqualityComparer<CorpseEntityPair>
            {
                public override bool Equals(CorpseEntityPair x, CorpseEntityPair y)
                    => new CorpseEntityPairEqualBlueprints().Equals(x, y) 
                    && x.Weight == y.Weight;

                public override int GetHashCode(CorpseEntityPair obj)
                    => new CorpseEntityPairEqualBlueprints().GetHashCode(obj)
                    ^ obj.Weight.GetHashCode();
            }
            public class CorpseEntityPairEqualBlueprintsAndRelationship : EqualityComparer<CorpseEntityPair>, IEqualityComparer<CorpseEntityPair>
            {
                public override bool Equals(CorpseEntityPair x, CorpseEntityPair y)
                    => new CorpseEntityPairEqualBlueprints().Equals(x, y) 
                    && x.Relationship == y.Relationship;

                public override int GetHashCode(CorpseEntityPair obj)
                    => new CorpseEntityPairEqualBlueprints().GetHashCode(obj)
                    ^ obj.Relationship.GetHashCode();
            }
            public class CorpseEntityPairEqualEntirely : EqualityComparer<CorpseEntityPair>, IEqualityComparer<CorpseEntityPair>
            {
                public override bool Equals(CorpseEntityPair x, CorpseEntityPair y)
                    => new CorpseEntityPairEqualBlueprints().Equals(x, y) 
                    && x.Weight == y.Weight
                    && x.Relationship == y.Relationship;

                public override int GetHashCode(CorpseEntityPair obj)
                    => new CorpseEntityPairEqualBlueprints().GetHashCode(obj)
                    ^ obj.Weight.GetHashCode()
                    ^ obj.Relationship.GetHashCode();
            }

            public class CorpseEntityPairCompareBlueprintsOnly : Comparer<CorpseEntityPair>, IComparer<CorpseEntityPair>
            {
                public static bool TryCompare(CorpseEntityPair x, CorpseEntityPair y, out int Comparison)
                {
                    Comparison = 0;
                    if (x.Equals(null) && !y.Equals(null))
                    {
                        Comparison = -1;
                        return true;
                    }
                    if (!x.Equals(null) && y.Equals(null))
                    {
                        Comparison = 1;
                        return true;
                    }
                    if (x.Equals(null) == y.Equals(null)
                        || (x.Corpse != y.Corpse || x.Entity != y.Entity))
                    {
                        Comparison = 0;
                        return true;
                    }
                    return false;
                }
                public override int Compare(CorpseEntityPair x, CorpseEntityPair y)
                {
                    if (x.Equals(null) && !y.Equals(null))
                    {
                        return -1;
                    }
                    if (!x.Equals(null) && y.Equals(null))
                    {
                        return 1;
                    }
                    return 0;
                }
            }
            public class CorpseEntityPairCompareWeightOnly : Comparer<CorpseEntityPair>, IComparer<CorpseEntityPair>
            {
                public override int Compare(CorpseEntityPair x, CorpseEntityPair y)
                    => CorpseEntityPairCompareBlueprintsOnly.TryCompare(x, y, out int comparison) 
                    ? comparison 
                    : x.Weight.CompareTo(y.Weight);
            }
            public class CorpseEntityPairCompareRelationshipOnly : Comparer<CorpseEntityPair>, IComparer<CorpseEntityPair>
            {
                public override int Compare(CorpseEntityPair x, CorpseEntityPair y)
                    => CorpseEntityPairCompareBlueprintsOnly.TryCompare(x, y, out int comparison)
                    ? comparison
                    : x.Relationship.CompareTo(y.Relationship);
            }
            public class CorpseEntityPairCompareWeightFirst : Comparer<CorpseEntityPair>, IComparer<CorpseEntityPair>
            {
                public override int Compare(CorpseEntityPair x, CorpseEntityPair y)
                {
                    if (CorpseEntityPairCompareBlueprintsOnly.TryCompare(x, y, out int comparison))
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
            }
            public class CorpseEntityPairCompareRelationshipFirst : Comparer<CorpseEntityPair>, IComparer<CorpseEntityPair>
            {
                public override int Compare(CorpseEntityPair x, CorpseEntityPair y)
                {
                    if (CorpseEntityPairCompareBlueprintsOnly.TryCompare(x, y, out int comparison))
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

            public class BlueprintWrapperWeightComparer : Comparer<BlueprintWeight>, IComparer<BlueprintWeight>
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
