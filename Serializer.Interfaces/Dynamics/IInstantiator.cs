using System;
using System.Reflection;

namespace Das.Serializer
{
    public interface IInstantiator
    {
        Object BuildDefault(Type type, Boolean isCacheConstructors);

        T BuildDefault<T>(Boolean isCacheConstructors);

        Delegate GetConstructorDelegate(Type type, Type delegateType);

        Func<object> GetConstructorDelegate(Type type);

        void OnDeserialized(Object obj, SerializationDepth depth);

        Boolean TryGetPropertiesConstructor(Type type, out ConstructorInfo constr);

        T CreatePrimitiveObject<T>(byte[] rawValue, Type objType);

        Object CreatePrimitiveObject(byte[] rawValue, Type objType);
    }
}