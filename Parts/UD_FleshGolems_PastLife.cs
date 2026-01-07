using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using Qud.API;

using XRL.UI;
using XRL.Wish;
using XRL.Language;
using XRL.World.AI;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.Anatomy;
using XRL.World.ObjectBuilders;
using XRL.World.Effects;
using XRL.World.Conversations.Parts;

using static XRL.World.Parts.UD_FleshGolems_CorpseReanimationHelper;
using static XRL.World.ObjectBuilders.UD_FleshGolems_OptionallyReanimated;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Parts.PastLifeHelpers;
using UD_FleshGolems.Events;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;
using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;

using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    [HasWishCommand]
    [Serializable]
    public class UD_FleshGolems_PastLife : IScribedPart, IReanimateEventHandler
    {
        public const string BRAIN_IN_A_JAR_BLUEPRINT = "UD_FleshGolems Brain In A Jar Widget";
        public const string PASTLIFE_BLUEPRINT_PROPTAG = "UD_FleshGolems_PastLife_Blueprint";
        public const string PREVIOUSLY_SENTIENT_BEINGS = "Previously Sentient Beings";

        public static List<string> PropTagsToNotRestore = new()
        {
            "Species",
            "Genotype",
            "Subtype",
            "Gender",
            "BleedLiquid",
            "Humanoid",
            "Bleeds",
            UD_FleshGolems_AskHowDied.ASK_HOW_DIED_PROP,
        };

        public static List<string> PartsToNotRetain = new()
        {
            nameof(Stomach),
            nameof(Inventory),
            nameof(Parts.Skills),
            nameof(Parts.Mutations),
            nameof(ReplaceObject),
            nameof(Spawner),
            nameof(CherubimSpawner),
            nameof(AnimatedObject),
            nameof(UD_FleshGolems_ReanimatedCorpse),
            nameof(UD_FleshGolems_DestinedForReanimation),
        };

        public GameObject BrainInAJar;

        public UD_FleshGolems_PastLife PastPastLife => BrainInAJar?.GetPart<UD_FleshGolems_PastLife>();

        [SerializeField]
        private bool _Init;
        public bool Init
        { 
            get => _Init; 
            protected set
            {
                _Init = value;
            }
        }

        [SerializeField]
        private bool _WasBuiltReanimated;
        public bool WasBuiltReanimated
        {
            get => _WasBuiltReanimated;
            protected set => _WasBuiltReanimated = value;
        }

        public bool WasCorpse => (Blueprint?.IsCorpse()).GetValueOrDefault();

        public bool WasPlayer 
            => (BrainInAJar != null && BrainInAJar.IsPlayerDuringWorldGen()) 
            || (BrainInAJar != null && BrainInAJar.HasPropertyOrTag("UD_FleshGolems_WasPlayer"));

        public int TimesReanimated;

        public string Blueprint;

        public bool ExcludeFromDynamicEncounters => Blueprint.IsExcludedFromDynamicEncounters();

        [SerializeField]
        private string _BaseDisplayName;
        public string BaseDisplayName => _BaseDisplayName ??= BrainInAJar?.BaseDisplayName;

        [SerializeField]
        private string _RenderDisplayName;
        public string RenderDisplayName
        {
            get => _RenderDisplayName ??= BrainInAJar?.Render?.DisplayName;
            set => _RenderDisplayName = value;
        }

        [SerializeField]
        private string _RefName;
        public string RefName => _RefName ??= BrainInAJar?.GetReferenceDisplayName(Short: true);

        public bool WasProperlyNamed => WasProperlyNamed(BrainInAJar);

        public DisplayNameAdjectives DisplayNameAdjectives => BrainInAJar?.GetPart<DisplayNameAdjectives>();
        public Titles Titles => BrainInAJar?.GetPart<Titles>();
        public Epithets Epithets => BrainInAJar?.GetPart<Epithets>();
        public Honorifics Honorifics => BrainInAJar?.GetPart<Honorifics>();

        public Render PastRender => BrainInAJar?.Render;

        private string _Description;
        public string Description => _Description ??= BrainInAJar?.GetPart<Description>()?._Short;

        public DeathCoordinates DeathAddress;

        public Physics Physics => BrainInAJar?.Physics;

        public Brain Brain => BrainInAJar?.Brain;

        public Gender Gender => BrainInAJar?.GetGender();
        public PronounSet PronounSet => BrainInAJar?.GetPronounSet();

        public string ConversationScriptID => BrainInAJar?.GetPart<ConversationScript>()?.ConversationID;

        public Dictionary<string, Statistic> Stats => BrainInAJar?.Statistics;

        public Body Body => BrainInAJar?.Body;
        public Dictionary<PseudoLimb, string> ExtraLimbs;

        public EntityTaxa EntityTaxa;
        public string Species => BrainInAJar?.GetSpecies();
        public string Genotype => BrainInAJar?.GetGenotype();
        public string Subtype => BrainInAJar?.GetSubtype();
        public Corpse Corpse => BrainInAJar?.GetPart<Corpse>();
        public string BleedLiquid => BrainInAJar?.GetBleedLiquid();

        public Mutations Mutations => BrainInAJar?.GetPart<Mutations>();

        public List<MutationData> MutationsList;

        public Skills Skills => BrainInAJar?.GetPart<Skills>();

        public Dictionary<string, string> Tags => Blueprint?.GetGameObjectBlueprint()?.Tags;
        public Dictionary<string, string> StringProperties => BrainInAJar?._Property;
        public Dictionary<string, int> IntProperties => BrainInAJar?._IntProperty;

        public EffectRack Effects => BrainInAJar?._Effects;

        public List<InstalledCybernetic> InstalledCybernetics;

        public UD_FleshGolems_PastLife()
        {
            BrainInAJar = GetNewBrainInAJar();
            _Init = false;
            _WasBuiltReanimated = false;

            TimesReanimated = 0;

            Blueprint = null;

            _BaseDisplayName = null;
            _RefName = null;
            _Description = null;

            DeathAddress = new();

            ExtraLimbs = new();

            EntityTaxa = new();

            MutationsList = new();

            InstalledCybernetics = new();
        }

        public UD_FleshGolems_PastLife(GameObject PastLife)
            : this()
            => Initialize(PastLife);

        public UD_FleshGolems_PastLife(UD_FleshGolems_PastLife PrevPastLife)
            : this()
            => Initialize(PrevPastLife);

        private static GameObject GetNewBrainInAJar()
            => GameObjectFactory.Factory.CreateUnmodifiedObject(BRAIN_IN_A_JAR_BLUEPRINT);

        public static bool IsProcessableCorpse(GameObjectBlueprint Corpse, Predicate<GameObjectBlueprint> Filter)
            => Corpse.IsCorpse(Filter)
            && (Corpse.HasPart(nameof(Butcherable)) || Corpse.HasPart(nameof(Harvestable)));

        public static bool IsProcessableCorpse(GameObjectBlueprint Corpse, bool ExcludeBase)
            => IsProcessableCorpse(Corpse, ExcludeBase ? IsNotBaseBlueprint : null);

        public static bool IsProcessableCorpse(GameObjectBlueprint Corpse)
            => IsProcessableCorpse(Corpse, true);

        public static List<GameObjectBlueprint> GetCorpseBlueprints(Predicate<GameObjectBlueprint> Filter)
            => NecromancySystem
                ?.GetCorpseBlueprints(Filter)
                ?.ToList()?.ConvertAll(cbp => cbp.GetGameObjectBlueprint());

        public static string GetAnEntityForCorpseWeighted(
            string CorpseBlueprint,
            bool Include0Weight = true,
            bool GuaranteeBlueprint = true)
        {
            Dictionary<string, int> weightedBlueprints = NecromancySystem
                ?.GetWeightedEntityStringsThisCorpseCouldBe(
                    CorpseBlueprint: CorpseBlueprint,
                    Include0Weight: Include0Weight,
                    Filter: IsNotBaseBlueprintOrPossiblyExcludedFromDynamicEncounters,
                    DrillIntoInheritance: false);

            if (GuaranteeBlueprint
                && weightedBlueprints.IsNullOrEmpty())
                weightedBlueprints = NecromancySystem
                    ?.GetWeightedEntityStringsThisCorpseCouldBe(
                        CorpseBlueprint: CorpseBlueprint,
                        Include0Weight: Include0Weight,
                        Filter: IsNotBaseBlueprintOrPossiblyExcludedFromDynamicEncounters,
                        DrillIntoInheritance: true);

            if (GuaranteeBlueprint
                && weightedBlueprints.IsNullOrEmpty())
                weightedBlueprints = NecromancySystem
                    ?.GetWeightedEntityStringsThisCorpseCouldBe(
                        CorpseBlueprint: CorpseBlueprint,
                        Include0Weight: Include0Weight,
                        Filter: IsNotBaseBlueprint,
                        DrillIntoInheritance: false);

            if (weightedBlueprints.GetWeightedRandom(Include0Weight) is string entity)
                return entity;

            if (!Include0Weight
                && GuaranteeBlueprint)
                return GetAnEntityForCorpseWeighted(CorpseBlueprint, true, false);

            return null;
        }
        public static string GetAnEntityForCorpse(string CorpseBlueprint, bool Include0Weight = true)
            => NecromancySystem
                ?.GetWeightedEntityStringsThisCorpseCouldBe(CorpseBlueprint, Include0Weight, IsBaseBlueprint)
                ?.Keys
                ?.GetRandomElementCosmetic();

        public UD_FleshGolems_PastLife Initialize(GameObject PastLife = null)
        {
            string callingTypeAndMethod = nameof(UD_FleshGolems_PastLife) + "." + nameof(Initialize);
            if (!Init)
            {
                bool obliteratePastLife = false;
                try
                {
                    BrainInAJar ??= GetNewBrainInAJar();
                    if (BrainInAJar != null)
                    {
                        Blueprint ??= ParentObject?.GetPropertyOrTag(PASTLIFE_BLUEPRINT_PROPTAG)
                            ?? ParentObject?.GetPropertyOrTag("SourceObject")
                            ?? PastLife?.Blueprint
                            ?? GetAnEntityForCorpseWeighted(ParentObject.Blueprint);

                        obliteratePastLife = PastLife == null;
                        PastLife ??= GameObject.CreateSample(Blueprint);
                        if (PastLife == null)
                        {
                            static bool hasCorpseWithNonZeroChance(GameObjectBlueprint bp)
                            {
                                return bp.TryGetCorpseChance(out int bpCorpseChance) && bpCorpseChance > 0;
                            }
                            string blueprint = EncountersAPI.GetACreatureBlueprintModel(hasCorpseWithNonZeroChance).Name;
                            PastLife = GameObject.CreateSample(blueprint);
                        }
                        PastLife ??= GameObject.CreateSample("Trash Monk");

                        Blueprint ??= PastLife?.Blueprint;

                        if (PastLife.HasStringProperty(REANIMATED_BYBUILDER)
                            || (ParentObject.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper reanimationHelper)
                                && reanimationHelper.AlwaysAnimate))
                            WasBuiltReanimated = true;

                        if (PastLife.TryGetPart(out UD_FleshGolems_PastLife prevPastLife)
                            && prevPastLife.DeepCopy(BrainInAJar, DeepCopyMapInventory) is UD_FleshGolems_PastLife prevPastLifeCopy)
                            BrainInAJar.AddPart(prevPastLifeCopy);

                        BrainInAJar._Property = new(PastLife._Property);
                        BrainInAJar._IntProperty = new(PastLife._IntProperty);

                        if (PastLife.IsPlayer()
                            || PastLife.IsPlayerDuringWorldGen())
                            BrainInAJar.SetStringProperty("UD_FleshGolems_WasPlayer", "Yep, I used to be the player!");

                        if (PastLife.GetBlueprint().InheritsFrom(BRAIN_IN_A_JAR_BLUEPRINT)
                            || PastLife.IsInanimateCorpse()
                            || WasCorpse)
                            TimesReanimated++;

                        BrainInAJar.HasProperName = PastLife.HasProperName 
                            || (PastLife.GetxTag("Grammar", "Proper") is string properGrammar
                                && properGrammar.EqualsNoCase("true"));

                        if (ParentObject.Render.DisplayName != ParentObject.GetBlueprint().DisplayName())
                            ParentObject.SetStringProperty("CreatureName", ParentObject.Render.DisplayName);
                        else
                        if (BrainInAJar.HasProperName)
                            ParentObject.SetStringProperty("CreatureName", PastLife.GetReferenceDisplayName(Short: true));
                        else
                            ParentObject.SetStringProperty("CreatureName", null);

                        if (PastLife.HasPart<DisplayNameAdjectives>())
                            BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<DisplayNameAdjectives>());

                        if (PastLife.HasPart<Titles>())
                            BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Titles>());

                        if (PastLife.HasPart<Epithets>())
                            BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Epithets>());

                        if (PastLife.HasPart<Honorifics>())
                            BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Honorifics>());

                        PastLife.RemoveAllEffects<LiquidCovered>();
                        PastLife.RemoveAllEffects<LiquidStained>();

                        TransferRenderFieldPropsFrom(PastLife);

                        Description bIAJ_Description = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Description>());

                        if (PastLife.CurrentCell is Cell deathCell
                            && deathCell.ParentZone is Zone deathZone)
                            DeathAddress = new(deathZone.ZoneID, deathCell.Location);

                        Physics bIAJ_Physics = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.Physics);
                        BrainInAJar.Physics = bIAJ_Physics;

                        PastLife.Brain ??= PastLife.RequirePart<Brain>();
                        Brain bIAJ_Brain = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.Brain);
                        BrainInAJar.Brain = bIAJ_Brain;
                        Brain pastBrain = PastLife.Brain;
                        try
                        {
                            bIAJ_Brain.PartyLeader = pastBrain.PartyLeader;
                        }
                        catch (Exception x)
                        {
                            MetricsManager.LogException(callingTypeAndMethod + " " + nameof(bIAJ_Brain.PartyLeader), x, "game_mod_exception");
                            bIAJ_Brain.PartyLeader = null;
                        }
                        try
                        {
                            pastBrain.PartyMembers ??= new();
                            foreach ((int flags, PartyMember partyMember) in pastBrain.PartyMembers)
                            {
                                PartyMember partyMemberCopy = new(partyMember.Reference, partyMember.Flags);
                                bIAJ_Brain.PartyMembers.TryAdd(partyMemberCopy.Reference.ID, partyMemberCopy);
                            }
                        }
                        catch (Exception x)
                        {
                            MetricsManager.LogException(callingTypeAndMethod + " " + nameof(bIAJ_Brain.PartyMembers), x, "game_mod_exception");
                            bIAJ_Brain.PartyMembers = new();
                        }
                        try
                        {
                            pastBrain.Opinions ??= new();
                            foreach ((int opinionSubjectID, OpinionList opinionList) in pastBrain.Opinions)
                            {
                                OpinionList opinionsCopy = new();
                                foreach (IOpinion opinionCopy in opinionList)
                                {
                                    opinionsCopy.Add(opinionCopy);
                                }
                                bIAJ_Brain.Opinions.TryAdd(opinionSubjectID, opinionsCopy);
                            }
                        }
                        catch (Exception x)
                        {
                            MetricsManager.LogException(callingTypeAndMethod + " " + nameof(bIAJ_Brain.Opinions), x, "game_mod_exception");
                            bIAJ_Brain.Opinions = new();
                        }

                        Body bIAJ_Body = null;
                        if (Anatomies.GetAnatomy(PastLife?.Body?.Anatomy ?? "Humanoid") is Anatomy.Anatomy pastAnatomy)
                        { 
                            if (BrainInAJar.Body == null)
                            {
                                bIAJ_Body = BrainInAJar.AddPart(new Body());
                                bIAJ_Body.Anatomy = pastAnatomy.Name;
                                BrainInAJar.Body = bIAJ_Body;
                            }
                            else
                            {
                                BrainInAJar.Body.Rebuild(pastAnatomy.Name);
                                bIAJ_Body = BrainInAJar.Body; 
                            }
                        }

                        RoughlyCopyAdditionalLimbs(bIAJ_Body, PastLife?.Body, ref ExtraLimbs);

                        string bleedLiquid = PastLife.GetBleedLiquid();
                        BrainInAJar.SetBleedLiquid(bleedLiquid);

                        Corpse bIAJ_Corpse = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Corpse>());
                        if (bIAJ_Corpse.CorpseBlueprint.IsNullOrEmpty()
                            && Blueprint
                                .GetGameObjectBlueprint()
                                .TryGetCorpseBlueprint(out string corpseBlueprint))
                            bIAJ_Corpse.CorpseBlueprint = corpseBlueprint;

                        if (bIAJ_Corpse.CorpseBlueprint.IsNullOrEmpty()
                            && !ParentObject.HasPart<ReplaceObject>())
                            bIAJ_Corpse.CorpseBlueprint = ParentObject.Blueprint;

                        if (bIAJ_Corpse.CorpseBlueprint.IsNullOrEmpty()
                            && (PastLife.GetSpecies() + " Corpse").GetGameObjectBlueprint() is var corpseModel)
                            bIAJ_Corpse.CorpseBlueprint = corpseModel.Name;

                        if (bIAJ_Corpse.CorpseBlueprint.IsNullOrEmpty()
                            && ("UD_FleshGolems " + PastLife.GetSpecies() + " Corpse").GetGameObjectBlueprint() is var corpseModdedModel)
                            bIAJ_Corpse.CorpseBlueprint = corpseModdedModel.Name;

                        if (!PastLife.GenderName.IsNullOrEmpty())
                            BrainInAJar.SetGender(PastLife.GenderName);

                        if (!PastLife.PronounSetName.IsNullOrEmpty())
                            BrainInAJar.SetPronounSet(PastLife.PronounSetName);

                        ConversationScript bIAJ_Conversation = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<ConversationScript>());

                        if (!PastLife.Statistics.IsNullOrEmpty())
                        {
                            BrainInAJar.Statistics = new();
                            foreach ((string statName, Statistic stat) in PastLife?.Statistics)
                            {
                                if (stat.Name == "Hitpoints")
                                    stat.Penalty = 0;

                                Statistic newStat = new(stat)
                                {
                                    Owner = BrainInAJar,
                                };

                                BrainInAJar.Statistics.Add(statName, newStat);
                            }
                            if ((PastLife.IsPlayer()
                                    || PastLife.IsPlayerDuringWorldGen())
                                && GetPlayerEmbarkStats() is Dictionary<string, int> playerStats)
                                foreach ((string statName, int baseValue) in playerStats)
                                    BrainInAJar.GetStat(statName).BaseValue = baseValue;
                        }

                        EntityTaxa = new(PastLife);
                        BrainInAJar.SetSpecies(EntityTaxa.Species);
                        BrainInAJar.SetGenotype(EntityTaxa.Genotype);
                        BrainInAJar.SetSubtype(EntityTaxa.Subtype);

                        Mutations bIAJ_Mutations = BrainInAJar.AddPart(PastLife.RequirePart<Mutations>());
                        if (!bIAJ_Mutations.ActiveMutationList.IsNullOrEmpty())
                            foreach (BaseMutation bIAJ_Mutation in bIAJ_Mutations.ActiveMutationList)
                            {
                                MutationData mutationData = new(bIAJ_Mutation, bIAJ_Mutation.GetRapidLevelAmount());
                                MutationsList.Add(mutationData);
                            }

                        Skills bIAJ_Skills = BrainInAJar.AddPart(PastLife.RequirePart<Skills>());
                        List<BaseSkill> pastSkills = PastLife.GetPartsDescendedFrom<BaseSkill>();
                        if (!pastSkills.IsNullOrEmpty())
                            foreach (BaseSkill baseSkill in pastSkills)
                                if (!bIAJ_Skills.SkillList.Contains(baseSkill))
                                    bIAJ_Skills.AddSkill(baseSkill);

                        if (!PastLife.Effects.IsNullOrEmpty())
                            foreach (Effect pastEffect in PastLife.Effects)
                                BrainInAJar.Effects.Add(pastEffect.DeepCopy(BrainInAJar, null));

                        if (PastLife?.Body is Body pastBody
                            && pastBody.GetInstalledCyberneticsReadonly() is List<GameObject> pastInstalledCybernetics
                            && InstalledCybernetics.IsNullOrEmpty())
                            foreach (GameObject pastInstalledCybernetic in pastInstalledCybernetics)
                                if (pastInstalledCybernetic?.Implantee?.Body is Body implanteeBody)
                                {
                                    InstalledCybernetic installedCybernetic = new(pastInstalledCybernetic, implanteeBody);
                                    InstalledCybernetics.Add(installedCybernetic);
                                }

                        if (!PastLife.PartsList.IsNullOrEmpty())
                        {
                            foreach (IPart pastPart in PastLife.PartsList)
                            {
                                if (BrainInAJar.HasPart(pastPart.Name)
                                    || PartsToNotRetain.Contains(pastPart.Name)
                                    || pastPart is BaseSkill
                                    || pastPart is BaseMutation)
                                    continue;

                                BrainInAJar.OverrideWithDeepCopyOrRequirePart(pastPart, DeepCopyMapInventory);
                                pastPart.ParentObject = BrainInAJar;
                            }
                        }
                    }
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(Name + "." + nameof(Initialize), x, "game_mod_exception");
                }
                finally
                {
                    Init = true;
                    if (obliteratePastLife)
                        PastLife?.Obliterate();
                }
            }
            return this;
        }

        public UD_FleshGolems_PastLife Initialize(UD_FleshGolems_PastLife PrevPastLife)
        {
            if (!Init)
            {
                if (PrevPastLife != null && PrevPastLife.Init)
                {
                    Initialize(PrevPastLife?.BrainInAJar);
                    TimesReanimated++;
                }
                else
                if (PrevPastLife?.ParentObject is GameObject pastLife)
                    Initialize(pastLife);
            }
            return this;
        }

        public GameObjectBlueprint GetBlueprint()
            => Blueprint?.GetGameObjectBlueprint();

        public bool TryGetBlueprint(out GameObjectBlueprint GameObjectBlueprint)
            => (GameObjectBlueprint = Blueprint?.GetGameObjectBlueprint()) != null;

        public static bool IsFactionRelationshipPropTag(KeyValuePair<string, string> Entry)
            => !Entry.Key.IsNullOrEmpty()
            && (Entry.Key.StartsWith("staticFaction") 
                || Entry.Key == "NoHateFactions");

        public static bool TransferRenderFieldProps(
            GameObject Source,
            GameObject Destination,
            Predicate<KeyValuePair<string, Type>> Filter = null)
        {
            if (Source == null
                || Destination == null
                || Source.Render is not Render sourceRender)
                return false;

            Render destinationRender = Destination.Render ?? Destination.RequirePart<Render>();
            Destination.Render = destinationRender;

            Dictionary<string, Traverse> sourceRenderFieldsProps = Source.Render
                ?.GetAssignableDeclaredFieldAndPropertyDictionary(HasValueIsPrimativeType);

            Dictionary<string, Traverse> destinationRenderFieldsProps = Destination.Render
                ?.GetAssignableDeclaredFieldAndPropertyDictionary(HasValueIsPrimativeType);

            bool any = false;
            if (!sourceRenderFieldsProps.IsNullOrEmpty() && !destinationRenderFieldsProps.IsNullOrEmpty())
            {
                foreach ((string sourceFieldPropName, Traverse sourceRenderFieldProp) in sourceRenderFieldsProps)
                {
                    object sourceRenderFieldPropValue = sourceRenderFieldProp.GetValue();
                    try
                    {
                        if (destinationRenderFieldsProps.ContainsKey(sourceFieldPropName)
                            && destinationRenderFieldsProps[sourceFieldPropName].GetValueType() == sourceRenderFieldProp.GetValueType()
                            && Filter == null || Filter(new(sourceFieldPropName, sourceRenderFieldProp.GetValueType())))
                        {
                            destinationRenderFieldsProps[sourceFieldPropName].SetValue(sourceRenderFieldPropValue);
                            any = true;
                        }
                    }
                    catch (Exception x)
                    {
                        MetricsManager.LogException(
                            nameof(UD_FleshGolems_PastLife) + " " +
                            nameof(TransferRenderFieldProps) + " " +
                            nameof(Destination.Render) + ": " +
                            sourceRenderFieldPropValue,
                            x,
                            GAME_MOD_EXCEPTION);
                    }
                }
            }
            return any;
        }
        public bool TransferRenderFieldPropsFrom(GameObject Source, Predicate<KeyValuePair<string, Type>> Filter = null)
            => TransferRenderFieldProps(Source, BrainInAJar, Filter);

        public bool TransferRenderFieldPropsTo(GameObject Destination, Predicate<KeyValuePair<string, Type>> Filter = null)
            => TransferRenderFieldProps(BrainInAJar, Destination, Filter);

        public static bool RestoreFactionRelationships(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife)
        {
            if (FrankenCorpse == null
                || PastLife == null
                || !PastLife.TryGetBlueprint(out GameObjectBlueprint sourceBlueprint))
                return false;

            foreach ((string tagName, string tagValue) in sourceBlueprint?.Tags?.Where(IsFactionRelationshipPropTag) ?? new Dictionary<string, string>())
                FrankenCorpse.SetStringProperty(tagName, tagValue);

            foreach ((string propName, string proprValue) in sourceBlueprint?.Props?.Where(IsFactionRelationshipPropTag) ?? new Dictionary<string, string>())
                FrankenCorpse.SetStringProperty(propName, proprValue);

            return true;
        }
        public bool RestoreFactionRelationships()
            => RestoreFactionRelationships(ParentObject, this);

        public static bool IsPropTagToRestore<T>(KeyValuePair<string, T> Entry)
            => !Entry.Key.IsNullOrEmpty()
            && !Entry.Key.StartsWith("Semantic")
            && !PropTagsToNotRestore.Contains(Entry.Key)
            && (Entry.Value is not string stringValue
                || !IsFactionRelationshipPropTag(new(Entry.Key, stringValue)))
            && !Entry.Key.StartsWith("UD_FleshGolems");

        public static bool RestoreSelectPropTags(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife)
        {
            if (FrankenCorpse == null
                || PastLife == null
                || !PastLife.TryGetBlueprint(out GameObjectBlueprint sourceBlueprint))
                return false;

            foreach ((string tagName, string tagValue) in sourceBlueprint?.Tags?.Where(IsPropTagToRestore) ?? new Dictionary<string, string>())
                FrankenCorpse.SetStringProperty(tagName, tagValue);

            foreach ((string propName, string propValue) in sourceBlueprint?.Props?.Where(IsPropTagToRestore) ?? new Dictionary<string, string>())
                FrankenCorpse.SetStringProperty(propName, propValue);

            foreach ((string intPropName, int intPropValue) in sourceBlueprint?.IntProps?.Where(IsPropTagToRestore) ?? new Dictionary<string, int>())
                FrankenCorpse.SetIntProperty(intPropName, intPropValue);

            return true;
        }
        public bool RestoreSelectPropTags()
            => RestoreSelectPropTags(ParentObject, this);

        public static bool AlignWithPreviouslySentientBeings(
            Brain FrankenBrain,
            UD_FleshGolems_PastLife PastLife)
        {
            if (FrankenBrain == null
                || PastLife == null
                || PastLife.Brain is not Brain pastBrain
                || FrankenBrain.ParentObject is not GameObject frankenCorpse)
                return false;

            if (frankenCorpse.GetIntProperty("UD_FleshGolems Alignment Adjusted") > 0
                && FrankenBrain.Allegiance.Any(a => a.Key == PREVIOUSLY_SENTIENT_BEINGS && a.Value > 0))
                return false;

            int previouslySentientBeingsRep = 100;

            if (PastLife.WasBuiltReanimated)
                previouslySentientBeingsRep -= 25;

            if (!UD_FleshGolems_Reanimated.HasWorldGenerated)
                previouslySentientBeingsRep -= 25;

            if (FrankenBrain.Allegiance.ContainsKey(PREVIOUSLY_SENTIENT_BEINGS))
            {
                int existingRep = FrankenBrain.Allegiance[PREVIOUSLY_SENTIENT_BEINGS];
                previouslySentientBeingsRep = Math.Max(-100, Math.Min(previouslySentientBeingsRep + existingRep, 100));
                FrankenBrain.Allegiance[PREVIOUSLY_SENTIENT_BEINGS] = previouslySentientBeingsRep;
            }
            else
                FrankenBrain.Allegiance.Add(PREVIOUSLY_SENTIENT_BEINGS, previouslySentientBeingsRep);

            frankenCorpse.SetIntProperty("UD_FleshGolems Alignment Adjusted", 1);
            return true;
        }
        public bool AlignWithPreviouslySentientBeings()
            => AlignWithPreviouslySentientBeings(Brain, this);

        public static bool RestoreBrain(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            bool ExcludedFromDynamicEncounters,
            out Brain FrankenBrain)
        {
            FrankenBrain = null;
            if (FrankenCorpse == null
                || PastLife == null)
                return false;

            FrankenBrain = FrankenCorpse.Brain;
            if (FrankenBrain != null
                && PastLife?.Brain is Brain pastBrain)
            {
                FrankenBrain.Allegiance ??= new();
                FrankenBrain.Allegiance.Clear();

                if (!UD_FleshGolems_Reanimated.HasWorldGenerated
                    || ExcludedFromDynamicEncounters
                    || PastLife.WasBuiltReanimated
                    || PastLife.WasPlayer)
                {
                    foreach ((string factionName, int repValue) in pastBrain.Allegiance)
                    {
                        if (!pastBrain.Allegiance.ContainsKey(factionName))
                            FrankenBrain.Allegiance.Add(factionName, repValue);
                        else
                        {
                            int clampedRepValue = Math.Min(Math.Max(-100, FrankenBrain.Allegiance[factionName] + repValue), 100);
                            FrankenBrain.Allegiance[factionName] = clampedRepValue;
                        }
                    }

                    if (!FrankenCorpse.HasPropertyOrTag("StartingPet") && !FrankenCorpse.HasPropertyOrTag("Pet"))
                    {
                        FrankenBrain.PartyMembers = pastBrain.PartyMembers;
                        foreach ((int memberID, PartyMember partyMember) in FrankenBrain.PartyMembers)
                            if (pastBrain.PartyLeader != null
                                && memberID == pastBrain.PartyLeader.BaseID)
                            {
                                FrankenBrain.SetPartyLeader(pastBrain.PartyLeader, Flags: partyMember.Flags, Silent: true);
                                break;
                            }
                    }
                }

                if (PastLife.GetIdentityType() is IdentityType identityType
                    && identityType > IdentityType.NamedVillager)
                    AlignWithPreviouslySentientBeings(FrankenBrain, PastLife);

                FrankenBrain.Allegiance.Hostile = pastBrain.Allegiance.Hostile;
                FrankenBrain.Allegiance.Calm = pastBrain.Allegiance.Calm;

                FrankenBrain.Flags = pastBrain.Flags;

                FrankenBrain.MaxKillRadius = pastBrain.MaxKillRadius;
                FrankenBrain.MaxMissileRange = pastBrain.MaxMissileRange;
                FrankenBrain.MaxWanderRadius = pastBrain.MaxWanderRadius;
                FrankenBrain.MinKillRadius = pastBrain.MinKillRadius;

                FrankenBrain.LastThought = "*wilhelm scream*";

                if (PastLife.DeathAddress.GetCell() is Cell deathCell)
                {
                    FrankenBrain.StartingCell = new(deathCell);
                    GlobalLocation startingCell = FrankenBrain.StartingCell;
                }

                return true;
            }
            return false;
        }
        public bool RestoreBrain(
            bool ExcludedFromDynamicEncounters,
            out Brain FrankenBrain)
            => RestoreBrain(ParentObject, this, ExcludedFromDynamicEncounters, out FrankenBrain);

        public enum IdentityType : int
        {
            Player,
            Librarian,
            Warden,
            NamedVillager,
            Named,
            Hero,
            ParticipantVillager,
            Villager,
            Nobody,
            Corpse,
            None,
        }
        public static IdentityType GetIdentityType(UD_FleshGolems_PastLife PastLife)
        {
            if (PastLife == null
                || PastLife.ParentObject is not GameObject frankenCorpse)
                return IdentityType.None;

            if (PastLife.WasPlayer || frankenCorpse.IsPlayer())
                return IdentityType.Player;

            if (frankenCorpse.IsLibrarian())
                return IdentityType.Librarian;

            if (frankenCorpse.IsVillageWarden())
                return IdentityType.Warden;

            if (frankenCorpse.IsNamedVillager())
                return IdentityType.NamedVillager;

            if (PastLife.WasProperlyNamed)
                return IdentityType.Named;

            if (PastLife.BrainInAJar.HasPropertyOrTag("Hero") 
                || (PastLife.BrainInAJar.GetPropertyOrTag("Role") is string bIAJRolePropTag && bIAJRolePropTag == "Hero"))
                return IdentityType.Hero;

            if (frankenCorpse.HasPropertyOrTag("Hero") 
                || (frankenCorpse.GetPropertyOrTag("Role") is string frankenRolePropTag && frankenRolePropTag == "Hero"))
                return IdentityType.Hero;

            if (PastLife.WasCorpse)
                return IdentityType.Corpse;

            if (frankenCorpse.IsParticipantVillager())
                return IdentityType.ParticipantVillager;

            if (frankenCorpse.IsVillager())
                return IdentityType.Villager;

            return IdentityType.Nobody;
        }
        public IdentityType GetIdentityType()
            => GetIdentityType(this);

        public static IdentityType GetLivingIdentityType(GameObject Entity)
        {
            if (Entity == null)
                return IdentityType.None;

            if (Entity.IsPlayer() || Entity.IsPlayerDuringWorldGen())
                return IdentityType.Player;

            if (Entity.IsLibrarian())
                return IdentityType.Librarian;

            if (Entity.IsVillageWarden())
                return IdentityType.Warden;

            if (Entity.IsNamedVillager())
                return IdentityType.NamedVillager;

            if (WasProperlyNamed(Entity))
                return IdentityType.Named;

            if (Entity.HasPropertyOrTag("Hero")
                || (Entity.GetPropertyOrTag("Role") is string rolePropTag && rolePropTag == "Hero"))
                return IdentityType.Hero;

            if (Entity.IsInanimateCorpse())
                return IdentityType.Corpse;

            if (Entity.IsParticipantVillager())
                return IdentityType.ParticipantVillager;

            if (Entity.IsVillager())
                return IdentityType.Villager;

            return IdentityType.Nobody;
        }

        public static string GenerateDisplayName(UD_FleshGolems_PastLife PastLife, out IdentityType IdentityType)
        {
            IdentityType = IdentityType.None;
            if (PastLife?.ParentObject is not GameObject frankenCorpse)
                return null;

            IdentityType = PastLife.GetIdentityType();

            if (frankenCorpse.Render.DisplayName != frankenCorpse.GetBlueprint().DisplayName())
                frankenCorpse.SetStringProperty("CreatureName", frankenCorpse.Render.DisplayName);
            else
            if (IdentityType <= IdentityType.Named)
                frankenCorpse.SetStringProperty("CreatureName", PastLife.BrainInAJar.GetReferenceDisplayName(Short: true));
            else
                frankenCorpse.SetStringProperty("CreatureName", null);

            string creatureName = frankenCorpse.GetStringProperty("CreatureName");

            if (creatureName.IsNullOrEmpty())
                frankenCorpse.SetStringProperty("CreatureName", creatureName = frankenCorpse.Render?.DisplayName);

            return (IdentityType switch
            {
                IdentityType.Player => The.Game.PlayerName,

                IdentityType.Librarian => frankenCorpse.Render?.DisplayName,
                
                IdentityType.Warden
                or IdentityType.NamedVillager
                or IdentityType.Named
                or IdentityType.Hero => creatureName,

                IdentityType.ParticipantVillager 
                or IdentityType.Villager 
                or IdentityType.Nobody => PastLife.RefName,

                IdentityType.Corpse => frankenCorpse.Render?.DisplayName,

                _ => frankenCorpse?.DisplayName,
            })
            ?.RemoveAll("[", "]");
        }
        public string GenerateDisplayName(out IdentityType IdentityType)
            => GenerateDisplayName(this, out IdentityType);

        public string GenerateDisplayName()
            => GenerateDisplayName(out _);

        public static string GeneratePostDescription(UD_FleshGolems_PastLife PastLife, out IdentityType IdentityType)
        {
            IdentityType = IdentityType.None;

            if (PastLife == null
                || PastLife.Description.IsNullOrEmpty()
                || PastLife.ParentObject is not GameObject frankenCorpse)
                return null;

            IdentityType = PastLife.GetIdentityType();

            string inLife = "In life this =subject.uD_xTag:UD_FleshGolems_CorpseText:CorpseDescription= was ";
            string whoTheyWere = (PastLife.RefName ?? PastLife.GetBlueprint()?.DisplayName() ?? "unfortunate soul");
            string endMark = ":\n";
            string oldDescription = PastLife.Description;

            if (IdentityType == IdentityType.Corpse)
            {
                if (PastLife?.PastPastLife?.GetIdentityType() < IdentityType.Villager)
                    whoTheyWere = "the " + whoTheyWere;
                else
                    whoTheyWere = Grammar.A(whoTheyWere);

                oldDescription = null;
                endMark = ".";
            }
            if (IdentityType > IdentityType.Named && IdentityType < IdentityType.Corpse)
                whoTheyWere = Grammar.A(whoTheyWere);

            if (IdentityType == IdentityType.Librarian)
            {
                whoTheyWere = frankenCorpse.Render?.DisplayName ?? "Sheba Hagadias";
                if (GameObject.CreateSample("UD_FleshGolems_Sample_Librarian") is GameObject sampleLibrarian)
                {
                    if (sampleLibrarian.TryGetPart(out MechanimistLibrarian librarianPart))
                    {
                        librarianPart.Initialize();
                        if (sampleLibrarian.TryGetPart(out Description librarianDescription))
                            oldDescription = librarianDescription.Short;

                        whoTheyWere = sampleLibrarian.DisplayName;
                    }
                    if (GameObject.Validate(ref sampleLibrarian))
                        sampleLibrarian.Obliterate();
                }
            }
            if (IdentityType == IdentityType.Player)
            {
                whoTheyWere = "you";
                oldDescription = null;
                endMark = ".";
            }
            return inLife + whoTheyWere.RemoveAll("[", "]") + endMark + oldDescription;
        }
        public string GeneratePostDescription(out IdentityType IdentityType)
            => GeneratePostDescription(this, out IdentityType);

        public string GeneratePostDescription()
            => GeneratePostDescription(out _);

        public static bool RestoreGenderIdentity(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            bool WantOldIdentity = false)
        {
            if (FrankenCorpse == null
                || PastLife == null
                || !WantOldIdentity)
                return false;

            if (PastLife.Gender?.Name is string pastGenderName)
                FrankenCorpse.SetGender(pastGenderName);

            if (PastLife.PronounSet?.Name is string pastPronounSetName)
                FrankenCorpse.SetPronounSet(pastPronounSetName);

            return true;
        }
        public bool RestoreGenderIdentity(bool WantOldIdentity = true)
            => RestoreGenderIdentity(ParentObject, this, WantOldIdentity);

        public static bool RestoreAnatomy(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out Body FrankenBody)
        {
            FrankenBody = null;
            if (FrankenCorpse == null
                || PastLife == null
                || PastLife.Body == null)
                return false;

            if (PastLife.Body is Body pastBody
                && Anatomies.GetAnatomy(pastBody?.Anatomy) is Anatomy.Anatomy anatomy)
            {
                if (FrankenCorpse.Body == null)
                    FrankenCorpse.AddPart(new Body()).Anatomy = anatomy.Name;
                else
                    FrankenCorpse.Body.Rebuild(anatomy.Name);
            }
            FrankenBody = FrankenCorpse.Body;
            return true;
        }
        public bool RestoreAnatomy(out Body FrankenBody)
            => RestoreAnatomy(ParentObject, this, out FrankenBody);

        public static bool IsAbstractOrExtrinsic(BodyPart BodyPart)
            => BodyPart != null 
            && (BodyPart.Abstract
                || BodyPart.Extrinsic);

        public static bool IsConcreteIntrinsic(BodyPart BodyPart)
            => BodyPart != null
            && !IsAbstractOrExtrinsic(BodyPart);

        public static bool IsManaged(BodyPart BodyPart)
            => BodyPart != null
            && !BodyPart.Manager.IsNullOrEmpty();

        public static bool IsManagedConcreteIntrinsic(BodyPart BodyPart)
            => BodyPart != null
            && IsManaged(BodyPart)
            && IsConcreteIntrinsic(BodyPart);

        public static bool IsAbstractOrExtrinsicOrNonNative(BodyPart BodyPart)
            => BodyPart != null
            && (IsAbstractOrExtrinsic(BodyPart)
                || !BodyPart.Native);

        public static bool IsNonChimericConcreteIntrinsicNative(BodyPart BodyPart)
            => BodyPart != null
            && BodyPart.Manager != "Chimera"
            && !IsAbstractOrExtrinsicOrNonNative(BodyPart);

        public static bool HasManagedConcreteIntrinsicSubpart(BodyPart BodyPart)
            => BodyPart.LoopSubparts().Any(IsManagedConcreteIntrinsic);

        public static bool RoughlyCopyAdditionalLimbs(
            Body DestinationBody,
            Body SourceBody,
            ref Dictionary<PseudoLimb, string> ExtraLimbs)
        {
            if (DestinationBody == null
                || DestinationBody.ParentObject is not GameObject destinationObject
                || SourceBody == null
                || SourceBody.ParentObject is not GameObject sourceObject)
                return false;

            int amountGiven = 0;
            ExtraLimbs ??= new();
            int totalSourceParts = SourceBody?.LoopParts()?.Count() ?? 0;
            int totalDestinationParts = 0;
            if (!ExtraLimbs.IsNullOrEmpty())
            {
                foreach ((PseudoLimb pseudoLimb, string _) in ExtraLimbs)
                    pseudoLimb.DebugPseudoLimb();

                foreach ((PseudoLimb pseudoLimb, string targetBodyPartType) in ExtraLimbs)
                {
                    BodyPart targetBodyPart = 
                        DestinationBody.LoopPart(targetBodyPartType)
                            ?.GetRandomElementCosmetic()
                        ?? DestinationBody.LoopParts(IsConcreteIntrinsic)
                            ?.GetRandomElementCosmetic();

                    if (targetBodyPart == null)
                        break;

                    pseudoLimb.GiveToEntity(destinationObject, targetBodyPart, ref amountGiven);
                }
                totalDestinationParts = DestinationBody?.LoopParts()?.Count() ?? 0;
                return true;
            }
            List<BodyPart> bodyPartsToLoop = SourceBody?.LoopParts()
                ?.Where(IsNonChimericConcreteIntrinsicNative)
                ?.Where(HasManagedConcreteIntrinsicSubpart)
                ?.ToList();
            List<BodyPart> accountedForBodyParts = new();
            List<BodyPart> accountedForDestinationBodyParts = new();
            int amountStored = 0;
            foreach (BodyPart bodyPart in bodyPartsToLoop)
            {
                string bodyPartName = bodyPart?.BodyPartString(WithManager: true).Strip();

                accountedForBodyParts.Add(bodyPart);

                List<BodyPart> subPartsToLoop = bodyPart?.LoopSubparts()
                    ?.Where(IsManagedConcreteIntrinsic)
                    ?.Where(bp => bp.Manager == "Chimera")
                    ?.ToList();

                if (subPartsToLoop.IsNullOrEmpty())
                    continue;

                List<PseudoLimb> bodyPartExtraLimbs = new();
                foreach (BodyPart subPart in subPartsToLoop)
                {
                    string subPartName = subPart?.BodyPartString(WithManager: true)?.Strip();
                    if (accountedForBodyParts.Contains(subPart))
                        continue;

                    if (subPart.Manager.IsNullOrEmpty())
                        continue;

                    accountedForBodyParts.Add(subPart);
                    PseudoLimb pseudoLimb = new(subPart, null, null, ref amountStored);
                    ExtraLimbs.Add(pseudoLimb, bodyPart.Type);
                    bodyPartExtraLimbs.Add(pseudoLimb);
                }
                if (bodyPartExtraLimbs.IsNullOrEmpty())
                    continue;

                List<BodyPart> potentialTargetBodyParts = DestinationBody?.LoopParts()
                    ?.Where(IsNonChimericConcreteIntrinsicNative)
                    ?.Where(bp => !accountedForDestinationBodyParts.Contains(bp))
                    ?.ToList();

                BodyPart targetBodyPart = 
                    potentialTargetBodyParts
                        ?.GetRandomElementCosmeticExcluding(bp => bp.Type != bodyPart.Type)
                    ?? potentialTargetBodyParts
                        ?.GetRandomElementCosmetic();

                if (targetBodyPart != null)
                {
                    accountedForDestinationBodyParts.Add(targetBodyPart);
                    foreach (PseudoLimb bodyPartExtraLimb in bodyPartExtraLimbs)
                        bodyPartExtraLimb.GiveToEntity(destinationObject, targetBodyPart, ref amountGiven);
                }
            }
            totalDestinationParts = DestinationBody?.LoopParts()?.Count() ?? 0;
            return true;
        }
        public static bool RestoreAdditionalLimbs(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out Body FrankenBody)
        {
            FrankenBody = null;
            if (FrankenCorpse == null
                || FrankenCorpse.Body == null
                || PastLife == null
                || PastLife.Body == null)
                return false;

            return RoughlyCopyAdditionalLimbs(FrankenCorpse.Body, PastLife.Body, ref PastLife.ExtraLimbs);
        }
        public bool RestoreAdditionalLimbs(out Body FrankenBody)
            => RestoreAdditionalLimbs(ParentObject, this, out FrankenBody);

        public bool RestoreAdditionalLimbs()
            => RestoreAdditionalLimbs(out _);

        public static bool RestoreTaxonomy(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife)
        {
            if (FrankenCorpse == null
                || PastLife == null)
                return false;

            if (PastLife.EntityTaxa != null
                && PastLife.EntityTaxa.RestoreTaxa(FrankenCorpse))
                return true;

            if (PastLife.Species is string pastSpecies)
                FrankenCorpse.SetSpecies(pastSpecies);

            if (PastLife.Genotype is string pastGenotype
                && FrankenCorpse.GetGenotype() == null)
                FrankenCorpse.SetGenotype(pastGenotype);

            if (PastLife.Subtype is string pastSubtype
                && FrankenCorpse.GetSubtype() == null)
                FrankenCorpse.SetSubtype(pastSubtype);

            return true;
        }
        public bool RestoreTaxonomy()
            => RestoreTaxonomy(ParentObject, this);

        public static bool RestoreMutations(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out Mutations FrankenMutations,
            Predicate<BaseMutation> Exclude = null)
        {
            FrankenMutations = null;
            bool any = false;
            if (FrankenCorpse == null
                || PastLife == null
                || PastLife.Mutations == null
                || PastLife.Mutations.ActiveMutationList.IsNullOrEmpty())
                return any;

            FrankenMutations = FrankenCorpse.RequirePart<Mutations>();
            foreach (BaseMutation baseMutation in PastLife.Mutations.ActiveMutationList)
            {
                BaseMutation baseMutationToAdd = baseMutation;
                bool alreadyHaveMutation = FrankenMutations.HasMutation(baseMutation.Name);
                if (alreadyHaveMutation)
                    baseMutationToAdd = FrankenMutations.GetMutation(baseMutation.Name);

                if (Exclude != null && Exclude(baseMutationToAdd))
                    continue;

                if (baseMutationToAdd.CapOverride == -1)
                    baseMutationToAdd.CapOverride = baseMutation.Level;

                if (!alreadyHaveMutation)
                    FrankenMutations.AddMutation(baseMutationToAdd.Name, baseMutationToAdd.Variant, baseMutation.Level);
                else
                    baseMutationToAdd.BaseLevel += baseMutation.Level;

                FrankenMutations.AddMutation(baseMutationToAdd, baseMutation.BaseLevel);

                any = true;
            }
            return any;
        }
        public bool RestoreMutations(
            out Mutations FrankenMutations,
            Predicate<BaseMutation> Exclude = null)
            => RestoreMutations(ParentObject, this, out FrankenMutations, Exclude);

        public static bool RestoreSkills(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out Skills FrankenSkills,
            Predicate<BaseSkill> Exclude = null)
        {
            FrankenSkills = null;
            bool any = false;
            if (FrankenCorpse == null
                || PastLife == null
                || PastLife.Skills == null
                || PastLife.Skills.SkillList.IsNullOrEmpty())
                return any;

            FrankenSkills = FrankenCorpse.RequirePart<Skills>();
            foreach (BaseSkill baseSkill in PastLife.Skills.SkillList)
            {
                if ((Exclude != null && Exclude(baseSkill))
                    || FrankenCorpse.HasSkill(baseSkill.Name)
                    || FrankenCorpse.HasPart(baseSkill.Name))
                    continue;

                any = FrankenSkills.AddSkill(baseSkill.DeepCopy(FrankenCorpse, null) as BaseSkill) || any;
            }
            return any;
        }

        public bool RestoreSkills(
            out Skills FrankenSkills,
            Predicate<BaseSkill> Exclude = null)
            => RestoreSkills(ParentObject, this, out FrankenSkills, Exclude);

        public static bool RestoreCybernetics(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out bool WereCyberneticsInstalled)
        {
            WereCyberneticsInstalled = false;
            if (FrankenCorpse == null
                || FrankenCorpse.Body is not Body frankenBody
                || PastLife == null
                || PastLife.InstalledCybernetics.IsNullOrEmpty())
                return false;

            foreach ((GameObject cybernetic, string bodyPartType) in PastLife.InstalledCybernetics)
            {
                if (frankenBody.FindCybernetics(cybernetic) != null)
                    continue;

                if (cybernetic.DeepCopy() is GameObject newCybernetic
                    && newCybernetic.TryRemoveFromContext())
                    if (newCybernetic.TryGetPart(out CyberneticsBaseItem cyberneticBasePart))
                    {
                        int cyberneticsCost = cyberneticBasePart.Cost;
                        FrankenCorpse.ModIntProperty(CYBERNETICS_LICENSES, cyberneticsCost);
                        FrankenCorpse.ModIntProperty(CYBERNETICS_LICENSES_FREE, cyberneticsCost);

                        List<BodyPart> bodyParts = frankenBody.GetPart(bodyPartType);
                        bodyParts.ShuffleInPlace();

                        foreach (BodyPart bodyPart in bodyParts)
                            if (bodyPart.CanReceiveCyberneticImplant()
                                && !bodyPart.HasInstalledCybernetics())
                            {
                                bodyPart.Implant(newCybernetic);
                                WereCyberneticsInstalled = true;
                                break;
                            }
                    }
            }
            return true;
        }
        public bool RestoreCybernetics(out bool WereCyberneticsInstalled)
            => RestoreCybernetics(ParentObject, this, out WereCyberneticsInstalled);

        public static bool RestoreParts(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            Predicate<IPart> Filter = null)
        {
            if (FrankenCorpse == null
                || PastLife == null
                || PastLife.BrainInAJar == null
                || PastLife.BrainInAJar.PartsList.IsNullOrEmpty())
                return false;

            List<string> bIAJ_BlueprintParts = new();
            if (GameObjectFactory.Factory.GetBlueprintIfExists(BRAIN_IN_A_JAR_BLUEPRINT) is GameObjectBlueprint bIAJ_Blueprint)
                bIAJ_BlueprintParts = bIAJ_Blueprint.Parts?.Values?.Select(p => p.Name)?.ToList();

            foreach (IPart pastPart in PastLife.BrainInAJar.PartsList)
                if (!FrankenCorpse.HasPart(pastPart.Name)
                    && !PartsToNotRetain.Contains(pastPart.Name)
                    && pastPart is not BaseSkill
                    && pastPart is not BaseMutation
                    && !bIAJ_BlueprintParts.Contains(pastPart.Name)
                    && (Filter == null || Filter(pastPart)))
                    FrankenCorpse.AddPart(pastPart);

            return true;
        }
        public bool RestoreParts(Predicate<IPart> Filter = null)
            => RestoreParts(ParentObject, this, Filter);

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == GetDebugInternalsEvent.ID
            ;

        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            E.AddEntry(this, nameof(Init), Init);
            E.AddEntry(this, nameof(WasBuiltReanimated), WasBuiltReanimated);
            E.AddEntry(this, nameof(WasCorpse), WasCorpse);
            E.AddEntry(this, nameof(WasPlayer), WasPlayer);
            E.AddEntry(this, nameof(TimesReanimated), TimesReanimated);
            E.AddEntry(this, nameof(Blueprint), Blueprint);
            E.AddEntry(this, nameof(BaseDisplayName), BaseDisplayName);
            E.AddEntry(this, nameof(RefName), RefName);
            IdentityType identityType = GetIdentityType(); 
            E.AddEntry(this, nameof(IdentityType), identityType.ToStringWithNum());
            E.AddEntry(this, nameof(WasProperlyNamed), WasProperlyNamed);

            if (DisplayNameAdjectives != null && !DisplayNameAdjectives.AdjectiveList.IsNullOrEmpty())
                E.AddEntry(this, nameof(DisplayNameAdjectives),
                    DisplayNameAdjectives.AdjectiveList
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            else
                E.AddEntry(this, nameof(DisplayNameAdjectives), "Empty");

            E.AddEntry(this, nameof(Titles), Titles?.TitleList ?? "Empty");
            E.AddEntry(this, nameof(Epithets), Epithets?.EpithetList ?? "Empty");
            E.AddEntry(this, nameof(Honorifics), Honorifics?.HonorificList ?? "Empty");

            if (PastRender != null)
                try
                {
                    List<string> pastRenderFieldPropValues = BrainInAJar.Render
                        ?.GetAssignableDeclaredFieldAndPropertyDictionary(HasValueIsPrimativeType)
                        ?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.GetValue()?.ToString() ?? NULL)
                        ?.ConvertToStringList(kvp => kvp.Key + ": " + kvp.Value)
                        ?.ToList();

                    E.AddEntry(this, nameof(PastRender), pastRenderFieldPropValues?.GenerateBulletList(Bullet: null, BulletColor: null));
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(nameof(GetDebugInternalsEvent) + "->" + nameof(PastRender), x, "game_mod_exception");
                    E.AddEntry(this, nameof(PastRender), "threw exception.");
                }
            else
                E.AddEntry(this, nameof(PastRender), "Empty");

            E.AddEntry(this, nameof(Description), YehNah(Description != null));
            E.AddEntry(this, nameof(DeathAddress), DeathAddress.ToString());

            if (Brain != null && !Brain.Allegiance.IsNullOrEmpty())
                try
                {
                    List<string> brainList = new()
                    {
                        Brain.Allegiance
                            ?.ToList()
                            ?.ConvertAll(a => nameof(Brain.Allegiance) + ": " + a.Key + "-" + a.Value)
                            ?.GenerateBulletList(
                                Bullet: null,
                                BulletColor: null),
                        Brain
                            ?.GetAssignableDeclaredFieldAndPropertyDictionary(HasValueIsPrimativeType)
                            ?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.GetValue()?.ToString() ?? NULL)
                            ?.ConvertToStringList(kvp => kvp.Key + ": " + kvp.Value)
                            ?.GenerateBulletList(
                                Bullet: null,
                                BulletColor: null),
                    };
                    E.AddEntry(this, nameof(Brain), brainList?.GenerateBulletList(Bullet: null, BulletColor: null));
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(nameof(GetDebugInternalsEvent) + "->" + nameof(Brain), x, "game_mod_exception");
                    E.AddEntry(this, nameof(Brain), "threw exception.");
                }
            else
                E.AddEntry(this, nameof(Brain), "Empty");

            E.AddEntry(this, nameof(Gender), Gender?.Name ?? NULL);
            E.AddEntry(this, nameof(PronounSet), PronounSet?.Name ?? NULL);
            E.AddEntry(this, nameof(ConversationScriptID), ConversationScriptID);

            if (!Stats.IsNullOrEmpty())
                E.AddEntry(this, nameof(Stats),
                    Stats
                    ?.ConvertToStringList(kvp => kvp.Key + ": " + (kvp.Value?.Value ?? 0) + "/" + (kvp.Value?.BaseValue ?? 0))
                    ?.ToList()
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            else
                E.AddEntry(this, nameof(Stats), "Empty");

            E.AddEntry(this, nameof(Body), Body?.Anatomy ?? "No Anatomy!?");

            if (!ExtraLimbs.IsNullOrEmpty())
                E.AddEntry(this, nameof(ExtraLimbs),
                    ExtraLimbs
                    ?.ConvertToStringList(kvp => "for " + kvp.Value + ": " + kvp.Key.ToString())
                    ?.ToList()
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            else
                E.AddEntry(this, nameof(ExtraLimbs), "Empty");

            E.AddEntry(this, nameof(Corpse), (Corpse?.CorpseBlueprint ?? NULL) + " (" + (Corpse?.CorpseChance ?? 0) + ")");

            E.AddEntry(this, nameof(Species), Species);
            E.AddEntry(this, nameof(Genotype), Genotype);
            E.AddEntry(this, nameof(Subtype), Subtype);

            if (!MutationsList.IsNullOrEmpty())
                E.AddEntry(this, nameof(MutationsList),
                    MutationsList
                    ?.ConvertAll(md => md.ToString())
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            else
                E.AddEntry(this, nameof(MutationsList), "Empty");

            if (Skills != null && !Skills.SkillList.IsNullOrEmpty())
                E.AddEntry(this, nameof(Skills),
                    Skills.SkillList
                    ?.ConvertAll(s => s.Name)
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            else
                E.AddEntry(this, nameof(Skills), "Empty");

            if (!InstalledCybernetics.IsNullOrEmpty())
                E.AddEntry(this, nameof(InstalledCybernetics),
                    InstalledCybernetics
                    ?.ConvertAll(ic => ic.ToString())
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            else
                E.AddEntry(this, nameof(InstalledCybernetics), "Empty");

            if (!Tags.IsNullOrEmpty())
                E.AddEntry(this, nameof(Tags),
                    Tags
                    ?.ConvertToStringList(kvp => (kvp.Value != null) ? (kvp.Key + ": " + kvp.Value) : kvp.Key)
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            else
                E.AddEntry(this, nameof(Tags), "Empty");

            if (!StringProperties.IsNullOrEmpty())
                E.AddEntry(this, nameof(StringProperties),
                    StringProperties
                    ?.ConvertToStringList(kvp => (kvp.Value != null) ? (kvp.Key + ": " + kvp.Value) : kvp.Key)
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            else
                E.AddEntry(this, nameof(StringProperties), "Empty");

            if (!IntProperties.IsNullOrEmpty())
                E.AddEntry(this, nameof(IntProperties),
                    IntProperties
                    ?.ConvertToStringList(kvp => kvp.Key + ": " + kvp.Value)
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            else
                E.AddEntry(this, nameof(IntProperties), "Empty");

            if (!Effects.IsNullOrEmpty())
                E.AddEntry(this, nameof(Effects),
                    Effects
                    .ToList()
                    ?.ConvertAll(fx => fx.ClassName + ": " + ((fx.Duration == 9999) ? "Indefinite" : fx.Duration.ToString()))
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            else
                E.AddEntry(this, nameof(Effects), "Empty");

            return base.HandleEvent(E);
        }

        public void SummonBrainInAJar()
        {
            if (!BrainInAJar.Statistics.ContainsKey("Enenrgy"))
                BrainInAJar.Statistics.Add("Enenrgy", new("Enenrgy", -100000, 100000, 0, BrainInAJar));
            else
                BrainInAJar.Statistics["Enenrgy"] = new("Enenrgy", -100000, 100000, 0, BrainInAJar);

            BrainInAJar?.FinalizeStats();
            if (BrainInAJar.Statistics.ContainsKey("Enenrgy"))
                The.Player.CurrentCell
                    ?.GetAdjacentCells()
                    ?.GetRandomElementCosmetic()
                    ?.AddObject(BrainInAJar);
            else
                Popup.Show("Couldn't give " + nameof(BrainInAJar) + ", " + (BrainInAJar?.DebugName ?? NULL) + ", energy stat.\n\n" +
                    "Unable to summon" + (BrainInAJar?.them ?? "it") + ".");
        }
    }
}
