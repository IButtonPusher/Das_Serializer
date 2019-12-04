using System;
using System.IO;

// ReSharper disable UnusedMemberInSuper.Global

// ReSharper disable UnusedMember.Global

namespace Das.Serializer
{
    public interface IBinarySerializer
    {
        T FromBytes<T>(Byte[] bytes);

        T FromBytes<T>(FileInfo fileName);

        T FromBytes<T>(Stream stream);

        Object FromBytes(Byte[] bytes);

        Byte[] ToBytes(Object o);

        /// <summary>
        /// Serialize up or down.  For example if TypeB inherits from TypeA
        /// and object obj is TypeB, passing the second parameter as typeof(TypeB)
        /// will create a byte array that cannot be deserialized as TypeB but
        /// can as TypeA
        /// </summary>
        Byte[] ToBytes(Object o, Type asType);

        Byte[] ToBytes<TObject>(TObject o);

        Byte[] ToBytes<TTarget>(Object o);

        void ToBytes(Object o, FileInfo fileName);

        void ToBytes<TTarget>(Object o, FileInfo fileName);

        void ToProtoStream<TObject, TPropertyAttribute>(Stream stream, TObject o,
            ProtoBufOptions<TPropertyAttribute> options)
            where TPropertyAttribute : Attribute;

        TObject FromProtoStream<TObject, TPropertyAttribute>(Stream stream,
            ProtoBufOptions<TPropertyAttribute> options)
            where TPropertyAttribute : Attribute;
    }
}