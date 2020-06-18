using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Das.Serializer;

using Das.Extensions;
using Das.Serializer.Objects;
using System.ComponentModel;

namespace Das.Types
{
    public class DasTypeBuilder : TypeCore, IDynamicTypes
    {
        internal DasTypeBuilder(ISerializerSettings settings, ITypeManipulator typeManipulator,
            IObjectManipulator objectManipulator) : base(settings)
        {
            _typeManipulator = typeManipulator;
            _objectManipulator = objectManipulator;
        }

        static DasTypeBuilder()
        {
            _codeGenerator = new DasCodeGenerator("DasSerializerTypes", "DAS_MODULE",
                AssemblyBuilderAccess.Run);
            _createdTypes = new ConcurrentDictionary<String, Type>();
            _createdDTypes = new ConcurrentDictionary<String, DasType>();
            _lockDynamic = new Object();
        }

        private readonly ITypeManipulator _typeManipulator;
        private readonly IObjectManipulator _objectManipulator;

        private static readonly ConcurrentDictionary<String, DasType> _createdDTypes;
        
        private static readonly ConcurrentDictionary<String, Type> _createdTypes;
        private static readonly DasCodeGenerator _codeGenerator;

        private static readonly Object _lockDynamic;

        private const MethodAttributes AccessorAttributes = 
            MethodAttributes.Public | MethodAttributes.SpecialName |
            MethodAttributes.HideBySig | MethodAttributes.Virtual;

        public Type GetDynamicImplementation(Type interfaceType)
        {
            var uniqueProps = new HashSet<String>();
            var allProps = _typeManipulator.GetPublicProperties(interfaceType);
            var propTypes = new List<DasProperty>();

            var allEvents = interfaceType.GetEvents();

            foreach (var pi in allProps)
            {
                if (!uniqueProps.Add(pi.Name))
                    continue;
                var cooked = new DasProperty(pi.Name, pi.PropertyType,
                    new DasAttribute[0]);
                propTypes.Add(cooked);
            }

            var buildType = GetDynamicType($"_{interfaceType}", propTypes.ToArray(),
                false, allEvents, null, interfaceType);

            return buildType.ManagedType;
        }

        public Boolean TryGetDynamicType(String clearName, out Type type)
            => _createdTypes.TryGetValue(clearName, out type);

        public Boolean TryGetFromAssemblyQualifiedName(String assemblyQualified, out Type type)
        {
            type = _createdTypes.Values.FirstOrDefault(t =>
                t.AssemblyQualifiedName == assemblyQualified);
            return type != null;
        }


        /// <summary>
        /// Returns the type along with property/method delegates.  Results are cached.
        /// </summary>
        /// <param name="typeName">The returned type may not get this exact name if a type with 
        /// the same name was created/invalidated</param>
        /// <param name="properties">List of properties to be added to the type.  Properties
        /// from an abstract base type or implemented interface(s) are added without specifying them here</param>
        /// <param name="isCreatePropertyDelegates">Specifies whether the PublicGetters
        /// and PublicSetters properties should have delegates to quickly get/set values
        /// for properties.</param>
        /// <param name="events">public events to be published by the Type</param>
        /// <param name="methodReplacements">For interface implementations, the methods
        /// are created but they return default primitives or null references</param>
        /// <param name="parentTypes">Can be a single unsealed class and/or 1-N interfaces</param>
        public IDynamicType GetDynamicType(String typeName, IEnumerable<DasProperty> properties,
            Boolean isCreatePropertyDelegates, IEnumerable<EventInfo> events,
            IDictionary<MethodInfo, MethodInfo>? methodReplacements,
            params Type[] parentTypes)
            => GetDynamicTypeImpl(typeName, properties, isCreatePropertyDelegates,
                methodReplacements, events, parentTypes);


        private DasType GetDynamicTypeImpl(String typeName, 
            IEnumerable<DasProperty> properties,
            Boolean isCreatePropertyDelegates, 
            IDictionary<MethodInfo, MethodInfo>? methodReplacements,
            IEnumerable<EventInfo> events,
            params Type[] parentTypes)
        {
            DasType dt;
            Type created;

            var props = properties.ToArray();

            lock (_lockDynamic)
            {
                if (_createdDTypes.TryGetValue(typeName, out var val))
                    return val;

                created = GetDynamicType(typeName, methodReplacements, props, events, 
                    parentTypes);

                //set our fake type as the ManagedType which acts like a real type but can still be
                //identified as dynamic/anonymous etc
                dt = new DasType(created, props);

                _createdDTypes.TryAdd(typeName, dt);
            }

            if (!isCreatePropertyDelegates)
                return dt;

            foreach (var prop in props)
            {
                var propInfo = created.GetProperty(prop.Name);
                dt.PublicSetters.Add(prop.Name, _typeManipulator.CreateSetMethod(propInfo));
                dt.PublicGetters.Add(prop.Name, _typeManipulator.CreatePropertyGetter(created, propInfo));
            }

            return dt;
        }


        /// <summary>
        /// Returns the type cached if it exists, builds/caches it otherwise
        /// </summary>
        /// <param name="typeName">The name of the type to be created</param>
        /// <param name="methodReplacements">or interface implementations, the methods
        /// are created but they return default primitives or null references</param>
        /// <param name="properties">properties from parent types do not need to be 
        /// specified</param>
        /// <param name="events">public events to be published by the Type</param>
        /// <param name="parentTypes">Can contain maximum one class and any amount of 
        /// interfaces</param>
        /// <returns></returns>
        public Type GetDynamicType(String typeName,
            IDictionary<MethodInfo, MethodInfo>? methodReplacements, 
            IEnumerable<DasProperty> properties, IEnumerable<EventInfo> events,
            params Type[] parentTypes)
        {
            if (_createdTypes.TryGetValue(typeName, out var val))
                return val;

            lock (_lockDynamic)
            {
                var typeBuilder = _codeGenerator.GetTypeBuilder(typeName);

                typeBuilder.DefineDefaultConstructor(
                    MethodAttributes.Public | MethodAttributes.SpecialName |
                    MethodAttributes.RTSpecialName);

                var addedMethods = new HashSet<String>();
                var addedProperties = new HashSet<String>();
                var addedEvents = new HashSet<String>();

                //abstract before interfaces in case an abstract already implements 
                //items from the interface
                foreach (var type in parentTypes.OrderBy(t => t.IsAbstract).ThenByDescending(t => t.IsClass))
                {
                    ImplementParent(typeBuilder, type, addedProperties, methodReplacements,
                        addedMethods,addedEvents);
                }

                if (properties != null)
                {
                    foreach (var kvp in properties)
                    {
                        var strKey = kvp.Name;

                        if (addedProperties.Contains(strKey))
                            continue;
                        CreateProperty(typeBuilder, kvp);
                    }
                }

                foreach (var eve in events)
                {
                    if (addedEvents.Contains(eve.Name))
                        continue;
                    
                    CreateEvent(typeBuilder, eve);
                }

                var created = typeBuilder.CreateType();
                _createdTypes.AddOrUpdate(typeName, created, (k, v) => created);
                return created;
            }
        }

        public DasAttribute[] Copy(IEnumerable<Object> attributes)
        {
            foreach (var o in attributes)
            {
                var valu = new ValueNode(o);
                var attr = new DasAttribute(o.GetType());

                foreach (var propVal in _objectManipulator.GetPropertyResults(valu, Settings))
                    attr.PropertyValues.Add(propVal.Name, propVal.Value!);
            }

            return new DasAttribute[0];
        }

        private void ImplementParent(TypeBuilder typeBuilder,
            Type interfaceType, ISet<String> addedProperties,
            IDictionary<MethodInfo, MethodInfo>? methodReplacements,
            ISet<String> addedMethods, ISet<String> addedEvents)
        {
            foreach (var prop in GetPublicProperties(interfaceType))
            {
                if (addedProperties.Contains(prop.Name))
                    continue;

                var dasProp = new DasProperty(prop.Name, prop.PropertyType,
                    Copy(prop.GetCustomAttributes(true)));

                if (!interfaceType.IsClass || IsAbstract(prop))
                    CreateProperty(typeBuilder, dasProp);
                addedProperties.Add(prop.Name);
            }

            foreach (var eve in interfaceType.GetEvents(BindingFlags.Public |
                                                        BindingFlags.Instance |
                                                        BindingFlags.FlattenHierarchy))
            {
                if (addedEvents.Contains(eve.Name))
                    continue;
                
                CreateEvent(typeBuilder, eve);
            }

            foreach (var method in _typeManipulator.GetInterfaceMethods(interfaceType))
            {
                if (!method.IsAbstract && !method.IsVirtual && !interfaceType.IsInterface)
                    continue;
                if (method.IsSpecialName)
                    continue;

                var describe = DescribeMethod(method);

                if (addedMethods.Contains(describe))
                    continue;

                if (methodReplacements != null &&
                    methodReplacements.TryGetValue(method, out var meths))
                    CreateMethod(typeBuilder, meths, method);
                else if (!interfaceType.IsClass || method.IsAbstract)
                    CreateMethod(typeBuilder, method);
                addedMethods.Add(describe);
            }


            if (!interfaceType.IsClass)
                typeBuilder.AddInterfaceImplementation(interfaceType);
            else
                typeBuilder.SetParent(interfaceType);
        }

        private static String DescribeMethod(MethodBase meth)
        {
            return $"{meth.Name}{meth.GetParameters().Select(p => p.ParameterType).ToString(Const.Comma)}";
        }


        #region private implementation

     

        private static void CreateMethod(TypeBuilder tb, MethodInfo meth,
            MethodInfo? replacing = null)
        {
            var returnType = meth.ReturnType;
            var methName = replacing?.Name ?? meth.Name;
            if (replacing == null)
                replacing = meth;

            var argumentTypes = new List<Type>();
            foreach (var parameterInfo in meth.GetParameters())
                argumentTypes.Add(parameterInfo.ParameterType);

            // Define the method
            var methodBuilder = tb.DefineMethod
            (methName, MethodAttributes.Public |
                       MethodAttributes.Virtual, returnType, argumentTypes.ToArray());

            if (replacing.IsGenericMethod)
            {
                var gArgs = replacing.GetGenericArguments();
                var gArgNames = gArgs.Select(a => a.Name).ToArray();
                methodBuilder.DefineGenericParameters(gArgNames);

                var arr = argumentTypes.ToArray();
                methodBuilder.SetParameters(arr);
            }

            var il = methodBuilder.GetILGenerator();

            // If there's a return type, create a default (preferably not null) value to return
            if (returnType != typeof(void))
            {
                var isRetted = false;

                if (!IsLeaf(returnType, true))
                {
                    var ctor = returnType.GetConstructor(new Type[0]);
                    if (ctor != null)
                    {
                        il.Emit(OpCodes.Newobj, ctor);
                        isRetted = true;
                    }
                }

                if (!isRetted)
                {
                    //should return null or default (0/false) for primis
                    var localBuilder = il.DeclareLocal(returnType);
                    il.Emit(OpCodes.Ldloc, localBuilder);
                }
            }

            il.Emit(OpCodes.Ret);
            tb.DefineMethodOverride(methodBuilder, replacing);
        }

        private static void CreateEvent(TypeBuilder tb, EventInfo eve)
        {
            var eventName = eve.Name;
            var eventType = eve.EventHandlerType;

            var fieldBuilder = tb.DefineField("_" + eventName, eventType!, FieldAttributes.Private);
            var theEvent = tb.DefineEvent(eve.Name, EventAttributes.None, eventType);

            var addMethod = tb.DefineMethod($"add_{eventName}",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                CallingConventions.Standard | CallingConventions.HasThis,
                typeof(void),
                new[] { eventType });
            var generator = addMethod.GetILGenerator();
            var combine = typeof(Delegate).GetMethod("Combine", 
                new[] { typeof(Delegate), typeof(Delegate) }) ?? throw new MissingMethodException();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, fieldBuilder);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Call, combine);
            generator.Emit(OpCodes.Castclass, eventType);
            generator.Emit(OpCodes.Stfld, fieldBuilder);
            generator.Emit(OpCodes.Ret);
            theEvent.SetAddOnMethod(addMethod);



            var removeMethod = tb.DefineMethod($"remove_{eventName}",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                CallingConventions.Standard | CallingConventions.HasThis,
                typeof(void),
                new[] { eventType });
            var remove = typeof(Delegate).GetMethod("Remove", new[] 
                             { typeof(Delegate), typeof(Delegate) })
            ?? throw new MissingMethodException();
            generator = removeMethod.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, fieldBuilder);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Call, remove);
            generator.Emit(OpCodes.Castclass, typeof(PropertyChangedEventHandler));
            generator.Emit(OpCodes.Stfld, fieldBuilder);
            generator.Emit(OpCodes.Ret);
            theEvent.SetRemoveOnMethod(removeMethod);
        }

        private static void CreateProperty(TypeBuilder tb, DasProperty prop)
        {
            var propBuilder = CreateProperty(tb, prop.Name, prop.Type, true, out _);

            if (prop.Attributes?.Length > 0)
                AddAttributes(propBuilder, prop.Attributes);
        }

        public static PropertyBuilder CreateProperty(TypeBuilder tb, 
            String propertyName, Type propertyType, Boolean addSetter,
            out FieldInfo backingfield)
        {
            backingfield = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            var propBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault,
                propertyType, null);
            var getPropMthdBldr = tb.DefineMethod("get_" + propertyName,
                AccessorAttributes,
                propertyType, Type.EmptyTypes);
            var getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, backingfield);
            getIl.Emit(OpCodes.Ret);

            propBuilder.SetGetMethod(getPropMthdBldr);

            if (addSetter)
            {
                var setPropMthdBldr =
                    tb.DefineMethod("set_" + propertyName,
                        AccessorAttributes,
                        null, new[] {propertyType});

                var setIl = setPropMthdBldr.GetILGenerator();
                var modifyProperty = setIl.DefineLabel();
                var exitSet = setIl.DefineLabel();

                setIl.MarkLabel(modifyProperty);
                setIl.Emit(OpCodes.Ldarg_0);
                setIl.Emit(OpCodes.Ldarg_1);
                setIl.Emit(OpCodes.Stfld, backingfield);

                setIl.Emit(OpCodes.Nop);
                setIl.MarkLabel(exitSet);
                setIl.Emit(OpCodes.Ret);

                propBuilder.SetSetMethod(setPropMthdBldr);
            }


            return propBuilder;
        }

        private static void AddAttributes(PropertyBuilder propBuilder,
            IEnumerable<DasAttribute> attributes)
        {
            foreach (var att in attributes)
            {
                CustomAttributeBuilder builder;

                var types = new List<Type>();
                foreach (var cVal in att.ConstructionValues)
                {
                    types.Add(cVal.GetType());
                }

                var prms = types.ToArray();
                var ctor = att.Type.GetConstructor(prms);

                if (ctor == null)
                    return;

                if (att.PropertyValues.Count == 0)
                    builder = new CustomAttributeBuilder(ctor, att.ConstructionValues);
                
                else
                {
                    var props = new List<PropertyInfo>();

                    foreach (var kvp in att.PropertyValues)
                    {
                        var pi = att.Type.GetProperty(kvp.Key);
                        props.Add(pi);
                    }

                    builder = new CustomAttributeBuilder(ctor, att.ConstructionValues,
                        props.ToArray(), att.PropertyValues.Values.ToArray());
                }

                propBuilder.SetCustomAttribute(builder);
            }
        }

        #endregion
    }
}