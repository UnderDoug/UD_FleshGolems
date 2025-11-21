using System;
using System.Collections.Generic;

using XRL.Core;
using XRL.World.Anatomy;
using XRL.World.Effects;

using static XRL.World.Parts.UD_FleshGolems_DestinedForReanimation;
using static XRL.World.Parts.UD_FleshGolems_RaggedNaturalWeapon;

using SerializeField = UnityEngine.SerializeField;
using Taxonomy = XRL.World.Parts.UD_FleshGolems_RaggedNaturalWeapon.TaxonomyAdjective;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using static UD_FleshGolems.Const;
using System.Linq;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_ReanimatedCorpse : IScribedPart
    {
        public struct LiquidPortion
        {
            public string Liquid;
            public int Portion;
            public LiquidPortion(string Liquid, int Portion)
            {
                this.Liquid = Liquid;
                this.Portion = Portion;
            }
            public override readonly string ToString() => Liquid + "-" + Portion;
            public void Deconstruct(out string Liquid, out int Portion)
            {
                Liquid = this.Liquid;
                Portion = this.Portion;
            }
        }

        public const string REANIMATED_ADJECTIVE = "{{UD_FleshGolems_reanimated|reanimated}}";

        public static List<string> PartsInNeedOfRemovalWhenAnimated => new()
        {
            nameof(Food),
            nameof(Butcherable),
            nameof(Harvestable),
        };

        public static List<string> MeatContaminationLiquids = new() { "putrid", "slime", "ooze", };
        public static List<string> RobotContaminationLiquids = new() { "putrid", "gel", "sludge", };
        public static List<string> PlantContaminationLiquids = new() { "putrid", "slime", "goo", };
        public static List<string> FungusContaminationLiquids = new() { "putrid", "slime", "acid", };

        [SerializeField]
        private GameObject _Reanimator;
        public GameObject Reanimator
        {
            get
            {
                GameObject.Validate(ref _Reanimator);
                return _Reanimator;
            }
            set
            {
                _Reanimator = value;
                AttemptToSuffer();
            }
        }

        public UD_FleshGolems_PastLife PastLife => ParentObject?.GetPart<UD_FleshGolems_PastLife>();

        private string _NewDisplayName;
        public string NewDisplayName => _NewDisplayName ??= PastLife?.GenerateDisplayName();

        private string _NewDescription;
        public string NewDescription => _NewDescription ??= PastLife?.GenerateDescription();

        public string BleedLiquid;

        public List<LiquidPortion> BleedLiquidPortions;

        public UD_FleshGolems_ReanimatedCorpse()
        {
            _Reanimator = null;
            _NewDisplayName = null;
            _NewDisplayName = null;
            BleedLiquid = null;
            BleedLiquidPortions = null;
        }

        public override void Attach()
        {
            if (ParentObject.GetBlueprint() is GameObjectBlueprint parentBlueprint)
            {
                ParentObject.AddPart(new UD_FleshGolems_CorpseIconColor(parentBlueprint));
            }
            foreach (string partToRemove in PartsInNeedOfRemovalWhenAnimated)
            {
                ParentObject.RemovePart(partToRemove);
            }
            if (BleedLiquid.IsNullOrEmpty())
            {
                _ = GetBleedLiquidEvent.GetFor(ParentObject);
            }
            AttemptToSuffer();
            base.Attach();
        }

        public override bool SameAs(IPart p)
        {
            return false;
        }

        public static bool TryGetLiquidPortion(string LiquidComponent, out LiquidPortion LiquidPortion)
        {
            LiquidPortion = new("water", 0);
            if (LiquidComponent.Contains('-'))
            {
                string[] liquidComponent = LiquidComponent.Split('-');
                if (int.TryParse(liquidComponent[1], out int portion))
                {
                    LiquidPortion.Liquid = liquidComponent[0];
                    LiquidPortion.Portion = portion;
                    return true;
                }
            }
            return false;
        }
        public static List<LiquidPortion> GetBleedLiquids(string BleedLiquids)
        {
            if (BleedLiquids.IsNullOrEmpty())
            {
                return new();
            }
            List<LiquidPortion> liquids = new();
            if (!BleedLiquids.Contains(',') && TryGetLiquidPortion(BleedLiquids, out LiquidPortion singleLiquidPortion))
            {
                liquids.Add(new(singleLiquidPortion.Liquid, singleLiquidPortion.Portion));
                return liquids;
            }
            foreach (string liquidComponent in BleedLiquids.CachedCommaExpansion())
            {
                if (TryGetLiquidPortion(liquidComponent, out LiquidPortion liquidPortion))
                {
                    liquids.Add(new(liquidPortion.Liquid, liquidPortion.Portion));
                }
            }
            return liquids;
        }

        public static Dictionary<string, int> GetBleedLiquidDict(List<LiquidPortion> LiquidPortionsList)
            => LiquidPortionsList.ToDictionary(lp => lp.Liquid, lp => lp.Portion);

        public static List<LiquidPortion> GetBleedLiquidPortions(Dictionary<string, int> LiquidDict)
            => LiquidDict.ToList().ConvertAll(kvp => new LiquidPortion(kvp.Key, kvp.Value));

        public static int GetTierFromLevel(GameObject Creature)
        {
            return Capabilities.Tier.Constrain((Creature.Stat("Level") - 1) / 5 + 1);
        }
        public int GetTierFromLevel() => GetTierFromLevel(ParentObject);

        public bool AttemptToSuffer()
        {
            if (ParentObject is GameObject frankenCorpse)
            {
                if (!frankenCorpse.TryGetEffect(out UD_FleshGolems_UnendingSuffering unendingSuffering))
                {
                    int tier = GetTierFromLevel(frankenCorpse);
                    return frankenCorpse.ForceApplyEffect(new UD_FleshGolems_UnendingSuffering(Reanimator, tier));
                }
                else
                if (unendingSuffering.SourceObject != Reanimator)
                {
                    unendingSuffering.SourceObject = Reanimator;
                    return true;
                }
            }
            return false;
        }

        private static bool BodyPartHasRaggedNaturalWeapon(BodyPart BodyPart)
            => BodyPart?.DefaultBehavior is GameObject defaultBehavior
            && defaultBehavior.GetBlueprint().InheritsFrom("UD_FleshGolems Ragged Weapon")
            && defaultBehavior.TryGetPart(out UD_FleshGolems_RaggedNaturalWeapon raggedNaturalWeaponPart)
            && raggedNaturalWeaponPart.Wielder == null;

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || ID == GetDisplayNameEvent.ID
                || ID == GetShortDescriptionEvent.ID
                || ID == DecorateDefaultEquipmentEvent.ID
                || ID == EndTurnEvent.ID
                || ID == GetBleedLiquidEvent.ID
                || ID == BeforeDeathRemovalEvent.ID
                || ID == GetDebugInternalsEvent.ID;
        }
        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if (!NewDisplayName.IsNullOrEmpty())
            {
                E.ReplacePrimaryBase(NewDisplayName);
            }
            if (int.TryParse(E.Object.GetPropertyOrTag("UD_FleshGolems_NoReanimatedNamePrefix", "0"), out int NoReanimatedNamePrefix)
                && NoReanimatedNamePrefix < 1)
            {
                E.AddAdjective(REANIMATED_ADJECTIVE, 5);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (!NewDescription.IsNullOrEmpty())
            {
                E.Infix.AppendLine().Append(NewDescription);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(DecorateDefaultEquipmentEvent E)
        {
            if (ParentObject?.Body is Body frankenBody
                && frankenBody == E.Body)
            {
                Debug.LogCaller(null, new Debug.ArgPair[]
                {
                    Debug.LogArg(nameof(DecorateDefaultEquipmentEvent)),
                    Debug.LogArg(ParentObject?.DebugName ?? NULL),
                });
                foreach (BodyPart bodyPart in frankenBody.LoopParts(BodyPartHasRaggedNaturalWeapon))
                {
                    if (bodyPart.DefaultBehavior is GameObject defaultBehavior
                        && defaultBehavior.GetBlueprint().InheritsFrom("UD_FleshGolems Ragged Weapon")
                        && defaultBehavior.TryGetPart(out UD_FleshGolems_RaggedNaturalWeapon raggedNaturalWeaponPart))
                    {
                        raggedNaturalWeaponPart.Wielder = ParentObject;
                    }
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            AttemptToSuffer();
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetBleedLiquidEvent E)
        {
            if (BleedLiquidPortions == null)
            {
                string baseBlood = E.BaseLiquid ?? E.Actor.GetStringProperty("BleedLiquid", "blood-1000");
                BleedLiquidPortions = GetBleedLiquids(baseBlood);
                int combinedPortions = 0;
                foreach ((string _, int portion) in BleedLiquidPortions)
                {
                    combinedPortions += portion;
                }
                if (combinedPortions == 0)
                {
                    combinedPortions = 500;
                }
                int combinedFactor = combinedPortions / 10;

                List<string> contaminationLiquids = DetermineTaxonomyAdjective(E.Actor) switch
                {
                    Taxonomy.Jagged => RobotContaminationLiquids,
                    Taxonomy.Fettid => PlantContaminationLiquids,
                    Taxonomy.Decayed => FungusContaminationLiquids,
                    _ => MeatContaminationLiquids,
                };

                List<LiquidPortion> contamination = new()
                {
                    new(contaminationLiquids[0], (int)Math.Ceiling(combinedFactor * 6.75)),
                    new(contaminationLiquids[1], (int)(combinedFactor * 3.0)),
                    new(contaminationLiquids[2], (int)Math.Floor(combinedFactor * 0.25)),
                };
                Dictionary<string, int> bleedLiquids = GetBleedLiquidDict(BleedLiquidPortions);
                foreach ((string liquid, int portion) in contamination)
                {
                    if (bleedLiquids.ContainsKey(liquid))
                    {
                        bleedLiquids[liquid] += portion;
                    }
                    else
                    {
                        bleedLiquids.Add(liquid, portion);
                    }
                }
                BleedLiquidPortions = GetBleedLiquidPortions(bleedLiquids);
            }
            if (BleedLiquid.IsNullOrEmpty())
            {
                LiquidVolume bleedLiquidVolume = new(GetBleedLiquidDict(BleedLiquidPortions));
                bleedLiquidVolume.NormalizeProportions();
                foreach ((string liquid, int portion) in bleedLiquidVolume.ComponentLiquids)
                {
                    if (!BleedLiquid.IsNullOrEmpty())
                    {
                        BleedLiquid += ",";
                    }

                    BleedLiquid += liquid + "-" + portion;
                }
            }
            E.Liquid = BleedLiquid;
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeDeathRemovalEvent E)
        {
            if (false && ParentObject is GameObject dying
                && dying == E.Dying
                && IsDyingCreatureCorpse(dying, out GameObject corpseObject)
                && corpseObject.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper reanimationHelper))
            {
                corpseObject.SetStringProperty("UD_FleshGolems_OriginalCreatureName", reanimationHelper.CreatureName);
                corpseObject.SetStringProperty("UD_FleshGolems_OriginalSourceBlueprint", reanimationHelper.SourceBlueprint);
                corpseObject.SetStringProperty("UD_FleshGolems_CorpseDescription", reanimationHelper.SourceBlueprint);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            E.AddEntry(this, nameof(Reanimator), Reanimator?.DebugName ?? NULL);
            E.AddEntry(this, nameof(BleedLiquid), BleedLiquid);
            return base.HandleEvent(E);
        }
    }
}