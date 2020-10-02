using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Das.Serializer.Remunerators
{
    public class ProtoBufWriter
    {
        [MethodImpl(256)]
        public static void Append(Byte[] data, Stream _outStream)
        {
            _outStream.Write(data, 0, data.Length);
        }

        public static Int32 GetPackedArrayLength16<TCollection>(TCollection packedArray)
            where TCollection : IEnumerable<Int16>
        {
            var counter = 0;
            foreach (var val in packedArray)
                GetVarIntLengthImpl(val, ref counter);
            return counter;
        }


        public static Int32 GetPackedArrayLength32<TCollection>(TCollection packedArray)
            where TCollection : IEnumerable<Int32>
        {
            var counter = 0;
            foreach (var val in packedArray)
                GetVarIntLengthImpl(val, ref counter);
            return counter;
        }

        public static Int32 GetPackedArrayLength64<TCollection>(TCollection packedArray)
            where TCollection : IEnumerable<Int64>
        {
            throw new NotImplementedException();
            //var counter = 0;
            //foreach (var val in packedArray)
            //    GetVarIntLengthImpl(val, ref counter);
            //return counter;
        }

        [MethodImpl(256)]
        public static int GetVarIntLength(Int32 value)
        {
            var counter = 0;
            GetVarIntLengthImpl(value, ref counter);
            return counter;
        }

        private static void GetVarIntLengthImpl(Int32 value, ref Int32 counter)
        {
            if (value >= 0)
            {
                if (value > 127)
                {
                    if (value > 16256)
                    {
                        if (value > 1040384)
                        {
                            if (value > 66584576)
                            {
                                if (value > 2130706432)
                                {
                                    counter += 5;
                                    return;
                                }

                                counter += 5;
                                return;
                            }

                            counter += 4;
                            return;
                        }

                        counter += 3;
                        return;
                    }

                    counter += 2;
                    return;
                }

                counter += 1;

                return;
            }

            counter += 10; //negative
        }

        [MethodImpl(256)]
        public static void Write(Byte[] vals, Stream _outStream)
        {
            Append(vals, _outStream);
        }


        public static void Write(Byte[] buffer, Int32 count, Stream _outStream)
        {
            Write(buffer, 0, count, _outStream);
        }

        public static void Write(Byte[] buffer, Int32 index, Int32 count, Stream _outStream)
        {
            _outStream.Write(buffer, index, count);
        }

        public static void WriteInt16(Int16 val, Stream outStream)
        {
            WriteInt32(val, outStream);
        }

        public static void WriteInt16(UInt16 val, Stream outStream)
        {
            WriteInt32(val, outStream);
        }

        public static void WriteInt32(Int32 value, Stream _outStream)
        {
            if (value >= 0)
            {
                if (value > 127)
                {
                    if (value > 16256)
                    {
                        if (value > 1040384)
                        {
                            if (value > 66584576)
                            {
                                if (value > 2130706432)
                                {
                                    _outStream.WriteByte((Byte) ((value & 127) | 128));
                                    _outStream.WriteByte((Byte) (((value & 16256) >> 7) | 128));
                                    _outStream.WriteByte((Byte) (((value & 1040384) >> 13) | 128));
                                    _outStream.WriteByte((Byte) (((value & 66584576) >> 19) | 128));
                                    _outStream.WriteByte((Byte) ((value & 1879048192) >> 28));

                                    return;
                                }

                                _outStream.WriteByte((Byte) ((value & 127) | 128));
                                _outStream.WriteByte((Byte) (((value & 16256) >> 7) | 128));
                                _outStream.WriteByte((Byte) (((value & 1040384) >> 13) | 128));
                                _outStream.WriteByte((Byte) (((value & 66584576) >> 19) | 128));
                                _outStream.WriteByte((Byte) ((value & 4261412864) >> 26));


                                return;
                            }

                            _outStream.WriteByte((Byte) ((value & 127) | 128));
                            _outStream.WriteByte((Byte) (((value & 16256) >> 7) | 128));
                            _outStream.WriteByte((Byte) (((value & 1040384) >> 13) | 128));
                            _outStream.WriteByte((Byte) ((value & 66584576) >> 20));

                            return;
                        }

                        _outStream.WriteByte((Byte) ((value & 127) | 128));
                        _outStream.WriteByte((Byte) (((value & 16256) >> 7) | 128));
                        _outStream.WriteByte((Byte) ((value & 1040384) >> 14));

                        return;
                    }

                    _outStream.WriteByte((Byte) ((value & 127) | 128));
                    _outStream.WriteByte((Byte) ((value & 16256) >> 7));

                    return;
                }

                _outStream.WriteByte((Byte) (value & 127));

                return;
            }

            for (var c = 0; c <= 4; c++)
            {
                var current = (Byte) (value | 128);
                value >>= 7;
                WriteInt8(current, _outStream);
            }

            Write(_negative32Fill, 0, 5, _outStream);
        }

        public static void WriteInt32(Int64 val, Stream outStream)
        {
            throw new NotImplementedException();
        }

        public static void WriteInt64(UInt64 val, Stream outStream)
        {
            throw new NotImplementedException();
        }

        public static void WriteInt64(Int64 value, Stream outStream)
        {
            if (value >= 0)
            {
                do
                {
                    var current = (Byte) (value & 127);
                    value >>= 7;
                    if (value > 0)
                        current += 128; //8th bit to specify more bytes remain
                    WriteInt8(current, outStream);
                } while (value > 0);
            }
            else
            {
                for (var c = 0; c <= 4; c++)
                {
                    var current = (Byte) (value | 128);
                    value >>= 7;
                    WriteInt8(current, outStream);
                }

                Write(_negative32Fill, 0, 5, outStream);
            }
        }


        [MethodImpl(256)]
        public static void WriteInt8(Byte value, Stream _outStream)
        {
            _outStream.WriteByte(value);
        }

        public static void WriteInt8(SByte value, Stream outStream)
        {
            throw new NotImplementedException();
        }

        public static void WritePacked16<TCollection>(TCollection packed, Stream _outStream)
            where TCollection : IEnumerable<short>
        {
            foreach (var item in packed)
                WriteInt32(item, _outStream);
        }

        public static void WritePacked32<TCollection>(TCollection packed, Stream _outStream)
            where TCollection : IEnumerable<int>
        {
            foreach (var item in packed)
                WriteInt32(item, _outStream);
        }


        public static void WritePacked64<TCollection>(TCollection packed, Stream _outStream)
            where TCollection : IEnumerable<Int64>
        {
            foreach (var item in packed)
                WriteInt64(item, _outStream);
        }

        private static readonly Byte[] _negative32Fill =
        {
            Byte.MaxValue, Byte.MaxValue,
            Byte.MaxValue, Byte.MaxValue, 1
        };
    }
}