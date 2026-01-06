using System;
using System.Collections.Generic;
using System.Text;

using ConsoleLib.Console;

using XRL.UI;
using XRL.World.AI;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class UD_FleshGolems_NecromanticAura : UD_FleshGolems_NanoNecroAnimation
    {
        public override string GetMutationType() => "Mental";
    }
}
