using System;

using XRL.World;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public partial class CorpseWeight : BlueprintWeight<CorpseBlueprint>, IComposite
    {
        public CorpseWeight(CorpseBlueprint Corpse, int Weight) : base(Corpse, Weight) { }
    }
}
