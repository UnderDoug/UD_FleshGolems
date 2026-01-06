using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using Genkit;
using Qud.API;

using XRL;
using XRL.UI;
using XRL.Wish;
using XRL.Rules;
using XRL.Language;
using XRL.Collections;
using XRL.World;
using XRL.World.AI;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.Anatomy;
using XRL.World.ObjectBuilders;
using XRL.World.Effects;
using XRL.World.Capabilities;

using static XRL.World.Parts.UD_FleshGolems_CorpseReanimationHelper;
using static XRL.World.Parts.Mutation.UD_FleshGolems_NanoNecroAnimation;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Capabilities;
using UD_FleshGolems.Capabilities.Necromancy;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using SerializeField = UnityEngine.SerializeField;
using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;

namespace UD_FleshGolems.Parts.PastLifeHelpers
{
    [Serializable]
    public struct MutationData : IComposite
    {
        public string Name;
        public string Variant;
        public int BaseLevel;
        public int CapOverride;
        public int RapidLevel;

        public MutationData(BaseMutation BaseMutation, int RapidLevel)
            : this()
        {
            Name = BaseMutation.GetMutationEntry().Name;
            Variant = BaseMutation.Variant;
            BaseLevel = BaseMutation.BaseLevel;
            CapOverride = BaseMutation.CapOverride;
            this.RapidLevel = RapidLevel;
        }
        public override readonly string ToString()
            => Name + "/" + (Variant ?? "base") + ": (" + BaseLevel + "|" + CapOverride + "|" + RapidLevel + ")";

        public bool GiveToEntity(GameObject Entity)
        {
            if (Entity == null
                || MutationFactory.GetMutationEntryByName(Name) is not MutationEntry entry)
            {
                return false;
            }
            var entityMutations = Entity.RequirePart<Mutations>();
            if (!entityMutations.HasMutation(entry.Class))
            {
                entityMutations.AddMutation(entry.Class, Variant);
            }
            if (entityMutations.GetMutation(entry.Class) is not BaseMutation baseMutation)
            {
                return false;
            }
            baseMutation.ChangeLevel(BaseLevel);
            baseMutation.CapOverride = CapOverride;
            baseMutation.SetRapidLevelAmount(RapidLevel);
            return true;
        }
    }
}
