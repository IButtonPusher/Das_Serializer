using System;
using System.Collections.Generic;
using Das.Serializer.Objects;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer
{
    public class ProtoCollectionStructure : IProtoStructure, IProtoScanStructure
    {
        public ProtoCollectionStructure(IProtoStructure structure, ITypeCore typeCore)
        {
          //  IsRepeating = true;
            _structure = structure;
            Type = typeCore.GetGermaneType(structure.Type);
            WireType = ProtoStructure.GetWireType(Type);
        }

         public ProtoWireTypes WireType { get; }

         private readonly IProtoStructure _structure;

        public virtual Boolean SetValue(String propName, ref Object targetObj, Object propVal,
            SerializationDepth depth)
        {
            return true;
        }

        public void SetPropertyValueUnsafe(String propName, ref Object targetObj, Object propVal)
        {
            _structure.SetPropertyValueUnsafe(propName, ref targetObj, propVal);
        }

        IProtoFieldAccessor IProtoStructure.this[Int32 idx] => _structure[idx];
        public Int32 GetValueCount(Object o) => throw new NotImplementedException();
        public Dictionary<Int32, IProtoStructure> PropertyStructures { get; }

        public Dictionary<Int32, IProtoFieldAccessor> FieldMap => _structure.FieldMap;

       // public Boolean IsRepeating {get; protected set; }
        // Boolean IProtoScanStructure.IsRepeating(ref ProtoWireTypes wireType,ref TypeCode typeCodes,
        //     ref Type type)
        // {
        //     throw new NotImplementedException();
        // }

        public Boolean TryGetHeader(INamedField field, out Int32 header)
        {
            return _structure.TryGetHeader(field, out header);
        }

        public Dictionary<String, INamedField> MemberTypes => _structure.MemberTypes;

        public Int32 PropertyCount => _structure.PropertyCount;
        public Type Type { get; set; }

        public SerializationDepth Depth => _structure.Depth;

        public Boolean OnDeserialized(Object obj, IObjectManipulator objectManipulator)
        {
            return _structure.OnDeserialized(obj, objectManipulator);
        }

        public IPropertyValueIterator<IProperty> GetPropertyValues(Object o, ISerializationDepth 
            depth)
        {
            return _structure.GetPropertyValues(o);
        }

        public IProtoPropertyIterator GetPropertyValues(Object o)
        {
            throw new NotImplementedException();
            //return this;
        }

        public IProtoPropertyIterator GetPropertyValues(Object o, Int32 fieldIndex)
        {
            throw new NotImplementedException();
        }

        public Object BuildDefault()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<INamedField> GetMembersToSerialize(ISerializationDepth depth)
        {
            return _structure.GetMembersToSerialize(depth);
        }

        public IProperty GetPropertyValue(Object o, String propertyName)
        {
            return _structure.GetPropertyValue(o, propertyName);
        }

        public Boolean SetFieldValue(String fieldName, Object targetObj, Object fieldVal)
        {
            return _structure.SetFieldValue(fieldName, targetObj, fieldVal);
        }

        public Boolean SetFieldValue<T>(String fieldName, Object targetObj, Object fieldVal)
        {
            return _structure.SetFieldValue<T>(fieldName, targetObj, fieldVal);
        }

      

        public Boolean TryGetAttribute<TAttribute>(String propertyName, out TAttribute value) where TAttribute : Attribute
        {
            return _structure.TryGetAttribute(propertyName, out value);
        }

        Boolean IProtoScanStructure.IsRepeating(ref ProtoWireTypes wireType, ref TypeCode typeCodes, ref Type type)
        {
            throw new NotImplementedException();
        }

        public void Set(IProtoFeeder byteFeeder, Int32 fieldHeader)
        {
            throw new NotImplementedException();
        }
    }
}
