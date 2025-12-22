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
    public class DeathMemory : IComposite
    {
        public const string RUDE_TO_ASK_PROPTAG = "UD_FleshGolems DeathDetails DeathMemory RudeToAsk";
        public const string RUDE_TO_ASK_CHANCE_PROPTAG = "UD_FleshGolems DeathDetails DeathMemory RudeToAsk ChanceOneIn";
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
        
        [SerializeField]
        private GameObject _Corpse;
        public GameObject Corpse
        {
            get => _Corpse;
            private set
            {
                _Corpse = value;
                CorpseID = _Corpse?.ID;
            }
        }

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

        public bool IsValid => Validate();

        private DeathMemory()
        {
            Corpse = null;
            CorpseID = null;
            Killed = null;
            Killer = null;
            Method = null;
            RudeToAsk = false;
        }
        private DeathMemory(GameObject Corpse, string CorpseID = null, bool? Killed = null, KillerMemory? Killer = null, bool? Method = null, bool RudeToAsk = false)
            : this()
        {
            this.Corpse = Corpse;
            this.CorpseID ??= CorpseID;
            this.Killed = Killed;
            this.Killer = Killer;
            this.Method = Method;
            this.RudeToAsk = RudeToAsk;
        }
        private DeathMemory(GameObject Corpse)
            : this()
        {
            this.Corpse = Corpse;
        }
        private DeathMemory(DeathMemory Source)
            : this(Source.Corpse, Source.CorpseID, Source.Killed, Source.Killer, Source.Method, Source.RudeToAsk)
        { }
        private DeathMemory(GameObject Corpse, DeathMemory Source)
            : this(Source)
        {
            this.Corpse = Corpse;
        }

        public string GetCorpseID()
            => CorpseID;

        private string ConstructRandomChannel(string Seed)
            => RandomChannelPrefix + ":" + CorpseID + ":" + Seed;

        private int SeededRandomHigh(string Seed, int High)
            => Stat.SeededRandom(ConstructRandomChannel(Seed), 0, 999) % High;

        private bool SeededRememberKilled()
            => SeededRandomHigh(nameof(Killed), 3) == 0;

        private KillerMemory SeededRememberKiller()
        {
            if ((KillerMemory)SeededRandomHigh(nameof(Killer), Enum.GetValues(typeof(KillerMemory)).Length) is KillerMemory killerMemory
                && killerMemory != KillerMemory.Amnesia)
                return killerMemory;
            return (KillerMemory)SeededRandomHigh(nameof(Killer) + ":Again", Enum.GetValues(typeof(KillerMemory)).Length);
        }

        private bool SeededRememberMethod()
            => SeededRandomHigh(nameof(Method), 3) == 0;

        private bool SeededRudeToAsk()
        {
            int rudeToAskChanceOneIn = RudeToAskChanceOneIn;
            if (Corpse != null)
                rudeToAskChanceOneIn = Corpse.GetIntProperty(RUDE_TO_ASK_CHANCE_PROPTAG, rudeToAskChanceOneIn);

            return SeededRandomHigh(nameof(RudeToAsk), rudeToAskChanceOneIn) == 0;
        }

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
            KillerDetails KillerDetails,
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
            out KillerDetails KillerDetails,
            out DeathDescription DeathDescription)
        {
            KillerDetails = null;
            DeathDescription = null;

            if (Corpse == null)
                throw new ArgumentNullException(nameof(Corpse));

            if (DeathEvent == null)
                throw new ArgumentNullException(nameof(DeathEvent));

            KillerDetails = DeathEvent.Killer != null ? new(Corpse, DeathEvent) : null;
            DeathDescription = DeathDescription.GetFromDeathEvent(Corpse, DeathEvent);

            return Make(Corpse, DeathEvent.Killer, DeathEvent.Weapon, KillerDetails, DeathDescription);
        }

        public static DeathMemory CopyMemories(GameObject Corpse, DeathMemory DeathMemory)
            => Corpse != null
            ? new(Corpse, DeathMemory)
            : default;

        public bool? GetRemembersKilled()
            => Killed;

        public bool RemembersKilled()
            => GetRemembersKilled() is bool killed
            && killed;

        public KillerMemory? GetRemembersKiller()
            => Killer;

        public bool RemembersKiller()
            => GetRemembersKiller() is KillerMemory killer
            && killer > KillerMemory.Amnesia;

        public bool RemembersKillerName()
            => GetRemembersKiller() is KillerMemory killer
            && killer >= KillerMemory.Name;

        public bool RemembersKillerCreature()
            => GetRemembersKiller() is KillerMemory killer
            && killer >= KillerMemory.Creature;

        public bool RemembersKillerFeature()
            => GetRemembersKiller() is KillerMemory killer
            && killer >= KillerMemory.Feature;

        public bool? GetRemembersMethod()
            => Method;

        public bool RemembersMethod()
            => GetRemembersMethod() is bool method
            && method;

        public bool GetIsRudeToAsk()
            => RudeToAsk;

        public bool HasAmnesia()
            => !EmptyDeath()
            && (Killed == null || Killed == false)
            && (Killer == null || Killer == KillerMemory.Amnesia)
            && (Method == null || Method == false);

        public bool EmptyDeath()
            => Killed == null
            && Killer == null
            && Method == null;

        public bool Validate(GameObject Corpse = null)
            => (Corpse == null || Corpse.ID == CorpseID)
            && !EmptyDeath();

        public bool MemoryIsCompatibleWithElements(IEnumerable<string> ElementsList, bool Known)
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
        public bool MemoryIsCompatibleWithElements(string Elements, bool Known)
            => MemoryIsCompatibleWithElements(Elements?.CachedCommaExpansion(), Known);

        public StringMap<string> DebugInternals(GameObject Corpse = null) => new()
        {
            { nameof(CorpseID) + ": " + (CorpseID ?? NULL), null },
            { nameof(IsValid), Validate(Corpse).YehNah() },
            { nameof(HasAmnesia), HasAmnesia().YehNah() },
            { nameof(Killed), Killed.YehNah() },
            { nameof(Killer), "[" + (Killer != null ? (int)Killer : "-") + "]" },
            { nameof(Method), Method.YehNah() },
        };
        public string DebugInternalsString(GameObject Corpse = null)
            => DebugInternals(Corpse)
                ?.Aggregate(
                    seed: "",
                    func: (a, n) => a + "\n" + n.Value + " " + n.Key);
    }
}
