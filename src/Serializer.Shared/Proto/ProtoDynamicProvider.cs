#if GENERATECODE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer.CodeGen;
using Das.Serializer.Remunerators;
using Reflection.Common;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    public partial class ProtoDynamicProvider<TPropertyAttribute> : BaseDynamicProvider<IProtoFieldAccessor, Boolean, ProtoPrintState>
        where TPropertyAttribute : Attribute
    {
        public ProtoDynamicProvider(IProtoBufOptions<TPropertyAttribute> protoSettings,
                                    ITypeManipulator typeManipulator,
                                    IInstantiator instantiator,
                                    IObjectManipulator objects,
                                    ISerializerSettings defaultSettings) 
            : base(typeManipulator, instantiator)
        {
            _protoSettings = protoSettings;
            
            _objects = objects;
            _defaultSettings = defaultSettings;
        
            var protoDynBase = typeof(ProtoDynamicBase);
            _proxyProviderField = protoDynBase.GetInstanceFieldOrDie("_proxyProvider");
            _readBytesField = protoDynBase.GetPrivateStaticFieldOrDie("_readBytes");

           
        }

        static ProtoDynamicProvider()
        {
            var writer = typeof(ProtoBufWriter);
            var bitConverter = typeof(BitConverter);
            var stream = typeof(Stream);
            var protoDynBase = typeof(ProtoDynamicBase);
            

            _dateFromFileTime = typeof(DateTime).GetPublicStaticMethodOrDie(
                nameof(DateTime.FromFileTime), typeof(Int64));

            //_writeInt8 = writer.GetPublicStaticMethodOrDie(
            //    nameof(ProtoBufWriter.WriteInt8), typeof(Byte), stream);
            //_writeInt16 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteInt16),
            //    typeof(Int16), stream);
            //_writeInt32 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteInt32), typeof(Int32), stream);
            //_writeUInt32 =
            //    writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteUInt32), typeof(UInt32), stream);

            _writeInt64 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteInt64), typeof(Int64), stream);
            _writeUInt64 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteUInt64), typeof(UInt64), stream);

            //_writeBytes = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.Write), typeof(Byte[]), stream);
            //_writeSomeBytes = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.Write),
            //    typeof(Byte[]), typeof(Int32), stream);

            //_writePacked16 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WritePacked16));
            //_writePacked32 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WritePacked32));
            //_writePacked64 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WritePacked64));

            //_getPackedInt32Length = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.GetPackedArrayLength32));
            //_getPackedInt16Length = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.GetPackedArrayLength16));
            //_getPackedInt64Length = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.GetPackedArrayLength64));

            _utf8 = protoDynBase.GetPrivateStaticFieldOrDie("Utf8");

            
            _getProtoProxy = typeof(IProtoProvider).GetMethodOrDie(nameof(IProtoProvider.GetProtoProxy), typeof(Boolean));

            _getAutoProtoProxy = typeof(IProtoProvider).GetMethodOrDie(nameof(IProtoProvider.GetAutoProtoProxy));

            //_getSingleBytes = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.GetBytes),
            //    typeof(Single));

            //_getDoubleBytes = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.GetBytes),
            //    typeof(Double));

            //_getSingleBytes = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.GetBytes),
            //    typeof(Single));

            //_getStringBytes = typeof(UTF8Encoding).GetMethodOrDie(nameof(UTF8Encoding.GetBytes),
            //    typeof(String));

            //_getArrayLength = typeof(Array).GetterOrDie(nameof(Array.Length), out _);

             _getStreamLength = stream.GetterOrDie(nameof(Stream.Length), out _);

            _copyMemoryStream = protoDynBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.CopyMemoryStream));
            _setStreamLength = stream.GetMethodOrDie(nameof(Stream.SetLength));
            _getStreamPosition = stream.GetterOrDie(nameof(Stream.Position), out _);
            _setStreamPosition = stream.SetterOrDie(nameof(Stream.Position));

            _readStreamByte = stream.GetMethodOrDie(nameof(Stream.ReadByte));
            _readStreamBytes = stream.GetMethodOrDie(nameof(Stream.Read));

            //_writeStreamByte = stream.GetMethodOrDie(nameof(Stream.WriteByte));

            _getPositiveInt32 = protoDynBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.GetPositiveInt32));
            _getPositiveInt64 = protoDynBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetPositiveInt64));

            _getInt32 = protoDynBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetInt32));
            _getInt64 = protoDynBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetInt64));

            _getUInt32 = protoDynBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetUInt32));
            _getUInt64 = protoDynBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetUInt64));

            _getColumnIndex = protoDynBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetColumnIndex));


            _bytesToSingle = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.ToSingle),
                typeof(Byte[]), typeof(Int32));

            _bytesToDouble = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.ToDouble),
                typeof(Byte[]), typeof(Int32));

            _bytesToDecimal = typeof(ExtensionMethods).GetPublicStaticMethodOrDie(
                nameof(ExtensionMethods.ToDecimal),
                typeof(Byte[]), typeof(Int32));

            _getStringFromBytes = typeof(Encoding).GetMethodOrDie(nameof(Encoding.GetString),
                typeof(Byte[]), typeof(Int32), typeof(Int32));

            _debugWriteline = typeof(ProtoDynamicBase).GetMethodOrDie(
                nameof(ProtoDynamicBase.DebugWriteline));

           

            _extractPackedInt16Itar = protoDynBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.ExtractPacked16));
            _extractPackedInt32Itar = protoDynBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.ExtractPacked32));
            _extractPackedInt64Itar = protoDynBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.ExtractPacked64));
        }

        public IProtoProxy<T> GetProtoProxy<T>(Boolean allowReadOnly = false)
        {
            return GetProtoProxy<T>(_defaultSettings, allowReadOnly);
        }

        public IProtoProxy<T> GetProtoProxy<T>(ISerializerSettings settings,
                                               Boolean allowReadOnly = false)
        {
            return ProxyLookup<T>.Instance ??= allowReadOnly
                ? CreateProxyTypeYesReadOnly<T>()
                : CreateProxyTypeNoReadOnly<T>();
        }

        Boolean IProtoProvider.TryGetProtoField(PropertyInfo prop,
                                                Boolean isRequireAttribute,
                                                out IProtoFieldAccessor field)
        {
            if (TryGetProtoField(prop, isRequireAttribute, out var f))
            {
                field = f;
                return true;
            }

            field = default!;
            return false;
        }

        //public ProtoFieldAction GetProtoFieldAction(Type pType)
        //{
        //    if (pType.IsPrimitive)
        //        return pType == typeof(Int32) ||
        //               pType == typeof(UInt32) ||
        //               pType == typeof(Int16) ||
        //               pType == typeof(Int64) ||
        //               pType == typeof(UInt64) ||
        //               pType == typeof(Byte) ||
        //               pType == typeof(Boolean)
        //            ? ProtoFieldAction.VarInt
        //            : ProtoFieldAction.Primitive;

        //    if (pType == Const.StrType)
        //        return ProtoFieldAction.String;

        //    if (pType == Const.ByteArrayType)
        //        return ProtoFieldAction.ByteArray;

        //    if (GetPackedArrayTypeCode(pType) != TypeCode.Empty)
        //        return ProtoFieldAction.PackedArray;

        //    if (typeof(IDictionary).IsAssignableFrom(pType))
        //        return ProtoFieldAction.Dictionary;

        //    if (_types.IsCollection(pType))
        //    {
        //        var germane = _types.GetGermaneType(pType);

        //        var subAction = GetProtoFieldAction(germane);

        //        switch (subAction)
        //        {
        //            case ProtoFieldAction.ChildObject:
        //                return pType.IsArray
        //                    ? ProtoFieldAction.ChildObjectArray
        //                    : ProtoFieldAction.ChildObjectCollection;

        //            default:
        //                return pType.IsArray
        //                    ? ProtoFieldAction.ChildPrimitiveArray
        //                    : ProtoFieldAction.ChildPrimitiveCollection;
        //        }
        //    }

        //    return ProtoFieldAction.ChildObject;
        //}


        #if DEBUG

        public override void DumpProxies()
        {
            #if NET45 || NET40
            if (System.Threading.Interlocked.Increment(ref _dumpCount) > 1)
            {
                System.Diagnostics.Debug.WriteLine("WARNING:  Proxies already dumped");
                return;
            }

            _asmBuilder.Save("protoTest.dll");
            #endif
        }

        #endif

        public MethodInfo GetStreamLength => _getStreamLength;

        public MethodInfo SetStreamPosition => _setStreamPosition;

        public MethodInfo WriteInt64 => _writeInt64;

        public MethodInfo WriteUInt64 => _writeUInt64;

        public MethodInfo CopyMemoryStream => _copyMemoryStream;

        public MethodInfo SetStreamLength => _setStreamLength;

        /// <summary>
        ///     Stream.Read(...)
        /// </summary>
        public MethodInfo ReadStreamBytes => _readStreamBytes;

        /// <summary>
        ///     static Int32 ProtoScanBase.GetPositiveInt32(Stream stream)
        /// </summary>
        public MethodInfo GetPositiveInt32 => _getPositiveInt32;

        /// <summary>
        ///     protected static Encoding Utf8;
        /// </summary>
        public FieldInfo Utf8 => _utf8;

        /// <summary>
        ///     Encoding-> public virtual string GetString(byte[] bytes, int index, int count)
        /// </summary>
        public MethodInfo GetStringFromBytes => _getStringFromBytes;

        public Boolean TryGetProtoField(PropertyInfo prop,
                                        Boolean isRequireAttribute,
                                        out IProtoFieldAccessor field)
        {
            return TryGetFieldAccessor(prop, isRequireAttribute, GetIndexFromAttribute, 0, out field);
        }



        protected override Boolean TryGetFieldAccessor(PropertyInfo prop,
                                                       Boolean isRequireAttribute,
                                                       GetFieldIndex getFieldIndex,
                                                       Int32 lastIndex,
                                                       out IProtoFieldAccessor field)
        {
            var index = 0;

            if (isRequireAttribute)
            {
                index = getFieldIndex(prop, lastIndex);
                if (index <= 0)
                {
                    field = default!;
                    return false;
                }
            }

            var pType = prop.PropertyType;

            var isCollection = _types.IsCollection(pType);

            var wire = ProtoBufSerializer.GetWireType(pType);

            var header = (Int32) wire + (index << 3);
            //var tc = Type.GetTypeCode(pType);

            var getter = prop.GetGetMethod() ?? throw new InvalidOperationException(prop.Name);
            var setter = prop.CanWrite ? prop.GetSetMethod(true) : default!;

            var headerBytes = GetBytes(header).ToArray();

            var fieldAction = GetProtoFieldAction(prop.PropertyType);

            field = new ProtoField(prop.Name, pType, wire,
                index, header, getter,
                _types.IsLeaf(pType, true), isCollection, fieldAction,
                headerBytes, setter);

            return true;
        }

        private Type? CreateProxyType(Type type,
                                      Boolean allowReadOnly)
        {
            if (!_instantiator.TryGetDefaultConstructor(type, out var dtoctor) &&
                !allowReadOnly)
                throw new InvalidProgramException($"No valid constructor found for {type}");

            var fields = GetPrintFields(type);

            var genericParent = typeof(ProtoDynamicBase<>).MakeGenericType(type);
            var retBldr = genericParent.GetMethodOrDie(
                nameof(ProtoDynamicBase<Object>.BuildDefault));

            return CreateProxyTypeImpl(type, dtoctor, fields, true, allowReadOnly,
                retBldr, genericParent);
        }

        private Type? CreateProxyTypeImpl(Type type,
                                          ConstructorInfo dtoCtor,
                                          IEnumerable<IProtoFieldAccessor> scanFields,
                                          Boolean canSetValuesInline,
                                          Boolean allowReadOnly,
                                          MethodBase buildReturnValue,
                                          Type? genericParent = null)
        {
            var scanFieldArr = scanFields.ToArray();

            var typeName = type.FullName ?? throw new InvalidOperationException();

            var bldr = _moduleBuilder.DefineType(typeName.Replace(".", "_"),
                TypeAttributes.Public | TypeAttributes.Class);

            genericParent ??= typeof(ProtoDynamicBase<>).MakeGenericType(type);

            var typeProxies = CreateProxyFields(bldr, scanFieldArr);

            var ctor = AddConstructors(bldr, dtoCtor,
                genericParent,
                typeProxies.Values,
                allowReadOnly);

        ////////////////////////////////////

            IEnumerable<IProtoFieldAccessor> printFields;
            if (canSetValuesInline || allowReadOnly)
                printFields = GetPrintFields(type);
            else
                printFields = scanFieldArr;

            AddPrintMethod(type, bldr, 
                printFields, typeProxies);

            ////////////////////////////////////


            var example = canSetValuesInline ? ctor.Invoke(new Object[0]) : default;
            AddScanMethod(type, bldr, genericParent, 
                scanFieldArr,
                example!, canSetValuesInline,
                buildReturnValue, 
                typeProxies);

            if (canSetValuesInline)
                AddDtoInstantiator(type, bldr, genericParent, ctor);


            bldr.SetParent(genericParent);

            var dType = bldr.CreateType();

            return dType;
        }

        private IProtoProxy<T> CreateProxyTypeNoReadOnly<T>()
        {
            var type = typeof(T);
            var ptype = CreateProxyType(type, false) ?? throw new TypeLoadException(type.Name);

            //DumpProxies();

            return InstantiateProxyInstance<T>(ptype);
        }

        private IProtoProxy<T> CreateProxyTypeYesReadOnly<T>()
        {
            var type = typeof(T);

            var ptype = CreateProxyType(type, true) ?? throw new TypeLoadException(type.Name);

            return InstantiateProxyInstance<T>(ptype);
        }


        private static IEnumerable<Byte> GetBytes(Int32 value)
        {
            if (value >= 0)
                do
                {
                    var current = (Byte) (value & 127);
                    value >>= 7;
                    if (value > 0)
                        current += 128; //8th bit to specify more bytes remain
                    yield return current;
                } while (value > 0);
            else
            {
                for (var c = 0; c <= 4; c++)
                {
                    var current = (Byte) (value | 128);
                    value >>= 7;
                    yield return current;
                }

                foreach (var b in _negative32Fill)
                {
                    yield return b;
                }
            }
        }

        protected override Int32 GetIndexFromAttribute(PropertyInfo prop,
                                                       Int32 lastIndex)
        {
            var attribs = prop.GetCustomAttributes(typeof(TPropertyAttribute), true)
                              .OfType<TPropertyAttribute>().ToArray();
            if (attribs.Length == 0) return -1;

            return _protoSettings.GetIndex(attribs[0]);
        }


        //protected override List<IProtoFieldAccessor> GetProtoPrintFields(Type type)
        //{
        //    var res = new List<IProtoFieldAccessor>();
        //    foreach (var prop in _types.GetPublicProperties(type))
        //    {
        //        if (TryGetProtoField(prop, true, out var protoField))
        //            res.Add(protoField);
        //    }

        //    return res;
        //}


        private IProtoProxy<T> InstantiateProxyInstance<T>(Type proxyType)
        {
        //DumpProxies();

            var res = Activator.CreateInstance(proxyType, this)
                      ?? throw new Exception(proxyType.Name);
            return (IProtoProxy<T>) res;
        }
        

        private const MethodAttributes MethodOverride = MethodAttributes.Public |
                                                        MethodAttributes.HideBySig |
                                                        MethodAttributes.Virtual |
                                                        MethodAttributes.CheckAccessOnOverride |
                                                        MethodAttributes.Final;

        // ReSharper disable once StaticMemberInGenericType

        #if DEBUG

        private static Int32 _dumpCount;

        #endif


        private static readonly Byte[] _negative32Fill =
        {
            Byte.MaxValue, Byte.MaxValue,
            Byte.MaxValue, Byte.MaxValue, 1
        };

        private readonly IProtoBufOptions<TPropertyAttribute> _protoSettings;
        private readonly FieldInfo _proxyProviderField;
        private readonly IObjectManipulator _objects;
        private readonly ISerializerSettings _defaultSettings;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        
        private static readonly MethodInfo _bytesToDouble;
        private static readonly MethodInfo _bytesToSingle;
        private static readonly MethodInfo _bytesToDecimal;

        // ReSharper disable once NotAccessedField.Local
        private static readonly MethodInfo _debugWriteline;

        private static readonly MethodInfo _extractPackedInt16Itar;
        private static readonly MethodInfo _extractPackedInt32Itar;
        private static readonly MethodInfo _extractPackedInt64Itar;

        //private static readonly MethodInfo _getArrayLength;
        private static readonly MethodInfo _getAutoProtoProxy;
        private static readonly MethodInfo _getColumnIndex;

        private static readonly MethodInfo _copyMemoryStream;
        private static readonly MethodInfo _setStreamLength;
        private static readonly MethodInfo _getStreamLength;

        ///// <summary>
        /////     BitConverter.GetBytes(Double)
        ///// </summary>
        //private static readonly MethodInfo _getDoubleBytes;

        /// <summary>
        ///     ProtoDynamicBase.GetInt32
        /// </summary>
        private static readonly MethodInfo _getInt32;

        /// <summary>
        ///     ProtoDynamicBase.GetInt64
        /// </summary>
        private static readonly MethodInfo _getInt64;

        //private static readonly MethodInfo _getPackedInt16Length;

        //private static readonly MethodInfo _getPackedInt32Length;
        //private static readonly MethodInfo _getPackedInt64Length;

        private static readonly MethodInfo _getPositiveInt32;
        private static readonly MethodInfo _getPositiveInt64;

        private static readonly MethodInfo _getProtoProxy;

        //private static readonly MethodInfo _getSingleBytes;

        ////////////////////////////////////////////////
        // READ
        ////////////////////////////////////////////////

        private static readonly MethodInfo _getStreamPosition;
        private static readonly MethodInfo _setStreamPosition;
        //private static readonly MethodInfo _getStringBytes;

        private static readonly MethodInfo _getUInt32;
        private static readonly MethodInfo _getUInt64;

        private static readonly MethodInfo _getStringFromBytes;
        
        //private readonly ModuleBuilder _moduleBuilder;
        

        private static readonly MethodInfo _dateFromFileTime;

        

        /// <summary>
        ///     Thread static Byte[]
        /// </summary>
        private readonly FieldInfo _readBytesField;

        private static readonly MethodInfo _readStreamByte;
        private static readonly MethodInfo _readStreamBytes;

        ///// <summary>
        /////     public static void Write(Byte[] vals, Stream _outStream)
        ///// </summary>
        //private static readonly MethodInfo _writeBytes;

        //private static readonly MethodInfo _writeInt16;
        //private static readonly MethodInfo _writeInt32;
        private static readonly MethodInfo _writeInt64;
        private static readonly MethodInfo _writeUInt64;

        //private static readonly MethodInfo _writeInt8;

        //private static readonly MethodInfo _writePacked16;
        //private static readonly MethodInfo _writePacked32;
        //private static readonly MethodInfo _writePacked64;
        //private static readonly MethodInfo _writeSomeBytes;
        //private static readonly MethodInfo _writeStreamByte;
        //private static readonly MethodInfo _writeUInt32;

        private static readonly FieldInfo _utf8;

        #if NET45 || NET40
        // ReSharper disable once StaticMemberInGenericType
        #if DEBUG

        #endif

        #endif

    }
}

#endif
