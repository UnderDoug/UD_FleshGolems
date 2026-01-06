using System;

using XRL.World;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public class CorpseBlueprint : BlueprintBox, IComposite
    {
        public CorpseBlueprint() : base() { }
        public CorpseBlueprint(string Blueprint) : base(Blueprint) { }
        public CorpseBlueprint(GameObjectBlueprint Blueprint) : base(Blueprint) { }
        public CorpseBlueprint(BlueprintBox Source) : base(Source.ToString()) { }
        public CorpseBlueprint(CorpseBlueprint Source) : base(Source) { }
    }
}
