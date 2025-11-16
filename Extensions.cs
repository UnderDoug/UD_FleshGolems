using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using UD_FleshGolems.Logging;

using XRL;
using XRL.CharacterBuilds.Qud;
using XRL.Rules;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

using static UD_FleshGolems.Const;
using Options = UD_FleshGolems.Options;

namespace UD_FleshGolems
{
    public static class Extensions
    {
        public static bool Contains(this List<MethodRegistryEntry> DebugRegistry, MethodBase MethodBase)
        {
            foreach (MethodBase methodBase in DebugRegistry)
            {
                if (MethodBase.Equals(methodBase))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool GetValue(this List<MethodRegistryEntry> DebugRegistry, MethodBase MethodBase)
        {
            foreach ((MethodBase methodBase, bool value )in DebugRegistry)
            {
                if (MethodBase.Equals(methodBase))
                {
                    return value;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(MethodBase), "Not found.");
        }

        public static bool IsPlayerBlueprint(this string Blueprint)
        {
            return Blueprint == Startup.PlayerBlueprint;
        }

        public static string ThisManyTimes(this string @string, int Times = 1)
        {
            if (Times < 1)
            {
                return null;
            }
            string output = "";

            for (int i = 0; i < Times; i++)
            {
                output += @string;
            }

            return output;
        }
        public static string ThisManyTimes(this char @char, int Times = 1)
        {
            return @char.ToString().ThisManyTimes(Times);
        }

        public static void TryAdd<T>(this ICollection<T> Collection, T Item)
        {
            if (!Collection.Contains(Item))
            {
                Collection.Add(Item);
            }
        }

        public static T GetRandomElementCosmeticExcluding<T>(this IEnumerable<T> Enumerable, Predicate<T> Exclude)
            where T : class
        {
            List<T> filteredList = new(Enumerable);
            filteredList.RemoveAll(m => Exclude != null && Exclude(m));
            return filteredList.GetRandomElementCosmetic();
        }

        public static Commerce BlurValue(this Commerce Commerce, int Margin)
        {
            int adjustment = Stat.RandomCosmetic(-Margin, Margin);
            Commerce.Value += adjustment;
            return Commerce;
        }
        public static Commerce BlurValue(this Commerce Commerce, double MarginPercent)
        {
            int margin = (int)(MarginPercent * 100.0);
            double adjustmentFactor = 1.0 + (Stat.RandomCosmetic(-margin, margin) / 100.0);
            Commerce.Value *= adjustmentFactor;
            return Commerce;
        }

        public static IEnumerable<string> GetPartNames(this GameObject Object)
        {
            foreach (IPart part in Object.PartsList)
            {
                yield return part.Name;
            }
        }

        public static bool OverlapsWith<T>(this IEnumerable<T> Enumerable1, IEnumerable<T> Enumerable2)
        {
            foreach (T item1 in Enumerable1)
            {
                foreach (T item2 in Enumerable2)
                {
                    if (item1.Equals(item2))
                    {
                        return true;
                    }    
                }
            }
            return false;
        }

        public static bool ContainsAll<T>(this ICollection<T> Collection1, ICollection<T> Collection2)
        {
            int matches = 0;
            int targetMatches = Collection2.Count;
            if (targetMatches > Collection1.Count)
            {
                return false;
            }
            foreach (T item2 in Collection2)
            {
                foreach (T item1 in Collection1)
                {
                    if (item1.Equals(item2))
                    {
                        matches++;
                        if (targetMatches == matches)
                        {
                            break;
                        }
                    }    
                }
            }
            return targetMatches >= matches;
        }

        public static GameObject SetWontSell(this GameObject Item, bool WontSell)
        {
            Item.SetIntProperty(nameof(WontSell), WontSell ? 1 : 0, RemoveIfZero: true);
            return Item;
        }

        public static int DamageTo1HP(this GameObject Creature)
        {
            if (Creature == null || Creature.GetStat("Hitpoints") is not Statistic hitpoints)
            {
                return 0;
            }
            return hitpoints.Value - 1;
        }

        public static T OverrideWithDeepCopyOrRequirePart<T>(this GameObject GameObject, T PartToCopy, Func<GameObject, GameObject> MapInv = null)
            where T : IPart, new()
        {
            if (PartToCopy == null)
            {
                return GameObject?.RequirePart<T>();
            }
            if (GameObject != null && GameObject.TryGetPart(out T existingPart))
            {
                GameObject?.RemovePart(existingPart);
            }
            T returnPart = null;
            try
            {
                returnPart = GameObject?.AddPart(PartToCopy.DeepCopy(GameObject, MapInv) as T);
            }
            catch (Exception x)
            {
                MetricsManager.LogException(
                    nameof(OverrideWithDeepCopyOrRequirePart) + " -> " + 
                    nameof(GameObject.AddPart) + "(" + 
                    nameof(PartToCopy.DeepCopy) + ")", 
                    x: x, 
                    category: "game_mod_exception");
                returnPart = PartToCopy;
            }
            return returnPart;
        }

        public static string SetSpecies(this GameObject GameObject, string Species)
        {
            GameObject.SetStringProperty(nameof(Species), Species);
            return Species;
        }
        public static string SetGenotype(this GameObject GameObject, string Genotype)
        {
            GameObject.SetStringProperty(nameof(Genotype), Genotype);
            return Genotype;
        }
        public static string SetSubtype(this GameObject GameObject, string Subtype)
        {
            GameObject.SetStringProperty(nameof(Subtype), Subtype);
            return Subtype;
        }
        public static string SetBleedLiquid(this GameObject GameObject, string BleedLiquid)
        {
            GameObject.SetStringProperty(nameof(BleedLiquid), BleedLiquid);
            return BleedLiquid;
        }
        public static string SetBleedPrefix(this GameObject GameObject, string BleedPrefix)
        {
            GameObject.SetStringProperty(nameof(BleedPrefix), BleedPrefix);
            return BleedPrefix;
        }
        public static string SetBleedColor(this GameObject GameObject, string BleedColor)
        {
            GameObject.SetStringProperty(nameof(BleedColor), BleedColor);
            return BleedColor;
        }

        public static bool AssignDefaultBehaviour(
            this BodyPart BodyPart,
            GameObject DefaultBehavior,
            bool SetDefaultBehaviorBlueprint = false)
        {
            BodyPart.DefaultBehavior = DefaultBehavior;
            if (SetDefaultBehaviorBlueprint)
            {
                BodyPart.DefaultBehaviorBlueprint = DefaultBehavior.Blueprint;
            }
            return BodyPart.DefaultBehavior == DefaultBehavior 
                && (!SetDefaultBehaviorBlueprint || BodyPart.DefaultBehaviorBlueprint == DefaultBehavior.Blueprint);
        }

        public static GameObjectBlueprint GetGameObjectBlueprint(this string Blueprint)
        {
            return Utils.GetGameObjectBlueprint(Blueprint);
        }

        public static string GetCorpseBlueprint(this GameObjectBlueprint Blueprint)
        {
            if (Blueprint.TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseBlueprint), out string corpseBlueprint))
            {
                return corpseBlueprint;
            }
            return null;
        }
        public static bool TryGetCorpseBlueprint(this GameObjectBlueprint Blueprint, out string CorpseBlueprint)
        {
            return (CorpseBlueprint = Blueprint.GetCorpseBlueprint()) != null
                && GameObjectFactory.Factory.HasBlueprint(CorpseBlueprint);
        }

        public static int? GetCorpseChance(this GameObjectBlueprint Blueprint)
        {
            if (Blueprint.TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseChance), out int corpseChance))
            {
                return corpseChance;
            }
            return null;
        }
        public static bool TryGetCorpseChance(this GameObjectBlueprint Blueprint, out int CorpseChance)
        {
            CorpseChance = 0;
            int? corpseChance = Blueprint.GetCorpseChance();
            if (corpseChance != null)
            {
                CorpseChance = (int)corpseChance;
                return true;
            }
            return false;
        }
        public static bool TryGetCorpseBlueprintAndChance(this GameObjectBlueprint Blueprint, out string CorpseBlueprint, out int CorpseChance)
        {
            CorpseChance = 0;
            return Blueprint.TryGetCorpseBlueprint(out CorpseBlueprint)
                && Blueprint.TryGetCorpseChance(out CorpseChance);
        }


        public static bool IsCorpse(this GameObjectBlueprint Blueprint, Predicate<GameObjectBlueprint> Filter)
        {
            return Blueprint != null
                && Blueprint.InheritsFrom("Corpse")
                && (Filter == null || Filter(Blueprint));
        }

        public static bool IsCorpse(this GameObjectBlueprint Blueprint)
        {
            return Blueprint != null
                && Blueprint.IsCorpse(null);
        }

        public static bool IsCorpse(this string Blueprint, Predicate<GameObjectBlueprint> Filter = null)
        {
            return Blueprint != null
                && Blueprint.GetGameObjectBlueprint().IsCorpse(Filter);
        }

        public static bool IsCorpse(this GameObject Corpse, Predicate<GameObjectBlueprint> Filter = null)
        {
            return Corpse != null
                && !Corpse.HasPart<AnimatedObject>()
                && Corpse.HasPart<UD_FleshGolems_CorpseReanimationHelper>()
                && Corpse.GetBlueprint().IsCorpse(Filter);
        }

        public static bool InheritsFrom(this string Blueprint, string BaseBlueprint)
        {
            return Utils.ThisBlueprintInheritsFromThatOne(Blueprint, BaseBlueprint);
        }

        public static bool IsBaseBlueprint(this string Blueprint)
        {
            return Utils.IsBaseGameObjectBlueprint(Blueprint);
        }

        public static bool IsExcludedFromDynamicEncounters(this string Blueprint)
        {
            return Utils.IsGameObjectBlueprintExcludedFromDynamicEncounters(Blueprint);
        }

        public static bool HasSTag(this GameObjectBlueprint Blueprint, string STag)
        {
            return Blueprint.HasTag("Semantic" + STag);
        }
        public static bool HasSTag(this string Blueprint, string STag)
        {
            return Blueprint.GetGameObjectBlueprint() is var gameObjectBlueprint
                && gameObjectBlueprint.HasSTag(STag);
        }

        public static bool IsChiliad(this GameObjectBlueprint Blueprint)
        {
            return Blueprint.HasSTag("Chiliad");
        }
        public static bool IsChiliad(this string Blueprint)
        {
            return Blueprint.GetGameObjectBlueprint() is var gameObjectBlueprint
                && gameObjectBlueprint.IsChiliad();
        }

        public static GameObjectBlueprint GetBlueprintIfExists(this PopulationItem PopItem)
        {
            return PopItem?.Name?.GetGameObjectBlueprint();
        }

        public static Dictionary<T, int> ConvertToWeightedList<T>(this List<KeyValuePair<T, int>> EntriesList)
        {
            Dictionary<T, int> weightedEntries = new();
            if (!EntriesList.IsNullOrEmpty())
            {
                foreach ((T item, int weight) in EntriesList)
                {
                    if (!weightedEntries.ContainsKey(item))
                    {
                        weightedEntries.Add(item, weight);
                    }
                    else
                    {
                        weightedEntries[item] += weight;
                    }
                }
            }
            return weightedEntries;
        }

        public static Dictionary<string, int> ConvertToWeightedList<T>(this List<UD_FleshGolems_PastLife.BlueprintWeightPair> EntriesList)
        {
            Dictionary<string, int> weightedEntries = new();
            if (!EntriesList.IsNullOrEmpty())
            {
                foreach ((string blueprint, int weight) in EntriesList)
                {
                    if (!weightedEntries.ContainsKey(blueprint))
                    {
                        weightedEntries.Add(blueprint, weight);
                    }
                    else
                    {
                        weightedEntries[blueprint] += weight;
                    }
                }
            }
            return weightedEntries;
        }

        public static List<string> ConvertToList(this List<UD_FleshGolems_PastLife.BlueprintWeightPair> EntriesList)
        {
            List<string> outputList = new();
            if (!EntriesList.IsNullOrEmpty())
            {
                foreach ((string item, int _) in EntriesList)
                {
                    if (!outputList.Contains(item))
                    {
                        outputList.Add(item);
                    }
                }
            }
            return outputList;
        }

        public static List<T> ConvertToList<T>(this List<KeyValuePair<T, int>> EntriesList)
        {
            List<T> outputList = new();
            if (!EntriesList.IsNullOrEmpty())
            {
                foreach ((T item, int _) in EntriesList)
                {
                    if (!outputList.Contains(item))
                    {
                        outputList.Add(item);
                    }
                }
            }
            return outputList;
        }

        public static Dictionary<T, int> ConvertToWeightedList<T>(this IEnumerable<T> Items)
        {
            Dictionary<T, int> weightedList = new();
            foreach (T item in Items)
            {
                if (weightedList.ContainsKey(item))
                {
                    weightedList[item]++;
                }
                else
                {
                    weightedList.Add(item, 1);
                }
            }
            return weightedList;
        }

        public static Dictionary<string, int> ConvertToWeightedList(this IEnumerable<UD_FleshGolems_PastLife.BlueprintWeightPair> Items)
        {
            Dictionary<string, int> weightedList = new();
            foreach (UD_FleshGolems_PastLife.BlueprintWeightPair item in Items)
            {
                if (weightedList.ContainsKey((string)item))
                {
                    weightedList[(string)item] += (int)item;
                }
                else
                {
                    weightedList.Add((string)item, (int)item);
                }
            }
            return weightedList;
        }

        public static string GenerateBulletList(
            this IEnumerable<string> Items,
            string Label = null,
            string Bullet = "\u0007",
            string BulletColor = "K",
            Func<string,string> ItemPreProc = null,
            Func<string,string> ItemPostProc = null)
        {
            Label = Label.IsNullOrEmpty() ? "" : Label + "\n";
            string output = "";
            foreach (string item in Items)
            {
                string preProcItem = ItemPreProc != null ? ItemPreProc(item) : item;
                if (!output.IsNullOrEmpty())
                {
                    output += "\n";
                }
                string prePostProc = "{{" + BulletColor + "|" + Bullet + "}} " + preProcItem;
                string postProcItem = ItemPostProc != null ? ItemPostProc(prePostProc) : prePostProc;
                output += postProcItem;
            }
            return Label + output;
        }

        public static string GenerateCSVList(
            this IEnumerable<List<string>> ItemLists,
            IEnumerable<string> Headings = null,
            string Delimiter = ",",
            string NewLine = "\n")
        {
            string headings = "";
            if (!Headings.IsNullOrEmpty())
            {
                foreach (string heading in Headings)
                {
                    if (!headings.IsNullOrEmpty())
                    {
                        headings += Delimiter;
                    }
                    headings += heading;
                }
                headings += NewLine;
            }
            string output = "";
            foreach (List<string> itemList in ItemLists)
            {
                string rowOutput = "";
                foreach (string item in itemList)
                {
                    if (!rowOutput.IsNullOrEmpty())
                    {
                        rowOutput += Delimiter;
                    }
                    rowOutput += item;
                }
                output += rowOutput + NewLine;
            }
            return headings + output;
        }

        public static string GenerateCSVList<T>(
            this IEnumerable<KeyValuePair<string, T>> ItemLists,
            IEnumerable<string> Headings = null,
            string Delimiter = ",",
            string NewLine = "\n")
        {
            List<List<string>> itemsList = new();
            foreach (KeyValuePair<string, T> entry in ItemLists)
            {
                itemsList.Add(new() { entry.Key, entry.Value.ToString() });
            }
            return itemsList.GenerateCSVList(Headings, Delimiter, NewLine);
        }

        public static List<string> ConvertToStringList<T>(this IEnumerable<T> Entries, Func<T, string> Proc)
        {
            List<string> outputList = new();
            foreach (T entry in Entries)
            {
                string entryProc = Proc != null ? Proc(entry) : entry.ToString();
                outputList.Add(entryProc);
            }
            return outputList;
        }
        public static IEnumerable<string> ConvertToStringList<T>(this IEnumerable<T> Entries)
        {
            return ConvertToStringList(Entries, null);
        }

        public static IEnumerable<string> ConvertToStringListWithItemCount<T>(
            this Dictionary<T, int> Entries)
        {
            foreach ((T item, int count) in Entries)
            {
                yield return count.Things(item.ToString());
            }
            yield break;
        }

        public static IEnumerable<string> ConvertToStringListWithItemCount<T>(
            this IEnumerable<T> Entries)
        {
            foreach ((T item, int count) in Entries.ConvertToWeightedList())
            {
                yield return count.Things(item.ToString());
            }
            yield break;
        }
        public static IEnumerable<string> ConvertToStringListWithItemCount<T>(
            this IEnumerable<T> Entries,
            Func<T, string> Proc)
        {
            foreach ((T item, int count) in Entries.ConvertToWeightedList())
            {
                string procItem = Proc != null ? Proc(item) : item.ToString();
                yield return count.Things(procItem);
            }
            yield break;
        }

        public static IEnumerable<string> ConvertToStringListWithItemCount<T>(
            this IEnumerable<KeyValuePair<T, int>> Entries,
            Func<KeyValuePair<T, int>, string> Proc)
        {
            foreach (KeyValuePair<T, int> entry in Entries)
            {
                string procEntry = Proc != null ? Proc(entry) : entry.Value.Things(entry.Key.ToString());
                yield return procEntry;
            }
            yield break;
        }
        public static IEnumerable<string> ConvertToStringListWithItemCount<T>(
            this IEnumerable<KeyValuePair<T, int>> Entries)
        {
            return ConvertToStringListWithItemCount(Entries, null);
        }

        public static IEnumerable<string> ConvertToStringListWithKeyValue<T>(
            this IEnumerable<KeyValuePair<string,T>> Entries,
            Func<KeyValuePair<string, T>, string> Proc)
        {
            foreach (KeyValuePair<string, T> entry in Entries)
            {
                string procOutput = Proc != null ? Proc(entry) : entry.Key + ": " + entry.Value.ToString();
                yield return procOutput;
            }
            yield break;
        }
        public static IEnumerable<string> ConvertToStringListWithKeyValue<T>(
            this IEnumerable<KeyValuePair<string,T>> Entries,
            Func<T, string> Proc)
        {
            return Entries.ConvertToStringListWithKeyValue(kvp => kvp.Key + ": " + (Proc != null ? Proc(kvp.Value) : kvp.Value.ToString()));
        }
        public static IEnumerable<string> ConvertToStringListWithKeyValue<T>(
            this IEnumerable<KeyValuePair<string,T>> Entries)
        {
            return Entries.ConvertToStringListWithKeyValue((Func<T, string>)null);
        }

        public static IEnumerable<string> ConvertToStringListWithKeyValue(
            this IEnumerable<KeyValuePair<string, GameObjectBlueprint>> Entries)
        {
            return ConvertToStringListWithKeyValue(Entries, bp => bp.Name);
        }

        public static IEnumerable<string> ConvertToStringListWithKeyValue(
            this IEnumerable<KeyValuePair<string, List<UD_FleshGolems_PastLife.BlueprintWeightPair>>> Entries,
            Func<List<UD_FleshGolems_PastLife.BlueprintWeightPair>, string> Proc)
        {
            return Entries.ConvertToStringListWithKeyValue(kvp => kvp.Key + ": " + (Proc != null ? Proc(kvp.Value) : kvp.Value.ToString()));
        }
    }
}
