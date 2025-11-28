using System;
using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using XRL.World;

using static UD_FleshGolems.Capabilities.UD_FleshGolems_NecromancySystem;
using static UD_FleshGolems.Utils;

namespace UD_FleshGolems.Capabilities.Necromancy
{
    [Serializable]
    public struct CorpseCountsAs : IComposite
    {
        public CorpseBlueprint Blueprint;
        public CountsAs CountasAs;
        public List<string> Paramaters;

        public CorpseCountsAs(CorpseBlueprint Blueprint, CountsAs CountasAs, List<string> Paramaters)
        {
            this.Blueprint = Blueprint;
            this.CountasAs = CountasAs;
            this.Paramaters = Paramaters;
        }

        public readonly void Deconstruct(out CorpseBlueprint Blueprint, out CountsAs CountasAs, out List<string> Paramaters)
        {
            Blueprint = this.Blueprint;
            CountasAs = this.CountasAs;
            Paramaters = this.Paramaters;
        }

        public override string ToString()
            => Blueprint.ToString() + " (" + CountasAs + ")::" + Paramaters.SafeJoin(Delimiter:":");

        public static CorpseCountsAs GetCorpseCountsAs(CorpseBlueprint CorpseBlueprint, string PropTag)
        {
            CorpseCountsAs corpseCountsAs = new()
            {
                Blueprint = CorpseBlueprint,
                Paramaters = new(),
                CountasAs = CountsAs.None,
            };
            if (PropTag.EqualsNoCase("any") || PropTag.Equals("*"))
            {
                corpseCountsAs.CountasAs = CountsAs.Any;
            }
            else
            if (PropTag.Contains(":"))
            {
                corpseCountsAs.Paramaters = PropTag.Split(":").ToList();
                corpseCountsAs.CountasAs = corpseCountsAs.Paramaters[0] switch
                {
                    "any" => CountsAs.Any,
                    "*" => CountsAs.Any,
                    "Keyword" => CountsAs.Keyword,
                    "Blueprint" => CountsAs.Blueprint,
                    "Population" => CountsAs.Population,
                    "Faction" => CountsAs.Faction,
                    "Species" => CountsAs.Species,
                    "Genotype" => CountsAs.Genotype,
                    "Subtype" => CountsAs.Subtype,
                    "OtherCorpse" => CountsAs.OtherCorpse,
                    _ => CountsAs.None,
                };
            }
            return corpseCountsAs;
        }
    }
}
