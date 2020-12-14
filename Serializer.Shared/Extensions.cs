using System;
using System.Threading.Tasks;
using Das.Serializer;

// ReSharper disable UnusedMember.Global

namespace Das.Extensions
{
    // ReSharper disable once UnusedType.Global
    public static class ExtensionMethods
    {
        public static ISerializationCore SerializationCore
        {
            get => _dynamicFacade ?? (_dynamicFacade = new DefaultStateProvider());
            set => _dynamicFacade = value;
        }

        internal static ISerializerSettings Settings
        {
            get => _settings ?? (_settings = DasSettings.Default);
            set => _settings = value;
        }

        public static T BuildDefault<T>(this Type type)
        {
            return (T) type.BuildDefault()!;
        }

        public static Object? BuildDefault(this Type type)
        {
            return SerializationCore.ObjectInstantiator.BuildDefault(type, false);
        }


        /// <summary>
        ///     Creates an easier to read string depiction of a type name, particularly
        ///     with generics. Can be parsed back into a type using DasType.FromClearName(..)
        ///     into
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isOmitAssemblyName">
        ///     Guarantees that the output string will be
        ///     valid xml or json markup but may lead to slower deserialization
        /// </param>
        public static String GetClearName(this Type type, Boolean isOmitAssemblyName)
        {
            return SerializationCore.TypeInferrer.ToClearName(type, isOmitAssemblyName);
        }

        public static T GetPropertyValue<T>(this Object obj, String propertyName)
        {
            return SerializationCore.ObjectManipulator.GetPropertyValue<T>(obj, propertyName);
        }

        public static Boolean TryGetPropertyValue<T>(this Object obj, String propertyName,
                                                     out T result)
        {
            return SerializationCore.ObjectManipulator.TryGetPropertyValue(obj,
                propertyName, out result);
        }

        public static Boolean TryGetPropertyValue(
            this Object obj,
            String propertyName,
            out Object result)
        {
            return SerializationCore.ObjectManipulator.TryGetPropertyValue(obj,
                propertyName, out result);
        }


        ///// <summary>
        ///// Gets property name/type/values including nulls.
        ///// Specify a base class or interface for the generic parameter to get a subset
        ///// </summary>
        //public static Dictionary<String, IProperty> GetPropertyValues<T>(this T obj)
        //{
        //    var node = new ValueNode(obj, typeof(T));
        //    var walues = SerializationCore.ObjectManipulator.GetPropertyResults(node, Settings);
        //    return walues.ToDictionary(w => w.Name, w => w);
        //    //var res = new Dictionary<String, IProperty>();
        //    //foreach (var kvp in walues)
        //    //    res[kvp.Name] = kvp;

        //    //return res;
        //}

        public static Boolean TrySetPropertyValue(this Object obj, String propertyName,
                                                  Object value)
        {
            return SerializationCore.ObjectManipulator.SetProperty(obj.GetType(), propertyName,
                ref obj, value);
        }

        private static ISerializationCore? _dynamicFacade;

        private static ISerializerSettings? _settings;
    }
}