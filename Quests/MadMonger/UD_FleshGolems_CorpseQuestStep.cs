using System;

using static XRL.World.QuestManagers.UD_FleshGolems_CorpseQuestSystem;

using SerializeField = UnityEngine.SerializeField;

using UD_FleshGolems;
using System.Linq;

namespace XRL.World.QuestManagers
{
    [Serializable]
    public class UD_FleshGolems_CorpseQuestStep : IComposite
    {
        [Serializable]
        public class CorpseItem : IComposite
        {
            public enum CorpseTaxonomy : int
            {
                Empty = -1,
                Any = 0,
                Species = 1,
                Base = 2,
                Faction = 3,
            }

            public CorpseTaxonomy Taxonomy;
            public string Value;

            public bool IsAny => Taxonomy == CorpseTaxonomy.Any;
            public bool IsSpecies => Taxonomy == CorpseTaxonomy.Species;
            public bool IsBase => Taxonomy == CorpseTaxonomy.Base;
            public bool IsFaction => Taxonomy == CorpseTaxonomy.Faction;

            private CorpseItem()
            {
                Taxonomy = CorpseTaxonomy.Empty;
                Value = null;
            }

            public CorpseItem(CorpseTaxonomy Taxonomy, string Value)
                : this()
            {
                this.Taxonomy = Taxonomy;
                this.Value = Value;
            }

            public bool CorpseCompletesThisStep(GameObject CorpseObject) => Taxonomy switch
            {
                CorpseTaxonomy.Any =>
                    IsCorpse(CorpseObject),

                CorpseTaxonomy.Species =>
                    IsCorpse(CorpseObject)
                    && GetAllCorpsesOfSpecies(Value).Contains(CorpseObject.Blueprint),

                CorpseTaxonomy.Base =>
                    IsCorpse(CorpseObject)
                    && CorpseObject.GetBlueprint().InheritsFrom(Value),

                CorpseTaxonomy.Faction =>
                    IsCorpse(CorpseObject)
                    && GetAllCorpsesOfFaction(Value).Contains(CorpseObject.Blueprint),

                _ => false,
            };

            public string GetACorpseForThisStep() => Taxonomy switch
            {
                CorpseTaxonomy.Any =>
                    GameObjectFactory.Factory
                    ?.GetBlueprintsInheritingFrom("Corpse")
                    ?.GetRandomElementCosmetic(bp => !bp.IsBaseBlueprint()).Name,

                CorpseTaxonomy.Species =>
                    GetAllCorpsesOfSpecies(Value)
                    ?.GetRandomElementCosmetic(),

                CorpseTaxonomy.Base =>
                    GetAllCorpsesOfBase(Value)
                    ?.GetRandomElementCosmetic(),

                CorpseTaxonomy.Faction =>
                    GetAllCorpsesOfFaction(Value)
                    ?.GetRandomElementCosmetic(),

                _ => null,
            };
        }

        private UD_FleshGolems_CorpseQuestSystem ParentSystem;

        public string Name;

        public string Text;

        public CorpseItem Corpse;

        public string Item;

        public bool Finished;

        private UD_FleshGolems_CorpseQuestStep()
        {
            Name = null;
            Text = null;
            Corpse = null;
            Item = null;
            Finished = false;
        }

        public UD_FleshGolems_CorpseQuestStep(UD_FleshGolems_CorpseQuestSystem ParentSystem)
            : this()
        {
            this.ParentSystem = ParentSystem;
        }

        public bool FinishStep()
        {
            if (!Finished)
            {
                Finished = true;
                if (!Name.IsNullOrEmpty() 
                    && ParentSystem?.Quest?.StepsByID[Name] is QuestStep thisStep)
                {
                    thisStep.Finished = true;
                }
                return true;
            }
            return false;
        }
        public bool UnfinishStep()
        {
            if (Finished)
            {
                Finished = false;
                if (!Name.IsNullOrEmpty()
                    && ParentSystem?.Quest?.StepsByID[Name] is QuestStep thisStep)
                {
                    thisStep.Finished = false;
                }
                return true;
            }
            return false;
        }

        public void Write(SerializationWriter Writer)
        {
            Writer.WriteObject(ParentSystem);
        }

        public void Read(SerializationReader Reader)
        {
            ParentSystem = Reader.ReadObject() as UD_FleshGolems_CorpseQuestSystem;
        }
    }
}