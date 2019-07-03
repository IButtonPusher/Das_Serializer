using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Das.Serializer;

namespace Serializer.Core
{
    public class AssemblyList : IAssemblyList
    {
        private static readonly ConcurrentDictionary<String, Assembly> _actualAssemblies;

        static AssemblyList()
        {
            
            _actualAssemblies = new ConcurrentDictionary<string, Assembly>(
                StringComparer.OrdinalIgnoreCase);
        }

        public bool TryGetAssembly(string name, out Assembly assembly)
        {
            if (_actualAssemblies.TryGetValue(name, out assembly))
                return true;

            if (TryGetRunning(name, out assembly))
                return true;

            if (TryGetRunningAndDependencies(name, out assembly))
                return true;

            assembly = default;
            return false;
        }

        private static Boolean TryGetRunning(String name, out Assembly assembly)
        {
            var running = GetRunning();
            assembly = running.FirstOrDefault(n => AreEqual(name, n));
            return assembly != null;
        }

        private static Boolean TryGetRunningAndDependencies(String name, out Assembly assembly)
        {
            var running = GetRunningAndDependencies();
            assembly = running.FirstOrDefault(n => AreEqual(name, n));
            return assembly != null;
        }

        private static Boolean AreEqual(String name, Assembly assembly) =>
            !assembly.IsDynamic && assembly.CodeBase.EndsWith(name,
                StringComparison.OrdinalIgnoreCase);

        private static IEnumerable<Assembly> GetRunning()
        {
            foreach (var dll in AppDomain.CurrentDomain.GetAssemblies())
            {
                Add(dll);
                yield return dll;
            }
        }

        private static IEnumerable<Assembly> GetRunningAndDependencies()
        {
            var sended = new HashSet<Assembly>();

            foreach (var dll in GetRunning())
            {
                if (sended.Add(dll))
                    yield return dll;
            }

            foreach (var sent in sended)
            {
                foreach (var ass in sent.GetReferencedAssemblies())
                {
                    if (_actualAssemblies.ContainsKey(ass.FullName))
                        continue;
                    var asm = Assembly.Load(ass);
                    Add(asm);
                    yield return asm;

                }
            }
        }

        public IEnumerable<Assembly> GetAll()
        {
            var sended = new HashSet<Assembly>();

            var allKnown = _actualAssemblies.Values.ToArray();

            foreach (var known in allKnown)
            {
                sended.Add(known);
                yield return known;
            }

            foreach (var runningAndNeeded in GetRunningAndDependencies())
            {
                if (sended.Add(runningAndNeeded))
                    yield return runningAndNeeded;
            }
        }

        private static void Add(Assembly assembly)
        {
            if (assembly.IsDynamic)
                return;

            var asFile = new FileInfo(assembly.Location);
           
                _actualAssemblies.TryAdd(asFile.Name, assembly);
        }
    }
}
