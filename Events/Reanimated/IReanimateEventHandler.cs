using System;
using System.Collections.Generic;
using System.Text;

using XRL.World;

namespace UD_FleshGolems.Events
{
    public interface IReanimateEventHandler
        : IModEventHandler<BeforeReanimateEvent>
        , IModEventHandler<ReanimateEvent>
        , IModEventHandler<AfterReanimateEvent>
    {
    }
}
