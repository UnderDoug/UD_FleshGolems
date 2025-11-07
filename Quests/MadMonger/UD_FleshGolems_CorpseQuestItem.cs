using System;

using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.QuestManagers
{
    [Serializable]
    public class UD_FleshGolems_CorpseQuestItem : IComposite
    {
        public string Name;

        public string Text;

        [NonSerialized]
        public (string Type, string Value) Corpse;

        public bool Finished;

        public void Write(SerializationWriter Writer)
        {
            Writer.WriteOptimized(Corpse.Type);
            Writer.WriteOptimized(Corpse.Value);
        }

        public void Read(SerializationReader Reader)
        {
            Corpse = new()
            {
                Type = Reader.ReadOptimizedString(),
                Value = Reader.ReadOptimizedString(),
            };
        }
    }
}