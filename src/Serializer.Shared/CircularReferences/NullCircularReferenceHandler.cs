using System;
using System.Collections.Generic;
using Das.Serializer.Remunerators;

namespace Das.Serializer;

public class NullCircularReferenceHandler : ICircularReferenceHandler
{

   public static readonly NullCircularReferenceHandler Instance = new();

   private NullCircularReferenceHandler()
   {
            
   }

   public bool TryHandleCircularReference<TObjectPrinter, TMany, TFew, TWriter>(
      Object? o,
      Type propType,
      NodeTypes nodeType,
      ISerializerSettings settings,
      TObjectPrinter objectPrinter,
      TWriter writer)
      where TObjectPrinter : IObjectPrinter<TMany, TFew, TWriter> 
      where TMany : IEnumerable<TFew> 
      where TWriter : IRemunerable<TMany, TFew>
   {
      return false;
   }

   public bool IsObjectReferenced(Object obj)
   {
      return false;
   }

   public bool CanPrintObject(Object obj)
   {
      return true;
   }

   public void AddPathReference(String name)
   {
            
   }

   public void AddPathReference(String txt1,
                                String txt2)
   {
           
   }

   public void AddPathReference(String txt1,
                                String txt2,
                                String txt3)
   {
           
   }

   public void AddPathReference<T>(String txt1,
                                   T item,
                                   String txt2)
   {
           
   }

   public void AddPathReference<TData>(TData data,
                                       Func<TData, string> name)
   {
            
   }

   public void AddPathReference<TData1, TData2>(TData1 data1,
                                                TData2 data2,
                                                Func<TData1, TData2, string> name)
   {
            
   }

   public void PopPathReference()
   {
            
   }

   public void PopPathObject()
   {
            
   }

   public void RemovePathReference(Object? obj)
   {
            
   }
}