#if GENERATECODE

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Das.Serializer.Proto;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute> : IStreamAccessor,
                                                                    // ReSharper disable once RedundantExtendsListEntry
                                                                    IProtoProvider where TPropertyAttribute : Attribute
    {
        public IProtoProxy<T> GetAutoProtoProxy<T>(Boolean allowReadOnly = false)
        {
            return ProxyLookup<T>.Instance ??= allowReadOnly
                ? CreateAutoProxyTypeYesReadOnly<T>()
                : CreateAutoProxyTypeNoReadOnly<T>();
        }

        public T BuildDefaultValue<T>()
        {
            return _instantiator.BuildDefault<T>(true);
        }

        private IProtoProxy<T> CreateAutoProxyTypeNoReadOnly<T>()
        {
            var type = typeof(T);

            var fields = GetProtoFields(type, out var ctor);

            var ptype = CreateProxyTypeImpl(type, ctor, fields, false, false, ctor) ??
                        throw new InvalidOperationException();

            return InstantiateProxyInstance<T>(ptype);
        }

        private IProtoProxy<T> CreateAutoProxyTypeYesReadOnly<T>()
        {
            var type = typeof(T);

            var fields = GetProtoFields(type, out var ctor);

            var ptype = CreateProxyTypeImpl(type, ctor, fields, false, true, ctor) ??
                        throw new InvalidOperationException();

            return InstantiateProxyInstance<T>(ptype);
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

                var next = c + 1;

                if (TryGetProtoFieldImpl(current, true, p => next, out var protoField))
                    protoFields.Add(protoField);
            }

            useCtor = emptyCtor ?? propCtor;
            return protoFields;
        }
    }
}

#endif