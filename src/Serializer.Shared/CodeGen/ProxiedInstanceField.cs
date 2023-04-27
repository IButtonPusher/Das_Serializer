using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.CodeGen;

public class ProxiedInstanceField
{
   public ProxiedInstanceField(Type proxyType,
                               FieldInfo proxyField,
                               MethodInfo printMethod)
   {
      ProxyType = proxyType;
      ProxyField = proxyField;
      PrintMethod = printMethod;
   }

   public Type ProxyType { get; }

   public FieldInfo ProxyField { get; }

   public MethodInfo PrintMethod { get; }
}