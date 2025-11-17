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
            public abstract class BlueprintWrapper
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
