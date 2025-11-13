using System;
using System.Collections.Generic;
using System.Text;

using ConsoleLib.Console;

using XRL.Language;
using XRL.UI;
using XRL.World.AI;
using XRL.World.Parts.Mutation;

using UD_FleshGolems;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class UD_FleshGolems_NanoNecroAnimation : BaseMutation
    {
        public const string COMMAND_NAME_REANIMATE_ONE = "Command_UD_FleshGolems_ReanimateOne";
        public const string COMMAND_NAME_REANIMATE_ALL = "Command_UD_FleshGolems_ReanimateAll";

        public const string REANIMATE_NAME = "{{UD_FleshGolems_reanimated|re-animate}}";

        public const string REANIMATE_ONE_NAME = REANIMATE_NAME + " one";
        public const string REANIMATE_ALL_NAME = REANIMATE_NAME + " all";

        public Guid ReanimateOneActivatedAbilityID;
        public Guid ReanimateAllActivatedAbilityID;

        public UD_FleshGolems_NanoNecroAnimation()
        {
            ReanimateOneActivatedAbilityID = Guid.Empty;
            ReanimateAllActivatedAbilityID = Guid.Empty;
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
                        Icon: "&#214",
                        UITileDefault: new("Mutations/sunder_mind.bmp", "G", "&r", "&r", 'K'));
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

        public override string GetMutationType() => "Physical";

        public override bool CanLevel() => false;

        public override string GetDescription()
        {
            return "You're able to drawn on some strange power to " + REANIMATE_NAME + " the corpses around you.";
        }

        public override string GetLevelText(int Level)
        {
            return "You may \"" + REANIMATE_ONE_NAME + "\" creature of your choosing, or \"" + REANIMATE_ALL_NAME + "\" creature on the screen.\n" +
                "You'll be given the choice of which ones will join you!";
        }

        public override bool Mutate(GameObject GO, int Level = 1)
        {
            AddActivatedAbilityReanimateOne();
            AddActivatedAbilityReanimateAll();
            return base.Mutate(GO, Level);
        }
        public override bool Unmutate(GameObject GO)
        {
            AddActivatedAbilityReanimateOne();
            AddActivatedAbilityReanimateAll();
            return base.Unmutate(GO);
        }

        public static bool IsCorpse(GameObject Corpse)
        {
            return Corpse.IsCorpse();
        }

        public bool ReanimateCorpse(GameObject Corpse)
        {
            if (!IsCorpse(Corpse))
            {
                return false;
            }
            AnimateObject.Animate(Corpse, ParentObject, ParentObject);
            return true;
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
                        targetCorpse.SetAlliedLeader<AllyProselytize>(ParentObject);
                    }
                    return true;
                }
            }
            else
            if (E.Command == COMMAND_NAME_REANIMATE_ALL)
            {
                List<GameObject> reanimatedCorpses = Event.NewGameObjectList();
                foreach (GameObject corpse in The.ActiveZone.GetObjects(IsCorpse))
                {
                    if (ReanimateCorpse(corpse))
                    {
                        reanimatedCorpses.Add(corpse);
                    }
                }
                if (!reanimatedCorpses.IsNullOrEmpty())
                {
                    List<string> corpseNames = new();
                    List<IRenderable> corpseRenderables = new();
                    foreach (GameObject reanimatedCorpse in reanimatedCorpses)
                    {
                        corpseNames?.Add(reanimatedCorpse.GetReferenceDisplayName(Short: true));
                        corpseRenderables?.Add(reanimatedCorpse.RenderForUI());
                    }
                    if (Popup.PickSeveral(
                        Title: "Pick which corpses should join you.",
                        Context: reanimatedCorpses?.GetRandomElement(),
                        AllowEscape: true) is List<(int picked, int amount)> pickedCorpses)
                    {
                        foreach ((int picked, int _) in pickedCorpses)
                        {
                            reanimatedCorpses[picked]?.SetAlliedLeader<AllyProselytize>(ParentObject);
                        }
                    }
                }
                if (!reanimatedCorpses.IsNullOrEmpty())
                {
                    return true;
                }
            }
            return base.HandleEvent(E);
        }
    }
}
