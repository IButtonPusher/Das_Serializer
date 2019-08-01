using System;
using System.Collections.Generic;
using System.Reflection;

namespace Das.Serializer
{
    public interface IDynamicTypes
    {
        IDynamicType GetDynamicType(String typeName, IList<DasProperty> properties,
            Boolean isCreatePropertyDelegates, Dictionary<MethodInfo, MethodInfo> methodReplacements,
            params Type[] parentTypes);

        Type GetDynamicType(String typeName,
            Dictionary<MethodInfo, MethodInfo> methodReplacements, IList<DasProperty> properties,
            params Type[] parentTypes);

        Type GetDynamicImplementation(Type baseInterface);

        Boolean TryGetDynamicType(String clearName, out Type type);

        Boolean TryGetFromAssemblyQualifiedName(String assemblyQualified, out Type type);

        ///// <summary>
        ///// Doesn't delete the types but creates a new module and doesn't use the old module
        ///// for cache lookups
        ///// </summary>
        //void InvalidateDynamicTypes();
    }
}
