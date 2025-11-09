using System;
using System.Collections.Generic;

using XRL;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.QuestManagers;

namespace XRL.World.Quests
{
    [Serializable]
    public class UD_FleshGolems_YouRaiseMeUpQuestSystem : IQuestSystem
    {
        public const string MongerQuestID = "You Raise Me Up";

        public static UD_FleshGolems_CorpseQuestSystem CorpseQuestSystem = The.Game.GetSystem<UD_FleshGolems_CorpseQuestSystem>();

        public UD_FleshGolems_YouRaiseMeUpQuestSystem()
        {
            QuestID = MongerQuestID;
        }

        public override void Start()
        {
            UD_FleshGolems_CorpseQuestSystem.StartQuest();
            base.Start();
        }

        public override void Register(XRLGame Game, IEventRegistrar Registrar)
        {
            Registrar.Register(ZoneActivatedEvent.ID);
        }

        public override void RegisterPlayer(GameObject Player, IEventRegistrar Registrar)
        {
            Registrar.Register(AfterConsumeEvent.ID);
        }

        public override bool HandleEvent(ZoneActivatedEvent E)
        {
            
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(AfterConsumeEvent E)
        {
            
            return base.HandleEvent(E);
        }

        public override GameObject GetInfluencer()
        {
            return GameObject.FindByBlueprint("UD_FleshGolems Mad Monger");
        }
    }
}