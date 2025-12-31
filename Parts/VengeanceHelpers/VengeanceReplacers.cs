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
            => Context.Capitalize ? Output?.CapitalizeEx() : Output;

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

        [VariableObjectReplacer("death.method")]
        public static string TargetDeath_Method(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDetails()
                    ?.Method());

        [VariableObjectReplacer("death.a.method")]
        public static string TargetDeath_A_Method(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDetails()
                    ?.Method(true));

        [VariableObjectReplacer("death.with.method")]
        public static string TargetDeath_With_Method(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                        ?.GetDeathDescription()
                        ?.GetWith("")
                    + Context.Target
                        ?.GetDeathDetails()
                        ?.Method());

        [VariableObjectReplacer("death.with.a.method")]
        public static string TargetDeath_With_A_Method(DelegateContext Context)
        {
            string with = Context.Target
                        ?.GetDeathDescription()
                        ?.GetWith(
                            Killer: "",
                            With: true,
                            ForceNoMethodArticle: true,
                            PrependSpace: false);

            string aMethod = Context.Target
                        ?.GetDeathDetails()
                        ?.Method(WithIndefiniteArticle: true);

            string capitalize = "null";
            if (Context != null)
                capitalize = Context.Capitalize.ToString();

            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Context.Capitalize), capitalize),
                    Debug.Arg(nameof(with), with),
                    Debug.Arg(nameof(aMethod), aMethod),
                });

            return ContextCapitalize(
                Context: Context,
                Output: with + aMethod);
        }
            
        /*
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                        ?.GetDeathDescription()
                        ?.GetWith(
                            Killer: "",
                            With: true,
                            ForceNoMethodArticle: true,
                            PrependSpace: false) 
                    + Context.Target
                        ?.GetDeathDetails()
                        ?.Method(WithIndefiniteArticle: true));
        */

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
                output = deathDescription.KilledByKiller(adverb, killerOverride) + deathDescription.WithMethod(killerOverride, null, methodOverride);
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

        public static string AccidentalString(string Accidental, DelegateContext Context)
        {
            if (Accidental.IsNullOrEmpty()
                || Context == null)
                return null;

            string prepend;
            string append;
            Dictionary<string, string> contextParams = GetOrderedLabelledContextParameters(
                Offset: 0,
                Context: Context,
                ParameterLabels: new string[]
                {
                    nameof(prepend),
                    nameof(append),
                });

            prepend = contextParams[nameof(prepend)];
            append = contextParams[nameof(append)];

            return prepend + Accidental + append;
        }

        // parameter0: prepend
        // parameter1: append
        [VariableObjectReplacer("death.accidentally")]
        public static string TargetDeath_Accidentally(DelegateContext Context)
            => Context.Target?.GetDeathDetails() is UD_FleshGolems_DeathDetails deathDetails
                && deathDetails.Accidental
            ? ContextCapitalize(
                Context: Context,
                Output: AccidentalString("accidentally", Context))
            : null;

        // parameter0: alternative
        // parameter1: prepend
        // parameter2: append
        [VariableObjectReplacer("death.accidentally.or")]
        public static string TargetDeath_Accidentally_Or(DelegateContext Context)
        {
            if (Context.Target?.GetDeathDetails() is UD_FleshGolems_DeathDetails deathDetails)
            {
                if (deathDetails.Accidental)
                {
                    Context.Parameters.RemoveAt(0);
                    return TargetDeath_Accidentally(Context);
                }
            }
            return ContextCapitalize(
                Context: Context,
                Output: Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null);
        }

        // parameter0: prepend
        // parameter1: append
        [VariableObjectReplacer("death.accidental")]
        public static string TargetDeath_Accidental(DelegateContext Context)
            => Context.Target?.GetDeathDetails() is UD_FleshGolems_DeathDetails deathDetails
                && deathDetails.Accidental
            ? ContextCapitalize(
                Context: Context,
                Output: AccidentalString("accidental", Context))
            : null;

        // parameter0: alternative
        // parameter1: prepend
        // parameter2: append
        [VariableObjectReplacer("death.accidental.or")]
        public static string TargetDeath_Accidental_Or(DelegateContext Context)
        {
            if (Context.Target?.GetDeathDetails() is UD_FleshGolems_DeathDetails deathDetails)
            {
                if (deathDetails.Accidental)
                {
                    Context.Parameters.RemoveAt(0);
                    return TargetDeath_Accidental(Context);
                }
            }
            return ContextCapitalize(
                Context: Context,
                Output: Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null);
        }

        // parameter0: prepend
        // parameter1: append
        [VariableObjectReplacer("death.accident")]
        public static string TargetDeath_Accident(DelegateContext Context)
            => Context.Target?.GetDeathDetails() is UD_FleshGolems_DeathDetails deathDetails
                && deathDetails.Accidental
            ? ContextCapitalize(
                Context: Context,
                Output: AccidentalString("accident", Context))
            : null;

        // parameter0: alternative
        // parameter1: prepend
        // parameter2: append
        [VariableObjectReplacer("death.accident.or")]
        public static string TargetDeath_Accident_Or(DelegateContext Context)
        {
            if (Context.Target?.GetDeathDetails() is UD_FleshGolems_DeathDetails deathDetails)
            {
                if (deathDetails.Accidental)
                {
                    Context.Parameters.RemoveAt(0);
                    return TargetDeath_Accident(Context);
                }
            }
            return ContextCapitalize(
                Context: Context,
                Output: Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null);
        }

        // death.fullDescription
    }
}
