using System;

using XRL.World;

using Relationship = UD_FleshGolems.Capabilities.Necromancy.CorpseEntityPair.PairRelationship;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public class EntityWeightRelationship : EntityWeight, IComposite
    {
        public readonly Relationship Relationship = Relationship.None;

        public EntityWeightRelationship(EntityWeight EntityWeight, Relationship Relationship)
            : base(EntityWeight.GetBlueprint(), EntityWeight.Weight)
        {
            this.Relationship = Relationship;
        }

        public EntityWeightRelationship(CorpseEntityPair Source) : this(Source.GetEntityWeight(), Source.Relationship) { }

        public override string ToString() => base.ToString() + " (" + Relationship + ")";

        public void Deconstruct(out EntityBlueprint EntityBlueprint, out int Weight, out Relationship Relationship)
        {
            EntityBlueprint = GetBlueprint();
            Weight = this.Weight;
            Relationship = this.Relationship;
        }
        public EntityWeight GetEntitWeight()
        {
            return new(GetBlueprint(), Weight);
        }
    }
}

