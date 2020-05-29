using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Das.Extensions;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer.Proto
{
    public abstract class ProtoStateBase
    {
        public Dictionary<IProtoFieldAccessor, LocalBuilder> ChildProxies { get; }
        public ILGenerator IL { get; }

        protected  ProtoStateBase(ILGenerator il, IEnumerable<IProtoFieldAccessor> fields,
            ITypeCore typeCore, Dictionary<IProtoFieldAccessor, LocalBuilder> childProxies)
        {
            IL = il;

            ChildProxies = childProxies ?? new Dictionary<IProtoFieldAccessor, LocalBuilder>();

            var _getProtoProxy = typeof(IProtoProvider).GetMethod(nameof(IProtoProvider.GetProtoProxy));
            var protoDynBase = typeof(ProtoDynamicBase);
            var _proxyProviderField = protoDynBase.GetField("_proxyProvider", Const.NonPublic);

            if (ChildProxies.Count > 0)
                return;

            foreach (var field in fields)
            {
                switch (field.FieldAction)
                {
                    case ProtoFieldAction.ChildObject:
                        var bldr = CreateLocalProxy(il, field.Type, 
                            _getProtoProxy, _proxyProviderField);

                        ChildProxies[field] = bldr;
                        break;

                    case ProtoFieldAction.ChildObjectArray:
                    case ProtoFieldAction.ChildObjectCollection:
                        var germane = typeCore.GetGermaneType(field.Type);
                        var bldr2 = CreateLocalProxy(il, germane, 
                            _getProtoProxy, _proxyProviderField);

                        ChildProxies[field] = bldr2;

                        break;
                }
            }
        }

        private static LocalBuilder CreateLocalProxy(ILGenerator il, Type germane,
            MethodInfo _getProtoProxy, FieldInfo _proxyProviderField)
        {
            var proxyType = typeof(IProtoProxy<>).MakeGenericType(germane);

            var localProxyRef = il.DeclareLocal(proxyType);

            var getProxyInstance = _getProtoProxy.MakeGenericMethod(germane);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, _proxyProviderField);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Callvirt, getProxyInstance);
            
            //var localProxyRef = _proxyProvider.GetProtoProxy<T>(false);
            il.Emit(OpCodes.Stloc, localProxyRef);

            //var streamSetter = proxyType.SetterOrDie(nameof(IProtoProxy<Object>.OutStream));


            //il.Emit(OpCodes.Ldloc, localProxyRef);
            
            //il.Emit(OpCodes.Ldarg_1);
            //il.Emit(OpCodes.Callvirt, streamSetter);

            return localProxyRef;
        }
    }
}
