#if GENERATECODE

using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Das.Serializer.Types;
// ReSharper disable All

namespace Das.Serializer.Printers
{
    public abstract class DynamicPrinterBuilderBase2<T>
    {
        protected DynamicPrinterBuilderBase2(ITypeInferrer typeInferrer,
                                             INodeTypeProvider nodeTypes,
                                             ITypeManipulator typeManipulator,
                                             ModuleBuilder moduleBuilder)
        {
            _typeInferrer = typeInferrer;
            _nodeTypes = nodeTypes;
            _typeManipulator = typeManipulator;

            _moduleBuilder = moduleBuilder;
        }

        public Type BuildProxyType(Type type,
                                   ISerializerSettings settings)
        {
            var typeName = type.FullName!.Replace(".", "_") + "2" ?? throw new InvalidOperationException();

            var bldr = _moduleBuilder.DefineType(typeName,
                TypeAttributes.Public | TypeAttributes.Class);

            var invariantCulture = bldr.DefineField("_invariantCulture", typeof(CultureInfo),
                FieldAttributes.Static | FieldAttributes.Private);

            BuildStaticConstructor(bldr, invariantCulture);

            var dynamicMethod = SetupPrintMethod(type, bldr, out var tWriter);

            var il = dynamicMethod.GetILGenerator();

            var nodeType = _nodeTypes.GetNodeType(type);

            switch (nodeType)
            {
                case NodeTypes.Collection:
                    PrintObjectCollection(type, il, settings);
                    break;

                case NodeTypes.Object:
                case NodeTypes.PropertiesToConstructor:
                    //OpenObject(type, il, settings);
                    var prepend  = PrintPropertyCollection(type, il, settings, invariantCulture, tWriter);
                    CloseObject(il, settings, tWriter, prepend);
                    break;

                default:
                    throw new InvalidOperationException();
            }

            il.Emit(OpCodes.Ret);

            var parentInterface = typeof(ISerializerTypeProxy<>).MakeGenericType(type);

            bldr.AddInterfaceImplementation(parentInterface);

            return bldr.CreateType();
        }

        private static MethodBuilder SetupPrintMethod(Type dtoDtype,
                                                      TypeBuilder bldr,
                                                      out GenericTypeParameterBuilder tWriter)
        {
            var mAttribs = MethodAttributes.Public | 
                           MethodAttributes.Final |
                           MethodAttributes.HideBySig |
                           MethodAttributes.Virtual | 
                           MethodAttributes.NewSlot;

            var dynamicMethod = bldr.DefineMethod("Print", mAttribs); 
            
            var genericParamNames = new[] {"TWriter"};
            var gParams = dynamicMethod.DefineGenericParameters(genericParamNames);
            tWriter = gParams[0];
            tWriter.SetInterfaceConstraints(typeof(ITextRemunerable));
            var argTypes = new[] {dtoDtype, tWriter};

            dynamicMethod.SetParameters(argTypes);
            dynamicMethod.SetReturnType(typeof(void));

            return dynamicMethod;
        }

        private static void BuildStaticConstructor(TypeBuilder bldr,
                                                   FieldInfo invariantCulture)
        {
            var cctor = bldr.DefineConstructor(MethodAttributes.Static,
                CallingConventions.Standard, Type.EmptyTypes);

            var il = cctor.GetILGenerator();
            var getInvariant = typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture),
                BindingFlags.Static | BindingFlags.Public)!.GetGetMethod();
            il.Emit(OpCodes.Call, getInvariant);
            il.Emit(OpCodes.Stsfld, invariantCulture);

            il.Emit(OpCodes.Ret);

            var ctor = bldr.DefineConstructor(MethodAttributes.Public,
                CallingConventions.Standard, Type.EmptyTypes);

            il = ctor.GetILGenerator();
            il.Emit(OpCodes.Ret);


        }

        private T PrintPropertyCollection(Type type,
                                             ILGenerator il,
                                             ISerializerSettings settings,
                                             FieldInfo invariantCulture,
                                             Type tWriter)
        {
            var typeStruct = _typeManipulator.GetTypeStructure(type);
            var properties = typeStruct.Properties;

            var prepend = default(T);

            for (var c = 0; c < properties.Length; c++)
            {
                var prop = properties[c];
                prepend = PrintProperty(prop, il, c, settings, 
                    invariantCulture, prepend!, tWriter);
            }

            return prepend!;
        }

        protected abstract T PrintProperty(IPropertyAccessor prop,
                                           ILGenerator il,
                                           Int32 index,
                                           ISerializerSettings settings,
                                           FieldInfo invariantCulture,
                                           T prepend,
                                           Type tWriter);

        protected abstract T PrintPrimitive(Type primitiveType,
                                            ILGenerator il,
                                            ISerializerSettings settings,
                                            Action<ILGenerator> loadPrimitiveValue,
                                            FieldInfo invariantCulture,
                                            Type tWriter);

        protected abstract T PrintFallback(IPropertyAccessor prop,
                                           ILGenerator il,
                                           ISerializerSettings settings,
                                           Action<ILGenerator> loadFallbackValue,
                                           FieldInfo invariantCulture,
                                           Type tWriter);

        protected abstract void OpenObject(Type type,
                                           ILGenerator il,
                                           ISerializerSettings settings,
                                           Type tWriter);

        protected abstract void CloseObject(ILGenerator il,
                                            ISerializerSettings settings,
                                            Type tWriter,
                                            T prepend);


        protected virtual T PrintPropertyValue(IPropertyAccessor prop,
                                               ILGenerator il,
                                               ISerializerSettings settings,
                                               FieldInfo invariantCulture,
                                               NodeTypes nodeType,
                                               Type tWriter)
        {
            switch (nodeType)
            {
                case NodeTypes.Primitive:
                    return PrintPrimitive(prop.PropertyType, il, settings,
                        g => GetPropertyValue(prop, g), invariantCulture, tWriter);

                case NodeTypes.Fallback:
                    return PrintFallback(prop, il, settings,
                        g => GetPropertyValue(prop, g), invariantCulture, tWriter);

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

            return default!;
        }

        protected void GetPropertyValue(IPropertyAccessor prop,
                                        ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, prop.PropertyInfo.GetGetMethod());
        }
      
        private void PrintObjectCollection(Type type,
                                           ILGenerator il,
                                           ISerializerSettings settings)
        {

        }

        protected readonly ITypeInferrer _typeInferrer;
        protected readonly INodeTypeProvider _nodeTypes;
        protected readonly ITypeManipulator _typeManipulator;

        private readonly ModuleBuilder _moduleBuilder;
    }
}

#endif