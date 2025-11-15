using System.Collections.Generic;
using System.Linq;
using System;

using Qud.UI;

using XRL;
using XRL.UI;
using XRL.World;

using static UD_FleshGolems.Const;
using static UD_FleshGolems.Options;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

namespace UD_FleshGolems
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    [HasCallAfterGameLoaded]
    public static class Startup
    {
        [GameBasedStaticCache( CreateInstance = false )]
        [ModSensitiveStaticCache]
        public static string _PlayerBlueprint = null;

        public static string PlayerBlueprint => _PlayerBlueprint ??= Utils.GetPlayerBlueprint();

        // Start-up calls in order that they happen.

        [ModSensitiveCacheInit]
        public static void ModSensitiveCacheInit()
        {
            // Called at game startup and whenever mod configuration changes
        }

        [GameBasedCacheInit]
        public static void GameBasedCacheInit()
        {
            // Called once when world is first generated.

            // The.Game registered events should go here.

            UnityEngine.Debug.Log( nameof(Startup) + "." + nameof(GameBasedCacheInit) + ", " + nameof(PlayerBlueprint) + ": " + PlayerBlueprint ?? NULL);
            CacheSomeCorpses();
        }

        // [PlayerMutator]

        // The.Player.FireEvent("GameRestored");
        // AfterGameLoadedEvent.Send(Return);  // Return is the game.

        [CallAfterGameLoaded]
        public static void OnLoadGameCallback()
        {
            // Gets called every time the game is loaded but not during generation

            UnityEngine.Debug.Log(nameof(Startup) + "." + nameof(GameBasedCacheInit) + ", " + nameof(PlayerBlueprint) + ": " + PlayerBlueprint ?? NULL);
            CacheSomeCorpses();
        }

        //
        // End Startup calls
        // 

        public static void CacheSomeCorpses()
        {
            UD_FleshGolems_PastLife.GetCorpseBlueprints();
            UD_FleshGolems_PastLife.GetEntitiesWithCorpseBlueprints();
            UD_FleshGolems_PastLife.GetProcessableCorpsesAndTheirProducts();
        }
    }

    // [ModSensitiveCacheInit]

    // [GameBasedCacheInit]

    [PlayerMutator]
    public class UD_FleshGolems_PlayerMutator : IPlayerMutator
    {
        // Called once when the player is generated (a fair bit after they're created.
        public void mutate(GameObject player)
        {
            if (DebugEnableTestKit)
            {
                Mutations playerMutations = player.RequirePart<Mutations>();
                if (player.GetPartsDescendedFrom<UD_FleshGolems_NanoNecroAnimation>().IsNullOrEmpty()
                    && !player.HasPart<UD_FleshGolems_NanoNecroAnimation>())
                {
                    Dictionary<string, string> reanimationMutationEntries = new()
                    {
                        { "Physical", nameof(UD_FleshGolems_NanoNecroAnimation) },
                        { "Mental", nameof(UD_FleshGolems_NecromanticAura) }
                    };
                    if (player.IsChimera())
                    {
                        playerMutations.AddMutation(reanimationMutationEntries["Physical"]);
                    }
                    else
                    if (player.IsEsper())
                    {
                        playerMutations.AddMutation(reanimationMutationEntries["Mental"]);
                    }
                    else
                    {
                        playerMutations.AddMutation(reanimationMutationEntries.GetRandomElementCosmetic().Value);
                    }
                }
            }
            
        }
    }

    // [CallAfterGameLoaded]
}