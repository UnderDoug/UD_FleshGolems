using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Genkit;

using XRL.Core;
using XRL.Rules;
using XRL.World.Anatomy;
using XRL.World.Effects;

using static XRL.World.Parts.UD_FleshGolems_DestinedForReanimation;
using static XRL.World.Parts.UD_FleshGolems_RaggedNaturalWeapon;

using SerializeField = UnityEngine.SerializeField;
using Taxonomy = XRL.World.Parts.UD_FleshGolems_RaggedNaturalWeapon.TaxonomyAdjective;
using IdentityType = XRL.World.Parts.UD_FleshGolems_PastLife.IdentityType;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Events;
using static UD_FleshGolems.Const;
using UD_FleshGolems.Parts.VengeanceHelpers;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_ReanimatedCorpse : IScribedPart, IReanimateEventHandler
    {
        [UD_FleshGolems_DebugRegistry]
        public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
        {
            Registry.Register(AccessTools.Method(typeof(UD_FleshGolems_ReanimatedCorpse), nameof(HandleEvent), new Type[] { typeof(DecorateDefaultEquipmentEvent) }), false);
            return Registry;
        }

        [Serializable]
        private struct LiquidPortion : IComposite
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
        public const string REANIMATED_NO_ADJECTIVE_PROPTAG = "UD_FleshGolems ReanimatedCorpse NoPrefix";
        public const string REANIMATED_USE_ICONCOLOR_PART_PROPTAG = "UD_FleshGolems ReanimatedCorpse UseIconColorPart";

        [Flags]
        public enum DeathMemoryElements : int
        {
            None = 0,
            KillerName = 1,
            KillerCreature = 2,
            Killer = KillerName | KillerCreature,
            Feature = 4,
            Weapon = 8,
            Description = 16,
            Method = Feature | Weapon | Description,
            Complete = Killer ^ Method,
        }
        public static DeathMemoryElements UndefinedDeathMemoryElement => (DeathMemoryElements)(-1);

        private static Dictionary<string, DeathMemoryElements> _DeathMemoryElementsValues;
        public static Dictionary<string, DeathMemoryElements> DeathMemoryElementsValues
            => _DeathMemoryElementsValues ??= Startup.RequireCachedEnumValueDictionary<DeathMemoryElements>();

        public static DeathMemoryElements AllDeathMemoryElements
            => DeathMemoryElementsValues.Aggregate(DeathMemoryElements.None, (accumulated, next) => accumulated | next.Value, s => s);

        private const string LIBRARIAN_FRAGMENT = "In the narthex of the Stilt, cloistered beneath a marble arch and close to";

        public static List<string> PartsInNeedOfRemovalWhenAnimated => new()
        {
            // These would serve as quick ways to insta-kill a corpse creature, since they consume the corpse without contest. 
            nameof(Food),
            nameof(Butcherable),
            nameof(Harvestable),

            nameof(SizeAdjective),

            nameof(RandomTileOnMove),
            nameof(RandomColorsOnMove),

            // Corpses are non-furniture objects (items), so it's conceivable they might have one of these.
            nameof(ThrownWeapon),
            // nameof(MeleeWeapon), // apparently *are* melee weapons...
            nameof(MissileWeapon),
        };

        public static List<string> MeatContaminationLiquids = new() { "putrid", "slime", "ooze", };
        public static List<string> RobotContaminationLiquids = new() { "putrid", "gel", "sludge", };
        public static List<string> PlantContaminationLiquids = new() { "putrid", "slime", "goo", };
        public static List<string> FungusContaminationLiquids = new() { "putrid", "slime", "acid", };

        public static int CorpseAdjective = 49;
        public static int CorpseClause = -588;

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

        public KillerDetails KillerDetails => ParentObject.GetPart<UD_FleshGolems_CorpseReanimationHelper>()?.KillerDetails;

        [SerializeField]
        private DeathMemoryElements _DeathMemory;
        public DeathMemoryElements DeathMemory
        {
            get
            {
                if (_DeathMemory != UndefinedDeathMemoryElement || ParentObject == null)
                {
                    return _DeathMemory;
                }
                DeathMemoryElements value = (DeathMemoryElements)ParentObject.GetSeededRange(nameof(DeathMemory), 1, (int)AllDeathMemoryElements);
                if (ParentObject.BaseID % 6 == 0)
                {
                    value = DeathMemoryElements.None;
                }
                return _DeathMemory = value;
            }
        }

        public bool DeathQuestionsAreRude;

        public UD_FleshGolems_PastLife PastLife => ParentObject?.GetPart<UD_FleshGolems_PastLife>();

        public IdentityType IdentityType => PastLife?.GetIdentityType() ?? IdentityType.None;

        [SerializeField]
        private string _NewDisplayName;
        public string NewDisplayName
        {
            get
            {
                if (!_NewDisplayName.IsNullOrEmpty())
                {
                    return _NewDisplayName;
                }
                IdentityType identityType = IdentityType.None;
                string output = PastLife?.GenerateDisplayName(out identityType);
                if (identityType == IdentityType.None)
                {
                    return output;
                }
                return _NewDisplayName = output;
            }
            set
            {
                _NewDisplayName = value;
            }
        }

        [SerializeField]
        private string _NewDescription;
        public string NewDescription => _NewDescription ??= PastLife?.GeneratePostDescription();

        public string BleedLiquid;

        private List<LiquidPortion> BleedLiquidPortions;

        [SerializeField]
        private bool IsRenderDisplayNameUpdated;

        [SerializeField]
        private bool IsAlteredDescription;

        public bool NoSuffer;

        public UD_FleshGolems_ReanimatedCorpse()
        {
            _Reanimator = null;
            _NewDisplayName = null;
            _DeathMemory = UndefinedDeathMemoryElement;
            DeathQuestionsAreRude = false;
            BleedLiquid = null;
            BleedLiquidPortions = null;
            IsRenderDisplayNameUpdated = false;
            IsAlteredDescription = false;
            NoSuffer = false;
        }

        public override void Attach()
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(ParentObject), ParentObject?.DebugName ?? NULL),
                    Debug.Arg(nameof(IdentityType), IdentityType.ToStringWithNum()),
                    Debug.Arg(nameof(IsAlteredDescription), IsAlteredDescription),
                    Debug.Arg(nameof(NewDescription), !NewDescription.IsNullOrEmpty()),
                });

            if (ParentObject.HasTagOrIntProperty(REANIMATED_USE_ICONCOLOR_PART_PROPTAG))
            {
                Debug.CheckYeh("Adding " + nameof(UD_FleshGolems_CorpseIconColor), indent[1]);
                ParentObject?.AddPart(new UD_FleshGolems_CorpseIconColor(ParentObject));
            }
            Debug.Log(nameof(PartsInNeedOfRemovalWhenAnimated), PartsInNeedOfRemovalWhenAnimated?.Count, indent[1]);
            foreach (string partToRemove in PartsInNeedOfRemovalWhenAnimated)
            {
                Debug.YehNah(
                    Message: partToRemove,
                    Good: ParentObject?.RemovePart(partToRemove),
                    Indent: indent[2]);
            }
            Debug.Log(nameof(BleedLiquid), BleedLiquid ?? NULL, indent[1]);
            if (BleedLiquid.IsNullOrEmpty())
            {
                Debug.Log(
                    nameof(GetBleedLiquidEvent) + "." + 
                    nameof(GetBleedLiquidEvent.GetFor), 
                    GetBleedLiquidEvent.GetFor(ParentObject),
                    indent[2]);
            }

            HaltGreaterVoiderLairCreation(ParentObject, Reanimator);
            SecretlySealLiquidVolume(ParentObject);

            DeathQuestionsAreRude = ParentObject.BaseID % 4 == 0;

            AttemptToSuffer();
            base.Attach();
        }

        public override bool SameAs(IPart p)
        {
            return false;
        }

        public UD_FleshGolems_ReanimatedCorpse RenderDisplayNameSetAltered()
        {
            IsRenderDisplayNameUpdated = true;
            return this;
        }
        public UD_FleshGolems_ReanimatedCorpse DescriptionSetAltered()
        {
            IsAlteredDescription = true;
            return this;
        }
        public static bool AllowReanimatedPrefix(GameObject Corpse)
            => Corpse.GetPropertyOrTag(REANIMATED_NO_ADJECTIVE_PROPTAG) is not string noAdjectivePropTag
            || !int.TryParse(noAdjectivePropTag, out int noAdjectiveResult)
            || noAdjectiveResult < 1;

        private static bool TryGetLiquidPortion(string LiquidComponent, out LiquidPortion LiquidPortion)
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
        private static List<LiquidPortion> GetBleedLiquids(string BleedLiquids)
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

        private static Dictionary<string, int> GetBleedLiquidDict(List<LiquidPortion> LiquidPortionsList)
            => LiquidPortionsList.ToDictionary(lp => lp.Liquid, lp => lp.Portion);

        private static List<LiquidPortion> GetBleedLiquidPortions(Dictionary<string, int> LiquidDict)
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
                if (!NoSuffer)
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
                        NoSuffer = !frankenCorpse.ForceApplyEffect(new UD_FleshGolems_UnendingSuffering(Reanimator, tier, timesReanimated + 1), Reanimator);
                        return !NoSuffer;
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

        public static void SecretlySealLiquidVolume(GameObject FrankenCorpse)
        {
            if (FrankenCorpse.TryGetPart(out LiquidVolume liquidVolume))
            {
                liquidVolume.Sealed = true;
                liquidVolume.ShowSeal = false;
                liquidVolume.LiquidVisibleWhenSealed = true;
            }
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || ID == GetDisplayNameEvent.ID
                || ID == GetShortDescriptionEvent.ID
                || ID == DecorateDefaultEquipmentEvent.ID
                || ID == EndTurnEvent.ID
                || ID == EnteredCellEvent.ID
                || ID == EnvironmentalUpdateEvent.ID
                || ID == AfterZoneBuiltEvent.ID
                || ID == GetBleedLiquidEvent.ID
                || ID == BeforeTakeActionEvent.ID
                || ID == GetDebugInternalsEvent.ID;
        }
        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if (E.Context == "CreatureType" && E.Reference)
            {
                E.ReplacePrimaryBase(E.Object.GetBlueprint().DisplayName().RemoveAll("[", "]"));

                if (IdentityType < IdentityType.ParticipantVillager)
                {
                    E.AddAdjective("corpse of", CorpseAdjective);
                }
                else
                if (IdentityType < IdentityType.Corpse)
                {
                    E.AddClause("corpse", CorpseClause);
                }

                E.AddAdjective(REANIMATED_ADJECTIVE, CorpseAdjective - 1);
            }
            else
            if (PastLife != null
                && PastLife.GenerateDisplayName(out IdentityType identityType) is string newDisplayName
                 && identityType != IdentityType.Librarian)
            {
                if (E.BaseOnly) // this is largely for the stone statues, we'll see if it busts anything else.
                {
                    if (identityType < IdentityType.ParticipantVillager)
                    {
                        newDisplayName = "corpse of " + newDisplayName;
                    }
                    else
                    if (identityType < IdentityType.Corpse)
                    {
                        newDisplayName += " corpse";
                    }
                    if (AllowReanimatedPrefix(E.Object) && identityType != IdentityType.Librarian)
                    {
                        newDisplayName = REANIMATED_ADJECTIVE + " " + newDisplayName;
                    }
                }
                else
                {
                    if (identityType < IdentityType.ParticipantVillager)
                    {
                        E.AddAdjective("corpse of", CorpseAdjective);
                        E.AddAdjective("the", CorpseAdjective - 2);
                    }
                    else
                    if (identityType < IdentityType.Corpse)
                    {
                        E.AddClause("corpse", CorpseClause);
                    }
                    if (AllowReanimatedPrefix(E.Object) && identityType != IdentityType.Librarian)
                    {
                        E.AddAdjective(REANIMATED_ADJECTIVE, CorpseAdjective - 1);
                    }
                }
                E.ReplacePrimaryBase(newDisplayName);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (!IsAlteredDescription
                && IdentityType > IdentityType.Librarian
                && !NewDescription.IsNullOrEmpty()
                && ParentObject.TryGetPart(out Description description))
            {
                IsAlteredDescription = true;
                description._Short += "\n\n" + NewDescription;
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
                        && defaultBehavior.InheritsFrom("UD_FleshGolems Ragged Weapon")
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
        public override bool HandleEvent(EnteredCellEvent E)
        {
            using Indent indent = new(1);
            if (ParentObject is GameObject frankenCorpse
                && IdentityType <= IdentityType.ParticipantVillager)
            {
                bool wantsToAlign = !frankenCorpse.Brain.Allegiance.Any(a => a.Key == UD_FleshGolems_PastLife.PREVIOUSLY_SENTIENT_BEINGS && a.Value > 0);
                if (wantsToAlign
                    || !IsAlteredDescription)
                {
                    Debug.LogCaller(indent,
                        ArgPairs: new Debug.ArgPair[]
                        {
                            Debug.Arg(nameof(EnteredCellEvent)),
                            Debug.Arg(nameof(IdentityType), IdentityType.ToStringWithNum()),
                        });
                }
                if (!IsAlteredDescription
                    && !NewDescription.IsNullOrEmpty()
                    && ParentObject.TryGetPart(out Description description))
                {
                    Debug.Log(nameof(NewDescription), !NewDescription.IsNullOrEmpty(), Indent: indent[1]);
                    if (IdentityType == IdentityType.Librarian
                        && description._Short.Contains(LIBRARIAN_FRAGMENT))
                    {
                        Debug.Log(nameof(IsAlteredDescription), IsAlteredDescription, Indent: indent[2]);
                        if (frankenCorpse.GetBlueprint().TryGetPartParameter(nameof(Description), nameof(Description._Short), out string corpseDescription)
                            && !corpseDescription.IsNullOrEmpty())
                        {
                            Debug.Log(nameof(IsAlteredDescription), IsAlteredDescription, Indent: indent[3]);
                            IsAlteredDescription = true;
                            string combinedDescription = corpseDescription + "\n\n" + NewDescription;
                            description._Short = combinedDescription;
                        }
                    }
                    else
                    if (IdentityType > IdentityType.Librarian)
                    {
                        IsAlteredDescription = true;
                        description._Short += "\n\n" + NewDescription;
                        Debug.Log(nameof(IsAlteredDescription), IsAlteredDescription, Indent: indent[2]);
                    }
                }
                if (wantsToAlign)
                {
                    PastLife.AlignWithPreviouslySentientBeings();
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnvironmentalUpdateEvent E)
        {
            if (!IsRenderDisplayNameUpdated
                && !ParentObject.IsPlayer()
                && !ParentObject.HasPlayerBlueprint()
                && ParentObject.BaseID != 1)
            {
                IsRenderDisplayNameUpdated = true;
                PastLife?.AlignWithPreviouslySentientBeings();
                if (ParentObject.Render is Render corpseRender)
                {
                    // PastLife.RenderDisplayName = null;
                    // corpseRender.DisplayName = ParentObject.GetReferenceDisplayName(Short: true);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AfterZoneBuiltEvent E)
        {
            using Indent indent = new(1);
            if (ParentObject is GameObject frankenCorpse
                && frankenCorpse.CurrentZone == E.Zone
                && IdentityType > IdentityType.ParticipantVillager)
            {
                if (!frankenCorpse.Brain.Allegiance.Any(a => a.Key == UD_FleshGolems_PastLife.PREVIOUSLY_SENTIENT_BEINGS)
                    || !IsAlteredDescription)
                {
                    Debug.LogCaller(indent,
                        ArgPairs: new Debug.ArgPair[]
                        {
                            Debug.Arg(nameof(EnteredCellEvent)),
                            Debug.Arg(nameof(IdentityType), IdentityType.ToStringWithNum()),
                        });
                }
                if (!IsAlteredDescription
                    && !NewDescription.IsNullOrEmpty()
                    && ParentObject.TryGetPart(out Description description))
                {
                    Debug.Log(nameof(NewDescription), !NewDescription.IsNullOrEmpty(), Indent: indent[1]);
                    if (IdentityType == IdentityType.Librarian
                        && description._Short.Contains(LIBRARIAN_FRAGMENT))
                    {
                        Debug.Log(nameof(IsAlteredDescription), IsAlteredDescription, Indent: indent[2]);
                        if (frankenCorpse.GetBlueprint().TryGetPartParameter(nameof(Description), nameof(Description._Short), out string corpseDescription)
                            && !corpseDescription.IsNullOrEmpty())
                        {
                            Debug.Log(nameof(IsAlteredDescription), IsAlteredDescription, Indent: indent[3]);
                            IsAlteredDescription = true;
                            string combinedDescription = corpseDescription + "\n\n" + NewDescription;
                            description._Short = combinedDescription;
                        }
                    }
                    else
                    if (IdentityType > IdentityType.Librarian)
                    {
                        IsAlteredDescription = true;
                        description._Short += "\n\n" + NewDescription;
                        Debug.Log(nameof(IsAlteredDescription), IsAlteredDescription, Indent: indent[2]);
                    }
                }
                PastLife.AlignWithPreviouslySentientBeings();
            }
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
            E.AddEntry(this, nameof(IdentityType), IdentityType.ToStringWithNum());
            E.AddEntry(this, nameof(DeathQuestionsAreRude), DeathQuestionsAreRude);
            if (DeathMemory != UndefinedDeathMemoryElement)
            {
                string deathMemoryHasFlags = DeathMemoryElementsValues
                    ?.ConvertToStringList(kvp => kvp.Key + " (" + (int)kvp.Value + "): " + DeathMemory.HasFlag(kvp.Value).YehNah())
                    ?.GenerateBulletList(Bullet: null, BulletColor: null);
                E.AddEntry(this, nameof(DeathMemory) + " (" + (int)DeathMemory + ")", deathMemoryHasFlags);
                E.AddEntry(nameof(UD_FleshGolems_VengeanceAssistant), nameof(DeathMemory) + " (" + (int)DeathMemory + ")", deathMemoryHasFlags);
            }
            else
            {
                E.AddEntry(this, nameof(DeathMemory), nameof(UndefinedDeathMemoryElement));
                E.AddEntry(nameof(UD_FleshGolems_VengeanceAssistant), nameof(DeathMemory), nameof(UndefinedDeathMemoryElement));
            }
            E.AddEntry(nameof(UD_FleshGolems_VengeanceAssistant), nameof(DeathQuestionsAreRude), DeathQuestionsAreRude);
            E.AddEntry(this, nameof(IsRenderDisplayNameUpdated), IsRenderDisplayNameUpdated);
            E.AddEntry(this, nameof(IsAlteredDescription), IsAlteredDescription);

            E.AddEntry(this, nameof(BleedLiquid), BleedLiquid);
            E.AddEntry(this, nameof(NoSuffer), NoSuffer);
            return base.HandleEvent(E);
        }
    }
}