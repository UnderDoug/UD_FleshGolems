using System;
using System.Collections.Generic;
using System.Text;

namespace UD_FleshGolems.Logging
{
    public partial class Indents
    {
        protected Indent[] Items;

        protected int Size;

        protected int Length;

        protected int Version;

        protected Indent Last;

        protected int DefaultCapacity => Indent.MaxIndent;

        public Indents()
        {
            Items = new Indent[1];
            Items[0] = Debug.GetNewIndent();
            EnsureCapacity(DefaultCapacity);
            Last = Items[0];

            Version = 0;
        }
        public Indents(Indent Source)
            : this()
        {
            Last = Source;
            Items[0] = Source;

            Version = 0;
        }
        public Indents(int Capacity)
            : this()
        {
            EnsureCapacity(Capacity);

            Version = 0;
        }
        public Indents(int Capacity, Indent Source)
            : this(Source)
        {
            EnsureCapacity(Capacity);

            Version = 0;
        }
        public Indents(Indents Source)
            : this(Source[0])
        {
            EnsureCapacity(Source.Count);
            Version = Source.Version;
            Last = Items[Source.IndexOf(Source.Last)];

            Version = 0;
        }

        protected void Resize(int Capacity)
        {
            if (Capacity == 0)
            {
                Capacity = DefaultCapacity;
            }
            Indent[] array = new Indent[Capacity];
            Indent seedIndent = new Indent(Items[0]) ?? new Indent(Debug.GetNewIndent());
            for (int i = 0; i < Capacity; i++)
            {
                array[i] = new(i, seedIndent);
            }
            Items = array;
            Size = Capacity;
            Version++;
        }

        public void EnsureCapacity(int Capacity)
        {
            Capacity = Math.Min(Capacity, Indent.MaxIndent - 1 - Items[0].Value);
            if (Size < Capacity)
            {
                Resize(Capacity);
            }
        }

        public override string ToString()
        {
            return Last?.ToString();
        }

        public static implicit operator Indent(Indents Source)
        {
            return Source.Last;
        }

        /*
        public static explicit operator Indents(Indent Source)
        {
            return new(Source);
        }
        */
    }
}
