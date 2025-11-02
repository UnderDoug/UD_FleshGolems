using System;
using System.Collections.Generic;

using ConsoleLib.Console;

using Qud.API;

using XRL.Core;
using XRL.UI;
using XRL.World.Capabilities;
using XRL.World.Effects;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_DestinedForReanimation : IScribedPart
    {
        public GameObject Corpse;

        public bool BuiltToBeReanimated;

        public bool Attempted;

        public UD_FleshGolems_DestinedForReanimation()
        {
            Corpse = null;
            BuiltToBeReanimated = false;
            Attempted = false;
        }

        public static bool IsDyingCreatureCorpse(GameObject Dying, out GameObject Corpse)
        {
            Corpse = null;
            if (Dying.HasPart<Corpse>()
                && Dying.GetDropInventory() is Inventory dropInventory)
            {
                GameObject bestMatch = null;
                GameObject secondBestMatch = null;
                GameObject thirdBestMatch = null;
                foreach (GameObject dropItem in dropInventory.GetObjects())
                {
                    if (!dropItem.GetBlueprint().InheritsFrom("Corpse"))
                    {
                        continue;
                    }
                    if (Dying.ID == dropItem.GetStringProperty("SourceID"))
                    {
                        Corpse = dropItem;
                        break;
                    }
                    if (Dying.Blueprint == dropItem.GetStringProperty("SourceBlueprint"))
                    {
                        if (bestMatch != null)
                        {
                            secondBestMatch ??= dropItem;
                            continue;
                        }
                        bestMatch ??= dropItem;
                    }
                    if (Dying.GetSpecies() == dropItem.Blueprint.Replace(" Corpse", ""))
                    {
                        if (secondBestMatch != null)
                        {
                            thirdBestMatch ??= dropItem;
                            continue;
                        }
                        secondBestMatch ??= dropItem;
                    }
                }
                Corpse ??= bestMatch ?? secondBestMatch ?? thirdBestMatch;
            }
            return Corpse != null;
        }
        public bool IsDyingCreatureCorpse(GameObject Dying)
        {
            return IsDyingCreatureCorpse(Dying, out _);
        }


        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            int eventOrder = EventOrder.EXTREMELY_LATE + EventOrder.EXTREMELY_LATE;
            Registrar.Register(BeforeDeathRemovalEvent.ID, eventOrder);
            Registrar.Register(BeforeObjectCreatedEvent.ID, -eventOrder);
            Registrar.Register(EnvironmentalUpdateEvent.ID, -eventOrder);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || ID == GetShortDescriptionEvent.ID
                || ID == BeforeDieEvent.ID;
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
        public override bool HandleEvent(BeforeDeathRemovalEvent E)
        {
            if (ParentObject is GameObject dying
                && dying == E.Dying)
            {
                IsDyingCreatureCorpse(dying, out Corpse);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnvironmentalUpdateEvent E)
        {
            if (!Attempted
                && BuiltToBeReanimated
                && ParentObject is GameObject soonToBeCorpse)
            {
                Attempted = true;
                soonToBeCorpse.Die();
                if (!soonToBeCorpse.IsPlayer())
                {
                    ReplaceInContextEvent.Send(soonToBeCorpse, Corpse);
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeDieEvent E)
        {
            if (ParentObject is GameObject dying
                && dying == E.Dying
                && dying.TryGetPart(out Corpse dyingCorpse)
                && !dyingCorpse.CorpseBlueprint.IsNullOrEmpty()
                && dying.IsPlayer())
            {
                Attempted = true;
                AfterDieEvent.Send(
                    Dying: dying,
                    Killer: E.Killer,
                    Weapon: E.Weapon,
                    Projectile: E.Projectile,
                    Accidental: E.Accidental,
                    AlwaysUsePopups: E.AlwaysUsePopups,
                    KillerText: E.KillerText,
                    Reason: E.Reason,
                    ThirdPersonReason: E.ThirdPersonReason);

                dying.StopMoving();

                KilledPlayerEvent.Send(
                    Dying: dying,
                    Killer: E.Killer,
                    Weapon: E.Weapon,
                    Projectile: E.Projectile,
                    Accidental: E.Accidental,
                    AlwaysUsePopups: E.AlwaysUsePopups,
                    KillerText: E.KillerText,
                    Reason: E.Reason,
                    ThirdPersonReason: E.ThirdPersonReason);

                string deathMessage = "You died.\n\n" + (E.Reason ?? The.Game.DeathReason);
                string deathCategory = The.Game.DeathCategory;
                Dictionary<string, Renderable> deathIcons = CheckpointingSystem.deathIcons;
                string deathMessageTitle = "";
                if (deathMessage.Contains("."))
                {
                    int titleSubstring = deathMessage.IndexOf('.') + 1;
                    int messageSubstring = deathMessage.IndexOf('.') + 2;
                    deathMessageTitle = deathMessage[..titleSubstring];
                    deathMessage = deathMessage[messageSubstring..];
                }
                Renderable deathIcon = null;
                if (!deathCategory.IsNullOrEmpty() && deathIcons.ContainsKey(deathCategory))
                {
                    deathMessage = deathMessage.Replace("You died.", "");
                    deathIcon = deathIcons[deathCategory];
                }
                Popup.ShowSpace(
                    Message: deathMessage,
                    Title: deathMessageTitle,
                    Sound: "Sounds/UI/ui_notification_death",
                    AfterRender: deathIcon,
                    LogMessage: true,
                    ShowContextFrame: deathIcon != null,
                    PopupID: "DeathMessage");

                IRenderable playerIcon = dying.RenderForUI();
                Popup.ShowSpace(
                    Message: "... and yet...\n\nYou don't relent.",
                    AfterRender: deathIcon != null ? (Renderable)playerIcon : null,
                    LogMessage: true,
                    ShowContextFrame: deathIcon != null,
                    PopupID: "DeathMessage");

                string deathReason = E.Reason ?? The.Game.DeathReason;
                if (!deathReason.IsNullOrEmpty())
                {
                    deathReason = deathReason[0].ToString().ToLower() + deathReason.Substring(1);
                }
                JournalAPI.AddAccomplishment(
                    text: "On the " + Calendar.GetDay() + " of " + Calendar.GetMonth() + ", " + deathReason.Replace("!", "."),
                    muralText: "",
                    gospelText: "");

                JournalAPI.AddAccomplishment(
                    text: "On the " + Calendar.GetDay() + " of " + Calendar.GetMonth() + ", " +
                        "you returned from the great beyond.",
                    muralText: "O! Fancieth way to say! Thou hatheth returned whence the thin-veil twixt living and the yonder!",
                    gospelText: "You just, sorta... woke back up from dying...");

                Achievement.DIE.Unlock();

                WeaponUsageTracking.TrackKill(
                    Actor: E.Killer,
                    Defender: dying,
                    Weapon: E.Weapon,
                    Projectile: E.Projectile,
                    Accidental: E.Accidental);

                EarlyBeforeDeathRemovalEvent.Send(
                    Dying: dying,
                    Killer: E.Killer,
                    Weapon: E.Weapon,
                    Projectile: E.Projectile,
                    Accidental: E.Accidental,
                    AlwaysUsePopups: E.AlwaysUsePopups,
                    KillerText: E.KillerText,
                    Reason: E.Reason,
                    ThirdPersonReason: E.ThirdPersonReason);

                BeforeDeathRemovalEvent.Send(
                    Dying: dying,
                    Killer: E.Killer,
                    Weapon: E.Weapon,
                    Projectile: E.Projectile,
                    Accidental: E.Accidental,
                    AlwaysUsePopups: E.AlwaysUsePopups,
                    KillerText: E.KillerText,
                    Reason: E.Reason,
                    ThirdPersonReason: E.ThirdPersonReason);

                OnDeathRemovalEvent.Send(
                    Dying: dying,
                    Killer: E.Killer,
                    Weapon: E.Weapon,
                    Projectile: E.Projectile,
                    Accidental: E.Accidental,
                    AlwaysUsePopups: E.AlwaysUsePopups,
                    KillerText: E.KillerText,
                    Reason: E.Reason,
                    ThirdPersonReason: E.ThirdPersonReason);

                DeathEvent.Send(
                    Dying: dying,
                    Killer: E.Killer,
                    Weapon: E.Weapon,
                    Projectile: E.Projectile,
                    Accidental: E.Accidental,
                    AlwaysUsePopups: E.AlwaysUsePopups,
                    KillerText: E.KillerText,
                    Reason: E.Reason,
                    ThirdPersonReason: E.ThirdPersonReason);

                ReplaceInContextEvent.Send(dying, Corpse);
                The.Game.Player.SetBody(Corpse);
                return false;
            }
            return base.HandleEvent(E);
        }
    }
}