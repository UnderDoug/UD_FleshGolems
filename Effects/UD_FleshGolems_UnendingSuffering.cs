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

using static XRL.World.Parts.UD_FleshGolems_RaggedNaturalWeapon;

using SerializeField = UnityEngine.SerializeField;
using Taxonomy = XRL.World.Parts.UD_FleshGolems_RaggedNaturalWeapon.TaxonomyAdjective;

using UD_FleshGolems;
using static UD_FleshGolems.Utils;
using static UD_FleshGolems.Const;

using UD_FleshGolems.Logging;

namespace XRL.World.Effects
{
    [HasConversationDelegate]
    [Serializable]
    public class UD_FleshGolems_UnendingSuffering : IScribedEffect, ITierInitialized
    {
        public const string ENDLESSLY_SUFFERING = "{{UD_FleshGolems_reanimated|endlessly suffering}}";

        [SerializeField]
        private int _FrameOffset;
        private int FrameOffset => GetFrameOffset();

        [SerializeField]
        private bool? _FlipRenderColors;
        private bool FlipRenderColors => GetFlipRenderColors();

        private List<int> FrameRanges => new()
        {
            15 + FrameOffset,
            25 + FrameOffset,
            45 + FrameOffset,
            55 + FrameOffset,
        };

        public static string MeatSufferColor = "R";
        public static string RobotSufferColor = "W";
        public static string PlantSufferColor = "W";
        public static string FungusSufferColor = "B";

        public static int BASE_SMEAR_CHANCE => 5;
        public static int BASE_SPATTER_CHANCE => 2;
        public static int GracePeriodTurns => 2;

        private int GracePeriod;

        public GameObject SourceObject;

        public string Damage;

        public int ChanceToDamage;
        public int ChanceToSmear;
        public int ChanceToSpatter;

        public string SufferColor;

        [SerializeField]
        private int CurrentTier;

        [SerializeField]
        private int CumulativeSuffering;

        public UD_FleshGolems_UnendingSuffering()
        {
            _FrameOffset = int.MinValue;
            _FlipRenderColors = null;

            GracePeriod = GracePeriodTurns;

            SourceObject = null;
            Damage = "1";
            ChanceToDamage = 10;
            ChanceToSmear = BASE_SMEAR_CHANCE;
            ChanceToSpatter = BASE_SPATTER_CHANCE;

            DisplayName = ENDLESSLY_SUFFERING;
            Duration = DURATION_INDEFINITE;

            SufferColor = MeatSufferColor;

            CurrentTier = 0;

            CumulativeSuffering = 0;
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

        public UD_FleshGolems_UnendingSuffering(GameObject Source, int Tier, int TimesReanimated = 1)
            : this(Source)
        {
            Initialize(Tier, TimesReanimated);
        }

        public UD_FleshGolems_UnendingSuffering(string Damage, int Duration, GameObject Source, int ChanceToSmear, int ChanceToSpatter)
            : this(Source)
        {
            this.Damage = Damage;
            this.ChanceToSmear = ChanceToSmear;
            this.ChanceToSpatter = ChanceToSpatter;

            this.Duration = Duration;
        }

        public int GetFrameOffset()
        {
            if (_FrameOffset > int.MinValue)
            {
                return _FrameOffset;
            }
            return (Object != null && int.TryParse(Object.ID, out int result))
                ? _FrameOffset = (result % 3) + 1
                : Stat.RollCached("1d3");
        }

        public bool GetFlipRenderColors()
        {
            if (_FlipRenderColors != null)
            {
                return _FlipRenderColors.GetValueOrDefault();
            }
            if (Object != null && int.TryParse(Object.ID, out int result))
            {
                _FlipRenderColors = (result % 2) == 0;
                return _FlipRenderColors.GetValueOrDefault();
            }
            return Stat.RollCached("1d2") == 1;
        }

        public void Initialize(int Tier, int TimesReanimated = 1)
        {
            Tier = Capabilities.Tier.Constrain(Stat.Random(Tier - 1, Tier + 1));

            using Indent indent = new(1);
            Debug.LogMethod(indent, ArgPairs: Debug.Arg(nameof(Tier), Tier));

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
            ChanceToDamage = 3 * (1 + Math.Max(1, Tier));

            ChanceToDamage *= Math.Max(1, TimesReanimated);

            ChanceToSmear *= Tier;
            ChanceToSpatter *= Tier;

            if (CurrentTier > 0 && Tier > CurrentTier)
            {
                WorsenedMessage(Object);
            }
            CurrentTier = Tier;
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
            return ChanceToDamage + "% chance per turn to suffer " + Damage + " damage" + dueToFolly + ".";
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
            using Indent indent = new(1);
            Debug.LogMethod(indent, ArgPairs: Debug.Arg(nameof(Object), Object?.DebugName ?? NULL));

            StatShifter.SetStatShift(
                target: Object,
                statName: "AcidResistance",
                amount: 200,
                true);

            SufferColor = DetermineTaxonomyAdjective(Object) switch
            {
                Taxonomy.Jagged => RobotSufferColor,
                Taxonomy.Fettid => PlantSufferColor,
                Taxonomy.Decayed => FungusSufferColor,
                _ => MeatSufferColor,
            };

            StartMessage(Object);

            Debug.Log("Calling base." + nameof(Apply), Indent: indent[1]);
            return base.Apply(Object);
        }
        public override void Remove(GameObject Object)
        {
            StatShifter.RemoveStatShifts(Object);
            base.Remove(Object);
        }

        public virtual void StartMessage(GameObject Object)
        {
            if (UD_FleshGolems_Reanimated.HasWorldGenerated)
            {
                using Indent indent = new(1);
                Debug.LogMethod(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(UD_FleshGolems_Reanimated.HasWorldGenerated), UD_FleshGolems_Reanimated.HasWorldGenerated),
                    });
                Debug.Arg(nameof(Object), Object?.DebugName ?? NULL).Log(1);
                Debug.Arg(nameof(DisplayNameStripped), DisplayNameStripped).Log(1);

                Object?.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_physicalRupture");
                DidX(Verb: "begin", Extra: DisplayNameStripped, EndMark: "!", ColorAsBadFor: Object);
            }
        }

        public virtual void WorsenedMessage(GameObject Object)
        {
            if (UD_FleshGolems_Reanimated.HasWorldGenerated)
            {
                using Indent indent = new(1);
                Debug.LogMethod(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(UD_FleshGolems_Reanimated.HasWorldGenerated), UD_FleshGolems_Reanimated.HasWorldGenerated),
                    });
                Debug.Arg(nameof(Object), Object?.DebugName ?? NULL).Log(1);
                Debug.Arg(nameof(DisplayNameStripped), DisplayNameStripped).Log(1);

                Object?.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_physicalRupture");
                DidX(Verb: "start", Extra: DisplayNameStripped + " even worse", EndMark: "!", ColorAsBadFor: Object);
            }
        }

        public void Suffer()
        {
            if (Object == null || Object.CurrentCell == null)
            {
                return;
            }

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

                string deathMessage = "=subject.name's= unending suffering... well, ended =subject.objective=."
                    .StartReplace()
                    .AddObject(Object)
                    .ToString();

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

                if (tookDamage)
                {
                    CumulativeSuffering += damage;
                }

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
                || ID == AfterLevelGainedEvent.ID
                || ID == GetDebugInternalsEvent.ID;
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
                E.ApplyColors((firstRange == !FlipRenderColors) ? "&K" : ("&" + SufferColor), ICON_COLOR_PRIORITY);
                return false;
            }
            return true;
        }
        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            E.AddEntry(this, nameof(FrameOffset), FrameOffset);
            E.AddEntry(this, nameof(FlipRenderColors), FlipRenderColors);
            E.AddEntry(this, nameof(SourceObject), SourceObject?.DebugName ?? NULL);
            E.AddEntry(this, nameof(Damage), Damage);
            E.AddEntry(this, nameof(CumulativeSuffering), CumulativeSuffering);
            E.AddEntry(this, nameof(ChanceToDamage), ChanceToDamage);
            E.AddEntry(this, nameof(ChanceToSmear), ChanceToSmear);
            E.AddEntry(this, nameof(ChanceToSpatter), ChanceToSpatter);
            E.AddEntry(this, nameof(SufferColor), SufferColor);
            E.AddEntry(this, nameof(CurrentTier), CurrentTier);
            return base.HandleEvent(E);
        }
    }
}
