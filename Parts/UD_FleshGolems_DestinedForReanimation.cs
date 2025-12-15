using System;
using System.Collections.Generic;

using ConsoleLib.Console;

using Qud.API;

using XRL.Core;
using XRL.Rules;
using XRL.UI;
using XRL.World.Effects;
using XRL.World.ObjectBuilders;
using XRL.World.Capabilities;
using XRL.World.Parts.Mutation;

using SerializeField = UnityEngine.SerializeField;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Events;
using static UD_FleshGolems.Const;
using UD_FleshGolems.Parts.VengeanceHelpers;
using System.Linq;
using System.Reflection;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_DestinedForReanimation : IScribedPart, IReanimateEventHandler
    {
        public static Dictionary<string, List<DeathDescription>> DeathCategoryDeathDescriptions => new()
        {
            {   // Heat damage w/ NoBurn (only steam)
                "cooked", new()
                {
                    new("cooked"),
                    new("cooked", Killed: "hard-boiled"),
                    new("cooked", Killed: "soft-boiled"),
                    new("cooked", Were: false, Killed: "broiled for our sins... ({{W|Ramen}})", Killer: "", Method: ""),
                    new("cooked", Killed: "cooked in a pot of broth", Method: ""),
                    new("cooked", Killed: "cooked in a pot of stew", Method: ""),
                    new("cooked", Were: false, Killed: "fell in a pot of broth", Killer: "", Method: ""),
                    new("cooked", Were: false, Killed: "fell in a pot of stew", Killer: "", Method: ""),
                    new("cooked", Were: false, Killed: "didn't realize the pot had come to a boil", Killer: "", Method: ""),
                }
            },
            {   // Heat damage w/o NoBurn
                "immolated", new()
                {
                    new("immolated"),
                    new("immolated", Killed: "scorched to death"),
                    new("immolated", Killed: "burned at the stake =subject.pastLife.byFaction.forHateReason=", Killer: "", Method: ""),
                    new("immolated", Were: false, Killed: "couldn't find a way to put out the fire", By: false, With: false),
                    new("immolated", Killed: "barbequed"),
                    new("immolated", Killed: "grilled"),
                    new("immolated", Killed: "more \"well-done\" than \"medium-rare\"", By: false, With: false),
                    new("immolated", Were: false, Killed: "had to leave the kitchen", By: false),
                    new("immolated", Were: false, Killed: "jumped out of the frying pan", Killer: "", Method: ""),
                    new("immolated", Were: false, Killed: "tried to befriend an elder flamebeard", Killer: "", Method: ""),
                }
            },
            {   // Plasma damage
                "plasma-burned to death", new()
                {
                    new("plasma-burned to death"),
                    new("plasma-burned to death", Killed: "deep fried"),
                    new("plasma-burned to death", Killed: "fried"),
                    new("plasma-burned to death", Were: false, Killed: "looked in the wrong end of a spacer rifle", Killer: "", Method: ""),
                    new("plasma-burned to death", Were: false, Killed: "fell into an astral forge", Killer: "", Method: ""),
                    new("plasma-burned to death", Killed: "knocked into an astral forge"),
                    new("plasma-burned to death", Were: false, Killed: "stared at the sun too long", Killer: "", Method: ""),
                    new("plasma-burned to death", Were: false, Killed: "caught a plasma grenade mkIII", By: false, Method: ""),
                    new("plasma-burned to death", Were: false, Killed: "mishandled a plasma grenade mkIII", Killer: "", Method: ""),
                }
            },
            {   // Cold damage
                "frozen to death", new()
                {
                    new("frozen to death"),
                    new("frozen to death", Killed: "snap-frozen"),
                    new("frozen to death", Killed: "flash frozen"),
                    new("frozen to death", Killed: "chilled to death"),
                    new("frozen to death", Killed: "locked inside a faulty cryo-tube", Method: ""),
                    new("frozen to death", Were: false, Killed: "fell through the ice", Killer: "", Method: ""),
                    new("frozen to death", Killed: "knocked through the ice"),
                    new("frozen to death", Were: false, Killed: "didn't hear the ice cracking", Killer: "", Method: ""),
                    new("immolated", Were: false, Killed: "tried to befriend an elder sleetbeard", Killer: "", Method: ""),
                }
            },
            {   // Electric damage
                "electrocuted", new()
                {
                    new("electrocuted"),
                    new("electrocuted", Killed: "zapped to death"),
                    new("electrocuted", Were: false, Killed: "tried to dig through a wired wall", Killer: "", Method: ""),
                    new("electrocuted", Were: false, Killed: "took a stun-rod to the =subject.bodyPart:face=", Killer: "", Method: ""),
                    new("electrocuted", Killed: "struck by lightning", Killer: "", Method: ""),
                    new("electrocuted", Killed: "overloaded", With: false, Method: "electrical discharge"),
                    new("electrocuted", Were: false, Killed: "licked a nuclear cell", Killer: "", Method: ""),
                    new("electrocuted", Were: false, Killed: "took too many volts"),
                }
            },
            {   // Thirst
                "thirst", new()
                {
                    new("thirst", Killed: "dessicated"),
                    new("thirst", Were: false, Killed: "ran out of fresh water", Killer: "", Method: ""),
                    new("thirst", Were: false, Killed: "forgot to drink", Killer: "", Method: ""),
                    new("thirst", Were: false, Killed: "tried to cross the Moghra'yi", Killer: "", Method: ""),
                    new("thirst", Were: false, Killed: "bought too many =uD_RandomItems=", By: false, Method: ""),
                    new("thirst", Were: false, Killed: "drank salt water", Killer: "", Method: ""),
                    new("thirst", Were: false, Killed: "performed the water-ritual one too many times", Killer: "", Method: ""),
                    new("thirst", Killed: "dried out"),
                }
            },
            {   // Poison damage
                "died of poison", new()
                {
                    new("died of poison", Killed: "poisoned"),
                    new("died of poison", Killed: "envenomed"),
                    new("died of poison", Killed: "tricked into consuming poison", Method: ""),
                    new("died of poison", Killed: "fed poison", Method: ""),
                    new("died of poison", Were: false, Killed: "tried to befriend an elder gallbeard"),
                    new("died of poison", Were: false, Killed: "drank green goo", Killer: "", Method: ""),
                    new("died of poison", Were: false, Killed: "left a wound to fester", Killer: "", Method: ""),
                    new("died of poison", Were: false, Killed: "ate the wrong kind of mushroom", Killer: "", Method: ""),
                    new("died of poison", Were: false, Killed: "didn't read the warning label", Killer: "", Method: ""),
                    new("died of poison", Were: false, Killed: "caught a poison gas grenade mkIII", By: false, Method: ""),
                    new("died of poison", Were: false, Killed: "mishandled a poison gas grenade mkIII", Killer: "", Method: ""),
                    new("died of poison", Were: false, Killed: "forgot to wear a gas mask", Killer: "", Method: ""),
                    new("died of poison", Were: false, Killed: "had my gas mask tampered with"),
                }
            },
            {   // Bleeding damage
                "bled to death", new()
                {
                    new("bled to death"),
                    new("bled to death", Killed: "exsanguinated"),
                    new("bled to death", Killed: "butchered"),
                    new("bled to death", Killed: "slashed to death"),
                    new("bled to death", Killed: "eviscerated"),
                    new("bled to death", Killed: "vivsected"),
                    new("bled to death", Were: false, Killed: "got too many paper-cuts", Killer: "", Method: ""),
                    new("bled to death", Were: false, Killed: "tried to remove a hangnail and went a little too far", Killer: "", Method: ""),
                    new("bled to death", Were: false, Killed: "had a nosebleed that just wouldn't stop", Killer: "", Method: ""),
                    new("bled to death", Were: false, Killed: "tried to befriend a leech", Killer: "", Method: ""),
                    new("bled to death", Were: false, Killed: "caught a falling =uD_RandomItem:inherits:BaseDagger=", Killer: "", Method: ""),
                    new("bled to death", Killed: "to slow to dodge a thrown =uD_RandomItem:inherits:BaseDagger=", By: false, Method: ""),
                    new("bled to death", Were: false, Killed: "swallowed a =uD_RandomItem:inherits:BaseDagger=", Killer: "", Method: ""),
                }
            },
            {   // Metabolic damage (hulk honey)
                "failed", new()
                {
                    new("failed", Were: false, Killed: "failed to metabolise a hulk honey fast enough", Killer: "", Method: ""),
                    new("failed", Were: false, Killed: "hulked out too hard", Killer: "", Method: ""),
                    new("failed", Were: false, Killed: "stayed mad too long", Killer: "", Method: ""),
                    new("failed", Were: false, Killed: "coped, seethed, and malded", Killing: "coping, seething, and malding", By: false, With: false),
                    new("failed", Were: false, Killed: "raged to death", Killer: "", Method: ""),
                    new("failed", Were: false, Killed: "used one too many hulk honey", Killer: "", Method: ""),
                    new("failed", Killed: "goaded into an uncontainable rage"),
                    new("failed", Killed: "too juiced up", By: false, With: false),
                }
            },
            {   // Asphyxiation damage (osseous ash)
                "died of asphyxiation", new()
                {
                    new("died of asphyxiation", Killed: "choked to death"),
                    new("died of asphyxiation", Killed: "suffocated"),
                    new("died of asphyxiation", Were: false, Killed: "inhaled too much osseous ash", Killer: "", Method: ""),
                    new("died of asphyxiation", Were: false, Killed: "tried to breathe underwater", Killer: "", Method: ""),
                    new("died of asphyxiation", Were: false, Killed: "held underwater"),
                }
            },
            {   // Killed
                "killed", new()
                {
                    new("killed"),
                    new("killed", Killed: "killed in a duel"),
                    new("killed", Killed: "thoroughly ended"),
                    new("killed", Killed: "wasted"),
                    new("killed", Killed: "merced"),
                    new("killed", Killed: "taken out"),
                }
            },
        };
        // Keys list lifted from books' https://codeberg.org/librarianmage/EloquentDeath
        // which you should check out for being awesome.
        public static Dictionary<string, List<string>> DeathCategoryDeathMessages => new()
        {
            // Heat damage w/ NoBurn (only steam)
            {
                "cooked", new()
                {
                    "=subject.verb:were:afterpronoun= cooked",
                    "=subject.verb:were:afterpronoun= hard-boiled",
                    "=subject.verb:were:afterpronoun= soft-boiled",
                    "broiled for our sins... ({{W|Ramen}})",
                    "=subject.verb:were:afterpronoun= flash boiled in steam",
                    "fell in a pot of broth",
                    "fell in a pot of stew",
                    "didn't realize the pot had come to a boil",
                }
            },
            // Heat damage w/o NoBurn
            {
                "immolated", new()
                {
                    "=subject.verb:were:afterpronoun= immolated",
                    "=subject.verb:were:afterpronoun= scorched to death",
                    "couldn't find a way to put =subject.reflexive= out",
                    "=subject.verb:were:afterpronoun= barbequed",
                    "=subject.verb:were:afterpronoun= grilled",
                    "=subject.verb:were:afterpronoun= more \"well-done\" than \"medium-rare\"",
                    "had to leave the kitchen",
                    "jumped out of the frying pan",
                }
            },
            // Plasma damage
            {
                "plasma-burned to death", new()
                {
                    "=subject.verb:were:afterpronoun= plasma-burned to death",
                    "=subject.verb:were:afterpronoun= deepfried",
                    "=subject.verb:were:afterpronoun= fried",
                    "looked in the wrong end of a spacer rifle",
                    "fell into an astral forge",
                }
            },
            // Cold damage
            {
                "frozen to death", new()
                {
                    "=subject.verb:were:afterpronoun= frozen to death",
                    "=subject.verb:were:afterpronoun= snap frozen",
                    "=subject.verb:were:afterpronoun= flash frozen",
                    "=subject.verb:were:afterpronoun= chilled to death",
                }
            },
            // Electric damage
            {
                "electrocuted", new()
                {
                    "=subject.verb:were:afterpronoun= electrocuted",
                    "=subject.verb:were:afterpronoun= zapped to death",
                }
            },
            // Thirst
            {
                "thirst", new()
                {
                    "thirst to death",
                    "=subject.verb:were:afterpronoun= dessicated",
                }
            },
            // Poison damage
            {
                "died of poison", new()
                {
                    "died of poison",
                }
            },
            // Bleeding damage
            {
                "bled to death", new()
                {
                    "bled to death",
                    "=subject.verb:were:afterpronoun= exsanguinated",
                }
            },
            // Metabolic damage (hulk honey)
            {
                "failed", new()
                {
                    "hulked out way too hard",
                }
            },
            // Asphyxiation damage (osseous ash)
            {
                "died of asphyxiation", new()
                {
                    "died of asphyxiation",
                    "=subject.verb:were:afterpronoun= asphyxiated",
                }
            },
            // Psionic damage
            {
                "psychically extinguished", new()
                {
                    "=subject.verb:were:afterpronoun= psychically extinguished",
                }
            },
            // Drain damage (syphon vim)
            {
                "drained to extinction", new()
                {
                    "=subject.verb:were:afterpronoun= drained to extinction",
                }
            },
            // Thorns damage
            {
                "pricked to death", new()
                {
                    "=subject.verb:were:afterpronoun= pricked to death",
                }
            },
            // Bite damage (any bite)
            {
                "bitten to death", new()
                {
                    "=subject.verb:were:afterpronoun= bitten to death",
                }
            },
        };

        public static bool HaveFakedDeath = false;

        private static bool IfPlayerStartUndeadUseTurnTickNotStringy => false;

        public GameObject Corpse;

        public bool BuiltToBeReanimated;

        public bool Attempted;

        public bool DelayTillZoneBuild;

        [SerializeField]
        private List<int> FailedToRegisterEvents;

        public bool PlayerWantsFakeDie;

        public UD_FleshGolems_DestinedForReanimation()
        {
            Corpse = null;
            BuiltToBeReanimated = false;
            Attempted = false;
            FailedToRegisterEvents = new();
            PlayerWantsFakeDie = false;
        }

        public static bool FakeDeath(
            GameObject Dying,
            GameObject Killer = null,
            GameObject Weapon = null,
            GameObject Projectile = null,
            bool Accidental = false,
            bool AlwaysUsePopups = false,
            string Category = null,
            string KillerText = null,
            string Reason = null,
            string ThirdPersonReason = null,
            bool DoFakeMessage = true,
            bool DoJournal = true,
            bool DoAchievement = false,
            IRenderable RelentlessIcon = null,
            string RelentlessTitle = null,
            DeathDescription DeathDescription = null)
        {
            if (Dying == null)
            {
                return false;
            }

            if (DeathDescription != null)
            {
                Category = DeathDescription.Category;
                Reason = DeathDescription.Reason(Accidental);
                ThirdPersonReason = DeathDescription.ThirdPersonReason(Accidental);
            }

            AfterDieEvent.Send(
                Dying: Dying,
                Killer: Killer,
                Weapon: Weapon,
                Projectile: Projectile,
                Accidental: Accidental,
                AlwaysUsePopups: AlwaysUsePopups,
                KillerText: KillerText,
                Reason: Reason,
                ThirdPersonReason: ThirdPersonReason);

            Dying.StopMoving();

            KilledPlayerEvent.Send(
                Dying: Dying,
                Killer: Killer,
                Weapon: Weapon,
                Projectile: Projectile,
                Accidental: Accidental,
                AlwaysUsePopups: AlwaysUsePopups,
                KillerText: KillerText,
                Reason: Reason,
                ThirdPersonReason: ThirdPersonReason);

            string deathMessageTitle = "You died.";
            string deathMessage = "=subject.Subjective= " + (Reason ?? The.Game.DeathReason) + ".";
            if (DeathDescription != null)
            {
                deathMessage = ThirdPersonReason;
            }
            string deathCategory = Category ?? The.Game.DeathCategory;
            Renderable deathIcon = null;
            Dictionary<string, Renderable> deathIcons = CheckpointingSystem.deathIcons;

            if (UI.Options.GetOptionBool("Books_EloquentDeath_EnableEloquentDeathMessage"))
            {
                deathMessageTitle = "You became a cord in time's silly carpet.";
            }

            if (!deathCategory.IsNullOrEmpty() && deathIcons.ContainsKey(deathCategory))
            {
                deathIcon = deathIcons[deathCategory];
            }

            if (DoFakeMessage && (Dying.IsPlayer() || Dying.Blueprint.IsPlayerBlueprint()))
            {
                deathMessage = deathMessage
                    .StartReplace()
                    .AddObject(Dying)
                    .AddObject(Killer)
                    .ToString();

                Popup.ShowSpace(
                    Message: deathMessage,
                    Title: deathMessageTitle,
                    Sound: "Sounds/UI/ui_notification_death",
                    AfterRender: deathIcon,
                    LogMessage: true,
                    ShowContextFrame: deathIcon != null,
                    PopupID: "DeathMessage");

                string andYetMsg = "... and yet...";
                string msgSpaces = "=ud_nbsp:12="
                    .StartReplace()
                    .ToString();
                string notRelentMsg = "...=subject.refname= =subject.verb:don't:afterpronoun= {{UD_FleshGolems_reanimated|relent}}..."
                    .StartReplace()
                    .AddObject(Dying)
                    .ToString();

                string fullMsg = andYetMsg + "\n\n" + msgSpaces + notRelentMsg;

                RelentlessIcon ??= Dying.RenderForUI();
                
                Popup.ShowSpace(
                    Message: fullMsg,
                    Title: RelentlessTitle,
                    AfterRender: new(RelentlessIcon),
                    LogMessage: true,
                    ShowContextFrame: deathIcon != null,
                    PopupID: "UndeathMessage");
                /*
                Popup.ShowSpace(
                    Message: notRelentMsg,
                    Title: andYetMsg,
                    AfterRender: new(RelentlessIcon),
                    LogMessage: true,
                    ShowContextFrame: deathIcon != null,
                    PopupID: "UndeathMessage");
                */
            }

            string deathReason = deathMessage;
            if (!deathReason.IsNullOrEmpty())
            {
                deathReason = deathReason[0].ToString().ToLower() + deathReason[1..];
            }
            if (DoJournal
                && !deathReason.IsNullOrEmpty()
                && (Dying.IsPlayer() || Dying.Blueprint.IsPlayerBlueprint()))
            {
                // Died
                JournalAPI.AddAccomplishment(
                    text: "On the " + Calendar.GetDay() + " of " + Calendar.GetMonth() + ", " + deathReason?.Replace("!", "."),
                    muralText: "",
                    gospelText: "");

                // Came back
                JournalAPI.AddAccomplishment(
                    text: "On the " + Calendar.GetDay() + " of " + Calendar.GetMonth() + ", " +
                        "you returned from the great beyond.",
                    muralText: "O! Fancieth way to say! =Name= hatheth returned whence the thin-veil twixt living and the yonder!",
                    gospelText: "=Name= just, sorta... woke back up from dying...");
            }

            if (DoAchievement
                && (Dying.IsPlayer() || Dying.Blueprint.IsPlayerBlueprint()))
            {
                Achievement.DIE.Unlock();
            }

            WeaponUsageTracking.TrackKill(
                Actor: Killer,
                Defender: Dying,
                Weapon: Weapon,
                Projectile: Projectile,
                Accidental: Accidental);

            DeathEvent.Send(
                Dying: Dying,
                Killer: Killer,
                Weapon: Weapon,
                Projectile: Projectile,
                Accidental: Accidental,
                AlwaysUsePopups: AlwaysUsePopups,
                KillerText: KillerText,
                Reason: Reason,
                ThirdPersonReason: ThirdPersonReason);

            return true;
        }
        public static bool FakeDeath(
            GameObject Dying,
            IDeathEvent E,
            bool DoFakeMessage = true,
            bool DoJournal = true,
            bool DoAchievement = false,
            IRenderable RelentlessIcon = null,
            string RelentlessTitle = null)
        {
            return FakeDeath(
                Dying: Dying,
                Killer: E?.Killer,
                Weapon: E?.Weapon,
                Projectile: E?.Projectile,
                Accidental: E == null || E.Accidental,
                AlwaysUsePopups: E == null || E.AlwaysUsePopups,
                KillerText: E?.KillerText,
                Reason: E?.Reason,
                ThirdPersonReason: E?.ThirdPersonReason,
                DoFakeMessage: DoFakeMessage,
                DoJournal: DoJournal,
                DoAchievement: DoAchievement,
                RelentlessIcon: RelentlessIcon,
                RelentlessTitle: RelentlessTitle);
        }
        public bool FakeDeath(IDeathEvent E)
        {
            return FakeDeath(ParentObject, E);
        }
        public bool FakeDeath()
        {
            return FakeDeath(null);
        }

        public static void RandomDeathCategoryAndReasonAndAccidental(out string Category, out string Reason, out bool Accidental)
        {
            Category = DeathCategoryDeathMessages
                    ?.Where(kvp => !kvp.Value.All(entry => entry.IsNullOrEmpty()))
                    ?.GetRandomElementCosmetic().Key
                ?? CheckpointingSystem.deathIcons
                    ?.Keys
                    ?.GetRandomElement();

            Reason = DeathCategoryDeathMessages[Category]
                ?.Where(s => !s.IsNullOrEmpty())
                ?.ToList()
                ?.GetRandomElementCosmetic();

            Accidental = Stat.RollCached("1d3") == 1;
        }

        public static void RandomDeathDescriptionAndAccidental(out DeathDescription DeathDescription, out bool Accidental, Predicate<DeathDescription> Filter = null)
        {
            DeathDescription = DeathCategoryDeathDescriptions
                ?.Aggregate(
                    seed: new List<DeathDescription>(),
                    func: delegate (List<DeathDescription> acc, KeyValuePair<string, List<DeathDescription>> next)
                    {
                        foreach (DeathDescription deathDescription in next.Value)
                            if (Filter == null || Filter(deathDescription))
                                acc.AddIf(deathDescription, item => !acc.Contains(item));

                        return acc;
                    })
                ?.GetRandomElementCosmetic();

            Accidental = Stat.RollCached("1d3") == 1;
        }

        public static KillerDetails ProduceKillerDetails(
            out GameObject Killer,
            out GameObject Weapon,
            out GameObject Projectile,
            out string Category,
            out string Reason,
            out bool Accidental,
            out bool KillerIsCached,
            int ChanceRandomKiller = 50)
        {
            Killer = null;
            Weapon = null;
            Projectile = null;
            Category = null;
            Reason = null;
            KillerIsCached = false;
            if (ChanceRandomKiller.in100())
            {
                if (50.in100())
                {
                    List<GameObject> cachedObjects = Event.NewGameObjectList(The.ZoneManager.CachedObjects.Values)
                        ?.Where(GO => (GO.HasPart<Combat>() && GO.HasPart<Body>()) || GO.HasTagOrProperty("BodySubstitute"))
                        ?.ToList();
                    if (cachedObjects?.GetRandomElement() is GameObject cachedKiller)
                    {
                        KillerIsCached = true;
                        Killer = cachedKiller;

                        Weapon = Killer.GetMissileWeapons()
                            ?.GetRandomElementCosmetic()
                            ?? Killer.GetPrimaryWeapon();
                    }
                }
                else
                if (!1.in10())
                {
                    Killer = GameObject.CreateSample(EncountersAPI.GetACreatureBlueprint());
                }
                else
                {
                    Killer = HeroMaker.MakeHero(GameObject.CreateSample(EncountersAPI.GetALegendaryEligibleCreatureBlueprint()));
                }
            }
            if (Killer != null
                && !KillerIsCached)
            {
                GameObjectBlueprint weaponBlueprint = EncountersAPI.GetAnItemBlueprintModel(
                    bp => (bp.InheritsFrom("MeleeWeapon") && !bp.InheritsFrom("Projectile"))
                    || bp.InheritsFrom("BaseMissileWeapon")
                    || bp.InheritsFrom("BaseThrownWeapon"));

                Weapon = GameObject.CreateSample(weaponBlueprint.Name);
            }
            if (Weapon != null
                && Weapon.HasPart<MissileWeapon>())
            {
                if (GetProjectileBlueprintEvent.GetFor(Weapon) is string projectileBlueprint)
                {
                    Projectile = GameObject.CreateSample(projectileBlueprint);
                }
            }
            bool haveKiller = Killer != null;
            bool haveWeapon = Weapon != null;
            bool haveProjectile = Projectile != null;
            bool haveMethod = haveWeapon || haveProjectile;
            bool MatchesSpec(DeathDescription DeathDescription)
            {
                if (haveKiller && DeathDescription.Killer == "")
                    return false;

                if (haveMethod && DeathDescription.Method == "")
                    return false;

                return true;
            }
            RandomDeathDescriptionAndAccidental(out DeathDescription deathDescription, out Accidental, MatchesSpec);

            Category = deathDescription.Category;
            Reason = deathDescription.Reason(Accidental);

            return new(Killer, Weapon, deathDescription, Accidental);
        }
        public static KillerDetails ProduceKillerDetails()
        {
            KillerDetails output = ProduceKillerDetails(
                Killer: out GameObject killer,
                Weapon: out GameObject weapon,
                Projectile: out GameObject projectile,
                Category: out _,
                Reason: out _,
                Accidental: out _,
                KillerIsCached: out bool killerIsCached);

            if (!killerIsCached)
            {
                killer?.Obliterate();
                weapon?.Obliterate();
            }
            projectile?.Obliterate();

            return output;
        }

        public static bool FakeRandomDeath(
            GameObject Dying,
            out KillerDetails KillerDetails,
            int ChanceRandomKiller = 50,
            bool DoAchievement = false,
            bool RequireKillerDetails = false,
            IRenderable RelentlessIcon = null,
            string RelentlessTitle = null)
        {
            GameObject killer = null;
            GameObject weapon = null;
            GameObject projectile = null;
            KillerDetails = null;
            bool killerIsCached = false;
            try
            {
                KillerDetails = ProduceKillerDetails(
                    Killer: out killer,
                    Weapon: out weapon,
                    Projectile: out projectile,
                    Category: out string category,
                    Reason: out string reason,
                    Accidental: out bool accidental,
                    KillerIsCached: out killerIsCached,
                    ChanceRandomKiller: ChanceRandomKiller);

                bool deathFaked = FakeDeath(
                    Dying: Dying,
                    Killer: killer,
                    Weapon: weapon,
                    Projectile: projectile,
                    Accidental: accidental,
                    Category: category,
                    Reason: reason,
                    ThirdPersonReason: reason,
                    DoAchievement: DoAchievement,
                    RelentlessIcon: RelentlessIcon,
                    RelentlessTitle: RelentlessTitle);

                if (!deathFaked)
                {
                    KillerDetails = null;
                }

                if (!killerIsCached)
                {
                    killer?.Obliterate();
                    weapon?.Obliterate();
                }
                projectile?.Obliterate();

                if (RequireKillerDetails
                    && Dying.IsCorpse()
                    && Dying.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper reanimationHelper))
                {
                    reanimationHelper.KillerDetails ??= KillerDetails;
                }

                return deathFaked;
            }
            finally
            {
                if (!killerIsCached)
                {
                    if (GameObject.Validate(ref killer))
                        killer.Obliterate();

                    if (GameObject.Validate(ref weapon))
                        weapon.Obliterate();
                }
                if (GameObject.Validate(ref projectile))
                    projectile.Obliterate();
            }
        }
        public bool FakeRandomDeath(out KillerDetails KillerDetails, int ChanceRandomKiller = 50, bool DoAchievement = false)
        {
            return FakeRandomDeath(
                Dying: ParentObject,
                out KillerDetails,
                ChanceRandomKiller: ChanceRandomKiller,
                DoAchievement: DoAchievement);
        }
        public bool FakeRandomDeath(int ChanceRandomKiller = 50, bool DoAchievement = false)
        {
            return FakeRandomDeath(
                out _,
                ChanceRandomKiller: ChanceRandomKiller,
                DoAchievement: DoAchievement);
        }

        public bool ActuallyDoTheFakeDieAndReanimate()
        {
            if (ParentObject == null
                || !PlayerWantsFakeDie
                || HaveFakedDeath)
            {
                return false;
            }
            bool success = UD_FleshGolems_Reanimated.ReplaceEntityWithCorpse(
                Entity: ParentObject,
                FakeDeath: PlayerWantsFakeDie,
                FakedDeath: out HaveFakedDeath,
                DeathEvent: null,
                Corpse: Corpse);

            PlayerWantsFakeDie = false;
            return success;
        }

        public bool ProcessObjectCreationEvent(IObjectCreationEvent E)
        {
            if (!Attempted
                && BuiltToBeReanimated
                && !DelayTillZoneBuild
                && ParentObject is GameObject soonToBeCorpse
                && soonToBeCorpse == E.Object
                && Corpse is GameObject soonToBeCreature)
            {
                if (soonToBeCreature.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper reanimationHelper))
                {
                    if (!soonToBeCorpse.IsPlayer()
                        && !soonToBeCorpse.Blueprint.IsPlayerBlueprint())
                    {
                        reanimationHelper.Animate();
                        E.ReplacementObject = soonToBeCreature;
                        Attempted = true;
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool AllowStaticRegistration()
            => true;

        public override bool WantTurnTick()
        {
            return IfPlayerStartUndeadUseTurnTickNotStringy;
        }
        public override void TurnTick(long TimeTick, int Amount)
        {
            ActuallyDoTheFakeDieAndReanimate();
            base.TurnTick(TimeTick, Amount);
        }
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            int eventOrder = EventOrder.EXTREMELY_EARLY + EventOrder.EXTREMELY_EARLY;
            try
            {
                Registrar?.Register(BeforeObjectCreatedEvent.ID, eventOrder);
                // Registrar?.Register(AfterObjectCreatedEvent.ID, eventOrder);
                Registrar?.Register(EnvironmentalUpdateEvent.ID, eventOrder);
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(UD_FleshGolems_DestinedForReanimation) + "." + nameof(Register), x, "game_mod_exception");
            }
            finally
            {
                if (ParentObject == null
                    || ParentObject.RegisteredEvents == null
                    || !ParentObject.RegisteredEvents.ContainsKey(BeforeObjectCreatedEvent.ID))
                {
                    FailedToRegisterEvents.Add(BeforeObjectCreatedEvent.ID);
                }
                if (ParentObject == null
                    || ParentObject.RegisteredEvents == null
                    || !ParentObject.RegisteredEvents.ContainsKey(AfterObjectCreatedEvent.ID))
                {
                    // FailedToRegisterEvents.Add(AfterObjectCreatedEvent.ID);
                }
                if (ParentObject == null
                    || ParentObject.RegisteredEvents == null
                    || !ParentObject.RegisteredEvents.ContainsKey(EnvironmentalUpdateEvent.ID))
                {
                    FailedToRegisterEvents.Add(EnvironmentalUpdateEvent.ID);
                }
            }
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || (ID == BeforeObjectCreatedEvent.ID && FailedToRegisterEvents.Contains(BeforeObjectCreatedEvent.ID))
                // || (ID == AfterObjectCreatedEvent.ID && FailedToRegisterEvents.Contains(AfterObjectCreatedEvent.ID))
                || (ID == EnvironmentalUpdateEvent.ID && FailedToRegisterEvents.Contains(EnvironmentalUpdateEvent.ID))
                || (ID == BeforeZoneBuiltEvent.ID && DelayTillZoneBuild)
                || ID == GetShortDescriptionEvent.ID
                || ID == BeforeDieEvent.ID
                || ID == GetDebugInternalsEvent.ID
                ;
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            string persistanceText =
                ("Something about =subject.objective= gives the sense " +
                "=subject.subjective==subject.verb:'re:afterpronoun= unnaturally relentless...")
                    .StartReplace()
                    .AddObject(E.Object)
                    .ToString();

            if (E.Object.HasTag("VerseDescription"))
            {
                E.Base.AppendLine().AppendLine().Append(persistanceText);
            }
            else
            {
                if (!E.Base.IsNullOrEmpty())
                {
                    E.Base.Append(" ");
                }
                E.Base.Append(persistanceText);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnvironmentalUpdateEvent E)
        {
            if (!Attempted
                && BuiltToBeReanimated
                && !DelayTillZoneBuild
                && ParentObject is GameObject soonToBeCorpse)
            {
                if ((soonToBeCorpse.Blueprint.IsPlayerBlueprint() || soonToBeCorpse.IsPlayer())
                    && !HaveFakedDeath
                    && (Corpse != null || UD_FleshGolems_Reanimated.TryProduceCorpse(soonToBeCorpse, out Corpse)))
                {
                    soonToBeCorpse.RegisterPartEvent(this, "GameStart");
                }
                else
                if (!soonToBeCorpse.IsPlayer())
                {   
                    // Attempted = true;
                    // ReplaceInContextEvent.Send(soonToBeCorpse, Corpse);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeObjectCreatedEvent E)
        {
            ProcessObjectCreationEvent(E);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AfterObjectCreatedEvent E)
        {
            ProcessObjectCreationEvent(E);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeZoneBuiltEvent E)
        {
            if (!Attempted
                && BuiltToBeReanimated
                && DelayTillZoneBuild
                && ParentObject is GameObject soonToBeCorpse
                && soonToBeCorpse.CurrentZone == E.Zone
                && !soonToBeCorpse.IsPlayer()
                && !soonToBeCorpse.Blueprint.IsPlayerBlueprint()
                // && UD_FleshGolems_Reanimated.TryProduceCorpse(soonToBeCorpse, out Corpse)
                // && Corpse is GameObject soonToBeCreature
                // && soonToBeCreature.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper reanimationHelper)
                && false
                )
            {
                using Indent indent = new(1);
                Debug.LogMethod(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(BeforeZoneBuiltEvent)),
                        Debug.Arg(nameof(soonToBeCorpse), soonToBeCorpse?.DebugName ?? NULL),
                        // Debug.Arg(nameof(soonToBeCreature), soonToBeCreature?.DebugName ?? NULL),
                    });
                // bool reanimated = reanimationHelper.Animate();
                bool reanimated = UD_FleshGolems_Reanimated.ReplaceEntityWithCorpse(soonToBeCorpse, Corpse: ref Corpse);
                // ReplaceInContextEvent.Send(soonToBeCorpse, Corpse);
                Attempted = true;
                Debug.YehNah((reanimated ? "Success" : "Fail") + "!", reanimated, indent[1]);
            }
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (E.ID == "GameStart"
                && Corpse != null
                && !HaveFakedDeath)
            {
                PlayerWantsFakeDie = true;
                if (!IfPlayerStartUndeadUseTurnTickNotStringy)
                {
                    ActuallyDoTheFakeDieAndReanimate();
                }
            }
            return base.FireEvent(E);
        }
        public override bool HandleEvent(BeforeDieEvent E)
        {
            if (!BuiltToBeReanimated
                && ParentObject is GameObject dying
                && dying == E.Dying
                && dying.TryGetPart(out Corpse dyingCorpse)
                && !dyingCorpse.CorpseBlueprint.IsNullOrEmpty()
                && dying.IsPlayer()
                && (!PlayerWantsFakeDie || !HaveFakedDeath)
                && UD_FleshGolems_Reanimated.ReplaceEntityWithCorpse(
                    Entity: ParentObject,
                    FakeDeath: PlayerWantsFakeDie,
                    FakedDeath: out HaveFakedDeath,
                    DeathEvent: E,
                    Corpse: Corpse))
            {
                return false;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            E.AddEntry(this, nameof(Corpse), Corpse?.DebugName ?? NULL);
            E.AddEntry(this, nameof(BuiltToBeReanimated), BuiltToBeReanimated);
            E.AddEntry(this, nameof(Attempted), Attempted);
            E.AddEntry(this, nameof(DelayTillZoneBuild), DelayTillZoneBuild);
            if (!FailedToRegisterEvents.IsNullOrEmpty())
            {
                E.AddEntry(this, nameof(FailedToRegisterEvents),
                    FailedToRegisterEvents
                    ?.ConvertAll(id => MinEvent.EventTypes.ContainsKey(id) ? MinEvent.EventTypes[id].ToString() : "Error")
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            }
            else
            {
                E.AddEntry(this, nameof(FailedToRegisterEvents), "Empty");
            }
            E.AddEntry(this, nameof(PlayerWantsFakeDie), PlayerWantsFakeDie);
            return base.HandleEvent(E);
        }
    }
}