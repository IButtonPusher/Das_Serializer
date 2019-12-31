using System;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer
{
    public interface IProtoScanStructure :  IProtoStructureBase
	{

        Boolean IsRepeating(ref ProtoWireTypes wireType, ref TypeCode typeCodes, ref Type type);

        void Set(IProtoFeeder byteFeeder, Int32 fieldHeader);
    }
}