using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IPropertyInfo : INamedField
    {
        MethodInfo GetMethod { get; }

        MethodInfo? SetMethod { get; }

        TypeCode TypeCode { get; }
    }
}
