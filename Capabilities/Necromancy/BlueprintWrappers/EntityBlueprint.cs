using System;

using XRL.World;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public class EntityBlueprint : BlueprintBox, IComposite
    {
        public EntityBlueprint() : base() { }
        public EntityBlueprint(string Blueprint) : base(Blueprint) { }
        public EntityBlueprint(GameObjectBlueprint Blueprint) : base(Blueprint) { }
        public EntityBlueprint(BlueprintBox Source) : base(Source.ToString()) { }
        public EntityBlueprint(EntityBlueprint Source) : base(Source) { }


    }
}
