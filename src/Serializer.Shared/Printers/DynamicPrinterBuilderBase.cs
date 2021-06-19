#if GENERATECODE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Das.Serializer.Types;

namespace Das.Serializer.Printers
{
    public abstract class DynamicPrinterBuilderBase
    {
        

        protected DynamicPrinterBuilderBase(ITypeInferrer typeInferrer,
                                            INodeTypeProvider nodeTypes,
                                            IObjectManipulator objectManipulator,
                                            ITypeManipulator typeManipulator,
                                            ModuleBuilder moduleBuilder)
        {
            _typeInferrer = typeInferrer;
            _nodeTypes = nodeTypes;
            _objectManipulator = objectManipulator;
            _typeManipulator = typeManipulator;

            var asmName = new AssemblyName("PRINT.Stuff");
            var access = AssemblyBuilderAccess.RunAndSave;
            _moduleBuilder = moduleBuilder;
            //_asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, access);
            //_moduleBuilder = _asmBuilder.DefineDynamicModule(AssemblyName, SaveFile);
        }

        public Type BuildProxyType<TMany, TFew, TWriter>(Type type,
                                                         ISerializerSettings settings)
            where TMany : IEnumerable<TFew>
            where TWriter : IRemunerable<TMany, TFew>
        {
            var typeName = type.FullName!.Replace(".", "_") ?? throw new InvalidOperationException();

            var bldr = _moduleBuilder.DefineType(typeName,
                TypeAttributes.Public | TypeAttributes.Class);

            var invariantCulture = bldr.DefineField("_invariantCulture", typeof(CultureInfo),
                FieldAttributes.Static | FieldAttributes.Private);

            BuildStaticConstructor(bldr, invariantCulture);


            var argTypes = new[] {type, typeof(TWriter)};


            var mAttribs = MethodAttributes.Public |
                           MethodAttributes.HideBySig |
                           MethodAttributes.Virtual |
                           MethodAttributes.CheckAccessOnOverride |
                           MethodAttributes.Final;

            

            var dynamicMethod = bldr.DefineMethod("Print",
                mAttribs, typeof(void), argTypes);



            //var dynamicMethod = new DynamicMethod("PRINT$_" + type.Name +
            //    settings.GetHashCode(), typeof(void), argTypes);//, ownerType);

            var il = dynamicMethod.GetILGenerator();

            var nodeType = _nodeTypes.GetNodeType(type);

            switch (nodeType)
            {
                case NodeTypes.Collection:
                    
                    PrintObjectCollection(type, il, settings);
                    
                    break;

                case NodeTypes.Object:
                case NodeTypes.PropertiesToConstructor:
                    OpenObject(type, il, settings);
                    PrintPropertyCollection(type, il, settings, invariantCulture);
                    CloseObject(il, settings);
                    break;

                default:
                    throw new InvalidOperationException();
            }

            il.Emit(OpCodes.Ret);

            var parentInterface = typeof(ISerializerTypeProxy<,,,>).MakeGenericType(type, typeof(TMany), 
                typeof(TFew), typeof(TWriter));

            bldr.AddInterfaceImplementation(parentInterface);

            return bldr.CreateType();
        }

        //#if DEBUG

        //public void DumpProxies()
        //{
        //    #if NET45 || NET40
        //    //if (Interlocked.Increment(ref _dumpCount) > 1)
        //    //{
        //    //    Debug.WriteLine("WARNING:  Proxies already dumped");
        //    //    return;
        //    //}

        //    _asmBuilder.Save("dynamicPrintTest.dll");
        //    #endif
        //}

        //#endif

        private static void BuildStaticConstructor(TypeBuilder bldr,
                                                   FieldInfo invariantCulture)
        {
            var cctor = bldr.DefineConstructor(MethodAttributes.Static,
                CallingConventions.Standard, Type.EmptyTypes);

            var il = cctor.GetILGenerator();
            var getInvariant = typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture), 
                BindingFlags.Static | BindingFlags.Public)!.GetMethod;
            il.Emit(OpCodes.Call, getInvariant);
            il.Emit(OpCodes.Stsfld, invariantCulture);

            il.Emit(OpCodes.Ret);

            var ctor = bldr.DefineConstructor(MethodAttributes.Public,
                CallingConventions.Standard, Type.EmptyTypes);

            il = ctor.GetILGenerator();
            il.Emit(OpCodes.Ret);


        }

        private void PrintPropertyCollection(Type type,
                                             ILGenerator il,
                                             ISerializerSettings settings,
                                             FieldInfo invariantCulture)
        {
            var typeStruct = _typeManipulator.GetTypeStructure(type);
            var properties = typeStruct.Properties;

            for (var c = 0; c < properties.Length; c++)
            {
                var prop = properties[c];
                PrintProperty(prop, il, c, settings, invariantCulture);
            }
        }

        protected abstract void PrintProperty(IPropertyAccessor prop,
                                              ILGenerator il,
                                              Int32 index,
                                              ISerializerSettings settings,
                                              FieldInfo invariantCulture);

        protected abstract void PrintPrimitive(Type primitiveType,
                                               ILGenerator il,
                                               ISerializerSettings settings,
                                               Action<ILGenerator> loadPrimitiveValue,
                                               FieldInfo invariantCulture);

        protected abstract void PrintFallback(IPropertyAccessor prop,
                                               ILGenerator il,
                                               ISerializerSettings settings,
                                               Action<ILGenerator> loadFallbackValue,
                                               FieldInfo invariantCulture);

        protected abstract void OpenObject(Type type,
                                              ILGenerator il,
                                              ISerializerSettings settings);

        protected abstract void CloseObject(ILGenerator il,
                                            ISerializerSettings settings);


        protected virtual void PrintPropertyValue(IPropertyAccessor prop,
                                                  ILGenerator il,
                                                  ISerializerSettings settings,
                                                  FieldInfo invariantCulture)
        {
            var nodeType = _nodeTypes.GetNodeType(prop.PropertyType);

            switch (nodeType)
            {   
                case NodeTypes.Primitive:
                    PrintPrimitive(prop.PropertyType, il, settings, 
                        g => GetPropertyValue(prop, g), invariantCulture);
                    break;

                case NodeTypes.Fallback:
                    PrintFallback(prop, il, settings, 
                        g => GetPropertyValue(prop, g), invariantCulture);
                    break;

                case NodeTypes.Object:
                    break;
                case NodeTypes.Collection:
                    break;
                case NodeTypes.PropertiesToConstructor:
                    break;
                case NodeTypes.Dynamic:
                    break;
                case NodeTypes.StringConvertible:
                    break;

                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected void GetPropertyValue(IPropertyAccessor prop,
                                        ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, prop.PropertyInfo.GetMethod);
        }

        private void PrintObjectCollection(Type type,
                                           ILGenerator il,
                                           ISerializerSettings settings)
        {

        }

        protected readonly ITypeInferrer _typeInferrer;
        protected readonly INodeTypeProvider _nodeTypes;
        private readonly IObjectManipulator _objectManipulator;
        protected readonly ITypeManipulator _typeManipulator;
        
        //private readonly AssemblyBuilder _asmBuilder;
        private readonly ModuleBuilder _moduleBuilder;

//        private readonly FieldInfo _invariantCulture;

        private const string AssemblyName = "PRINT.Stuff";
        private static readonly String SaveFile = $"{AssemblyName}.dll";
    }
}

#endif