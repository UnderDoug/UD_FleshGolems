using System;
using System.Collections.Generic;
using System.Text;

namespace UD_FleshGolems.Logging
{
    public class Indent
    {
        public static int MaxIndent = 12;

        public int Value;

        public int Factor;

        public char Char;

        public Indent()
        {
            Value = 0;
            Factor = 4;
            Char = ' ';
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

        public void ResetIndent()
        {
            Value = 0;
        }
        public void GetIndent(out Indent Indent)
        {
            Indent = this;
        }
        public void SetIndent(Indent Indent)
        {
            Value = (int)Indent;
        }
        public void GetIndents(int Offset, out Indents Indents)
        {
            Indent newIndent = new(this);
            newIndent.Value += Offset; 
            Indents = new(newIndent);
        }
        public void GetIndents(out Indents Indents)
        {
            GetIndents(0, out Indents);
        }

        public override string ToString()
        {
            return Char.ThisManyTimes(Math.Min(MaxIndent, Value) * Factor);
        }

        public static implicit operator int(Indent Operand)
        {
            return Operand.Value;
        }
        public static implicit operator Indent(int Operand)
        {
            return new(Operand);
        }
        public static int operator +(Indent Operand1, int Operand2) => Operand1.Value + Operand2;
        public static int operator -(Indent Operand1, int Operand2) => Operand1.Value - Operand2;
        public static int operator +(int Operand1, Indent Operand2) => Operand1 + Operand2.Value;
        public static int operator -(int Operand1, Indent Operand2) => Operand1 - Operand2.Value;

        public override bool Equals(object obj)
        {
            if (obj is int asInt)
            {
                return Value.Equals(asInt);
            }
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(Indent Operand1, Indent Operand2) => Operand1.Value == Operand2.Value;
        public static bool operator !=(Indent Operand1, Indent Operand2) => !(Operand1 == Operand2);

        public static bool operator ==(Indent Operand1, int Operand2) => Operand1.Value == Operand2;
        public static bool operator !=(Indent Operand1, int Operand2) => !(Operand1 == Operand2);

        public static bool operator ==(int Operand1, Indent Operand2) => Operand2 == Operand1;
        public static bool operator !=(int Operand1, Indent Operand2) => Operand2 != Operand1;

        public static bool operator >(Indent Operand1, int Operand2) => Operand1.Value > Operand2;
        public static bool operator <(Indent Operand1, int Operand2) => Operand1.Value < Operand2;

        public static bool operator >(int Operand1, Indent Operand2) => Operand1 > Operand2.Value;
        public static bool operator <(int Operand1, Indent Operand2) => Operand1 < Operand2.Value;

        public static bool operator >=(Indent Operand1, int Operand2) => Operand1.Value >= Operand2;
        public static bool operator <=(Indent Operand1, int Operand2) => Operand1.Value <= Operand2;

        public static bool operator >=(int Operand1, Indent Operand2) => Operand1 >= Operand2.Value;
        public static bool operator <=(int Operand1, Indent Operand2) => Operand1 <= Operand2.Value;
    }
}
