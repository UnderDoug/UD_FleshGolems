using System;
using System.Collections.Generic;
using System.Text;

using ConsoleLib.Console;

using XRL.Language;
using XRL.UI;
using XRL.World.AI;
using XRL.World.Parts.Mutation;

using UD_FleshGolems;
using Qud.API;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class UD_FleshGolems_NanoNecroAnimation : BaseMutation
    {
        public const string COMMAND_NAME_REANIMATE_ONE = "Command_UD_FleshGolems_ReanimateOne";
        public const string COMMAND_NAME_REANIMATE_ALL = "Command_UD_FleshGolems_ReanimateAll";
        public const string COMMAND_NAME_SUMMON_CORPSES = "Command_UD_FleshGolems_SummonCorpses";
        public const string COMMAND_NAME_ASSESS_CORPSE = "Command_UD_FleshGolems_AssessCorpse";
        public const string COMMAND_NAME_POWERWORD_KILL = "Command_UD_FleshGolems_PowerWordKill";

        public const string REANIMATE_NAME = "{{UD_FleshGolems_reanimated|re-animate}}";

        public const string REANIMATE_ONE_NAME = REANIMATE_NAME + " corpse";
        public const string REANIMATE_ALL_NAME = REANIMATE_NAME + " horde";
        public const string SUMMON_CORPSES_NAME = "summon corpses";
        public const string ASSESS_CORPSE_NAME = "assess a corpse";
        public const string POWERWORD_KILL_NAME = "{{M|power}} {{Y|word}}: {{r|kill}}";

        public Guid ReanimateOneActivatedAbilityID;
        public Guid ReanimateAllActivatedAbilityID;
        public Guid SummonCorpsesActivatedAbilityID;
        public Guid AssessCorpseActivatedAbilityID;
        public Guid PowerWordKillActivatedAbilityID;

        public UD_FleshGolems_NanoNecroAnimation()
        {
            ReanimateOneActivatedAbilityID = Guid.Empty;
            ReanimateAllActivatedAbilityID = Guid.Empty;
            SummonCorpsesActivatedAbilityID = Guid.Empty;
            AssessCorpseActivatedAbilityID = Guid.Empty;
            PowerWordKillActivatedAbilityID = Guid.Empty;
        }

        public virtual Guid AddActivatedAbilityReanimateOne(GameObject GO, bool Force = false, bool Silent = false)
        {
            if (ReanimateOneActivatedAbilityID == Guid.Empty || Force)
            {
                ReanimateOneActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: Grammar.MakeTitleCase(REANIMATE_ONE_NAME),
                        Command: COMMAND_NAME_REANIMATE_ONE,
                        Class: GetMutationType() + " Mutation",
                        Description: "You " + REANIMATE_NAME + " a single corpse of your choosing, recruiting it if you desire!",
                        Icon: "&#214",
                        UITileDefault: new("Mutations/syphon_vim.bmp", "G", "&r", "&r", 'K'));
            }
            return ReanimateOneActivatedAbilityID;
        }
        public Guid AddActivatedAbilityReanimateOne(bool Force = false, bool Silent = false)
        {
            return AddActivatedAbilityReanimateOne(ParentObject, Force, Silent);
        }
        public virtual bool RemoveActivatedAbilityReanimateOne(GameObject GO, bool Force = false)
        {
            bool removed = false;
            if (ReanimateOneActivatedAbilityID != Guid.Empty || Force)
            {
                if (removed = RemoveMyActivatedAbility(ref ReanimateOneActivatedAbilityID, GO))
                {
                    ReanimateOneActivatedAbilityID = Guid.Empty;
                }
            }
            return removed && ReanimateOneActivatedAbilityID == Guid.Empty;
        }
        public bool RemoveActivatedAbilityReanimateOne(bool Force = false)
        {
            return RemoveActivatedAbilityReanimateOne(ParentObject, Force);
        }

        public virtual Guid AddActivatedAbilityReanimateAll(GameObject GO, bool Force = false, bool Silent = false)
        {
            if (ReanimateAllActivatedAbilityID == Guid.Empty || Force)
            {
                ReanimateAllActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: Grammar.MakeTitleCase(REANIMATE_ALL_NAME),
                        Command: COMMAND_NAME_REANIMATE_ALL,
                        Class: GetMutationType() + " Mutation",
                        Description: "You " + REANIMATE_NAME + " every corpse in the zone, recruiting any of them if you desire!",
                        Icon: "&#214",
                        UITileDefault: new("Mutations/sunder_mind.bmp", "W", "&r", "&r", 'K'));
            }
            return ReanimateAllActivatedAbilityID;
        }
        public Guid AddActivatedAbilityReanimateAll(bool Force = false, bool Silent = false)
        {
            return AddActivatedAbilityReanimateAll(ParentObject, Force, Silent);
        }
        public virtual bool RemoveActivatedAbilityReanimateAll(GameObject GO, bool Force = false)
        {
            bool removed = false;
            if (ReanimateAllActivatedAbilityID != Guid.Empty || Force)
            {
                if (removed = RemoveMyActivatedAbility(ref ReanimateAllActivatedAbilityID, GO))
                {
                    ReanimateAllActivatedAbilityID = Guid.Empty;
                }
            }
            return removed && ReanimateAllActivatedAbilityID == Guid.Empty;
        }
        public bool RemoveActivatedAbilityReanimateAll(bool Force = false)
        {
            return RemoveActivatedAbilityReanimateAll(ParentObject, Force);
        }

        public virtual Guid AddActivatedAbilitySummonCorpses(GameObject GO, bool Force = false, bool Silent = false)
        {
            if (SummonCorpsesActivatedAbilityID == Guid.Empty || Force)
            {
                SummonCorpsesActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: Grammar.MakeTitleCase(SUMMON_CORPSES_NAME),
                        Command: COMMAND_NAME_SUMMON_CORPSES,
                        Class: GetMutationType() + " Mutation",
                        Description: "You summon a number of corpses in a radius around you, ready for you to " + REANIMATE_NAME + "!",
                        Icon: "&#214",
                        UITileDefault: new("items/sw_splat1.bmp", "X", "&r", "&r", 'K'));
            }
            return SummonCorpsesActivatedAbilityID;
        }
        public Guid AddActivatedAbilitySummonCorpses(bool Force = false, bool Silent = false)
        {
            return AddActivatedAbilitySummonCorpses(ParentObject, Force, Silent);
        }
        public virtual bool RemoveActivatedAbilitySummonCorpses(GameObject GO, bool Force = false)
        {
            bool removed = false;
            if (SummonCorpsesActivatedAbilityID != Guid.Empty || Force)
            {
                if (removed = RemoveMyActivatedAbility(ref SummonCorpsesActivatedAbilityID, GO))
                {
                    SummonCorpsesActivatedAbilityID = Guid.Empty;
                }
            }
            return removed && SummonCorpsesActivatedAbilityID == Guid.Empty;
        }
        public bool RemoveActivatedAbilitySummonCorpses(bool Force = false)
        {
            return RemoveActivatedAbilitySummonCorpses(ParentObject, Force);
        }

        public virtual Guid AddActivatedAbilityAssessCorpse(GameObject GO, bool Force = false, bool Silent = false)
        {
            if (AssessCorpseActivatedAbilityID == Guid.Empty || Force)
            {
                AssessCorpseActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: Grammar.MakeTitleCase(ASSESS_CORPSE_NAME),
                        Command: COMMAND_NAME_ASSESS_CORPSE,
                        Class: GetMutationType() + " Mutation",
                        Description: "You study a corpse making a broad assessment about what kind of creature it may have been in life!",
                        Icon: "&#214",
                        UITileDefault: new("Mutations/telepathy.bmp", "X", "&r", "&r", 'K'));
            }
            return AssessCorpseActivatedAbilityID;
        }
        public Guid AddActivatedAbilityAssessCorpse(bool Force = false, bool Silent = false)
        {
            return AddActivatedAbilityAssessCorpse(ParentObject, Force, Silent);
        }
        public virtual bool RemoveActivatedAbilityAssessCorpse(GameObject GO, bool Force = false)
        {
            bool removed = false;
            if (AssessCorpseActivatedAbilityID != Guid.Empty || Force)
            {
                if (removed = RemoveMyActivatedAbility(ref AssessCorpseActivatedAbilityID, GO))
                {
                    AssessCorpseActivatedAbilityID = Guid.Empty;
                }
            }
            return removed && AssessCorpseActivatedAbilityID == Guid.Empty;
        }
        public bool RemoveActivatedAbilityAssessCorpse(bool Force = false)
        {
            return RemoveActivatedAbilityAssessCorpse(ParentObject, Force);
        }

        public virtual Guid AddActivatedAbilityPowerWordKill(GameObject GO, bool Force = false, bool Silent = false)
        {
            if (PowerWordKillActivatedAbilityID == Guid.Empty || Force)
            {
                PowerWordKillActivatedAbilityID =
                    AddMyActivatedAbility(
                        Name: Grammar.MakeTitleCase(POWERWORD_KILL_NAME),
                        Command: COMMAND_NAME_POWERWORD_KILL,
                        Class: GetMutationType() + " Mutation",
                        Description: "You speak the {{Y|word}} of {{M|power}} \"Kill\" and your target instantly dies, guaranteed to leave behind a perfectly intact corpse!",
                        Icon: "&#214",
                        UITileDefault: new("Mutations/psionic_migraines.bmp", "X", "&r", "&r", 'K'));
            }
            return PowerWordKillActivatedAbilityID;
        }
        public Guid AddActivatedAbilityPowerWordKill(bool Force = false, bool Silent = false)
        {
            return AddActivatedAbilityPowerWordKill(ParentObject, Force, Silent);
        }
        public virtual bool RemoveActivatedAbilityPowerWordKill(GameObject GO, bool Force = false)
        {
            bool removed = false;
            if (PowerWordKillActivatedAbilityID != Guid.Empty || Force)
            {
                if (removed = RemoveMyActivatedAbility(ref PowerWordKillActivatedAbilityID, GO))
                {
                    PowerWordKillActivatedAbilityID = Guid.Empty;
                }
            }
            return removed && PowerWordKillActivatedAbilityID == Guid.Empty;
        }
        public bool RemoveActivatedAbilityPowerWordKill(bool Force = false)
        {
            return RemoveActivatedAbilityPowerWordKill(ParentObject, Force);
        }

        public override string GetMutationType() => "Physical";

        public override bool CanLevel() => false;

        public override string GetDescription()
        {
            return "You're able to drawn on some strange power to " + REANIMATE_NAME + " the corpses around you.";
        }

        public override string GetLevelText(int Level)
        {
            return "You may \"" + REANIMATE_ONE_NAME + "\", reanimating a creature of your choosing, or \"" + REANIMATE_ALL_NAME + "\", reanimating every creature on the screen.\n" +
                "You'll be given the choice of which ones will join you!\n\n" +
                "Additionally, you may \"" + SUMMON_CORPSES_NAME + "\" and \"" + ASSESS_CORPSE_NAME + "\".";
        }

        public override bool Mutate(GameObject GO, int Level = 1)
        {
            AddActivatedAbilityReanimateOne();
            AddActivatedAbilityReanimateAll();
            AddActivatedAbilitySummonCorpses();
            AddActivatedAbilityAssessCorpse();
            AddActivatedAbilityPowerWordKill();
            return base.Mutate(GO, Level);
        }
        public override bool Unmutate(GameObject GO)
        {
            RemoveActivatedAbilityReanimateOne();
            RemoveActivatedAbilityReanimateAll();
            RemoveActivatedAbilitySummonCorpses();
            RemoveActivatedAbilityAssessCorpse();
            RemoveActivatedAbilityPowerWordKill();
            return base.Unmutate(GO);
        }

        public static bool IsCorpse(GameObject Corpse)
        {
            return Corpse.IsCorpse();
        }

        public static bool IsCorpse(GameObjectBlueprint CorpseBlueprint)
        {
            return CorpseBlueprint.IsCorpse();
        }

        public bool ReanimateCorpse(GameObject Corpse, bool SkipReanimatorSwirl = false)
        {
            if (!IsCorpse(Corpse))
            {
                return false;
            }
            if (!SkipReanimatorSwirl)
            {
                ParentObject?.SmallTeleportSwirl(Color: "&K", Sound: "Sounds/StatusEffects/sfx_statusEffect_negativeVitality");
            }
            Corpse?.SmallTeleportSwirl(Color: "&r", Sound: "Sounds/StatusEffects/sfx_statusEffect_positiveVitality", IsOut: true);
            AnimateObject.Animate(Corpse, ParentObject, ParentObject);
            return true;
        }

        public static bool HasCorpse(GameObject Entity)
        {
            return Entity.GetBlueprint().TryGetCorpseBlueprint(out string corpseBlueprint)
                && IsCorpse(corpseBlueprint.GetGameObjectBlueprint());
        }

        public override bool AllowStaticRegistration() => true;

        public override bool WantEvent(int ID, int cascade)
            => base.WantEvent(ID, cascade)
            || ID == CommandEvent.ID;

        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == COMMAND_NAME_REANIMATE_ONE)
            {
                Cell currentCell = ParentObject.CurrentCell;
                GameObject targetCorpse = null;
                if (IsCorpse(ParentObject.Target))
                {
                    targetCorpse = ParentObject.Target;
                }
                if (PickTarget.ShowPicker(
                    PickTarget.PickStyle.EmptyCell,
                    StartX: currentCell.X,
                    StartY: currentCell.Y,
                    ObjectTest: IsCorpse,
                    Label: "Pick a Corpse to " + REANIMATE_NAME) is Cell pickedCell)
                {
                    List<GameObject> cellCorpses = pickedCell.GetObjectsViaEventList(IsCorpse);
                    if (!cellCorpses.IsNullOrEmpty())
                    {
                        if (cellCorpses.Count == 1)
                        {
                            targetCorpse = cellCorpses[0];
                        }
                        else
                        {
                            targetCorpse = Popup.PickGameObject(
                                Title: "Which corpse did you want to " + REANIMATE_NAME,
                                Objects: cellCorpses,
                                AllowEscape: true,
                                ShortDisplayNames: true);
                        }
                    }
                }
                if (targetCorpse != null
                    && ReanimateCorpse(targetCorpse))
                {
                    string recruitMessage = "Would you like for " + targetCorpse.GetReferenceDisplayName(Short: true) + " to join your party";
                    if (Popup.ShowYesNoCancel(
                        Message: recruitMessage) == DialogResult.Yes)
                    {
                        ParentObject?.SmallTeleportSwirl(Color: "&M", Sound: "Sounds/StatusEffects/sfx_statusEffect_charm");
                        targetCorpse?.SetAlliedLeader<AllyProselytize>(ParentObject);
                        targetCorpse?.SmallTeleportSwirl(Color: "&m", IsOut: true);
                    }
                    return true;
                }
            }
            else
            if (E.Command == COMMAND_NAME_REANIMATE_ALL)
            {
                List<GameObject> corpsesInZone = The.ActiveZone.GetObjects(IsCorpse);
                int corpsesInZoneCount = corpsesInZone.Count;
                if (corpsesInZoneCount < 21
                    || Popup.ShowYesNoCancel(
                        Message: 
                        "You sense there are " + corpsesInZoneCount + " corpses in the vacinity.\n\n" +
                        "It may take some time to reanimate them all.\n\n" +
                        "Proceed?") == DialogResult.Yes)
                {
                    ParentObject?.SmallTeleportSwirl(Color: "&K", Sound: "Sounds/StatusEffects/sfx_statusEffect_negativeVitality", IsOut: true);
                    List<GameObject> reanimatedCorpses = Event.NewGameObjectList();
                    foreach (GameObject corpse in corpsesInZone)
                    {
                        try
                        {
                            if (ReanimateCorpse(corpse) && corpse.IsAlive)
                            {
                                reanimatedCorpses.Add(corpse);
                                corpse?.SmallTeleportSwirl(Color: "&r", Sound: "Sounds/StatusEffects/sfx_statusEffect_positiveVitality");
                            }
                        }
                        catch (Exception x)
                        {
                            MetricsManager.LogException(
                                Name + "." + nameof(COMMAND_NAME_REANIMATE_ALL),
                                x: x,
                                category: "game_mod_exception");
                        }
                    }
                    if (!reanimatedCorpses.IsNullOrEmpty())
                    {
                        reanimatedCorpses?.ShuffleInPlace();
                        // ParentObject?.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_negativeVitality");
                        // reanimatedCorpses?.GetRandomElementCosmetic()?.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_positiveVitality");
                        List<string> corpseNames = new();
                        List<IRenderable> corpseRenderables = new();
                        foreach (GameObject reanimatedCorpse in reanimatedCorpses)
                        {
                            corpseNames?.Add(reanimatedCorpse?.GetReferenceDisplayName(Short: true));
                            corpseRenderables?.Add(reanimatedCorpse?.RenderForUI());
                        }
                        if (Popup.PickSeveral(
                            Title: "Pick which corpses should join you.",
                            Context: reanimatedCorpses?.GetRandomElementCosmetic(),
                            AllowEscape: true) is List<(int picked, int amount)> pickedCorpses)
                        {
                            ParentObject?.SmallTeleportSwirl(Color: "&M", Sound: "Sounds/StatusEffects/sfx_statusEffect_charm");
                            foreach ((int picked, int _) in pickedCorpses)
                            {
                                reanimatedCorpses[picked]?.SetAlliedLeader<AllyProselytize>(ParentObject);
                                reanimatedCorpses[picked]?.SmallTeleportSwirl(Color: "&m", IsOut: true);
                            }
                        }
                    }
                    if (!reanimatedCorpses.IsNullOrEmpty())
                    {
                        return true;
                    }
                }
            }
            else
            if (E.Command == COMMAND_NAME_SUMMON_CORPSES)
            {
                if (Popup.AskNumber("How many corpses do you want?", Start: 20, Min: 0, Max: 100) is int corpseCount
                    && corpseCount > 0)
                {
                    if (corpseCount < 21
                        || Popup.ShowYesNoCancel(
                            Message: corpseCount + " is a lot of corpses.\n\n" +
                            "Are you sure you want that many?.") == DialogResult.Yes)
                    {
                        ParentObject?.SmallTeleportSwirl(Color: "&K", Sound: "Sounds/StatusEffects/sfx_statusEffect_negativeVitality");
                        int corpseRadius = corpseCount / 4;
                        int maxAttempts = corpseCount * 2;
                        int originalCorpseCount = corpseCount;
                        List<string> corpseBlueprints = new();
                        Loading.SetLoadingStatus("Summoning " + 0 + "/" + originalCorpseCount + " fresh corpses...");
                        while (corpseCount > 0 && maxAttempts > 0)
                        {
                            maxAttempts--;
                            if (GameObject.CreateSample(EncountersAPI.GetAnItemBlueprint(IsCorpse)) is GameObject corpseObject)
                            {
                                Loading.SetLoadingStatus("Summoning " + (corpseBlueprints.Count + 1) + "/" + originalCorpseCount + " (" + corpseObject.Blueprint + ")...");
                                ParentObject.CurrentCell
                                    .GetAdjacentCells(corpseRadius)
                                    .GetRandomElementCosmeticExcluding(Exclude: c => !c.IsEmptyFor(corpseObject))
                                    .AddObject(corpseObject);

                                if (corpseObject.CurrentCell != null)
                                {
                                    corpseObject.CurrentCell.TelekinesisBlip();

                                    corpseObject?.SmallTeleportSwirl(Color: "&r", IsOut: true);
                                    corpseBlueprints.Add(corpseObject.Blueprint);
                                    corpseCount--;
                                }
                                else
                                {
                                    corpseObject?.Obliterate();
                                }
                            }
                        }
                        Loading.SetLoadingStatus("Summoned " + corpseBlueprints.Count + "/" + originalCorpseCount + " fresh corpses...");
                        if (corpseBlueprints.Count > 0)
                        {
                            string corpseListLabel = "Summoned " + corpseBlueprints.Count + " corpses of various types, in a radius of " + corpseRadius + " cells.";
                            string corpseListOutput = corpseBlueprints.GenerateBulletList(Label: corpseListLabel);
                            Popup.Show(corpseListOutput);

                            Loading.SetLoadingStatus(null);
                            return true;
                        }
                        Loading.SetLoadingStatus(null);
                    }
                }
            }
            else
            if (E.Command == COMMAND_NAME_ASSESS_CORPSE)
            {
                int startX = 40;
                int startY = 12;
                if (ParentObject.CurrentCell is Cell assesserCell)
                {
                    startX = assesserCell.X;
                    startY = assesserCell.Y;
                }
                if (PickTarget.ShowPicker(
                    Style: PickTarget.PickStyle.EmptyCell,
                    StartX: startX,
                    StartY: startY,
                    VisLevel: AllowVis.Any,
                    ObjectTest: IsCorpse,
                    Label: "pick a corpse to get a list of possible creatrues") is Cell pickedCell
                    && pickedCell.GetObjectsViaEventList(Filter: IsCorpse) is List<GameObject> corpseList
                    && !corpseList.IsNullOrEmpty())
                {
                    GameObject targetCorpse = null;
                    if (corpseList.Count == 1)
                    {
                        targetCorpse = corpseList[0];
                    }
                    if (targetCorpse == null
                        && Popup.PickGameObject(
                            Title: "which corpse?",
                            Objects: corpseList,
                            AllowEscape: true,
                            ShortDisplayNames: true) is GameObject pickedObject)
                    {
                        targetCorpse = pickedObject;
                    }
                    if (targetCorpse != null)
                    {
                        ParentObject?.SmallTeleportSwirl(Color: "&K");
                        targetCorpse?.SmallTeleportSwirl(Color: "&r", Sound: "Sounds/Abilities/sfx_ability_mutation_psychometry_activate", IsOut: true);
                        List<string> possibleBlueprints = new(UD_FleshGolems_PastLife.GetBlueprintsWhoseCorpseThisCouldBe(targetCorpse.Blueprint).Keys);

                        string corpseListLabel =
                            targetCorpse.IndicativeProximal + " " + targetCorpse.GetReferenceDisplayName(Short: true) + 
                            " might have been any of the following:";

                        string corpseListOutput = possibleBlueprints.GenerateBulletList(Label: corpseListLabel);
                        Popup.Show(corpseListOutput);
                        return true;
                    }
                }
                Popup.Show("No corpse selected to get a creature list from.");
            }
            else
            if (E.Command == COMMAND_NAME_POWERWORD_KILL)
            {
                int startX = 40;
                int startY = 12;
                if (The.Player.CurrentCell is Cell playerCell)
                {
                    startX = playerCell.X;
                    startY = playerCell.Y;
                }
                if (PickTarget.ShowPicker(
                    Style: PickTarget.PickStyle.EmptyCell,
                    StartX: startX,
                    StartY: startY,
                    VisLevel: AllowVis.Any,
                    ObjectTest: HasCorpse,
                    Label: "pick a target to instantly die") is Cell pickedCell
                    && pickedCell.GetObjectsViaEventList(Filter: HasCorpse) is List<GameObject> creatureList
                    && !creatureList.IsNullOrEmpty())
                {
                    GameObject targetCreature = null;
                    if (creatureList.Count == 1)
                    {
                        targetCreature = creatureList[0];
                    }
                    if (targetCreature == null
                        && Popup.PickGameObject(
                            Title: "which corpse?",
                            Objects: creatureList,
                            AllowEscape: true,
                            ShortDisplayNames: true) is GameObject pickedObject)
                    {
                        targetCreature = pickedObject;
                    }
                    if (targetCreature?.GetPart<Corpse>() is Corpse targetCreatureCorpse)
                    {
                        int corpseChanceBefore = targetCreatureCorpse.CorpseChance;
                        targetCreatureCorpse.CorpseChance = 100;
                        if (targetCreature.Die(
                            Killer: targetCreature,
                            Reason: "=object.T= commanded that =subject.subjective= die, and =subject.subjective= simply did.",
                            ThirdPersonReason: "=object.T= commanded that =subject.subjective= die, and =subject.subjective= simply did.",
                            Accidental: true,
                            Force: true))
                        {
                            pickedCell.PsychicPulse();
                            ParentObject?.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_dig_success");
                            targetCreature?.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_dig_success");
                            return true;
                        }
                        targetCreatureCorpse.CorpseChance = corpseChanceBefore;
                        ParentObject?.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_dig_fail");
                        Popup.Show("you words hang impotently in the air before dissipating without effect...");
                    }
                }
                else
                {
                    Popup.Show("no creatures selected to make instantly die");
                }
            }
            return base.HandleEvent(E);
        }
    }
}
