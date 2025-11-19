using System.Collections.Generic;

using XRL.World;

namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        public partial class CorpseSheet : IComposite
        {
            public partial class EntityWeight
                : BlueprintWeight<EntityBlueprint>
                , IComposite
            {
                public EntityWeight(EntityBlueprint Entity, int Weight) : base(Entity, Weight) { }

                public static implicit operator KeyValuePair<EntityBlueprint, int>(EntityWeight Operand)
                    => new((EntityBlueprint)Operand.Blueprint, Operand.Weight);

                public static implicit operator EntityWeight(KeyValuePair<EntityBlueprint, int> Operand)
                    => new(Operand.Key, Operand.Value);
            }
        }
    }
}
