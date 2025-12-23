
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
using XRL.World.WorldBuilders;

using IdentityType = XRL.World.Parts.UD_FleshGolems_PastLife.IdentityType;
using static XRL.World.Parts.UD_FleshGolems_PastLife;
using static XRL.World.Parts.UD_FleshGolems_ReanimatedCorpse;

using UD_FleshGolems;
using static UD_FleshGolems.Const;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Capabilities;
using UD_FleshGolems.Capabilities.Necromancy;
using UD_FleshGolems.Parts.VengeanceHelpers;

namespace XRL.World.ObjectBuilders
{
    [Serializable]
    [HasWishCommand]
    public class UD_FleshGolems_Reanimated : IObjectBuilder
    {
        public const string CREATURE_BLUEPRINT = "Creature";

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
        public static List<string> PropTagsIndicatingNeedDelayedReanimation => new()
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

        public static bool EntityNeedsDelayedReanimation(GameObject Creature)
            => PartsThatNeedDelayedReanimation.Any(s => Creature.HasPart(s))
            || BlueprintsThatNeedDelayedReanimation.Any(s => Creature.GetBlueprint().InheritsFrom(s))
            || PropTagsIndicatingNeedDelayedReanimation.Any(s => Creature.HasPropertyOrTag(s));

        public static bool IsOntologicallyAnEntity(GameObject Object)
            => (Object.HasPart<Combat>() && Object.HasPart<Body>())
            || Object.HasTagOrProperty("BodySubstitute");

        public static bool CorpseModelIsAcceptable(GameObjectBlueprint CorpseModel)
            => CorpseModel != null
            && !CorpseModel.IsBaseBlueprint()
            && !CorpseModel.IsExcludedFromDynamicEncounters();

        public static bool CorpseSheetHasAcceptableCorpse(CorpseSheet CorpseSheet, string Entity)
            => CorpseSheet.CorpseHasEntity(Entity)
            && CorpseSheet.GetCorpse() is CorpseBlueprint corpseBlueprint
            && corpseBlueprint.GetGameObjectBlueprint() is GameObjectBlueprint corpseModel
            && CorpseModelIsAcceptable(corpseModel);

        public static bool TryGetRandomCorpseFromNecronomicon(string Entity, out GameObjectBlueprint CorpseModel, Predicate<CorpseSheet> CorpseSheetFilter = null)
            => (CorpseModel = NecromancySystem
                ?.GetWeightedCorpseStringsForEntity(Entity, CorpseSheetFilter)
                ?.GetWeightedRandom()
                ?.GetGameObjectBlueprint()) != null;

        public static GameObject ProduceCorpse(
            GameObject Entity,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true,
            bool PreemptivelyGiveEnergy = true)
        {
            GameObject corpse = null;
            try
            {
                using Indent indent = new(1);
                Debug.LogMethod(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(Entity), Entity?.DebugName ?? NULL),
                        Debug.Arg(nameof(ForImmediateReanimation), ForImmediateReanimation),
                        Debug.Arg(nameof(OverridePastLife), OverridePastLife),
                        Debug.Arg(nameof(PreemptivelyGiveEnergy), PreemptivelyGiveEnergy),
                    });

                Body body = Entity.Body;
                string corpseBlueprint = null;
                GameObjectBlueprint corpseModel = null;

                if ((corpseBlueprint.IsNullOrEmpty() || !corpseModel.IsCorpse())
                    && Entity.TryGetPart(out Corpse corpsePart)
                    && !corpsePart.CorpseBlueprint.IsNullOrEmpty())
                {
                    corpseModel = GameObjectFactory.Factory.GetBlueprintIfExists(corpsePart?.CorpseBlueprint);
                    if (corpseModel != null
                        && corpseModel.IsCorpse())
                    {
                        corpseBlueprint = corpsePart.CorpseBlueprint;
                        Debug.CheckYeh("Entity's " + nameof(corpseBlueprint), corpseBlueprint ?? NULL, Indent: indent[1]);
                    }
                }

                bool corpseSheetHasAcceptableCorpse(CorpseSheet CorpseSheet)
                    => CorpseSheetHasAcceptableCorpse(CorpseSheet, Entity.Blueprint);

                if ((corpseBlueprint.IsNullOrEmpty() || !corpseModel.IsCorpse())
                    && TryGetRandomCorpseFromNecronomicon(Entity.Blueprint, out corpseModel, corpseSheetHasAcceptableCorpse))
                {
                    corpseBlueprint = corpseModel?.Name;
                    Debug.CheckYeh(nameof(NecromancySystem) + " " + nameof(corpseBlueprint), corpseBlueprint ?? NULL, Indent: indent[1]);
                }
                if (corpseBlueprint.IsNullOrEmpty() || !corpseModel.IsCorpse())
                {
                    string creatureBaseBlueprint = Entity.GetBlueprint().GetBaseTypeName();
                    corpseBlueprint = creatureBaseBlueprint + " Corpse";
                    corpseModel = GameObjectFactory.Factory.GetBlueprintIfExists(corpseBlueprint);

                    if (corpseModel == null
                        || !CorpseModelIsAcceptable(corpseModel))
                    {
                        corpseModel = creatureBaseBlueprint
                            ?.GetGameObjectBlueprint() // get the creature's model
                            ?.GetCorpseBlueprint() // get the corpse blueprint for creature's model
                            ?.GetGameObjectBlueprint(); // get the corpse model
                        corpseBlueprint = corpseModel?.GetCorpseBlueprint();
                    }
                    Debug.CheckYeh("Base " + nameof(corpseBlueprint), corpseBlueprint ?? NULL, Indent: indent[1]);
                }
                if (corpseBlueprint.IsNullOrEmpty() || !corpseModel.IsCorpse())
                {
                    string speciesCorpseBlueprint = Entity.GetSpecies() + " " + nameof(Corpse);
                    if (speciesCorpseBlueprint.GetGameObjectBlueprint() is var speciesCorpseModel
                        && CorpseModelIsAcceptable(speciesCorpseModel))
                    {
                        corpseModel = speciesCorpseModel;
                        corpseBlueprint = speciesCorpseBlueprint;
                        Debug.CheckYeh("Species " + nameof(corpseBlueprint), corpseBlueprint ?? NULL, Indent: indent[1]);
                    }
                }
                if (corpseBlueprint.IsNullOrEmpty() || !corpseModel.IsCorpse())
                {
                    string fallbackCorpse = "Fresh " + nameof(Corpse);
                    if (fallbackCorpse.GetGameObjectBlueprint() is var fallbackCorpseModel
                        && CorpseModelIsAcceptable(fallbackCorpseModel))
                    {
                        corpseBlueprint = fallbackCorpse;
                        corpseModel = fallbackCorpseModel;
                        Debug.CheckYeh("Fallback " + nameof(corpseBlueprint), corpseBlueprint ?? NULL, Indent: indent[1]);
                    }
                }
                if (!corpseModel.IsCorpse())
                {
                    corpseBlueprint = null;
                    corpseModel = null;
                }
                if ((corpse = GameObject.Create(corpseBlueprint, Context: nameof(UD_FleshGolems_PastLife))) == null)
                {
                    Debug.CheckNah("Unable to find suitable corpse...", Indent: indent[1]);
                    return null;
                }
                Parts.Temporary.CarryOver(Entity, corpse);
                Phase.carryOver(Entity, corpse);
                if (Utils.WasProperlyNamed(Entity))
                {
                    corpse.SetStringProperty("CreatureName", Entity.BaseDisplayName);
                }
                else
                {
                    string creatureName = NameMaker.MakeName(Entity, FailureOkay: true);
                    if (creatureName != null)
                    {
                        corpse.SetStringProperty("CreatureName", creatureName);
                    }
                }
                if (Entity.HasID)
                {
                    corpse.SetStringProperty("SourceID", Entity.ID);
                }
                corpse.SetStringProperty("SourceBlueprint", Entity.Blueprint);

                var corpseReanimationHelper = corpse.RequirePart<UD_FleshGolems_CorpseReanimationHelper>();
                var deathDescription = UD_FleshGolems_DestinedForReanimation.ProduceRandomDeathDescriptionWithComponents(
                    Entity,
                    out GameObject killer,
                    out GameObject weapon,
                    out GameObject projectile,
                    out string deathCategory,
                    out string deathReason,
                    out bool accidental,
                    out bool killerIsCached);

                Debug.YehNah(nameof(deathDescription), deathDescription != null, Indent: indent[1]);
                var deathDetails = corpse.RequirePart<UD_FleshGolems_DeathDetails>();
                deathDetails.Initialize(killer, weapon, projectile, deathDescription, accidental, killerIsCached);

                if (killer != null
                    && killer.HasID)
                {
                    corpse.SetStringProperty("KillerID", deathDetails.KillerDetails?.ID);
                    corpse.SetStringProperty("KillerBlueprint", deathDetails.KillerDetails?.Blueprint);
                }
                corpse.SetStringProperty("DeathReason", deathReason);

                string genotype = Entity.GetGenotype();
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
                            UnimplantedEvent.Send(Entity, part.Cybernetics, part);
                            ImplantRemovedEvent.Send(Entity, part.Cybernetics, part);
                        }
                    }
                    if (list != null)
                    {
                        var butcherableCybernetics = corpse.RequirePart<CyberneticsButcherableCybernetic>();
                        butcherableCybernetics.Cybernetics.AddRange(list);
                        corpse.RemovePart<Food>();
                    }
                }

                if (PreemptivelyGiveEnergy) // fixes cases where corpses are being added to the action queue before they've been animated.
                {
                    corpse.Statistics ??= new();
                    string energyStatName = "Energy";
                    Statistic energyStat = null;
                    if (GameObjectFactory.Factory.GetBlueprintIfExists(CREATURE_BLUEPRINT) is var baseCreatureBlueprint)
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

                if (OverridePastLife)
                {
                    corpse.RemovePart<UD_FleshGolems_PastLife>();
                }

                var pastLife = corpse.RequirePart<UD_FleshGolems_PastLife>();

                if (Entity.TryGetPart(out UD_FleshGolems_PastLife prevPastLife)
                    && prevPastLife.Init && prevPastLife.WasCorpse)
                {
                    corpse.RemovePart(pastLife);
                    pastLife = corpse.AddPart(prevPastLife);
                }
                else
                {
                    pastLife.Initialize(Entity);
                }

                corpse.RequirePart<UD_FleshGolems_PastLife>().Initialize(Entity);
                if (ForImmediateReanimation)
                {
                    string reanimatedDisplayName = REANIMATED_ADJECTIVE + " " + corpse.Render.DisplayName;
                    // corpse.Render.DisplayName = reanimatedDisplayName;

                    corpseReanimationHelper.AlwaysAnimate = true;

                    var destinedForReanimation = Entity.RequirePart<UD_FleshGolems_DestinedForReanimation>();
                    destinedForReanimation.Corpse = corpse;
                    destinedForReanimation.BuiltToBeReanimated = true;
                    if (EntityNeedsDelayedReanimation(Entity) && false)
                    {
                        destinedForReanimation.DelayTillZoneBuild = true;
                    }
                }
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(ProduceCorpse), x, "game_mod_exception");
            }
            return corpse;
        }

        public static bool TryProduceCorpse(
            GameObject Entity,
            out GameObject Corpse,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true)
        {
            Corpse = ProduceCorpse(Entity, ForImmediateReanimation, OverridePastLife);
            return Corpse != null;
        }

        public static bool TransferInventory(GameObject Entity, GameObject Corpse)
        {
            if (Entity == null
                || Corpse == null)
            {
                return false;
            }
            Inventory entityInventory = Entity.RequirePart<Inventory>();
            Inventory corpseInventory = Corpse.RequirePart<Inventory>();
            Corpse.Inventory = corpseInventory;
            int erroredItems = 0;
            bool any = false;
            bool anyToTransfer = entityInventory.Objects.Count > 1;
            while (entityInventory.Objects.Count > erroredItems)
            {
                try
                {
                    GameObject inventoryItem = entityInventory.Objects[0];
                    entityInventory.RemoveObject(inventoryItem);
                    corpseInventory.AddObject(inventoryItem);
                    any = true;
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(Debug.GetCallingTypeAndMethod(TrimModPrefix: false) + " transfer", x, "game_mod_exception");
                    erroredItems++;
                }
            }
            if (Entity.Body is not Body entityBody
                || Corpse.Body is not Body corpseBody)
            {
                return any || !anyToTransfer;
            }
            List<GameObject> equippedItems = Event.NewGameObjectList();
            List<BodyPart> equippedLimbs = new();
            List<KeyValuePair<BodyPart, GameObject>> entityEquippedLimbItems = new();
            foreach (BodyPart bodyPart in entityBody.LoopParts().Where(bp => bp.Equipped != null && !bp.Equipped.IsNatural()))
            {
                try
                {
                    if (bodyPart.Equipped is GameObject equippedItem
                        && !equippedItem.IsNatural())
                    {
                        equippedItem.SetStringProperty(ReanimatedEquipped, bodyPart.Type);

                        Entity.FireEvent(Event.New("CommandUnequipObject", "BodyPart", bodyPart, "SemiForced", 1));

                        entityEquippedLimbItems.Add(new(bodyPart, equippedItem));
                    }
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(Debug.GetCallingTypeAndMethod(TrimModPrefix: false) + " unequip", x, "game_mod_exception");
                }
            }
            entityInventory.Clear();
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
            return !anyToTransfer
                || (any && EquipPastLifeItems(Corpse));
        }

        private static bool WantsToBeEquippedByReanimated(GameObject Item)
            => Item.HasStringProperty(ReanimatedEquipped);

        public static bool EquipPastLifeItems(GameObject FrankenCorpse, bool RemoveProperty = false)
        {
            if (FrankenCorpse == null
                || FrankenCorpse.Body is not Body frankenBody
                || FrankenCorpse.Inventory is not Inventory frankenInventory)
                return false;

            List<GameObject> itemsToEquip = frankenInventory.GetObjects(WantsToBeEquippedByReanimated);

            bool any = false;
            bool anyToEquip = (itemsToEquip?.Count ?? 0) > 1;

            List<int> equippedBodyParts = new();
            bool bodyPartNotHasBeenEquipped(BodyPart BodyPart)
                => !equippedBodyParts.Contains(BodyPart.ID);

            foreach (GameObject inventoryItem in itemsToEquip)
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
            return any || !anyToEquip;
        }

        public static bool TryTransferInventoryToCorpse(GameObject Entity, GameObject Corpse)
        {
            bool transferred;
            try
            {
                transferred = TransferInventory(Entity, Corpse);
            }
            catch (Exception x)
            {
                MetricsManager.LogException(Debug.GetCallingTypeAndMethod(TrimModPrefix: false), x, "game_mod_exception");
                transferred = false;
            }
            return transferred;
        }

        public static bool Unkill(GameObject Entity, out GameObject Corpse, string Context = null)
        {
            Corpse = null;
            UD_FleshGolems_DestinedForReanimation destinedForReanimation = null;
            if (Entity.IsPlayer() || Entity.HasPlayerBlueprint())
            {
                destinedForReanimation = Entity.RequirePart<UD_FleshGolems_DestinedForReanimation>();
                destinedForReanimation.PlayerWantsFakeDie = true;
                UD_FleshGolems_DestinedForReanimation.HaveFakedDeath = false;
                // return true;
            }
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
            if (Entity.HasPart<UD_FleshGolems_ReanimatedCorpse>())
            {
                return false;
            }
            if (!TryProduceCorpse(Entity, out Corpse))
            {
                return false;
            }
            if (!Corpse.TryGetPart(out destinedForReanimation))
            {
                return false;
            }
            if (Corpse == null)
            {
                if (Entity.IsPlayer())
                {
                    if (!ReplacePlayerWithCorpse())
                    {
                        Popup.Show("Something terrible has happened (not really, it just failed).\n\nCheck Player.log for errors.");
                        return false;
                    }
                }
                else
                {
                    if (!ReplaceEntityWithCorpse(Entity))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public static bool Unkill(GameObject Creature, string Context = null)
        {
            return Unkill(Creature, out _, Context);
        }

        public static bool ReplaceEntityWithCorpse(
            GameObject Entity,
            bool FakeDeath,
            out bool FakedDeath,
            IDeathEvent DeathEvent = null,
            GameObject Corpse = null,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Entity), Entity?.DebugName ?? NULL),
                    Debug.Arg(nameof(FakeDeath), FakeDeath),
                    Debug.Arg("out " + nameof(FakedDeath)),
                    Debug.Arg(nameof(DeathEvent), DeathEvent?.GetType()?.Name ?? NULL),
                    Debug.Arg(nameof(Corpse), Corpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(ForImmediateReanimation), ForImmediateReanimation),
                    Debug.Arg(nameof(OverridePastLife), OverridePastLife),
                });

            FakedDeath = false;
            if (Entity == null)
            {
                Debug.Log(nameof(Entity) + " null", Indent: indent[1]);
                return false;
            }

            if (Corpse == null
                && !TryProduceCorpse(Entity, out Corpse, ForImmediateReanimation, OverridePastLife))
            {
                Debug.Log(nameof(Corpse) + " null and couldn't produce one", Indent: indent[1]);
                return false;
            }

            Corpse.RequireAbilities();

            if (Entity.IsPlayer() || Entity.Blueprint.IsPlayerBlueprint())
            {
                Corpse.SetIntProperty("UD_FleshGolems_SkipLevelsOnReanimate", 1);
            }

            if (!ForImmediateReanimation)
            {
                return true;
            }

            if (!Corpse.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper reanimationHelper)
                || !reanimationHelper.Animate(out Corpse))
            {
                Debug.Log(nameof(UD_FleshGolems_CorpseReanimationHelper) + " missing or failed to " + nameof(reanimationHelper.Animate), Indent: indent[1]);
                return false;
            }

            if (!TryTransferInventoryToCorpse(Entity, Corpse))
            {
                MetricsManager.LogModError(Utils.ThisMod, 
                    "Failed to " + nameof(ReplaceEntityWithCorpse) + " due to failure of " + nameof(TryTransferInventoryToCorpse));
                return false;
            }

            bool replaced = false;
            try
            {
                if (FakeDeath)
                {
                    var deathDetails = Corpse.RequirePart<UD_FleshGolems_DeathDetails>();
                    if (DeathEvent == null)
                    {
                        FakedDeath = UD_FleshGolems_DestinedForReanimation.FakeRandomDeath(
                            Dying: Entity,
                            DeathDetails: ref deathDetails,
                            RelentlessIcon: Corpse.RenderForUI(),
                            RelentlessTitle: Corpse.GetReferenceDisplayName(Short: true));
                    }
                    else
                    {
                        deathDetails.Initialize(DeathEvent);
                        FakedDeath = UD_FleshGolems_DestinedForReanimation.FakeDeath(
                            Dying: Entity,
                            E: DeathEvent,
                            DoAchievement: true,
                            RelentlessIcon: Corpse.RenderForUI(),
                            RelentlessTitle: Corpse.GetReferenceDisplayName(Short: true));
                    }
                    deathDetails.KillerDetails?.Log();
                }

                ReplaceInContextEvent.Send(Entity, Corpse);

                if (Entity.IsPlayer() || Entity.Blueprint.IsPlayerBlueprint())
                {
                    The.Game.Player.SetBody(Corpse);

                    if (Corpse.TryGetPart(out UD_FleshGolems_ReanimatedCorpse reanimatedCorpsePart))
                    {
                        if (Corpse.Render is Render corpseRender)
                        {
                            reanimatedCorpsePart.RenderDisplayNameSetAltered();
                            corpseRender.DisplayName = The.Game.PlayerName;
                            if (Corpse.TryGetPart(out UD_FleshGolems_PastLife pastLifePart))
                            {
                                pastLifePart.BrainInAJar.Render.DisplayName = The.Game.PlayerName;
                                The.Game.PlayerName = pastLifePart.GenerateDisplayName();
                                corpseRender.DisplayName = Corpse.GetReferenceDisplayName(Short: true);
                            }
                        }
                        if (Corpse.TryGetPart(out Description description)
                            && reanimatedCorpsePart.NewDescription is string newDescription)
                        {
                            reanimatedCorpsePart.DescriptionSetAltered();
                            description._Short += "\n\n" + newDescription;
                        }
                    }

                    if (Entity.HasIntProperty("OriginalPlayerBody"))
                    {
                        Corpse.SetStringProperty("OriginalPlayerBody", "1");
                        Corpse.InjectGeneID("OriginalPlayer");
                    }
                    Corpse.Brain.Allegiance.Clear();
                    Corpse.Brain.Allegiance["Player"] = 100;

                    if (Corpse.TryGetPart(out UD_FleshGolems_PastLife pastLife))
                    {
                        pastLife.AlignWithPreviouslySentientBeings();
                    }
                }

                Entity.MakeInactive();
                Corpse.MakeActive();

                bool doIDSwap = false;
                if (doIDSwap)
                {
                    string entityID = Entity.ID;
                    int entityBaseID = Entity.BaseID;

                    Entity.ID = Corpse.ID;
                    Entity.BaseID = Corpse.BaseID;

                    Corpse.ID = entityID;
                    Corpse.BaseID = entityBaseID;

                    Debug.Log(nameof(Corpse) + ": " + Corpse.ID + "|" + nameof(Entity) + ": " + Entity.ID);
                }

                Entity.Obliterate();
                replaced = true;
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(UD_FleshGolems_Reanimated) + "." + nameof(ReplaceEntityWithCorpse), x, "game_mod_exception");
                replaced = false;
            }
            return replaced;
        }
        public static bool ReplaceEntityWithCorpse(
            GameObject Entity,
            bool FakeDeath = true,
            IDeathEvent DeathEvent = null,
            GameObject Corpse = null,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true)
        {
            return ReplaceEntityWithCorpse(Entity, FakeDeath, out _, DeathEvent, Corpse, ForImmediateReanimation, OverridePastLife);
        }
        public static bool ReplaceEntityWithCorpse(
            GameObject Entity,
            ref GameObject Corpse)
        {
            Corpse ??= ProduceCorpse(Entity);
            return ReplaceEntityWithCorpse(Entity, Corpse: Corpse);
        }
        public static bool ReplacePlayerWithCorpse(
            bool FakeDeath,
            out bool FakedDeath,
            IDeathEvent DeathEvent = null,
            GameObject Corpse = null,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true)
        {
            return ReplaceEntityWithCorpse(The.Player, FakeDeath, out FakedDeath, DeathEvent, Corpse, ForImmediateReanimation, OverridePastLife);
        }
        public static bool ReplacePlayerWithCorpse(
            bool FakeDeath = true,
            IDeathEvent DeathEvent = null,
            GameObject Corpse = null,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true)
        {
            return ReplaceEntityWithCorpse(The.Player, FakeDeath, out _, DeathEvent, Corpse, ForImmediateReanimation, OverridePastLife);
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
                    if (soonToBeCreature == null && !ReplaceEntityWithCorpse(soonToBeCorpse, true, null, soonToBeCreature))
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
