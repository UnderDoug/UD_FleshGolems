using System;
using System.Collections.Generic;
using System.Text;

using HarmonyLib;

using Genkit;

using XRL.Core;
using XRL.Language;
using XRL.Rules;
using XRL.World.Anatomy;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.ObjectBuilders;
using XRL.World.Quests.GolemQuest;
using XRL.World.Effects;
using XRL.World.Skills;
using XRL.World.AI;

using NanoNecroAnimation = XRL.World.Parts.Mutation.UD_FleshGolems_NanoNecroAnimation;
using RaggedNaturalWeapon = XRL.World.Parts.UD_FleshGolems_RaggedNaturalWeapon;
using Taxonomy = XRL.World.Parts.UD_FleshGolems_RaggedNaturalWeapon.TaxonomyAdjective;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Parts.PastLifeHelpers;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using SerializeField = UnityEngine.SerializeField;
using System.Linq;
using static XRL.World.Parts.UD_FleshGolems_PastLife;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_CorpseReanimationHelper : IScribedPart
    {
        [UD_FleshGolems_DebugRegistry]
        public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
        {
            Registry.Register(nameof(AssignStatsFromStatistics), false);
            return Registry;
        }

        public const string REANIMATED_CONVO_ID_TAG = "UD_FleshGolems_ReanimatedConversationID";
        public const string REANIMATED_EPITHETS_TAG = "UD_FleshGolems_ReanimatedEpithets";
        public const string REANIMATED_ALT_TILE_PROPTAG = "UD_FleshGolems_AlternateTileFor:";
        public const string REANIMATED_TILE_PROPTAG = "UD_FleshGolems_PastLife_TileOverride";
        public const string REANIMATED_TAXA_XTAG = "UD_FleshGolems_Taxa";
        public const string REANIMATED_PART_EXCLUSIONS_PROPTAG = "UD_FleshGolems_Reanimated_PartExclusions";
        public const string REANIMATED_FLIP_COLORS_PROPTAG = "UD_FleshGolems ReanimationHelper FlipColors";
        public const string REANIMATED_SET_COLORS_PROPTAG = "UD_FleshGolems ReanimationHelper SetColors";
        public const string REANIMATED_GEN_SOURCE_INV_PROPTAG = "UD_FleshGolems ReanimationHelper GenerateSourceInventory";

        public const string CYBERNETICS_LICENSES = "CyberneticsLicenses";
        public const string CYBERNETICS_LICENSES_FREE = "FreeCyberneticsLicenses";

        public UD_FleshGolems_PastLife PastLife => ParentObject?.GetPart<UD_FleshGolems_PastLife>();

        public static List<string> PhysicalStats => new() { "Strength", "Agility", "Toughness", };
        public static List<string> MentalStats => new() { "Intelligence", "Willpower", "Ego", };

        public static List<string> IPartsToSkipWhenReanimating => new()
        {
            nameof(SizeAdjective),
            nameof(Titles),
            nameof(Epithets),
            nameof(Honorifics),
            // nameof(Lovely),
            nameof(SecretObject),
            nameof(ConvertSpawner),
            // nameof(Leader),
            // nameof(Followers),
            nameof(DromadCaravan),
            nameof(HasGuards),
            nameof(SnapjawPack1),
            nameof(BaboonHero1Pack),
            nameof(GoatfolkClan1),
            nameof(EyelessKingCrabSkuttle1),
        };

        public static List<string> BlueprintsToSkipCheckingForCorpses => new()
        {
            "OrPet",
        };

        public bool IsALIVE;

        public bool AlwaysAnimate;

        public GameObject Reanimator;

        private List<int> FailedToRegisterEvents;

        public UD_FleshGolems_CorpseReanimationHelper()
        {
            IsALIVE = false;
            AlwaysAnimate = false;
            Reanimator = null;
            FailedToRegisterEvents = new();
        }

        public override bool AllowStaticRegistration()
        {
            return true;
        }

        public bool Animate(out GameObject FrankenCorpse)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(ParentObject), ParentObject?.DebugName ?? NULL),
                });

            FrankenCorpse = null;
            if (ParentObject != null && !ParentObject.HasPart<AnimatedObject>())
            {
                Debug.CheckYeh(nameof(ParentObject) + " not " + nameof(AnimatedObject), indent[1]);

                AnimateObject.Animate(ParentObject);

                if (ParentObject.HasPart<AnimatedObject>())
                {
                    FrankenCorpse = ParentObject;
                    return true;
                }
            }
            return false;
        }
        public bool Animate()
        {
            return Animate(out _);
        }

        public static bool AssignStatsFromStatistics(
            GameObject FrankenCorpse,
            Dictionary<string, Statistic> Statistics,
            bool Override = true,
            Dictionary<string, int> StatAdjustments = null,
            bool HitpointsFallbackToMinimum = false)
        {
            using Indent indent = new();
            bool any = false;
            if (FrankenCorpse == null || Statistics.IsNullOrEmpty())
            {
                Debug.CheckNah("No " + nameof(FrankenCorpse) + " or " + nameof(Statistics) + " empty!", indent[1]);
                return any;
            }
            foreach ((string statName, Statistic sourceStat) in Statistics)
            {
                Statistic statistic = new(sourceStat);
                if (sourceStat.Name == "Hitpoints")
                {
                    if (HitpointsFallbackToMinimum)
                    {
                        if (sourceStat.BaseValue < 1)
                        {
                            sourceStat.BaseValue = 1;
                        }
                        if (sourceStat.sValue.IsNullOrEmpty())
                        {
                            sourceStat.sValue = "5,(t)*5,4d3*(t)";
                        }
                        if (sourceStat.Penalty != 0)
                        {
                            sourceStat.Penalty = 0;
                        }
                    }
                       
                }
                if (!FrankenCorpse.HasStat(statName))
                {
                    FrankenCorpse.Statistics.Add(statName, statistic);
                }
                else
                if (Override)
                {
                    FrankenCorpse.Statistics[statName] = statistic;
                }
                int statAdjust = 0;
                if (!StatAdjustments.IsNullOrEmpty()
                    && StatAdjustments.ContainsKey(statName))
                {
                    FrankenCorpse.Statistics[statName].BaseValue += StatAdjustments[statName];
                    statAdjust = StatAdjustments[statName];
                }
                statistic.Owner = FrankenCorpse;

                int statValue = FrankenCorpse.Statistics[statName].Value;
                int statBaseValue = FrankenCorpse.Statistics[statName].BaseValue;
                string sValue = FrankenCorpse.Statistics[statName].sValue;
                Debug.Arg(statName, statValue + "/" + statBaseValue + " (" + statAdjust.Signed() + ") | " + (sValue ?? "no sValue")).Log(indent[1]);
                any = true;
            }

            Debug.CheckYeh(nameof(Statistics) + " Assigned!", indent[0]);

            return any;
        }
        public static bool AssignStatsFromPastLife(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            bool Override = true,
            Dictionary<string, int> StatAdjustments = null,
            bool HitpointsFallbackToMinimum = false)
        {
            return AssignStatsFromStatistics(FrankenCorpse, PastLife.Stats, Override, StatAdjustments, HitpointsFallbackToMinimum);
        }
        public static bool AssignStatsFromPastLifeWithAdjustment(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            bool Override = true,
            int PhysicalAdjustment = 0,
            int MentalAdjustment = 0,
            bool HitpointsFallbackToMinimum = false)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(PastLife), PastLife != null),
                    Debug.Arg(nameof(Override), Override),
                    Debug.Arg(nameof(PhysicalAdjustment), PhysicalAdjustment),
                    Debug.Arg(nameof(MentalAdjustment), MentalAdjustment),
                });
            return AssignStatsFromStatistics(FrankenCorpse, PastLife.Stats, Override, new()
            {
                { "Strength", PhysicalAdjustment },
                { "Agility", PhysicalAdjustment },
                { "Toughness", PhysicalAdjustment },
                { "Intelligence", MentalAdjustment },
                { "Willpower", MentalAdjustment },
                { "Ego", MentalAdjustment },
            }, HitpointsFallbackToMinimum);
        }
        public static bool AssignStatsFromPastLifeWithFactor(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            bool Override = true,
            float PhysicalAdjustmentFactor = 1f,
            float MentalAdjustmentFactor = 1f,
            bool HitpointsFallbackToMinimum = false)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(PastLife), PastLife != null),
                    Debug.Arg(nameof(Override), Override),
                    Debug.Arg(nameof(PhysicalAdjustmentFactor), PhysicalAdjustmentFactor),
                    Debug.Arg(nameof(MentalAdjustmentFactor), MentalAdjustmentFactor),
                });
            if (FrankenCorpse == null || PastLife == null || PastLife.Stats.IsNullOrEmpty())
            {
                return false;
            }
            Dictionary<string, int> StatAdjustments = new();
            foreach ((string statName, Statistic stat) in PastLife?.Stats)
            {
                if (PhysicalStats.Contains(statName) || MentalStats.Contains(statName))
                {
                    float adjustmentFactor = 1f;
                    if (PhysicalStats.Contains(statName))
                    {
                        adjustmentFactor = PhysicalAdjustmentFactor;
                    }
                    else
                    if (MentalStats.Contains(statName))
                    {
                        adjustmentFactor = MentalAdjustmentFactor;
                    }
                    if (adjustmentFactor != 1f)
                    {
                        int adjustmentValue = (int)(stat.BaseValue * adjustmentFactor) - stat.BaseValue;
                        StatAdjustments.Add(statName, adjustmentValue);
                        Debug.Log(statName, adjustmentValue.Signed(), indent[1]);
                    }
                }
            }
            return AssignStatsFromStatistics(FrankenCorpse, PastLife.Stats, Override, StatAdjustments, HitpointsFallbackToMinimum);
        }
        public static bool AssignStatsFromBlueprint(
            GameObject FrankenCorpse,
            GameObjectBlueprint SourceBlueprint,
            bool Override = true,
            Dictionary<string, int> StatAdjustments = null,
            bool HitpointsFallbackToMinimum = false)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(SourceBlueprint), SourceBlueprint.Name),
                    Debug.Arg(nameof(Override), Override),
                    Debug.Arg(nameof(StatAdjustments), StatAdjustments?.Count ?? 0),
                });
            return AssignStatsFromStatistics(FrankenCorpse, SourceBlueprint.Stats, Override, StatAdjustments, HitpointsFallbackToMinimum);
        }

        public static void AssignPartsFromBlueprint(
            GameObject FrankenCorpse,
            GameObjectBlueprint SourceBlueprint,
            Predicate<IPart> Exclude = null)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(SourceBlueprint), SourceBlueprint?.Name ?? NULL),
                    Debug.Arg(nameof(Exclude), Exclude != null),
                });
            if (FrankenCorpse == null
                || SourceBlueprint == null
                || SourceBlueprint.allparts.IsNullOrEmpty())
            {
                return;
            }
            foreach (GamePartBlueprint sourcePartBlueprint in SourceBlueprint.allparts.Values)
            {
                if (Stat.Random(1, sourcePartBlueprint.ChanceOneIn) != 1)
                {
                    Debug.CheckNah(sourcePartBlueprint.Name, nameof(sourcePartBlueprint.ChanceOneIn) + " failed", indent[1]);
                    continue;
                }
                if (sourcePartBlueprint.T == null)
                {
                    MetricsManager.LogModError(ThisMod, "Unknown part " + sourcePartBlueprint.Name + "!");
                    continue;
                }
                if ((sourcePartBlueprint.Reflector?.GetInstance() ?? (Activator.CreateInstance(sourcePartBlueprint.T) as IPart)) is not IPart sourcePart)
                {
                    MetricsManager.LogError("Part " + sourcePartBlueprint.T + " is not an IPart");
                    continue;
                }
                if (Exclude != null && Exclude(sourcePart))
                {
                    Debug.CheckNah(sourcePartBlueprint.Name, "part excluded", indent[1]);
                    continue;
                }
                sourcePart.ParentObject = FrankenCorpse;
                sourcePartBlueprint.InitializePartInstance(sourcePart);
                FrankenCorpse.AddPart(sourcePart, Creation: true);
                Debug.CheckYeh(sourcePartBlueprint.Name, "added", indent[1]);

                if (sourcePartBlueprint.TryGetParameter("Builder", out string partBuilderName)
                    && ModManager.ResolveType("XRL.World.PartBuilders", partBuilderName) is Type partBuilderType
                    && Activator.CreateInstance(partBuilderType) is IPartBuilder partBuilder)
                {
                    partBuilder.BuildPart(sourcePart, Context: "Initialization");
                }
            }
        }

        public static bool AssignMutationsFromBlueprint(
            Mutations FrankenMutations,
            GameObjectBlueprint SourceBlueprint,
            Predicate<BaseMutation> Exclude = null)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenMutations), FrankenMutations != null),
                    Debug.Arg(nameof(SourceBlueprint), SourceBlueprint?.Name ?? NULL),
                    Debug.Arg(nameof(Exclude), Exclude != null),
                });
            bool any = false;
            if (FrankenMutations == null
                || SourceBlueprint == null
                || SourceBlueprint.Mutations.IsNullOrEmpty())
            {
                return any;
            }
            foreach (GamePartBlueprint sourceMutationBlueprint in SourceBlueprint.Mutations.Values)
            {
                if (Stat.Random(1, sourceMutationBlueprint.ChanceOneIn) != 1)
                {
                    Debug.CheckNah(sourceMutationBlueprint.Name, nameof(sourceMutationBlueprint.ChanceOneIn) + " failed", indent[1]);
                    continue;
                }
                string mutationNamespace = "XRL.World.Parts.Mutation." + sourceMutationBlueprint.Name;
                Type mutationType = ModManager.ResolveType(mutationNamespace);

                if (mutationType == null)
                {
                    MetricsManager.LogError("Unknown mutation " + mutationNamespace);
                    continue;
                }
                if ((sourceMutationBlueprint.Reflector?.GetNewInstance() ?? Activator.CreateInstance(mutationType)) is not BaseMutation baseMutation)
                {
                    MetricsManager.LogError("Mutation " + mutationNamespace + " is not a BaseMutation");
                    continue;
                }
                if (Exclude != null && Exclude(baseMutation))
                {
                    Debug.CheckNah(sourceMutationBlueprint.Name, "excluded", indent[1]);
                    continue;
                }
                sourceMutationBlueprint.InitializePartInstance(baseMutation);
                if (sourceMutationBlueprint.TryGetParameter("Builder", out string mutationBuilderName)
                    && ModManager.ResolveType("XRL.World.PartBuilders." + mutationBuilderName) is Type mutationBuilderType
                    && Activator.CreateInstance(mutationBuilderType) is IPartBuilder mutationBuilder)
                {
                    mutationBuilder.BuildPart(baseMutation, Context: "Initialization");
                }
                BaseMutation baseMutationToAdd = baseMutation;
                bool alreadyHaveMutation = FrankenMutations.HasMutation(sourceMutationBlueprint.Name);
                if (alreadyHaveMutation)
                {
                    baseMutationToAdd = FrankenMutations.GetMutation(sourceMutationBlueprint.Name);
                    baseMutationToAdd.BaseLevel += baseMutation.Level;
                }
                else
                {
                    FrankenMutations.AddMutation(baseMutationToAdd, baseMutation.Level);
                }
                bool capOverridden = false;
                if (baseMutationToAdd.CapOverride == -1)
                {
                    baseMutationToAdd.CapOverride = baseMutation.Level;
                    capOverridden = true;
                }
                string alredyHaveString = alreadyHaveMutation ? " - had already" : " added";
                string capOverrideString = capOverridden ? (" (capOverride: " + baseMutation.Level + ")") : null ;
                Debug.CheckYeh(sourceMutationBlueprint.Name + alredyHaveString + capOverridden, indent[1]);
                any = true;
            }
            return any;
        }

        public static bool AssignSkillsFromBlueprint(
            Skills FrankenSkills,
            GameObjectBlueprint SourceBlueprint,
            Predicate<BaseSkill> Exclude = null)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenSkills), FrankenSkills != null),
                    Debug.Arg(nameof(SourceBlueprint), SourceBlueprint?.Name ?? NULL),
                    Debug.Arg(nameof(Exclude), Exclude != null),
                });

            bool any = false;
            if (FrankenSkills == null
                || SourceBlueprint == null
                || SourceBlueprint.Skills.IsNullOrEmpty())
            {
                return any;
            }
            foreach (GamePartBlueprint sourceSkillBlueprint in SourceBlueprint.Skills.Values)
            {
                if (Stat.Random(1, sourceSkillBlueprint.ChanceOneIn) != 1)
                {
                    Debug.CheckNah(sourceSkillBlueprint.Name, nameof(sourceSkillBlueprint.ChanceOneIn) + " failed", indent[1]);
                    continue;
                }
                string skillNamespace = "XRL.World.Parts.Skill." + sourceSkillBlueprint.Name;
                Type skillType = ModManager.ResolveType(skillNamespace);

                if (skillType == null)
                {
                    MetricsManager.LogError("Unknown skill " + skillNamespace);
                    return any;
                }
                if ((sourceSkillBlueprint.Reflector?.GetNewInstance() ?? Activator.CreateInstance(skillType)) is not BaseSkill baseSkill)
                {
                    MetricsManager.LogError("Skill " + skillNamespace + " is not a " + nameof(BaseSkill));
                    continue;
                }
                if (Exclude != null && Exclude(baseSkill))
                {
                    Debug.CheckNah(sourceSkillBlueprint.Name, "excluded", indent[1]);
                    continue;
                }

                sourceSkillBlueprint.InitializePartInstance(baseSkill);
                if (sourceSkillBlueprint.TryGetParameter("Builder", out string skillBuilderName)
                    && ModManager.ResolveType("XRL.World.PartBuilders." + skillBuilderName) is Type skillBuilderType
                    && Activator.CreateInstance(skillBuilderType) is IPartBuilder skillBuilder)
                {
                    skillBuilder.BuildPart(baseSkill, Context: "Initialization");
                }

                Debug.CheckYeh(sourceSkillBlueprint.Name + " added", indent[1]);

                any = FrankenSkills.AddSkill(baseSkill) || any;
            }
            return any;
        }

        public static bool ImplantCyberneticsFromAttachedParts(
            GameObject FrankenCorpse,
            out bool AnyImplanted)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                });
            AnyImplanted = false;
            if (FrankenCorpse.Body is Body frankenBody)
            {
                if (FrankenCorpse.TryGetPart(out CyberneticsButcherableCybernetic butcherableCybernetics))
                {
                    int startingLicenses = Stat.RollCached("2d2-1");

                    FrankenCorpse.SetIntProperty(CYBERNETICS_LICENSES, startingLicenses);
                    FrankenCorpse.SetIntProperty(CYBERNETICS_LICENSES_FREE, startingLicenses);

                    List<GameObject> butcherableCyberneticsList = Event.NewGameObjectList(butcherableCybernetics.Cybernetics);
                    foreach (GameObject butcherableCybernetic in butcherableCyberneticsList)
                    {
                        if (butcherableCybernetic.TryGetPart(out CyberneticsBaseItem butcherableCyberneticBasePart)
                            && butcherableCyberneticBasePart.Slots is string slotsString)
                        {
                            int cyberneticsCost = butcherableCyberneticBasePart.Cost;
                            FrankenCorpse.ModIntProperty(CYBERNETICS_LICENSES, cyberneticsCost);
                            FrankenCorpse.ModIntProperty(CYBERNETICS_LICENSES_FREE, cyberneticsCost);

                            List<string> slotsList = slotsString.CachedCommaExpansion();
                            slotsList.ShuffleInPlace();
                            foreach (string slot in slotsList)
                            {
                                List<BodyPart> bodyParts = frankenBody.GetPart(slot);
                                bodyParts.ShuffleInPlace();

                                foreach (BodyPart bodyPart in bodyParts)
                                {
                                    if (bodyPart.CanReceiveCyberneticImplant()
                                        && !bodyPart.HasInstalledCybernetics())
                                    {
                                        bodyPart.Implant(butcherableCybernetic);
                                        break;
                                    }
                                }
                                butcherableCybernetics.Cybernetics.Remove(butcherableCybernetic);
                            }
                        }
                    }
                    FrankenCorpse.RemovePart(butcherableCybernetics);
                    AnyImplanted = true || AnyImplanted;
                }
                if (!AnyImplanted)
                {
                    if (FrankenCorpse.TryGetPart(out CyberneticsHasRandomImplants sourceRandomImplants))
                    {
                        if (sourceRandomImplants.ImplantChance.RollCached().in100())
                        {
                            int attempts = 0;
                            int maxAttempts = 30;
                            int atLeastLicensePoints = sourceRandomImplants.LicensesAtLeast.RollCached();
                            int availableLicensePoints = FrankenCorpse.GetIntProperty(CYBERNETICS_LICENSES);
                            int spentLicensePoints = 0;
                            string implantTable = sourceRandomImplants.ImplantTable;
                            if (availableLicensePoints < atLeastLicensePoints)
                            {
                                FrankenCorpse.SetIntProperty(CYBERNETICS_LICENSES, atLeastLicensePoints);
                            }
                            else
                            {
                                atLeastLicensePoints = availableLicensePoints;
                            }
                            while (++attempts <= maxAttempts && spentLicensePoints < atLeastLicensePoints)
                            {
                                string possibleImplantBlueprintName = PopulationManager.RollOneFrom(implantTable).Blueprint;
                                if (possibleImplantBlueprintName == null)
                                {
                                    MetricsManager.LogError("got null blueprint from " + sourceRandomImplants.ImplantTable);
                                    continue;
                                }
                                if (!GameObjectFactory.Factory.Blueprints.TryGetValue(possibleImplantBlueprintName, out var possibleImplantBlueprint))
                                {
                                    MetricsManager.LogError("got invalid blueprint \"" + possibleImplantBlueprintName + "\" from " + implantTable);
                                    continue;
                                }
                                if (!possibleImplantBlueprint.TryGetPartParameter(nameof(CyberneticsBaseItem), nameof(CyberneticsBaseItem.Slots), out string slotTypes))
                                {
                                    MetricsManager.LogError("Weird blueprint in random cybernetics table: " + possibleImplantBlueprintName + " from table " + implantTable);
                                    continue;
                                }

                                List<string> slotTypesList = new(slotTypes?.Split(','));
                                slotTypesList.ShuffleInPlace();

                                if (GameObject.Create(possibleImplantBlueprintName) is GameObject cyberneticObject)
                                {
                                    if (!cyberneticObject.TryGetPart(out CyberneticsBaseItem cyberneticsBaseItem))
                                    {
                                        cyberneticObject?.Obliterate();
                                    }
                                    else
                                    {
                                        foreach (string requiredType in slotTypesList)
                                        {
                                            List<BodyPart> bodyPartsList = frankenBody.GetPart(requiredType);
                                            bodyPartsList.ShuffleInPlace();
                                            foreach (BodyPart implantTargetBodyPart in bodyPartsList)
                                            {
                                                if (implantTargetBodyPart == null || implantTargetBodyPart._Cybernetics != null)
                                                {
                                                    continue;
                                                }
                                                if (atLeastLicensePoints - spentLicensePoints >= cyberneticsBaseItem.Cost)
                                                {
                                                    spentLicensePoints += cyberneticsBaseItem.Cost;
                                                    implantTargetBodyPart.Implant(cyberneticObject);
                                                    AnyImplanted = true || AnyImplanted;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    if (frankenBody.FindCybernetics(cyberneticObject) is not BodyPart implantedLimb)
                                    {
                                        cyberneticObject?.Obliterate();
                                    }
                                }
                            }
                        }
                    }

                    if (FrankenCorpse.TryGetPart(out CyberneticsHasImplants sourceImplants))
                    {
                        List<string> implantsAtLocationList = new(sourceImplants.Implants.Split(','));

                        foreach (string implantBlueprintLocation in implantsAtLocationList)
                        {
                            string[] implantAtLocation = implantBlueprintLocation.Split('@');
                            if (GameObject.Create(implantAtLocation[0]) is GameObject implantObject)
                            {
                                if (frankenBody.GetPartByNameWithoutCybernetics(implantAtLocation[1]) is BodyPart bodyPartWithoutImplant)
                                {
                                    bodyPartWithoutImplant.Implant(implantObject);
                                    AnyImplanted = true || AnyImplanted;
                                }
                                else
                                {
                                    implantObject?.Obliterate();
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        public static bool ProcessMoveToDeathCell(GameObject corpse, UD_FleshGolems_PastLife PastLife)
        {
            return PastLife == null
                || (corpse != null
                    && PastLife?.DeathAddress is DeathCoordinates deathAddress
                    && deathAddress.DeathZone == corpse.CurrentZone?.ZoneID
                    && deathAddress.GetLocation() != corpse.CurrentCell.Location
                    && corpse.Physics is Physics corpsePhysics
                    && corpsePhysics.ProcessTargetedMove(
                        TargetCell: corpse.CurrentZone.GetCell(deathAddress.GetLocation()),
                        Type: "DirectMove",
                        PreEvent: "BeforeDirectMove",
                        PostEvent: "AfterDirectMove",
                        EnergyCost: 0,
                        System: true,
                        IgnoreCombat: true,
                        IgnoreGravity: true));
        }

        public static IEnumerable<GameObjectBlueprint> GetRaggedNaturalWeapons(Predicate<GameObjectBlueprint> Filter = null)
        {
            foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.GetBlueprintsInheritingFrom("UD_FleshGolems Ragged Weapon"))
            {
                if (bp.IsBaseBlueprint() || (Filter != null && !Filter(bp)))
                {
                    continue;
                }
                yield return bp;
            }
        }
        public static bool MeleeWeaponSlotAndSkillMatchesBlueprint(GameObjectBlueprint GameObjectBlueprint, MeleeWeapon MeleeWeapon)
        {
            return MeleeWeaponSlotMatchesBlueprint(GameObjectBlueprint, MeleeWeapon)
                && MeleeWeaponSkillMatchesBlueprint(GameObjectBlueprint, MeleeWeapon);
        }
        public static bool MeleeWeaponSlotMatchesBlueprint(GameObjectBlueprint GameObjectBlueprint, string Slot)
        {
            if (!GameObjectBlueprint.TryGetPartParameter(nameof(Parts.MeleeWeapon), nameof(Parts.MeleeWeapon.Slot), out string blueprintMeleeWeaponSlot)
                || Slot != blueprintMeleeWeaponSlot)
            {
                return false;
            }
            return true;
        }
        public static bool MeleeWeaponSlotMatchesBlueprint(GameObjectBlueprint GameObjectBlueprint, MeleeWeapon MeleeWeapon)
        {
            return MeleeWeaponSlotMatchesBlueprint(GameObjectBlueprint, MeleeWeapon.Slot);
        }
        public static bool MeleeWeaponSkillMatchesBlueprint(GameObjectBlueprint GameObjectBlueprint, string Skill)
        {
            if (!GameObjectBlueprint.TryGetPartParameter(nameof(Parts.MeleeWeapon), nameof(Parts.MeleeWeapon.Skill), out string blueprintMeleeWeaponSkill)
                || Skill != blueprintMeleeWeaponSkill)
            {
                return false;
            }
            return true;
        }
        public static bool MeleeWeaponSkillMatchesBlueprint(GameObjectBlueprint GameObjectBlueprint, MeleeWeapon MeleeWeapon)
        {
            return MeleeWeaponSkillMatchesBlueprint(GameObjectBlueprint, MeleeWeapon.Skill);
        }

        public static void AddPlayerSkillsIfPlayer(GameObject FrankenCorpse, bool WasPlayer)
        {
            if (WasPlayer
                && FrankenCorpse.RequirePart<Skills>() is Skills frankenSkills)
            {
                if (!FrankenCorpse.HasSkill(nameof(Survival_Camp)))
                {
                    frankenSkills.AddSkill(nameof(Survival_Camp));
                }
                if (!FrankenCorpse.HasSkill(nameof(Tactics_Run)))
                {
                    frankenSkills.AddSkill(nameof(Tactics_Run));
                }
            }
        }

        public static void GiveInventoryObject(GameObject FrankenCorpse, InventoryObject InventoryObject)
        {
            if (!InventoryObject.Chance.in100())
            {
                return;
            }
            Action<GameObject> beforeObjectCreated = null;
            if (InventoryObject.NeedsToPreconfigureObject())
            {
                beforeObjectCreated = delegate (GameObject GO)
                {
                    InventoryObject.PreconfigureObject(GO);
                };
            }
            GameObjectFactory.ProcessSpecification(
                Blueprint: InventoryObject.Blueprint,
                InventoryObject: InventoryObject,
                Count: InventoryObject.Number.RollCached(),
                BonusModChance: InventoryObject.BoostModChance ? 30 : 0,
                SetModNumber: InventoryObject.SetMods,
                AutoMod: InventoryObject.AutoMod,
                Context: "Initialization",
                BeforeObjectCreated: beforeObjectCreated,
                OwningObject: FrankenCorpse,
                TargetInventory: FrankenCorpse.Inventory);
        }

        public enum TileMappingKeyword
        {
            Override,
            Blueprint,
            Taxon,
            Species,
            Golem,
        }
        public static bool TileMappingTagExistsAndContainsLookup(string ParameterString, out List<string> Parameters, params string[] Lookup)
        {
            Parameters = new();
            return !ParameterString.IsNullOrEmpty()
                && !Lookup.IsNullOrEmpty()
                && !(Parameters = ParameterString.CachedCommaExpansion()).IsNullOrEmpty()
                && Parameters.Count > 0
                && Parameters.Any(s => s.EqualsAny(Lookup));
        }
        public static bool ParseTileMappings(TileMappingKeyword Keyword, out List<string> TileList, params string[] Lookup)
        {
            TileList = new();
            string alternateTileTag = REANIMATED_ALT_TILE_PROPTAG + Keyword + ":";

            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Keyword), Keyword),
                    Debug.Arg(nameof(Lookup), Lookup?.ToList()?.SafeJoin() ?? "empty"),
                });

            if (Keyword == TileMappingKeyword.Override)
            {
                if (Lookup.IsNullOrEmpty())
                {
                    return false; // No tag, so nothing to parse.
                }
                if (Lookup.ToList() is not List<string> valueList
                    || valueList.IsNullOrEmpty())
                {
                    Debug.MetricsManager_LogCallingModError(
                        nameof(ParseTileMappings) + " passed invalid " + 
                        nameof(Lookup) + " for " + 
                        nameof(TileMappingKeyword) + "." + Keyword + ": " + Lookup);
                }
                else
                {
                    TileList.AddRange(valueList);
                }
                return true;
            }
            if (GameObjectFactory.Factory.GetBlueprintIfExists("UD_FleshGolems_TileMappings") is not GameObjectBlueprint tileMappingsModel)
            {
                return false; // No Blueprint, so nothing to parse.
            }
            bool any = false;
            foreach ((string tagName, string tagValue) in tileMappingsModel.Tags)
            {
                bool tileMappingExists = false;
                List<string> tileMappingParameters = new();
                List<string> tagParameterList = new();
                if (tagName.StartsWith(alternateTileTag)
                    && tagName?.Replace(alternateTileTag, "") is string parameterString)
                {
                    if (parameterString.Contains(":"))
                    {
                        tileMappingParameters = alternateTileTag.Split(":").ToList();
                        parameterString = tileMappingParameters[^1];
                        tileMappingParameters.Remove(parameterString);
                    }
                    tileMappingExists = TileMappingTagExistsAndContainsLookup(
                        ParameterString: parameterString
                        Parameters: out tagParameterList,
                        Lookup: Lookup);
                }
                
                any = tileMappingExists || any;

                if (Keyword == TileMappingKeyword.Taxon)
                {
                    Debug.ArgPair[] lookupArgPairs = new Debug.ArgPair[0];
                    Debug.ArgPair[] tileMapParamArgPairs = new Debug.ArgPair[0];
                    Debug.ArgPair[] tagParamArgPairs = new Debug.ArgPair[0];

                    if (!Lookup.IsNullOrEmpty())
                    {
                        List<Debug.ArgPair> lookupArgPairList = new();
                        for (int i = 0; i < Lookup.Length; i++)
                        {
                            lookupArgPairList.Add(Debug.Arg(i.ToString(), Lookup[i]));
                        }
                        lookupArgPairs = lookupArgPairList.ToArray();
                    }
                    if (!tileMappingParameters.IsNullOrEmpty())
                    {
                        List<Debug.ArgPair> tileMapParamArgPairList = new();
                        for (int i = 0; i < tileMappingParameters.Count; i++)
                        {
                            tileMapParamArgPairList.Add(Debug.Arg(i.ToString(), tileMappingParameters[i]));
                        }
                        tileMapParamArgPairs = tileMapParamArgPairList.ToArray();
                    }
                    if (!tagParameterList.IsNullOrEmpty())
                    {
                        List<Debug.ArgPair> tagParamArgPairList = new();
                        for (int i = 0; i < tagParameterList.Count; i++)
                        {
                            tagParamArgPairList.Add(Debug.Arg(i.ToString(), tagParameterList[i]));
                        }
                        tagParamArgPairs = tagParamArgPairList.ToArray();
                    }

                    if (lookupArgPairs.IsNullOrEmpty()
                        || tileMapParamArgPairs.IsNullOrEmpty()
                        || tagParamArgPairs.IsNullOrEmpty())
                    {
                        Debug.LogArgs(Keyword + " " + nameof(Lookup) + " (", ")", indent[1], ArgPairs: lookupArgPairs);
                        Debug.LogArgs(nameof(tileMappingParameters) + " (", ")", indent[1], ArgPairs: tileMapParamArgPairs);
                        Debug.LogArgs(nameof(tagParameterList) + " (", ")", indent[1], ArgPairs: tagParamArgPairs);
                    }

                    if (Lookup.IsNullOrEmpty()
                        || Lookup.Length < 2
                        || tileMappingParameters.IsNullOrEmpty()
                        || tagParameterList.IsNullOrEmpty()
                        || tileMappingParameters[0] != Lookup[0]
                        || Lookup[1].CachedCommaExpansion() is not List<string> lookupParams
                        || lookupParams.Count < 1
                        || !tagParameterList.All(s => !lookupParams.Contains(s)))
                    {
                        continue;
                    }
                }

                if (!tileMappingExists
                    || tagValue.CachedCommaExpansion() is not List<string> valueList
                    || valueList.IsNullOrEmpty())
                {
                    continue;
                }
                TileList.AddRange(valueList);
            }
            return any; // successfully collected results, including none if the tag value was empty (logs warning).
        }

        public static bool CollectProspectiveTiles(ref Dictionary<TileMappingKeyword, List<string>> Dictionary, TileMappingKeyword Keyword, params string[] Lookup)
        {
            Dictionary ??= new()
            {
                { TileMappingKeyword.Override, new() },
                { TileMappingKeyword.Blueprint, new() },
                { TileMappingKeyword.Taxon, new() },
                { TileMappingKeyword.Species, new() },
                { TileMappingKeyword.Golem, new() },
            };
            if (!Dictionary.ContainsKey(Keyword))
            {
                Debug.MetricsManager_LogCallingModError(
                    "Unexpected " + nameof(Keyword) + " supplied to " +
                    nameof(CollectProspectiveTiles) + ": " + Keyword);

                Dictionary.Add(Keyword, new());
            }
            if (!ParseTileMappings(Keyword, out List<string> prospectiveTiles, Lookup))
            {
                return true; // We successfully got 0 results due to absent tag.
            }
            if (prospectiveTiles.IsNullOrEmpty())
            {
                Debug.MetricsManager_LogCallingModError(
                    "Empty " + nameof(prospectiveTiles) + " list parsed by " +
                    nameof(ParseTileMappings) + " for " + nameof(Keyword) + ": " + Keyword);

                return false; // We unsucessfully got any results because tag value was empty.
            }

            Dictionary[Keyword] ??= new();
            Dictionary[Keyword].AddRange(prospectiveTiles);
            return true;
        }

        public static int CalculateMinimumHitpoints(GameObject FrankenCorpse)
        {
            return Stat.RollCached("4d3+5") * UD_FleshGolems_ReanimatedCorpse.GetTierFromLevel(FrankenCorpse);
        }

        public static bool MakeItALIVE(
            GameObject Corpse,
            UD_FleshGolems_PastLife PastLife)
        {
            using Indent indent = new();
            Debug.LogMethod(indent[1],
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Corpse), Corpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(PastLife), PastLife == null ? NULL : ("not " + NULL)),
                });

            if (Corpse is GameObject frankenCorpse
                && frankenCorpse.RequirePart<UD_FleshGolems_CorpseReanimationHelper>() is var reanimationHelper)
            {
                if (frankenCorpse.GetPropertyOrTag("UD_FleshGolems_Reanimator") is string reanimatorFallback)
                {
                    if (int.TryParse(reanimatorFallback, out int reanimatorFallbackID))
                    {
                        reanimationHelper.Reanimator ??= GameObject.FindByID(reanimatorFallbackID.ToString());
                    }
                    else
                    {
                        reanimationHelper.Reanimator ??= GameObject.FindByBlueprint(reanimatorFallback);
                    }
                }
                Debug.Log(nameof(PastLife) + "?." + nameof(PastLife.Blueprint), PastLife?.Blueprint, indent[2]);

                bool proceedWithMakeLive = true;
                try
                {
                    if (PastLife == null || PastLife.Blueprint.IsNullOrEmpty() || PastLife.GetBlueprint() == null)
                    {
                        frankenCorpse.RemovePart<UD_FleshGolems_PastLife>();
                        PastLife = frankenCorpse.RequirePart<UD_FleshGolems_PastLife>().Initialize();
                    }
                    if (PastLife == null || PastLife.Blueprint.IsNullOrEmpty() || PastLife.GetBlueprint() == null)
                    {
                        throw new InvalidOperationException(nameof(UD_FleshGolems_PastLife) + " failed repeatedly to " + nameof(UD_FleshGolems_PastLife.Initialize));
                    }
                }
                catch (Exception x)
                {
                    proceedWithMakeLive = false;
                    frankenCorpse.RemovePart<UD_FleshGolems_PastLife>();
                    MetricsManager.LogException("Failed to Generate a PastLife for " + (frankenCorpse?.DebugName ?? NULL), x, "game_mod_exception");
                }
                if (!proceedWithMakeLive)
                {
                    return false;
                }
                if (!frankenCorpse.FireEvent(Event.New(
                        ID: "UD_FleshGolems_BeforeReanimated",
                        "FrankenCorpse", frankenCorpse,
                        "Reanimator", reanimationHelper.Reanimator,
                        "PastLifePart", PastLife)))
                {
                    return false;
                }

                Dictionary<TileMappingKeyword, List<string>> prospectiveTiles = null;

                CollectProspectiveTiles(
                    Dictionary: ref prospectiveTiles,
                    Keyword: TileMappingKeyword.Override,
                    Lookup: frankenCorpse
                        .GetPropertyOrTag(REANIMATED_TILE_PROPTAG, Default: null)
                        ?.CachedCommaExpansion()
                        ?.ToArray());

                IdentityType identityType = PastLife.GetIdentityType();

                frankenCorpse.SetStringProperty("OverlayColor", "&amp;G^k");

                frankenCorpse.Physics.Organic = PastLife.Physics.Organic;

                Debug.Log("Assigning string and int properties...", Indent: indent[2]);
                if (UD_FleshGolems_Reanimated.HasWorldGenerated)
                {
                    Debug.Log(nameof(PastLife.StringProperties), PastLife?.StringProperties?.Count ?? 0, indent[3]);
                    foreach ((string stringProp, string value) in PastLife?.StringProperties)
                    {
                        frankenCorpse.SetStringProperty(stringProp, value);
                        Debug.Log(stringProp, "\"" + (value ?? "null") + "\"", indent[4]);
                    }
                    Debug.Log(nameof(PastLife.IntProperties), PastLife?.IntProperties?.Count ?? 0, indent[3]);
                    foreach ((string intProp, int value) in PastLife?.IntProperties)
                    {
                        frankenCorpse.SetIntProperty(intProp, value);
                        Debug.Log(intProp, value, indent[4]);
                    }
                }
                else
                {
                    Debug.CheckNah("Skipped.", indent[3]);
                }

                bool wasPlayer = PastLife != null && PastLife.WasPlayer;

                bool excludedFromDynamicEncounters = PastLife.ExcludeFromDynamicEncounters;

                Debug.YehNah(nameof(wasPlayer), wasPlayer, indent[2]);
                Debug.YehNah(nameof(excludedFromDynamicEncounters), excludedFromDynamicEncounters, indent[2]);

                frankenCorpse.SetIntProperty("NoAnimatedNamePrefix", 1);
                frankenCorpse.SetIntProperty("Bleeds", 1);

                frankenCorpse.Render.RenderLayer = 10;

                PastLife.RestoreBrain(excludedFromDynamicEncounters, out Brain frankenBrain);

                bool wantOldIdentity = identityType == IdentityType.Librarian || 50.in100();

                PastLife.RestoreGenderIdentity(WantOldIdentity: wantOldIdentity);

                if (frankenCorpse.GetPropertyOrTag(nameof(CyberneticsButcherableCybernetic)) is string butcherableCyberneticsProp
                    && butcherableCyberneticsProp != null)
                {
                    UD_FleshGolems_HasCyberneticsButcherableCybernetic.EmbedButcherableCyberneticsList(frankenCorpse, butcherableCyberneticsProp);
                }

                string convoID = frankenCorpse.GetPropertyOrTag(REANIMATED_CONVO_ID_TAG);
                if (frankenCorpse.TryGetPart(out ConversationScript convo)
                    && (convo.ConversationID == "NewlySentientBeings" || !convoID.IsNullOrEmpty()))
                {
                    convoID ??= "UD_FleshGolems NewlyReanimatedBeings";
                    convo.ConversationID = convoID;
                }

                Epithets frankenEpithets = null;
                if (frankenCorpse.GetPropertyOrTag(REANIMATED_EPITHETS_TAG) is string frankenEpithetsString)
                {
                    frankenEpithets = frankenCorpse.RequirePart<Epithets>();
                    frankenEpithets.Primary = frankenEpithetsString;
                }

                Description frankenDescription = frankenCorpse.RequirePart<Description>();
                if (frankenDescription != null)
                {
                    if (frankenCorpse.GetPropertyOrTag("UD_FleshGolems_CorpseDescription") is string corpseDescription)
                    {
                        frankenDescription._Short = corpseDescription.StartReplace().AddObject(frankenCorpse).ToString();
                    }
                }

                frankenCorpse.RequireAbilities();

                PastLife.RestoreAnatomy(out Body frankenBody);

                bool taxonomyRestored = PastLife.RestoreTaxonomy();

                PastLife.RestoreCybernetics(out bool installedCybernetics);
                if (!installedCybernetics)
                {
                    ImplantCyberneticsFromAttachedParts(frankenCorpse, out installedCybernetics);
                }

                PastLife.RestoreMutations(out Mutations frankenMutations);
                
                AddPlayerSkillsIfPlayer(frankenCorpse, wasPlayer);
                PastLife.RestoreSkills(out Skills frankenSkills);

                if (frankenCorpse.GetBlueprint().Tags.ContainsKey(nameof(Gender)))
                {
                    frankenCorpse.SetGender(frankenCorpse.GetBlueprint().Tags[nameof(Gender)]);
                }
                if (frankenCorpse.GetBlueprint().Tags.ContainsKey(nameof(PronounSet)))
                {
                    frankenCorpse.SetPronounSet(frankenCorpse.GetBlueprint().Tags[nameof(PronounSet)]);
                }

                string bleedLiquid = PastLife?.BleedLiquid;

                int guaranteedNaturalWeapons = 3;
                int bestowedNaturalWeapons = 0;
                bool guaranteeNaturalWeapon() => guaranteedNaturalWeapons >= bestowedNaturalWeapons;
                Debug.Log("Getting SourceBlueprint...", Indent: indent[2]);
                if (GameObjectFactory.Factory.GetBlueprintIfExists(PastLife?.Blueprint) is GameObjectBlueprint sourceBlueprint)
                {
                    Debug.CheckYeh(nameof(sourceBlueprint), sourceBlueprint.Name, Indent: indent[3]);
                    

                    CollectProspectiveTiles(
                        Dictionary: ref prospectiveTiles,
                        Keyword: TileMappingKeyword.Blueprint,
                        Lookup: sourceBlueprint.Name);

                    if (sourceBlueprint.xTags.TryGetValue(REANIMATED_TAXA_XTAG, out Dictionary<string, string> sourceTaxa))
                    {
                        foreach ((string taxonLabel, string taxon) in sourceTaxa)
                        {
                            CollectProspectiveTiles(
                                Dictionary: ref prospectiveTiles,
                                Keyword: TileMappingKeyword.Taxon,
                                Lookup: new string[] { taxonLabel, taxon, });
                        }
                    }

                    PastLife.RestoreFactionRelationships();
                    PastLife.RestoreSelectPropTags();

                    bool isProblemPartOrFollowerPartOrPartAlreadyHave(IPart p)
                    {
                        return IPartsToSkipWhenReanimating.Contains(p.Name)
                            || frankenCorpse.HasPart(p.Name)
                            || (frankenCorpse.GetPropertyOrTag(REANIMATED_PART_EXCLUSIONS_PROPTAG) is string propertyPartExclusions
                                && propertyPartExclusions.CachedCommaExpansion() is List<string> partExclusionsList
                                && partExclusionsList.Contains(p.Name));
                    }

                    _ = indent[2];

                    AssignStatsFromBlueprint(frankenCorpse, sourceBlueprint, HitpointsFallbackToMinimum: true);

                    /*
                    Debug.Log("Pre-" + nameof(frankenCorpse.FinalizeStats) + " figures...", Indent: indent[2]);
                    foreach ((string statName, Statistic stat) in frankenCorpse?.Statistics)
                    {
                        Debug.Log(statName, stat.Value + "/" + stat.BaseValue + " | " + (stat.sValue ?? "no sValue"), indent[3]);
                    }
                    */
                    Debug.CheckYeh(nameof(frankenCorpse.FinalizeStats), indent[2]);
                    frankenCorpse?.FinalizeStats();

                    _ = indent[2];

                    float physicalAdjustmentFactor = 1.2f; // wasPlayer ? 1.0f : 1.2f;
                    float mentalAdjustmentFactor = 0.80f; // wasPlayer ? 1.0f : 0.80f;
                    AssignStatsFromPastLifeWithFactor(
                        FrankenCorpse: frankenCorpse,
                        PastLife: PastLife,
                        PhysicalAdjustmentFactor: physicalAdjustmentFactor,
                        MentalAdjustmentFactor: mentalAdjustmentFactor,
                        HitpointsFallbackToMinimum: true);

                    AssignPartsFromBlueprint(frankenCorpse, sourceBlueprint, Exclude: isProblemPartOrFollowerPartOrPartAlreadyHave);

                    AssignMutationsFromBlueprint(frankenMutations, sourceBlueprint);

                    AssignSkillsFromBlueprint(frankenSkills, sourceBlueprint);

                    excludedFromDynamicEncounters = PastLife != null
                        && !PastLife.Tags.IsNullOrEmpty()
                        && PastLife.Tags.ContainsKey("ExcludeFromDynamicEncounters");

                    Debug.Log("Adding Levels and XP...", Indent: indent[2]);
                    if (frankenCorpse.TryGetPart(out Leveler frankenLeveler))
                    {
                        if (int.TryParse(frankenCorpse.GetPropertyOrTag("UD_FleshGolems_SkipLevelsOnReanimate", "0"), out int SkipLevelsOnReanimate)
                            && SkipLevelsOnReanimate < 1)
                        {
                            Debug.CheckYeh(nameof(frankenLeveler.LevelUp), indent[3]);
                            frankenLeveler?.LevelUp();
                            if (Stat.RollCached("1d2") == 1)
                            {
                                Debug.CheckYeh(nameof(frankenLeveler.LevelUp) + " again", indent[3]);
                                frankenLeveler?.LevelUp();
                            }
                        }
                        int floorXP = Leveler.GetXPForLevel(frankenCorpse.Level);
                        int ceilingXP = Leveler.GetXPForLevel(frankenCorpse.Level + 1);
                        frankenCorpse.GetStat("XP").BaseValue = Stat.RandomCosmetic(floorXP, ceilingXP);
                        Debug.Log("XP set", frankenCorpse.GetStat("XP").BaseValue, indent[3]);
                    }

                    if ((!taxonomyRestored || frankenCorpse.GetStringProperty("Species") is null)
                        && sourceBlueprint.TryGetStringPropertyOrTag("Species", out string sourceSpecies))
                    {
                        frankenCorpse.SetStringProperty("Species", sourceSpecies);
                        Debug.CheckYeh("Species given from sourceBlueprint", frankenCorpse.GetSpecies(), indent[2]);
                    }

                    CollectProspectiveTiles(
                        Dictionary: ref prospectiveTiles,
                        Keyword: TileMappingKeyword.Species,
                        Lookup: sourceBlueprint.GetPropertyOrTag("Species"));

                    if (sourceBlueprint.GetPropertyOrTag(REANIMATED_CONVO_ID_TAG) is string sourceCreatureConvoID
                        && convo != null)
                    {
                        convo.ConversationID = sourceCreatureConvoID;
                    }

                    if (sourceBlueprint.GetPropertyOrTag(REANIMATED_EPITHETS_TAG) is string sourceCreatureEpithets)
                    {
                        frankenEpithets = frankenCorpse.RequirePart<Epithets>();
                        frankenEpithets.Primary = sourceCreatureEpithets;
                    }

                    if (bleedLiquid.IsNullOrEmpty()
                        && sourceBlueprint.Tags.ContainsKey(nameof(bleedLiquid)))
                    {
                        bleedLiquid = sourceBlueprint.Tags[nameof(bleedLiquid)];
                    }

                    if (frankenCorpse.GetPropertyOrTag("KillerID") is string killerID
                        && GameObject.FindByID(killerID) is GameObject killer)
                    {
                        frankenCorpse.GetPropertyOrTag("KillerName", killer.GetReferenceDisplayName(Short: true));
                    }

                    if (sourceBlueprint.TryGetPartParameter(nameof(Physics), nameof(Physics.Weight), out int sourceWeight))
                    {
                        frankenCorpse.Physics.Weight = sourceWeight;
                        frankenCorpse.FlushWeightCaches();
                    }

                    if (sourceBlueprint.TryGetPartParameter(nameof(Body), nameof(Body.Anatomy), out string sourceAnatomy))
                    {
                        if (frankenCorpse.Body == null)
                        {
                            frankenCorpse.AddPart(new Body()).Anatomy = sourceAnatomy;
                        }
                        else
                        {
                            frankenCorpse.Body.Rebuild(sourceAnatomy);
                        }
                    }

                    Debug.Log("Getting Golem Anatomy...", Indent: indent[2]);
                    if (GolemBodySelection.GetBodyBlueprintFor(sourceBlueprint) is GameObjectBlueprint golemBodyBlueprint)
                    {
                        Debug.Log(nameof(golemBodyBlueprint), golemBodyBlueprint.Name, indent[3]);
                        if (golemBodyBlueprint.TryGetPartParameter(nameof(Body), nameof(Body.Anatomy), out string golemAnatomy))
                        {
                            if (frankenCorpse.Body == null)
                            {
                                frankenCorpse.AddPart(new Body()).Anatomy = golemAnatomy;
                            }
                            else
                            {
                                frankenCorpse.Body.Rebuild(golemAnatomy);
                            }
                        }

                        string golemSpecies = golemBodyBlueprint.Name.Replace(" Golem", "");
                        CollectProspectiveTiles(
                            Dictionary: ref prospectiveTiles,
                            Keyword: TileMappingKeyword.Golem,
                            Lookup: golemSpecies);

                        if (prospectiveTiles.ContainsKey(TileMappingKeyword.Golem)
                            && prospectiveTiles[TileMappingKeyword.Golem].IsNullOrEmpty()
                            && golemBodyBlueprint.TryGetPartParameter(nameof(Parts.Render), nameof(Parts.Render.Tile), out string golemTile))
                        {
                            prospectiveTiles[TileMappingKeyword.Golem].Add(golemTile);
                            MetricsManager.LogModWarning(ThisMod,
                                "No custom Flesh Golem tile for " + nameof(golemSpecies) + ": " + 
                                golemSpecies + "; Fallback tile used: \"" + golemTile + "\"");
                        }

                        bool giganticIfNotAlready(BaseMutation BM)
                        {
                            return !frankenMutations.HasMutation(BM)
                                && BM.GetMutationClass() == "GigantismPlus";
                        }
                        // AssignStatsFromBlueprint(frankenCorpse, golemBodyBlueprint);
                        AssignMutationsFromBlueprint(frankenMutations, golemBodyBlueprint, Exclude: giganticIfNotAlready);
                        AssignSkillsFromBlueprint(frankenSkills, golemBodyBlueprint);
                    }

                    bool generateInventory = frankenCorpse.HasPropertyOrTag(REANIMATED_GEN_SOURCE_INV_PROPTAG);

                    Debug.Log("Granting SourceBlueprint Natural Equipment...", Indent: indent[2]);
                    Debug.Log(sourceBlueprint.Name, nameof(sourceBlueprint.Inventory), indent[3]);
                    if (sourceBlueprint.Inventory != null)
                    {
                        foreach (InventoryObject inventoryObject in sourceBlueprint.Inventory)
                        {
                            string itemLabel = (inventoryObject?.Blueprint ?? NULL) + " | ";
                            if (GameObjectFactory.Factory.GetBlueprintIfExists(inventoryObject.Blueprint) is GameObjectBlueprint inventoryObjectBlueprint
                                && inventoryObjectBlueprint.IsNatural())
                            {
                                if (GameObject.CreateSample(inventoryObjectBlueprint.Name) is GameObject sampleNaturalGear
                                    && sampleNaturalGear.EquipAsDefaultBehavior())
                                {
                                    if (sampleNaturalGear.TryGetPart(out MeleeWeapon mw)
                                        && !mw.IsImprovisedWeapon()
                                        && GetRaggedNaturalWeapons(bp => MeleeWeaponSlotAndSkillMatchesBlueprint(bp, mw))?.GetRandomElement()?.Name is string raggedWeaponBlueprintName)
                                    {
                                        if (GameObject.CreateUnmodified(raggedWeaponBlueprintName) is GameObject raggedWeaponObject)
                                        {
                                            string debugOutput =
                                                nameof(sampleNaturalGear) + ": " + sampleNaturalGear.DebugName + " | " +
                                                nameof(raggedWeaponObject) + ": " + raggedWeaponObject.DebugName;
                                            bool anyAssigned = false;
                                            List<string> weaponSlots = mw.Slot.CachedCommaExpansion().ShuffleInPlace();
                                            foreach (string weaponSlot in weaponSlots)
                                            {
                                                if (frankenCorpse.Body.GetFirstPart(mw.Slot, bp => bp.DefaultBehavior == null) is BodyPart bodyPart
                                                    && bodyPart.AssignDefaultBehaviour(raggedWeaponObject, SetDefaultBehaviorBlueprint: true))
                                                {
                                                    if (raggedWeaponObject.TryGetPart(out RaggedNaturalWeapon raggedWeaponPart))
                                                    {
                                                        raggedWeaponPart.Wielder = frankenCorpse;
                                                    }
                                                    bestowedNaturalWeapons++;

                                                    Debug.CheckYeh(itemLabel + debugOutput, Indent: indent[4]);
                                                    anyAssigned = true;
                                                    break;
                                                }
                                            }
                                            if (!anyAssigned
                                                && GameObject.Validate(ref raggedWeaponObject))
                                            {
                                                Debug.CheckNah(itemLabel + debugOutput, Indent: indent[4]);
                                                raggedWeaponObject.Obliterate();
                                            }
                                        }
                                    }
                                    if (GameObject.Validate(ref sampleNaturalGear))
                                    {
                                        sampleNaturalGear?.Obliterate();
                                    }
                                }
                            }
                            else
                            {
                                Debug.CheckNah(itemLabel + "Not Natural'", Indent: indent[4]);
                            }
                        }
                    }

                    Debug.Log("Granting SourceBlueprint Inventory...", Indent: indent[2]);
                    if (generateInventory)
                    {
                        if (GameObject.CreateSample(sourceBlueprint.Name) is GameObject sampleSourceEntity)
                        {
                            Debug.CheckYeh("Inventory Generated", Indent: indent[3]);
                            if (UD_FleshGolems_Reanimated.TryTransferInventoryToCorpse(sampleSourceEntity, Corpse))
                            {
                                Debug.CheckYeh("Transferred", Indent: indent[3]);
                            }
                            else
                            {
                                Debug.CheckNah("Transfer Failed", Indent: indent[3]);
                            }
                        }
                    }
                    else
                    {
                        Debug.CheckNah("Skipped", Indent: indent[3]);
                    }
                }
                _ = indent[1];
                PastLife?.RestoreAdditionalLimbs();

                frankenBody ??= frankenCorpse.Body;
                Debug.Log("Granting Additional Natural Equipment...", Indent: indent[2]);
                Debug.Log(nameof(frankenBody), nameof(frankenBody.LoopParts), indent[3]);
                List<BodyPart> frankenLimbs = new(frankenBody.LoopParts());
                frankenLimbs.ShuffleInPlace();
                foreach (BodyPart frankenLimb in frankenBody.LoopParts())
                {
                    string limbLabel = frankenLimb.Type + " | ";
                    if (frankenLimb.DefaultBehavior is GameObject frankenNaturalGear
                        && frankenNaturalGear.GetBlueprint() is GameObjectBlueprint frankenNaturalGearBlueprint
                        && !frankenNaturalGearBlueprint.InheritsFrom("UD_FleshGolems Ragged Weapon"))
                    {
                        if (frankenNaturalGear.TryGetPart(out MeleeWeapon mw)
                            && !mw.IsImprovisedWeapon()
                            && GetRaggedNaturalWeapons(bp => MeleeWeaponSlotAndSkillMatchesBlueprint(bp, mw))?.GetRandomElement()?.Name is string raggedWeaponBlueprintName
                            && GameObject.CreateUnmodified(raggedWeaponBlueprintName) is GameObject raggedWeaponObject
                            && frankenLimb.AssignDefaultBehaviour(raggedWeaponObject, SetDefaultBehaviorBlueprint: true))
                        {
                            if (raggedWeaponObject.TryGetPart(out RaggedNaturalWeapon raggedWeaponPart))
                            {
                                raggedWeaponPart.Wielder = frankenCorpse;
                            }
                            bestowedNaturalWeapons++;

                            string debugOutput =
                                nameof(frankenNaturalGear) + ": " + (frankenNaturalGear?.DebugName ?? NULL) + " | " +
                                nameof(raggedWeaponObject) + ": " + (raggedWeaponObject.DebugName ?? NULL);
                            Debug.CheckYeh(limbLabel + debugOutput, Indent: indent[4]);

                            if (GameObject.Validate(ref frankenNaturalGear))
                            {
                                frankenNaturalGear?.Obliterate();
                            }
                        }
                    }
                    else
                    if ((guaranteeNaturalWeapon() || 25.in100())
                        && GetRaggedNaturalWeapons(bp => MeleeWeaponSlotMatchesBlueprint(bp, frankenLimb.Type))?.GetRandomElement()?.Name is string raggedWeaponBlueprintName
                        && GameObject.CreateUnmodified(raggedWeaponBlueprintName) is GameObject raggedWeaponObject
                        && frankenLimb.AssignDefaultBehaviour(raggedWeaponObject, SetDefaultBehaviorBlueprint: true))
                    {
                        if (raggedWeaponObject.TryGetPart(out RaggedNaturalWeapon raggedWeaponPart))
                        {
                            raggedWeaponPart.Wielder = frankenCorpse;
                        }
                        bestowedNaturalWeapons++;

                        string debugOutput = "Rolled 1 in 4 | " + nameof(raggedWeaponObject) + ": " + (raggedWeaponObject?.DebugName ?? NULL);
                        Debug.CheckYeh(limbLabel + debugOutput, Indent: indent[4]);
                    }
                    else
                    {
                        Debug.CheckNah(limbLabel + "nuffin'", Indent: indent[4]);
                    }
                }

                Debug.Log("Getting New Tile!", Indent: indent[2]);
                string chosenTile = null;
                foreach (TileMappingKeyword keyword in GetEnumValues<TileMappingKeyword>())
                {
                    if (prospectiveTiles.IsNullOrEmpty()
                        || !prospectiveTiles.ContainsKey(keyword)
                        || prospectiveTiles[keyword].IsNullOrEmpty())
                    {
                        Debug.CheckNah(keyword + "'s TileMapping Is Empty!", indent[3]);
                        continue;
                    }
                    chosenTile = prospectiveTiles[keyword].GetRandomElementCosmetic();

                    Debug.CheckYeh(keyword + "'s " + nameof(chosenTile) + ": " + chosenTile, indent[3]);
                    break;
                }

                if (chosenTile != null)
                {
                    frankenCorpse.Render.Tile = chosenTile;
                    Debug.Log("Tile changed", "\"" + chosenTile + "\"", indent[2]);
                }
                else
                {
                    Debug.Log("Uh-oh! Something went wrong!", Indent: indent[3]);
                    Debug.Log("Changing tile to the PastLife Tile", Indent: indent[3]);
                    if (PastLife?.PastRender?.Tile is string pastTile)
                    {
                        Debug.Log("Tile changed", "\"" + pastTile + "\"", indent[2]);
                        frankenCorpse.Render.Tile = pastTile;
                    }
                    else
                    {
                        Debug.Log("Uh-oh! Something went wrong again!", Indent: indent[4]);
                        Debug.Log("Changing tile to the sourceBlueprint Tile", Indent: indent[4]);
                        sourceBlueprint = PastLife.GetBlueprint();
                        if (sourceBlueprint != null
                            && sourceBlueprint.TryGetPartParameter(nameof(Parts.Render), nameof(Parts.Render.Tile), out string sourceTile))
                        {
                            Debug.Log("Tile changed", "\"" + sourceTile + "\"", indent[2]);
                            frankenCorpse.Render.Tile = sourceTile;
                        }
                    }
                }
                if (frankenCorpse.HasTagOrStringProperty(REANIMATED_FLIP_COLORS_PROPTAG))
                {
                    string newTileColor = "&" + frankenCorpse.Render.DetailColor;
                    string newDetailColor = frankenCorpse.Render.TileColor;

                    frankenCorpse.Render.TileColor = newTileColor;
                    frankenCorpse.Render.ColorString = newTileColor;
                    frankenCorpse.Render.DetailColor = newDetailColor[^1].ToString();
                }
                if (frankenCorpse.GetPropertyOrTag(REANIMATED_SET_COLORS_PROPTAG) is string setColorsPropTag
                    && setColorsPropTag.CachedDictionaryExpansion() is Dictionary<string, string> setColors)
                {
                    if (setColors.ContainsKey("TileColor"))
                    {
                        string newTileColor = "&" + setColors["TileColor"][^1];
                        frankenCorpse.Render.TileColor = newTileColor;
                        frankenCorpse.Render.ColorString = newTileColor;
                    }
                    if (setColors.ContainsKey("DetailColor"))
                    {
                        frankenCorpse.Render.DetailColor = setColors["DetailColor"][^1].ToString();
                    }
                }

                frankenMutations = frankenCorpse.RequirePart<Mutations>();
                bool giveRegen = true;
                if (giveRegen
                    && GetMutationClassByName("Regeneration") is string regenerationMutationClass)
                {
                    if (frankenMutations.GetMutation(regenerationMutationClass) is not BaseMutation regenerationMutation)
                    {
                        frankenMutations.AddMutation(regenerationMutationClass, Level: 10);
                        regenerationMutation = frankenMutations.GetMutation(regenerationMutationClass);
                    }
                    regenerationMutation.CapOverride.SetMinValue(5);

                    if (regenerationMutation.BaseLevel < 10)
                    {
                        regenerationMutation.ChangeLevel(10);
                    }
                }
                string nightVisionMutaitonName = "Night Vision";
                string darkVisionMutationName = "Dark Vision";
                MutationEntry nightVisionEntry = MutationFactory.GetMutationEntryByName(nightVisionMutaitonName);
                MutationEntry darkVisionEntry = MutationFactory.GetMutationEntryByName(darkVisionMutationName);
                if (!frankenMutations.HasMutation(nightVisionEntry.Class) && !frankenMutations.HasMutation(darkVisionEntry.Class))
                {
                    frankenMutations.AddMutation(darkVisionEntry.Class, 8);
                    if (frankenMutations.GetMutation(darkVisionEntry.Class) is BaseMutation darkVisionMutation)
                    {
                        darkVisionMutation.CapOverride.SetMinValue(8);
                    }
                }

                if (bleedLiquid.IsNullOrEmpty())
                {
                    bleedLiquid = RaggedNaturalWeapon.DetermineTaxonomyAdjective(frankenCorpse) switch
                    {
                        Taxonomy.Jagged => "oil-1000",
                        Taxonomy.Fettid => "sap-1000",
                        Taxonomy.Decayed => "proteangunk-1000",
                        _ => "blood-1000",
                    };
                }
                if (!bleedLiquid.IsNullOrEmpty())
                {
                    frankenCorpse.SetBleedLiquid(bleedLiquid);
                }

                if (!UD_FleshGolems_Reanimated.HasWorldGenerated || excludedFromDynamicEncounters)
                {
                    if (PastLife?.ConversationScriptID is string pastConversationID)
                    {
                        convo.ConversationID = pastConversationID;
                    }
                }

                if (!frankenCorpse.IsPlayer() && frankenCorpse?.CurrentCell is Cell frankenCell)
                {
                    bool isItemThatNotSelf(GameObject GO)
                    {
                        return GO.GetBlueprint().InheritsFrom("Item")
                            && GO != frankenCorpse;
                    }
                    frankenCorpse.TakeObject(Event.NewGameObjectList(frankenCell.GetObjects(isItemThatNotSelf)));
                    frankenCorpse.Brain?.WantToReequip();

                    if (frankenCorpse.TryGetPart(out GenericInventoryRestocker frankenRestocker))
                    {
                        if (!UD_FleshGolems_Reanimated.HasWorldGenerated)
                        {
                            frankenRestocker.PerformRestock();
                            frankenRestocker.LastRestockTick = 0L;
                        }
                        else
                        {
                            frankenRestocker.LastRestockTick = The.Game.TimeTicks;
                        }
                    }

                    frankenCell?.RefreshMinimapColor();
                }

                if (frankenCorpse.GetStat("Hitpoints") is Statistic frankenHitpoints)
                {
                    int minHitpoints = CalculateMinimumHitpoints(frankenCorpse);
                    frankenHitpoints.BaseValue = Math.Max(minHitpoints, frankenHitpoints.BaseValue);
                    frankenHitpoints.Penalty = 0;
                    Debug.Log(nameof(frankenHitpoints), frankenHitpoints.Value + "/" + frankenHitpoints.BaseValue + "(min: " + minHitpoints + ")", Indent: indent[2]);
                }

                frankenBrain?.WantToReequip();

                var reanimatedCorpse = frankenCorpse.RequirePart<UD_FleshGolems_ReanimatedCorpse>();
                if (GameObject.Validate(ref reanimationHelper.Reanimator))
                {
                    reanimatedCorpse.Reanimator = reanimationHelper.Reanimator;
                }

                Debug.Log(
                    nameof(reanimatedCorpse) + "." + 
                    nameof(reanimatedCorpse.Reanimator) + " set", 
                    reanimatedCorpse?.Reanimator?.DebugName ?? NULL, 
                    indent[1]);

                Debug.Log("Calling " + nameof(frankenBody) + "." + nameof(frankenBody.UpdateBodyParts), Indent: indent[2]);
                frankenBody?.UpdateBodyParts();
                Debug.CheckYeh("Didn't fail, fortuantely!", indent[3]);

                frankenCorpse.RequirePart<Corpse>().CorpseChance = 100;

                if (frankenCorpse.TryGetIntProperty("Hero", out int frankenHero)
                    && frankenHero > 0
                    && identityType > IdentityType.ParticipantVillager)
                {
                    frankenCorpse.SetIntProperty("Hero", 0, RemoveIfZero: true);
                    HeroMaker.MakeHero(frankenCorpse);
                }
                else
                if (frankenCorpse.TryGetPart(out GivesRep frankenRep))
                {
                    frankenRep.FillInRelatedFactions(true);
                }

                frankenCorpse.FireEvent(Event.New(
                    ID: "UD_FleshGolems_Reanimated",
                    "FrankenCorpse", frankenCorpse,
                    "Reanimator", reanimationHelper.Reanimator,
                    "PastLifePart", PastLife));

                return true;
            }
            return false;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            base.Register(Object, Registrar);
            try
            {
                Registrar?.Register(DroppedEvent.ID, EventOrder.EXTREMELY_EARLY);
                Registrar?.Register("ObjectExtracted");
            }
            catch (Exception x)
            {
                MetricsManager.LogException(Name + "." + nameof(Register), x, "game_mod_exception");
            }
            finally
            {
                if (ParentObject == null
                    || ParentObject.RegisteredEvents == null
                    || !ParentObject.RegisteredEvents.ContainsKey(DroppedEvent.ID))
                {
                    FailedToRegisterEvents.Add(DroppedEvent.ID);
                }
            }
        }
        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == BeforeObjectCreatedEvent.ID
            || ID == AnimateEvent.ID
            || ID == EnvironmentalUpdateEvent.ID
            || (ID == DroppedEvent.ID && FailedToRegisterEvents.Contains(DroppedEvent.ID))
            || ID == GetDebugInternalsEvent.ID;

        public override bool HandleEvent(BeforeObjectCreatedEvent E)
        {
            if (!IsALIVE
                && ParentObject == E.Object
                && false)
            {
                using Indent indent = new();
                Debug.LogCaller(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(BeforeObjectCreatedEvent)),
                        Debug.Arg(nameof(IsALIVE), IsALIVE),
                    });
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AnimateEvent E)
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(AnimateEvent)),
                    Debug.Arg(nameof(E.Object), E.Object?.DebugName ?? NULL),
                    Debug.Arg(nameof(IsALIVE), IsALIVE),
                });

            if (!IsALIVE
                && ParentObject == E.Object)
            {
                if (!E.Object.TryGetPart(out UD_FleshGolems_PastLife pastLife))
                {
                    pastLife = E.Object.RequirePart<UD_FleshGolems_PastLife>().Initialize();
                }
                Reanimator = E.Actor;
                IsALIVE = true;
                MakeItALIVE(E.Object, pastLife);
                Debug.Log(nameof(MakeItALIVE) + " resolved", Indent: indent[1]);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnvironmentalUpdateEvent E)
        {
            if (AlwaysAnimate
                && !IsALIVE
                && ParentObject != null
                && Animate(out GameObject corpse))
            {
                using Indent indent = new();
                Debug.LogCaller(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(EnvironmentalUpdateEvent)),
                        Debug.Arg(nameof(IsALIVE), IsALIVE),
                    });
                if (!UD_FleshGolems_Reanimated.HasWorldGenerated)
                {
                    ProcessMoveToDeathCell(corpse, PastLife);
                }
                return true;
            }
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (E.ID == "ObjectExtracted"
                && E.GetGameObjectParameter("Source") is GameObject extractionSource
                && extractionSource.TryGetPart(out UD_FleshGolems_PastLife pastLife)
                && PastLife == null)
            {
                using Indent indent = new();
                Debug.LogCaller(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(Event)),
                        Debug.Arg(nameof(E.ID), E.ID),
                        Debug.Arg(nameof(extractionSource), extractionSource?.DebugName ?? null),
                    });
                extractionSource.RemovePart(pastLife);
                ParentObject.AddPart(pastLife);
            }
            return base.FireEvent(E);
        }
        public override bool HandleEvent(DroppedEvent E)
        {
            if (E.Item is GameObject corpse
                && corpse == ParentObject
                && E.Actor is GameObject dying
                && dying.IsDying)
            {
                using Indent indent = new();
                Debug.LogCaller(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(DroppedEvent)),
                        Debug.Arg(nameof(corpse), corpse?.DebugName ?? null),
                        Debug.Arg(nameof(dying), dying?.DebugName ?? null),
                    });
                corpse.RequirePart<UD_FleshGolems_PastLife>().Initialize(dying);
            }
            if (AlwaysAnimate
                && !IsALIVE
                && ParentObject != null
                && Animate())
            {
                using Indent indent = new();
                Debug.LogCaller(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(DroppedEvent)),
                        Debug.Arg(nameof(AlwaysAnimate), AlwaysAnimate),
                        Debug.Arg(nameof(ParentObject), ParentObject?.DebugName ?? null),
                    });
                return true;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            E.AddEntry(this, nameof(IsALIVE), IsALIVE);
            E.AddEntry(this, nameof(AlwaysAnimate), AlwaysAnimate);
            E.AddEntry(this, nameof(Reanimator), Reanimator?.DebugName ?? NULL);
            if (!FailedToRegisterEvents.IsNullOrEmpty())
            {
                E.AddEntry(this, nameof(FailedToRegisterEvents),
                    FailedToRegisterEvents
                    ?.ConvertAll(id => MinEvent.EventTypes.ContainsKey(id) ? MinEvent.EventTypes[id].ToString() : "Error")
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            }
            else
            {
                E.AddEntry(this, nameof(FailedToRegisterEvents), "Empty");
            }
            return base.HandleEvent(E);
        }
    }
}
