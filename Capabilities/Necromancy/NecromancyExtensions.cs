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

using SerializeField = UnityEngine.SerializeField;

using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;
using Relationship = UD_FleshGolems.Capabilities.Necromancy.CorpseEntityPair.PairRelationship;
using static UD_FleshGolems.Capabilities.Necromancy.CorpseEntityPair;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    public static class NecromancyExtensions
    {
        public static bool HasType(this int Type, CompareType CompareType)
        {
            if (!Type.HasBit((int)CompareType))
            {
                return false;
            }
            return true;
        }

        public static bool HasAllTypes(this int Type, params CompareType[] CompareType)
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

        public static bool HasAnyTypes(this int Type, params CompareType[] CompareType)
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

        public static bool HasOnlyType(this int Type, CompareType CompareType)
        {
            return Type == (int)CompareType;
        }
    }
}
