using System;
using System.Collections.Generic;
using System.Text;

using XRL;
using XRL.World;
using XRL.World.Capabilities;
using XRL.World.Parts;
using XRL.Rules;

using SerializeField = UnityEngine.SerializeField;
using XRL.Core;
using XRL.World.Conversations;

namespace XRL.World.Effects
{
    [HasConversationDelegate]
    [Serializable]
    public class UD_FleshGolems_UnendingSuffering : IScribedEffect, ITierInitialized
    {
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

        public string Damage;

        public int ChanceToSmear;
        public int ChanceToSplatter;

        public UD_FleshGolems_UnendingSuffering()
        {
            SourceObject = null;
            Damage = "1";
            ChanceToSmear = 75;
            ChanceToSplatter = 10;

            DisplayName = "{{UD_FleshGolem_reanimated|endlessly suffering}}";
            Duration = 1;
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
            SourceObject = Source;
            Initialize(Tier);
        }

        public UD_FleshGolems_UnendingSuffering(string Damage, int Duration, GameObject Source, int ChanceToSmear, int ChanceToSplatter)
            : this(Source)
        {
            SourceObject = Source;
            this.Damage = Damage;
            this.ChanceToSmear = ChanceToSmear;
            this.ChanceToSplatter = ChanceToSplatter;

            DisplayName = "{{UD_FleshGolem_reanimated|endlessly suffering}}";
            this.Duration = Duration;
        }

        public void Initialize(int Tier)
        {
            Tier = Capabilities.Tier.Constrain(Stat.Random(Tier - 1, Tier + 1));

            if (Tier >= 7)
            {
                Damage = "3-4";
                ChanceToSmear = 75;
                ChanceToSplatter = 30;
            }
            else if (Tier >= 5)
            {
                Damage = "2-3";
                ChanceToSmear = 70;
                ChanceToSplatter = 25;
            }
            else if (Tier >= 3)
            {
                Damage = "1-2";
                ChanceToSmear = 60;
                ChanceToSplatter = 20;
            }
            else if (Tier >= 1)
            {
                Damage = "1d3-2";
                ChanceToSmear = 50;
                ChanceToSplatter = 10;
            }
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
            return "Bleeding Unavoidable";
        }
        public virtual string DamageMessage()
        {
            return "from " + DisplayNameStripped + ".";
        }

        public override bool Apply(GameObject Object)
        {
            if (!Object.FireEvent(Event.New("Apply" + ClassName, "Effect", this)))
            {
                return false;
            }
            if (!ApplyEffectEvent.Check(Object, ClassName, this))
            {
                return false;
            }

            StartMessage(Object);
            StatShifter.SetStatShift("AcidResistance", 100);
            return base.Apply(Object);
        }
        public override void Remove(GameObject Object)
        {
            StatShifter.RemoveStatShifts();
            base.Remove(Object);
        }

        public virtual void StartMessage(GameObject Object)
        {
            Object?.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_physicalRupture");
            DidX(Verb: "begin", Extra: DisplayNameStripped, EndMark: "!", ColorAsBadFor: Object);
        }

        public void Suffer()
        {
            string deathMessage = "=subject.name's= unending suffering... well, ended =subject.objective=."
                .StartReplace()
                .AddObject(Object)
                .ToString();

            if (50.in100())
            {
                Object.TakeDamage(
                    Amount: Damage.RollCached(),
                    Attributes: DamageAttributes(),
                    Owner: Object,
                    Message: DamageMessage(),
                    DeathReason: deathMessage,
                    ThirdPersonDeathReason: deathMessage,
                    Source: Object,
                    Indirect: true);
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
            if (!inLiquid && ChanceToSplatter.in100())
            {
                if (GameObject.Create("BloodSplash") is GameObject bloodySplashObject)
                {
                    if (bloodySplashObject.LiquidVolume is LiquidVolume bloodSplashVolume)
                    {
                        bloodSplashVolume.InitialLiquid = bleedLiquid;
                        suferrerCell.AddObject(bloodySplashObject);
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
                || ID == PhysicalContactEvent.ID;
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
            Suffer();
            return base.HandleEvent(E);
        }
        public override bool Render(RenderEvent E)
        {
            _ = Object.Render;
            int currentFrame = XRLCore.CurrentFrame % 120;
            bool firstRange = currentFrame > 15 && currentFrame < 25;
            bool secondRange = currentFrame > 45 && currentFrame < 55;
            if (firstRange || secondRange)
            {
                E.RenderString = "\u0003";
                E.ApplyColors(firstRange ? "&K" : "&R", ICON_COLOR_PRIORITY);
                return false;
            }
            return true;
        }
        public override bool HandleEvent(PhysicalContactEvent E)
        {
            E.Actor.MakeBloody(E.Object.GetBleedLiquid(), Stat.Random(1, 3));
            return base.HandleEvent(E);
        }
    }
}
