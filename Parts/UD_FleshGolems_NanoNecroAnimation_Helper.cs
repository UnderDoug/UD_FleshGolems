using System;
using System.Collections.Generic;
using System.Text;

using NanoNecroAnimation = XRL.World.Parts.Mutation.UD_FleshGolems_NanoNecroAnimation;

using UD_FleshGolems;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_NanoNecroAnimation_Helper : IScribedPart
    {
        public bool IsReanimatableCorpse = false;
        public bool HasCorpse = false;

        public override void Attach()
        {
            IsReanimatableCorpse = NanoNecroAnimation.IsReanimatableCorpse(ParentObject);
            HasCorpse = NanoNecroAnimation.HasCorpse(ParentObject);
            base.Attach();
        }

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ((IsReanimatableCorpse || HasCorpse) && ID == GetInventoryActionsEvent.ID);

        public override bool HandleEvent(GetInventoryActionsEvent E)
        {
            if (E.Actor.HasPart<NanoNecroAnimation>() || E.Actor.HasPartDescendedFrom<NanoNecroAnimation>())
            {
                if (IsReanimatableCorpse)
                {
                    E.AddAction(
                        Name: "Reaimate Corpse",
                        Display: "reanimate corpse",
                        Command: NanoNecroAnimation.COMMAND_NAME_REANIMATE_ONE,
                        Key: 'C',
                        FireOnActor: true,
                        Priority: 3,
                        WorksAtDistance: true,
                        WorksTelekinetically: true,
                        WorksTelepathically: true);
                    E.AddAction(
                        Name: "Assess Corpse",
                        Display: "assess corpse",
                        Command: NanoNecroAnimation.COMMAND_NAME_POWERWORD_KILL,
                        Key: 'S',
                        FireOnActor: true,
                        Priority: 3,
                        WorksAtDistance: true,
                        WorksTelekinetically: true,
                        WorksTelepathically: true);
                }
                if (HasCorpse)
                {
                    E.AddAction(
                        Name: "Power Word: Kill",
                        Display: "power word: kill",
                        Command: NanoNecroAnimation.COMMAND_NAME_POWERWORD_KILL,
                        Key: 'K',
                        FireOnActor: true,
                        Priority: 3,
                        WorksAtDistance: true,
                        WorksTelekinetically: true,
                        WorksTelepathically: true);
                }
            }
            return base.HandleEvent(E);
        }
    }
}
