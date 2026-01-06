using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UD_FleshGolems.Logging;
using UD_FleshGolems;
using UD_FleshGolems.ModdedText;

namespace UD_FleshGolems.ModdedText.TextHelpers
{
    public class Word
    {
        public static bool UseWordComponents => false;

        private enum TextType : int
        {
            None,
            ShaderOpen,
            ShaderText,
            Text,
            Punctuation,
            ShaderClose,
            Replacer,
        }

        private class TextComponentData
        {
            public static TextComponentData Empty => new(TextType.None, 0, 0, 0);

            public TextType Type;

            public Index Pos;
            public Index Start;
            public Index End;

            public Range? Range
            {
                get => new(Start, End);
                set
                {
                    if (value != null)
                    {
                        Start = (value?.Start).GetValueOrDefault();
                        End = (value?.End).GetValueOrDefault();
                    }
                    else
                    {
                        Start = default;
                        End = default;
                    }
                }
            }

            public TextComponentData()
            {
                Type = TextType.None;
                Pos = default;
                Start = default;
                End = default;
            }
            public TextComponentData(TextType Type, Index Pos, Index Start, Index? End)
                : this()
            {
                this.Type = Type;
                this.Pos = Pos;
                this.Start = Start;
                if (End != null)
                    this.End = End.GetValueOrDefault();
            }
            public TextComponentData(TextType Type, int Pos, int Start, int End)
                : this(Type, new Index(Pos), new Index(Start), new Index(End))
            { }
            public TextComponentData(TextType Type, Index Pos, Range? Range)
                : this(Type, Pos, (Range?.Start).GetValueOrDefault(), (Range?.End).GetValueOrDefault())
            { }
            public TextComponentData(TextType Type, int Pos, Range? Range)
                : this(Type, new Index(Pos), Range)
            { }
            public TextComponentData(TextComponentData Source)
                : this(Source.Type, Source.Pos, Source.Range)
            { }

            public override string ToString()
                => Type + "|[" + Pos + "][" + Range.ToString() + "]";
        }

        private class TextComponent
        {
            public TextComponentData Data;
            public string Value;

            public TextType Type
            {
                get => Data != null ? Data.Type : TextType.None;
                set
                {
                    if (Data != null)
                        Data.Type = value;
                }
            }

            public TextComponent()
            {
                Value = null;
                Data = null;
            }
            public TextComponent(TextComponentData Data, string Value = null)
                : this()
            {
                this.Data = Data;
                this.Value = Value;
            }
            public TextComponent(TextComponent Source)
                : this(Source.Data, Source.Value)
            { }

            public void Deconstruct(out TextComponentData Data, out string Value)
            {
                Data = this.Data;
                Value = this.Value;
            }

            public override string ToString()
                => ToString(false);

            public string ToString(bool IncludeData)
                => !IncludeData
                ? Value
                : (Data ?? TextComponentData.Empty).ToString() + " " + Value;

            public TextComponent UpdateDataRangeEnd()
            {
                if (Data != null)
                    Data.End = new(Data.Start.Value + Value?.Length ?? 0);
                return this;
            }

            public static explicit operator KeyValuePair<TextComponentData, string>(TextComponent TextComponent)
                => new(TextComponent.Data, TextComponent.Value);

            public static explicit operator TextComponent(KeyValuePair<TextComponentData, string> KVP)
                => new(KVP.Key, KVP.Value);
        }

        public static Word Empty => new(null, null, null, null);

        private readonly bool Open;
        private readonly string Shader;

        private string _Text;
        public string Text
        {
            get => !UseWordComponents ? _Text : TextComponents?.Aggregate("", (a, n) => a + n.Value);
            set
            {
                _Text = value;
                if (value != null)
                    TextComponentsFromText(value);
                else
                    TextComponents = null;
            }
        }
        private readonly bool Close;

        private List<TextComponent> TextComponents;

        private string StrippedText
            => TextComponents
                ?.Aggregate(
                    seed: (string)null,
                    func: (a, n) 
                        => n.Type == TextType.Text 
                        ? a + n.Value
                        : a);

        public int Length => Text?.Length ?? 0;

        private Word(bool? Open, string Shader, string Text, bool? Close)
        {
            this.Open = Open.GetValueOrDefault();
            this.Shader = Shader;
            this.Close = Close.GetValueOrDefault();
            TextComponents = null;
            this.Text = Text;
        }
        public Word(string Text)
            : this(
                  Open: Text?.StartsWith("{{"),
                  Shader: null,
                  Text: null,
                  Close: Text?.EndsWith("}}"))
        {
            string text = Text;
            if (text != null)
            {
                if (text.StartsWith("{{"))
                {
                    text = text[2..];
                }
                if (text.EndsWith("}}"))
                {
                    text = text[..^2];
                }
                if (!text.IsNullOrEmpty())
                {
                    if (text.TryGetIndexOf("|", out int pipeBefore, EndOfSearch: false)
                        && pipeBefore + 1 is int pipeAfter)
                    {
                        if (!text.TryGetIndexOf("=", out int replacerStart, EndOfSearch: false)
                            || replacerStart > pipeBefore)
                        {
                            Shader = text[..pipeBefore];
                            this.Text = text[pipeAfter..];
                        }
                    }
                    else
                    if (Open)
                    {
                        Shader = text;
                    }
                    else
                    {
                        this.Text = text;
                    }
                }
            }
        }

        public char this[int Index] => Text[Index];

        public string this[Range Range] => Text[Range];

        private static void ProcessNewComponent(
            ref List<TextComponent> TextComponents,
            ref TextComponent NewComponent,
            ref TextType CurrentTextType)
        {
            TextComponent newComponent = new(NewComponent);
            newComponent?.UpdateDataRangeEnd();

            TextComponents ??= new();
            TextComponents.Add(newComponent);

            CurrentTextType = TextType.None;

            NewComponent = new TextComponent(
                Data: new TextComponentData(
                    Type: CurrentTextType,
                    Pos: TextComponents.Count,
                    Start: newComponent.Data.End.Value + 1,
                    End: null),
                Value: null);

            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(newComponent), newComponent.ToString(true)),
                });
        }
        private static void PrepareForLoop(
            ref char? PrevChar,
            ref char? CurrentChar,
            ref char? NextChar)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(PrevChar), PrevChar?.ToString() ?? "default"),
                    Debug.Arg(nameof(CurrentChar), CurrentChar?.ToString() ?? "default"),
                    Debug.Arg(nameof(NextChar), NextChar?.ToString() ?? "default"),
                });

            char? currentChar = CurrentChar;

            CurrentChar = null;
            PrevChar = currentChar;
            NextChar = null;
        }
        private void TextComponentsFromText(string Text)
        {
            if (!UseWordComponents)
                return;

            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Text), Text ?? "null"),
                });

            if (Text == null)
                return;

            TextComponents ??= new();
            TextComponent newComponent = new(new TextComponentData(TextType.None, 0, 0, null));

            TextType currentTextType = TextType.None;
            char? prevChar = default;
            char? currentChar = default;
            char? nextChar = default;

            for (int i = 0; i < Text.Length; i++)
            {
                currentChar ??= Text[i];
                nextChar ??= i < (Text.Length - 1) ? Text[i + 1] : null;

                Debug.LogArgs(
                    MessageBefore: i + " | ",
                    MessageAfter: null,
                    Indent: indent[1],
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(prevChar), prevChar?.ToString() ?? "default"),
                        Debug.Arg(nameof(currentChar), currentChar?.ToString() ?? "default"),
                        Debug.Arg(nameof(nextChar), nextChar?.ToString() ?? "default"),
                        Debug.Arg(nameof(TextType) + "." + currentTextType.ToString()),
                    });

                if (currentTextType == TextType.Text)
                {
                    if (currentChar?.ToString() + nextChar?.ToString() is string upcomingSubstring
                        && upcomingSubstring.EqualsAny("{{", "}}"))
                        ProcessNewComponent(ref TextComponents, ref newComponent, ref currentTextType);

                    if (currentChar != null
                        && !currentChar.GetValueOrDefault().IsLetterAndNotException(Utils.CapitalizationExceptions))
                        ProcessNewComponent(ref TextComponents, ref newComponent, ref currentTextType);
                }

                newComponent.Value += currentChar;

                if (currentChar == '='
                    && currentTextType == TextType.Replacer)
                {
                    ProcessNewComponent(ref TextComponents, ref newComponent, ref currentTextType);
                    PrepareForLoop(ref prevChar, ref currentChar, ref nextChar);
                    continue;
                }
                else
                if (currentChar == '=')
                    currentTextType = TextType.Replacer;
                else
                if (currentTextType == TextType.Replacer)
                    continue;

                if (currentChar == '{')
                {
                    if (prevChar == '{'
                        || currentTextType == TextType.ShaderOpen)
                    {
                        ProcessNewComponent(ref TextComponents, ref newComponent, ref currentTextType);
                        PrepareForLoop(ref prevChar, ref currentChar, ref nextChar);
                        continue;
                    }
                    newComponent.Type = currentTextType = TextType.ShaderOpen;
                }
                else
                if (prevChar == '{')
                    newComponent.Type = currentTextType = TextType.ShaderText;

                if (currentChar == '}')
                {
                    if (prevChar == '}'
                        || currentTextType == TextType.ShaderClose)
                    {
                        ProcessNewComponent(ref TextComponents, ref newComponent, ref currentTextType);
                        PrepareForLoop(ref prevChar, ref currentChar, ref nextChar);
                        continue;
                    }
                    newComponent.Type = currentTextType = TextType.ShaderClose;
                }
                else
                if (prevChar == '}')
                    newComponent.Type = currentTextType = TextType.Text;

                if (currentChar == '|'
                    && currentTextType == TextType.ShaderText)
                {
                    ProcessNewComponent(ref TextComponents, ref newComponent, ref currentTextType);
                    PrepareForLoop(ref prevChar, ref currentChar, ref nextChar);
                    continue;
                }

                if (prevChar == '|'
                    && currentTextType != TextType.Replacer)
                    newComponent.Type = currentTextType = TextType.Text;

                if (i >= Text.Length)
                    ProcessNewComponent(ref TextComponents, ref newComponent, ref currentTextType);

                if (currentTextType == TextType.None)
                {
                    if (currentChar != null
                        && !currentChar.GetValueOrDefault().IsLetterAndNotException())
                    {
                        currentTextType = newComponent.Type = TextType.Punctuation;
                        ProcessNewComponent(ref TextComponents, ref newComponent, ref currentTextType);
                        PrepareForLoop(ref prevChar, ref currentChar, ref nextChar);
                        continue;
                    }
                    else
                        currentTextType = newComponent.Type = TextType.Text;
                }

                PrepareForLoop(ref prevChar, ref currentChar, ref nextChar);
            }
        }

        public void SyncComponents()
        {
            // write code to propagate changes to TextComponents list's Data pos/start/end.
        }

        public override string ToString()
            => GetOpenString()
            + GetShaderString()
            + GetTextString()
            + GetCloseString();

        private string GetOpenString()
            => Open ? "{{" : null;

        private string GetShaderString()
            => !Shader.IsNullOrEmpty() ? Shader + "|" : null;

        private string GetTextString(bool DebugSilent = true)
            => IsGuarded(DebugSilent: DebugSilent) ? Text[1..^1] : Text;

        private string GetCloseString()
            => Close ? "}}" : null;

        public static Word ReplaceWord(Word Word, string Text)
            => new(Word?.Open, Word?.Shader, Text, Word?.Close);

        public Word ReplaceWord(string Text)
            => ReplaceWord(this, Text);

        public Word CopyWithoutText()
            => ReplaceWord(this, null);

        public Word Capitalize()
            => ReplaceWord(Text.CapitalizeEx());

        public Word Uncapitalize()
            => ReplaceWord(Text.UncapitalizeEx());

        public bool IsCapitalized()
            => Text
                ?.Strip()
                ?.Aggregate(
                    seed: "",
                    func: (a, n) => a + (n.IsLetterAndNotException(Utils.CapitalizationExceptions) ? n : null)) is string strippedWord
            && strippedWord[0].ToString() == strippedWord[0].ToString().ToUpper();

        private static bool IsLetterOrWhitelisted(char Char, params char[] Whitelist)
            => char.IsLetter(Char)
            || (!Whitelist.IsNullOrEmpty()
                && Char.EqualsAny(Whitelist));

        public string LettersOnly(params char[] Whitelist)
        => Text
            ?.Strip()
            ?.Aggregate(
                seed: "",
                func: (a, n) => a + (IsLetterOrWhitelisted(n, Whitelist) ? n : null));

        public bool ImpliesCapitalization(bool ExcludeElipses = false)
            => Text.IsNullOrEmpty()
            || Text.EndsInCapitalizingPunctuation(ExcludeElipses);

        public Word MatchCapitalization(string Word)
            => Word.IsCapitalized()
            ? ReplaceWord(this, Text.CapitalizeEx())
            : ReplaceWord(this, Text.UncapitalizeEx());

        public Word Replace(string OldValue, string NewValue)
            => ReplaceWord(this, Text?.Replace(OldValue, NewValue));

        public Word ReplaceNoCase(string OldValue, string NewValue)
            => ReplaceWord(this, Text?.ReplaceNoCase(OldValue, NewValue));

        public bool Contains(string String)
            => !Text.IsNullOrEmpty()
            && Text.Contains(String);

        public bool StartsWith(string String)
            => !Text.IsNullOrEmpty()
            && Text.StartsWith(String);

        public bool EndsWith(string String)
            => !Text.IsNullOrEmpty()
            && Text.EndsWith(String);

        public bool TextEquals(string String)
            => !Text.IsNullOrEmpty()
            && Text.Equals(String);

        public bool TextEqualsNoCase(string String)
            => !Text.IsNullOrEmpty()
            && Text.EqualsNoCase(String);

        public bool Any(Func<char, bool> predicate)
            => !Text.IsNullOrEmpty()
            && Text.Any(predicate);

        public bool All(Func<char, bool> predicate)
            => !Text.IsNullOrEmpty()
            && Text.All(predicate);

        public IEnumerable<char> EnumerateText()
        {
            if (Text.IsNullOrEmpty())
                yield break;

            foreach (char @char in Text)
                yield return @char;
        }

        public IEnumerable<string> SubstringsOfLength(int Length)
            => Text?.SubstringsOfLength(Length);

        public bool ContainsNoCase(string Value)
            => !Text.IsNullOrEmpty()
            && Text.ContainsNoCase(Value);

        public Word Guard()
            => !IsGuarded(DebugSilent: true)
            ? ReplaceWord("%" + Text + "%")
            : this;

        public bool IsGuarded(bool DebugSilent = false)
        {
            bool result = Text != null
                && Text.Length > 1
                && '%'.EqualsAll(Text[0], Text[^1]);

            using Indent indent = new(1);

            if (!DebugSilent)
                Debug.LogArgs(
                    MessageBefore: result.YehNah() + " " + Debug.GetCallingMethod(true) + "(",
                    MessageAfter: ")",
                    Indent: indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(Text),
                    });

            return result;
        }

        public Word Unguard()
            => IsGuarded(DebugSilent: true)
            ? ReplaceWord(Text[1..^1])
            : this;

        public void DebugLog()
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(ToString()),
                });
            foreach ((TextComponentData data, string value) in TextComponents ?? new())
                Debug.Log((data?.Type ?? TextType.None).ToString(), value, Indent: indent[1]);
        }
    }
}
