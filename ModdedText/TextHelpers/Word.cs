using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UD_FleshGolems.Logging;
using UD_FleshGolems.ModdedText;

namespace UD_FleshGolems.ModdedText.TextHelpers
{
    public struct Word
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

        private struct TextComponent
        {
            public TextType TextType;
            public string Value;

            public TextComponent(TextType TextType, string Value = null)
            {
                this.TextType = TextType;
                this.Value = Value;
            }

            public TextComponent(TextComponent Source)
                : this(Source.TextType, Source.Value)
            {
            }

            public readonly void Deconstruct(out TextType TextType, out string Value)
            {
                TextType = this.TextType;
                Value = this.Value;
            }

            public static explicit operator KeyValuePair<TextType, string>(TextComponent TextComponent)
                => new(TextComponent.TextType, TextComponent.Value);

            public static explicit operator TextComponent(KeyValuePair<TextType, string>  KVP)
                => new(KVP.Key, KVP.Value);
        }

        public static Word Empty => new(null, null, null, null);

        private readonly bool Open;
        private readonly string Shader;
        public string Text
        {
            readonly get => TextComponents?.Aggregate("", (a, n) => a + n);
            set
            {
                TextComponentsFromText(value);
            }
        }
        private readonly bool Close;

        private List<TextComponent> TextComponents;

        public readonly int Length => Text?.Length ?? 0;

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

        public readonly char this[int Index] => Text[Index];

        public readonly string this[Range Range] => Text[Range];

        private void TextComponentsFromText(string Text)
        {
            TextComponents ??= new();
            TextComponent newComponent = new(TextType.None);
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
                    newComponent.TextType = currentTextType = TextType.ShaderOpen;
                }
                else
                if (prevChar == '{')
                    newComponent.TextType = currentTextType = TextType.ShaderText;

                if (currentChar == '}')
                {
                    if (prevChar == '}'
                        || currentTextType == TextType.ShaderClose)
                    {
                        TextComponents.Add(newComponent);
                        PrepareForLoop(ref currentChar, ref prevChar, ref currentTextType, ClearTextType: true);
                        continue;
                    }
                    newComponent.TextType = currentTextType = TextType.ShaderClose;
                }
                else
                if (prevChar == '}')
                    newComponent.TextType = currentTextType = TextType.Text;

                if (currentChar == '|'
                    && currentTextType == TextType.ShaderText)
                {
                    TextComponents.Add(newComponent);
                    PrepareForLoop(ref currentChar, ref prevChar, ref currentTextType, ClearTextType: true);
                    continue;
                }

                if (prevChar == '|'
                    && currentTextType != TextType.Replacer)
                    newComponent.TextType = currentTextType = TextType.Text;

                if (i >= Text.Length)
                    TextComponents.Add(newComponent);

                PrepareForLoop(ref currentChar, ref prevChar, ref currentTextType);
            }
        }

        public override readonly string ToString()
            => GetOpenString()
            + GetShaderString()
            + GetTextString()
            + GetCloseString();

        private readonly string GetOpenString()
            => Open ? "{{" : null;

        private readonly string GetShaderString()
            => !Shader.IsNullOrEmpty() ? Shader + "|" : null;

        private readonly string GetTextString()
            => IsGuarded() ? Text[1..^1] : Text;
        private readonly string GetCloseString()
            => Open ? "}}" : null;

        public static Word ReplaceWord(Word? Word, string Text)
            => new(Word?.Open, Word?.Shader, Text, Word?.Close);

        public readonly Word ReplaceWord(string Text)
            => ReplaceWord(this, Text);

        public readonly Word CopyWithoutText()
            => ReplaceWord(this, null);

        public readonly Word Capitalize()
            => ReplaceWord(Text.CapitalizeEx());

        public readonly Word Uncapitalize()
            => ReplaceWord(Text.UncapitalizeEx());

        public readonly bool IsCapitalized()
            => Text
                ?.Strip()
                ?.Aggregate(
                    seed: "",
                    func: (a, n) => a + (n.IsLetterAndNotException(Utils.CapitalizationExceptions) ? n : null)) is string strippedWord
            && strippedWord[0].ToString() == strippedWord[0].ToString().ToUpper();

        public readonly string LettersOnly()
        => Text
            ?.Strip()
            ?.Aggregate(
                seed: "",
                func: (a, n) => a + (char.IsLetter(n) ? n : null));

        public readonly bool ImpliesCapitalization(bool ExcludeElipses = false)
            => Text.IsNullOrEmpty()
            || Text.EndsInCapitalizingPunctuation(ExcludeElipses);

        public readonly Word MatchCapitalization(string Word)
            => Word.IsCapitalized()
            ? ReplaceWord(this, Text.CapitalizeEx())
            : ReplaceWord(this, Text.UncapitalizeEx());

        public readonly Word Replace(string OldValue, string NewValue)
            => ReplaceWord(this, Text?.Replace(OldValue, NewValue));

        public readonly Word ReplaceNoCase(string OldValue, string NewValue)
            => ReplaceWord(this, Text?.ReplaceNoCase(OldValue, NewValue));

        public readonly bool Contains(string String)
            => !Text.IsNullOrEmpty()
            && Text.Contains(String);

        public readonly bool StartsWith(string String)
            => !Text.IsNullOrEmpty()
            && Text.StartsWith(String);

        public readonly bool EndsWith(string String)
            => !Text.IsNullOrEmpty()
            && Text.EndsWith(String);

        public readonly bool TextEquals(string String)
            => !Text.IsNullOrEmpty()
            && Text.Equals(String);

        public readonly bool TextEqualsNoCase(string String)
            => !Text.IsNullOrEmpty()
            && Text.EqualsNoCase(String);

        public readonly bool Any(Func<char, bool> predicate)
            => !Text.IsNullOrEmpty()
            && Text.Any(predicate);

        public readonly bool All(Func<char, bool> predicate)
            => !Text.IsNullOrEmpty()
            && Text.All(predicate);

        public readonly IEnumerable<char> EnumerateText()
        {
            if (Text.IsNullOrEmpty())
                yield break;

            foreach (char @char in Text)
                yield return @char;
        }

        public readonly Word Guard()
            => !IsGuarded()
            ? ReplaceWord("%" + Text + "%")
            : this;

        public readonly bool IsGuarded()
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

        public readonly Word Unguard()
            => IsGuarded()
            ? ReplaceWord(Text[1..^1])
            : this;

        public readonly void DebugLog()
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(ToString()),
                });
            foreach ((TextType textType, string value) in TextComponents ?? new())
                Debug.Log(textType.ToString(), value, Indent: indent[1]);
        }
    }
}
