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
        public const string RUDE_TO_ASK_PROPTAG = "UD_FleshGolems DeathDetails DeathMemory RudeToAsk";
        public static int BaseAmnesiaChance => 10;

        public static string RandomChannelPrefix => nameof(VengeanceHelpers) + "." + nameof(DeathMemory);

        public static int RudeToAskChanceOneIn = 4;

        [Serializable]
        public enum KillerMemory : int
        {
            Amnesia,
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

        [SerializeField]
        private bool RudeToAsk;

        public readonly bool IsValid => Validate();

        private DeathMemory(string CorpseID, bool? Killed = null, KillerMemory? Killer = null, bool? Method = null, bool RudeToAsk = false)
        {
            this.CorpseID = CorpseID;
            this.Killed = Killed;
            this.Killer = Killer;
            this.Method = Method;
            this.RudeToAsk = RudeToAsk;
        }
        private DeathMemory(GameObject Corpse)
            : this(Corpse.ID)
        {
            // _Corpse = Corpse;
            // CorpseID = Corpse.ID;
        }
        private DeathMemory(DeathMemory Source)
            : this(Source.CorpseID, Source.Killed, Source.Killer, Source.Method, Source.RudeToAsk)
        { }
        private DeathMemory(string CorpseID, DeathMemory Source)
            : this(Source)
        {
            this.CorpseID = CorpseID;
        }

        public readonly string GetCorpseID()
            => CorpseID;

        private readonly string ConstructRandomChannel(string Seed)
            => RandomChannelPrefix + ":" + CorpseID + ":" + Seed;

        private readonly int SeededRandomHigh(string Seed, int High)
            => Stat.SeededRandom(ConstructRandomChannel(Seed), 0, 999) % High;

        private readonly bool SeededRememberKilled()
            => SeededRandomHigh(nameof(Killed), 2) == 0;

        private readonly KillerMemory SeededRememberKiller()
            => (KillerMemory)SeededRandomHigh(nameof(Killer), Enum.GetValues(typeof(KillerMemory)).Length);

        private readonly bool SeededRememberMethod()
            => SeededRandomHigh(nameof(Method), 2) == 0;

        private readonly bool SeededRudeToAsk()
            => SeededRandomHigh(nameof(RudeToAsk), RudeToAskChanceOneIn) == 0;

        private DeathMemory SetSeededRememberKilled(bool Amnesia = false)
        {
            Killed = false;
            if (!Amnesia)
                Killed = SeededRememberKilled();
            return this;
        }

        private DeathMemory SetSeededRememberKiller(bool Amnesia = false)
        {
            Killer = KillerMemory.Amnesia;
            if (!Amnesia)
                Killer = SeededRememberKiller();
            return this;
        }

        private DeathMemory SetSeededRememberMethod(bool Amnesia = false)
        {
            Method = false;
            if (!Amnesia)
                Method = SeededRememberMethod();
            return this;
        }

        private DeathMemory SetSeededRudeToAsk(GameObject Corpse, bool? Override = null)
        {
            bool setRudeToAsk = false;
            if (Override.HasValue)
            {
                setRudeToAsk = Override.GetValueOrDefault();
            }
            else
            if (Corpse != null
                && Corpse.GetPropertyOrTag(RUDE_TO_ASK_PROPTAG) is string rudeToAskPropTag)
            {
                if (rudeToAskPropTag.EqualsNoCase("true"))
                    setRudeToAsk = true;
                else
                if (rudeToAskPropTag.EqualsNoCase("false"))
                    setRudeToAsk = false;
            }
            else
            {
                setRudeToAsk = SeededRudeToAsk();
            }
            RudeToAsk = setRudeToAsk;
            return this;
        }

        private DeathMemory SetSeededRudeToAsk(bool? Override = null)
            => SetSeededRudeToAsk(null, Override);

        public static DeathMemory Make(
            GameObject Corpse,
            GameObject Killer,
            GameObject Weapon,
            KillerDetails? KillerDetails,
            DeathDescription DeathDescription)
        {
            if (DeathDescription == null)
                throw new ArgumentNullException(nameof(DeathDescription));

            DeathDescription.ParentCorpse = Corpse ?? throw new ArgumentNullException(nameof(Corpse));

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

            bool amnesia = amnesiaChance >= amnesiaRoll;

            if (!DeathDescription.Killed.IsNullOrEmpty())
                deathMemory.SetSeededRememberKilled(amnesia);

            if (Killer != null)
                deathMemory.SetSeededRememberKiller(amnesia);

            if (Weapon != null || !DeathDescription.Method.IsNullOrEmpty())
                deathMemory.SetSeededRememberMethod(amnesia);

            deathMemory.SetSeededRudeToAsk(Corpse);

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
            DeathDescription = DeathDescription.GetFromDeathEvent(Corpse, DeathEvent);

            return Make(Corpse, DeathEvent.Killer, DeathEvent.Weapon, KillerDetails, DeathDescription);
        }

        public static DeathMemory CopyMemories(GameObject Corpse, DeathMemory DeathMemory)
            => Corpse != null
            ? new(Corpse.ID, DeathMemory)
            : default;

        public readonly bool? GetRemembersKilled()
            => Killed;

        public readonly bool RemembersKilled()
            => GetRemembersKilled() is bool killed
            && killed;

        public readonly KillerMemory? GetRemembersKiller()
            => Killer;

        public readonly bool RemembersKiller()
            => GetRemembersKiller() is KillerMemory killer
            && killer > KillerMemory.Amnesia;

        public readonly bool RemembersKillerName()
            => GetRemembersKiller() is KillerMemory killer
            && killer >= KillerMemory.Name;

        public readonly bool RemembersKillerCreature()
            => GetRemembersKiller() is KillerMemory killer
            && killer >= KillerMemory.Creature;

        public readonly bool RemembersKillerFeature()
            => GetRemembersKiller() is KillerMemory killer
            && killer >= KillerMemory.Feature;

        public readonly bool? GetRemembersMethod()
            => Method;

        public readonly bool RemembersMethod()
            => GetRemembersMethod() is bool method
            && method;

        public readonly bool GetIsRudeToAsk()
            => RudeToAsk;

        public readonly bool HasAmnesia()
            => Killed == false
            && Killer == KillerMemory.Amnesia
            && Method == false;

        public readonly bool EmptyDeath()
            => Killed == null
            && Killer == null
            && Method == null;

        public readonly bool Validate(GameObject Corpse = null)
            => (Corpse == null || Corpse.ID == CorpseID)
            && !EmptyDeath();

        public readonly bool MemoryIsCompatibleWithElements(IEnumerable<string> ElementsList, bool Known)
        {
            if (ElementsList.IsNullOrEmpty())
                return false;

            foreach (string element in ElementsList)
            {
                switch (element)
                {
                    case "Killed":
                        if (RemembersKilled() != Known)
                            return false;
                        break;

                    case "Killer":
                        if (RemembersKiller() != Known)
                            return false;
                        break;
                    case "KillerName":
                        if (RemembersKillerName() != Known)
                            return false;
                        break;
                    case "KillerCreature":
                        if (RemembersKillerCreature() != Known)
                            return false;
                        break;
                    case "KillerFeature":
                        if (RemembersKillerCreature() != Known)
                            return false;
                        break;

                    case "Environment":
                        if (RemembersKiller())
                            return false;
                        break;

                    case "Method":
                        if (RemembersMethod() != Known)
                            return false;
                        break;
                }
            }
            return true;
        }
        public readonly bool MemoryIsCompatibleWithElements(string Elements, bool Known)
            => MemoryIsCompatibleWithElements(Elements?.CachedCommaExpansion(), Known);

        public readonly StringMap<string> DebugInternals(GameObject Corpse = null) => new()
        {
            { nameof(CorpseID) + ": " + (CorpseID ?? NULL), null },
            { nameof(IsValid), Validate(Corpse).YehNah() },
            { nameof(HasAmnesia), HasAmnesia().YehNah() },
            { nameof(Killed), Killed.YehNah() },
            { nameof(Killer), "[" + (Killer != null ? (int)Killer : "-") + "]" },
            { nameof(Method), Method.YehNah() },
        };
        public readonly string DebugInternalsString(GameObject Corpse = null)
            => DebugInternals(Corpse)
                ?.Aggregate(
                    seed: "",
                    func: (a, n) => a + "\n" + n.Value + " " + n.Key);
    }
}
