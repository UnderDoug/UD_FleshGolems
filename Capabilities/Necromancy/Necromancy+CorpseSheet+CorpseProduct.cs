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
            public class CorpseProduct : IComposite, IEquatable<KeyValuePair<string, List<string>>>, IEquatable<Corpse>
            {
                public string Blueprint;

                public List<string> CorpseBlueprints;

                private CorpseProduct()
                {
                    Blueprint = null;
                    CorpseBlueprints = new();
                }

                public CorpseProduct(string Blueprint)
                    : this()
                {
                    this.Blueprint = Blueprint;
                }
                public CorpseProduct(string Blueprint, List<string> CorpseBlueprints)
                    : this(Blueprint)
                {
                    this.CorpseBlueprints = CorpseBlueprints;
                }

                public GameObjectBlueprint GetGameObjectBlueprint()
                {
                    return Blueprint.GetGameObjectBlueprint();
                }

                public override string ToString()
                {
                    return Blueprint;
                }

                public void Deconstruct(out string Blueprint) => Blueprint = this.Blueprint;
                public void Deconstruct(out List<string> CorpseBlueprints) => CorpseBlueprints = this.CorpseBlueprints;
                public void Deconstruct(out string Blueprint, out List<string> CorpseBlueprints)
                {
                    Deconstruct(out Blueprint);
                    Deconstruct(out CorpseBlueprints);
                }
                public void Deconstruct(out GameObjectBlueprint Blueprint) => Blueprint = GetGameObjectBlueprint();

                public static implicit operator KeyValuePair<string, List<string>>(CorpseProduct Operand) => new(Operand.Blueprint, Operand.CorpseBlueprints);
                public static implicit operator CorpseProduct(KeyValuePair<string, List<string>> Operand) => new(Operand.Key, Operand.Value);

                public static explicit operator string(CorpseProduct Operand) => Operand.Blueprint;
                public static explicit operator List<string>(CorpseProduct Operand) => Operand.CorpseBlueprints;
                public static explicit operator GameObjectBlueprint(CorpseProduct Operand) => Operand.GetGameObjectBlueprint();

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
                public override int GetHashCode() => Blueprint.GetHashCode() ^ CorpseBlueprints.GetHashCode();

                public bool Equals(KeyValuePair<string, List<string>> other)
                {
                    return !other.Equals(default)
                        && Blueprint.Equals(other.Key)
                        && CorpseBlueprints.Equals(other.Value);
                }

                public bool Equals(Corpse other)
                {
                    return !CorpseBlueprints.IsNullOrEmpty()
                        && other is not null
                        && CorpseBlueprints.Contains(other.Blueprint);
                }

                public static bool operator ==(CorpseProduct Operand1, KeyValuePair<string, List<string>> Operand2) => Operand1.Equals(Operand2);
                public static bool operator !=(CorpseProduct Operand1, KeyValuePair<string, List<string>> Operand2) => !(Operand1 == Operand2);
                public static bool operator ==(KeyValuePair<string, List<string>> Operand1, CorpseProduct Operand2) => Operand2 == Operand1;
                public static bool operator !=(KeyValuePair<string, List<string>> Operand1, CorpseProduct Operand2) => Operand2 != Operand1;

                public static bool operator ==(CorpseProduct Operand1, Corpse Operand2) => Operand1.Equals(Operand2);
                public static bool operator !=(CorpseProduct Operand1, Corpse Operand2) => !(Operand1 == Operand2);
                public static bool operator ==(Corpse Operand1, CorpseProduct Operand2) => Operand2 == Operand1;
                public static bool operator !=(Corpse Operand1, CorpseProduct Operand2) => Operand2 != Operand1;
            }
        }
    }
}
