using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UD_FleshGolems;
using UD_FleshGolems.Parts.VengeanceHelpers;

using XRL.UI;
using XRL.World.Parts;

using static XRL.World.Parts.UD_FleshGolems_ReanimatedCorpse;

namespace XRL.World.Conversations.Parts
{
    public class UD_FleshGolems_AskHowDied : IConversationPart
    {
        public const string ASK_HOW_DIED_PROP = "UD_FleshGolems AskedHowDied";

        public static Dictionary<string, DeathMemoryElements> DeathMemoryElementsValues
            => Startup.RequireCachedEnumValueDictionary<DeathMemoryElements>();

        public override bool WantEvent(int ID, int Propagation)
            => base.WantEvent(ID, Propagation)
            || ID == IsElementVisibleEvent.ID
            || ID == GetTextElementEvent.ID
            || ID == PrepareTextEvent.ID
            || ID == EnteredElementEvent.ID;

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
            if (speaker.HasPart<UD_FleshGolems_ReanimatedCorpse>())
            {
                return false;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetTextElementEvent E)
        {
            if (!E.Texts.IsNullOrEmpty()
                && The.Speaker is GameObject speaker
                && speaker.TryGetPart(out UD_FleshGolems_ReanimatedCorpse reanimatedCorpsePart)
                && reanimatedCorpsePart.KillerDetails is KillerDetails killerDetails
                && reanimatedCorpsePart.DeathMemory != int.MinValue)
            {

                bool knownKiller = reanimatedCorpsePart.DeathMemory
                List<ConversationText> killerTexts = E.Texts
                    ?.Where()
                    ?.ToList();
            }
            if (!base.Eliminated.Contains(base.Thief))
            {
                E.Selected = E.Texts.Find((ConversationText x) => x.ID == base.Circumstance?.ID) ?? E.Selected;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(PrepareTextEvent E)
        {
            if (The.Speaker is GameObject speaker
                && speaker.TryGetPart(out UD_FleshGolems_ReanimatedCorpse reanimatedCorpsePart)
                && reanimatedCorpsePart.KillerDetails is KillerDetails killerDetails
                && reanimatedCorpsePart.DeathMemory != int.MinValue)
            {

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
                    speaker.SetStringProperty(ASK_HOW_DIED_PROP, askedHowDied + playerString);

                if (speaker.KnowsEntityKilledThem(player))
                {
                    if (E.Element.Attributes.ContainsKey("AllowEscape"))
                    {
                        E.Element.Attributes["AllowEscape"] = "false";
                    }
                    else
                    {
                        E.Element.Attributes.Add("AllowEscape", "false");
                    }
                }
                    
            }
            return base.HandleEvent(E);
        }
    }
}
