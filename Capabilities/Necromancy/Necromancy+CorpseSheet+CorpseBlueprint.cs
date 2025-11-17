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
            public class CorpseBlueprint : BlueprintWrapper, IComposite, IEquatable<CorpseBlueprint>
            {
                public CorpseBlueprint() : base() { }
                public CorpseBlueprint(string Blueprint) : base(Blueprint) { }
                public CorpseBlueprint(GameObjectBlueprint Blueprint) : base(Blueprint) { }
                public CorpseBlueprint(BlueprintWrapper Source) : base(Source.Blueprint) { }
                public CorpseBlueprint(CorpseBlueprint Source) : base(Source) { }

                public static explicit operator CorpseBlueprint(string Operand) => new(Operand);
                public static explicit operator CorpseBlueprint(GameObjectBlueprint Operand) => new(Operand);

                // Equality
                public override bool Equals(object obj)
                {
                    return base.Equals(obj);
                }

                public override int GetHashCode()
                {
                    return base.GetHashCode();
                }

                public override bool Equals(BlueprintWrapper other)
                {
                    return other is CorpseBlueprint otherEntity 
                        && Equals(otherEntity);
                }

                public bool Equals(CorpseBlueprint other)
                {
                    return Blueprint == other.Blueprint;
                }

                public static bool operator ==(CorpseBlueprint Operand1, EntityBlueprint Operand2) => false;
                public static bool operator !=(CorpseBlueprint Operand1, EntityBlueprint Operand2) => true;

                public static bool operator ==(CorpseBlueprint Operand1, string Operand2) => Operand1.Blueprint == Operand2;
                public static bool operator !=(CorpseBlueprint Operand1, string Operand2) => !(Operand1 == Operand2);

                public static bool operator ==(CorpseBlueprint Operand1, GameObjectBlueprint Operand2) => Operand1 == Operand2.Name;
                public static bool operator !=(CorpseBlueprint Operand1, GameObjectBlueprint Operand2) => !(Operand1 == Operand2);
            }
        }
    }
}
