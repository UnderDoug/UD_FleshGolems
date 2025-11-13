using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UD_FleshGolems.Logging
{
    public partial class Indents : IEnumerable<Indent>
    {
        public struct Enumerator
            : IEnumerator<Indent>
            , IEnumerator
            , IDisposable
        {
            private Indents Indents;

            private Indent[] Items;

            private readonly int Version;

            private int Index;

            public readonly Indent Current => Items[Index];

            readonly object IEnumerator.Current => Current;

            public Enumerator(Indents Indents)
            {
                this.Indents = Indents;
                Items = new Indent[Indents.Count];
                Array.Copy(Indents.Items, Items, Indents.Count);
                Version = Indents.Version;
                Index = -1;
            }

            public readonly Enumerator GetEnumerator()
            {
                return this;
            }

            public bool MoveNext()
            {
                if (Version != Indents.Version)
                {
                    throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
                }
                while (++Index < Items.Length)
                {
                    if (Items[Index] != null)
                    {
                        return true;
                    }
                }
                return false;
            }

            public void Dispose()
            {
                Array.Clear(Items, 0, Items.Length);
                Items = null;
            }

            public void Reset()
            {
                if (Version != Indents.Version)
                {
                    throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
                }
                Index = -1;
            }
        }

        public IEnumerator<Indent> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}