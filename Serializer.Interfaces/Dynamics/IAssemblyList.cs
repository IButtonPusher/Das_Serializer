using System;
using System.Collections.Generic;
using System.Reflection;

namespace Das.Serializer
{
    /// <summary>
    /// To find aspecific Assembly or to iterate all in the current app domain
    /// </summary>
    public interface IAssemblyList
    {
        Boolean TryGetAssembly(String name, out Assembly assembly);

        IEnumerable<Assembly> GetAll();
    }
}
