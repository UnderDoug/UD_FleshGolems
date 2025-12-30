using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UD_FleshGolems.ModdedText;

namespace UD_FleshGolems.ModdedText.TextHelpers
{
    public readonly struct Word
    {
        public static Word Empty => new(null, null, null, null);

        private readonly bool Open;
        private readonly string Shader;
        public readonly string Text;
        private readonly bool Close;

        public int Length => Text?.Length ?? 0;

        private Word(bool? Open, string Shader, string Text, bool? Close)
        {
            this.Open = Open.GetValueOrDefault();
            this.Shader = Shader;
            this.Text = Text;
            this.Close = Close.GetValueOrDefault();
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

        public override readonly string ToString()
            => (Open ? "{{" : null)
            + (!Shader.IsNullOrEmpty() ? Shader + "|" : null)
            + Text
            + (Close ? "}}" : null);

        public static Word ReplaceWord(Word? Word, string Text)
            => new(Word?.Open, Word?.Shader, Text, Word?.Close);

        public readonly Word ReplaceWord(string Text)
            => ReplaceWord(this, Text);

        public readonly Word Capitalize()
            => ReplaceWord(Text.CapitalizeEx());

        public readonly Word Uncapitalize()
            => ReplaceWord(Text.UncapitalizeEx());

        public readonly bool IsCapitalized()
            => Text
                ?.Strip()
                ?.Aggregate(
                    seed: "",
                    func: (a, n) => a + (n.IsLetterAndNotException() ? n : null)) is string strippedWord
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
            ? ReplaceWord(this, Text.CapitalizeExcept())
            : ReplaceWord(this, Text.Uncapitalize());

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

        public IEnumerable<char> EnumerateText()
        {
            if (Text.IsNullOrEmpty())
                yield break;

            foreach (char @char in Text)
                yield return @char;
        }
    }
}
