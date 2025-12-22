using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.World.Text;
using XRL.World.AI;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.Effects;

using static XRL.World.Parts.UD_FleshGolems_ReanimatedCorpse;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Events;
using UD_FleshGolems.Parts.VengeanceHelpers;

using static UD_FleshGolems.Const;
using static UD_FleshGolems.Options;
using static UD_FleshGolems.Utils;

namespace XRL.World.Conversations.Parts
{
    public class UD_FleshGolems_ReanimatedJoinParty : IConversationPart
    {
        public static string RecruitedPropTag => nameof(UD_FleshGolems_ReanimatedJoinParty) + ".Recruited";

        public bool ReanimatorOnly;
        public bool AskedFirstOnly;

        private bool Visible;
        private bool CanRecruit;
        private int Difficulty;

        public UD_FleshGolems_ReanimatedJoinParty()
        {
            ReanimatorOnly = true;
            AskedFirstOnly = true;
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

        public static int CalculateDifficulty(GameObject Player, GameObject Speaker, out int Defense, out int Attack, bool Silent = false)
        {
            using Indent indent = new(1);

            Defense = Speaker.Stat("Level");
            Attack = Player.Stat("Level") + Player.StatMod("Ego");
            AdjustDifficultyBasedOnEffects(ref Defense, Player, Speaker);
            int difficulty = Defense - Attack;

            if (!Silent)
            {
                Debug.Log(nameof(Defense), Defense, Indent: indent);
                Debug.Log(nameof(Attack), Attack, Indent: indent);
                Debug.Log(nameof(difficulty), difficulty, Indent: indent);
            }

            return difficulty;
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

            bool isPlayerTelepathic = Player.HasPart<XRL.World.Parts.Mutation.Telepathy>();
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
                string telepathicallyFailedMsg = "Without a tongue, you cannot recruit =subject.name=";
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
                string frozenFailedMsg = "Frozen solid, you cannot recruit =subject.name=."
                    .StartReplace()
                    .AddObject(Speaker)
                    .ToString();
                return Player.Fail(frozenFailedMsg, Silent);
            }

            Difficulty = CalculateDifficulty(Player, Speaker, out _, out _);

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
                && (!AskedFirstOnly
                    || UD_FleshGolems_AskHowDied.GetPlayerHasAskedBefore(player, speaker))
                && CanRecruitReanimatedWithoutRep.Check(player, speaker))
            {
                Visible = true;
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
            if (CheckCanRecruit(The.Player, The.Speaker, out Difficulty, true))
            {
                lowlight = "g";
                numeric = "W";
            }
            string difficulty = Difficulty == int.MaxValue ? ("-" + "\xEC") : (-Difficulty).Signed();
            E.Tag = "{{" + lowlight + "|[{{" + numeric + "|" + difficulty + "}} compelling]}}";
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnteredElementEvent E)
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(EnteredElementEvent)),
                });

            if (The.Speaker is GameObject speaker
                && The.Player is GameObject player
                && CheckCanRecruit(player, speaker, out Difficulty))
            {
                Debug.Log(nameof(Difficulty), Difficulty, Indent: indent[1]);
                speaker.SetAlliedLeader<AllyProselytize>(player);
                speaker.SetIntProperty(RecruitedPropTag, 1);

                if (speaker.TryGetEffect(out Lovesick lovesick))
                    lovesick.PreviousLeader = player;

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
                int Difficulty = CalculateDifficulty(player, speaker, out int defense, out int attack, true);

                string textAddition = 
                    "\n\n" +
                    HONLY.ThisManyTimes(40) +
                    "\n\n" +
                    "Defense: {{r|" + defense + "}} | Attack: {{g|" + attack + "}} | Difficulty: {{W|" + Difficulty + "}}";

                IConversationElement superParentElement = ParentElement;
                while (superParentElement.Parent is IConversationElement shallowParentElement)
                {
                    superParentElement = shallowParentElement;
                }
                superParentElement ??= ParentElement;
                superParentElement.Text += "{{K|" + textAddition + "}}";
            }
            return base.HandleEvent(E);
        }
    }
}
