using System;
using System.Collections.Generic;
using System.Text;

using Genkit;
using XRL.Core;
using XRL.Language;
using XRL.Rules;
using XRL.World.Anatomy;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.ObjectBuilders;
using XRL.World.Quests.GolemQuest;
using XRL.World.Skills;
using XRL.World.AI;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_CorpseReanimationHelper : IScribedPart
    {
        public const string REANIMATED_CONVO_ID_TAG = "UD_FleshGolems_ReanimatedConversationID";
        public const string REANIMATED_EPITHETS_TAG = "UD_FleshGolems_ReanimatedEpithets";


        public const string CYBERNETICS_LICENSES = "CyberneticsLicenses";
        public const string CYBERNETICS_LICENSES_FREE = "FreeCyberneticsLicenses";

        public UD_FleshGolems_PastLife PastLife => ParentObject?.GetPart<UD_FleshGolems_PastLife>();

        public static List<string> PhysicalStats => new() { "Strength", "Agility", "Toughness", };
        public static List<string> MentalStats => new() { "Intelligence", "Willpower", "Ego", };

        public static List<string> IPartsToSkipWhenReanimating => new()
        {
            nameof(Titles),
            nameof(Epithets),
            nameof(Honorifics),
            nameof(Lovely),
            nameof(SecretObject),
            nameof(ConvertSpawner),
            nameof(Leader),
            nameof(Followers),
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

        public string CreatureName;

        public string SourceBlueprint;

        public string CorpseDescription;

        [SerializeField]
        private string ReanimatorID;

        private GameObject _Reanimator;
        public GameObject Reanimator
        {
            get => _Reanimator ??= GameObject.FindByID(ReanimatorID);
            set
            {
                ReanimatorID = value?.ID;
                _Reanimator = value;
            }
        }

        private List<int> FailedToRegisterEvents;

        public UD_FleshGolems_CorpseReanimationHelper()
        {
            IsALIVE = false;
            AlwaysAnimate = false;
            CreatureName = null;
            SourceBlueprint = null;
            CorpseDescription = null;
            FailedToRegisterEvents = new();
        }

        public override bool AllowStaticRegistration()
        {
            return true;
        }

        public bool Animate(out GameObject FrankenCorpse)
        {
            FrankenCorpse = null;
            UnityEngine.Debug.Log(nameof(UD_FleshGolems_CorpseReanimationHelper) + "." + nameof(Animate));
            if (ParentObject != null && !ParentObject.HasPart<AnimatedObject>())
            {
                UnityEngine.Debug.Log("    " + nameof(ParentObject) + " not " + nameof(AnimatedObject));
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

        public static string FigureOutWhatBlueprintThisCorpseCameFrom(GameObject Corpse, UD_FleshGolems_PastLife PastLife = null, bool PrintCheckEvenWhenPastLife = false)
        {
            Debug.GetIndents(out Indents indent);
            string blueprint = null;
            if (PastLife?.Blueprint is string pastLifeBlueprint)
            {
                if (!PrintCheckEvenWhenPastLife)
                {
                    return pastLifeBlueprint;
                }
                blueprint = pastLifeBlueprint;
            }
            if (Corpse.GetPropertyOrTag("UD_FleshGolems_PastLife_Blueprint") is string pastLifeBlueprintTagOrProp)
            {
                if (!PrintCheckEvenWhenPastLife)
                {
                    return pastLifeBlueprintTagOrProp;
                }
                blueprint = pastLifeBlueprintTagOrProp;
            }
            string corpseDisplayNameLC = Corpse.GetReferenceDisplayName(Stripped: true, Short: true)?.ToLower();
            string baseGameSourceBlueprintLC = Corpse.GetPropertyOrTag("SourceBlueprint")?.ToLower();
            string corpseType = Corpse.Blueprint.Replace(" Corpse", "").Replace("UD_FleshGolems ", "");
            string corpseSpecies = Corpse.GetStringProperty("Species", corpseType);
            List<string> probableBlueprints = new();
            List<string> possibleBlueprints = new();
            List<string> fallbackBlueprints = new();
            Debug.Log(
                "Finding Blueprints for " + (Corpse?.DebugName ?? NULL) + " | " +
                nameof(corpseDisplayNameLC) + ": " + (corpseDisplayNameLC ?? NULL) + ", " +
                nameof(baseGameSourceBlueprintLC) + ": " + (baseGameSourceBlueprintLC ?? NULL) + ", " +
                nameof(corpseType) + ": " + (corpseType ?? NULL) + ", " +
                nameof(corpseSpecies) + ": " + (corpseSpecies ?? NULL) + ", " +
                nameof(corpseDisplayNameLC) + ": " + (corpseDisplayNameLC ?? NULL), indent);
            List<GameObjectBlueprint> blueprintsToCheck = new(GameObjectFactory.Factory.Blueprints.Values);
            blueprintsToCheck.ShuffleInPlace();
            int maxChecks = int.MaxValue; // 2500;
            int checkCounter = 0;
            Debug.Log("Checking " + (maxChecks == int.MaxValue ? blueprintsToCheck.Count : maxChecks) + "/" + blueprintsToCheck.Count, indent[1]);
            foreach (GameObjectBlueprint gameObjectBlueprint in blueprintsToCheck)
            {
                if (++checkCounter > maxChecks)
                {
                    break;
                }
                string blueprintName = gameObjectBlueprint.Name;
                string blueprintNameLC = blueprintName?.ToLower() ?? "XKCD";
                string gameObjectDisplayName = gameObjectBlueprint.DisplayName()?.Strip()?.ToLower() ?? "XKCD";
                if (blueprintName == Corpse.Blueprint)
                {
                    // Debug.Log(AppendCross("    ") + blueprintName + " is this corpse", indent[2]);
                    continue;
                }
                if (gameObjectBlueprint.IsBaseBlueprint())
                {

                    // Debug.Log(AppendCross("    ") + blueprintName + " is base blueprint", indent[2]);
                    continue;
                }
                if (!gameObjectBlueprint.InheritsFrom("Creature")
                    && !gameObjectBlueprint.InheritsFrom("Fungus")
                    && !gameObjectBlueprint.InheritsFrom("Plant")
                    && !gameObjectBlueprint.InheritsFrom("Corpse")
                    && !gameObjectBlueprint.InheritsFrom("Robot"))
                {
                    // Debug.Log(AppendCross("    ") + blueprintName + " is not an entity capable of dying", indent[2]);
                    continue;
                }
                if (BlueprintsToSkipCheckingForCorpses.Contains(gameObjectBlueprint.Name))
                {
                    // Debug.Log(AppendCross("    ") + blueprintName + " is definitely not the correct one.", indent[2]);
                    continue;
                }
                bool corpseDisplayNameContainsBlueprintDisplayName = corpseDisplayNameLC != null && corpseDisplayNameLC.Contains(gameObjectDisplayName);
                bool corpseTaggedWithBlueprint = baseGameSourceBlueprintLC != null && baseGameSourceBlueprintLC == blueprintNameLC;
                if (corpseDisplayNameContainsBlueprintDisplayName || corpseTaggedWithBlueprint)
                {
                    if (!probableBlueprints.Contains(blueprintName))
                    {
                        probableBlueprints.Add(blueprintName);
                        /*
                        Debug.Log(AppendTick("    ") + blueprintName + " added to " + nameof(probableBlueprints) + " }{ [" +
                            corpseDisplayNameLC + "] contains [" + gameObjectDisplayName + "] || [" + baseGameSourceBlueprintLC + "] == [" + blueprintNameLC + "]", indent[2]);
                        */
                    }
                }
                bool corpseTypeMatchesBlueprintName = corpseType != null && corpseType.ToLower() == blueprintNameLC;
                if (corpseTypeMatchesBlueprintName)
                {
                    if(!possibleBlueprints.Contains(blueprintName))
                    {
                        possibleBlueprints.Add(blueprintName);
                        /*
                        Debug.Log(AppendTick("    ") + blueprintName + " added to " + nameof(possibleBlueprints) + " }{ [" +
                            corpseType.ToLower() + "] == [" + blueprintNameLC + "]", indent[2]);
                        */
                    }
                }
                bool corpseSpeciesMatchesBlueprintSpecies = corpseSpecies != null && corpseSpecies?.ToLower() == gameObjectBlueprint.GetPropertyOrTag("Species")?.ToLower();
                if (corpseSpeciesMatchesBlueprintSpecies)
                {
                    if (!fallbackBlueprints.Contains(blueprintName))
                    {
                        fallbackBlueprints.Add(blueprintName);
                        /*
                        Debug.Log(AppendTick("    ") + blueprintName + " added to " + nameof(fallbackBlueprints) + " }{ [" +
                            corpseSpecies?.ToLower() + "] == [" + gameObjectBlueprint.GetPropertyOrTag("Species")?.ToLower() + "]", indent[2]);
                        */
                    }
                }
            }

            if (possibleBlueprints.IsNullOrEmpty())
            {
                possibleBlueprints.AddRange(fallbackBlueprints);
                fallbackBlueprints.Clear();
            }
            if (probableBlueprints.IsNullOrEmpty())
            {
                probableBlueprints.AddRange(possibleBlueprints);
            }
            try
            {
                Debug.Log("Probable Blueprints for " + Corpse.DebugName, indent[1]);
                if (!probableBlueprints.IsNullOrEmpty())
                {
                    probableBlueprints.ShuffleInPlace();
                    foreach (string probableBlueprint in probableBlueprints)
                    {
                        Debug.Log("    " + probableBlueprint, indent[2]);
                    }
                    blueprint ??= probableBlueprints.GetRandomElement();
                    Debug.Log(nameof(blueprint) + " picked: " + blueprint, indent[1]);
                    return blueprint;
                }

                blueprint ??= "Trash Monk";
                Debug.Log(nameof(blueprint) + " picked: " + blueprint, indent[1]);
                return blueprint;
            }
            finally
            {
                Debug.SetIndent(indent[0]);
            }
        }

        public static bool AssignStatsFromStatistics(
            GameObject FrankenCorpse,
            Dictionary<string, Statistic> Statistics,
            bool Override = true,
            Dictionary<string, int> StatAdjustments = null)
        {
            bool any = false;
            if (FrankenCorpse == null || Statistics.IsNullOrEmpty())
            {
                return any;
            }
            foreach ((string statName, Statistic sourceStat) in Statistics)
            {
                Statistic statistic = new(sourceStat)
                {
                    Owner = FrankenCorpse,
                };
                if (!FrankenCorpse.HasStat(statName))
                {
                    FrankenCorpse.Statistics.Add(statName, statistic);
                }
                else
                if (Override)
                {
                    FrankenCorpse.Statistics[statName] = statistic;
                }
                if (!StatAdjustments.IsNullOrEmpty() && StatAdjustments.ContainsKey(statName))
                {
                    FrankenCorpse.Statistics[statName].BaseValue += StatAdjustments[statName];
                }
                any = true;
            }
            FrankenCorpse.FinalizeStats();
            return any;
        }
        public static bool AssignStatsFromPastLife(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            bool Override = true,
            Dictionary<string, int> StatAdjustments = null)
        {
            return AssignStatsFromStatistics(FrankenCorpse, PastLife.Stats, Override, StatAdjustments);
        }
        public static bool AssignStatsFromPastLifeWithAdjustment(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            bool Override = true,
            int PhysicalAdjustment = 0,
            int MentalAdjustment = 0)
        {
            return AssignStatsFromStatistics(FrankenCorpse, PastLife.Stats, Override, new()
            {
                { "Strength", PhysicalAdjustment },
                { "Agility", PhysicalAdjustment },
                { "Toughness", PhysicalAdjustment },
                { "Intelligence", MentalAdjustment },
                { "Willpower", MentalAdjustment },
                { "Ego", MentalAdjustment },
            });
        }
        public static bool AssignStatsFromPastLifeWithFactor(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            bool Override = true,
            float PhysicalAdjustmentFactor = 1f,
            float MentalAdjustmentFactor = 1f)
        {
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
                        StatAdjustments.Add(statName, (int)(stat.BaseValue * adjustmentFactor) - stat.BaseValue);
                    }
                }
            }
            return AssignStatsFromStatistics(FrankenCorpse, PastLife.Stats, Override, StatAdjustments);
        }
        public static bool AssignStatsFromBlueprint(
            GameObject FrankenCorpse,
            GameObjectBlueprint SourceBlueprint,
            bool Override = true,
            Dictionary<string, int> StatAdjustments = null)
        {
            return AssignStatsFromStatistics(FrankenCorpse, SourceBlueprint.Stats, Override, StatAdjustments);
        }

        public static void AssignPartsFromBlueprint(
            GameObject FrankenCorpse,
            GameObjectBlueprint SourceBlueprint,
            Predicate<IPart> Exclude = null)
        {
            if (FrankenCorpse == null || SourceBlueprint == null || SourceBlueprint.allparts.IsNullOrEmpty())
            {
                return;
            }
            foreach (GamePartBlueprint sourcePartBlueprint in SourceBlueprint.allparts.Values)
            {
                if (Stat.Random(1, sourcePartBlueprint.ChanceOneIn) == 1)
                {
                    if (sourcePartBlueprint.T == null)
                    {
                        XRLCore.LogError("Unknown part " + sourcePartBlueprint.Name + "!");
                        return;
                    }
                    IPart sourcePart = sourcePartBlueprint.Reflector?.GetInstance() ?? (Activator.CreateInstance(sourcePartBlueprint.T) as IPart);
                    if (sourcePart == null || Exclude != null && Exclude(sourcePart))
                    {
                        continue;
                    }
                    sourcePart.ParentObject = FrankenCorpse;
                    sourcePartBlueprint.InitializePartInstance(sourcePart);
                    FrankenCorpse.AddPart(sourcePart);

                    if (sourcePartBlueprint.TryGetParameter("Builder", out string partBuilderName)
                        && ModManager.ResolveType("XRL.World.PartBuilders", partBuilderName) is Type partBuilderType
                        && Activator.CreateInstance(partBuilderType) is IPartBuilder partBuilder)
                    {
                        partBuilder.BuildPart(sourcePart);
                    }
                }
            }
        }

        public static bool AssignMutationsFromBlueprint(
            Mutations FrankenMutations,
            GameObjectBlueprint SourceBlueprint,
            Predicate<BaseMutation> Exclude = null)
        {
            bool any = false;
            if (FrankenMutations == null || SourceBlueprint == null || SourceBlueprint.Mutations.IsNullOrEmpty())
            {
                return any;
            }
            foreach (GamePartBlueprint sourceMutationBlueprint in SourceBlueprint.Mutations.Values)
            {
                if (Stat.Random(1, sourceMutationBlueprint.ChanceOneIn) != 1)
                {
                    continue;
                }
                string mutationNamespace = "XRL.World.Parts.Mutation." + sourceMutationBlueprint.Name;
                Type mutationType = ModManager.ResolveType(mutationNamespace);

                if (mutationType == null)
                {
                    MetricsManager.LogError("Unknown mutation " + mutationNamespace);
                    return any;
                }
                if ((sourceMutationBlueprint.Reflector?.GetNewInstance() ?? Activator.CreateInstance(mutationType)) is not BaseMutation baseMutation)
                {
                    MetricsManager.LogError("Mutation " + mutationNamespace + " is not a BaseMutation");
                    continue;
                }
                if (Exclude != null && Exclude(baseMutation))
                {
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
                }
                if (baseMutationToAdd.CapOverride == -1)
                {
                    baseMutationToAdd.CapOverride = baseMutation.Level;
                }
                if (!alreadyHaveMutation)
                {
                    FrankenMutations.AddMutation(baseMutationToAdd, baseMutation.Level);
                }
                else
                {
                    baseMutationToAdd.BaseLevel += baseMutation.Level;
                }
                any = true;
            }
            return any;
        }

        public static bool AssignSkillsFromBlueprint(
            Skills FrankenSkills,
            GameObjectBlueprint SourceBlueprint,
            Predicate<BaseSkill> Exclude = null)
        {
            bool any = false;
            if (FrankenSkills == null || SourceBlueprint == null || SourceBlueprint.Skills.IsNullOrEmpty())
            {
                return any;
            }
            foreach (GamePartBlueprint sourceSkillBlueprint in SourceBlueprint.Skills.Values)
            {
                if (Stat.Random(1, sourceSkillBlueprint.ChanceOneIn) != 1)
                {
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
                    continue;
                }
                sourceSkillBlueprint.InitializePartInstance(baseSkill);
                if (sourceSkillBlueprint.TryGetParameter("Builder", out string skillBuilderName)
                    && ModManager.ResolveType("XRL.World.PartBuilders." + skillBuilderName) is Type skillBuilderType
                    && Activator.CreateInstance(skillBuilderType) is IPartBuilder skillBuilder)
                {
                    skillBuilder.BuildPart(baseSkill, Context: "Initialization");
                }
                any = FrankenSkills.AddSkill(baseSkill) || any;
            }
            return any;
        }

        public static bool ImplantCyberneticsFromAttachedParts(
            GameObject FrankenCorpse,
            out bool AnyImplanted)
        {
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
                    && PastLife?.DeathAddress is UD_FleshGolems_PastLife.UD_FleshGolems_DeathAddress deathAddress
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
            if (WasPlayer)
            {
                if (FrankenCorpse.RequirePart<Skills>() is Skills frankenSkills)
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
        }

        public static bool MakeItALIVE(
            GameObject Corpse,
            UD_FleshGolems_PastLife PastLife)
        {
            Debug.LogHeader(nameof(Corpse) + ": " + (Corpse?.DebugName ?? "null"), out Indents indent);
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

                bool wasPlayer = PastLife != null && PastLife.WasPlayer;

                bool excludedFromDynamicEncounters = PastLife.ExcludeFromDynamicEncounters;

                frankenCorpse.SetIntProperty("NoAnimatedNamePrefix", 1);
                frankenCorpse.SetIntProperty("Bleeds", 1);

                frankenCorpse.Render.RenderLayer = 10;

                PastLife.RestoreBrain(excludedFromDynamicEncounters, out Brain frankenBrain);

                PastLife.RestoreGenderIdentity(WantOldIdentity: 50.in100());

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

                PastLife.RestoreAnatomy(out Body frankenBody);

                PastLife.RestoreTaxonomy();

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

                bool wantDoRequip = false;
                int guaranteedNaturalWeapons = 3;
                int bestowedNaturalWeapons = 0;
                bool guaranteeNaturalWeapon() => guaranteedNaturalWeapons >= bestowedNaturalWeapons;
                Debug.Log("Getting SourceBlueprint Natural Equipment...", indent[1]);
                if (GameObjectFactory.Factory.GetBlueprintIfExists(PastLife?.Blueprint) is GameObjectBlueprint sourceBlueprint)
                {
                    bool isProblemPartOrFollowerPartOrPartAlreadyHave(IPart p)
                    {
                        return IPartsToSkipWhenReanimating.Contains(p.Name)
                            || frankenCorpse.HasPart(p.Name)
                            || (frankenCorpse.GetPropertyOrTag("UD_FleshGolems_Reanimated_PartExclusions") is string propertyPartExclusions
                                && propertyPartExclusions.CachedCommaExpansion() is List<string> partExclusionsList
                                && partExclusionsList.Contains(p.Name));
                    }
                    AssignStatsFromBlueprint(frankenCorpse, sourceBlueprint);

                    float physicalAdjustmentFactor = 1.2f; // wasPlayer ? 1.0f : 1.2f;
                    float mentalAdjustmentFactor = 0.80f; // wasPlayer ? 1.0f : 0.80f;
                    AssignStatsFromPastLifeWithFactor(frankenCorpse, PastLife, PhysicalAdjustmentFactor: physicalAdjustmentFactor, MentalAdjustmentFactor: mentalAdjustmentFactor);

                    AssignPartsFromBlueprint(frankenCorpse, sourceBlueprint, Exclude: isProblemPartOrFollowerPartOrPartAlreadyHave);

                    AssignMutationsFromBlueprint(frankenMutations, sourceBlueprint);

                    AssignSkillsFromBlueprint(frankenSkills, sourceBlueprint);

                    excludedFromDynamicEncounters = PastLife != null && !PastLife.Tags.IsNullOrEmpty() && PastLife.Tags.ContainsKey("ExcludeFromDynamicEncounters");

                    if (frankenCorpse.TryGetPart(out Leveler frankenLeveler))
                    {
                        if (int.TryParse(frankenCorpse.GetPropertyOrTag("UD_FleshGolems_SkipLevelsOnReanimate", "0"), out int SkipLevelsOnReanimate)
                            && SkipLevelsOnReanimate < 1)
                        {
                            frankenLeveler?.LevelUp();
                            if (Stat.RollCached("1d2") == 1)
                            {
                                frankenLeveler?.LevelUp();
                            }
                        }
                        int floorXP = Leveler.GetXPForLevel(frankenCorpse.Level);
                        int ceilingXP = Leveler.GetXPForLevel(frankenCorpse.Level + 1);
                        frankenCorpse.GetStat("XP").BaseValue = Stat.RandomCosmetic(floorXP, ceilingXP);
                    }

                    if (sourceBlueprint.Tags.ContainsKey("Species"))
                    {
                        frankenCorpse.SetStringProperty("Species", sourceBlueprint.Tags["Species"]);
                    }
                    

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

                    string BleedLiquid = null;
                    if (sourceBlueprint.Tags.ContainsKey(nameof(BleedLiquid)))
                    {
                        BleedLiquid = sourceBlueprint.Tags[nameof(BleedLiquid)];
                    }
                    if (BleedLiquid.IsNullOrEmpty())
                    {
                        BleedLiquid = "blood-1000";
                    }
                    frankenCorpse.GetPropertyOrTag("BleedLiquid", BleedLiquid);

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

                    Debug.Log("Getting Golem Anatomy...", indent[1]);
                    if (GolemBodySelection.GetBodyBlueprintFor(sourceBlueprint) is GameObjectBlueprint golemBodyBlueprint)
                    {
                        Debug.Log(nameof(golemBodyBlueprint), golemBodyBlueprint.Name, indent[2]);
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
                            PastLife.RestoreAdditionalLimbs();
                        }
                        if (golemBodyBlueprint.TryGetPartParameter(nameof(Parts.Render), nameof(Parts.Render.Tile), out string golemTile))
                        {
                            frankenCorpse.Render.Tile = golemTile;
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
                    else
                    {
                        Debug.Log("Uh-oh! Something went wrong!", indent[2]);
                        Debug.Log("Changing tile to the PastLife Tile", indent[2]);
                        if (PastLife?.PastRender?.Tile is string pastTile)
                        {
                            Debug.Log("Tile changed", "\"" + pastTile + "\"", indent[3]);
                            frankenCorpse.Render.Tile = pastTile;
                        }
                        else
                        {
                            Debug.Log("Uh-oh! Something went wrong!", indent[3]);
                            Debug.Log("Changing tile to the sourceBlueprint Tile", indent[3]);
                            if (sourceBlueprint.TryGetPartParameter(nameof(Parts.Render), nameof(Parts.Render.Tile), out string sourceTile))
                            {
                                Debug.Log("Tile changed", "\"" + sourceTile + "\"", indent[4]);
                                frankenCorpse.Render.Tile = sourceTile;
                            }
                        }
                        PastLife.RestoreAdditionalLimbs();
                    }

                    Debug.Log("Granting SourceBlueprint Natural Equipment...", indent[1]);
                    Debug.Log(nameof(sourceBlueprint), nameof(sourceBlueprint.Inventory), indent[2]);
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
                                        && GetRaggedNaturalWeapons(bp => MeleeWeaponSlotAndSkillMatchesBlueprint(bp, mw))?.GetRandomElement()?.Name is string raggedWeaponBlueprintName
                                        && GameObject.CreateUnmodified(raggedWeaponBlueprintName) is GameObject raggedWeaponObject
                                        && frankenCorpse.ReceiveObject(raggedWeaponObject))
                                    {
                                        string debugOutput =
                                            nameof(sampleNaturalGear) + ": " + sampleNaturalGear.DebugName + " | " +
                                            nameof(raggedWeaponObject) + ": " + raggedWeaponObject.DebugName;
                                        Debug.Log(AppendTick("") + itemLabel + debugOutput, Indent: indent[3]);
                                        wantDoRequip = true;
                                        bestowedNaturalWeapons++;
                                    }
                                    sampleNaturalGear?.Obliterate();
                                }
                            }
                            else
                            {
                                Debug.Log(AppendCross("") + itemLabel + "Not Natural'", Indent: indent[3]);
                            }
                        }
                    }
                }
                frankenBody ??= frankenCorpse.Body;
                Debug.Log("Granting Additional Natural Equipment...", indent[1]);
                Debug.Log(nameof(frankenBody), nameof(frankenBody.LoopParts), indent[2]);
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
                            && GameObject.CreateUnmodified(raggedWeaponBlueprintName) is GameObject raggedWeaponObject)
                        {
                            string debugOutput =
                                nameof(frankenNaturalGear) + ": " + (frankenNaturalGear?.DebugName ?? NULL) + " | " +
                                nameof(raggedWeaponObject) + ": " + (raggedWeaponObject.DebugName ?? NULL);
                            Debug.Log(AppendTick("") + limbLabel + debugOutput, Indent: indent[3]);

                            frankenNaturalGear.Obliterate();
                            frankenLimb.DefaultBehavior = raggedWeaponObject;
                            frankenLimb.DefaultBehaviorBlueprint = raggedWeaponBlueprintName;
                            bestowedNaturalWeapons++;
                        }
                    }
                    else
                    if ((guaranteeNaturalWeapon() || 25.in100())
                        && GetRaggedNaturalWeapons(bp => MeleeWeaponSlotMatchesBlueprint(bp, frankenLimb.Type))?.GetRandomElement()?.Name is string raggedWeaponBlueprintName
                        && GameObject.CreateUnmodified(raggedWeaponBlueprintName) is GameObject raggedWeaponObject)
                    {
                        string debugOutput = "Rolled 1 in 4 | " + nameof(raggedWeaponObject) + ": " + (raggedWeaponObject?.DebugName ?? NULL);
                        Debug.Log(AppendTick("") + limbLabel + debugOutput, Indent: indent[3]);

                        frankenLimb.DefaultBehavior = raggedWeaponObject;
                        frankenLimb.DefaultBehaviorBlueprint = raggedWeaponBlueprintName;
                        bestowedNaturalWeapons++;
                    }
                    else
                    {
                        Debug.Log(AppendCross("") + limbLabel + "nuffin'", Indent: indent[3]);
                    }
                }
                if (wantDoRequip)
                {
                    frankenBrain?.WantToReequip();
                }

                if (frankenMutations != null)
                {
                    bool giveRegen = true;
                    if (giveRegen
                        && MutationFactory.GetMutationEntryByName("Regeneration").Class is string regenerationMutationClass)
                    {
                        if (frankenMutations.GetMutation(regenerationMutationClass) is not BaseMutation regenerationMutation)
                        {
                            frankenMutations.AddMutation(regenerationMutationClass, Level: 10);
                            regenerationMutation = frankenMutations.GetMutation(regenerationMutationClass);
                        }
                        regenerationMutation.CapOverride = 5;

                        if (regenerationMutation.Level < 5)
                        {
                            regenerationMutation.ChangeLevel(5);
                        }
                    }
                    string nightVisionMutaitonName = "Night Vision";
                    string darkVisionMutationName = "Dark Vision";
                    MutationEntry nightVisionEntry = MutationFactory.GetMutationEntryByName(nightVisionMutaitonName);
                    MutationEntry darkVisionEntry = MutationFactory.GetMutationEntryByName(darkVisionMutationName);
                    if (!frankenMutations.HasMutation(nightVisionEntry.Class) && !frankenMutations.HasMutation(darkVisionEntry.Class))
                    {
                        if (darkVisionEntry.Instance is BaseMutation darkVisionMutation)
                        {
                            if (darkVisionMutation.CapOverride == -1)
                            {
                                darkVisionMutation.CapOverride = 8;
                            }
                            frankenMutations.AddMutation(darkVisionMutation, 8);
                        }
                    }
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
                        frankenRestocker.PerformRestock();
                        frankenRestocker.LastRestockTick = The.Game.TimeTicks;
                    }
                }

                if (frankenCorpse.GetStat("Hitpoints") is Statistic frankenHitpoints)
                {
                    int minHitpoints = Stat.RollCached("4d3+5");
                    Debug.Log(nameof(frankenHitpoints) + " " + nameof(minHitpoints), minHitpoints, Indent: indent[1]);
                    frankenHitpoints.BaseValue = Math.Max(minHitpoints, frankenHitpoints.BaseValue);
                    frankenHitpoints.Penalty = 0;
                    Debug.Log(nameof(frankenHitpoints), frankenHitpoints.Value + "/" + frankenHitpoints.BaseValue, Indent: indent[1]);
                }

                if (frankenCorpse != null)
                {
                    var reanimatedCorpse = frankenCorpse.RequirePart<UD_FleshGolems_ReanimatedCorpse>();
                    reanimatedCorpse.Reanimator = reanimationHelper.Reanimator;
                    // reanimatedCorpse.AttemptToSuffer();
                }
                Debug.SetIndent(indent[0]);
                return true;
            }
            Debug.SetIndent(indent[0]);
            return false;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            base.Register(Object, Registrar);
            try
            {
                Registrar?.Register(DroppedEvent.ID, EventOrder.EXTREMELY_EARLY);
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
        {
            return base.WantEvent(ID, Cascade)
                || ID == BeforeObjectCreatedEvent.ID
                || ID == AnimateEvent.ID
                || ID == EnvironmentalUpdateEvent.ID
                || (FailedToRegisterEvents.Contains(DroppedEvent.ID) && ID == DroppedEvent.ID);
        }
        public override bool HandleEvent(BeforeObjectCreatedEvent E)
        {

            if (!IsALIVE
                && ParentObject == E.Object
                && false)
            {
                UnityEngine.Debug.Log(
                    nameof(UD_FleshGolems_CorpseReanimationHelper) + "." + nameof(BeforeObjectCreatedEvent) + ", " +
                    nameof(IsALIVE) + ": " + IsALIVE);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AnimateEvent E)
        {
            UnityEngine.Debug.Log(
                nameof(UD_FleshGolems_CorpseReanimationHelper) + "." + nameof(AnimateEvent) + ", " +
                nameof(IsALIVE) + ": " + IsALIVE);

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
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnvironmentalUpdateEvent E)
        {
            if (AlwaysAnimate
                && !IsALIVE
                && ParentObject is GameObject corpse
                && Animate(out corpse))
            {
                ProcessMoveToDeathCell(corpse, PastLife);
                return true;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(DroppedEvent E)
        {
            if (E.Item is GameObject corpse
                && corpse == ParentObject
                && E.Actor is GameObject dying
                && dying.IsDying)
            {
                corpse.RequirePart<UD_FleshGolems_PastLife>().Initialize(dying);
            }
            if (AlwaysAnimate
                && !IsALIVE
                && ParentObject != null
                && Animate())
            {
                return true;
            }
            return base.HandleEvent(E);
        }
    }
}
