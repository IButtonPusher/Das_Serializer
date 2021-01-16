using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Das.Serializer.Types
{
    public class NullTypeBuilder : IDynamicTypes
    {
        Type IDynamicTypes.GetDynamicImplementation(Type baseInterface)
        {
            throw new NotSupportedException();
        }

        IDynamicType IDynamicTypes.GetDynamicType(String typeName, 
                                                  IEnumerable<DasProperty> properties, 
                                                  Boolean isCreatePropertyDelegates,
                                                  IEnumerable<EventInfo> events, 
                                                  IDictionary<MethodInfo, MethodInfo> methodReplacements, 
                                                  params Type[] parentTypes)
        {
            throw new NotSupportedException();
        }

        Type IDynamicTypes.GetDynamicType(String typeName, 
                                          IDictionary<MethodInfo, MethodInfo> methodReplacements, 
                                          IEnumerable<DasProperty> properties, 
                                          IEnumerable<EventInfo> events,
                                          params Type[] parentTypes)
        {
            throw new NotSupportedException();
        }

        bool IDynamicTypes.TryGetDynamicType(String clearName, out Type? type)
        {
            type = default;
            return false;
        }

        bool IDynamicTypes.TryGetFromAssemblyQualifiedName(String assemblyQualified, out Type type)
        {
            type = default!;
            return false;
        }
    }
}
