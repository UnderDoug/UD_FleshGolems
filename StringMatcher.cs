/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using UD_FleshGolems.Logging;

namespace UD_FleshGolems
{
    public class StringMatcher
    {
        private readonly List<char> chars;
        private Dictionary<string, char> LastMatches;
        private string LastError;
        public int Pos { get; private set; } = -1;

        public int Length => chars.Count;

        public bool IsValid => Pos >= 0 && Pos < Length;

        public bool IsInvalid => !IsValid;

        public int Remaining => Length - Math.Max(0, Pos);

        public char current => chars[Pos];

        private void FixStart()
        {
            Pos = Math.Max(0, Pos);
        }

        private void SetOutOfBounds(int direction)
        {
            Pos = ((direction > 0) ? Length : (-1));
        }

        public StringMatcher()
        {
            chars = new();
            LastMatches = new();
            LastError = null;
        }

        public StringMatcher(IEnumerable<char> String)
            : this()
            => chars = String.ToList();

        public StringMatcher(StringBuilder StringBuilder)
            : this(StringBuilder.ToString()) { }

        public char CharAt(int offset)
            => chars[Pos + offset];

        public List<char> Chars()
            => chars;

        public List<char> Chars(int Count)
            => chars.GetRange(Pos, Count);

        public List<char> CharsInRange(int Start, int End)
            => chars.GetRange(Math.Min(Start, End), Math.Max(Start, End) - Math.Min(Start, End) + 1);

        public List<char> CharsWithOffsets(int StartOffset, int EndOffset)
            => CharsInRange(Pos + StartOffset, Pos + EndOffset);

        public string String()
            => chars.AsString();

        public string String(int Count)
            => CharsInRange(Pos, Count).AsString();

        public string StringInRange(int Start, int End)
            => CharsInRange(Start, End).AsString();

        public string StringWithOffsets(int StartOffset, int EndOffset)
            => CharsWithOffsets(StartOffset, EndOffset).AsString();

        public IEnumerable<char> CharEnumeration()
            => chars.AsEnumerable();

        public bool ReportFailure(MethodBase Method, Action<string> Logger)
        {
            if (IsValid)
            {
                return false;
            }
            string arg = LastError ?? "Unexpected char";
            Logger(arg + " in " + Method);
            return true;
        }
        public bool ReportFailure(MethodBase Method)
            => ReportFailure(
                Method: Method,
                Logger: delegate (string Message)
                {
                    using Indent indent = new(1);
                    Debug.Log(Message, Indent: indent);
                });

    }
}
*/