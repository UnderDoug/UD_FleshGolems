using System;

using XRL.World;

using CountsAs = UD_FleshGolems.Capabilities.UD_FleshGolems_NecromancySystem.CountsAs;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public class EntityWeightCountsAs : EntityWeight, IComposite
    {
        public readonly CountsAs CountsAs = CountsAs.None;

        public EntityWeightCountsAs(EntityBlueprint Entity, int Weight, CountsAs CountsAs) : base(Entity, Weight)
            => this.CountsAs = CountsAs;

        public EntityWeightCountsAs(EntityWeight EntityWeight, CountsAs CountsAs)
            : this(EntityWeight.GetBlueprint(), EntityWeight.Weight, CountsAs) { }

        public override string ToString() => base.ToString() + " (" + CountsAs + ")";

        public void Deconstruct(out EntityBlueprint EntityBlueprint, out int Weight, out CountsAs CountsAs)
        {
            EntityBlueprint = GetBlueprint();
            Weight = this.Weight;
            CountsAs = this.CountsAs;
        }
        public EntityWeight GetEntitWeight()
        {
            return new(GetBlueprint(), Weight);
        }
    }
}

