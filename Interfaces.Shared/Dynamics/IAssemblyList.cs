using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer
{
    /// <summary>
    ///     To find aspecific Assembly or to iterate all in the current app domain
    /// </summary>
    public interface IAssemblyList : IEnumerable<Assembly>
    {
        IEnumerable<Assembly> GetAll();

        Boolean TryGetAssembly(String name, out Assembly assembly);
    }
}