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
            public string Type;
            public string Value;

            public bool IsSpecies => !Type.IsNullOrEmpty() && Type == "Species";
            public bool IsBase => !Type.IsNullOrEmpty() && Type == "Base";
            public bool IsFaction => !Type.IsNullOrEmpty() && Type == "Faction";
            public bool IsAny => !Type.IsNullOrEmpty() && Type == "Any";

            private CorpseItem()
            {
                Type = null;
                Value = null;
            }

            public CorpseItem(string Type, string Value)
                : this()
            {
                this.Type = Type;
                this.Value = Value;
            }

            public bool CorpseCompletesThisStep(GameObject CorpseObject)
            {
                if (IsSpecies
                    && IsCorpse(CorpseObject)
                    && GetAllCorpsesOfSpecies(Value).Contains(CorpseObject.Blueprint))
                {
                    return true;
                }
                if (IsBase
                    && IsCorpse(CorpseObject)
                    && CorpseObject.GetBlueprint().InheritsFrom(Value))
                {
                    return true;
                }
                if (IsFaction
                    && IsCorpse(CorpseObject)
                    && GetAllCorpsesOfFaction(Value).Contains(CorpseObject.Blueprint))
                {
                    return true;
                }
                if (IsAny
                    && IsCorpse(CorpseObject))
                {
                    return true;
                }
                return false;
            }
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

        public void FinishStep()
        {
            Finished = true;
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