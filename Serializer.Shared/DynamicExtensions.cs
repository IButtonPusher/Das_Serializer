using System;
using System.Threading.Tasks;
using Das.Serializer;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local

namespace Das.Extensions
{
    public static class DynamicExtensions
    {
        static DynamicExtensions()
        {
            _ctx = new DefaultStateProvider();
        }

        public static void CopyTo<T>(this T obj, ref T res) where T : class
        {
            _ctx.ObjectConverter.Copy(obj, ref res, _ctx.Settings);
        }

        //public static IStateProvider Context
        //{
        //    get => _ctx ?? (_ctx = new DefaultStateProvider());
        //    private set => _ctx = value;
        //}


        public static void Update<T>(this T updating, T withValuesOf) where T : class
        {
            _ctx.ObjectConverter.Copy(withValuesOf, ref updating,
                _ctx.Settings);
        }

        private static readonly IStateProvider _ctx;
    }
}