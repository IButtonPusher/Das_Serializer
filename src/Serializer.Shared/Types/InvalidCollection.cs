using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer.Types
{
    public class InvalidCollection<A, B> : IDictionary<A, B>, IList<B>
    {
        IEnumerator<KeyValuePair<A, B>> IEnumerable<KeyValuePair<A, B>>.GetEnumerator()
        {
            throw new InvalidOperationException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new InvalidOperationException();
        }

        public void Add(KeyValuePair<A, B> item)
        {
            throw new InvalidOperationException();
        }

        void ICollection<KeyValuePair<A, B>>.Clear()
        {
            throw new InvalidOperationException();
        }

        public Boolean Contains(KeyValuePair<A, B> item)
        {
            throw new InvalidOperationException();
        }


        public void CopyTo(KeyValuePair<A, B>[] array, Int32 arrayIndex)
        {
            throw new InvalidOperationException();
        }

        public Boolean Remove(KeyValuePair<A, B> item)
        {
            throw new InvalidOperationException();
        }

        Int32 ICollection<KeyValuePair<A, B>>.Count => throw new InvalidOperationException();

        Boolean ICollection<KeyValuePair<A, B>>.IsReadOnly => throw new InvalidOperationException();

        public void Add(A key, B value)
        {
            throw new InvalidOperationException();
        }

        public Boolean ContainsKey(A key)
        {
            throw new InvalidOperationException();
        }

        public Boolean Remove(A key)
        {
            throw new InvalidOperationException();
        }

        public Boolean TryGetValue(A key, out B value)
        {
            throw new InvalidOperationException();
        }

        public B this[A key]
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        public ICollection<A> Keys => throw new InvalidOperationException();

        public ICollection<B> Values => throw new InvalidOperationException();

        IEnumerator<B> IEnumerable<B>.GetEnumerator()
        {
            throw new InvalidOperationException();
        }

        public void Add(B item)
        {
            throw new InvalidOperationException();
        }

        void ICollection<B>.Clear()
        {
            throw new InvalidOperationException();
        }

        public Boolean Contains(B item)
        {
            throw new InvalidOperationException();
        }


        public void CopyTo(B[] array, Int32 arrayIndex)
        {
            throw new InvalidOperationException();
        }

        public Boolean Remove(B item)
        {
            throw new InvalidOperationException();
        }

        Int32 ICollection<B>.Count => throw new InvalidOperationException();

        Boolean ICollection<B>.IsReadOnly => throw new InvalidOperationException();

        public Int32 IndexOf(B item)
        {
            throw new InvalidOperationException();
        }

        public void Insert(Int32 index, B item)
        {
            throw new InvalidOperationException();
        }

        public void RemoveAt(Int32 index)
        {
            throw new InvalidOperationException();
        }

        public B this[Int32 index]
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }
    }
}