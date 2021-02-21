using System;

namespace Das.Serializer.CircularReferences
{
    /// <summary>
    /// Changes circular references to null property values
    /// </summary>
    public class IgnoringCircularReferenceHandler : BaseCircularReferenceHandler
    {
       
        public sealed override bool TryHandleCircularReference<TObjectPrinter, TMany, TFew, TWriter>(
            Object? o,
            Type propType,
            NodeTypes nodeType,
            ISerializerSettings settings,
            TObjectPrinter objectPrinter,
            TWriter writer)
        {
            if (TryAddPathReference(o))
                return false;

            if (objectPrinter.IsPrintNullProperties)
                objectPrinter.PrintObject(null, propType, nodeType, writer, settings, this);

            return true;
        }

        public override bool CanPrintObject(Object obj)
        {
            return !IsObjectReferenced(obj);
        }
    }
}
