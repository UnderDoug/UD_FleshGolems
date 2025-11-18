using System;

using XRL.World;

namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        public partial class CorpseSheet : IComposite
        {
            public partial class CorpseBlueprint 
                : BlueprintWrapper
                , IComposite
                , IEquatable<CorpseBlueprint>
            {
                public CorpseBlueprint() : base() { }
                public CorpseBlueprint(string Blueprint) : base(Blueprint) { }
                public CorpseBlueprint(GameObjectBlueprint Blueprint) : base(Blueprint) { }
                public CorpseBlueprint(BlueprintWrapper Source) : base(Source.Blueprint) { }
                public CorpseBlueprint(CorpseBlueprint Source) : base(Source) { }

                public static explicit operator CorpseBlueprint(string Operand) => new(Operand);
                public static explicit operator CorpseBlueprint(GameObjectBlueprint Operand) => new(Operand);

                // Equality
                public override bool Equals(object obj) => base.Equals(obj);

                public override int GetHashCode() => base.GetHashCode();

                public override bool Equals(BlueprintWrapper other)
                    => other is CorpseBlueprint otherEntity
                    && Equals(otherEntity);

                public bool Equals(CorpseBlueprint other) => Blueprint == other.Blueprint;

                public static bool operator ==(CorpseBlueprint Operand1, EntityBlueprint Operand2)
                    => Operand1.GetType() == Operand2.GetType()
                    && Operand1.Blueprint == Operand2.Blueprint;
                public static bool operator !=(CorpseBlueprint Operand1, EntityBlueprint Operand2) => !(Operand1 == Operand2);

                public static bool operator ==(CorpseBlueprint Operand1, string Operand2) => Operand1.Blueprint == Operand2;
                public static bool operator !=(CorpseBlueprint Operand1, string Operand2) => !(Operand1 == Operand2);

                public static bool operator ==(CorpseBlueprint Operand1, GameObjectBlueprint Operand2) => Operand1 == Operand2.Name;
                public static bool operator !=(CorpseBlueprint Operand1, GameObjectBlueprint Operand2) => !(Operand1 == Operand2);
            }
        }
    }
}
