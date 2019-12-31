using System;
using System.Collections.Generic;

namespace Das.Serializer.ProtoBuf
{
    public class NullProtoScanStructure : IProtoScanStructure
    {
        public Type Type => throw new NotImplementedException();

        public void SetPropertyValueUnsafe(String propName, ref Object targetObj, Object propVal)
        {
            throw new NotImplementedException();
        }

        public Object BuildDefault()
        {
            throw new NotImplementedException();
        }

        public Dictionary<Int32, IProtoFieldAccessor> FieldMap => throw new NotImplementedException();

        public Boolean IsRepeating(ref ProtoWireTypes wireType, ref TypeCode typeCodes, ref Type type)
        {
            return false;
        }

        public void Set(IProtoFeeder byteFeeder, Int32 fieldHeader)
        {
            throw new NotImplementedException();
        }
    }
}
