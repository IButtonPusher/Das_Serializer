using System;
using System.Reflection;

namespace Das.Serializer
{
    public interface IInstantiator
    {
        Object BuildDefault(Type type, Boolean isCacheConstructors);

        T BuildDefault<T>(Boolean isCacheConstructors);

        Delegate GetConstructorDelegate(Type type, Type delegateType);

        void OnDeserialized(IValueNode node, ISerializationDepth depth);

        Boolean TryGetPropertiesConstructor(Type type, out ConstructorInfo constr);

        T CreatePrimitiveObject<T>(Byte[] rawValue, Type objType);

        Object CreatePrimitiveObject(Byte[] rawValue, Type objType);
    }
}