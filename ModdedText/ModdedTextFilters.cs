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
                "ing", StringAndNDeleteString("ing", 2)
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
            { new() { 'v', 'b', }, 1 },
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

        public static List<string> StringAndNDeleteString(string String, int N)
        {
            List<string> output = new();
            if (!String.IsNullOrEmpty())
                output.Add(String);

            for (int i = 0; i < N; i++)
                output.Add(DeleteString);

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

                        Debug.Log(
                            nameof(SnapifyWordReplacements) + "." + nameof(SnapifyWordReplacements.Count),
                            SnapifyWordReplacements?.Count ?? 0,
                            Indent: indent[1]);

                        foreach ((string key, List<string> values) in SnapifyWordReplacements)
                        {
                            if (values.GetRandomElementCosmetic() is string replacement
                                && word.LettersOnly() is string wordWithoutPunctuation
                                && wordWithoutPunctuation.EqualsNoCase(key))
                            {
                                string originalWordWithoutPunctuation = wordWithoutPunctuation;
                                if (key.EqualsNoCase("I")
                                    && i > 0
                                    && words[i - 1] is Word previousWord
                                    && !previousWord.ImpliesCapitalization())
                                {
                                    wordWithoutPunctuation = wordWithoutPunctuation.Uncapitalize();
                                }

                                string safeReplacement = replacement.MatchCapitalization(wordWithoutPunctuation);
                                if (replacement.EqualsAny(DeleteString, NoSpace))
                                    safeReplacement = replacement;

                                words[i] = words[i].Replace(
                                    OldValue: originalWordWithoutPunctuation,
                                    NewValue: safeReplacement);

                                //stop = true;
                                string debugReplaceString = replacement + " (" + safeReplacement + ")";
                                Debug.CheckYeh(word.ToString() + "|" + key + ": " + debugReplaceString, Indent: indent[2]);
                                Debug.Log(nameof(originalWordWithoutPunctuation), originalWordWithoutPunctuation, Indent: indent[3]);
                                Debug.Log(nameof(wordWithoutPunctuation), wordWithoutPunctuation, Indent: indent[3]);
                                break;
                            }
                            else
                            {
                                Debug.CheckNah(word.ToString() + "|" + key, Indent: indent[2]);
                            }
                        }
                        word = words[i];
                        if (stop)
                        {
                            continue;
                        }

                        Debug.Log(
                            nameof(SnapifyPartialReplacements) + "." + nameof(SnapifyPartialReplacements.Count),
                            SnapifyPartialReplacements?.Count ?? 0,
                            Indent: indent[1]);

                        foreach ((string key, List<string> values) in SnapifyPartialReplacements)
                        {
                            if (values.GetRandomElementCosmetic() is string replacement)
                            {
                                Word? newWord = word.ReplaceWord(null);
                                for (int j = 0; j < word.Length; j++)
                                {
                                    if (j == word.Length)
                                        newWord = word.ReplaceWord(newWord?.Text + word[j]);

                                    int k = j + key.Length;
                                    if (k < word.Length
                                        && word[j..k].EqualsNoCase(key)
                                        && Stat.RollCached("1d1") == 1)
                                    {
                                        string safeReplacement = replacement.MatchCapitalization(word[j..k]);
                                        if (replacement.EqualsAny(DeleteString, NoSpace))
                                            safeReplacement = replacement;

                                        newWord = words[i].ReplaceWord(newWord?.Text + safeReplacement);
                                        j += key.Length - 1;
                                    }
                                    else
                                    {
                                        newWord = word.ReplaceWord(newWord?.Text + word[j]);
                                    }
                                }
                                words[i] = newWord ?? words[i];
                            }
                        }
                        word = words[i];
                        if (stop)
                        {
                            continue;
                        }

                        Debug.Log(
                            nameof(SnapifySwaps) + "." + nameof(SnapifySwaps.Count),
                            SnapifySwaps?.Count ?? 0,
                            Indent: indent[1]);

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
                        word = words[i];
                        if (stop)
                        {
                            continue;
                        }
                    }
                }

                Debug.Log(
                    nameof(SnapifyAdditions) + "." + nameof(SnapifyAdditions.Count),
                    SnapifyAdditions?.Count ?? 0,
                    Indent: indent[1]);

                for (int i = words.Count - 1; i >= 0; i--)
                {
                    if (Stat.RollCached("1d6") == 1
                        && SnapifyAdditions.GetRandomElementCosmetic() is string snapifyAddition)
                    {
                        words.Insert(i, new(snapifyAddition));
                        Debug.CheckYeh(nameof(i) + ": " + i + " \"" + snapifyAddition + "\"", Indent: indent[2]);
                    }
                    else
                    {
                        Debug.CheckNah(nameof(i) + ": " + i, Indent: indent[2]);
                    }
                }
                if (!words.IsNullOrEmpty())
                {
                    Phrase = words
                            ?.Aggregate("", CreateSentence)
                            ?.RemoveAllNoCase(" " + DeleteString, DeleteString + " ", DeleteString)
                        ?? Phrase;


                    Debug.Log(nameof(NoSpace), Indent: indent[1]);

                    while (Phrase.TryGetIndexOf(NoSpace, out int noSpaceStartIndex, false)
                        && noSpaceStartIndex - 1 is int noSpaceStartWithSpaceIndex
                        && noSpaceStartWithSpaceIndex >= 0
                        && noSpaceStartIndex + (NoSpace?.Length ?? 0) is int noSpaceEndIndex
                        && noSpaceEndIndex <= Phrase.Length)
                    {
                        Phrase = Phrase[..noSpaceStartWithSpaceIndex] + Phrase[noSpaceEndIndex..];
                        Debug.CheckYeh("removed between " + noSpaceStartWithSpaceIndex + " and " + noSpaceEndIndex, Indent: indent[2]);
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
