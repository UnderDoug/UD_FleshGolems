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

using static XRL.World.Parts.UD_FleshGolems_ReanimatedCorpse;

namespace XRL.World.Conversations.Parts
{
    public class UD_FleshGolems_AskHowDied : IConversationPart
    {
        public const string RUDE_TO_ASK = "RudeToAsk";
        public const string MEMORY_ELEMENT = "MemoryElement";
        public const string KNOWN = "Known";
        public const string ENVIRONMENT = "Environment";
        public const string KILLED_BY_PLAYER = "KilledByPlayer";
        public const string PLAYER_LED = "PlayerLed";

        public const string ASK_HOW_DIED_PROP = "UD_FleshGolems AskedHowDied";
        public const string DUMMY_KILLER_BLUEPRINT = "UD_FleshGolems KillerDetails Dummy Killer";
        public const string DUMMY_WEAPON_BLUEPRINT = "UD_FleshGolems KillerDetails Dummy Weapon";

        public static ConversationText DefaultRudeToAskText => new() { Text = "... That's a rude thing to ask..." };
        public static ConversationText DefaultKillerText => new() { Text = "I don't know, if anyone, who killed me..." };
        public static ConversationText DefaultEnvironmentText => new() { Text = "No one is singularly responsible for my death..." };
        public static ConversationText DefaultMethodText => new() { Text = "... I just don't remember exactly how I was killed..." };
        public static ConversationText DefaultDescriptionText => new() { Text = "... I definitely died, though." };
        public static ConversationText DefaultCompleteText => new() { Text = "I don't know, if anyone, who killed me...\n\n... and I don't know even how I was killed...\n\n... though, certainly, I did die." };
        public static ConversationText DefaultNoText => new() { Text = "... I actually can't remember at all...\n\nStrange!" };
        public static ConversationText DefaultPlayerKilledText 
            => The.Speaker is GameObject speaker && !speaker.IsPlayerLed() 
            ? new() { Text = "IT WAS {{R|=killer.refname|upper=}}!!" }
            : new() { Text = "{{Y|=killer.Refname=}} killed me... but {{Y|=killer.subjective=}} already knew that... didn't {{Y|=killer.subjective=}}?" };

        public static Dictionary<string, DeathMemoryElements> DeathMemoryElementsValues
            => UD_FleshGolems_ReanimatedCorpse.DeathMemoryElementsValues;

        public List<ConversationText> CompleteTexts;

        public bool KnowsPlayerKilledThem;

        public List<ConversationText> KilledByPlayerTexts;

        public UD_FleshGolems_AskHowDied()
        {
            CompleteTexts = new();
            KnowsPlayerKilledThem = false;
            KilledByPlayerTexts = new();
        }

        public static DeathMemoryElements GetDeathMemoryElementsFromStringList(List<string> StringElements)
        {
            List<DeathMemoryElements> convertedList = StringElements
                ?.ConvertAll(
                    s => DeathMemoryElementsValues.ContainsKey(s) 
                    ? DeathMemoryElementsValues[s] 
                    : DeathMemoryElements.None)
                ?.Where(e => e != DeathMemoryElements.None)
                ?.ToList();
            if (convertedList.IsNullOrEmpty())
            {
                return DeathMemoryElements.None;
            }
            return convertedList?.Aggregate(DeathMemoryElements.None, (accumulated, next) => accumulated | next, s => s)
                ?? DeathMemoryElements.None;
        }
        public static DeathMemoryElements GetDeathMemoryElementsFromString(string String)
            => GetDeathMemoryElementsFromStringList(String?.CachedCommaExpansion());

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
        }

        public override bool WantEvent(int ID, int Propagation)
            => base.WantEvent(ID, Propagation)
            || ID == GetTextElementEvent.ID
            || ID == PrepareTextEvent.ID
            || ID == EnteredElementEvent.ID
            ;
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
                && speaker.TryGetPart(out UD_FleshGolems_ReanimatedCorpse reanimatedCorpsePart)
                && reanimatedCorpsePart.KillerDetails is KillerDetails killerDetails
                && reanimatedCorpsePart.DeathMemory != UndefinedDeathMemoryElement)
            {
                if (reanimatedCorpsePart.DeathQuestionsAreRude)
                {
                    Debug.Log(nameof(reanimatedCorpsePart.DeathQuestionsAreRude), Indent: indent[1]);

                    List<ConversationText> rudeToAskTexts = E.Texts
                        ?.Aggregate(
                            seed: new List<ConversationText>(),
                            func: (acc, next) => GetConversationTextsWithTextRetainingRudeToAskAttributes(next, ref acc));

                    if (rudeToAskTexts.IsNullOrEmpty())
                    {
                        rudeToAskTexts ??= new();
                        rudeToAskTexts.Add(DefaultRudeToAskText);
                    }

                    E.Selected = rudeToAskTexts.GetRandomElementCosmetic();

                    Debug.Log(nameof(rudeToAskTexts), Indent: indent[1]);
                    foreach (ConversationText rudeToAskText in rudeToAskTexts)
                    {
                        Debug.Log(rudeToAskText.Text, Indent: indent[2]);
                    }
                }
                else
                {
                    List<ConversationText> possibleTexts = E.Texts
                        ?.Aggregate(
                            seed: new List<ConversationText>(),
                            func: (acc, next) => GetConversationTextsWithTextRetainingMemoryAttributes(next, ref acc));

                    Debug.Log(nameof(possibleTexts), Indent: indent[1]);
                    foreach (ConversationText possibleText in possibleTexts)
                    {
                        string attributesString = "(" + possibleText.Attributes?.ToStringForCachedDictionaryExpansion() + "): ";
                        Debug.Log(attributesString + possibleText.Text, Indent: indent[2]);
                        Debug.Log(HONLY.ThisManyTimes(25), Indent: indent[2]);
                    }

                    DeathMemoryElements corpseDeathMemoryFlags = reanimatedCorpsePart.DeathMemory;

                    Debug.Log("Looping " + nameof(possibleTexts), Indent: indent[1]);
                    Dictionary<DeathMemoryElements, List<ConversationText>> organizedTexts = new();
                    if (!possibleTexts.IsNullOrEmpty())
                    {
                        foreach (ConversationText conversationText in possibleTexts.Where(t => t.HasAttribute(MEMORY_ELEMENT)))
                        {
                            if (!conversationText.HasAttribute(MEMORY_ELEMENT))
                                continue;

                            string memoryElementString = conversationText.Attributes[MEMORY_ELEMENT];

                            DeathMemoryElements key = GetDeathMemoryElementsFromString(memoryElementString);

                            bool elementIsKnown = conversationText.HasAttributeWithValue(KNOWN, "true");
                            bool memoryHasKey = corpseDeathMemoryFlags.HasFlag(key);

                            bool textIsAppropriate = (memoryHasKey && elementIsKnown)
                                || (!memoryHasKey && !elementIsKnown);

                            if (key != DeathMemoryElements.None
                                && textIsAppropriate)
                            {
                                if (!organizedTexts.ContainsKey(key))
                                {
                                    organizedTexts.Add(key, new());
                                }
                                organizedTexts[key] ??= new();

                                if (!conversationText.Text.IsNullOrEmpty())
                                {
                                    Debug.CheckYeh(
                                        memoryElementString + " (" + (int)key + ")", 
                                        conversationText.PathID + " Added (" + conversationText.Attributes?.ToStringForCachedDictionaryExpansion() + ")",
                                        Indent: indent[2]);
                                    organizedTexts[key].Add(conversationText);
                                }
                            }
                            else
                            {
                                Debug.CheckNah(conversationText.PathID + " Not Added (" + conversationText.Attributes?.ToStringForCachedDictionaryExpansion() + ")", Indent: indent[2]);
                            }
                        }
                    }
                    else
                    {
                        Debug.Log(nameof(possibleTexts), "empty", Indent: indent[2]);
                    }

                    List<List<ConversationText>> conversationTextsList = new();

                    List<ConversationText> killerTexts = organizedTexts
                        ?.Where(kvp => kvp.Key.HasKillerElement(Only: true))
                        ?.Select(kvp => kvp.Value)
                        ?.Aggregate(
                            seed: new List<ConversationText>(),
                            func: (current, accumulated) => current.ForEach(item => accumulated.Add(item), accumulated))
                        ?? new();

                    if (killerTexts.IsNullOrEmpty())
                    {
                        killerTexts ??= new();
                        killerTexts.Add(DefaultKillerText);
                    }

                    if (killerDetails.HasNoKiller() || killerDetails.WasEnvironment)
                    {
                        killerTexts = E.Texts
                            ?.Aggregate(
                                seed: new List<ConversationText>(),
                                func: (acc, next) => GetConversationTextsWithTextRetainingEnvironmentAttributes(next, ref acc))
                            ?? new();

                        if (killerTexts.IsNullOrEmpty())
                        {
                            killerTexts ??= new();
                            killerTexts.Add(DefaultEnvironmentText);
                        }
                    }
                    conversationTextsList.AddIf(killerTexts, e => !e.IsNullOrEmpty() && !conversationTextsList.Contains(e));

                    List<ConversationText> methodTexts = organizedTexts
                        ?.Where(kvp => kvp.Key.HasMethodElement(Only: true))
                        ?.Select(kvp => kvp.Value)
                        ?.Aggregate(
                            seed: new List<ConversationText>(),
                            func: (current, accumulated) => current.ForEach(item => accumulated.Add(item), accumulated))
                        ?? new();

                    if (methodTexts.IsNullOrEmpty())
                    {
                        methodTexts ??= new();
                        methodTexts.Add(DefaultMethodText);
                    }
                    conversationTextsList.AddIf(methodTexts, e => !e.IsNullOrEmpty() && !conversationTextsList.Contains(e));

                    List<ConversationText> descriptionTexts = organizedTexts
                        ?.Where(kvp => kvp.Key.HasDescriptionElement(Only: true))
                        ?.Select(kvp => kvp.Value)
                        ?.Aggregate(
                            seed: new List<ConversationText>(),
                            func: (current, accumulated) => current.ForEach(item => accumulated.Add(item), accumulated))
                        ?? new();

                    if (descriptionTexts.IsNullOrEmpty())
                    {
                        descriptionTexts ??= new();
                        descriptionTexts.Add(DefaultDescriptionText);
                    }
                    conversationTextsList.AddIf(descriptionTexts, e => !e.IsNullOrEmpty() && !conversationTextsList.Contains(e));



                    if (CompleteTexts.IsNullOrEmpty())
                    {
                        CompleteTexts = organizedTexts
                            ?.Where(kvp => kvp.Key.HasKillerElement() && kvp.Key.HasMethodElement() && kvp.Key.HasDescriptionElement())
                            ?.Select(kvp => kvp.Value)
                            ?.Aggregate(
                                seed: new List<ConversationText>(),
                                func: (current, accumulated) => current.ForEach(item => accumulated.Add(item), accumulated))
                            ?? new();

                        if (!conversationTextsList.IsNullOrEmpty()
                            && conversationTextsList.Count > 1)
                        {
                            List<List<ConversationText>> conversationTextsListForProcessing = conversationTextsList
                                ?.Where(l => !l.IsNullOrEmpty())
                                ?.Select(l => l?.Where(ct => ct?.Text != null)?.ToList())
                                ?.ToList();

                            List<ConversationText> firstList = null;
                            List<ConversationText> secondList = null;
                            int maxLoops = (conversationTextsList.Count * 3) + 50;
                            int counter = 0;
                            bool firstSuccessfulLoop = true;
                            while (!conversationTextsListForProcessing.IsNullOrEmpty()
                                && conversationTextsListForProcessing.Count > 1
                                && ++counter < maxLoops)
                            {
                                firstList = conversationTextsListForProcessing[^2];
                                secondList = conversationTextsListForProcessing[^1];

                                conversationTextsListForProcessing.Remove(firstList);
                                conversationTextsListForProcessing.Remove(secondList);

                                conversationTextsListForProcessing.Add(new());
                                bool any = false;
                                foreach (ConversationText firstText in firstList)
                                {
                                    foreach (ConversationText secondText in secondList)
                                    {
                                        string firstID = firstText?.Parent?.ID + "." + firstText?.ID;
                                        string secondID = (firstSuccessfulLoop ? secondText?.Parent?.ID + "." : null) + secondText?.ID;
                                        conversationTextsListForProcessing[^1].Add(new()
                                        {
                                            ID = firstID + ":" + secondID,
                                            Parent = ParentElement,
                                            Text = firstText.Text + /*"\n\n... "*/ " " + secondText.Text,
                                        });
                                        any = true;
                                    }
                                }
                                firstSuccessfulLoop = !any && firstSuccessfulLoop;
                            }
                            if (!conversationTextsListForProcessing.IsNullOrEmpty()
                                && !conversationTextsListForProcessing[0].IsNullOrEmpty())
                            {
                                conversationTextsListForProcessing[0].ForEach(ct => ct.ID = "[" + ct.ID + "]");
                                CompleteTexts.AddRange(conversationTextsListForProcessing[0]);
                            }
                        }
                    }

                    if (CompleteTexts.IsNullOrEmpty())
                        CompleteTexts ??= new()
                        {
                            DefaultCompleteText
                        };

                    Debug.Log(nameof(CompleteTexts), Indent: indent[1]);
                    foreach (ConversationText completeText in CompleteTexts)
                    {
                        Debug.Log(completeText.ID, Indent: indent[2]);
                        Debug.Log(HONLY.ThisManyTimes(25), Indent: indent[2]);
                    }

                    E.Selected = CompleteTexts
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

                string killerName = "mysterious entity";
                string killerCreatureType = killerName;
                string killerString = killerName;

                string weaponString = "deadly force";
                string feature = weaponString;
                string deathMethod = weaponString;

                string description = "killed to death";

                if (speaker.TryGetPart(out UD_FleshGolems_ReanimatedCorpse reanimatedCorpsePart)
                    && reanimatedCorpsePart.KillerDetails is KillerDetails killerDetails
                    && reanimatedCorpsePart.DeathMemory != UndefinedDeathMemoryElement)
                {
                    DeathMemoryElements deathMemory = reanimatedCorpsePart.DeathMemory;

                    if (deathMemory.HasFlag(DeathMemoryElements.Killer))
                        killerString = killerDetails.KilledBy(deathMemory);

                    if (!killerDetails.DisplayName.IsNullOrEmpty()
                        && deathMemory.HasFlag(DeathMemoryElements.KillerName))
                        killerName = killerDetails.DisplayName;

                    if (!killerDetails.CreatureType.IsNullOrEmpty()
                        && deathMemory.HasFlag(DeathMemoryElements.KillerCreature))
                    {
                        killerCreatureType = killerDetails.CreatureType;
                    }

                    killerDetails.KilledHow(
                        DeathMemory: deathMemory,
                        Weapon: out string deathWeapon,
                        Feature: out string deathFeature,
                        Description: out DeathDescription deathDescription,
                        Accidentally: out bool _,
                        Environment: out bool _);

                    if (!deathWeapon.IsNullOrEmpty()
                        && deathMemory.HasFlag(DeathMemoryElements.Weapon))
                        weaponString = deathWeapon;

                    if (!deathFeature.IsNullOrEmpty()
                        && deathMemory.HasFlag(DeathMemoryElements.NotableFeature))
                        feature = deathFeature;

                    if (deathDescription != null
                        && deathMemory.HasFlag(DeathMemoryElements.Description))
                        description = deathDescription.ToString();

                    if ((!deathWeapon.IsNullOrEmpty()
                            || !deathFeature.IsNullOrEmpty()
                            || deathDescription != null)
                        && deathMemory.HasFlag(DeathMemoryElements.Method))
                        deathMethod = weaponString ?? feature ?? description;

                    if (GameObject.Validate(killerDetails.Killer))
                    {
                        killer = killerDetails.Killer;
                        killerIsSample = false;
                    }
                    if (GameObject.Validate(killerDetails.Weapon))
                    {
                        weapon = killerDetails.Weapon;
                        weaponIsSample = false;
                    }
                }

                if (killerIsSample)
                {
                    killer.DisplayName = killerName;
                    killer.SetCreatureType(killerCreatureType);
                    killer.SetNotableFeature(feature);
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
                    && KilledByPlayerTexts?.GetRandomElementCosmetic()?.Text is string killedByPlayerString)
                {
                    if (!speaker.IsPlayerLed())
                    {
                        int selectedTextFinalIndex = E.Text.ToString().Length;
                        int roughlyHalfSelectedText = Math.Min(Math.Max(0, (selectedTextFinalIndex / 2) + Stat.RandomCosmetic(-5, 5)), selectedTextFinalIndex);
                        int latterRoughlyHalfSelectedText = selectedTextFinalIndex - roughlyHalfSelectedText;
                        Debug.Log(nameof(selectedTextFinalIndex), selectedTextFinalIndex, indent[1]);
                        Debug.Log(nameof(roughlyHalfSelectedText), roughlyHalfSelectedText, indent[1]);
                        Debug.Log(nameof(latterRoughlyHalfSelectedText), latterRoughlyHalfSelectedText, indent[1]);
                        E.Text.Remove(roughlyHalfSelectedText, latterRoughlyHalfSelectedText);
                        killedByPlayerString = "... \n\n=ud_nbsp:6=... " + killedByPlayerString;
                    }
                    else
                    {
                        E.Text.Clear();
                    }
                    E.Text.Append(killedByPlayerString)
                        .StartReplace()
                        .AddObject(E.Subject)
                        .AddObject(E.Object)
                        .AddObject(killer, "killer")
                        .AddObject(weapon, "weapon")
                        .Execute();
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnteredElementEvent E)
        {
            if (The.Speaker is GameObject speaker
                && The.Player is GameObject player)
            {
                string playerString = ";" + player.ID + ";";
                string askedHowDied = speaker.GetStringProperty(ASK_HOW_DIED_PROP, "");

                if (askedHowDied.IsNullOrEmpty()
                    || !askedHowDied.Contains(playerString))
                    speaker.SetStringProperty(ASK_HOW_DIED_PROP, askedHowDied + playerString);

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
    }
}
