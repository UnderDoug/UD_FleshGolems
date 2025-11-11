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
        }

        public GameObject BrainInAJar;

        public bool Init { get; protected set; }
        public bool WasCorpse => (GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint)?.InheritsFrom("Corpse")).GetValueOrDefault();

        public bool WasPlayer => Blueprint.IsPlayerBlueprint() || BrainInAJar.HasPropertyOrTag("UD_FleshGolems_WasPlayer");

        public int TimesReanimated;

        public string Blueprint;

        public string BaseDisplayName => BrainInAJar?.BaseDisplayName;
        public Render PastRender => BrainInAJar?.Render;

        public string Description => BrainInAJar?.GetPart<Description>()?._Short;

        public UD_FleshGolems_DeathAddress DeathAddress;

        public Brain Brain => BrainInAJar?.Brain;

        [NonSerialized]
        public string GenderName;
        [NonSerialized]
        public Gender Gender;

        [NonSerialized]
        public string PronounSetName;
        [NonSerialized]
        public PronounSet PronounSet;

        public string ConversationScriptID;

        [NonSerialized]
        public Dictionary<string, Statistic> Stats;
        public string Species;
        public string Genotype;
        public string Subtype;

        public Dictionary<string, int> MutationLevels;
        public List<string> Skills;
        public List<KeyValuePair<string, string>> InstalledCybernetics;

        public Dictionary<string, string> Tags;
        public Dictionary<string, string> _Property;
        public Dictionary<string, int> _IntProperty;

        public List<KeyValuePair<string, int>> Effects;

        public Titles Titles;
        public Epithets Epithets;
        public Honorifics Honorifics;

        public UD_FleshGolems_PastLife()
        {
            BrainInAJar = GetBrainInAJar();
            Init = false;

            TimesReanimated = 0;

            Blueprint = null;

            DeathAddress = null;

            GenderName = null;
            Gender = null;
            PronounSetName = null;
            PronounSet = null;
            ConversationScriptID = null;

            Stats = null;
            Species = null;
            Genotype = null;
            Subtype = null;

            MutationLevels = null;
            Skills = null;
            InstalledCybernetics = null;

            Tags = null;
            _Property = null;
            _IntProperty = null;

            Effects = null;

            Titles = null;
            Epithets = null;
            Honorifics = null;
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

        private static GameObject GetBrainInAJar()
        {
            return GameObjectFactory.Factory.CreateUnmodifiedObject("UD_FleshGolems Brain In A Jar Widget");
        }

        public void Initialize(GameObject PastLife)
        {
            if (!Init)
            {
                try
                {
                    BrainInAJar ??= GetBrainInAJar();
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

                        Body bIAJ_Body = BrainInAJar.OverrideWithDeepCopyOrRequirePart(PastLife.Body);
                        BrainInAJar.Body = bIAJ_Body;

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
                        BrainInAJar._Property = PastLife._Property;
                        BrainInAJar._IntProperty = PastLife._IntProperty;

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
                debugLog(nameof(BaseDisplayName), BaseDisplayName);
                debugLog(nameof(Blueprint), Blueprint);

                debugLog(nameof(Init), Init);

                debugLog(nameof(TimesReanimated), TimesReanimated);

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

                debugLog(nameof(MutationLevels), MutationLevels?.Count);
                foreach ((string mutation, int level) in MutationLevels ?? new())
                {
                    debugLog(mutation, level, 1);
                }
                debugLog(nameof(Skills), Skills?.Count);
                foreach (string skill in Skills ?? new())
                {
                    debugLog(skill, null, 1);
                }
                debugLog(nameof(InstalledCybernetics), InstalledCybernetics?.Count);
                foreach ((string blueprint, string limb) in InstalledCybernetics ?? new())
                {
                    debugLog(blueprint, limb, 1);
                }

                debugLog(nameof(Tags), Tags?.Count);
                foreach ((string name, string value) in Tags ?? new())
                {
                    debugLog(name, value, 1);
                }
                debugLog(nameof(_Property), _Property?.Count);
                foreach ((string name, string value) in _Property ?? new())
                {
                    debugLog(name, value, 1);
                }
                debugLog(nameof(_IntProperty), _IntProperty?.Count);
                foreach ((string name, int value) in _IntProperty ?? new())
                {
                    debugLog(name, value, 1);
                }

                debugLog(nameof(Effects), Effects?.Count);
                foreach ((string effectName, int effectDuration) in Effects ?? new())
                {
                    debugLog(effectName + ",  duration" , effectDuration, 1);
                }

                debugLog(nameof(Titles), Titles);
                debugLog(nameof(Epithets), Epithets);
                debugLog(nameof(Honorifics), Honorifics);
            }
            catch (Exception x)
            {
                MetricsManager.LogException(Name + "." + nameof(DebugOutput), x, "game_mod_exception");
            }
        }

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            base.Write(Basis, Writer);

            Writer.WriteOptimized(Gender?.Name);
            Writer.WriteOptimized(PronounSet?.Name);

            Writer.Write(Stats.Count);
            foreach ((string _, Statistic stat) in Stats)
            {
                stat.Save(Writer);
            }
        }
        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            base.Read(Basis, Reader);

            GenderName = Reader.ReadOptimizedString();

            PronounSetName = Reader.ReadOptimizedString();

            int statCount = Reader.ReadInt32();
            Stats = new(statCount);
            for (int i = 0; i < statCount; i++)
            {
                Statistic statistic = Statistic.Load(Reader, Basis);
                Stats.TryAdd(statistic.Name, statistic);
            }
        }
        public override void FinalizeRead(SerializationReader Reader)
        {
            base.FinalizeRead(Reader);
            Gender = new(GenderName);
            PronounSet = PronounSet.Get(PronounSetName);
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
