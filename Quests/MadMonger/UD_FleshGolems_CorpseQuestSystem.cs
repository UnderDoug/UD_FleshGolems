using System;
using System.Collections.Generic;
using System.Linq;

using XRL;
using XRL.Rules;
using XRL.UI;
using XRL.World.Parts;
using XRL.World.Quests;
using XRL.Language;
using XRL.Wish;

using CorpseItem = XRL.World.QuestManagers.UD_FleshGolems_CorpseQuestStep.CorpseItem;
using CorpseTaxonomy = XRL.World.QuestManagers.UD_FleshGolems_CorpseQuestStep.CorpseItem.CorpseTaxonomy;

using UD_FleshGolems;
using UD_FleshGolems.Logging;

namespace XRL.World.QuestManagers
{
    [HasWishCommand]
    [Serializable]
    public class UD_FleshGolems_CorpseQuestSystem : IQuestSystem
    {
        public static int RequiredToComplete => 3;

        public static string QuestName = "Find 3 Questionable Materials (for Science)";

        public static string QuestGiverBlueprint = "UD_FleshGolems Mad Monger";

        public const string PREVIOUSLY_SENTIENT_BEINGS = "Previously Sentient Beings";

        public static List<string> SpeciesExclusions => new()
        {
            "robot",
            "species",
            "baetyl",
            "mecha",
            "mech",
            "infrastructure",
            "slime",
            "humor",
            "clam",
            "living flesh",
            "*",
            "[",
            "]",
        };

        public static List<string> FactionExclusions => new()
        {
            "robots",
            "slimes",
            "oozes",
            "baetyl",
        };

        public static List<string> AllBaseCorpses => new(GetAllBaseCorpses());
        public static List<string> AllSpecies => new(GetAllSpecies(SpeciesIsNotExcluded));
        public static List<string> AllFactions => new(GetFactionsWithLivingMembersWhoDropCorpses());

        public UD_FleshGolems_YouRaiseMeUpQuestSystem ParentSystem;

        public Quest ParentQuest;

        public string MyQuestID;

        public QuestStep ParentQuestStep => (ParentQuest != null && !ParentQuest.StepsByID.IsNullOrEmpty())
            ? ParentQuest.StepsByID["UD_FleshGolems I'm dead serious!"]
            : null;

        public List<UD_FleshGolems_CorpseQuestStep> Steps;

        public int CompletedSteps => (from step in Steps where step.Finished select step).Count();

        public bool Completable// => CompletedSteps >= RequiredToComplete;
        {
            get
            {
                Quest?.SetProperty(nameof(CompletedSteps), CompletedSteps);
                Quest?.SetProperty(nameof(Completable), (CompletedSteps >= RequiredToComplete).ToString());
                return Quest != null && Quest.GetProperty(nameof(Completable)).EqualsNoCase("true");
            }
        }

        public string InfluencerRefName => GetInfluencer()?.GetReferenceDisplayName(Short: true);

        [NonSerialized]
        private bool PlayerAdvisedCompletable;

        private GameObject _Influencer;

        public UD_FleshGolems_CorpseQuestSystem()
        {
            ParentSystem = null;
            ParentQuest = null;

            MyQuestID = null;

            Steps = new();

            PlayerAdvisedCompletable = false;

            _Influencer = null;
        }

        public override GameObject GetInfluencer() => _Influencer ??=
            GameObject.FindByBlueprint(QuestGiverBlueprint) 
            ?? GameObject.CreateSample(QuestGiverBlueprint);

        public static bool IsCorpse(UD_FleshGolems_CorpseQuestStep QuestItem, out CorpseItem CorpseItem)
        {
            CorpseItem = QuestItem?.Corpse;
            return QuestItem != null
                && CorpseItem != null;
        }
        public static bool IsCorpse(UD_FleshGolems_CorpseQuestStep QuestItem)
        {
            return IsCorpse(QuestItem, out _);
        }

        public static IEnumerable<string> GetAllSpecies(Predicate<string> Filter = null)
        {
            List<string> speciesList = new();
            foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.BlueprintList)
            {
                if (bp.Tags.ContainsKey("Species")
                    && bp.Tags["Species"] is string species
                    && !speciesList.Contains(species)
                    && !species.ToLower().StartsWith("base")
                    && (Filter == null || Filter(species)))
                {
                    speciesList.Add(species);
                    yield return species;
                }
            }
        }
        public static bool SpeciesIsNotExcluded(string Species)
        {
            foreach (string exclusion in SpeciesExclusions)
            {
                if (Species.ToLower().Contains(exclusion.ToLower())
                    || Species.ToLower() == exclusion.ToLower())
                {
                    return false;
                }
            }
            return true;
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
                    && GameObjectFactory.Factory.GetBlueprintIfExists(corpseBleprint) is GameObjectBlueprint corpseBP
                    && corpseBP.IsCorpse()
                    && !corpseBlueprintList.Contains(corpseBleprint))
                {
                    corpseBlueprintList.Add(corpseBleprint);
                    yield return corpseBleprint;
                }
            }
        }
        public static bool CheckCorpseSpecies(UD_FleshGolems_CorpseQuestStep QuestItem, GameObject CorpseObject)
        {
            return IsCorpse(QuestItem, out CorpseItem corpseItem)
                && CorpseObject.IsInanimateCorpse()
                && corpseItem.IsSpecies
                && GetAllCorpsesOfSpecies(corpseItem.Value).Contains(CorpseObject.Blueprint);
        }

        public static IEnumerable<string> GetAllBaseCorpses(Predicate<GameObjectBlueprint> Filter = null)
        {
            List<string> baseCorpseList = new();
            foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.BlueprintList)
            {
                if (bp.InheritsFrom("Corpse")
                    && bp.Name != "Corpse"
                    && bp.IsBaseBlueprint()
                    && !bp.Name.Contains("Robot")
                    && !bp.Name.StartsWith("Base")
                    && bp.Name.EndsWith(" Corpse")
                    && (Filter == null || Filter(bp))
                    && !baseCorpseList.Contains(bp.Name))
                {
                    baseCorpseList.Add(bp.Name);
                    yield return bp.Name;
                }
            }
        }
        public static IEnumerable<string> GetAllCorpsesOfBase(string BaseCorpse)
        {
            List<string> baseCorpseList = new();
            foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.BlueprintList)
            {
                if (bp.InheritsFrom(BaseCorpse)
                    && bp.Name != "Corpse"
                    && bp.InheritsFrom("Corpse")
                    && !bp.Name.Contains("Robot")
                    && !bp.Name.StartsWith("Base")
                    && bp.Name.EndsWith(" Corpse")
                    && !baseCorpseList.Contains(bp.Name))
                {
                    baseCorpseList.Add(bp.Name);
                    yield return bp.Name;
                }
            }
        }

        public static bool FactionHasAtLeastOneMember(string Faction)
        {
            return GameObjectFactory.Factory.AnyFactionMembers(Faction);
        }
        public static bool FactionHasAtLeastOneMember(Faction Faction)
        {
            return FactionHasAtLeastOneMember(Faction.Name);
        }
        public static IEnumerable<string> GetAllCorpsesOfFaction(string Faction)
        {
            List<string> corpseBlueprintList = new();
            foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.GetFactionMembers(Faction, SkipExclude: true, ReadOnly: true))
            {
                if (bp.TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseChance), out int corpseChance)
                    && corpseChance > 0
                    && bp.TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseBlueprint), out string corpseBleprint)
                    && bp.InheritsFrom("Corpse")
                    && !corpseBlueprintList.Contains(corpseBleprint))
                {
                    corpseBlueprintList.Add(corpseBleprint);
                    yield return corpseBleprint;
                }
            }
        }
        public static IEnumerable<string> GetFactionsWithLivingMembersWhoDropCorpses()
        {
            foreach (string factionName in Factions.GetFactionNames())
            {
                if (!FactionHasAtLeastOneMember(factionName))
                {
                    continue;
                }
                if (Factions.Get(factionName) is Faction faction
                    && !faction.Visible)
                {
                    continue;
                }
                if (factionName.ToLower().Contains("village"))
                {
                    continue;
                }
                bool anyDroppableCorpse = false;
                foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.GetFactionMembers(factionName, SkipExclude: true, ReadOnly: true))
                {
                    if (bp.TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseChance), out int corpseChance)
                        && corpseChance > 0)
                    {
                        anyDroppableCorpse = true;
                        break;
                    }
                }
                if (anyDroppableCorpse && !FactionExclusions.Contains(factionName))
                {
                    yield return factionName;
                }
            }
        }

        public static bool CorpseMatchesAnyQuestStep(List<UD_FleshGolems_CorpseQuestStep> Steps, GameObject CorpseObject)
        {
            foreach (UD_FleshGolems_CorpseQuestStep questStep in Steps)
            {
                if (!CorpseObject.IsInanimateCorpse())
                {
                    continue;
                }
                if (IsCorpse(questStep, out CorpseItem corpseItem)
                    && corpseItem.CorpseCompletesThisStep(CorpseObject))
                {
                    return true;
                }
                else
                {
                    continue;
                }
            }
            return false;
        }
        public bool CorpseMatchesAnyQuestStep(GameObject CorpseObject)
        {
            return CorpseMatchesAnyQuestStep(Steps, CorpseObject);
        }

        public static IEnumerable<UD_FleshGolems_CorpseQuestStep> GetQuestStepsMatchingCorpse(List<UD_FleshGolems_CorpseQuestStep> Steps, GameObject CorpseObject)
        {
            foreach (UD_FleshGolems_CorpseQuestStep questStep in Steps)
            {
                if (questStep.Corpse.CorpseCompletesThisStep(CorpseObject))
                {
                    yield return questStep;
                }
            }
        }
        public IEnumerable<UD_FleshGolems_CorpseQuestStep> GetQuestStepsMatchingCorpse(GameObject CorpseObject)
        {
            return GetQuestStepsMatchingCorpse(Steps, CorpseObject);
        }
        public int GetCountMatchingCorpsesHeldByPlayer()
        {
            int matches = 0;
            foreach (GameObject item in Player?.GetInventoryAndEquipmentReadonly())
            {
                if (CorpseMatchesAnyQuestStep(item))
                {
                    matches++;
                }
            }
            return matches;
        }
        public static bool HasQuestStepWithCorpseItemValue(List<UD_FleshGolems_CorpseQuestStep> Steps, string CorpseItemValue)
        {
            foreach (UD_FleshGolems_CorpseQuestStep questStep in Steps)
            {
                if (IsCorpse(questStep, out CorpseItem corpseItem)
                    && corpseItem.Value == CorpseItemValue)
                {
                    return true;
                }
            }
            return false;
        }
        public bool HasQuestStepWithCorpseItemValue(string CorpseItemValue)
        {
            return HasQuestStepWithCorpseItemValue(Steps, CorpseItemValue);
        }

        public static UD_FleshGolems_CorpseQuestStep CreateASpeciesCorpseQuestStep(List<UD_FleshGolems_CorpseQuestStep> Steps, UD_FleshGolems_CorpseQuestSystem ParentSystem)
        {
            string species = AllSpecies.GetRandomElementCosmeticExcluding(Exclude: s => HasQuestStepWithCorpseItemValue(Steps, s));
            UD_FleshGolems_CorpseQuestStep questStep = new(ParentSystem)
            {
                Name = "Find " + Grammar.A(species.Capitalize()) + " Species Corpse",
                Corpse = new(CorpseTaxonomy.Species, species),
            };
            return questStep?.SetGeneratedStepText();
        }
        public UD_FleshGolems_CorpseQuestStep CreateASpeciesCorpseQuestStep()
        {
            return CreateASpeciesCorpseQuestStep(Steps, this);
        }
        public static UD_FleshGolems_CorpseQuestStep CreateABaseCorpseQuestStep(List<UD_FleshGolems_CorpseQuestStep> Steps, UD_FleshGolems_CorpseQuestSystem ParentSystem)
        {
            string baseBlueprint = AllBaseCorpses.GetRandomElementCosmeticExcluding(Exclude: s => HasQuestStepWithCorpseItemValue(Steps, s));
            UD_FleshGolems_CorpseQuestStep questStep = new(ParentSystem)
            {
                Name = "Find " + Grammar.A(baseBlueprint.Capitalize()),
                Corpse = new(CorpseTaxonomy.Base, baseBlueprint),
            };
            return questStep?.SetGeneratedStepText();
        }
        public UD_FleshGolems_CorpseQuestStep CreateABaseCorpseQuestStep()
        {
            return CreateABaseCorpseQuestStep(Steps, this);
        }
        public static UD_FleshGolems_CorpseQuestStep CreateAFactionCorpseQuestStep(List<UD_FleshGolems_CorpseQuestStep> Steps, UD_FleshGolems_CorpseQuestSystem ParentSystem)
        {
            string factionName = AllFactions.GetRandomElementCosmeticExcluding(Exclude: s => HasQuestStepWithCorpseItemValue(Steps, s));
            string factionDisplayName = Factions.Get(factionName).DisplayName;
            UD_FleshGolems_CorpseQuestStep questStep = new(ParentSystem)
            {
                Name = "Find " + Grammar.A(Grammar.MakeTitleCase(factionDisplayName)) + " Faction Corpse",
                Corpse = new(CorpseTaxonomy.Faction, factionName),
            };
            return questStep?.SetGeneratedStepText();
        }
        public UD_FleshGolems_CorpseQuestStep CreateAFactionCorpseQuestStep()
        {
            return CreateAFactionCorpseQuestStep(Steps, this);
        }

        public static UD_FleshGolems_CorpseQuestStep CreateAnAnyCorpseQuestStep(UD_FleshGolems_CorpseQuestSystem ParentSystem)
        {
            UD_FleshGolems_CorpseQuestStep questStep = new(ParentSystem)
            {
                Name = "Find Any Corpse",
                Corpse = new(CorpseTaxonomy.Any, "Any"),
            };
            return questStep?.SetGeneratedStepText();
        }
        public UD_FleshGolems_CorpseQuestStep CreateAnAnyCorpseQuestStep()
        {
            return CreateAnAnyCorpseQuestStep(this);
        }

        public void Init()
        {
            if (Steps != null && Steps.Count > 0)
            {
                return;
            }
            Steps ??= new();

            int desiredCorpsSteps = 10;
            bool haveAnAnyQuestStep = false;

            while (Steps.Count < desiredCorpsSteps)
            {
                UD_FleshGolems_CorpseQuestStep corpseQuestStep = null;

                int questStepRoll = Stat.Random(1, (haveAnAnyQuestStep ? 12 : 13));
                if (questStepRoll == 13)
                {
                    corpseQuestStep = CreateAnAnyCorpseQuestStep();
                    haveAnAnyQuestStep = true;
                }
                else
                {
                    int processedRoll = (questStepRoll % 3) + 1;
                    CorpseTaxonomy questStepTaxonomy = (CorpseTaxonomy)processedRoll;
                    corpseQuestStep = (questStepTaxonomy) switch
                    {
                        CorpseTaxonomy.Species => CreateASpeciesCorpseQuestStep(),
                        CorpseTaxonomy.Base => CreateABaseCorpseQuestStep(),
                        CorpseTaxonomy.Faction => CreateAFactionCorpseQuestStep(),
                        _ => CreateAnAnyCorpseQuestStep(),
                    };
                }

                if (!HasStepWithName(corpseQuestStep.Name))
                    Steps.Add(corpseQuestStep);
            }
        }
        private bool HasStepWithName(string s)
        {
            foreach (UD_FleshGolems_CorpseQuestStep questStep in Steps)
                if (questStep.Name == s)
                    return true;

            return false;
        }

        static bool IsCorpseWithQuestHelperPart(GameObject GO)
            => GO.IsInanimateCorpse()
            && GO.HasPart<UD_FleshGolems_CorpseQuestHelperPart>();

        public static bool CheckItem(UD_FleshGolems_CorpseQuestSystem CorpseQuestSystem, GameObject Item, bool Unfinish = false)
        {
            if (Item.TryGetPart(out UD_FleshGolems_CorpseQuestHelperPart corpseQuestHelperPart)
                && corpseQuestHelperPart.MarkedForCollection)
                return false;

            foreach (UD_FleshGolems_CorpseQuestStep finishableQuestStep in CorpseQuestSystem?.GetQuestStepsMatchingCorpse(Item))
            {
                if (!Unfinish)
                {
                    if (finishableQuestStep.FinishStep())
                    {
                        Item.SetIntProperty(nameof(UD_FleshGolems_CorpseQuestSystem), 1);
                        if (CorpseQuestSystem.Completable && !CorpseQuestSystem.PlayerAdvisedCompletable)
                        {
                            Popup.Show(
                                "You have the " + RequiredToComplete + " questionable materials that " +
                                CorpseQuestSystem.InfluencerRefName + " said he needed.");
                            CorpseQuestSystem.PlayerAdvisedCompletable = true;
                        }
                        return true;
                    }
                }
                else
                {
                    if (finishableQuestStep.UnfinishStep())
                    {
                        Item.SetIntProperty(nameof(UD_FleshGolems_CorpseQuestSystem), 0, true);
                        if (!CorpseQuestSystem.Completable && CorpseQuestSystem.PlayerAdvisedCompletable)
                        {
                            Popup.Show(
                                "You no longer have the " + RequiredToComplete + " questionable materials that " +
                                CorpseQuestSystem.InfluencerRefName + " said he needed.");
                            CorpseQuestSystem.PlayerAdvisedCompletable = false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        public bool CheckItem(GameObject Item, bool Unfinish = false)
            => CheckItem(this, Item, Unfinish);

        public void ProcessEvent(IActOnItemEvent E, bool Unfinish = false)
        {
            if (E.Item is GameObject item)
            {
                if (item.TryGetPart(out UD_FleshGolems_CorpseQuestHelperPart corpseQuestHelperPart)
                    && corpseQuestHelperPart.MarkedForCollection)
                    return;

                if (CheckItem(item, Unfinish))
                {
                    if (!Unfinish)
                    {
                        item.RequirePart<UD_FleshGolems_CorpseQuestHelperPart>();
                        item.RegisterEvent(this, DroppedEvent.ID, Serialize: true);
                        item.RegisterEvent(this, TakenEvent.ID, Serialize: true);
                    }
                    else
                    {
                        item.RemovePart<UD_FleshGolems_CorpseQuestHelperPart>();
                        item.UnregisterEvent(this, DroppedEvent.ID);
                        item.UnregisterEvent(this, TakenEvent.ID);
                    }
                }
            }
        }

        public static bool StartQuest()
            => The.Game.StartQuest(UD_FleshGolems_YouRaiseMeUpQuestSystem.MongerQuestID) != null;

        public static bool StartQuest(UD_FleshGolems_YouRaiseMeUpQuestSystem ParentSystem, Quest ParentQuest)
        {
            try
            {
                if (The.Game.RequireSystem<UD_FleshGolems_CorpseQuestSystem>() is var corpseQuestSystem)
                {
                    corpseQuestSystem.ParentQuest = ParentQuest;
                    corpseQuestSystem.ParentSystem = ParentSystem;
                    corpseQuestSystem.Init();
                    Quest quest = new()
                    {
                        ID = "UD_FleshGolems " + QuestName,
                        System = corpseQuestSystem,
                        Name = QuestName,
                        Level = 20,
                        Factions = PREVIOUSLY_SENTIENT_BEINGS,
                        Reputation = "100",
                        Finished = false,
                        Accomplishment = "Conspiring with a \"scientist\" most mad, you broke \"important\" scientfic discovery into the nature of life and death.",
                        Hagiograph = "Forget not the bloody " + Calendar.GetDay() + " of darkest " + Calendar.GetMonth() + ", when =name= demanded of =pronouns.possessive= loyal servant, that he take the first born babe of every denizen of Bethesda Susa in recompence for the sins of the Saads of old!",
                        HagiographCategory = "DoesSomethingRad",
                        StepsByID = new Dictionary<string, QuestStep>()
                    };
                    corpseQuestSystem.MyQuestID = quest.ID;
                    for (int i = 0; i < corpseQuestSystem.Steps.Count; i++)
                    {
                        QuestStep questStep = new()
                        {
                            ID = Guid.NewGuid().ToString(),
                            Name = corpseQuestSystem.Steps[i].Name,
                            Finished = false,
                            Text = corpseQuestSystem.Steps[i].Text,
                            XP = 0,
                            Awarded = true,
                        };
                        corpseQuestSystem.Steps[i].Name = questStep.ID;
                        quest.StepsByID.Add(questStep.ID, questStep);
                    }

                    if (corpseQuestSystem.Steps.Count > 0)
                    {
                        The.Game.StartQuest(quest, corpseQuestSystem.InfluencerRefName);
                        foreach (GameObject item in corpseQuestSystem.Player.GetInventory())
                            corpseQuestSystem.CheckItem(item);

                        _ = corpseQuestSystem.Completable;
                        return true;
                    }
                }
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(UD_FleshGolems_CorpseQuestSystem) + "." + nameof(StartQuest), x, "game_mod_exception");
            }
            return false;
        }

        public override void Finish()
        {
            if (ParentQuest == null)
                Game.TryGetQuest(UD_FleshGolems_YouRaiseMeUpQuestSystem.MongerQuestID, out ParentQuest);

            if (ParentQuest != null
                && !ParentQuest.IsStepFinished(ParentQuestStep.ID))
                ParentQuest.FinishStep(ParentQuestStep);

            base.Finish();
        }

        // Game Events
        public override void Register(XRLGame Game, IEventRegistrar Registrar)
        {
            base.Register(Game, Registrar);
            Registrar.Register(QuestFinishedEvent.ID);
        }
        public override bool HandleEvent(QuestFinishedEvent E)
        {
            if (E.Quest == Quest
                && !Quest.Finished
                && GetInfluencer() is GameObject influencer
                && Player.CurrentZone == influencer?.CurrentZone
                && Player.CurrentCell.GetAdjacentCells().Contains(influencer.CurrentCell))
            {
                int giveCount = 0;
                List<GameObject> corpseItems = Event.NewGameObjectList(Player.GetInventory(IsCorpseWithQuestHelperPart));
                foreach (GameObject corpse in corpseItems)
                    if (corpse.TryGetPart(out UD_FleshGolems_CorpseQuestHelperPart questHelperPart))
                    {
                        questHelperPart.MarkedForCollection = true;
                        if (questHelperPart.MarkedForCollection
                            && !influencer.ReceiveObject(corpse, Context: "Quest"))
                        {
                            Popup.ShowFail("You cannot give " + corpse.t() + " to " + influencer.t() + "!");
                            Player.ReceiveObject(corpse);
                            continue;
                        }
                        foreach(UD_FleshGolems_CorpseQuestStep questStep in GetQuestStepsMatchingCorpse(corpse))
                            questStep.MarkHandedIn();

                        giveCount++;
                    }

                if (giveCount < RequiredToComplete)
                    return false;
            }
            return base.HandleEvent(E);
        }

        // Player Events
        public override void RegisterPlayer(GameObject Player, IEventRegistrar Registrar)
        {
            base.RegisterPlayer(Player, Registrar);
            Registrar.Register(TookEvent.ID);
        }
        public override bool HandleEvent(TookEvent E)
        {
            ProcessEvent(E);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(DroppedEvent E)
        {
            ProcessEvent(E, Unfinish: true);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(TakenEvent E)
        {
            ProcessEvent(E, Unfinish: true);
            return base.HandleEvent(E);
        }

        /*
         * 
         * Wishes!
         * 
         */
        [WishCommand(Command = "UD_FleshGolems debug quest corpses")]
        public static void DebugQuestCorpses_Wish()
        {
            if (The.Game.HasQuest("UD_FleshGolems You Raise Me Up")
                && The.Game.GetSystem<UD_FleshGolems_CorpseQuestSystem>() is UD_FleshGolems_CorpseQuestSystem corpseQuestSystem
                && !corpseQuestSystem.Steps.IsNullOrEmpty())
            {
                foreach (UD_FleshGolems_CorpseQuestStep questStep in corpseQuestSystem.Steps)
                    if (questStep.Corpse is CorpseItem corpseItem)
                        for (int i = 0; i < 3; i++)
                            if (corpseItem.GetACorpseForThisStep() is string corpseBlueprint
                                && GameObject.Create(corpseBlueprint) is GameObject corpseObject)
                                The.Player.CurrentCell
                                    .GetAdjacentCells(2)
                                    .GetRandomElementCosmetic(c => c.IsEmptyFor(corpseObject))
                                    .AddObject(corpseObject);
            }
            else
                Popup.Show("You don't have that quest. Spawn the Mad Monger first and start it.");
        }
    }
}