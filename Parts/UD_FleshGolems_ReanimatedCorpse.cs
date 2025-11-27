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
using XRL.Rules;
using Genkit;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_ReanimatedCorpse : IScribedPart
    {
        [Serializable]
        public struct LiquidPortion : IComposite
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
            // These would serve as quick ways to insta-kill a corpse creature, since they consume the corpse without contest. 
            nameof(Food),
            nameof(Butcherable),
            nameof(Harvestable),

            nameof(SizeAdjective),

            // Corpses are non-furniture objects (items), so it's conceivable they might have one of these.
            nameof(ThrownWeapon),
            nameof(MeleeWeapon),
            nameof(MissileWeapon),
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
                using Indent indent = new(1);
                Debug.LogCaller(indent, 
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(_Reanimator), _Reanimator?.DebugName ?? NULL),
                        Debug.Arg(nameof(value), value?.DebugName ?? NULL),
                    });
                if (_Reanimator != value)
                {
                    _Reanimator = value;
                    AttemptToSuffer();
                }
            }
        }

        public UD_FleshGolems_PastLife PastLife => ParentObject?.GetPart<UD_FleshGolems_PastLife>();

        private string _NewDisplayName;
        public string NewDisplayName 
            => _NewDisplayName 
            ??= PastLife?.GenerateDisplayName()
                    ?.Replace("[", "").Replace("]", "");

        private string _NewDescription;
        public string NewDescription => _NewDescription ??= PastLife?.GenerateDescription();

        public string BleedLiquid;

        public List<LiquidPortion> BleedLiquidPortions;

        [SerializeField]
        private bool AlteredRenderDisplayName;

        public UD_FleshGolems_ReanimatedCorpse()
        {
            _Reanimator = null;
            _NewDisplayName = null;
            BleedLiquid = null;
            BleedLiquidPortions = null;
        }

        public override void Attach()
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent, ArgPairs: Debug.Arg(nameof(ParentObject), ParentObject?.DebugName ?? NULL));

            ParentObject?.AddPart(new UD_FleshGolems_CorpseIconColor(ParentObject));
            Debug.Log(nameof(PartsInNeedOfRemovalWhenAnimated), PartsInNeedOfRemovalWhenAnimated?.Count, indent[2]);
            foreach (string partToRemove in PartsInNeedOfRemovalWhenAnimated)
            {
                Debug.YehNah(
                    Message: partToRemove,
                    Good: ParentObject?.RemovePart(partToRemove),
                    Indent: indent[3]);
            }
            Debug.Log(nameof(BleedLiquid), BleedLiquid ?? NULL, indent[2]);
            if (BleedLiquid.IsNullOrEmpty())
            {
                Debug.Log(
                    nameof(GetBleedLiquidEvent) + "." + 
                    nameof(GetBleedLiquidEvent.GetFor), 
                    GetBleedLiquidEvent.GetFor(ParentObject),
                    indent[3]);
            }

            HaltGreaterVoiderLairCreation(ParentObject, Reanimator);

            if (!NewDescription.IsNullOrEmpty()
                && ParentObject.TryGetPart(out Description description))
            {
                description._Short += "\n\n" + NewDescription;
            }
            /*
            if (!NewDisplayName.IsNullOrEmpty()
                && !AlteredRenderDisplayName
                && ParentObject?.Render is Render corpseRender)
            {
                AlteredRenderDisplayName = true;
                corpseRender.DisplayName = NewDisplayName;
            }
            */

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
            using Indent indent = new();
            if (ParentObject is GameObject frankenCorpse)
            {
                if (!frankenCorpse.TryGetEffect(out UD_FleshGolems_UnendingSuffering unendingSuffering))
                {
                    int tier = GetTierFromLevel(frankenCorpse);
                    Debug.LogMethod(indent[1],
                        ArgPairs: new Debug.ArgPair[]
                        {
                            Debug.Arg(nameof(unendingSuffering), unendingSuffering != null),
                            Debug.Arg(nameof(tier), tier),
                        });
                    int timesReanimated = 1;
                    if (ParentObject.TryGetPart(out UD_FleshGolems_PastLife pastLife))
                    {
                        timesReanimated = pastLife.TimesReanimated;
                    }
                    return frankenCorpse.ForceApplyEffect(new UD_FleshGolems_UnendingSuffering(Reanimator, tier, timesReanimated + 1), Reanimator);
                }
                if (unendingSuffering.SourceObject != Reanimator)
                {
                    Debug.LogMethod(indent[1],
                        ArgPairs: new Debug.ArgPair[]
                        {
                            Debug.Arg(nameof(unendingSuffering.SourceObject), unendingSuffering.SourceObject?.DebugName ?? NULL),
                            Debug.Arg(nameof(Reanimator), Reanimator?.DebugName ?? NULL),
                        });
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

        public static void HaltGreaterVoiderLairCreation(GameObject FrankenCorpse, GameObject Reanimator)
        {
            if (FrankenCorpse.TryGetPart(out GreaterVoider greaterVoider))
            {
                greaterVoider.createdLair = true;
                greaterVoider.lairZone = FrankenCorpse?.CurrentZone?.ZoneID;
                Location2D lairOrigin = null;
                if (FrankenCorpse?.CurrentCell is Cell currentCell)
                {
                    lairOrigin = currentCell.Location;
                }
                else
                if (Reanimator?.CurrentCell is Cell reanimatorCell)
                {
                    lairOrigin = reanimatorCell.Location;
                }
                else
                {
                    lairOrigin = new(Stat.RandomCosmetic(0, 79), Stat.RandomCosmetic(0, 24));
                }
                greaterVoider.lairRect = new(lairOrigin.X - 1, lairOrigin.Y - 1, lairOrigin.X + 1, lairOrigin.Y + 1);
            }
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || ID == AfterPlayerBodyChangeEvent.ID
                || ID == GetDisplayNameEvent.ID
                || ID == GetShortDescriptionEvent.ID
                || ID == DecorateDefaultEquipmentEvent.ID
                || ID == EndTurnEvent.ID
                || ID == GetBleedLiquidEvent.ID
                || ID == BeforeTakeActionEvent.ID
                || ID == GetDebugInternalsEvent.ID;
        }
        public override bool HandleEvent(AfterPlayerBodyChangeEvent E)
        {
            if (E.NewBody is GameObject playerCorpse
                && E.OldBody is GameObject playerPastLife)
            {
                if (playerCorpse.Render is Render corpseRender)
                {
                    AlteredRenderDisplayName = true;
                    corpseRender.DisplayName = The.Game.PlayerName;
                    if (playerCorpse.TryGetPart(out UD_FleshGolems_PastLife pastLifePart))
                    {
                        The.Game.PlayerName = pastLifePart.GenerateDisplayName();
                        corpseRender.DisplayName = The.Game.PlayerName;
                    }
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if (!NewDisplayName.IsNullOrEmpty())
            {
                // E.ReplacePrimaryBase(NewDisplayName);
            }
            E.ReplacePrimaryBase(PastLife.GenerateDisplayName());
            if (int.TryParse(E.Object?.GetPropertyOrTag("UD_FleshGolems_NoReanimatedNamePrefix", "0"), out int NoReanimatedNamePrefix)
                && NoReanimatedNamePrefix < 1)
            {
                E.AddAdjective(REANIMATED_ADJECTIVE, 5);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (!NewDescription.IsNullOrEmpty()
                && Utils.HasSpecialIdentity(E.Object))
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
                Debug.LogCaller(
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(DecorateDefaultEquipmentEvent)),
                        Debug.Arg(ParentObject?.DebugName ?? NULL),
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
            if (BleedLiquidPortions == null || BleedLiquid.IsNullOrEmpty())
            {
                Debug.LogMethod(
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(GetBleedLiquidEvent)),
                        Debug.Arg(ParentObject?.DebugName ?? NULL),
                    });
            }
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
        public override bool HandleEvent(BeforeTakeActionEvent E)
        {
            if (ParentObject.TryGetPart(out Stomach undeadStomach))
            {
                undeadStomach.Water = RuleSettings.WATER_MAXIMUM - 1000;
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