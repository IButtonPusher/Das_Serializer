using System;
using System.Collections.Generic;
using System.Linq;
using Das.Serializer;
using Das.Serializer.Objects;

// ReSharper disable UnusedMember.Global

namespace Das.Extensions
{
    public static class ExtensionMethods
    {
        private static ISerializationCore _dynamicFacade;

        public static ISerializationCore SerializationCore
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

        
        /// <summary>
        /// Creates an easier to read string depiction of a type name, particularly
        /// with generics. Can be parsed back into a type using DasType.FromClearName(..)
        /// into
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isOmitAssemblyName">Guarantees that the output string will be
        /// valid xml or json markup but may lead to slower deserialization</param>
        public static String GetClearName(this Type type, Boolean isOmitAssemblyName)
            => SerializationCore.TypeInferrer.ToClearName(type, isOmitAssemblyName);

        public static Boolean TryGetPropertyValue(this Object obj, String propertyName,
            out Object result) 
            => SerializationCore.ObjectManipulator.TryGetPropertyValue(obj, propertyName, out result);


        /// <summary>
        /// Gets property name/type/values including nulls.
        /// Specify a base class or interface for the generic parameter to get a subset
        /// </summary>
        public static Dictionary<String, NamedValueNode> GetPropertyValues<T>(this T obj)
        {
            var node = new ValueNode(obj, typeof(T));
            var walues = SerializationCore.ObjectManipulator.GetPropertyResults(node, Settings);
            return walues.ToDictionary(w => w.Name, w => w);
        }
      
        public static Boolean TrySetPropertyValue(this Object obj, String propertyName,
            Object value) =>
            SerializationCore.ObjectManipulator.SetProperty(obj.GetType(), propertyName,
                ref obj, value);

        public static T BuildDefault<T>(this Type type) => (T) type.BuildDefault();

        public static Object BuildDefault(this Type type)
            => SerializationCore.ObjectInstantiator.BuildDefault(type, false);

    }
}