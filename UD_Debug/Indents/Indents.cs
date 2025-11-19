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
            Init();
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

        protected void Init(bool ClearFirst = false)
        {
            if (ClearFirst)
            {
                Clear();
            }
            Items = new Indent[1];
            Items[0] = Debug.GetNewIndent();
            EnsureCapacity(DefaultCapacity);
            Last = Items[0];

            Version = 0;
        }

        protected int GetMaxCapacity(int Capacity, int Base) => Math.Min(Capacity, Indent.MaxIndent - (1 + Base));

        protected int GetMaxCapacity(int Capacity, Indent Base) => GetMaxCapacity(Capacity, (int)Base);

        protected void Resize(int Capacity)
        {
            if (Capacity == 0)
            {
                Capacity = DefaultCapacity;
            }
            Indent[] array = new Indent[Capacity];
            int start = 0;
            if (Items != null
                && Items.Length > 0
                && Items[0] != null)
            {
                start = (int)Items[0];
            }
            for (int i = 0; i < Capacity; i++)
            {
                array[i] = new(i + start);
            }
            Items = array;
            Size = Capacity;
            Version++;
        }

        public void EnsureCapacity(int Capacity)
        {
            Capacity = GetMaxCapacity(Capacity, Items[0]);
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
            return Source?.Last;
        }

        public void Reseed(int Offset)
        {
            if (Items == null)
            {
                Init(true);
            }
            int last = (int)Last + Offset;
            for (int i = 0; i < Items.Length; i++)
            {
                Items[i] = new(i + Offset);
            }
            Last = Items[last];
        }
        public void Reseed(Indent Source)
        {
            if (Items == null)
            {
                Init(true);
            }
            Source ??= Debug.GetNewIndent();
            Reseed((int)Source);
        }
    }
}
