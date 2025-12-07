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

namespace UD_FleshGolems.Parts.VengeanceHelpers
{
    [Serializable]
    public class KillerDetails : IComposite
    {
        public string ID;
        public string Blueprint;
        public string DisplayName;
        public string CreatureType;

        public string NotableFeature;

        public string WithWeapon;
        public string DeathDescription;

        public bool WasAccident;
        public bool WasEnvironment;

        public bool KillerIsDeceased;

        public KillerDetails()
        {
            ID = null;
            Blueprint = null;
            DisplayName = null;
            CreatureType = null;

            NotableFeature = null;

            WithWeapon = null;
            DeathDescription = null;

            WasAccident = false;
            WasEnvironment = false;

            KillerIsDeceased = false;
        }
        public KillerDetails(
            string ID,
            string Blueprint,
            string DisplayName,
            string CreatureType,
            string NotableFeature,
            string WithWeapon,
            string DeathDescription,
            bool WasAccident,
            bool WasEnvironment,
            bool KillerIsDeceased = false)
            : this()
        {
            this.ID = ID;
            this.Blueprint = Blueprint;
            this.DisplayName = DisplayName;
            this.CreatureType = CreatureType;
            this.NotableFeature = NotableFeature;
            this.WithWeapon = WithWeapon;
            this.DeathDescription = DeathDescription;
            this.WasAccident = WasAccident;
            this.WasEnvironment = WasEnvironment;
            this.KillerIsDeceased = KillerIsDeceased;
        }
        public KillerDetails(
            GameObject Killer,
            GameObject Weapon,
            string DeathDescription,
            bool WasAccident,
            bool WasEnvironment)
            : this(
                  ID: Killer?.ID,
                  Blueprint: Killer?.Blueprint,
                  DisplayName: Killer?.GetReferenceDisplayName(Short: true),
                  CreatureType: Killer?.GetCreatureType(),
                  NotableFeature: GetNotableFeature(Killer),
                  WithWeapon: Weapon?.GetReferenceDisplayName(Short: true),
                  DeathDescription: DeathDescription,
                  WasAccident: WasAccident,
                  WasEnvironment: WasEnvironment)
        { }
        public KillerDetails(IDeathEvent E)
            : this(
                  Killer: E?.Killer,
                  Weapon: E?.Weapon,
                  DeathDescription: E?.Reason,
                  WasAccident: E == null || E.Accidental,
                  WasEnvironment: E != null && (E?.Killer == null || !E.Killer.IsCreature) && E.Accidental)
        { }

        public static bool HasDefaultBehaviorOrNaturalWeaponEquipped(BodyPart BodyPart)
            => BodyPart != null
            && ((BodyPart.Equipped is GameObject equipped
                    && equipped.IsNatural())
                || BodyPart.DefaultBehavior != null);

        protected static string GetNotableFeature(GameObject Killer)
        {
            if (Killer == null || Killer.Body is not Body killerBody)
                return "a striking absence";

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
                    notableFeatures.TryAdd(implant.An(Short: true, Reference: true), cyberneticsWeight);
                }
            }
            int naturalWeaponWeight = cyberneticsWeight / cyberneticsCount;
            foreach (BodyPart bodyPart in killerBody.LoopParts(HasDefaultBehaviorOrNaturalWeaponEquipped))
            {
                if (bodyPart.Equipped is GameObject equipped
                    && equipped.IsNatural()
                    && equipped.TryGetPart(out MeleeWeapon equippedMeleeWeapon)
                    && !equippedMeleeWeapon.IsImprovisedWeapon())
                    notableFeatures.TryAdd(equipped.An(Short: true, Reference: true), naturalWeaponWeight);
                else
                if (bodyPart.DefaultBehavior is GameObject defaultBehavior
                    && defaultBehavior.TryGetPart(out MeleeWeapon defaultMeleeWeapon)
                    && !defaultMeleeWeapon.IsImprovisedWeapon())
                    notableFeatures.TryAdd(defaultBehavior.An(Short: true, Reference: true), naturalWeaponWeight);
            }

            return notableFeatures.GetWeightedRandom();
        }

        public bool IsMystery()
            => ID.IsNullOrEmpty()
            && Blueprint.IsNullOrEmpty()
            && DisplayName.IsNullOrEmpty()
            && CreatureType.IsNullOrEmpty()
            && WithWeapon.IsNullOrEmpty()
            && DeathDescription.IsNullOrEmpty();

        public bool IsPartialMystery()
            => !IsMystery()
            && ((ID.IsNullOrEmpty()
                    && Blueprint.IsNullOrEmpty()
                    && DisplayName.IsNullOrEmpty()
                    && CreatureType.IsNullOrEmpty())
                || WithWeapon.IsNullOrEmpty()
                || DeathDescription.IsNullOrEmpty()
                );

        public string KilledBy() => DisplayName;

        public string KilledByA() => CreatureType;

        public string KilledWith() => WithWeapon;

        public string KilledHow() => DeathDescription;

        public bool IsKiller(GameObject Entity)
            => Entity != null
            && Entity.ID == ID;
    }
}
