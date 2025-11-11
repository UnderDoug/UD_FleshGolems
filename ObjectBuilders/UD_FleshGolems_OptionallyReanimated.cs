
using System;
using System.Collections.Generic;

using XRL.World.Parts;

using static UD_FleshGolems.Options;

namespace XRL.World.ObjectBuilders
{
    [Serializable]
    public class UD_FleshGolems_OptionallyReanimated : IObjectBuilder
    {
        public static int SpecialChanceOneIn => SpecialReanimatedBuilderChanceOneIn;

        public static List<string> ContextsToIgnore => new()
        {
            nameof(UD_FleshGolems_PastLife),
            "Sample",
            "Wish",
        };

        public override void Apply(GameObject Object, string Context)
        {
            if (SpecialChanceOneIn > 0 && 1.ChanceIn(SpecialChanceOneIn) && !ContextsToIgnore.Contains(Context))
            {
                if (UD_FleshGolems_Reanimated.HasWorldGenerated)
                {
                    Object.SetStringProperty("UD_FleshGolems_Reanimator", "UD_FleshGolems Mad Monger");
                }
                UD_FleshGolems_Reanimated.Unkill(Object, Context);
            }
        }
    }
}
