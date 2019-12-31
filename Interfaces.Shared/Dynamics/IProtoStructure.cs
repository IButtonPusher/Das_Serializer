using System;
using System.Collections.Generic;
using Das.Serializer.Objects;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer
{
    public interface IProtoStructure : ITypeStructure
    {
        IProtoFieldAccessor this[Int32 idx] {get;}

        Int32 GetValueCount(Object obj);

        Dictionary<Int32, IProtoStructure> PropertyStructures { get; }

        Dictionary<Int32, IProtoFieldAccessor> FieldMap { get; }

        Boolean IsRepeating(ref ProtoWireTypes wireType, ref TypeCode typeCodes, ref Type type);

        /// <summary>
        /// returns wire type as first 3 bits then field index as an int per proto spec
        /// </summary>
        Boolean TryGetHeader(INamedField field, out Int32 header);

        IProtoPropertyIterator GetPropertyValues(Object o);

        IProtoPropertyIterator GetPropertyValues(Object o, Int32 fieldIndex);

        Object BuildDefault();
    }
}
