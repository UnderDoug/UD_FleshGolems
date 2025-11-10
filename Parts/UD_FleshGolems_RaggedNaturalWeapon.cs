using System;
using System.Collections.Generic;
using System.Text;

using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_RaggedNaturalWeapon : IModification
    {
        public static string Adjective => "{{UD_FleshGolems_ragged|ragged}}";

        [SerializeField]
        private bool DisplayNameAdjusted;
        
        [SerializeField]
        private string WielderID;

        private GameObject _Wielder;
        public GameObject Wielder
        {
            get => _Wielder ??= GameObject.FindByID(WielderID);
            set
            {
                WielderID = value?.ID;
                _Wielder = value;
            }
        }

        [SerializeField]
        private string Description;

        public UD_FleshGolems_RaggedNaturalWeapon()
        {
            DisplayNameAdjusted = false;
            WielderID = null;
            _Wielder = null;
            Description = null;
        }

        public override void Configure()
        {
            WorksOnSelf = true;
        }

        public override int GetModificationSlotUsage() => 0;

        public override bool ModificationApplicable(GameObject Object) => 
            Object.HasPart<Physics>()
            && Object.IsNatural();

        public override void ApplyModification(GameObject obj)
        {
            
            base.ApplyModification(obj);
        }

        public override bool AllowStaticRegistration() => true;

        public void ProcessDescriptionElements(GameObject Wielder = null)
        {
            if (ParentObject.TryGetPart(out Description descriptionPart)
                && Wielder != null || this.Wielder != null)
            {
                Description = descriptionPart._Short.StartReplace().AddObject(Wielder).ToString();
                UnityEngine.Debug.Log(descriptionPart._Short.StartReplace().AddObject(Wielder).ToString());
                UnityEngine.Debug.Log(Description);
                descriptionPart._Short = Description;
                UnityEngine.Debug.Log(descriptionPart._Short);
            }
        }

        public override bool WantEvent(int ID, int cascade) => base.WantEvent(ID, cascade)
            || ID == GetDisplayNameEvent.ID
            || ID == GetShortDescriptionEvent.ID;

        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if ((E.Understood() || E.AsIfKnown) && !E.Object.HasProperName)
            {
                if (!DisplayNameAdjusted
                    && ParentObject.Render is Render render
                    && render.DisplayName.Contains("ragged "))
                {
                    render.DisplayName = render.DisplayName.Replace("ragged ", "");
                    DisplayNameAdjusted = true;
                }
                E.AddAdjective(Adjective);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (Description.IsNullOrEmpty() && Wielder != null)
            {
                ProcessDescriptionElements(Wielder);
            }
            return base.HandleEvent(E);
        }
    }
}
