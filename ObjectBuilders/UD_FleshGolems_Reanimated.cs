
using System;
using System.Collections.Generic;
using System.Linq;

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
using static UD_FleshGolems.Const;
using XRL.World.WorldBuilders;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Capabilities;
using UD_FleshGolems.Capabilities.Necromancy;

namespace XRL.World.ObjectBuilders
{
    [Serializable]
    [HasWishCommand]
    public class UD_FleshGolems_Reanimated : IObjectBuilder
    {
        public static bool IsGameRunning => The.Game != null && The.Game.Running;
        public static bool HasWorldGenerated => IsGameRunning && The.Player != null;

        public static UD_FleshGolems_NecromancySystem NecromancySystem => UD_FleshGolems_NecromancySystem.System;

        public static string ReanimatedEquipped => nameof(UD_FleshGolems_Reanimated) + ":Equipped";

        public static List<string> PartsThatNeedDelayedReanimation => new()
        {
            nameof(ReplaceObject),
            nameof(ConvertSpawner),
            nameof(CherubimSpawner),
        };
        public static List<string> BlueprintsThatNeedDelayedReanimation => new()
        {
            "BaseCherubimSpawn",
        };
        public static List<string> PropertiesAndTagsIndicatingNeedDelayedReanimation => new()
        {
            "AlternateCreatureType",
        };

        public override void Initialize()
        {
        }

        public override void Apply(GameObject Object, string Context = null)
        {
            Unkill(Object, Context);
        }

        public static bool CreatureNeedsDelayedReanimation(GameObject Creature)
        {
            return PartsThatNeedDelayedReanimation.Any(s => Creature.HasPart(s))
                || BlueprintsThatNeedDelayedReanimation.Any(s => Creature.GetBlueprint().InheritsFrom(s))
                || PropertiesAndTagsIndicatingNeedDelayedReanimation.Any(s => Creature.HasPropertyOrTag(s));
        }

        public static GameObject ProduceCorpse(GameObject Creature,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true,
            bool PreemptivelyGiveEnergy = true)
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
                    if (corpseBlueprint != null
                        && corpseBlueprint.IsCorpse())
                        corpseBlueprintName = corpsePart.CorpseBlueprint;
                }
                if (NecromancySystem != null
                    && NecromancySystem.RequireEntityBlueprint(Creature) is EntityBlueprint entityBlueprint)
                {
                    List<CorpseWeight> corpseWeights = new();

                    foreach (CorpseSheet corpseSheet in NecromancySystem.GetCorpseSheets((CorpseSheet cs) => cs.CorpseHasEntity(entityBlueprint)))
                        if (corpseSheet.GetCorpseWeight(entityBlueprint) is CorpseWeight corpseWeight)
                            corpseWeights.Add(corpseWeight);

                    if (!(corpseBlueprintName = corpseWeights?.ToDictionary(cw => cw.GetBlueprint().ToString(), cw => cw.Weight)?.GetWeightedRandom()).IsNullOrEmpty())
                        corpseBlueprint = corpseBlueprintName?.GetGameObjectBlueprint();
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
                        corpseBlueprint = creatureBaseBlueprint?.GetGameObjectBlueprint()?.GetCorpseBlueprint()?.GetGameObjectBlueprint();
                        corpseBlueprintName = corpseBlueprint?.GetCorpseBlueprint();
                    }

                    corpseBlueprintName = corpseBlueprint?.Name ?? fallbackCorpse;
                }
                if (!corpseBlueprintName.IsNullOrEmpty() && !corpseBlueprintName.GetGameObjectBlueprint().IsCorpse())
                {
                    corpseBlueprintName = null;
                }
                if ((corpse = GameObject.Create(corpseBlueprintName, Context: nameof(UD_FleshGolems_PastLife))) == null)
                {
                    return null;
                }
                Parts.Temporary.CarryOver(Creature, corpse);
                Phase.carryOver(Creature, corpse);
                if (Utils.WasProperlyNamed(Creature))
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
                    if (50.in100())
                    {
                        List<GameObject> cachedObjects = Event.NewGameObjectList(The.ZoneManager.CachedObjects.Values);
                        cachedObjects.RemoveAll(GO => (!GO.HasPart<Combat>() || !GO.HasPart<Body>()) && !GO.HasTagOrProperty("BodySubstitute"));
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
                string deathReason =
                    UD_FleshGolems_DestinedForReanimation.DeathCategoryDeathMessages
                        ?.Values
                        ?.GetRandomElementCosmetic()
                        ?.GetRandomElementCosmetic()
                    ?? CheckpointingSystem.deathIcons
                        ?.Keys
                        ?.GetRandomElement();

                corpse.SetStringProperty("DeathReason", deathReason);

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
                        var butcherableCybernetics = corpse.RequirePart<CyberneticsButcherableCybernetic>();
                        butcherableCybernetics.Cybernetics.AddRange(list);
                        corpse.RemovePart<Food>();
                    }
                }
                if (OverridePastLife)
                {
                    corpse.RemovePart<UD_FleshGolems_PastLife>();
                }

                var pastLife = corpse.RequirePart<UD_FleshGolems_PastLife>();

                if (Creature.TryGetPart(out UD_FleshGolems_PastLife prevPastLife)
                    && prevPastLife.Init && prevPastLife.WasCorpse)
                {
                    corpse.RemovePart(pastLife);
                    pastLife = corpse.AddPart(prevPastLife);
                }
                else
                {
                    pastLife.Initialize(Creature);
                }

                corpse.RequirePart<UD_FleshGolems_PastLife>().Initialize(Creature);
                if (ForImmediateReanimation)
                {
                    var corpseReanimationHelper = corpse.RequirePart<UD_FleshGolems_CorpseReanimationHelper>();
                    corpseReanimationHelper.AlwaysAnimate = true;

                    var destinedForReanimation = Creature.RequirePart<UD_FleshGolems_DestinedForReanimation>();
                    destinedForReanimation.Corpse = corpse;
                    destinedForReanimation.BuiltToBeReanimated = true;
                    if (PartsThatNeedDelayedReanimation.Any(s => Creature.HasPart(s)))
                    {
                        destinedForReanimation.DelayTillZoneBuild = true;
                    }
                }

                if (PreemptivelyGiveEnergy) // fixes cases where corpses are being added to the action queue before they've been animated.
                {
                    corpse.Statistics ??= new();
                    string energyStatName = "Energy";
                    Statistic energyStat = null;
                    if (GameObjectFactory.Factory.GetBlueprintIfExists(nameof(Creature)) is var baseCreatureBlueprint)
                    {
                        if (!baseCreatureBlueprint.Stats.IsNullOrEmpty()
                            && baseCreatureBlueprint.Stats.ContainsKey(energyStatName))
                        {
                            energyStat = new(baseCreatureBlueprint.Stats[energyStatName])
                            {
                                Owner = corpse,
                            };
                        }
                    }
                    else
                    {
                        energyStat = new(energyStatName, -100000, 100000, 0, corpse);
                    }
                    corpse.Statistics.TryAdd(energyStatName, energyStat);
                }
                corpse.RequireAbilities();
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
            Corpse = ProduceCorpse(Creature, ForImmediateReanimation, OverridePastLife);
            return Corpse != null;
        }

        public static bool TransferInventory(GameObject Creature, GameObject Corpse)
        {
            if (Creature == null
                || Corpse == null)
            {
                return false;
            }
            Inventory creatureInventory = Creature.RequirePart<Inventory>();
            Inventory corpseInventory = Corpse.RequirePart<Inventory>();
            Corpse.Inventory = corpseInventory;
            int erroredItems = 0;
            bool any = false;
            while (creatureInventory.Objects.Count > erroredItems)
            {
                try
                {
                    GameObject inventoryItem = creatureInventory.Objects[0];
                    creatureInventory.RemoveObject(inventoryItem);
                    corpseInventory.AddObject(inventoryItem);
                    any = true;
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(Debug.GetCallingTypeAndMethod(TrimModPrefix: false) + " transfer", x, "game_mod_exception");
                    erroredItems++;
                }
            }
            if (Creature.Body is not Body creatureBody
                || Corpse.Body is not Body corpseBody)
            {
                return any;
            }
            List<GameObject> equippedItems = Event.NewGameObjectList();
            List<BodyPart> equippedLimbs = new();
            List<KeyValuePair<BodyPart, GameObject>> creatureEquippedLimbItems = new();
            foreach (BodyPart bodyPart in creatureBody.LoopParts().Where(bp => bp.Equipped != null && !bp.Equipped.IsNatural()))
            {
                try
                {
                    if (bodyPart.Equipped is GameObject equippedItem
                        && !equippedItem.IsNatural())
                    {
                        equippedItem.SetStringProperty(ReanimatedEquipped, bodyPart.Type);

                        Creature.FireEvent(Event.New("CommandUnequipObject", "BodyPart", bodyPart, "SemiForced", 1));

                        creatureEquippedLimbItems.Add(new(bodyPart, equippedItem));
                    }
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(Debug.GetCallingTypeAndMethod(TrimModPrefix: false) + " unequip", x, "game_mod_exception");
                }
            }
            creatureInventory.Clear();
            // creatureInventory.Objects.Clear();
            /*
            foreach ((BodyPart creatureBodyPart, GameObject previouslyEquippedItem) in creatureEquippedLimbItems)
            {
                try
                {
                    if (corpseBody.GetPartByName(creatureBodyPart.Name) is BodyPart corpseMatchingBodyPart)
                    {
                        if (corpseMatchingBodyPart.Equipped?.DisplayName == previouslyEquippedItem?.DisplayName)
                        {
                            corpseMatchingBodyPart._Equipped = previouslyEquippedItem;
                            previouslyEquippedItem.Physics._Equipped = Corpse;
                            continue;
                        }
                        else
                        if (corpseMatchingBodyPart.Equipped == null)
                        {
                            if (Corpse.FireEvent(Event.New("CommandEquipObject", "Object", previouslyEquippedItem, "BodyPart", corpseMatchingBodyPart))
                                && corpseMatchingBodyPart.Equipped == previouslyEquippedItem)
                                continue;
                        }
                    }

                    if ((corpseMatchingBodyPart = corpseBody.GetUnequippedPart(creatureBodyPart.Type)?.FirstOrDefault()) is not null
                        || !Corpse.FireEvent(Event.New("CommandEquipObject", "Object", previouslyEquippedItem, "BodyPart", corpseMatchingBodyPart))
                        || corpseMatchingBodyPart.Equipped != previouslyEquippedItem)
                        if (!corpseInventory.Objects.Contains(previouslyEquippedItem))
                        {
                            corpseInventory.AddObject(previouslyEquippedItem);
                            continue;
                        }

                    if (previouslyEquippedItem.Equipped != Corpse
                        && (corpseMatchingBodyPart = corpseBody.GetUnequippedPart(creatureBodyPart.Type)?.FirstOrDefault()) is not null)
                        Corpse.FireEvent(Event.New("CommandEquipObject", "Object", previouslyEquippedItem, "BodyPart", corpseMatchingBodyPart));

                }
                catch (Exception x)
                {
                    MetricsManager.LogException(Debug.GetCallingTypeAndMethod(TrimModPrefix: false) + " equip", x, "game_mod_exception");

                    corpseInventory.AddObject(previouslyEquippedItem);
                }
            }
            */
            return any
                && EquipPastLifeItems(Corpse);
        }

        public static bool EquipPastLifeItems(GameObject FrankenCorpse, bool RemoveProperty = false)
        {
            if (FrankenCorpse == null
                || FrankenCorpse.Body is not Body frankenBody
                || FrankenCorpse.Inventory is not Inventory frankenInventory)
                return false;

            bool any = false;

            List<int> equippedBodyParts = new();
            bool bodyPartNotHasBeenEquipped(BodyPart BodyPart)
                => !equippedBodyParts.Contains(BodyPart.ID);

            foreach (GameObject inventoryItem in frankenInventory.GetObjects(GO => GO.HasStringProperty(ReanimatedEquipped)))
            {
                try
                {
                    if (inventoryItem.GetStringProperty(ReanimatedEquipped) is string bodyPartType
                        && frankenBody.GetUnequippedPart(bodyPartType)?.Where(bodyPartNotHasBeenEquipped).ToList() is List<BodyPart> unequippedParts
                        && unequippedParts.GetRandomElementCosmetic() is BodyPart equippablePart)
                        any = FrankenCorpse.FireEvent(Event.New("CommandEquipObject", "Object", inventoryItem, "BodyPart", equippablePart)) || any;
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(Debug.GetCallingTypeAndMethod(TrimModPrefix: false), x, "game_mod_exception");
                }
                finally
                {
                    if (RemoveProperty)
                        inventoryItem.RemoveStringProperty(ReanimatedEquipped);
                }
            }
            return any;
        }

        public static bool TryTransferInventoryToCorpse(GameObject soonToBeCorpse, GameObject soonToBeCreature)
        {
            bool transferred;
            try
            {
                soonToBeCreature.RequirePart<Inventory>();
                soonToBeCorpse.RequirePart<Inventory>();

                transferred = TransferInventory(soonToBeCorpse, soonToBeCreature);
            }
            catch (Exception x)
            {
                MetricsManager.LogException(Debug.GetCallingTypeAndMethod(TrimModPrefix: false), x, "game_mod_exception");
                transferred = false;
            }
            return transferred;
        }

        public static bool Unkill(GameObject Creature, out GameObject Corpse, string Context = null)
        {
            Corpse = null;
            if (!HasWorldGenerated)
            {
                // return false;
            }
            if (Context == "Sample")
            {
                return false;
            }
            if (Context == nameof(UD_FleshGolems_MadMonger_WorldBuilder))
            {
                return false;
            }
            if (Creature.HasPart<UD_FleshGolems_ReanimatedCorpse>())
            {
                return false;
            }
            if (!TryProduceCorpse(Creature, out Corpse))
            {
                return false;
            }

            if (Creature.IsPlayer())
            {
                if (Corpse == null || !ReplacePlayerWithCorpse(Corpse: Corpse))
                {
                    Popup.Show("Something terrible has happened (not really, it just failed).\n\nCheck Player.log for errors.");
                    return false;
                }
            }
            else
            if (HasWorldGenerated)
            {
                if (Corpse == null || !ReplaceCreatureWithCorpse(Creature, FakeDeath: true, Corpse: Corpse, ForImmediateReanimation: true, OverridePastLife: true))
                {
                    return false;
                }
            }
            return true;
        }
        public static bool Unkill(GameObject Creature, string Context = null)
        {
            return Unkill(Creature, out _, Context);
        }

        public static bool ReplaceCreatureWithCorpse(
            GameObject Creature,
            bool FakeDeath,
            out bool FakedDeath,
            IDeathEvent DeathEvent = null,
            GameObject Corpse = null,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true)
        {
            FakedDeath = false;
            if (Creature == null
                || (Corpse == null
                    && !TryProduceCorpse(Creature, out Corpse, ForImmediateReanimation, OverridePastLife)))
            {
                return false;
            }
            if (!TryTransferInventoryToCorpse(Creature, Corpse))
            {
                MetricsManager.LogModError(Utils.ThisMod, 
                    "Failed to " + nameof(ReplaceCreatureWithCorpse) + " due to failure of " + nameof(TryTransferInventoryToCorpse));
                return false;
            }
            bool replaced = false;
            try
            {
                if (FakeDeath)
                {
                    if (DeathEvent == null)
                    {
                        FakedDeath = UD_FleshGolems_DestinedForReanimation.FakeRandomDeath(Creature);
                    }
                    else
                    {
                        FakedDeath = UD_FleshGolems_DestinedForReanimation.FakeDeath(Creature, DeathEvent, DoAchievement: true);
                    }
                }

                Corpse.RequireAbilities();

                if (Creature.IsPlayer() || Creature.Blueprint.IsPlayerBlueprint())
                {
                    Corpse.SetIntProperty("UD_FleshGolems_SkipLevelsOnReanimate", 1);
                }

                ReplaceInContextEvent.Send(Creature, Corpse);

                if (Creature.IsPlayer() || Creature.Blueprint.IsPlayerBlueprint())
                {
                    The.Game.Player.SetBody(Corpse);
                    if (Corpse.Render is Render corpseRender
                        && Corpse.TryGetPart(out UD_FleshGolems_ReanimatedCorpse reanimatedCorpsePart))
                    {
                        reanimatedCorpsePart.RenderDisplayNameSetAltered();
                        corpseRender.DisplayName = The.Game.PlayerName;
                        if (Corpse.TryGetPart(out UD_FleshGolems_PastLife pastLifePart))
                        {
                            pastLifePart.BrainInAJar.Render.DisplayName = The.Game.PlayerName;
                            The.Game.PlayerName = pastLifePart.GenerateDisplayName();
                            corpseRender.DisplayName = The.Game.PlayerName;
                        }
                    }
                }

                Creature.MakeInactive();
                Corpse.MakeActive();

                bool doIDSwap = true;
                if (doIDSwap)
                {
                    string creatureID = Creature.ID;
                    int creatureBaseID = Creature.BaseID;

                    Creature.ID = Corpse.ID;
                    Creature.BaseID = Corpse.BaseID;

                    Corpse.ID = creatureID;
                    Corpse.BaseID = creatureBaseID;
                }

                Creature.Obliterate();
                replaced = true;
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(UD_FleshGolems_Reanimated) + "." + nameof(ReplaceCreatureWithCorpse), x, "game_mod_exception");
                replaced = false;
            }
            return replaced;
        }
        public static bool ReplaceCreatureWithCorpse(
            GameObject Player,
            bool FakeDeath = true,
            IDeathEvent DeathEvent = null,
            GameObject Corpse = null,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true)
        {
            return ReplaceCreatureWithCorpse(Player, FakeDeath, out _, DeathEvent, Corpse, ForImmediateReanimation, OverridePastLife);
        }
        public static bool ReplacePlayerWithCorpse(
            bool FakeDeath,
            out bool FakedDeath,
            IDeathEvent DeathEvent = null,
            GameObject Corpse = null,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true)
        {
            return ReplaceCreatureWithCorpse(The.Player, FakeDeath, out FakedDeath, DeathEvent, Corpse, ForImmediateReanimation, OverridePastLife);
        }
        public static bool ReplacePlayerWithCorpse(
            bool FakeDeath = true,
            IDeathEvent DeathEvent = null,
            GameObject Corpse = null,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true)
        {
            return ReplaceCreatureWithCorpse(The.Player, FakeDeath, out _, DeathEvent, Corpse, ForImmediateReanimation, OverridePastLife);
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
                    // "Reanimating the player by wish is currently broken.\n\n" +
                    // "If you meant to reanimate something else, " +
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
                    corpseReanimationHelper.AlwaysAnimate = true;
                }
                if (Blueprint != null)
                {
                    The.PlayerCell.getClosestEmptyCell().AddObject(soonToBeCreature);
                }
                else
                if (Blueprint == null)
                {
                    if (soonToBeCreature == null && !ReplaceCreatureWithCorpse(soonToBeCorpse, true, null, soonToBeCreature))
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
