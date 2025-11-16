using System;
using System.Collections.Generic;
using System.Text;

using ConsoleLib.Console;

using XRL;
using XRL.Language;
using XRL.Messages;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.AI;
using XRL.World.Parts.Mutation;
using XRL.World.Text;

namespace UD_FleshGolems
{
    public class UD_FleshGolems_OngoingSummonCorpse : OngoingAction
    {
        public GameObject Summoner;

        public UD_FleshGolems_NanoNecroAnimation SummoningMutation => Summoner?.GetFirstPartDescendedFrom<UD_FleshGolems_NanoNecroAnimation>();

        public int NumberWanted;

        public int _SummonRadius;
        public int SummonRadius => _SummonRadius == 0 ? (_SummonRadius = Math.Max(5, NumberWanted / 4)) : _SummonRadius;

        public int NumberDone;

        public int OriginalCount;

        public List<GameObject> SummonedList;

        public bool Abort;

        public string InterruptBecause;

        public int EnergyCostPer;

        public UD_FleshGolems_OngoingSummonCorpse()
        {
            Summoner = null;

            NumberWanted = 0;
            _SummonRadius = 0;
            NumberDone = 0;
            OriginalCount = 0;

            SummonedList = new();

            Abort = false;

            InterruptBecause = null;

            EnergyCostPer = 1000;
        }

        public UD_FleshGolems_OngoingSummonCorpse(GameObject Summoner, int NumberWanted)
            : this()
        {
            this.Summoner = Summoner;

            this.NumberWanted = NumberWanted;
            OriginalCount = NumberWanted;
        }

        public UD_FleshGolems_OngoingSummonCorpse(GameObject Summoner, int NumberWanted, int EnergyCostPer)
            : this(Summoner, NumberWanted)
        {
            this.EnergyCostPer = EnergyCostPer;
        }

        public UD_FleshGolems_OngoingSummonCorpse(GameObject Summoner, int NumberWanted, int EnergyCostPer, int SummonRadius)
            : this(Summoner, NumberWanted, EnergyCostPer)
        {
            _SummonRadius = SummonRadius;
        }

        public override string GetDescription()
        {
            return "summoning";
        }

        public override bool IsMovement() => false;

        public override bool IsCombat() => false;

        public override bool IsExploration() => false;

        public override bool IsGathering() => false;

        public override bool IsResting() => false;

        public override bool IsRateLimited() => false;

        public override bool Continue()
        {
            var SB = Event.NewStringBuilder();
            var RB = GameText.StartReplace(SB);
            RB.AddObject(Summoner, "summoner");

            bool interrupt = false;
            if (!interrupt && SummoningMutation == null)
            {
                SB.Append($"=summoner.subjective= is no longer capable of reanimating corpses");
                interrupt = true;
            }
            if (!interrupt && !Summoner.CanMoveExtremities("Summon", AllowTelekinetic: true))
            {
                SB.Append($"=summoner.subjective= can no longer move =summoner.possessive= extremities");
                interrupt = true;
            }
            if (!interrupt && Summoner.ArePerceptibleHostilesNearby(logSpot: true, popSpot: true, Action: this))
            {
                // SB.Append($"=summoner.subjective= can no longer summon safely");
                // interrupt = true;
            }
            if (!interrupt)
            {
                ReplaceBuilder.Return(RB);
            }
            else
            {
                InterruptBecause = RB.ToString();
                return false;
            }
            bool useEnergy = false;
            try
            {
                if (SummoningMutation.TrySummonCorpse(SummonRadius, out GameObject summonedCorpse))
                {
                    if (NumberDone % 3 == 0)
                    {
                        Summoner?.SmallTeleportSwirl(Color: "&K", Sound: "Sounds/StatusEffects/sfx_statusEffect_negativeVitality");
                    }
                    bool multipleObjects = NumberWanted > 1 && OriginalCount > 1;
                    SummonedList ??= new();
                    SummonedList.Add(summonedCorpse);
                }
                if (NumberDone < NumberWanted)
                {
                    NumberDone++;
                    if (NumberWanted > 1)
                    {
                        Loading.SetLoadingStatus($"Summoned {NumberDone.Things("corpse")} of {NumberWanted}...");
                    }
                    if (!Abort)
                    {
                        if (Summoner.HasRegisteredEvent("UD_FleshGolems_ObjectSummonedCorpse"))
                        {
                            Event @event = Event.New("UD_FleshGolems_ObjectSummonedCorpse", "Corpse", summonedCorpse, "SourcePart", SummoningMutation);
                            Summoner.FireEvent(@event);
                        }
                    }
                    useEnergy = true;
                }
                if (NumberDone >= NumberWanted)
                {
                    if (!Abort)
                    {
                        /*
                        GameObject prevCorpse = Corpse;
                        if (!CorpseQueue.IsNullOrEmpty() && !CorpseQueue.TryDequeue(out Corpse))
                        {
                            Corpse = CorpseQueue.Dequeue();
                        }
                        if (Corpse == prevCorpse || Corpse == null)
                        {
                            Abort = true;
                        }
                        */
                    }
                }
            }
            finally
            {
                if (useEnergy)
                {
                    Summoner.UseEnergy(EnergyCostPer, "Mutation Summon Corpse");
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
            if (NumberWanted > 1 && NumberDone < NumberWanted)
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
                Loading.SetLoadingStatus("Finished summoning.");
            }
            base.Complete();
        }

        public override void End()
        {
            var SB = Event.NewStringBuilder();
            SB.Append("=subject.Name= summoned");
            bool doBulletList = NumberDone > 3
                && !SummonedList.IsNullOrEmpty()
                && SummonedList.Count > 3;

            if (!doBulletList)
            {
                SB.Append(" ");
            }
            else
            {
                SB.Append(":\n");
            }
            if (SummonedList.IsNullOrEmpty())
            {
                SB.Append("something...");
            }
            else
            {
                List<string> summonedNames = new(SummonedList.ConvertToStringList(GO => GO.GetReferenceDisplayName(Short: true)));
                if (!doBulletList)
                {
                    SB.Append(Grammar.MakeAndList(summonedNames));
                    SB.Append('.');
                }
                else
                {
                    SB.Append(summonedNames.ConvertToStringListWithItemCount().GenerateBulletList());
                }
            }
            Popup.Show(GameText.StartReplace(SB).AddObject(Summoner).ToString());

            Loading.SetLoadingStatus(null);
            Keyboard.PushKey(UnityEngine.KeyCode.None); // UnityEngine.KeyCode.Space works, testing with "no input".
        }
    }
}
