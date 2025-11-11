using System;
using System.Collections.Generic;
using System.Text;

using Genkit;

using XRL.Collections;
using XRL.World;
using XRL.World.AI;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;

using UD_FleshGolems;
using HarmonyLib;
using XRL.Wish;
using XRL.UI;
using XRL.Language;
using XRL.World.Anatomy;

using SerializeField = UnityEngine.SerializeField;
using XRL.World.ObjectBuilders;

namespace XRL.World.Parts
{
    [HasWishCommand]
    [Serializable]
    public class UD_FleshGolems_PastLife : IScribedPart
    {
        [Serializable]
        public class UD_FleshGolems_DeathAddress : IComposite
        {
            public string DeathZone;
            public int X;
            public int Y;

            private UD_FleshGolems_DeathAddress()
            {
                DeathZone = null;
                X = 0;
                Y = 0;
            }
            public UD_FleshGolems_DeathAddress(string DeathZone, int X, int Y)
                : this()
            {
                this.DeathZone = DeathZone;
                this.X = X;
                this.Y = Y;
            }
            public UD_FleshGolems_DeathAddress(string DeathZone, Location2D DeathLocation)
                : this(DeathZone, DeathLocation.X, DeathLocation.Y)
            {
            }

            public Location2D GetLocation() => new(X, Y);

            public ZoneRequest GetZoneRequest() => new(DeathZone);

            public Cell GetCell() => The.ZoneManager?.GetZone(DeathZone)?.GetCell(X, Y);

            public static explicit operator Cell(UD_FleshGolems_DeathAddress Source)
            {
                return Source.GetCell();
            }
        }

        [Serializable]
        public class UD_FleshGolems_InstalledCybernetic : IComposite
        {
            public string ImplantedLimbType;

            [SerializeField]
            private string CyberneticID;

            private GameObject _Cybernetic;
            public GameObject Cybernetic
            {
                get => _Cybernetic ??= GameObject.FindByID(CyberneticID);
                set
                {
                    CyberneticID = value?.ID;
                    _Cybernetic = value;
                }
            }

            protected UD_FleshGolems_InstalledCybernetic()
            {
                ImplantedLimbType = null;
                Cybernetic = null;
            }
            public UD_FleshGolems_InstalledCybernetic(GameObject Cybernetic, string ImplantedPart)
                : this()
            {
                ImplantedLimbType = ImplantedPart;
                this.Cybernetic = Cybernetic;
            }
            public UD_FleshGolems_InstalledCybernetic(GameObject Cybernetic, BodyPart ImplantedPart)
                : this(Cybernetic, ImplantedPart.Type)
            {
            }
            public UD_FleshGolems_InstalledCybernetic(GameObject Cybernetic, Body ImplantedBody)
                : this(Cybernetic, ImplantedBody.FindCybernetics(Cybernetic))
            {
            }
            public UD_FleshGolems_InstalledCybernetic(GameObject Cybernetic)
                : this(Cybernetic, Cybernetic.Implantee.Body)
            {
            }
            public void Deconstruct(out GameObject Cybernetic, out string ImplantedLimbType)
            {
                ImplantedLimbType = this.ImplantedLimbType;
                Cybernetic = this.Cybernetic;
            }
            public void Deconstruct(out GameObject Cybernetic)
            {
                Cybernetic = this.Cybernetic;
            }
            public void Deconstruct(out string ImplantedLimbType)
            {
                ImplantedLimbType = this.ImplantedLimbType;
            }

            public static implicit operator KeyValuePair<GameObject, string>(UD_FleshGolems_InstalledCybernetic Source)
            {
                return new(Source.Cybernetic, Source.ImplantedLimbType);
            }
            public static implicit operator UD_FleshGolems_InstalledCybernetic(KeyValuePair<GameObject, string> Source)
            {
                return new(Source.Key, Source.Value);
            }
        }

        public GameObject BrainInAJar;

        public bool Init { get; protected set; }
        public bool WasCorpse => (GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint)?.InheritsFrom("Corpse")).GetValueOrDefault();

        public bool WasPlayer => Blueprint.IsPlayerBlueprint() || BrainInAJar.HasPropertyOrTag("UD_FleshGolems_WasPlayer");

        public int TimesReanimated;

        public string Blueprint;

        public bool ExcludeFromDynamicEncounters => (Blueprint?.GetGameObjectBlueprint()?.IsExcludedFromDynamicEncounters()).GetValueOrDefault();

        public string BaseDisplayName => BrainInAJar?.BaseDisplayName;
        public Titles Titles => BrainInAJar?.GetPart<Titles>();
        public Epithets Epithets => BrainInAJar?.GetPart<Epithets>();
        public Honorifics Honorifics => BrainInAJar?.GetPart<Honorifics>();

        public Render PastRender => BrainInAJar?.Render;

        public string Description => BrainInAJar?.GetPart<Description>()?._Short;

        public UD_FleshGolems_DeathAddress DeathAddress;

        public Brain Brain => BrainInAJar?.Brain;

        public Gender Gender => BrainInAJar?.GetGender();

        public PronounSet PronounSet => BrainInAJar?.GetPronounSet();

        public string ConversationScriptID => BrainInAJar?.GetPart<ConversationScript>()?.ConversationID;

        public Dictionary<string, Statistic> Stats => BrainInAJar?.Statistics;

        public string Species => BrainInAJar?.GetSpecies();
        public string Genotype => BrainInAJar?.GetGenotype();
        public string Subtype => BrainInAJar?.GetSubtype();

        public Mutations Mutations => BrainInAJar?.GetPart<Mutations>();

        public Skills Skills => BrainInAJar?.GetPart<Skills>();

        public Dictionary<string, string> Tags => Blueprint.GetGameObjectBlueprint().Tags;
        public Dictionary<string, string> StringProperties => BrainInAJar?._Property;
        public Dictionary<string, int> IntProperties => BrainInAJar?._IntProperty;

        public EffectRack Effects => BrainInAJar?._Effects;

        public IEnumerable<UD_FleshGolems_InstalledCybernetic> InstalledCybernetics
        {
            get
            {
                foreach (GameObject installedCybernetic in BrainInAJar?.Body.GetInstalledCyberneticsReadonly())
                {
                    yield return new(installedCybernetic);
                }
            }
        }

        public UD_FleshGolems_PastLife()
        {
            BrainInAJar = GetNewBrainInAJar();
            Init = false;

            TimesReanimated = 0;

            Blueprint = null;

            DeathAddress = null;
        }
        public UD_FleshGolems_PastLife(GameObject PastLife)
            : this()
        {
            Initialize(PastLife);
        }
        public UD_FleshGolems_PastLife(UD_FleshGolems_PastLife PrevPastLife)
            : this()
        {
            Initialize(PrevPastLife);
        }

        private static GameObject GetNewBrainInAJar()
        {
            return GameObjectFactory.Factory.CreateUnmodifiedObject("UD_FleshGolems Brain In A Jar Widget");
        }

        public void Initialize(GameObject PastLife)
        {
            if (!Init)
            {
                try
                {
                    BrainInAJar ??= GetNewBrainInAJar();
                    if (BrainInAJar != null)
                    {
                        Blueprint = PastLife.Blueprint;

                        if (PastLife.IsPlayer())
                        {
                            BrainInAJar.SetStringProperty("UD_FleshGolems_WasPlayer", "Yep, I used to be the player!");
                        }

                        if (PastLife.GetBlueprint().InheritsFrom("UD_FleshGolems Brain In A Jar Widget")
                            || PastLife.GetBlueprint().InheritsFrom("Corpse"))
                        {
                            TimesReanimated++;
                        }

                        BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Titles>());
                        BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Epithets>());
                        BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Honorifics>());

                        BrainInAJar._Property = PastLife._Property;
                        BrainInAJar._IntProperty = PastLife._IntProperty;

                        Render bIAJ_Render = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.Render);
                        BrainInAJar.Render = bIAJ_Render;

                        Description bIAJ_Description = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Description>());

                        if (PastLife.CurrentCell is Cell deathCell
                            && deathCell.ParentZone is Zone deathZone)
                        {
                            DeathAddress = new(deathZone.ZoneID, deathCell.Location);
                        }

                        Physics bIAJ_Physics = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.Physics);
                        BrainInAJar.Physics = bIAJ_Physics;

                        Brain bIAJ_Brain = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.Brain);
                        BrainInAJar.Brain = bIAJ_Brain;
                        try
                        {
                            foreach ((int flags, PartyMember partyMember) in bIAJ_Brain.PartyMembers)
                            {
                                PartyMember partyMemberCopy = new(partyMember.Reference, partyMember.Flags);
                                Brain.PartyMembers.TryAdd(flags, partyMemberCopy);
                            }
                        }
                        catch (Exception x)
                        {
                            MetricsManager.LogException(Name + "." + nameof(Initialize), x, "game_mod_exception");
                            Brain.PartyMembers = new();
                        }
                        try
                        {
                            foreach ((int key, OpinionList opinionList) in bIAJ_Brain.Opinions)
                            {
                                OpinionList opinionsCopy = new();
                                foreach (IOpinion opinionCopy in opinionList)
                                {
                                    opinionsCopy.Add(opinionCopy);
                                }
                                Brain.Opinions.TryAdd(key, opinionsCopy);
                            }
                        }
                        catch (Exception x)
                        {
                            MetricsManager.LogException(Name + "." + nameof(Initialize), x, "game_mod_exception");
                            Brain.Opinions = new();
                        }

                        Body bIAJ_Body = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.Body);
                        BrainInAJar.Body = bIAJ_Body;

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

                        if (!PastLife.GenderName.IsNullOrEmpty())
                        {
                            BrainInAJar.SetGender(PastLife.GenderName);
                        }

                        if (!PastLife.PronounSetName.IsNullOrEmpty())
                        {
                            BrainInAJar.SetPronounSet(PastLife.PronounSetName);
                        }

                        ConversationScript bIAJ_Conversation = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<ConversationScript>());

                        if (PastLife.Statistics.IsNullOrEmpty())
                        {
                            BrainInAJar.Statistics = new();
                            foreach ((string statName, Statistic stat) in PastLife.Statistics)
                            {
                                Statistic newStat = new(stat);
                                if (statName == "Hitpoints")
                                {
                                    newStat.Penalty = 0;
                                }
                                newStat.Owner = BrainInAJar;
                                BrainInAJar.Statistics.Add(statName, newStat);
                            }
                        }

                        BrainInAJar.SetSpecies(PastLife.GetSpecies());
                        BrainInAJar.SetGenotype(PastLife.GetGenotype());
                        BrainInAJar.SetSubtype(PastLife.GetSubtype());

                        Mutations bIAJ_Mutations = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Mutations>());
                        Skills bIAJ_Skills = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.GetPart<Skills>());
                        foreach (BaseSkill baseSkill in PastLife.GetPartsDescendedFrom<BaseSkill>())
                        {
                            // There's a bug in v1.04 with how Skills serializes its BaseSkills
                            // that means the only way to guarantee copying them is via the parts list.
                            if (!bIAJ_Skills.SkillList.Contains(baseSkill))
                            {
                                bIAJ_Skills.AddSkill(baseSkill);
                            }
                        }
                        /*
                        if (PastLife?.GetInstalledCyberneticsReadonly() is List<GameObject> pastInstalledCybernetics)
                        {
                            InstalledCybernetics = new();
                            foreach (GameObject pastInstalledCybernetic in pastInstalledCybernetics)
                            {
                                if (pastInstalledCybernetic.ID is string cyberneticID
                                    && PastLife?.Body?.FindCybernetics(pastInstalledCybernetic)?.Type is string implantedLimb)
                                {
                                    InstalledCybernetics.Add(new(cyberneticID, implantedLimb));
                                }
                            }
                        }
                        */

                        foreach (Effect pastEffect in PastLife.Effects)
                        {
                            BrainInAJar.Effects.Add(pastEffect.DeepCopy(BrainInAJar, null));
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
                }
            }
        }

        public void Initialize(UD_FleshGolems_PastLife PrevPastLife)
        {
            if (!Init && PrevPastLife != null && PrevPastLife.Init)
            {
                Initialize(PrevPastLife.BrainInAJar);
                TimesReanimated++;
            }
            else
            if (PrevPastLife?.ParentObject is GameObject pastLife)
            {
                Initialize(pastLife);
            }
        }

        public override void Attach()
        {
            base.Attach();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public virtual void DebugOutput()
        {
            void debugLog(string Field, object Value = null, int Indent = 0)
            {
                string indent = " ".ThisManyTimes(Math.Min(12, Indent) * 4);
                string output = indent + Field;
                if (Value != null &&
                    !Value.ToString().IsNullOrEmpty())
                {
                    output += ": " + Value;
                }
                UnityEngine.Debug.Log(output);
            }
            try
            {
                debugLog(nameof(Init), Init);
                debugLog(nameof(WasCorpse), WasCorpse);
                debugLog(nameof(WasPlayer), WasPlayer);

                debugLog(nameof(TimesReanimated), TimesReanimated);

                debugLog(nameof(Blueprint), Blueprint);
                debugLog(nameof(BaseDisplayName), BaseDisplayName);

                debugLog(nameof(Titles), Titles);
                debugLog(nameof(Epithets), Epithets);
                debugLog(nameof(Honorifics), Honorifics);

                debugLog(nameof(PastRender), PastRender);
                debugLog(nameof(Description), Description);

                debugLog(nameof(DeathAddress), DeathAddress);

                debugLog(nameof(Brain), Brain != null);
                debugLog(nameof(Brain.Allegiance), null, 1);
                foreach ((string faction, int rep) in Brain?.Allegiance ?? new())
                {
                    debugLog(faction, rep, 2);
                }
                if (Brain != null)
                {
                    debugLog("bools", null, 1);
                    Traverse brainWalk = new(Brain);
                    foreach (string field in brainWalk.Fields() ?? new())
                    {
                        string fieldValue = brainWalk?.Field(field)?.GetValue()?.ToString();
                        debugLog(field, fieldValue ?? "??", 2);
                    }
                }
                debugLog(nameof(Gender), Gender);
                debugLog(nameof(PronounSet), PronounSet);
                debugLog(nameof(ConversationScriptID), ConversationScriptID);

                debugLog(nameof(Stats), Stats?.Count);
                foreach ((string statName, Statistic stat) in Stats ?? new())
                {
                    debugLog(statName, stat.BaseValue, 1);
                }
                debugLog(nameof(Species), Species);
                debugLog(nameof(Genotype), Genotype);
                debugLog(nameof(Subtype), Subtype);

                debugLog(nameof(Mutations), Mutations?.ActiveMutationList.Count);
                foreach (BaseMutation mutation in Mutations?.ActiveMutationList)
                {
                    debugLog(mutation.Name, mutation.BaseLevel, 1);
                }
                debugLog(nameof(Skills), Skills?.SkillList.Count);
                foreach (BaseSkill baseSkill in Skills?.SkillList)
                {
                    debugLog(baseSkill.Name, null, 1);
                }
                debugLog(nameof(InstalledCybernetics), new List<UD_FleshGolems_InstalledCybernetic>(InstalledCybernetics)?.Count);
                foreach ((GameObject cybernetic, string implantedLimb) in InstalledCybernetics)
                {
                    debugLog(implantedLimb, cybernetic.Blueprint, 1);
                }

                debugLog(nameof(Tags), Tags?.Count);
                foreach ((string name, string value) in Tags ?? new())
                {
                    debugLog(name, value, 1);
                }
                debugLog(nameof(StringProperties), StringProperties?.Count);
                foreach ((string name, string value) in StringProperties ?? new())
                {
                    debugLog(name, value, 1);
                }
                debugLog(nameof(IntProperties), IntProperties?.Count);
                foreach ((string name, int value) in IntProperties ?? new())
                {
                    debugLog(name, value, 1);
                }

                debugLog(nameof(Effects), Effects?.Count);
                foreach (Effect Effect in Effects)
                {
                    debugLog(Effect.ClassName + ",  duration" , Effect.Duration, 1);
                }
            }
            catch (Exception x)
            {
                MetricsManager.LogException(Name + "." + nameof(DebugOutput), x, "game_mod_exception");
            }
        }

        public static bool RestoreBrainFromPastLife(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            bool ExcludedFromDynamicEncounters,
            out Brain FrankenBrain)
        {
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

        public static bool RestoreCyberneticsFromPastLife(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            out bool WereCyberneticsInstalled)
        {
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
                            FrankenCorpse.ModIntProperty(UD_FleshGolems_CorpseReanimationHelper.CYBERNETICS_LICENSES, cyberneticsCost);
                            FrankenCorpse.ModIntProperty(UD_FleshGolems_CorpseReanimationHelper.CYBERNETICS_LICENSES_FREE, cyberneticsCost);

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

        [WishCommand("UD_FleshGolems debug PastLife")]
        public static void Debug_PastLife_WishHandler()
        {
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
        }
    }
}
