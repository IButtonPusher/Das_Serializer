using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Reflection.Emit;

namespace Das.Serializer
{
    public class DasCodeGenerator
    {
        public DasCodeGenerator(String assemblyName, String moduleName,
            AssemblyBuilderAccess access)
        {
            _lock = new Object();
            _assemblyName = assemblyName;
            _moduleName = moduleName;
            _access = access;
        }

        private AssemblyBuilder AssemblyBuilder =>
            _assemblyBuilder ?? (_assemblyBuilder = GetAssemblyBuilder());

        private ModuleBuilder ModuleBuilder =>
            _moduleBuilder ?? (_moduleBuilder = GetModuleBuilder());

        private AssemblyBuilder _assemblyBuilder;
        private readonly String _assemblyName;
        private ModuleBuilder _moduleBuilder;
        private readonly String _moduleName;
        private readonly AssemblyBuilderAccess _access;
        private readonly Object _lock;

        public TypeBuilder GetTypeBuilder<TBase>(String typeName)
        {
            var builder = GetTypeBuilder(typeName);
            var tType = typeof(TBase);

            if (tType.IsInterface)
                builder.AddInterfaceImplementation(tType);
            else
                builder.SetParent(tType);

            return builder;
        }

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

        [Pure]
        private AssemblyBuilder GetAssemblyBuilder()
        {
            lock (_lock)
            {
                var asmName = new AssemblyName(_assemblyName);
                return AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, _access);
            }
        }

        [Pure]
        private ModuleBuilder GetModuleBuilder()
        {
            lock (_lock)
            {
                //if we will be saving to disk, create the module to be saved as well.
                if (_access == AssemblyBuilderAccess.Save ||
                    _access == AssemblyBuilderAccess.RunAndSave)
                    return AssemblyBuilder.DefineDynamicModule(_moduleName, _moduleName + ".netmodule");

                return AssemblyBuilder.DefineDynamicModule(_moduleName);
            }
        }

        public void Save(String fileName)
        {
            _assemblyBuilder.Save(fileName);
        }
    }
}