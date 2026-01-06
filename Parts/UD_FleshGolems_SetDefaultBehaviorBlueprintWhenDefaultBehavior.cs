using System;
using System.Collections.Generic;
using System.Text;

using XRL.World.Anatomy;

using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_SetDefaultBehaviorBlueprintWhenDefaultBehavior : IScribedPart
    {
        [SerializeField]
        private bool Assigned = false;
        [SerializeField]
        private bool RaggedAssigned = false;

        public bool ProcessAssignment(BodyPart bodyPart, GameObject Wielder = null)
        {
            if (!Assigned
                && bodyPart.DefaultBehavior == ParentObject
                && bodyPart.DefaultBehaviorBlueprint != ParentObject.Blueprint)
            {
                bodyPart.DefaultBehaviorBlueprint = ParentObject.Blueprint;
                Assigned = bodyPart.DefaultBehaviorBlueprint == ParentObject.Blueprint;
            }
            return true;
        }

        public bool ProcessRaggedDescription(GameObject Wielder = null)
        {
            if (!RaggedAssigned
                && ParentObject.TryGetPart(out UD_FleshGolems_RaggedNaturalWeapon raggedNaturalWeapon)
                && Wielder != null)
            {
                raggedNaturalWeapon.Wielder = Wielder;
                raggedNaturalWeapon.ClearDescription();
                raggedNaturalWeapon.ProcessDescriptionElements(Wielder);
                RaggedAssigned = true;
                return true;
            }
            return false;
        }

        public bool UnprocessAssignment(BodyPart bodyPart)
        {
            if (Assigned)
            {
                bodyPart.DefaultBehaviorBlueprint = ParentObject.Blueprint;
                if (RaggedAssigned
                    && ParentObject.TryGetPart(out UD_FleshGolems_RaggedNaturalWeapon raggedNaturalWeapon))
                {
                    raggedNaturalWeapon.Wielder = null;
                    RaggedAssigned = false;
                }
            }
            return true;
        }

        public override bool AllowStaticRegistration() => true;

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(EquippedEvent.ID, EventOrder.EXTREMELY_LATE, true);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade) => base.WantEvent(ID, Cascade)
            // || (!Assigned && ID == EquippedEvent.ID)
            || (RaggedAssigned && ID == UnequippedEvent.ID)
            || (!Assigned && ID == BeforeMeleeAttackEvent.ID);

        public override bool HandleEvent(EquippedEvent E)
        {
            if (!Assigned
                && E.Item is GameObject item
                && item == ParentObject
                && E.Part is BodyPart bodyPart
                && bodyPart.DefaultBehavior == item
                && bodyPart.DefaultBehaviorBlueprint != item.Blueprint
                && bodyPart.ParentBody.ParentObject is GameObject wielder)
            {
                ProcessRaggedDescription(wielder);
                ProcessAssignment(bodyPart, wielder);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeMeleeAttackEvent E)
        {
            if (!Assigned
                && E.Weapon is GameObject item
                && item == ParentObject
                && E.Actor?.Body.FindDefaultBehavior(item) is BodyPart bodyPart
                && bodyPart.DefaultBehavior == item
                && bodyPart.DefaultBehaviorBlueprint != item.Blueprint
                && bodyPart.ParentBody.ParentObject is GameObject wielder)
            {
                ProcessAssignment(bodyPart, wielder);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeUnequippedEvent E)
        {
            if (!Assigned
                && E.Item is GameObject item
                && item == ParentObject
                && E.Actor?.Body.FindDefaultBehavior(item) is BodyPart bodyPart)
            {
                UnprocessAssignment(bodyPart);
            }
            return base.HandleEvent(E);
        }
    }
}
