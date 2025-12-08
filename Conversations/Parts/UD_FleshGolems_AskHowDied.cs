using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Parts.VengeanceHelpers;

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

                if (reanimatedCorpsePart.DeathQuestionsAreRude)
                {
                    E.Selected = possibleTexts
                        ?.Where(t => !t.HasAttributeWithValue("RudeToAsk", "true"))
                        ?.ToList()
                        ?.GetRandomElementCosmetic()
                        ?? new() { Text = "... That's a rude thing to ask..." };
                }
                else
                {
                    DeathMemoryElements corpseDeathMemoryFlags = reanimatedCorpsePart.DeathMemory;

                    Dictionary<DeathMemoryElements, List<ConversationText>> organizedTexts = new();
                    foreach (ConversationText conversationText in possibleTexts)
                    {
                        if (conversationText.Attributes.IsNullOrEmpty()
                            || !conversationText.Attributes.ContainsKey("MemoryElement"))
                            continue;

                        bool elementIsUnknown = conversationText.HasAttributeWithValue("Known", "true");

                        DeathMemoryElements key = GetDeathMemoryElementsFromString(conversationText.Attributes["MemoryElement"]);
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
                                foreach (ConversationText subText in conversationText.Texts)
                                    organizedTexts[key].Add(subText);
                            else
                                organizedTexts[key].Add(conversationText);
                        }
                    }
                    List<ConversationText> killerTexts = organizedTexts
                        ?.Where(kvp => DeathMemoryElements.Killer.HasFlag(kvp.Key))
                        ?.Select(kvp => kvp.Value)
                        ?.Aggregate(new List<ConversationText>(), (current, accumulated) => current.ForEach(item => accumulated.Add(item), accumulated), s => s)
                        ?? new() { new() { Text = "I don't know who, if anyone, killed me..." }, };

                    List<ConversationText> methodTexts = organizedTexts
                        ?.Where(kvp => DeathMemoryElements.Method.HasFlag(kvp.Key))
                        ?.Select(kvp => kvp.Value)
                        ?.Aggregate(new List<ConversationText>(), (current, accumulated) => current.ForEach(item => accumulated.Add(item), accumulated), s => s)
                        ?? new() { new() { Text = "... I don't know how I was killed..." }, };

                    List<ConversationText> completeTexts = organizedTexts
                        ?.Where(kvp => DeathMemoryElements.Complete.HasFlag(kvp.Key))
                        ?.Select(kvp => kvp.Value)
                        ?.Aggregate(new List<ConversationText>(), (current, accumulated) => current.ForEach(item => accumulated.Add(item), accumulated), s => s)
                        ?? new() { new() { Text = "I don't know who, if anyone, killed me...\n\n... I don't know how I was killed..." }, };

                    foreach (ConversationText killerText in killerTexts)
                        foreach (ConversationText methodText in methodTexts)
                            completeTexts.Add(new()
                            {
                                Text = killerText.Text + "\n\n" + methodText.Text,
                            });

                    using Indent indent = new(1);
                    Debug.LogMethod(indent,
                        ArgPairs: new Debug.ArgPair[]
                        {
                            Debug.Arg(nameof(speaker), speaker.DebugName),
                        });
                    foreach (ConversationText completeText in completeTexts)
                        Debug.Log(completeText.Text, Indent: indent[1]);

                    E.Selected = completeTexts
                        ?.GetRandomElementCosmetic()
                        ?? new() { Text = "... I actually can't remember at all...\n\nStrange!" };
                }
                List<ConversationText> playerKilledTexts = null;
                if (KnowsPlayerKilledThem)
                {
                    playerKilledTexts = possibleTexts
                        ?.Where(t => t.HasAttributeWithValue("KilledByPlayer", "true"))
                        ?.ToList();

                    int roughlyHalfSelectedText = (E.Selected.Text.Length / 2) + Stat.RandomCosmetic(-5, 5);
                    E.Selected.Text = E.Selected.Text[roughlyHalfSelectedText..] + "\n\n...\n\n" + playerKilledTexts.GetRandomElementCosmetic().Text;
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(PrepareTextEvent E)
        {
            if (The.Speaker is GameObject speaker
                && speaker.TryGetPart(out UD_FleshGolems_ReanimatedCorpse reanimatedCorpsePart)
                && reanimatedCorpsePart.KillerDetails is KillerDetails killerDetails
                && reanimatedCorpsePart.DeathMemory != UndefinedDeathMemoryElement)
            {
                DeathMemoryElements deathMemory = reanimatedCorpsePart.DeathMemory;
                string killerName = killerDetails.DisplayName ?? "mysterious entity";
                string creatureType = killerDetails.CreatureType ?? "mysterious entity";
                killerDetails.KilledHow(deathMemory, out string weapon, out string feature, out string description, out bool accident, out bool environment);
                string deathMethod = weapon ?? feature ?? "deadly force";
                weapon ??= "deadly force";
                feature ??= "deadly force";
                description ??= "killed to death";
                ReplaceBuilder RB = E.Text.StartReplace()
                    .AddReplacer("Killer", killerDetails.KilledBy(deathMemory, true))
                    .AddReplacer("killer", killerDetails.KilledBy(deathMemory))
                    .AddReplacer("KillerName", killerName.Capitalize())
                    .AddReplacer("killerName", killerName[0].ToString().ToLower() + killerName[..1])
                    .AddReplacer("KillerCreature", creatureType.Capitalize())
                    .AddReplacer("killerCreature", creatureType[0].ToString().ToLower() + creatureType[..1])
                    .AddReplacer("AKillerCreature", killerDetails.KilledByA(deathMemory, true))
                    .AddReplacer("aKillerCreature", killerDetails.KilledByA(deathMemory))
                    .AddReplacer("Method", deathMethod.Capitalize())
                    .AddReplacer("method", deathMethod[0].ToString().ToLower() + deathMethod[..1])
                    .AddReplacer("Weapon", weapon.Capitalize())
                    .AddReplacer("weapon", weapon[0].ToString().ToLower() + weapon[..1])
                    .AddReplacer("Feature", feature.Capitalize())
                    .AddReplacer("feature", feature[0].ToString().ToLower() + feature[..1])
                    .AddReplacer("Description", description.Capitalize())
                    .AddReplacer("description", description[0].ToString().ToLower() + description[..1]);
                
                if (GameObject.Validate(killerDetails.Killer))
                {
                    RB.AddObject(killerDetails.Killer, "killer");
                }
                else
                {
                    RB.AddExplicit(killerDetails.DisplayName, "killer", new Gender("nonspecific"));
                }
                if (GameObject.Validate(killerDetails.Weapon))
                {
                    RB.AddObject(killerDetails.Weapon, "weapon");
                }
                else
                {
                    RB.AddExplicit(killerDetails.WeaponName, "weapon", new Gender("neuter"));
                }
                RB.Execute();
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

                if (askedHowDied.IsNullOrEmpty() || !askedHowDied.Contains(playerString))
                {
                    speaker.SetStringProperty(ASK_HOW_DIED_PROP, askedHowDied + playerString);
                }

                if (KnowsPlayerKilledThem)
                {
                    if (E.Element.Attributes.ContainsKey("AllowEscape"))
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
