
using System;
using System.Collections.Generic;

using ConsoleLib.Console;

using Qud.API;

using XRL;
using XRL.Names;
using XRL.UI;
using XRL.Wish;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Capabilities;
using XRL.World.ObjectBuilders;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

namespace XRL.World.ObjectBuilders
{
    [Serializable]
    [HasWishCommand]
    public class UD_FleshGolems_Reanimated : IObjectBuilder
    {
        public override void Initialize()
        {
        }

        public override void Apply(GameObject Object, string Context = null)
        {
            Unkill(Object, out _, Context);
        }

        public static GameObject ProduceCorpse(GameObject Creature)
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
                    corpseBlueprintName = GameObjectFactory.Factory.GetBlueprintIfExists(creatureBaseBlueprint)
                        .GetPartParameter(nameof(Parts.Corpse), nameof(Parts.Corpse.CorpseBlueprint), speciesCorpse);
                }
                corpseBlueprint = GameObjectFactory.Factory.GetBlueprintIfExists(corpseBlueprintName);

                corpseBlueprintName = corpseBlueprint?.Name ?? fallbackCorpse;
            }
            else
            {
                corpseBlueprintName = corpsePart.CorpseBlueprint;
            }
            if (GameObject.Create(corpseBlueprintName) is not GameObject corpse)
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
            return corpse;
        }

        public static bool TryProduceCorpse(GameObject Creature, out GameObject Corpse)
        {
            return (Corpse = ProduceCorpse(Creature)) != null;
        }

        public static bool Unkill(GameObject Creature, out GameObject Corpse, string Context = null)
        {
            Corpse = null;
            if (Creature.HasPart<UD_FleshGolems_ReanimatedCorpse>())
            {
                return false;
            }
            if (!Creature.IsAlive)
            {
                return false;
            }
            if (Creature.RequirePart<UD_FleshGolems_DestinedForReanimation>() is not UD_FleshGolems_DestinedForReanimation destinedForReanimation)
            {
                return false;
            }
            if (!TryProduceCorpse(Creature, out destinedForReanimation.Corpse))
            {
                return false;
            }
            Corpse = destinedForReanimation.Corpse;
            destinedForReanimation.BuiltToBeReanimated = true;
            if (Corpse.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper corpseReanimationHelper)
                && !corpseReanimationHelper.AlwaysAnimate)
            {
                corpseReanimationHelper.AlwaysAnimate = true;
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
                    && soonToBeCreature.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper corpseReanimationHelper)
                    && !corpseReanimationHelper.AlwaysAnimate)
                {
                    corpseReanimationHelper.AlwaysAnimate = true;
                }
                if (Blueprint != null)
                {
                    The.PlayerCell.getClosestEmptyCell().AddObject(soonToBeCreature);
                }
                else
                {
                    The.Player.Die();
                }
                return true;
            }
            return false;
        }
    }
}
