using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Das.Serializer.Remunerators;

namespace Das.Serializer.ProtoBuf
{
    public abstract class ProtoDynamicBase<T> : ProtoDynamicBase, IProtoProxy<T>
    {
        protected ProtoDynamicBase(//Func<T> defaultConstructor, 
            IProtoProvider proxyProvider)
            : base(256, proxyProvider)
        {
         
        }

        public abstract void Print(T obj, Stream target);

        public T Scan(Stream stream)
        {
            return Scan(stream, stream.Length);
        }

        public virtual T Scan(Stream stream, Int64 byteCount)
        {
            throw new NotSupportedException();
        }

        public virtual T BuildDefault()
        {
            throw new NotSupportedException();
        }

        //private readonly Func<T> _defaultConstructor;
        
    }

    public abstract class ProtoDynamicBase : ProtoBufWriter
    {
        // ReSharper disable once NotAccessedField.Global
        protected readonly IProtoProvider _proxyProvider;

        public static BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;

        static ProtoDynamicBase()
        {
            Utf8 = Encoding.UTF8;
        }

        public IEnumerable<TChild> GetChildren<TChild>(Stream stream,
            IProtoProxy<TChild> proxy)
        {
            var length = GetPositiveInt32(stream);

            var positionWas = stream.Position;
            var endPosition = positionWas + length;

            while (positionWas < endPosition)
            {
                yield return proxy.Scan(stream);
                positionWas = stream.Position;
            }
        }

        protected ProtoDynamicBase(Int32 startSize, IProtoProvider proxyProvider) : base(startSize)
        {
            _proxyProvider = proxyProvider;
        }

        public abstract Boolean IsReadOnly { get; }

        public void DebugWriteline(Object obj1, object obj2)
        {
            Debug.WriteLine("Deebug " + obj1 + "\t" + obj2);
        }

        public static IEnumerable<Int16> ExtractPacked16(Stream stream, Int32 bytesToUse)
        {
            var end = stream.Position + bytesToUse;

            while (end > stream.Position) yield return (Int16) GetInt32(stream);
        }


        public static IEnumerable<Int32> ExtractPacked32(Stream stream, Int32 bytesToUse)
        {
            var end = stream.Position + bytesToUse;

            while (end > stream.Position) 
                yield return GetInt32(stream);
        }

        public static IEnumerable<Int64> ExtractPacked64(Stream stream, Int32 bytesToUse)
        {
            var end = stream.Position + bytesToUse;

            while (end > stream.Position) yield return GetInt64(stream);
        }

        public static Int32 GetColumnIndex(Stream stream)
        {
            var result = 0;
            var push = 0;

            while (true)
            {
                var currentByte = stream.ReadByte();

                result += (currentByte & 0x7F) << push;

                push += 7;
                if ((currentByte & 0x80) == 0)
                    return result >> 3;
            }
        }

        public static Int32 GetInt32(Stream stream)
        {
            var result = 0;
            var push = 0;

            while (true)
            {
                var currentByte = stream.ReadByte();

                result += (currentByte & 0x7F) << push;
                if (push == 28 && result < 0)
                {
                    stream.Position += 5;
                    return result;
                }

                push += 7;
                if ((currentByte & 0x80) == 0)
                    return result;
            }
        }

        public static Int64 GetInt64(Stream stream)
        {
            var result = 0L;
            var push = 0;

            while (true)
            {
                var currentByte = (Byte) stream.ReadByte();

                result += (currentByte & 0x7F) << push;
                if (push == 28 && result < 0)
                {
                    stream.Position += 5;
                    return result;
                }

                push += 7;
                if ((currentByte & 0x80) == 0)
                    return result;
            }
        }

        public static Int32 GetPositiveInt32(Stream stream)
        {
            var result = 0;
            var push = 0;

            while (true)
            {
                var currentByte = stream.ReadByte();

                result += (currentByte & 0x7F) << push;

                push += 7;
                if ((currentByte & 0x80) == 0)
                    return result;
            }
        }

        public static Int64 GetPositiveInt64(Stream stream)
        {
            var result = 0L;
            var push = 0;

            while (true)
            {
                var currentByte = stream.ReadByte();

                result += (currentByte & 0x7F) << push;

                push += 7;
                if ((currentByte & 0x80) == 0)
                    return result;
            }
        }

        protected static Encoding Utf8;

        [ThreadStatic] protected static Byte[] _readBytes;
    }
}