using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Reflection;

using HarmonyLib;

using Qud.API;

using XRL;
using XRL.Wish;
using XRL.World;
using XRL.World.Parts.Mutation;
using XRL.World.Text.Attributes;
using XRL.World.Text.Delegates;
using XRL.Language;
using XRL.CharacterBuilds;
using XRL.CharacterBuilds.Qud;
using XRL.World.Parts;
using XRL.UI;

using UD_FleshGolems;
using UD_FleshGolems.Logging;
using UD_FleshGolems.Capabilities;
using Debug = UD_FleshGolems.Logging.Debug;
using Options = UD_FleshGolems.Options;

using static UD_FleshGolems.Const;
using XRL.Rules;
using UD_FleshGolems.Parts.VengeanceHelpers;
using XRL.World.Effects;
using UD_FleshGolems.Parts.PastLifeHelpers;


namespace UD_FleshGolems
{
    [HasWishCommand]
    public static class Utils
    {
        [UD_FleshGolems_DebugRegistry]
        public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
        {
            // Registry.Register(typeof(Utils).GetMethod(nameof(UD_xTag)), false);
            return Registry;
        }

        public static ModInfo ThisMod => ModManager.GetMod(MOD_ID);

        private static EntityTaxa PlayerTaxa = null;

        public static ModInfo GetFirstCallingModNot(ModInfo ThisMod)
        {
            try
            {
                Dictionary<Assembly, ModInfo> modAssemblies = ModManager.ActiveMods
                    ?.Where(mi => mi != ThisMod && mi.Assembly != null)
                    ?.ToDictionary(mi => mi.Assembly, mi => mi);

                if (modAssemblies.IsNullOrEmpty())
                {
                    return null;
                }
                StackTrace stackTrace = new();
                for (int i = 0; i < 12 && stackTrace?.GetFrame(i) is StackFrame stackFrameI; i++)
                {
                    if (stackFrameI?.GetMethod() is MethodBase methodBase
                        && methodBase.DeclaringType is Type declaringType
                        && modAssemblies.ContainsKey(declaringType.Assembly))
                    {
                        return modAssemblies[declaringType.Assembly];
                    }
                }
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(GetFirstCallingModNot), x, GAME_MOD_EXCEPTION);
            }
            return null;
        }
        public static bool TryGetFirstCallingModNot(ModInfo ThisMod, out ModInfo FirstCallingMod)
        {
            return (FirstCallingMod = GetFirstCallingModNot(ThisMod)) != null;
        }

        public static IEnumerable<T> GetEnumValues<T>()
            where T : Enum
            => Enum.GetValues(typeof(T))?.Cast<T>() ?? new List<T>();

        public static Dictionary<string, T> EnumNamedValues<T>()
            where T : Enum
            => GetEnumValues<T>()
            ?.ToDictionary(
                keySelector: e => Enum.GetName(typeof(T), e),
                elementSelector: e => e);

        public static bool EnumHasValue<T>(string Name)
            where T : Enum
            => (GetEnumValues<T>()
            ?.ToList()
            ?.Any(v => v.ToString() == Name))
            .GetValueOrDefault();

        public static T GetEnumValue<T>(string Name)
            where T : Enum
        {
            if (GetEnumValues<T>()?.ToList() is List<T> enumValues)
            {
                return enumValues.FirstOrDefault(v => v.ToString() == Name);
            }
            return default;
        }

        public static EntityTaxa GetPlayerTaxa()
        {
            using Indent indent = new(1);

            if (PlayerTaxa != null)
            {
                Debug.LogMethod(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                    Debug.Arg(nameof(PlayerTaxa), PlayerTaxa != null),
                    Debug.Arg(nameof(The.Player), The.Player != null),
                    Debug.Arg(nameof(PlayerTaxa.Blueprint), PlayerTaxa?.Blueprint ?? NULL),
                    });
                return PlayerTaxa;
            }

            if (PlayerTaxa == null 
                && !EmbarkBuilderConfiguration.activeModules.IsNullOrEmpty())
            {
                foreach (AbstractEmbarkBuilderModule activeModule in EmbarkBuilderConfiguration.activeModules)
                {
                    if (activeModule.type == nameof(QudSpecificCharacterInitModule))
                    {
                        if (activeModule is QudSpecificCharacterInitModule characterInit)
                        {
                            string blueprint = characterInit
                                    ?.builder
                                    ?.GetModule<QudGenotypeModule>()
                                    ?.data
                                    ?.Entry
                                    ?.BodyObject
                                ?? characterInit
                                    ?.builder
                                    ?.GetModule<QudSubtypeModule>()
                                    ?.data
                                    ?.Entry
                                    ?.BodyObject
                                ?? "Humanoid";

                            blueprint = characterInit
                                ?.builder
                                ?.info
                                ?.fireBootEvent(QudGameBootModule.BOOTEVENT_BOOTPLAYEROBJECTBLUEPRINT, The.Game, blueprint);

                            if (!blueprint.IsNullOrEmpty())
                            {
                                PlayerTaxa ??= new();
                                PlayerTaxa.Blueprint = blueprint;

                                PlayerTaxa.Subtype = characterInit
                                        ?.builder
                                        ?.GetModule<QudSubtypeModule>()
                                        ?.data
                                        ?.Entry
                                        ?.Name;

                                PlayerTaxa.Genotype = characterInit
                                        ?.builder
                                        ?.GetModule<QudGenotypeModule>()
                                        ?.data
                                        ?.Entry
                                        ?.Name;

                                PlayerTaxa.Species = PlayerTaxa.Blueprint?.GetGameObjectBlueprint()?.GetPropertyOrTag(nameof(PlayerTaxa.Species));

                                Debug.LogMethod(indent,
                                    ArgPairs: new Debug.ArgPair[]
                                    {
                                Debug.Arg(nameof(PlayerTaxa), PlayerTaxa != null),
                                Debug.Arg(nameof(The.Player), The.Player != null),
                                Debug.Arg(nameof(PlayerTaxa.Blueprint), PlayerTaxa?.Blueprint ?? NULL),
                                    });
                            }
                            
                            return PlayerTaxa;
                        }
                    }
                }
            }

            if (The.Player != null)
            {
                PlayerTaxa ??= new();
                PlayerTaxa.Blueprint = The.Player.Blueprint;
                PlayerTaxa.Subtype = The.Player.GetSubtype();
                PlayerTaxa.Genotype = The.Player.GetGenotype();
                PlayerTaxa.Species = The.Player.GetSpecies();

                Debug.LogMethod(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                    Debug.Arg(nameof(PlayerTaxa), PlayerTaxa != null),
                    Debug.Arg(nameof(The.Player), The.Player != null),
                    Debug.Arg(nameof(PlayerTaxa.Blueprint), PlayerTaxa?.Blueprint ?? NULL),
                    });
                return PlayerTaxa;
            }

            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(PlayerTaxa), PlayerTaxa != null),
                    Debug.Arg(nameof(The.Player), The.Player != null),
                    Debug.Arg(nameof(PlayerTaxa.Blueprint), PlayerTaxa?.Blueprint ?? NULL),
                });

            return null;
        }

        public static string GetPlayerBlueprint()
        {
            if (The.Player != null)
                return The.Player.Blueprint;

            return GetPlayerTaxa()?.Blueprint;
        }

        public static string AppendTick(string String, bool WithSpaceAfter = true)
        {
            return String + "[" + TICK + "]" + (WithSpaceAfter ? " " : "");
        }
        public static string AppendCross(string String, bool WithSpaceAfter = true)
        {
            return String + "[" + CROSS + "]" + (WithSpaceAfter ? " " : "");
        }
        public static string AppendYehNah(string String, bool Yeh, bool WithSpaceAfter = true)
        {
            return String + "[" + (Yeh ? TICK : CROSS) + "]" + (WithSpaceAfter ? " " : "");
        }

        public static string YehNah(bool? Yeh = null)
            => "[" + (Yeh == null ? "-" : (Yeh.GetValueOrDefault() ? TICK : CROSS)) + "]";

        public static int CapDamageTo1HPRemaining(GameObject Creature, int DamageAmount)
        {
            if (Creature == null || Creature.GetStat("Hitpoints") is not Statistic hitpoints)
            {
                return 0;
            }
            return Math.Max(0, Math.Min(hitpoints.Value - 1, DamageAmount));
        }

        public static GameObject DeepCopyMapInventory(GameObject Source)
        {
            if (Source == null)
            {
                return null;
            }
            return Source.DeepCopy(MapInv: DeepCopyMapInventory);
        }

        public static bool IsBaseBlueprint(GameObjectBlueprint Blueprint)
        {
            return Blueprint != null
                && Blueprint.IsBaseBlueprint();
        }

        public static bool IsNotBaseBlueprint(GameObjectBlueprint Blueprint)
        {
            return Blueprint != null
                && !Blueprint.IsBaseBlueprint();
        }

        public static bool IsNotBaseBlueprintOrPossiblyExcludedFromDynamicEncounters(GameObjectBlueprint Blueprint)
        {
            return IsNotBaseBlueprint(Blueprint)
                && UD_FleshGolems_NecromancySystem.PossiblyExcludedFromDynamicEncounters(Blueprint);
        }

        public static GameObjectBlueprint GetGameObjectBlueprint(string Blueprint)
        {
            return GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint);
        }

        public static bool ThisBlueprintInheritsFromThatOne(string ThisBlueprint, string ThatBlueprint)
        {
            return !ThatBlueprint.IsNullOrEmpty()
                && ThisBlueprint?.GetGameObjectBlueprint() is GameObjectBlueprint thisGameObjectBlueprint
                && thisGameObjectBlueprint.InheritsFrom(ThatBlueprint);
        }

        public static bool IsBaseGameObjectBlueprint(string Blueprint)
        {
            return Blueprint?.GetGameObjectBlueprint() is GameObjectBlueprint gameObjectBlueprint
                && gameObjectBlueprint.IsBaseBlueprint();
        }

        public static bool IsGameObjectBlueprintExcludedFromDynamicEncounters(string Blueprint)
        {
            return Blueprint?.GetGameObjectBlueprint() is GameObjectBlueprint gameObjectBlueprint
                && gameObjectBlueprint.IsExcludedFromDynamicEncounters();
        }

        public static List<string> GetDistinctFromPopulation(
            string TableName,
            int n,
            Dictionary<string, string> vars = null,
            string defaultIfNull = null)
        {
            using Indent indent = new();
            Debug.LogCaller(indent[1],
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(TableName), TableName),
                    Debug.Arg(nameof(n), n),
                    Debug.Arg(nameof(vars), vars == null ? 0 : vars.Count),
                    Debug.Arg(nameof(defaultIfNull), defaultIfNull)
                });

            List<string> returnList = new();
            if (PopulationManager.RollDistinctFrom(TableName, n, vars, defaultIfNull) is List<List<PopulationResult>> populationResultsList)
            {
                foreach (List<PopulationResult> populationResults in populationResultsList)
                {
                    foreach (PopulationResult populationResult in populationResults)
                    {
                        Debug.Log(populationResult.Blueprint, Indent: indent[2]);
                        returnList.AddUnique(populationResult.Blueprint);
                    }
                }
            }
            return returnList;
        }

        public static bool EitherNull<T1,T2>(T1 x, T2 y, out bool AreEqual)
        {
            AreEqual = (x is null) == (y is null);
            if (x is null || y is null)
            {
                return true;
            }
            return false;
        }

        public static bool EitherNull<T1, T2>(T1 x, T2 y, out int Comparison)
        {
            Comparison = 0;
            if (x is not null && y is null)
            {
                Comparison = 1;
                return true;
            }
            if (x is null && y is not null)
            {
                Comparison = -1;
                return true;
            }
            if ((x is null) && (y is null))
            {
                Comparison = 0;
                return true;
            }
            return false;
        }

        public static string UIFriendlyNBPS(int Count = 1)
            => ("=ud_nbsp:" + Count + "=")
                .StartReplace()
                .ToString();

        public static string Bullet(string Bullet = "\u0007", string BulletColor = "K")
        {
            if (BulletColor.IsNullOrEmpty())
            {
                return Bullet;
            }
            return "{{" + BulletColor + "|" + Bullet + "}}";
        }

        public static string GetMutationClassByName(string Name)
            => MutationFactory.GetMutationEntryByName(Name)?.Class;

        public static bool WasProperlyNamed(GameObject Object)
        {
            if (Object == null)
                return false;

            if (Object.HasProperName)
                return true;

            if (Object.TryGetPart(out Titles titles)
                && !titles.TitleList.IsNullOrEmpty())
                return true;

            if (Object.TryGetPart(out Epithets epithets)
                && !epithets.EpithetList.IsNullOrEmpty())
                return true;

            if (Object.TryGetPart(out Honorifics honorifics)
                && !honorifics.HonorificList.IsNullOrEmpty())
                return true;

            if (Object.GetPropertyOrTag("Role") == "Hero")
                return true;

            return false;
        }

        public static bool HasSpecialIdentity(GameObject Object)
        {
            if (Object == null)
            {
                return false;
            }
            if (Object.GetTagOrStringProperty("SpawnedFrom") is string spawnedFrom
                && spawnedFrom.EqualsAny("Mechanimist Convert Librarian"))
            {
                return true;
            }
            if (Object.TryGetIntProperty("Villager", out int villagerProp)
                && villagerProp > 0)
            {
                // return true;
            }
            return false;
        }

        public static ICollection<T> SafeCombineLists<T>(params ICollection<T>[] args)
        {
            List<T> output = new();
            if (args.IsNullOrEmpty())
            {
                return output;
            }
            foreach (ICollection<T> arg in args)
            {
                if (arg.IsNullOrEmpty())
                {
                    continue;
                }
                output.AddRange(arg);
            }
            return output;
        }

        public static bool HasValueIsPrimativeType(Traverse Member)
            => Member != null
            && Member.GetValueType() != null
            && Member.GetValueType().EqualsAny(
                new Type[]
                {
                    typeof(string),
                    typeof(int),
                    typeof(bool),
                });

        public static string IndefiniteArticle(string Word)
            => Grammar.IndefiniteArticle(Word, false)?.Uncapitalize()?.Trim();

        public static string IndefiniteArticle(GameObject Object)
        {
            if (Object == null)
                return null;

            return Object.IsPlural 
                ? "some"
                : Grammar.IndefiniteArticle(Object.GetReferenceDisplayName(Short: true), false).Uncapitalize().Trim();
        }

        public static string WithIndefiniteArticle(string Word)
            => !Word.IsNullOrEmpty()
            ? IndefiniteArticle(Word) + " " + Word
            : null;

        public static string WithIndefiniteArticle(GameObject Object)
        {
            if (Object == null)
                return null;
            string word = Object.GetReferenceDisplayName(Short: true);
            return Object.IsPlural 
                ? "some " + word
                : WithIndefiniteArticle(word);
        }

        public static bool IsNotImprovisedWeapon(GameObject Object)
            => Object.TryGetPart(out MeleeWeapon mw)
            && !mw.IsImprovisedWeapon();

        /* 
         * 
         * Wishes!
         * 
         */

    }
    }