using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Das.Serializer
{
    public class AssemblyList : IAssemblyList
    {
        private static readonly ConcurrentDictionary<String, Assembly> _actualAssemblies;
        private static readonly Object _loadLock;
        private static readonly HashSet<AssemblyName> _failedToLoad;

        static AssemblyList()
        {
            _loadLock = new Object();
            _actualAssemblies = new ConcurrentDictionary<String, Assembly>(
                StringComparer.OrdinalIgnoreCase);
            _failedToLoad = new HashSet<AssemblyName>();
        }

        public Boolean TryGetAssembly(String name, out Assembly assembly)
        {
            if (_actualAssemblies.TryGetValue(name, out assembly))
                return true;

            if (TryGetRunning(name, out assembly))
                return true;

            if (TryGetRunningAndDependencies(name, out assembly))
                return true;

            if (TryFromBinFolder(name, out assembly))
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
            var foundName = running.FirstOrDefault(n => name.Equals(n.Name));
            if (foundName != null)
                return TryLoad(foundName, out assembly);

            assembly = default;
            return false;
        }

        private static Boolean AreEqual(String name, Assembly assembly) =>
            IsAssemblyUsable(assembly) && assembly.CodeBase.EndsWith(name,
                StringComparison.OrdinalIgnoreCase);

        private static IEnumerable<Assembly> GetRunning()
        {
            var sended = new HashSet<Assembly>();
            foreach (var dll in _actualAssemblies.Values)
            {
                if (sended.Add(dll))
                    yield return dll;
            }

            foreach (var dll in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!IsAssemblyUsable(dll))
                    continue;

                if (!sended.Add(dll))
                    continue;

                Add(dll);
                yield return dll;
            }
        }

        private static Boolean IsAssemblyUsable(Assembly dll)
            => !dll.IsDynamic && !String.IsNullOrWhiteSpace(dll.Location);

        private static IEnumerable<AssemblyName> GetRunningAndDependencies()
        {
            var sended = new HashSet<AssemblyName>();

            foreach (var dll in GetRunning())
            {
                var name = dll.GetName();

                if (sended.Add(name))
                    yield return name;

                foreach (var dependency in dll.GetReferencedAssemblies())
                {
                    if (sended.Add(dependency))
                        yield return dependency;
                }
            }
        }

        private static Boolean TryLoad(AssemblyName name, out Assembly realMcKoy)
        {
            try
            {
                lock (_loadLock)
                {
                    if (_failedToLoad.Contains(name))
                        goto fail;
                }

                realMcKoy = Assembly.Load(name);
                return true;
            }
            catch
            {
                lock (_loadLock)
                    _failedToLoad.Add(name);
            }

            fail:
            realMcKoy = default;
            return false;
        }

        private static Boolean TryFromBinFolder(String name, out Assembly found)
        {
            found = default;
            var binDir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
            if (binDir == null)
            {
                return false;
            }

            var dll = binDir.GetFiles("*.dll").FirstOrDefault(d =>
                d.Name.EndsWith(name, StringComparison.OrdinalIgnoreCase));

            if (dll != null)
            {
                try
                {
                    found = Assembly.LoadFile(dll.FullName);
                }
                catch
                {
                }
            }

            return found != null;
        }

        public IEnumerable<Assembly> GetAll()
        {
            var sended = new HashSet<AssemblyName>();

            var allKnown = _actualAssemblies.Values.ToArray();

            foreach (var known in allKnown)
            {
                var name = known.GetName();
                sended.Add(name);
                yield return known;
            }

            foreach (var runningAndNeeded in GetRunningAndDependencies())
            {
                if (sended.Add(runningAndNeeded) && TryLoad(runningAndNeeded, out var asm))
                    yield return asm;
            }
        }

        private static void Add(Assembly assembly)
        {
            if (!IsAssemblyUsable(assembly))
                return;

            var asFile = new FileInfo(assembly.Location);

            _actualAssemblies.TryAdd(asFile.Name, assembly);
        }

        public IEnumerator<Assembly> GetEnumerator() => GetRunning().GetEnumerator();


        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}