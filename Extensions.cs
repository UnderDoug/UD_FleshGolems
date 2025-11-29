using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using XRL;
using XRL.CharacterBuilds.Qud;
using XRL.Collections;
using XRL.Rules;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

using UD_FleshGolems.Capabilities.Necromancy;
using UD_FleshGolems.Logging;

using Relationship = UD_FleshGolems.Capabilities.Necromancy.CorpseEntityPair.PairRelationship;

using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;
using static UD_FleshGolems.Const;
using Options = UD_FleshGolems.Options;
using HarmonyLib;

namespace UD_FleshGolems
{
    public static class Extensions
    {
        [UD_FleshGolems_DebugRegistry]
        public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
        {
            if (typeof(Extensions).GetMethod(nameof(GetWeightedRandom)) is MethodBase getWeightedRandom)
            {
                Registry.Register(getWeightedRandom, true);
            }
            return Registry;
        }
        public static bool Contains(this ICollection<MethodRegistryEntry> DebugRegistry, MethodBase MethodBase)
        {
            foreach ((MethodBase method, bool _) in DebugRegistry)
            {
                if (MethodBase.Equals(method))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool GetValue(this ICollection<MethodRegistryEntry> DebugRegistry, MethodBase MethodBase)
        {
            foreach ((MethodBase methodBase, bool value) in DebugRegistry)
            {
                if (MethodBase.Equals(methodBase))
                {
                    return value;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(MethodBase), "Not found.");
        }
        public static bool TryGetValue(
            this ICollection<MethodRegistryEntry> DebugRegistry,
            MethodBase MethodBase,
            out bool Value)
        {
            Value = default;
            if (DebugRegistry.Contains(MethodBase))
            {
                Value = DebugRegistry.GetValue(MethodBase);
                return true;
            }
            return false;
        }

        public static bool EqualsAny<T>(this T Value, params T[] args)
            => !args.IsNullOrEmpty()
            && !args.Where(t => t.Equals(Value)).IsNullOrEmpty();

        public static bool EqualsAll<T>(this T Value, params T[] args)
            => !args.IsNullOrEmpty()
            && args.Where(t => t.Equals(Value)).Count() == args.Length;

        public static bool InheritsFrom(this Type T, Type Type, bool IncludeSelf = true)
            => (IncludeSelf && T == Type) 
            || Type.IsSubclassOf(T) 
            || T.IsAssignableFrom(Type) 
            || (T.YieldInheritedTypes().ToList() is List<Type> inheritedTypes 
                && inheritedTypes.Contains(Type));

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

        public static void AddUnique<T>(this ICollection<T> Collection, T Item, EqualityComparer<T> Comparer)
        {
            if (Collection == null)
            {
                throw new ArgumentNullException(nameof(Collection));
            }
            if (Item == null)
            {
                throw new ArgumentNullException(nameof(Item));
            }
            foreach (T item in Collection)
            {
                if ((Comparer != null && Comparer.Equals(item, Item))
                    || (Comparer == null && Item.Equals(item)))
                {
                    return;
                }
            }
            Collection.Add(Item);
        }
        public static void AddUnique<T>(this ICollection<T> Collection, T Item)
        {
            Collection?.AddUnique(Item, null);
        }

        /// <summary>
        /// Adds an <paramref name="Item"/> to the <paramref name="List"/> if it doesn't already contain the item. Replaces the <paramref name="Item"/> in the <paramref name="List"/>, or only conditonally does so <paramref name="OnBasisOldNew"/> of the provided <see cref="Func{T, T, bool}"/>, returning true if the <paramref name="List"/> is altered.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="List">The list of <paramref name="Item"/>s to add the unique entry to.</param>
        /// <param name="Item">The item being added tp the <paramref name="List"/> if the <paramref name="List"/> already contains the item according to the provided <paramref name="Compare"/>.</param>
        /// <param name="Compare">The function by which to determine whether the <paramref name="List"/> already contains the passed <paramref name="Item"/>.</param>
        /// <param name="OnBasisOldNew">The function by which to determine if the passed <paramref name="Item"/> should replace an existing one as determined by <paramref name="Compare"/>.</param>
        /// <returns>
        ///     <see langword="true"/> if the <paramref name="List"/> is altered;<br />
        ///     <see langword="false"/>, otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     If <paramref name="List"/> is <see langword="null"/>, or if <paramref name="Item"/>.<see cref="object.Equals"/>(<see langword="default"/>)
        /// </exception>
        public static bool AddUnique<T>(this IList<T> List, T Item, Func<T, T, bool> Compare, Func<T, T, bool> OnBasisOldNew)
        {
            if (List == null)
            {
                throw new ArgumentNullException(nameof(List));
            }
            if (Item.Equals(default))
            {
                throw new ArgumentNullException(nameof(Item));
            }
            int index = -1;
            foreach (T item in List)
            {
                if ((Compare != null && Compare(item, Item))
                    || (Compare == null && item.Equals(Item)))
                {
                    index = List.IndexOf(item);
                    break;
                }
            }
            if (index >= 0)
            {
                if (OnBasisOldNew == null || OnBasisOldNew(Item, List[index]))
                {
                    List[index] = Item;
                    return true;
                }
                return false;
            }
            List.Add(Item);
            return true;
        }

        public static bool AddUnique<T>(this IList<T> List, T Item, EqualityComparer<T> EqComparer, Comparer<T> Comparer)
        {
            if (List == null)
            {
                throw new ArgumentNullException(nameof(List));
            }
            int index = -1;
            foreach (T item in List)
            {
                if ((EqComparer != null && EqComparer.Equals(item, Item))
                    || (EqComparer == null && item.Equals(Item)))
                {
                    index = List.IndexOf(item);
                    break;
                }
            }
            if (index >= 0)
            {
                if (Comparer == null || Comparer.Compare(Item, List[index]) > 0)
                {
                    List[index] = Item;
                    return true;
                }
                return false;
            }
            List.Add(Item);
            return true;
        }
        public static bool AddUniqueObject<T>(this IList<T> List, T Item, EqualityComparer<T> EqComparer, Comparer<T> Comparer)
            where T : class
        {
            if (Item is null)
            {
                throw new ArgumentNullException(nameof(Item));
            }
            return List.AddUnique(Item, EqComparer, Comparer);
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
                    nameof(PartToCopy.DeepCopy) + " of " + 
                    typeof(T).Name + ")", 
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

        public static IEnumerable<GameObjectBlueprint> GetBlueprints(this GameObjectFactory Factory, Predicate<GameObjectBlueprint> Filter)
        {
            return Factory.BlueprintList.Where(bp => Filter == null || Filter(bp));
        }

        public static GameObjectBlueprint GetGameObjectBlueprint(this string Blueprint)
        {
            return Utils.GetGameObjectBlueprint(Blueprint);
        }

        public static IEnumerable<GameObjectBlueprint> GetBlueprintInherits(this GameObjectBlueprint Blueprint)
        {
            GameObjectBlueprint inheritedBlueprint = Blueprint?.Inherits?.GetGameObjectBlueprint();
            while (inheritedBlueprint != null)
            {
                yield return inheritedBlueprint;
                inheritedBlueprint = inheritedBlueprint?.Inherits?.GetGameObjectBlueprint();
            }
        }
        public static IReadOnlyList<GameObjectBlueprint> GetBlueprintInheritsList(this GameObjectBlueprint Blueprint)
        {
            return GetBlueprintInherits(Blueprint)?.ToList();
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
        public static void GetCorpseBlueprintAndChance(this GameObjectBlueprint Blueprint, out string CorpseBlueprint, out int CorpseChance)
        {
            CorpseBlueprint = Blueprint?.GetCorpseBlueprint();
            CorpseChance = (Blueprint?.GetCorpseChance()).GetValueOrDefault();
        }
        public static bool TryGetCorpseBlueprintAndChance(this GameObjectBlueprint Blueprint, out string CorpseBlueprint, out int CorpseChance)
        {
            CorpseChance = 0;
            return Blueprint.TryGetCorpseBlueprint(out CorpseBlueprint)
                && Blueprint.TryGetCorpseChance(out CorpseChance);
        }
        public static CorpseEntityPair GetCorpseBlueprintWeightPair(this GameObjectBlueprint Blueprint)
        {
            CorpseEntityPair CorpseBlueprintWeightPair = null;
            if (Blueprint?.GetCorpseBlueprint() is string corpseBlueprint
                && Blueprint?.GetCorpseChance() is int corpseChance)
            {
                CorpseBlueprintWeightPair = new(new CorpseBlueprint(corpseBlueprint), new EntityBlueprint(Blueprint), corpseChance, CorpseEntityPair.PairRelationship.PrimaryCorpse);
            }
            return CorpseBlueprintWeightPair;
        }
        public static bool TryGetCorpseBlueprintAndChance(this GameObjectBlueprint Blueprint, out CorpseEntityPair CorpseBlueprintWeightPair)
        {
            return (CorpseBlueprintWeightPair = GetCorpseBlueprintWeightPair(Blueprint)) != null;
        }

        public static bool IsCorpse(this GameObjectBlueprint Blueprint, Predicate<GameObjectBlueprint> Filter)
        {
            return Blueprint != null
                && (Blueprint.InheritsFrom("Corpse") || Blueprint.Name == "Corpse" || Blueprint.HasTag(PSEUDOCORPSE))
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

        public static bool InheritsFromAny(this GameObjectBlueprint Blueprint, params string[] BaseBlueprints)
        {
            foreach (string baseBlueprint in BaseBlueprints)
            {
                if (Blueprint.InheritsFrom(baseBlueprint))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool InheritsFrom(this string Blueprint, string BaseBlueprint)
        {
            return Utils.ThisBlueprintInheritsFromThatOne(Blueprint, BaseBlueprint);
        }
        public static bool InheritsFromAny(this string Blueprint, List<string> BaseBlueprints)
        {
            foreach (string baseBlueprint in BaseBlueprints)
            {
                if (Blueprint.InheritsFrom(baseBlueprint))
                {
                    return true;
                }
            }
            return false;
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

        public static IEnumerable<BodyPart> LoopParts(this Body Body, Predicate<BodyPart> Filter)
        {
            if (Body == null)
            {
                yield break;
            }
            foreach(BodyPart bodyPart in Body.LoopParts())
            {
                if (Filter == null || Filter(bodyPart))
                {
                    yield return bodyPart;
                }
            }
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

        public static Dictionary<string, int> ConvertToWeightedList(this IEnumerable<EntityWeight> Entries)
        {
            Dictionary<string, int> weightedList = new();
            foreach ((BlueprintBox blueprint, int weight) in Entries)
            {
                string key = blueprint.ToString();
                if (weightedList.ContainsKey(key))
                {
                    weightedList[key] += weight;
                }
                else
                {
                    weightedList.Add(key, weight);
                }
            }
            return weightedList;
        }

        public static Dictionary<string, int> ConvertToWeightedList(this IEnumerable<EntityWeightRelationship> Entries)
        {
            Dictionary<string, int> weightedList = new();
            foreach ((BlueprintBox blueprint, int weight, Relationship rel) in Entries)
            {
                string key = blueprint.ToString() + "@" + rel.ToString();
                if (weightedList.ContainsKey(key))
                {
                    weightedList[key] += weight;
                }
                else
                {
                    weightedList.Add(key, weight);
                }
            }
            return weightedList;
        }

        public static string PrependBullet(
            this string Text,
            string Bullet = "\u0007",
            string BulletColor = "K")
        {
            if (Bullet.IsNullOrEmpty())
            {
                return Text;
            }
            return Utils.Bullet(Bullet, BulletColor) + " " + Text;
        }

        public static string GenerateBulletList(
            this IEnumerable<string> Items,
            string Label = null,
            string Bullet = "\u0007",
            string BulletColor = "K",
            Func<string,string> ItemPreProc = null,
            Func<string,string> ItemPostProc = null)
        {
            Label = Label == null ? "" : Label + "\n";
            string output = "";
            foreach (string item in Items)
            {
                string preProcItem = ItemPreProc != null ? ItemPreProc(item) : item;
                if (!output.IsNullOrEmpty())
                {
                    output += "\n";
                }
                string prePostProc = preProcItem.PrependBullet(Bullet, BulletColor);
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

        public static IEnumerable<string> ConvertToStringListWithItemCount<T>(
            this IEnumerable<T> Entries)
        {
            foreach ((T item, int count) in Entries.ConvertToWeightedList())
            {
                yield return count.Things(item.ToString());
            }
            yield break;
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

        public static T GetWeightedRandom<T>(this Dictionary<T, int> WeightedList, bool Include0Weight = true)
        {
            using Indent indent = new();

            int maxWeight = 0;
            List<T> tickets = new(WeightedList.Keys);
            foreach (T ticket in tickets)
            {
                if (Include0Weight && WeightedList[ticket] == 0)
                {
                    WeightedList[ticket]++;
                }
                maxWeight += WeightedList[ticket];
            }
            int rolledAmount = Stat.RandomCosmetic(0, maxWeight - 1);

            Debug.LogMethod(indent[1],
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(rolledAmount), rolledAmount),
                    Debug.Arg(nameof(maxWeight), maxWeight)
                });

            int cumulativeWeight = 0;
            foreach ((T ticket, int weight) in WeightedList)
            {
                if (weight < 1)
                {
                    continue;
                }
                cumulativeWeight += weight;
                if (rolledAmount < cumulativeWeight)
                {
                    return ticket;
                }
            }
            return default;
        }

        public static List<T> ForEach<T>(this List<T> List, Action<T> Action)
        {
            List.ForEach(Action);
            return List;
        }

        public static TOut ForEach<T, TOut>(this List<T> List, Action<T> Action, TOut Return)
        {
            List.ForEach(Action);
            return Return;
        }

        public static string Pluralize<T>(this T EnumEntry)
            where T : struct, Enum
        {
            return EnumEntry.ToString().Pluralize();
        }

        public static int SetMinValue(ref this int Int, int Min)
        {
            if (Int < Min)
            {
                Int = Min;
            }
            return Int;
        }
        public static int SetMaxValue(ref this int Int, int Max)
        {
            if (Int > Max)
            {
                Int = Max;
            }
            return Int;
        }

        public static string YehNah(this bool? Yeh)
            => Utils.YehNah(Yeh);

        public static string BodyPartString(this BodyPart BodyPart, bool WithManager = false, bool WithParent = false, bool Recursive = false)
        {
            if (BodyPart == null)
            {
                return NULL;
            }
            string managed = null;
            if (WithManager && !BodyPart.Manager.IsNullOrEmpty())
            {
                managed = "[::" + BodyPart.Manager + "]";
            }
            string parent = null;
            if (WithParent && BodyPart.ParentPart is BodyPart parentPart)
            {
                parent = " of parent " + parentPart.BodyPartString(WithManager, Recursive, Recursive);
            }
            return "(" + BodyPart.ID + "):" + BodyPart.Type + "/" + BodyPart.VariantType + managed + parent;
        }

        public static IEnumerable<string> GetFieldAndPropertyNames(this object Root)
        {
            if (Root == null)
                yield break;

            Traverse rootWalk = new(Root);

            foreach (string fieldName in rootWalk.Fields())
                yield return fieldName;

            foreach (string propertyName in rootWalk.Properties())
                yield return propertyName;
        }

        public static IEnumerable<MemberInfo> GetMembers(this object Root, IEnumerable<string> MemberNameList)
            => Root?.GetType()?.GetMembers()
                ?.Where(mi => !MemberNameList.IsNullOrEmpty() && MemberNameList.Contains(mi.Name));

        public static IEnumerable<MemberInfo> GetFieldsAndProperties(this object Root)
            => Root.GetMembers(Root.GetFieldAndPropertyNames());

        public static IEnumerable<KeyValuePair<string, Traverse>> GetAssignableDeclaredFieldAndPropertyKeyValuePairs(
            this object Root,
            Predicate<Traverse> TraverseFilter = null,
            Predicate<FieldInfo> FieldFilter = null,
            Predicate<PropertyInfo> PropertyFilter = null)
        {
            if (Root == null)
                yield break;

            Traverse rootWalk = new(Root);
            foreach (MemberInfo memberInfo in Root.GetFieldsAndProperties())
            {
                if (memberInfo is FieldInfo fieldInfo)
                {
                    if (fieldInfo.IsLiteral)
                        continue;

                    if (FieldFilter != null && !FieldFilter(fieldInfo))
                        continue;
                }

                if (memberInfo is PropertyInfo propertyInfo)
                {
                    if (!propertyInfo.CanWrite)
                        continue;

                    if (PropertyFilter != null && !PropertyFilter(propertyInfo))
                        continue;
                }

                if (rootWalk.GetFieldOrProp(memberInfo.Name) is Traverse fieldProp)
                    if (TraverseFilter == null || TraverseFilter(fieldProp))
                        yield return new(memberInfo.Name, fieldProp);
            }
        }

        public static Dictionary<string, Traverse> GetAssignableDeclaredFieldAndPropertyDictionary(
            this object Root,
            Predicate<Traverse> TraverseFilter = null,
            Predicate<FieldInfo> FieldFilter = null,
            Predicate<PropertyInfo> PropertyFilter = null)
        {
            Dictionary<string, Traverse> output = new();

            if (Root == null)
                return output;

            Traverse rootWalk = new(Root);
            foreach (MemberInfo memberInfo in Root.GetFieldsAndProperties())
            {
                if (memberInfo is FieldInfo fieldInfo)
                {
                    if (fieldInfo.IsLiteral)
                        continue;

                    if (FieldFilter != null && !FieldFilter(fieldInfo))
                        continue;

                    if (rootWalk.Field(memberInfo.Name) is Traverse field)
                        if (TraverseFilter == null || TraverseFilter(field))
                            output.TryAdd(memberInfo.Name, field);

                    continue;
                }

                if (memberInfo is PropertyInfo propertyInfo)
                {
                    if (!propertyInfo.CanWrite || !propertyInfo.CanRead)
                        continue;

                    if (PropertyFilter != null && !PropertyFilter(propertyInfo))
                        continue;

                    if (rootWalk.Property(memberInfo.Name) is Traverse property)
                        if (TraverseFilter == null || TraverseFilter(property))
                        output.TryAdd(memberInfo.Name, property);

                    continue;
                }
            }
            return output;
        }

        public static Traverse GetFieldOrProp(this Traverse FromWalk, string FieldPropName)
        {
            if (FromWalk == null || FieldPropName.IsNullOrEmpty())
                return null;

            if (FromWalk.Field(FieldPropName) is Traverse field)
                if (field.FieldExists())
                    return field;

            if (FromWalk.Property(FieldPropName) is Traverse prop)
                if (prop.PropertyExists())
                    return prop;

            return null;
        }

        public static bool HasFieldOrProp(this Traverse FromWalk, string FieldPropName)
            => FromWalk.Field(FieldPropName).FieldExists()
            || FromWalk.Property(FieldPropName).PropertyExists();

        public static bool HasValueIsPrimativeType(this Traverse Member)
            => Utils.HasValueIsPrimativeType(Member);

        public static bool HasFieldOrPropAndValueIsPrimativeType(this Traverse FromWalk, string FieldPropName)
            => !FieldPropName.IsNullOrEmpty()
            && FromWalk.HasFieldOrProp(FieldPropName)
            && FromWalk.GetFieldOrProp(FieldPropName) is Traverse fieldProp
            && fieldProp.HasValueIsPrimativeType();

        public static bool IsLibrarian(this GameObject Object)
            => Object.GetTagOrStringProperty("SpawnedFrom") is string spawnedFrom
            && spawnedFrom.Equals("Mechanimist Convert Librarian");

        public static bool HasIntPropertyGTZeo(this GameObject Object, string Property)
            => Object.TryGetIntProperty(Property, out int result)
            && result > 0;

        public static bool IsVillageWarden(this GameObject Object)
            => Object.HasIntPropertyGTZeo("VillageWarden");

        public static bool IsNamedVillager(this GameObject Object)
            => Object.HasIntPropertyGTZeo("NamedVillager");

        public static bool IsParticipantVillager(this GameObject Object)
            => Object.HasIntPropertyGTZeo("ParticipantVillager");

        public static bool IsVillager(this GameObject Object)
            => Object.HasIntPropertyGTZeo("Villager");

        public static string ToStringWithNum<T>(this T Enum)
            where T : struct, Enum
            => (Enum is int intEnum)
            ? Enum + "(" + intEnum + ")"
            : Enum.ToString();

        public static string SafeJoin<T>(this IEnumerable<T> Enumerable, string Delimiter = ", ")
        {
            if (Enumerable == null || Enumerable.Count() < 1)
                return null;

            return Enumerable.ConvertToStringList(t => t?.ToString() ?? "").Join(delimiter: Delimiter);
        }

        public static GameObject CreateSample(this GameObjectBlueprint Model, Action<GameObject> BeforeObjectCreated = null)
            => Model != null
            ? GameObjectFactory.Factory.CreateSampleObject(Model, BeforeObjectCreated)
            : null;
    }
}
