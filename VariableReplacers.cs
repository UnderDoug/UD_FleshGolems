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
using System.Globalization;
using Qud.API;
using XRL.World.Parts;

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

        private static bool? StoredAllowSecondPerson = null;
        [VariableReplacer("no2nd")]
        public static string UD_no2nd(DelegateContext Context)
        {
            StoredAllowSecondPerson ??= Grammar.AllowSecondPerson;
            Grammar.AllowSecondPerson = false;
            return null;
        }

        [VariableReplacer("no2nd.restore")]
        public static string UD_no2nd_restore(DelegateContext Context)
        {
            if (StoredAllowSecondPerson is bool storedAllowSecondPerson)
            {
                Grammar.AllowSecondPerson = storedAllowSecondPerson;
            }
            StoredAllowSecondPerson = null;
            return null;
        }

        [VariableReplacer]
        public static string UD_RandomItem(DelegateContext Context)
        {
            string inherits = null;
            string hasTags = null;
            if (Context.Parameters is List<string> parameters
                && parameters.Count > 1)
            {
                switch (parameters[0])
                {
                    case "inherits":
                        inherits = parameters[1];
                        break;
                    case "hasTags":
                        hasTags = parameters[1];
                        break;
                }
            }
            bool matchesParameters(GameObjectBlueprint Model)
            {
                if (!inherits.IsNullOrEmpty() && !Model.InheritsFrom(inherits))
                    return false;
                if (!hasTags.IsNullOrEmpty())
                    foreach (string tag in hasTags.CachedCommaExpansion())
                        if (!Model.HasTagOrProperty(tag))
                            return false;
                return true;
            }
            GameObject itemSample = EncountersAPI.GetAnItem(matchesParameters);
            string itemName = itemSample?.GetReferenceDisplayName(Short: true);
            itemSample?.Obliterate();
            return itemName.ContextCapitalize(Context);
        }
        [VariableReplacer]
        public static string UD_RandomItems(DelegateContext Context)
        {
            GameObject itemSample = EncountersAPI.GetAnItem();
            string itemName = itemSample?.GetReferenceDisplayName(Short: true);
            if (!itemSample.IsPlural)
                itemName = itemName.Pluralize();
            itemSample?.Obliterate();
            return itemName.ContextCapitalize(Context);
        }

        /* 
         * 
         * Variable Object
         * 
         */

        public static void ParseDeathMemoryContextParameters(
            DelegateContext Context,
            out string Article,
            out string AfterArticleAdjectives)
        {
            Article = null;
            AfterArticleAdjectives = null;
            if (Context.Parameters is List<string> parameters
                && !parameters.IsNullOrEmpty())
            {
                if (parameters[0].EqualsAnyNoCase("a", "an"))
                    Article = (Context.Target?.a ?? "a") + " ";
                else
                if (parameters[0].EqualsNoCase("the"))
                    Article = (Context.Target?.the ?? "the") + " ";
                else
                if (parameters[0].EqualsAnyNoCase("this", "these"))
                    Article = (Context.Target?.indicativeProximal ?? "this") + " ";
                else
                if (parameters[0].EqualsAnyNoCase("that", "those"))
                    Article = (Context.Target?.indicativeDistal ?? "that") + " ";
                else
                    Article = parameters[0] + " ";

                if (parameters.Count > 1
                    && !parameters[1].IsNullOrEmpty())
                    AfterArticleAdjectives = parameters[1] + " ";
            }
        }
        public static string ProcessDeathMemoryReplacerResult(string Result, DelegateContext Context)
        {
            ParseDeathMemoryContextParameters(Context, out string article, out string afterArticleAdjectives);
            string output = article + afterArticleAdjectives + Result;
            return Context.Capitalize
                ? output?.Capitalize()
                : output;
        }

        [VariableObjectReplacer("pastLife.byFaction.forHateReason")]
        public static string UD_ByFaction_ForHateReason(DelegateContext Context)
        {
            string faction = Factions.GetRandomFaction().Name;
            if (Context.Target is GameObject frankenCorpse
                && frankenCorpse.TryGetPart(out UD_FleshGolems_PastLife pastLife)
                && pastLife.BrainInAJar is GameObject brainInAJar)
            {
                faction = GenerateFriendOrFoe.getRandomFaction(brainInAJar);
            }
            return ("by " + faction + " for " + GenerateFriendOrFoe.getHateReason()).ContextCapitalize(Context);
        }

        [VariableObjectReplacer]
        public static string UD_NotableFeature(DelegateContext Context)
            => ProcessDeathMemoryReplacerResult(KillerDetails.GetNotableFeature(Context.Target) ?? "a lethal absence", Context);

        [VariableObjectReplacer]
        public static string UD_CreatureType(DelegateContext Context)
            => ProcessDeathMemoryReplacerResult(Context.Target?.GetCreatureType() ?? "hideous specimen", Context);

        public static void ProcessVerbReplacement(ref string deathDescription, List<string> parameters)
        {
            if (parameters.Count > 1
                && parameters[0].EqualsNoCase("verb")
                && !parameters[1].IsNullOrEmpty()
                && parameters[1].Uncapitalize() is string newVerb)
            {
                if (deathDescription.StartsWith("=")
                    && deathDescription.TryGetIndexOf("verb:", out int verbStart)
                    && deathDescription[..verbStart].TryGetIndexOf("=", out int replacerEnd)
                    && deathDescription[..verbStart][..(replacerEnd - 1)] is string oldVerb)
                {
                    if (!oldVerb.TryGetIndexOf(":", out int verbEnd))
                    {
                        verbEnd = verbStart + replacerEnd;
                    }
                    deathDescription = deathDescription[..verbStart] + newVerb + deathDescription[(verbEnd - 1)..];
                    verbEnd = verbStart + newVerb.Length;
                    replacerEnd = deathDescription[verbEnd..].IndexOf("=") - 1;
                    if (parameters.Count > 2
                        && !parameters[2].IsNullOrEmpty()
                        && parameters[2].Contains("afterpronoun"))
                    {
                        if (parameters[2].Contains("remove"))
                        {
                            if (deathDescription[(verbEnd + 1)].ToString() == ":")
                                deathDescription = deathDescription[..verbEnd] + deathDescription[replacerEnd..];
                        }
                        else
                        {
                            deathDescription = deathDescription[..verbEnd] + ":afterpronoun" + deathDescription[replacerEnd..];
                        }
                    }
                }
            }
        }

        [VariableObjectReplacer]
        public static string UD_DeathDescription(DelegateContext Context)
        {
            string deathDescription = Context.Target?.GetKillerDetails()?.DeathDescription.ToString()
                ?? "=subject.verb:were:afterpronoun= killed to death";

            if (Context.Parameters is List<string> parameters
                && !parameters.IsNullOrEmpty())
            {
                if (parameters.Count > 1)
                {
                    if (parameters.Count > 2
                        && parameters[0].EqualsNoCase("add")
                        && parameters[1].EqualsNoCase("verb"))
                    {
                        if (!deathDescription.StartsWith("=")
                            || !deathDescription[1..].TryGetIndexOf("=", out int replacerEnd)
                            || deathDescription[..replacerEnd].Contains("verb"))
                        {
                            string verbAddition = "=subject.verb:were";
                            if (parameters.Count > 3
                                && !parameters[3].IsNullOrEmpty()
                                && parameters[3].EqualsNoCase("afterpronoun"))
                            {
                                verbAddition += ":afterpronoun";
                            }
                            verbAddition += "= ";
                            deathDescription = verbAddition + deathDescription;
                        }
                        parameters.RemoveAt(0);
                    }
                    ProcessVerbReplacement(ref deathDescription, parameters);
                }
                if (parameters[0].EqualsAnyNoCase("RemoveVerb", "RemoveBakedVerb"))
                {
                    if (deathDescription.StartsWith("=")
                        && deathDescription[1..].TryGetIndexOf("=", out int replacerEnd)
                        && deathDescription[..replacerEnd].Contains("verb"))
                    {
                        deathDescription = deathDescription[replacerEnd..];
                    }
                    List<string> bakedVerbs = new()
                    {
                        "was",
                        "were",
                    };
                    if (parameters[0].EqualsNoCase("RemoveBakedVerb"))
                    {
                        foreach (string bakedVerb in bakedVerbs)
                            if (!bakedVerb.IsNullOrEmpty()
                                && deathDescription[..bakedVerb.Length].EqualsNoCase(bakedVerb))
                                deathDescription = deathDescription[(bakedVerb.Length + 1)..];
                    }
                }
            }

            string output = deathDescription
                ?.StartReplace()
                ?.AddObject(Context.Target)
                ?.ToString();

            return output.ContextCapitalize(Context);
        }

        [VariableObjectReplacer("bodyPart", Default = "body")]
        public static string BodyPart(DelegateContext Context)
        {
            string requiredType = null;
            if (Context.Parameters is List<string> parameters
                && !parameters.IsNullOrEmpty())
            {
                requiredType = parameters[0];
            }
            return Context.Target
                ?.Body
                ?.LoopPart(!Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null)
                ?.GetRandomElementCosmetic()
                ?.Description
                ?.ContextCapitalize(Context);
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
                output = tagValue.ContextCapitalize(Context);
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
            }
            return output.ContextCapitalize(Context);
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
            }
            return output.ContextCapitalize(Context);
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
                    output = output.ContextCapitalize(Context);
                }
            }
            return output;
        }

        /* 
         * 
         * Variable Post Processors
         * 
         */

        [VariablePostProcessor("no2nd.restore")]
        public static void UD_post_no2nd_restore(DelegateContext Context)
        {
            if (StoredAllowSecondPerson is bool storedAllowSecondPerson)
            {
                Grammar.AllowSecondPerson = storedAllowSecondPerson;
            }
            StoredAllowSecondPerson = null;
        }
    }
}
