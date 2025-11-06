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

namespace XRL.World.Parts
{
    [HasWishCommand]
    [Serializable]
    public class UD_FleshGolems_PastLife : IScribedPart
    {
        public bool Init { get; protected set; }
        public bool IsCorpse => (GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint)?.InheritsFrom("Corpse")).GetValueOrDefault();

        public int TimesReanimated;

        public string Blueprint;
        public string BaseDisplayName;
        public Render PastRender;
        public string Description;

        public (string DeathZone, Location2D DeathLocation) DeathAddress;

        [NonSerialized]
        public Brain Brain;

        [NonSerialized]
        public Gender Gender;

        [NonSerialized]
        public PronounSet PronounSet;

        public ConversationScript ConversationScript;

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

        public EffectRack _Effects;

        public Titles Titles;
        public Epithets Epithets;
        public Honorifics Honorifics;

        public UD_FleshGolems_PastLife()
        {
            Init = false;

            TimesReanimated = 0;

            Blueprint = null;
            BaseDisplayName = null;
            PastRender = null;
            Description = null;

            DeathAddress = (null, null);

            Brain = null;
            Gender = null;
            PronounSet = null;
            ConversationScript = null;

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

            _Effects = null;

            Titles = null;
            Epithets = null;
            Honorifics = null;
        }

        public void Initialize(GameObject PastLife)
        {
            if (!Init)
            {
                Blueprint = PastLife?.Blueprint;
                if (GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint).InheritsFrom("Corpse"))
                {
                    TimesReanimated = 1;
                }
                BaseDisplayName = PastLife?.BaseDisplayName;
                PastRender = PastLife?.Render?.DeepCopy(ParentObject) as Render;
                Description = PastLife?.GetPart<Description>()?._Short;

                DeathAddress = (PastLife?.CurrentZone?.ZoneID, PastLife?.CurrentCell?.Location);

                if (PastLife?.Brain is Brain pastBrain)
                {
                    Brain = pastBrain.DeepCopy(ParentObject) as Brain;
                    if (Brain != null)
                    {
                        foreach ((int flags, PartyMember partyMember) in pastBrain.PartyMembers)
                        {
                            Brain.PartyMembers.TryAdd(flags, partyMember);
                        }
                        foreach ((int key, OpinionList opinionList) in pastBrain.Opinions)
                        {
                            Brain.Opinions.TryAdd(key, opinionList);
                        }
                    }
                }
                Gender = new(PastLife?.GetGender(AsIfKnown: true));
                if (PastLife?.GetGender(AsIfKnown: true) is Gender pastGender)
                {
                    Gender = new(pastGender);
                }
                if (PastLife?.GetPronounSet() is PronounSet pastPronouns)
                {
                    PronounSet = new(pastPronouns);
                }
                ConversationScript = PastLife?.GetPart<ConversationScript>();

                Stats = new(PastLife?.Statistics);
                Species = PastLife?.GetSpecies();
                Genotype = PastLife?.GetGenotype();
                Subtype = PastLife?.GetSubtype();

                if (PastLife?.GetPart<Mutations>() is Mutations pastMutations)
                {
                    MutationLevels = new();
                    foreach (BaseMutation baseMutation in pastMutations.MutationList)
                    {
                        MutationLevels.TryAdd(baseMutation.GetMutationEntry().Name, baseMutation.BaseLevel);
                    }
                }
                if (PastLife?.GetPart<Skills>() is Skills pastSkills)
                {
                    Skills = new();
                    foreach (BaseSkill baseSkill in pastSkills.SkillList)
                    {
                        Skills.Add(baseSkill.Name);
                    }
                }
                if (PastLife?.GetInstalledCyberneticsReadonly() is List<GameObject> pastInstalledCybernetics)
                {
                    foreach (GameObject pastInstalledCybernetic in pastInstalledCybernetics)
                    {
                        if (pastInstalledCybernetic.ID is string cyberneticID
                            && PastLife?.Body?.FindCybernetics(pastInstalledCybernetic)?.Type is string implantedLimb)
                        {
                            InstalledCybernetics.Add(new(cyberneticID, implantedLimb));
                        }
                    }
                }

                Tags = new(PastLife?.GetBlueprint()?.Tags);
                _Property = new(PastLife?._Property);
                _IntProperty = new(PastLife?._IntProperty);

                if (PastLife != null && !PastLife._Effects.IsNullOrEmpty())
                {
                    _Effects = new();
                    foreach (Effect pastEffect in PastLife._Effects)
                    {
                        _Effects.Add(pastEffect.DeepCopy(ParentObject));
                    }
                }

                Titles = PastLife?.GetPart<Titles>()?.DeepCopy(ParentObject) as Titles;
                Epithets = PastLife?.GetPart<Epithets>()?.DeepCopy(ParentObject) as Epithets;
                Honorifics = PastLife?.GetPart<Honorifics>()?.DeepCopy(ParentObject) as Honorifics;
                Init = true;
            }
        }

        public void Initialize(UD_FleshGolems_PastLife PrevPastLife)
        {
            if (!Init && PrevPastLife != null && PrevPastLife.Init)
            {
                TimesReanimated = PrevPastLife.TimesReanimated;

                Blueprint = PrevPastLife.Blueprint;
                BaseDisplayName = PrevPastLife.BaseDisplayName;
                PastRender = PrevPastLife.PastRender;
                Description = PrevPastLife.Description;

                DeathAddress = PrevPastLife.DeathAddress;

                Brain = PrevPastLife.Brain;
                Gender = PrevPastLife.Gender;
                PronounSet = PrevPastLife.PronounSet;
                ConversationScript = PrevPastLife.ConversationScript;

                Stats = PrevPastLife.Stats;
                Species = PrevPastLife.Species;
                Genotype = PrevPastLife.Genotype;
                Subtype = PrevPastLife.Subtype;

                MutationLevels = PrevPastLife.MutationLevels;
                Skills = PrevPastLife.Skills;
                InstalledCybernetics = PrevPastLife.InstalledCybernetics;

                Tags = PrevPastLife.Tags;
                _Property = PrevPastLife._Property;
                _IntProperty = PrevPastLife._IntProperty;

                _Effects = PrevPastLife._Effects;

                Titles = PrevPastLife.Titles;
                Epithets = PrevPastLife.Epithets;
                Honorifics = PrevPastLife.Honorifics;

                Init = true;
            }
            else
            if (PrevPastLife.ParentObject is GameObject pastLife)
            {
                Initialize(pastLife);
            }
        }

        public override void Attach()
        {
            if (Init && GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint).InheritsFrom("Corpse"))
            {
                TimesReanimated++;
            }
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
                    Traverse branWalk = new(Brain);
                    foreach (string field in branWalk.Fields() ?? new())
                    {
                        string fieldValue = branWalk.Field(field).GetValue().ToString();
                        debugLog(field, fieldValue ?? "??", 2);
                    }
                }
                debugLog(nameof(Gender), Gender);
                debugLog(nameof(PronounSet), PronounSet);
                debugLog(nameof(ConversationScript), ConversationScript?.ConversationID);

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

                debugLog(nameof(_Effects), _Effects?.Count);
                foreach (Effect effect in _Effects ?? new())
                {
                    debugLog(effect.ClassName, effect.Duration, 1);
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

            Writer.WriteOptimized(DeathAddress.DeathZone);
            Writer.Write(DeathAddress.DeathLocation);

            Brain?.Write(Basis, Writer);

            Gender?.Save(Writer);

            PronounSet?.Save(Writer);

            Writer.WriteOptimized(Stats.Count);
            foreach ((string statName, Statistic stat) in Stats)
            {
                Writer.WriteOptimized(statName);
                stat.Save(Writer);
            }
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            base.Read(Basis, Reader);

            DeathAddress = new(Reader.ReadOptimizedString(), Reader.ReadLocation2D());

            Brain ??= new Brain();
            Brain.Read(Basis, Reader);

            Gender = Gender.Load(Reader);

            PronounSet = PronounSet.Load(Reader);

            int statCount = Reader.ReadOptimizedInt32();
            Stats = new(statCount);
            for (int i = 0; i < statCount; i++)
            {
                Stats.TryAdd(Reader.ReadOptimizedString(), Statistic.Load(Reader, Basis));
            }
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
            }
            else
            {
                Popup.Show("nothing selected to debug " + nameof(UD_FleshGolems_PastLife));
            }
        }
    }
}
