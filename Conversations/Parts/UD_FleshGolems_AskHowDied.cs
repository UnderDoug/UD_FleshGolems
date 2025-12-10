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

        public const string ASK_HOW_DIED_PROP = "UD_FleshGolems AskedHowDied";

        public static ConversationText DefaultRudeToAskText => new() { Text = "... That's a rude thing to ask..." };
        public static ConversationText DefaultKillerText => new() { Text = "I don't know, if anyone, who killed me..." };
        public static ConversationText DefaultMethodText => new() { Text = "... I don't know how I was killed..." };
        public static ConversationText DefaultCompleteText => new() { Text = "I don't know, if anyone, who killed me...\n\n... I don't know even how I was killed..." };
        public static ConversationText DefaultNoText => new() { Text = "... I actually can't remember at all...\n\nStrange!" };
        public static ConversationText DefaultPlayerKilledText => new() { Text = "IT WAS YOU!!" };

        public static Dictionary<string, DeathMemoryElements> DeathMemoryElementsValues
            => UD_FleshGolems_ReanimatedCorpse.DeathMemoryElementsValues;

        public bool KnowsPlayerKilledThem;

        public UD_FleshGolems_AskHowDied()
        {
            KnowsPlayerKilledThem = false;
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

        public static List<ConversationText> GetConversationTextsWithTextRetainingMemoryAttributes(
            ConversationText ConversationText,
            List<ConversationText> ConversationTextList)
            => ConversationText?.GetConversationTextsWithText(
                ConversationTextList: ConversationTextList,
                Proc: delegate (ConversationText subText)
                {
                    subText.Attributes ??= new();
                    if (ConversationText.HasAttribute(MEMORY_ELEMENT))
                        subText.Attributes[MEMORY_ELEMENT] = ConversationText.Attributes[MEMORY_ELEMENT];

                    if (ConversationText.HasAttribute(KNOWN))
                        subText.Attributes[KNOWN] = ConversationText.Attributes[KNOWN];
                })
            ?? new();

        public override void Awake()
        {
            base.Awake();
            KnowsPlayerKilledThem = The.Speaker is GameObject speaker
                && The.Player is GameObject player
                && speaker.KnowsEntityKilledThem(player);
        }

        public override bool WantEvent(int ID, int Propagation)
            => base.WantEvent(ID, Propagation)
            // || ID == IsElementVisibleEvent.ID
            || ID == GetTextElementEvent.ID
            || ID == PrepareTextEvent.ID
            || ID == EnteredElementEvent.ID
            ;
        public override bool HandleEvent(IsElementVisibleEvent E)
        {
            GameObject speaker = The.Speaker;
            if (!ConversationUI.StartNode.AllowEscape)
            {
                return false;
            }
            if (!speaker.IsCorpse())
            {
                return false;
            }
            if (!speaker.HasPart<UD_FleshGolems_ReanimatedCorpse>())
            {
                return false;
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
                && speaker.TryGetPart(out UD_FleshGolems_ReanimatedCorpse reanimatedCorpsePart)
                && reanimatedCorpsePart.KillerDetails is KillerDetails killerDetails
                && reanimatedCorpsePart.DeathMemory != UndefinedDeathMemoryElement)
            {
                List<ConversationText> possibleTexts = E.Texts
                    ?.Where(t => t.CheckPredicates())
                    ?.Aggregate(
                        seed: new List<ConversationText>(),
                        func: (acc, next) => GetConversationTextsWithTextRetainingMemoryAttributes(next, acc));

                Debug.Log(nameof(possibleTexts), Indent: indent[1]);
                foreach (ConversationText possibleText in possibleTexts)
                {
                    string attributesString = "(" + possibleText.Attributes?.ToStringForCachedDictionaryExpansion() + "): ";
                    Debug.Log(attributesString + possibleText.Text, Indent: indent[2]);
                    // possibleText?.Texts?.ForEach(ct => Debug.Log("(" + ct.Attributes?.ToStringForCachedDictionaryExpansion() + "): " + ct.Text, Indent: indent[3]));
                    Debug.Log(HONLY.ThisManyTimes(25), Indent: indent[2]);
                }

                if (reanimatedCorpsePart.DeathQuestionsAreRude)
                {
                    Debug.Log(nameof(reanimatedCorpsePart.DeathQuestionsAreRude), Indent: indent[1]);
                    E.Selected = possibleTexts
                        ?.Where(t => t.HasAttributeWithValue(RUDE_TO_ASK, "true"))
                        ?.ToList()
                        ?.GetRandomElementCosmetic()
                        ?? DefaultRudeToAskText;
                }
                else
                {
                    DeathMemoryElements corpseDeathMemoryFlags = reanimatedCorpsePart.DeathMemory;

                    Debug.Log("Looping " + nameof(possibleTexts), Indent: indent[1]);
                    Dictionary<DeathMemoryElements, List<ConversationText>> organizedTexts = new();
                    if (!possibleTexts.IsNullOrEmpty())
                    {
                        foreach (ConversationText conversationText in possibleTexts.Where(t => t.HasAttribute(MEMORY_ELEMENT)))
                        {
                            if (!conversationText.HasAttribute(MEMORY_ELEMENT))
                                continue;

                            bool elementIsUnknown = conversationText.HasAttributeWithValue(KNOWN, "false");

                            string memoryElementString = conversationText.Attributes[MEMORY_ELEMENT];

                            DeathMemoryElements key = GetDeathMemoryElementsFromString(memoryElementString);

                            Debug.Log(memoryElementString + " (" + (int)key + ")", Indent: indent[2]);
                            if (key != DeathMemoryElements.None
                                && (corpseDeathMemoryFlags.HasFlag(key)
                                    || (elementIsUnknown)))
                            {
                                if (!organizedTexts.ContainsKey(key))
                                {
                                    organizedTexts.Add(key, new());
                                }
                                organizedTexts[key] ??= new();

                                if (!conversationText.Texts.IsNullOrEmpty())
                                {
                                    Debug.Log(nameof(conversationText) + " has " + conversationText.Texts.Count + " Texts", Indent: indent[2]);
                                    foreach (ConversationText subText in conversationText.Texts)
                                        organizedTexts[key].Add(subText);
                                }
                                if (!conversationText.Text.IsNullOrEmpty())
                                {
                                    Debug.Log(nameof(conversationText) + " has single Text", Indent: indent[2]);
                                    organizedTexts[key].Add(conversationText);
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Log(nameof(possibleTexts), "empty", Indent: indent[2]);
                    }
                    List<ConversationText> killerTexts = organizedTexts
                        ?.Where(kvp => DeathMemoryElements.Killer.HasFlag(kvp.Key))
                        ?.Select(kvp => kvp.Value)
                        ?.Aggregate(new List<ConversationText>(), (current, accumulated) => current.ForEach(item => accumulated.Add(item), accumulated), s => s)
                        ?? new();

                    List<ConversationText> methodTexts = organizedTexts
                        ?.Where(kvp => DeathMemoryElements.Method.HasFlag(kvp.Key))
                        ?.Select(kvp => kvp.Value)
                        ?.Aggregate(new List<ConversationText>(), (current, accumulated) => current.ForEach(item => accumulated.Add(item), accumulated), s => s)
                        ?? new();

                    if (killerTexts.IsNullOrEmpty())
                    {
                        killerTexts ??= new();
                        killerTexts.Add(DefaultKillerText);
                    }
                    if (methodTexts.IsNullOrEmpty())
                    {
                        methodTexts ??= new();
                        methodTexts.Add(DefaultMethodText);
                    }

                    List<ConversationText> completeTexts = organizedTexts
                        ?.Where(kvp => DeathMemoryElements.Complete.HasFlag(kvp.Key))
                        ?.Select(kvp => kvp.Value)
                        ?.Aggregate(new List<ConversationText>(), (current, accumulated) => current.ForEach(item => accumulated.Add(item), accumulated), s => s)
                        ?? new();

                    foreach (string killerText in killerTexts.Select(ct => ct.Text))
                    {
                        foreach (string methodText in methodTexts.Select(ct => ct.Text))
                        {
                            ConversationText newText = new()
                            {
                                Text = killerText + "\n\n" + methodText,
                            };
                            completeTexts.Add(newText);
                        }
                    }
                    if (completeTexts.IsNullOrEmpty())
                    {
                        completeTexts ??= new();
                        completeTexts.Add(DefaultCompleteText);
                    }

                    Debug.Log(nameof(completeTexts), Indent: indent[1]);
                    foreach (ConversationText completeText in completeTexts)
                    {
                        Debug.Log(completeText.Text, Indent: indent[2]);
                        Debug.Log(HONLY.ThisManyTimes(25), Indent: indent[2]);
                    }

                    E.Selected = completeTexts
                        ?.GetRandomElementCosmetic()
                        ?? DefaultNoText;
                }
                if (KnowsPlayerKilledThem)
                {
                    Debug.Log(nameof(KnowsPlayerKilledThem), Indent: indent[1]);
                    List<ConversationText> playerKilledTexts = possibleTexts
                        ?.Where(t => t.HasAttributeWithValue("KilledByPlayer", "true"))
                        ?.ToList();

                    if (playerKilledTexts.IsNullOrEmpty())
                    {
                        playerKilledTexts ??= new();
                        playerKilledTexts.Add(DefaultPlayerKilledText);
                    }

                    E.Selected ??= DefaultNoText;

                    int selectedTextFinalIndex = E.Selected.Text.Length - 1;
                    int roughlyHalfSelectedText = Math.Min(Math.Max(1, (selectedTextFinalIndex / 2) + Stat.RandomCosmetic(-5, 5)), selectedTextFinalIndex);

                    E.Selected.Text =
                        E.Selected.Text[..roughlyHalfSelectedText] +
                        "\n\n...\n\n" +
                        playerKilledTexts?.GetRandomElementCosmetic()?.Text;
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(PrepareTextEvent E)
        {
            GameObject killer = null;
            GameObject weapon = null;

            string killerName = "mysterious entity";
            string killerCreatureType = killerName;
            string aKillerCreature = Grammar.A(killerName);
            string killerString = killerName;

            string weaponString = "deadly force";
            string feature = weaponString;
            string deathMethod = weaponString;

            string description = "killed to death";

            if (The.Speaker is GameObject speaker
                && speaker.TryGetPart(out UD_FleshGolems_ReanimatedCorpse reanimatedCorpsePart)
                && reanimatedCorpsePart.KillerDetails is KillerDetails killerDetails
                && reanimatedCorpsePart.DeathMemory != UndefinedDeathMemoryElement)
            {
                DeathMemoryElements deathMemory = reanimatedCorpsePart.DeathMemory;

                if (!killerDetails.DisplayName.IsNullOrEmpty()
                    && deathMemory.HasFlag(DeathMemoryElements.Killer))
                    killerString = killerDetails.KilledBy(deathMemory);

                if (!killerDetails.DisplayName.IsNullOrEmpty()
                    && deathMemory.HasFlag(DeathMemoryElements.KillerName))
                    killerName = killerDetails.DisplayName;

                if (!killerDetails.CreatureType.IsNullOrEmpty()
                    && deathMemory.HasFlag(DeathMemoryElements.KillerCreature))
                    killerCreatureType = killerDetails.CreatureType;

                killerDetails.KilledHow(
                    DeathMemory: deathMemory,
                    Weapon: out string deathWeapon,
                    Feature: out string deathFeature,
                    Description: out string deathDescription,
                    Accidentally: out bool accident,
                    Environment: out bool environment);

                if (!deathWeapon.IsNullOrEmpty()
                    && deathMemory.HasFlag(DeathMemoryElements.Weapon))
                    weaponString = deathWeapon;

                if (!deathFeature.IsNullOrEmpty()
                    && deathMemory.HasFlag(DeathMemoryElements.Feature))
                    feature = deathFeature;

                if (!deathDescription.IsNullOrEmpty()
                    && deathMemory.HasFlag(DeathMemoryElements.Description))
                    description = deathDescription;

                if ((!deathWeapon.IsNullOrEmpty()
                        || !deathFeature.IsNullOrEmpty()
                        || !deathDescription.IsNullOrEmpty())
                    && deathMemory.HasFlag(DeathMemoryElements.Method))
                    deathMethod = deathWeapon ?? deathFeature ?? deathDescription;

                if (GameObject.Validate(killerDetails.Killer))
                {
                    killer = killerDetails.Killer;
                }
                if (GameObject.Validate(killerDetails.Weapon))
                {
                    weapon = killerDetails.Weapon;
                }
            }

            static string lowerize(string String) => String[0].ToString().ToLower() + String[..1];

            E.Text
                .Replace("=Killer=", killerString.Capitalize())
                .Replace("=killer=", lowerize(killerString))
                .Replace("=KillerName=", killerName.Capitalize())
                .Replace("=killerName=", lowerize(killerName))
                .Replace("=KillerCreature=", killerCreatureType.Capitalize())
                .Replace("=killerCreature=", lowerize(killerCreatureType))
                .Replace("=AKillerCreature=", aKillerCreature.Capitalize())
                .Replace("=aKillerCreature=", lowerize(aKillerCreature))
                .Replace("=Method=", deathMethod.Capitalize())
                .Replace("=method=", lowerize(deathMethod))
                .Replace("=Weapon=", weaponString.Capitalize())
                .Replace("=weapon=", lowerize(weaponString))
                .Replace("=Feature=", feature.Capitalize())
                .Replace("=feature=", lowerize(feature))
                .Replace("=Description=", description.Capitalize())
                .Replace("=description=", lowerize(description));

            ReplaceBuilder RB = E.Text.StartReplace();

            if (GameObject.Validate(killer))
            {
                RB.AddObject(killer, "killer");
            }
            else
            {
                RB.AddExplicit(killerName, "killer", new Gender("nonspecific"));
            }
            if (GameObject.Validate(weapon))
            {
                RB.AddObject(weapon, "weapon");
            }
            else
            {
                RB.AddExplicit(weaponString, "weapon", new Gender("neuter"));
            }
            RB.Execute();
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnteredElementEvent E)
        {
            if (The.Speaker is GameObject speaker
                && The.Player is GameObject player)
            {
                string playerString = ";" + player.ID + ";";
                string askedHowDied = speaker.GetStringProperty(ASK_HOW_DIED_PROP, "");

                if (askedHowDied.IsNullOrEmpty() || !askedHowDied.Contains(playerString))
                {
                    speaker.SetStringProperty(ASK_HOW_DIED_PROP, askedHowDied + playerString);
                }

                if (KnowsPlayerKilledThem)
                {
                    E.Element.Attributes ??= new();
                    if (E.Element.HasAttribute("AllowEscape"))
                    {
                        E.Element.Attributes["AllowEscape"] = "false";
                    }
                    else
                    {
                        E.Element.Attributes.Add("AllowEscape", "false");
                    }
                    ParentElement.Elements?.RemoveAll(e => e is Choice && !e.HasPart<StartFight>());
                    if (ParentElement.Elements?.Where(e => e is Choice) is not List<IConversationElement> choices
                        || choices.IsNullOrEmpty())
                    {
                        Choice fightChoice = ParentElement.AddChoice(Text: "!", Target: "End");
                        fightChoice.AddPart(new StartFight());
                    }
                }
            }
            return base.HandleEvent(E);
        }
    }
}
