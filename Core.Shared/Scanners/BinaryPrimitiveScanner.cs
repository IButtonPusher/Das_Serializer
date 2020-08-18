using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;


namespace Das.Serializer
{
    internal class BinaryPrimitiveScanner : SerializerCore, IBinaryPrimitiveScanner
    {
        #region fields		

        private readonly BinaryFormatter _fallbackFormatter;
        private readonly IInstantiator _instantiator;

        #endregion

        #region construction

        public BinaryPrimitiveScanner(ISerializationCore dynamicFacade, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            _fallbackFormatter = new BinaryFormatter();
            _instantiator = ObjectInstantiator;
        }

        #endregion

        #region public interface

        public Object GetValue(Byte[] input, Type type)
        {
            Object res;
            if (input == null)
            {
                return null;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                    return GetString(input);
                    
                case TypeCode.Decimal:
                    return ToDecimal(input);
                    
                case TypeCode.DateTime:
                    var ticks = _instantiator.CreatePrimitiveObject<Int64>(input, typeof(Int64));
                    return new DateTime(ticks);
                    
                case TypeCode.Double:
                    return BitConverter.ToDouble(input, 0);
                    
                case TypeCode.Single:
                    return BitConverter.ToSingle(input, 0);
                    
                default:
                    if (type.IsEnum)
                    {
                        res = _instantiator.CreatePrimitiveObject(input, 
                            Enum.GetUnderlyingType(type));
                        res = Enum.ToObject(type, res);
                    }
                    else if (IsLeaf(type, false))
                        res = _instantiator.CreatePrimitiveObject(input, type);
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

        public unsafe Int32 GetInt32(Byte[] value)
        {
            fixed (Byte* pbyte = &value[0])
                return *(Int32*) pbyte;
        }
      

        public virtual unsafe String GetString(Byte[] tempByte)
        {
            if (tempByte == null)
                return null;

            fixed (Byte* bptr = tempByte)
            {
                var cptr = (Char*) bptr;
                return new String(cptr, 0, tempByte.Length / 2);
            }
        }

        #endregion
    }
}