using System;
using System.Collections;
using System.Collections.Generic;
using Das.Serializer.Objects;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer
{
    public class ProtoCollectionStructure : IProtoStructure
        //, IProtoPropertyIterator
    {
        public ProtoCollectionStructure(IProtoStructure structure, ITypeCore typeCore)
        {
            IsCollection = true;
            _structure = structure;
            Type = typeCore.GetGermaneType(structure.Type);
            WireType = ProtoStructure.GetWireType(Type);
        }

         public ProtoWireTypes WireType { get; }
         public Int32 Header { get; }
         public Int32 Index { get; }
         public TypeCode TypeCode { get; }
         public Boolean IsLeaf { get; }

         private readonly IProtoStructure _structure;

        public virtual Boolean SetValue(String propName, ref Object targetObj, Object propVal,
            SerializationDepth depth)
        {
            return true;
        }

        IProtoFieldAccessor IProtoStructure.this[Int32 idx] => _structure[idx];
        public Int32 GetterCount { get; }
        public Dictionary<Int32, IProtoStructure> PropertyStructures { get; }

        public Dictionary<Int32, IProtoFieldAccessor> FieldMap => _structure.FieldMap;

        public Boolean IsCollection {get; protected set; }
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
      

        public String Name { get; }
        public Object Value { get; }
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Boolean IsEmptyInitialized { get; }
        public Type DeclaringType { get; }
        public Boolean MoveNext()
        {
            throw new NotImplementedException();
        }

        public IProtoProperty this[Int32 index] => throw new NotImplementedException();

        public T Get<T>(Int32 index) where T : INamedValue
            => 
        throw new NotImplementedException();

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public Int32 Count => throw new NotImplementedException();

        public IEnumerator<IProtoProperty> GetEnumerator()
        {
            throw new NotImplementedException();
        }

//        IEnumerator IEnumerable.GetEnumerator()
//        {
//            return GetEnumerator();
//        }

        public void Push()
        {
            throw new NotImplementedException();
        }

        public Boolean Pop()
        {
            throw new NotImplementedException();
        }
    }
}
