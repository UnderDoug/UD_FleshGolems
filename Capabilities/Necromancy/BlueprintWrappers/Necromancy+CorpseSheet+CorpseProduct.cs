using System;
using System.Collections.Generic;
using System.Linq;

using XRL.World;

namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        public partial class CorpseSheet : IComposite
        {
            public partial class CorpseProduct 
                : CorpseBlueprint
                , IComposite
                , IEquatable<CorpseProduct>
                , IEquatable<KeyValuePair<CorpseBlueprint, List<CorpseBlueprint>>>
            {
                public List<CorpseBlueprint> CorpseBlueprints;

                private CorpseProduct() : base() { CorpseBlueprints = new(); }

                public CorpseProduct(string Blueprint) : base(Blueprint) {  CorpseBlueprints = new(); }
                public CorpseProduct(CorpseBlueprint CorpseBlueprint) : base(CorpseBlueprint) {  CorpseBlueprints = new(); }
                public CorpseProduct(string Blueprint, List<CorpseBlueprint> CorpseBlueprints) : base(Blueprint) { this.CorpseBlueprints = CorpseBlueprints; }
                public CorpseProduct(CorpseBlueprint CorpseBlueprint, List<CorpseBlueprint> CorpseBlueprints) : base(CorpseBlueprint) { this.CorpseBlueprints = CorpseBlueprints; }

                public void Deconstruct(out string Blueprint) => Blueprint = this.Name;

                public void Deconstruct(out CorpseBlueprint CorpseBlueprint) => CorpseBlueprint = this;
                public void Deconstruct(out List<CorpseBlueprint> CorpseBlueprints) => CorpseBlueprints = this.CorpseBlueprints;
                public void Deconstruct(out CorpseBlueprint CorpseBlueprint, out List<CorpseBlueprint> CorpseBlueprints)
                {
                    Deconstruct(out CorpseBlueprint);
                    Deconstruct(out CorpseBlueprints);
                }

                public static implicit operator KeyValuePair<CorpseBlueprint, List<CorpseBlueprint>>(CorpseProduct Operand) => new(Operand, Operand.CorpseBlueprints);
                public static implicit operator CorpseProduct(KeyValuePair<CorpseBlueprint, List<CorpseBlueprint>> Operand) => new(Operand.Key, Operand.Value);

                public static explicit operator string(CorpseProduct Operand) => Operand.Name;
                public static explicit operator List<CorpseBlueprint>(CorpseProduct Operand) => Operand.CorpseBlueprints;
                public static explicit operator GameObjectBlueprint(CorpseProduct Operand) => Operand.GetGameObjectBlueprint();

                public override bool Equals(object obj)
                {
                    if (obj == null)
                    {
                        return false;
                    }
                    if (obj is KeyValuePair<CorpseBlueprint, List<CorpseBlueprint>> kvpObj)
                    {
                        return Equals(kvpObj);
                    }
                    return base.Equals(obj);
                }
                public override int GetHashCode() => Name.GetHashCode() ^ CorpseBlueprints.GetHashCode();

                public bool Equals(KeyValuePair<CorpseBlueprint, List<CorpseBlueprint>> other)
                    => !other.Equals(default)
                    && Name.Equals(other.Key)
                    && CorpseBlueprints.Equals(other.Value);

                public bool Equals(CorpseProduct other)
                    => !other.Equals(null)
                    && Name.Equals(other.Name)
                    && CorpseBlueprints.All(cbp => other.CorpseBlueprints.Any(ocbp =>  cbp.Equals(ocbp)));
                        // all blueprints are equal to any other blueprint

                // with self
                public static bool operator ==(CorpseProduct Operand1, CorpseProduct Operand2) => Operand1.Equals(Operand2);
                public static bool operator !=(CorpseProduct Operand1, CorpseProduct Operand2) => !(Operand1 == Operand2);

                public static bool operator ==(CorpseProduct Operand1, EntityBlueprint Operand2)
                    => Operand1.GetType() == Operand2.GetType()
                    && Operand1.Name == Operand2.Name;
                public static bool operator !=(CorpseProduct Operand1, EntityBlueprint Operand2) => !(Operand1 == Operand2);

                public static bool operator ==(CorpseProduct Operand1, CorpseBlueprint Operand2) => ((CorpseBlueprint)Operand1) == Operand2;
                public static bool operator !=(CorpseProduct Operand1, CorpseBlueprint Operand2) => !(Operand1 == Operand2);

                public static bool operator ==(CorpseProduct Operand1, string Operand2) => Operand1.Name == Operand2;
                public static bool operator !=(CorpseProduct Operand1, string Operand2) => !(Operand1 == Operand2);

                public static bool operator ==(CorpseProduct Operand1, GameObjectBlueprint Operand2) => ((GameObjectBlueprint)Operand1).Name == Operand2.Name;
                public static bool operator !=(CorpseProduct Operand1, GameObjectBlueprint Operand2) => !(Operand1 == Operand2);
            }
        }
    }
}
