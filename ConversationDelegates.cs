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
        [ConversationDelegate]
        public static bool IfSourceOfSuffering(DelegateContext Context)
        {
            return Conversation.Speaker is GameObject speaker
                && speaker.TryGetEffect(out UD_FleshGolems_UnendingSuffering unendingSuffering)
                && unendingSuffering.SourceObject == Context.Target;
        }

        [ConversationDelegate]
        public static bool IfHaveCompanionWithBlueprint(DelegateContext Context)
        {
            return Context.Target.GetCompanionsReadonly(The.ActiveZone.Width / 2, GO => GO.Blueprint == Context.Value).Count > 0;
        }
    }
}
