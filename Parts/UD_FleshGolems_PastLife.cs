using System;
using System.Collections.Generic;
using System.Text;

using XRL.Collections;
using XRL.World;
using XRL.World.AI;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_PastLife : IScribedPart
    {
        public string Blueprint;
        public string BaseDisplayName;
        public Render PastRender;
        public string Description;

        public Brain Brain;
        public Gender Gender;
        public PronounSet PronounSet;
        public ConversationScript ConversationScript;

        public Dictionary<string, Statistic> Stats;
        public string Species;
        public string Genotype;
        public string Subtype;

        public Dictionary<string, int> MutationLevels;
        public List<string> Skills;

        public Dictionary<string, string> Tags;
        public Dictionary<string, string> _Property;
        public Dictionary<string, int> _IntProperty;

        public EffectRack _Effects;

        public Titles Titles;
        public Epithets Epithets;
        public Honorifics Honorifics;

        public UD_FleshGolems_PastLife()
        {
            Blueprint = null;
            BaseDisplayName = null;
            PastRender = null;
            Description = null;

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
            Blueprint = PastLife?.Blueprint;
            BaseDisplayName = PastLife?.BaseDisplayName;
            PastRender = PastLife?.Render?.DeepCopy(ParentObject) as Render;
            Description = PastLife?.GetPart<Description>()?._Short;

            if (PastLife?.Brain is Brain pastBrain)
            {
                Brain = pastBrain.DeepCopy(ParentObject) as Brain;
                if (Brain != null)
                {
                    foreach ((int flags, PartyMember partyMember) in pastBrain.PartyMembers)
                    {
                        Brain.PartyMembers.Add(flags, partyMember);
                    }
                    foreach ((int key, OpinionList opinionList) in pastBrain.Opinions)
                    {
                        Brain.Opinions.Add(key, opinionList);
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
                    MutationLevels.Add(baseMutation.GetMutationEntry().Name, baseMutation.BaseLevel);
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
        }
    }
}
