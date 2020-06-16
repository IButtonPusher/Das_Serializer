using System;
using System.Collections.Generic;
using System.Reflection;

namespace Das.Serializer.ProtoBuf
{
    public partial class ProtoDynamicProvider<TPropertyAttribute> : IStreamAccessor, 
        IProtoProvider where TPropertyAttribute : Attribute
    {
        public IProtoProxy<T> GetAutoProtoProxy<T>(Boolean allowReadOnly = false)
        {
            return GetProtoProxyImpl<T>(CreateAutoProxyType<T>, allowReadOnly);
        }

        public T BuildDefaultValue<T>()
        {
            return _instantiator.BuildDefault<T>(true);
        }

        private Type CreateAutoProxyType<T>(Boolean allowReadOnly)
        {
            var type = typeof(T);
            var fields = GetProtoFields(type, out var ctor);

            return CreateProxyTypeImpl<T>(ctor, fields, false, allowReadOnly, ctor);

        }

        private List<ProtoField> GetProtoFields(Type type, out ConstructorInfo useCtor)
        {

            _instantiator.TryGetDefaultConstructor(type, out var emptyCtor);
            var hasPropCtor = _instantiator.TryGetPropertiesConstructor(type, out var propCtor);


            var ctorParamNames = new Dictionary<String, Type>(StringComparer.OrdinalIgnoreCase);
            if (hasPropCtor)
            {
                foreach (var prm in propCtor.GetParameters())
                {
                    if (String.IsNullOrEmpty(prm.Name))
                        continue;

                    ctorParamNames.Add(prm.Name, prm.ParameterType);
                }
            }


            var useProperties = new List<PropertyInfo>();

            foreach (var prop in type.GetProperties())
            {
                var hasSetter = prop.GetSetMethod(true) != null;
                if (hasSetter)
                    useProperties.Add(prop);
                else
                {
                    if (ctorParamNames.TryGetValue(prop.Name, out var ctorArgType)
                        && ctorArgType == prop.PropertyType)
                    {
                        useProperties.Add(prop);
                    }
                }
            }

            var protoFields = new List<ProtoField>();

            for (var c = 0; c < useProperties.Count; c++)
            {
                var current = useProperties[c];

                if (TryGetProtoFieldImpl(current, true, p => c + 1, out var protoField))
                    protoFields.Add(protoField);

            }

            useCtor = emptyCtor ?? propCtor;
            return protoFields;
        }

    }
}
