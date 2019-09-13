using System;
using System.Collections.Generic;
using System.Reflection;

namespace Das.Serializer
{
    public interface IDynamicTypes
    {
        /// <summary>
        /// Gets a dynamic type in a wrapper that allows for properties to be accessed quickly
        /// </summary>
        IDynamicType GetDynamicType(String typeName, IEnumerable<DasProperty> properties,
            Boolean isCreatePropertyDelegates, IEnumerable<EventInfo> events,
            IDictionary<MethodInfo, MethodInfo> methodReplacements, params Type[] parentTypes);

        Type GetDynamicType(String typeName, IDictionary<MethodInfo, MethodInfo> methodReplacements, 
            IEnumerable<DasProperty> properties, IEnumerable<EventInfo> events,
            params Type[] parentTypes);

        Type GetDynamicImplementation(Type baseInterface);

        Boolean TryGetDynamicType(String clearName, out Type type);

        Boolean TryGetFromAssemblyQualifiedName(String assemblyQualified, out Type type);
    }
}