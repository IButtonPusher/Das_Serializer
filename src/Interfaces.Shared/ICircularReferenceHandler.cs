using System;
using System.Collections.Generic;
using Das.Serializer.Remunerators;

namespace Das.Serializer
{
    public interface ICircularReferenceHandler
    {
        Boolean TryHandleCircularReference<TObjectPrinter, TMany, TFew, TWriter>(Object? o,
                                                                               Type propType,
                                                                               NodeTypes nodeType,
                                                                               ISerializerSettings settings,
                                                                               TObjectPrinter objectPrinter,
                                                                               TWriter writer)
            where TMany : IEnumerable<TFew>
            where TWriter : IRemunerable<TMany, TFew>
            where TObjectPrinter : IObjectPrinter<TMany, TFew, TWriter>;

        Boolean IsObjectReferenced(Object obj);

        /// <summary>
        /// Either there is no known reference to this object or we
        /// are handling circular dependencies
        /// </summary>
        Boolean CanPrintObject(Object obj);

        /// <summary>
        /// PushStack()
        /// </summary>
        void AddPathReference(String name);

        /// <summary>
        /// PushStack()
        /// </summary>
        void AddPathReference<TData>(TData data,
                                     Func<TData, String> name);

        /// <summary>
        /// PushStack()
        /// </summary>
        void AddPathReference<TData1, TData2>(TData1 data1,
                                                  TData2 data2,
                                                  Func<TData1, TData2, String> name);

        /// <summary>
        /// PopStack()
        /// </summary>
        void PopPathReference();

        /// <summary>
        /// _pathObjects.RemoveAt(_pathObjects.Count - 1);
        /// </summary>
        void PopPathObject();

        /// <summary>
        /// _pathReferences.Remove(obj);
        /// </summary>
        void RemovePathReference(Object? obj);
    }
}
