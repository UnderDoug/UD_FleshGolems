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
using DeathMemory = XRL.World.Parts.UD_FleshGolems_ReanimatedCorpse.DeathMemoryElements;

namespace UD_FleshGolems.Parts.VengeanceHelpers
{
    [Serializable]
    public class KillerDetails : IComposite
    {
        public GameObject Killer;
        public string ID;
        public string Blueprint;
        public string DisplayName;
        public string CreatureType;

        public string NotableFeature;

        public GameObject Weapon;
        public string WeaponName;

        public string DeathDescription;

        public bool WasAccident;
        public bool WasEnvironment;

        public bool? KillerIsDeceased;

        public KillerDetails()
        {
            Killer = null;
            ID = null;
            Blueprint = null;
            DisplayName = null;
            CreatureType = null;

            NotableFeature = null;

            Weapon = null;
            WeaponName = null;

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
            string WeaponName,
            string DeathDescription,
            bool WasAccident,
            bool? WasEnvironment = null,
            bool? KillerIsDeceased = null)
            : this()
        {
            this.ID = ID;
            this.Blueprint = Blueprint;
            this.DisplayName = DisplayName;
            this.CreatureType = CreatureType;
            this.NotableFeature = NotableFeature;
            this.WeaponName = WeaponName;
            this.DeathDescription = DeathDescription;
            this.WasAccident = WasAccident;
            if (WasEnvironment.HasValue)
            {
                this.WasEnvironment = WasEnvironment.GetValueOrDefault();
            }
            else
            {
                this.WasEnvironment = ID.IsNullOrEmpty()
                    && Blueprint.IsNullOrEmpty()
                    && DisplayName.IsNullOrEmpty()
                    && CreatureType.IsNullOrEmpty()
                    && WeaponName.IsNullOrEmpty();
            }
            this.KillerIsDeceased = KillerIsDeceased;
        }
        public KillerDetails(
            GameObject Killer,
            GameObject Weapon,
            string DeathDescription,
            bool WasAccident,
            bool? WasEnvironment = null)
            : this(
                  ID: Killer?.ID,
                  Blueprint: Killer?.Blueprint,
                  DisplayName: Killer?.GetReferenceDisplayName(Short: true),
                  CreatureType: Killer?.GetCreatureType(),
                  NotableFeature: GetNotableFeature(Killer),
                  WeaponName: Weapon?.GetReferenceDisplayName(Short: true),
                  DeathDescription: DeathDescription,
                  WasAccident: WasAccident,
                  WasEnvironment: WasEnvironment)
        {
            this.Killer = Killer;
            this.Weapon = Weapon;
            if (Killer != null && !Killer.IsNowhere())
            {
                KillerIsDeceased = Killer.IsDying;
            }
        }
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

        public static string GetNotableFeature(GameObject Killer)
        {
            if (Killer == null || Killer.Body is not Body killerBody)
                return "striking absence";

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
                    notableFeatures.TryAdd(implant.GetReferenceDisplayName(Short: true), cyberneticsWeight);
                }
            }
            int naturalWeaponWeight = cyberneticsWeight / cyberneticsCount;
            foreach (BodyPart bodyPart in killerBody.LoopParts(HasDefaultBehaviorOrNaturalWeaponEquipped))
            {
                if (bodyPart.Equipped is GameObject equipped
                    && equipped.IsNatural()
                    && equipped.TryGetPart(out MeleeWeapon equippedMeleeWeapon)
                    && !equippedMeleeWeapon.IsImprovisedWeapon())
                    notableFeatures.TryAdd(equipped.GetReferenceDisplayName(Short: true), naturalWeaponWeight);
                else
                if (bodyPart.DefaultBehavior is GameObject defaultBehavior
                    && defaultBehavior.TryGetPart(out MeleeWeapon defaultMeleeWeapon)
                    && !defaultMeleeWeapon.IsImprovisedWeapon())
                    notableFeatures.TryAdd(defaultBehavior.GetReferenceDisplayName(Short: true), naturalWeaponWeight);
            }
            return notableFeatures.GetWeightedSeededRandom(nameof(GetNotableFeature) + "::" + Killer.ID);
        }

        public bool IsMystery()
            => ID.IsNullOrEmpty()
            && Blueprint.IsNullOrEmpty()
            && DisplayName.IsNullOrEmpty()
            && CreatureType.IsNullOrEmpty()
            && WeaponName.IsNullOrEmpty()
            && DeathDescription.IsNullOrEmpty();

        public bool IsPartialMystery()
            => !IsMystery()
            && ((ID.IsNullOrEmpty()
                    && Blueprint.IsNullOrEmpty()
                    && DisplayName.IsNullOrEmpty()
                    && CreatureType.IsNullOrEmpty())
                || WeaponName.IsNullOrEmpty()
                || DeathDescription.IsNullOrEmpty()
                );

        public string KilledBy(DeathMemory DeathMemory, bool Capitalize = false)
        {
            string output = "mysterious entity";
            if (DeathMemory.HasFlag(DeathMemory.KillerName))
            {
                if (!DisplayName.IsNullOrEmpty())
                    output = DisplayName;
                else
                if (Blueprint.GetGameObjectBlueprint() is GameObjectBlueprint killerModel
                    && killerModel.GetxTag("Grammer", "Proper") is string hasProperName
                    && hasProperName.EqualsNoCase("true"))
                    output = killerModel.DisplayName();
            }
            else
            if (DeathMemory.HasFlag(DeathMemory.KillerCreature))
            {
                if (!CreatureType.IsNullOrEmpty())
                    output = CreatureType;
                else
                if (Blueprint.GetGameObjectBlueprint() is GameObjectBlueprint killerModel
                    && (killerModel.GetxTag("Grammer", "Proper") is not string hasProperName
                        || !hasProperName.EqualsNoCase("true")))
                    output = killerModel.DisplayName().ToLower();
            }
            return Capitalize ? output.Capitalize() : output;
        }

        public string KilledByA(DeathMemory DeathMemory, bool Capitalize = false)
        {
            string output = "a mysterious entity";
            if (DeathMemory.HasFlag(DeathMemory.KillerCreature))
            {
                output = Grammar.A(KilledBy(DeathMemory.KillerCreature));
            }
            return Capitalize ? output.Capitalize() : output;
        }

        public void KilledHow(DeathMemory DeathMemory, out string Weapon, out string Feature, out string Description, out bool Accidentally, out bool Environment)
        {
            Weapon = null;
            Feature = null;
            Description = null;
            Accidentally = WasAccident;
            Environment = WasEnvironment;

            if (DeathMemory.HasFlag(DeathMemory.Weapon)
                && !WeaponName.IsNullOrEmpty())
            {
                Weapon = WeaponName;
            }
            if (DeathMemory.HasFlag(DeathMemory.Feature)
                && !NotableFeature.IsNullOrEmpty())
            {
                Feature = NotableFeature;
            }
            if (DeathMemory.HasFlag(DeathMemory.Description)
                && !Description.IsNullOrEmpty())
            {
                Description = DeathDescription;
            }
        }

        public bool IsKiller(GameObject Entity)
            => Entity != null
            && Entity.ID == ID;

        public KillerDetails Log()
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent);
            Debug.Log(nameof(Killer), Killer?.DebugName ?? NULL, indent[1]);
            Debug.Log(nameof(ID), ID ?? NULL, indent[1]);
            Debug.Log(nameof(Blueprint), Blueprint ?? NULL, indent[1]);
            Debug.Log(nameof(DisplayName), DisplayName ?? NULL, indent[1]);
            Debug.Log(nameof(CreatureType), CreatureType ?? NULL, indent[1]);
            Debug.Log(nameof(NotableFeature), NotableFeature ?? NULL, indent[1]);
            Debug.Log(nameof(Weapon), Weapon?.DebugName ?? NULL, indent[1]);
            Debug.Log(nameof(WeaponName), WeaponName ?? NULL, indent[1]);
            Debug.Log(nameof(DeathDescription), DeathDescription ?? NULL, indent[1]);
            Debug.Log(nameof(WasAccident), WasAccident, indent[1]);
            Debug.Log(nameof(WasEnvironment), WasEnvironment, indent[1]);
            Debug.Log(nameof(KillerIsDeceased), KillerIsDeceased?.ToString() ?? NULL, indent[1]);
            return this;
        }
    }
}
