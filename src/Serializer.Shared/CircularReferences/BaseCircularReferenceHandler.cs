using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Das.Serializer.Remunerators;

namespace Das.Serializer.CircularReferences;

public abstract class BaseCircularReferenceHandler : ICircularReferenceHandler
{
   protected BaseCircularReferenceHandler()
   {
      _pathStack = new List<String>();
      _pathReferences = new HashSet<Object>();
      _pathObjects = new List<Object?>();
   }

   public abstract bool TryHandleCircularReference<TObjectPrinter, TMany, TFew, TWriter>(Object? o,
      Type propType,
      NodeTypes nodeType,
      ISerializerSettings settings,
      TObjectPrinter objectPrinter,
      TWriter writer)
      where TObjectPrinter : IObjectPrinter<TMany, TFew, TWriter>
      where TMany : IEnumerable<TFew>
      where TWriter : IRemunerable<TMany, TFew>;

   public bool IsObjectReferenced(Object obj) => _pathReferences.Contains(obj);

   public virtual bool CanPrintObject(Object obj) => true;

   public void AddPathReference(String name)
   {
      _pathStack.Add(name);
   }

   public void AddPathReference(String txt1,
                                String txt2)
   {
      _pathStack.Add(string.Concat(txt1, txt2));
   }

   public void AddPathReference(String txt1,
                                String txt2,
                                String txt3)
   {
      _pathStack.Add(string.Concat(txt1, txt2, txt3));
   }

   public void AddPathReference<T>(String txt1,
                                   T item,
                                   String txt2)
   {
      _pathStack.Add(string.Concat(txt1, item, txt2));
   }

   public void AddPathReference<TData>(TData data,
                                       Func<TData, string> name)
   {
      _pathStack.Add(name(data));
   }

   public void AddPathReference<TData1, TData2>(TData1 data1,
                                                TData2 data2,
                                                Func<TData1, TData2, string> name)
   {
      _pathStack.Add(name(data1, data2));
   }


   public void PopPathReference()
   {
      _pathStack.RemoveAt(_pathStack.Count - 1);
   }

   public void PopPathObject()
   {
      _pathObjects.RemoveAt(_pathObjects.Count - 1);
   }

   public void RemovePathReference(Object? obj)
   {
      if (obj != null)
         _pathReferences.Remove(obj);
   }

   public void Clear()
   {
      _pathStack.Clear();
      _pathObjects.Clear();
      _pathReferences.Clear();
   }

   protected Boolean TryAddPathReference(Object? o)
   {
      if (o == null || _pathReferences.Add(o))
      {
         _pathObjects.Add(o);
         return true;
      }

      return false;
   }

   //to map the order to property names
   protected readonly List<Object?> _pathObjects;

   //to quickly detected if an object exists in this path
   protected readonly HashSet<Object> _pathReferences;

   protected readonly List<String> _pathStack;
}