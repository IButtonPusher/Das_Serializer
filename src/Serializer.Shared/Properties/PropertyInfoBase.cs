using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.Properties
{
    public class PropertyInfoBase : IPropertyInfo
    {
        public PropertyInfoBase(String name,
                                Type type,
                                MethodInfo getMethod,
                                MethodInfo? setMethod)
        {
            Name = name;
            Type = type;
            GetMethod = getMethod;
            SetMethod = setMethod;

            TypeCode = Type.GetTypeCode(type);
        }

        public String Name { get; }

        public Type Type { get; }

        public MethodInfo GetMethod { get; }

        public MethodInfo? SetMethod { get; }

        public TypeCode TypeCode { get; }
    }
}
