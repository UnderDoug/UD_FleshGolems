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
        public static string DeleteString => "##DELETE";
        public static string NoSpaceBefore => "##NoSpace";
        public static string NoSpaceAfter => "NoSpace##";

        public static Dictionary<string, int> IsOrDelete => new()
        {
            { "is", 1 },
            { DeleteString, 1 }
        };
        public static Dictionary<string, int> NotOrNo => new()
        {
            { "not", 2 },
            { "no", 1 },
        };
        public static List<RandomStringReplacement> SnapifyWordReplacements => new()
        {
            new(
                Key: "I",
                ChanceOneIn: 1, 
                WeightedEntries: new()
                { 
                    { "me", 1 },
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
                    { "tha", 1 },
                    { "da", 1 },
                    { DeleteString, 2 },
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
                Key: "to",
                ChanceOneIn: 1,
                WeightedEntries: new()
                {
                    { DeleteString, 1 },
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
                Key: "ar",
                ChanceOneIn: 2,
                WeightedEntries: new()
                {
                    { "arf- ar", 2 },
                    { "a-{{emote|*snap*}} -ar", 1 },
                }),
            new(
                Key: "ra",
                ChanceOneIn: 2,
                WeightedEntries: new()
                {
                    { "raf-ra", 2 },
                    { "ra-{{emote|*snap*}} -ra", 1 },
                }),
            new(
                Key: "re",
                ChanceOneIn: 2,
                WeightedEntries: new()
                {
                    { "reh- re", 1 },
                }),
            new(
                Key: "ru",
                ChanceOneIn: 2,
                WeightedEntries: new()
                {
                    { "ruh- ru", 2 },
                    { "ru-{{emote|*snap*}} -ru", 1 },
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
                    { "ed", 3 },
                    { "eded", 1 },
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
        };
        public static Dictionary<List<char>, int> SnapifySwaps => new()
        {
            { new() { 'p', 'b', 'd', }, 1 },
            { new() { 'k', 'g', }, 3 },
            // { new() { 'y', 'h', }, 3 },
            { new() { 'd', 't', }, 1 },
            { new() { 'n', 'm', }, 1 },
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
            { NoSpaceBefore + "...er...", 3 },
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

        public static Word PerformSnapifyWordReplacemnts(ref Word Word, Word? PrevWord, out bool Stop)
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
                    {
                        wordWithoutPunctuation = wordWithoutPunctuation.Uncapitalize();
                    }

                    string safeReplacement = replacement.MatchCapitalization(wordWithoutPunctuation);
                    if (replacement.EqualsAny(DeleteString, NoSpaceBefore))
                        safeReplacement = replacement;

                    Word = Word.Replace(
                        OldValue: originalWordWithoutPunctuation,
                        NewValue: safeReplacement);

                    //stop = true;
                    string debugReplaceString = replacement + " (" + safeReplacement + ")";
                    Debug.CheckYeh(Word.ToString() + "|" + key + ": " + debugReplaceString, Indent: indent[1]);
                    Debug.Log(nameof(originalWordWithoutPunctuation), originalWordWithoutPunctuation, Indent: indent[2]);
                    Debug.Log(nameof(wordWithoutPunctuation), wordWithoutPunctuation, Indent: indent[2]);
                    break;
                }
                else
                {
                    Debug.CheckNah(Word.ToString() + "|" + key, Indent: indent[1]);
                }
            }
            return Word;
        }

        public static Word PerformSnapifyPartialReplacements(ref Word Word, out bool Stop)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Word), Word.Text ?? "no text"),
                    Debug.Arg(
                        Name: nameof(SnapifyPartialReplacements) + "." + nameof(SnapifyPartialReplacements.Count),
                        Value: SnapifyPartialReplacements?.Count ?? 0),
                });

            Stop = false;

            foreach ((string key, int chanceOneIn, Dictionary<string, int> replacements) in SnapifyPartialReplacements)
            {
                Word newWord = Word.CopyWithoutText();
                for (int j = 0; j < Word.Length; j++)
                {
                    if (j == Word.Length)
                        newWord = Word.ReplaceWord(newWord.Text + Word[j]);

                    int k = j + key.Length;
                    if (1.ChanceIn(chanceOneIn)
                        && k < Word.Length
                        && Word[j..k].EqualsNoCase(key)
                        && Stat.RollCached("1d2") == 1
                        && replacements.GetWeightedRandom() is string replacement)
                    {
                        string safeReplacement = replacement.MatchCapitalization(Word[j..k]);

                        if (replacement.EqualsAny(DeleteString, NoSpaceBefore))
                            safeReplacement = replacement;

                        newWord = Word.ReplaceWord(newWord.Text + safeReplacement);
                        j += key.Length - 1;
                    }
                    else
                    {
                        newWord = Word.ReplaceWord(newWord.Text + Word[j]);
                    }
                }
                Debug.Log((Word.Text ?? "no text") + " -> " + (newWord.Text ?? "no text"), Indent: indent[1]);
                Word = newWord;
            }
            return Word;
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

            foreach ((List<char> swaps, int odds) in SnapifySwaps)
            {
                int maxIndex = Word.Length - 1;
                string replacementWord = "";
                bool didSwap = false;
                char previousSwap = default;
                for (int j = 0; j < Word.Length; j++)
                {
                    if (j != maxIndex)
                    {
                        if (swaps.Contains(Word[j])
                            && Word[j] is char currentChar
                            && swaps.GetRandomElementCosmeticExcluding(c => c == currentChar) is char swapChar)
                        {
                            bool currentMatchesPreviousChar = j > 0 && Word[j - 1] == currentChar;
                            bool currentMatchesNextChar = j < maxIndex && Word[j + 1] == currentChar;
                            bool swapMatchesPreviousChar = j > 0 && Word[j - 1] == swapChar;
                            bool swapMatchesNextChar = j < maxIndex && Word[j + 1] == swapChar;
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
                                    j++;

                                if (didSwap
                                    && (currentMatchesPreviousChar
                                        || swapMatchesPreviousChar))
                                {
                                    previousSwap = default;
                                    didSwap = false;
                                    continue;
                                }
                                Debug.CheckYeh(nameof(swapChar), currentChar + " -> " + swapChar, indent[1]);
                                replacementWord += swapChar;
                                previousSwap = swapChar;
                                didSwap = true;
                                continue;
                            }
                        }
                    }
                    previousSwap = default;
                    didSwap = false;
                    replacementWord += Word[j];
                }
                Word = Word.ReplaceWord(replacementWord);
            }
            return Word;
        }

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

            for (i = wordsCount - 1; i >= 0; i--)
            {
                if (Stat.RollCached("1d15") == 1)
                {
                    Words.Insert(i, Words[i]);
                    Debug.CheckYeh(nameof(i) + ": " + i + " \"" + Words[i] + "\"", Indent: indent[1]);
                }
                else
                {
                    Debug.CheckNah(nameof(i) + ": " + i, Indent: indent[1]);
                }
            }
            return Words;
        }

        public static string GetSnapifyAddition(Dictionary<string, int> SnapifyAdditions, Func<KeyValuePair<string, int>, bool> Filter)
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
                if (Stat.RollCached("1d8") == 1
                    && GetSnapifyAddition(SnapifyAdditions, EligibleAddition) is string snapifyAddition)
                {
                    if (Stat.RollCached("1d12") == 1
                        && GetSnapifyAddition(SnapifyAdditions, EligibleAddition) is string snapifyAdditionalAddition)
                    {
                        Words.Insert(i, new(snapifyAdditionalAddition));
                        Debug.CheckYeh(nameof(i) + ": " + i + " \"" + snapifyAdditionalAddition + "\"", Indent: indent[1]);
                    }
                    Words.Insert(i, new(snapifyAddition));
                    Debug.CheckYeh(nameof(i) + ": " + i + " \"" + snapifyAddition + "\"", Indent: indent[1]);
                }
                else
                {
                    Debug.CheckNah(nameof(i) + ": " + i, Indent: indent[1]);
                }
            }
            return Words;
        }

        public static bool TrimNoSpaceString(ref string Phrase)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Phrase), Phrase?.Length ?? 0),
                    Debug.Arg(nameof(NoSpaceBefore), NoSpaceBefore),
                });

            if (!Phrase.Contains(NoSpaceBefore)
                && !Phrase.Contains(NoSpaceAfter))
                return false;

            if (Phrase.Contains(NoSpaceBefore))
            {
                string[] noSpaceBeforeSplit = Phrase.Split(NoSpaceBefore);
                int indices = noSpaceBeforeSplit[0]?.Length ?? 0;

                for (int i = 1; i < noSpaceBeforeSplit.Length - 1; i++)
                {
                    int currentLength = noSpaceBeforeSplit[i]?.Length ?? 0;
                    indices += currentLength;
                    noSpaceBeforeSplit[i] = currentLength > 1
                        ? noSpaceBeforeSplit[i][..^1]
                        : noSpaceBeforeSplit[i];

                    if (currentLength == 1)
                        noSpaceBeforeSplit[i] = "";

                    Debug.CheckYeh("removed between " + indices + " and " + (indices + NoSpaceBefore.Length), Indent: indent[2]);
                }
                Phrase = noSpaceBeforeSplit.Aggregate("", (a, n) => a + n);
            }
            
            if (Phrase.Contains(NoSpaceAfter))
            {
                string[] noSpaceAfterSplit = Phrase.Split(NoSpaceAfter);
                int indices = noSpaceAfterSplit[0]?.Length ?? 0;

                for (int i = 1; i < noSpaceAfterSplit.Length - 1; i++)
                {
                    int currentLength = noSpaceAfterSplit[i]?.Length ?? 0;
                    indices += currentLength;
                    noSpaceAfterSplit[i] = currentLength > 1
                        ? noSpaceAfterSplit[i][1..]
                        : noSpaceAfterSplit[i];

                    if (currentLength == 1)
                        noSpaceAfterSplit[i] = "";

                    Debug.CheckYeh("removed between " + indices + " and " + (indices + NoSpaceAfter.Length), Indent: indent[2]);
                }
                Phrase = noSpaceAfterSplit.Aggregate("", (a, n) => a + n);
            }

            return true;
        }

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
                            continue;

                        words[i] = PerformSnapifyWordReplacemnts(ref word, i > 0 ? words[i -1] : null, out stop);

                        if (stop)
                            continue;

                        words[i] = PerformSnapifyPartialReplacements(ref word, out stop);

                        if (stop)
                            continue;

                        words[i] = PerformSnapifySwaps(ref word, out stop);

                        if (stop)
                            continue;
                    }
                }

                if (!PerformSnapifyAdditions(ref words).IsNullOrEmpty()
                    && !PerformSnapifyReduplications(ref words).IsNullOrEmpty())
                {
                    Phrase = words
                            ?.Aggregate("", CreateSentence)
                            ?.RemoveAllNoCase(" " + DeleteString, DeleteString + " ", DeleteString)
                        ?? Phrase;

                    if (TrimNoSpaceString(ref Phrase))
                    {
                        Debug.CheckYeh("", Indent: indent[2]);
                    }
                }
            }
            return Phrase;
        }
        public static StringBuilder Snapify(this StringBuilder SB)
            => (SB != null
                && Snapify(SB.ToString()) is string snapified)
            ? SB.Clear().Append(snapified)
            : null;
    }
}
