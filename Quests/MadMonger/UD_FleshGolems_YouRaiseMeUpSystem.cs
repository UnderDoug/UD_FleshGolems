using System;
using System.Collections.Generic;

using XRL;
using XRL.Rules;
using XRL.UI;
using XRL.World;

namespace XRL.World.QuestManagers
{
    [Serializable]
    public class UD_FleshGolems_YouRaiseMeUpSystem : IQuestSystem
    {
        public const string MongerQuestID = "You Raise Me Up";

        public static UD_FleshGolems_CorpseQuestSystem CorpseQuestSystem = The.Game.GetSystem<UD_FleshGolems_CorpseQuestSystem>();

        public UD_FleshGolems_YouRaiseMeUpSystem()
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
            if (E.Zone.X == 1 && E.Zone.Y == 1 && E.Zone.Z == 10 && E.Zone.GetTerrainObject()?.Blueprint == "TerrainFungalCenter")
            {
                The.Game.FinishQuestStep("Pax Klanq, I Presume?", "Seek the Heart of the Rainbow");
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(AfterConsumeEvent E)
        {
            if (E.Ingest && E.Object.Blueprint == "Godshroom Cap")
            {
                The.Game.FinishQuestStep("Pax Klanq, I Presume?", "Eat the God's Flesh");
            }
            return base.HandleEvent(E);
        }

        public override GameObject GetInfluencer()
        {
            return GameObject.FindByBlueprint("Pax Klanq");
        }
    }
}