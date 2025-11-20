using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using XRL.World;

using static UD_FleshGolems.Const;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public class CorpseProduct : CorpseBlueprint, IComposite
    {
        private CorpseProduct() : base() { }
        public CorpseProduct(string Blueprint) : base(Blueprint) { }
        public CorpseProduct(CorpseBlueprint CorpseBlueprint) : base(CorpseBlueprint) { }
    }
}
