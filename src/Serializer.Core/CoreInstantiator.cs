using System;
using System.Collections;
using System.Reflection;

namespace Das.Serializer
{
    public class CoreInstantiator : IInstantiator
    {
        public CoreInstantiator()
        {
            
        }

        public object? BuildDefault(Type type, 
                                    Boolean isCacheConstructors)
        {
            throw new NotImplementedException();
        }

        public T BuildDefault<T>(Boolean isCacheConstructors)
        {
            throw new NotImplementedException();
        }

        public IList BuildGenericList(Type type)
        {
            throw new NotImplementedException();
        }

        public T CreatePrimitiveObject<T>(Byte[] rawValue, 
                                          Type objType)
        {
            throw new NotImplementedException();
        }

        public object CreatePrimitiveObject(Byte[] rawValue, 
                                            Type objType)
        {
            throw new NotImplementedException();
        }

        public TDelegate GetConstructorDelegate<TDelegate>(Type type) 
            where TDelegate : Delegate
        {
            throw new NotImplementedException();
        }

        public Func<object> GetDefaultConstructor(Type type)
        {
            throw new NotImplementedException();
        }

        public Func<T> GetDefaultConstructor<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public void OnDeserialized(IValueNode node, ISerializationDepth depth)
        {
            throw new NotImplementedException();
        }

        public bool TryGetConstructorDelegate<TDelegate>(Type type, 
                                                         out TDelegate result) 
            where TDelegate : Delegate
        {
            throw new NotImplementedException();
        }

        public bool TryGetDefaultConstructor(Type type, 
                                             out ConstructorInfo ctor)
        {
            throw new NotImplementedException();
        }

        public bool TryGetDefaultConstructor<T>(out ConstructorInfo? ctor)
        {
            throw new NotImplementedException();
        }

        public bool TryGetDefaultConstructorDelegate<T>(out Func<T> res) where T : class
        {
            throw new NotImplementedException();
        }

        public bool TryGetPropertiesConstructor(Type type, 
                                                out ConstructorInfo constr)
        {
            throw new NotImplementedException();
        }
    }
}
