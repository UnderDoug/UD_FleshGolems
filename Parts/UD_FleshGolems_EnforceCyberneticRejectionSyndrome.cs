using System;
using System.Collections.Generic;
using System.Text;

using XRL.World.Anatomy;

using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_EnforceCyberneticRejectionSyndrome : IScribedPart
    {
        
        public override bool AllowStaticRegistration() => true;

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            // Registrar.Register(EquippedEvent.ID, EventOrder.EXTREMELY_LATE, true);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade) => base.WantEvent(ID, Cascade)
            || ID == EquippedEvent.ID
            || ID == UnequippedEvent.ID
            || ID == BeforeMeleeAttackEvent.ID;

        public override bool HandleEvent(EquippedEvent E)
        {
            
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeMeleeAttackEvent E)
        {
            
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeUnequippedEvent E)
        {
            
            return base.HandleEvent(E);
        }
    }
}
