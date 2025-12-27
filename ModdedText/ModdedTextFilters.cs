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
using Debug = UD_FleshGolems.Logging.Debug;

using static UD_FleshGolems.Const;
using UD_FleshGolems.ModdedText.TextHelpers;

namespace UD_FleshGolems.ModdedText
{
    public static class ModdedTextFilters
    {
        public static string DeleteString => "##DELETE";
        public static string NoSpace => "##NoSpace";

        public static List<string> IsOrDelete => new() { "is", DeleteString, };
        public static List<string> NotsOrNo => new() { "not", "not", "no", };
        public static Dictionary<string, List<string>> SnapifyWordReplacements => new()
        {
            {
                "I", new() { "me" }
            },
            {
                "am", IsOrDelete
            },
            {
                "are", IsOrDelete
            },
            {
                "is", IsOrDelete
            },
            {
                "you", new() { "yoo", "yu", }
            },
            {
                "the", new() { "tha", "da", DeleteString, }
            },
            {
                "a", new() { DeleteString, }
            },
            {
                "an", new() { DeleteString, }
            },
            {
                "to", new() { DeleteString, }
            },
            {
                "ing", new() { "ing", DeleteString, DeleteString, }
            },
            {
                "can't", NotsOrNo
            },
            {
                "won't", NotsOrNo
            },
            {
                "don't", NotsOrNo
            },
            {
                "doesn't", NotsOrNo
            },
            {
                "haven't", NotsOrNo
            },
            {
                "aren't", NotsOrNo
            },
        };
        public static Dictionary<string, List<string>> SnapifyPartialReplacements => new()
        {
            {
                "ar", new() { "arf- ar", "a-{{emote|*snap*}} ar", }
            },
            {
                "ra", new() { "raf-ra", "ra-{{emote|*snap*}} ra", }
            },
            {
                "re", new() { "reh- re", }
            },
            {
                "ru", new() { "ruh- ru", "ru-{{emote|*snap*}} ru", }
            },
            {
                "y", new() { "yipp- y", "yi- yi- y", }
            },
            {
                "th", new() { "d", "y", DeleteString, }
            },
            {
                "ee", new() { "e- yi yi -", "ee- yi! -", "i", }
            },
            {
                "ve", new() { "b", "be", }
            },
            {
                "dr", new() { "d", }
            },
            {
                "oul", new() { "ul", "u", "oo", }
            },
            {
                "ou", new() { "ow", "-awoo! -", }
            },
            {
                "he", new() { "he-yeye-", "", }
            },
            {
                "ph", new() { "ff", "f", }
            },
        };
        public static Dictionary<List<char>, int> SnapifySwaps => new()
        {
            { new() { 'p', 'b', 'd', }, 1 },
            { new() { 'k', 'g', }, 3 },
            { new() { 'y', 'h', }, 3 },
            { new() { 'd', 't', }, 1 },
            { new() { 'n', 'm', }, 1 },
            { new() { 'v', 'b', 'f', }, 1 },
            // { new() { 'w', 'v', }, 4 },
        };
        public static List<string> SnapifyAdditions => new()
        {
            "raff!",
            NoSpace + "- raff!",
            "arf!",
            NoSpace + "- yi! yi!",
            "yi! yi!",
            NoSpace + "- graa!",
            "graa!",
            "{{emote|*snap*}}",
            "{{emote|*snap-snap*}}",
        };

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
            ? Replacement.Capitalize()
            : Replacement.Uncapitalize();

        private static string CreateSentence(string Accumulator, Word Next)
            => VariableReplacers.CreateSentence(Accumulator, Next.ToString());

        public static string Snapify(string Phrase)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Phrase) + "." + nameof(Phrase.Length), Phrase?.Length ?? 0),
                });

            if (Phrase?.Split(' ')?.ToList()?.ConvertAll(s => new Word(s)) is List<Word> words
                && words.Count > 0)
            {
                for (int i = 0; i < words.Count; i++)
                {
                    if (words[i] is Word word)
                    {
                        bool stop = false;
                        if (stop)
                        {
                            continue;
                        }
                        foreach ((string key, List<string> values) in SnapifyWordReplacements)
                        {
                            if (word.LettersOnly() is string wordWithoutPunctuation
                                && wordWithoutPunctuation.EqualsNoCase(key))
                            {
                                if (values.GetRandomElementCosmetic() is string replacement)
                                {
                                    string originalWordWithoutPunctuation = wordWithoutPunctuation;
                                    if (key.EqualsNoCase("I")
                                        && i > 0
                                        && words[i - 1] is Word previousWord
                                        && !previousWord.ImpliesCapitalization())
                                    {
                                        wordWithoutPunctuation = wordWithoutPunctuation.Uncapitalize();
                                    }
                                    words[i] = words[i].Replace(
                                        OldValue: originalWordWithoutPunctuation,
                                        NewValue: replacement.MatchCapitalization(wordWithoutPunctuation));
                                    //stop = true;
                                    break;
                                }
                            }
                        }
                        if (stop)
                        {
                            continue;
                        }
                        foreach ((string key, List<string> values) in SnapifyPartialReplacements)
                        {
                            if (word.LettersOnly() is string wordWithoutPunctuation
                                && values.GetRandomElementCosmetic() is string replacement)
                            {
                                if (wordWithoutPunctuation.StartsWith(key)
                                    && Stat.RollCached("1d2") == 1)
                                {
                                    wordWithoutPunctuation = replacement + wordWithoutPunctuation[key.Length..];
                                    words[i] = words[i].Replace(wordWithoutPunctuation, replacement.MatchCapitalization(wordWithoutPunctuation));
                                    // stop = true;
                                    break;
                                }

                                for (int j = 0; j < wordWithoutPunctuation.Length - 1; j++)
                                {
                                    if (wordWithoutPunctuation[j..].StartsWith(key)
                                        && Stat.RollCached("1d1") == 1)
                                    {
                                        string start = wordWithoutPunctuation[..j];
                                        string end = wordWithoutPunctuation[(j + key.Length)..];
                                        wordWithoutPunctuation = start + replacement + end;
                                        words[i] = words[i].Replace(wordWithoutPunctuation, replacement.MatchCapitalization(wordWithoutPunctuation));
                                        // stop = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (stop)
                        {
                            continue;
                        }
                        foreach ((List<char> swaps, int odds) in SnapifySwaps)
                        {
                            int maxIndex = word.Length - 1;
                            string replacementWord = "";
                            int skipCount = 0;
                            bool didSwap = false;
                            char previousSwap = default;
                            for (int j = 0; j < word.Length; j++)
                            {
                                if (skipCount > 0)
                                {
                                    skipCount = Math.Max(0, --skipCount);
                                    continue;
                                }
                                skipCount = Math.Max(0, --skipCount);
                                if (j != maxIndex)
                                {
                                    if (swaps.Contains(word[j])
                                        && word[j] is char currentChar
                                        && swaps.GetRandomElementCosmeticExcluding(c => c == currentChar) is char swapChar)
                                    {
                                        bool currentMatchesPreviousChar = j > 0 && word[j - 1] == currentChar;
                                        bool currentMatchesNextChar = j < maxIndex && word[j + 1] == currentChar;
                                        bool swapMatchesPreviousChar = j > 0 && word[j - 1] == swapChar;
                                        bool swapMatchesNextChar = j < maxIndex && word[j + 1] == swapChar;
                                        bool currentMatchesPreviousSwap = didSwap && currentChar == previousSwap;

                                        if (currentMatchesPreviousSwap)
                                        {
                                            previousSwap = default;
                                            didSwap = false;
                                            continue;
                                        }
                                        if (Stat.RollCached("1d" + odds) == 1)
                                        {
                                            if (currentMatchesNextChar || swapMatchesNextChar)
                                                skipCount++;

                                            if (didSwap
                                                && (currentMatchesPreviousChar
                                                    || swapMatchesPreviousChar))
                                            {
                                                previousSwap = default;
                                                didSwap = false;
                                                continue;
                                            }

                                            replacementWord += swapChar;
                                            previousSwap = swapChar;
                                            didSwap = true;
                                            continue;
                                        }
                                    }
                                }
                                previousSwap = default;
                                didSwap = false;
                                replacementWord += word[j];
                            }
                            words[i] = words[i].ReplaceWord(replacementWord);
                        }
                        if (stop)
                        {
                            continue;
                        }
                    }
                }
                for (int i = words.Count - 1; i >= 0; i--)
                {
                    if (Stat.RollCached("1d6") == 0
                        && SnapifyAdditions.GetRandomElementCosmetic() is string snapifyAddition)
                    {
                        words.Insert(i, new(snapifyAddition));
                    }
                }
                if (!words.IsNullOrEmpty())
                {
                    Phrase = words
                            ?.Aggregate("", CreateSentence)
                            ?.RemoveAllNoCase(" " + DeleteString, DeleteString + " ", DeleteString)
                        ?? Phrase;

                    while (Phrase.TryGetIndexOf(NoSpace, out int noSpaceStartIndex, false)
                        && noSpaceStartIndex - 1 is int noSpaceStartWithSpaceIndex
                        && noSpaceStartWithSpaceIndex >= 0
                        && noSpaceStartIndex + (NoSpace?.Length ?? 0) is int noSpaceEndIndex
                        && noSpaceEndIndex <= Phrase.Length)
                    {
                        Phrase = Phrase[..noSpaceStartWithSpaceIndex] + Phrase[noSpaceEndIndex..];
                    }
                }
            }
            return Phrase;
        }
        public static StringBuilder Snapify(this StringBuilder SB)
        {
            if (SB == null)
                return null;

            string snapified = Snapify(SB.ToString());
            return SB.Clear().Append(snapified);
        }
    }
}
