using System;
using System.Collections.Generic;

namespace Das.Serializer.Remunerators;

public interface IObjectPrinter<TMany, TFew, in TWriter> : IObjectPrinter
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
                           NodeTypes nodeType,
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
   Char PathSeparator {get;}

   Boolean IsPrintNullProperties {get;}
}