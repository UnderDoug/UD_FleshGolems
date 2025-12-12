using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using XRL;
using XRL.World;
using XRL.World.Text.Attributes;
using XRL.World.Text.Delegates;
using XRL.Language;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Parts.VengeanceHelpers;
using Debug = UD_FleshGolems.Logging.Debug;

using static UD_FleshGolems.Const;

namespace UD_FleshGolems
{
    [HasVariableReplacer]
    public static class VariableReplacers
    {
        [UD_FleshGolems_DebugRegistry]
        public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
        {
            Type thisType = typeof(VariableReplacers);
            Registry.Register(thisType.GetMethod(nameof(UD_xTag)), false);
            Registry.Register(thisType.GetMethod(nameof(UD_xTagSingle)), false);
            Registry.Register(thisType.GetMethod(nameof(UD_xTagMulti)), false);
            Registry.Register(thisType.GetMethod(nameof(UD_xTagMultiU)), false);
            Registry.Register(thisType.GetMethod(nameof(UD_xTagMultiU)), false);
            return Registry;
        }

        /* 
         * 
         * Variable (no object)
         * 
         */
        [VariableReplacer]
        public static string ud_nbsp(DelegateContext Context)
        {
            string nbsp = "\xFF";
            string output = nbsp;
            if (!Context.Parameters.IsNullOrEmpty()
                && int.TryParse(Context.Parameters[0], out int count))
            {
                for (int i = 1; i < count; i++)
                {
                    output += nbsp;
                }
            }
            return output;
        }

        [VariableReplacer]
        public static string ud_weird(DelegateContext Context)
        {
            string output = null;
            if (!Context.Parameters.IsNullOrEmpty())
            {
                if (Context.Parameters.Count > 1)
                {
                    output = "{{" + Context.Parameters[0] + "|";
                    for (int i = 1; i < Context.Parameters.Count; i++)
                    {
                        if (i > 1)
                        {
                            output += " ";
                        }
                        output += TextFilters.Weird(Context.Parameters[i]);
                    }
                    output += "}}";
                }
                else
                {
                    return TextFilters.Weird(Context.Parameters[0]);
                }
            }
            return output;
        }

        /* 
         * 
         * Variable Object
         * 
         */
        [VariableObjectReplacer]
        public static string UD_NotableFeature(DelegateContext Context)
        {
            string output = KillerDetails.GetNotableFeature(Context.Target);
            return Context.Capitalize
                ? output?.Capitalize()
                : output;
        }

        [VariableObjectReplacer]
        public static string UD_CreatureType(DelegateContext Context)
        {
            string article = null;
            string afterArticleAdjectives = null;
            if (Context.Parameters is List<string> parameters
                && !parameters.IsNullOrEmpty())
            {
                if (parameters[0].EqualsAnyNoCase("a", "an"))
                    article = (Context.Target?.a ?? "a") + " ";
                else
                if (parameters[0].EqualsNoCase("the"))
                    article = (Context.Target?.the ?? "the") + " ";
                else
                if (parameters[0].EqualsAnyNoCase("this", "these"))
                    article = (Context.Target?.indicativeProximal ?? "this") + " ";
                else
                if (parameters[0].EqualsAnyNoCase("that", "those"))
                    article = (Context.Target?.indicativeDistal ?? "that") + " ";
                else
                    article = parameters[0] + " ";

                if (parameters.Count > 1
                    && !parameters[1].IsNullOrEmpty())
                    afterArticleAdjectives = parameters[1] + " ";
            }
            string creatureType = Context.Target?.GetCreatureType() ?? "hideous specimen";
            string output = article + afterArticleAdjectives + creatureType;
            return Context.Capitalize
                ? output.Capitalize()
                : output;
        }

        [VariableObjectReplacer]
        public static string UD_DeathDescription(DelegateContext Context)
        {
            string deathDescription = "=subject.verb:were:afterpronoun= killed to death";
            if (Context.Target?.GetKillerDetails() is KillerDetails killerDetails)
                deathDescription = killerDetails.DeathDescription;

            string output = deathDescription
                ?.StartReplace()
                ?.AddObject(Context.Target)
                ?.ToString();

            return Context.Capitalize
                ? output.Capitalize()
                : output;
        }

        [VariableObjectReplacer]
        public static string UD_xTagSingle(DelegateContext Context)
        {
            using Indent indent = new();
            Debug.LogMethod(indent[1]);
            string output = null;
            if (!Context.Parameters.IsNullOrEmpty()
                && Context.Parameters.Count > 1
                && Context.Target is GameObject target
                && target.GetxTag(Context.Parameters[0], Context.Parameters[1]) is string xtag
                && xtag.CachedCommaExpansion().GetRandomElementCosmetic() is string tagValue)
            {
                output = tagValue;
                if (Context.Capitalize)
                {
                    output = output.Capitalize();
                }
            }
            return output;
        }

        [VariableObjectReplacer]
        public static string UD_xTagMulti(DelegateContext Context)
        {
            using Indent indent = new();
            Debug.LogMethod(indent[1]);
            string output = null;
            if (!Context.Parameters.IsNullOrEmpty()
                && Context.Parameters.Count > 2
                && Context.Target is GameObject target
                && int.TryParse(Context.Parameters[0], out int count)
                && target.GetxTag(Context.Parameters[1], Context.Parameters[2]) is string multixTag
                && multixTag.CachedCommaExpansion() is List<string> multiTagList)
            {
                List<string> andList = new();
                for (int i = 0; i < count; i++)
                {
                    if (multiTagList.GetRandomElementCosmetic() is string entry)
                    {
                        andList.Add(entry);
                    }
                    else
                    {
                        break;
                    }
                }
                output = Grammar.MakeAndList(andList);
                if (andList.Count > 2 && !output.Contains(", and "))
                {
                    output = output.Replace(" and ", ", and ");
                }
                if (Context.Capitalize)
                {
                    output = output.Capitalize();
                }
            }
            return output;
        }

        [VariableObjectReplacer]
        public static string UD_xTagMultiU(DelegateContext Context)
        {
            using Indent indent = new();
            Debug.LogMethod(indent[1]);
            string output = null;
            if (!Context.Parameters.IsNullOrEmpty()
                && Context.Parameters.Count > 3
                && Context.Target is GameObject target
                && int.TryParse(Context.Parameters[0], out int count)
                && Context.Parameters[1] is string unique
                && (unique.EqualsNoCase("U") || unique.EqualsNoCase("Unique"))
                && target.GetxTag(Context.Parameters[2], Context.Parameters[3]) is string multixTag
                && multixTag.CachedCommaExpansion() is List<string> multiTagList)
            {
                List<string> andList = new();
                Debug.Log(nameof(multiTagList) + " Entries:", Indent: indent[2]);
                for (int i = 0; i < count; i++)
                {
                    if (multiTagList.GetRandomElementCosmeticExcluding(Exclude: s => andList.Contains(s)) is string entry)
                    {
                        Debug.Log(entry, Indent: indent[3]);
                        andList.Add(entry);
                    }
                    else
                    {
                        break;
                    }
                }
                output = Grammar.MakeAndList(andList);
                if (andList.Count > 2 && !output.Contains(", and "))
                {
                    output = output.Replace(" and ", ", and ");
                }
                if (Context.Capitalize)
                {
                    output = output.Capitalize();
                }
            }
            return output;
        }

        [VariableObjectReplacer]
        public static string UD_xTag(DelegateContext Context)
        {
            using Indent indent = new();
            Debug.LogCaller(indent[1],
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Context.Target), Context?.Target?.DebugName ?? NULL),
                });
            string parameters = null;
            foreach (string parameter in Context.Parameters)
            {
                if (!parameters.IsNullOrEmpty())
                {
                    parameters += ", ";
                }
                parameters += parameter;
            }
            Debug.Log(nameof(Context.Parameters), parameters, indent[2]);
            string output = null;
            if (!Context.Parameters.IsNullOrEmpty()
                && Context.Target is GameObject target)
            {
                switch (Context.Parameters.Count)
                {
                    case 0:
                    case 1:
                        Debug.Log("Uh-oh!", Indent: indent[3]);
                        break;

                    case 2:
                        output = UD_xTagSingle(Context);
                        Debug.Log(nameof(UD_xTagSingle), Indent: indent[3]);
                        break;

                    case 3:
                        output = UD_xTagMulti(Context);
                        Debug.Log(nameof(UD_xTagMulti), Indent: indent[3]);
                        break;

                    default: // 4
                        output = UD_xTagMultiU(Context);
                        Debug.Log(nameof(UD_xTagMultiU), Indent: indent[3]);
                        break;
                }
                if (Context.Capitalize)
                {
                    Debug.Log(nameof(Context.Capitalize), Indent: indent[3]);
                    output = output.Capitalize();
                }
            }
            return output;
        }
    }
}
