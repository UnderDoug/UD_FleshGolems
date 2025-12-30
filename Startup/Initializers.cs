using System.Collections.Generic;
using System.Linq;
using System;

using Qud.UI;

using XRL;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

using PastLife = XRL.World.Parts.UD_FleshGolems_PastLife;

using UD_FleshGolems.Logging;

using static UD_FleshGolems.Const;
using static UD_FleshGolems.Options;
using XRL.World.Tinkering;
using static XRL.World.Parts.UD_FleshGolems_CorpseReanimationHelper;
using static XRL.World.Parts.UD_FleshGolems_ReanimatedCorpse;
using XRL.Language;

namespace UD_FleshGolems.Startup
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    [HasCallAfterGameLoaded]
    public static class Initializers
    {
        [ModSensitiveStaticCache]
        public static bool CachedCorpses = false;

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

            UnityEngine.Debug.Log(
                nameof(Startup) + "." + nameof(GameBasedCacheInit) + ", " + 
                nameof(PlayerBlueprint) + ": " + PlayerBlueprint ?? NULL);
            CacheSomeCorpses();
            CacheSomeEnumValueDictionaries();
        }

        // [PlayerMutator]

        // The.Player.FireEvent("GameRestored");
        // AfterGameLoadedEvent.Send(Return);  // Return is the game.

        [CallAfterGameLoaded]
        public static void OnLoadGameCallback()
        {
            // Gets called every time the game is loaded but not during generation

            UnityEngine.Debug.Log(
                nameof(Startup) + "." + nameof(GameBasedCacheInit) + ", " +
                nameof(PlayerBlueprint) + ": " + PlayerBlueprint ?? NULL);
            CacheSomeCorpses();
            CacheSomeEnumValueDictionaries();
        }

        //
        // End Startup calls
        // 

        public static int CachingPeriods = 0;
        public static string GetPeriods(int Periods, out int NewPeriods)
        {
            NewPeriods = Periods + 1;
            return ".".ThisManyTimes(3 - (Periods % 4));
        }
        public static void SetLoadingStatusCaching()
        {
            Loading.SetLoadingStatus("Loading Corpses" + GetPeriods(CachingPeriods, out CachingPeriods));
        }
        public static void CacheSomeCorpses()
        {
            if (!CachedCorpses)
            {
                /*
                Loading.SetLoadingStatus("Caching Corpses");
                Debug.GetIndents(out Indents indent);
                Debug.Log(Debug.GetCallingTypeAndMethod(true), "Started!", indent[1]);

                Debug.Log(nameof(PastLife.GetCorpseBlueprints) + "...", indent[2]);
                List<GameObjectBlueprint> corpseBlueprints = PastLife.GetCorpseBlueprints(ForCache: true);

                CachingPeriods = 1;

                Debug.Log(nameof(PastLife.GetCorpseCountsAsBlueprints) + "...", indent[2]);
                foreach (GameObjectBlueprint corpseBlueprint in corpseBlueprints)
                {
                    PastLife.GetCorpseCountsAsBlueprints(corpseBlueprint, false, ForCache: true);
                }

                Debug.Log(nameof(PastLife.GetEntitiesWithCorpseBlueprints) + "...", indent[2]);
                PastLife.GetEntitiesWithCorpseBlueprints(ForCache: true);

                Debug.Log(nameof(PastLife.GetProcessableCorpsesAndTheirProducts) + "...", indent[2]);
                PastLife.GetProcessableCorpsesAndTheirProducts(ExcludeEmpty: false, ForCache: true);

                Debug.Log(Debug.GetCallingTypeAndMethod(true), "Finished!", indent[1]);

                Loading.SetLoadingStatus("Caching Corpses Finished!");

                Debug.DiscardIndent();
                CachedCorpses = true;
                */
            }
        }

        public static Dictionary<string, T> RequireCachedEnumValueDictionary<T>()
            where T : struct, Enum
        {
            string objectGameStateKey = typeof(T).Name + "Values";
            if (The.Game?.GetObjectGameState(objectGameStateKey) is not Dictionary<string, T> cachedEnumValues)
            {
                cachedEnumValues = Utils.EnumNamedValues<T>();
                The.Game?.SetObjectGameState(objectGameStateKey, cachedEnumValues);
                CachedEnumTypes.AddIf(typeof(T), t => !CachedEnumTypes.Contains(t));
            }
            return cachedEnumValues;
        }
        private static List<Type> CachedEnumTypes = new();
        public static void CacheSomeEnumValueDictionaries()
        {
            RequireCachedEnumValueDictionary<TileMappingKeyword>();
        }
        public static void ClearSomeCachedEnumValueDictionaries()
        {
            foreach (Type enumBeingCached in CachedEnumTypes)
            {
                if (!enumBeingCached.IsEnum)
                    continue;

                string objectGameStateKey = enumBeingCached.Name + "Values";
                if ((The.Game?.HasObjectGameState(objectGameStateKey)).GetValueOrDefault())
                    The.Game?.SetObjectGameState(objectGameStateKey, (object)null);
            }
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
                foreach (GameObjectBlueprint recoilerModel in GameObjectFactory.Factory.GetBlueprints(IsStaticRecoiler))
                {
                    GameObject recoilerObject = recoilerModel.createUnmodified();
                    TinkeringHelpers.StripForTinkering(recoilerObject);
                    recoilerObject.MakeUnderstood();

                    GameObject antiMatterCell = GameObject.Create("Antimatter Cell", AutoMod: "ModRadioPowered");
                    antiMatterCell.MakeUnderstood();

                    if (recoilerObject.TryGetPart(out EnergyCellSocket cellSocket))
                    {
                        cellSocket.Cell = antiMatterCell;
                        CellChangedEvent.Send(null, recoilerObject, null, antiMatterCell);

                        if (cellSocket.Cell != antiMatterCell
                            && GameObject.Validate(ref antiMatterCell))
                        {
                            antiMatterCell.Obliterate();
                        }
                    }
                    player.ReceiveObject(recoilerObject);
                }
                player.ReceiveObject("Floating Glowsphere");
                if (GameObject.Create("ScrapCape", AutoMod: "ModDisguise") is GameObject scrapCape)
                {
                    player.ReceiveObject(scrapCape);
                }
            }
        }
        public static bool IsStaticRecoiler(GameObjectBlueprint Model)
            => Model.InheritsFrom("BaseRecoiler")
            && !Model.IsBaseBlueprint()
            && !Model.HasPart(nameof(RandomRuinRecoiler))
            && !Model.HasPart(nameof(ProgrammableRecoiler))
            && Model.TryGetPartParameter(nameof(Teleporter), nameof(Teleporter.DestinationZone), out string destinationZone)
            && !destinationZone.IsNullOrEmpty();
    }

    // [CallAfterGameLoaded]
}