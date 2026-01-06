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

        private static Dictionary<string, int> PlayerStartingStats = null;

        public static List<char> CapitalizingPunctuation = new()
        {
            '.',
            '!',
            '?',
        };

        public static char[] CapitalizationExceptions => new char[]
        {
            NBSP[0],
            NBSP.Capitalize()[0],
            'ÿ',
            'Ÿ',
        };

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

        public static T GetEmbarkBuilderModule<T>()
            where T : AbstractEmbarkBuilderModule
        {
            string activeModulesString = nameof(EmbarkBuilderConfiguration) + "." + nameof(EmbarkBuilderConfiguration.activeModules);
            if (EmbarkBuilderConfiguration.activeModules.IsNullOrEmpty())
                throw new Exception(activeModulesString + " null or empty");

            foreach (AbstractEmbarkBuilderModule activeModule in EmbarkBuilderConfiguration.activeModules)
                if (activeModule.type == typeof(T).Name
                    && activeModule is T desiredModule)
                    return desiredModule;

            throw new Exception(typeof(T).Name + " not in " + activeModulesString);
        }
        public static bool TryGetEmbarkBuilderModule<T>(out T EmbarkBuilderModule)
            where T : AbstractEmbarkBuilderModule
        {
            try
            {
                EmbarkBuilderModule = GetEmbarkBuilderModule<T>();
            }
            catch (Exception x)
            {
                MetricsManager.LogModWarning(ThisMod, x);
                EmbarkBuilderModule = null;
                return false;
            }
            return true;
        }

        public static Dictionary<string, int> GetPlayerEmbarkStats()
            => PlayerStartingStats.IsNullOrEmpty()
                && TryGetEmbarkBuilderModule(out QudAttributesModule attributesModules)
            ? PlayerStartingStats = attributesModules.GetFinalStats(attributesModules.builder)
            : PlayerStartingStats;
        
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
                && TryGetEmbarkBuilderModule(out QudSpecificCharacterInitModule characterInit))
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
            => The.Player?.Blueprint
            ?? GetPlayerTaxa()?.Blueprint;

        public static string CallerString(Type CallingType, MethodBase CallingMethod)
            => CallingType.Name + "." + CallingMethod.Name;

        public static string CallerSignatureString(Type CallingType, MethodBase CallingMethod)
            => CallerString(CallingType, CallingMethod) + 
            "(" +
            CallingMethod
                ?.GetParameters()
                ?.Aggregate("", delegate (string a, ParameterInfo n)
                {
                    string paramType = n?.ParameterType?.Name ?? "null??";
                    return !a.IsNullOrEmpty() 
                        ? a + ", " + paramType
                        : a + paramType;
                }) +
            ")";

        public static string AppendTick(string String, bool AppendSpace = true)
            => String + "[" + TICK + "]" + (AppendSpace ? " " : "");

        public static string AppendCross(string String, bool AppendSpace = true)
            => String + "[" + CROSS + "]" + (AppendSpace ? " " : "");

        public static string AppendYehNah(string String, bool Yeh, bool AppendSpace = true)
            => String + "[" + (Yeh ? TICK : CROSS) + "]" + (AppendSpace ? " " : "");

        public static string YehNah(bool? Yeh = null)
            => "[" + (Yeh == null ? "-" : (Yeh.GetValueOrDefault() ? TICK : CROSS)) + "]";


        public static bool EndsInCapitalizingPunctuation(this string Word, bool ExcludeElipses = false)
            => !Word.IsNullOrEmpty()
            && Word.Length > 0
            && Word[^1].EqualsAny(CapitalizingPunctuation.ToArray())
                && (!ExcludeElipses
                    || Word.Length > 1
                        && Word[^2] != '.');

        public static string CreateSentence(string Accumulator, string Next)
            => Accumulator + (!Accumulator.IsNullOrEmpty() ? " " : null) + Next;

        public static string CreateSentence(string Accumulator, ModdedText.TextHelpers.Word Next)
            => Accumulator + (!Accumulator.IsNullOrEmpty() ? " " : null) + Next.ToString();

        public static int CapDamageTo1HPRemaining(GameObject Creature, int DamageAmount)
            => (Creature == null
                || Creature.GetStat("Hitpoints") is not Statistic hitpoints)
            ? 0
            : Math.Max(0, Math.Min(hitpoints.Value - 1, DamageAmount));

        public static GameObject DeepCopyMapInventory(GameObject Source)
            => Source?.DeepCopy(MapInv: DeepCopyMapInventory);

        public static bool IsBaseBlueprint(GameObjectBlueprint Blueprint)
            => Blueprint != null
            && Blueprint.IsBaseBlueprint();

        public static bool IsNotBaseBlueprint(GameObjectBlueprint Blueprint)
            => Blueprint != null
            && !Blueprint.IsBaseBlueprint();

        public static bool IsNotBaseBlueprintOrPossiblyExcludedFromDynamicEncounters(GameObjectBlueprint Blueprint)
            => IsNotBaseBlueprint(Blueprint)
            && UD_FleshGolems_NecromancySystem.PossiblyExcludedFromDynamicEncounters(Blueprint);

        public static GameObjectBlueprint GetGameObjectBlueprint(string Blueprint)
            => GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint);

        public static bool ThisBlueprintInheritsFromThatOne(string ThisBlueprint, string ThatBlueprint)
            => !ThatBlueprint.IsNullOrEmpty()
            && ThisBlueprint?.GetGameObjectBlueprint() is GameObjectBlueprint thisGameObjectBlueprint
            && thisGameObjectBlueprint.InheritsFrom(ThatBlueprint);

        public static bool IsBaseGameObjectBlueprint(string Blueprint)
            => Blueprint?.GetGameObjectBlueprint() is GameObjectBlueprint gameObjectBlueprint
            && gameObjectBlueprint.IsBaseBlueprint();

        public static bool IsGameObjectBlueprintExcludedFromDynamicEncounters(string Blueprint)
            => Blueprint?.GetGameObjectBlueprint() is GameObjectBlueprint gameObjectBlueprint
            && gameObjectBlueprint.IsExcludedFromDynamicEncounters();

        public static List<string> GetDistinctFromPopulation(
            string TableName,
            int N,
            Dictionary<string, string> Vars = null,
            string DefaultIfNull = null)
        {
            using Indent indent = new();
            Debug.LogCaller(indent[1],
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(TableName), TableName),
                    Debug.Arg(nameof(N), N),
                    Debug.Arg(nameof(Vars), Vars == null ? 0 : Vars.Count),
                    Debug.Arg(nameof(DefaultIfNull), DefaultIfNull)
                });

            List<string> returnList = new();
            if (PopulationManager.RollDistinctFrom(TableName, N, Vars, DefaultIfNull) is List<List<PopulationResult>> populationResultsList)
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
            => BulletColor.IsNullOrEmpty()
            ? Bullet
            : "{{" + BulletColor + "|" + Bullet + "}}";

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
            if (args.IsNullOrEmpty())
                return null;

            List<T> output = new();
            foreach (ICollection<T> arg in args)
            {
                if (arg.IsNullOrEmpty())
                    continue;

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
            => Grammar.IndefiniteArticle(Word, false)
                ?.Trim();

        public static string IndefiniteArticle(GameObject Object)
        {
            if (Object == null)
                return null;

            return Object.IsPlural 
                ? "some"
                : Grammar.IndefiniteArticle(Object.GetReferenceDisplayName(Short: true), false)
                    ?.Trim();
        }

        public static string WithIndefiniteArticle(string Word)
            => !Word.IsNullOrEmpty()
            ? IndefiniteArticle(Word) + " " + Word
            : null;

        public static string WithIndefiniteArticle(GameObject Object)
        {
            if (Object.GetReferenceDisplayName(Short: true) is not string word)
                return null;

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