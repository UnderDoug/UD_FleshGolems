
using System;
using ConsoleLib.Console;
using XRL;
using XRL.Wish;
using XRL.World;
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
            if (!Creature.TryGetPart(out Corpse creatureCorpsePart))
            {
                creatureCorpsePart = Creature.RequirePart<Corpse>();
                string creatureBaseBlueprint = Creature.GetBlueprint().GetBaseTypeName();
                string corpseBlueprintName = creatureBaseBlueprint + " Corpse";
                var corpseBlueprint = GameObjectFactory.Factory.GetBlueprintIfExists(corpseBlueprintName);

                string speciesCorpse = Creature.GetSpecies() + " " + nameof(Corpse);
                string fallbackCorpse = "Fresh " + nameof(Corpse);

                if (corpseBlueprint == null)
                {
                    corpseBlueprintName = GameObjectFactory.Factory.GetBlueprintIfExists(creatureBaseBlueprint)
                        .GetPartParameter(nameof(Parts.Corpse), nameof(Parts.Corpse.CorpseBlueprint), speciesCorpse);
                }
                corpseBlueprint = GameObjectFactory.Factory.GetBlueprintIfExists(corpseBlueprintName);

                creatureCorpsePart.CorpseBlueprint = corpseBlueprint?.Name ?? fallbackCorpse;
            }
            creatureCorpsePart.CorpseChance = 100;
            creatureCorpsePart.BurntCorpseChance = 0;
            creatureCorpsePart.VaporizedCorpseChance = 0;
            Creature.SetIntProperty("SuppressCorpseDrops", 0);

            var destinedForReanimation = Creature.RequirePart<UD_FleshGolems_DestinedForReanimation>();
            destinedForReanimation.BuiltToBeReanimated = true;

            Creature.Die();

            Corpse = destinedForReanimation.Corpse;
            if (Context != "Wish")
            {
                ReplaceInContextEvent.Send(Creature, Corpse);
            }
            return true;
        }

        [WishCommand("UD_FleshGolems reanimated")]
        public static void Reanimated_WishHandler()
        {
            Reanimated_WishHandler(null);
        }
        [WishCommand("UD_FleshGolems reanimated", null)]
        public static void Reanimated_WishHandler(string Blueprint)
        {
            GameObject soonToBeCorpse;
            if (Blueprint == null)
            {
                soonToBeCorpse = The.Player;
            }
            else
            {
                WishResult wishResult = WishSearcher.SearchForBlueprint(Blueprint);
                soonToBeCorpse = GameObjectFactory.Factory.CreateObject(wishResult.Result, Context: "Wish");
            }
            if (Unkill(soonToBeCorpse, out GameObject reanimatedCorpse, Context: "Wish"))
            {
                if (Blueprint != null)
                {
                    The.PlayerCell.getClosestEmptyCell().AddObject(reanimatedCorpse);
                }
            }
        }
    }
}
