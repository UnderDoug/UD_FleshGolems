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
            && Object.IsInanimateCorpse()
            && Object.GetStringProperty("SourceID") == ParentObject?.ID;

        public static bool IsCorpseOfThisEntityOrDying(GameObject Entity, GameObject Object, IDeathEvent E)
            => Object != null
            && Object.IsInanimateCorpse()
            && Object.GetStringProperty("SourceID") is string sourceID
            && (sourceID == Entity?.ID
                || sourceID == E.Dying?.ID);

        public bool IsCorpseOfThisEntityOrDying(GameObject Object, IDeathEvent E)
            => Object != null
            && Object.IsInanimateCorpse()
            && Object.GetStringProperty("SourceID") is string sourceID
            && (sourceID == ParentObject?.ID
                || sourceID == E.Dying?.ID);

        public override bool HandleEvent(OnDeathRemovalEvent E)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(OnDeathRemovalEvent)),
                    Debug.Arg(nameof(E.Dying), E.Dying?.DebugName ?? NULL),
                    Debug.Arg(nameof(ParentObject), ParentObject?.DebugName ?? NULL),
                });

            if (E.Dying.GetDropInventory() is IInventory dropInventory
                && dropInventory.GetInventoryZone() is Zone deathZone
                && deathZone.Built
                && dropInventory.GetInventoryCell() is Cell dropCell
                && dropCell.FindObject(GO => IsCorpseOfThisEntityOrDying(GO, E)) is GameObject corpse
                && corpse.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper corpseReanimationHelper))
            {
                if (!corpse.TryGetDeathDetails(out UD_FleshGolems_DeathDetails deathDetails))
                {
                    deathDetails = corpse.RequirePart<UD_FleshGolems_DeathDetails>();
                }
                deathDetails.Initialize(E);
                Debug.CheckYeh(nameof(deathDetails), "Got!", Indent: indent[1]);
                corpseReanimationHelper.KillerDetails?.Log();
            }

            return base.HandleEvent(E);
        }
    }
}
