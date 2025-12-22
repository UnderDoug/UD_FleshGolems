using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.World.Parts;
using XRL.World.Text;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Parts.VengeanceHelpers;

using static UD_FleshGolems.Const;
using static UD_FleshGolems.Options;
using static UD_FleshGolems.Utils;

using static XRL.World.Parts.UD_FleshGolems_ReanimatedCorpse;
using XRL.World.Parts.Mutation;
using XRL.World.Effects;
using XRL.World.AI;
using UD_FleshGolems.Events;

namespace XRL.World.Conversations.Parts
{
    public class UD_FleshGolems_ReanimatedJoinParty : IConversationPart
    {
        public static string RecruitedPropTag => nameof(UD_FleshGolems_ReanimatedJoinParty) + ".Recruited";

        public bool ReanimatorOnly;

        private bool Visible;
        private bool CanRecruit;
        private int Difficulty;

        public UD_FleshGolems_ReanimatedJoinParty()
        {
            ReanimatorOnly = true;
            Visible = false;
            CanRecruit = false;
            Difficulty = 0;
        }

        private static void AdjustProselytized(ref int Difficulty, Effect FX, GameObject Player)
        {
            if (Player != null)
                if (FX is Proselytized proselytized)
                {
                    if (proselytized.Proselytizer == Player)
                        Difficulty++;
                    else
                        Difficulty--;
                }
        }
        private static void AdjustBeguiled(ref int Difficulty, Effect FX, GameObject Player)
        {
            if (Player != null)
                if (FX is Beguiled beguiled)
                {
                    if (beguiled.Beguiler == Player)
                        Difficulty++;
                    else
                        Difficulty--;
                }
        }
        private static void AdjustRebuked(ref int Difficulty, Effect FX, GameObject Player)
        {
            if (Player != null)
                if (FX is Rebuked rebuked)
                {
                    if (rebuked.Rebuker == Player)
                        Difficulty++;
                    else
                        Difficulty--;
                }
        }
        private static void AdjustLovesick(ref int Difficulty, Effect FX, GameObject Player)
        {
            if (Player != null)
                if (FX is Lovesick lovesick)
                {
                    if (lovesick.Beauty == Player)
                        Difficulty++;
                    else
                        Difficulty--;
                }
        }

        private static void AdjustDifficultyBasedOnEffects(ref int Difficulty, GameObject Player, GameObject Speaker)
        {
            if (Player == null
                || Speaker == null
                || Speaker.Effects.IsNullOrEmpty())
                return;

            foreach (Effect fX in Speaker.Effects)
            {
                AdjustProselytized(ref Difficulty, fX, Player);
                AdjustBeguiled(ref Difficulty, fX, Player);
                AdjustRebuked(ref Difficulty, fX, Player);
                AdjustLovesick(ref Difficulty, fX, Player);
            }
        }

        public static bool CheckCanRecruit(GameObject Player, GameObject Speaker, out int Difficulty, bool Silent = false)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Player), Player?.DebugName ?? NULL),
                    Debug.Arg(nameof(Speaker), Speaker?.DebugName ?? NULL),
                    Debug.Arg(nameof(Silent), Silent),
                });

            Difficulty = int.MaxValue;
            if (Player == null
                || Speaker == null)
                return false;

            bool isPlayerTelepathic = Player.HasPart<Telepathy>();
            if (Player.IsMissingTongue()
                && !isPlayerTelepathic)
                return Player.Fail("You cannot proselytize without a tongue.", Silent);

            if (!Player.CheckFrozen(Telepathic: true))
                return false;

            if (Speaker == Player
                || Speaker.HasCopyRelationship(Player)
                || Speaker.IsOriginalPlayerBody())
                return Player.Fail("You can't proselytize " + Player.itself + "!", Silent);

            if (!Player.CanMakeTelepathicContactWith(Speaker))
            {
                string telepathicallyFailedMsg = "Without a tongue, you cannot proselytize =subject.name=";
                if (isPlayerTelepathic)
                {
                    telepathicallyFailedMsg += ", as you cannot make telepathic contact with =subject.objective=";
                }
                telepathicallyFailedMsg += ".";
                telepathicallyFailedMsg = telepathicallyFailedMsg
                    .StartReplace()
                    .AddObject(Speaker)
                    .ToString();
                return Player.Fail(telepathicallyFailedMsg, Silent);
            }
            if (!Player.CheckFrozen(Telepathic: true, Telekinetic: false, Silent: true, Speaker))
            {
                string frozenFailedMsg = "Frozen solid, you cannot proselytize =subject.name=."
                    .StartReplace()
                    .AddObject(Speaker)
                    .ToString();
                return Player.Fail(frozenFailedMsg, Silent);
            }

            int defense = Speaker.Stat("Level");
            int attack = Player.Stat("Level") + Player.StatMod("Ego");
            AdjustDifficultyBasedOnEffects(ref defense, Player, Speaker);
            Difficulty = defense - attack;

            Debug.Log(nameof(defense), defense, Indent: indent[1]);
            Debug.Log(nameof(attack), attack, Indent: indent[1]);
            Debug.Log(nameof(Difficulty), Difficulty, Indent: indent[1]);
            bool result = Difficulty <= 0;
            Debug.YehNah("able to recruit", result, Indent: indent[0]);

            return result;
        }

        public override void Awake()
        {
            if (The.Speaker is GameObject speaker
                && The.Player is GameObject player
                && speaker.GetIntProperty(RecruitedPropTag) < 1
                && (!ReanimatorOnly
                    || speaker.GetPart<UD_FleshGolems_ReanimatedCorpse>()?.Reanimator == player
                    || speaker.GetEffect<UD_FleshGolems_UnendingSuffering>()?.SourceObject == player)
                && CanRecruitReanimatedWithoutRep.Check(player, speaker))
            {
                Visible = true;
                CanRecruit = CheckCanRecruit(speaker, player, out Difficulty, true);
            }
            base.Awake();
        }
        public override bool WantEvent(int ID, int Propagation)
            => base.WantEvent(ID, Propagation)
            || ID == IsElementVisibleEvent.ID
            || ID == GetChoiceTagEvent.ID
            || ID == EnteredElementEvent.ID
            || ID == PrepareTextLateEvent.ID
            ;
        public override bool HandleEvent(IsElementVisibleEvent E)
            => base.HandleEvent(E)
            && Visible
            ;
        public override bool HandleEvent(GetChoiceTagEvent E)
        {
            string lowlight = "K";
            string numeric = "r";
            string difficulty = Difficulty == int.MaxValue ? ("-" + "\xEC") : (-Difficulty).Signed();
            if (CanRecruit)
            {
                lowlight = "g";
                numeric = "W";
            }
            E.Tag = "{{" + lowlight + "|[{{" + numeric + "|" + difficulty + "}} compelling]}}";
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnteredElementEvent E)
        {
            if (The.Speaker is GameObject speaker
                && The.Player is GameObject player
                && CanRecruit)
            {
                speaker.SetAlliedLeader<AllyProselytize>(player);
                speaker.SetIntProperty(RecruitedPropTag, 1);

                if (speaker.TryGetEffect<Lovesick>(out var lovesick))
                    lovesick.PreviousLeader = The.Player;

                string successMsg = "=subject.Name= =subject.verb:join= =object.name=!"
                    .StartReplace()
                    .AddObject(speaker)
                    .AddObject(player)
                    .ToString();

                Popup.Show(successMsg);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(PrepareTextLateEvent E)
        {
            if (The.Speaker is GameObject speaker
                && The.Player is GameObject player
                && Visible
                && DebugEnableConversationDebugText)
            {
                int defense = speaker.Stat("Level");
                int attack = player.Stat("Level") + player.StatMod("Ego");
                AdjustDifficultyBasedOnEffects(ref defense, player, speaker);
                int Difficulty = defense - attack;

                string textAddition = 
                    "\n\n" +
                    HONLY.ThisManyTimes(40) +
                    "\n\n" +
                    "Defense: {{r|" + defense + "}} | Attack: {{g|" + attack + "}} | Difficulty: {{W|" + Difficulty + "}}";

                E.Text += "{{K|" + textAddition + "}}";
            }
            return base.HandleEvent(E);
        }
    }
}
