using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Das.Serializer;

namespace Das.Streamers
{
    public class BinaryFeeder : SerializerCore, 
                                IBinaryFeeder
    {
        public BinaryFeeder(IBinaryPrimitiveScanner primitiveScanner,
                            ISerializationCore dynamicFacade, IByteArray bytes, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            _scanner = primitiveScanner;
            _typeInferrer = dynamicFacade.TypeInferrer;

            _currentBytes = bytes;
            _currentEndIndex = (Int32) _currentBytes.Length - 1;
        }

        #region fields

        protected IByteArray _currentBytes;
        protected Int32 _currentEndIndex;

        public virtual Int32 GetInt32()
        {
            return (Int32) GetPrimitive(typeof(Int32));
        }


        public Double GetDouble()
        {
            var bytes = GetBytes(8);
            var res = _scanner.GetValue(bytes, typeof(Double));
            return (Double) res;
        }

        public Int32 PeekInt32(Int32 advanceIf)
        {
            if (_currentBytes.Length <= _byteIndex)
                return -1;

            var val = GetInt32();
            if (val != advanceIf)
            {
                _byteIndex--;
                _currentBytes.StepBack();
            }

            return val;
        }


        public Int32 Index
        {
            get => _byteIndex;
            protected set => _byteIndex = value;
        }

        public virtual Boolean HasMoreBytes => _byteIndex < _currentEndIndex;

        protected Int32 _byteIndex;

        private readonly IBinaryPrimitiveScanner _scanner;
        private readonly ITypeInferrer _typeInferrer;

        #endregion


        #region public interface

        /// <summary>
        ///     Returns the amount of bytes that the next object will use.  Advances
        ///     the byte index forward by 4 bytes
        /// </summary>
        public virtual Int32 GetNextBlockSize()
        {
            var forInt = _currentBytes[_byteIndex, 4];

            var length = _scanner.GetInt32(forInt);

            _byteIndex += 4;

            return length;
        }

        public Byte GetCircularReferenceIndex()
        {
            return GetBytes(1)[0];
        }

        public T GetPrimitive<T>()
        {
            return (T) GetPrimitive(typeof(T));
        }


        public Object GetPrimitive(Type type)
        {
            var bytes = GetPrimitiveBytes(type);
            var res = _scanner.GetValue(bytes, type);
            return res;
        }

        public Single GetInt8()
        {
            var bytes = GetBytes(4);
            var res = _scanner.GetValue(bytes, typeof(Single));
            return (Single) res;
        }


        private Byte[]? GetPrimitiveBytes(Type type)
        {
            while (true)
            {
                if (type.IsValueType && !type.IsGenericType)
                {
                    var res = GetBytesForValueTypeObject(type);
                    return res;
                }

                if (TryGetNullableType(type, out var asPrimitive))
                {
                    var hasValue = GetPrimitive<Boolean>();
                    if (!hasValue)
                        return null;

                    type = asPrimitive;
                    continue;
                }

                var length = GetNextBlockSize();

                if (IsString(type))
                {
                    if (length == -1) return null;
                }
                else if (length == 0)
                {
                    return null;
                }

                return GetBytes(length);
            }
        }

        private Byte[] GetBytesForValueTypeObject(Type type)
        {
            var length = TypeInferrer.BytesNeeded(type);
            return GetBytes(length);
        }

        /// <summary>
        ///     takes the next 4 bytes for length then the next N bytes and turns them into a Type
        /// </summary>
        public Type? GetNextType()
        {
            var bytes = GetTypeBytes();
            var str = _scanner.GetString(bytes);
            if (str == null)
                throw new InvalidOperationException();

            var typeName = _typeInferrer.GetTypeFromClearName(str);

            return typeName;
        }

        public Byte[] GetTypeBytes()
        {
            var lengthOfTypeName = GetNextBlockSize();
            return GetBytes(lengthOfTypeName);
        }


        public virtual Byte[] GetBytes(Int32 count)
        {
            var res = _currentBytes[_byteIndex, count];
            _byteIndex += count;
            return res;
        }

        [MethodImpl(256)]
        public Byte[] IncludeBytes(Int32 count)
        {
            _byteIndex += count;
            return _currentBytes.IncludeBytes(count);
        }


        public Object? GetFallback(Type dataType, ref Int32 blockSize)
        {
            //collection data we have to open a new node and that has to get these bytes
            if (IsCollection(dataType))
                return null;

            Byte[] bytes;

            if (!IsNumeric(dataType))
            {
                if (blockSize == 0)
                    return null;

                bytes = GetBytes(blockSize);
            }
            else
            {
                bytes = GetBytesForValueTypeObject(dataType);
            }

            var res = _scanner.GetValue(bytes, dataType);
            //_logger.Debug("fallback to " + res);
            return res;
        }

        #endregion
    }
}