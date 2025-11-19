using System;
using System.Collections.Generic;

using XRL.World;

using UD_FleshGolems;
using static UD_FleshGolems.Utils;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public abstract class BlueprintWeight<T>
        : BlueprintWeight
        , IComposite
        , IEquatable<BlueprintWeight>
        where T : BlueprintBox, new()
    {
        public BlueprintWeight(T BlueprintBox, int Weight) : base(BlueprintBox, Weight) { }

        public void Deconstruct(out T Blueprint, out int Weight)
        {
            Blueprint = this.Blueprint as T;
            Weight = this.Weight;
        }

        public bool Equals(BlueprintWeight other)
        {
            if (EitherNull(this, other, out bool areEqual))
            {
                return areEqual;
            }
            return other.Blueprint is T otherBlueprint && Blueprint == otherBlueprint;
        }

        public T GetBlueprint() => Blueprint as T;

        public override KeyValuePair<BlueprintBox, int> GetKeyValuePair()
        {
            return new(GetBlueprint(), Weight);
        }
    }
}
