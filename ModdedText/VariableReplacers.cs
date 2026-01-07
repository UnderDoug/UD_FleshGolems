using System.Collections.Generic;

using Qud.API;

using XRL;
using XRL.World;
using XRL.World.Text.Attributes;
using XRL.World.Text.Delegates;
using XRL.World.Parts;
using XRL.Language;
using UD_FleshGolems.Parts.VengeanceHelpers;

namespace UD_FleshGolems.ModdedText
{
    [HasVariableReplacer]
    public static class VariableReplacers
    {
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
                for (int i = 1; i < count; i++)
                    output += nbsp;

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
                            output += " ";

                        output += TextFilters.Weird(Context.Parameters[i]);
                    }
                    output += "}}";
                }
                else
                    return TextFilters.Weird(Context.Parameters[0]);
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
                Grammar.AllowSecondPerson = storedAllowSecondPerson;

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
                switch (parameters[0])
                {
                    case "inherits":
                        inherits = parameters[1];
                        break;
                    case "hasTags":
                        hasTags = parameters[1];
                        break;
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
                ? output?.CapitalizeEx()
                : output;
        }

        [VariableObjectReplacer("pastLife.byFaction.forHateReason")]
        public static string UD_ByFaction_ForHateReason(DelegateContext Context)
        {
            string faction = Factions.GetRandomFaction().Name;
            if (Context.Target is GameObject frankenCorpse
                && frankenCorpse.TryGetPart(out UD_FleshGolems_PastLife pastLife)
                && pastLife.BrainInAJar is GameObject brainInAJar)
                faction = GenerateFriendOrFoe.getRandomFaction(brainInAJar);

            return ("by " + faction + " for " + GenerateFriendOrFoe.getHateReason()).ContextCapitalize(Context);
        }

        [VariableObjectReplacer]
        public static string UD_NotableFeature(DelegateContext Context)
            => ProcessDeathMemoryReplacerResult(KillerDetails.GetNotableFeature(Context.Target), Context);

        [VariableObjectReplacer]
        public static string UD_CreatureType(DelegateContext Context)
            => ProcessDeathMemoryReplacerResult(Context.Target?.GetCreatureType() ?? "hideous specimen", Context);

        public static string ProcessVerbReplacement(string DeathDescription, List<string> Parameters)
        {
            if (DeathDescription.IsNullOrEmpty())
                return DeathDescription;

            if (Parameters.Count > 1
                && Parameters[0].EqualsNoCase("verb")
                && !Parameters[1].IsNullOrEmpty()
                && Parameters[1].Uncapitalize() is string newVerb
                && DeathDescription.StartsWith("=")
                && DeathDescription.TryGetIndexOf("verb:", out int verbStart)
                && DeathDescription[..verbStart].TryGetIndexOf("=", out int replacerEnd)
                && DeathDescription[..verbStart][..(replacerEnd - 1)] is string oldVerb)
            {
                if (!oldVerb.TryGetIndexOf(":", out int verbEnd))
                    verbEnd = verbStart + replacerEnd;

                DeathDescription = DeathDescription[..verbStart] + newVerb + DeathDescription[(verbEnd - 1)..];
                verbEnd = verbStart + newVerb.Length;
                replacerEnd = DeathDescription[verbEnd..].IndexOf("=") - 1;
                if (Parameters.Count > 2
                    && !Parameters[2].IsNullOrEmpty()
                    && Parameters[2].Contains("afterpronoun"))
                {
                    if (Parameters[2].Contains("remove"))
                    {
                        if (DeathDescription[verbEnd + 1].ToString() == ":")
                            DeathDescription = DeathDescription[..verbEnd] + DeathDescription[replacerEnd..];
                    }
                    else
                    {
                        DeathDescription = DeathDescription[..verbEnd] + ":afterpronoun" + DeathDescription[replacerEnd..];
                    }
                }
            }
            return DeathDescription;
        }

        [VariableObjectReplacer]
        public static string UD_DeathDescription(DelegateContext Context)
        {
            string deathDescription = Context.Target?.GetDeathDescription()?.ToString()
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
                    deathDescription = ProcessVerbReplacement(deathDescription, parameters);
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

        [VariableObjectReplacer("bodyPart.NoCase", Default = "body")]
        public static string UD_Target_BodyPart(DelegateContext Context)
            => Context.Target
                ?.Body
                ?.LoopPart(!Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0]?.CapitalizeEx() : null)
                ?.GetRandomElementCosmetic()
                ?.Description
                ?.ContextCapitalize(Context);

        [VariableObjectReplacer]
        public static string UD_xTagSingle(DelegateContext Context)
            => !Context.Parameters.IsNullOrEmpty()
                && Context.Parameters.Count > 1
                && Context.Target is GameObject target
                && target.GetxTag(Context.Parameters[0], Context.Parameters[1]) is string xtag
                && xtag.CachedCommaExpansion().GetRandomElementCosmetic() is string tagValue
            ? tagValue.ContextCapitalize(Context)
            : null;

        [VariableObjectReplacer]
        public static string UD_xTagMulti(DelegateContext Context)
        {
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
                    if (multiTagList.GetRandomElementCosmetic() is string entry)
                        andList.Add(entry);
                    else
                        break;

                output = Grammar.MakeAndList(andList);
                if (andList.Count > 2 && !output.Contains(", and "))
                    output = output.Replace(" and ", ", and ");
            }
            return output.ContextCapitalize(Context);
        }

        [VariableObjectReplacer]
        public static string UD_xTagMultiU(DelegateContext Context)
        {
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
                for (int i = 0; i < count; i++)
                    if (multiTagList.GetRandomElementCosmeticExcluding(Exclude: s => andList.Contains(s)) is string entry)
                        andList.Add(entry);
                    else
                        break;

                output = Grammar.MakeAndList(andList);
                if (andList.Count > 2 && !output.Contains(", and "))
                    output = output.Replace(" and ", ", and ");
            }
            return output.ContextCapitalize(Context);
        }

        [VariableObjectReplacer]
        public static string UD_xTag(DelegateContext Context)
        {
            string output = null;
            if (!Context.Parameters.IsNullOrEmpty()
                && Context.Target is not null)
                output = Context.Parameters.Count switch
                {
                    0 or
                    1 => null,

                    2 => UD_xTagSingle(Context),

                    3 => UD_xTagMulti(Context),

                    // 4
                    _ => UD_xTagMultiU(Context)
                };

            return output?.ContextCapitalize(Context);
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
                Grammar.AllowSecondPerson = storedAllowSecondPerson;

            StoredAllowSecondPerson = null;
        }

        [VariablePostProcessor("capitalize")]
        public static void UD_Capitalize(DelegateContext Context)
        {
            if (Context.Value.ToString() is string contextValue)
            {
                Context.Value.Clear();
                Context.Value.Append(contextValue.CapitalizeEx());
            }
        }
    }
}
