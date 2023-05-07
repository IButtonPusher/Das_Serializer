#if GENERATECODE

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute> : IStreamAccessor,
                                                                    // ReSharper disable once RedundantExtendsListEntry
                                                                    IProtoProvider 
        where TPropertyAttribute : Attribute
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

            var scanFields = GetProtoScanFields(type, out var ctor);

            var ptype = CreateProxyTypeImpl(type, ctor, scanFields,
                            false, false, ctor) ??
                        throw new InvalidOperationException();

            return InstantiateProxyInstance<T>(ptype);
        }

        private IProtoProxy<T> CreateAutoProxyTypeYesReadOnly<T>()
        {
            var type = typeof(T);

            var scanFields = GetProtoScanFields(type, out var ctor);

            var ptype = CreateProxyTypeImpl(type, ctor, scanFields, false, true, ctor) ??
                        throw new InvalidOperationException();

            return InstantiateProxyInstance<T>(ptype);
        }

        protected override MethodInfo GetProxyMethod => _getProtoProxy;
    }
}

#endif
