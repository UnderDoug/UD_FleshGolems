using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UD_FleshGolems.Logging;

using XRL;
using XRL.Collections;
using XRL.Language;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Text.Attributes;
using XRL.World.Text.Delegates;

using static XRL.World.Parts.UD_FleshGolems_DestinedForReanimation;
using static UD_FleshGolems.Parts.VengeanceHelpers.DeathDescription;

namespace UD_FleshGolems.Parts.VengeanceHelpers
{
    [HasVariableReplacer]
    public static class VengeanceReplacers
    {
        private static string ContextCapitalize(DelegateContext Context, string Output)
            => Context.Capitalize ? Output?.Capitalize() : Output;

        private static Dictionary<string, string> GetOrderedLabelledContextParameters(
            int Offset,
            DelegateContext Context,
            params string[] ParameterLabels)
        {
            if (ParameterLabels.IsNullOrEmpty())
                return null;

            List<string> contextParams = Context.Parameters ?? new();
            contextParams = contextParams
                ?.Where(p => contextParams.IndexOf(p) >= Offset)
                ?.ToList();

            return ParameterLabels.ToDictionary(
                keySelector: s => s,
                elementSelector: delegate (string e)
                {
                    e = null;
                    if (contextParams.Count > 0)
                    {
                        e = contextParams[0];
                        contextParams.RemoveAt(0);
                    }
                    return e;
                });
        }
        private static Dictionary<string, string> GetOrderedLabelledContextParameters(
            string Key,
            DelegateContext Context,
            params string[] ParameterLabels)
            => GetOrderedLabelledContextParameters(
                Offset: Math.Abs((Key?.Length ?? 0) - (Key?.Remove(".")?.Length ?? 0)),
                Context: Context,
                ParameterLabels: ParameterLabels);

        private static string ProcessWere(DelegateContext Context)
            => Context.Target?.GetDeathDescription()
                ?.GetWere()
                ?.StartReplace()
                ?.AddObject(Context.Target)
                ?.ToString();

        private static bool TryProcessWere(DelegateContext Context, out string Were)
            => !(Were = ProcessWere(Context)).IsNullOrEmpty();

        /*
         * 
         * VariableObjectReplacers
         * 
         */

        // parameter0: killer override.
        [VariableObjectReplacer("death.killer")]
        public static string TargetDeath_Killer(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDescription()
                    ?.GetKiller(!Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null));

        [VariableObjectReplacer("death.killer.feature")]
        public static string TargetDeath_Killer_Feature(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDetails()
                    ?.KillerFeature());

        [VariableObjectReplacer("death.killer.creature")]
        public static string TargetDeath_Killer_Creature(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDetails()
                    ?.KillerCreature());

        [VariableObjectReplacer("death.killer.name")]
        public static string TargetDeath_Killer_Name(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDetails()
                    ?.KillerName());

        // parameter0: killer override.
        [VariableObjectReplacer("death.byKiller")]
        public static string TargetDeath_ByKiller(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDescription()
                    ?.ByKiller(!Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null));

        // parameter0: adverb.
        [VariableObjectReplacer("death.killed", "death.verbed")]
        public static string TargetDeath_Killed(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDetails()
                    ?.Killed(!Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null));

        // parameter0: adverb.
        [VariableObjectReplacer("death.was.killed", "death.was.verbed")]
        public static string TargetDeath_Was_Killed(DelegateContext Context)
        {
            string adverb = !Context.Parameters.IsNullOrEmpty()
                    ? Context.Parameters[0]
                        ?.CachedCommaExpansion()
                        ?.GetRandomElementCosmetic()
                    : null;

            return ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDetails()
                    ?.WereKilled(adverb, FirstPerson: true));
        }

        // parameter0: adverb.
        [VariableObjectReplacer("death.were.killed", "death.were.verbed")]
        public static string TargetDeath_Were_Killed(DelegateContext Context)
        {
            string adverb = !Context.Parameters.IsNullOrEmpty()
                    ? Context.Parameters[0]
                        ?.CachedCommaExpansion()
                        ?.GetRandomElementCosmetic()
                    : null;

            return ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDetails()
                    ?.WereKilled(adverb));
        }

        // parameter0: adverb.
        // parameter1: killer override
        [VariableObjectReplacer("death.killedBy", "death.verbedBy")]
        public static string TargetDeath_KilledBy(DelegateContext Context)
        {
            string adverb;
            string killerOverride;
            Dictionary<string, string> contextParams = GetOrderedLabelledContextParameters(
                    Offset: 0,
                    Context: Context,
                    ParameterLabels: new string[]
                    {
                        nameof(adverb),
                        nameof(killerOverride),
                    });
            adverb = contextParams[nameof(adverb)];
            killerOverride = contextParams[nameof(killerOverride)];
            return ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDescription()
                    ?.KilledByKiller(adverb, killerOverride));
        }

        // parameter0: adverb.
        [VariableObjectReplacer("death.killedBy.killer", "death.verbedBy.killer")]
        public static string TargetDeath_KilledBy_Killer(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDescription()
                    ?.KilledByKiller(!Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null));

        // parameter0: killer override.
        // parameter1: adverb.
        [VariableObjectReplacer("death.killer.killed", "death.killer.verbed")]
        public static string TargetDeath_KillerKilled(DelegateContext Context)
        {
            string killerOverride;
            string adverb;
            Dictionary<string, string> contextParams = GetOrderedLabelledContextParameters(
                    Offset: 0,
                    Context: Context,
                    ParameterLabels: new string[]
                    {
                        nameof(killerOverride),
                        nameof(adverb),
                    });
            killerOverride = contextParams[nameof(killerOverride)];
            adverb = contextParams[nameof(adverb)];
            return ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDescription()
                    ?.KillerKilled(killerOverride, adverb));
        }

        // parameter0: adverb.
        // parameter1: method override.
        // parameter2: ForceNoMethodArticle override.
        [VariableObjectReplacer("death.killedWith", "death.verbedWith")]
        public static string TargetDeath_KilledWith(DelegateContext Context)
        {
            string output = null;
            if (Context.Target?.GetDeathDescription() is DeathDescription deathDescription)
            {
                string adverb;
                string methodOverride;
                bool forceNoMethodArticle = deathDescription.ForceNoMethodArticle;
                Dictionary<string, string> contextParams = GetOrderedLabelledContextParameters(
                    Offset: 0,
                    Context: Context,
                    ParameterLabels: new string[]
                    {
                        nameof(adverb),
                        nameof(methodOverride),
                        nameof(deathDescription.ForceNoMethodArticle),
                    });
                adverb = contextParams[nameof(adverb)];
                methodOverride = contextParams[nameof(methodOverride)];
                if (contextParams[nameof(deathDescription.ForceNoMethodArticle)] is string forceNoMethodArticleParam
                    && forceNoMethodArticleParam.IsNullOrEmpty())
                {
                    deathDescription.ForceNoMethodArticle = forceNoMethodArticleParam.EqualsNoCase("true");
                }
                output = deathDescription.KilledWithMethod(adverb, methodOverride);
                deathDescription.ForceNoMethodArticle = forceNoMethodArticle;
            }
            return ContextCapitalize(
                Context: Context,
                Output: output);
        }

        // parameter0: adverb.
        [VariableObjectReplacer("death.killedWith.method", "death.verbedWith.method")]
        public static string TargetDeath_KilledWith_Method(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDescription()
                    ?.KilledWithMethod(!Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null));

        // parameter0: method override.
        [VariableObjectReplacer("death.method")]
        public static string TargetDeath_Method(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDescription()
                    ?.GetMethod(!Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null));

        // parameter0: method override.
        [VariableObjectReplacer("death.withMethod")]
        public static string TargetDeath_WithMethod(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDescription()
                    ?.WithMethod(!Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null));

        // parameter0: adverb.
        // parameter1: killer override.
        // parameter2: method override.
        // parameter3: ForceNoMethodArticle override.
        [VariableObjectReplacer("death.killed.byWith", "death.verbed.byWith")]
        public static string TargetDeath_Killed_ByWith(DelegateContext Context)
        {
            string output = null;
            if (Context.Target?.GetDeathDescription() is DeathDescription deathDescription)
            {
                string adverb;
                string killerOverride;
                string methodOverride;
                bool forceNoMethodArticle = deathDescription.ForceNoMethodArticle;
                Dictionary<string, string> contextParams = GetOrderedLabelledContextParameters(
                    Offset: 0,
                    Context: Context,
                    ParameterLabels: new string[]
                    {
                        nameof(adverb),
                        nameof(killerOverride),
                        nameof(methodOverride),
                        nameof(deathDescription.ForceNoMethodArticle),
                    });
                adverb = contextParams[nameof(adverb)];
                killerOverride = contextParams[nameof(killerOverride)];
                methodOverride = contextParams[nameof(methodOverride)];
                if (contextParams[nameof(deathDescription.ForceNoMethodArticle)] is string forceNoMethodArticleParam
                    && forceNoMethodArticleParam.IsNullOrEmpty())
                {
                    deathDescription.ForceNoMethodArticle = forceNoMethodArticleParam.EqualsNoCase("true");
                }
                output = deathDescription.KilledByKiller(adverb, killerOverride) + deathDescription.WithMethod(methodOverride);
                deathDescription.ForceNoMethodArticle = forceNoMethodArticle;
            }
            return ContextCapitalize(
                Context: Context,
                Output: output);
        }

        // parameter0: adverb.
        [VariableObjectReplacer("death.killed.byKiller.withMethod", "death.verbed.byKiller.withMethod")]
        public static string TargetDeath_Killed_ByKiller_WithMethod(DelegateContext Context)
        {
            string output = null;
            if (Context.Target?.GetDeathDescription() is DeathDescription deathDescription)
            {
                string adverb;
                Dictionary<string, string> contextParams = GetOrderedLabelledContextParameters(
                    Offset: 0,
                    Context: Context,
                    ParameterLabels: new string[]
                    {
                        nameof(adverb),
                    });
                adverb = contextParams[nameof(adverb)];
                output = deathDescription.KilledByKiller(adverb) + deathDescription.WithMethod();
            }
            return ContextCapitalize(
                Context: Context,
                Output: output);
        }

        // parameter0: adverb.
        // parameter1: method override.
        // parameter2: ForceNoMethodArticle override.
        [VariableObjectReplacer("death.was.killedWith", "death.was.verbedWith")]
        public static string TargetDeath_Was_KilledWith(DelegateContext Context)
        {
            string output = null;
            if (Context.Target?.GetDeathDescription() is DeathDescription deathDescription)
            {
                string adverb;
                string methodOverride;
                bool forceNoMethodArticle = deathDescription.ForceNoMethodArticle;
                Dictionary<string, string> contextParams = GetOrderedLabelledContextParameters(
                    Offset: 0,
                    Context: Context,
                    ParameterLabels: new string[]
                    {
                        nameof(adverb),
                        nameof(methodOverride),
                    });
                adverb = contextParams[nameof(adverb)];
                methodOverride = contextParams[nameof(methodOverride)];
                if (contextParams[nameof(deathDescription.ForceNoMethodArticle)] is string forceNoMethodArticleParam
                    && forceNoMethodArticleParam.IsNullOrEmpty())
                {
                    deathDescription.ForceNoMethodArticle = forceNoMethodArticleParam.EqualsNoCase("true");
                }
                if (TryProcessWere(Context, out string were))
                {
                    output += were;
                }
                output += deathDescription.KilledWithMethod(adverb, methodOverride);
                deathDescription.ForceNoMethodArticle = forceNoMethodArticle;
            }
            return ContextCapitalize(
                Context: Context,
                Output: output);
        }

        // parameter0: adverb.
        [VariableObjectReplacer("death.was.killedWith.method", "death.was.verbedWith.method")]
        public static string TargetDeath_Was_KilledWith_Method(DelegateContext Context)
        {
            string output = null;
            if (Context.Target?.GetDeathDescription() is DeathDescription deathDescription)
            {
                string adverb;
                Dictionary<string, string> contextParams = GetOrderedLabelledContextParameters(
                    Offset: 0,
                    Context: Context,
                    ParameterLabels: new string[]
                    {
                        nameof(adverb),
                    });
                adverb = contextParams[nameof(adverb)];
                if (TryProcessWere(Context, out string were))
                {
                    output += were;
                }
                output += deathDescription.KilledWithMethod(adverb);
            }
            return ContextCapitalize(
                Context: Context,
                Output: output);
        }

        // parameter0: adverb.
        [VariableObjectReplacer("death.beingKilled", "death.beingVerbed")]
        public static string TargetDeath_BeingVerbed(DelegateContext Context)
        {
            string output = null;
            if (Context.Target?.GetDeathDescription() is DeathDescription deathDescription)
            {
                string adverb;
                Dictionary<string, string> contextParams = GetOrderedLabelledContextParameters(
                    Offset: 0,
                    Context: Context,
                    ParameterLabels: new string[]
                    {
                        nameof(adverb),
                    });
                adverb = contextParams[nameof(adverb)];
                output = deathDescription.BeingKilled(adverb);
            }
            return ContextCapitalize(
                Context: Context,
                Output: output);
        }

        // death.was.killed.byWith, death.was.verbed.byWith

        // death.was.killed.byKiller.withMethod, death.was.verbed.byKiller.withMethod

        // death.fullDescription
    }
}
