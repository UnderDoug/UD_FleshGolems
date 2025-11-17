using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using Genkit;
using Qud.API;

using XRL.UI;
using XRL.Wish;
using XRL.Rules;
using XRL.Language;
using XRL.Collections;
using XRL.World;
using XRL.World.AI;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.Anatomy;
using XRL.World.ObjectBuilders;

using static XRL.World.Parts.UD_FleshGolems_CorpseReanimationHelper;
using static XRL.World.Parts.Mutation.UD_FleshGolems_NanoNecroAnimation;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using SerializeField = UnityEngine.SerializeField;
using XRL;
using XRL.World.Parts;

namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        public partial class CorpseSheet : IComposite
        {
            [Serializable]
            public class Corpse : IEquatable<KeyValuePair<string, int>>, IComparable<Corpse>, IComparable<KeyValuePair<string, int>>
            {
                public class CorpseBlueprintEqualityComparer : IEqualityComparer<Corpse>
                {
                    public bool Equals(Corpse x, Corpse y)
                    {
                        return x.Blueprint == y.Blueprint;
                    }

                    public int GetHashCode(Corpse obj)
                    {
                        return obj.Blueprint.GetHashCode();
                    }
                }

                public string Blueprint;
                public int Weight;

                private Corpse()
                {
                    Blueprint = null;
                    Weight = 0;
                }

                public Corpse(string Blueprint, int Weight)
                    : this()
                {
                    this.Blueprint = Blueprint;
                    this.Weight = Weight;
                }

                public Corpse(Corpse BlueprintWeightPair)
                    : this(BlueprintWeightPair.Blueprint, BlueprintWeightPair.Weight)
                {
                }

                public GameObjectBlueprint GetGameObjectBlueprint()
                {
                    return Blueprint.GetGameObjectBlueprint();
                }

                public override string ToString()
                {
                    return Blueprint + ": " + Weight;
                }

                public void Deconstruct(out string Blueprint) => Blueprint = this.Blueprint;
                public void Deconstruct(out int Weight) => Weight = this.Weight;
                public void Deconstruct(out string Blueprint, out int Weight)
                {
                    Deconstruct(out Blueprint);
                    Deconstruct(out Weight);
                }
                public void Deconstruct(out GameObjectBlueprint Blueprint) => Blueprint = GetGameObjectBlueprint();

                public static implicit operator KeyValuePair<string, int>(Corpse Operand) => new(Operand.Blueprint, Operand.Weight);
                public static implicit operator Corpse(KeyValuePair<string, int> Operand) => new(Operand.Key, Operand.Value);

                public static explicit operator string(Corpse Operand) => Operand.Blueprint;
                public static explicit operator int(Corpse Operand) => Operand.Weight;
                public static explicit operator GameObjectBlueprint(Corpse Operand) => Operand.GetGameObjectBlueprint();

                // Equality
                public override bool Equals(object obj)
                {
                    if (obj == null)
                    {
                        return false;
                    }
                    if (obj is KeyValuePair<string, int> kvpObj)
                    {
                        return Equals(kvpObj);
                    }
                    return base.Equals(obj);
                }

                public override int GetHashCode() => Blueprint.GetHashCode() ^ Weight.GetHashCode();

                public bool Equals(KeyValuePair<string, int> other)
                {
                    return Blueprint.Equals(other.Key)
                        && Weight.Equals(other.Value);
                }

                public static bool operator ==(Corpse Operand1, KeyValuePair<string, int> Operand2) => Operand1.Equals(Operand2);
                public static bool operator !=(Corpse Operand1, KeyValuePair<string, int> Operand2) => !(Operand1 == Operand2);

                public static bool operator ==(KeyValuePair<string, int> Operand1, Corpse Operand2) => Operand2 == Operand1;
                public static bool operator !=(KeyValuePair<string, int> Operand1, Corpse Operand2) => Operand2 != Operand1;

                // Comparison
                public int CompareTo(Corpse other)
                {
                    if (other == null)
                    {
                        return 1;
                    }
                    return Weight.CompareTo(other.Weight);
                }

                public int CompareTo(KeyValuePair<string, int> other)
                {
                    return CompareTo((Corpse)other);
                }

                public static bool operator >(Corpse Operand1, Corpse Operand2) => Operand1.Weight > Operand2.Weight;
                public static bool operator <(Corpse Operand1, Corpse Operand2) => Operand1.Weight < Operand2.Weight;
                public static bool operator >=(Corpse Operand1, Corpse Operand2) => Operand1.Weight >= Operand2.Weight;
                public static bool operator <=(Corpse Operand1, Corpse Operand2) => Operand1.Weight <= Operand2.Weight;
            }
        }
    }
}
