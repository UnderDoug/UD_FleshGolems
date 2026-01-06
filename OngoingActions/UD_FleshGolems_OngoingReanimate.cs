using System;
using System.Collections.Generic;
using System.Text;

using ConsoleLib.Console;

using XRL;
using XRL.Language;
using XRL.UI;
using XRL.World;
using XRL.World.AI;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.Text;

namespace UD_FleshGolems
{
    public class UD_FleshGolems_OngoingReanimate : OngoingAction
    {
        public GameObject Reanimator;

        public UD_FleshGolems_NanoNecroAnimation ReanimationMutation => Reanimator?.GetFirstPartDescendedFrom<UD_FleshGolems_NanoNecroAnimation>();

        public Queue<GameObject> CorpseQueue;

        public int NumberWanted;

        public int NumberDone;

        public int OriginalCount;

        public GameObject Corpse;

        public List<GameObject> ReanimatedList;

        public List<string> ReanimatedNames;

        public bool Abort;

        public string InterruptBecause;

        public int EnergyCostPer;

        public UD_FleshGolems_OngoingReanimate()
        {
            Reanimator = null;

            CorpseQueue = new();

            NumberWanted = 0;
            NumberDone = 0;
            OriginalCount = 0;

            Corpse = null;

            ReanimatedList = new();
            ReanimatedNames = new();

            Abort = false;

            InterruptBecause = null;

            EnergyCostPer = 1000;
        }

        public UD_FleshGolems_OngoingReanimate(GameObject Reanimator, IEnumerable<GameObject> Corpses)
            : this()
        {
            this.Reanimator = Reanimator;

            CorpseQueue = new(Corpses);

            NumberWanted = CorpseQueue.Count;
            OriginalCount = CorpseQueue.Count;

            if (!CorpseQueue.IsNullOrEmpty())
            {
                Corpse = CorpseQueue.Dequeue();
            }
        }

        public UD_FleshGolems_OngoingReanimate(GameObject Reanimator, IEnumerable<GameObject> Corpses, int EnergyCostPer)
            : this(Reanimator, Corpses)
        {
            this.EnergyCostPer = EnergyCostPer;
        }

        public override string GetDescription()
        {
            return "reanimating";
        }

        public override bool IsMovement() => false;

        public override bool IsCombat() => false;

        public override bool IsExploration() => false;

        public override bool IsGathering() => false;

        public override bool IsResting() => false;

        public override bool IsRateLimited() => false;

        public override bool Continue()
        {
            if (CorpseQueue.IsNullOrEmpty() || (Corpse == null && !CorpseQueue.TryDequeue(out Corpse)))
            {
                MetricsManager.LogPotentialModError(Utils.ThisMod, nameof(CorpseQueue) + " is null or empty, or " + nameof(Corpse) + " is null, or failed to Dequeu Corpse.");
                return false;
            }

            SoundManager.PreloadClipSet("Sounds/StatusEffects/sfx_statusEffect_negativeVitality");
            SoundManager.PreloadClipSet("Sounds/StatusEffects/sfx_statusEffect_positiveVitality");

            bool interrupt = false;
            string interruptBecause = null;
            if (!interrupt && (!GameObject.Validate(ref Corpse) || Corpse.IsNowhere()))
            {
                interruptBecause = "the corpse =reanimator.subjective= =verb:were:afterpronoun= ranimating disappeared";
                interrupt = true;
            }
            if (!interrupt && Corpse.IsInGraveyard())
            {
                interruptBecause = "the corpse =reanimator.subjective= =verb:were:afterpronoun= ranimating was destroyed";
                interrupt = true;
            }
            if (!interrupt && Corpse.IsInStasis())
            {
                interruptBecause = "=reanimator.subjective= can no longer interact with =corpse.t=";
                interrupt = true;
            }
            if (!interrupt && ReanimationMutation == null)
            {
                interruptBecause = "=reanimator.subjective= is no longer capable of reanimating corpses";
                interrupt = true;
            }
            if (!interrupt && !UD_FleshGolems_NanoNecroAnimation.IsReanimatableCorpse(Corpse))
            {
                interruptBecause = "=corpse.t= can no longer be reanimated";
                interrupt = true;
            }
            if (!interrupt && !Reanimator.CanMoveExtremities("Reanimate", AllowTelekinetic: true))
            {
                interruptBecause = "=reanimator.subjective= can no longer move =reanimator.possessive= extremities";
                interrupt = true;
            }
            if (!interrupt && Reanimator.ArePerceptibleHostilesNearby(logSpot: true, popSpot: true, Action: this))
            {
                // interruptBecause = "=reanimator.subjective= can no longer reanimate safely";
                // interrupt = true;
            }
            if (interrupt)
            {
                InterruptBecause = interruptBecause
                    .StartReplace()
                    .AddObject(Reanimator, nameof(Reanimator).ToLower())
                    .AddObject(Corpse, nameof(Corpse).ToLower())
                    .ToString();

                return false;
            }
            bool useEnergy = false;
            try
            {
                if (ReanimationMutation.ReanimateCorpse(Corpse, (NumberDone % 3) == 0))
                {
                    if (Corpse?.Brain?.Allegiance != null)
                    {
                        Corpse.Brain.Allegiance.Calm = true;
                    }
                    bool multipleObjects = NumberWanted > 1 && OriginalCount > 1;
                    ReanimatedList ??= new();
                    ReanimatedNames ??= new();

                    ReanimatedList.Add(Corpse);
                    ReanimatedNames.Add(Corpse.GetReferenceDisplayName(Short: true));

                    GameObject prevCorpse = Corpse;
                    if (!CorpseQueue.IsNullOrEmpty())
                    {
                        Corpse = CorpseQueue.Dequeue();
                    }
                    else
                    {
                        Corpse = null;
                    }
                    if (Corpse == prevCorpse || Corpse == null)
                    {
                        Abort = true;
                    }
                }
                if (NumberDone < NumberWanted)
                {
                    NumberDone++;
                    if (NumberWanted > 1)
                    {
                        Loading.SetLoadingStatus($"Reanimated {NumberDone.Things("corpse")} of {NumberWanted}...");
                    }
                    if (!Abort)
                    {
                        if (Reanimator.HasRegisteredEvent("UD_FleshGolems_ObjectReanimatedCorpse"))
                        {
                            Event @event = Event.New("UD_FleshGolems_ObjectReanimatedCorpse", "Corpse", Corpse, "SourcePart", ReanimationMutation);
                            Reanimator.FireEvent(@event);
                        }
                    }
                    useEnergy = true;
                }
            }
            finally
            {
                if (useEnergy)
                {
                    Reanimator.UseEnergy(EnergyCostPer, "Mutation Reanimate Corpse");
                }
            }
            return base.Continue();
        }
        public override string GetInterruptBecause()
        {
            return InterruptBecause;
        }

        public override bool ShouldHostilesInterrupt() => false;

        public override void Interrupt()
        {
            ("=reanimator.T= =reanimator.verb:stop= " + GetDescription() + " because " + GetInterruptBecause() + ".")
                .StartReplace()
                .AddObject(Reanimator, nameof(Reanimator).ToLower())
                .EmitMessage();

            if (NumberWanted > 1)
            {
                Loading.SetLoadingStatus("Interrupted!");
            }
            base.Interrupt();
        }

        public override bool CanComplete() => Abort || NumberDone >= NumberWanted;

        public override void Complete()
        {
            if (NumberWanted > 1)
            {
                Loading.SetLoadingStatus("Finished reanimating.");
            }
            base.Complete();
        }

        public override void End()
        {
            /*
            foreach (GameObject reanimatedCorpse in ReanimatedList)
            {
                if (reanimatedCorpse.HasIntProperty("UD_FLeshGolems Deferred PastLife Hostility"))
                {
                    if (reanimatedCorpse.TryGetPart(out UD_FleshGolems_PastLife pastLife)
                        && Corpse?.Brain?.Allegiance != null
                        && pastLife?.Brain?.Allegiance != null)
                    {
                        Corpse.Brain.Allegiance.Hostile = pastLife.Brain.Allegiance.Hostile;
                    }
                    reanimatedCorpse.RemoveIntProperty("UD_FLeshGolems Deferred PastLife Hostility");
                }
            }
            */

            var SB = Event.NewStringBuilder();
            SB.Append("=subject.Name= reanimated");
            bool doBulletList = NumberDone > 3
                && !ReanimatedNames.IsNullOrEmpty()
                && ReanimatedNames.Count > 3;
            if (!doBulletList)
            {
                SB.Append(" ");
            }
            else
            {
                SB.Append(":\n");
            }
            if (ReanimatedNames.IsNullOrEmpty())
            {
                SB.Append("someone...");
            }
            else
            {
                if (!doBulletList)
                {
                    SB.Append(Grammar.MakeAndList(ReanimatedNames));
                    SB.Append('.');
                }
                else
                {
                    SB.Append(ReanimatedNames.GenerateBulletList());
                }
            }

            Popup.Show(GameText.StartReplace(SB).AddObject(Reanimator).ToString());

            List<string> reanimatedNames = new();
            List<IRenderable> reanimatedIcons = new();
            List<GameObject> reanimatedCorpses = Event.NewGameObjectList();
            for (int i = 0; i < ReanimatedList.Count; i++)
            {
                if (ReanimationMutation.CanRecruitCorpse(ReanimatedList[i]))
                {
                    reanimatedNames.Add(ReanimatedNames[i]);
                    reanimatedIcons.Add(ReanimatedList[i].RenderForUI());
                    reanimatedCorpses.Add(ReanimatedList[i]);
                }
            }

            if (!reanimatedNames.IsNullOrEmpty())
            {
                if (Popup.PickSeveral(
                    Title: "Pick which corpses should join you.",
                    Options: reanimatedNames,
                    Icons: reanimatedIcons,
                    IntroIcon: new Renderable("Mutations/sunder_mind.bmp", "W", "&r", "&r", 'K'),
                    AllowEscape: true) is List<(int picked, int amount)> pickedCorpses)
                {
                    Reanimator?.SmallTeleportSwirl(Color: "&M", Sound: "Sounds/StatusEffects/sfx_statusEffect_charm");
                    foreach ((int picked, int _) in pickedCorpses)
                    {
                        reanimatedCorpses[picked]?.SetAlliedLeader<AllyProselytize>(Reanimator);
                        reanimatedCorpses[picked]?.SmallTeleportSwirl(Color: "&m", IsOut: true);
                    }
                }
            }
            Loading.SetLoadingStatus(null);
            Keyboard.PushKey(UnityEngine.KeyCode.None); // UnityEngine.KeyCode.Space works, testing with "no input".
        }
    }
}
