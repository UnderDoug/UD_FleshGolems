using System;
using System.Collections.Generic;
using System.Text;

using XRL.World;

namespace UD_FleshGolems.Parts.VengeanceHelpers
{
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

        public DeathDescription()
        {
            Category = null;
            Were = true;
            Killed = null;
            By = true;
            Killer = null;
            With = true;
            Method = null;
        }
        public DeathDescription(
            string Category,
            bool Were,
            string Killed,
            bool By,
            string Killer,
            bool With,
            string Method)
            : this()
        {
            this.Category = Category;
            this.Were = Were;
            this.Killed = Killed;
            this.By = By;
            this.Killer = Killer;
            this.With = With;
            this.Method = Method;
        }
        public DeathDescription(IDeathEvent E)
            : this(
                  Category: null,
                  Were: true,
                  Killed: null,
                  By: true,
                  Killer: E.Killer?.GetReferenceDisplayName(Short: true),
                  With: true,
                  Method: E.Weapon?.GetReferenceDisplayName(Short: true))
        {
            Were = true;
            Category = GetDeathCategoryFromEvent(E.ThirdPersonReason, out By);
            Killed = Category;
            Method ??= GetKilledFromEvent(E.ThirdPersonReason);
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

        public string GetWith(string Article = null, string Killer = null)
            => With
            ? " with " + (!Article.IsNullOrEmpty() ? Article + " ": null)
            : (!GetKiller(Killer).IsNullOrEmpty() ? "'s " : null); // pass "" as Killer to override result when With is false.

        public string GetMethod(string Method = null)
            => Method ?? this.Method;

        public string KilledBy(string Adverb = null, string Killer = null)
            => GetKilled(Adverb) + GetBy(Killer); // pass "" as Killer to override/mask killer.

        public string ByKiller(string Killer = null)
            => GetBy(Killer) + GetKiller(Killer); // pass "" as Killer to override/mask killer.

        public string ByKillerWith(string Article = null, string Killer = null)
            => ByKiller(Killer) + GetWith(Article, Killer);

        public string KilledWith(string Adverb = null, string Article = null)
            => GetKilled(Adverb) + GetWith(Article, "");

        public string WithMethod(string Article = null, string Method = null)
            => GetWith(Article, "") + GetMethod(Method);

        public string KilledWithMethod(string Adverb = null, string Article = null, string Method = null)
            => KilledWith(Adverb, Article) + GetMethod(Method);

        public string WasKilledBy(string Alias = "subject", string Adverb = null, string Killer = null)
            => GetWere(Alias) + GetKilled(Adverb) + ByKiller(Killer);

        public string WasKilledByKillerWithMethod(string Alias = "subject", string Adverb = null, string Killer = null, string Article = null, string Method = null)
            => GetWere(Alias) + GetKilled(Adverb) + ByKiller(Killer) + WithMethod(Article, Method);

        public string TheyWereKilledByKiller(string Alias = "subject", bool Capitalize = false, string Adverb = null, string Killer = null)
            => TheyWere(Alias, Capitalize) + GetKilled(Adverb) + ByKiller(Killer);

        public string TheyWereKilledWithMethod(string Alias = "subject", bool Capitalize = false, string Adverb = null, string Article = null, string Method = null)
            => TheyWere(Alias, Capitalize) + KilledWithMethod(Adverb, Article, Method);

        public string TheyWereKilledByKillerWithMethod(string Alias = "subject", bool Capitalize = false, string Adverb = null, string Killer = null, string Article = null, string Method = null)
            => TheyWereKilledByKiller(Alias, Capitalize, Adverb, Killer) + KilledWithMethod(Adverb, Article, Method);

        public string KilledThem(string Adverb = null, string Alias = "subject")
            => GetKilled(Adverb) + GetThem(Alias);

        public string KillerKilledThem(string Killer = null, string Adverb = null, string Alias = "subject")
            => GetKiller(Killer) + GetKilled(Adverb) + GetThem(Alias);

        public string KilledThemWithMethod(string Adverb = null, string Alias = "subject", string Article = null, string Method = null)
            => GetKilled(Adverb) + GetThem(Alias) + WithMethod(Article, Method);

        public string KillerKilledThemWithMethod(string Killer = null, string Adverb = null, string Alias = "subject", string Article = null, string Method = null)
            => KillerKilledThem(Killer, Adverb, Alias) + WithMethod(Article, Method);
    }
}
