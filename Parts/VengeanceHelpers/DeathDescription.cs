using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XRL;
using XRL.Language;
using XRL.World;
using XRL.World.Text.Attributes;
using XRL.World.Text.Delegates;

namespace UD_FleshGolems.Parts.VengeanceHelpers
{
    [HasVariableReplacer]
    [Serializable]
    public class DeathDescription : IComposite
    {
        public const string CATEGORY_MARKER = "@@";
        public const string REASON_MARKER = "##";

        /*
        [Serializable]
        public struct DeathVerb : IComposite
        {
            [Serializable]
            public enum Tense : int
            {
                FutureSimple,
                PresentSimple,
                PastSimple,
                FuturePerfect,
                PresentPerfect,
                PastPerfect,
                FutureContinuous,
                PresentContinuous,
                PastContinuous,
                FuturePerfectContinuous,
                PresentPerfectContinuous,
                PastPerfectContinuous,
            }

            public string Verb;
            public string Verbed;
            public string Verbing;

            public DeathVerb(string Verb, string Verbed, string Verbing)
            {
                this.Verb = Verb;
                this.Verbed = Verbed ?? (Verb + "ed");
                this.Verbing = Verbing ?? (Verb + "ing");
            }

            public readonly string GetVerb(Tense Tense)
            {
                if (Tense < Tense.PastSimple)
                    return Verb;

                if (Tense < Tense.FutureContinuous)
                    return Verbed;

                return Verbing;
            }
        }
        */

        public string Category;
        public bool Were;
        public string Killed;
        public bool By;
        public string Killer;
        public bool With;
        public string Method;
        public bool ForceNoMethodArticle;
        public bool PluralMethod;

        public DeathDescription()
        {
            Category = null;
            Were = true;
            Killed = null;
            By = true;
            Killer = null;
            With = true;
            Method = null;
            ForceNoMethodArticle = false;
            PluralMethod = false;
        }
        public DeathDescription(
            string Category,
            bool Were,
            string Killed,
            bool By,
            string Killer,
            bool With,
            string Method,
            bool ForceNoArticle,
            bool PluralMethod)
            : this()
        {
            this.Category = Category;
            this.Were = Were;
            this.Killed = Killed;
            this.By = By;
            this.Killer = Killer;
            this.With = With;
            this.Method = Method;
            this.ForceNoMethodArticle = ForceNoArticle;
            this.PluralMethod = PluralMethod;
        }
        public DeathDescription(IDeathEvent E)
            : this(
                  Category: null,
                  Were: true,
                  Killed: null,
                  By: true,
                  Killer: E.Killer?.GetReferenceDisplayName(Short: true),
                  With: true,
                  Method: E.Weapon?.GetReferenceDisplayName(Short: true),
                  ForceNoArticle: false,
                  PluralMethod: false)
        {
            Were = true;
            Category = GetDeathCategoryFromEvent(E.ThirdPersonReason, out By);
            Killed = Category;
            Method ??= GetKilledFromEvent(E.ThirdPersonReason);
            if (E.Weapon != null)
            {
                PluralMethod = E.Weapon.IsPlural;
            }
        }

        public static string GetDeathCategoryFromEvent(string DeathEventReason, out bool From)
        {
            From = false;
            if (DeathEventReason.TryGetIndexOf(CATEGORY_MARKER, out int categoryStart))
            {
                DeathEventReason = DeathEventReason[categoryStart..];
            }
            if (DeathEventReason.TryGetIndexOf(REASON_MARKER, out int categoryEnd, true))
            {
                DeathEventReason = DeathEventReason[..categoryEnd];
                if (DeathEventReason.TryGetIndexOf(" by ", out int byStart))
                {
                    DeathEventReason = DeathEventReason[..byStart];
                }
                else
                if (DeathEventReason.TryGetIndexOf(" from ", out int fromStart))
                {
                    DeathEventReason = DeathEventReason[..fromStart];
                    From = true;
                }
            }
            return DeathEventReason;
        }

        public static string GetKilledFromEvent(string DeathEventReason)
        {
            if (DeathEventReason.TryGetIndexOf(REASON_MARKER, out int categoryEnd))
            {
                DeathEventReason = DeathEventReason[categoryEnd..].Remove("##").TrimStart();
            }
            if (DeathEventReason.StartsWith("with ")
                && DeathEventReason.TryGetIndexOf("with ", out int withEnd))
            {
                DeathEventReason = DeathEventReason[withEnd..];
            }
            foreach (string article in new List<string>() { "some", "an", "a", })
            {
                if (DeathEventReason.StartsWith(article + " ")
                    && DeathEventReason.TryGetIndexOf(article + " ", out int articleEnd))
                {
                    DeathEventReason = DeathEventReason[articleEnd..];
                }
            }
            return DeathEventReason;
        }

        public override string ToString()
            => TheyWereKilledByKillerWithMethod();

        public string GetThey(string Alias = "subject", bool Capitalize = false)
            => "=" + Alias + "." + (Capitalize ? "S" : "s") + "ubjective= ";

        public string GetThem(string Alias = "subject", bool Capitalize = false)
            => "=" + Alias + "." + (Capitalize ? "O" : "o") + "bjective= ";

        public string GetWere(string Alias = "subject")
            => Were ? ("=" + Alias + ".verb:were:afterpronoun= ") : "";

        public string TheyWere(string Alias = "subject", bool Capitalize = false)
            => GetThey(Alias, Capitalize) + GetWere(Alias);

        public string GetKilled(string Adverb = null)
            => (!Adverb.IsNullOrEmpty() ? Adverb + " " : null) + Killed;

        public string GetBy(string Killer = null)
            => (Killer ?? this.Killer) != null && By
            ? " by "
            : " from ";

        public string GetKiller(string Killer = null)
            => Killer ?? this.Killer;

        public string GetWith(string Killer = null, bool ForceNoMethodArticle = false)
            => With
            ? " with " + (GetArticle(ForceNoMethodArticle) is string article && !article.IsNullOrEmpty() ? article + " ": null)
            : (!GetKiller(Killer).IsNullOrEmpty() ? "'s " : null); // pass "" as Killer to override result when With is false.

        private string GetArticle(bool ForceNoMethodArticle = false)
            => !ForceNoMethodArticle
            ? (PluralMethod ? "some" : Utils.IndefiniteArticle(GetMethod(Method)))
            : null;

        public string GetMethod(string Method = null)
            => Method ?? this.Method;

        public string KilledBy(string Adverb = null, string Killer = null)
            => GetKilled(Adverb) + GetBy(Killer); // pass "" as Killer to override/mask killer.

        public string ByKiller(string Killer = null)
            => GetBy(Killer) + GetKiller(Killer); // pass "" as Killer to override/mask killer.

        public string KilledByKiller(string Adverb = null, string Killer = null)
            => GetKilled(Adverb) + GetBy(Killer) + GetKiller(Killer); // pass "" as Killer to override/mask killer.

        public string ByKillerWith(string Killer = null)
            => ByKiller(Killer) + GetWith(Killer);

        public string KilledWith(string Adverb = null)
            => GetKilled(Adverb) + GetWith("");

        public string WithMethod(string Method = null)
            => GetWith("") + GetMethod(Method);

        public string KilledWithMethod(string Adverb = null, string Method = null)
            => KilledWith(Adverb) + GetMethod(Method);

        public string WasKilledBy(string Alias = "subject", string Adverb = null, string Killer = null)
            => GetWere(Alias) + GetKilled(Adverb) + ByKiller(Killer);

        public string WasKilledByKillerWithMethod(string Alias = "subject", string Adverb = null, string Killer = null, string Method = null)
            => GetWere(Alias) + GetKilled(Adverb) + ByKiller(Killer) + WithMethod(Method);

        public string TheyWereKilledByKiller(string Alias = "subject", bool Capitalize = false, string Adverb = null, string Killer = null)
            => TheyWere(Alias, Capitalize) + GetKilled(Adverb) + ByKiller(Killer);

        public string TheyWereKilledWithMethod(string Alias = "subject", bool Capitalize = false, string Adverb = null, string Method = null)
            => TheyWere(Alias, Capitalize) + KilledWithMethod(Adverb, Method);

        public string TheyWereKilledByKillerWithMethod(string Alias = "subject", bool Capitalize = false, string Adverb = null, string Killer = null, string Method = null)
            => TheyWereKilledByKiller(Alias, Capitalize, Adverb, Killer) + KilledWithMethod(Adverb, Method);

        public string KilledThem(string Adverb = null, string Alias = "subject")
            => GetKilled(Adverb) + GetThem(Alias);

        public string KillerKilled(string Killer = null, string Adverb = null)
            => GetKiller(Killer) + GetKilled(Adverb);

        public string KillerKilledThem(string Killer = null, string Adverb = null, string Alias = "subject")
            => KillerKilled(Killer, Adverb) + GetThem(Alias);

        public string KilledThemWithMethod(string Adverb = null, string Alias = "subject", string Method = null)
            => GetKilled(Adverb) + GetThem(Alias) + WithMethod(Method);

        public string KillerKilledThemWithMethod(string Killer = null, string Adverb = null, string Alias = "subject", string Method = null)
            => KillerKilledThem(Killer, Adverb, Alias) + WithMethod(Method);

        /*
         * 
         * VariableObjectReplacers
         * 
         */
        private static string ContextCapitalize(DelegateContext Context, string Output)
            => Context.Capitalize ? Output?.Capitalize() : Output;

        private static Dictionary<string, string> GetOrderedLabelledContextParameters(DelegateContext Context, params string[] ParameterLabels)
        {
            if (ParameterLabels.IsNullOrEmpty())
                return null;
            List<string> contextParams = Context.Parameters ?? new();
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
        private static string ProcessWere(DelegateContext Context)
            => Context.Target.GetDeathDescription()?.GetWere()?.StartReplace()?.AddObject(Context.Target)?.ToString();

        private static bool TryProcessWere(DelegateContext Context, out string Were)
            => !(Were = ProcessWere(Context)).IsNullOrEmpty();

        // parameter0: killer override.
        [VariableObjectReplacer("death.killer")]
        public static string TargetDeath_Killer(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDescription()
                    ?.GetKiller(Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null));

        // parameter0: killer override.
        [VariableObjectReplacer("death.byKiller")]
        public static string TargetDeath_ByKiller(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDescription()
                    ?.ByKiller(Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null));

        // parameter0: adverb.
        [VariableObjectReplacer("death.killed", "death.verbed")]
        public static string TargetDeath_Killed(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDescription()
                    ?.GetKilled(Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null));

        // parameter0: adverb.
        [VariableObjectReplacer("death.was.killed", "death.was.verbed")]
        public static string TargetDeath_Was_Killed(DelegateContext Context)
        {
            string output = null;
            if (Context.Target?.GetDeathDescription() is DeathDescription deathDescription)
            {
                if (TryProcessWere(Context, out string were))
                {
                    output += were;
                }
                output += deathDescription.GetKilled(Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null);
            }
            return ContextCapitalize(
                Context: Context,
                Output: output);
        }

        // parameter0: adverb.
        // parameter1: killer override
        [VariableObjectReplacer("death.killedBy", "death.verbedBy")]
        public static string TargetDeath_KilledBy(DelegateContext Context)
        {
            string adverb;
            string killerOverride;
            Dictionary<string, string> contextParams = GetOrderedLabelledContextParameters(
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
                    ?.KilledByKiller(Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null));

        // parameter0: killer override.
        // parameter1: adverb.
        [VariableObjectReplacer("death.killerKilled", "death.killerVerbed")]
        public static string TargetDeath_KillerKilled(DelegateContext Context)
        {
            string killerOverride;
            string adverb;
            Dictionary<string, string> contextParams = GetOrderedLabelledContextParameters(
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
        [VariableObjectReplacer("death.killedWith.method", "death.killedWith.method")]
        public static string TargetDeath_KilledWith_Method(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDescription()
                    ?.KilledWithMethod(Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null));

        // parameter0: adverb.
        // parameter1: killer override.
        // parameter2: method override.
        // parameter3: ForceNoMethodArticle override.
        [VariableObjectReplacer("death.killed.byWith", "death.killed.byWith")]
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
        [VariableObjectReplacer("death.killed.byKiller.withMethod", "death.killed.byKiller.withMethod")]
        public static string TargetDeath_Killed_ByKiller_WithMethod(DelegateContext Context)
        {
            string output = null;
            if (Context.Target?.GetDeathDescription() is DeathDescription deathDescription)
            {
                string adverb;
                Dictionary<string, string> contextParams = GetOrderedLabelledContextParameters(
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
            }
            return ContextCapitalize(
                Context: Context,
                Output: output);
        }

        // parameter0: adverb.
        [VariableObjectReplacer("death.was.killedWith", "death.was.verbedWith")]
        public static string TargetDeath_Was_KilledWith_Method(DelegateContext Context)
        {
            string output = null;
            if (Context.Target?.GetDeathDescription() is DeathDescription deathDescription)
            {
                string adverb;
                Dictionary<string, string> contextParams = GetOrderedLabelledContextParameters(
                    Context: Context,
                    ParameterLabels: new string[]
                    {
                        nameof(adverb),
                    });
                adverb = contextParams[nameof(adverb)];
                if (contextParams[nameof(deathDescription.ForceNoMethodArticle)] is string forceNoMethodArticleParam
                    && forceNoMethodArticleParam.IsNullOrEmpty())
                {
                    deathDescription.ForceNoMethodArticle = forceNoMethodArticleParam.EqualsNoCase("true");
                }
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

        // death.was.killed.byWith, death.was.verbed.byWith

        // death.was.killed.byKiller.withMethod, death.was.verbed.byKiller.withMethod

        // death.fullDescription
    }
}
