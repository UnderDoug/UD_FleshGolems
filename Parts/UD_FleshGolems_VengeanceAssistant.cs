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

        public bool IsCorpseOfThisEntityOrDying(GameObject Object, IDeathEvent E)
            => Object != null
            && Object.IsCorpse()
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

            if (E.Dying.GetDropInventory() is IInventory dropInventory)
            {
                Debug.CheckYeh(nameof(E.Dying.GetDropInventory), Indent: indent[1]);
                if (dropInventory.GetInventoryZone() is Zone deathZone)
                {
                    Debug.CheckYeh(nameof(dropInventory.GetInventoryZone), Indent: indent[1]);
                    if (deathZone.Built)
                    {
                        Debug.CheckYeh(nameof(deathZone.Built), Indent: indent[1]);
                        if (dropInventory.GetInventoryCell() is Cell dropCell)
                        {
                            Debug.CheckYeh(nameof(dropInventory.GetInventoryCell), Indent: indent[1]);
                            if (dropCell.FindObject(GO => IsCorpseOfThisEntityOrDying(GO, E)) is GameObject corpse)
                            {
                                Debug.CheckYeh(nameof(dropCell.FindObject), Indent: indent[1]);
                                if (corpse.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper corpseReanimationHelper))
                                {
                                    corpseReanimationHelper.KillerDetails = new(E);
                                    Debug.CheckYeh(nameof(KillerDetails), "Got!", Indent: indent[1]);
                                    corpseReanimationHelper.KillerDetails.Log();
                                }
                                else Debug.CheckNah(nameof(UD_FleshGolems_CorpseReanimationHelper), Indent: indent[1]);
                            }
                            else Debug.CheckNah(nameof(dropCell.FindObject), Indent: indent[1]);
                        }
                        else Debug.CheckNah(nameof(dropInventory.GetInventoryCell), Indent: indent[1]);
                    }
                    else Debug.CheckNah(nameof(deathZone.Built), Indent: indent[1]);
                }
                else Debug.CheckNah(nameof(dropInventory.GetInventoryZone), Indent: indent[1]);
            }
            else Debug.CheckNah(nameof(E.Dying.GetDropInventory), Indent: indent[1]);

            return base.HandleEvent(E);
        }
    }
}
