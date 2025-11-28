using System;
using System.Collections.Generic;
using System.Text;

using UD_FleshGolems;
using UD_FleshGolems.Capabilities;

namespace XRL.World.Parts
{
    public class UD_FleshGolems_ReplaceCorpse : IScribedPart
    {
        public override bool AllowStaticRegistration()
            => true;

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(BeforeObjectCreatedEvent.ID, EventOrder.EXTREMELY_EARLY);
            base.Register(Object, Registrar);
        }

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            ; // || ID == BeforeObjectCreatedEvent.ID;

        public override bool HandleEvent(BeforeObjectCreatedEvent E)
        {
            if (ParentObject is GameObject corpseToReplace
                && corpseToReplace == E.Object
                && UD_FleshGolems_NecromancySystem.IsReanimatableCorpse(corpseToReplace.GetBlueprint()))
            {
                if (!corpseToReplace.TryGetPart(out UD_FleshGolems_PastLife pastLifePart) || !pastLifePart.Init)
                {
                    pastLifePart = corpseToReplace.RequirePart<UD_FleshGolems_PastLife>().Initialize();
                }
                GameObject replacementCorpse = null;
                if (pastLifePart.Corpse?.CorpseBlueprint is string pastLifeCorpseBlueprint
                    && pastLifeCorpseBlueprint != corpseToReplace.Blueprint
                    && !pastLifeCorpseBlueprint.GetGameObjectBlueprint().HasPart(nameof(ReplaceObject)))
                {
                    replacementCorpse = GameObjectFactory.Factory.CreateObject(pastLifeCorpseBlueprint);
                }
                else
                if (pastLifePart.GetBlueprint().TryGetCorpseBlueprint(out pastLifeCorpseBlueprint)
                    && pastLifeCorpseBlueprint != corpseToReplace.Blueprint
                    && !pastLifeCorpseBlueprint.GetGameObjectBlueprint().HasPart(nameof(ReplaceObject)))
                {
                    replacementCorpse = GameObjectFactory.Factory.CreateObject(pastLifeCorpseBlueprint);
                }
                if (replacementCorpse != null)
                {
                    if (corpseToReplace.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper oldCorpseReanimationHelper)
                        && replacementCorpse.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper newCorpseReanimationHelper))
                    {
                        newCorpseReanimationHelper.AlwaysAnimate = oldCorpseReanimationHelper.AlwaysAnimate;
                    }
                    E.ReplacementObject = replacementCorpse;
                }
            }
            return base.HandleEvent(E);
        }
    }
}
