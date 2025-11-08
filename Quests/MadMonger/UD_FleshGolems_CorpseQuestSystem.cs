using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Qud.API;

using XRL;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Quests;
using XRL.Language;

using CorpseItem =  XRL.World.QuestManagers.UD_FleshGolems_CorpseQuestStep.CorpseItem;

using UD_FleshGolems;

namespace XRL.World.QuestManagers
{
    [Serializable]
    public class UD_FleshGolems_CorpseQuestSystem : IQuestSystem
    {
        public static int RequiredToComplete => 3;

        public static string QuestName = "Find 3 Questionable Materials";

        public static List<string> Types => new()
        {
            "Species",
            "Base",
            "Any",
        };

        public static List<string> SpeciesExclusions => new()
        {
            "robot",
            "*",
            "[",
            "]",
        };

        public static List<string> AllBaseCorpses => new(GetAllBaseCorpse());

        public static List<string> AllSpecies => new(GetAllSpecies());

        public string MyQuestID;

        public Quest ParentQuest;

        public List<UD_FleshGolems_CorpseQuestStep> Steps;

        public int CompletedSteps => (from step in Steps where step.Finished select step).Count();

        public bool Completable => CompletedSteps >= RequiredToComplete;

        public string InfluencerRefName => GetInfluencer().GetReferenceDisplayName(Short: true);

        [NonSerialized]
        private bool PlayerAdvisedCompletable;

        public UD_FleshGolems_CorpseQuestSystem()
        {
            MyQuestID = null;
            ParentQuest = null;
            Steps = new();

            PlayerAdvisedCompletable = false;
        }

        public static bool IsCorpse(GameObject Object)
        {
            return Object != null
                && Object.GetBlueprint().InheritsFrom("Corpse");
        }

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

        public static IEnumerable<string> GetAllSpecies()
        {
            List<string> speciesList = new();
            foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.BlueprintList)
            {
                if (bp.Tags.ContainsKey("Species")
                    && bp.Tags["Species"] is string species
                    && !speciesList.Contains(species)
                    && !species.ToLower().StartsWith("base"))
                {
                    foreach (string exclusion in SpeciesExclusions)
                    {
                        if (species.ToLower().Contains(exclusion))
                        {
                            continue;
                        }
                    }
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
        public static bool CheckCorpseSpecies(UD_FleshGolems_CorpseQuestStep QuestItem, GameObject CorpseObject)
        {
            return IsCorpse(QuestItem, out CorpseItem corpseItem)
                && IsCorpse(CorpseObject)
                && corpseItem.IsSpecies
                && GetAllCorpsesOfSpecies(corpseItem.Value).Contains(CorpseObject.Blueprint);
        }

        public static IEnumerable<string> GetAllBaseCorpse()
        {
            List<string> baseCorpseList = new();
            foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.BlueprintList)
            {
                if (bp.InheritsFrom("Corpse")
                    && bp.Name != "Corpse"
                    && bp.IsBaseBlueprint()
                    && !baseCorpseList.Contains(bp.Name))
                {
                    baseCorpseList.Add(bp.Name);
                    yield return bp.Name;
                }
            }
        }
        public static bool CheckCorpseBase(UD_FleshGolems_CorpseQuestStep QuestItem, GameObject CorpseObject)
        {
            return IsCorpse(QuestItem, out CorpseItem corpseItem)
                && IsCorpse(CorpseObject)
                && corpseItem.IsBase
                && CorpseObject.GetBlueprint().InheritsFrom(corpseItem.Value);
        }
        public static bool CheckCorpseAny(UD_FleshGolems_CorpseQuestStep QuestItem, GameObject CorpseObject)
        {
            return IsCorpse(QuestItem, out CorpseItem corpseItem)
                && corpseItem.IsAny
                && IsCorpse(CorpseObject);
        }
        public static bool CorpseMatchesAnyQuestStep(List<UD_FleshGolems_CorpseQuestStep> Steps, GameObject CorpseObject)
        {
            foreach (UD_FleshGolems_CorpseQuestStep questStep in Steps)
            {
                if (!IsCorpse(CorpseObject))
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

        public static List<UD_FleshGolems_CorpseQuestStep> GetQuestStepsMatchingCorpse(List<UD_FleshGolems_CorpseQuestStep> Steps, GameObject CorpseObject)
        {
            List<UD_FleshGolems_CorpseQuestStep> possibleSteps = new();
            foreach (UD_FleshGolems_CorpseQuestStep questStep in Steps)
            {
                if (CheckCorpseBase(questStep, CorpseObject))
                {
                    possibleSteps.TryAdd(questStep);
                }
                if (CheckCorpseSpecies(questStep, CorpseObject))
                {
                    possibleSteps.TryAdd(questStep);
                }
            }
            return possibleSteps;
        }
        public List<UD_FleshGolems_CorpseQuestStep> GetQuestStepsMatchingCorpse(GameObject CorpseObject)
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
            string species = AllSpecies.GetRandomElementCosmetic(Exclude: s => HasQuestStepWithCorpseItemValue(Steps, s));
            UD_FleshGolems_CorpseQuestStep questStep = new(ParentSystem)
            {
                Name = "Find a " + species.Capitalize() + " Corpse",
                Text = "\"Acquire\" a corpse from any member of the " + species.ToLower() + " species...",
                Corpse = new("Species", species)
            };
            return questStep;
        }
        public UD_FleshGolems_CorpseQuestStep CreateASpeciesCorpseQuestStep()
        {
            return CreateASpeciesCorpseQuestStep(Steps, this);
        }
        public static UD_FleshGolems_CorpseQuestStep CreateABaseCorpseQuestStep(List<UD_FleshGolems_CorpseQuestStep> Steps, UD_FleshGolems_CorpseQuestSystem ParentSystem)
        {
            string baseBlueprint = AllBaseCorpses.GetRandomElementCosmetic(Exclude: s => HasQuestStepWithCorpseItemValue(Steps, s));
            UD_FleshGolems_CorpseQuestStep questStep = new(ParentSystem)
            {
                Name = "Find a " + baseBlueprint.Capitalize(),
                Text = "\"Acquire\" any kind of " + baseBlueprint.ToLower() + "...",
                Corpse = new("Base", baseBlueprint)
            };
            return questStep;
        }
        public UD_FleshGolems_CorpseQuestStep CreateABaseCorpseQuestStep()
        {
            return CreateABaseCorpseQuestStep(Steps, this);
        }

        public static UD_FleshGolems_CorpseQuestStep CreateAnAnyCorpseQuestStep(UD_FleshGolems_CorpseQuestSystem ParentSystem)
        {
            UD_FleshGolems_CorpseQuestStep questStep = new(ParentSystem)
            {
                Name = "Find Any Corpse",
                Text = "\"Acquire\" quite literally any kind of corpse...",
                Corpse = new("Any", "Any")
            };
            return questStep;
        }
        public UD_FleshGolems_CorpseQuestStep CreateAnAnyCorpseQuestStep()
        {
            return CreateAnAnyCorpseQuestStep(this);
        }

        static void debugLog(string Field, object Value = null, int Indent = 0)
        {
            string indent = " ".ThisManyTimes(Math.Min(12, Indent) * 4);
            string output = indent + Field;
            if (Value != null &&
                !Value.ToString().IsNullOrEmpty())
            {
                output += ": " + Value;
            }
            UnityEngine.Debug.Log(output);
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
            debugLog(nameof(UD_FleshGolems_CorpseQuestSystem) + "." + nameof(Init));
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
                    corpseQuestStep = (questStepRoll % 2) switch
                    {
                        0 => CreateASpeciesCorpseQuestStep(),
                        _ => CreateABaseCorpseQuestStep(),
                    };
                }
                debugLog(nameof(corpseQuestStep), corpseQuestStep.Name, 1);
                if (!HasStepWithName(corpseQuestStep.Name))
                {
                    Steps.Add(corpseQuestStep);
                }
            }
        }
        private bool HasStepWithName(string s)
        {
            foreach (UD_FleshGolems_CorpseQuestStep questStep in Steps)
            {
                if (questStep.Name == s)
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
                var corpseQuestSystem = The.Game.RequireSystem<UD_FleshGolems_CorpseQuestSystem>();
                debugLog(nameof(UD_FleshGolems_CorpseQuestSystem) + "." + nameof(StartQuest));
                corpseQuestSystem.Init();
                Quest quest = new()
                {
                    ID = "UD_FleshGolems " + QuestName,
                    System = corpseQuestSystem,
                    Name = QuestName,
                    Level = 20,
                    Factions = "Newly Sentient Beings",
                    Reputation = "100",
                    Finished = false,
                    Accomplishment = "Conspiring with a \"scientist\" most mad, you broke \"important\" scientfic discovery into the nature of life and death.",
                    Hagiograph = "Forget not the bloody " + Calendar.GetDay() + " of darkest " + Calendar.GetMonth() + ", when =name= demanded of =pronoun.possessive= loyal servant, that he take the first born babe of every denizen of Bethesda Susa in recompence for the sins of the sins of the Saads of old!",
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
                        XP = 1000
                    };
                    corpseQuestSystem.Steps[i].Name = questStep.ID;
                    quest.StepsByID.Add(questStep.ID, questStep);
                }
                if (corpseQuestSystem.Steps.Count > 0)
                {
                    The.Game.StartQuest(quest, corpseQuestSystem.InfluencerRefName);
                    return true;
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
            {
                Game.TryGetQuest(UD_FleshGolems_YouRaiseMeUpQuestSystem.MongerQuestID, out ParentQuest);
            }
            ParentQuest?.FinishStep(ParentQuest.StepsByID[QuestName]);
            base.Finish();
        }
        public override void Register(XRLGame Game, IEventRegistrar Registrar)
        {
            Registrar.Register(ZoneActivatedEvent.ID);
        }

        public override void RegisterPlayer(GameObject Player, IEventRegistrar Registrar)
        {
            Registrar.Register(TookEvent.ID);
        }

        public override bool HandleEvent(ZoneActivatedEvent E)
        {
            if (E.Zone.X == 1 && E.Zone.Y == 1 && E.Zone.Z == 10 && E.Zone.GetTerrainObject()?.Blueprint == "TerrainFungalCenter")
            {
                The.Game.FinishQuestStep("Pax Klanq, I Presume?", "Seek the Heart of the Rainbow");
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(TookEvent E)
        {
            if (E.Item is GameObject item)
            {
                GetQuestStepsMatchingCorpse(item)?.ShuffleInPlace()?.GetRandomElementCosmetic(Exclude: s => s.Finished)?.FinishStep();
                if (Completable && !PlayerAdvisedCompletable)
                {
                    Popup.Show("You have the " + RequiredToComplete + "questionable materials that " + GetInfluencer().GetReferenceDisplayName(Short: true) + " said he needed.");
                    PlayerAdvisedCompletable = true;
                }
            }
            return base.HandleEvent(E);
        }

        public override GameObject GetInfluencer()
        {
            return GameObject.FindByBlueprint("UD_FleshGolems Mad Monger");
        }
    }
}