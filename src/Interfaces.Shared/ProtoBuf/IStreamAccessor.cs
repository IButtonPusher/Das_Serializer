using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.ProtoBuf
{
    public interface IStreamAccessor
    {
        /// <summary>
        ///     public static void ProtoDynamicBase.CopyMemoryStream(
        ///     MemoryStream copyFrom,  Stream  destination)
        /// </summary>
        MethodInfo CopyMemoryStream { get; }

        /// <summary>
        ///     public static Int32 ProtoDynamicBase.GetPositiveInt32(
        ///     Stream stream);
        /// </summary>
        MethodInfo GetPositiveInt32 { get; }

        /// <summary>
        ///     public long Stream.Length { get; }
        /// </summary>
        MethodInfo GetStreamLength { get; }


        /// <summary>
        ///     public string Encoding.GetString(
        ///     byte[] bytes, int index, int count)
        /// </summary>
        MethodInfo GetStringFromBytes { get; }


        /// <summary>
        ///     public int Stream.Read(byte[] buffer, int offset, int count);
        /// </summary>
        MethodInfo ReadStreamBytes { get; }

        /// <summary>
        ///     public void Stream.SetLength(long value);
        /// </summary>
        MethodInfo SetStreamLength { get; }


        /// <summary>
        ///     public long Stream.Position { set; }
        /// </summary>
        MethodInfo SetStreamPosition { get; }


        /// <summary>
        ///     protected static Encoding ProtoDynamicBase.Utf8;
        /// </summary>
        FieldInfo Utf8 { get; }


        /// <summary>
        ///     public static void ProtoBufWriter.WriteInt64(
        ///     Int64 value, Stream outStream)
        /// </summary>
        MethodInfo WriteInt64 { get; }


        /// <summary>
        ///     public static void ProtoBufWriter.WriteUInt64(
        ///     Int64 value, Stream outStream)
        /// </summary>
        MethodInfo WriteUInt64 { get; }
    }
}
