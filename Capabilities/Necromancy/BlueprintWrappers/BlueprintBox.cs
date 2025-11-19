using System;

using SerializeField = UnityEngine.SerializeField;

using XRL.World;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public abstract class BlueprintBox : IComposite
    {
        [SerializeField]
        private readonly string Name;

        public BlueprintBox()
        {
            Name = null;
        }
        public BlueprintBox(string Blueprint) : this() => Name = Blueprint;
        public BlueprintBox(GameObjectBlueprint Blueprint) : this(Blueprint.Name) { }
        public BlueprintBox(BlueprintBox Source) : this(Source.Name) { }

        public GameObjectBlueprint GetGameObjectBlueprint()
        {
            return Name.GetGameObjectBlueprint();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}