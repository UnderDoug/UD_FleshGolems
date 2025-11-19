using System;
using System.Collections.Generic;

using XRL.World;

namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        public partial class CorpseSheet : IComposite
        {
            public abstract partial class BlueprintWeight<T> 
                : BlueprintWeight
                , IComposite
                , IEquatable<BlueprintWeight>
                where T : BlueprintBox, new()
            {
                public BlueprintWeight(T BlueprintBox, int Weight) : base(BlueprintBox, Weight) { }

                public static implicit operator KeyValuePair<BlueprintBox, int>(BlueprintWeight<T> Operand)
                    => new(Operand.Blueprint, Operand.Weight);

                public void Deconstruct(out T Blueprint, out int Weight)
                {
                    Blueprint = this.Blueprint as T;
                    Weight = this.Weight;
                }

                public bool Equals(BlueprintWeight other)
                {
                    return (Blueprint == (BlueprintBox)null) == (other == null)
                        || (other.Blueprint is T otherBlueprint
                            && Blueprint == otherBlueprint);
                }

                public T GetBlueprint() => Blueprint as T;
            }
            public abstract partial class BlueprintWeight : IComposite
            {
                public virtual BlueprintBox Blueprint { get; set; }
                public int Weight;
                public BlueprintWeight(BlueprintBox BlueprintBox, int Weight)
                {
                    Blueprint = BlueprintBox;
                    this.Weight = Weight;
                }
                public static implicit operator KeyValuePair<BlueprintBox, int>(BlueprintWeight Operand)
                    => new(Operand.Blueprint, Operand.Weight);

                public void Deconstruct(out BlueprintBox Blueprint, out int Weight)
                {
                    Blueprint = this.Blueprint;
                    Weight = this.Weight;
                }
            }
        }
    }
}
