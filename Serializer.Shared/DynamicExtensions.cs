using System;
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


        public static void Update<T>(this T updating, T withValuesOf) where T : class
        {
            Context.ObjectConverter.Copy(withValuesOf, ref updating,
                Context.Settings);
        }

        public static void CopyTo<T>(this T obj, ref T res) where T : class
            => Context.ObjectConverter.Copy(obj, ref res, Context.Settings);
    }
}