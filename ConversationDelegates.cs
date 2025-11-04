using System;
using System.Collections.Generic;
using System.Text;

using XRL;
using XRL.World;
using XRL.World.Conversations;
using XRL.World.Effects;

namespace UD_FleshGolems
{
    [HasConversationDelegate]
    public static class ConversationDelegates
    {
        [ConversationDelegate(Speaker = true)]
        public static bool IfSourceOfSuffering(DelegateContext Context)
        {
            if (Conversation.Speaker is GameObject speaker
                && The.Player is GameObject player)
            {
                GameObject sufferer = null;
                if (Context.Target == speaker)
                {
                    sufferer = player;
                }
                else
                {
                    sufferer = speaker;
                }
                return sufferer.TryGetEffect(out UD_FleshGolems_UnendingSuffering unendingSuffering)
                    && unendingSuffering.SourceObject == Context.Target;
            }
            return false;
        }

        [ConversationDelegate(Speaker = true)]
        public static bool IfHaveCompanionWithBlueprint(DelegateContext Context)
        {
            foreach (GameObject companion in Context.Target.GetCompanionsReadonly(The.ActiveZone.Width))
            {
                UnityEngine.Debug.Log("    [" + companion.Blueprint);
            }
            return Context.Target.GetCompanionsReadonly(The.ActiveZone.Width, GO => GO.Blueprint == Context.Value).Count > 0;
        }
    }
}
