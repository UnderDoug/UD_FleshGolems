using System;
using System.Collections.Generic;
using System.Text;

namespace UD_FleshGolems.Logging
{
    public class Indent
    {
        public static int MaxIndent = 12;

        protected int PinnedValue;

        protected int Value;

        protected int Factor;

        protected char Char;

        protected bool _Pinned;

        protected bool Pinned
        {
            get => _Pinned;
            set
            {
                if (value)
                {
                    PinnedValue = Value;
                }
                else
                {
                    Value = PinnedValue;
                    PinnedValue = 0;
                }
                _Pinned = value;
            }
        }

        public Indent()
        {
            PinnedValue = 0;
            Value = 0;
            Factor = 4;
            Char = ' ';
            _Pinned = false;
        }
        public Indent(int Value)
            : this()
        {
            this.Value = Value;
        }
        public Indent(int Value, char Char)
            : this(Value)
        {
            this.Char = Char;
        }
        public Indent(int Value, int Factor)
            : this(Value)
        {
            this.Factor = Factor;
        }
        public Indent(int Value, int Factor, char Char)
            : this(Value, Factor)
        {
            this.Char = Char;
        }
        public Indent(Indent Source)
            : this(Source.Value, Source.Factor, Source.Char)
        {
        }
        public Indent(int offset, Indent Source)
            : this(Source.Value + offset, Source.Factor, Source.Char)
        {
        }

        public Indent this[int Indent]
        {
            get
            {
                Value = CapIndent(Value + Indent);
                return this;
            }
            protected set
            {
                Value = CapIndent(value + Indent);
                Pinned = true;
            }
        }

        protected int CapIndent(int Indent)
            => Math.Min(MaxIndent, Indent);

        protected int CapIndent()
            => Math.Min(MaxIndent, Value);

        public void ResetIndent()
        {
            PinnedValue = 0;
            Pinned = false;
        }

        public void ResetIndent(out Indent Indent)
        {
            PinnedValue = 0;
            Pinned = false;
            Indent = this;
        }
        public void GetIndent(out Indent Indent, bool? Pinned = null)
        {
            if (Pinned is bool pinned)
            {
                this.Pinned = pinned;
            }
            Indent = this;
        }
        public void SetIndent(Indent Indent)
        {
            Pinned = false;
            this[(int)Indent] = 0;
        }
        public void GetIndent(int Offset, out Indent Indents)
        {
            Indents = new(new Indent(Offset, this));
        }
        public void GetIndent(out Indent Indent)
        {
            GetIndent(0, out Indent);
        }

        public override string ToString()
        {
            return Char.ThisManyTimes(CapIndent() * Factor);
        }

        public static implicit operator int(Indent Operand)
        {
            return Operand.Value;
        }
        public static implicit operator Indent(int Operand)
        {
            return new(Operand);
        }
    }
}
