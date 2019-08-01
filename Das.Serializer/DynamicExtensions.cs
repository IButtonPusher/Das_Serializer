using System;
using System.Reflection;
using Das.Serializer;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local

namespace Das.Extensions
{
    public static class DynamicExtensions
    {
        private static IStateProvider _ctx;

        public static IStateProvider Context
        {
            get => _ctx ?? (_ctx = new DefaultStateProvider());
            set => _ctx = value;
        }

        public static Object ConvertEx(this Object obj, Type newObjectType)
            => Context.ObjectConverter.ConvertEx(obj, newObjectType, Context.Settings);


        // ReSharper disable once UnusedParameter.Local
        private static T ConvertEx<T>(this Object obj, T newObject) =>
            ConvertEx<T>(obj);

        public static void Update<T>(this T updating, T withValuesOf) where T : class
        {
            Context.ObjectConverter.Copy(withValuesOf, ref updating,
                Context.Settings);
        }


        public static T Copy<T>(this T obj) where T : class
            => Context.ObjectConverter.Copy(obj);

        public static void CopyTo<T>(this T obj, ref T res) where T : class
            => Context.ObjectConverter.Copy(obj, ref res, Context.Settings);


        public static T ConvertEx<T>(this Object obj)
            => Context.ObjectConverter.ConvertEx<T>(obj, Context.Settings);

        public static Func<object> GetConstructorDelegate(this Type type)
            => (Func<object>) Context.ObjectInstantiator.GetConstructorDelegate(type,
                typeof(Func<object>));

        public static void Method(this Object obj, String methodName, Object[] parameters,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            Context.ObjectManipulator.Method(obj, methodName, parameters, flags);
        }

        public static Object Func(this Object obj, String funcName, Object[] parameters,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
            => Context.ObjectManipulator.Func(obj, funcName, parameters, flags);
    }
}