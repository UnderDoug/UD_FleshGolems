using System;
using System.Collections.Generic;
using System.Text;

using XRL.World;
using XRL.Rules;
using XRL.Collections;

using UD_FleshGolems.Events;
using static UD_FleshGolems.Const;

using SerializeField = UnityEngine.SerializeField;
using XRL.World.Parts;
using System.Linq;

namespace UD_FleshGolems.Parts.VengeanceHelpers
{
    [Serializable]
    public struct DeathMemory : IComposite
    {
        public static int BaseAmnesiaChance => 10;

        public static string RandomChannelPrefix => nameof(VengeanceHelpers) + "." + nameof(DeathMemory);

        [Serializable]

        public enum KillerMemory : int
        {
            Feature,
            Creature,
            Name,
        }
        /*
        [SerializeField]
        private GameObject _Corpse;
        public GameObject Corpse
        {
            readonly get => _Corpse;
            private set
            {
                _Corpse = value;
                CorpseID = _Corpse.ID;
            }
        }
        public UD_FleshGolems_DeathMemory DeathMemoryPart => Corpse?.GetPart<UD_FleshGolems_DeathMemory>();
        */

        [SerializeField]
        private string CorpseID;

        // null: info doesn't exist;
        // false: info exists but is unknown;
        // true: info exists and is known.
        // enum: higher number means greater detail. Each one includes the ones less than it.
        [SerializeField]
        private bool? Killed;

        [SerializeField]
        private KillerMemory? Killer;

        [SerializeField]
        private bool? Method;

        private DeathMemory(string CorpseID, bool? Killed = null, KillerMemory? Killer = null, bool? Method = null)
        {
            this.CorpseID = CorpseID;
            this.Killed = Killed;
            this.Killer = Killer;
            this.Method = Method;
        }
        private DeathMemory(GameObject Corpse)
            : this(Corpse.ID)
        {
            // _Corpse = Corpse;
            // CorpseID = Corpse.ID;
        }
        private DeathMemory(DeathMemory Source)
            : this(Source.CorpseID, Source.Killed, Source.Killer, Source.Method)
        { }
        private DeathMemory(string CorpseID, DeathMemory Source)
            : this(Source)
        {
            this.CorpseID = CorpseID;
        }

        private readonly string ConstructRandomChannel(string Seed)
            => RandomChannelPrefix + ":" + CorpseID + ":" + Seed;

        private readonly int SeededRandomHigh(string Seed, int High)
            => Stat.SeededRandom(ConstructRandomChannel(Seed), 0, 999) % High;

        private readonly bool SeededRememberKilled()
            => SeededRandomHigh(nameof(Killed), 2) == 0;

        private readonly KillerMemory SeededRememberKiller()
            => (KillerMemory)SeededRandomHigh(nameof(Killer), Enum.GetValues(typeof(KillerMemory)).Length - 1);

        private readonly bool SeededRememberMethod()
            => SeededRandomHigh(nameof(Method), 2) == 0;

        private DeathMemory SetSeededRememberKilled()
        {
            Killed = SeededRememberKilled();
            return this;
        }

        private DeathMemory SetSeededRememberKiller()
        {
            Killer = SeededRememberKiller();
            return this;
        }

        private DeathMemory SetSeededRememberMethod()
        {
            Method = SeededRememberMethod();
            return this;
        }

        public static DeathMemory Make(
            GameObject Corpse,
            GameObject Killer,
            GameObject Weapon,
            KillerDetails? KillerDetails,
            DeathDescription DeathDescription)
        {
            if (Corpse == null)
                throw new ArgumentNullException(nameof(Corpse));

            if (DeathDescription == null)
                throw new ArgumentNullException(nameof(DeathDescription));

            DeathMemory deathMemory = new(Corpse);
            int amnesiaRoll = deathMemory.SeededRandomHigh(nameof(GetDeathMemoryAmnesiaChanceEvent), 100);
            int amnesiaChance = GetDeathMemoryAmnesiaChanceEvent.GetFor(
                Corpse,
                Killer,
                Weapon,
                KillerDetails,
                DeathDescription,
                BaseAmnesiaChance,
                typeof(DeathMemory).ToString());

            if (amnesiaChance < amnesiaRoll)
            {
                if (!DeathDescription.Killed.IsNullOrEmpty())
                    deathMemory.SetSeededRememberKilled();

                if (Killer != null)
                    deathMemory.SetSeededRememberKiller();

                if (Weapon != null || !DeathDescription.Method.IsNullOrEmpty())
                    deathMemory.SetSeededRememberMethod();
            }

            return deathMemory;
        }

        public static DeathMemory Make(
            GameObject Corpse,
            IDeathEvent DeathEvent,
            out KillerDetails? KillerDetails,
            out DeathDescription DeathDescription)
        {
            KillerDetails = null;
            DeathDescription = null;

            if (Corpse == null)
                throw new ArgumentNullException(nameof(Corpse));

            if (DeathEvent == null)
                throw new ArgumentNullException(nameof(DeathEvent));

            KillerDetails = DeathEvent.Killer != null ? new(DeathEvent) : null;
            DeathDescription = new(DeathEvent);

            return Make(Corpse, DeathEvent.Killer, DeathEvent.Weapon, KillerDetails, DeathDescription);
        }

        public static DeathMemory CopyMemories(GameObject Corpse, DeathMemory DeathMemory)
            => Corpse != null
            ? new(Corpse.ID, DeathMemory)
            : default;

        public readonly bool? RemebersKilled()
            => Killed;

        public readonly KillerMemory? RemebersKiller()
            => Killer;

        public readonly bool RemebersKillerName()
            => Killer != null
            && (KillerMemory)Killer >= KillerMemory.Name;

        public readonly bool RemebersKillerCreature()
            => Killer != null
            && (KillerMemory)Killer >= KillerMemory.Creature;

        public readonly bool RemebersKillerFeature()
            => Killer != null
            && (KillerMemory)Killer >= KillerMemory.Feature;

        public readonly bool? RemebersMethod()
            => Method;

        public readonly bool HasAmnesia()
            => Killed == null
            && Killer == null
            && Method == null;

        public readonly StringMap<string> DebugInternals() => new()
        {
            { nameof(CorpseID) + ": " + (CorpseID ?? NULL), null },
            { nameof(Killed), Killed?.YehNah() },
            { nameof(Killer), "[" + (Killer != null ? (int)Killer : "-") + "]" },
            { nameof(Method), Method?.YehNah() },
        };
        public readonly string DebugInternalsString()
            => DebugInternals()
                ?.Aggregate(
                    seed: "",
                    func: (a, n) => a + "\n" + n.Value + " " + n.Key);
    }
}
