using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Das.Serializer.State;

namespace Das.Serializer;

public readonly struct ValueCollectionWrapper<T> : IList,
                                                   ICollectionWrapper
{
   private readonly ICollection<T> _baseCollection;

   public ValueCollectionWrapper(ICollection<T> baseCollection)
   {
      _baseCollection = baseCollection;
   }

   Object ICollectionWrapper.GetBaseCollection() => _baseCollection;

   public ICollection GetBaseCollection()
   {
      if (_baseCollection is ICollection { } good)
         return good;
            
      return new List<T>(_baseCollection);
   }

   public IEnumerator GetEnumerator()
   {
      return _baseCollection.GetEnumerator();
   }

   public void CopyTo(Array array,
                      Int32 index)
   {
      _baseCollection.CopyTo((T[])array, index);
   }

   public Int32 Count => _baseCollection.Count;

   public Object SyncRoot => throw new NotSupportedException();

   public Boolean IsSynchronized => false;

   int IList.Add(Object? value)
   {
      if (value is T v)
      {
         _baseCollection.Add(v);
         return 1;
      }

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

public readonly struct ValueCollectionWrapper : IList,
                                                ICollectionWrapper
{
   private readonly ICollection _baseCollection;
   private readonly VoidMethod _addDelegate;

   public ValueCollectionWrapper(ICollection baseCollection,
                                 VoidMethod addDelegate)
   {
      _baseCollection = baseCollection;
      _addDelegate = addDelegate;
   }

   Object ICollectionWrapper.GetBaseCollection() => GetBaseCollection();

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