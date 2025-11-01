using System;
using System.Collections.Generic;

using XRL.World.Effects;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolem_ReanimatedCorpse : IScribedPart
    {
        public string BleedLiquid;

        public Dictionary<string, int> BleedLiquidPortions;

        public UD_FleshGolem_ReanimatedCorpse()
        {
            BleedLiquid = null;
            BleedLiquidPortions = null;
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

        public bool AttemptToBleed()
        {
            if (ParentObject is GameObject frankenCorpse
                && !frankenCorpse.HasEffect<Bleeding>())
            {
                return frankenCorpse.ForceApplyEffect(new Bleeding( Damage: "1d2", SaveTarget: 999));
            }
            return false;
        }

        public override bool WantTurnTick()
        {
            return true;
        }
        public override void TurnTick(long TimeTick, int Amount)
        {
            base.TurnTick(TimeTick, Amount);
            if (AttemptToBleed())
            {
                if (75.in100())
                {
                    ParentObject?.Bloodsplatter();
                }
                else
                if (50.in100())
                {
                    ParentObject?.BigBloodsplatter(SelfSplatter: false);
                }
            }
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
                || ID == GetBleedLiquidEvent.ID;
        }
        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if (!E.Object.HasProperName && !E.Object.HasTagOrProperty("NoReanimatedNamePrefix"))
            {
                E.AddAdjective("{{UD_FleshGolem_reanimated|reanimated}}", 5);
            }
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
        public override bool FireEvent(Event E)
        {
            if (E.ID == "Regenera")
            {
                AttemptToBleed();
            }
            return base.FireEvent(E);
        }
    }
}