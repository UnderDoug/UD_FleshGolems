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
using static XRL.World.Parts.UD_FleshGolems_ReanimatedCorpse;
using static XRL.World.Parts.Mutation.UD_FleshGolems_NanoNecroAnimation;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Capabilities;
using UD_FleshGolems.Capabilities.Necromancy;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;

using SerializeField = UnityEngine.SerializeField;
using static UD_FleshGolems.Parts.VengeanceHelpers.DeathMemory;

namespace UD_FleshGolems.Parts.VengeanceHelpers
{
    [Serializable]
    public struct KillerDetails : IComposite
    {
        public const string NOTABLE_FEATURE_PROPTAG = "UD_FleshGolems KillerDetails NotableFeature";

        public string ID;
        public string Blueprint;

        public string NotableFeature;
        public string CreatureType;
        public string DisplayName;

        public bool? KillerIsDeceased;

        public KillerDetails(GameObject Killer)
        {
            ID = Killer?.ID;
            Blueprint = Killer?.Blueprint;

            NotableFeature = GetNotableFeature(Killer);

            CreatureType = GetCreatureType(Killer);

            DisplayName = Killer?.GetReferenceDisplayName(Short: true);

            KillerIsDeceased = Killer?.IsDying ?? Killer?.IsInGraveyard();
        }
        public KillerDetails(IDeathEvent E)
            : this(E?.Killer)
        { }

        public readonly string this[DeathMemory DeathMemory]
        {
            get
            {
                if (DeathMemory.RemebersKiller() == null)
                    return null;

                if (DeathMemory.RemebersKillerName())
                    return DisplayName;

                if (DeathMemory.RemebersKillerCreature())
                    return CreatureType;

                if (DeathMemory.RemebersKillerFeature())
                    return NotableFeature;

                return null;
            }
        }

        public static bool HasDefaultBehaviorOrNaturalWeaponEquipped(BodyPart BodyPart)
            => BodyPart != null
            && ((BodyPart.Equipped is GameObject equipped
                    && equipped.IsNatural())
                || BodyPart.DefaultBehavior != null);

        public static string GetNotableFeature(GameObject Killer)
        {
            if (Killer == null)
                return null;

            if (Killer.GetPropertyOrTag(NOTABLE_FEATURE_PROPTAG) is not string notableFeature)
            {
                Dictionary<string, int> notableFeatures = new();

                int mutationCount = 1;
                int highestMutationLevel = 1;
                if (Killer.TryGetPart(out Mutations mutations)
                    && !mutations.ActiveMutationList.IsNullOrEmpty())
                {
                    mutationCount = mutations.ActiveMutationList.Count;
                    foreach (BaseMutation mutation in mutations.ActiveMutationList)
                    {
                        highestMutationLevel = Math.Max(highestMutationLevel, mutation.Level);
                        notableFeatures.TryAdd(mutation.GetDisplayName(false), mutation.Level);
                    }

                }
                int cyberneticsWeight = Math.Max(mutationCount, highestMutationLevel);
                int cyberneticsCount = 1;
                if (Killer.HasInstalledCybernetics())
                {
                    foreach (GameObject implant in Killer.GetInstalledCyberneticsReadonly())
                    {
                        cyberneticsCount++;
                        notableFeatures.TryAdd(IndefiniteArticle(implant.GetReferenceDisplayName(Short: true)), cyberneticsWeight);
                    }
                }
                int naturalWeaponWeight = cyberneticsWeight / cyberneticsCount;
                if (Killer.Body is Body killerBody)
                {
                    foreach (BodyPart bodyPart in killerBody.LoopParts(HasDefaultBehaviorOrNaturalWeaponEquipped))
                    {
                        if (bodyPart.Equipped is GameObject equipped
                            && equipped.IsNatural()
                            && equipped.TryGetPart(out MeleeWeapon equippedMeleeWeapon)
                            && !equippedMeleeWeapon.IsImprovisedWeapon())
                            notableFeatures.TryAdd(IndefiniteArticle(equipped.GetReferenceDisplayName(Short: true)), naturalWeaponWeight);
                        else
                        if (bodyPart.DefaultBehavior is GameObject defaultBehavior
                            && defaultBehavior.TryGetPart(out MeleeWeapon defaultMeleeWeapon)
                            && !defaultMeleeWeapon.IsImprovisedWeapon())
                            notableFeatures.TryAdd(IndefiniteArticle(defaultBehavior.GetReferenceDisplayName(Short: true)), naturalWeaponWeight);
                    }
                }
                notableFeature = notableFeatures.GetWeightedSeededRandom(nameof(GetNotableFeature) + "::" + Killer.ID);
                Killer.SetNotableFeature(notableFeature);
            }
            return "someone with " + notableFeature;
        }

        public static string GetCreatureType(GameObject Killer)
        {
            if (Killer?.GetCreatureType() is string creatureType)
            {
                return (Killer.IsPlural ? "some " : IndefiniteArticle(creatureType)) + creatureType;
            }
            return null;
        }

        public readonly bool Any()
        {
            return !ID.IsNullOrEmpty()
                    || !Blueprint.IsNullOrEmpty()
                    || !NotableFeature.IsNullOrEmpty()
                    || !CreatureType.IsNullOrEmpty()
                    || !DisplayName.IsNullOrEmpty();
        }

        public readonly KillerDetails Log()
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent);
            Debug.Log(nameof(ID), ID ?? NULL, indent[1]);
            Debug.Log(nameof(Blueprint), Blueprint ?? NULL, indent[1]);
            Debug.Log(nameof(DisplayName), DisplayName ?? NULL, indent[1]);
            Debug.Log(nameof(CreatureType), CreatureType ?? NULL, indent[1]);
            Debug.Log(nameof(NotableFeature), NotableFeature ?? NULL, indent[1]);
            Debug.Log(nameof(KillerIsDeceased), KillerIsDeceased?.ToString() ?? NULL, indent[1]);
            return this;
        }

        public readonly StringMap<string> DebugInternals() => new()
        {
            { nameof(ID), ID ?? NULL },
            { nameof(Blueprint), Blueprint ?? NULL },
            { nameof(DisplayName), DisplayName ?? NULL },
            { nameof(CreatureType), CreatureType ?? NULL },
            { nameof(NotableFeature), NotableFeature ?? NULL },
            { nameof(KillerIsDeceased), KillerIsDeceased?.ToString() ?? NULL },
        };
    }
}
