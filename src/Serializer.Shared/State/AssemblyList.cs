using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class AssemblyList : IAssemblyList
    {
        static AssemblyList()
        {
            _assembliesByFileName = new ConcurrentDictionary<String, Assembly>(
                StringComparer.OrdinalIgnoreCase);
            _assembliesByName = new ConcurrentDictionary<String, Assembly>(
                StringComparer.OrdinalIgnoreCase);

            var appDomain = AppDomain.CurrentDomain;
            appDomain.AssemblyLoad += OnAssemblyLoaded;

            foreach (var asm in GetRunning())
            {
                Add(asm);
            }
        }

        public Boolean TryGetAssemblyByFileName(String fileName,
                                                out Assembly assembly)
        {
            return _assembliesByFileName.TryGetValue(fileName, out assembly);
        }

        public bool TryGetAssemblyByName(String name,
                                         out Assembly assembly)
        {
            return _assembliesByName.TryGetValue(name, out assembly);
        }

        public Type? TryGetConcreteType(Type interfaceType)
        {
            foreach (var asm in GetAll())
            {
                foreach (var type in asm.GetTypes())
                {
                    if (interfaceType.IsAssignableFrom(type))
                        return type;
                }
            }


            return default;
        }

        public IEnumerable<Assembly> GetAll()
        {
            return _assembliesByFileName.Values.ToArray();
        }

        private static void Add(Assembly assembly)
        {
            if (!IsAssemblyUsable(assembly))
                return;

            var asFile = new FileInfo(assembly.Location);
            var myNameIs = assembly.GetName().Name;

            _assembliesByFileName.TryAdd(asFile.Name, assembly);
            _assembliesByName.TryAdd(myNameIs, assembly);
        }


        /// <summary>
        ///     does not check or modify the cache
        /// </summary>
        private static IEnumerable<Assembly> GetRunning()
        {
            var sended = new HashSet<Assembly>();


            foreach (var dll in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!IsAssemblyUsable(dll))
                    continue;

                if (!sended.Add(dll))
                    continue;

                yield return dll;
            }
        }

        private static Boolean IsAssemblyUsable(Assembly dll)
        {
            return !dll.IsDynamic && !String.IsNullOrWhiteSpace(dll.Location);
        }

        private static void OnAssemblyLoaded(Object sender,
                                             AssemblyLoadEventArgs args)
        {
            Add(args.LoadedAssembly);
        }


        private static readonly ConcurrentDictionary<String, Assembly> _assembliesByFileName;
        private static readonly ConcurrentDictionary<String, Assembly> _assembliesByName;
    }
}
