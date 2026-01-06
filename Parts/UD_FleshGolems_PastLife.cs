using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using Genkit;
using Qud.API;

using XRL.UI;
using XRL.Wish;
using XRL.Rules;
using XRL.Language;
using XRL.Collections;
using XRL.World;
using XRL.World.AI;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.Anatomy;
using XRL.World.ObjectBuilders;
using XRL.World.Effects;
using XRL.World.Capabilities;

using static XRL.World.Parts.UD_FleshGolems_CorpseReanimationHelper;
using static XRL.World.Parts.Mutation.UD_FleshGolems_NanoNecroAnimation;
using static XRL.World.ObjectBuilders.UD_FleshGolems_OptionallyReanimated;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Capabilities;
using UD_FleshGolems.Capabilities.Necromancy;
using UD_FleshGolems.Parts.PastLifeHelpers;
using UD_FleshGolems.Events;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using SerializeField = UnityEngine.SerializeField;
using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;
using XRL.World.Conversations.Parts;

namespace XRL.World.Parts
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    [HasWishCommand]
    [Serializable]
    public class UD_FleshGolems_PastLife : IScribedPart, IReanimateEventHandler
    {
        [UD_FleshGolems_DebugRegistry]
        public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
        {
            Registry.Register(nameof(GenerateDisplayName), false);
            Registry.Register(nameof(RoughlyCopyAdditionalLimbs), false);
            Registry.Register(nameof(GeneratePostDescription), false);

            Registry.Register(nameof(GetCorpseBlueprints), true);
            Registry.Register(nameof(GetAnEntityForCorpseWeighted), true);
            Registry.Register(nameof(GetAnEntityForCorpse), true);

            return Registry;
        }

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
            protected set
            {
                _WasBuiltReanimated = value;
            }
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
            using Indent indent = new();
            Debug.LogMethod(indent[1],
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(CorpseBlueprint),
                    Debug.Arg(nameof(Include0Weight), Include0Weight),
                    Debug.Arg(nameof(GuaranteeBlueprint), GuaranteeBlueprint),
                });
            
            Dictionary<string, int> weightedBlueprints = NecromancySystem
                ?.GetWeightedEntityStringsThisCorpseCouldBe(
                    CorpseBlueprint: CorpseBlueprint,
                    Include0Weight: Include0Weight,
                    Filter: IsNotBaseBlueprintOrPossiblyExcludedFromDynamicEncounters,
                    DrillIntoInheritance: false);

            if (GuaranteeBlueprint && weightedBlueprints.IsNullOrEmpty())
            {
                weightedBlueprints = NecromancySystem
                    ?.GetWeightedEntityStringsThisCorpseCouldBe(
                        CorpseBlueprint: CorpseBlueprint,
                        Include0Weight: Include0Weight,
                        Filter: IsNotBaseBlueprintOrPossiblyExcludedFromDynamicEncounters,
                        DrillIntoInheritance: true);
            }
            if (GuaranteeBlueprint && weightedBlueprints.IsNullOrEmpty())
            {
                weightedBlueprints = NecromancySystem
                    ?.GetWeightedEntityStringsThisCorpseCouldBe(
                        CorpseBlueprint: CorpseBlueprint,
                        Include0Weight: Include0Weight,
                        Filter: IsNotBaseBlueprint,
                        DrillIntoInheritance: false);
            }
            if (weightedBlueprints.GetWeightedRandom(Include0Weight) is string entity)
            {
                return entity;
            }
            if (!Include0Weight && GuaranteeBlueprint)
            {
                return GetAnEntityForCorpseWeighted(CorpseBlueprint, true, false);
            }
            return null;
        }
        public static string GetAnEntityForCorpse(string CorpseBlueprint, bool Include0Weight = true)
        {
            return NecromancySystem
                ?.GetWeightedEntityStringsThisCorpseCouldBe(CorpseBlueprint, Include0Weight, IsBaseBlueprint)
                ?.Keys
                ?.GetRandomElementCosmetic();
        }

        public UD_FleshGolems_PastLife Initialize(GameObject PastLife = null)
        {
            using Indent indent = new();
            Debug.LogCaller(indent[1],
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(PastLife), PastLife?.DebugName ?? NULL),
                });

            string callingTypeAndMethod = Debug.GetCallingTypeAndMethod();
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

                        Debug.Log(nameof(Blueprint), Blueprint ?? NULL, indent[2]);

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
                        {
                            WasBuiltReanimated = true;
                        }

                        Debug.Log(nameof(PastLife), PastLife?.DebugName ?? NULL, indent[2]);

                        if (PastLife.TryGetPart(out UD_FleshGolems_PastLife prevPastLife)
                            && prevPastLife.DeepCopy(BrainInAJar, DeepCopyMapInventory) is UD_FleshGolems_PastLife prevPastLifeCopy)
                        {
                            Debug.Log(nameof(prevPastLifeCopy), prevPastLifeCopy?.BrainInAJar?.DebugName ?? NULL, indent[2]);
                            BrainInAJar.AddPart(prevPastLifeCopy);
                        }

                        BrainInAJar._Property = new(PastLife._Property);
                        BrainInAJar._IntProperty = new(PastLife._IntProperty);

                        if (PastLife.IsPlayer()
                            || PastLife.IsPlayerDuringWorldGen())
                        {
                            BrainInAJar.SetStringProperty("UD_FleshGolems_WasPlayer", "Yep, I used to be the player!");
                        }
                        Debug.Log(nameof(WasPlayer), WasPlayer, indent[2]);

                        if (PastLife.GetBlueprint().InheritsFrom(BRAIN_IN_A_JAR_BLUEPRINT)
                            || PastLife.IsInanimateCorpse()
                            || WasCorpse)
                        {
                            TimesReanimated++;
                        }
                        Debug.Log(nameof(TimesReanimated), TimesReanimated, indent[2]);

                        Debug.Log(nameof(BrainInAJar._Property), BrainInAJar._Property?.Count ?? 0, indent[2]);
                        foreach ((string name, string value) in BrainInAJar._Property)
                        {
                            Debug.Log(name, "\"" + (value ?? "null") + "\"", indent[3]);
                        }
                        Debug.Log(nameof(BrainInAJar._IntProperty), BrainInAJar._IntProperty?.Count ?? 0, indent[2]);
                        foreach ((string name, int value) in BrainInAJar._IntProperty)
                        {
                            Debug.Log(name, value, indent[3]);
                        }

                        BrainInAJar.HasProperName = PastLife.HasProperName 
                            || (PastLife.GetxTag("Grammar", "Proper") is string properGrammar
                                && properGrammar.EqualsNoCase("true"));

                        if (ParentObject.Render.DisplayName != ParentObject.GetBlueprint().DisplayName())
                        {
                            ParentObject.SetStringProperty("CreatureName", ParentObject.Render.DisplayName);
                        }
                        else
                        if (BrainInAJar.HasProperName)
                        {
                            ParentObject.SetStringProperty("CreatureName", PastLife.GetReferenceDisplayName(Short: true));
                        }
                        else
                        {
                            ParentObject.SetStringProperty("CreatureName", null);
                        }

                        Debug.Log(nameof(BrainInAJar.HasProperName), BrainInAJar.HasProperName, indent[2]);

                        if (PastLife.HasPart<DisplayNameAdjectives>())
                            BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<DisplayNameAdjectives>());

                        if (PastLife.HasPart<Titles>())
                            BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Titles>());

                        if (PastLife.HasPart<Epithets>())
                            BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Epithets>());

                        if (PastLife.HasPart<Honorifics>())
                            BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Honorifics>());

                        if (WasCorpse && false)
                        {
                            DisplayNameAdjectives bIAJ_Adjectives = BrainInAJar.RequirePart<DisplayNameAdjectives>();
                            if (bIAJ_Adjectives.AdjectiveList.IsNullOrEmpty()
                                || !bIAJ_Adjectives.AdjectiveList.Contains(UD_FleshGolems_ReanimatedCorpse.REANIMATED_ADJECTIVE))
                            {
                                bIAJ_Adjectives.AddAdjective(UD_FleshGolems_ReanimatedCorpse.REANIMATED_ADJECTIVE);
                            }
                        }
                        Debug.Log(nameof(WasCorpse), WasCorpse, indent[2]);

                        PastLife.RemoveAllEffects<LiquidCovered>();
                        PastLife.RemoveAllEffects<LiquidStained>();

                        TransferRenderFieldPropsFrom(PastLife);

                        Description bIAJ_Description = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Description>());

                        if (PastLife.CurrentCell is Cell deathCell
                            && deathCell.ParentZone is Zone deathZone)
                        {
                            DeathAddress = new(deathZone.ZoneID, deathCell.Location);
                        }
                        Debug.Log(nameof(DeathAddress), DeathAddress.ToString() ?? NULL, indent[2]);

                        Physics bIAJ_Physics = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.Physics);
                        BrainInAJar.Physics = bIAJ_Physics;

                        PastLife.Brain ??= PastLife.RequirePart<Brain>();
                        Brain bIAJ_Brain = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.Brain);
                        BrainInAJar.Brain = bIAJ_Brain;
                        Brain pastBrain = PastLife.Brain;
                        try
                        {
                            Debug.Log("Storing " + nameof(bIAJ_Brain.PartyLeader) + "...", Indent: indent[2]);
                            bIAJ_Brain.PartyLeader = pastBrain.PartyLeader;
                            Debug.Log(nameof(bIAJ_Brain.PartyLeader), bIAJ_Brain.PartyLeader?.DebugName ?? NULL, indent[3]);
                        }
                        catch (Exception x)
                        {
                            MetricsManager.LogException(callingTypeAndMethod + " " + nameof(bIAJ_Brain.PartyLeader), x, "game_mod_exception");
                            bIAJ_Brain.PartyLeader = null;
                        }
                        try
                        {
                            Debug.Log("Storing " + nameof(bIAJ_Brain.PartyMembers) + "...", Indent: indent[2]);
                            pastBrain.PartyMembers ??= new();
                            foreach ((int flags, PartyMember partyMember) in pastBrain.PartyMembers)
                            {
                                PartyMember partyMemberCopy = new(partyMember.Reference, partyMember.Flags);
                                bIAJ_Brain.PartyMembers.TryAdd(partyMemberCopy.Reference.ID, partyMemberCopy);
                                Debug.Log(partyMemberCopy.Reference?.Object?.DebugName ?? NULL, Indent: indent[3]);
                            }
                        }
                        catch (Exception x)
                        {
                            MetricsManager.LogException(callingTypeAndMethod + " " + nameof(bIAJ_Brain.PartyMembers), x, "game_mod_exception");
                            bIAJ_Brain.PartyMembers = new();
                        }
                        try
                        {
                            Debug.Log("Storing " + nameof(bIAJ_Brain.Opinions) + "...", Indent: indent[2]);
                            pastBrain.Opinions ??= new();
                            foreach ((int opinionSubjectID, OpinionList opinionList) in pastBrain.Opinions)
                            {
                                OpinionList opinionsCopy = new();
                                foreach (IOpinion opinionCopy in opinionList)
                                {
                                    opinionsCopy.Add(opinionCopy);
                                }
                                bIAJ_Brain.Opinions.TryAdd(opinionSubjectID, opinionsCopy);

                                GameObject opinionSubject = GameObject.FindByID(opinionSubjectID);
                                string bulletIndent = indent[4].ToString();
                                string opinionsString = opinionList?.ToList()
                                    ?.ConvertAll(o => o.GetText(opinionSubject))
                                    ?.GenerateBulletList(Label: "", Bullet: bulletIndent + "-", BulletColor: null);
                                Debug.Log(opinionSubject?.DebugName ?? NULL, opinionsString, indent[3]);
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
                        Debug.Log(nameof(bIAJ_Body), bIAJ_Body?.Anatomy, indent[2]);

                        RoughlyCopyAdditionalLimbs(bIAJ_Body, PastLife?.Body, ref ExtraLimbs);

                        string bleedLiquid = PastLife.GetBleedLiquid();
                        BrainInAJar.SetBleedLiquid(bleedLiquid);
                        Debug.Log(nameof(bleedLiquid), bleedLiquid ?? NULL, indent[2]);

                        Corpse bIAJ_Corpse = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Corpse>());
                        if (bIAJ_Corpse.CorpseBlueprint.IsNullOrEmpty()
                            && Blueprint
                                .GetGameObjectBlueprint()
                                .TryGetCorpseBlueprint(out string corpseBlueprint))
                        {
                            bIAJ_Corpse.CorpseBlueprint = corpseBlueprint;
                        }
                        if (bIAJ_Corpse.CorpseBlueprint.IsNullOrEmpty()
                            && !ParentObject.HasPart<ReplaceObject>())
                        {
                            bIAJ_Corpse.CorpseBlueprint = ParentObject.Blueprint;
                        }
                        if (bIAJ_Corpse.CorpseBlueprint.IsNullOrEmpty()
                            && (PastLife.GetSpecies() + " Corpse").GetGameObjectBlueprint() is var corpseModel)
                        {
                            bIAJ_Corpse.CorpseBlueprint = corpseModel.Name;
                        }
                        if (bIAJ_Corpse.CorpseBlueprint.IsNullOrEmpty()
                            && ("UD_FleshGolems " + PastLife.GetSpecies() + " Corpse").GetGameObjectBlueprint() is var corpseModdedModel)
                        {
                            bIAJ_Corpse.CorpseBlueprint = corpseModdedModel.Name;
                        }
                        Debug.Log(
                            nameof(bIAJ_Corpse.CorpseBlueprint), bIAJ_Corpse.CorpseBlueprint + " (" +
                            bIAJ_Corpse.CorpseChance + ")",
                            indent[2]);

                        if (!PastLife.GenderName.IsNullOrEmpty())
                        {
                            BrainInAJar.SetGender(PastLife.GenderName);
                        }
                        Debug.Log(nameof(BrainInAJar.GenderName), BrainInAJar.GenderName ?? NULL, indent[2]);

                        if (!PastLife.PronounSetName.IsNullOrEmpty())
                        {
                            BrainInAJar.SetPronounSet(PastLife.PronounSetName);
                        }
                        Debug.Log(nameof(BrainInAJar.PronounSetName), BrainInAJar.PronounSetName ?? NULL, indent[2]);

                        ConversationScript bIAJ_Conversation = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<ConversationScript>());
                        Debug.Log(nameof(bIAJ_Conversation.ConversationID), bIAJ_Conversation?.ConversationID ?? NULL, indent[2]);

                        if (!PastLife.Statistics.IsNullOrEmpty())
                        {
                            Debug.Log(nameof(PastLife.Statistics), PastLife?.Statistics?.Count ?? 0, indent[2]);
                            BrainInAJar.Statistics = new();
                            foreach ((string statName, Statistic stat) in PastLife?.Statistics)
                            {
                                if (stat.Name == "Hitpoints")
                                {
                                    stat.Penalty = 0;
                                }
                                Statistic newStat = new(stat)
                                {
                                    Owner = BrainInAJar,
                                };

                                BrainInAJar.Statistics.Add(statName, newStat);
                                int statValue = newStat.Value;
                                int statBaseValue = newStat.BaseValue;
                                string statsValue = (newStat.sValue ?? "no sValue");
                                Debug.CheckYeh(statName, statValue + "/" + statBaseValue + " | " + statsValue, indent[3]);
                            }
                            if (PastLife.IsPlayer()
                                || PastLife.IsPlayerDuringWorldGen())
                            {
                                Debug.CheckYeh("Player " + nameof(PastLife.Statistics), Indent: indent[2]);
                                if (GetPlayerEmbarkStats() is Dictionary<string, int> playerStats)
                                    foreach ((string statName, int baseValue) in playerStats)
                                    {
                                        Debug.Log(statName, baseValue, Indent: indent[3]);
                                        BrainInAJar.GetStat(statName).BaseValue = baseValue;
                                    }
                            }
                        }
                        else
                        {
                            Debug.CheckNah("no " + nameof(PastLife.Statistics), Indent: indent[2]);
                        }

                        EntityTaxa = new(PastLife);
                        BrainInAJar.SetSpecies(EntityTaxa.Species);
                        BrainInAJar.SetGenotype(EntityTaxa.Genotype);
                        BrainInAJar.SetSubtype(EntityTaxa.Subtype);
                        
                        Debug.Log(nameof(Species), Species ?? NULL, indent[2]);
                        Debug.Log(nameof(Genotype), Genotype ?? NULL, indent[2]);
                        Debug.Log(nameof(Subtype), Subtype ?? NULL, indent[2]);

                        Mutations bIAJ_Mutations = BrainInAJar.AddPart(PastLife.RequirePart<Mutations>());
                        if (!bIAJ_Mutations.ActiveMutationList.IsNullOrEmpty())
                        {
                            Debug.Log(nameof(MutationsList) + "(BaseLevel|CapOverride|RapidLevel)", Indent: indent[2]);
                            foreach (BaseMutation bIAJ_Mutation in bIAJ_Mutations.ActiveMutationList)
                            {
                                MutationData mutationData = new(bIAJ_Mutation, bIAJ_Mutation.GetRapidLevelAmount());
                                MutationsList.Add(mutationData);
                                Debug.CheckYeh(mutationData.ToString(), Indent: indent[3]);
                            }
                        }
                        else
                        {
                            Debug.CheckNah("no " + nameof(MutationsList), Indent: indent[2]);
                        }

                        Skills bIAJ_Skills = BrainInAJar.AddPart(PastLife.RequirePart<Skills>());
                        List<BaseSkill> pastSkills = PastLife.GetPartsDescendedFrom<BaseSkill>();
                        if (!pastSkills.IsNullOrEmpty())
                        {
                            Debug.Log(nameof(pastSkills), pastSkills?.Count ?? 0, indent[2]);
                            foreach (BaseSkill baseSkill in pastSkills)
                            {
                                Debug.CheckYeh(baseSkill.Name, Indent: indent[3]);
                                if (!bIAJ_Skills.SkillList.Contains(baseSkill))
                                {
                                    bIAJ_Skills.AddSkill(baseSkill);
                                }
                            }
                        }
                        else
                        {
                            Debug.CheckNah("no " + nameof(pastSkills), Indent: indent[2]);
                        }

                        if (!PastLife.Effects.IsNullOrEmpty())
                        {
                            Debug.Log(nameof(PastLife.Effects), PastLife.Effects?.Count ?? 0, indent[2]);
                            foreach (Effect pastEffect in PastLife.Effects)
                            {
                                BrainInAJar.Effects.Add(pastEffect.DeepCopy(BrainInAJar, null));
                                Debug.CheckYeh(pastEffect.DisplayNameStripped, Indent: indent[3]);
                            }
                        }
                        else
                        {
                            Debug.CheckNah("no " + nameof(PastLife.Effects), Indent: indent[2]);
                        }

                        if (PastLife?.Body is Body pastBody
                            && pastBody.GetInstalledCyberneticsReadonly() is List<GameObject> pastInstalledCybernetics
                            && InstalledCybernetics.IsNullOrEmpty())
                        {
                            Debug.Log(nameof(InstalledCybernetics) + "...", Indent: indent[2]);
                            foreach (GameObject pastInstalledCybernetic in pastInstalledCybernetics)
                            {
                                if (pastInstalledCybernetic?.Implantee?.Body is Body implanteeBody)
                                {
                                    InstalledCybernetic installedCybernetic = new(pastInstalledCybernetic, implanteeBody);
                                    InstalledCybernetics.Add(installedCybernetic);
                                    Debug.CheckYeh(installedCybernetic.ToString(), Indent: indent[3]);
                                }
                            }
                        }
                        else
                        {
                            Debug.CheckNah("no " + nameof(InstalledCybernetics), Indent: indent[2]);
                        }

                        Debug.Log(nameof(PastLife.PartsList) + "...", Indent: indent[2]);
                        if (!PastLife.PartsList.IsNullOrEmpty())
                        {
                            foreach (IPart pastPart in PastLife.PartsList)
                            {
                                if (BrainInAJar.HasPart(pastPart.Name))
                                {
                                    Debug.CheckNah(pastPart.Name + " already present", Indent: indent[3]);
                                    continue;
                                }

                                if (PartsToNotRetain.Contains(pastPart.Name))
                                {
                                    Debug.CheckNah(pastPart.Name + " already present", Indent: indent[3]);
                                    continue;
                                }

                                if (pastPart is BaseSkill)
                                {
                                    Debug.CheckNah(pastPart.Name + " is " + nameof(BaseSkill), Indent: indent[3]);
                                    continue;
                                }

                                if (pastPart is BaseMutation)
                                {
                                    Debug.CheckNah(pastPart.Name + " is " + nameof(BaseMutation), Indent: indent[3]);
                                    continue;
                                }

                                BrainInAJar.OverrideWithDeepCopyOrRequirePart(pastPart, DeepCopyMapInventory);
                                pastPart.ParentObject = BrainInAJar;
                                Debug.CheckYeh(pastPart.Name + " added", Indent: indent[3]);
                            }
                        }
                        else
                        {
                            Debug.CheckNah("none!", Indent: indent[3]);
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
                    {
                        PastLife?.Obliterate();
                    }
                }
            }
            Debug.LogMethod("Finished", Indent: indent[1]);
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
                {
                    Initialize(pastLife);
                }
            }
            return this;
        }

        public override void Attach()
        {
            base.Attach();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public GameObjectBlueprint GetBlueprint()
        {
            return Blueprint?.GetGameObjectBlueprint();
        }
        public bool TryGetBlueprint(out GameObjectBlueprint GameObjectBlueprint)
        {
            return (GameObjectBlueprint = Blueprint?.GetGameObjectBlueprint()) != null;
        }

        public static bool IsFactionRelationshipPropTag(KeyValuePair<string, string> Entry)
            => !Entry.Key.IsNullOrEmpty()
            && (Entry.Key.StartsWith("staticFaction") 
                || Entry.Key == "NoHateFactions");

        public static bool TransferRenderFieldProps(
            GameObject Source,
            GameObject Destination,
            Predicate<KeyValuePair<string, Type>> Filter = null)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Source), Source?.DebugName ?? NULL),
                    Debug.Arg(nameof(Destination), Destination?.DebugName ?? NULL),
                });

            if (Source == null
                || Destination == null
                || Source.Render is not Render sourceRender)
            {
                Debug.CheckNah("No source, or no destination, or no source Render.", indent[1]);
                return false;
            }
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
                            && destinationRenderFieldsProps[sourceFieldPropName].GetValueType() == sourceRenderFieldProp.GetValueType())
                        {
                            if (Filter == null || Filter(new(sourceFieldPropName, sourceRenderFieldProp.GetValueType())))
                            {
                                destinationRenderFieldsProps[sourceFieldPropName].SetValue(sourceRenderFieldPropValue);
                                Debug.CheckYeh(sourceFieldPropName, sourceRenderFieldPropValue.ToString(), indent[1]);
                                any = true;
                            }
                            else
                            {
                                Debug.CheckNah("Filtered " + sourceFieldPropName, sourceRenderFieldPropValue.ToString(), indent[1]);
                            }
                        }
                        else
                        {
                            Debug.CheckNah(sourceFieldPropName, sourceRenderFieldPropValue.ToString(), indent[1]);
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
                            "game_mod_exception");
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
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(PastLife), PastLife != null),
                });

            if (FrankenCorpse == null
                || PastLife == null
                || !PastLife.TryGetBlueprint(out GameObjectBlueprint sourceBlueprint))
            {
                Debug.CheckNah("Missing FrankenCorpse or PastLife.", indent[1]);
                return false;
            }

            foreach ((string tagName, string tagValue) in sourceBlueprint.Tags.Where(IsFactionRelationshipPropTag))
            {
                FrankenCorpse.SetStringProperty(tagName, tagValue);
            }
            foreach ((string tagName, string tagValue) in sourceBlueprint.Props.Where(IsFactionRelationshipPropTag))
            {
                FrankenCorpse.SetStringProperty(tagName, tagValue);
            }
            return true;
        }
        public bool RestoreFactionRelationships()
            => RestoreFactionRelationships(ParentObject, this);

        public static bool IsPropTagToRestore<T>(KeyValuePair<string, T> Entry)
        {
            if (Entry.Key.IsNullOrEmpty())
                return false;

            if (Entry.Key.StartsWith("Semantic"))
                return false;

            if (PropTagsToNotRestore.Contains(Entry.Key))
                return false;

            if (Entry.Value is string stringValue)
                if (IsFactionRelationshipPropTag(new(Entry.Key, stringValue)))
                    return false;

            if (Entry.Key.StartsWith("UD_FleshGolems"))
                return false;

            return true;
        }

        public static bool RestoreSelectPropTags(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(PastLife), PastLife != null),
                });

            if (FrankenCorpse == null
                || PastLife == null
                || !PastLife.TryGetBlueprint(out GameObjectBlueprint sourceBlueprint))
            {
                Debug.CheckNah("Missing FrankenCorpse or PastLife.", indent[1]);
                return false;
            }
            foreach ((string tagName, string tagValue) in sourceBlueprint.Tags.Where(IsPropTagToRestore))
            {
                FrankenCorpse.SetStringProperty(tagName, tagValue);
            }
            foreach ((string propName, string propValue) in sourceBlueprint.Props.Where(IsPropTagToRestore))
            {
                FrankenCorpse.SetStringProperty(propName, propValue);
            }
            foreach ((string intPropName, int intPropValue) in sourceBlueprint.IntProps.Where(IsPropTagToRestore))
            {
                FrankenCorpse.SetIntProperty(intPropName, intPropValue);
            }
            return true;
        }
        public bool RestoreSelectPropTags()
            => RestoreSelectPropTags(ParentObject, this);

        public static bool AlignWithPreviouslySentientBeings(
            Brain FrankenBrain,
            UD_FleshGolems_PastLife PastLife)
        {
            using Indent indent = new(1);

            if (FrankenBrain == null
                || PastLife == null
                || PastLife.Brain is not Brain pastBrain
                || FrankenBrain.ParentObject is not GameObject frankenCorpse)
            {
                Debug.CheckNah(Debug.GetCallingMethod(true) + "failed", indent[1]);
                return false;
            }
            if (frankenCorpse.GetIntProperty("UD_FleshGolems Alignment Adjusted") > 0
                && FrankenBrain.Allegiance.Any(a => a.Key == PREVIOUSLY_SENTIENT_BEINGS && a.Value > 0))
            {
                // Debug.CheckNah(Debug.GetCallingMethod(true) + "skipped", indent[1]);
                return false;
            }

            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenBrain), FrankenBrain != null),
                    Debug.Arg(nameof(PastLife), PastLife != null),
                });
            int previouslySentientBeingsRep = 100;
            Debug.Log(nameof(previouslySentientBeingsRep), previouslySentientBeingsRep, indent[1]);

            Debug.Log("Altering " + nameof(previouslySentientBeingsRep), Indent: indent[1]);
            if (PastLife.WasBuiltReanimated)
            {
                previouslySentientBeingsRep -= 25;
                Debug.CheckYeh(nameof(PastLife.WasBuiltReanimated), nameof(previouslySentientBeingsRep) + "-" + previouslySentientBeingsRep, indent[2]);
            }
            if (!UD_FleshGolems_Reanimated.HasWorldGenerated)
            {
                previouslySentientBeingsRep -= 25;
                Debug.CheckYeh(nameof(UD_FleshGolems_Reanimated.HasWorldGenerated), nameof(previouslySentientBeingsRep) + "-" + previouslySentientBeingsRep, indent[2]);
            }
            Debug.Log("Final " + nameof(previouslySentientBeingsRep), previouslySentientBeingsRep, Indent: indent[1]);
            if (FrankenBrain.Allegiance.ContainsKey(PREVIOUSLY_SENTIENT_BEINGS))
            {
                int existingRep = FrankenBrain.Allegiance[PREVIOUSLY_SENTIENT_BEINGS];
                previouslySentientBeingsRep = Math.Max(-100, Math.Min(previouslySentientBeingsRep + existingRep, 100));
                FrankenBrain.Allegiance[PREVIOUSLY_SENTIENT_BEINGS] = previouslySentientBeingsRep;
            }
            else
            {
                FrankenBrain.Allegiance.Add(PREVIOUSLY_SENTIENT_BEINGS, previouslySentientBeingsRep);
            }
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
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(PastLife), PastLife != null),
                    Debug.Arg(nameof(ExcludedFromDynamicEncounters), ExcludedFromDynamicEncounters),
                    Debug.Arg("out " + nameof(FrankenBrain)),
                });
            FrankenBrain = null;
            if (FrankenCorpse == null
                || PastLife == null)
            {
                return false;
            }
            FrankenBrain = FrankenCorpse.Brain;
            if (FrankenBrain != null
                && PastLife?.Brain is Brain pastBrain)
            {
                FrankenBrain.Allegiance ??= new();
                FrankenBrain.Allegiance.Clear();
                Debug.Log(nameof(FrankenBrain.Allegiance), "Cleared", indent[1]);

                if (!UD_FleshGolems_Reanimated.HasWorldGenerated
                    || ExcludedFromDynamicEncounters
                    || PastLife.WasBuiltReanimated
                    || PastLife.WasPlayer)
                {
                    Debug.LogArgs("Any of: (", ")", indent[1],
                        ArgPairs: new Debug.ArgPair[]
                        {
                            Debug.Arg(nameof(UD_FleshGolems_Reanimated.HasWorldGenerated), UD_FleshGolems_Reanimated.HasWorldGenerated), 
                            Debug.Arg(nameof(ExcludedFromDynamicEncounters), ExcludedFromDynamicEncounters), 
                            Debug.Arg(nameof(PastLife.WasBuiltReanimated), PastLife.WasBuiltReanimated), 
                            Debug.Arg(nameof(PastLife.WasPlayer), PastLife.WasPlayer), 
                        });

                    Debug.Log("Iterating Faction Reps...", Indent: indent[2]);
                    foreach ((string factionName, int repValue) in pastBrain.Allegiance)
                    {
                        if (!pastBrain.Allegiance.ContainsKey(factionName))
                        {
                            FrankenBrain.Allegiance.Add(factionName, repValue);
                            Debug.CheckYeh(factionName + " added", repValue, Indent: indent[3]);
                        }
                        else
                        {
                            int clampedRepValue = Math.Min(Math.Max(-100, FrankenBrain.Allegiance[factionName] + repValue), 100);
                            FrankenBrain.Allegiance[factionName] = clampedRepValue;
                            Debug.CheckYeh(factionName + " adjusted to", clampedRepValue, Indent: indent[3]);
                        }
                    }

                    Debug.Log("Transferring Party Members...", Indent: indent[2]);
                    if (!FrankenCorpse.HasPropertyOrTag("StartingPet") && !FrankenCorpse.HasPropertyOrTag("Pet"))
                    {
                        FrankenBrain.PartyMembers = pastBrain.PartyMembers;
                        foreach ((int memberID, PartyMember partyMember) in FrankenBrain.PartyMembers)
                        {
                            if (pastBrain.PartyLeader != null
                                && memberID == pastBrain.PartyLeader.BaseID)
                            {
                                FrankenBrain.SetPartyLeader(pastBrain.PartyLeader, Flags: partyMember.Flags, Silent: true);
                                Debug.CheckYeh("Party Leader set", pastBrain.PartyLeader?.DebugName ?? NULL, Indent: indent[3]);
                                break;
                            }
                        }
                        Debug.CheckYeh("Party Members transferred", Indent: indent[3]);
                    }
                    else
                    {
                        Debug.CheckNah("Skipped due to being pet", Indent: indent[3]);
                    }
                    Debug.CheckYeh("Conditional Transfer finished", Indent: indent[2]);
                }

                if (PastLife.GetIdentityType() is IdentityType identityType
                    && identityType > IdentityType.NamedVillager)
                {
                    AlignWithPreviouslySentientBeings(FrankenBrain, PastLife);
                }

                FrankenBrain.Allegiance.Hostile = pastBrain.Allegiance.Hostile;
                FrankenBrain.Allegiance.Calm = pastBrain.Allegiance.Calm;

                FrankenBrain.Flags = pastBrain.Flags;

                FrankenBrain.MaxKillRadius = pastBrain.MaxKillRadius;
                FrankenBrain.MaxMissileRange = pastBrain.MaxMissileRange;
                FrankenBrain.MaxWanderRadius = pastBrain.MaxWanderRadius;
                FrankenBrain.MinKillRadius = pastBrain.MinKillRadius;

                Debug.CheckYeh(nameof(FrankenBrain.Allegiance.Hostile), FrankenBrain.Allegiance.Hostile, Indent: indent[1]);
                Debug.CheckYeh(nameof(FrankenBrain.Allegiance.Calm), FrankenBrain.Allegiance.Calm, Indent: indent[1]);
                Debug.CheckYeh(nameof(FrankenBrain.Flags), FrankenBrain.Flags, Indent: indent[1]);
                Debug.CheckYeh(nameof(FrankenBrain.MaxKillRadius), FrankenBrain.MaxKillRadius, Indent: indent[1]);
                Debug.CheckYeh(nameof(FrankenBrain.MaxMissileRange), FrankenBrain.MaxMissileRange, Indent: indent[1]);
                Debug.CheckYeh(nameof(FrankenBrain.MaxWanderRadius), FrankenBrain.MaxWanderRadius, Indent: indent[1]);
                Debug.CheckYeh(nameof(FrankenBrain.MinKillRadius), FrankenBrain.MinKillRadius, Indent: indent[1]);

                FrankenBrain.LastThought = "*wilhelm scream*";
                Debug.CheckYeh(nameof(FrankenBrain.LastThought), FrankenBrain.LastThought, Indent: indent[1]);

                Debug.Log("Setting " + nameof(FrankenBrain.StartingCell) + "...", Indent: indent[1]);
                if (PastLife.DeathAddress.GetCell() is Cell deathCell)
                {
                    FrankenBrain.StartingCell = new(deathCell);
                    GlobalLocation startingCell = FrankenBrain.StartingCell;
                    Debug.CheckYeh(
                        nameof(FrankenBrain.StartingCell), 
                        startingCell?.ZoneID + "[" + startingCell?.CellX + "," + startingCell?.CellY + "]",
                        Indent: indent[2]);
                }
                else
                {
                    Debug.CheckNah("No " + nameof(PastLife.DeathAddress) + " to set from", Indent: indent[2]);
                }
                Debug.CheckYeh("Brain restoration complete!", Indent: indent[0]);
                return true;
            }
            Debug.CheckNah("No Brain to transfer from (or to)", Indent: indent[0]);
            return false;
        }
        public bool RestoreBrain(
            bool ExcludedFromDynamicEncounters,
            out Brain FrankenBrain)
        {
            return RestoreBrain(ParentObject, this, ExcludedFromDynamicEncounters, out FrankenBrain);
        }

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
            {
                return null;
            }
            IdentityType = PastLife.GetIdentityType();

            using Indent indent = new();
            Debug.LogMethod(indent[1],
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(frankenCorpse), frankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(IdentityType), IdentityType.ToStringWithNum()),
                });


            if (frankenCorpse.Render.DisplayName != frankenCorpse.GetBlueprint().DisplayName())
            {
                frankenCorpse.SetStringProperty("CreatureName", frankenCorpse.Render.DisplayName);
            }
            else
            if (IdentityType <= IdentityType.Named)
            {
                frankenCorpse.SetStringProperty("CreatureName", PastLife.BrainInAJar.GetReferenceDisplayName(Short: true));
            }
            else
            {
                frankenCorpse.SetStringProperty("CreatureName", null);
            }
            string creatureName = frankenCorpse.GetStringProperty("CreatureName");
            if (creatureName.IsNullOrEmpty())
            {
                frankenCorpse.SetStringProperty("CreatureName", creatureName = frankenCorpse.Render?.DisplayName);
            }
            string newIdentity = IdentityType switch
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
            };
            Debug.Log(nameof(newIdentity) + "(" + IdentityType + ")", newIdentity ?? NULL, indent[1]);
            return newIdentity?.RemoveAll("[", "]");
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
            {
                return null;
            }

            IdentityType = PastLife.GetIdentityType();

            string inLife = "In life this =subject.uD_xTag:UD_FleshGolems_CorpseText:CorpseDescription= was ";
            string whoTheyWere = (PastLife.RefName ?? PastLife.GetBlueprint()?.DisplayName() ?? "unfortunate soul");
            string endMark = ":\n";
            string oldDescription = PastLife.Description;

            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(frankenCorpse), frankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(whoTheyWere), whoTheyWere ?? NULL),
                    Debug.Arg(nameof(IdentityType), IdentityType.ToStringWithNum()),
                });

            if (IdentityType == IdentityType.Corpse)
            {
                if (PastLife?.PastPastLife?.GetIdentityType() < IdentityType.Villager)
                {
                    whoTheyWere = "the " + whoTheyWere;
                }
                else
                {
                    whoTheyWere = Grammar.A(whoTheyWere);
                }
                oldDescription = null;
                endMark = ".";
            }
            if (IdentityType > IdentityType.Named && IdentityType < IdentityType.Corpse)
            {
                whoTheyWere = Grammar.A(whoTheyWere);
            }
            if (IdentityType == IdentityType.Librarian)
            {
                whoTheyWere = frankenCorpse.Render?.DisplayName ?? "Sheba Hagadias";
                if (GameObject.CreateSample("UD_FleshGolems_Sample_Librarian") is GameObject sampleLibrarian)
                {
                    if (sampleLibrarian.TryGetPart(out MechanimistLibrarian librarianPart))
                    {
                        librarianPart.Initialize();
                        if (sampleLibrarian.TryGetPart(out Description librarianDescription))
                        {
                            oldDescription = librarianDescription.Short;
                        }
                        whoTheyWere = sampleLibrarian.DisplayName;
                    }
                    if (GameObject.Validate(ref sampleLibrarian))
                    {
                        sampleLibrarian.Obliterate();
                    }
                }
            }
            if (IdentityType == IdentityType.Player)
            {
                whoTheyWere = "you";
                oldDescription = null;
                endMark = ".";
            }
            string postDescription = inLife + whoTheyWere.RemoveAll("[", "]") + endMark + oldDescription;
            Debug.Log(nameof(postDescription), postDescription ?? NULL, indent[1]);
            return postDescription;
        }
        public string GeneratePostDescription(out IdentityType IdentityType)
        {
            return GeneratePostDescription(this, out IdentityType);
        }
        public string GeneratePostDescription()
        {
            return GeneratePostDescription(out _);
        }

        public static bool RestoreGenderIdentity(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            bool WantOldIdentity = false)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(PastLife), PastLife != null),
                    Debug.Arg(nameof(WantOldIdentity), WantOldIdentity),
                });
            if (FrankenCorpse == null || PastLife == null || !WantOldIdentity)
            {
                return false;
            }
            if (PastLife.Gender?.Name is string pastGenderName)
            {
                FrankenCorpse.SetGender(pastGenderName);
            }
            if (PastLife.PronounSet?.Name is string pastPronounSetName)
            {
                FrankenCorpse.SetPronounSet(pastPronounSetName);
            }
            return true;
        }
        public bool RestoreGenderIdentity(bool WantOldIdentity = true)
        {
            return RestoreGenderIdentity(ParentObject, this, WantOldIdentity);
        }

        public static bool RestoreAnatomy(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out Body FrankenBody)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(PastLife), PastLife != null),
                });
            FrankenBody = null;
            if (FrankenCorpse == null || PastLife == null || PastLife.Body == null)
            {
                return false;
            }
            if (PastLife.Body is Body pastBody
                && Anatomies.GetAnatomy(pastBody?.Anatomy) is Anatomy.Anatomy anatomy)
            {
                if (FrankenCorpse.Body == null)
                {
                    FrankenCorpse.AddPart(new Body()).Anatomy = anatomy.Name;
                }
                else
                {
                    FrankenCorpse.Body.Rebuild(anatomy.Name);
                }
            }
            FrankenBody = FrankenCorpse.Body;
            return true;
        }
        public bool RestoreAnatomy(out Body FrankenBody)
        {
            return RestoreAnatomy(ParentObject, this, out FrankenBody);
        }

        public static bool IsIntrinsicAndNative(BodyPart BodyPart)
            => BodyPart != null
            && BodyPart.Native
            && !BodyPart.Extrinsic;

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

        public static bool IsUnmanaged(BodyPart BodyPart)
            => BodyPart != null
            && !IsManaged(BodyPart);

        public static bool IsManagedConcreteIntrinsic(BodyPart BodyPart)
            => BodyPart != null
            && IsManaged(BodyPart)
            && IsConcreteIntrinsic(BodyPart);

        public static bool IsUnmanagedConcreteIntrinsic(BodyPart BodyPart)
            => BodyPart != null
            && IsUnmanaged(BodyPart)
            && IsConcreteIntrinsic(BodyPart);

        public static bool IsAbstractOrExtrinsicOrNonNative(BodyPart BodyPart)
            => BodyPart != null
            && (IsAbstractOrExtrinsic(BodyPart)
                || !BodyPart.Native);

        public static bool IsConcreteIntrinsicNative(BodyPart BodyPart)
            => BodyPart != null
            && !IsAbstractOrExtrinsicOrNonNative(BodyPart);

        public static bool IsUnmanagedConcreteIntrinsicNative(BodyPart BodyPart)
            => BodyPart != null
            && IsUnmanaged(BodyPart)
            && !IsAbstractOrExtrinsicOrNonNative(BodyPart);

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
            using Indent indent = new();
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(DestinationBody), DestinationBody?.ParentObject?.DebugName ?? NULL),
                    Debug.Arg(nameof(SourceBody), SourceBody?.ParentObject?.DebugName ?? NULL),
                });
            if (DestinationBody == null
                || DestinationBody.ParentObject is not GameObject destinationObject
                || SourceBody == null
                || SourceBody.ParentObject is not GameObject sourceObject)
            {
                Debug.CheckNah("Nothing to work with.", Indent: indent[1]);
                return false;
            }

            int amountGiven = 0;
            ExtraLimbs ??= new();
            int totalSourceParts = SourceBody?.LoopParts()?.Count() ?? 0;
            int totalDestinationParts = 0;
            if (!ExtraLimbs.IsNullOrEmpty())
            {
                Debug.CheckYeh("Looping all " + ExtraLimbs.Count.Things(nameof(ExtraLimbs) + " tree") + ".", Indent: indent[1]);
                foreach ((PseudoLimb pseudoLimb, string _) in ExtraLimbs)
                {
                    pseudoLimb.DebugPseudoLimb();
                }

                Debug.Log("Growing " + ExtraLimbs.Count.Things(nameof(ExtraLimbs) + " tree") + ".", Indent: indent[1]);
                foreach ((PseudoLimb pseudoLimb, string targetBodyPartType) in ExtraLimbs)
                {
                    BodyPart targetBodyPart = 
                        DestinationBody.LoopPart(targetBodyPartType)
                            ?.GetRandomElementCosmetic()
                        ?? DestinationBody.LoopParts(IsConcreteIntrinsic)
                            ?.GetRandomElementCosmetic();

                    if (targetBodyPart == null)
                    { 
                        Debug.CheckNah("Actually no limbs to grow on!", Indent: indent[2]);
                        break;
                    }
                    pseudoLimb.GiveToEntity(destinationObject, targetBodyPart, ref amountGiven);
                }
                totalDestinationParts = DestinationBody?.LoopParts()?.Count() ?? 0;
                Debug.LogArgs("Body Copy Info (", ")", indent[1],
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(totalSourceParts), totalSourceParts),
                        Debug.Arg(nameof(amountGiven), amountGiven),
                        Debug.Arg(nameof(totalDestinationParts), totalDestinationParts),
                    });
                /*
                Debug.CheckYeh("Looping all DestinationBody (" + (destinationObject?.DebugName ?? NULL) + ") parts.", Indent: indent[1]);
                foreach (BodyPart bodyPart in DestinationBody.LoopParts())
                {
                    int depth = DestinationBody.GetBody().GetPartDepth(bodyPart, 0);
                    Debug.Log(bodyPart.BodyPartString(WithManager: true, WithParent: true), Indent: indent[depth + 2]);
                }
                */
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
                {
                    Debug.CheckNah(bodyPartName + " has no subparts that " + nameof(IsManagedConcreteIntrinsic), Indent: indent[1]);
                    continue;
                }
                Debug.CheckYeh(bodyPartName + " eligible", Indent: indent[1]);

                List<PseudoLimb> bodyPartExtraLimbs = new();
                Debug.Log("Looping Subparts", Indent: indent[2]);
                foreach (BodyPart subPart in subPartsToLoop)
                {
                    string subPartName = subPart?.BodyPartString(WithManager: true)?.Strip();
                    if (accountedForBodyParts.Contains(subPart))
                    {
                        Debug.CheckNah(subPartName + " in " + nameof(accountedForBodyParts), Indent: indent[3]);
                        continue;
                    }
                    if (subPart.Manager.IsNullOrEmpty())
                    {
                        Debug.CheckNah(subPartName + " not managed", Indent: indent[3]);
                        continue;
                    }
                    Debug.CheckYeh(subPartName + " eligible", Indent: indent[3]);

                    accountedForBodyParts.Add(subPart);
                    PseudoLimb pseudoLimb = new(subPart, null, null, ref amountStored);
                    ExtraLimbs.Add(pseudoLimb, bodyPart.Type);
                    bodyPartExtraLimbs.Add(pseudoLimb);
                }
                if (bodyPartExtraLimbs.IsNullOrEmpty())
                {
                    Debug.CheckYeh("Subparts Looped, nothing to copy", Indent: indent[2]);
                    continue;
                }
                Debug.CheckYeh("Subparts Looped, " + bodyPartExtraLimbs.Count.Things("limb tree") + " copied", Indent: indent[2]);

                List<BodyPart> potentialTargetBodyParts = DestinationBody?.LoopParts()
                    ?.Where(IsNonChimericConcreteIntrinsicNative)
                    ?.Where(bp => !accountedForDestinationBodyParts.Contains(bp))
                    ?.ToList();

                BodyPart targetBodyPart = 
                    potentialTargetBodyParts
                        ?.GetRandomElementCosmeticExcluding(bp => bp.Type != bodyPart.Type)
                    ?? potentialTargetBodyParts
                        ?.GetRandomElementCosmetic();

                Debug.CheckYeh("Getting " + nameof(targetBodyPart) + " from " + nameof(DestinationBody), Indent: indent[2]);
                Debug.YehNah(targetBodyPart?.BodyPartString(WithManager: true)?.Strip(), targetBodyPart != null, Indent: indent[3]);
                if (targetBodyPart != null)
                {
                    accountedForDestinationBodyParts.Add(targetBodyPart);
                    foreach (PseudoLimb bodyPartExtraLimb in bodyPartExtraLimbs)
                    {
                        bool given = bodyPartExtraLimb.GiveToEntity(destinationObject, targetBodyPart, ref amountGiven);
                        Debug.YehNah(bodyPartExtraLimb?.ToString(), given, Indent: indent[4]);
                    }
                }
            }
            totalDestinationParts = DestinationBody?.LoopParts()?.Count() ?? 0;
            Debug.LogArgs("Body Copy Info (", ")", indent[1],
                ArgPairs: new Debug.ArgPair[]
                {
                        Debug.Arg(nameof(totalSourceParts), totalSourceParts),
                        Debug.Arg(nameof(amountStored), amountStored),
                        Debug.Arg(nameof(amountGiven), amountGiven),
                        Debug.Arg(nameof(totalDestinationParts), totalDestinationParts),
                });

            Debug.CheckYeh("Looping all DestinationBody (" + (destinationObject?.DebugName ?? NULL) + ") parts.", Indent: indent[1]);
            foreach (BodyPart bodyPart in DestinationBody.LoopParts())
            {
                int depth = DestinationBody.GetBody().GetPartDepth(bodyPart, 0);
                Debug.Log(bodyPart.BodyPartString(WithManager: true, WithParent: true), Indent: indent[depth + 2]);
            }
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
            {
                return false;
            }
            return RoughlyCopyAdditionalLimbs(FrankenCorpse.Body, PastLife.Body, ref PastLife.ExtraLimbs);
        }
        public bool RestoreAdditionalLimbs(out Body FrankenBody)
        {
            return RestoreAdditionalLimbs(ParentObject, this, out FrankenBody);
        }
        public bool RestoreAdditionalLimbs()
        {
            return RestoreAdditionalLimbs(out _);
        }

        public static bool RestoreTaxonomy(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(PastLife), PastLife != null),
                    Debug.Arg(nameof(PastLife.Species), PastLife?.Species ?? NULL),
                    Debug.Arg(nameof(PastLife.Genotype), PastLife?.Genotype ?? NULL),
                    Debug.Arg(nameof(PastLife.Subtype), PastLife?.Subtype ?? NULL),
                });

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
        {
            return RestoreTaxonomy(ParentObject, this);
        }

        public static bool RestoreMutations(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out Mutations FrankenMutations,
            Predicate<BaseMutation> Exclude = null)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(PastLife), PastLife != null),
                    Debug.Arg("out " + nameof(FrankenMutations)),
                    Debug.Arg(nameof(Exclude), Exclude != null),
                });
            FrankenMutations = null;
            bool any = false;
            if (FrankenCorpse == null
                || PastLife == null
                || PastLife.Mutations == null
                || PastLife.Mutations.ActiveMutationList.IsNullOrEmpty())
            {
                Debug.CheckNah("PastLife, Mutations, or ActiveMutations missing.", indent[1]);
                return any;
            }
            FrankenMutations = FrankenCorpse.RequirePart<Mutations>();
            foreach (BaseMutation baseMutation in PastLife.Mutations.ActiveMutationList)
            {
                BaseMutation baseMutationToAdd = baseMutation;
                bool alreadyHaveMutation = FrankenMutations.HasMutation(baseMutation.Name);
                if (alreadyHaveMutation)
                {
                    baseMutationToAdd = FrankenMutations.GetMutation(baseMutation.Name);
                }
                if (Exclude != null && Exclude(baseMutationToAdd))
                {
                    Debug.CheckNah(baseMutationToAdd?.DebugName, "Excluded", indent[1]);
                    continue;
                }
                if (baseMutationToAdd.CapOverride == -1)
                {
                    baseMutationToAdd.CapOverride = baseMutation.Level;
                }
                if (!alreadyHaveMutation)
                {
                    FrankenMutations.AddMutation(baseMutationToAdd.Name, baseMutationToAdd.Variant, baseMutation.Level);
                }
                else
                {
                    baseMutationToAdd.BaseLevel += baseMutation.Level;
                }
                FrankenMutations.AddMutation(baseMutationToAdd, baseMutation.BaseLevel);
                Debug.CheckYeh(baseMutationToAdd.GetDisplayName(), baseMutationToAdd.Level, indent[1]);
                any = true;
            }
            return any;
        }
        public bool RestoreMutations(
            out Mutations FrankenMutations,
            Predicate<BaseMutation> Exclude = null)
        {
            return RestoreMutations(ParentObject, this, out FrankenMutations, Exclude);
        }

        public static bool RestoreSkills(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out Skills FrankenSkills,
            Predicate<BaseSkill> Exclude = null)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(PastLife), PastLife != null),
                    Debug.Arg("out " + nameof(FrankenSkills)),
                    Debug.Arg(nameof(Exclude), Exclude != null),
                });
            FrankenSkills = null;
            bool any = false;
            if (FrankenCorpse == null
                || PastLife == null
                || PastLife.Skills == null
                || PastLife.Skills.SkillList.IsNullOrEmpty())
            {
                return any;
            }
            FrankenSkills = FrankenCorpse.RequirePart<Skills>();
            foreach (BaseSkill baseSkill in PastLife.Skills.SkillList)
            {
                if ((Exclude != null && Exclude(baseSkill))
                    || FrankenCorpse.HasSkill(baseSkill.Name)
                    || FrankenCorpse.HasPart(baseSkill.Name))
                {
                    Debug.CheckNah(baseSkill.Name, indent[1]);
                    continue;
                }
                any = FrankenSkills.AddSkill(baseSkill.DeepCopy(FrankenCorpse, null) as BaseSkill) || any;
                Debug.CheckYeh(baseSkill.Name, indent[1]);
            }
            return any;
        }

        public bool RestoreSkills(
            out Skills FrankenSkills,
            Predicate<BaseSkill> Exclude = null)
        {
            return RestoreSkills(ParentObject, this, out FrankenSkills, Exclude);
        }

        public static bool RestoreCybernetics(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out bool WereCyberneticsInstalled)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(PastLife), PastLife != null),
                    Debug.Arg(nameof(PastLife.InstalledCybernetics), PastLife?.InstalledCybernetics?.Count ?? 0),
                });
            WereCyberneticsInstalled = false;
            if (FrankenCorpse == null
                || FrankenCorpse.Body is not Body frankenBody
                || PastLife == null
                || PastLife.InstalledCybernetics.IsNullOrEmpty())
            {
                return false;
            }
            foreach ((GameObject cybernetic, string bodyPartType) in PastLife.InstalledCybernetics)
            {
                if (frankenBody.FindCybernetics(cybernetic) != null)
                {
                    continue;
                }
                if (cybernetic.DeepCopy() is GameObject newCybernetic
                    && newCybernetic.TryRemoveFromContext())
                {
                    if (newCybernetic.TryGetPart(out CyberneticsBaseItem cyberneticBasePart))
                    {
                        int cyberneticsCost = cyberneticBasePart.Cost;
                        FrankenCorpse.ModIntProperty(CYBERNETICS_LICENSES, cyberneticsCost);
                        FrankenCorpse.ModIntProperty(CYBERNETICS_LICENSES_FREE, cyberneticsCost);

                        List<BodyPart> bodyParts = frankenBody.GetPart(bodyPartType);
                        bodyParts.ShuffleInPlace();

                        foreach (BodyPart bodyPart in bodyParts)
                        {
                            if (bodyPart.CanReceiveCyberneticImplant()
                                && !bodyPart.HasInstalledCybernetics())
                            {
                                bodyPart.Implant(newCybernetic);
                                WereCyberneticsInstalled = true;
                                break;
                            }
                        }
                    }
                }
            }
            return true;
        }
        public bool RestoreCybernetics(out bool WereCyberneticsInstalled)
        {
            return RestoreCybernetics(ParentObject, this, out WereCyberneticsInstalled);
        }

        public static bool RestoreParts(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            Predicate<IPart> Filter = null)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(PastLife), PastLife != null),
                    Debug.Arg(nameof(Filter), Filter != null),  
                });

            if (FrankenCorpse == null
                || PastLife == null
                || PastLife.BrainInAJar == null
                || PastLife.BrainInAJar.PartsList.IsNullOrEmpty())
            {
                return false;
            }

            List<string> bIAJ_BlueprintParts = new();
            if (GameObjectFactory.Factory.GetBlueprintIfExists(BRAIN_IN_A_JAR_BLUEPRINT) is GameObjectBlueprint bIAJ_Blueprint)
            {
                bIAJ_BlueprintParts = bIAJ_Blueprint.Parts?.Values?.Select(p => p.Name)?.ToList();
            }

            foreach (IPart pastPart in PastLife.BrainInAJar.PartsList)
            {
                if (FrankenCorpse.HasPart(pastPart.Name))
                {
                    Debug.CheckNah(pastPart.Name + " already present", Indent: indent[1]);
                    continue;
                }

                if (PartsToNotRetain.Contains(pastPart.Name))
                {
                    Debug.CheckNah(pastPart.Name + " already present", Indent: indent[1]);
                    continue;
                }

                if (pastPart is BaseSkill)
                {
                    Debug.CheckNah(pastPart.Name + " is " + nameof(BaseSkill), Indent: indent[1]);
                    continue;
                }

                if (pastPart is BaseMutation)
                {
                    Debug.CheckNah(pastPart.Name + " is " + nameof(BaseMutation), Indent: indent[1]);
                    continue;
                }

                if (bIAJ_BlueprintParts.Contains(pastPart.Name))
                {
                    Debug.CheckNah(pastPart.Name + " is " + nameof(BrainInAJar) + " part", Indent: indent[1]);
                    continue;
                }

                if (Filter != null && !Filter(pastPart))
                {
                    Debug.CheckNah(pastPart.Name + " not " + nameof(Filter), Indent: indent[1]);
                    continue;
                }

                FrankenCorpse.AddPart(pastPart);
                Debug.CheckYeh(pastPart.Name + " added", Indent: indent[1]);
            }
            return true;
        }
        public bool RestoreParts(Predicate<IPart> Filter = null)
            => RestoreParts(ParentObject, this, Filter);

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == GetDebugInternalsEvent.ID;

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
            {
                E.AddEntry(this, nameof(DisplayNameAdjectives),
                    DisplayNameAdjectives.AdjectiveList
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            }
            else
            {
                E.AddEntry(this, nameof(DisplayNameAdjectives), "Empty");
            }

            E.AddEntry(this, nameof(Titles), Titles?.TitleList ?? "Empty");
            E.AddEntry(this, nameof(Epithets), Epithets?.EpithetList ?? "Empty");
            E.AddEntry(this, nameof(Honorifics), Honorifics?.HonorificList ?? "Empty");

            if (PastRender != null)
            {
                try
                {
                    // Traverse pastRenderWalk = new(PastRender);
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
            }
            else
            {
                E.AddEntry(this, nameof(PastRender), "Empty");
            }

            E.AddEntry(this, nameof(Description), YehNah(Description != null));
            E.AddEntry(this, nameof(DeathAddress), DeathAddress.ToString());

            if (Brain != null && !Brain.Allegiance.IsNullOrEmpty())
            {
                try
                {
                    // Traverse brainWalk = new(Brain);

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
            }
            else
            {
                E.AddEntry(this, nameof(Brain), "Empty");
            }

            E.AddEntry(this, nameof(Gender), Gender?.Name ?? NULL);
            E.AddEntry(this, nameof(PronounSet), PronounSet?.Name ?? NULL);
            E.AddEntry(this, nameof(ConversationScriptID), ConversationScriptID);

            if (!Stats.IsNullOrEmpty())
            {
                E.AddEntry(this, nameof(Stats),
                    Stats
                    ?.ConvertToStringList(kvp => kvp.Key + ": " + (kvp.Value?.Value ?? 0) + "/" + (kvp.Value?.BaseValue ?? 0))
                    ?.ToList()
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            }
            else
            {
                E.AddEntry(this, nameof(Stats), "Empty");
            }

            E.AddEntry(this, nameof(Body), Body?.Anatomy ?? "No Anatomy!?");

            if (!ExtraLimbs.IsNullOrEmpty())
            {
                E.AddEntry(this, nameof(ExtraLimbs),
                    ExtraLimbs
                    ?.ConvertToStringList(kvp => "for " + kvp.Value + ": " + kvp.Key.ToString())
                    ?.ToList()
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            }
            else
            {
                E.AddEntry(this, nameof(ExtraLimbs), "Empty");
            }

            E.AddEntry(this, nameof(Corpse), (Corpse?.CorpseBlueprint ?? NULL) + " (" + (Corpse?.CorpseChance ?? 0) + ")");

            E.AddEntry(this, nameof(Species), Species);
            E.AddEntry(this, nameof(Genotype), Genotype);
            E.AddEntry(this, nameof(Subtype), Subtype);

            if (!MutationsList.IsNullOrEmpty())
            {
                E.AddEntry(this, nameof(MutationsList),
                    MutationsList
                    ?.ConvertAll(md => md.ToString())
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            }
            else
            {
                E.AddEntry(this, nameof(MutationsList), "Empty");
            }

            if (Skills != null && !Skills.SkillList.IsNullOrEmpty())
            {
                E.AddEntry(this, nameof(Skills),
                    Skills.SkillList
                    ?.ConvertAll(s => s.Name)
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            }
            else
            {
                E.AddEntry(this, nameof(Skills), "Empty");
            }

            if (!InstalledCybernetics.IsNullOrEmpty())
            {
                E.AddEntry(this, nameof(InstalledCybernetics),
                    InstalledCybernetics
                    ?.ConvertAll(ic => ic.ToString())
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            }
            else
            {
                E.AddEntry(this, nameof(InstalledCybernetics), "Empty");
            }

            if (!Tags.IsNullOrEmpty())
            {
                E.AddEntry(this, nameof(Tags),
                    Tags
                    ?.ConvertToStringList(kvp => (kvp.Value != null) ? (kvp.Key + ": " + kvp.Value) : kvp.Key)
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            }
            else
            {
                E.AddEntry(this, nameof(Tags), "Empty");
            }

            if (!StringProperties.IsNullOrEmpty())
            {
                E.AddEntry(this, nameof(StringProperties),
                    StringProperties
                    ?.ConvertToStringList(kvp => (kvp.Value != null) ? (kvp.Key + ": " + kvp.Value) : kvp.Key)
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            }
            else
            {
                E.AddEntry(this, nameof(StringProperties), "Empty");
            }

            if (!IntProperties.IsNullOrEmpty())
            {
                E.AddEntry(this, nameof(IntProperties),
                    IntProperties
                    ?.ConvertToStringList(kvp => kvp.Key + ": " + kvp.Value)
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            }
            else
            {
                E.AddEntry(this, nameof(IntProperties), "Empty");
            }

            if (!Effects.IsNullOrEmpty())
            {
                E.AddEntry(this, nameof(Effects),
                    Effects
                    .ToList()
                    ?.ConvertAll(fx => fx.ClassName + ": " + ((fx.Duration == 9999) ? "Indefinite" : fx.Duration.ToString()))
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            }
            else
            {
                E.AddEntry(this, nameof(Effects), "Empty");
            }

            return base.HandleEvent(E);
        }

        public void SummonBrainInAJar()
        {
            if (!BrainInAJar.Statistics.ContainsKey("Enenrgy"))
            {
                BrainInAJar.Statistics.Add("Enenrgy", new("Enenrgy", -100000, 100000, 0, BrainInAJar));
            }
            else
            {
                BrainInAJar.Statistics["Enenrgy"] = new("Enenrgy", -100000, 100000, 0, BrainInAJar);
            }
            BrainInAJar?.FinalizeStats();
            if (BrainInAJar.Statistics.ContainsKey("Enenrgy"))
            {
                The.Player.CurrentCell
                    ?.GetAdjacentCells()
                    ?.GetRandomElementCosmetic()
                    ?.AddObject(BrainInAJar);
            }
            else
            {
                Popup.Show("Couldn't give " + nameof(BrainInAJar) + ", " + (BrainInAJar?.DebugName ?? NULL) + ", energy stat.\n\n" +
                    "Unable to summon" + (BrainInAJar?.them ?? "it") + ".");
            }
        }

        /*
         * 
         * Wishes!
         * 
         */

        public virtual void DebugOutput()
        {
            Debug.ResetIndent();
            using Indent indent = new();
            try
            {
                Debug.Log(nameof(UD_FleshGolems_PastLife), ParentObject.DebugName, indent);

                Debug.Log(nameof(Init), Init, indent[1]);
                Debug.Log(nameof(WasCorpse), WasCorpse, indent);
                Debug.Log(nameof(WasPlayer), WasPlayer, indent);

                Debug.Log(nameof(TimesReanimated), TimesReanimated, indent);

                Debug.Log(nameof(Blueprint), Blueprint, indent);
                Debug.Log(nameof(BaseDisplayName), BaseDisplayName, indent);
                Debug.Log(nameof(RefName), RefName, indent);
                Debug.Log(nameof(WasProperlyNamed), WasProperlyNamed, indent);

                Debug.Log(nameof(DisplayNameAdjectives), DisplayNameAdjectives, indent);
                Debug.Log(nameof(Titles), Titles, indent);
                Debug.Log(nameof(Epithets), Epithets, indent);
                Debug.Log(nameof(Honorifics), Honorifics, indent);

                Debug.Log(nameof(PastRender), PastRender, indent);
                Debug.Log(nameof(Description), Description, indent);

                Debug.Log(nameof(DeathAddress), DeathAddress, indent);

                Debug.Log(nameof(Brain), Brain != null, indent);
                Debug.Log(nameof(Brain.Allegiance), Indent: indent[2]);
                foreach ((string faction, int rep) in Brain?.Allegiance ?? new())
                {
                    Debug.Log(faction, rep, indent[3]);
                }
                if (Brain != null)
                {
                    Debug.Log("bools", Indent: indent[2]);
                    Traverse brainWalk = new(Brain);
                    foreach (string field in brainWalk.Fields() ?? new())
                    {
                        string fieldValue = brainWalk?.Field(field)?.GetValue()?.ToString();
                        Debug.Log(field, fieldValue ?? "??", indent[3]);
                    }
                }
                Debug.Log(nameof(Gender), Gender, indent[1]);
                Debug.Log(nameof(PronounSet), PronounSet, indent);
                Debug.Log(nameof(ConversationScriptID), ConversationScriptID, indent);

                Debug.Log(nameof(Stats), Stats?.Count, indent);
                foreach ((string statName, Statistic stat) in Stats ?? new())
                {
                    Debug.Log(statName, stat.BaseValue, indent[2]);
                }
                Debug.Log(nameof(Species), Species, indent[1]);
                Debug.Log(nameof(Genotype), Genotype, indent);
                Debug.Log(nameof(Subtype), Subtype, indent);

                Debug.Log(nameof(Mutations), Mutations?.ActiveMutationList?.Count, indent);
                foreach (BaseMutation mutation in Mutations?.ActiveMutationList)
                {
                    Debug.Log(mutation.Name, mutation.BaseLevel, indent[2]);
                }
                Debug.Log(nameof(Skills), Skills?.SkillList?.Count, indent[1]);
                foreach (BaseSkill baseSkill in Skills?.SkillList)
                {
                    Debug.Log(baseSkill.Name, Indent: indent[2]);
                }
                Debug.Log(nameof(InstalledCybernetics), new List<InstalledCybernetic>(InstalledCybernetics)?.Count, indent[1]);
                foreach ((GameObject cybernetic, string implantedLimb) in InstalledCybernetics)
                {
                    Debug.Log(implantedLimb, cybernetic.Blueprint, indent[2]);
                }

                Debug.Log(nameof(Tags), Tags?.Count, indent[1]);
                foreach ((string name, string value) in Tags ?? new())
                {
                    Debug.Log(name, value, indent[2]);
                }
                Debug.Log(nameof(StringProperties), StringProperties?.Count, indent[1]);
                foreach ((string name, string value) in StringProperties ?? new())
                {
                    Debug.Log(name, value, indent[2]);
                }
                Debug.Log(nameof(IntProperties), IntProperties?.Count, indent[1]);
                foreach ((string name, int value) in IntProperties ?? new())
                {
                    Debug.Log(name, value, indent[2]);
                }

                Debug.Log(nameof(Effects), Effects?.Count, indent[1]);
                foreach (Effect Effect in Effects)
                {
                    Debug.Log(Effect.ClassName + ",  duration", Effect.Duration, indent[2]);
                }
            }
            catch (Exception x)
            {
                MetricsManager.LogException(Name + "." + nameof(DebugOutput), x, "game_mod_exception");
            }
        }

        [WishCommand("UD_FleshGolems debug PastLife")]
        public static void Debug_PastLife_WishHandler()
        {
            bool silenceLogging = Debug.SilenceLogging;
            Debug.SilenceLogging = true;

            int startX = 40;
            int startY = 12;
            if (The.Player.CurrentCell is Cell playerCell)
            {
                startX = playerCell.X;
                startY = playerCell.Y;
            }
            if (PickTarget.ShowPicker(
                Style: PickTarget.PickStyle.EmptyCell,
                StartX: startX,
                StartY: startY,
                VisLevel: AllowVis.Any,
                ObjectTest: GO => GO.HasPart<UD_FleshGolems_PastLife>(),
                Label: "debug " + nameof(UD_FleshGolems_PastLife)) is Cell pickCell
                && Popup.PickGameObject(
                    Title: "pick a thing with a past life",
                    Objects: pickCell.GetObjectsWithPart(nameof(UD_FleshGolems_PastLife)),
                    AllowEscape: true,
                    ShortDisplayNames: true) is GameObject pickedObject)
            {
                pickedObject?.GetPart<UD_FleshGolems_PastLife>().DebugOutput();
                Popup.Show(
                    "debug output for " + Grammar.MakePossessive(pickedObject.ShortDisplayNameSingleStripped) + " " +
                    nameof(UD_FleshGolems_PastLife));
            }
            else
            {
                Popup.Show("nothing selected to debug " + nameof(UD_FleshGolems_PastLife));
            }
            Debug.SilenceLogging = silenceLogging;
        }
    }
}
