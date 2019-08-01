using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Das.Serializer;
using Serializer.Core;
using Das.CoreExtensions;
using Das.Serializer.Objects;
using Serializer;


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
            _codeGenerator = new DasCodeGenerator("DasSerializerTypes","DAS_MODULE",
                AssemblyBuilderAccess.Run);
            _createdTypes = new ConcurrentDictionary<string, Type>();
            _createdDTypes = new ConcurrentDictionary<string, DasType>();
            _lockDynamic = new object();
        }

        private readonly ITypeManipulator _typeManipulator;
        private readonly IObjectManipulator _objectManipulator;

        private static readonly ConcurrentDictionary<String, DasType> _createdDTypes;

        //private static AssemblyBuilder _assemblyBuilder;
        //private static ModuleBuilder _moduleBuilder;
        
        //private static Int32 _moduleIndex;
        private static readonly ConcurrentDictionary<String, Type> _createdTypes;
        private static readonly DasCodeGenerator _codeGenerator;
        
        private static readonly Object _lockDynamic;

        //private static Int32 _assemblyIndex = 1;

        //public void InvalidateDynamicTypes()
        //{
        //    if (_createdTypes.IsEmpty && _createdDTypes.IsEmpty)
        //        return;

        //        throw new NotImplementedException();
        //    //lock (_lockDynamic)
        //    //{
        //    //    if (_createdTypes.IsEmpty && _createdDTypes.IsEmpty)
        //    //        return;

        //    //    _createdTypes.Clear();
        //    //    _createdDTypes.Clear();
        //    //    _moduleBuilder = null;
        //    //    _timesBuildersConstructed = 0;
        //    //}
        //}

        //private static TypeBuilder GetTypeBuilder(String typeName)
        //{
        //    if (_moduleBuilder == null)
        //    {
        //        if (Interlocked.Increment(ref _timesBuildersConstructed) == 1)
        //        {
        //            CreateBuilders();
        //        }

        //        while (_moduleBuilder == null)
        //            Thread.Sleep(10);
        //    }

        //    var typeBuilder = _moduleBuilder.DefineType(typeName,
        //        TypeAttributes.Public |
        //        TypeAttributes.Class |
        //        TypeAttributes.AutoClass |
        //        TypeAttributes.AnsiClass |
        //        TypeAttributes.BeforeFieldInit |
        //        TypeAttributes.AutoLayout,
        //        null);
        //    return typeBuilder;
        //}

  //      private static void CreateBuilders()
		//{
		//	var typeSignature = $"DasTypes{_assemblyIndex++}";
		//	var an = new AssemblyName(typeSignature);

		//	_assemblyBuilder = _assemblyBuilder ?? AppDomain.CurrentDomain.DefineDynamicAssembly(an,
		//		AssemblyBuilderAccess.RunAndSave);

  //          var index = Interlocked.Increment(ref _moduleIndex);

		//	_moduleBuilder = _assemblyBuilder.DefineDynamicModule("DasDynamicTypes" + index);
		//}


        public Type GetDynamicImplementation(Type interfaceType)
        {
            var uniqueProps = new HashSet<String>();
            var allProps = _typeManipulator.GetPublicProperties(interfaceType);
            var propTypes = new List<DasProperty>();

            foreach (var pi in allProps)
            {
                if (!uniqueProps.Add(pi.Name))
                    continue;
                var cooked = new DasProperty(pi.Name, pi.PropertyType);
                propTypes.Add(cooked);
            }

            var buildType = GetDynamicType(
                            $"_{interfaceType}", propTypes.ToArray(),
                            false, null,
                            interfaceType);

            return buildType.ManagedType;
        }

        public bool TryGetDynamicType(string clearName, out Type type)
            => _createdTypes.TryGetValue(clearName, out type);

        public bool TryGetFromAssemblyQualifiedName(string assemblyQualified, out Type type)
        {
            type = _createdTypes.Values.FirstOrDefault(t => 
            t.AssemblyQualifiedName == assemblyQualified);
            return type != null;
        }

        

        /// <summary>
        /// Returns the type along with property/method delegates.  Results are cached. If a type needs
        /// to be redefined call InvalidateType
        /// </summary>
        /// <param name="typeName">The returned type may not get this exact name if a type with 
        /// the same name was created/invalidated</param>
        /// <param name="properties">List of properties to be added to the type.  Properties
        /// from an abstract base type or implemented interface(s) are added without specifying them here</param>
        /// <param name="isCreatePropertyDelegates">Specifies whether the PublicGetters
        /// and PublicSetters properties should have delegates to quickly get/set values
        /// for properties.</param>
        /// <param name="methodReplacements">For interface implementations, the methods
        /// are created but they return default primitives or null references</param>
        /// <param name="parentTypes">Can be a single unsealed class and/or 1-N interfaces</param>
        
        public IDynamicType GetDynamicType(String typeName, IList<DasProperty> properties,
            Boolean isCreatePropertyDelegates, 
            Dictionary<MethodInfo, MethodInfo> methodReplacements,
            params Type[] parentTypes) 
            => GetDynamicTypeImpl(typeName, properties, isCreatePropertyDelegates,
                methodReplacements, parentTypes);
        

        private DasType GetDynamicTypeImpl(String typeName, IList<DasProperty> properties,
            Boolean isCreatePropertyDelegates, Dictionary<MethodInfo, MethodInfo> methodReplacements,
            params Type[] parentTypes)
        {
            DasType dt;
            Type created;

            lock (_lockDynamic)
            {
                if (_createdDTypes.TryGetValue(typeName, out var val))
                    return val;

                created = GetDynamicType(typeName, methodReplacements, properties, parentTypes);
                
                //set our fake type as the ManagedType which acts like a real type but can still be
                //identified as dynamic/anonymous etc
                dt = new DasType(created, properties);

                _createdDTypes.TryAdd(typeName, dt);
            }

            if (!isCreatePropertyDelegates)
                return dt;

            foreach (var prop in properties)
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
        /// <param name="parentTypes">Can contain maximum one class and any amount of 
        /// interfaces</param>
        /// <returns></returns>
        public Type GetDynamicType(String typeName,
			Dictionary<MethodInfo, MethodInfo> methodReplacements, IList<DasProperty> properties,
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
                var addedProperties = new HashSet<string>();

                //abstract before interfaces in case an abstract already implements 
                //items from the interface
                foreach (var type in parentTypes.OrderBy(t => t.IsAbstract).
                    ThenByDescending(t => t.IsClass))
                {
                    ImplementParent(typeBuilder, type, addedProperties, methodReplacements,
                        addedMethods);
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

                var created = typeBuilder.CreateType();
                _createdTypes.AddOrUpdate(typeName, created, (k, v) => created);
                return created;
            }
		}

        public DasAttribute[] Copy(IEnumerable<object> attributes)
        {
            foreach (var o in attributes)
            {
                var valu = new ValueNode(o);
                var attr = new DasAttribute { Type = o.GetType() };

                foreach (var propVal in _objectManipulator.GetPropertyResults(valu, Settings))
                    attr.PropertyValues.Add(propVal.Name, propVal.Value);
            }

            return new DasAttribute[0];
        }

        private  void ImplementParent(TypeBuilder typeBuilder, 
			Type interfaceType, ISet<string> addedProperties,
			IDictionary<MethodInfo, MethodInfo> methodReplacements, 
			ISet<String> addedMethods)
		{
			foreach (var prop in GetPublicProperties(interfaceType))
			{
				if (addedProperties.Contains(prop.Name))
					continue;

				var dasProp = new DasProperty(prop.Name, prop.PropertyType)
				{
					Attributes = Copy(prop.GetCustomAttributes(true))
				};

				if (!interfaceType.IsClass || IsAbstract(prop))
					CreateProperty(typeBuilder, dasProp);
				addedProperties.Add(prop.Name);
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

		

		private void CreateMethod(TypeBuilder tb, MethodInfo meth, 
			MethodInfo replacing = null)
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

			// If there's a return type, create a default value to return
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

		private static void CreateProperty(TypeBuilder tb, DasProperty prop)
		{
			var propertyName = prop.Name;
			var propertyType = prop.Type;

			var fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

			var propBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
			var getPropMthdBldr = tb.DefineMethod("get_" + propertyName,
				MethodAttributes.Public | MethodAttributes.SpecialName |
				MethodAttributes.HideBySig | MethodAttributes.Virtual, propertyType, Type.EmptyTypes);
			var getIl = getPropMthdBldr.GetILGenerator();

			getIl.Emit(OpCodes.Ldarg_0);
			getIl.Emit(OpCodes.Ldfld, fieldBuilder);
			getIl.Emit(OpCodes.Ret);

			var setPropMthdBldr =
				tb.DefineMethod("set_" + propertyName,
				  MethodAttributes.Public |
				  MethodAttributes.SpecialName |
				  MethodAttributes.HideBySig | MethodAttributes.Virtual,
				  null, new[] { propertyType });

			var setIl = setPropMthdBldr.GetILGenerator();
			var modifyProperty = setIl.DefineLabel();
			var exitSet = setIl.DefineLabel();

			setIl.MarkLabel(modifyProperty);
			setIl.Emit(OpCodes.Ldarg_0);
			setIl.Emit(OpCodes.Ldarg_1);
			setIl.Emit(OpCodes.Stfld, fieldBuilder);

			setIl.Emit(OpCodes.Nop);
			setIl.MarkLabel(exitSet);
			setIl.Emit(OpCodes.Ret);

			propBuilder.SetGetMethod(getPropMthdBldr);
			propBuilder.SetSetMethod(setPropMthdBldr);

			if (prop.Attributes?.Length > 0)
				AddAttributes(propBuilder, prop.Attributes);
		}

		private static void AddAttributes(PropertyBuilder propBuilder, 
			DasAttribute[] attributes)
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
				{
					builder = new CustomAttributeBuilder(ctor, att.ConstructionValues);
				}
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
