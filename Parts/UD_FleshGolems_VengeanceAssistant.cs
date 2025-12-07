using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using Genkit;

using XRL.Core;
using XRL.Language;
using XRL.Rules;
using XRL.World.Anatomy;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.ObjectBuilders;
using XRL.World.Quests.GolemQuest;
using XRL.World.Effects;
using XRL.World.Skills;
using XRL.World.AI;

using static XRL.World.Parts.UD_FleshGolems_PastLife;

using NanoNecroAnimation = XRL.World.Parts.Mutation.UD_FleshGolems_NanoNecroAnimation;
using RaggedNaturalWeapon = XRL.World.Parts.UD_FleshGolems_RaggedNaturalWeapon;
using Taxonomy = XRL.World.Parts.UD_FleshGolems_RaggedNaturalWeapon.TaxonomyAdjective;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Parts.PastLifeHelpers;
using UD_FleshGolems.Parts.VengeanceHelpers;
using UD_FleshGolems.Events;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_VengeanceAssistant : IScribedPart
    {
        public override bool AllowStaticRegistration()
            => true;

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == OnDeathRemovalEvent.ID;

        public bool IsCorpseOfThisEntity(GameObject Object)
            => Object != null
            && Object.IsCorpse()
            && Object.GetStringProperty("SourceID") == ParentObject?.ID;

        public override bool HandleEvent(OnDeathRemovalEvent E)
        {
            if (ParentObject.GetDropInventory() is Inventory dropInventory
                && dropInventory.GetInventoryZone() is Zone deathZone
                && deathZone.Built
                && dropInventory.FindObject(IsCorpseOfThisEntity) is GameObject corpse
                && corpse.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper corpseReanimationHelper))
            {
                corpseReanimationHelper.KillerDetails = new(E);
            }
            return base.HandleEvent(E);
        }
    }
}
