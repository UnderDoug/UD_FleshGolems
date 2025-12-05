using static UD_FleshGolems.Utils;

namespace XRL.World.Parts
{
    public class UD_FleshGolems_ObliterateSelf : IScribedPart
    {
        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == EnvironmentalUpdateEvent.ID;

        public override bool HandleEvent(EnvironmentalUpdateEvent E)
        {
            if ((ParentObject?.Obliterate()).GetValueOrDefault())
            {
                return false;
            }
            MetricsManager.LogModError(ThisMod, (ParentObject?.DebugName ?? "null object") + " failed to obliterate itself.");
            return base.HandleEvent(E);
        }
    }
}
