using System;
using System.Collections.Generic;
using System.Linq;

using ConsoleLib.Console;

using Qud.API;

using XRL.Rules;
using XRL.UI;
using XRL.World.Capabilities;

using static XRL.World.ObjectBuilders.UD_FleshGolems_Reanimated;

using SerializeField = UnityEngine.SerializeField;

using UD_FleshGolems;
using UD_FleshGolems.Events;
using UD_FleshGolems.Parts.VengeanceHelpers;

using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_DestinedForReanimation : IScribedPart, IReanimateEventHandler
    {
        public static int AccidentalChanceOneIn = 3;
        // Keys list lifted from books' https://codeberg.org/librarianmage/EloquentDeath
        // which you should check out for being awesome.
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
                    new("immolated", Were: false, Killed: "had to leave the kitchen", Killer: "", Method: ""),
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
                    new("electrocuted", Were: false, Killed: "took a stun-rod to the =subject.bodyPart.NoCase:face=", Killer: "", Method: ""),
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
                    new("bled to death", Killed: "too slow to dodge a thrown =uD_RandomItem:inherits:BaseDagger=", By: false, Method: ""),
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
                    new("died of asphyxiation", Killed: "held underwater"),
                    new("died of asphyxiation", Were: false, Killed: "forgot to inhale again after the last exhale", Killer: "", Method: ""),
                    new("died of asphyxiation", Were: false, Killed: "ran out of air", By: false),
                }
            },
            {   // Psionic damage
                "psychically extinguished", new()
                {
                    new("psychically extinguished"),
                    new("psychically extinguished", Killed: "commanded to die", Method: ""),
                    new("psychically extinguished", Killed: "compelled to simply die", Method: ""),
                    new("psychically extinguished", Killed: "convinced to die", Method: ""),
                    new("psychically extinguished", Killed: "convinced to embrace death", Method: ""),
                    new("psychically extinguished", Killed: "sundered dead", Method: ""),
                    new("psychically extinguished", Killed: "mentally obliterated", Method: ""),
                    new("psychically extinguished", Were: false, Killed: "couldn't maintain a strong enough sense of self", Killer: "", Method: ""),
                    new("psychically extinguished", Were: false, Killed: "beheld the breadth of the psychic sea", Method: ""),
                    new("psychically extinguished", Killed: "shown the breadth of the psychic sea", Killer: "Ptoh", Method: ""),
                    new("psychically extinguished", Were: false, Killed: "tried to understand things better left a mystery", Method: ""),
                    new("psychically extinguished", Were: false, Killed: "dared to challenge Ptoh and, well...", Killer: "", Method: ""),
                    new("psychically extinguished", Were: false, Killed: "risked the proximity of a darkling star", Killer: "", Method: ""),
                    new("psychically extinguished", Were: false, Killed: "sought oblivion too keenly", Killer: "", Method: ""),
                    new("psychically extinguished", Were: false, Killed: "had a shattered mental mirror", Killer: "", Method: ""),
                }
            },
            {   // Drain damage (syphon vim)
                "drained to extinction", new()
                {
                    new("drained to extinction"),
                    new("drained to extinction", Killed: "absorbed"),
                    new("drained to extinction", Were: false, Killed: "went =subject.bodyPart.NoCase:head=-to-head with a leech", Killer: "", Method: ""),
                    new("drained to extinction", Killed: "drained of all vital essence", Method: ""),
                    new("drained to extinction", Killed: "syphoned to a husk"),
                    new("drained to extinction", Were: false, Killed: "didn't notice the stat-saps", Killer: "", Method: ""),
                    new("drained to extinction", Were: false, Killed: "didn't notice the leeches", Killer: "", Method: ""),
                }
            },
            {   // Thorns damage
                "pricked to death", new()
                {
                    new("pricked to death"),
                    new("pricked to death", Killed: "pin-cushioned"),
                    new("pricked to death", Killed: "skewered"),
                    new("pricked to death", Were: false, Killed: "sat on a junk dollar", Killer: "", Method: ""),
                    new("pricked to death", Killed: "shoved onto a junk dollar"),
                    new("pricked to death", Were: false, Killed: "fell on top of an urshiib", Killer: "", Method: ""),
                    new("pricked to death", Were: false, Killed: "made the wrong urshiib angry", Killer: "", Method: ""),
                    new("pricked to death", Were: false, Killed: "got the thorns", By: false, Method: ""),
                }
            },
            {   // Bite damage (any bite)
                "bitten to death", new()
                {
                    new("bitten to death"),
                    new("bitten to death", Killed: "chewed on"),
                    new("bitten to death", Killed: "half-eaten"),
                    new("bitten to death", Killed: "chomped"),
                    new("bitten to death", Were: false, Killed: "got too close to a salt kraken", Killer: "", Method: ""),
                    new("bitten to death", Were: false, Killed: "tried to swim with a madpole", Killer: "", Method: ""),
                    new("bitten to death", Were: false, Killed: "upset some cannibals", Killer: "", Method: ""),
                }
            },
            {   // Killed
                "killed", new()
                {
                    new("killed"),
                    new("killed", Killed: "killed in a duel"),
                    new("killed", Killed: "thoroughly ended"),
                    new("killed", Killed: "ended"),
                    new("killed", Killed: "{{R|wasted}}"),
                    new("killed", Killed: "merced"),
                    new("killed", Killed: "taken out"),
                    new("killed", Killed: "taken down"),
                    new("killed", Killed: "put down"),
                    new("killed", Killed: "knocked off"),
                    new("killed", Killed: "run through"),
                }
            },
        };

        public static bool HaveFakedDeath = false;

        public GameObject Corpse;

        public bool BuiltToBeReanimated;

        public bool Attempted;

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
                return false;

            if (DeathDescription != null)
            {
                Category = DeathDescription.Category;
                Reason = DeathDescription.Reason(Accidental);
                ThirdPersonReason = DeathDescription.ThirdPersonReason(Accidental: Accidental);
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
                deathMessage = ThirdPersonReason + ".";

            string deathCategory = Category ?? The.Game.DeathCategory;
            Renderable deathIcon = null;
            Dictionary<string, Renderable> deathIcons = CheckpointingSystem.deathIcons;

            if (UI.Options.GetOptionBool("Books_EloquentDeath_EnableEloquentDeathMessage"))
                deathMessageTitle = "You became a cord in time's silly carpet.";

            if (!deathCategory.IsNullOrEmpty() && deathIcons.ContainsKey(deathCategory))
                deathIcon = deathIcons[deathCategory];

            if (DoFakeMessage
                && (Dying.IsPlayer()
                    || Dying.IsPlayerDuringWorldGen()))
            {
                deathMessage = deathMessage
                    ?.StartReplace()
                    ?.AddObject(Dying)
                    ?.AddObject(Killer)
                    ?.ToString()
                    ?.Capitalize();

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
                
                Popup.ShowSpace(
                    Message: "... some time passes... ",
                    LogMessage: true,
                    PopupID: "TimePassMessage");
            }

            string deathReason = deathMessage;
            if (!deathReason.IsNullOrEmpty())
                deathReason = deathReason.Uncapitalize();

            if (DoJournal
                && !deathReason.IsNullOrEmpty()
                && (Dying.IsPlayer() || Dying.IsPlayerDuringWorldGen()))
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
                && (Dying.IsPlayer() 
                    || Dying.IsPlayerDuringWorldGen()))
                Achievement.DIE.Unlock();

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
            => FakeDeath(ParentObject, E);

        public bool FakeDeath()
            => FakeDeath(null);

        public static void RandomDeathDescriptionAndAccidental(
            GameObject For,
            out DeathDescription DeathDescription,
            out bool Accidental,
            Predicate<DeathDescription> Filter = null)
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
                ?.GetRandomElementCosmetic()
                ?.Copy();

            if (DeathDescription != null)
                DeathDescription.Killed = DeathDescription?.Killed
                    ?.StartReplace()
                    ?.AddObject(For)
                    ?.ToString();

            Accidental = Stat.RollCached("1d" + AccidentalChanceOneIn) == 1;
        }

        public static DeathDescription ProduceRandomDeathDescriptionWithComponents(
            GameObject For,
            out GameObject Killer,
            out GameObject Weapon,
            out GameObject Projectile,
            out string Category,
            out string Reason,
            out bool Accidental,
            out bool KillerIsCached,
            int ChanceRandomKiller = 50,
            Predicate<DeathDescription> Filter = null)
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
                && Weapon.HasPart<MissileWeapon>()
                && GetProjectileBlueprintEvent.GetFor(Weapon) is string projectileBlueprint)
                Projectile = GameObject.CreateSample(projectileBlueprint);

            bool haveKiller = Killer != null;
            bool haveWeapon = Weapon != null;
            bool haveProjectile = Projectile != null;
            bool haveMethod = haveWeapon || haveProjectile;
            bool MatchesSpec(DeathDescription DeathDescription)
            {
                if (For.GetPropertyOrTag("UD_FleshGolems DeathDetails DeathDescription Category") is string categoryPropTag
                    && categoryPropTag.CachedCommaExpansion()?.ToArray() is string[] categories
                    && !DeathDescription.Category.EqualsAny(categories))
                    return false;

                if (Filter != null && !Filter(DeathDescription))
                    return false;

                if (haveKiller && DeathDescription.Killer == "")
                    return false;

                if (haveMethod && DeathDescription.Method == "")
                    return false;

                return true;
            }
            RandomDeathDescriptionAndAccidental(For, out DeathDescription deathDescription, out Accidental, MatchesSpec);

            Category = deathDescription.Category;
            Reason = deathDescription.Reason(Accidental);

            deathDescription
                .SetKiller(Killer)
                .SetMethod(Weapon)
                .SetMethodFallback(Projectile);

            return deathDescription;
        }
        public static DeathDescription ProduceRandomDeathDescription(
            GameObject For,
            int ChanceRandomKiller = 50,
            Predicate<DeathDescription> Filter = null)
            => ProduceRandomDeathDescriptionWithComponents(
                For: For,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                ChanceRandomKiller: ChanceRandomKiller,
                Filter: Filter);

        public UD_FleshGolems_DeathDetails InitializeRandomDeathDetails()
            => InitializeRandomDeathDetails(ParentObject);

        public static UD_FleshGolems_DeathDetails InitializeRandomDeathDetails(
            GameObject ForCorpse,
            UD_FleshGolems_DeathDetails DeathDetails = null)
        {
            DeathDetails = ForCorpse.RequirePart<UD_FleshGolems_DeathDetails>();

            DeathDescription deathDescription = ProduceRandomDeathDescriptionWithComponents(
                For: ForCorpse,
                Killer: out GameObject killer,
                Weapon: out GameObject weapon,
                Projectile: out GameObject projectile,
                Category: out _,
                Reason: out _,
                Accidental: out bool accidental,
                KillerIsCached: out bool killerIsCached);
            try
            {
                if (ForCorpse.GetBlueprint().IsCorpse())
                {
                    DeathDetails = ForCorpse.RequirePart<UD_FleshGolems_DeathDetails>();

                    if (!DeathDetails.Initialize(killer, weapon, projectile, deathDescription, accidental, killerIsCached))
                    {
                        ForCorpse.RemovePart(DeathDetails);
                        return null;
                    }
                }
                return DeathDetails;
            }
            finally
            {
                if (!killerIsCached)
                {
                    killer?.Obliterate();
                    weapon?.Obliterate();
                }
                projectile?.Obliterate();
            }
        }

        public static bool FakeRandomDeath(
            GameObject Dying,
            ref UD_FleshGolems_DeathDetails DeathDetails,
            int ChanceRandomKiller = 50,
            bool DoAchievement = false,
            IRenderable RelentlessIcon = null,
            string RelentlessTitle = null)
        {
            bool weaponIsProjectile = DeathDetails?.Weapon is GameObject ddWeapon 
                && (ddWeapon.InheritsFrom("Projectile")
                    || ddWeapon.HasPart<Projectile>());

            GameObject killer = DeathDetails?.Killer;
            GameObject weapon = DeathDetails?.Weapon;
            GameObject projectile = weaponIsProjectile ? DeathDetails?.Weapon : null;
            bool killerIsCached = DeathDetails != null && DeathDetails.KillerIsCached;
            bool accidental = DeathDetails != null && DeathDetails.Accidental;
            string category = DeathDetails?.DeathDescription?.Category;
            string reason = DeathDetails?.DeathDescription?.Reason();
            try
            {
                DeathDescription deathDescription = DeathDetails?.DeathDescription
                    ?? ProduceRandomDeathDescriptionWithComponents(
                        For: Dying,
                        Killer: out killer,
                        Weapon: out weapon,
                        Projectile: out projectile,
                        Category: out category,
                        Reason: out reason,
                        Accidental: out accidental,
                        KillerIsCached: out killerIsCached,
                        ChanceRandomKiller: ChanceRandomKiller);

                if (DeathDetails == null)
                    MetricsManager.LogModWarning(
                        mod: ThisMod,
                        Message: nameof(FakeRandomDeath) + " passed null " + nameof(UD_FleshGolems_DeathDetails) +
                            " for " + (Dying?.DebugName ?? "null entity"));
                
                if (DeathDetails != null
                    && !DeathDetails.Init)
                    DeathDetails?.Initialize(killer, weapon, projectile, deathDescription, accidental, killerIsCached);

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
                    RelentlessTitle: RelentlessTitle,
                    DeathDescription: deathDescription);

                if (!killerIsCached)
                {
                    killer?.Obliterate();
                    weapon?.Obliterate();
                }
                projectile?.Obliterate();

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
        public bool FakeRandomDeath(ref UD_FleshGolems_DeathDetails DeathDetails, int ChanceRandomKiller = 50, bool DoAchievement = false)
        {
            return FakeRandomDeath(
                Dying: ParentObject,
                ref DeathDetails,
                ChanceRandomKiller: ChanceRandomKiller,
                DoAchievement: DoAchievement);
        }

        public bool ActuallyDoTheFakeDieAndReanimate()
        {
            if (ParentObject is not GameObject player
                || !PlayerWantsFakeDie
                || HaveFakedDeath
                || (!player.IsPlayer() 
                    && !player.IsPlayerDuringWorldGen()))
                return false;

            bool success = ReplaceEntityWithCorpse(
                Entity: player,
                FakeDeath: PlayerWantsFakeDie,
                FakedDeath: out HaveFakedDeath,
                DeathEvent: null,
                Corpse: Corpse);

            PlayerWantsFakeDie = false;
            if (success)
            {
                GetPlayerTaxa()?.RestoreTaxa(Corpse);
                Corpse?.SetStringProperty("OriginalPlayerBody", "Not really, but we pretend!");
            }
            return success;
        }

        public bool ProcessObjectCreationEvent(IObjectCreationEvent E)
        {
            if (!Attempted
                && BuiltToBeReanimated
                && ParentObject is GameObject entity
                && entity == E.Object
                && (Corpse != null
                    || TryProduceCorpse(entity, out Corpse))
                && Corpse.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper reanimationHelper))
            {
                if (USE_OLD_METHOD_FOR_PLAYER)
                {
                    if (!entity.IsPlayer()
                        && !entity.IsPlayerDuringWorldGen())
                    {
                        reanimationHelper.Animate();
                        E.ReplacementObject = Corpse;
                        Attempted = true;
                    }
                }
                else
                {
                    if (entity.IsPlayer()
                        || entity.IsPlayerDuringWorldGen())
                    {
                        Corpse.AddPart(this, Creation: true);
                        PlayerWantsFakeDie = true;
                        Corpse.RegisterPartEvent(this, "GameStart");
                    }
                    reanimationHelper.Animate();
                    E.ReplacementObject = Corpse;
                    Attempted = true;
                }
                if (Attempted)
                    return true;
            }
            return false;
        }

        public bool EventMatchesAndFailedToRegister(int WantID, int EventID)
            => WantID == EventID
            && FailedToRegisterEvents.Contains(EventID);

        public override bool AllowStaticRegistration()
            => true;

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            int eventOrder = EventOrder.EXTREMELY_EARLY + EventOrder.EXTREMELY_EARLY;
            try
            {
                Registrar?.Register(BeforeObjectCreatedEvent.ID, eventOrder);
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
                    FailedToRegisterEvents.Add(BeforeObjectCreatedEvent.ID);

                if (ParentObject == null
                    || ParentObject.RegisteredEvents == null
                    || !ParentObject.RegisteredEvents.ContainsKey(EnvironmentalUpdateEvent.ID))
                    FailedToRegisterEvents.Add(EnvironmentalUpdateEvent.ID);
            }
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int cascade)
            => base.WantEvent(ID, cascade)
            || EventMatchesAndFailedToRegister(ID, BeforeObjectCreatedEvent.ID)
            || EventMatchesAndFailedToRegister(ID, EnvironmentalUpdateEvent.ID)
            || ID == GetShortDescriptionEvent.ID
            || ID == BeforeDieEvent.ID
            || ID == GetDebugInternalsEvent.ID
            ;

        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            string persistanceText =
                ("Something about =subject.objective= gives the sense " +
                "=subject.subjective==subject.verb:'re:afterpronoun= unnaturally relentless...")
                    .StartReplace()
                    .AddObject(E.Object)
                    .ToString();

            if (E.Object.HasTag("VerseDescription"))
                E.Base.AppendLine().AppendLine().Append(persistanceText);
            else
            {
                if (!E.Base.IsNullOrEmpty())
                    E.Base.Append(" ");

                E.Base.Append(persistanceText);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnvironmentalUpdateEvent E)
        {
            if (!Attempted
                && BuiltToBeReanimated
                && PlayerWantsFakeDie
                && ParentObject is GameObject entity
                && (entity.IsPlayer()
                    || entity.IsPlayerDuringWorldGen())
                && !HaveFakedDeath
                && (Corpse != null
                    || TryProduceCorpse(entity, out Corpse)))
                entity.RegisterPartEvent(this, "GameStart");

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeObjectCreatedEvent E)
        {
            ProcessObjectCreationEvent(E);
            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (E.ID == "GameStart")
            {
                if (!HaveFakedDeath
                    && BuiltToBeReanimated
                    && PlayerWantsFakeDie
                    && !ActuallyDoTheFakeDieAndReanimate())
                {
                    string replacedTile = null;
                    string replacedTileColor = null;
                    string replacedColorString = null;
                    string replacedDetailColor = null;
                    if (ParentObject.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper reanimationHelper))
                    {
                        reanimationHelper.RestoreCorpseTile(out replacedTile);
                        reanimationHelper.RestoreCorpseColors(out replacedTileColor, out replacedColorString, out replacedDetailColor);
                    }
                    if (InitializeDeathDetailsThenFakeDeath(ParentObject, Corpse, null))
                    {
                        if (ParentObject.GetBlueprint().IsCorpse())
                            ParentObject.RemovePart(this);
                    }
                    else
                    {
                        if (ParentObject.Render != null)
                        {
                            if (!replacedTile.IsNullOrEmpty())
                                ParentObject.Render.Tile = replacedTile;

                            if (!replacedTileColor.IsNullOrEmpty())
                                ParentObject.Render.TileColor = replacedTileColor;

                            if (!replacedColorString.IsNullOrEmpty())
                                ParentObject.Render.ColorString = replacedColorString;

                            if (!replacedDetailColor.IsNullOrEmpty())
                                ParentObject.Render.DetailColor = replacedDetailColor;
                        }
                    }
                }
            }
            return base.FireEvent(E);
        }
        public override bool HandleEvent(BeforeDieEvent E)
        {
            if (!Attempted
                && !BuiltToBeReanimated
                && ParentObject is GameObject dying
                && dying == E.Dying
                && dying.TryGetPart(out Corpse dyingCorpse)
                && !dyingCorpse.CorpseBlueprint.IsNullOrEmpty()
                && dying.IsPlayer()
                && (!PlayerWantsFakeDie || !HaveFakedDeath)
                && ReplaceEntityWithCorpse(
                    Entity: ParentObject,
                    FakeDeath: PlayerWantsFakeDie,
                    FakedDeath: out HaveFakedDeath,
                    DeathEvent: E,
                    Corpse: Corpse))
                return !(Attempted = true);

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            E.AddEntry(this, nameof(Corpse), Corpse?.DebugName ?? NULL);
            E.AddEntry(this, nameof(BuiltToBeReanimated), BuiltToBeReanimated);
            E.AddEntry(this, nameof(Attempted), Attempted);
            if (!FailedToRegisterEvents.IsNullOrEmpty())
                E.AddEntry(this, nameof(FailedToRegisterEvents),
                    FailedToRegisterEvents
                    ?.ConvertAll(id => MinEvent.EventTypes.ContainsKey(id) ? MinEvent.EventTypes[id].ToString() : "Error")
                    ?.GenerateBulletList(Bullet: null, BulletColor: null));
            else
                E.AddEntry(this, nameof(FailedToRegisterEvents), "Empty");

            E.AddEntry(this, nameof(PlayerWantsFakeDie), PlayerWantsFakeDie);
            return base.HandleEvent(E);
        }
    }
}