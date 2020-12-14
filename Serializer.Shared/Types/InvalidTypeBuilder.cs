#if !GENERATECODE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Das.Serializer;

namespace Das.Types
{
    public class InvalidTypeBuilder : IDynamicTypes
    {
        private readonly IAssemblyList _assemblyList;

        public InvalidTypeBuilder(IAssemblyList assemblyList)
        {
            _assemblyList = assemblyList;
        }

        static InvalidTypeBuilder()
        {
            _abstractImplementations = new ConcurrentDictionary<Type, Type>();
        }
        private static readonly ConcurrentDictionary<Type, Type> _abstractImplementations;

        public Type GetDynamicImplementation(Type baseInterface)
        {
            if (_abstractImplementations.TryGetValue(baseInterface, out var found))
                return found;

            found = _assemblyList.TryGetConcreteType(baseInterface);
            if (found != null)
            {
                _abstractImplementations[baseInterface] = found;
                return found;
            }

            throw new NotSupportedException();
        }

        public IPropertyType GetDynamicType(String typeName, 
                                            IEnumerable<DasProperty> properties, 
                                            Boolean isCreatePropertyDelegates,
                                            IEnumerable<EventInfo> events, 
                                            IDictionary<MethodInfo, MethodInfo> methodReplacements, 
                                            params Type[] parentTypes)
        {
            throw new NotSupportedException();
        }

        public Type GetDynamicType(String typeName, 
                                   IDictionary<MethodInfo, MethodInfo> methodReplacements, 
                                   IEnumerable<DasProperty> properties, IEnumerable<EventInfo> events,
                                   params Type[] parentTypes)
        {
            throw new NotSupportedException();
        }

        public bool TryGetDynamicType(String clearName, 
                                      out Type? type)
        {
            type = default;
            return false;
        }

        public bool TryGetFromAssemblyQualifiedName(String assemblyQualified, 
                                                    out Type type)
        {
            type = default!;
            return false;
        }
    }
}

#endif