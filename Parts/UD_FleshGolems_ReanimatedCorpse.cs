using System;
using System.Collections.Generic;

using XRL.Core;
using XRL.World.Effects;

using static XRL.World.Parts.UD_FleshGolems_DestinedForReanimation;

using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_ReanimatedCorpse : IScribedPart
    {
        public const string REANIMATED_ADJECTIVE = "{{UD_FleshGolem_reanimated|reanimated}}";
        public const int ICON_COLOR_PRIORITY = 100;

        [SerializeField]
        private string SourceID;

        private GameObject _SourceObject;
        public GameObject SourceObject
        {
            get => _SourceObject ??= GameObject.FindByID(SourceID);
            set
            {
                SourceID = value?.ID;
                _SourceObject = value;
            }
        }

        public string BleedLiquid;

        public Dictionary<string, int> BleedLiquidPortions;

        public UD_FleshGolems_ReanimatedCorpse()
        {
            BleedLiquid = null;
            BleedLiquidPortions = null;
        }

        public override void Attach()
        {
            base.Attach();
            AttemptToSuffer();
        }

        public override bool SameAs(IPart p)
        {
            return false;
        }

        public static bool TryGetLiquidPortion(string LiquidComponent, out (string Liquid, int Portion) LiquidPortion)
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
        public static Dictionary<string, int> GetBleedLiquids(string BleedLiquids)
        {
            if (BleedLiquids.IsNullOrEmpty())
            {
                return new();
            }
            Dictionary<string, int> liquids = new();
            if (!BleedLiquids.Contains(',') && TryGetLiquidPortion(BleedLiquids, out (string Liquid, int Portion) singleLiquidPortion))
            {
                liquids.Add(singleLiquidPortion.Liquid, singleLiquidPortion.Portion);
                return liquids;
            }
            foreach (string liquidComponent in BleedLiquids.Split(','))
            {
                if (TryGetLiquidPortion(liquidComponent, out (string Liquid, int Portion) liquidPortion))
                {
                    liquids.Add(liquidPortion.Liquid, liquidPortion.Portion);
                }
            }
            return liquids;
        }

        public bool AttemptToSuffer()
        {
            if (ParentObject is GameObject frankenCorpse)
            {
                if (!frankenCorpse.TryGetEffect(out UD_FleshGolems_UnendingSuffering unendingSuffering))
                {
                    return frankenCorpse.ForceApplyEffect(new UD_FleshGolems_UnendingSuffering(SourceObject, frankenCorpse.GetTier()));
                }
                else
                if (unendingSuffering.SourceObject == null && SourceObject != null)
                {
                    unendingSuffering.SourceObject = SourceObject;
                }
            }
            return false;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("Regenera");
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || ID == GetDisplayNameEvent.ID
                || ID == EndTurnEvent.ID
                || ID == GetBleedLiquidEvent.ID
                || ID == BeforeDeathRemovalEvent.ID;
        }
        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if (int.TryParse(E.Object.GetPropertyOrTag("UD_FleshGolems_NoReanimatedNamePrefix", "0"), out int NoReanimatedNamePrefix)
                && NoReanimatedNamePrefix < 1)
            {
                E.AddAdjective(REANIMATED_ADJECTIVE, 5);
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
                List<(string Liquid, int Portion)> contamination = new()
                {
                    ("putrid", combinedFactor * 6),
                    ("gel", combinedFactor * 3),
                    ("acid", combinedFactor * 1),
                };
                foreach ((string liquid, int portion) in contamination)
                {
                    if (BleedLiquidPortions.ContainsKey(liquid))
                    {
                        BleedLiquidPortions[liquid] += portion;
                    }
                    else
                    {
                        BleedLiquidPortions.Add(liquid, portion);
                    }
                }
            }
            if (BleedLiquid.IsNullOrEmpty())
            {
                LiquidVolume bleedLiquidVolume = new(BleedLiquidPortions);
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
            if (ParentObject is GameObject dying
                && dying == E.Dying
                && IsDyingCreatureCorpse(dying, out GameObject corpseObject)
                && corpseObject.TryGetPart(out UD_FleshGolems_CoprseReanimationHelper reanimationHelper))
            {
                corpseObject.SetStringProperty("UD_FleshGolems_OriginalCreatureName", reanimationHelper.CreatureName);
                corpseObject.SetStringProperty("UD_FleshGolems_OriginalSourceBlueprint", reanimationHelper.SourceBlueprint);
                corpseObject.SetStringProperty("UD_FleshGolems_CorpseDescription", reanimationHelper.SourceBlueprint);
            }
            return base.HandleEvent(E);
        }
        public override bool Render(RenderEvent E)
        {
            if (ParentObject.IsPlayerLed())
            {
                _ = ParentObject.Render;
                E.ApplyColors("&r", "w", ICON_COLOR_PRIORITY, ICON_COLOR_PRIORITY);
            }
            return base.Render(E);
        }
    }
}