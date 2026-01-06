using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using HarmonyLib;

using XRL;
using XRL.CharacterBuilds.Qud;
using XRL.Collections;
using XRL.Rules;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;
using XRL.World.Effects;
using XRL.World.Conversations;

using static XRL.World.Parts.UD_FleshGolems_ReanimatedCorpse;

using UD_FleshGolems.Parts.VengeanceHelpers;
using UD_FleshGolems.Capabilities.Necromancy;
using UD_FleshGolems.Logging;

using Relationship = UD_FleshGolems.Capabilities.Necromancy.CorpseEntityPair.PairRelationship;

using static UD_FleshGolems.Capabilities.Necromancy.CorpseSheet;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;
using Options = UD_FleshGolems.Options;

using ConvoDelegateContext = XRL.World.Conversations.DelegateContext;
using TextDelegateContext = XRL.World.Text.Delegates.DelegateContext;
using XRL.World.Conversations.Parts;
using System.Text.RegularExpressions;
using UD_FleshGolems.ModdedText.TextHelpers;
using Range = System.Range;

namespace UD_FleshGolems
{
    public static class Extensions
    {
        [UD_FleshGolems_DebugRegistry]
        public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
        {
            Dictionary<string, bool> multiMethodRegistrations = new()
            {
                { nameof(GetWeightedRandom), false },
                { nameof(GetWeightedSeededRandom), false },
                { nameof(TryGetFirstStartsWith), false },
                { nameof(TryGetFirstEndsWith), false },
            };

            foreach (MethodBase extensionMethod in typeof(UD_FleshGolems.Extensions).GetMethods() ?? new MethodBase[0])
                if (multiMethodRegistrations.ContainsKey(extensionMethod.Name))
                    Registry.Register(extensionMethod, multiMethodRegistrations[extensionMethod.Name]);

            return Registry;
        }
        public static bool Contains(this ICollection<MethodRegistryEntry> DebugRegistry, MethodBase MethodBase)
            => !DebugRegistry.IsNullOrEmpty()
            && MethodBase is not null
            && DebugRegistry.Any(mb => mb.Equals(MethodBase));
        
        public static bool GetValue(this ICollection<MethodRegistryEntry> DebugRegistry, MethodBase MethodBase)
        {
            foreach ((MethodBase methodBase, bool value) in DebugRegistry)
                if (MethodBase.Equals(methodBase))
                    return value;

            throw new ArgumentOutOfRangeException(nameof(MethodBase), "Not found.");
        }
        public static bool TryGetValue(
            this ICollection<MethodRegistryEntry> DebugRegistry,
            MethodBase MethodBase,
            out bool Value)
        {
            Value = default;
            if (!DebugRegistry.IsNullOrEmpty()
                && MethodBase is not null
                && DebugRegistry.Contains(MethodBase))
            {
                Value = DebugRegistry.GetValue(MethodBase);
                return true;
            }
            return false;
        }

        public static bool EqualIncludingBothNull<T>(this T Operand1, T Operand2)
            => (Utils.EitherNull(Operand1, Operand2, out bool areEqual) && areEqual) || (Operand1 != null && Operand1.Equals(Operand2));

        public static bool EqualsAny<T>(this T Value, params T[] args)
            => !args.IsNullOrEmpty()
            && !args.Where(t => t.EqualIncludingBothNull(Value)).IsNullOrEmpty();

        public static bool EqualsAnyNoCase(this string Value, params string[] args)
            => !args.IsNullOrEmpty()
            && !args.Where(t => Value != null && Value.EqualsNoCase(t)).IsNullOrEmpty();

        public static bool EqualsAll<T>(this T Value, params T[] args)
            => !args.IsNullOrEmpty()
            && args.Where(t => t.Equals(Value)).Count() == args.Length;

        public static bool EqualsAllNoCase(this string Value, params string[] args)
            => !args.IsNullOrEmpty()
            && args.Where(t => t.EqualsNoCase(Value)).Count() == args.Length;

        public static bool InheritsFrom(this Type T, Type Type, bool IncludeSelf = true)
            => (IncludeSelf && T == Type) 
            || Type.IsSubclassOf(T) 
            || T.IsAssignableFrom(Type) 
            || (T.YieldInheritedTypes().ToList() is List<Type> inheritedTypes 
                && inheritedTypes.Contains(Type));

        public static bool IsPlayerBlueprint(this string Blueprint)
            => Blueprint == Startup.Initializers.PlayerBlueprint;

        public static bool HasPlayerBlueprint(this GameObject Entity)
        {
            if (Entity.Blueprint.IsPlayerBlueprint())
            {
                Startup.Initializers.PlayerID ??= Entity.ID;
                return true;
            }
            return false;
        }

        public static bool HasPlayerID(this GameObject Entity)
            => Entity.ID == Startup.Initializers.PlayerID;

        public static bool IsPlayerDuringWorldGen(this GameObject Entity)
            => Entity.HasPlayerID()
            || Entity.HasPlayerBlueprint()
            || Entity.IsPlayer();

        public static string ToLiteral(this string String, bool Quotes = false)
        {
            if (String.IsNullOrEmpty())
            {
                return null;
            }
            string output = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(String, false);
            if (Quotes)
            {
                output = "\"" + output + "\"";
            }
            return output;
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
            => @char.ToString().ThisManyTimes(Times);

        public static void AddUnique<T>(this List<T> Collection, T Item)
            => Collection?.AddIfNot(Item, e => Collection.Contains(Item));

        public static T GetRandomElementCosmeticExcluding<T>(this IEnumerable<T> Enumerable, Predicate<T> Exclude)
            where T : class
            => Enumerable
                ?.Where(t => Exclude == null || !Exclude(t))
                ?.GetRandomElementCosmetic();

        public static char GetRandomElementCosmeticExcluding(this IEnumerable<char> Enumerable, Predicate<char> Exclude)
            => Enumerable
                ?.Where(t => Exclude == null || !Exclude(t))
                ?.GetRandomElementCosmetic()
            ?? default;

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
                yield return part.Name;
        }

        public static bool OverlapsWith<T>(this IEnumerable<T> Enumerable1, IEnumerable<T> Enumerable2)
        {
            if (Enumerable1 != null
                && Enumerable2 != null)
                foreach (T item1 in Enumerable1)
                    foreach (T item2 in Enumerable2)
                        if (item1.Equals(item2))
                            return true;

            return false;
        }

        public static bool ContainsAny<T>(this IEnumerable<T> Enumerable, params T[] Items)
            => Items == null
                || Enumerable == null
            ? (Items == null) == (Enumerable == null)
            : Enumerable.OverlapsWith(Items);

        public static bool ContainsAll<T>(this ICollection<T> Collection1, ICollection<T> Collection2)
        {
            int matches = 0;
            int targetNumMatches = Collection2.Count;

            if (targetNumMatches > Collection1.Count)
                return false;

            foreach (T item2 in Collection2)
                foreach (T item1 in Collection1)
                    if (item1.Equals(item2)
                        && targetNumMatches == ++matches)
                        break;

            return targetNumMatches >= matches;
        }
        public static bool ContainsAll<T>(this ICollection<T> Collection, params T[] Items)
            => (Items == null 
                || Collection == null)
            ? (Items == null) == (Collection == null)
            : Collection.ContainsAll((ICollection<T>)Items);

        public static bool ContainsAll(this string String, params string[] Strings)
        {
            if (Strings == null || String == null)
                return (Strings == null) == (String == null);

            foreach (string item in Strings)
                if (!String.Contains(item))
                    return false;

            return true;
        }

        public static bool ContainsAny(this string String, params string[] Strings)
        {
            if (Strings == null || String == null)
                return (Strings == null) == (String == null);

            foreach (string item in Strings)
                if (String.Contains(item))
                    return true;

            return false;
        }

        public static GameObject SetWontSell(this GameObject Item, bool WontSell)
        {
            Item.SetIntProperty(nameof(WontSell), WontSell ? 1 : 0, RemoveIfZero: true);
            return Item;
        }

        public static int DamageTo1HP(this GameObject Creature)
            => (Creature == null 
                || Creature.GetStat("Hitpoints") is not Statistic hitpoints)
            ? 0
            : hitpoints.Value - 1;

        public static T OverrideWithDeepCopyOrRequirePart<T>(
            this GameObject GameObject,
            T PartToCopy,
            Func<GameObject, GameObject> MapInv = null)
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
                BodyPart.DefaultBehaviorBlueprint = DefaultBehavior.Blueprint;

            return BodyPart.DefaultBehavior == DefaultBehavior 
                && (!SetDefaultBehaviorBlueprint 
                    || BodyPart.DefaultBehaviorBlueprint == DefaultBehavior.Blueprint);
        }

        public static IEnumerable<GameObjectBlueprint> GetBlueprints(
            this GameObjectFactory Factory,
            Predicate<GameObjectBlueprint> Filter)
            => Factory.BlueprintList.Where(bp => Filter == null || Filter(bp));

        public static GameObjectBlueprint GetGameObjectBlueprint(this string Blueprint)
            => Utils.GetGameObjectBlueprint(Blueprint);

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
            => Blueprint?.GetBlueprintInherits()?.ToList();

        public static string GetCorpseBlueprint(this GameObjectBlueprint Blueprint)
            => (Blueprint != null
                && Blueprint.TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseBlueprint), out string corpseBlueprint))
            ? corpseBlueprint
            : null;

        public static bool TryGetCorpseBlueprint(this GameObjectBlueprint Blueprint, out string CorpseBlueprint)
            => (CorpseBlueprint = Blueprint?.GetCorpseBlueprint()) != null
            && GameObjectFactory.Factory.HasBlueprint(CorpseBlueprint);

        public static bool TryGetCorpseModel(this GameObjectBlueprint Blueprint, out GameObjectBlueprint CorpseModel)
            => (CorpseModel = Blueprint?.GetCorpseBlueprint()?.GetGameObjectBlueprint()) != null;

        public static int? GetCorpseChance(this GameObjectBlueprint Blueprint)
            => (Blueprint != null
                && Blueprint.TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseChance), out int corpseChance))
            ? corpseChance
            : null;

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
            => (Blueprint?.GetCorpseBlueprint() is string corpseBlueprint
                && Blueprint?.GetCorpseChance() is int corpseChance)
            ? new(
                Corpse: new CorpseBlueprint(corpseBlueprint),
                Entity: new EntityBlueprint(Blueprint),
                Weight: corpseChance,
                Relationship: CorpseEntityPair.PairRelationship.PrimaryCorpse)
            : null;

        public static bool TryGetCorpseBlueprintAndChance(
            this GameObjectBlueprint Blueprint,
            out CorpseEntityPair CorpseBlueprintWeightPair)
            => (CorpseBlueprintWeightPair = GetCorpseBlueprintWeightPair(Blueprint)) != null;

        public static bool IsOrInheritsCorpseOrPseudoCorpse(this GameObjectBlueprint Blueprint)
            => Blueprint != null
            && (Blueprint.InheritsFrom("Corpse")
                || Blueprint.Name == "Corpse"
                || Blueprint.HasTag(PSEUDOCORPSE));

        public static bool IsCorpse(this GameObjectBlueprint Blueprint, Predicate<GameObjectBlueprint> Filter)
            => Blueprint != null
            && Blueprint.IsOrInheritsCorpseOrPseudoCorpse()
            && (Filter == null || Filter(Blueprint));

        public static bool IsCorpse(this GameObjectBlueprint Blueprint)
            => Blueprint != null
            && Blueprint.IsCorpse(null);

        public static bool IsCorpse(this string Blueprint, Predicate<GameObjectBlueprint> Filter = null)
            => Blueprint?.GetGameObjectBlueprint() is GameObjectBlueprint blueprintModel
            && blueprintModel.IsCorpse(Filter);

        public static bool IsInanimateCorpse(this GameObject Corpse, Predicate<GameObjectBlueprint> Filter = null)
            => Corpse != null
            && !Corpse.HasPart<AnimatedObject>()
            && Corpse.HasPart<UD_FleshGolems_CorpseReanimationHelper>()
            && Corpse.GetBlueprint() is GameObjectBlueprint corpseModel
            && corpseModel.IsCorpse(Filter);

        public static bool InheritsFromAny(this GameObjectBlueprint Blueprint, params string[] BaseBlueprints)
            => Blueprint != null
            && !BaseBlueprints.IsNullOrEmpty()
            && BaseBlueprints.Any(bb => Blueprint.InheritsFrom(bb));

        public static bool InheritsFrom(this GameObject Object, string BaseBlueprint)
            => Object != null
            && Object.Blueprint.InheritsFrom(BaseBlueprint);

        public static bool InheritsFrom(this string Blueprint, string BaseBlueprint)
            => Utils.ThisBlueprintInheritsFromThatOne(Blueprint, BaseBlueprint);

        public static bool InheritsFromAny(this string Blueprint, params string[] BaseBlueprints)
            => Blueprint?.GetGameObjectBlueprint() is GameObjectBlueprint blueprintModel
            && blueprintModel.InheritsFromAny(BaseBlueprints);

        public static bool IsBaseBlueprint(this string Blueprint)
            => Utils.IsBaseGameObjectBlueprint(Blueprint);

        public static bool IsExcludedFromDynamicEncounters(this string Blueprint)
            => Utils.IsGameObjectBlueprintExcludedFromDynamicEncounters(Blueprint);

        public static bool HasSTag(this GameObjectBlueprint Blueprint, string STag)
            => Blueprint.HasTag("Semantic" + STag);

        public static bool HasSTag(this string Blueprint, string STag)
            => Blueprint.GetGameObjectBlueprint() is var gameObjectBlueprint
            && gameObjectBlueprint.HasSTag(STag);

        public static bool IsChiliad(this GameObjectBlueprint Blueprint)
            => Blueprint.HasSTag("Chiliad");

        public static bool IsChiliad(this string Blueprint)
            => Blueprint.GetGameObjectBlueprint() is var gameObjectBlueprint
            && gameObjectBlueprint.IsChiliad();

        public static IEnumerable<BodyPart> LoopParts(this Body Body, Predicate<BodyPart> Filter)
        {
            if (Body == null)
                yield break;

            foreach (BodyPart bodyPart in Body.LoopParts())
                if (Filter == null || Filter(bodyPart))
                    yield return bodyPart;
        }

        public static Dictionary<T, int> ConvertToWeightedList<T>(this IEnumerable<T> Items)
        {
            Dictionary<T, int> weightedList = new();
            foreach (T item in Items)
                if (weightedList.ContainsKey(item))
                    weightedList[item]++;
                else
                    weightedList.Add(item, 1);

            return weightedList;
        }

        public static Dictionary<string, int> ConvertToWeightedList(this IEnumerable<EntityWeight> Entries)
        {
            Dictionary<string, int> weightedList = new();
            foreach ((BlueprintBox blueprint, int weight) in Entries)
            {
                string key = blueprint.ToString();

                if (weightedList.ContainsKey(key))
                    weightedList[key] += weight;
                else
                    weightedList.Add(key, weight);
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
                    weightedList[key] += weight;
                else
                    weightedList.Add(key, weight);
            }
            return weightedList;
        }

        public static string PrependBullet(
            this string Text,
            string Bullet = "\u0007",
            string BulletColor = "K")
            => Bullet.IsNullOrEmpty()
            ? Text
            : Utils.Bullet(Bullet, BulletColor) + " " + Text;

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
                yield return count.Things(item.ToString());
        }

        public static IEnumerable<string> ConvertToStringListWithKeyValue<T>(
            this IEnumerable<KeyValuePair<string,T>> Entries,
            Func<KeyValuePair<string, T>, string> Proc)
        {
            foreach (KeyValuePair<string, T> entry in Entries)
                yield return Proc != null ? Proc(entry) : entry.Key + ": " + entry.Value.ToString();
        }

        public static IEnumerable<string> ConvertToStringListWithKeyValue<T>(
            this IEnumerable<KeyValuePair<string,T>> Entries,
            Func<T, string> Proc)
            => Entries.ConvertToStringListWithKeyValue(kvp => kvp.Key + ": " + (Proc != null ? Proc(kvp.Value) : kvp.Value.ToString()));

        public static IEnumerable<string> ConvertToStringListWithKeyValue<T>(
            this IEnumerable<KeyValuePair<string,T>> Entries)
            => Entries.ConvertToStringListWithKeyValue((Func<T, string>)null);

        public static T GetWeightedSeededRandom<T>(this Dictionary<T, int> WeightedList, string Seed, bool Include0Weight = true)
        {
            int maxWeight = 0;
            List<T> tickets = new(WeightedList.Keys);
            foreach (T ticket in tickets)
            {
                if (Include0Weight && WeightedList[ticket] == 0)
                    WeightedList[ticket]++;

                maxWeight += WeightedList[ticket];
            }
            int rolledAmount = 0;
            if (Seed == null)
            {
                rolledAmount = Stat.RandomCosmetic(0, maxWeight - 1);
            }
            else
            {
                Seed = ThisMod.Manifest.ID + "::" + nameof(GetWeightedSeededRandom) + "::" + Seed;
                rolledAmount = Stat.GetSeededRandomGenerator(Seed).Next(0, maxWeight);
            }

            if (Seed != null)
            {
                using Indent indent = new(1);
                Debug.LogMethod(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                    Debug.Arg(nameof(Seed), Seed ?? NULL),
                    Debug.Arg(nameof(rolledAmount), rolledAmount),
                    Debug.Arg(nameof(maxWeight), maxWeight)
                    });
            }

            int cumulativeWeight = 0;
            foreach ((T ticket, int weight) in WeightedList)
            {
                if (weight < 1)
                    continue;

                cumulativeWeight += weight;

                if (rolledAmount < cumulativeWeight)
                    return ticket;
            }
            return default;
        }
        public static T GetWeightedRandom<T>(this Dictionary<T, int> WeightedList, bool Include0Weight = true)
            => WeightedList.GetWeightedSeededRandom(null, Include0Weight);

        public static List<T> ForEach<T>(this List<T> List, Action<T> Action)
        {
            List?.ForEach(Action);
            return List;
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> Enumerable, Action<T> Action)
        {
            Enumerable?.ToList()?.ForEach(Action);
            return Enumerable;
        }

        public static TOut ForEach<T, TOut>(this List<T> List, Action<T> Action, TOut Return)
        {
            List?.ForEach(Action);
            return Return;
        }

        public static TOut ForEach<T, TOut>(this IEnumerable<T> Enumerable, Action<T> Action, TOut Return)
            => !Enumerable.IsNullOrEmpty()
            ? Enumerable.ToList().ForEach(Action, Return)
            : Return;

        public static string Pluralize<T>(this T EnumEntry)
            where T : struct, Enum
            => EnumEntry.ToString().Pluralize();

        public static int SetMinValue(ref this int Int, int Min)
        {
            if (Int < Min)
                Int = Min;

            return Int;
        }
        public static int SetMaxValue(ref this int Int, int Max)
        {
            if (Int > Max)
                Int = Max;

            return Int;
        }

        public static string ReplaceNoCase(this string Text, string OldValue, string NewValue)
            => !Text.IsNullOrEmpty()
            ? Regex.Replace(Text, OldValue ?? "", NewValue ?? "", RegexOptions.IgnoreCase)
            : Text;

        public static string Remove(this string Text, string String)
            => Text != null
                && String != null
                && Text.Contains(String)
            ? Text.Replace(String, "")
            : Text;

        public static string RemoveAll(this string Text, params string[] Items)
            => Items?.Aggregate(Text, (a, n) => a?.Remove(n))
            ?? Text;

        public static string RemoveAllNoCase(this string Text, params string[] Items)
            => Items?.Aggregate(Text, (a, n) => Regex.Replace(a, n, "", RegexOptions.IgnoreCase))
            ?? Text;

        public static string YehNah(this bool? Yeh)
            => Utils.YehNah(Yeh);

        public static string YehNah(this bool Yeh)
            => Utils.YehNah(Yeh);

        public static string BodyPartString(this BodyPart BodyPart, bool WithManager = false, bool WithParent = false, bool Recursive = false)
        {
            if (BodyPart == null)
                return NULL;

            string managed = null;
            if (WithManager && !BodyPart.Manager.IsNullOrEmpty())
                managed = "[::" + BodyPart.Manager + "]";

            string parent = null;
            if (WithParent && BodyPart.ParentPart is BodyPart parentPart)
                parent = " of parent " + parentPart.BodyPartString(WithManager, Recursive, Recursive);

            return "(" + BodyPart.ID + "):" + BodyPart.Type + "/" + BodyPart.VariantType + managed + parent;
        }

        public static IEnumerable<string> GetFieldAndPropertyNames(this object Root)
        {
            if (Root == null)
                yield break;

            Traverse rootWalk = new(Root);

            foreach (string fieldName in rootWalk?.Fields() ?? new())
                yield return fieldName;

            foreach (string propertyName in rootWalk?.Properties() ?? new())
                yield return propertyName;
        }

        public static IEnumerable<MemberInfo> GetMembers(this object Root, IEnumerable<string> MemberNameList)
            => Root
                ?.GetType()
                ?.GetMembers()
                ?.Where(mi => !MemberNameList.IsNullOrEmpty() && MemberNameList.Contains(mi.Name));

        public static IEnumerable<MemberInfo> GetFieldsAndProperties(this object Root)
            => Root?.GetMembers(Root?.GetFieldAndPropertyNames());

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
            => (Enumerable != null
                && Enumerable.Count() > 0)
            ? Enumerable.ConvertToStringList(t => t?.ToString() ?? "").Join(delimiter: Delimiter)
            : null;

        public static string Uncapitalize(this string String)
            => !String.IsNullOrEmpty()
            ? String.UncapitalizeEx()
            : String;

        public static string ReplacerCapitalize(this string String)
            => !String.IsNullOrEmpty()
                && String.Length > 1
                && String.StartsWith("=")
                && String[1..].TryGetIndexOf("=", out int replacerEnd, false)
            ? String[..(replacerEnd + 1)] + "|capitalize" + String[(replacerEnd + 1)..]
            : String.CapitalizeEx();

        public static ConversationText ReplacerCapitalize(this ConversationText ConversationText)
        {
            if (ConversationText?.Text is string conversationTextString)
                ConversationText.Text = conversationTextString.ReplacerCapitalize();

            return ConversationText;
        }

        public static bool IsLetterAndNotException(this char Char, params char[] Exceptions)
            => char.IsLetter(Char)
            && (Exceptions.IsNullOrEmpty()
                || !Char.EqualsAny(Exceptions));

        public static bool IsUpper(this string String)
            => !String.IsNullOrEmpty()
            && String == String.ToUpper();

        public static bool IsUpper(this char Char)
            => Char != default
            && Char.ToString().IsUpper();

        public static bool IsLower(this string String)
            => !String.IsNullOrEmpty()
            && String == String.ToLower();

        public static bool IsLower(this char Char)
            => Char != default
            && Char.ToString().IsLower();

        public static string ReplaceAt(this string String, char Char, int Pos)
        {
            if (String.IsNullOrEmpty())
                throw new ArgumentNullException(
                    paramName: nameof(String),
                    message: "cannot be null or empty");

            if (Pos < 0 || Pos >= String.Length)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(Pos),
                    message: "must be less than length of " + nameof(String) + " and greater than or equal to 0");

            string preString = Pos >= 0 ? String[..Pos] : null;
            string replaceChar = Char != default ? Char.ToString() : "";
            string postString = String.Length > Pos ? String[(Pos + 1)..] : null;

            return preString + replaceChar + postString;
        }


        public static bool TryCapitalize(this char Char, out char CapChar)
        {
            CapChar = default;
            if (Char != default
                && Char.ToString().Capitalize() is string capitalized
                && capitalized != Char.ToString())
            {
                CapChar = capitalized[0];
                return true;
            }
            return false;
        }
        public static string CapitalizeExcept(this string Text, params char[] Exceptions)
        {
            for (int i = 0; i < (Text?.Length ?? 0); i++)
            {
                if (!Text[i].IsLetterAndNotException(Exceptions))
                    continue;

                if (Text[i].IsUpper())
                    break;

                if (Text[i].TryCapitalize(out char capChar))
                    return Text.ReplaceAt(capChar, i);
            }
            return Text;
        }
        public static string CapitalizeEx(this string Text)
            => Text.CapitalizeExcept(CapitalizationExceptions);

        public static bool TryUncapitalize(this char Char, out char UncapChar)
        {
            UncapChar = default;
            if (Char != default
                && Char.ToString().ToLower() is string uncapitalized
                && uncapitalized != Char.ToString())
            {
                UncapChar = uncapitalized[0];
                return true;
            }
            return false;
        }
        public static string UncapitalizeExcept(this string Text, params char[] Exceptions)
        {
            for (int i = 0; i < (Text?.Length ?? 0); i++)
            {
                if (!Text[i].IsLetterAndNotException(Exceptions))
                    continue;

                if (Text[i].IsLower())
                    break;

                if (Text[i].TryUncapitalize(out char uncapChar))
                    return Text.ReplaceAt(uncapChar, i);
            }
            return Text;
        }
        public static string UncapitalizeEx(this string Text)
            => Text.UncapitalizeExcept(CapitalizationExceptions);

        public static string CapitalizeSentences(this string String, bool ExcludeElipses = true)
        {
            if (!String.IsNullOrEmpty())
            {
                List<List<Word>> lines = new();
                foreach (string line in String.Split("\n"))
                {
                    if (line?.Split(' ')?.ToList()?.ConvertAll(s => new Word(s)) is List<Word> words)
                    {
                        lines.Add(words);
                    }
                    else
                    {
                        lines.Add(new() { new(line) });
                    }
                }
                if (!lines.IsNullOrEmpty())
                {
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i] is List<Word> words)
                        {
                            bool capitalizeNext = true;
                            if (words[0].Capitalize().Text is string capitalizedFirst
                                && capitalizedFirst != words[0].Text)
                            {
                                lines[i][0] = lines[i][0].ReplaceWord(capitalizedFirst);
                                if (!words[0].ImpliesCapitalization(ExcludeElipses))
                                    capitalizeNext = false;
                            }
                            for (int j = 0; j < words.Count; j++)
                            {
                                if (words[j] is Word word)
                                {
                                    if (capitalizeNext
                                        && word.Capitalize().Text is string capitalizedWord
                                        && capitalizedWord != word.Text)
                                        words[j] = word.ReplaceWord(capitalizedWord);

                                    if (!words[j].ImpliesCapitalization(ExcludeElipses))
                                        capitalizeNext = false;
                                    else
                                        capitalizeNext = true;
                                }
                            }
                        }
                    }
                }
                List<string> compiledWords = new();
                if (!lines.IsNullOrEmpty())
                {
                    foreach (List<Word> words in lines)
                    {
                        compiledWords.Add(words?.Aggregate("", CreateSentence) ?? "");
                    }
                }
                String = compiledWords
                        ?.Aggregate("", (a, n) => a + (!a.IsNullOrEmpty() ? "\n" : null) + n)
                        ?.CapitalizeEx()
                    ?? String;
            }
            return String?.CapitalizeEx();
        }

        public static bool TryGetIndexOf(this string Text, string Search, out int Index, bool EndOfSearch = true)
            => !((Index = Text.IndexOf(Search) + (EndOfSearch ? Search?.Length ?? 0 : 0)) < 0);

        public static string ReplaceFirst(this string Text, string Search, string Replace)
        {
            int pos = Text.IndexOf(Search);

            if (pos < 0)
                return Text;

            int postPos = pos + Search.Length;
            string textAfter = null;

            if (postPos < Search.Length)
                textAfter = Text[postPos..];

            return Text[..pos] + Replace + textAfter;
            
        }

        public static string ReplaceLast(this string Text, string Search, string Replace)
        {
            string haystack = Text;
            int pos = -1;
            while (haystack.TryGetIndexOf(Search, out int foundIndex, false))
            {
                pos = foundIndex;
                haystack = haystack[foundIndex..];
            }

            if (pos < 0)
                return Text;

            int postPos = pos + Search.Length;
            string textAfter = null;

            if (postPos < Search.Length)
                textAfter = Text[postPos..];

            return Text[..pos] + Replace + textAfter;
        }

        public static bool WasKilledByEntity(this GameObject Corpse, GameObject Entity, out UD_FleshGolems_DeathDetails DeathDetails)
        {
            DeathDetails = null;
            return Corpse != null
                && Entity != null
                && Corpse.TryGetDeathDetails(out DeathDetails)
                && Entity.ID.EqualsAllNoCase(DeathDetails?.Killer?.ID, DeathDetails.KillerDetails?.ID);
        }
        public static bool WasKilledByEntity(this GameObject Corpse, GameObject Entity)
            => Corpse.WasKilledByEntity(Entity);

        public static bool TryRecognizeKiller(
            this GameObject FrankenCorpse,
            int Difficulty,
            string Contest = "Deduction",
            bool IgnoreNaturals = false)
        {
            bool saved = FrankenCorpse.MakeSave(
                Stat: "Intelligence",
                Difficulty: Difficulty,
                Vs: nameof(DeathMemory) + ":" + Contest,
                IgnoreNaturals: IgnoreNaturals);

            using Indent indent = new(1);
            Debug.LogArgs(
                MessageBefore: saved.YehNah() + " " + Debug.GetCallingMethod() + " (",
                MessageAfter: ")",
                Indent: indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(FrankenCorpse), FrankenCorpse?.DebugName ?? NULL),
                    Debug.Arg(nameof(Difficulty), Difficulty),
                    Debug.Arg(nameof(Contest), nameof(DeathMemory) + ":" + Contest),
                    Debug.Arg(nameof(IgnoreNaturals), IgnoreNaturals),
                });
            
            return saved;
        }

        public static bool TryRecognizeKillerEasy(
            this GameObject FrankenCorpse,
            string Contest = "Deduction",
            int Adjustment = 0,
            bool IgnoreNaturals = false)
            => FrankenCorpse.TryRecognizeKiller(
                Difficulty: 15 + Adjustment,
                Contest: Contest,
                IgnoreNaturals: IgnoreNaturals);

        public static bool TryRecognizeKillerMedium(
            this GameObject FrankenCorpse,
            string Contest = "Deduction",
            int Adjustment = 0,
            bool IgnoreNaturals = false)
            => FrankenCorpse.TryRecognizeKiller(
                Difficulty: 20 + Adjustment,
                Contest: Contest,
                IgnoreNaturals: IgnoreNaturals);

        public static bool TryRecognizeKillerHard(
            this GameObject FrankenCorpse,
            string Contest = "Deduction",
            int Adjustment = 0,
            bool IgnoreNaturals = false)
            => FrankenCorpse.TryRecognizeKiller(
                Difficulty: 25 + Adjustment,
                Contest: Contest,
                IgnoreNaturals: IgnoreNaturals);

        public static bool KnowsEntityKilledThem(
            this GameObject Corpse,
            GameObject Entity,
            out UD_FleshGolems_DeathDetails DeathDetails)
        {
            if (WasKilledByEntity(Corpse, Entity, out DeathDetails)
                && DeathDetails.DeathMemory is DeathMemory deathMemory
                && deathMemory.RemembersKiller()
                && DeathDetails.KillerDetails is KillerDetails killerDetails
                && killerDetails.ID == Entity.ID)
            {
                if (deathMemory.GetRemembersKiller() > DeathMemory.KillerMemory.Feature)
                {
                    if (Entity.HasEffect<Disguised>()
                        // && !Corpse.TryRecognizeKillerEasy(nameof(Disguised))
                        )
                        return false;

                    if (deathMemory.RemembersKillerName()
                        && Entity.GetReferenceDisplayName(Short: true) != killerDetails.DisplayName
                        && !Corpse.TryRecognizeKillerEasy(nameof(killerDetails.DisplayName)))
                        return false;

                    if (deathMemory.GetRemembersKiller() > DeathMemory.KillerMemory.Name
                        && deathMemory.RemembersMethod()
                        && !Corpse.TryRecognizeKillerMedium("Deduction"))
                        return false;

                    return Corpse.TryRecognizeKillerEasy("General", IgnoreNaturals: true);
                }
                else
                if (deathMemory.RemembersMethod()
                    && Corpse.TryRecognizeKillerHard("Deduction", IgnoreNaturals: true))
                {
                    if (Entity.HasEffect<Disguised>()
                        // && !Corpse.TryRecognizeKillerEasy(nameof(Disguised))
                        )
                        return false;

                    return true;
                }
            }

            foreach (GameObject companion in Entity.GetCompanions(40) ?? new())
                if (KnowsEntityKilledThem(Corpse, companion, out DeathDetails))
                    return true;

            return false;
        }
        public static bool KnowsEntityKilledThem(this GameObject Corpse, GameObject Entity)
            => Corpse.KnowsEntityKilledThem(Entity, out _);

        public static bool IsNameOfGameObjectBlueprint(this string BlueprintName)
            => !BlueprintName.IsNullOrEmpty()
            && GameObjectFactory.Factory.HasBlueprint(BlueprintName);

        public static bool HasMatchingPath(this ConversationText ConversationText, ConversationText OtherConversationText)
            => ConversationText?.PathID != null
            && ConversationText?.PathID == OtherConversationText?.PathID;

        public static bool HasAnyMatchingPath(this IEnumerable<ConversationText> ConversationTextList, ConversationText ConversationText)
            => !ConversationTextList.IsNullOrEmpty()
            && ConversationTextList.Any(ct => ct.HasMatchingPath(ConversationText));

        public static bool HasAnyMatchingPath(this IEnumerable<IEnumerable<ConversationText>> ConversationTextList, ConversationText ConversationText)
            => !ConversationTextList.IsNullOrEmpty()
            && ConversationTextList.Any(ctl => ctl.Any(ct => ct.HasMatchingPath(ConversationText)));

        public static bool HasAnyMatchingPath<TKey>(
            this Dictionary<TKey, List<ConversationText>>.ValueCollection ConversationTextValueCollection,
            ConversationText ConversationText)
            => !ConversationTextValueCollection.IsNullOrEmpty()
            && ConversationTextValueCollection.AsEnumerable().HasAnyMatchingPath(ConversationText);

        public static bool HasAttribute(this IConversationElement ConversationText, string Attribute)
            => !ConversationText.Attributes.IsNullOrEmpty()
            && ConversationText.Attributes.ContainsKey(Attribute);

        public static bool HasAttributes(this IConversationElement ConversationText, params string[] Attributes)
            => !ConversationText.Attributes.IsNullOrEmpty()
            && !Attributes.IsNullOrEmpty()
            && Attributes.All(a => ConversationText.HasAttribute(a));

        public static bool HasAnyAttributes(this IConversationElement ConversationText, params string[] Attributes)
            => !ConversationText.Attributes.IsNullOrEmpty()
            && !Attributes.IsNullOrEmpty()
            && Attributes.Any(a => ConversationText.HasAttribute(a));

        public static bool HasAttributeWithValue(this IConversationElement ConversationText, string Attribute, string Value)
            => ConversationText.HasAttribute(Attribute)
            && ConversationText.Attributes[Attribute].EqualsNoCase(Value);

        public static bool HasAttributeWithListItemValue(this IConversationElement ConversationText, string Attribute, string Value)
            => ConversationText.HasAttribute(Attribute)
            && ConversationText.Attributes[Attribute].CachedCommaExpansion() is List<string> attributeValues
            && attributeValues.Any(s => s.EqualsNoCase(Value));

        public static List<ConversationText> GetConversationTextsWithText(
            this ConversationText ConversationText,
            List<ConversationText> ConversationTextList,
            Predicate<ConversationText> Filter = null,
            Action<ConversationText> Proc = null,
            bool OnlyPredicateChecked = false)
        {
            if (ConversationText == null)
                return ConversationTextList ?? new();

            ConversationTextList ??= new();
            if (!ConversationText.Text.IsNullOrEmpty()
                && (!OnlyPredicateChecked || ConversationText.CheckPredicates())
                && (Filter == null || Filter(ConversationText))
                && !ConversationTextList.HasAnyMatchingPath(ConversationText))
            {
                Proc?.Invoke(ConversationText);
                ConversationTextList.Add(ConversationText);
            }
            if (!ConversationText.Texts.IsNullOrEmpty())
                foreach (ConversationText conversationSubText in ConversationText.Texts)
                    conversationSubText.GetConversationTextsWithText(
                        ConversationTextList: ConversationTextList,
                        Filter: Filter,
                        Proc: Proc,
                        OnlyPredicateChecked: OnlyPredicateChecked);

            return ConversationTextList ?? new();
        }

        public static UD_FleshGolems_DeathDetails GetDeathDetails(this GameObject Corpse)
            => Corpse?.GetPart<UD_FleshGolems_DeathDetails>();

        public static bool TryGetDeathDetails(this GameObject Corpse, out UD_FleshGolems_DeathDetails DeathDetails)
            => (DeathDetails = Corpse?.GetDeathDetails()) != null;

        public static KillerDetails GetKillerDetails(this GameObject Corpse)
            => Corpse?.GetDeathDetails()?.KillerDetails;

        public static bool TryGetKillerDetails(this GameObject Corpse, out KillerDetails KillerDetails)
            => (KillerDetails = Corpse?.GetKillerDetails()) != null;

        public static DeathDescription GetDeathDescription(this GameObject Corpse)
            => Corpse?.GetDeathDetails()?.DeathDescription;

        public static bool TryGetDeathDescription(this GameObject Corpse, out DeathDescription DeathDescription)
            => (DeathDescription = Corpse?.GetDeathDescription()) != null;

        public static string SetCreatureType(this GameObject GameObject, string CreatureType)
        {
            GameObject.SetStringProperty(nameof(CreatureType), CreatureType);
            return CreatureType;
        }
        public static string SetNotableFeature(this GameObject GameObject, string NotableFeature)
        {
            GameObject.SetStringProperty(KillerDetails.NOTABLE_FEATURE_PROPTAG, NotableFeature);
            return NotableFeature;
        }

        public static ConversationText Append(
            this ConversationText ConversationText,
            ConversationText OtherConversationText,
            string Joiner = null,
            List<string> AttributesToConcatenate = null)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(ConversationText), ConversationText != null),
                    Debug.Arg(nameof(OtherConversationText), OtherConversationText != null),
                    Debug.Arg(nameof(Joiner), "\"" + (Joiner?.ToLiteral() ?? "null") + "\""),
                    Debug.Arg(nameof(AttributesToConcatenate), "\"" + (AttributesToConcatenate?.Aggregate("", (a,n) => a + "," + n)?[1..] ?? "none") + "\""),
                });

            if (ConversationText == null
                || OtherConversationText == null)
                return ConversationText ?? OtherConversationText;

            Dictionary<string, string> attributes = new();
            if (ConversationText.Attributes != null)
                attributes = new(ConversationText.Attributes);

            Dictionary<string, string> otherAttributes = new();
            if (OtherConversationText.Attributes != null)
                otherAttributes = new(OtherConversationText.Attributes);

            foreach ((string key, string value) in attributes)
            {
                string newValue = value;
                if (key.EqualsAnyNoCase(AttributesToConcatenate?.ToArray() ?? new string[0])
                    && otherAttributes.TryGetValue(key, out string otherValue))
                    newValue += "," + otherValue;

                otherAttributes[key] = newValue;
            }

            ConversationText newConversationText = new()
            {
                Parent = ConversationText.Parent ?? OtherConversationText.Parent,
                ID = ConversationText.ID + ":" + OtherConversationText.Parent?.ID + "." + OtherConversationText.ID,
                Text = ConversationText.Text + Joiner + OtherConversationText.Text,
                Attributes = otherAttributes,
            };

            string attributesString = "(" + newConversationText.Attributes?.ToStringForCachedDictionaryExpansion() + "): ";
            Debug.Log(
                newConversationText.PathID.TextAfter(".") + 
                attributesString, // + 
                // newConversationText.Text?.ToLiteral(), 
                Indent: indent[1]);

            return newConversationText;
        }

        public static string TextAfter(this string Text, string Delimiter, int InstanceNumber = 1, string IfNotFound = null)
        {
            if (Text.IsNullOrEmpty() || Delimiter.IsNullOrEmpty())
                return IfNotFound;

            string output = Text;
            for (int i = 0; i < InstanceNumber; i++)
            {
                if (!output.TryGetIndexOf(Delimiter, out int endDelimited))
                {
                    output = IfNotFound;
                    break;
                }
                output = output[endDelimited..];
            }
            return output;
        }

        public static string FirstRoughlyHalf(this string String, int Offset = 5)
        {
            if (String.IsNullOrEmpty())
                return null;

            Offset = Stat.RandomCosmetic(-Offset, Offset);
            int roughlyHalfOfString = Math.Min(Math.Max(0, (String.Length / 2) + Offset), String.Length);
            return String[..roughlyHalfOfString];
        }

        public static StringBuilder TrimLatterRoughlyHalf(this StringBuilder SB, int Offset = 5)
        {
            if (SB.IsNullOrEmpty())
                return null;

            Offset = Stat.RandomCosmetic(-Offset, Offset);
            int roughlyHalfOfString = Math.Min(Math.Max(0, (SB.Length / 2) + Offset), SB.Length);
            int latterRoughlyHalfOfString = SB.Length - roughlyHalfOfString;
            return SB.Remove(roughlyHalfOfString, latterRoughlyHalfOfString);
        }

        public static string AsString(this List<char> CharList)
            => CharList.Aggregate("", (a, n) => a += n);

        public static string ContextCapitalize(this string Output, TextDelegateContext Context)
            => Context.Capitalize ? Output?.CapitalizeEx() : Output;

        public static bool Fail(this GameObject Object, string Message, bool Silent)
            => !Silent && Object.Fail(Message);

        public static int GetLength(Range Range)
            => Range.End.Value - Range.Start.Value;

        public static IEnumerable<string> SubstringsOfLength(this string String, int Length)
        {
            if (String.IsNullOrEmpty()
                || String.Length < Length)
                yield break;

            for (int i = 0; i < String.Length - Length; i++)
                yield return String[i..(i + Length)];
        }

        public static bool ContainsNoCase(this string Text, string Value)
            => !Text.IsNullOrEmpty()
            && !Value.IsNullOrEmpty()
            && Text.SubstringsOfLength(Value.Length)?.ToArray() is string[] substrings
            && Value.EqualsAnyNoCase(substrings);

        public static bool TryGetFirstStartsWith(
            this string Text,
            out string StartsWith,
            bool SortLongestFirst = false,
            params string[] Args)
        {
            StartsWith = null;
            if (Text.IsNullOrEmpty())
                return false;

            if (Args.IsNullOrEmpty())
                return false;

            List<string> argsList = new List<string>(Args)
                ?.Where(s => !s.IsNullOrEmpty())
                ?.ToList();

            if (SortLongestFirst
                && !argsList.IsNullOrEmpty())
                argsList?.Sort((first, second) => first.Length.CompareTo(second.Length));

            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(StartsWith),
                    Debug.Arg(nameof(Text), Text),
                    Debug.Arg(nameof(SortLongestFirst), SortLongestFirst),
                    Debug.Arg(nameof(Args) + "." + nameof(Args.Length), Args?.Length ?? 0),
                });

            foreach (string arg in argsList)
            {
                Debug.YehNah(arg, Good: Text.StartsWith(arg), Indent: indent[1]);
                if (Text.StartsWith(arg))
                {
                    StartsWith = arg;
                    return true;
                }
            }

            return false;
        }
        public static bool TryGetFirstEndsWith(
            this string Text,
            out string EndsWith,
            bool SortLongestFirst = false,
            params string[] Args)
        {
            EndsWith = null;
            if (Text.IsNullOrEmpty())
                return false;

            if (Args.IsNullOrEmpty())
                return false;

            List<string> argsList = new List<string>(Args)
                ?.Where(s => !s.IsNullOrEmpty())
                ?.ToList();

            if (SortLongestFirst
                && !argsList.IsNullOrEmpty())
                argsList?.Sort((first, second) => first.Length.CompareTo(second.Length));

            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(EndsWith),
                    Debug.Arg(nameof(Text), Text),
                    Debug.Arg(nameof(SortLongestFirst), SortLongestFirst),
                    Debug.Arg(nameof(Args) + "." + nameof(Args.Length), Args?.Length ?? 0),
                });

            foreach (string arg in argsList)
            {
                Debug.YehNah(arg, Good: Text.StartsWith(arg), Indent: indent[1]);
                if (Text.EndsWith(arg))
                {
                    EndsWith = arg;
                    return true;
                }
            }

            return false;
        }

        public static string ValueUnits(this TimeSpan Duration)
        {
            string durationUnit = "minute";
            double durationValue = Duration.TotalMinutes;
            if (Duration.TotalMinutes < 1)
            {
                durationUnit = "second";
                durationValue = Duration.TotalSeconds;
            }
            if (Duration.TotalSeconds < 1)
            {
                durationUnit = "millisecond";
                durationValue = Duration.TotalMilliseconds;
            }
            if (Duration.TotalMilliseconds < 1)
            {
                durationUnit = "microsecond";
                durationValue = Duration.TotalMilliseconds / 1000;
            }
            return durationValue.Things(durationUnit);
        }
    }
}
