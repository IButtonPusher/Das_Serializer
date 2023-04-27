using System;
using System.Threading.Tasks;
using Das.Serializer.Properties;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer;

public interface IProtoField : IPropertyInfo, 
                               IEquatable<IProtoField>,
                               IIndexedProperty
{
   /// <summary>
   ///     Wire Type | Index bit shift left 3
   /// </summary>
   Int32 Header { get; }

   Boolean IsLeafType { get; }

   Boolean IsRepeatedField { get; }

   ProtoWireTypes WireType { get; }
}