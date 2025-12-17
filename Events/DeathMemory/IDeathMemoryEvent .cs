using System;
using System.Collections.Generic;
using System.Text;

using XRL.World;
using XRL.World.Parts;

namespace UD_FleshGolems.Events
{
    [GameEvent(Base = true, Cascade = CASCADE_EQUIPMENT | CASCADE_INVENTORY | CASCADE_SLOTS, Cache = Cache.Pool)]
    public abstract class IDeathMemoryEvent<T> : ModPooledEvent<T>
        where T : IDeathMemoryEvent<T>, new()
    {
        public new static readonly int CascadeLevel = CASCADE_EQUIPMENT | CASCADE_INVENTORY | CASCADE_SLOTS;

        public static string RegisteredEventID => typeof(T).Name;

        public GameObject Corpse;

        public GameObject Killer;

        public GameObject Weapon;

        public Event StringyEvent;

        public IDeathMemoryEvent()
        {
            Corpse = null;
            Killer = null;
            Weapon = null;
            StringyEvent = null;
        }

        public virtual string GetRegisteredEventID()
            => RegisteredEventID;

        public override int GetCascadeLevel()
            => CascadeLevel;

        public override void Reset()
        {
            base.Reset();
            Corpse = null;
            Killer = null;
            Weapon = null;
            StringyEvent?.Clear();
            StringyEvent = null;
        }

        public static T FromPool(GameObject Corpse, GameObject Killer = null, GameObject Weapon = null)
        {
            if (Corpse == null)
            {
                return FromPool();
            }
            T E = FromPool();
            E.Corpse = Corpse;
            E.Killer = Killer;
            E.Weapon = Weapon;
            E.StringyEvent = E.GetStringyEvent();
            return E;
        }

        public static Event GetStringyEvent(IDeathMemoryEvent<T> ForEvent)
            => ForEvent == null
            ? Event.New(RegisteredEventID)
            : Event.New(ForEvent.GetRegisteredEventID(),
                nameof(ForEvent.Corpse), ForEvent?.Corpse,
                nameof(ForEvent.Killer), ForEvent?.Killer,
                nameof(ForEvent.Weapon), ForEvent?.Weapon);

        public virtual Event GetStringyEvent()
            => GetStringyEvent(this);

        protected static T Process(GameObject Corpse, GameObject Killer, GameObject Weapon, out bool Success)
        {
            Success = true;
            T E = FromPool(Corpse, Killer, Weapon);
            if (Success
                && GameObject.Validate(ref Corpse)
                && Corpse.HasRegisteredEvent(E.GetRegisteredEventID()))
            {
                Success = Corpse.FireEvent(E.StringyEvent);
            }
            if (Success
                && GameObject.Validate(ref Corpse)
                && Corpse.WantEvent(E.GetID(), E.GetCascadeLevel()))
            {
                Success = Corpse.HandleEvent(E);
            }
            return E;
        }

        public static bool Check(GameObject Corpse, GameObject Killer, GameObject Weapon)
        {
            Process(Corpse, Killer, Weapon, out bool success);
            return success;
        }

        public static void Send(GameObject Corpse, GameObject Killer, GameObject Weapon)
        {
            Process(Corpse, Killer, Weapon, out _);
        }

        protected void ValidateGameObject(GameObject Object, out bool WantsMin, out bool WantsStr)
        {
            WantsMin = (Object?.WantEvent(GetID(), GetCascadeLevel())).GetValueOrDefault();
            WantsStr = (Object?.HasRegisteredEvent(GetRegisteredEventID())).GetValueOrDefault();
        }

        protected virtual bool Validate(
            out bool CorpseWantsMin,
            out bool CorpseWantsStr,
            out bool KillerWantsMin,
            out bool KillerWantsStr,
            out bool WeaponWantsMin,
            out bool WeaponWantsStr)
        {
            CorpseWantsMin = false;
            CorpseWantsStr = false;
            KillerWantsMin = false;
            KillerWantsStr = false;
            WeaponWantsMin = false;
            WeaponWantsStr = false;

            if (Corpse == null)
                return false;

            ValidateGameObject(Corpse, out CorpseWantsMin, out CorpseWantsStr);

            ValidateGameObject(Killer, out KillerWantsMin, out KillerWantsStr);

            ValidateGameObject(Weapon, out WeaponWantsMin, out WeaponWantsStr);

            return true;
        }

        protected virtual bool UpdateStringyEvent()
        {
            if (StringyEvent == null)
                return false;

            return true;
        }

        protected virtual bool ProcessGameObject(GameObject Object, bool WantsMin, bool WantsStr)
        {
            if (WantsMin && !Object.HandleEvent(this))
                return false;

            UpdateStringyEvent();

            if (WantsStr && !Object.FireEvent(StringyEvent))
                return false;

            return true;
        }

        protected virtual bool Process(
            bool CorpseWantsMin,
            bool CorpseWantsStr,
            bool KillerWantsMin,
            bool KillerWantsStr,
            bool WeaponWantsMin,
            bool WeaponWantsStr)
        {
            if (!ProcessGameObject(Corpse, CorpseWantsMin, CorpseWantsStr))
                return false;

            if (!ProcessGameObject(Killer, KillerWantsMin, KillerWantsStr))
                return false;

            if (!ProcessGameObject(Weapon, WeaponWantsMin, WeaponWantsStr))
                return false;

            return true;
        }

        protected virtual bool CheckValidateAndProcess()
            => Validate(
                out bool corpseWantsMin,
                out bool corpseWantsStr,
                out bool killerWantsMin,
                out bool killerWantsStr,
                out bool weaponWantsMin,
                out bool weaponWantsStr)
            && Process(
                corpseWantsMin,
                corpseWantsStr,
                killerWantsMin,
                killerWantsStr,
                weaponWantsMin,
                weaponWantsStr);

        protected virtual T ValidateAndProcess()
        {
            CheckValidateAndProcess();
            return (T)this;
        }
    }
}
