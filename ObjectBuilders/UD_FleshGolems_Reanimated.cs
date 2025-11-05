
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

        public static GameObject ProduceCorpse(GameObject Creature,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true)
        {
            GameObject corpse = null;
            try
            {
                Body body = Creature.Body;
                string corpseBlueprintName = null;
                GameObjectBlueprint corpseBlueprint = null;
                if (Creature.TryGetPart(out Corpse corpsePart)
                    && !corpsePart.CorpseBlueprint.IsNullOrEmpty())
                {
                    corpseBlueprint = GameObjectFactory.Factory.GetBlueprintIfExists(corpsePart?.CorpseBlueprint);
                    if (corpseBlueprint.InheritsFrom("Corpse"))
                    {
                        corpseBlueprintName = corpsePart.CorpseBlueprint;
                    }
                }
                if (corpseBlueprintName.IsNullOrEmpty())
                { 
                    string creatureBaseBlueprint = Creature.GetBlueprint().GetBaseTypeName();
                    corpseBlueprintName = creatureBaseBlueprint + " Corpse";
                    corpseBlueprint = GameObjectFactory.Factory.GetBlueprintIfExists(corpseBlueprintName);

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
                if (OverridePastLife)
                {
                    corpse.RemovePart<UD_FleshGolems_PastLife>();
                }
                corpse.RequirePart<UD_FleshGolems_PastLife>().Initialize(Creature);
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

        public static bool TryProduceCorpse(
            GameObject Creature,
            out GameObject Corpse,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true)
        {
            return (Corpse = ProduceCorpse(Creature, ForImmediateReanimation, OverridePastLife)) != null;
        }

        public static bool TryTransferInventoryToCorpse(GameObject soonToBeCorpse, GameObject soonToBeCreature)
        {
            bool transferred = false;
            try
            {
                soonToBeCreature.RequirePart<Inventory>();
                soonToBeCorpse.RequirePart<Inventory>();
                Metamorphosis.TransferInventory(soonToBeCorpse, soonToBeCreature, bTagLastEquipped: false);
                transferred = true;
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(UD_FleshGolems_Reanimated) + "." + nameof(TryTransferInventoryToCorpse), x, "game_mod_exception");
                transferred = false;
            }
            return transferred;
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
                if (Corpse == null && !ReplacePlayerWithCorpse(Corpse))
                {
                    Popup.Show("Something terrible has happened (not really, it just failed).\n\nCheck Player.log for errors.");
                    return false;
                }
            }
            return true;
        }

        public static bool ReplacePlayerWithCorpse(
            out bool FakedDeath,
            GameObject Corpse = null,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true)
        {
            FakedDeath = false;
            GameObject Creature = The.Player;
            if (Corpse == null && !TryProduceCorpse(Creature, out Corpse, ForImmediateReanimation, OverridePastLife))
            {
                return false;
            }
            if (!TryTransferInventoryToCorpse(Creature, Corpse))
            {
                return false;
            }
            bool replaced = false;
            try
            {
                FakedDeath = UD_FleshGolems_DestinedForReanimation.FakeRandomDeath(Creature);
                Corpse.SetIntProperty("UD_FleshGolems_SkipLevelsOnReanimate", 1);

                ReplaceInContextEvent.Send(Creature, Corpse);
                The.Game.Player.SetBody(Corpse);

                Creature.MakeInactive();
                Corpse.MakeActive();
                replaced = true;
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(UD_FleshGolems_Reanimated) + "." + nameof(ReplacePlayerWithCorpse), x, "game_mod_exception");
                replaced = false;
            }
            return replaced;
        }
        public static bool ReplacePlayerWithCorpse(
            GameObject Corpse = null,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true)
        {
            return ReplacePlayerWithCorpse(out _, Corpse, ForImmediateReanimation, OverridePastLife);
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
                    if (soonToBeCreature == null && !ReplacePlayerWithCorpse(soonToBeCreature))
                    {
                        Popup.Show("Something terrible has happened (not really, it just failed).\n\nCheck Player.log for errors.");
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
