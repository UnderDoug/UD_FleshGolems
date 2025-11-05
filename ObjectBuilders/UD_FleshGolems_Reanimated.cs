
using System;
using System.Collections.Generic;

using ConsoleLib.Console;

using Qud.API;

using XRL;
using XRL.Core;
using XRL.Names;
using XRL.UI;
using XRL.Wish;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Capabilities;
using XRL.World.ObjectBuilders;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

using UD_FleshGolems;

namespace XRL.World.ObjectBuilders
{
    [Serializable]
    [HasWishCommand]
    public class UD_FleshGolems_Reanimated : IObjectBuilder
    {
        public static bool IsGameRunning => The.Game != null && The.Game.Running;
        public static bool HAsWorldGenerated => IsGameRunning && The.Game.Running;

        public override void Initialize()
        {
        }

        public override void Apply(GameObject Object, string Context = null)
        {
            Unkill(Object, out _, Context);
        }

        public static GameObject ProduceCorpse(GameObject Creature, bool ForImmediateReanimation = true)
        {
            GameObject corpse = null;
            try
            {
                Body body = Creature.Body;
                string corpseBlueprintName = null;
                if (!Creature.TryGetPart(out Corpse corpsePart) || corpsePart.CorpseBlueprint.IsNullOrEmpty())
                {
                    string creatureBaseBlueprint = Creature.GetBlueprint().GetBaseTypeName();
                    corpseBlueprintName = creatureBaseBlueprint + " Corpse";
                    var corpseBlueprint = GameObjectFactory.Factory.GetBlueprintIfExists(corpseBlueprintName);

                    string speciesCorpse = Creature.GetSpecies() + " " + nameof(Corpse);
                    string fallbackCorpse = "Fresh " + nameof(Corpse);

                    if (corpseBlueprint == null)
                    {
                        corpseBlueprintName = GameObjectFactory.Factory?.GetBlueprintIfExists(creatureBaseBlueprint)
                            ?.GetPartParameter(nameof(Corpse), nameof(Corpse.CorpseBlueprint), speciesCorpse);
                    }
                    corpseBlueprint = GameObjectFactory.Factory.GetBlueprintIfExists(corpseBlueprintName);

                    corpseBlueprintName = corpseBlueprint?.Name ?? fallbackCorpse;
                }
                else
                {
                    corpseBlueprintName = corpsePart.CorpseBlueprint;
                }
                if ((corpse = GameObject.Create(corpseBlueprintName)) == null)
                {
                    return null;
                }
                Parts.Temporary.CarryOver(Creature, corpse);
                Phase.carryOver(Creature, corpse);
                if (Creature.HasProperName)
                {
                    corpse.SetStringProperty("CreatureName", Creature.BaseDisplayName);
                }
                else
                {
                    string creatureName = NameMaker.MakeName(Creature, FailureOkay: true);
                    if (creatureName != null)
                    {
                        corpse.SetStringProperty("CreatureName", creatureName);
                    }
                }
                if (Creature.HasID)
                {
                    corpse.SetStringProperty("SourceID", Creature.ID);
                }
                corpse.SetStringProperty("SourceBlueprint", Creature.Blueprint);
                if (50.in100())
                {
                    string killerBlueprint = EncountersAPI.GetACreatureBlueprint();
                    if (100.in100())
                    {
                        List<GameObject> cachedObjects = Event.NewGameObjectList(The.ZoneManager.CachedObjects.Values);
                        cachedObjects.RemoveAll(GO => !GO.IsAlive);
                        if (cachedObjects.GetRandomElement() is GameObject killer
                            && killer.HasID)
                        {
                            killerBlueprint = killer.Blueprint;
                            corpse.SetStringProperty("KillerID", killer.ID);
                        }
                        cachedObjects.Clear();
                    }
                    corpse.SetStringProperty("KillerBlueprint", killerBlueprint);
                }
                corpse.SetStringProperty("DeathReason", CheckpointingSystem.deathIcons.Keys.GetRandomElement());

                string genotype = Creature.GetGenotype();
                if (!genotype.IsNullOrEmpty())
                {
                    corpse.SetStringProperty("FromGenotype", genotype);
                }
                if (body != null)
                {
                    List<GameObject> list = null;
                    foreach (BodyPart part in body.GetParts())
                    {
                        if (part.Cybernetics != null)
                        {
                            list ??= Event.NewGameObjectList();
                            list.Add(part.Cybernetics);
                            UnimplantedEvent.Send(Creature, part.Cybernetics, part);
                            ImplantRemovedEvent.Send(Creature, part.Cybernetics, part);
                        }
                    }
                    if (list != null)
                    {
                        corpse.AddPart(new CyberneticsButcherableCybernetic(list));
                        corpse.RemovePart<Food>();
                    }
                }
                corpse.RequirePart<UD_FleshGolems_PastLife>();
                if (ForImmediateReanimation)
                {
                    if (!Creature.TryRequirePart(out UD_FleshGolems_DestinedForReanimation destinedForReanimation))
                    {
                        throw new InvalidOperationException("Failed to " + nameof(UD_FleshGolems.Extensions.TryRequirePart) + "<" + nameof(UD_FleshGolems_DestinedForReanimation) + ">");
                    }
                    if (!corpse.TryRequirePart(out UD_FleshGolems_CorpseReanimationHelper corpseReanimationHelper))
                    {
                        throw new InvalidOperationException("Failed to " + nameof(UD_FleshGolems.Extensions.TryRequirePart) + "<" + nameof(UD_FleshGolems_CorpseReanimationHelper) + ">");
                    }
                    destinedForReanimation.Corpse = corpse;
                    destinedForReanimation.BuiltToBeReanimated = true;
                    corpseReanimationHelper.AlwaysAnimate = true;
                }
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(ProduceCorpse), x, "game_mod_exception");
            }
            return corpse;
        }

        public static bool TryProduceCorpse(GameObject Creature, out GameObject Corpse, bool ForImmediateReanimation = true)
        {
            return (Corpse = ProduceCorpse(Creature, ForImmediateReanimation)) != null;
        }

        public static bool Unkill(GameObject Creature, out GameObject Corpse, string Context = null)
        {
            Corpse = null;
            if (!IsGameRunning)
            {
                // return false;
            }
            if (Context == "Sample")
            {
                return false;
            }
            if (Creature.HasPart<UD_FleshGolems_ReanimatedCorpse>())
            {
                return false;
            }
            if (!Creature.IsAlive)
            {
                return false;
            }
            if (!TryProduceCorpse(Creature, out Corpse))
            {
                return false;
            }

            if (Creature.IsPlayer())
            {
                if (Corpse == null)
                {
                    Popup.Show("Something terrible has happened (not really, it just failed).\n\nCheck Player.log for errors.");
                    return false;
                }
                else
                {
                    UD_FleshGolems_DestinedForReanimation.FakeRandomDeath(The.Player);
                    Corpse.SetIntProperty("UD_FleshGolems_SkipLevelsOnReanimate", 1);
                    Corpse.RequirePart<Inventory>();
                    Metamorphosis.TransferInventory(Creature, Corpse);
                    ReplaceInContextEvent.Send(Creature, Corpse);
                    The.Game.Player.SetBody(Corpse);
                    Creature.MakeInactive();
                    Corpse.MakeActive();
                }
            }
            return true;
        }

        [WishCommand("UD_FleshGolems reanimated")]
        public static void Reanimated_WishHandler()
        {
            Reanimated_WishHandler(null);
        }
        [WishCommand("UD_FleshGolems reanimated", null)]
        public static bool Reanimated_WishHandler(string Blueprint)
        {
            GameObject soonToBeCorpse;
            if (Blueprint == null)
            {
                if (Popup.ShowYesNo(
                    "This {{Y|probably}} won't end your run. " +
                    "Last chance to back out.\n\n" +
                    "If you meant to reanimate something else," +
                    "make this wish again but include a blueprint.") == DialogResult.No)
                {
                    return false;
                }
                soonToBeCorpse = The.Player;
            }
            else
            {
                WishResult wishResult = WishSearcher.SearchForBlueprint(Blueprint);
                soonToBeCorpse = GameObjectFactory.Factory.CreateObject(wishResult.Result, Context: "Wish");
            }
            if (Unkill(soonToBeCorpse, out GameObject soonToBeCreature, Context: "Wish"))
            {
                if (!soonToBeCreature.HasPart<AnimatedObject>()
                    && soonToBeCreature.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper corpseReanimationHelper))
                {
                    // corpseReanimationHelper.AlwaysAnimate = true;
                }
                if (Blueprint != null)
                {
                    The.PlayerCell.getClosestEmptyCell().AddObject(soonToBeCreature);
                }
                else
                if (Blueprint == null && false)
                {
                    if (soonToBeCreature == null)
                    {
                        Popup.Show("Something terrible has happened (not really, it just failed).\n\nCheck Player.log for errors.");
                        return false;
                    }
                    else
                    {
                        UD_FleshGolems_DestinedForReanimation.FakeRandomDeath(The.Player);
                        soonToBeCreature.SetIntProperty("UD_FleshGolems_SkipLevelsOnReanimate", 1);
                        soonToBeCreature.RequirePart<Inventory>();
                        Metamorphosis.TransferInventory(soonToBeCorpse, soonToBeCreature);
                        ReplaceInContextEvent.Send(soonToBeCorpse, soonToBeCreature);
                        The.Game.Player.SetBody(soonToBeCreature);
                        soonToBeCorpse.MakeInactive();
                        soonToBeCreature.MakeActive();
                    }
                }
                return true;
            }
            return false;
        }
    }
}
