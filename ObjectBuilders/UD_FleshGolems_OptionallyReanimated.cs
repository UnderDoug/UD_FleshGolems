using System;
using System.Collections.Generic;

using XRL.World.Parts;

using UD_FleshGolems;

using static UD_FleshGolems.Options;

namespace XRL.World.ObjectBuilders
{
    [Serializable]
    public class UD_FleshGolems_OptionallyReanimated : IObjectBuilder
    {
        public const string REANIMATED_BYBUILDER = "UD_FleshGolems Reanimated ByBuilder";
        public static int SpecialChanceOneIn => SpecialReanimatedBuilderChanceOneIn;

        public static List<string> ContextsToIgnore => new()
        {
            nameof(UD_FleshGolems_PastLife),
            nameof(UD_FleshGolems_ReplaceCorpse),
            "Sample",
            "Wish",
        };

        public static List<string> BlueprintsToIgnore => new()
        {
            "Mechanimist Convert Librarian",
        };

        public override void Apply(GameObject Object, string Context)
        {
            if (SpecialChanceOneIn > 0 
                && 1.ChanceIn(SpecialChanceOneIn) 
                && !ContextsToIgnore.Contains(Context)
                && !BlueprintsToIgnore.Contains(Object.Blueprint)
                && !Object.IsInanimateCorpse()
                && !Object.HasTagOrProperty("UD_FleshGolems OptionallyReanimated Excluded"))
            {
                if (UD_FleshGolems_Reanimated.HasWorldGenerated)
                    Object.SetStringProperty("UD_FleshGolems_Reanimator", "UD_FleshGolems Mad Monger");

                Object.SetStringProperty(REANIMATED_BYBUILDER, "Yep! I was built reanimated.");
                Object.RequireAbilities();
                UD_FleshGolems_Reanimated.Unkill(Object, Context);
            }
        }
    }
}
