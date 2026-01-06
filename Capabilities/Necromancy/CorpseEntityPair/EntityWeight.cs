using System;

using XRL.World;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public class EntityWeight : BlueprintWeight<EntityBlueprint>, IComposite
    {
        public EntityWeight(EntityBlueprint Entity, int Weight) : base(Entity, Weight) { }
    }
}

