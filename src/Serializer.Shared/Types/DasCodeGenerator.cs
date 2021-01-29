#if GENERATECODE

using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class DasCodeGenerator
    {
        public DasCodeGenerator(String assemblyName,
                                String moduleName,
                                AssemblyBuilderAccess access)
        {
            _lock = new Object();
            _assemblyName = assemblyName;
            _moduleName = moduleName;
            _access = access;
        }

        private AssemblyBuilder AssemblyBuilder =>
            _assemblyBuilder ??= GetAssemblyBuilder();

        private ModuleBuilder ModuleBuilder =>
            _moduleBuilder ??= GetModuleBuilder();

        public TypeBuilder GetTypeBuilder(String typeName)
        {
            var typeBuilder = ModuleBuilder.DefineType(typeName,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout,
                null);

            return typeBuilder;
        }

        // ReSharper disable once UnusedMember.Global
        public void Save(String saveAs)
        {
            #if NET45 || NET40
            AssemblyBuilder.Save(saveAs);
            #endif
        }

        [Pure]
        private AssemblyBuilder GetAssemblyBuilder()
        {
            var access = _access;
            lock (_lock)
            {
                var asmName = new AssemblyName(_assemblyName);

                #if NET40
                return AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, access);

                #else
                return AssemblyBuilder.DefineDynamicAssembly(asmName, access);

                #endif
            }
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Boolean GetCanSave()
        {
            #if NET45
            return _access == AssemblyBuilderAccess.Save ||
                   _access == AssemblyBuilderAccess.RunAndSave;
            #else
            return false;
            #endif
        }

        [Pure]
        private ModuleBuilder GetModuleBuilder()
        {
            lock (_lock)
            {
                #if NET45 || NET40

                var canSave = GetCanSave();
                //if we will be saving to disk, create the module to be saved as well.
                if (canSave)
                    return AssemblyBuilder.DefineDynamicModule(_moduleName, _moduleName + ".netmodule");
                #endif

                return AssemblyBuilder.DefineDynamicModule(_moduleName);
            }
        }

        private readonly AssemblyBuilderAccess _access;
        private readonly String _assemblyName;
        private readonly Object _lock;
        private readonly String _moduleName;

        private AssemblyBuilder? _assemblyBuilder;
        private ModuleBuilder? _moduleBuilder;
    }
}

#endif
