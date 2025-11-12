using System;
using System.Collections.Generic;
using System.Text;

namespace UD_FleshGolems.Debug
{
    public partial class Indents : ICollection<Indent>
    {
        public int Count
        {
            get => Length;
            protected set => Length = value;
        }

        public bool IsReadOnly => false;

        public void Clear()
        {
            Items = new Indent[DefaultCapacity];
            Size = DefaultCapacity;
            Count = 0;
            Version = 0;
            Last = null;
        }

        public bool Contains(Indent Item)
        {
            for (int i = 0; i < Length; i++)
            {
                if (Items[i] == Item)
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(Indent[] Array, int ArrayIndex)
        {
            System.Array.Copy(Items, Array, ArrayIndex);
        }

        void ICollection<Indent>.Add(Indent Item)
        {
            throw new NotImplementedException("Items are fixed in an increment order and shouldn't be added this way.");
        }

        bool ICollection<Indent>.Remove(Indent Item)
        {
            throw new NotImplementedException("Items are fixed in an increment order and shouldn't be removed this way.");
        }
    }
}