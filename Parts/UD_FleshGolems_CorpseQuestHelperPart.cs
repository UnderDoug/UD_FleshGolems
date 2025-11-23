using System;
using System.Collections.Generic;
using System.Text;

using XRL.World;
using XRL.Liquids;
using UD_FleshGolems;

namespace XRL.World.Parts
{
    public  class UD_FleshGolems_CorpseQuestHelperPart : IScribedPart
    {
        private bool _MarkedForCollection;
        public bool MarkedForCollection
        {
            get => _MarkedForCollection;
            set
            {
                ParentObject
                    ?.SetWontSell(value)
                    ?.SetImportant(value);
                _MarkedForCollection = value;
            }
        }

        public bool Primed;

        public bool IsCorpse => ParentObject != null
            && ParentObject.GetBlueprint().InheritsFrom("Corpse");

        public bool IsBrainBrine => ParentObject != null
            && ParentObject.TryGetPart(out LiquidVolume liquidVolume)
            && liquidVolume.IsPureLiquid(false)
            && liquidVolume.Primary == LiquidBrainBrine.ID;

        public bool IsMedicalBed => ParentObject != null
            && ParentObject.HasPart<Bed>() 
            && ParentObject.GetBlueprint().InheritsFrom("Medical Bed");

        public bool IsElectrothing => ParentObject != null
            && ParentObject.GetBlueprint().InheritsFrom("Electrothing");

        public UD_FleshGolems_CorpseQuestHelperPart()
        {
            MarkedForCollection = false;
            Primed = false;
        }

        public static bool ObjectIsCorpse(GameObject GO)
            => GO.TryGetPart(out UD_FleshGolems_CorpseQuestHelperPart corpseQuestHelperPart)
            && corpseQuestHelperPart.IsCorpse;

        public static bool ObjectIsBrainBrine(GameObject GO)
            => GO.TryGetPart(out UD_FleshGolems_CorpseQuestHelperPart corpseQuestHelperPart)
            && corpseQuestHelperPart.IsBrainBrine;

        public static bool ObjectIsMedicalBed(GameObject GO)
            => GO.TryGetPart(out UD_FleshGolems_CorpseQuestHelperPart corpseQuestHelperPart)
            && corpseQuestHelperPart.IsMedicalBed;

        public static bool ObjectIsBrainBrineOrMedicalBed(GameObject GO)
            => ObjectIsBrainBrine(GO)
            || ObjectIsMedicalBed(GO);

        public static bool CellHasMedicalBed(Cell Cell)
            => !Cell.GetObjects(ObjectIsMedicalBed).IsNullOrEmpty();

        public static bool CellHasBrainBrine(Cell Cell)
            => !Cell.GetObjects(ObjectIsBrainBrine).IsNullOrEmpty();

        public static bool CellHasBrainBrineOrMedicalBed(Cell Cell)
            => CellHasMedicalBed(Cell)
            || CellHasBrainBrine(Cell);

        public override bool WantEvent(int ID, int Cascade) => base.WantEvent(ID, Cascade)
            || ID == EnteredCellEvent.ID
            || ID == BeforeApplyDamageEvent.ID;

        public override bool HandleEvent(EnteredCellEvent E)
        {
            if (E.Object == ParentObject)
            {
                if ((IsCorpse && CellHasBrainBrineOrMedicalBed(E.Cell))
                    || (IsBrainBrine && E.Cell.GetObjects(ObjectIsCorpse).Count > 2 && CellHasMedicalBed(E.Cell))
                    || (IsMedicalBed && E.Cell.GetObjects(ObjectIsCorpse).Count > 2 && CellHasBrainBrine(E.Cell)))
                {
                    Primed = true;
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeApplyDamageEvent E)
        {
            if (E.Damage.IsElectricDamage())
            {
                if (Primed || (IsElectrothing && ParentObject.CurrentCell.AnyAdjacentCell(CellHasMedicalBed)))
                {
                    E.Damage.Amount = Math.Min(ParentObject.GetStat("Hitpoints").Value - 1, E.Damage.Amount);
                }
            }
            return base.HandleEvent(E);
        }
    }
}
