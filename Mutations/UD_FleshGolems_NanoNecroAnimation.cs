using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ConsoleLib.Console;

using Qud.API;

using XRL.Language;
using XRL.UI;
using XRL.World.AI;
using XRL.World.Parts.Mutation;
using XRL.World.Effects;
using XRL.World.Capabilities;
using XRL.Messages;

using static XRL.World.Parts.UD_FleshGolems_PastLife;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using ArgPair =  UD_FleshGolems.Logging.Debug.ArgPair;

using UD_FleshGolems.Capabilities.Necromancy;
using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;
using UD_FleshGolems.Parts.VengeanceHelpers;

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

        public static bool IsReanimatableCorpse(GameObject Corpse)
        {
            return Corpse != null
                && Corpse.IsCorpse(IsReanimatableCorpse)
                && !Corpse.HasPart<AnimatedObject>()
                && !Corpse.HasPart<UD_FleshGolems_ReanimatedCorpse>();
        }

        public static bool IsReanimatable(GameObjectBlueprint CorpseBlueprint)
        {
            return CorpseBlueprint != null
                && CorpseBlueprint.HasPart(nameof(UD_FleshGolems_CorpseReanimationHelper))
                && CorpseBlueprint.HasTagOrProperty("Animatable");
        }
        public static bool IsReanimatableCorpse(GameObjectBlueprint CorpseBlueprint)
        {
            return CorpseBlueprint != null
                && CorpseBlueprint.IsCorpse(IsReanimatable);
        }

        public static bool IsCorpse(GameObjectBlueprint CorpseBlueprint)
        {
            return CorpseBlueprint.IsCorpse();
        }

        public bool ReanimateCorpse(GameObject Corpse, bool SkipReanimatorSwirl = false)
        {
            if (!IsReanimatableCorpse(Corpse))
            {
                return false;
            }
            if (!SkipReanimatorSwirl)
            {
                ParentObject?.SmallTeleportSwirl(Color: "&K", Sound: "Sounds/StatusEffects/sfx_statusEffect_negativeVitality");
            }
            Corpse?.SmallTeleportSwirl(Color: "&r", Sound: "Sounds/StatusEffects/sfx_statusEffect_positiveVitality", IsOut: true);
            string corpseTile = Corpse.Render.Tile;
            string corpseForeground = Corpse.Render.GetTileForegroundColor();
            char corpseDetail = Corpse.Render.getDetailColor();
            AnimateObject.Animate(Corpse, ParentObject, ParentObject);
            CombatJuice.playPrefabAnimation(
                gameObject: Corpse,
                animation: "Abilities/AbilityVFXAnimated",
                objectId: Corpse.ID,
                configurationString: corpseTile + ";" + corpseForeground + ";" + corpseDetail);

            "=subject.Name= =subject.verb:ranimate= =object.refname=."
                .StartReplace()
                .AddObject(ParentObject)
                .AddObject(Corpse)
                .EmitMessage();

            return true;
        }

        public virtual bool CanRecruitCorpse(GameObject Corpse)
        {
            return Corpse.TryGetEffect(out UD_FleshGolems_UnendingSuffering FX) && FX.SourceObject == ParentObject;
        }

        public static bool HasCorpse(GameObject Entity)
        {
            return Entity != null
                && Entity.GetBlueprint().TryGetCorpseBlueprint(out string corpseBlueprint)
                && IsCorpse(corpseBlueprint.GetGameObjectBlueprint());
        }

        public static bool TrySummonCorpse(GameObject Summoner, CorpseBlueprint CorpseBlueprint, int CorpseRadius, out GameObject Corpse)
        {
            Corpse = null;
            if (GameObject.CreateSample(CorpseBlueprint.ToString()) is GameObject corpseObject)
            {
                Corpse = corpseObject;
                int corpseRadius = Math.Min(40, CorpseRadius);

                Cell placementCell = Summoner?.CurrentCell
                    ?.GetAdjacentCells(corpseRadius)
                    ?.GetRandomElementCosmeticExcluding(Exclude: c => !c.IsEmptyFor(corpseObject) && c.HasObject(GO => GO.IsCorpse()));

                placementCell ??= Summoner?.CurrentCell
                    ?.GetAdjacentCells(corpseRadius)
                    ?.GetRandomElementCosmeticExcluding(Exclude: c => !c.IsEmptyFor(corpseObject));

                placementCell?.AddObject(Corpse);

                if (Corpse.CurrentCell != null)
                {
                    Corpse = corpseObject;

                    Corpse?.SmallTeleportSwirl(Color: "&r", IsOut: true);

                     "=subject.Name= =subject.verb:summon= =object.an==object.name=."
                        .StartReplace()
                        .AddObject(Summoner)
                        .AddObject(Corpse)
                        .EmitMessage();

                    return true;
                }
                else
                {
                    if (GameObject.Validate(ref Corpse))
                    {
                        Corpse.Obliterate();
                        Corpse = null;
                    }
                }
            }
            return false;
        }

        public bool TrySummonCorpse(int CorpseRadius, CorpseBlueprint CorpseBlueprint, out GameObject Corpse)
        {
            return TrySummonCorpse(ParentObject, CorpseBlueprint, CorpseRadius, out Corpse);
        }

        public bool ProcessReanimateOne(GameObject TargetCorpse = null)
        {
            using Indent indent = new();
            Debug.LogMethod(indent[0], ArgPairs: Debug.Arg(nameof(TargetCorpse), TargetCorpse?.DebugName));

            if (TargetCorpse == null
                && IsReanimatableCorpse(ParentObject?.Target))
            {
                TargetCorpse = ParentObject?.Target;
            }
            int startX = 40;
            int startY = 12;
            if (ParentObject.CurrentCell is Cell reanimatorCell)
            {
                startX = reanimatorCell.X;
                startY = reanimatorCell.Y;
            }
            if (TargetCorpse == null
                && IsReanimatableCorpse(ParentObject.Target))
            {
                TargetCorpse = ParentObject.Target;
            }
            if (TargetCorpse == null
                && PickTarget.ShowPicker(
                    PickTarget.PickStyle.EmptyCell,
                    StartX: startX,
                    StartY: startY,
                    ObjectTest: IsReanimatableCorpse,
                    Label: "Pick a Corpse to " + REANIMATE_NAME) is Cell pickedCell
                && pickedCell.GetObjectsViaEventList(IsReanimatableCorpse) is List<GameObject> cellCorpses
                && !cellCorpses.IsNullOrEmpty())
            {
                if (cellCorpses.Count == 1)
                {
                    TargetCorpse = cellCorpses[0];
                }
                if (TargetCorpse == null
                    && Popup.PickGameObject(
                        Title: "Which corpse did you want to " + REANIMATE_NAME,
                        Objects: cellCorpses,
                        AllowEscape: true,
                        ShortDisplayNames: true) is GameObject pickedObject)
                {
                    TargetCorpse = pickedObject;
                }
                else
                if (TargetCorpse == null)
                {
                    Popup.Show("No corpse selected to reanimate.");
                }
            }
            if (TargetCorpse != null
                && IsReanimatableCorpse(TargetCorpse)
                && ReanimateCorpse(TargetCorpse))
            {
                string recruitMessage = "Would you like for " + TargetCorpse.GetReferenceDisplayName(Short: true) + " to join your party";
                if (Popup.ShowYesNoCancel(
                    Message: recruitMessage) == DialogResult.Yes)
                {
                    ParentObject?.SmallTeleportSwirl(Color: "&M", Sound: "Sounds/StatusEffects/sfx_statusEffect_charm");
                    TargetCorpse?.SetAlliedLeader<AllyProselytize>(ParentObject);
                    TargetCorpse?.SmallTeleportSwirl(Color: "&m", IsOut: true);
                }
                return true;
            }
            return false;
        }

        public bool ProcessAssessCorpse(GameObject TargetCorpse = null)
        {
            using Indent indent = new();
            Debug.LogMethod(indent[0], ArgPairs: Debug.Arg(nameof(TargetCorpse), TargetCorpse?.DebugName));

            if (TargetCorpse == null
                && IsReanimatableCorpse(ParentObject?.Target))
            {
                TargetCorpse = ParentObject?.Target;
            }
            int startX = 40;
            int startY = 12;
            if (ParentObject?.CurrentCell is Cell assesserCell)
            {
                startX = assesserCell.X;
                startY = assesserCell.Y;
            }
            if (TargetCorpse == null
                && PickTarget.ShowPicker(
                    Style: PickTarget.PickStyle.EmptyCell,
                    StartX: startX,
                    StartY: startY,
                    VisLevel: AllowVis.Any,
                    ObjectTest: IsReanimatableCorpse,
                    Label: "pick a corpse to get a list of possible creatrues") is Cell pickedCell
                && pickedCell?.GetObjectsViaEventList(Filter: IsReanimatableCorpse) is List<GameObject> corpseList
                && !corpseList.IsNullOrEmpty())
            {
                if (corpseList.Count == 1)
                {
                    TargetCorpse = corpseList[0];
                }
                if (TargetCorpse == null
                    && Popup.PickGameObject(
                        Title: "which corpse?",
                        Objects: corpseList,
                        AllowEscape: true,
                        ShortDisplayNames: true) is GameObject pickedObject)
                {
                    TargetCorpse = pickedObject;
                }
            }
            if (TargetCorpse != null)
            {
                ParentObject?.SmallTeleportSwirl(Color: "&K");
                TargetCorpse?.SmallTeleportSwirl(
                    Color: "&r",
                    Sound: "Sounds/Abilities/sfx_ability_mutation_psychometry_activate",
                    IsOut: true);

                string corpseListLabel = TargetCorpse?.IndicativeProximal + " " + TargetCorpse?.GetReferenceDisplayName(Short: true);


                if (TargetCorpse.TryGetPart(out UD_FleshGolems_PastLife pastLife)
                    && !pastLife.Blueprint.IsNullOrEmpty())
                {
                    string pastLifeName = pastLife.BaseDisplayName;
                    if (!pastLife.WasProperlyNamed)
                    {
                        pastLifeName = Grammar.A(pastLife.BaseDisplayName);
                    }
                    corpseListLabel += (" was\n" + pastLifeName.PrependBullet() + "\n\n" +
                        "but, if =subject.subjective= =subject.verb:wasn't:afterpronoun=... " +
                        "=subject.subjective= ").StartReplace().ToString();
                }
                corpseListLabel += " might have been any of the following:";

                string corpseListOutput = NecromancySystem
                    ?.GetWeightedEntityStringsThisCorpseCouldBe(TargetCorpse, true, Utils.IsNotBaseBlueprint)
                    ?.ConvertToStringListWithKeyValue()
                    ?.GenerateBulletList(Label: corpseListLabel);

                Popup.Show(corpseListOutput);
                return true;
            }
            else
            {
                Popup.Show("No corpse selected to get a creature list from.");
            }
            return false;
        }

        public bool ProcessPowerWordKill(GameObject TargetCreature = null)
        {
            using Indent indent = new();
            Debug.LogMethod(indent[0], ArgPairs: Debug.Arg(nameof(TargetCreature), TargetCreature?.DebugName));
            if (TargetCreature == null
                && HasCorpse(ParentObject.Target))
            {
                TargetCreature = ParentObject.Target;
            }
            int startX = 40;
            int startY = 12;
            if (ParentObject.CurrentCell is Cell sayerCell)
            {
                startX = sayerCell.X;
                startY = sayerCell.Y;
            }
            if (TargetCreature == null
                && PickTarget.ShowPicker(
                    Style: PickTarget.PickStyle.EmptyCell,
                    StartX: startX,
                    StartY: startY,
                    VisLevel: AllowVis.Any,
                    ObjectTest: HasCorpse,
                    Label: "pick a target to instantly die") is Cell pickedCell
                && pickedCell.GetObjectsViaEventList(Filter: HasCorpse) is List<GameObject> creatureList
                && !creatureList.IsNullOrEmpty())
            {
                if (creatureList.Count == 1)
                {
                    TargetCreature = creatureList[0];
                }
                if (TargetCreature == null
                    && Popup.PickGameObject(
                        Title: "which corpse?",
                        Objects: creatureList,
                        AllowEscape: true,
                        ShortDisplayNames: true) is GameObject pickedObject)
                {
                    TargetCreature = pickedObject;
                }
            }
            if (TargetCreature != null
                && HasCorpse(TargetCreature)
                && TargetCreature?.GetPart<Corpse>() is Corpse targetCreatureCorpse)
            {
                Cell targetCell = TargetCreature?.CurrentCell;

                string sureTargetSelf = ("That's =subject.refname=... \n\n" +
                    "=subject.verb:are:afterpronoun= =subject.subjective= sure =subject.subjective= want to use " +
                    "\"Instantly Die\" on =subject.reflexive=?")
                        .StartReplace()
                        .AddObject(ParentObject)
                        .ToString();

                if (TargetCreature != ParentObject
                    || Popup.ShowYesNoCancel(sureTargetSelf) == DialogResult.Yes)
                {
                    int corpseChanceBefore = targetCreatureCorpse.CorpseChance;
                    targetCreatureCorpse.CorpseChance = 100;
                    string reason = "=subject.verb:were:afterpronoun= commanded to die, and simply did."
                        .StartReplace()
                        .AddObject(TargetCreature)
                        .AddObject(ParentObject)
                        .ToString();
                    string thirdPersonReason = "=object.T= commanded that =subject.subjective= die, and =subject.subjective= simply did."
                        .StartReplace()
                        .AddObject(TargetCreature)
                        .AddObject(ParentObject)
                        .ToString();

                    UD_FleshGolems_DestinedForReanimation.RandomDeathDescriptionAndAccidental(
                        out DeathDescription deathDescription,
                        out bool accidental,
                        dd => dd.Killer != "");

                    thirdPersonReason = "=subject.Subjective= " + reason;

                    if (TargetCreature.Die(
                        Killer: ParentObject,
                        KillerText: null,
                        Reason: deathDescription.Reason(accidental),
                        ThirdPersonReason: deathDescription.ThirdPersonReason(false, accidental),
                        Accidental: accidental,
                        Force: true,
                        DeathVerb: "cease",
                        DeathCategory: deathDescription.Category))
                    {
                        targetCell?.PsychicPulse();
                        ParentObject?.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_dig_success");
                        TargetCreature?.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_dig_success");
                        return true;
                    }

                    targetCreatureCorpse.CorpseChance = corpseChanceBefore;
                    ParentObject?.PlayWorldSound("Sounds/Abilities/sfx_ability_sunderMind_dig_fail");
                    Popup.Show("you words hang impotently in the air before dissipating without effect...");
                    return true;
                }
            }
            else
            {
                Popup.Show("no creatures selected to make instantly die");
            }
            return false;
        }

        public override bool AllowStaticRegistration() => true;

        public override bool WantEvent(int ID, int cascade)
            => base.WantEvent(ID, cascade)
            || ID == InventoryActionEvent.ID
            || ID == CommandEvent.ID;

        public override bool HandleEvent(InventoryActionEvent E)
        {
            // these are provided by UD_FleshGolems_NanoNecroAnimation_Helper
            if (E.Command == COMMAND_NAME_REANIMATE_ONE
                && ProcessReanimateOne(E.Item))
            {
                E.RequestInterfaceExit();
                return true;
            }
            else
            if (E.Command == COMMAND_NAME_ASSESS_CORPSE
                && ProcessAssessCorpse(E.Item))
            {
                // E.RequestInterfaceExit();
                return true;
            }
            else
            if (E.Command == COMMAND_NAME_POWERWORD_KILL
                && ProcessPowerWordKill(E.Item))
            {
                E.RequestInterfaceExit();
                return true;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(CommandEvent E)
        {
            if (E.Command == COMMAND_NAME_REANIMATE_ONE
                && ProcessReanimateOne())
            {
                return true;
            }
            else
            if (E.Command == COMMAND_NAME_REANIMATE_ALL)
            {
                List<GameObject> corpsesInZone = The.ActiveZone.GetObjects(IsReanimatableCorpse);
                int corpsesInZoneCount = corpsesInZone.Count;
                if (corpsesInZoneCount < 21
                    || Popup.ShowYesNoCancel(
                        Message: "You sense there are " + corpsesInZoneCount + " corpses in the vacinity.\n\n" +
                            "It may take some time to reanimate them all.\n\n" +
                            "Proceed?") == DialogResult.Yes)
                {
                    ParentObject?.SmallTeleportSwirl(Color: "&K", Sound: "Sounds/StatusEffects/sfx_statusEffect_negativeVitality", IsOut: true);
                    UD_FleshGolems_OngoingReanimate ongoingReanimate = new(ParentObject, corpsesInZone, 100);

                    AutoAct.Action = ongoingReanimate;
                    AutoAct.Setting = "o";
                    The.Player.ForfeitTurn(EnergyNeutral: true);

                    E.RequestInterfaceExit();
                    return true;
                }
            }
            else
            if (E.Command == COMMAND_NAME_SUMMON_CORPSES)
            {
                if (Popup.AskNumber(
                    Message: "How many corpses do you want?",
                    Start: 20,
                    Min: 0,
                    Max: 1000) is int corpseCount
                    && corpseCount > 0)
                {
                    string excessiveCorpsesMsg = corpseCount + " is ";
                    excessiveCorpsesMsg += 
                        corpseCount > 100
                        ? "is an inordinate amount"
                        : "is a lot";
                    excessiveCorpsesMsg += " of corpses.\n\n";

                    if (corpseCount > 20
                        && Popup.ShowYesNoCancel(
                            Message: excessiveCorpsesMsg +
                            "You can cancel this mid-way the same way as other auto actions.\n\n" +
                            "Are you sure you want that many?") != DialogResult.Yes)
                    {
                        return false;
                    }

                    UD_FleshGolems_OngoingSummonCorpse ongoingSummon = new(
                        Summoner: ParentObject,
                        NumberWanted: corpseCount,
                        EnergyCostPer: 100,
                        Unique: Popup.ShowYesNoCancel(
                            "Unique corpses for as many corpses as possible?") == DialogResult.Yes);

                    AutoAct.Action = ongoingSummon;
                    AutoAct.Setting = "o";
                    The.Player.ForfeitTurn(EnergyNeutral: true);

                    E.RequestInterfaceExit();
                    return true;
                }
            }
            else
            if (E.Command == COMMAND_NAME_ASSESS_CORPSE
                && ProcessAssessCorpse())
            {
                return true;
            }
            else
            if (E.Command == COMMAND_NAME_POWERWORD_KILL
                && ProcessPowerWordKill())
            {
                return true;
            }
            return base.HandleEvent(E);
        }
    }
}
