using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.World.Parts;
using XRL.World.Text;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Parts.VengeanceHelpers;

using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;
using static UD_FleshGolems.Options;

using static XRL.World.Parts.UD_FleshGolems_ReanimatedCorpse;

namespace XRL.World.Conversations.Parts
{
    public class UD_FleshGolems_AskHowDied : IConversationPart
    {
        [UD_FleshGolems_DebugRegistry]
        public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
        {
            Registry.Register(nameof(GetDeathMemoryElements), false);
            Registry.Register(nameof(CheckConversationTextFitsInList), false);
            return Registry;
        }

        public const string RUDE_TO_ASK = "RudeToAsk";
        public const string MEMORY_ELEMENT = "MemoryElement";
        public const string KNOWN = "Known";
        public const string ENVIRONMENT = "Environment";
        public const string KILLED_BY_PLAYER = "KilledByPlayer";
        public const string PLAYER_LED = "PlayerLed";

        public const string ASK_HOW_DIED_PROP = "UD_FleshGolems AskedHowDied";
        public const string ACCUSED_PLAYER_PROP = "UD_FleshGolems AccusedPlayerOfKillingThem";

        public const string DUMMY_KILLER_BLUEPRINT = "UD_FleshGolems KillerDetails Dummy Killer";
        public const string DUMMY_WEAPON_BLUEPRINT = "UD_FleshGolems KillerDetails Dummy Weapon";

        public static ConversationText DefaultRudeToAskText => NewDefaultText(
            Text: "... That's a rude thing to ask...",
            Attributes: new() { { RUDE_TO_ASK, "true" } });

        public static ConversationText DefaultKilledText => NewDefaultDeathElementText(
            Text: "I definitely died...",
            Elements: "Killed",
            Known: false);

        public static ConversationText DefaultKillerText => NewDefaultDeathElementText(
            Text: "I don't know, if anyone, who killed me...",
            Elements: "Killer",
            Known: false);

        public static ConversationText DefaultEnvironmentText => NewDefaultDeathElementText(
            Text: "No one is singularly responsible for my death...",
            Elements: "Environment",
            Known: true);

        public static ConversationText DefaultMethodText => NewDefaultDeathElementText(
            Text: "... I just don't remember exactly how I was killed...",
            Elements: "Method",
            Known: false);

        public static ConversationText DefaultCompleteText => NewDefaultDeathElementText(
            Text: "I don't know, if anyone, who killed me...\n\n... and I don't know even how I was killed...\n\n... though, certainly, I did die.",
            Elements: "Killed,Killer,Method",
            Known: false);

        public static ConversationText DefaultNoText => NewDefaultText(
            Text: "... I actually can't remember at all...\n\nStrange!",
            Attributes: new() { { KNOWN, "false" } });

        public static ConversationText DefaultPlayerKilledText
            => The.Speaker is GameObject speaker && !speaker.IsPlayerLed()
            ? NewDefaultText(
                Text: "IT WAS {{R|=killer.refname|upper=}}!!",
                Attributes: new() { { KILLED_BY_PLAYER, "true" } })
            : NewDefaultText(
                Text: "{{Y|=killer.Refname=}} killed me... but {{Y|=killer.subjective=}} already knew that... didn't {{Y|=killer.subjective=}}?",
                Attributes: new() { { KILLED_BY_PLAYER, "false" } });

        public List<ConversationText> CompleteTexts;

        public bool KnowsPlayerKilledThem;

        public List<ConversationText> KilledByPlayerTexts;

        public UD_FleshGolems_AskHowDied()
        {
            CompleteTexts = new();
            KnowsPlayerKilledThem = false;
            KilledByPlayerTexts = new();
        }

        public static ConversationText NewDefaultText(string Text, Dictionary<string, string> Attributes = null)
            => new()
            {
                Text = Text,
                Attributes = Attributes,
            };

        public static ConversationText NewDefaultDeathElementText(string Text, string Elements, bool Known)
            => NewDefaultText(Text, new()
            {
                { MEMORY_ELEMENT, Elements },
                { KNOWN, Known.ToString().Uncapitalize() }
            });

        public static List<ConversationText> GetConversationTextsWithTextRetainingAttributes(
            ConversationText ConversationText,
            ref List<ConversationText> ConversationTextList,
            Predicate<ConversationText> Filter,
            params string[] Attributes)
            => ConversationText?.GetConversationTextsWithText(
                ConversationTextList: ConversationTextList,
                Filter: Filter,
                Proc: delegate (ConversationText subText)
                {
                    if (!Attributes.IsNullOrEmpty())
                    {
                        subText.Attributes ??= new();
                        foreach (string attribute in Attributes)
                            if (ConversationText.HasAttribute(attribute))
                                subText.Attributes[attribute] = ConversationText.Attributes[attribute];
                    }
                })
            ?? new();

        public static List<ConversationText> GetConversationTextsWithTextRetainingRudeToAskAttributes(
            ConversationText ConversationText,
            ref List<ConversationText> ConversationTextList)
            => GetConversationTextsWithTextRetainingAttributes(
                ConversationText: ConversationText,
                ConversationTextList: ref ConversationTextList,
                Filter: null, // ct => ct.CheckPredicates(),
                Attributes: new string[]
                {
                    RUDE_TO_ASK,
                })
            ?.Where(ct => ct.HasAttribute(RUDE_TO_ASK))
            ?.ToList();

        public static List<ConversationText> GetConversationTextsWithTextRetainingMemoryAttributes(
            ConversationText ConversationText,
            ref List<ConversationText> ConversationTextList)
            => GetConversationTextsWithTextRetainingAttributes(
                ConversationText: ConversationText,
                ConversationTextList: ref ConversationTextList,
                Filter: null,
                Attributes: new string[]
                {
                    MEMORY_ELEMENT,
                    KNOWN,
                })
            ?.Where(ct => ct.HasAttributes(MEMORY_ELEMENT, KNOWN))
            ?.ToList();

        public static List<ConversationText> GetConversationTextsWithTextRetainingEnvironmentAttributes(
            ConversationText ConversationText,
            ref List<ConversationText> ConversationTextList)
            => GetConversationTextsWithTextRetainingAttributes(
                ConversationText: ConversationText,
                ConversationTextList: ref ConversationTextList,
                Filter: null,
                Attributes: new string[]
                {
                    ENVIRONMENT
                })
            ?.Where(ct => ct.HasAttributes(ENVIRONMENT))
            ?.ToList();

        public static List<ConversationText> GetConversationTextsWithTextRetainingKilledByPlayerAttributes(
            ConversationText ConversationText,
            ref List<ConversationText> ConversationTextList)
            => GetConversationTextsWithTextRetainingAttributes(
                ConversationText: ConversationText,
                ConversationTextList: ref ConversationTextList,
                Filter: null,
                Attributes: new string[]
                {
                    KILLED_BY_PLAYER,
                    PLAYER_LED,
                })
            ?.Where(ct => ct.HasAttributes(KILLED_BY_PLAYER, PLAYER_LED))
            ?.ToList();

        public static List<ConversationText> GetRudeToAskTexts(GetTextElementEvent E)
        {
            List<ConversationText> rudeToAskTexts = E?.Texts
                ?.Aggregate(
                    seed: new List<ConversationText>(),
                    func: (acc, next) => GetConversationTextsWithTextRetainingRudeToAskAttributes(next, ref acc));

            if (rudeToAskTexts.IsNullOrEmpty())
            {
                rudeToAskTexts ??= new();
                rudeToAskTexts.Add(DefaultRudeToAskText);
            }
            return rudeToAskTexts;
        }
        public static ConversationText GetRudeToAskText(GetTextElementEvent E)
            => GetRudeToAskTexts(E)?.GetRandomElementCosmetic();

        public static List<ConversationText> GetKilledByPlayerTexts(GetTextElementEvent E)
        {
            List<ConversationText> killedByPlayerTexts = E?.Texts
                ?.Aggregate(
                    seed: new List<ConversationText>(),
                    func: (acc, next) => GetConversationTextsWithTextRetainingKilledByPlayerAttributes(next, ref acc));

            if (killedByPlayerTexts.IsNullOrEmpty())
            {
                killedByPlayerTexts ??= new();
                killedByPlayerTexts.Add(DefaultPlayerKilledText);
            }
            return killedByPlayerTexts;
        }
        public static ConversationText GetKilledByPlayerText(GetTextElementEvent E)
            => GetKilledByPlayerTexts(E)?.GetRandomElementCosmetic();

        public static List<ConversationText> GetDeathMemoryCompatibleTexts(GetTextElementEvent E, DeathMemory DeathMemory = null)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(E), E != null),
                    Debug.Arg(nameof(DeathMemory), DeathMemory?.IsValid),
                });

            List<ConversationText> possibleTexts = E.Texts
                ?.Aggregate(
                    seed: new List<ConversationText>(),
                    func: (acc, next) => GetConversationTextsWithTextRetainingMemoryAttributes(next, ref acc));

            if (possibleTexts.IsNullOrEmpty())
            {
                possibleTexts ??= new();
                possibleTexts.Add(DefaultPlayerKilledText);
                possibleTexts.Add(DefaultKillerText);
                possibleTexts.Add(DefaultEnvironmentText);
                possibleTexts.Add(DefaultMethodText);
            }

            if (DeathMemory is DeathMemory deathMemory
                && deathMemory.IsValid)
            {
                possibleTexts = possibleTexts
                    ?.Where(ct => IsCompatibleWithDeathMemory(ct, deathMemory))
                    ?.ToList();
            }

            return possibleTexts;
        }

        public static List<string> GetDeathMemoryElements(ConversationText ConversationText)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(ConversationText), ConversationText?.PathID?.TextAfter(".")),
                });

            if (ConversationText == null
                || !ConversationText.TryGetAttribute(MEMORY_ELEMENT, out string deathMemoryElements))
                return new();

            Debug.Log(nameof(deathMemoryElements), deathMemoryElements, indent[1]);

            return deathMemoryElements.CachedCommaExpansion();
        }

        public static bool HasMemoryElement(ConversationText ConversationText, string MemoryElement)
            => GetDeathMemoryElements(ConversationText) is List<string> memoryElementList
            && MemoryElement.EqualsAnyNoCase(memoryElementList.ToArray());

        public static bool HasMemoryElementContaining(ConversationText ConversationText, string MemoryElement)
            => GetDeathMemoryElements(ConversationText) is List<string> memoryElementList
            && memoryElementList.Any(s => s.Contains(MemoryElement));

        public static bool HasMemoryElementStartsWith(ConversationText ConversationText, string MemoryElement)
            => GetDeathMemoryElements(ConversationText) is List<string> memoryElementList
            && memoryElementList.Any(s => s.StartsWith(MemoryElement));

        public static bool HasKilledMemoryElement(ConversationText ConversationText)
            => HasMemoryElement(ConversationText, "Killed");

        public static bool HasKillerMemoryElement(ConversationText ConversationText)
            => HasMemoryElementStartsWith(ConversationText, "Killer");

        public static bool HasEnvironmentMemoryElement(ConversationText ConversationText)
            => HasMemoryElement(ConversationText, ENVIRONMENT);

        public static bool HasKillerEquivalentMemoryElement(ConversationText ConversationText)
            => HasKillerMemoryElement(ConversationText)
            || HasEnvironmentMemoryElement(ConversationText);

        public static bool HasMethodMemoryElement(ConversationText ConversationText)
            => HasMemoryElement(ConversationText, "Method");

        public static bool IsKnownMemoryText(ConversationText ConversationText)
            => ConversationText != null
            && ConversationText.HasAttributeWithValue(KNOWN, "true");

        public static bool IsUnknownMemoryText(ConversationText ConversationText)
            => ConversationText != null
            && ConversationText.HasAttribute(MEMORY_ELEMENT)
            && ConversationText.HasAttributeWithValue(KNOWN, "false");

        public static bool IsCompatibleWithDeathMemory(ConversationText ConversationText, DeathMemory DeathMemory)
            => ConversationText != null
            && DeathMemory.IsValid
            && DeathMemory.MemoryIsCompatibleWithElements(GetDeathMemoryElements(ConversationText), IsKnownMemoryText(ConversationText));

        public static DeathMemory.KillerMemory? GetKillerMemoryForElements(params string[] Elements)
        {
            if (Elements.IsNullOrEmpty())
                return null;

            DeathMemory.KillerMemory killerElementValue = DeathMemory.KillerMemory.Amnesia;

            if ("KillerFeature".EqualsAnyNoCase(Elements))
                killerElementValue = DeathMemory.KillerMemory.Feature;

            if ("KillerCreature".EqualsAnyNoCase(Elements))
                killerElementValue = DeathMemory.KillerMemory.Creature;

            if ("KillerName".EqualsAnyNoCase(Elements))
                killerElementValue = DeathMemory.KillerMemory.Name;

            return killerElementValue;
        }
        public static DeathMemory.KillerMemory? GetKillerMemoryForElements(ConversationText ConversationText)
            => GetKillerMemoryForElements(GetDeathMemoryElements(ConversationText)?.ToArray());

        public static bool CheckCompleteConversationText(ConversationText ConversationText, DeathMemory DeathMemory)
        {
            if (ConversationText == null
                || ConversationText.Text.IsNullOrEmpty()
                || !DeathMemory.IsValid
                || !ConversationText.HasAttribute(MEMORY_ELEMENT))
                return false;

            if (DeathMemory.HasAmnesia()
                && ConversationText.HasAttributeWithValue(KNOWN, "true"))
                return false;

            if (DeathMemory.GetRemembersKilled().HasValue
                && !HasKilledMemoryElement(ConversationText))
                return false;
            
            if (DeathMemory.GetRemembersKiller().HasValue
                && !HasKillerMemoryElement(ConversationText))
                return false;
            
            if (!DeathMemory.GetRemembersKiller().HasValue
                && !HasEnvironmentMemoryElement(ConversationText))
                return false;

            if (DeathMemory.GetRemembersMethod().HasValue
                && !HasMethodMemoryElement(ConversationText))
                return false;

            return true;
        }
        public static bool CheckCompleteConversationText(List<ConversationText> ConversationTextList, DeathMemory DeathMemory)
        {
            if (ConversationTextList.IsNullOrEmpty()
                || !DeathMemory.IsValid)
                return false;

            if (DeathMemory.HasAmnesia()
                && ConversationTextList.Any(ct => IsKnownMemoryText(ct)))
                return false;

            bool? killed = DeathMemory.GetRemembersKilled();
            DeathMemory.KillerMemory? killer = DeathMemory.GetRemembersKiller();
            bool? method = DeathMemory.GetRemembersMethod();

            List<string> combinedTextMemoryElements = ConversationTextList
                ?.Aggregate(
                    seed: new List<string>(),
                    func: delegate (List<string> acc, ConversationText next)
                    {
                        foreach (string element in GetDeathMemoryElements(next) ?? new List<string>())
                            acc.AddIfNot(element, e => acc.Contains(e));
                        return acc;
                    })
                ?? new();

            string[] combinedTextElementsArray = combinedTextMemoryElements.ToArray();
            bool haveGenericKillerElement = "Killer".EqualsAnyNoCase(combinedTextElementsArray);

            if (killed.HasValue
                && !ConversationTextList.Any(ct => HasKilledMemoryElement(ct)))
                return false;

            if (killer.HasValue && !haveGenericKillerElement)
            {
                if (ConversationTextList.Any(ct => HasEnvironmentMemoryElement(ct)))
                {
                    ConversationTextList.RemoveAll(ct => HasEnvironmentMemoryElement(ct));
                    return false;
                }

                if (!ConversationTextList.Any(ct => HasKillerMemoryElement(ct)))
                    return false;

                DeathMemory.KillerMemory killerValue = (DeathMemory.KillerMemory)killer;

                if (GetKillerMemoryForElements(combinedTextElementsArray) is not DeathMemory.KillerMemory killerElementValue)
                    return false;

                if (killerElementValue > killerValue)
                {
                    ConversationTextList.RemoveAll(ct => GetKillerMemoryForElements(ct) > killerValue);
                    return false;
                }
            }

            if (!killer.HasValue
                && !ConversationTextList.Any(ct => HasEnvironmentMemoryElement(ct)))
            {
                ConversationTextList.RemoveAll(ct => HasKillerMemoryElement(ct));
                return false;
            }

            if (method.HasValue
                && !ConversationTextList.Any(ct => HasMethodMemoryElement(ct)))
                return false;

            return true;
        }

        public static bool HasElementAlreadyInList(
            List<ConversationText> ConversationTextList,
            ConversationText ProspectiveConversationText,
            Predicate<ConversationText> ElementPredicate)
            => !ConversationTextList.IsNullOrEmpty()
            && ElementPredicate != null
            && ConversationTextList.Any(ct => ElementPredicate(ct))
            && ElementPredicate(ProspectiveConversationText);

        public static bool CheckConversationTextFitsInList(
            ref List<ConversationText> ConversationTextList,
            DeathMemory DeathMemory,
            ConversationText ProspectiveConversationText)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(ConversationTextList.Count), ConversationTextList?.Count ?? 0),
                    Debug.Arg(nameof(DeathMemory) + "." + nameof(DeathMemory.IsValid), DeathMemory.IsValid),
                    Debug.Arg(nameof(ProspectiveConversationText.PathID), ProspectiveConversationText?.PathID?.TextAfter(".") ?? "no ID"),
                });

            ConversationTextList ??= new();

            if (!DeathMemory.IsValid
                || ProspectiveConversationText == null)
                return false;

            if (HasElementAlreadyInList(ConversationTextList, ProspectiveConversationText, HasKilledMemoryElement))
                return false;

            if (HasElementAlreadyInList(ConversationTextList, ProspectiveConversationText, HasKillerEquivalentMemoryElement))
                return false;

            if (HasElementAlreadyInList(ConversationTextList, ProspectiveConversationText, HasMethodMemoryElement))
                return false;

            List<ConversationText> testList = new(ConversationTextList)
            {
                ProspectiveConversationText
            };

            List<string> combinedMemoryElementsStrings = ConversationTextList
                ?.Aggregate(
                    seed: new List<string>(),
                    func: delegate (List<string> acc, ConversationText next)
                    {
                        foreach (string element in GetDeathMemoryElements(next) ?? new List<string>())
                            acc.AddIfNot(element, e => acc.Contains(e));
                        return acc;
                    })
                ?? new();

            Debug.Log(nameof(combinedMemoryElementsStrings), combinedMemoryElementsStrings?.Count ?? 0, Indent: indent[1]);
            Debug.Log(
                nameof(combinedMemoryElementsStrings) + "." + nameof(Enumerable.Distinct),
                combinedMemoryElementsStrings?.Distinct()?.Count() ?? 0,
                Indent: indent[1]);

            if (combinedMemoryElementsStrings.Count != combinedMemoryElementsStrings.Distinct().Count())
                return false;

            return testList.All(ct => IsCompatibleWithDeathMemory(ct, DeathMemory));
        }

        public static bool AddConversationTextToListIfFits(
            ref List<ConversationText> ConversationTextList,
            DeathMemory DeathMemory,
            ConversationText ProspectiveConversationText)
        {
            string attributesString = ProspectiveConversationText?.Attributes?.ToStringForCachedDictionaryExpansion();
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(ConversationTextList.Count), ConversationTextList?.Count ?? 0),
                    Debug.Arg("(" + attributesString + ")"),
                    Debug.Arg(nameof(ProspectiveConversationText.PathID), ProspectiveConversationText?.PathID?.TextAfter(".") ?? "no ID"),
                });

            ConversationTextList ??= new();
            if (DeathMemory.IsValid
                && ProspectiveConversationText != null
                && CheckConversationTextFitsInList(ref ConversationTextList, DeathMemory, ProspectiveConversationText))
            {
                ConversationTextList.Add(ProspectiveConversationText);
                Debug.CheckYeh("Added", Indent: indent[1]);
                return true;
            }
            Debug.CheckNah("Not added", Indent: indent[1]);
            return false;
        }

        public static void SortCompleteConversationTextList(ref List<ConversationText> ConversationTextList)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(ConversationTextList), ConversationTextList?.Count ?? 0),
                });

            List<ConversationText> sortedConversationTextList = new();
            int maxAttempts = (ConversationTextList?.Count ?? 1) * 10;
            int attemptCount = 0;
            if ((ConversationTextList?.Count ?? 1) > 1)
            {
                while (!ConversationTextList.IsNullOrEmpty() && attemptCount++ < maxAttempts)
                {
                    ConversationText currentText = ConversationTextList[0];
                    ConversationTextList.Remove(currentText);
                    string attributesString = "(" + currentText.Attributes?.ToStringForCachedDictionaryExpansion() + "): ";
                    Debug.Log(
                        attemptCount + "] " + attributesString + currentText?.PathID?.TextAfter("."),
                        nameof(sortedConversationTextList.Count) + " (" + sortedConversationTextList.Count + ")",
                        Indent: indent[1]);

                    if (HasKilledMemoryElement(currentText))
                    {
                        sortedConversationTextList.Add(currentText);
                        Debug.CheckYeh(nameof(HasKilledMemoryElement), indent[2]);
                        continue;
                    }
                    if (sortedConversationTextList.Any(ct => HasKilledMemoryElement(ct)))
                    {
                        if (HasKillerEquivalentMemoryElement(currentText))
                        {
                            sortedConversationTextList.Add(currentText);
                            Debug.CheckYeh(nameof(HasKillerEquivalentMemoryElement), indent[2]);
                            continue;
                        }
                        if (sortedConversationTextList.Any(ct => HasKillerEquivalentMemoryElement(ct)))
                        {
                            if (HasMethodMemoryElement(currentText))
                            {
                                sortedConversationTextList.Add(currentText);
                                Debug.CheckYeh(nameof(HasMethodMemoryElement), indent[2]);
                                continue;
                            }
                        }
                    }
                    ConversationTextList.Add(currentText);
                    Debug.CheckNah("Added back to pool.", indent[2]);
                }
            }
            if (!ConversationTextList.IsNullOrEmpty())
            {
                sortedConversationTextList.AddRange(ConversationTextList);
            }
            ConversationTextList = sortedConversationTextList;
        }

        private static string GetJoiner(int Iteration)
                => Iteration % 2 == 0
                ? "\n\n"
                : " ";
        private static ConversationText MergeAccumulatedTextWithNextText(
            ConversationText Accumulate,
            ConversationText Next,
            ref int Iteration,
            ConversationText SeedConversationText,
            Indent Indent)
        {
            int capIteration = 2;
            int capOffset = 0;
            bool doCapitalization = Iteration % capIteration == capOffset;

            ConversationText newConversationText = Accumulate;
            if (Next != SeedConversationText)
            {
                if (doCapitalization)
                    Next.ReplacerCapitalize();

                newConversationText = Accumulate.Append(Next, GetJoiner(Iteration), new() { MEMORY_ELEMENT });
            }
            string capitalized = doCapitalization ? " Cap" : " noCap";
            Debug.Log("[" + Iteration + "] " + newConversationText?.PathID?.TextAfter(".") + capitalized, Indent: Indent);
            Iteration++;
            return newConversationText;
        }
        public static ConversationText CompileConversationTexts(params ConversationText[] ConversationTexts)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(ConversationTexts), ConversationTexts?.Length ?? 0),
                });

            if (ConversationTexts.IsNullOrEmpty())
                return null;

            int iteration = 0;
            ConversationText seedConversationText = ConversationTexts[0];
            ConversationText mergeAccumulatedTextWithNextText(ConversationText Accumulate, ConversationText Next)
                => MergeAccumulatedTextWithNextText(Accumulate, Next, ref iteration, seedConversationText, indent[1]);
            return ConversationTexts
                .Aggregate(
                    seed: seedConversationText,
                    func: mergeAccumulatedTextWithNextText);
        }

        public static bool GetPlayerHasAskedBefore(GameObject Player, GameObject Speaker)
            => Player != null
            && Speaker != null
            && Speaker.GetStringProperty(ASK_HOW_DIED_PROP) is string askedHowDied
            && askedHowDied.Contains(";" + Player.ID + ";");

        public static bool SetPlayerHasAskedBefore(GameObject Player, GameObject Speaker)
        {
            if (Player == null
                || Speaker == null)
                return false;

            string playerString = ";" + Player.ID + ";";
            string askedHowDied = Speaker.GetStringProperty(ASK_HOW_DIED_PROP, "");

            if (askedHowDied.IsNullOrEmpty()
                || !askedHowDied.Contains(playerString))
            {
                Speaker.SetStringProperty(ASK_HOW_DIED_PROP, askedHowDied + playerString);
                return true;
            }
            return GetPlayerHasAskedBefore(Player, Speaker);
        }

        public string GetAccusedProp()
            => ACCUSED_PLAYER_PROP + ":" + The.Player?.ID;

        public override void Awake()
        {
            base.Awake();
            KnowsPlayerKilledThem = The.Speaker is GameObject speaker
                && The.Player is GameObject player
                && speaker.KnowsEntityKilledThem(player);

            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(KnowsPlayerKilledThem), KnowsPlayerKilledThem),
                });

            SetPlayerHasAskedBefore(The.Player, The.Speaker);
        }

        public override bool WantEvent(int ID, int Propagation)
            => base.WantEvent(ID, Propagation)
            || ID == EnteredElementEvent.ID
            || ID == GetTextElementEvent.ID
            || ID == PrepareTextEvent.ID
            || ID == PrepareTextLateEvent.ID
            ;
        public override bool HandleEvent(EnteredElementEvent E)
        {
            if (The.Speaker is GameObject speaker
                && The.Player is GameObject player)
            {
                if (KnowsPlayerKilledThem
                    && !speaker.IsPlayerLed())
                {
                    ParentElement.Attributes ??= new();
                    ParentElement.Attributes["AllowEscape"] = "false";
                    if (ParentElement is Node parentNode)
                        parentNode.AllowEscape = false;

                    ParentElement.Elements?.RemoveAll(e => e is Choice && !e.HasPart<StartFight>());
                    if (ParentElement.Elements?.Where(e => e is Choice) is not List<IConversationElement> choices
                        || choices.IsNullOrEmpty())
                    {
                        Choice fightChoice = ParentElement.AddChoice(Text: "Oh! Shi-", Target: "End");
                        fightChoice.AddPart(new StartFight());
                    }
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetTextElementEvent E)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(GetTextElementEvent)),
                    Debug.Arg(nameof(The.Speaker), The.Speaker?.DebugName),
                });

            if (!E.Texts.IsNullOrEmpty()
                && The.Speaker is GameObject speaker
                && The.Player is GameObject player
                && speaker.TryGetPart(out UD_FleshGolems_DeathDetails deathDetails))
            {
                if (deathDetails.DeathQuestionsAreRude
                    && !speaker.IsPlayerLed())
                {
                    Debug.Log(nameof(deathDetails.DeathQuestionsAreRude), Indent: indent[1]);

                    E.Selected = GetRudeToAskText(E);

                    Debug.Log(nameof(E.Selected), E.Selected.Text, Indent: indent[2]);
                }
                else
                {
                    if (deathDetails.DeathQuestionsAreRude)
                    {
                        string playerRefName = player?.GetReferenceDisplayName(Short: true);
                        Debug.Log(nameof(deathDetails.DeathQuestionsAreRude) + " but I'm friends with " + playerRefName, Indent: indent[1]);
                    }

                    if (!deathDetails.DeathMemory.Validate(speaker))
                        Debug.Log(
                            nameof(deathDetails.DeathMemory) + " failed to validate " + speaker?.DebugName,
                            speaker.ID + "/" + deathDetails.DeathMemory.GetCorpseID(),
                            Indent: indent[1]);

                    List<ConversationText> possibleTexts = GetDeathMemoryCompatibleTexts(E, deathDetails.DeathMemory);

                    Debug.Log(nameof(possibleTexts), Indent: indent[1]);
                    foreach (ConversationText possibleText in possibleTexts)
                    {
                        string attributesString = "(" + possibleText.Attributes?.ToStringForCachedDictionaryExpansion() + "): ";
                        Debug.Log(attributesString + possibleText.Text, Indent: indent[2]);
                        Debug.Log(HONLY.ThisManyTimes(25), Indent: indent[2]);
                    }

                    CompleteTexts ??= possibleTexts
                        ?.Where(ct => CheckCompleteConversationText(ct, deathDetails.DeathMemory))
                        ?.ToList();

                    possibleTexts?.RemoveAll(ct => !CompleteTexts.IsNullOrEmpty() && CompleteTexts.Contains(ct));

                    if (!CompleteTexts.IsNullOrEmpty())
                    {
                        E.Selected = CompleteTexts?.GetRandomElementCosmetic();
                    }
                    if (!possibleTexts.IsNullOrEmpty())
                    {
                        int maxAttempts = possibleTexts.Count * 10;
                        int attempt = 0;
                        List<ConversationText> possibleTextsWorkingList = new(possibleTexts);
                        List<ConversationText> constructedCompleteTextList = new();

                        bool isConversationTextUnableToFitInList(ConversationText ConversationText)
                            => !CheckConversationTextFitsInList(ref constructedCompleteTextList, deathDetails.DeathMemory, ConversationText);

                        Debug.Log("Compiling " + nameof(constructedCompleteTextList), Indent: indent[1]);
                        while (!possibleTextsWorkingList.IsNullOrEmpty() && attempt++ < maxAttempts)
                        {
                            ConversationText prospectiveText = possibleTextsWorkingList
                                ?.GetRandomElementCosmeticExcluding(isConversationTextUnableToFitInList);
                            if (prospectiveText == null)
                            {
                                MetricsManager.LogModWarning(
                                    mod: ThisMod,
                                    Message: GetType().Name + " failed to get next text from non-empty " + 
                                        nameof(possibleTextsWorkingList) + " list after at " + attempt.Things(nameof(attempt)));
                                break;
                            }

                            Debug.Log(nameof(prospectiveText), prospectiveText.PathID.TextAfter("."), Indent: indent[2]);

                            if (AddConversationTextToListIfFits(ref constructedCompleteTextList, deathDetails.DeathMemory, prospectiveText))
                                possibleTextsWorkingList.RemoveAll(isConversationTextUnableToFitInList);
                            else
                                possibleTextsWorkingList.Remove(prospectiveText);

                            _ = indent[1];
                            if (CheckCompleteConversationText(possibleTextsWorkingList, deathDetails.DeathMemory))
                            {
                                Debug.CheckYeh("Texts Completed", indent[1]);
                                break;
                            }
                        }
                        if (!constructedCompleteTextList.IsNullOrEmpty())
                        {
                            SortCompleteConversationTextList(ref constructedCompleteTextList);
                            E.Selected = CompileConversationTexts(constructedCompleteTextList.ToArray());
                            Debug.YehNah(nameof(E.Selected), E.Selected != null, indent[1]);
                        }
                    }
                    E.Selected ??= CompleteTexts
                        ?.GetRandomElementCosmetic()
                        ?? DefaultNoText;
                }
                if (KnowsPlayerKilledThem)
                {
                    string playerLedString = speaker.IsPlayerLed() ? "true" : "false";
                    Debug.Log(nameof(KnowsPlayerKilledThem), Indent: indent[1]);
                    KilledByPlayerTexts = E.Texts
                        ?.Aggregate(
                            seed: new List<ConversationText>(),
                            func: (acc, next) => GetConversationTextsWithTextRetainingKilledByPlayerAttributes(next, ref acc))
                        ?.Where(t => t.HasAttributeWithValue(PLAYER_LED, playerLedString))
                        ?.ToList();

                    if (KilledByPlayerTexts.IsNullOrEmpty())
                    {
                        KilledByPlayerTexts ??= new();
                        KilledByPlayerTexts.Add(DefaultPlayerKilledText);
                    }

                    Debug.Log(nameof(KilledByPlayerTexts), Indent: indent[1]);
                    foreach (ConversationText playerKilledText in KilledByPlayerTexts)
                    {
                        Debug.Log(playerKilledText.Text, Indent: indent[2]);
                    }
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(PrepareTextEvent E)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(PrepareTextEvent)),
                    Debug.Arg(nameof(The.Speaker), The.Speaker?.DebugName),
                });

            if (The.Speaker is GameObject speaker)
            {
                bool killerIsSample = true;
                bool weaponIsSample = true;
                GameObject killer = GameObject.CreateSample(DUMMY_KILLER_BLUEPRINT);
                GameObject weapon = GameObject.CreateSample(DUMMY_WEAPON_BLUEPRINT);

                string killerName = "someone";
                string killerCreatureType = "a mysterious entity";
                string killerFeature = "someone with mysteriousness";
                string killerString = killerName;

                string weaponString = "deadly force";


                if (speaker.TryGetPart(out UD_FleshGolems_DeathDetails deathDetails)
                    && deathDetails.DeathMemory.IsValid)
                {
                    DeathDescription deathDescription = deathDetails.DeathDescription;

                    if (deathDetails.KillerDetails != null
                        && deathDetails.DeathMemory.RemembersKiller())
                        killerString = deathDetails.KnownKiller();

                    if (deathDetails.KillerDetails != null
                        && deathDetails.DeathMemory.RemembersKillerName())
                        killerName = deathDetails?.KillerDetails?.DisplayName;

                    if (deathDetails.KillerDetails != null
                        && deathDetails.DeathMemory.RemembersKillerCreature())
                        killerCreatureType = deathDetails?.KillerDetails?.CreatureType;

                    if (deathDetails.KillerDetails != null
                        && deathDetails.DeathMemory.RemembersKillerFeature())
                        killerFeature = deathDetails?.KillerDetails?.NotableFeature;

                    if (deathDetails.Weapon != null
                        && deathDetails.DeathMemory.RemembersMethod()
                        && deathDetails.Weapon != null)
                        weaponString = deathDetails?.Weapon?.GetReferenceDisplayName(Short: true);

                    if (GameObject.Validate(deathDetails?.Killer))
                    {
                        killer = deathDetails.Killer;
                        killerIsSample = false;
                    }
                    if (GameObject.Validate(deathDetails?.Weapon))
                    {
                        weapon = deathDetails.Weapon;
                        weaponIsSample = false;
                    }
                }

                if (killerIsSample)
                {
                    killer.DisplayName = killerName;
                    killer.SetCreatureType(killerCreatureType);
                    killer.SetNotableFeature(killerFeature);
                }
                if (weaponIsSample)
                {
                    weapon.DisplayName = weaponString;
                }

                E.Text.StartReplace()
                    .AddObject(E.Subject)
                    .AddObject(E.Object)
                    .AddObject(killer, "killer")
                    .AddObject(weapon, "weapon")
                    .Execute();

                if (killerIsSample)
                    killer?.Obliterate();

                if (weaponIsSample)
                    weapon?.Obliterate();

                if (KnowsPlayerKilledThem
                    && KilledByPlayerTexts?.GetRandomElementCosmetic()?.Text is string killedByPlayerString
                    && speaker.GetIntProperty(GetAccusedProp(), 0) is int accusedCount)
                {
                    if (accusedCount == 0
                        || (Math.Min(Math.Max(4, accusedCount), 8) is int makeAccusationDieSize
                            && Stat.RollCached("1d" + makeAccusationDieSize) == 1))
                    {
                        speaker.ModIntProperty(GetAccusedProp(), 1);

                        E.Text
                            .StartReplace()
                            .AddObject(E.Subject)
                            .AddObject(E.Object)
                            .AddObject(killer, "killer")
                            .AddObject(weapon, "weapon")
                            .Execute();

                        if (!speaker.IsPlayerLed())
                        {
                            Debug.Log("Preparing " + nameof(KilledByPlayerTexts) + " for non-party member...", Indent: indent[1]);
                            if (E.Text?.ToString()?.Split(' ')?.ToList() is List<string> textWords)
                            {
                                int offset = Stat.RandomCosmetic(-2, 2);
                                int roughlyHalfSelectedWords = Math.Min(Math.Max(0, (textWords.Count / 2) + offset), textWords.Count);
                                int latterRoughlyHalfSelectedWords = textWords.Count - roughlyHalfSelectedWords;
                                textWords.RemoveRange(roughlyHalfSelectedWords, latterRoughlyHalfSelectedWords);

                                if (textWords[^1] is string lastWord)
                                {
                                    string colorFormatOpen = null;
                                    string colorFormatClose = null;
                                    string shader = null;
                                    string fragment = null;
                                    if (lastWord.StartsWith("{{"))
                                    {
                                        colorFormatOpen = "{{";
                                        colorFormatClose = "}}";
                                        if (lastWord.EndsWith("}}"))
                                        {
                                            lastWord = lastWord[..^2];
                                        }
                                        if (lastWord.TryGetIndexOf("|", out int shaderIndex))
                                        {
                                            shader = lastWord[2..][..(shaderIndex - 2)];
                                            lastWord = lastWord[shaderIndex..];
                                        }
                                    }
                                    fragment = lastWord.FirstRoughlyHalf(1);

                                    Debug.Log(nameof(shader), shader, indent[2]);
                                    Debug.Log(nameof(fragment), fragment, indent[2]);

                                    textWords[^1] = colorFormatOpen + shader + fragment + colorFormatClose;
                                }
                                E.Text.Clear().Append(textWords.Aggregate(
                                    seed: "",
                                    func: delegate (string a, string n)
                                    {
                                        if (!a.IsNullOrEmpty())
                                            a += " ";
                                        return a + n;
                                    }));
                            }
                            else
                            {
                                E.Text.TrimLatterRoughlyHalf(5);
                            }
                            killedByPlayerString = "=no2nd.restore=... \n\n=ud_nbsp:4=... " + killedByPlayerString;
                        }
                        else
                        {
                            E.Text.Clear();
                        }

                        E.Text.Append(killedByPlayerString
                            .StartReplace()
                            .AddObject(E.Subject)
                            .AddObject(E.Object)
                            .AddObject(killer, "killer")
                            .AddObject(weapon, "weapon")
                            .ToString());

                        if (E.Text?.ToString()?.CapitalizeEx() is string capitalizedText)
                            E.Text?.Clear()?.Append(capitalizedText);
                    }
                }
            }
            string text = E.Text?.ToString()?.CapitalizeSentences(ExcludeElipses: true);

            E.Text.Clear().Append(text);

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(PrepareTextLateEvent E)
        {
            if (The.Speaker is GameObject speaker
                && The.Player is GameObject player
                && DebugEnableConversationDebugText
                && speaker.GetDeathDetails() is UD_FleshGolems_DeathDetails deathDetails)
            {
                string debugString = deathDetails.DeathMemory?.DebugInternalsString(speaker, " | ", true);
                if (debugString.TryGetIndexOf("RudeToAsk | ", out int firstPipeIndex))
                {
                    debugString = debugString[..(firstPipeIndex - 3)] + "\n" + debugString.TextAfter(" | ", 3);
                }
                E.Text += "\n\n{{K|" + debugString + "}}";
            }
            return base.HandleEvent(E);
        }
    }
}
