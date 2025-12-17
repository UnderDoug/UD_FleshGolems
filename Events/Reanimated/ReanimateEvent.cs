using System;
using System.Collections.Generic;
using System.Text;

using XRL.World;
using XRL.World.Parts;

namespace UD_FleshGolems.Events
{
    [GameEvent(Cascade = CASCADE_EQUIPMENT | CASCADE_INVENTORY | CASCADE_SLOTS, Cache = Cache.Pool)]
    public class ReanimateEvent : IReanimateEvent<ReanimateEvent>
    {
        public ReanimateEvent()
            : base() { }

        public ReanimateEvent(GameObject Corpse, UD_FleshGolems_PastLife PastLifePart, GameObject Actor = null, GameObject Using = null, List<IPart> PartsToRemove = null)
            : base(Corpse, PastLifePart, Actor, Using, PartsToRemove) { }

        public override string GetRegisteredEventID()
            => RegisteredEventID;

        public override int GetCascadeLevel()
            => CascadeLevel;
    }
}
