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
using UD_FleshGolems.Logging;

namespace UD_FleshGolems.Parts.VengeanceHelpers
{
    [Serializable]
    public class DeathMemory : IComposite
    {
        public const string KILLED_PROPTAG = "UD_FleshGolems DeathDetails DeathMemory Killed";
        public const string KILLER_PROPTAG = "UD_FleshGolems DeathDetails DeathMemory Killer";
        public const string METHOD_PROPTAG = "UD_FleshGolems DeathDetails DeathMemory Method";

        public const string RUDE_TO_ASK_PROPTAG = "UD_FleshGolems DeathDetails DeathMemory RudeToAsk";
        public const string RUDE_TO_ASK_CHANCE_PROPTAG = "UD_FleshGolems DeathDetails DeathMemory RudeToAsk ChanceOneIn";
        public const string AMNESIA_PROPTAG = "UD_FleshGolems DeathDetails DeathMemory Amnesia";
        public const string AMNESIA_CHANCE_PROPTAG = "UD_FleshGolems DeathDetails DeathMemory Amnesia ChanceOneIn";

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

        [NonSerialized]
        public GameObject Corpse;

        // null: info doesn't exist;
        // false: info exists but is unknown;
        // true: info exists and is known.
        // enum: higher number means greater detail. Each one includes the ones less than it.
        private bool? Killed;
        private KillerMemory? Killer;
        private bool? Method;
        private bool RudeToAsk;

        public bool IsValid => Validate();

        private bool Environment => GetRemembersKiller() == null;

        private DeathMemory()
        {
            Corpse = null;
            Killed = null;
            Killer = null;
            Method = null;
            RudeToAsk = false;
        }
        private DeathMemory(GameObject Corpse, bool? Killed = null, KillerMemory? Killer = null, bool? Method = null, bool RudeToAsk = false)
            : this()
        {
            this.Corpse = Corpse;
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
            : this(Source.Corpse, Source.Killed, Source.Killer, Source.Method, Source.RudeToAsk)
        { }
        private DeathMemory(GameObject Corpse, DeathMemory Source)
            : this(Source)
        {
            this.Corpse = Corpse;
        }

        public string GetCorpseID()
            => Corpse?.ID;

        private string ConstructRandomChannel(string Seed)
            => RandomChannelPrefix + ":" + Corpse?.ID + ":" + Seed;

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

        private bool SeededRudeToAsk(int? ChanceOneIn = null)
            => SeededRandomHigh(nameof(RudeToAsk), ChanceOneIn ?? RudeToAskChanceOneIn) == 0;

        private DeathMemory SetSeededRememberKilled(bool Amnesia = false, bool? Override = null)
        {
            if (Override != null)
            {
                Killed = Override;
                return this;
            }

            Killed = false;
            if (!Amnesia)
                Killed = SeededRememberKilled();

            return this;
        }

        private DeathMemory SetSeededRememberKiller(bool Amnesia = false, KillerMemory? Override = null)
        {
            if (Override != null)
            {
                Killer = Override;
                return this;
            }

            Killer = KillerMemory.Amnesia;
            if (!Amnesia)
                Killer = SeededRememberKiller();

            return this;
        }

        private DeathMemory SetSeededRememberMethod(bool Amnesia = false, bool? Override = null)
        {
            if (Override != null)
            {
                Method = Override;
                return this;
            }

            Method = false;
            if (!Amnesia)
                Method = SeededRememberMethod();

            return this;
        }

        private DeathMemory SetSeededRudeToAsk(bool? Override = null, int? ChanceOneIn = null)
        {
            bool setRudeToAsk;
            if (Override != null)
            {
                setRudeToAsk = Override.GetValueOrDefault();
            }
            else
            {
                setRudeToAsk = SeededRudeToAsk(ChanceOneIn);
            }

            RudeToAsk = setRudeToAsk;
            return this;
        }

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

            return new DeathMemory(Corpse).SetMemories(Corpse, Killer, Weapon, KillerDetails, DeathDescription);
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

        private DeathMemory SetMemories(
            GameObject Corpse,
            GameObject Killer,
            GameObject Weapon,
            KillerDetails KillerDetails,
            DeathDescription DeathDescription)
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Corpse), Corpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(Killer), Killer?.DebugName ?? NULL),
                    Debug.Arg(nameof(Weapon), Weapon?.DebugName ?? NULL),
                    Debug.Arg(nameof(KillerDetails), (KillerDetails != null).YehNah()),
                    Debug.Arg(nameof(DeathDescription), (DeathDescription != null).YehNah()),
                });

            int rudeToAskChanceOverride = -1;
            int? rudeToAskChanceOneIn = null;
            if (Corpse?.GetPropertyOrTag(RUDE_TO_ASK_CHANCE_PROPTAG, null) is string rudeToAskChancePropTag
                && int.TryParse(rudeToAskChancePropTag, out rudeToAskChanceOverride)
                && rudeToAskChanceOverride >= 0)
                rudeToAskChanceOneIn = rudeToAskChanceOverride;

            bool? overrideRudeToAsk = null;
            if (Corpse?.GetPropertyOrTag(RUDE_TO_ASK_PROPTAG) is string rudeToAskPropTag)
            {
                if (rudeToAskPropTag.EqualsNoCase("true"))
                    overrideRudeToAsk = true;
                else
                if (rudeToAskPropTag.EqualsNoCase("false"))
                    overrideRudeToAsk = false;

                Debug.CheckYeh(RUDE_TO_ASK_PROPTAG, rudeToAskPropTag, indent[1]);
            }

            SetSeededRudeToAsk(overrideRudeToAsk, rudeToAskChanceOneIn);

            int amnesiaRoll = SeededRandomHigh(nameof(GetDeathMemoryAmnesiaChanceEvent), 100);
            int amnesiaChance = GetDeathMemoryAmnesiaChanceEvent.GetFor(
                Corpse: Corpse,
                Killer: Killer,
                Weapon: Weapon,
                KillerDetails: KillerDetails,
                DeathDescription: DeathDescription,
                BaseChance: BaseAmnesiaChance,
                Context: typeof(DeathMemory).ToString());

            int amnesiaChanceOverride = -1;
            if (Corpse?.GetPropertyOrTag(AMNESIA_CHANCE_PROPTAG) is string amnesiaChancePropTag
                && int.TryParse(amnesiaChancePropTag, out amnesiaChanceOverride)
                && amnesiaChanceOverride >= 0)
            {
                amnesiaChance = amnesiaChanceOverride;
                Debug.CheckYeh(AMNESIA_CHANCE_PROPTAG, amnesiaChanceOverride, indent[1]);
            }

            if (Corpse?.GetPropertyOrTag(AMNESIA_PROPTAG) is string amnesiaPropTag)
            {
                if (amnesiaPropTag.EqualsNoCase("true"))
                    amnesiaChance = 100;
                else
                if (amnesiaPropTag.EqualsNoCase("false"))
                    amnesiaChance = 0;

                Debug.CheckYeh(AMNESIA_PROPTAG, amnesiaPropTag, indent[1]);
            }

            bool amnesia = amnesiaChance >= amnesiaRoll;

            bool? killedOverride = null;
            KillerMemory? killerOverride = null;
            bool? methodOverride = null;

            if (Corpse?.GetPropertyOrTag(KILLED_PROPTAG) is string killedOverridePropTag)
            {
                if (killedOverridePropTag.EqualsNoCase("true"))
                    killedOverride = true;
                else
                if (killedOverridePropTag.EqualsNoCase("false"))
                    killedOverride = false;

                Debug.CheckYeh(KILLED_PROPTAG, killedOverridePropTag, indent[1]);
            }
            if (Corpse?.GetPropertyOrTag(KILLER_PROPTAG) is string killerOverridePropTag)
            {
                killerOverride = killerOverridePropTag.ToLower().CapitalizeExcept() switch
                {
                    "3" or
                    "True" or
                    nameof(KillerMemory.Name) => KillerMemory.Name,

                    "2" or
                    nameof(KillerMemory.Creature) => KillerMemory.Creature,

                    "1" or
                    nameof(KillerMemory.Feature) => KillerMemory.Feature,

                    "0" or
                    "False" or
                    _ => KillerMemory.Amnesia,
                };
                Debug.CheckYeh(KILLER_PROPTAG, killerOverridePropTag.ToLower().CapitalizeExcept(), indent[1]);
            }
            if (Corpse?.GetPropertyOrTag(METHOD_PROPTAG) is string methodOverridePropTag)
            {
                if (methodOverridePropTag.EqualsNoCase("true"))
                    methodOverride = true;
                else
                if (methodOverridePropTag.EqualsNoCase("false"))
                    methodOverride = false;

                Debug.CheckYeh(METHOD_PROPTAG, methodOverridePropTag, indent[1]);
            }

            if (!DeathDescription.Killed.IsNullOrEmpty())
                SetSeededRememberKilled(amnesia, killedOverride);

            if (Killer != null)
                SetSeededRememberKiller(amnesia, killerOverride);

            if (Weapon != null || !DeathDescription.Method.IsNullOrEmpty())
                SetSeededRememberMethod(amnesia, methodOverride);

            Debug.YehNah(nameof(killedOverride), killedOverride, indent[0]);
            Debug.Log("[" + (killerOverride != null ? (int)killerOverride : "-") + "] " + nameof(killerOverride), Indent: indent[0]);
            Debug.YehNah(nameof(methodOverride), methodOverride, indent[0]);

            Debug.YehNah(nameof(Killed), Killed, indent[0]);
            Debug.Log("[" + (this.Killer != null ? (int)this.Killer : "-") + "]", nameof(this.Killer), indent[0]);
            Debug.YehNah(nameof(Method), Method, indent[0]);

            return this;
        }

        public static DeathMemory CopyMemories(GameObject Corpse, DeathMemory DeathMemory)
        {
            if (Corpse != null
                && DeathMemory != null
                && Corpse.GetDeathDetails() is UD_FleshGolems_DeathDetails deathDetails)
            {
                return new DeathMemory(Corpse, DeathMemory)
                    ?.SetMemories(
                        Corpse: Corpse,
                        Killer: deathDetails.Killer,
                        Weapon: deathDetails.Weapon,
                        KillerDetails: deathDetails.KillerDetails,
                        deathDetails.DeathDescription);
            }
            return null;
        }

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
            => (Corpse == null || Corpse == this.Corpse)
            && !EmptyDeath();

        public bool MemoryIsCompatibleWithElements(IEnumerable<string> ElementsList, bool Known)
        {
            if (ElementsList.IsNullOrEmpty())
                return false;

            KillerMemory killerMemory = GetRemembersKiller() ?? KillerMemory.Amnesia;
            foreach (string element in ElementsList)
            {
                switch (element)
                {
                    case nameof(Killed):
                        if (RemembersKilled() != Known
                            || GetRemembersKilled() == null)
                            return false;
                        break;

                    case nameof(Killer):
                        if (RemembersKiller() != Known
                            || GetRemembersKiller() == null)
                            return false;
                        break;
                    case nameof(Killer) + nameof(KillerMemory.Name):
                        if (RemembersKillerName() != Known
                            || (killerMemory >= KillerMemory.Name) != Known
                            || GetRemembersKiller() == null)
                            return false;
                        break;
                    case nameof(Killer) + nameof(KillerMemory.Creature):
                        if (RemembersKillerCreature() != Known
                            || (killerMemory >= KillerMemory.Creature) != Known
                            || GetRemembersKiller() == null)
                            return false;
                        break;
                    case nameof(Killer) + nameof(KillerMemory.Feature):
                        if (RemembersKillerFeature() != Known
                            || (killerMemory >= KillerMemory.Feature) != Known
                            || GetRemembersKiller() == null)
                            return false;
                        break;

                    case nameof(Environment):
                        if (Environment != Known)
                            return false;
                        break;

                    case nameof(Method):
                        if (RemembersMethod() != Known
                            || GetRemembersMethod() == null)
                            return false;
                        break;
                }
            }
            return true;
        }
        public bool MemoryIsCompatibleWithElements(string Elements, bool Known)
            => MemoryIsCompatibleWithElements(Elements?.CachedCommaExpansion(), Known);

        public StringMap<string> DebugInternals(GameObject Corpse = null)
            => new()
            {
                { this.Corpse?.ID ?? NULL, nameof(this.Corpse) + "." + nameof(this.Corpse.ID) + ":" },
                { nameof(IsValid), Validate(Corpse).YehNah() },
                { nameof(RudeToAsk), RudeToAsk.YehNah() },
                { nameof(HasAmnesia), HasAmnesia().YehNah() },
                { nameof(Killed), Killed.YehNah() },
                { nameof(Killer), "[" + (Killer != null ? (int)Killer : "-") + "]" },
                { nameof(Method), Method.YehNah() },
            };

        private static string ProcessDebugInternalKVP(
            string Accumulator,
            KeyValuePair<string, string> Next,
            string Joiner = "\n",
            bool Color = false)
        {
            if (!Accumulator.IsNullOrEmpty())
                Accumulator += Joiner;
            string value = Next.Value;
            string key = Next.Key;
            if (Color
                && !value.IsNullOrEmpty()
                && !key.IsNullOrEmpty())
            {
                if (value.Contains("-"))
                    value = value[0] + "{{w|" + value[1] + "}}" + value[2];
                else
                if (value.Length > 2 && int.TryParse(value[1].ToString(), out int valueNum))
                {
                    if (valueNum == 0)
                        value = value[0] + "{{r|" + value[1] + "}}" + value[2];
                    else
                    if (valueNum < 3)
                        value = value[0] + "{{W|" + value[1] + "}}" + value[2];
                    else
                        value = value[0] + "{{g|" + value[1] + "}}" + value[2];
                }
                else
                if (value.Contains(TICK))
                    value = value[0] + "{{g|" + value[1] + "}}" + value[2];
                else
                if (value.Contains(CROSS))
                    value = value[0] + "{{r|" + value[1] + "}}" + value[2];
                else
                if (int.TryParse(key, out int corpseID))
                    key = "{{w|" + corpseID + "}}";
            }
            return Accumulator + value + " " + key;
        }
        public string DebugInternalsString(GameObject Corpse = null, string Joiner = "\n", bool Color = false)
            => DebugInternals(Corpse)
                ?.Aggregate("", func: (a, n) => ProcessDebugInternalKVP(a, n, Joiner, Color));

        public static int ConvertNullableBoolToInt(bool? NullableBool)
        {
            if (NullableBool == null)
                return 0;

            if (NullableBool.GetValueOrDefault())
                return 1;
            else
                return -1;
        }
        public static bool? ConvertIntToNullableBool(int Int)
            => Int == 0 ? null : Int > 0;

        public static int ConvertNullableKillerMemoryToInt(KillerMemory? NullableKillerMemory)
            => NullableKillerMemory == null ? -1 : (int)NullableKillerMemory.GetValueOrDefault();

        public static KillerMemory? ConvertIntToNullableKillerMemory(int Int)
            => Int >= 0 ? (KillerMemory)Int : null;

        public virtual void Write(SerializationWriter Writer)
        {
            Writer.WriteGameObject(Corpse);
            Writer.WriteOptimized(ConvertNullableBoolToInt(Killed));
            Writer.WriteOptimized(ConvertNullableKillerMemoryToInt(Killer));
            Writer.WriteOptimized(ConvertNullableBoolToInt(Method));
            Writer.Write(RudeToAsk);
        }
        public virtual void Read(SerializationReader Reader)
        {
            Corpse = Reader.ReadGameObject();
            Killed = ConvertIntToNullableBool(Reader.ReadOptimizedInt32());
            Killer = ConvertIntToNullableKillerMemory(Reader.ReadOptimizedInt32());
            Method = ConvertIntToNullableBool(Reader.ReadOptimizedInt32());
            RudeToAsk = Reader.ReadBoolean();
        }
    }
}
