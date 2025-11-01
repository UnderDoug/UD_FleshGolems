using System;
using System.Collections.Generic;
using System.Text;

namespace XRL.World.Parts
{
    [Serializable]
    public class UnderDoug_FleshGolems_CoprseReanimationHelper : IScribedPart
    {

        public override bool AllowStaticRegistration()
        {
            return true;
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == AnimateEvent.ID;
        }

        public override bool HandleEvent(AnimateEvent E)
        {
            if (ParentObject is GameObject frankenCorpse
                && frankenCorpse == E.Object)
            {
                if (frankenCorpse.TryGetPart(out ConversationScript convo)
                    && convo.ConversationID == "NewlySentientBeings")
                {
                    frankenCorpse.RemovePart(convo);
                    frankenCorpse.AddPart(new ConversationScript("UnderDoug_FleshGolems NewlyReanimatedBeings"));
                }
                if (frankenCorpse.GetStringProperty("SourceBlueprint") is string sourceBlueprint)
                {

                }
                frankenCorpse.Body.Rebuild("SlugWithHands");
            }
            return base.HandleEvent(E);
        }
    }
}
