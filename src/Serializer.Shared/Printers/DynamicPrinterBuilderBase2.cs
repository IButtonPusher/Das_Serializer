#if GENERATECODE

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Serializer.CodeGen;
using Das.Serializer.Properties;
using Das.Serializer.State;
using Das.Serializer.Types;
using Reflection.Common;

// ReSharper disable All

namespace Das.Serializer.Printers
{
    public abstract class DynamicPrinterBuilderBase2<TResult, TField, TState> : BaseDynamicProvider<TField, TResult, TState>
        where TField : IPropertyInfo, IPropertyActionAware, IIndexedProperty
        where TState : IDynamicPrintState<TField, TResult>
    {
        protected DynamicPrinterBuilderBase2(ITypeInferrer typeInferrer,
                                             INodeTypeProvider nodeTypes,
                                             ITypeManipulator typeManipulator,
                                             ModuleBuilder moduleBuilder,
                                             IInstantiator instantiator)
            : base(typeManipulator, instantiator)
        {
            _typeInferrer = typeInferrer;
            _nodeTypes = nodeTypes;
            //    _typeManipulator = typeManipulator;

            //_moduleBuilder = moduleBuilder;
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

            Type tWriter;

            var dynamicMethod = SetupPrintMethod(type, bldr, out var gWriter);
            tWriter = gWriter;

            tWriter = dynamicMethod.GetGenericArguments()[0];

            var il = dynamicMethod.GetILGenerator();

            /////////////////////////////////////////

            var printFields = GetPrintFields(type);

            var proxies = CreateProxyFields(bldr, printFields);

            var converters = BuildConstructor(bldr, proxies, printFields);

            var state = GetInitialState(type, il, tWriter, invariantCulture,
                settings, proxies, printFields, converters);



            //state.AppendChar('{');

            //if (false)
            {

                foreach (TState protoField in state)
                    /////////////////////////////////////////
                {
                    AddFieldToPrintMethod(protoField);

                    //break;
                }



            }

            state.AppendChar('}');

            /////////////////////////////////////////

            //var nodeType = _nodeTypes.GetNodeType(type);

            //switch (nodeType)
            //{
            //case NodeTypes.Collection:
            //PrintObjectCollection(type, il, settings);
            //break;

            //case NodeTypes.Object:
            //case NodeTypes.PropertiesToConstructor:
            ////OpenObject(type, il, settings);
            //var prepend = PrintPropertyCollection(type, il, settings, invariantCulture, tWriter);
            //CloseObject(il, settings, tWriter, prepend);
            //break;

            //default:
            //throw new InvalidOperationException();
            //}

            /////////////////////////////////////////

            il.Emit(OpCodes.Ret);

            var parentInterface = typeof(ISerializerTypeProxy<>).MakeGenericType(type);

            bldr.AddInterfaceImplementation(parentInterface);

            return bldr.CreateType();
        }

        protected abstract TState GetInitialState(Type dtoType,
                                                  ILGenerator il,
                                                  Type tWriter,
                                                  FieldInfo invariantCulture,
                                                  ISerializerSettings settings,
                                                  IDictionary<Type, ProxiedInstanceField> typeProxies,
                                                  IEnumerable<TField> properties,
                                                  Dictionary<TField, FieldInfo> converterFields);

        protected abstract TResult PrintProperty(IPropertyInfo prop,
                                                 ILGenerator il,
                                                 Int32 index,
                                                 ISerializerSettings settings,
                                                 FieldInfo invariantCulture,
                                                 TResult prepend,
                                                 Type tWriter);

        protected abstract TResult PrintPrimitive(Type primitiveType,
                                                  ILGenerator il,
                                                  ISerializerSettings settings,
                                                  Action<ILGenerator> loadPrimitiveValue,
                                                  FieldInfo invariantCulture,
                                                  Type tWriter);

        protected abstract TResult PrintFallback(IPropertyInfo prop,
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
                                            TResult prepend);


        protected virtual TResult PrintPropertyValue(IPropertyInfo prop,
                                                     ILGenerator il,
                                                     ISerializerSettings settings,
                                                     FieldInfo invariantCulture,
                                                     NodeTypes nodeType,
                                                     Type tWriter)
        {
            switch (nodeType)
            {
                case NodeTypes.Primitive:
                    return PrintPrimitive(prop.Type, il, settings,
                        g => GetPropertyValue(prop, g), invariantCulture, tWriter);

                case NodeTypes.Fallback:
                    return PrintFallback(prop, il, settings,
                        g => GetPropertyValue(prop, g), invariantCulture, tWriter);


                case NodeTypes.Collection:
                    PrintObjectCollection(prop.Type, il, settings);
                    break;

                case NodeTypes.PropertiesToConstructor:
                case NodeTypes.Object:
                    var prepend = PrintPropertyCollection(prop.Type, il, settings,
                        invariantCulture, tWriter);
                    CloseObject(il, settings, tWriter, prepend);
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

        protected void GetPropertyValue(IPropertyInfo prop,
                                        ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, prop.GetMethod);
        }

      

        protected static MethodBuilder SetupPrintMethod(Type dtoDtype,
                                                      TypeBuilder bldr,
                                                      out GenericTypeParameterBuilder tWriter)
        {
            var mAttribs = MethodAttributes.Public |
                           MethodAttributes.Final |
                           MethodAttributes.HideBySig |
                           MethodAttributes.Virtual |
                           MethodAttributes.NewSlot;

            var dynamicMethod = bldr.DefineMethod("Print", mAttribs);

            var genericParamNames = new[] { "TWriter" };
            var gParams = dynamicMethod.DefineGenericParameters(genericParamNames);
            tWriter = gParams[0];
            tWriter.SetInterfaceConstraints(typeof(ITextRemunerable));
            var argTypes = new[] { dtoDtype, tWriter };

            dynamicMethod.SetParameters(argTypes);
            dynamicMethod.SetReturnType(typeof(void));

            return dynamicMethod;
        }

        private Dictionary<TField, FieldInfo> BuildConstructor(TypeBuilder bldr,
                                                               Dictionary<Type, ProxiedInstanceField> proxies,
                                                               IEnumerable<TField> fields)
        {
            var converters = new Dictionary<TField, FieldInfo>();

            var convertersAdded = new Dictionary<Type, FieldInfo>();

            var ctor = bldr.DefineConstructor(MethodAttributes.Public,
                CallingConventions.Standard, new Type[]
                {
                    typeof(IProxyProvider),
                    typeof(ISerializerSettings)
                });

            var il = ctor.GetILGenerator();


            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(Object).GetDefaultConstructorOrDie());

            foreach (var kvp in proxies)
            {
                var fieldType = kvp.Value.ProxyField.FieldType;

                var gargs = fieldType.GetGenericArguments();

                if (gargs.Length != 1)
                    throw new InvalidOperationException(
                        $"{fieldType} should have exactly one generic argument");

                var garg = gargs[0];

                var getProxyInstance = GetProxyMethod.MakeGenericMethod(garg);

                il.Emit(OpCodes.Ldarg_0);
                {
                    il.Emit(OpCodes.Ldarg_1); //provider
                    il.Emit(OpCodes.Ldarg_2); //settings
                    il.Emit(OpCodes.Callvirt, getProxyInstance);
                }
                il.Emit(OpCodes.Stfld, kvp.Value.ProxyField);
            }

            foreach (var field in fields)
            {
                if (field.FieldAction == FieldAction.FallbackSerializable)
                {
                    var conv = TypeDescriptor.GetConverter(field.Type);
                    var convType = conv.GetType();

                    if (convertersAdded.TryGetValue(convType, out var convField))
                    {
                        converters.Add(field, convField);
                        continue;
                    }

                    convField = bldr.DefineField($"_{field.Name}Converter", convType,
                        FieldAttributes.Private);
                    convertersAdded.Add(convType, convField);
                    converters.Add(field, convField);

                    var emptyCtor = convType.GetConstructor(Type.EmptyTypes);

                    il.Emit(OpCodes.Ldarg_0);

                    if (emptyCtor != null)
                        il.Emit(OpCodes.Newobj, emptyCtor!);
                    else
                    {
                        var getConverter = typeof(TypeDescriptor).GetPublicStaticMethodOrDie(
                            nameof(TypeDescriptor.GetConverter), typeof(Type));

                        throw new NotImplementedException();
                        //il.Emit(OpCodes.Call, );
                    }

                    il.Emit(OpCodes.Stfld, convField);

                }
            }

            il.Emit(OpCodes.Ret);

            return converters;
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

        private TResult PrintPropertyCollection(Type type,
                                                ILGenerator il,
                                                ISerializerSettings settings,
                                                FieldInfo invariantCulture,
                                                Type tWriter)
        {
            var typeStruct = _types.GetTypeStructure(type);
            var properties = typeStruct.Properties;

            var prepend = default(TResult);

            for (var c = 0; c < properties.Length; c++)
            {
                var prop = properties[c];
                prepend = PrintProperty(prop, il, c, settings,
                    invariantCulture, prepend!, tWriter);
            }

            return prepend!;
        }

        private void PrintObjectCollection(Type type,
                                           ILGenerator il,
                                           ISerializerSettings settings)
        {
        }
        //protected readonly ITypeManipulator _typeManipulator;

        //private readonly ModuleBuilder _moduleBuilder;
        protected readonly INodeTypeProvider _nodeTypes;

        protected readonly ITypeInferrer _typeInferrer;

        protected override int GetIndexFromAttribute(PropertyInfo prop,
                                                     Int32 lastIndex)
        {
            return lastIndex + 1;
        }

        //protected override List<TField> GetProtoPrintFields(Type type)
        //{
        //    var res = new List<TField>();
        //    foreach (var prop in _types.GetPublicProperties(type))
        //    {
        //        if (TryGetFieldAccessor(prop, true, GetIndexFromAttribute, out var protoField))
        //        //if (TryGetProtoField(prop, true, out var protoField))
        //            res.Add(protoField);
        //    }

        //    return res;
        //}

    }
}

#endif
