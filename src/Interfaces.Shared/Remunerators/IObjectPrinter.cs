using System;
using System.Collections.Generic;

namespace Das.Serializer.Remunerators
{
    public interface IObjectPrinter<TMany, TFew, TWriter> : IObjectPrinter
        where TMany : IEnumerable<TFew>
        where TWriter : IRemunerable<TMany, TFew>
    {
        void PrintNamedObject(String nodeName,
                              Type? propType,
                              Object? nodeValue,
                              NodeTypes nodeType,
                              TWriter writer,
                              ISerializerSettings settings,
                              ICircularReferenceHandler circularReferenceHandler);

        Boolean PrintObject(Object? o,
                            Type propType,
                            NodeTypes nodeType,
                            TWriter writer,
                            ISerializerSettings settings,
                            ICircularReferenceHandler circularReferenceHandler);

        void PrintReferenceType(Object? value,
                                Type valType,
                                TWriter writer,
                                ISerializerSettings settings,
                                ICircularReferenceHandler circularReferenceHandler);

        void PrintCircularDependency(Int32 index,
                                     TWriter writer,
                                     ISerializerSettings settings,
                                     IEnumerable<String> pathStack,
                                     ICircularReferenceHandler circularReferenceHandler);
    }

    public interface IObjectPrinter
    {
        //Boolean PrintObject(Object? o,
        //                    Type propType,
        //                    NodeTypes nodeType,
        //                    ISerializerSettings settings,
        //                    ICircularReferenceHandler circularReferenceHandler);

        //void PrintCircularDependency(Int32 index,
        //                             ISerializerSettings settings,
        //                             IEnumerable<String> pathStack,
        //                             ICircularReferenceHandler circularReferenceHandler);

        Char PathSeparator {get;}

        Boolean IsPrintNullProperties {get;}
    }
}
