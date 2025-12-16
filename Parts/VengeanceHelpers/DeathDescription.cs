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

namespace UD_FleshGolems.Parts.VengeanceHelpers
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    [HasVariableReplacer]
    [Serializable]
    public class DeathDescription : IComposite
    {
        public const string CATEGORY_MARKER = "@@";
        public const string REASON_MARKER = "##";

        [ModSensitiveStaticCache]
        [GameBasedStaticCache(ClearInstance = true)]
        public static StringMap<string> VerbFormConversions;

        protected static Dictionary<string, string> VerbFormConversionExceptions => new()
        {
            { "ate", "eating" },
            { "bought", "buying" },
            { "brought", "bringing" },
            { "caught", "catching" },
            { "drank", "drinking" },
            { "forgot", "forgetting" },
            { "had", "having" },
            { "left", "leaving" },
            { "ran", "running" },
            { "spoke", "speaking" },
            { "took", "taking" },
        };

        public string Category;
        public bool Were;
        public string Killed;
        public string Killing;
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
            Killing = null;
            By = true;
            Killer = null;
            With = true;
            Method = null;
            ForceNoMethodArticle = false;
            PluralMethod = false;
        }
        public DeathDescription(
            string Category,
            bool Were = true,
            string Killed = null,
            string Killing = null,
            bool By = true,
            string Killer = null,
            bool With = true,
            string Method = null,
            bool ForceNoMethodArticle = false,
            bool PluralMethod = false)
            : this()
        {
            this.Category = Category;
            this.Were = Were;
            this.Killed = Killed ?? Category;
            this.Killing = Killing;
            this.By = By;
            this.Killer = Killer;
            this.With = With;
            this.Method = Method;
            this.ForceNoMethodArticle = ForceNoMethodArticle;
            this.PluralMethod = PluralMethod;
        }
        public DeathDescription(DeathDescription Source)
            : this(
                  Category: Source.Category,
                  Were: Source.Were,
                  Killed: Source.Killed,
                  Killing: Source.Killing,
                  By: Source.By,
                  Killer: Source.Killer,
                  With: Source.With,
                  Method: Source.Method,
                  ForceNoMethodArticle: Source.ForceNoMethodArticle,
                  PluralMethod: Source.PluralMethod)
        { }
        public DeathDescription(IDeathEvent E, string NotableFeature = null)
            : this()
        {
            ProcessDeathEvent(E, NotableFeature);
        }

        [ModSensitiveCacheInit]
        [GameBasedCacheInit]
        public static bool InitializeVerbFormConversions()
        {
            VerbFormConversions = new();
            bool any = false;
            foreach ((string verbed, string verbing) in VerbFormConversionExceptions)
            {
                VerbFormConversions[verbed] = verbing;
                VerbFormConversions[verbing] = verbed;
                any = true;
            }
            if (!DeathCategoryDeathDescriptions.IsNullOrEmpty())
            {
                foreach ((string _, List<DeathDescription> deathDescriptions) in DeathCategoryDeathDescriptions)
                    deathDescriptions.ForEach(delegate (DeathDescription deathDescription)
                    {
                        deathDescription.BeingKilled();
                    });
                any = true;
            }
            return any;
        }
        public static string ConvertPastSimpleToContinuous(string Verbed)
        {
            string verbing;
            if (VerbFormConversions.TryGetValue(Verbed, out verbing)
                && !verbing.IsNullOrEmpty())
            {
                VerbFormConversions[verbing] = Verbed;
            }
            else
            if (VerbFormConversionExceptions.TryGetValue(Verbed, out verbing)
                && !verbing.IsNullOrEmpty())
            {
                VerbFormConversions[Verbed] = verbing;
                VerbFormConversions[verbing] = Verbed;
            }
            else
            if (Verbed.EndsWith("ied"))
            {
                verbing = Verbed[..^3] + "ying";
                VerbFormConversions[Verbed] = verbing;
                VerbFormConversions[verbing] = Verbed;
            }
            else
            if (Verbed.EndsWith("ed"))
            {
                verbing = Verbed[..^2] + "ing";
                VerbFormConversions[Verbed] = verbing;
                VerbFormConversions[verbing] = Verbed;
            }
            else
            if (Verbed.EndsWith("e"))
            {
                verbing = Verbed[..^1] + "ing";
                VerbFormConversions[Verbed] = verbing;
                VerbFormConversions[verbing] = Verbed;
            }
            else
            {
                verbing = Verbed + "ing";
                VerbFormConversions[Verbed] = verbing;
                VerbFormConversions[verbing] = Verbed;
            }
            return VerbFormConversions[Verbed];
        }
        public static bool ConvertContractedNegativeAuxiliaryAndVerb(string Text, out string Verbed, out string Verbing)
        {
            Verbed = null;
            Verbing = null;
            if (Text.IsNullOrEmpty())
                return false;

            Text = Text.Trim();

            if (!Text.ContainsAll(" ", "n't"))
                return false;

            if (Text.Split(' ')?.ToList() is not List<string> words
                || words.Count < 2
                || !words[0].Contains("n't"))
                return false;

            Verbed = words[0] + " " + words[1];
            if (!VerbFormConversions.TryGetValue(Verbed, out Verbing))
            {
                Verbing = "not" + " " + ConvertPastSimpleToContinuous(words[1]);
                VerbFormConversions[Verbed] = Verbing;
            }
            VerbFormConversions[Verbing] = Verbed;
            return true;
        }

        public static string GetKilling(string Killed)
        {
            if (Killed.IsNullOrEmpty())
                return null;

            Killed = Killed.Trim();
            List<string> words = new() { Killed };
            if (Killed.Contains(" "))
            {
                words = Killed.Split(' ').ToList();
            }
            if (words[0].Contains("n't"))
            {
                words.RemoveAt(0);
                words[0] = "not " + words[0];
            }
            words[0] = ConvertPastSimpleToContinuous(words[0]);
            return words.SafeJoin(" ");
        }

        public static DeathDescription Copy(DeathDescription Source)
            => new(Source);

        public DeathDescription Copy()
            => Copy(this);

        public static DeathDescription MakeLooselyAccidentalCopy(DeathDescription Source)
        {
            var copy = Copy(Source);
            copy.By = false;
            copy.With = false;
            return copy;
        }
        public DeathDescription MakeLooselyAccidentalCopy()
            => MakeLooselyAccidentalCopy(this);

        public static string GetDeathCategoryFromEvent(string DeathEventReason, out bool By)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(DeathEventReason),
                });

            By = true;
            if (DeathEventReason.TryGetIndexOf(CATEGORY_MARKER, out int categoryStart))
            {
                DeathEventReason = DeathEventReason[categoryStart..];
                Debug.Log(nameof(DeathEventReason), DeathEventReason, indent[1]);
            }
            if (DeathEventReason.TryGetIndexOf(REASON_MARKER, out int categoryEnd, false))
            {
                DeathEventReason = DeathEventReason[..categoryEnd];
                Debug.Log(nameof(DeathEventReason), DeathEventReason, indent[1]);
                if (DeathEventReason.TryGetIndexOf(" by ", out int byStart, false))
                {
                    DeathEventReason = DeathEventReason[..byStart];
                    Debug.Log(nameof(DeathEventReason), DeathEventReason, indent[1]);
                }
                else
                if (DeathEventReason.TryGetIndexOf(" from ", out int fromStart, false))
                {
                    DeathEventReason = DeathEventReason[..fromStart];
                    Debug.Log(nameof(DeathEventReason), DeathEventReason, indent[1]);
                    By = false;
                }
            }
            Debug.Log(nameof(DeathEventReason), DeathEventReason, indent[1]);
            Debug.Log(nameof(By), By, indent[1]);
            return DeathEventReason;
        }

        public static string GetMethodFromEvent(string DeathEventReason)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(DeathEventReason), DeathEventReason),
                });
            if (DeathEventReason.TryGetIndexOf(REASON_MARKER, out int categoryEnd))
            {
                DeathEventReason = DeathEventReason[categoryEnd..].Remove("##").TrimStart();
                Debug.Log(nameof(DeathEventReason), DeathEventReason, indent[1]);
            }
            if (DeathEventReason.StartsWith("with ")
                && DeathEventReason.TryGetIndexOf("with ", out int withEnd))
            {
                DeathEventReason = DeathEventReason[withEnd..];
                Debug.Log(nameof(DeathEventReason), DeathEventReason, indent[1]);
            }
            Debug.Log("Remove articles", Indent: indent[1]);
            foreach (string article in new List<string>() { "some", "an", "a", })
            {
                if (DeathEventReason.StartsWith(article + " ")
                    && DeathEventReason.TryGetIndexOf(article + " ", out int articleEnd))
                {
                    DeathEventReason = DeathEventReason[articleEnd..];
                    Debug.Log(nameof(DeathEventReason), DeathEventReason, indent[2]);
                }
            }
            Debug.Log(nameof(DeathEventReason), DeathEventReason, indent[1]);
            return DeathEventReason;
        }

        public static DeathDescription GetFromDeathEvent(IDeathEvent E, string NotableFeature = null)
            => new(E, NotableFeature);

        protected DeathDescription ProcessDeathEvent(IDeathEvent E, string NotableFeature = null)
        {
            Category = GetDeathCategoryFromEvent(E.ThirdPersonReason, out By);
            if (!DeathCategoryDeathDescriptions.TryGetValue(Category, out List<DeathDescription> killedCategoryDescriptionsList))
            {
                killedCategoryDescriptionsList = DeathCategoryDeathDescriptions
                    ?.Aggregate(
                        seed: new List<DeathDescription>(),
                        func: delegate (List<DeathDescription> acc, KeyValuePair<string, List<DeathDescription>> next)
                        {
                            if (next.Value.IsNullOrEmpty())
                                return acc;

                            acc.AddRange(next.Value);
                            return acc;
                        });
            }
            Killed = killedCategoryDescriptionsList
                ?.GetRandomElementCosmetic(delegate (DeathDescription deathDescription)
                {
                    return (deathDescription.Killer != "") == (E.Killer != null)
                        && (deathDescription.Method != "") == (E.Weapon != null);
                }).Killed ?? Category;

            bool killerProperlyNamed = E.Killer != null && E.Killer.HasProperName;
            Killer = E.Killer?.GetReferenceDisplayName(Short: true, WithIndefiniteArticle: !killerProperlyNamed);
            Method = E.Weapon?.GetReferenceDisplayName(Short: true)
                ?? NotableFeature
                ?? GetMethodFromEvent(E.ThirdPersonReason);
            if (E.Weapon != null)
            {
                PluralMethod = E.Weapon.IsPlural;
            }
            return this;
        }

        public override string ToString()
            => TheyWereKilledByKillerWithMethod();

        public string GetThey(string Alias = "subject", bool Capitalize = false)
            => "=" + Alias + "." + (Capitalize ? "S" : "s") + "ubjective= ";

        public string GetThem(string Alias = "subject", bool Capitalize = false)
            => "=" + Alias + "." + (Capitalize ? "O" : "o") + "bjective= ";

        public string GetWere(string Alias = "subject")
            => Were
            ? ("=" + Alias + ".verb:were:afterpronoun= ")
            : "";

        public string GetWas()
            => Were
            ? "was "
            : "";

        public string TheyWere(string Alias = "subject", bool Capitalize = false)
            => GetThey(Alias, Capitalize) + GetWere(Alias);

        public string GetKilled(string Adverb = null)
            => (!Adverb.IsNullOrEmpty() ? Adverb + " " : null) + Killed;

        public string GetBy(string Killer = null, string Method = null)
        {
            if (GetKiller(Killer) == "" && GetMethod(Method).IsNullOrEmpty())
                return null;

            return GetKiller(Killer) != null && By
                ? " by "
                : " from ";
        }


        public string GetKiller(string Killer = null)
            => Killer ?? this.Killer;

        public string GetWith(string Killer = null, string Method = null, bool ForceNoMethodArticle = false)
        {
            if (GetKiller(Killer).IsNullOrEmpty()
                && GetMethod(Method).IsNullOrEmpty())
                return null;

            return With && !GetMethod().IsNullOrEmpty()
                ? " with " + (GetArticle(ForceNoMethodArticle) is string article && !article.IsNullOrEmpty() ? article + " " : null)
                : (!GetKiller(Killer).IsNullOrEmpty() ? "'s " : null); // pass "" as Killer to override result when With is false.
        }

        private string GetArticle(bool ForceNoMethodArticle = false)
            => !ForceNoMethodArticle
            ? (PluralMethod ? "some" : Utils.IndefiniteArticle(GetMethod(Method)))
            : null;

        public string GetMethod(string Method = null)
            => Method ?? this.Method;

        public string Reason(bool Accidental = false)
            => KilledByKiller(Accidental ? "accidentally" : null) + WithMethod();

        public string ThirdPersonReason(bool Capitalize = false, bool Accidental = false)
            => TheyWereKilledByKillerWithMethod(Capitalize: Capitalize, Adverb: Accidental ? "accidentally" : null);

        public string BeingKilled(string Adverb = null)
        {
            string adverb = !Adverb.IsNullOrEmpty() ? Adverb + " " : null;
            string killed = Killed ?? "killed to death";
            if (!Killing.IsNullOrEmpty())
                return adverb + Killing;

            if (Were)
                return "being " + adverb + killed;

            return adverb + GetKilling(killed);
        }

        public string KilledBy(string Adverb = null, string Killer = null, string Method = null)
            => GetKilled(Adverb) + GetBy(Killer, Method); // pass "" as Killer to override/mask killer.

        public string ByKiller(string Killer = null)
            => GetBy(Killer, Method) + GetKiller(Killer); // pass "" as Killer to override/mask killer.

        public string KilledByKiller(string Adverb = null, string Killer = null)
            => KilledBy(Adverb, Killer, Method) + GetKiller(Killer); // pass "" as Killer to override/mask killer.

        public string ByKillerWith(
            string Killer = null,
            string Method = null,
            bool ForceNoMethodArticle = false)
            => ByKiller(Killer) + GetWith(Killer, Method, ForceNoMethodArticle);

        public string KilledWith(
            string Adverb = null,
            string Method = null,
            bool ForceNoMethodArticle = false)
            => GetKilled(Adverb) + GetWith(Killer, Method, ForceNoMethodArticle);

        public string WithMethod(string Method = null, bool ForceNoMethodArticle = false)
            => GetWith(Killer, Method, ForceNoMethodArticle) + GetMethod(Method);

        public string KilledWithMethod(
            string Adverb = null,
            string Method = null,
            bool ForceNoMethodArticle = false)
            => KilledWith(Adverb, Method, ForceNoMethodArticle) + GetMethod(Method);

        public string WereKilled(
            string Alias = "subject",
            string Adverb = null)
            => GetWere(Alias) + GetKilled(Adverb);

        public string WasKilled(
            string Adverb = null)
            => GetWas() + GetKilled(Adverb);

        public string WereKilledBy(
            string Alias = "subject",
            string Adverb = null,
            string Killer = null)
            => GetWere(Alias) + KilledBy(Adverb, Killer, Method);

        public string WasKilledBy(
            string Adverb = null,
            string Killer = null)
            => GetWas() + KilledBy(Adverb, Killer, Method);

        public string WereKilledByKillerWithMethod(
            string Alias = "subject", 
            string Adverb = null,
            string Killer = null,
            string Method = null,
            bool ForceNoMethodArticle = false)
            => GetWere(Alias) + KilledByKiller(Adverb, Killer) + WithMethod(Method, ForceNoMethodArticle);

        public string WasKilledByKillerWithMethod(
            string Adverb = null,
            string Killer = null,
            string Method = null,
            bool ForceNoMethodArticle = false)
            => GetWas() + KilledByKiller(Adverb, Killer) + WithMethod(Method, ForceNoMethodArticle);

        public string TheyWereKilledByKiller(
            string Alias = "subject",
            bool Capitalize = false,
            string Adverb = null,
            string Killer = null)
            => TheyWere(Alias, Capitalize) + KilledByKiller(Adverb, Killer);

        public string TheyWereKilledWithMethod(
            string Alias = "subject",
            bool Capitalize = false,
            string Adverb = null,
            string Method = null,
            bool ForceNoMethodArticle = false)
            => TheyWere(Alias, Capitalize) + KilledWithMethod(Adverb, Method, ForceNoMethodArticle);

        public string TheyWereKilledByKillerWithMethod(
            string Alias = "subject",
            bool Capitalize = false,
            string Adverb = null,
            string Killer = null,
            string Method = null,
            bool ForceNoMethodArticle = false)
            => TheyWereKilledByKiller(Alias, Capitalize, Adverb, Killer) + WithMethod(Method, ForceNoMethodArticle);

        public string KilledThem(string Adverb = null, string Alias = "subject")
            => GetKilled(Adverb) + GetThem(Alias);

        public string KillerKilled(string Killer = null, string Adverb = null)
            => GetKiller(Killer) + GetKilled(Adverb);

        public string KillerKilledThem(
            string Killer = null,
            string Adverb = null,
            string Alias = "subject")
            => KillerKilled(Killer, Adverb) + GetThem(Alias);

        public string KilledThemWithMethod(
            string Adverb = null,
            string Alias = "subject",
            string Method = null,
            bool ForceNoMethodArticle = false)
            => GetKilled(Adverb) + GetThem(Alias) + WithMethod(Method, ForceNoMethodArticle);

        public string KillerKilledThemWithMethod(
            string Killer = null,
            string Adverb = null,
            string Alias = "subject",
            string Method = null,
            bool ForceNoMethodArticle = false)
            => KillerKilledThem(Killer, Adverb, Alias) + WithMethod(Method, ForceNoMethodArticle);

        /*
         * 
         * VariableObjectReplacers
         * 
         */
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

        // parameter0: killer override.
        [VariableObjectReplacer("death.killer")]
        public static string TargetDeath_Killer(DelegateContext Context)
            => ContextCapitalize(
                Context: Context,
                Output: Context.Target
                    ?.GetDeathDescription()
                    ?.GetKiller(!Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null));

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
                    ?.GetDeathDescription()
                    ?.GetKilled(!Context.Parameters.IsNullOrEmpty() ? Context.Parameters[0] : null));

        // parameter2: adverb.
        [VariableObjectReplacer("death.was.killed", "death.was.verbed")]
        public static string TargetDeath_Was_Killed(DelegateContext Context)
        {
            using Indent indent = new(1);

            string output = null;
            if (Context.Target?.GetDeathDescription() is DeathDescription deathDescription)
            {
                string adverb = !Context.Parameters.IsNullOrEmpty()
                    ? Context.Parameters[2]
                        ?.CachedCommaExpansion()
                        ?.GetRandomElementCosmetic()
                    : null;

                output = deathDescription.WasKilled(Adverb: adverb)
                    ?.StartReplace()
                    ?.AddObject(Context.Target)
                    ?.ToString();
            }
            return ContextCapitalize(
                Context: Context,
                Output: output);
        }

        // parameter2: adverb.
        [VariableObjectReplacer("death.were.killed", "death.were.verbed")]
        public static string TargetDeath_Were_Killed(DelegateContext Context)
        {
            using Indent indent = new(1);

            string output = null;
            if (Context.Target?.GetDeathDescription() is DeathDescription deathDescription)
            {
                string adverb = !Context.Parameters.IsNullOrEmpty() 
                    ? Context.Parameters[2]
                        ?.CachedCommaExpansion()
                        ?.GetRandomElementCosmetic()
                    : null;

                output = deathDescription.WereKilled(Adverb: adverb)
                    ?.StartReplace()
                    ?.AddObject(Context.Target)
                    ?.ToString();
            }
            return ContextCapitalize(
                Context: Context,
                Output: output);
        }

        // parameter1: adverb.
        // parameter2: killer override
        [VariableObjectReplacer("death.killedBy", "death.verbedBy")]
        public static string TargetDeath_KilledBy(DelegateContext Context)
        {
            string adverb;
            string killerOverride;
            Dictionary<string, string> contextParams = GetOrderedLabelledContextParameters(
                    Offset: 1,
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
                    Offset: 2,
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
                    Offset: 1,
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
                    Offset: 2,
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
                    Offset: 3,
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
                    Offset: 2,
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
                    Offset: 3,
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
                    Offset: 1,
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
