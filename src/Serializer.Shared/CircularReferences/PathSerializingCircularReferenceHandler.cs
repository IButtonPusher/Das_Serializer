using System;

namespace Das.Serializer.CircularReferences
{
    public class PathSerializingCircularReferenceHandler : BaseCircularReferenceHandler
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

            var objIndex = _pathObjects.IndexOf(o);
            objectPrinter.PrintCircularDependency(objIndex, writer, settings, _pathStack, this);
            return true;
        }
    }
}
