using System;
using System.Collections.Generic;

using XRL.World;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public class CorpseProduct : CorpseBlueprint, IComposite
    {
        public List<CorpseBlueprint> CorpseBlueprints;

        private CorpseProduct() : base() { CorpseBlueprints = new(); }

        public CorpseProduct(string Blueprint) : base(Blueprint) { CorpseBlueprints = new(); }
        public CorpseProduct(CorpseBlueprint CorpseBlueprint) : base(CorpseBlueprint) { CorpseBlueprints = new(); }
        public CorpseProduct(string Blueprint, List<CorpseBlueprint> CorpseBlueprints) : base(Blueprint) { this.CorpseBlueprints = CorpseBlueprints; }
        public CorpseProduct(CorpseBlueprint CorpseBlueprint, List<CorpseBlueprint> CorpseBlueprints) : base(CorpseBlueprint) { this.CorpseBlueprints = CorpseBlueprints; }

        public void Deconstruct(out CorpseBlueprint CorpseBlueprint, out List<CorpseBlueprint> CorpseBlueprints)
        {
            CorpseBlueprint = this;
            CorpseBlueprints = this.CorpseBlueprints;
        }
    }
}
