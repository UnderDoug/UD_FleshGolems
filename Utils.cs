using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

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
using Debug = UD_FleshGolems.Logging.Debug;

using static UD_FleshGolems.Const;
using Options = UD_FleshGolems.Options;
using UD_FleshGolems.Capabilities;
using System.Reflection;
using HarmonyLib;

namespace UD_FleshGolems
{
    [HasWishCommand]
    [HasVariableReplacer]
    public static class Utils
    {
        [UD_FleshGolems_DebugRegistry]
        public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
        {
            Registry.Register(typeof(Utils).GetMethod(nameof(UD_xTag)), false);
            Registry.Register(typeof(Utils).GetMethod(nameof(UD_xTagSingle)), false);
            Registry.Register(typeof(Utils).GetMethod(nameof(UD_xTagMulti)), false);
            Registry.Register(typeof(Utils).GetMethod(nameof(UD_xTagMultiU)), false);
            Registry.Register(typeof(Utils).GetMethod(nameof(UD_xTagMultiU)), false);
            return Registry;
        }

        public static ModInfo ThisMod => ModManager.GetMod(MOD_ID);

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
                MetricsManager.LogException(nameof(GetFirstCallingModNot), x, GAME_MOD_EXCEOPTION);
            }
            return null;
        }
        public static bool TryGetFirstCallingModNot(ModInfo ThisMod, out ModInfo FirstCallingMod)
        {
            return (FirstCallingMod = GetFirstCallingModNot(ThisMod)) != null;
        }

        public static IEnumerable<T> GetEnumValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static string GetPlayerBlueprint()
        {
            if (!EmbarkBuilderConfiguration.activeModules.IsNullOrEmpty())
            {
                foreach (AbstractEmbarkBuilderModule activeModule in EmbarkBuilderConfiguration.activeModules)
                {
                    if (activeModule.type == nameof(QudSpecificCharacterInitModule))
                    {
                        QudSpecificCharacterInitModule characterInit = activeModule as QudSpecificCharacterInitModule;
                        string blueprint = characterInit?.builder?.GetModule<QudGenotypeModule>()?.data?.Entry?.BodyObject
                            ?? characterInit?.builder?.GetModule<QudSubtypeModule>()?.data?.Entry?.BodyObject
                            ?? "Humanoid";
                        return characterInit?.builder?.info?.fireBootEvent(QudGameBootModule.BOOTEVENT_BOOTPLAYEROBJECTBLUEPRINT, The.Game, blueprint);
                    }
                }
            }
            return null;
        }

        [VariableReplacer]
        public static string ud_nbsp(DelegateContext Context)
        {
            string nbsp = "\xFF";
            string output = nbsp;
            if (!Context.Parameters.IsNullOrEmpty() && int.TryParse(Context.Parameters[0], out int count))
            {
                for (int i = 1; i < count; i++)
                {
                    output += nbsp;
                }
            }
            return output;
        }

        [VariableReplacer]
        public static string ud_weird(DelegateContext Context)
        {
            string output = null;
            if (!Context.Parameters.IsNullOrEmpty())
            {
                if (Context.Parameters.Count > 1)
                {
                    output = "{{" + Context.Parameters[0] + "|";
                    for (int i = 1; i < Context.Parameters.Count; i++)
                    {
                        if (i > 1)
                        {
                            output += " ";
                        }
                        output += TextFilters.Weird(Context.Parameters[i]);
                    }
                    output += "}}";
                }
                else
                {
                    return TextFilters.Weird(Context.Parameters[0]);
                }
            }
            return output;
        }

        [VariableObjectReplacer]
        public static string UD_xTagSingle(DelegateContext Context)
        {
            using Indent indent = new();
            Debug.LogMethod(indent[1]);
            string output = null;
            if (!Context.Parameters.IsNullOrEmpty()
                && Context.Parameters.Count > 1
                && Context.Target is GameObject target
                && target.GetxTag(Context.Parameters[0], Context.Parameters[1]) is string xtag
                && xtag.CachedCommaExpansion().GetRandomElementCosmetic() is string tagValue)
            {
                output = tagValue;
                if (Context.Capitalize)
                {
                    output = output.Capitalize();
                }
            }
            return output;
        }

        [VariableObjectReplacer]
        public static string UD_xTagMulti(DelegateContext Context)
        {
            using Indent indent = new();
            Debug.LogMethod(indent[1]);
            string output = null;
            if (!Context.Parameters.IsNullOrEmpty()
                && Context.Parameters.Count > 2
                && Context.Target is GameObject target
                && int.TryParse(Context.Parameters[0], out int count)
                && target.GetxTag(Context.Parameters[1], Context.Parameters[2]) is string multixTag
                && multixTag.CachedCommaExpansion() is List<string> multiTagList)
            {
                List<string> andList = new();
                for (int i = 0; i < count; i++)
                {
                    if (multiTagList.GetRandomElementCosmetic() is string entry)
                    {
                        andList.Add(entry);
                    }
                    else
                    {
                        break;
                    }
                }
                output = Grammar.MakeAndList(andList);
                if (andList.Count > 2 && !output.Contains(", and "))
                {
                    output = output.Replace(" and ", ", and ");
                }
                if (Context.Capitalize)
                {
                    output = output.Capitalize();
                }
            }
            return output;
        }

        [VariableObjectReplacer]
        public static string UD_xTagMultiU(DelegateContext Context)
        {
            using Indent indent = new();
            Debug.LogMethod(indent[1]);
            string output = null;
            if (!Context.Parameters.IsNullOrEmpty()
                && Context.Parameters.Count > 3
                && Context.Target is GameObject target
                && int.TryParse(Context.Parameters[0], out int count)
                && Context.Parameters[1] is string unique
                && (unique.EqualsNoCase("U") || unique.EqualsNoCase("Unique"))
                && target.GetxTag(Context.Parameters[2], Context.Parameters[3]) is string multixTag
                && multixTag.CachedCommaExpansion() is List<string> multiTagList)
            {
                List<string> andList = new();
                Debug.Log(nameof(multiTagList) + " Entries:", Indent: indent[2]);
                for (int i = 0; i < count; i++)
                {
                    if (multiTagList.GetRandomElementCosmeticExcluding(Exclude: s => andList.Contains(s)) is string entry)
                    {
                        Debug.Log(entry, Indent: indent[3]);
                        andList.Add(entry);
                    }
                    else
                    {
                        break;
                    }
                }
                output = Grammar.MakeAndList(andList);
                if (andList.Count > 2 && !output.Contains(", and "))
                {
                    output = output.Replace(" and ", ", and ");
                }
                if (Context.Capitalize)
                {
                    output = output.Capitalize();
                }
            }
            return output;
        }

        [VariableObjectReplacer]
        public static string UD_xTag(DelegateContext Context)
        {
            using Indent indent = new();
            Debug.LogCaller(indent[1],
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Context.Target), Context?.Target?.DebugName ?? NULL),
                });
            string parameters = null;
            foreach (string parameter in Context.Parameters)
            {
                if (!parameters.IsNullOrEmpty())
                {
                    parameters += ", ";
                }
                parameters += parameter;
            }
            Debug.Log(nameof(Context.Parameters), parameters, indent[2]);
            string output = null;
            if (!Context.Parameters.IsNullOrEmpty()
                && Context.Target is GameObject target)
            {
                switch (Context.Parameters.Count)
                {
                    case 0:
                    case 1:
                        Debug.Log("Uh-oh!", Indent: indent[3]);
                        break;

                    case 2:
                        output = UD_xTagSingle(Context);
                        Debug.Log(nameof(UD_xTagSingle), Indent: indent[3]);
                        break;

                    case 3:
                        output = UD_xTagMulti(Context);
                        Debug.Log(nameof(UD_xTagMulti), Indent: indent[3]);
                        break;

                    default: // 4
                        output = UD_xTagMultiU(Context);
                        Debug.Log(nameof(UD_xTagMultiU), Indent: indent[3]);
                        break;
                }
                if (Context.Capitalize)
                {
                    Debug.Log(nameof(Context.Capitalize), Indent: indent[3]);
                    output = output.Capitalize();
                }
            }
            return output;
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

        public static bool BlueprintsMatchOrThatBlueprintInheritsFromThisOne(string ThisBlueprint, string ThatBlueprint)
        {
            if (ThisBlueprint.IsNullOrEmpty() || ThatBlueprint.IsNullOrEmpty())
            {
                return false;
            }
            if (ThisBlueprint == ThatBlueprint)
            {
                return true;
            }
            if (ThisBlueprint.IsBaseBlueprint() && ThatBlueprint.InheritsFrom(ThisBlueprint))
            {
                return true;
            }
            return false;
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
            {
                return false;
            }
            if (Object.HasProperName)
            {
                return true;
            }
            if (Object.PartsList.Any(p => p.Name.EqualsAny(nameof(Titles), nameof(Epithets), nameof(Honorifics))))
            {
                return true;
            }
            if (Object.GetPropertyOrTag("Role") == "Hero")
            {
                return true;
            }
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

        /* 
         * 
         * Wishes!
         * 
         */
        [WishCommand( Command = "UD_FleshGolems test kit" )]
        public static bool ReanimationTestKit_WishHandler()
        {
            return ReanimationTestKit_WishHandler(null);
        }
        [WishCommand( Command = "UD_FleshGolems test kit" )]
        public static bool ReanimationTestKit_WishHandler(string Parameters)
        {
            if (Parameters.IsNullOrEmpty() || (!Parameters.EqualsNoCase("Corpse") && !Parameters.EqualsNoCase("Corpses")))
            {
                The.Player.ReceiveObject("Neuro Animator");
                The.Player.ReceiveObject("Antimatter Cell");
                Examiner.IDAll();
                Mutations playerMutations = The.Player.RequirePart<Mutations>();
                if (The.Player.GetPartsDescendedFrom<UD_FleshGolems_NanoNecroAnimation>().IsNullOrEmpty()
                    && !The.Player.HasPart<UD_FleshGolems_NanoNecroAnimation>())
                {
                    Dictionary<string, string> reanimationMutationEntries = new()
                    {
                        { "Physical", nameof(UD_FleshGolems_NanoNecroAnimation) },
                        { "Mental", nameof(UD_FleshGolems_NecromanticAura) }
                    };
                    if (The.Player.IsChimera())
                    {
                        playerMutations.AddMutation(reanimationMutationEntries["Physical"]);
                    }
                    else
                    if (The.Player.IsEsper())
                    {
                        playerMutations.AddMutation(reanimationMutationEntries["Mental"]);
                    }
                    else
                    {
                        playerMutations.AddMutation(reanimationMutationEntries.GetRandomElementCosmetic().Value);
                    }
                }
            }
            if (Popup.AskNumber("How many corpses do you want?", Start: 20, Min: 0, Max: 100) is int corpseCount
                && corpseCount > 0)
            {
                int corpseRadius = corpseCount / 4;
                int maxAttempts = corpseCount * 2;
                int originalCorpseCount = corpseCount;
                List<string> corpseBlueprints = new();
                Loading.SetLoadingStatus("Summoning " + 0 + "/" + originalCorpseCount + " fresh corpses...");
                while (corpseCount > 0 && maxAttempts > 0)
                {
                    maxAttempts--;
                    if (GameObject.CreateSample(EncountersAPI.GetAnItemBlueprint(Extensions.IsCorpse)) is GameObject corpseObject)
                    {
                        Loading.SetLoadingStatus("Summoning " + (corpseBlueprints.Count + 1) + "/" + originalCorpseCount + " (" + corpseObject.Blueprint + ")...");
                        The.Player.CurrentCell
                            .GetAdjacentCells(corpseRadius)
                            .GetRandomElementCosmeticExcluding(Exclude: c => !c.IsEmptyFor(corpseObject))
                            .AddObject(corpseObject);

                        if (corpseObject.CurrentCell != null)
                        {
                            corpseBlueprints.Add(corpseObject.Blueprint);
                            corpseCount--;
                        }
                        else
                        {
                            corpseObject?.Obliterate();
                        }
                    }
                }
                Loading.SetLoadingStatus("Summoned " + corpseBlueprints.Count + "/" + originalCorpseCount + " fresh corpses...");
                if (corpseBlueprints.Count > 0)
                {
                    string corpseListLabel = "Generated " + corpseBlueprints.Count + " corpses of various types, in a radius of " + corpseRadius + " cells.";
                    string corpseListOutput = corpseBlueprints.GenerateBulletList(Label: corpseListLabel);
                    Popup.Show(corpseListOutput);
                }
                Loading.SetLoadingStatus(null);
            }
            return true;
        }
    }
}