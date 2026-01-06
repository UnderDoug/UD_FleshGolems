using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using Qud.API;

using XRL;
using XRL.World;
using XRL.World.Text.Attributes;
using XRL.World.Text.Delegates;
using XRL.World.Parts;
using XRL.Language;
using XRL.Rules;

using UD_FleshGolems.Logging;
using UD_FleshGolems.Parts.VengeanceHelpers;
using UD_FleshGolems.ModdedText.TextHelpers;
using UD_FleshGolems.Attributes;
using Debug = UD_FleshGolems.Logging.Debug;

using static UD_FleshGolems.Const;
using System.Reflection;
using System.Diagnostics;
using XRL.Wish;
using XRL.UI;

namespace UD_FleshGolems.ModdedText
{
    [HasWishCommand]
    [Has_UD_FleshGolems_ModdedTextFilter]
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    public static class ModdedTextFilters
    {
        [UD_FleshGolems_DebugRegistry]
        public static List<MethodRegistryEntry> doDebugRegistry(List<MethodRegistryEntry> Registry)
        {
            Dictionary<string, bool> multiMethodRegistrations = new()
            {
                // { nameof(CheckCanBeReplaced), false },
                { nameof(CheckCanStartBeReplaced), false },
                { nameof(CheckCanEndBeReplaced), false },
            };

            foreach (MethodBase extensionMethod in typeof(UD_FleshGolems.ModdedText.ModdedTextFilters).GetMethods() ?? new MethodBase[0])
                if (multiMethodRegistrations.ContainsKey(extensionMethod.Name))
                    Registry.Register(extensionMethod, multiMethodRegistrations[extensionMethod.Name]);

            return Registry;
        }

        public enum ReplacementLocation
        {
            None,
            Whole,
            Start,
            Middle,
            End,
            Any,
        }
        public struct RandomStringReplacement
        {
            public string Key;
            public int ChanceOneIn;
            public Dictionary<string, int> WeightedEntries;
            public RandomStringReplacement(
                string Key,
                int ChanceOneIn,
                Dictionary<string, int> WeightedEntries)
            {
                this.Key = Key;
                this.ChanceOneIn = ChanceOneIn;
                this.WeightedEntries = WeightedEntries;
            }
            public void Deconstruct(out string Key, out int ChanceOneIn, out Dictionary<string, int> WeightedEntries)
            {
                Key = this.Key;
                ChanceOneIn = this.ChanceOneIn;
                WeightedEntries = this.WeightedEntries;
            }
        }

        [ModSensitiveStaticCache(createEmptyInstance: false)]
        [GameBasedStaticCache(ClearInstance: true)]
        private static Dictionary<string, MethodInfo> _TextFilterEntries;
        public static Dictionary<string, MethodInfo> TextFilterEntries
        {
            get
            {
                if (_TextFilterEntries.IsNullOrEmpty())
                {
                    bool returnsString(MethodInfo MethodInfo)
                        => MethodInfo?.ReturnType == typeof(string);

                    bool firstParameterIsString(MethodInfo MethodInfo)
                        => MethodInfo?.GetParameters() is ParameterInfo[] parameters
                        && parameters.Length > 0
                        && parameters[0].ParameterType == typeof(string);

                    List<MethodInfo> textFilterMethods = ModManager.GetMethodsWithAttribute(
                        AttributeType: typeof(UD_FleshGolems_ModdedTextFilterAttribute),
                        ClassFilterType: typeof(Has_UD_FleshGolems_ModdedTextFilterAttribute),
                        Cache: false)
                            ?.Where(returnsString)
                            ?.Where(firstParameterIsString)
                            ?.ToList()
                        ?? new();

                    foreach (MethodInfo textFilterMethod in textFilterMethods)
                    {
                        _TextFilterEntries ??= new();

                        if (textFilterMethod?.GetCustomAttribute<UD_FleshGolems_ModdedTextFilterAttribute>() is var textFilterAttribute
                            && (!_TextFilterEntries.ContainsKey(textFilterAttribute.Key) 
                                || textFilterAttribute.Override))
                                _TextFilterEntries[textFilterAttribute.Key] = textFilterMethod;

                        if (textFilterMethod?.Name is string key
                            && !_TextFilterEntries.ContainsKey(key))
                            _TextFilterEntries[key] = textFilterMethod;
                    }
                }
                return _TextFilterEntries;
            }
        }

        public static string LastSnapifiedPhrase = null;

        public static string DeleteString => "##DELETE##";
        public static string NoSpaceBefore => "##NoSpace";
        public static string NoSpaceAfter => "NoSpace##";

        public static string[] ProtectedStrings => new string[]
        {
            DeleteString,
            NoSpaceBefore,
            NoSpaceAfter,
        };

        public static string[] AmAreIs => new string[]
        {
            "am",
            "are",
            "is"
        };

        public static Dictionary<string, int> IsOrDelete => new()
        {
            { "is", 1 },
            { DeleteString, 1 }
        };
        public static Dictionary<string, int> NotOrNo => new()
        {
            { "%not%", 2 },
            { "%no%", 1 },
        };
        public static List<RandomStringReplacement> SnapifyWordReplacements => new()
        {
            new(
                Key: "I",
                ChanceOneIn: 1, 
                WeightedEntries: new()
                { 
                    { "%me%", 1 },
                }),
            new(
                Key: "I'm",
                ChanceOneIn: 1, 
                WeightedEntries: new()
                { 
                    { "%me%", 3 },
                    { "%is%", 1 },
                }),
            new(
                Key: "am",
                ChanceOneIn: 1, 
                WeightedEntries: IsOrDelete),
            new(
                Key: "are",
                ChanceOneIn: 1, 
                WeightedEntries: IsOrDelete),
            new(
                Key: "is",
                ChanceOneIn: 1, 
                WeightedEntries: IsOrDelete),
            new(
                Key: "you",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "yoo", 3 },
                    { "yu", 3 },
                    { "yu-yoo", 1 },
                }),
            new(
                Key: "the",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "%tha%", 2 },
                    { "%da%", 2 },
                    { "%deh%", 1 },
                    { DeleteString, 4 },
                }),
            new(
                Key: "a",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { DeleteString, 1 },
                }),
            new(
                Key: "an",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { DeleteString, 1 },
                }),
            new(
                Key: "and",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "%an'%", 2 },
                    { "%'n'%", 3 },
                    { "%en'%", 1 },
                }),
            new(
                Key: "to",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { DeleteString, 1 },
                }),
            new(
                Key: "what",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "%wat%", 4 },
                    { "%wot%", 3 },
                    { "%wha'%", 1 },
                    { "%wa'%", 2 },
                }),
            new(
                Key: "when",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "%wen%", 1 },
                }),
            new(
                Key: "can't",
                ChanceOneIn: 1,
                WeightedEntries: NotOrNo),
            new(
                Key: "won't",
                ChanceOneIn: 1,
                WeightedEntries: NotOrNo),
            new(
                Key: "don't",
                ChanceOneIn: 1,
                WeightedEntries: NotOrNo),
            new(
                Key: "doesn't",
                ChanceOneIn: 1,
                WeightedEntries: NotOrNo),
            new(
                Key: "haven't",
                ChanceOneIn: 1,
                WeightedEntries: NotOrNo),
            new(
                Key: "aren't",
                ChanceOneIn: 1,
                WeightedEntries: NotOrNo),
        };
        public static List<RandomStringReplacement> SnapifyPartialReplacements => new()
        {
            new(
                Key: "y",
                ChanceOneIn: 3,
                WeightedEntries: new()
                {
                    { "yipp-yi", 1 },
                    { "-yi-yi", 2 },
                }),
            new(
                Key: "ee",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "e-yi-", 1 },
                    { "ee-yi!-", 1 },
                    { "i", 3 },
                }),
            new(
                Key: "ve",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "b", 2 },
                    { "be", 1 },
                }),
            new(
                Key: "v",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "b", 1 },
                }),
            new(
                Key: "dr",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "d", 1 },
                }),
            new(
                Key: "oul",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "ul", 1 },
                    { "u", 3 },
                    { "oo", 2 },
                }),
            new(
                Key: "ou",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "ow", 3 },
                    { "-awoo!-", 1 },
                }),
            new(
                Key: "oo",
                ChanceOneIn: 2,
                WeightedEntries: new()
                {
                    { "u", 1 },
                }),
            new(
                Key: "ph",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "ff", 1 },
                    { "f", 1 },
                }),
            new(
                Key: "ea",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "i", 1 },
                    { "e", 1 },
                }),
            new(
                Key: "sc",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "s", 1 },
                }),
            new(
                Key: "ere",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "eer", 1 },
                    { "ir", 1 },
                }),
        };
        public static List<RandomStringReplacement> SnapifyStartReplacements => new()
        {
            new(
                Key: "ar",
                ChanceOneIn: 3,
                WeightedEntries: new()
                {
                    { "arf-ar", 2 },
                    { "a-{{emote|*snap*}}-ar", 1 },
                }),
            new(
                Key: "ra",
                ChanceOneIn: 3,
                WeightedEntries: new()
                {
                    { "raf-ra", 2 },
                    { "ra-{{emote|*snap*}}-ra", 1 },
                }),
            new(
                Key: "re",
                ChanceOneIn: 3,
                WeightedEntries: new()
                {
                    { "reh-re", 1 },
                }),
            new(
                Key: "ru",
                ChanceOneIn: 3,
                WeightedEntries: new()
                {
                    { "ruh-ru", 2 },
                    { "ru-{{emote|*snap*}}-ru", 1 },
                }),
            new(
                Key: "y",
                ChanceOneIn: 3,
                WeightedEntries: new()
                {
                    { "yipp-yi", 1 },
                }),
            new(
                Key: "th",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "d", 2 },
                    { "y", 2 },
                    { DeleteString, 1 },
                }),
            new(
                Key: "ou",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "ow", 1 },
                }),
            new(
                Key: "he",
                ChanceOneIn: 3,
                WeightedEntries: new()
                {
                    { "he-yeye-", 1 },
                    { "heheHE-", 1 },
                }),
            new(
                Key: "wh",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "w", 1 },
                }),
        };
        public static List<RandomStringReplacement> SnapifyEndReplacements => new()
        {
            new(
                Key: "ar",
                ChanceOneIn: 2,
                WeightedEntries: new()
                {
                    { "arf-ar", 2 },
                    { "a-{{emote|*snap*}}-ar", 1 },
                }),
            new(
                Key: "ra",
                ChanceOneIn: 4,
                WeightedEntries: new()
                {
                    { "raf-ra", 2 },
                    { "ra-{{emote|*snap*}}-ra", 1 },
                }),
            new(
                Key: "re",
                ChanceOneIn: 4,
                WeightedEntries: new()
                {
                    { "reh-re", 1 },
                }),
            new(
                Key: "ru",
                ChanceOneIn: 4,
                WeightedEntries: new()
                {
                    { "ruh- ru", 2 },
                    { "ru-{{emote|*snap*}}-ru", 1 },
                }),
            new(
                Key: "y",
                ChanceOneIn: 3,
                WeightedEntries: new()
                {
                    { "yipp-yi", 1 },
                    { "-yi-yi", 2 },
                }),
            new(
                Key: "th",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "d", 2 },
                    { "y", 2 },
                    { DeleteString, 1 },
                }),
            new(
                Key: "ing",
                ChanceOneIn: 1,
                WeightedEntries: NDeleteStringsAndStrings(3, "in'", "en")),
            new(
                Key: "ee",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "e-yi-", 1 },
                    { "ee-yi!-", 1 },
                    { "i", 3 },
                }),
            new(
                Key: "ve",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "b", 2 },
                    { "be", 1 },
                }),
            new(
                Key: "v",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "b", 1 },
                }),
            new(
                Key: "ed",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "%ed%", 3 },
                    { "%eded%", 1 },
                    { DeleteString, 2 },
                }),
            new(
                Key: "dr",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "d", 1 },
                }),
            new(
                Key: "oul",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "ul", 1 },
                    { "u", 3 },
                    { "oo", 2 },
                }),
            new(
                Key: "ou",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "ow", 3 },
                    { "-awoo!-", 1 },
                }),
            new(
                Key: "oo",
                ChanceOneIn: 2,
                WeightedEntries: new()
                {
                    { "u", 1 },
                }),
            new(
                Key: "he",
                ChanceOneIn: 3,
                WeightedEntries: new()
                {
                    { "he-yeye-", 1 },
                    { "heheHE-", 1 },
                }),
            new(
                Key: "ph",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "ff", 1 },
                    { "f", 1 },
                }),
            new(
                Key: "ea",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "i", 1 },
                    { "e", 1 },
                }),
            new(
                Key: "sc",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "s", 1 },
                }),
            new(
                Key: "th",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "f", 3 },
                    { "ff", 1 },
                }),
            new(
                Key: "ere",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { "eer", 1 },
                    { "ir", 1 },
                }),
        };
        public static Dictionary<List<char>, int> SnapifySwaps => new()
        {
            { new() { 'p', 'b', 'd', }, 1 },
            { new() { 'b', 'd', }, 3 },
            { new() { 'd', 'b', }, 3 },
            { new() { 'k', 'g', }, 2 },
            { new() { 'g', 'k', }, 5 },
            // { new() { 'y', 'h', }, 3 },
            // { new() { 'd', 't', }, 1 },
            { new() { 'n', 'm', }, 1 },
            { new() { 'm', 'n', }, 2 },
        };
        public static Dictionary<string, int> SnapifyAdditions => new()
        {
            { "{{emote|raff!}}", 3 },
            { "{{emote|raf!}}", 3 },
            { "{{emote|raf! Raf}}", 3 },
            { "{{emote|reh!}}", 3 },
            { "{{emote|reh! Reh!}}", 3 },
            { "{{emote|ref!}}", 3 },
            { "{{emote|ref! Ref}}", 3 },
            { NoSpaceBefore + "- {{emote|raff!}} -" + NoSpaceAfter, 1 },
            { "{{emote|arf!}}", 3 },
            { "{{emote|arf! Rarf!}}", 3 },
            { NoSpaceBefore + "- {{emote|yi! Yi!}} -" + NoSpaceAfter, 1 },
            { "{{emote|yi! Yi!}}", 3 },
            { NoSpaceBefore + "- {{emote|yipp! Yipp!}} -" + NoSpaceAfter, 1 },
            { "{{emote|yipp! Yipp!}}", 3 },
            { NoSpaceBefore + "- {{emote|yi!}} -" + NoSpaceAfter, 1 },
            { "{{emote|yi!}}", 3 },
            { NoSpaceBefore + "- {{emote|graa!}} -" + NoSpaceAfter, 1 },
            { "{{emote|graa!}}", 3 },
            { NoSpaceBefore + "- {{emote|rah! Graa!}} -" + NoSpaceAfter, 1 },
            { "{{emote|rah! Graa!}}", 3 },
            { "{{emote|fruf!}}", 3 },
            { "{{emote|*snap*}}", 3 },
            { "{{emote|*snap-snap*}}", 3 },
            { NoSpaceBefore + "... uh...", 3 },
            { NoSpaceBefore + "... hmm...", 3 },
            { NoSpaceBefore + "... er...", 3 },
            { "uh...", 6 },
            { "hmm...", 6 },
            { "er...", 6 },
        };

        public static Dictionary<string, int> StringAndNDeleteString(string String, int N)
        {
            Dictionary<string, int> output = new();
            if (!String.IsNullOrEmpty())
                output.Add(String, 1);

            output.Add(DeleteString, N);

            return output;
        }
        public static Dictionary<string, int> NDeleteStringsAndStrings(int N, params string[] Strings)
        {
            Dictionary<string, int> output = new()
            {
                { DeleteString, N }
            };
            if (!Strings.IsNullOrEmpty())
                foreach (string @string in Strings)
                    if (output.ContainsKey(@string))
                    {
                        output[@string]++;
                    }
                    else
                    {
                        output[@string] = 1;
                    }
            return output;
        }

        public static bool IsCapitalized(this string Word)
            => Word
                ?.Strip()
                ?.Aggregate(
                    seed: "",
                    func: (a, n) => a + (char.IsLetter(n) ? n : null)) is string strippedWord
            && strippedWord[0].ToString() == strippedWord[0].ToString().ToUpper();

        public static string LettersOnly(this string Word)
            => Word
                ?.Strip()
                ?.Aggregate(
                    seed: "", 
                    func: (a, n) => a + (char.IsLetter(n) ? n : null));

        public static string MatchCapitalization(this string Replacement, string Word)
            => Word.IsCapitalized()
            ? Replacement.CapitalizeEx()
            : Replacement.UncapitalizeEx();

        private static string CreateSentence(string Accumulator, Word Next)
            => Utils.CreateSentence(Accumulator, Next.ToString());

        public static Word PerformSnapifyWordReplacemnts(ref Word Word, Word PrevWord, out bool Stop)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Word), Word.Text ?? "no text"),
                    Debug.Arg(nameof(PrevWord), PrevWord != null),
                    Debug.Arg(nameof(SnapifyWordReplacements) + "." + nameof(SnapifyWordReplacements.Count), SnapifyWordReplacements?.Count ?? 0),
                });

            Stop = false;

            if (Word.IsGuarded())
                return Word;

            foreach ((string key, int chanceOneIn, Dictionary<string, int> replacements) in SnapifyWordReplacements)
            {
                if (1.ChanceIn(chanceOneIn)
                    && replacements.GetWeightedRandom() is string replacement
                    && Word.LettersOnly() is string wordWithoutPunctuation
                    && wordWithoutPunctuation.EqualsNoCase(key))
                {
                    string originalWordWithoutPunctuation = wordWithoutPunctuation;
                    if (key.EqualsNoCase("I")
                        && PrevWord is Word previousWord
                        && !previousWord.ImpliesCapitalization())
                        wordWithoutPunctuation = wordWithoutPunctuation.Uncapitalize();

                    if (key.EqualsAnyNoCase(AmAreIs)
                        &&  (!Word.TextEqualsNoCase(wordWithoutPunctuation)
                            || !Word[^1].IsLetterAndNotException(Utils.CapitalizationExceptions))
                        && AmAreIs?.GetRandomElementCosmetic(s => !s.EqualsNoCase(key)) is string alternateReplacemnt)
                        replacement = alternateReplacemnt;

                    string safeReplacement = replacement.MatchCapitalization(wordWithoutPunctuation);
                    if (replacement.EqualsAny(ProtectedStrings))
                        safeReplacement = replacement;

                    Word = Word.Replace(
                        OldValue: originalWordWithoutPunctuation,
                        NewValue: safeReplacement);

                    //stop = true;
                    string debugReplaceString = replacement + " (" + safeReplacement + ")";
                    Debug.CheckYeh(originalWordWithoutPunctuation + "|" + key + ": " + debugReplaceString, Indent: indent[1]);
                    break;
                }
                else
                {
                    Debug.CheckNah(Word.ToString() + "|" + key, Indent: indent[1]);
                }
            }
            return Word;
        }

        public static bool CheckCanBeReplaced(
            Word Word,
            string Key,
            out string WordWithoutPunctuation,
            ReplacementLocation ReplacementLocation = ReplacementLocation.None)
        {
            using Indent indent = new();

            WordWithoutPunctuation = null;

            if (Word == null
                || Word.Length < 1)
                return false;
            Debug.CheckYeh("word not null", indent[1]);

            if (Key == null
                || Key.Length < 1)
                return false;
            Debug.CheckYeh("key not null", indent[1]);

            if (Key.Length >= Word.Length)
                return false;
            Debug.CheckYeh("key shorter than word", indent[1]);

            WordWithoutPunctuation = Word.LettersOnly('%');

            if (WordWithoutPunctuation.IsNullOrEmpty())
                return false;
            Debug.CheckYeh("word has letters and/or whitelisted punctuation", indent[1]);

            if (ReplacementLocation.EqualsAny(ReplacementLocation.Start, ReplacementLocation.Whole)
                && (WordWithoutPunctuation[0] == '%'
                    || Word.Text.TryGetFirstStartsWith(out _, false, ProtectedStrings)))
                return false;
            Debug.CheckYeh("for start and start not guarded", indent[1]);

            if (ReplacementLocation.EqualsAny(ReplacementLocation.End, ReplacementLocation.Whole)
                && (WordWithoutPunctuation[^1] == '%'
                    || ProtectedStrings.Any(s => Word.EndsWith(s))))
                return false;
            Debug.CheckYeh("for end and end not guarded", indent[1]);

            Debug.CheckYeh(nameof(CheckCanBeReplaced), indent[0]);
            return true;
        }
        public static bool CheckCanStartBeReplaced(
            Word Word,
            string Key,
            out string WordWithoutPunctuation,
            out string OriginalStartText)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Word), Word.Text ?? "no text"),
                    Debug.Arg(nameof(Key), Key ?? "no key"),
                    Debug.Arg(nameof(ReplacementLocation) + "." + ReplacementLocation.Start.ToString()),
                });

            OriginalStartText = null;
            if (!CheckCanBeReplaced(Word, Key, out WordWithoutPunctuation, ReplacementLocation.Start))
                return false;

            OriginalStartText = WordWithoutPunctuation[..(Key.Length - 1)];

            Debug.Log(nameof(OriginalStartText), OriginalStartText, Indent: indent[1]);
            Debug.Log(nameof(WordWithoutPunctuation), WordWithoutPunctuation, Indent: indent[1]);
            Debug.YehNah("Match", Good: OriginalStartText.EqualsNoCase(Key), Indent: indent[0]);
            return OriginalStartText.EqualsNoCase(Key);
        }
        public static bool CheckCanEndBeReplaced(
            Word Word,
            string Key,
            out string WordWithoutPunctuation,
            out string OriginalEndText,
            out int EndKeyStartIndex)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Word), Word.Text ?? "no text"),
                    Debug.Arg(nameof(Key), Key ?? "no key"),
                    Debug.Arg(nameof(ReplacementLocation) + "." + ReplacementLocation.End.ToString()),
                });

            OriginalEndText = null;
            EndKeyStartIndex = 0;
            if (!CheckCanBeReplaced(Word, Key, out WordWithoutPunctuation, ReplacementLocation.End))
                return false;

            EndKeyStartIndex = Key.Length - 1;
            OriginalEndText = WordWithoutPunctuation[^EndKeyStartIndex..];

            Debug.Log(nameof(OriginalEndText), OriginalEndText, Indent: indent[1]);
            Debug.Log(nameof(WordWithoutPunctuation), WordWithoutPunctuation, Indent: indent[1]);
            Debug.YehNah("Match", Good: OriginalEndText.EqualsNoCase(Key), Indent: indent[0]);
            return OriginalEndText.EqualsNoCase(Key);
        }
        public static Word PerformSnapifyPartialReplacements(
            List<RandomStringReplacement> ReplacementsList,
            ref Word Word,
            Word PrevWord,
            out bool Stop,
            ReplacementLocation ReplacementLocation = ReplacementLocation.Any)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Word), Word.Text ?? "no text"),
                    Debug.Arg(
                        Name: nameof(ReplacementsList) + "." + nameof(ReplacementsList.Count),
                        Value: ReplacementsList?.Count ?? 0),
                    Debug.Arg(nameof(ReplacementLocation) + "." + ReplacementLocation.ToString()),
                });

            Stop = false;

            if (Word.IsGuarded())
                return Word;

            foreach ((string key, int chanceOneIn, Dictionary<string, int> replacements) in ReplacementsList)
            {
                Debug.LogArgs(
                    MessageBefore: "Replacement(",
                    MessageAfter: ")",
                    Indent: indent[1],
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(key ?? "no key"),
                        Debug.Arg(nameof(chanceOneIn), chanceOneIn),
                        Debug.Arg(
                            Name: nameof(replacements),
                            Value: "[" + 
                                replacements
                                    ?.Aggregate(
                                        seed: "",
                                        func: delegate (string Accumulator, KeyValuePair<string, int> Next)
                                        {
                                            if (!Accumulator.IsNullOrEmpty())
                                                Accumulator += "|";
                                            return Accumulator + Next.Key + ": " + Next.Value;
                                        }) + 
                                    "]"),
                    });

                Word newWord = Word.CopyWithoutText();
                int loopStart = 0;
                int loopLength = 0;
                bool abort = false;
                switch (ReplacementLocation)
                {
                    case ReplacementLocation.Whole:
                        if (1.ChanceIn(chanceOneIn)
                            && replacements.GetWeightedRandom() is string wholeReplacement
                            && Word.LettersOnly() is string wordWithoutPunctuation
                            && wordWithoutPunctuation.EqualsNoCase(key))
                        {
                            string originalWordWithoutPunctuation = wordWithoutPunctuation;
                            if (key.EqualsNoCase("I")
                                && PrevWord is Word previousWord
                                && !previousWord.ImpliesCapitalization())
                                wordWithoutPunctuation = wordWithoutPunctuation.Uncapitalize();

                            if (key.EqualsAnyNoCase(AmAreIs)
                                && (!Word.TextEqualsNoCase(wordWithoutPunctuation)
                                    || !Word[^1].IsLetterAndNotException(Utils.CapitalizationExceptions))
                                && AmAreIs?.GetRandomElementCosmetic(s => !s.EqualsNoCase(key)) is string alternateReplacemnt)
                                wholeReplacement = alternateReplacemnt;

                            string safeReplacement = wholeReplacement.MatchCapitalization(wordWithoutPunctuation);
                            if (wholeReplacement.EqualsAny(ProtectedStrings))
                                safeReplacement = wholeReplacement;

                            newWord = Word.Replace(
                                OldValue: originalWordWithoutPunctuation,
                                NewValue: safeReplacement);

                            Debug.CheckYeh(nameof(newWord), newWord.Text, Indent: indent[1]);

                            abort = true;
                        }
                        else
                            newWord = Word;
                        break;

                    case ReplacementLocation.Start:
                        if (1.ChanceIn(chanceOneIn)
                            && CheckCanStartBeReplaced(
                                Word: Word,
                                Key: key,
                                WordWithoutPunctuation: out string startWordWithoutPunctuation,
                                OriginalStartText: out string originalStartText)
                            && replacements.GetWeightedRandom() is string startReplacement
                            && startReplacement.MatchCapitalization(originalStartText) is string capMatchedStartReplacement)
                        {
                            string safeReplacement = capMatchedStartReplacement;
                            if (startReplacement.EqualsAny(ProtectedStrings))
                                safeReplacement = startReplacement;

                            newWord = Word.ReplaceWord(Word.Text.ReplaceFirst(originalStartText, safeReplacement));
                            Debug.CheckYeh(nameof(newWord), newWord.Text, Indent: indent[1]);
                            // newWord = Word.ReplaceWord(safeReplacement + Word[key.Length..]);
                        }
                        else
                            newWord = Word;
                        break;

                    case ReplacementLocation.End:
                        if (1.ChanceIn(chanceOneIn)
                            && CheckCanEndBeReplaced(
                                Word: Word, 
                                Key: key,
                                WordWithoutPunctuation: out string endWordWithoutPunctuation,
                                OriginalEndText: out string originalEndText,
                                EndKeyStartIndex: out int endKeyStartIndex)
                            && replacements.GetWeightedRandom() is string endReplacement
                            && endReplacement.MatchCapitalization(originalEndText) is string capMatchedEndReplacement)
                        {
                            string safeReplacement = capMatchedEndReplacement;
                            if (endReplacement.EqualsAny(ProtectedStrings))
                                safeReplacement = endReplacement;

                            newWord = Word.ReplaceWord(Word.Text.ReplaceLast(originalEndText, safeReplacement));
                            Debug.CheckYeh(nameof(newWord), newWord.Text, Indent: indent[1]);
                            // newWord = Word.ReplaceWord(Word[..^endKeyStartIndex] + safeReplacement);
                        }
                        else
                            newWord = Word;
                        break;

                    case ReplacementLocation.Middle:
                        if (Word.Length > 2
                            && Word.Length > key.Length
                            && Word[1..^1].ContainsNoCase(key))
                        {
                            if (Word.Length > 0)
                                newWord = Word.ReplaceWord(Word[0].ToString());
                            loopStart = 1;
                            loopLength = Word.Length - 1;
                            Debug.CheckYeh(ReplacementLocation.Middle.ToString(), "[" + loopStart + ".." + loopLength + "]", Indent: indent[1]);
                        }
                        else
                            newWord = Word;
                        break;

                    case ReplacementLocation.Any:
                        if (Word.ContainsNoCase(key))
                        {
                            loopStart = 0;
                            loopLength = Word.Length;
                            Debug.CheckYeh(ReplacementLocation.Any.ToString(), "[" + loopStart + ".." + loopLength + "]", Indent: indent[1]);
                        }
                        else
                            newWord = Word;
                        break;

                    case ReplacementLocation.None:
                    default:
                        Debug.CheckNah(ReplacementLocation.None.ToString(), Indent: indent[1]);
                        newWord = Word;
                        break;
                }
                bool guarded = loopStart > 0 && Word[0] == '%';
                for (int j = loopStart; j < loopLength; j++)
                {
                    Debug.Log(j.ToString(), newWord.Text + "+" + Word[j], Indent: indent[2]);

                    if (Word[j] == '%')
                        guarded = !guarded;

                    if (guarded)
                    {
                        newWord = Word.ReplaceWord(newWord.Text + Word[j]);
                        continue;
                    }

                    if (Word[j..].TryGetFirstStartsWith(
                        out string startsWith,
                        SortLongestFirst: true,
                        Args: ProtectedStrings))
                    {
                        newWord = Word.ReplaceWord(newWord.Text + startsWith);
                        j += startsWith.Length;
                        continue;
                    }

                    int k = j + key.Length;
                    if (1.ChanceIn(chanceOneIn)
                        && k < loopLength
                        && Word[j..k].EqualsNoCase(key)
                        && replacements.GetWeightedRandom() is string replacement)
                    {
                        string safeReplacement = replacement.MatchCapitalization(Word[j..k]);

                        if (replacement.EqualsAny(ProtectedStrings))
                            safeReplacement = replacement;

                        newWord = Word.ReplaceWord(newWord.Text + safeReplacement);
                        j += safeReplacement.Length - key.Length;
                        loopLength += safeReplacement.Length - key.Length;
            }
                    else
                    {
                        newWord = Word.ReplaceWord(newWord.Text + Word[j]);
                    }
                }
                if (loopLength < (Word.Length - 1)
                    && loopLength != 0)
                    newWord = Word.ReplaceWord(newWord.Text + Word[loopLength..]);

                if (Word.Text != newWord.Text)
                    Debug.Log(
                        Field: ReplacementLocation.ToString(),
                        Value: (Word.Text ?? "no text") + " -> " + (newWord.Text ?? "no text"),
                        Indent: indent[2]);

                Word = newWord;

                if (abort)
                    break;
            }
            return Word;
        }

        private static void ClearSwap(ref char PreviousSwap, ref bool DidSwap)
        {
            PreviousSwap = default;
            DidSwap = false;

            using Indent indent = new(1);
            Debug.CheckNah(nameof(ClearSwap), indent);
        }

        public static Word PerformSnapifySwaps(ref Word Word, out bool Stop)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Word), Word.Text ?? "no text"),
                    Debug.Arg(nameof(SnapifySwaps) + "." + nameof(SnapifySwaps.Count), SnapifySwaps?.Count ?? 0),
                });

            Stop = false;

            if (Word.Text.IsNullOrEmpty())
                return Word;

            if (Word.IsGuarded())
                return Word;

            if (Word.Text?.RemoveAllNoCase(ProtectedStrings) is string wordWithOutProtectedStrings
                && wordWithOutProtectedStrings.All(c => !c.IsLetterAndNotException(Utils.CapitalizationExceptions)))
                return Word;

            foreach ((List<char> swaps, int odds) in SnapifySwaps)
            {
                string key = swaps[0].ToString();

                List<char> values = new(swaps);
                values.RemoveAt(0);

                int maxIndex = Word.Length - 1;
                string replacementWord = null;
                bool didSwap = false;
                char previousSwap = default;

                bool guarded = false;

                if (!Word.Contains(key))
                {
                    Debug.CheckNah(key + " not present in " + Word.Text, indent[1]);
                    continue;
                }
                else
                    Debug.CheckYeh(
                        Message: nameof(key),
                        Value: key + " (" + values?.Aggregate("", (a, n) => a + (!a.IsNullOrEmpty() ? "," : null) + n) + ")",
                        Indent: indent[1]);

                for (int i = 0; i < Word.Length; i++)
                {
                    Debug.Log(i + "|" + (replacementWord ?? "null"), Word[i], Indent: indent[2]);
                    if (Word[i] is char currentChar)
                    {
                        if (i > maxIndex)
                        {
                            ClearSwap(ref previousSwap, ref didSwap);
                            replacementWord += currentChar;
                            break;
                        }

                        if (currentChar == '%')
                            guarded = !guarded;

                        if (guarded)
                        {
                            replacementWord += currentChar;
                            ClearSwap(ref previousSwap, ref didSwap);
                            continue;
                        }

                        if (Word[i..].TryGetFirstStartsWith(
                            out string startsWith,
                            SortLongestFirst: true,
                            Args: ProtectedStrings))
                        {
                            replacementWord += startsWith;
                            i += startsWith.Length;
                            ClearSwap(ref previousSwap, ref didSwap);
                            continue;
                        }

                        if (!currentChar.IsLetterAndNotException(Utils.CapitalizationExceptions))
                        {
                            replacementWord += currentChar;
                            ClearSwap(ref previousSwap, ref didSwap);
                            continue;
                        }

                        if (!replacementWord.IsNullOrEmpty()
                            && replacementWord[^1] == '\\')
                        {
                            replacementWord += currentChar;
                            ClearSwap(ref previousSwap, ref didSwap);
                            continue;
                        }

                        if (didSwap && currentChar == previousSwap)
                        {
                            ClearSwap(ref previousSwap, ref didSwap);
                            continue;
                        }

                        if (currentChar.ToString().EqualsNoCase(key)
                            && values.GetRandomElementCosmetic() is char swapChar
                            && Stat.RollCached("1d" + odds) == 1)
                        {
                            bool currentMatchesPreviousChar = i > 0 && Word[i - 1] == currentChar;
                            bool currentMatchesNextChar = i < maxIndex && Word[i + 1] == currentChar;
                            bool swapMatchesPreviousChar = i > 0 && Word[i - 1] == swapChar;
                            bool swapMatchesNextChar = i < maxIndex && Word[i + 1] == swapChar;

                            if (currentMatchesNextChar || swapMatchesNextChar)
                                i++;

                            if (didSwap
                                && (currentMatchesPreviousChar
                                    || swapMatchesPreviousChar))
                            {
                                ClearSwap(ref previousSwap, ref didSwap);
                                continue;
                            }
                            Debug.CheckYeh(nameof(swapChar), currentChar + " -> " + swapChar, indent[2]);
                            replacementWord += swapChar.ToString().MatchCapitalization(currentChar.ToString());
                            previousSwap = swapChar;
                            didSwap = true;
                            continue;
                        }
                        replacementWord += currentChar;
                        ClearSwap(ref previousSwap, ref didSwap);
                    }
                }
                Word = Word.ReplaceWord(replacementWord);
            }
            return Word;
        }

        private static bool IsLetterOrGuard(this char Char)
            => Char.IsLetterAndNotException(Utils.CapitalizationExceptions)
            || Char == '%';
        public static List<Word> PerformSnapifyReduplications(ref List<Word> Words)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Words), Words?.Count ?? 0),
                });

            int wordsCount = Words.Count;
            int i;
            int skip = 0;
            for (i = wordsCount - 1; i >= 0; i--)
            {
                if (skip-- > 0)
                {
                    Debug.CheckNah(i.ToString() + " skipped", "\"" + Words[i] + "\"", Indent: indent[1]);
                    continue;
                }
                if (Words[i].Text.EqualsAnyNoCase(ProtectedStrings))
                {
                    Debug.CheckNah(i.ToString() + " protected", "\"" + Words[i] + "\"", Indent: indent[1]);
                    continue;
                }

                if (1.ChanceIn(20))
                {
                    Words.Insert(i, Words[i]);
                    if (1.ChanceIn(2))
                    {
                        string replacementString = Words[i].Text;
                        string endsWith = null;
                        while (!replacementString.IsNullOrEmpty())
                        {
                            if (IsLetterOrGuard(replacementString[^1]))
                            {
                                replacementString = replacementString[..^1];
                                continue;
                            }

                            if (replacementString.TryGetFirstEndsWith(out endsWith, true, ProtectedStrings)
                                && !endsWith.IsNullOrEmpty())
                            {
                                replacementString = replacementString[..^endsWith.Length];
                                continue;
                            }

                            break;
                        }

                        if (!replacementString.IsNullOrEmpty())
                            Words[i] = Words[i].ReplaceWord(replacementString + "-" + NoSpaceAfter);
                    }
                    Debug.CheckYeh(i.ToString() + " added", "\"" + Words[i] + "\"", Indent: indent[1]);

                    skip = Stat.RandomCosmetic(3, 5);
                    Debug.Log("Skipping " + skip, Indent: indent[2]);
                }
                else
                    Debug.CheckNah(i.ToString()+ " failed roll", "\"" + Words[i] + "\"", Indent: indent[1]);
            }
            return Words;
        }

        public static string GetFilterAddition(Dictionary<string, int> SnapifyAdditions, Func<KeyValuePair<string, int>, bool> Filter)
            => SnapifyAdditions
                ?.Where(Filter)
                ?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                ?.GetWeightedRandom();

        public static List<Word> PerformSnapifyAdditions(ref List<Word> Words)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Words), Words?.Count ?? 0),
                    Debug.Arg(nameof(SnapifyAdditions) + "." + nameof(SnapifyAdditions.Count), SnapifyAdditions?.Count ?? 0),
                });

            int wordsCount = Words.Count;

            if (wordsCount <= 1)
                return Words;

            int i;

            static bool NotFirstOrNotStartWithNoSpace(int Index, string Addition)
                => Index > 0
                || !Addition.StartsWith(NoSpaceBefore);

            static bool NotLastOrNotEndWithNoSpace(int Index, int WordsCount, string Addition)
                => Index < (WordsCount - 1)
                || !Addition.EndsWith(NoSpaceAfter);

            bool EligibleAddition(KeyValuePair<string, int> kvp)
                => NotFirstOrNotStartWithNoSpace(i, kvp.Key)
                && NotLastOrNotEndWithNoSpace(i, wordsCount, kvp.Key);

            for (i = wordsCount - 1; i >= 0; i--)
            {
                if (1.ChanceIn(8)
                    && GetFilterAddition(SnapifyAdditions, EligibleAddition) is string snapifyAddition)
                {
                    if (1.ChanceIn(16)
                        && GetFilterAddition(SnapifyAdditions, EligibleAddition) is string snapifyAdditionalAddition)
                    {
                        Word additionalWordAddition = new(snapifyAdditionalAddition);
                        Words.Insert(i, additionalWordAddition);
                        Debug.CheckYeh(nameof(i) + ": " + i + " \"" + additionalWordAddition.ToString() + "\"", Indent: indent[1]);
                    }
                    Word wordAddition = new(snapifyAddition);
                    Words.Insert(i, wordAddition);
                    Debug.CheckYeh(nameof(i) + ": " + i + " \"" + wordAddition.ToString() + "\"", Indent: indent[1]);
                }
                else
                {
                    Debug.CheckNah(nameof(i) + ": " + i, Indent: indent[1]);
                }
            }
            return Words;
        }

        public static string TrimNoSpaceFromSegment(
            ref string CurrentSegment,
            ref int Indices,
            string BeingTrimmed,
            int SnippetLength = 10)
        {
            using Indent indent = new(1);
            Debug.Log(nameof(CurrentSegment), CurrentSegment, Indent: indent);

            int currentLength = CurrentSegment?.Length ?? 0;
            Indices += currentLength;
            int indexOfSpace = -1;
            for (int j = 1; j < currentLength; j++)
            {
                Index index = BeingTrimmed == NoSpaceBefore ? ^j : j - 1;

                if (CurrentSegment[index] == ' ')
                {
                    indexOfSpace = index.Value;
                    break;
                }
                if (CurrentSegment[index].IsLetterAndNotException(Utils.CapitalizationExceptions))
                    break;
            }

            string segmentForSnippet = CurrentSegment;

            if (indexOfSpace >= 0)
                CurrentSegment = CurrentSegment.Remove(indexOfSpace, 1);

            if (currentLength == 1
                && CurrentSegment == " ")
                CurrentSegment = "";

            if (indexOfSpace >= 0
                || (currentLength == 1
                    && CurrentSegment == " "))
                Debug.CheckYeh("removed at " + (Indices + Math.Max(0, indexOfSpace)), Indent: indent[1]);

            if (indexOfSpace >= 0)
            {
                int halfSnippetLength = SnippetLength / 2;
                int snippetStart = Math.Max(0, indexOfSpace - halfSnippetLength);
                int snippetEnd = Math.Min(indexOfSpace + (SnippetLength - Math.Min(halfSnippetLength, snippetStart)), currentLength - 1);
                string snippetBefore = "{" + segmentForSnippet[snippetStart..indexOfSpace];
                string snippetAfter = segmentForSnippet[indexOfSpace..snippetEnd] + "}";
                Debug.Log("Snippet", snippetBefore + "_" + snippetAfter, Indent: indent[3]);
            }

            Debug.CheckYeh("removed between " + Indices + " and " + (Indices + BeingTrimmed.Length), Indent: indent[1]);
            
            return CurrentSegment;
        }
        
        public static bool TrimNoSpaceString(ref string Phrase)
        {
            using Indent indent = new(1);

            if (!Phrase.Contains(NoSpaceBefore)
                && !Phrase.Contains(NoSpaceAfter))
                return false;

            if (Phrase.Contains(NoSpaceBefore))
            {
                Debug.LogMethod(indent[0],
                    ArgPairs: new Debug.ArgPair[]
                    {
                    Debug.Arg(nameof(Phrase), Phrase?.Length ?? 0),
                    Debug.Arg(nameof(NoSpaceBefore), NoSpaceBefore),
                    });

                string[] noSpaceBeforeSplit = Phrase.Split(NoSpaceBefore);
                int indices = noSpaceBeforeSplit[0]?.Length ?? 0;

                for (int i = 0; i < noSpaceBeforeSplit.Length - 1; i++)
                    TrimNoSpaceFromSegment(ref noSpaceBeforeSplit[i], ref indices, NoSpaceBefore);

                Phrase = noSpaceBeforeSplit.Aggregate("", (a, n) => a + n);
            }
            
            if (Phrase.Contains(NoSpaceAfter))
            {
                Debug.LogMethod(indent[0],
                    ArgPairs: new Debug.ArgPair[]
                    {
                    Debug.Arg(nameof(Phrase), Phrase?.Length ?? 0),
                    Debug.Arg(nameof(NoSpaceAfter), NoSpaceAfter),
                    });

                string[] noSpaceAfterSplit = Phrase.Split(NoSpaceAfter);
                int indices = noSpaceAfterSplit[0]?.Length ?? 0;

                for (int i = 1; i < noSpaceAfterSplit.Length; i++)
                    TrimNoSpaceFromSegment(ref noSpaceAfterSplit[i], ref indices, NoSpaceAfter);

                Phrase = noSpaceAfterSplit.Aggregate("", (a, n) => a + n);
            }

            return true;
        }

        public static List<Word> UnguardWords(ref List<Word> Words, bool IncludeInternal = true)
        {
            Words ??= new();
            for (int i = 0; i < Words.Count; i++)
            {
                Words[i] = Words[i].Unguard();
                if (IncludeInternal)
                    Words[i].Text = Words[i].Text.RemoveAll("%");
                Words[i].DebugLog();
            }
            return Words;
        }

        [UD_FleshGolems_ModdedTextFilter(Key = "snapify")]
        public static string Snapify(string Phrase)
            => Snapify(Phrase, false);

        public static string Snapify(string Phrase, bool WithDebugLogging)
        {
            bool silenceLogging = Debug.SilenceLogging;
            if (!WithDebugLogging)
                Debug.SilenceLogging = true;

            LastSnapifiedPhrase = Phrase;

            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                Debug.Arg(nameof(Phrase) + "." + nameof(Phrase.Length), Phrase?.Length ?? 0),
                });

            Stopwatch sw = new();
            sw.Start();
            string output = Phrase;
            try
            {
                List<string> processedLines = new();
                if (Phrase?.Split("\n")?.ToList() is List<string> lines)
                {
                    foreach (string line in lines)
                    {
                        if (line?.Split(' ')?.ToList()?.ConvertAll(s => new Word(s)) is List<Word> words
                            && words.Count > 0)
                        {
                            for (int i = 0; i < words.Count; i++)
                            {
                                if (words[i] is Word word)
                                {
                                    Word prevWord = i > 0 ? words[i - 1] : null;
                                    bool stop = false;

                                    if (stop)
                                        continue;

                                    words[i] = PerformSnapifyWordReplacemnts(
                                        Word: ref word,
                                        PrevWord: prevWord,
                                        Stop: out stop);

                                    if (stop)
                                        continue;

                                    words[i] = PerformSnapifyPartialReplacements(
                                        ReplacementsList: SnapifyStartReplacements,
                                        Word: ref word,
                                        PrevWord: prevWord,
                                        Stop: out stop,
                                        ReplacementLocation: ReplacementLocation.Start);

                                    if (stop)
                                        continue;

                                    words[i] = PerformSnapifyPartialReplacements(
                                        ReplacementsList: SnapifyPartialReplacements,
                                        Word: ref word,
                                        PrevWord: prevWord,
                                        Stop: out stop,
                                        ReplacementLocation: ReplacementLocation.Middle);

                                    if (stop)
                                        continue;

                                    words[i] = PerformSnapifyPartialReplacements(
                                        ReplacementsList: SnapifyEndReplacements,
                                        Word: ref word,
                                        PrevWord: prevWord,
                                        Stop: out stop,
                                        ReplacementLocation: ReplacementLocation.End);

                                    if (stop)
                                        continue;

                                    words[i] = PerformSnapifySwaps(ref word, out stop);

                                    if (stop)
                                        continue;
                                }
                            }

                            if (!UnguardWords(ref words).IsNullOrEmpty()
                                && !PerformSnapifyAdditions(ref words).IsNullOrEmpty()
                                && !PerformSnapifyReduplications(ref words).IsNullOrEmpty())
                            {
                                string processedLine = words
                                        ?.Aggregate("", CreateSentence)
                                        ?.RemoveAllNoCase(" " + DeleteString, DeleteString + " ", DeleteString)
                                    ?? "";

                                if (TrimNoSpaceString(ref processedLine))
                                    Debug.CheckYeh("Trimmed NoSpace Strings", Indent: indent[2]);

                                processedLines.Add(processedLine);
                            }
                        }
                    }
                    if (!processedLines.IsNullOrEmpty())
                    {
                        int iteration = 0;
                        Phrase = processedLines
                                ?.Aggregate(
                                    seed: "",
                                    func: delegate (string Accumulator, string Next)
                                    {
                                        if (iteration++ > 0)
                                            Accumulator += "\n";
                                        return Accumulator + Next;
                                    })
                            ?? Phrase;
                    }
                }
                output = Phrase.RemoveAll("%");
            }
            catch (Exception x)
            {
                MetricsManager.LogCallingModError(x);
            }
            finally
            {
                Debug.LogTimeStop(Debug.GetCallingMethod() + " took ", sw, Indent: indent[0]);
                if (!WithDebugLogging)
                    Debug.SilenceLogging = silenceLogging;
            }
            return output;
        }
        public static StringBuilder Snapify(this StringBuilder SB)
            => (SB != null
                && Snapify(SB.ToString()) is string snapified)
            ? SB.Clear().Append(snapified)
            : null;

        [WishCommand(Command = "UD_FleshGolems debug snapify")]
        public static void Wish_DebugSnapify()
        {
            string lastSnapifiedPhrase = LastSnapifiedPhrase;
            string output = Snapify(LastSnapifiedPhrase, true);

            UnityEngine.Debug.Log(lastSnapifiedPhrase);
            UnityEngine.Debug.Log(output);

            Popup.Show(lastSnapifiedPhrase);
            Popup.Show(output);
        }
    }
}
