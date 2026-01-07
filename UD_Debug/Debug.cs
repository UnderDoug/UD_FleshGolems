using XRL;

using static UD_FleshGolems.Utils;

namespace UD_FleshGolems.Logging
{
    public static class Debug
    {
        public static void MetricsManager_LogCallingModError(object Message)
        {
            if (!TryGetFirstCallingModNot(ThisMod, out ModInfo callingMod))
                callingMod = ThisMod;

            MetricsManager.LogModError(callingMod, Message);
        }
    }
}
