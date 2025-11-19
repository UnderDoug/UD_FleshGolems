using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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

using static UD_FleshGolems.Const;
using Options = UD_FleshGolems.Options;

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
                        return characterInit.builder.info.fireBootEvent(QudGameBootModule.BOOTEVENT_BOOTPLAYEROBJECTBLUEPRINT, The.Game, blueprint);
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
            Debug.LogCaller();
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
            Debug.LogCaller();
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
            Debug.GetIndents(out Indents oldIndent);
            Indents indent = new(Debug.GetNewIndent());
            Debug.LogCaller(indent);
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
                Debug.Log(nameof(multiTagList) + " Entries:", indent[1]);
                for (int i = 0; i < count; i++)
                {
                    if (multiTagList.GetRandomElementCosmeticExcluding(Exclude: s => andList.Contains(s)) is string entry)
                    {
                        Debug.Log(entry, indent[2]);
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
            Debug.SetIndent(oldIndent[0]);
            return output;
        }

        [VariableObjectReplacer]
        public static string UD_xTag(DelegateContext Context)
        {
            Debug.GetIndents(out Indents oldIndent);
            Indents indent = new(Debug.GetNewIndent());
            Debug.LogCaller(indent);
            Debug.Log(nameof(UD_xTag) + "." + nameof(Context.Target), Context?.Target?.DebugName ?? NULL, indent);
            string parameters = null;
            foreach (string parameter in Context.Parameters)
            {
                if (!parameters.IsNullOrEmpty())
                {
                    parameters += ", ";
                }
                parameters += parameter;
            }
            Debug.Log(nameof(Context.Parameters), parameters, indent[1]);
            string output = null;
            if (!Context.Parameters.IsNullOrEmpty()
                && Context.Target is GameObject target)
            {
                switch (Context.Parameters.Count)
                {
                    case 0:
                    case 1:
                        Debug.Log("Uh-oh!", indent[1]);
                        break;

                    case 2:
                        output = UD_xTagSingle(Context);
                        Debug.Log(nameof(UD_xTagSingle), indent[1]);
                        break;

                    case 3:
                        output = UD_xTagMulti(Context);
                        Debug.Log(nameof(UD_xTagMulti), indent[1]);
                        break;

                    default: // 4
                        output = UD_xTagMultiU(Context);
                        Debug.Log(nameof(UD_xTagMultiU), indent[1]);
                        break;
                }
                if (Context.Capitalize)
                {
                    Debug.Log(nameof(Context.Capitalize), indent[1]);
                    output = output.Capitalize();
                }
            }
            Debug.SetIndent(oldIndent[0]);
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

        public static int CapDamageTo1HPRemaining(GameObject Creature, int DamageAmount)
        {
            if (Creature == null || Creature.GetStat("Hitpoints") is not Statistic hitpoints)
            {
                return 0;
            }
            return Math.Min(hitpoints.Value - 1, DamageAmount);
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
            Debug.GetIndents(out Indents indent);
            Debug.Log(
                Debug.GetCallingTypeAndMethod() + "(" +
                nameof(TableName) + ": " + TableName + ", " +
                nameof(n) + ": " + n + ", " +
                nameof(vars) + ": " + (vars == null ? 0 : vars.Count) + ", " +
                nameof(defaultIfNull) + ": " + defaultIfNull + ")",
                indent[1]);

            List<string> returnList = new();
            if (PopulationManager.RollDistinctFrom(TableName, n, vars, defaultIfNull) is List<List<PopulationResult>> populationResultsList)
            {
                foreach (List<PopulationResult> populationResults in populationResultsList)
                {
                    foreach (PopulationResult populationResult in populationResults)
                    {
                        Debug.Log(populationResult.Blueprint, indent[2]);
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