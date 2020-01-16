using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Das.Serializer.Remunerators;

namespace Das.Serializer.ProtoBuf
{
    public abstract class ProtoDynamicBase<T> : ProtoDynamicBase, IProtoProxy<T>
    {

        private readonly Func<T> _defaultConstructor;        

        public abstract void Print(T obj);

        public abstract T Scan(Stream stream);

        protected ProtoDynamicBase(Func<T> defaultConstructor) 
            : base(256)
        {
            _defaultConstructor = defaultConstructor;
        }

        public T BuildDefault() => _defaultConstructor();
    }

    public abstract class ProtoDynamicBase : ProtoBufWriter{
        protected static Encoding Utf8;

        static ProtoDynamicBase()
        {
            Utf8 = Encoding.UTF8;
        }

        protected ProtoDynamicBase(Int32 startSize) : base(startSize)
        {
        }

        public static Int32 GetInt32(Stream stream)
        {
            var result=0;
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
            var result=0L;
            var push = 0;

            while (true)
            {
                var currentByte = (Byte)stream.ReadByte();
                
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

        public void DebugWriteline(Object obj1, object obj2)
        {
            Debug.WriteLine("Deebug " + obj1 + "\t" + obj2);
        }

        public static Int32 GetColumnIndex(Stream stream)
        {
            var result=0;
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

        public static Int32 GetPositiveInt32(Stream stream)
        {
            var result=0;
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
            var result=0L;
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
    }
}
