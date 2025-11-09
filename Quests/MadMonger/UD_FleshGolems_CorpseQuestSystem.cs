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
using XRL.Wish;

using CorpseItem = XRL.World.QuestManagers.UD_FleshGolems_CorpseQuestStep.CorpseItem;
using CorpseTaxonomy = XRL.World.QuestManagers.UD_FleshGolems_CorpseQuestStep.CorpseItem.CorpseTaxonomy;

using UD_FleshGolems;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

namespace XRL.World.QuestManagers
{
    [HasWishCommand]
    [Serializable]
    public class UD_FleshGolems_CorpseQuestSystem : IQuestSystem
    {
        public static int RequiredToComplete => 3;

        public static string QuestName = "Find 3 Questionable Materials (for Science)";

        public static string QuestGiverBlueprint = "UD_FleshGolems Mad Monger";

        public static List<string> SpeciesExclusions => new()
        {
            "robot",
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
            "trolls",
            "slimes",
        };

        public static List<string> FindVerbs => new()
        {
            "Acquire",
            "Attain",
            "Collect",
            "Fetch",
            "Find",
            "Gather",
            "Get",
            "Locate",
            "Obtain",
            "Procure",
            "Secure",
            "Snag",
            "Source",
        };
        public static List<string> GetGenericCorpseQuestText => new()
        {
            "*Find* one of any kind of *type* corpse...",
            "*Find* the corpse of any type of *type*...",
            "*Find* single *type* corpse...",
        };
        public static List<string> GetSpeciesCorpseQuestText => new()
        {
            "*Find* the corpse of any species of *type*...",
        };
        public static List<string> GetBaseCorpseQuestText => new()
        {
            "*Find* the corpse of any species of *species*...",
            "*Find* one of any kind of *species* corpse...",
            "*Find* the corpse of any type of *species*...",
            "*Find* single *species* corpse...",
        };

        public static List<string> AllBaseCorpses => new(GetAllBaseCorpse());
        public static List<string> AllSpecies => new(GetAllSpecies(SpeciesIsNotExcluded));
        public static List<string> AllFactions => new(GetFactionsWithLivingMembersWhoDropCorpses());

        public UD_FleshGolems_YouRaiseMeUpQuestSystem ParentSystem;

        public Quest ParentQuest;

        public string MyQuestID;

        public QuestStep ParentQuestStep => (ParentQuest == null || ParentQuest.StepsByID.IsNullOrEmpty()) ? null : ParentQuest.StepsByID["UD_FleshGolems I'm dead serious!"];

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

        public string InfluencerRefName => GetInfluencer().GetReferenceDisplayName(Short: true);

        [NonSerialized]
        private bool PlayerAdvisedCompletable;

        public UD_FleshGolems_CorpseQuestSystem()
        {
            ParentSystem = null;
            ParentQuest = null;

            MyQuestID = null;

            Steps = new();

            PlayerAdvisedCompletable = false;
        }

        public override GameObject GetInfluencer() => GameObject.FindByBlueprint(QuestGiverBlueprint);

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

        public static IEnumerable<string> GetAllBaseCorpse(Predicate<GameObjectBlueprint> Filter = null)
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
        public static bool CheckCorpseBase(UD_FleshGolems_CorpseQuestStep QuestItem, GameObject CorpseObject)
        {
            return IsCorpse(QuestItem, out CorpseItem corpseItem)
                && IsCorpse(CorpseObject)
                && corpseItem.IsBase
                && CorpseObject.GetBlueprint().InheritsFrom(corpseItem.Value);
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
        public static bool CheckCorpseFaction(UD_FleshGolems_CorpseQuestStep QuestItem, GameObject CorpseObject)
        {
            return IsCorpse(QuestItem, out CorpseItem corpseItem)
                && IsCorpse(CorpseObject)
                && corpseItem.IsSpecies
                && GetAllCorpsesOfFaction(corpseItem.Value).Contains(CorpseObject.Blueprint);
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
            string species = AllSpecies.GetRandomElementCosmetic(Exclude: s => HasQuestStepWithCorpseItemValue(Steps, s));
            UD_FleshGolems_CorpseQuestStep questStep = new(ParentSystem)
            {
                Name = "Find " + Grammar.A(species.Capitalize()) + " Corpse",
                Text = "\"Acquire\" a corpse from any member of the " + species.ToLower() + " species...",
                Corpse = new(CorpseTaxonomy.Species, species)
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
                Name = "Find " + Grammar.A(baseBlueprint.Capitalize()),
                Text = "\"Acquire\" any kind of " + baseBlueprint.ToLower() + "...",
                Corpse = new(CorpseTaxonomy.Base, baseBlueprint)
            };
            return questStep;
        }
        public UD_FleshGolems_CorpseQuestStep CreateABaseCorpseQuestStep()
        {
            return CreateABaseCorpseQuestStep(Steps, this);
        }
        public static UD_FleshGolems_CorpseQuestStep CreateAFactionCorpseQuestStep(List<UD_FleshGolems_CorpseQuestStep> Steps, UD_FleshGolems_CorpseQuestSystem ParentSystem)
        {
            string factionName = AllFactions.GetRandomElementCosmetic(Exclude: s => HasQuestStepWithCorpseItemValue(Steps, s));
            string factionDisplayName = Factions.Get(factionName).DisplayName;
            UD_FleshGolems_CorpseQuestStep questStep = new(ParentSystem)
            {
                Name = "Find " + Grammar.A(Grammar.MakeTitleCase(factionDisplayName)) + " Corpse",
                Text = "\"Acquire\" any kind of " + factionDisplayName.ToLower() + "...",
                Corpse = new(CorpseTaxonomy.Faction, factionName)
            };
            return questStep;
        }
        public UD_FleshGolems_CorpseQuestStep CreateAFactionCorpseQuestStep()
        {
            return CreateABaseCorpseQuestStep(Steps, this);
        }

        public static UD_FleshGolems_CorpseQuestStep CreateAnAnyCorpseQuestStep(UD_FleshGolems_CorpseQuestSystem ParentSystem)
        {
            UD_FleshGolems_CorpseQuestStep questStep = new(ParentSystem)
            {
                Name = "Find Any Corpse",
                Text = "*Find* quite literally any kind of corpse...",
                Corpse = new(CorpseTaxonomy.Any, "Any")
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
                    CorpseTaxonomy questStepTaxonomy = (CorpseTaxonomy)((questStepRoll % 3) + 1);
                    corpseQuestStep = (questStepTaxonomy) switch
                    {
                        CorpseTaxonomy.Species => CreateASpeciesCorpseQuestStep(),
                        CorpseTaxonomy.Base => CreateABaseCorpseQuestStep(),
                        CorpseTaxonomy.Faction => CreateAFactionCorpseQuestStep(),
                        _ => CreateAnAnyCorpseQuestStep(),
                    };
                }
                debugLog(corpseQuestStep.Corpse.Taxonomy.ToString(), corpseQuestStep.Name, 1);
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

        public static bool CheckItem(UD_FleshGolems_CorpseQuestSystem CorpseQuestSystem, GameObject Item, bool Unfinish = false)
        {
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
        {
            return CheckItem(this, Item, Unfinish);
        }

        public void ProcessEvent(IActOnItemEvent E, bool Unfinish = false)
        {
            if (E.Item is GameObject item
                && CheckItem(item, Unfinish))
            {
                if (!Unfinish)
                {
                    item.RegisterEvent(this, DroppedEvent.ID, Serialize: true);
                    item.RegisterEvent(this, TakenEvent.ID, Serialize: true);
                }
                else
                {
                    item.UnregisterEvent(this, DroppedEvent.ID);
                    item.UnregisterEvent(this, TakenEvent.ID);
                }
            }
        }

        public static bool StartQuest()
        {
            return The.Game.StartQuest(UD_FleshGolems_YouRaiseMeUpQuestSystem.MongerQuestID) != null;
        }
        public static bool StartQuest(UD_FleshGolems_YouRaiseMeUpQuestSystem ParentSystem, Quest ParentQuest)
        {
            try
            {
                var corpseQuestSystem = The.Game.RequireSystem<UD_FleshGolems_CorpseQuestSystem>();
                corpseQuestSystem.ParentQuest = ParentQuest;
                corpseQuestSystem.ParentSystem = ParentSystem;
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
                    Hagiograph = "Forget not the bloody " + Calendar.GetDay() + " of darkest " + Calendar.GetMonth() + ", when =name= demanded of =pronoun.possessive= loyal servant, that he take the first born babe of every denizen of Bethesda Susa in recompence for the sins of the Saads of old!",
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
                        XP = 0
                    };
                    corpseQuestSystem.Steps[i].Name = questStep.ID;
                    quest.StepsByID.Add(questStep.ID, questStep);
                }
                if (corpseQuestSystem.Steps.Count > 0)
                {
                    The.Game.StartQuest(quest, corpseQuestSystem.InfluencerRefName);
                    foreach (GameObject item in corpseQuestSystem.Player.GetInventory())
                    {
                        corpseQuestSystem.CheckItem(item);
                    }
                    _ = corpseQuestSystem.Completable;
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
            ParentQuest?.FinishStep(ParentQuestStep);
            base.Finish();
        }

        public override void Register(XRLGame Game, IEventRegistrar Registrar)
        {
            Registrar.Register(ZoneActivatedEvent.ID);
        }

        public override void RegisterPlayer(GameObject Player, IEventRegistrar Registrar)
        {
            Registrar.Register(TookEvent.ID);
            // Registrar.Register("PerformDrop");
        }

        public override bool HandleEvent(ZoneActivatedEvent E)
        {
            /*
            if (E.Zone.X == 1 && E.Zone.Y == 1 && E.Zone.Z == 10 && E.Zone.GetTerrainObject()?.Blueprint == "TerrainFungalCenter")
            {
                The.Game.FinishQuestStep("Pax Klanq, I Presume?", "Seek the Heart of the Rainbow");
            }
            */
            return base.HandleEvent(E);
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
        [WishCommand(Command = "UD_FleshGolems debug quest species")]
        public static void DebugQuestSpecies_Wish()
        {
            UnityEngine.Debug.Log("All species (included or not)...");
            foreach (string species in GetAllSpecies())
            {
                UnityEngine.Debug.Log(AppendYehNah("    ", SpeciesIsNotExcluded(species)) + species);
            }
        }
        [WishCommand(Command = "UD_FleshGolems debug quest species corpses")]
        public static void DebugQuestSpeciesCorpses_Wish()
        {
            if (Popup.PickOption("pick which species", Options: AllSpecies, AllowEscape: true) is int pickedOption)
            {
                if (AllFactions.Count > pickedOption)
                {
                    UnityEngine.Debug.Log("All " + AllSpecies[pickedOption] + " corpses...");
                    foreach (string corpse in GetAllCorpsesOfSpecies(AllSpecies[pickedOption]))
                    {
                        UnityEngine.Debug.Log(AppendYehNah("    ", SpeciesIsNotExcluded(corpse)) + corpse);
                    }
                }
                else
                {
                    Popup.Show("Something went wrong there, " + nameof(AllSpecies) + " doesn't appear to have the picked entry.");
                }
            }
        }
        [WishCommand(Command = "UD_FleshGolems debug quest base")]
        public static void DebugQuestBases_Wish()
        {
            UnityEngine.Debug.Log("All Base Corpse blueprints...");
            foreach (string baseCorpse in GetAllBaseCorpse())
            {
                UnityEngine.Debug.Log("    " + baseCorpse);
            }
        }
        [WishCommand(Command = "UD_FleshGolems debug quest faction corpses")]
        public static void DebugQuestFactionCorpses_Wish()
        {
            List<string> factionNames = new();
            foreach (string factionName in AllFactions)
            {
                factionNames.Add(Factions.Get(factionName).DisplayName);
            }
            if (Popup.PickOption("pick which faction", Options: factionNames, AllowEscape: true) is int pickedOption)
            {
                if (AllFactions.Count > pickedOption)
                {
                    UnityEngine.Debug.Log("All " + AllFactions[pickedOption] + " corpses...");
                    foreach (string corpse in GetAllCorpsesOfSpecies(AllFactions[pickedOption]))
                    {
                        UnityEngine.Debug.Log("    " + corpse);
                    }
                }
                else
                {
                    Popup.Show("Something went wrong there, " + nameof(AllFactions) + " doesn't appear to have the picked entry.");
                }
            }
        }
        [WishCommand(Command = "UD_FleshGolems debug quest corpses")]
        public static void DebugQuestCorpses_Wish()
        {
            if (The.Game.GetSystem<UD_FleshGolems_CorpseQuestSystem>() is var corpseQuestSystem
                && !corpseQuestSystem.Steps.IsNullOrEmpty())
            {
                List<UD_FleshGolems_CorpseQuestStep> questSteps = new(corpseQuestSystem.Steps);
                questSteps.ShuffleInPlace();

                debugLog("UD_FleshGolems debug quest corpses...");
                for (int i = 0; i < 10; i++)
                {
                    if (questSteps[i].Corpse is CorpseItem corpseItem
                        && corpseItem.GetACorpseForThisStep() is string corpseBlueprint)
                    {
                        debugLog(corpseItem.Taxonomy.ToString() + ", " + corpseItem.Value, corpseBlueprint, 1);
                        if (GameObject.Create(corpseBlueprint) is GameObject corpseObject)
                        {
                            The.Player.CurrentCell
                                .GetAdjacentCells(3)
                                .GetRandomElementCosmetic(c => c.IsEmptyFor(corpseObject))
                                .AddObject(corpseObject);
                        }
                    }
                }
            }
            else
            {
                Popup.Show("You don't have that quest. Spawn the Mad Monger first and start it.");
            }
        }
    }
}