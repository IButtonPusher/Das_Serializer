using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer
{
    /// <summary>
    ///     To find aspecific Assembly or to iterate all in the current app domain
    /// </summary>
    public interface IAssemblyList
    {
        /// <summary>
        ///     Returns all the assemblies that are referenced by the running process
        /// </summary>
        IEnumerable<Assembly> GetAll();

        Boolean TryGetAssemblyByFileName(String fileName,
                                         out Assembly assembly);

        Boolean TryGetAssemblyByName(String name,
                                     out Assembly assembly);

        Type? TryGetConcreteType(Type interfaceType);
    }
}
