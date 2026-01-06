using System;
using System.Collections.Generic;

using XRL.World;

using static UD_FleshGolems.Utils;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public abstract class BlueprintWeight : IComposite
    {
        public class BlueprintWeightComparer : Comparer<BlueprintWeight>, IComparer<BlueprintWeight>
        {
            public override int Compare(BlueprintWeight x, BlueprintWeight y)
            {
                if (EitherNull(x, y, out int Comparison))
                {
                    return Comparison;
                }
                return x.Weight.CompareTo(y.Weight);
            }
        }

        public virtual BlueprintBox Blueprint { get; set; }
        public int Weight;

        public BlueprintWeight(BlueprintBox BlueprintBox, int Weight)
        {
            Blueprint = BlueprintBox;
            this.Weight = Weight;
        }

        public void Deconstruct(out BlueprintBox Blueprint, out int Weight)
        {
            Blueprint = this.Blueprint;
            Weight = this.Weight;
        }

        public virtual KeyValuePair<BlueprintBox, int> GetKeyValuePair()
        {
            return new(Blueprint, Weight);
        }

        public override string ToString()
        {
            return Blueprint.ToString() + ":" + Weight;
        }
    }
}
