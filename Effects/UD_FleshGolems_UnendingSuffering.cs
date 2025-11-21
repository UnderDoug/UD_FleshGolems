using System;
using System.Collections.Generic;
using System.Text;

using XRL;
using XRL.Core;
using XRL.Rules;
using XRL.World;
using XRL.World.Parts;
using XRL.World.ObjectBuilders;
using XRL.World.Capabilities;
using XRL.World.Conversations;

using UD_FleshGolems;
using static UD_FleshGolems.Utils;
using static UD_FleshGolems.Const;

using SerializeField = UnityEngine.SerializeField;
using UD_FleshGolems.Logging;

namespace XRL.World.Effects
{
    [HasConversationDelegate]
    [Serializable]
    public class UD_FleshGolems_UnendingSuffering : IScribedEffect, ITierInitialized
    {
        public const string ENDLESSLY_SUFFERING = "{{UD_FleshGolems_reanimated|endlessly suffering}}";

        private int _FrameOffset;
        private int FrameOffset => int.TryParse(Object?.ID, out int result) 
            ? _FrameOffset = (result % 3) + 1 
            : Stat.RollCached("1d3");

        private bool _FlipRenderColors;
        private bool FlipRenderColors => int.TryParse(Object?.ID, out int result)
            ? _FlipRenderColors = (result % 2) == 0
            : Stat.RollCached("1d2") == 1;

        private List<int> FrameRanges => new()
        {
            15 + FrameOffset,
            25 + FrameOffset,
            45 + FrameOffset,
            55 + FrameOffset,
        };

        public static int BASE_SMEAR_CHANCE => 5;
        public static int BASE_SPATTER_CHANCE => 2;
        public static int GracePeriodTurns => 2;

        private int GracePeriod;

        public GameObject SourceObject;

        public string Damage;

        public int ChanceToDamage;
        public int ChanceToSmear;
        public int ChanceToSpatter;

        [SerializeField]
        private int CurrentTier;

        public UD_FleshGolems_UnendingSuffering()
        {
            _FrameOffset = int.MinValue;
            _FlipRenderColors = false;

            GracePeriod = GracePeriodTurns;

            SourceObject = null;
            Damage = "1";
            ChanceToDamage = 10;
            ChanceToSmear = BASE_SMEAR_CHANCE;
            ChanceToSpatter = BASE_SPATTER_CHANCE;

            DisplayName = ENDLESSLY_SUFFERING;
            Duration = 1;

            CurrentTier = 0;
        }

        public UD_FleshGolems_UnendingSuffering(GameObject Source)
            : this()
        {
            SourceObject = Source;
        }

        public UD_FleshGolems_UnendingSuffering(int Tier)
            : this()
        {
            Initialize(Tier);
        }

        public UD_FleshGolems_UnendingSuffering(GameObject Source, int Tier)
            : this(Source)
        {
            Initialize(Tier);
        }

        public UD_FleshGolems_UnendingSuffering(string Damage, int Duration, GameObject Source, int ChanceToSmear, int ChanceToSpatter)
            : this(Source)
        {
            this.Damage = Damage;
            this.ChanceToSmear = ChanceToSmear;
            this.ChanceToSpatter = ChanceToSpatter;

            this.Duration = Duration;
        }

        public void Initialize(int Tier)
        {
            Tier = Capabilities.Tier.Constrain(Stat.Random(Tier - 1, Tier + 1));

            Debug.LogMethod(out _, Debug.LogArg(nameof(Tier), Tier));

            if (Tier >= 7)
            {
                Damage = "3-4";
            }
            else
            if (Tier >= 5)
            {
                Damage = "2-3";
            }
            else
            if (Tier >= 3)
            {
                Damage = "1-2";
            }
            else
            if (Tier >= 1)
            {
                Damage = "1d3-2";
            }
            ChanceToDamage = 10 * (1 + Math.Max(1, Tier));
            ChanceToSmear *= Tier;
            ChanceToSpatter *= Tier;

            if (CurrentTier > 0 && Tier > CurrentTier)
            {
                WorsenedMessage(Object);
            }
            CurrentTier = Tier;

            Debug.DiscardIndent();
        }

        public override int GetEffectType()
        {
            return TYPE_MENTAL | TYPE_STRUCTURAL | TYPE_NEUROLOGICAL;
        }
        public override string GetDetails()
        {
            string dueToFolly = null;
            if (SourceObject != null)
            {
                dueToFolly += " due to the existential folly of " + SourceObject.GetReferenceDisplayName(Short: true, Stripped: true);
            }
            return Damage + " damage per turn" + dueToFolly + ".";
        }
        public virtual string DamageAttributes()
        {
            return "Bleeding Unavoidable Suffering";
        }
        public virtual string DamageMessage()
        {
            return "from " + DisplayNameStripped + ".";
        }

        public override bool Apply(GameObject Object)
        {
            Debug.LogMethod(out Indent indent, Debug.LogArg(nameof(Object), Object?.DebugName ?? NULL));
            if (!Object.FireEvent(Event.New("Apply" + ClassName, "Effect", this)))
            {
                Debug.DiscardIndent();
                return false;
            }
            if (!ApplyEffectEvent.Check(Object, ClassName, this))
            {
                Debug.DiscardIndent();
                return false;
            }

            StatShifter.SetStatShift(
                target: Object,
                statName: "AcidResistance",
                amount: 200,
                true);

            StartMessage(Object);

            Debug.Log("Calling base." + nameof(Apply), indent[1]);
            Debug.DiscardIndent();
            return base.Apply(Object);
        }
        public override void Remove(GameObject Object)
        {
            StatShifter.RemoveStatShifts(Object);
            base.Remove(Object);
        }

        public virtual void StartMessage(GameObject Object)
        {
            Debug.LogMethod(out _, Debug.LogArg(nameof(UD_FleshGolems_Reanimated.HasWorldGenerated), UD_FleshGolems_Reanimated.HasWorldGenerated));
            if (UD_FleshGolems_Reanimated.HasWorldGenerated)
            {
                Object?.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_physicalRupture");
                DidX(Verb: "begin", Extra: DisplayNameStripped, EndMark: "!", ColorAsBadFor: Object);
            }
            Debug.DiscardIndent();
        }

        public virtual void WorsenedMessage(GameObject Object)
        {
            Debug.LogMethod(out _, Debug.LogArg(nameof(UD_FleshGolems_Reanimated.HasWorldGenerated), UD_FleshGolems_Reanimated.HasWorldGenerated));
            if (UD_FleshGolems_Reanimated.HasWorldGenerated)
            {
                Object?.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_physicalRupture");
                DidX(Verb: "start", Extra: DisplayNameStripped + " even worse", EndMark: "!", ColorAsBadFor: Object);
            }
        }

        public void Suffer()
        {
            if (Object == null)
            {
                return;
            }

            string deathMessage = "=subject.name's= unending suffering... well, ended =subject.objective=."
                .StartReplace()
                .AddObject(Object)
                .ToString();

            int chanceToDamage = !Object.CurrentCell.OnWorldMap() ? ChanceToDamage : (int)Math.Max(1, ChanceToDamage * 0.01);
            bool tookDamage = false;
            if (chanceToDamage.in100())
            {
                string oldAutoActSetting = AutoAct.Setting;
                bool isAutoActing = AutoAct.IsActive();

                if (Object.IsPlayerControlled())
                {
                    if (isAutoActing)
                    {
                        AutoAct.Setting = "";
                    }
                }

                int damage = CapDamageTo1HPRemaining(Object, Damage.RollCached());
                tookDamage = Object.TakeDamage(
                    Amount: damage,
                    Attributes: DamageAttributes(),
                    Owner: Object,
                    Message: DamageMessage(),
                    DeathReason: deathMessage,
                    ThirdPersonDeathReason: deathMessage,
                    Source: Object,
                    Indirect: true,
                    SilentIfNoDamage: true);

                if (Object.IsPlayerControlled())
                {
                    if (isAutoActing)
                    {
                        AutoAct.Setting = oldAutoActSetting;
                    }
                }
            }

            if (Object.CurrentCell is not Cell suferrerCell || suferrerCell.OnWorldMap())
            {
                return;
            }
            bool inLiquid = false;
            string bleedLiquid = Object.GetBleedLiquid();
            if (ChanceToSmear.in100())
            {
                foreach (GameObject renderdObject in suferrerCell.GetObjectsWithPartReadonly("Render"))
                {
                    if (renderdObject.LiquidVolume is LiquidVolume liquidVolumeInCell
                        && liquidVolumeInCell.IsOpenVolume())
                    {
                        LiquidVolume bloodVolumeForCell = new()
                        {
                            InitialLiquid = bleedLiquid,
                            Volume = 2
                        };
                        liquidVolumeInCell.MixWith(bloodVolumeForCell, null, null, null);
                        inLiquid = true;
                    }
                    else
                    {
                        renderdObject.MakeBloody(bleedLiquid, Stat.Random(1, 3));
                    }
                }
            }
            if (!inLiquid && ChanceToSpatter.in100())
            {
                if (GameObject.Create("BloodSplash") is GameObject bloodySplashObject)
                {
                    if (bloodySplashObject.LiquidVolume is LiquidVolume bloodSplashVolume)
                    {
                        bloodSplashVolume.InitialLiquid = bleedLiquid;
                        suferrerCell.AddObject(bloodySplashObject);
                        if (tookDamage)
                        {
                            DidX("spatter", "viscous gunk everywhere", "!");
                        }
                    }
                    else
                    {
                        MetricsManager.LogError("generated " + bloodySplashObject.Blueprint + " with no " + nameof(LiquidVolume));
                        bloodySplashObject?.Obliterate();
                    }
                }
            }
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || ID == GetCompanionStatusEvent.ID
                || ID == EndTurnEvent.ID
                || ID == PhysicalContactEvent.ID
                || ID == AfterLevelGainedEvent.ID;
        }
        public override bool HandleEvent(GetCompanionStatusEvent E)
        {
            if (E.Object == Object)
            {
                E.AddStatus("suffering", 20);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            if (GracePeriod < 1)
            {
                Suffer();
            }
            else
            {
                GracePeriod--;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(PhysicalContactEvent E)
        {
            E.Actor.MakeBloody(E.Object.GetBleedLiquid(), Stat.RollCached("2d2-2"));
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AfterLevelGainedEvent E)
        {
            Initialize(UD_FleshGolems_ReanimatedCorpse.GetTierFromLevel(Object));
            return base.HandleEvent(E);
        }
        public override bool Render(RenderEvent E)
        {
            _ = Object.Render;
            int currentFrame = XRLCore.CurrentFrame % 60;

            bool firstRange = currentFrame > FrameRanges[0] && currentFrame < FrameRanges[1];
            bool secondRange = currentFrame > FrameRanges[2] && currentFrame < FrameRanges[3];
            if (firstRange || secondRange)
            {
                E.RenderString = "\u0003";
                E.ApplyColors((firstRange == !FlipRenderColors) ? "&K" : "&R", ICON_COLOR_PRIORITY);
                return false;
            }
            return true;
        }
    }
}
