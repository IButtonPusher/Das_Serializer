using System;
using System.Reflection;
using Das.Extensions;

namespace Das.Serializer.Properties
{
    public class FieldAccessor : IMemberAccessor
    {
        private readonly FieldInfo _field;
        private readonly Action<object, object>? _fieldSetter;

        public FieldAccessor(FieldInfo field,
                             Action<Object, Object>? fieldSetter)
        {
            IsMemberSerializable = field.GetCustomAttribute<NonSerializedAttribute>() == null!;
            

            _field = field;
            _fieldSetter = fieldSetter;
            Name = field.Name;
            Type = field.FieldType;
        }

        public String Name { get; }

        public Type Type { get; }

        public Boolean IsMemberSerializable { get; }

        public object? GetValue(Object obj) => _field.GetValue(obj);

        public bool TrySetValue(ref Object targetObj,
                                Object? propVal)
        {
            if (_fieldSetter is not { })
                return false;
            
            _fieldSetter(targetObj, propVal!);
            return true;
        }

        public bool IsValidForSerialization(SerializationDepth depth) =>
            (depth & SerializationDepth.PrivateFields) == SerializationDepth.PrivateFields;
    }
}
