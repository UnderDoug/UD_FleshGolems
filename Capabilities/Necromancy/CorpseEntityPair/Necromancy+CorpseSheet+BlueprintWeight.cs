using System.Collections.Generic;

using XRL.World;

namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        public partial class CorpseSheet : IComposite
        {
            public abstract partial class BlueprintWeight : IComposite
            {
                public BlueprintWrapper Blueprint;
                public int Weight;
                public BlueprintWeight(BlueprintWrapper BlueprintWrapper, int Weight)
                {
                    this.Blueprint = BlueprintWrapper;
                    this.Weight = Weight;
                }
                public static implicit operator KeyValuePair<BlueprintWrapper, int>(BlueprintWeight Operand) => new(Operand.Blueprint, Operand.Weight);
            }
        }
    }
}
