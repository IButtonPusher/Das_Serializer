using System;
using System.Collections.Generic;
using Das.Serializer.Remunerators;

namespace Das.Serializer.CircularReferences
{
    public abstract class BaseCircularReferenceHandler : ICircularReferenceHandler
    {
        protected BaseCircularReferenceHandler()
        {
            _pathStack = new List<String>();
            _pathReferences = new HashSet<Object>();
            _pathObjects = new List<Object?>();
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

        public abstract bool TryHandleCircularReference<TObjectPrinter, TMany, TFew, TWriter>(Object? o,
            Type propType,
            NodeTypes nodeType,
            ISerializerSettings settings,
            TObjectPrinter objectPrinter,
            TWriter writer) 
            where TObjectPrinter : IObjectPrinter<TMany, TFew, TWriter> 
            where TMany : IEnumerable<TFew> 
            where TWriter : IRemunerable<TMany, TFew>;

        public bool IsObjectReferenced(Object obj)
        {
            return _pathReferences.Contains(obj);
        }

        public virtual bool CanPrintObject(Object obj)
        {
            return true;
        }

        public void AddPathReference(String name)
        {
            _pathStack.Add(name);
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

        //to quickly detected if an object exists in this path
        protected readonly HashSet<Object> _pathReferences;

        protected readonly List<String> _pathStack;
        //to map the order to property names
        protected readonly List<Object?> _pathObjects;
    }
}
