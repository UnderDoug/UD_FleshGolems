using System;
using System.Collections.Generic;
using System.Text;

namespace UD_FleshGolems.Debug
{
    public partial class Indents : IList<Indent>
    {

        public Indent this[int Index]
        {
            get
            {
                if (Index >= Size)
                {
                    EnsureCapacity(Index);
                }
                return Last = Items[Index];
            }
        }

        Indent IList<Indent>.this[int index]
        { 
            get => throw new NotImplementedException("Items are fixed in an increment order and shouldn't be accessed this way.");
            set => throw new NotImplementedException("Items are fixed in an increment order and shouldn't be altered this way.");
        }

        public int IndexOf(Indent Item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (Items[i] == Item)
                {
                    return i;
                }
            }
            return -1;
        }

        void IList<Indent>.Insert(int Index, Indent Item)
        {
            throw new NotImplementedException("Items are fixed in an increment order and shouldn't be added this way.");
        }

        void IList<Indent>.RemoveAt(int Index)
        {
            throw new NotImplementedException("Items are fixed in an increment order and shouldn't be removed this way.");
        }
    }
}