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

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Capabilities;
using UD_FleshGolems.Capabilities.Necromancy;
using UD_FleshGolems.Parts.PastLifeHelpers;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using SerializeField = UnityEngine.SerializeField;
using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;

namespace XRL.World.Parts
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    [HasWishCommand]
    [Serializable]
    public class UD_FleshGolems_PastLife : IScribedPart
    {
        [UD_FleshGolems_DebugRegistry]
        public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
        {
            Registry.Register(nameof(GetCorpseBlueprints), true);
            Registry.Register(nameof(GetAnEntityForCorpseWeighted), true);
            Registry.Register(nameof(GetAnEntityForCorpse), true);
            return Registry;
        }

        public const string PASTLIFE_BLUEPRINT_PROPTAG = "UD_FleshGolems_PastLife_Blueprint";

        public GameObject BrainInAJar;

        public UD_FleshGolems_PastLife PastPastLife => BrainInAJar?.GetPart<UD_FleshGolems_PastLife>();

        public bool Init { get; protected set; }
        public bool WasCorpse => (Blueprint?.IsCorpse()).GetValueOrDefault();

        public bool WasPlayer 
            => (!Blueprint.IsNullOrEmpty() && Blueprint.IsPlayerBlueprint()) 
            || (BrainInAJar != null && BrainInAJar.HasPropertyOrTag("UD_FleshGolems_WasPlayer"));

        public int TimesReanimated;

        public string Blueprint;

        public bool ExcludeFromDynamicEncounters => Blueprint.IsExcludedFromDynamicEncounters();

        [SerializeField]
        private string _BaseDisplayName;
        public string BaseDisplayName => _BaseDisplayName ??= BrainInAJar?.BaseDisplayName;

        [SerializeField]
        private string _RefName;
        public string RefName => _RefName ??= BrainInAJar?.GetReferenceDisplayName(Short: true);
        public bool WasProperlyNamed => (BrainInAJar?.HasProperName).GetValueOrDefault();
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
        public bool IsTrifling => (BrainInAJar?.IsTrifling).GetValueOrDefault();

        public Body Body => BrainInAJar?.Body;
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
            Init = false;

            TimesReanimated = 0;

            Blueprint = null;

            _BaseDisplayName = null;
            _RefName = null;
            _Description = null;

            DeathAddress = new();

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
            => GameObjectFactory.Factory.CreateUnmodifiedObject("UD_FleshGolems Brain In A Jar Widget");

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

                        Debug.Log(nameof(PastLife), PastLife?.DebugName ?? NULL, indent[2]);

                        if (PastLife.TryGetPart(out UD_FleshGolems_PastLife prevPastLife)
                            && prevPastLife.DeepCopy(BrainInAJar, DeepCopyMapInventory) is UD_FleshGolems_PastLife prevPastLifeCopy)
                        {
                            Debug.Log(nameof(prevPastLifeCopy), prevPastLifeCopy?.BrainInAJar?.DebugName ?? NULL, indent[2]);
                            BrainInAJar.AddPart(prevPastLifeCopy);
                        }

                        if (PastLife.IsPlayer())
                        {
                            BrainInAJar.SetStringProperty("UD_FleshGolems_WasPlayer", "Yep, I used to be the player!");
                        }
                        Debug.Log(nameof(WasPlayer), WasPlayer, indent[2]);

                        if (PastLife.GetBlueprint().InheritsFrom("UD_FleshGolems Brain In A Jar Widget")
                            || PastLife.GetBlueprint().InheritsFrom("Corpse")
                            || WasCorpse)
                        {
                            TimesReanimated++;
                        }
                        Debug.Log(nameof(TimesReanimated), TimesReanimated, indent[2]);

                        BrainInAJar.HasProperName = PastLife.HasProperName 
                            || PastLife.GetBlueprint().GetxTag("Grammar", "Proper").EqualsNoCase("true");

                        Debug.Log(nameof(BrainInAJar.HasProperName), BrainInAJar.HasProperName, indent[2]);

                        BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<DisplayNameAdjectives>());
                        BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Titles>());
                        BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Epithets>());
                        BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Honorifics>());

                        if (WasCorpse)
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

                        BrainInAJar._Property = new(PastLife._Property);
                        BrainInAJar._IntProperty = new(PastLife._IntProperty);

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

                        Render bIAJ_Render = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.Render);
                        BrainInAJar.Render = bIAJ_Render;

                        Description bIAJ_Description = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Description>());

                        if (PastLife.CurrentCell is Cell deathCell
                            && deathCell.ParentZone is Zone deathZone)
                        {
                            DeathAddress = new(deathZone.ZoneID, deathCell.Location);
                        }
                        Debug.Log(nameof(DeathAddress), DeathAddress.ToString() ?? NULL, indent[2]);

                        Physics bIAJ_Physics = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.Physics);
                        BrainInAJar.Physics = bIAJ_Physics;

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

                        RoughlyCopyAdditionalLimbs(bIAJ_Body, PastLife?.Body);

                        string bleedLiquid = PastLife.GetBleedLiquid();
                        BrainInAJar.SetBleedLiquid(bleedLiquid);
                        Debug.Log(nameof(bleedLiquid), bleedLiquid ?? NULL, indent[2]);

                        Corpse bIAJ_Corpse = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Corpse>());
                        bIAJ_Corpse.CorpseBlueprint = ParentObject.Blueprint;
                        if (bIAJ_Corpse.CorpseBlueprint.IsNullOrEmpty())
                        {
                            if (Blueprint
                                .GetGameObjectBlueprint()
                                .TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseBlueprint), out string corpseBlueprint))
                            {
                                bIAJ_Corpse.CorpseBlueprint = corpseBlueprint;
                            }
                            else
                            if ((PastLife.GetSpecies() + " Corpse").GetGameObjectBlueprint() is GameObjectBlueprint corpseGameObjectBlueprint
                                && corpseGameObjectBlueprint
                                    .TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseBlueprint), out string speciesCorpseBlueprint))
                            {
                                bIAJ_Corpse.CorpseBlueprint = speciesCorpseBlueprint;
                            }
                        }
                        bIAJ_Corpse.CorpseChance = 100;
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

                        Debug.Log(nameof(PastLife.Statistics), PastLife?.Statistics?.Count ?? 0, indent[2]);
                        if (!PastLife.Statistics.IsNullOrEmpty())
                        {
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
                                /*
                                if (statName == "Hitpoints")
                                {
                                    newStat.Penalty = 0;
                                }
                                newStat.Owner = BrainInAJar;
                                */

                                BrainInAJar.Statistics.Add(statName, newStat);
                                Debug.Log(statName, newStat.Value + "/" + newStat.BaseValue + " | " + (newStat.sValue ?? "no sValue"), indent[3]);
                            }
                        }

                        BrainInAJar.IsTrifling = PastLife.IsTrifling;

                        BrainInAJar.SetSpecies(PastLife.GetSpecies());
                        BrainInAJar.SetGenotype(PastLife.GetGenotype());
                        BrainInAJar.SetSubtype(PastLife.GetSubtype());


                        Debug.Log(nameof(Species), Species ?? NULL, indent[2]);
                        Debug.Log(nameof(Genotype), Genotype ?? NULL, indent[2]);
                        Debug.Log(nameof(Subtype), Subtype ?? NULL, indent[2]);

                        Mutations bIAJ_Mutations = BrainInAJar.AddPart(PastLife.RequirePart<Mutations>());
                        Debug.Log(nameof(MutationsList) + "(BaseLevel|CapOverride|RapidLevel)", Indent: indent[2]);
                        foreach (BaseMutation bIAJ_Mutation in bIAJ_Mutations.ActiveMutationList)
                        {
                            // bIAJ_Mutations.AddMutation(pastMutation.GetMutationClass(), pastMutation.Variant, pastMutation.Level);
                            /*
                            if (bIAJ_Mutations.GetMutation(bIAJ_Mutation.Name) is BaseMutation bIAJ_Mutation)
                            {
                                if (bIAJ_Mutation.GetRapidLevelAmount() is int pastRapidLevel
                                    && pastRapidLevel > 0)
                                {
                                    bIAJ_Mutation.SetRapidLevelAmount(pastRapidLevel);
                                }
                                bIAJ_Mutation.CapOverride = bIAJ_Mutation.CapOverride;
                            }
                            */
                            MutationData mutationData = new(bIAJ_Mutation, bIAJ_Mutation.GetRapidLevelAmount());
                            MutationsList.Add(mutationData);
                            Debug.Log(mutationData.ToString(), Indent: indent[3]);
                        }

                        Skills bIAJ_Skills = BrainInAJar.AddPart(PastLife.RequirePart<Skills>());
                        List<BaseSkill> pastSkills = PastLife.GetPartsDescendedFrom<BaseSkill>();
                        Debug.Log(nameof(pastSkills), pastSkills?.Count ?? 0, indent[2]);
                        foreach (BaseSkill baseSkill in pastSkills)
                        {
                            Debug.Log(baseSkill.Name, Indent: indent[3]);
                            // There's a bug in v1.04 with how Skills serializes its BaseSkills
                            // that means the only way to guarantee copying them is via the parts list.
                            if (!bIAJ_Skills.SkillList.Contains(baseSkill))
                            {
                                bIAJ_Skills.AddSkill(baseSkill);
                            }
                        }

                        Debug.Log(nameof(PastLife.Effects), PastLife.Effects?.Count ?? 0, indent[2]);
                        foreach (Effect pastEffect in PastLife.Effects)
                        {
                            BrainInAJar.Effects.Add(pastEffect.DeepCopy(BrainInAJar, null));
                            Debug.Log(pastEffect.DisplayNameStripped, Indent: indent[3]);
                        }

                        Debug.Log(nameof(InstalledCybernetics) + "...", Indent: indent[2]);
                        if (PastLife?.Body is Body pastBody
                            && pastBody.GetInstalledCyberneticsReadonly() is List<GameObject> installedCybernetics
                            && InstalledCybernetics.IsNullOrEmpty())
                        {
                            foreach (GameObject installedCybernetic in installedCybernetics)
                            {
                                if (installedCybernetic?.Implantee?.Body is Body implanteeBody)
                                {
                                    InstalledCybernetics.Add(new(installedCybernetic, implanteeBody));
                                }
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
                    {
                        PastLife?.Obliterate();
                    }
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
            if (FrankenCorpse == null || PastLife == null)
            {
                return false;
            }
            FrankenBrain = FrankenCorpse.Brain;
            if (FrankenBrain != null
                && PastLife?.Brain is Brain pastBrain)
            {
                FrankenCorpse.Brain.Allegiance ??= new();
                FrankenBrain.Allegiance.Hostile = pastBrain.Allegiance.Hostile;
                FrankenBrain.Allegiance.Calm = pastBrain.Allegiance.Calm;
                if ((!UD_FleshGolems_Reanimated.HasWorldGenerated || ExcludedFromDynamicEncounters))
                {
                    FrankenCorpse.Brain.Allegiance.Clear();
                    FrankenCorpse.Brain.Allegiance.Add("Newly Sentient Beings", 75);
                    foreach ((string faction, int rep) in pastBrain.Allegiance)
                    {
                        if (!pastBrain.Allegiance.ContainsKey(faction))
                        {
                            FrankenCorpse.Brain.Allegiance.Add(faction, rep);
                        }
                        else
                        {
                            FrankenCorpse.Brain.Allegiance[faction] += rep;
                        }
                    }
                    if (!FrankenCorpse.HasPropertyOrTag("StartingPet") && !FrankenCorpse.HasPropertyOrTag("Pet"))
                    {
                        FrankenCorpse.Brain.PartyLeader = pastBrain.PartyLeader;
                        FrankenCorpse.Brain.PartyMembers = pastBrain.PartyMembers;

                        FrankenCorpse.Brain.Opinions = pastBrain.Opinions;
                    }
                }
                FrankenBrain.Wanders = pastBrain.Wanders;
                FrankenBrain.WallWalker = pastBrain.WallWalker;
                FrankenBrain.HostileWalkRadius = pastBrain.HostileWalkRadius;

                FrankenBrain.Mobile = pastBrain.Mobile;
            }
            return true;
        }
        public bool RestoreBrain(
            bool ExcludedFromDynamicEncounters,
            out Brain FrankenBrain)
        {
            return RestoreBrain(ParentObject, this, ExcludedFromDynamicEncounters, out FrankenBrain);
        }

        public static string GenerateDisplayName(UD_FleshGolems_PastLife PastLife)
        {
            if (PastLife == null)
            {
                return null;
            }
            string oldIdentity = PastLife?.RefName;
            string newIdentity;
            using Indent indent = new();
            Debug.LogMethod(indent[1],
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(PastLife.ParentObject), PastLife.ParentObject?.DebugName ?? NULL),
                    Debug.Arg(nameof(oldIdentity), oldIdentity ?? NULL),
                });
            if (PastLife.WasProperlyNamed)
            {
                newIdentity = "corpse of " + oldIdentity;
            }
            else
            {
                if (!PastLife.WasCorpse)
                {
                    newIdentity = oldIdentity + " corpse";
                }
                else
                {
                    newIdentity = /*PastLife.BrainInAJar?.DisplayName ??*/ PastLife.BrainInAJar?.Render?.DisplayName;
                }
            }
            Debug.Log(nameof(newIdentity), newIdentity ?? NULL, indent[1]);
            return newIdentity;
        }
        public string GenerateDisplayName()
        {
            return GenerateDisplayName(this);
        }

        public static string GenerateDescription(UD_FleshGolems_PastLife PastLife)
        {
            if (PastLife == null || PastLife.Description.IsNullOrEmpty())
            {
                return null;
            }
            string oldDescription = PastLife.Description;
            string postDescription = "In life this =subject.uD_xTag:UD_FleshGolems_CorpseText:CorpseDescription= was ";
            if (PastLife.WasPlayer)
            {
                postDescription += "you.";
            }
            else
            {

                string whoTheyWere = (PastLife?.RefName ?? PastLife.GetBlueprint().DisplayName());
                if (!PastLife.WasProperlyNamed)
                {
                    whoTheyWere = Grammar.A(whoTheyWere);
                }
                string postDescriptionEnd = PastLife.WasCorpse ? "." : ":\n" + oldDescription;
                postDescription += whoTheyWere + postDescriptionEnd;
            }
            return postDescription;
        }
        public string GenerateDescription()
        {
            return GenerateDescription(this);
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
        public bool RestoreAnatomy(
            out Body FrankenBody)
        {
            return RestoreAnatomy(ParentObject, this, out FrankenBody);
        }

        public static bool IsIntrinsicAndNative(BodyPart BodyPart)
        {
            return BodyPart.Native
                && !BodyPart.Extrinsic;
        }
        private static bool RoughlyCopyAdditionalLimbs(
            Body DestinationBody,
            Body SourceBody)
        {
            if (DestinationBody == null || SourceBody == null || SourceBody.ParentObject == null)
            {
                return false;
            }

            foreach (BodyPart bodyPart in SourceBody?.LoopParts())
            {
                if (bodyPart.Native
                    || bodyPart.Extrinsic
                    || !bodyPart.ParentPart.Native
                    || bodyPart?.ParentPart is not BodyPart parentPart
                    || parentPart.Type is not string parentPartType
                    || DestinationBody?.GetPart(parentPartType) is not List<BodyPart> destinationBodyPartsOfType
                    || destinationBodyPartsOfType.GetRandomElementCosmetic(IsIntrinsicAndNative) is not BodyPart targetBodyPart)
                {
                    continue;
                }
                BodyPart newParentPart = DestinationBody.GetParts()
                    ?.Where(bp => bp.Native && !bp.Extrinsic && bp.Type == bodyPart.Type)
                    ?.GetRandomElementCosmetic();
                targetBodyPart.AddPart(bodyPart?.DeepCopy(DestinationBody?.ParentObject, DestinationBody, newParentPart, DeepCopyMapInventory));
            }
            return true;
        }
        public static bool RestoreAdditionalLimbs(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out Body FrankenBody)
        {
            FrankenBody = null;
            if (FrankenCorpse == null || PastLife == null || PastLife.Body == null)
            {
                return false;
            }
            FrankenBody = FrankenCorpse.Body;
            return RoughlyCopyAdditionalLimbs(FrankenBody, PastLife.Body);
        }
        public bool RestoreAdditionalLimbs(
            out Body FrankenBody)
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
            if (FrankenCorpse == null || PastLife == null)
            {
                return false;
            }
            if (PastLife.Species is string pastSpecies)
            {
                FrankenCorpse.SetSpecies(pastSpecies);
            }
            if (PastLife.Genotype is string pastGenotype)
            {
                FrankenCorpse.SetGenotype(pastGenotype);
            }
            if (PastLife.Subtype is string pastSubtype)
            {
                FrankenCorpse.SetSubtype(pastSubtype);
            }
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
                Debug.CheckYeh(baseMutationToAdd.DebugName, baseMutationToAdd.Level, indent[1]);
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
            if (FrankenCorpse == null || PastLife == null)
            {
                return false;
            }
            if (!PastLife.InstalledCybernetics.IsNullOrEmpty())
            {
                Body frankenBody = FrankenCorpse.Body;
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
            }
            return true;
        }
        public bool RestoreCybernetics(out bool WereCyberneticsInstalled)
        {
            return RestoreCybernetics(ParentObject, this, out WereCyberneticsInstalled);
        }

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == GetDebugInternalsEvent.ID;

        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            E.AddEntry(this, nameof(Init), Init);
            return base.HandleEvent(E);
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
            Debug.SetSilenceLogging(true);
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
            Debug.SetSilenceLogging(false);
        }
    }
}
