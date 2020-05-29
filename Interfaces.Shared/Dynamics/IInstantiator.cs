using System;
using System.Reflection;

namespace Das.Serializer
{
    public interface IInstantiator
    {
        Object BuildDefault(Type type, Boolean isCacheConstructors);

        T BuildDefault<T>(Boolean isCacheConstructors);

        TDelegate GetConstructorDelegate<TDelegate>(Type type)
            where TDelegate : Delegate;

        Boolean TryGetConstructorDelegate<TDelegate>(Type type,
            out TDelegate result) 
            where TDelegate : Delegate;

        void OnDeserialized(IValueNode node, ISerializationDepth depth);

        Boolean TryGetPropertiesConstructor(Type type, out ConstructorInfo constr);

        Func<Object> GetDefaultConstructor(Type type);

        Func<T> GetDefaultConstructor<T>() where T : class;

        Boolean TryGetDefaultConstructorDelegate<T>(out Func<T> res) 
            where T : class;

        Boolean TryGetDefaultConstructor<T>(out ConstructorInfo ctor) 
            where T : class;

        T CreatePrimitiveObject<T>(Byte[] rawValue, Type objType);

        Object CreatePrimitiveObject(Byte[] rawValue, Type objType);
    }
}