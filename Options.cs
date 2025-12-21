using XRL;

namespace UD_FleshGolems
{
    [HasModSensitiveStaticCache]
    [HasOptionFlagUpdate(Prefix = "Option_UD_FleshGolems_")]
    public static class Options
    {
        // Debug Settings
        [OptionFlag] public static bool DebugEnableTestKit;
        [OptionFlag] public static bool DebugEnableLogging;
        [OptionFlag] public static bool DebugDisableWorldGenLogging;
        [OptionFlag] public static bool DebugEnableAllLogging;

        // General Settings
        [OptionFlag] public static int SpecialReanimatedBuilderBaseChanceOneIn;
        [OptionFlag] public static int SpecialReanimatedBuilderChanceOneInMulti;

        [OptionFlag] public static int SpecialCorpseAnimatedBuilderBaseChanceOneIn;
        [OptionFlag] public static int SpecialCorpseAnimatedBuilderChanceOneInMulti;

        public static int SpecialReanimatedBuilderChanceOneIn => SpecialReanimatedBuilderBaseChanceOneIn * SpecialReanimatedBuilderChanceOneInMulti;
        public static int SpecialCorpseAnimatedBuilderChanceOneIn => SpecialCorpseAnimatedBuilderBaseChanceOneIn * SpecialCorpseAnimatedBuilderChanceOneInMulti;
    }
}
