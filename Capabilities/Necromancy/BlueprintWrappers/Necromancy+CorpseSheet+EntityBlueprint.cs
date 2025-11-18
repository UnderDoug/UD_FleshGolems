using System;

using XRL.World;

namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        public partial class CorpseSheet : IComposite
        {
            public partial class EntityBlueprint 
                : BlueprintWrapper
                , IComposite
                , IEquatable<EntityBlueprint>
            {
                public EntityBlueprint() : base() { }
                public EntityBlueprint(string Blueprint) : base(Blueprint) { }
                public EntityBlueprint(GameObjectBlueprint Blueprint) : base(Blueprint) { }
                public EntityBlueprint(BlueprintWrapper Source) : base(Source.Blueprint) { }
                public EntityBlueprint(EntityBlueprint Source) : base(Source) { }

                public static explicit operator EntityBlueprint(string Operand) => new(Operand);
                public static explicit operator EntityBlueprint(GameObjectBlueprint Operand) => new(Operand);

                // Equality
                public override bool Equals(object obj) => base.Equals(obj);

                public override int GetHashCode() => base.GetHashCode();

                public override bool Equals(BlueprintWrapper other)
                    => other is EntityBlueprint otherEntity 
                    && Equals(otherEntity);

                public bool Equals(EntityBlueprint other) => Blueprint == other.Blueprint;

                public static bool operator ==(EntityBlueprint Operand1, CorpseBlueprint Operand2)
                    => Operand1.GetType() == Operand2.GetType()
                    && Operand1.Blueprint == Operand2.Blueprint;
                public static bool operator !=(EntityBlueprint Operand1, CorpseBlueprint Operand2) => !(Operand1 == Operand2);

                public static bool operator ==(EntityBlueprint Operand1, CorpseProduct Operand2)
                    => Operand1.GetType() == Operand2.GetType()
                    && false;
                public static bool operator !=(EntityBlueprint Operand1, CorpseProduct Operand2) => !(Operand1 == Operand2);

                public static bool operator ==(EntityBlueprint Operand1, string Operand2) => Operand1.Blueprint == Operand2;
                public static bool operator !=(EntityBlueprint Operand1, string Operand2) => !(Operand1 == Operand2);

                public static bool operator ==(EntityBlueprint Operand1, GameObjectBlueprint Operand2) => Operand1 == Operand2.Name;
                public static bool operator !=(EntityBlueprint Operand1, GameObjectBlueprint Operand2) => !(Operand1 == Operand2);
            }
        }
    }
}
