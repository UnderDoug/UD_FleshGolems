using System;

using XRL.World;

namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        public partial class CorpseSheet : IComposite
        {
            public abstract partial class BlueprintWrapper
                : IComposite
                , IEquatable<BlueprintWrapper>
                , IEquatable<string>
                , IEquatable<GameObjectBlueprint>
            {
                public string Blueprint;

                public BlueprintWrapper()
                {
                    Blueprint = null;
                }
                public BlueprintWrapper(string Blueprint)
                    : this()
                {
                    this.Blueprint = Blueprint;
                }
                public BlueprintWrapper(GameObjectBlueprint Blueprint)
                    : this(Blueprint.Name)
                {
                }
                public BlueprintWrapper(BlueprintWrapper Source)
                    : this(Source.Blueprint)
                {
                }

                public GameObjectBlueprint GetGameObjectBlueprint()
                {
                    return Blueprint.GetGameObjectBlueprint();
                }

                public override string ToString()
                {
                    return Blueprint;
                }

                public void Deconstruct(out GameObjectBlueprint Blueprint)
                {
                    Blueprint = GetGameObjectBlueprint();
                }

                public static explicit operator string(BlueprintWrapper Operand) => Operand.Blueprint;
                public static explicit operator GameObjectBlueprint(BlueprintWrapper Operand) => Operand.GetGameObjectBlueprint();

                // Equality
                public override bool Equals(object obj)
                {
                    if (obj == null)
                    {
                        return false;
                    }
                    if (obj is BlueprintWrapper blueprintWrapperObj)
                    {
                        return Equals(blueprintWrapperObj);
                    }
                    if (obj is string stringObj)
                    {
                        return Equals(stringObj);
                    }
                    if (obj is GameObjectBlueprint gameObjectBlueprintObj)
                    {
                        return Equals(gameObjectBlueprintObj);
                    }
                    return base.Equals(obj);
                }

                public override int GetHashCode()
                {
                    return Blueprint.GetHashCode();
                }

                public virtual bool Equals(BlueprintWrapper other)
                {
                    return Blueprint.Equals(other.Blueprint)
                        && GetType() == other.GetType();
                }

                public bool Equals(string other)
                {
                    return Blueprint.Equals(other);
                }

                public bool Equals(GameObjectBlueprint other)
                {
                    return GetGameObjectBlueprint() == other;
                }

                public static bool operator ==(BlueprintWrapper Operand1, BlueprintWrapper Operand2) => Operand1.Equals(Operand2);
                public static bool operator !=(BlueprintWrapper Operand1, BlueprintWrapper Operand2) => !(Operand1 == Operand2);

                public static bool operator ==(BlueprintWrapper Operand1, string Operand2) => Operand1.Blueprint == Operand2;
                public static bool operator !=(BlueprintWrapper Operand1, string Operand2) => !(Operand1 == Operand2);

                public static bool operator ==(BlueprintWrapper Operand1, GameObjectBlueprint Operand2) => Operand1 == Operand2.Name;
                public static bool operator !=(BlueprintWrapper Operand1, GameObjectBlueprint Operand2) => !(Operand1 == Operand2);
            }
        }
    }
}
