#if GENERATECODE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer.Remunerators;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    public partial class ProtoDynamicProvider<TPropertyAttribute>
        where TPropertyAttribute : Attribute
    {
        public ProtoDynamicProvider(ProtoBufOptions<TPropertyAttribute> protoSettings,
                                    ITypeManipulator typeManipulator,
                                    IInstantiator instantiator,
                                    IObjectManipulator objects)
        {
            _protoSettings = protoSettings;
            _types = typeManipulator;
            _instantiator = instantiator;
            _objects = objects;

            var asmName = new AssemblyName("BOB.Stuff");
            // ReSharper disable once JoinDeclarationAndInitializer
            AssemblyBuilderAccess access;


            #if NET45 || NET40
            access = AssemblyBuilderAccess.RunAndSave;

            _asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, access);
            _moduleBuilder = _asmBuilder.DefineDynamicModule(AssemblyName, SaveFile);

            #else
            access = AssemblyBuilderAccess.Run;
            _asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, access);
            _moduleBuilder = _asmBuilder.DefineDynamicModule(AssemblyName);
            #endif

            var writer = typeof(ProtoBufWriter);
            var bitConverter = typeof(BitConverter);

            var stream = typeof(Stream);

            _writeInt8 = writer.GetPublicStaticMethodOrDie(
                nameof(ProtoBufWriter.WriteInt8), typeof(Byte), stream);
            _writeInt16 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteInt16),
                typeof(Int16), stream);
            _writeInt32 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteInt32), typeof(Int32), stream);
            _writeUInt32 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteUInt32), typeof(UInt32), stream);
            
            WriteInt64 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteInt64), typeof(Int64), stream);
            WriteUInt64 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteUInt64), typeof(UInt64), stream);
            
            _writeBytes = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.Write), typeof(Byte[]), stream);
            _writeSomeBytes = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.Write),
                typeof(Byte[]), typeof(Int32), stream);

            _writePacked16 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WritePacked16));
            _writePacked32 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WritePacked32));
            _writePacked64 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WritePacked64));

            _getPackedInt32Length = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.GetPackedArrayLength32));
            _getPackedInt16Length = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.GetPackedArrayLength16));
            _getPackedInt64Length = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.GetPackedArrayLength64));

            var protoDynBase = typeof(ProtoDynamicBase);


            Utf8 = protoDynBase.GetStaticFieldOrDie("Utf8");

            _proxyProviderField = protoDynBase.GetInstanceFieldOrDie("_proxyProvider");
            _getProtoProxy = typeof(IProtoProvider).GetMethodOrDie(nameof(IProtoProvider.GetProtoProxy));

            _getAutoProtoProxy = typeof(IProtoProvider).GetMethodOrDie(nameof(IProtoProvider.GetAutoProtoProxy));

            _getSingleBytes = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.GetBytes),
                typeof(Single));

            _getDoubleBytes = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.GetBytes),
                typeof(Double));

            _getSingleBytes = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.GetBytes),
                typeof(Single));

            _getStringBytes = typeof(UTF8Encoding).GetMethodOrDie(nameof(UTF8Encoding.GetBytes),
                typeof(String));

            _getArrayLength = typeof(Array).GetterOrDie(nameof(Array.Length), out _);

            var protoBase = typeof(ProtoDynamicBase);

            GetStreamLength = stream.GetterOrDie(nameof(Stream.Length), out _);

            CopyMemoryStream = protoDynBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.CopyMemoryStream));
            SetStreamLength = stream.GetMethodOrDie(nameof(Stream.SetLength));
            _getStreamPosition = stream.GetterOrDie(nameof(Stream.Position), out _);
            SetStreamPosition = stream.SetterOrDie(nameof(Stream.Position));

            _readStreamByte = stream.GetMethodOrDie(nameof(Stream.ReadByte));
            ReadStreamBytes = stream.GetMethodOrDie(nameof(Stream.Read));

            _writeStreamByte = stream.GetMethodOrDie(nameof(Stream.WriteByte));

            GetPositiveInt32 = protoBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.GetPositiveInt32));
            _getPositiveInt64 = protoBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetPositiveInt64));

            _getInt32 = protoBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetInt32));
            _getInt64 = protoBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetInt64));

            _getUInt32 = protoBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetUInt32));
            _getUInt64 = protoBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetUInt64));

            _getColumnIndex = protoBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetColumnIndex));


            _bytesToSingle = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.ToSingle),
                typeof(Byte[]), typeof(Int32));

            _bytesToDouble = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.ToDouble),
                typeof(Byte[]), typeof(Int32));

            GetStringFromBytes = typeof(Encoding).GetMethodOrDie(nameof(Encoding.GetString),
                typeof(Byte[]), typeof(Int32), typeof(Int32));

            _debugWriteline = typeof(ProtoDynamicBase).GetMethodOrDie(
                nameof(ProtoDynamicBase.DebugWriteline));

            _readBytesField = protoDynBase.GetStaticFieldOrDie("_readBytes");

            _extractPackedInt16Itar = protoDynBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.ExtractPacked16));
            _extractPackedInt32Itar = protoDynBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.ExtractPacked32));
            _extractPackedInt64Itar = protoDynBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.ExtractPacked64));
        }

        public IProtoProxy<T> GetProtoProxy<T>(Boolean allowReadOnly = false)
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

        public ProtoFieldAction GetProtoFieldAction(Type pType)
        {
            if (pType.IsPrimitive)
                return pType == typeof(Int32) ||
                       pType == typeof(UInt32) ||
                       pType == typeof(Int16) ||
                       pType == typeof(Int64) ||
                       pType == typeof(UInt64) ||
                       pType == typeof(Byte) ||
                       pType == typeof(Boolean)
                    ? ProtoFieldAction.VarInt
                    : ProtoFieldAction.Primitive;

            if (pType == Const.StrType)
                return ProtoFieldAction.String;

            if (pType == Const.ByteArrayType)
                return ProtoFieldAction.ByteArray;

            if ( GetPackedArrayTypeCode(pType) != TypeCode.Empty)
                return ProtoFieldAction.PackedArray;

            if (typeof(IDictionary).IsAssignableFrom(pType))
                return ProtoFieldAction.Dictionary;

            if (_types.IsCollection(pType))
            {
                var germane = _types.GetGermaneType(pType);

                var subAction = GetProtoFieldAction(germane);

                switch (subAction)
                {
                    case ProtoFieldAction.ChildObject:
                        return pType.IsArray
                            ? ProtoFieldAction.ChildObjectArray
                            : ProtoFieldAction.ChildObjectCollection;

                    default:
                        return pType.IsArray
                            ? ProtoFieldAction.ChildPrimitiveArray
                            : ProtoFieldAction.ChildPrimitiveCollection;
                }
            }

            return ProtoFieldAction.ChildObject;
        }


        #if DEBUG
        #if NET45 || NET40

        #endif


        public void DumpProxies()
        {
            #if NET45 || NET40
            if (Interlocked.Increment(ref _dumpCount) > 1)
            {
                Debug.WriteLine("WARNING:  Proxies already dumped");
                return;
            }

            _asmBuilder.Save("protoTest.dll");
            #endif
        }

        #endif

        public MethodInfo GetStreamLength { get; }

        public MethodInfo SetStreamPosition { get; }

        public MethodInfo WriteInt64 { get; }

        public MethodInfo WriteUInt64 { get; }

        public MethodInfo CopyMemoryStream { get; }

        public MethodInfo SetStreamLength { get; }

        /// <summary>
        ///     Stream.Read(...)
        /// </summary>
        public MethodInfo ReadStreamBytes { get; }

        /// <summary>
        ///     static Int32 ProtoScanBase.GetPositiveInt32(Stream stream)
        /// </summary>
        public MethodInfo GetPositiveInt32 { get; }

        /// <summary>
        ///     protected static Encoding Utf8;
        /// </summary>
        public FieldInfo Utf8 { get; }

        /// <summary>
        ///     Encoding-> public virtual string GetString(byte[] bytes, int index, int count)
        /// </summary>
        public MethodInfo GetStringFromBytes { get; }

        public Boolean TryGetProtoField(PropertyInfo prop,
                                        Boolean isRequireAttribute,
                                        out ProtoField field)
        {
            return TryGetProtoFieldImpl(prop, isRequireAttribute, GetIndexFromAttribute, out field);
        }

        public Boolean TryGetProtoFieldImpl(PropertyInfo prop,
                                            Boolean isRequireAttribute,
                                            Func<PropertyInfo, Int32> getFieldIndex,
                                            out ProtoField field)
        {
            var index = 0;

            if (isRequireAttribute)
            {
                index = getFieldIndex(prop);
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
            var tc = Type.GetTypeCode(pType);

            var getter = prop.GetGetMethod() ?? throw new InvalidOperationException(prop.Name);
            var setter = prop.CanWrite ? prop.GetSetMethod(true) : default!;

            var headerBytes = GetBytes(header).ToArray();

            var fieldAction = GetProtoFieldAction(prop.PropertyType);

            field = new ProtoField(prop.Name, pType, wire,
                index, header, getter, tc,
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

            var fields = GetProtoPrintFields(type);

            var genericParent = typeof(ProtoDynamicBase<>).MakeGenericType(type);
            var retBldr = genericParent.GetMethodOrDie(
                nameof(ProtoDynamicBase<Object>.BuildDefault));

            return CreateProxyTypeImpl(type, dtoctor, fields, true, allowReadOnly, retBldr);
        }

        private Type? CreateProxyTypeImpl(Type type,
                                          ConstructorInfo dtoCtor,
                                          IEnumerable<IProtoFieldAccessor> scanFields,
                                          Boolean canSetValuesInline,
                                          Boolean allowReadOnly,
                                          MethodBase buildReturnValue)
        {
            var scanFieldArr = scanFields.ToArray();

            var typeName = type.FullName ?? throw new InvalidOperationException();

            var bldr = _moduleBuilder.DefineType(typeName.Replace(".", "_"),
                TypeAttributes.Public | TypeAttributes.Class);

            var genericParent = typeof(ProtoDynamicBase<>).MakeGenericType(type);

            var typeProxies = CreateProxyFields(bldr, scanFieldArr);

            var ctor = AddConstructors(bldr, dtoCtor,
                genericParent,
                typeProxies.Values,
                allowReadOnly);

            IEnumerable<IProtoFieldAccessor> printFields;
            if (canSetValuesInline || allowReadOnly)
                printFields = GetProtoPrintFields(type);
            else
                printFields = scanFieldArr;

            AddPrintMethod(type, bldr, genericParent, printFields, typeProxies);

            if (ctor != null)
            {
                var example = canSetValuesInline ? ctor.Invoke(new Object[0]) : default;
                AddScanMethod(type, bldr, genericParent, scanFieldArr,
                    example!, canSetValuesInline,
                    buildReturnValue, typeProxies);

                if (canSetValuesInline)
                    AddDtoInstantiator(type, bldr, genericParent, ctor);
            }

            bldr.SetParent(genericParent);

            var dType = bldr.CreateType();

            return dType;
        }

        private IProtoProxy<T> CreateProxyTypeNoReadOnly<T>()
        {
            var type = typeof(T);
            var ptype = CreateProxyType(type, false) ?? throw new TypeLoadException(type.Name);

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

        private Int32 GetIndexFromAttribute(PropertyInfo prop)
        {
            var attribs = prop.GetCustomAttributes(typeof(TPropertyAttribute), true)
                              .OfType<TPropertyAttribute>().ToArray();
            if (attribs.Length == 0) return -1;

            return _protoSettings.GetIndex(attribs[0]);
        }


        private List<IProtoFieldAccessor> GetProtoPrintFields(Type type)
        {
            var res = new List<IProtoFieldAccessor>();
            foreach (var prop in _types.GetPublicProperties(type))
            {
                if (TryGetProtoField(prop, true, out var protoField))
                    res.Add(protoField);
            }

            return res;
        }


        private IProtoProxy<T> InstantiateProxyInstance<T>(Type proxyType)
        {
            var res = Activator.CreateInstance(proxyType, this)
                      ?? throw new Exception(proxyType.Name);
            return (IProtoProxy<T>) res;
        }

        private const string AssemblyName = "BOB.Stuff";

        private const MethodAttributes MethodOverride = MethodAttributes.Public |
                                                        MethodAttributes.HideBySig |
                                                        MethodAttributes.Virtual |
                                                        MethodAttributes.CheckAccessOnOverride |
                                                        MethodAttributes.Final;

        // ReSharper disable once StaticMemberInGenericType
        private static Int32 _dumpCount;
        private static readonly String SaveFile = $"{AssemblyName}.dll";

        private static readonly Byte[] _negative32Fill =
        {
            Byte.MaxValue, Byte.MaxValue,
            Byte.MaxValue, Byte.MaxValue, 1
        };

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly AssemblyBuilder _asmBuilder;
        private readonly MethodInfo _bytesToDouble;

        private readonly MethodInfo _bytesToSingle;

        // ReSharper disable once NotAccessedField.Local
        private readonly MethodInfo _debugWriteline;

        private readonly MethodInfo _extractPackedInt16Itar;
        private readonly MethodInfo _extractPackedInt32Itar;
        private readonly MethodInfo _extractPackedInt64Itar;

        private readonly MethodInfo _getArrayLength;
        private readonly MethodInfo _getAutoProtoProxy;
        private readonly MethodInfo _getColumnIndex;
        
        /// <summary>
        /// BitConverter.GetBytes(Double)
        /// </summary>
        private readonly MethodInfo _getDoubleBytes;

        /// <summary>
        /// ProtoDynamicBase.GetInt32
        /// </summary>
        private readonly MethodInfo _getInt32;
        
        /// <summary>
        /// ProtoDynamicBase.GetInt64
        /// </summary>
        private readonly MethodInfo _getInt64;

        private readonly MethodInfo _getUInt32;
        private readonly MethodInfo _getUInt64;

        private readonly MethodInfo _getPackedInt16Length;

        private readonly MethodInfo _getPackedInt32Length;
        private readonly MethodInfo _getPackedInt64Length;

        private readonly MethodInfo _getPositiveInt64;

        private readonly MethodInfo _getProtoProxy;

        private readonly MethodInfo _getSingleBytes;

        ////////////////////////////////////////////////
        // READ
        ////////////////////////////////////////////////

        private readonly MethodInfo _getStreamPosition;
        private readonly MethodInfo _getStringBytes;
        private readonly IInstantiator _instantiator;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly IObjectManipulator _objects;

        private readonly ProtoBufOptions<TPropertyAttribute> _protoSettings;

        private readonly FieldInfo _proxyProviderField;

        /// <summary>
        ///     Thread static Byte[]
        /// </summary>
        private readonly FieldInfo _readBytesField;

        private readonly MethodInfo _readStreamByte;

        private readonly ITypeManipulator _types;

        /// <summary>
        ///     public static void Write(Byte[] vals, Stream _outStream)
        /// </summary>
        private readonly MethodInfo _writeBytes;

        private readonly MethodInfo _writeInt16;
        private readonly MethodInfo _writeInt32;
        private readonly MethodInfo _writeUInt32;

        private readonly MethodInfo _writeInt8;

        private readonly MethodInfo _writePacked16;
        private readonly MethodInfo _writePacked32;
        private readonly MethodInfo _writePacked64;
        private readonly MethodInfo _writeSomeBytes;
        private readonly MethodInfo _writeStreamByte;
        #if NET45 || NET40
        // ReSharper disable once StaticMemberInGenericType
        #if DEBUG

        #endif

        #endif
    }
}

#endif
