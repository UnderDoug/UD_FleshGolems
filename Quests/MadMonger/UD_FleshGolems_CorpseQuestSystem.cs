using System;
using System.Collections.Generic;
using System.Globalization;

using Qud.API;

using XRL;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;

namespace XRL.World.QuestManagers
{
    [Serializable]
    public class UD_FleshGolems_CorpseQuestSystem : IQuestSystem
    {
        public static string questName = "Find some questionable materials";

        public Quest ParentQuest;

        public List<UD_FleshGolems_CorpseQuestItem> Steps;

        public UD_FleshGolems_CorpseQuestSystem()
        {
            ParentQuest = null;
            Steps = new();
        }

        public static IEnumerable<string> GetAllSpecies()
        {
            List<string> speciesList = new();
            foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.BlueprintList)
            {
                if (bp.Tags.ContainsKey("Species")
                    && bp.Tags["Species"] is string species
                    && !speciesList.Contains(species))
                {
                    speciesList.Add(species);
                    yield return species;
                }
            }
        }
        public static IEnumerable<string> GetAllCorpsesOfSpecies(string Species)
        {
            List<string> corpseBlueprintList = new();
            foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.BlueprintList)
            {
                if (bp.Tags.ContainsKey("Species")
                    && bp.Tags["Species"] is string species
                    && species == Species
                    && bp.TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseBlueprint), out string corpseBleprint)
                    && !corpseBlueprintList.Contains(corpseBleprint))
                {
                    corpseBlueprintList.Add(corpseBleprint);
                    yield return corpseBleprint;
                }
            }
        }
        public bool CheckCorpseSpecies(UD_FleshGolems_CorpseQuestItem QuestItem, GameObject CorpseObject)
        {
            return QuestItem != null
                && CorpseObject != null
                && QuestItem.Corpse.Type != "Species"
                && new List<string>(GetAllCorpsesOfSpecies(QuestItem.Corpse.Value)).Contains(CorpseObject.Blueprint);
        }

        public void Init()
        {
            if (Steps != null)
            {
                return;
            }
            Steps ??= new();
            List<string> list = new List<string>(paxPlaces);
            if (!The.Game.GetStringGameState("embark").Contains("Joppa"))
            {
                list.RemoveAll(paxPlacesExcludeAltStarts.Contains<string>);
            }
            int num = 6;
            while (Steps.Count < num)
            {
                UD_FleshGolems_CorpseQuestItem corpseQuestStep = new();
                switch (Stat.Random(1, 4))
                {
                    case 1:
                        corpseQuestStep.Name = "Spread Klanq Deep in the Earth";
                        corpseQuestStep.Target = "Underground:20";
                        corpseQuestStep.Text = "Puff Klanq spores at a depth of at least 20 levels.";
                        break;
                    case 2:
                        {
                            Faction randomFactionWithAtLeastOneMember = Factions.GetRandomFactionWithAtLeastOneMember((Faction f) => !f.Name.Contains("villagers of"));
                            TextInfo textInfo2 = new CultureInfo("en-US", useUserOverride: false).TextInfo;
                            corpseQuestStep.Name = "Spread Klanq to " + textInfo2.ToTitleCase(randomFactionWithAtLeastOneMember.DisplayName);
                            corpseQuestStep.Target = "Faction:" + randomFactionWithAtLeastOneMember.Name;
                            corpseQuestStep.Text = "Puff Klanq spores on a sentient member of the " + randomFactionWithAtLeastOneMember.DisplayName + " faction.";
                            break;
                        }
                    case 3:
                        {
                            string anObjectBlueprint = EncountersAPI.GetAnObjectBlueprint((GameObjectBlueprint ob) => ob.GetPartParameter("Physics", "IsReal", Default: true) && ob.GetPartParameter("Physics", "Takeable", Default: true) && !ob.HasPart("Brain") && !ob.HasPart("Combat") && !ob.HasTag("NoSparkingQuest"));
                            GameObject gameObject = GameObject.Create(anObjectBlueprint);
                            TextInfo textInfo = new CultureInfo("en-US", useUserOverride: false).TextInfo;
                            corpseQuestStep.Name = "Spread Klanq onto " + textInfo.ToTitleCase(gameObject.GetReferenceDisplayName(int.MaxValue, null, null, NoColor: false, Stripped: true, ColorOnly: false, WithoutTitles: false, Short: true, BaseOnly: false, WithIndefiniteArticle: true));
                            corpseQuestStep.Target = "Item:" + anObjectBlueprint;
                            corpseQuestStep.Text = "Puff Klanq spores onto " + gameObject.GetReferenceDisplayName(int.MaxValue, null, null, NoColor: false, Stripped: true, ColorOnly: false, WithoutTitles: false, Short: true, BaseOnly: false, WithIndefiniteArticle: true);
                            break;
                        }
                    case 4:
                        {
                            string randomElement = list.GetRandomElement();
                            if (!paxPlacesDisplay.TryGetValue(randomElement, out var value))
                            {
                                value = randomElement;
                            }
                            if (!paxPlacesPreposition.TryGetValue(randomElement, out var value2))
                            {
                                value2 = "in";
                            }
                            corpseQuestStep.Name = "Spread Klanq " + value2 + " " + value;
                            corpseQuestStep.Target = "Place:" + randomElement;
                            corpseQuestStep.Text = "Puff Klanq spores in the vicinity of " + value + ".";
                            break;
                        }
                }
                if (!hasStepName(corpseQuestStep.Name))
                {
                    Steps.Add(corpseQuestStep);
                }
            }
        }
        private bool hasStepName(string s)
        {
            for (int i = 0; i < Steps.Count; i++)
            {
                if (Steps[i].Name == s)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool StartQuest()
        {
            try
            {
                Quest quest = new Quest();
                SpreadPax spreadPax = The.Game.RequireSystem<SpreadPax>();
                spreadPax.Init();
                quest.ID = Guid.NewGuid().ToString();
                quest.System = spreadPax;
                quest.Name = questName;
                quest.Level = 25;
                quest.Finished = false;
                quest.Accomplishment = "Conspiring with its eponymous mushroom scientist, you spread Klanq throughout Qud.";
                quest.Hagiograph = "Bless the " + Calendar.GetDay() + " of " + Calendar.GetMonth() + ", when =name= cemented a historic alliance with the godhead Klanq and the two became one! Together the being known as Klanq-=name= puffed the Royal Vapor into every nook and crevice of Qud.";
                quest.HagiographCategory = "DoesSomethingRad";
                quest.StepsByID = new Dictionary<string, QuestStep>();
                spreadPax.MyQuestID = quest.ID;
                for (int i = 0; i < spreadPax.Steps.Count; i++)
                {
                    QuestStep questStep = new QuestStep();
                    questStep.ID = Guid.NewGuid().ToString();
                    questStep.Name = spreadPax.Steps[i].Name;
                    questStep.Finished = false;
                    questStep.Text = spreadPax.Steps[i].Text;
                    questStep.XP = 1500;
                    spreadPax.Steps[i].Name = questStep.ID;
                    quest.StepsByID.Add(questStep.ID, questStep);
                }
                The.Game.StartQuest(quest, "Pax Klanq");
            }
            catch (Exception x)
            {
                MetricsManager.LogException("SpreadPax.StartQuest", x);
            }
            return true;
        }

        public override void Finish()
        {
            if (ParentQuest == null)
            {
                The.Game.TryGetQuest(UD_FleshGolems_YouRaiseMeUpSystem.MongerQuestID, out ParentQuest);
            }
            if (ParentQuest != null)
            {
                ParentQuest.FinishStep(ParentQuest.StepsByID[questName]);
            }
            base.Finish();
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