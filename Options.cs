using XRL;

namespace UD_FleshGolems
{
    [HasModSensitiveStaticCache]
    [HasOptionFlagUpdate(Prefix = "Option_UD_FleshGolems_")]
    public static class Options
    {
        // General Settings
        [OptionFlag] public static bool ExampleOption;
    }
}
