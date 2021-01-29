using System;
using System.Collections;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public readonly struct ValueCollectionWrapper : IList
    {
        private readonly ICollection _baseCollection;
        private readonly VoidMethod _addDelegate;

        public ValueCollectionWrapper(ICollection baseCollection,
                                      VoidMethod addDelegate)
        {
            _baseCollection = baseCollection;
            _addDelegate = addDelegate;
        }

        public ICollection GetBaseCollection()
        {
            return _baseCollection;
        }

        public IEnumerator GetEnumerator()
        {
            return _baseCollection.GetEnumerator();
        }

        public void CopyTo(Array array,
                           Int32 index)
        {
            _baseCollection.CopyTo(array, index);
        }

        public Int32 Count => _baseCollection.Count;

        public Object SyncRoot => _baseCollection.SyncRoot;

        public Boolean IsSynchronized => _baseCollection.IsSynchronized;

        int IList.Add(Object value)
        {
            _addDelegate(_baseCollection, value);
            return 0; //TODO:
        }

        bool IList.Contains(Object value)
        {
            throw new NotSupportedException();
        }

        void IList.Clear()
        {
            throw new NotSupportedException();
        }

        int IList.IndexOf(Object value)
        {
            throw new NotSupportedException();
        }

        void IList.Insert(Int32 index,
                          Object value)
        {
            throw new NotSupportedException();
        }

        void IList.Remove(Object value)
        {
            throw new NotSupportedException();
        }

        void IList.RemoveAt(Int32 index)
        {
            throw new NotSupportedException();
        }

        Object IList.this[Int32 index]
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        Boolean IList.IsReadOnly => throw new NotSupportedException();

        Boolean IList.IsFixedSize => throw new NotSupportedException();
    }
}
