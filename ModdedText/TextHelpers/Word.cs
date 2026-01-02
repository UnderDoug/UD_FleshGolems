using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UD_FleshGolems.Logging;
using UD_FleshGolems.ModdedText;

namespace UD_FleshGolems.ModdedText.TextHelpers
{
    public class Word
    {
        private enum TextType : int
        {
            None,
            ShaderOpen,
            ShaderText,
            Text,
            ShaderClose,
            Replacer,
        }

        private class TextComponentData
        {
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

            public static explicit operator KeyValuePair<TextComponentData, string>(TextComponent TextComponent)
                => new(TextComponent.Data, TextComponent.Value);

            public static explicit operator TextComponent(KeyValuePair<TextComponentData, string> KVP)
                => new(KVP.Key, KVP.Value);
        }

        public static Word Empty => new(null, null, null, null);

        private readonly bool Open;
        private readonly string Shader;
        public string Text
        {
            get => TextComponents?.Aggregate("", (a, n) => a + n);
            set
            {
                TextComponentsFromText(value);
            }
        }
        private readonly bool Close;

        private List<TextComponent> TextComponents;

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
            this.Text = Text;
            if (this.Text != null)
            {
                if (this.Text.StartsWith("{{"))
                {
                    this.Text = this.Text[2..];
                }
                if (this.Text.EndsWith("}}"))
                {
                    this.Text = this.Text[..^2];
                }
                if (!this.Text.IsNullOrEmpty())
                {
                    if (this.Text.TryGetIndexOf("|", out int pipeBefore, EndOfSearch: false)
                        && pipeBefore + 1 is int pipeAfter)
                    {
                        if (!this.Text.TryGetIndexOf("=", out int replacerStart, EndOfSearch: false)
                            || replacerStart > pipeBefore)
                        {
                            Shader = this.Text[..pipeBefore];
                            this.Text = this.Text[pipeAfter..];
                        }
                    }
                    else
                    if (Open)
                    {
                        Shader = this.Text;
                        this.Text = null;
                    }
                    else
                    {
                    }
                }
            }
        }

        public char this[int Index] => Text[Index];

        public string this[Range Range] => Text[Range];

        private void TextComponentsFromText(string Text)
        {
            TextComponents ??= new();
            TextComponent newComponent = new(new TextComponentData(TextType.None, 0, 0, null));
            TextType currentTextType = TextType.None;
            char? currentChar = default;
            char? prevChar = default;
            static void PrepareForLoop(ref char? CurrentChar, ref char? PrevChar, ref TextType CurrentTextType, bool ClearTextType = false)
            {
                if (ClearTextType)
                    CurrentTextType = TextType.None;

                PrevChar = CurrentChar.Value;
                CurrentChar = null;
            }
            for (int i = 0; i < Text.Length; i++)
            {
                currentChar ??= Text[i];
                newComponent.Value += currentChar;

                if (currentChar == '='
                    && currentTextType == TextType.Replacer)
                {
                    TextComponents.Add(newComponent);
                    PrepareForLoop(ref currentChar, ref prevChar, ref currentTextType, ClearTextType: true);
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
                        TextComponents.Add(newComponent);
                        PrepareForLoop(ref currentChar, ref prevChar, ref currentTextType, ClearTextType: true);
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
                        TextComponents.Add(newComponent);
                        PrepareForLoop(ref currentChar, ref prevChar, ref currentTextType, ClearTextType: true);
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
                    TextComponents.Add(newComponent);
                    PrepareForLoop(ref currentChar, ref prevChar, ref currentTextType, ClearTextType: true);
                    continue;
                }

                if (prevChar == '|'
                    && currentTextType != TextType.Replacer)
                    newComponent.Type = currentTextType = TextType.Text;

                if (i >= Text.Length)
                    TextComponents.Add(newComponent);

                PrepareForLoop(ref currentChar, ref prevChar, ref currentTextType);
            }
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

        private string GetTextString()
            => IsGuarded() ? Text[1..^1] : Text;
        private string GetCloseString()
            => Open ? "}}" : null;

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

        public string LettersOnly()
        => Text
            ?.Strip()
            ?.Aggregate(
                seed: "",
                func: (a, n) => a + (char.IsLetter(n) ? n : null));

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

        public Word Guard()
            => !IsGuarded()
            ? ReplaceWord("%" + Text + "%")
            : this;

        public bool IsGuarded()
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(Text),
                });

            if (Text == null
                || Text.Length < 2)
                return false;

            int i;
            for (i = 0; i < Text.Length; i++)
                if (Text[i] != '%')
                    break;

            if (i % 2 == 0)
                return false;

            for (i = Text.Length - 1; i >= 0; i--)
                if (Text[i] != '%')
                    return (Text.Length - 1) - i % 2 == 1;

            return false;
        }

        public Word Unguard()
            => IsGuarded()
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
