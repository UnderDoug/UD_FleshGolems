using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Parts.VengeanceHelpers;

using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.World.Parts;
using XRL.World.Text;

using static XRL.World.Parts.UD_FleshGolems_ReanimatedCorpse;

namespace XRL.World.Conversations.Parts
{
    public class UD_FleshGolems_AskHowDied : IConversationPart
    {
        public const string ASK_HOW_DIED_PROP = "UD_FleshGolems AskedHowDied";

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
                    ?.ToList();

                Debug.Log(nameof(possibleTexts), Indent: indent[1]);
                foreach (ConversationText possibleText in possibleTexts)
                {
                    string attributesString = "(" + possibleText.Attributes?.ToStringForCachedDictionaryExpansion() + "): ";
                    Debug.Log(attributesString + possibleText.Text, Indent: indent[2]);
                    Debug.Log("______________________", Indent: indent[2]);
                }

                if (reanimatedCorpsePart.DeathQuestionsAreRude)
                {
                    Debug.Log(nameof(reanimatedCorpsePart.DeathQuestionsAreRude), Indent: indent[1]);
                    E.Selected = possibleTexts
                        ?.Where(t => !t.HasAttributeWithValue("RudeToAsk", "true"))
                        ?.ToList()
                        ?.GetRandomElementCosmetic()
                        ?? new() { Text = "... That's a rude thing to ask..." };
                }
                else
                {
                    DeathMemoryElements corpseDeathMemoryFlags = reanimatedCorpsePart.DeathMemory;

                    Debug.Log("Looping " + nameof(possibleTexts), Indent: indent[1]);
                    Dictionary<DeathMemoryElements, List<ConversationText>> organizedTexts = new();
                    foreach (ConversationText conversationText in possibleTexts)
                    {
                        if (conversationText.Attributes.IsNullOrEmpty()
                            || !conversationText.Attributes.ContainsKey("MemoryElement"))
                            continue;

                        bool elementIsUnknown = conversationText.HasAttributeWithValue("Known", "true");

                        string memoryElementString = conversationText.Attributes["MemoryElement"];

                        DeathMemoryElements key = GetDeathMemoryElementsFromString(conversationText.Attributes["MemoryElement"]);

                        Debug.Log(memoryElementString + " (" + (int)key + ")", Indent: indent[2]);
                        if (key != DeathMemoryElements.None
                            && (corpseDeathMemoryFlags.HasFlag(key)
                            || (conversationText.HasAttribute("Known")
                                && elementIsUnknown)))
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
                    List<ConversationText> killerTexts = organizedTexts
                        ?.Where(kvp => kvp.Key.HasFlag(DeathMemoryElements.Killer))
                        ?.Select(kvp => kvp.Value)
                        ?.Aggregate(new List<ConversationText>(), (current, accumulated) => current.ForEach(item => accumulated.Add(item), accumulated), s => s)
                        ?? new() { new() { Text = "I don't know, if anyone, who killed me..." }, };

                    List<ConversationText> methodTexts = organizedTexts
                        ?.Where(kvp => kvp.Key.HasFlag(DeathMemoryElements.Method))
                        ?.Select(kvp => kvp.Value)
                        ?.Aggregate(new List<ConversationText>(), (current, accumulated) => current.ForEach(item => accumulated.Add(item), accumulated), s => s)
                        ?? new() { new() { Text = "... I don't know how I was killed..." }, };

                    List<ConversationText> completeTexts = organizedTexts
                        ?.Where(kvp => kvp.Key.HasFlag(DeathMemoryElements.Complete))
                        ?.Select(kvp => kvp.Value)
                        ?.Aggregate(new List<ConversationText>(), (current, accumulated) => current.ForEach(item => accumulated.Add(item), accumulated), s => s)
                        ?? new() { new() { Text = "I don't know, if anyone, who killed me...\n\n... I don't know even how I was killed..." }, };

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

                    Debug.Log(nameof(completeTexts), Indent: indent[1]);
                    foreach (ConversationText completeText in completeTexts)
                        Debug.Log(completeText.Text + "\n______________________", Indent: indent[2]);

                    E.Selected = completeTexts
                        ?.GetRandomElementCosmetic()
                        ?? new() { Text = "... I actually can't remember at all...\n\nStrange!" };
                }
                List<ConversationText> playerKilledTexts = null;
                if (KnowsPlayerKilledThem)
                {
                    Debug.Log(nameof(KnowsPlayerKilledThem), Indent: indent[1]);
                    playerKilledTexts = possibleTexts
                        ?.Where(t => t.HasAttributeWithValue("KilledByPlayer", "true"))
                        ?.ToList();

                    int roughlyHalfSelectedText = (E.Selected.Text.Length / 2) + Stat.RandomCosmetic(-5, 5);
                    E.Selected.Text =
                        E.Selected.Text[..roughlyHalfSelectedText] +
                        "\n\n...\n\n" +
                        playerKilledTexts.GetRandomElementCosmetic().Text;
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
