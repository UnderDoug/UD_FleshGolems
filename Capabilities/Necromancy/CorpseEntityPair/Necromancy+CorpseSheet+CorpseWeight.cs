using System.Collections.Generic;

using XRL.World;

namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        public partial class CorpseSheet : IComposite
        {
            public partial class CorpseWeight : BlueprintWeight<CorpseBlueprint>, IComposite
            {
                public CorpseWeight(CorpseBlueprint Corpse, int Weight) : base (Corpse, Weight) { }

                public static implicit operator KeyValuePair<CorpseBlueprint, int>(CorpseWeight Operand)
                    => new((CorpseBlueprint)Operand.Blueprint, Operand.Weight);

                public static implicit operator CorpseWeight(KeyValuePair<CorpseBlueprint, int>  Operand)
                    => new(Operand.Key, Operand.Value);
            }
        }
    }
}
