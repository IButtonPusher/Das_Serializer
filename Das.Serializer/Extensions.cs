using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Das.Serializer;
using Das.Serializer.Objects;

// ReSharper disable UnusedMember.Global

namespace Das.Extensions
{
    public static class ExtensionMethods
    {
        private static readonly Random _random;


        private static IDynamicFacade _dynamicFacade;

        public static IDynamicFacade DynamicFacade
        {
            get => _dynamicFacade ?? (_dynamicFacade = new DefaultStateProvider());
            set => _dynamicFacade = value;
        }

        private static ISerializerSettings _settings;

        internal static ISerializerSettings Settings
        {
            get => _settings ?? (_settings = DasSettings.Default);
            set => _settings = value;
        }

        static ExtensionMethods()
        {
            _random = new Random();
        }

        public static Int32 BytesNeeded(this Type typ)
            => DynamicFacade.TypeInferrer.BytesNeeded(typ);


        public static T CastDynamic<T>(this Object o)
            => DynamicFacade.ObjectManipulator.CastDynamic<T>(o);


        public static Boolean TryCastDynamic<T>(this Object o, out T casted)
            => DynamicFacade.ObjectManipulator.TryCastDynamic(o, out casted);

        public static Boolean IsDefaultValue(this Object o)
            => DynamicFacade.TypeInferrer.IsDefaultValue(o);


        public static Object CreatePrimitiveObject(this byte[] rawValue, Type objType)
            => DynamicFacade.ObjectInstantiator.CreatePrimitiveObject(rawValue, objType);

        public static T CreatePrimitiveObject<T>(this byte[] rawValue, Type objType)
            => DynamicFacade.ObjectInstantiator.CreatePrimitiveObject<T>(rawValue, objType);

        /// <summary>
        /// if this is a generic collection of T or T[] it will return typeof(T)
        /// otherwise returns the same type
        /// </summary>
        internal static Type GetGermaneType(this Type ownerType)
            => DynamicFacade.TypeInferrer.GetGermaneType(ownerType);

        public static Boolean TryGetNullableType(this Type candidate, out Type primitive)
            => DynamicFacade.TypeInferrer.TryGetNullableType(candidate, out primitive);


        /// <summary>
        /// whether there are read only properties with the same name/type as a constructor
        /// </summary>
        /// <returns></returns>
        public static Boolean TryGetPropertiesConstructor(this Type type, out
            ConstructorInfo constr) => DynamicFacade.ObjectInstantiator.TryGetPropertiesConstructor(type, out constr);

        public static T Random<T>(this IList<T> collection)
        {
            if (collection == null || collection.Count == 0)
                return default;
            return collection.Skip(_random.Next(collection.Count)).First();
        }

        /// <summary>
        /// Creates an easier to read string depiction of a type name, particularly
        /// with generics. Can be parsed back into a type using DasType.FromClearName(..)
        /// into
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isOmitAssemblyName">Guarantees that the output string will be
        /// valid xml or json markup but may lead to slower deserialization</param>
        public static String GetClearName(this Type type, Boolean isOmitAssemblyName)
            => DynamicFacade.TypeInferrer.ToClearName(type, isOmitAssemblyName);


        public static Boolean IsInstantiable(this Type t)
            => DynamicFacade.TypeInferrer.IsInstantiable(t);


        public static Boolean IsLeaf(this Type t, Boolean isStringCounts) =>
            DynamicFacade.TypeInferrer.IsLeaf(t, isStringCounts);

        public static T GetPropertyValue<T>(this Object obj, String propertyName)
            => DynamicFacade.ObjectManipulator.GetPropertyValue<T>(obj, propertyName);


        public static Boolean TryGetPropertyValue<T>(this Object obj, String propertyName,
            out T result) => DynamicFacade.ObjectManipulator.TryGetPropertyValue(obj,
            propertyName, out result);


        public static Boolean TryGetPropertyValue(this Object obj, String propertyName,
            out Object result) => DynamicFacade.ObjectManipulator.TryGetPropertyValue(obj, propertyName, out result);


        public static Object GetPropertyValue(this Object obj, String propertyName) =>
            DynamicFacade.ObjectManipulator.GetPropertyResult(obj, obj.GetType(),
                propertyName).Value;


        /// <summary>
        /// Gets property name/type/values including nulls.
        /// Specify a base class or interface for the generic parameter to get a subset
        /// </summary>
        public static Dictionary<String, NamedValueNode> GetPropertyValues<T>(this T obj)
        {
            var node = new ValueNode(obj, typeof(T));
            var walues = DynamicFacade.ObjectManipulator.GetPropertyResults(node, Settings);
            return walues.ToDictionary(w => w.Name, w => w);
        }

        public static void GenericMethod(this Object obj, String methodName,
            Type[] genericParameters, Object[] parameters,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
            => DynamicFacade.ObjectManipulator.GenericMethod(obj, methodName,
                genericParameters, parameters, flags);

        public static Object GenericFunc(this Object obj, String funcName, Object[] parameters,
            Type[] genericParameters, BindingFlags flags = BindingFlags.Public
                                                           | BindingFlags.Instance)
            => DynamicFacade.ObjectManipulator.GenericFunc(obj, funcName,
                parameters, genericParameters, flags);


        public static String XmlEscape(this String input) => System.Security.SecurityElement.Escape(input);

        public static Boolean TrySetPropertyValue(this Object obj, String propertyName,
            Object value) =>
            DynamicFacade.ObjectManipulator.SetProperty(obj.GetType(), propertyName,
                ref obj, value);


        /// <summary>
        /// Only tries to set public properties with public setters by default. Pass a value for
        /// settings.SerializationDepth for non public
        /// </summary>
        public static void SetPropertyValue(this Object obj, String propertyName, Object value)
        {
            DynamicFacade.ObjectManipulator.SetProperty(obj.GetType(), propertyName,
                ref obj, value);
        }

        public static void SetFieldValue(this Object obj, String fieldName, Object value)
        {
            DynamicFacade.ObjectManipulator.SetFieldValue(obj.GetType(), fieldName, obj, value);
        }

        public static void SetFieldValue<TObject, TValue>(this TObject obj,
            String fieldName, Object value)
        {
            DynamicFacade.ObjectManipulator.SetFieldValue<TValue>(obj.GetType(),
                fieldName, obj, value);
        }

        public static void SetFieldValue<T>(this Object obj, String fieldName, Object value)
        {
            DynamicFacade.ObjectManipulator.SetFieldValue<T>(obj.GetType(), fieldName, obj, value);
        }

        public static bool IsNumeric(this Type myType) =>
            DynamicFacade.TypeInferrer.IsNumeric(myType);


        public static T BuildDefault<T>(this Type type) => (T) type.BuildDefault();

        public static Object BuildDefault(this Type type)
            => DynamicFacade.ObjectInstantiator.BuildDefault(type, false);


        public static Boolean IsHasEmptyConstructor(this Type t)
            => DynamicFacade.TypeInferrer.HasEmptyConstructor(t);

        public static IEnumerable<PropertyInfo> GetPublicProperties(this Type type,
            Boolean numericFirst = true) =>
            DynamicFacade.TypeInferrer.GetPublicProperties(type, numericFirst);


        public static IEnumerable<FieldInfo> GetRecursivePrivateFields(this Type type)
            => DynamicFacade.TypeManipulator.GetRecursivePrivateFields(type);


        public static Type PropertyType(this MemberInfo info)
            => DynamicFacade.TypeManipulator.InstanceMemberType(info);

        public static Boolean IsAbstract(this PropertyInfo propInfo) =>
            DynamicFacade.TypeInferrer.IsAbstract(propInfo);


        public static IEnumerable<MethodInfo> GetInterfaceMethods(this Type type)
            => DynamicFacade.TypeManipulator.GetInterfaceMethods(type);
    }
}