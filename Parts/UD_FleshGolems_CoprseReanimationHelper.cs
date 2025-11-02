using System;
using System.Collections.Generic;
using System.Text;

using XRL.Language;
using XRL.Rules;
using XRL.World.Anatomy;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.Quests.GolemQuest;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_CoprseReanimationHelper : IScribedPart
    {
        public const string REANIMATED_CONVO_ID_TAG = "UD_FleshGolems_ReanimatedConversationID";
        public const string REANIMATED_EPITHETS_TAG = "UD_FleshGolems_ReanimatedEpithets";

        public bool IsALIVE;

        public bool AlwaysAnimate;

        public string CreatureName;

        public string SourceBlueprint;

        public string CorpseDescription;

        public UD_FleshGolems_CoprseReanimationHelper()
        {
            IsALIVE = false;
            AlwaysAnimate = false;
            CreatureName = null;
            SourceBlueprint = null;
            CorpseDescription = null;
        }

        public override bool AllowStaticRegistration()
        {
            return true;
        }


        public static void AssignStatsFromBlueprint(GameObject FrankenCorpse, GameObjectBlueprint SourceBlueprint)
        {
            if (FrankenCorpse == null || SourceBlueprint == null || SourceBlueprint.Stats.IsNullOrEmpty())
            {
                return;
            }
            foreach ((string statName, Statistic sourceStat) in SourceBlueprint.Stats)
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
                {
                    FrankenCorpse.Statistics[statName] = statistic;
                }
            }
            FrankenCorpse.FinalizeStats();
        }

        public static void AssignMutationsFromBlueprint(
            Mutations FrankenMutations,
            GameObjectBlueprint SourceBlueprint,
            Predicate<BaseMutation> Exclude = null)
        {
            if (FrankenMutations == null || SourceBlueprint == null || SourceBlueprint.Mutations.IsNullOrEmpty())
            {
                return;
            }
            foreach (GamePartBlueprint sourceMutation in SourceBlueprint.Mutations.Values)
            {
                if (Stat.Random(1, sourceMutation.ChanceOneIn) != 1)
                {
                    continue;
                }
                string mutationNamespace = "XRL.World.Parts.Mutation." + sourceMutation.Name;
                Type mutationType = ModManager.ResolveType(mutationNamespace);

                if (mutationType == null)
                {
                    MetricsManager.LogError("Unknown mutation " + mutationNamespace);
                    return;
                }
                if ((sourceMutation.Reflector?.GetNewInstance() ?? Activator.CreateInstance(mutationType)) is not BaseMutation baseMutation)
                {
                    MetricsManager.LogError("Mutation " + mutationNamespace + " is not a BaseMutation");
                    continue;
                }
                if (Exclude != null && Exclude(baseMutation))
                {
                    continue;
                }
                sourceMutation.InitializePartInstance(baseMutation);
                if (sourceMutation.TryGetParameter("Builder", out string mutationBuilderName)
                    && ModManager.ResolveType("XRL.World.PartBuilders." + mutationBuilderName) is Type mutationBuilderType
                    && Activator.CreateInstance(mutationBuilderType) is IPartBuilder mutationBuilder)
                {
                    mutationBuilder.BuildPart(baseMutation, Context: "Initialization");
                }
                if (baseMutation.CapOverride == -1)
                {
                    baseMutation.CapOverride = baseMutation.Level;
                }
                FrankenMutations.AddMutation(baseMutation, baseMutation.Level);
            }
        }
        public static void AssignSkillsFromBlueprint(
            Skills FrankenSkills,
            GameObjectBlueprint SourceBlueprint,
            Predicate<BaseSkill> Exclude = null)
        {
            if (FrankenSkills == null || SourceBlueprint == null || SourceBlueprint.Skills.IsNullOrEmpty())
            {
                return;
            }
            foreach (GamePartBlueprint sourceSkill in SourceBlueprint.Skills.Values)
            {
                if (Stat.Random(1, sourceSkill.ChanceOneIn) != 1)
                {
                    continue;
                }
                string skillNamespace = "XRL.World.Parts.Skill." + sourceSkill.Name;
                Type skillType = ModManager.ResolveType(skillNamespace);

                if (skillType == null)
                {
                    MetricsManager.LogError("Unknown skill " + skillNamespace);
                    return;
                }
                if ((sourceSkill.Reflector?.GetNewInstance() ?? Activator.CreateInstance(skillType)) is not BaseSkill baseSkill)
                {
                    MetricsManager.LogError("Skill " + skillNamespace + " is not a " + nameof(BaseSkill));
                    continue;
                }
                if (Exclude != null && Exclude(baseSkill))
                {
                    continue;
                }
                sourceSkill.InitializePartInstance(baseSkill);
                if (sourceSkill.TryGetParameter("Builder", out string skillBuilderName)
                    && ModManager.ResolveType("XRL.World.PartBuilders." + skillBuilderName) is Type skillBuilderType
                    && Activator.CreateInstance(skillBuilderType) is IPartBuilder skillBuilder)
                {
                    skillBuilder.BuildPart(baseSkill, Context: "Initialization");
                }
                FrankenSkills.AddSkill(baseSkill);
            }
        }

        public static bool MakeItALIVE(GameObject Corpse, ref string CreatureName, ref string SourceBlueprint, ref string CorpseDescription)
        {
            UnityEngine.Debug.Log("    " + nameof(MakeItALIVE) + ", " + nameof(Corpse) + ": " + Corpse?.DebugName ?? "null");
            if (Corpse is GameObject frankenCorpse)
            {
                frankenCorpse.SetIntProperty("NoAnimatedNamePrefix", 1);
                frankenCorpse.SetIntProperty("Bleeds", 1);

                UD_FleshGolems_ReanimatedCorpse reanimatedCorpse = frankenCorpse.RequirePart<UD_FleshGolems_ReanimatedCorpse>();

                string convoID = frankenCorpse.GetPropertyOrTag(REANIMATED_CONVO_ID_TAG);
                if (frankenCorpse.TryGetPart(out ConversationScript convo)
                    && (convo.ConversationID == "NewlySentientBeings" || !convoID.IsNullOrEmpty()))
                {
                    convoID ??= "UD_FleshGolems NewlyReanimatedBeings";
                    frankenCorpse.RemovePart(convo);
                    convo = frankenCorpse.AddPart(new ConversationScript(convoID));
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
                    List<string> poeticFeatures = new(frankenCorpse.GetxTag("TextFragments", "PoeticFeatures").Split(','));
                    string firstPoeticFeature = poeticFeatures.GetRandomElement();
                    poeticFeatures.Remove(firstPoeticFeature);
                    string secondPoeticFeature = poeticFeatures.GetRandomElement();
                    poeticFeatures.Remove(secondPoeticFeature);
                    string poeticVerb = frankenCorpse.GetxTag("TextFragments", "PoeticVerbs").Split(',').GetRandomElement();
                    string poeticAdjective = frankenCorpse.GetxTag("TextFragments", "PoeticAdjectives").Split(',').GetRandomElement();
                    List<string> poeticNoises = new(frankenCorpse.GetxTag("TextFragments", "PoeticnNoises").Split(','));
                    string firstPoeticNoise = poeticNoises.GetRandomElement();
                    poeticNoises.Remove(firstPoeticNoise);
                    string secondPoeticNoise = poeticNoises.GetRandomElement();
                    poeticFeatures.Remove(secondPoeticNoise);

                    CorpseDescription = frankenDescription._Short;
                    frankenDescription._Short =
                        ("*FirstFeature* and *secondFeature* are brought *verbing* back into a horrific facsimile of life. " +
                        "*Adjective* *firstNoise* and *secondNoise* escape =subject.possessive= every movement and twist the " +
                        "gut of anyone unfortunate enough to hear it.")
                            .Replace("*FirstFeature*", firstPoeticFeature.Capitalize())
                            .Replace("*secondFeature*", secondPoeticFeature)
                            .Replace("*verbing*", poeticVerb)
                            .Replace("*Adjective*", poeticAdjective.Capitalize())
                            .Replace("*firstNoise*", firstPoeticNoise)
                            .Replace("*secondNoise*", secondPoeticNoise);
                }
                string sourceBlueprintName = frankenCorpse.GetPropertyOrTag("SourceBlueprint")
                    ?? frankenCorpse.GetPropertyOrTag("OriginalCorpse")
                    ?? frankenCorpse.GetPropertyOrTag("CorpseBlueprint", frankenCorpse?.Blueprint)?.Replace(" Corpse", "")
                    ?? "Trash Monk";
                SourceBlueprint = sourceBlueprintName;

                Corpse frankenCorpseCorpse = frankenCorpse.RequirePart<Corpse>();
                if (frankenCorpseCorpse != null)
                {
                    frankenCorpseCorpse.CorpseBlueprint = frankenCorpse.Blueprint;
                    frankenCorpseCorpse.CorpseChance = 100;
                }

                string frankenGenotype = frankenCorpse?.GetPropertyOrTag("FromGenotype");
                if (frankenGenotype != null)
                {
                    frankenCorpse.SetStringProperty("Genotype", frankenGenotype);
                }
                if (frankenCorpse.TryGetPart(out CyberneticsButcherableCybernetic butcherableCybernetics)
                    && frankenCorpse.Body is Body frankenBody)
                {
                    string cyberneticsLicenses = "CyberneticsLicenses";
                    string cyberneticsLicensesFree = "FreeCyberneticsLicenses";
                    int startingLicenses = Stat.RollCached("2d2-1");

                    frankenCorpse.SetIntProperty(cyberneticsLicenses, startingLicenses);
                    frankenCorpse.SetIntProperty(cyberneticsLicensesFree, startingLicenses);

                    List<GameObject> butcherableCyberneticsList = Event.NewGameObjectList(butcherableCybernetics.Cybernetics);
                    foreach (GameObject butcherableCybernetic in butcherableCyberneticsList)
                    {
                        if (butcherableCybernetic.TryGetPart(out CyberneticsBaseItem butcherableCyberneticBasePart)
                            && butcherableCyberneticBasePart.Slots is string slotsString)
                        {
                            int cyberneticsCost = butcherableCyberneticBasePart.Cost;
                            frankenCorpse.ModIntProperty(cyberneticsLicenses, cyberneticsCost);
                            frankenCorpse.ModIntProperty(cyberneticsLicensesFree, cyberneticsCost);

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
                    frankenCorpse.RemovePart(butcherableCybernetics);
                }

                Mutations frankenMutations = frankenCorpse.RequirePart<Mutations>();
                Skills frankenSkills = frankenCorpse.RequirePart<Skills>();

                if (GameObjectFactory.Factory.GetBlueprintIfExists(sourceBlueprintName) is GameObjectBlueprint sourceBlueprint)
                {
                    AssignStatsFromBlueprint(frankenCorpse, sourceBlueprint);
                    AssignMutationsFromBlueprint(frankenMutations, sourceBlueprint);
                    AssignSkillsFromBlueprint(frankenSkills, sourceBlueprint);

                    if (sourceBlueprint.GetPropertyOrTag(REANIMATED_CONVO_ID_TAG) is string sourceCreatureConvoID
                        && convo != null)
                    {
                        frankenCorpse.RemovePart(convo);
                        convo = frankenCorpse.AddPart(new ConversationScript(convoID));
                    }

                    if (sourceBlueprint.GetPropertyOrTag(REANIMATED_EPITHETS_TAG) is string sourceCreatureEpithets)
                    {
                        frankenEpithets = frankenCorpse.RequirePart<Epithets>();
                        frankenEpithets.Primary = sourceCreatureEpithets;
                    }

                    string corpsePartName = nameof(Parts.Corpse);
                    string corpsepartBlueprintName = nameof(Parts.Corpse.CorpseBlueprint);
                    if (frankenCorpseCorpse != null
                        && sourceBlueprint.TryGetPartParameter(corpsePartName, corpsepartBlueprintName, out string frankenCorpseCorpseBlueprint))
                    {
                        frankenCorpseCorpse.CorpseBlueprint = frankenCorpseCorpseBlueprint;
                    }

                    if (sourceBlueprint.DisplayName() is string sourceBlueprintDisplayName)
                    {
                        string sourceCreatureName = null;
                        bool wereProperlyNamed = false;
                        if (GameObject.CreateSample(sourceBlueprint.Name) is GameObject sampleSourceObject)
                        {
                            wereProperlyNamed = sampleSourceObject.HasProperName;
                            if (wereProperlyNamed)
                            {
                                sourceCreatureName = sampleSourceObject.GetReferenceDisplayName(Short: true);
                            }
                        }
                        if (frankenCorpse.GetPropertyOrTag("CreatureProperName") is string frankenCorpseProperName)
                        {
                            wereProperlyNamed = true;
                            sourceCreatureName = frankenCorpseProperName;
                        }
                        sourceCreatureName ??= frankenCorpse.GetPropertyOrTag("CreatureName");

                        if (frankenDescription != null
                            && sourceBlueprint.TryGetPartParameter(nameof(Description), nameof(Description.Short), out string sourceDescription))
                        {
                            if (frankenCorpse.GetPropertyOrTag("CorpseDescription") is string sourceCorpseDescription)
                            {
                                sourceDescription = sourceCorpseDescription;
                            }
                            string whoTheyWere = wereProperlyNamed ? sourceCreatureName : sourceBlueprintDisplayName;
                            if (whoTheyWere.ToLower().EndsWith(" corpse") || whoTheyWere.ToLower().StartsWith("corpse of "))
                            {
                                whoTheyWere = UD_FleshGolems_ReanimatedCorpse.REANIMATED_ADJECTIVE + " " + whoTheyWere;
                            }
                            if (!wereProperlyNamed)
                            {
                                whoTheyWere = Grammar.A(whoTheyWere);
                            }
                            frankenDescription._Short += "\n\n" + "In life, this mess was " + whoTheyWere + ":\n" + sourceDescription;
                        }
                        if (!sourceCreatureName.IsNullOrEmpty())
                        {
                            frankenCorpse.Render.DisplayName = "corpse of " + sourceCreatureName;

                            frankenCorpse.GiveProperName(sourceCreatureName);
                            frankenCorpse.RequirePart<Honorifics>().Primary = "corpse of";
                        }
                        else
                        {
                            frankenCorpse.Render.DisplayName = sourceBlueprintDisplayName + " corpse";
                        }
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

                    if (GolemBodySelection.GetBodyBlueprintFor(sourceBlueprint) is GameObjectBlueprint golemBodyBlueprint)
                    {
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
                        if (golemBodyBlueprint.TryGetPartParameter(nameof(Parts.Render), nameof(Parts.Render.Tile), out string golemTile))
                        {
                            frankenCorpse.Render.Tile = golemTile;
                            UnityEngine.Debug.Log(nameof(golemTile) + ": " + golemTile);
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
                    if (frankenMutations != null)
                    {
                        bool giveRegen = false;
                        if (giveRegen
                            && MutationFactory.GetMutationEntryByName("Regeneration").Class is string regenerationMutationClass)
                        {
                            if (frankenMutations.GetMutation(regenerationMutationClass) is not BaseMutation regenerationMutation)
                            {
                                frankenMutations.AddMutation(regenerationMutationClass, Level: 10);
                                regenerationMutation = frankenMutations.GetMutation(regenerationMutationClass);
                            }
                            regenerationMutation.CapOverride = 10;

                            if (regenerationMutation.Level < 10)
                            {
                                regenerationMutation.ChangeLevel(10);
                            }
                        }

                        string darkVisionMutationName = "Dark Vision";
                        if (frankenMutations.GetMutationByName(darkVisionMutationName) is not BaseMutation darkVisionMutation)
                        {
                            darkVisionMutation = MutationFactory.GetMutationEntryByName(darkVisionMutationName)?.CreateInstance();
                        }
                        if (darkVisionMutation != null)
                        {
                            frankenMutations.AddMutation(darkVisionMutation, 12);
                        }
                    }
                }

                if (!frankenCorpse.IsPlayer())
                {
                    bool isItemThatNotSelf(GameObject GO)
                    {
                        return GO.GetBlueprint().InheritsFrom("Item")
                            && GO != frankenCorpse;
                    }
                    frankenCorpse.TakeObject(Event.NewGameObjectList(frankenCorpse.CurrentCell.GetObjects(isItemThatNotSelf)));
                    frankenCorpse.Brain?.WantToReequip();
                }

                return true;
            }
            return false;
        }

        public bool Animate(out GameObject FrankenCorpse)
        {
            FrankenCorpse = null;
            UnityEngine.Debug.Log(nameof(UD_FleshGolems_CoprseReanimationHelper) + "." + nameof(Animate));
            if (!ParentObject.HasPart<AnimatedObject>())
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

        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == AnimateEvent.ID
                || ID == EnvironmentalUpdateEvent.ID;
        }
        public override bool HandleEvent(AnimateEvent E)
        {
            UnityEngine.Debug.Log(
                nameof(UD_FleshGolems_CoprseReanimationHelper) + "." + nameof(AnimateEvent) + ", " +
                nameof(IsALIVE) + ": " + IsALIVE);

            if (!IsALIVE
                && ParentObject == E.Object)
            {
                IsALIVE = MakeItALIVE(E.Object, ref CreatureName, ref SourceBlueprint, ref CorpseDescription);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnvironmentalUpdateEvent E)
        {
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
