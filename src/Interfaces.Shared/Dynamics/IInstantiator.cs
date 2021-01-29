using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IInstantiator
    {
        Object? BuildDefault(Type type,
                             Boolean isCacheConstructors);

        T BuildDefault<T>(Boolean isCacheConstructors);

        IList BuildGenericList(Type type);

        T CreatePrimitiveObject<T>(Byte[] rawValue,
                                   Type objType);

        Object CreatePrimitiveObject(Byte[] rawValue,
                                     Type objType);

        TDelegate GetConstructorDelegate<TDelegate>(Type type)
            where TDelegate : Delegate;

        Func<Object> GetDefaultConstructor(Type type);

        Func<T> GetDefaultConstructor<T>()
            where T : class;

        void OnDeserialized(IValueNode node,
                            ISerializationDepth depth);

        Boolean TryGetConstructorDelegate<TDelegate>(Type type,
                                                     out TDelegate result)
            where TDelegate : Delegate;

        Boolean TryGetDefaultConstructor(Type type,
                                         out ConstructorInfo ctor);

        Boolean TryGetDefaultConstructor<T>(out ConstructorInfo? ctor);

        Boolean TryGetDefaultConstructorDelegate<T>(out Func<T> res)
            where T : class;

        Boolean TryGetPropertiesConstructor(Type type,
                                            out ConstructorInfo constr);
    }
}
