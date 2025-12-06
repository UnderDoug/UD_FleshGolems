using System;
using System.Collections.Generic;
using System.Text;

using XRL.World;
using XRL.World.Parts;

namespace UD_FleshGolems.Events
{
    [GameEvent(Base = true, Cascade = CASCADE_EQUIPMENT | CASCADE_INVENTORY | CASCADE_SLOTS, Cache = Cache.Pool)]
    public abstract class IReanimateEvent<T> : ModPooledEvent<T>
        where T : IReanimateEvent<T>, new()
    {
        public new static readonly int CascadeLevel = CASCADE_EQUIPMENT | CASCADE_INVENTORY | CASCADE_SLOTS;

        public static string RegisteredEventID => typeof(T).Name;

        public GameObject Corpse;

        public UD_FleshGolems_PastLife PastLifePart;

        public GameObject Actor;

        public GameObject Using;

        public List<IPart> PartsToRemove;

        public IReanimateEvent()
        {
            Corpse = null;
            PastLifePart = null;
            Actor = null;
            Using = null;
            PartsToRemove = null;
        }

        public IReanimateEvent(GameObject Corpse, UD_FleshGolems_PastLife PastLifePart, GameObject Actor = null, GameObject Using = null, List<IPart> PartsToRemove = null)
            : this()
        {
            this.Corpse = Corpse;
            this.PastLifePart = PastLifePart;
            this.Actor = Actor;
            this.Using = Using;
            this.PartsToRemove = PartsToRemove ?? new();
        }

        public virtual string GetRegisteredEventID()
            => RegisteredEventID;

        public override int GetCascadeLevel()
            => CascadeLevel;

        public override void Reset()
        {
            base.Reset();
            Actor = null;
            Corpse = null;
            Using = null;
            PartsToRemove = null;
        }

        public static T FromPool(GameObject Corpse, UD_FleshGolems_PastLife PastLifePart, GameObject Actor = null, GameObject Using = null, List<IPart> PartsToRemove = null)
        {
            if (Corpse == null || PastLifePart == null)
            {
                return FromPool();
            }
            T E = FromPool();
            E.Corpse = Corpse;
            E.PastLifePart = PastLifePart;
            E.Actor = Actor;
            E.Using = Using;
            E.PartsToRemove = PartsToRemove ?? new();
            return E;
        }

        protected static T Process(GameObject Corpse, UD_FleshGolems_PastLife PastLifePart, out bool Success, GameObject Actor = null, GameObject Using = null, List<IPart> PartsToRemove = null)
        {
            PartsToRemove ??= new();
            Success = true;
            T E = FromPool(Corpse, PastLifePart, Actor, Using, PartsToRemove);
            if (Success
                && GameObject.Validate(ref Corpse)
                && Corpse.HasRegisteredEvent(E.GetRegisteredEventID()))
            {
                PartsToRemove ??= new();
                Success = Corpse.FireEvent(Event.New(
                    ID: E.GetRegisteredEventID(),
                    nameof(Corpse), Corpse,
                    nameof(PastLifePart), PastLifePart,
                    nameof(Actor), Actor,
                    nameof(Using), Using,
                    nameof(PartsToRemove), PartsToRemove));
            }
            if (Success
                && GameObject.Validate(ref Corpse)
                && Corpse.WantEvent(E.GetID(), E.GetCascadeLevel()))
            {
                Success = Corpse.HandleEvent(E);
            }
            return E;
        }

        public static bool Check(GameObject Corpse, UD_FleshGolems_PastLife PastLifePart, GameObject Actor = null, GameObject Using = null)
        {
            Process(Corpse, PastLifePart, out bool success, Actor, Using);
            return success;
        }

        public static void Send(GameObject Corpse, UD_FleshGolems_PastLife PastLifePart, GameObject Actor = null, GameObject Using = null, bool DoPartRemoval = false)
        {
            T E = Process(Corpse, PastLifePart, out bool success, Actor, Using);
            if (success
                && !E.PartsToRemove.IsNullOrEmpty()
                && DoPartRemoval)
                foreach (IPart partToRemove in E.PartsToRemove)
                    Corpse.RemovePart(partToRemove);
        }
    }
}
