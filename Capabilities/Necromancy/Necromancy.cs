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
using XRL;
using XRL.Collections;
using XRL.World;
using XRL.World.Parts;
using XRL.World.AI;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.Anatomy;
using XRL.World.ObjectBuilders;

using static XRL.World.Parts.UD_FleshGolems_CorpseReanimationHelper;
using static XRL.World.Parts.Mutation.UD_FleshGolems_NanoNecroAnimation;

using UD_FleshGolems;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using SerializeField = UnityEngine.SerializeField;

using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;
using Relationship = UD_FleshGolems.Capabilities.Necromancy.CorpseSheet.CorpseEntityPair.PairRelationship;

namespace UD_FleshGolems.Capabilities
{
    public static partial class Necromancy
    {
        public static UD_FleshGolems_NecromancySystem System => UD_FleshGolems_NecromancySystem.System;

        [Serializable] public partial class CorpseSheet : IComposite { }

        
    }
}
