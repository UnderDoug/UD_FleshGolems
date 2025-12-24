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
        private int Difficulty;

        private string DebugString;

        public UD_FleshGolems_ReanimatedJoinParty()
        {
            ReanimatorOnly = true;
            AskedFirstOnly = true;
            Visible = false;
            Difficulty = 0;

            DebugString = null;
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

            string doReplacement(string Text)
                => Text
                    ?.StartReplace()
                    ?.AddObject(Speaker)
                    ?.AddObject(Player)
                    ?.ToString();

            if (Speaker == Player
                || Speaker.HasCopyRelationship(Player)
                || Speaker.IsOriginalPlayerBody())
                return Player.Fail(doReplacement("=object.Name= can't recruit =object.reflexive=!"), Silent);

            if (Player.IsMissingTongue() && !Player.CanMakeTelepathicContactWith(Speaker))
            {
                string telepathicallyFailedMsg = "Without a tongue, =object.name= cannot recruit =subject.name=";
                if (Player.HasPart<XRL.World.Parts.Mutation.Telepathy>())
                {
                    telepathicallyFailedMsg += ", as =object.name= cannot make telepathic contact with =subject.objective=";
                }
                telepathicallyFailedMsg += ".";
                return Player.Fail(doReplacement(telepathicallyFailedMsg), Silent);
            }

            if (!Player.CheckFrozen(Telepathic: true, Telekinetic: false, Silent: true, Speaker))
                return Player.Fail(doReplacement("Frozen solid, =object.name= cannot recruit =subject.name=."), Silent);

            Difficulty = CalculateDifficulty(Player, Speaker, out _, out _);

            bool result = Difficulty <= 0;
            Debug.YehNah("able to recruit", result, Indent: indent[0]);

            if (!Silent
                && !result)
                Player.Fail(
                    Message: doReplacement("=object.Name= cannot recruit =subject.name= because " +
                        "=subject.subjective==subject.verb:'ve:afterpronoun= " +
                        "farmed more aura than =object.subjective= =object.verb:have:afterpronoun=."),
                    Silent: Silent);

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
                Difficulty = CalculateDifficulty(player, speaker, out _, out _, Silent: true);
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
                    Debug.Arg(nameof(RecruitedPropTag), The.Speaker?.GetIntProperty(RecruitedPropTag) ?? -1),
                });

            if (The.Speaker is not GameObject speaker
                || The.Player is not GameObject player
                || !CheckCanRecruit(player, speaker, out Difficulty))
                return false;

            Visible = false;
            Debug.Log(nameof(Difficulty), Difficulty, Indent: indent[1]);
            speaker.SetAlliedLeader<AllyProselytize>(player);
            speaker.SetIntProperty(RecruitedPropTag, 1);

            Debug.Log(nameof(RecruitedPropTag), speaker.GetIntProperty(RecruitedPropTag), Indent: indent[1]);

            if (speaker.TryGetEffect(out Lovesick lovesick))
                lovesick.PreviousLeader = player;

            string successMsg = "=subject.Name= =subject.verb:join= =object.name=!"
                .StartReplace()
                .AddObject(speaker)
                .AddObject(player)
                .ToString();

            Popup.Show(successMsg);

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(PrepareTextLateEvent E)
        {
            if (The.Speaker is GameObject speaker
                && The.Player is GameObject player)
            {
                IConversationElement superParentElement = ParentElement;
                while (superParentElement.Parent is IConversationElement shallowParentElement)
                {
                    bool doBreak = false;
                    if (superParentElement is Choice
                        && shallowParentElement is not Choice)
                        doBreak = true;
                    superParentElement = shallowParentElement;

                    if (doBreak)
                        break;
                }
                superParentElement ??= ParentElement;

                if (Visible
                    && DebugEnableConversationDebugText)
                {
                    if (DebugString.IsNullOrEmpty())
                    {
                        int Difficulty = CalculateDifficulty(player, speaker, out int defense, out int attack, true);

                        DebugString =
                            "{{K|" +
                            "\n\n" +
                            HONLY.ThisManyTimes(40) +
                            "\n\n" +
                            "Defense: {{r|" + defense + "}} | Attack: {{g|" + attack + "}} | Difficulty: {{W|" + Difficulty + "}}" +
                            "}}";
                    }
                    if (!superParentElement.Text.Contains(DebugString))
                        superParentElement.Text += DebugString;
                }
                else
                {
                    if (superParentElement.Text.Contains(DebugString))
                        superParentElement.Text.Remove(DebugString);

                    DebugString = null;
                }
            }
            return base.HandleEvent(E);
        }
    }
}
