using System;
using System.Collections.Generic;
using System.Text;

using XRL;

namespace UD_FleshGolems.Logging
{
    public class Indent
    {
        public static int MaxIndent = 12;

        protected int BaseValue;

        protected int LastValue;

        protected int Factor;

        protected char Char;

        public Indent()
        {
            BaseValue = 0;
            LastValue = 0;
            Factor = 4;
            Char = ' ';
        }
        public Indent(int Value)
            : this()
        {
            BaseValue = Value;
            LastValue = Value;
        }
        public Indent(int Value, char Char) : this(Value) => this.Char = Char;
        public Indent(int Value, int Factor) : this(Value) => this.Factor = Factor;
        public Indent(int Value, int Factor, char Char) : this(Value, Factor) => this.Char = Char;
        public Indent(Indent Source) : this(Source.LastValue, Source.Factor, Source.Char) { }
        public Indent(int offset, Indent Source) : this(Source.LastValue + offset, Source.Factor, Source.Char) { }

        public Indent this[int Indent]
        {
            get
            {
                LastValue = CapIndent(BaseValue + Indent);
                return this;
            }
            protected set
            {
                BaseValue = CapIndent(value + Indent);
                LastValue = BaseValue;
            }
        }

        protected int CapIndent(int Indent)
            => Math.Min(MaxIndent, Indent);

        protected int CapIndent()
            => CapIndent(LastValue);

        public Indent ResetIndent()
            => ResetIndent(out _);

        public Indent ResetIndent(out Indent Indent)
        {
            LastValue = BaseValue;
            return Indent = this;
        }
        public Indent SetIndent(int Offset)
            => this[Offset] = 0;

        public Indent GetBaseValue()
            => BaseValue;

        public override string ToString()
            => Char.ThisManyTimes(CapIndent() * Factor);

        public static implicit operator int(Indent Operand)
            => Operand.LastValue;
        public static implicit operator Indent(int Operand)
            => new(Operand);

        public Indent DiscardIndent()
            => Debug.DiscardIndent();

    }
}
