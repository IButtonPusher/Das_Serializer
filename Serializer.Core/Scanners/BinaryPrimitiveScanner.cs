using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Das.Serializer;
using Serializer.Core;


namespace Das.Scanners
{
    internal class BinaryPrimitiveScanner : SerializerCore, IBinaryPrimitiveScanner
    {
        #region fields		

        private readonly BinaryFormatter _fallbackFormatter;

        #endregion

        #region construction

        public BinaryPrimitiveScanner(IDynamicFacade dynamicFacade, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            _fallbackFormatter = new BinaryFormatter();
        }

        #endregion

        #region public interface

        public object GetValue(Byte[] input, Type type)
        {
            Object res;
            if (input == null)
            {
                return null;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                    res = GetString(input);
                    break;
                case TypeCode.Decimal:
                    res = ToDecimal(input);
                    break;
                case TypeCode.DateTime:
                    var ticks = CreatePrimitiveObject<Int64>(input, typeof(Int64));
                    res = new DateTime(ticks);
                    break;
                default:
                    if (type.IsEnum)
                    {
                        res = CreatePrimitiveObject(input, Enum.GetUnderlyingType(type));
                        res = Enum.ToObject(type, res);
                    }
                    else if (IsLeaf(type, false))
                        res = CreatePrimitiveObject(input, type);
                    else if (TryGetNullableType(type, out type))
                    {
                        res = GetValue(input, type);
                    }
                    else
                    {
                        using (var ms = new MemoryStream(input))
                            res = _fallbackFormatter.Deserialize(ms);
                    }

                    break;
            }

            return res;
        }

        public unsafe Int32 GetInt32(byte[] value)
        {
            fixed (byte* pbyte = &value[0])
                return *((int*) pbyte);
        }

        public unsafe String GetString(Byte[] tempByte)
        {
            if (tempByte == null)
                return null;

            fixed (byte* bptr = tempByte)
            {
                var cptr = (char*) bptr;
                return new string(cptr, 0, tempByte.Length / 2);
            }
        }

        #endregion
    }
}