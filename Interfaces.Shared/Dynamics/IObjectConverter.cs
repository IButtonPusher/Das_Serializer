using System;
using Das.Serializer;

namespace Das
{
    public interface IObjectConverter
    {
        T Copy<T>(T from) where T : class;

        T Copy<T>(T from, ISerializerSettings settings) where T : class;

        void Copy<T>(T from, ref T to, ISerializerSettings settings) where T : class;

        T ConvertEx<T>(Object obj, ISerializerSettings settings);

        // ReSharper disable once UnusedMember.Global
        T ConvertEx<T>(Object obj);

        Object ConvertEx(Object obj, Type newObjectType,
            ISerializerSettings settings);

        Object SpawnCollection(Object[] objects, Type collectionType,
            ISerializerSettings settings, Type collectionGenericArgs = null);
    }
}