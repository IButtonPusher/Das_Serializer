using System;

namespace Das.Serializer.CircularReferences;

/// <summary>
/// Throws an exception when a circular reference is detected
/// </summary>
public sealed class ExceptionCircularReferenceHandler : BaseCircularReferenceHandler
{

   public override bool TryHandleCircularReference<TObjectPrinter, TMany, TFew, TWriter>(
      Object? o,
      Type propType,
      NodeTypes nodeType,
      ISerializerSettings settings,
      TObjectPrinter objectPrinter,
      TWriter writer)
   {
      if (TryAddPathReference(o))
         return false;

      throw new CircularReferenceException(_pathStack, objectPrinter.PathSeparator);
   }
}