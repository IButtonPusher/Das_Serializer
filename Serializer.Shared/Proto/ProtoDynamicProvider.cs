using System;
using System.Collections;
using System.Collections.Generic;
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
    public partial class ProtoDynamicProvider<TPropertyAttribute> : IStreamAccessor, 
        IProtoProvider where TPropertyAttribute : Attribute
    {
        public ProtoDynamicProvider(ProtoBufOptions<TPropertyAttribute> protoSettings,
            ITypeManipulator typeManipulator, IInstantiator instantiator)
        {
            _protoSettings = protoSettings;
            _types = typeManipulator;
            _instantiator = instantiator;
            _lookupLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            _objects = new Dictionary<Type, ProtoDynamicBase>();
            _proxiesByDtoType = new Dictionary<Type, Type>();
            _proxyInstancesByDtoType = new Dictionary<Type, object>();

            var asmName = new AssemblyName("BOB.Stuff");
            // ReSharper disable once JoinDeclarationAndInitializer
            AssemblyBuilderAccess access;


#if NET45 || NET40
            access = AssemblyBuilderAccess.RunAndSave;
            //access = AssemblyBuilderAccess.Run;
            _asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, access);
            _moduleBuilder = _asmBuilder.DefineDynamicModule(AssemblyName, SaveFile);
            //_moduleBuilder = _asmBuilder.DefineDynamicModule(AssemblyName);
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
            _writeInt64 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteInt64), typeof(Int64), stream);
            _writeBytes = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.Write), typeof(Byte[]), stream);
            _writeSomeBytes = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.Write),
                typeof(Byte[]), typeof(Int32), stream);

            _writePacked16 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WritePacked16));
            _writePacked32 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WritePacked32));
            _writePacked64 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WritePacked64));

            _getPackedInt32Length = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.GetPackedArrayLength32));
            _getPackedInt16Length = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.GetPackedArrayLength16));
            _getPackedInt64Length = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.GetPackedArrayLength64));

            //_push = writer.GetMethodOrDie(nameof(IProtoWriter.Push), Type.EmptyTypes);
            //_pop = writer.GetMethodOrDie(nameof(IProtoWriter.Pop), Type.EmptyTypes);
            //_flush = writer.GetMethodOrDie(nameof(IProtoWriter.Flush), Type.EmptyTypes);

            var protoDynBase = typeof(ProtoDynamicBase);

            
            _utf8 = protoDynBase.GetStaticFieldOrDie("Utf8");

            //_outStreamField = writer.GetInstanceFieldOrDie("_outStream");
            //_stackDepthField= writer.GetInstanceFieldOrDie("_stackDepth");
            _proxyProviderField = protoDynBase.GetInstanceFieldOrDie("_proxyProvider");
            _getProtoProxy = typeof(IProtoProvider).GetMethodOrDie(nameof(IProtoProvider.GetProtoProxy));

            _getAutoProtoProxy = typeof(IProtoProvider).GetMethodOrDie(nameof(IProtoProvider.GetAutoProtoProxy));

            _getSingleBytes = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.GetBytes),
                typeof(Single));

            _getDoubleBytes = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.GetBytes),
                typeof(Double));

            _getStringBytes = typeof(UTF8Encoding).GetMethodOrDie(nameof(UTF8Encoding.GetBytes),
                typeof(String));

            _getArrayLength = typeof(Array).GetterOrDie(nameof(Array.Length), out _);

            var protoBase = typeof(ProtoDynamicBase);

            _getStreamLength = stream.GetterOrDie(nameof(Stream.Length), out _);
            _copyStreamTo = stream.GetMethodOrDie(nameof(Stream.CopyTo), stream, typeof(Int32));
            _copyMemoryStream = protoDynBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.CopyMemoryStream));
            _setStreamLength = stream.GetMethodOrDie(nameof(Stream.SetLength));
            _getStreamPosition = stream.GetterOrDie(nameof(Stream.Position), out _);
            _setStreamPosition = stream.SetterOrDie(nameof(Stream.Position));

            _readStreamByte = stream.GetMethodOrDie(nameof(Stream.ReadByte));
            _readStreamBytes = stream.GetMethodOrDie(nameof(Stream.Read));

            _writeStreamByte = stream.GetMethodOrDie(nameof(Stream.WriteByte));

            _getPositiveInt32 = protoBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.GetPositiveInt32));
            _getPositiveInt64 = protoBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetPositiveInt64));

            //_getInt16 = protoBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetInt16));
            _getInt32 = protoBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetInt32));
            _getInt64 = protoBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetInt64));

            _getColumnIndex = protoBase.GetPublicStaticMethodOrDie(nameof(ProtoDynamicBase.GetColumnIndex));


            _bytesToSingle = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.ToSingle),
                typeof(Byte[]), typeof(Int32));

            _bytesToDouble = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.ToDouble),
                typeof(Byte[]), typeof(Int32));

            _bytesToString = typeof(Encoding).GetMethodOrDie(nameof(Encoding.GetString),
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

            return GetProtoProxyImpl<T>(CreateProxyType<T>, allowReadOnly);

            //var forType = typeof(T);

            //var enterWrite = !_lookupLock.IsWriteLockHeld;
            //var enterRead = !enterWrite && !_lookupLock.IsReadLockHeld;

            //if (enterRead)
            //    _lookupLock.EnterUpgradeableReadLock();

            //try
            //{
            //    if (_proxyInstancesByDtoType.TryGetValue(forType, out var instance) &&
            //        instance is IProtoProxy<T> ready)
            //        return ready;

            //    if (_proxiesByDtoType.TryGetValue(forType, out var proxyType))
            //        return InstantiateProxyInstance<T>(proxyType);

            //    if (enterWrite)
            //        _lookupLock.EnterWriteLock();

            //    ///////////////////////////////////////
            //    var dynamicType = CreateProxyType<T>(allowReadOnly);
            //    ///////////////////////////////////////

            //    _proxiesByDtoType[forType] = dynamicType;
            //    ready = InstantiateProxyInstance<T>(dynamicType);
            //    _proxyInstancesByDtoType[forType] = ready;

            //    return ready;
            //}
            //finally
            //{
            //    //if (_lookupLock.IsWriteLockHeld)
            //    if (enterWrite)
            //        _lookupLock.ExitWriteLock();

            //    if (enterRead)
            //        _lookupLock.ExitUpgradeableReadLock();
            //}
        }

        private IProtoProxy<T> GetProtoProxyImpl<T>(
            Func<Boolean, Type> buildNew,
            Boolean allowReadOnly = false)
        {
            var forType = typeof(T);

            var enterWrite = !_lookupLock.IsWriteLockHeld;
            var enterRead = !enterWrite && !_lookupLock.IsReadLockHeld;

            if (enterRead)
                _lookupLock.EnterUpgradeableReadLock();


            try
            {
                if (_proxyInstancesByDtoType.TryGetValue(forType, out var instance) &&
                    instance is IProtoProxy<T> ready)
                {
                    enterWrite = false;
                    return ready;
                }

                if (_proxiesByDtoType.TryGetValue(forType, out var proxyType))
                {
                    enterWrite = false;
                    return InstantiateProxyInstance<T>(proxyType);
                }

                if (enterWrite)
                    _lookupLock.EnterWriteLock();

                ///////////////////////////////////////
                var dynamicType = buildNew(allowReadOnly);
                ///////////////////////////////////////

                _proxiesByDtoType[forType] = dynamicType;
                ready = InstantiateProxyInstance<T>(dynamicType);
                _proxyInstancesByDtoType[forType] = ready;

                return ready;
            }
            finally
            {
                //if (_lookupLock.IsWriteLockHeld)
                if (enterWrite)
                    _lookupLock.ExitWriteLock();

                if (enterRead)
                    _lookupLock.ExitUpgradeableReadLock();
            }
        }


        private Type CreateProxyType<T>(Boolean allowReadOnly)
        {
            var type = typeof(T);

            if (!_instantiator.TryGetDefaultConstructor(type, out var dtoctor) &&
                !allowReadOnly)
                throw new InvalidProgramException($"No valid constructor found for {type}");

            var fields = GetProtoFields(type);

            var genericParent = typeof(ProtoDynamicBase<>).MakeGenericType(type);
            var retBldr = genericParent.GetMethodOrDie(
                nameof(ProtoDynamicBase<Object>.BuildDefault));

            return CreateProxyTypeImpl<T>(dtoctor, fields, true, allowReadOnly, retBldr);

            
            
            //var typeName = type.FullName ?? throw new InvalidOperationException();

            //var bldr = _moduleBuilder.DefineType(typeName.Replace(".", "_"),
            //    TypeAttributes.Public | TypeAttributes.Class);

            //var genericParent = typeof(ProtoDynamicBase<>).MakeGenericType(type);

            //var childProxies = CreateProxyFields(bldr, fields);

            

            //var ctor = AddConstructors(bldr, dtoctor, 
            //    genericParent, childProxies);

            //AddPrintMethod(type, bldr, genericParent, //utf, 
            //    fields, childProxies);

            //if (ctor != null)
            //{
            //    var example = ctor.Invoke(new Object[0]);
            //    AddScanMethod(type, bldr, genericParent, fields, example!, childProxies);
            //    AddDtoInstantiator(type, bldr, genericParent, ctor);
            //}

            //bldr.SetParent(genericParent);
            
            //var dType = bldr.CreateType();

            //return dType;
        }

        private Type CreateProxyTypeImpl<T>(
            ConstructorInfo dtoCtor, 
            IEnumerable<IProtoFieldAccessor> fields,
            Boolean canSetValuesInline,
            Boolean allowReadOnly,
            MethodBase buildReturnValue)
        {
            var type = typeof(T);

            var fArr = fields.ToArray();
            
            var typeName = type.FullName ?? throw new InvalidOperationException();

            var bldr = _moduleBuilder.DefineType(typeName.Replace(".", "_"),
                TypeAttributes.Public | TypeAttributes.Class);

            var genericParent = typeof(ProtoDynamicBase<>).MakeGenericType(type);

            var childProxies = CreateProxyFields(bldr, fArr);


            var ctor = AddConstructors(bldr, dtoCtor, 
                genericParent, childProxies, allowReadOnly);

            AddPrintMethod(type, bldr, genericParent, //utf, 
                fArr, childProxies);

            if (ctor != null)
            {
                var example = canSetValuesInline ? ctor.Invoke(new Object[0]) : default;
                AddScanMethod(type, bldr, genericParent, fArr, 
                    example!, childProxies, canSetValuesInline, buildReturnValue);
                
                if (canSetValuesInline)
                    AddDtoInstantiator(type, bldr, genericParent, ctor);
            }

            bldr.SetParent(genericParent);
            
            var dType = bldr.CreateType();

            return dType;
        }

      


#if DEBUG

        public void DumpProxies()
        {
#if NET45 || NET40
            _asmBuilder.Save("protoTest.dll");
#endif
        }

#endif

        Boolean IProtoProvider.TryGetProtoField(PropertyInfo prop, Boolean isRequireAttribute,
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
                       pType == typeof(Int16) ||
                       pType == typeof(Int64) ||
                       pType == typeof(Byte) ||
                       pType == typeof(Boolean)
                    ? ProtoFieldAction.VarInt
                    : ProtoFieldAction.Primitive;

            if (pType == Const.StrType)
                return ProtoFieldAction.String;

            if (pType == Const.ByteArrayType)
                return ProtoFieldAction.ByteArray;

            if (GetPackedArrayType(pType) != null)
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

        




        private static IEnumerable<Byte> GetBytes(Int32 value)
        {
            if (value >= 0)
            {
                do
                {
                    var current = (Byte) (value & 127);
                    value >>= 7;
                    if (value > 0)
                        current += 128; //8th bit to specify more bytes remain
                    yield return current;
                } while (value > 0);
            }
            else
            {
                for (var c = 0; c <= 4; c++)
                {
                    var current = (Byte) (value | 128);
                    value >>= 7;
                    yield return current;
                }

                foreach (var b in _negative32Fill)
                    yield return b;
            }
        }


        private List<IProtoFieldAccessor> GetProtoFields(Type type)
        {
            var res = new List<IProtoFieldAccessor>();
            foreach (var prop in _types.GetPublicProperties(type))
                if (TryGetProtoField(prop, true, out var protoField))
                    res.Add(protoField);

            //var attribs = prop.GetCustomAttributes(typeof(TPropertyAttribute), true)
            //    .OfType<TPropertyAttribute>().ToArray();
            //if (attribs.Length == 0)
            //    continue;

            //var pType = prop.PropertyType;

            //var isCollection = _types.IsCollection(pType);
            //var index = _protoSettings.GetIndex(attribs[0]);

            //var wire = ProtoBufSerializer.GetWireType(pType);

            //var header = (Int32)wire + (index << 3);
            //var tc = Type.GetTypeCode(pType);

            //var getter = _types.CreatePropertyGetter(type, prop);

            //var protoField = new ProtoField(prop.Name, pType, wire, 
            //    index, header, getter, tc,
            //    _types.IsLeaf(pType, true), isCollection);

            //res.Add(protoField);

            return res;
        }

        private ProtoDynamicBase<TDto> InstantiateProxyInstance<TDto>(Type proxyType)
        {
            var res = Activator.CreateInstance(proxyType, this)
                      ?? throw new Exception(proxyType.Name);
            return (ProtoDynamicBase<TDto>) res;
        }

        private Int32 GetIndexFromAttribute(PropertyInfo prop)
        {
            var attribs = prop.GetCustomAttributes(typeof(TPropertyAttribute), true)
                .OfType<TPropertyAttribute>().ToArray();
            if (attribs.Length == 0)
            {
                return -1;
            }

            return _protoSettings.GetIndex(attribs[0]);
        }

        public Boolean TryGetProtoField(PropertyInfo prop, Boolean isRequireAttribute,
            out ProtoField field)
        {

            return TryGetProtoFieldImpl(prop, isRequireAttribute, GetIndexFromAttribute, out field);

            //var index = 0;

            //if (isRequireAttribute)
            //{
            //    var attribs = prop.GetCustomAttributes(typeof(TPropertyAttribute), true)
            //        .OfType<TPropertyAttribute>().ToArray();
            //    if (attribs.Length == 0)
            //    {
            //        field = default!;
            //        return false;
            //    }

            //    index = _protoSettings.GetIndex(attribs[0]);
            //}

            //var pType = prop.PropertyType;

            //var isCollection = _types.IsCollection(pType);


            //var wire = ProtoBufSerializer.GetWireType(pType);

            //var header = (Int32) wire + (index << 3);
            //var tc = Type.GetTypeCode(pType);


            //var getter = prop.GetGetMethod();
            //var setter = prop.CanWrite ? prop.GetSetMethod(true) : default!;
            ////_types.CreatePropertyGetter(prop.DeclaringType, prop);

            //var headerBytes = GetBytes(header).ToArray();

            //var fieldAction = GetProtoFieldAction(prop.PropertyType);

            //field = new ProtoField(prop.Name, pType, wire,
            //    index, header, getter, tc,
            //    _types.IsLeaf(pType, true), isCollection, fieldAction,
            //    headerBytes, setter);

            //return true;
        }

        public Boolean TryGetProtoFieldImpl(PropertyInfo prop, Boolean isRequireAttribute,
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

                //var attribs = prop.GetCustomAttributes(typeof(TPropertyAttribute), true)
                //    .OfType<TPropertyAttribute>().ToArray();
                //if (attribs.Length == 0)
                //{
                //    field = default!;
                //    return false;
                //}

                //index = _protoSettings.GetIndex(attribs[0]);
            }

            var pType = prop.PropertyType;

            var isCollection = _types.IsCollection(pType);


            var wire = ProtoBufSerializer.GetWireType(pType);

            var header = (Int32) wire + (index << 3);
            var tc = Type.GetTypeCode(pType);


            var getter = prop.GetGetMethod();
            var setter = prop.CanWrite ? prop.GetSetMethod(true) : default!;
            //_types.CreatePropertyGetter(prop.DeclaringType, prop);

            var headerBytes = GetBytes(header).ToArray();

            var fieldAction = GetProtoFieldAction(prop.PropertyType);

            field = new ProtoField(prop.Name, pType, wire,
                index, header, getter, tc,
                _types.IsLeaf(pType, true), isCollection, fieldAction,
                headerBytes, setter);

            return true;
        }

        private const string AssemblyName = "BOB.Stuff";

        private const MethodAttributes MethodOverride = MethodAttributes.Public |
                                                        MethodAttributes.HideBySig |
                                                        MethodAttributes.Virtual |
                                                        MethodAttributes.CheckAccessOnOverride
                                                        | MethodAttributes.Final;
#if NET45 || NET40
        // ReSharper disable once StaticMemberInGenericType
        private static readonly String SaveFile = $"{AssemblyName}.dll";
#endif

        // ReSharper disable once StaticMemberInGenericType
        private static readonly Byte[] _negative32Fill =
        {
            Byte.MaxValue, Byte.MaxValue,
            Byte.MaxValue, Byte.MaxValue, 1
        };

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly AssemblyBuilder _asmBuilder;
        private readonly MethodInfo _bytesToDouble;

        private readonly MethodInfo _bytesToSingle;

        /// <summary>
        ///     Encoding-> public virtual string GetString(byte[] bytes, int index, int count)
        /// </summary>
        private readonly MethodInfo _bytesToString;

        private readonly MethodInfo _copyMemoryStream;
        private readonly MethodInfo _copyStreamTo;


        // ReSharper disable once NotAccessedField.Local
        private readonly MethodInfo _debugWriteline;

        private readonly MethodInfo _extractPackedInt16Itar;
        private readonly MethodInfo _extractPackedInt32Itar;
        private readonly MethodInfo _extractPackedInt64Itar;

        private readonly MethodInfo _getArrayLength;
        private readonly MethodInfo _getColumnIndex;
        private readonly MethodInfo _getDoubleBytes;

        private readonly MethodInfo _getInt32;
        private readonly MethodInfo _getInt64;
        private readonly MethodInfo _getPackedInt16Length;

        private readonly MethodInfo _getPackedInt32Length;
        private readonly MethodInfo _getPackedInt64Length;

        /// <summary>
        /// static Int32 ProtoScanBase.GetPositiveInt32(Stream stream)
        /// </summary>
        private readonly MethodInfo _getPositiveInt32;
        private readonly MethodInfo _getPositiveInt64;

        private readonly MethodInfo _getProtoProxy;
        private readonly MethodInfo _getAutoProtoProxy;

        private readonly MethodInfo _getSingleBytes;

        //private readonly MethodInfo _push;
        //private readonly MethodInfo _pop;
        //private readonly MethodInfo _flush;


        ////////////////////////////////////////////////
        // READ
        ////////////////////////////////////////////////

        private readonly MethodInfo _getStreamLength;
        private readonly MethodInfo _getStreamPosition;
        private readonly MethodInfo _getStringBytes;
        private readonly IInstantiator _instantiator;
        private readonly ReaderWriterLockSlim _lookupLock;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly Dictionary<Type, ProtoDynamicBase> _objects;

        private readonly ProtoBufOptions<TPropertyAttribute> _protoSettings;

        private readonly Dictionary<Type, Type> _proxiesByDtoType;
        private readonly Dictionary<Type, Object> _proxyInstancesByDtoType;

        //private readonly FieldInfo _outStreamField;
        //private readonly FieldInfo _stackDepthField;

        private readonly FieldInfo _proxyProviderField;
        
        /// <summary>
        /// Thread static Byte[]
        /// </summary>
        private readonly FieldInfo _readBytesField;

        private readonly MethodInfo _readStreamByte;

        /// <summary>
        /// Stream.Read(...)
        /// </summary>
        private readonly MethodInfo _readStreamBytes;
        private readonly MethodInfo _setStreamLength;
        private readonly MethodInfo _setStreamPosition;

        private readonly ITypeManipulator _types;
        
        /// <summary>
        /// protected static Encoding Utf8;
        /// </summary>
        private readonly FieldInfo _utf8;

        /// <summary>
        ///     public static void Write(Byte[] vals, Stream _outStream)
        /// </summary>
        private readonly MethodInfo _writeBytes;

        private readonly MethodInfo _writeInt16;
        private readonly MethodInfo _writeInt32;
        private readonly MethodInfo _writeInt64;

        private readonly MethodInfo _writeInt8;

        private readonly MethodInfo _writePacked16;
        private readonly MethodInfo _writePacked32;
        private readonly MethodInfo _writePacked64;
        private readonly MethodInfo _writeSomeBytes;
        private readonly MethodInfo _writeStreamByte;

        public MethodInfo GetStreamLength => _getStreamLength;

        public MethodInfo SetStreamPosition => _setStreamPosition;

        public MethodInfo WriteInt64 => _writeInt64;

        public MethodInfo CopyMemoryStream => _copyMemoryStream;

        public MethodInfo SetStreamLength => _setStreamLength;

        public MethodInfo ReadStreamBytes => _readStreamBytes;

        public MethodInfo GetPositiveInt32 => _getPositiveInt32;

        public FieldInfo Utf8 => _utf8;

        public MethodInfo GetStringFromBytes => _bytesToString;

        private delegate void ScanMethod(
            Type parentType, 
            TypeBuilder bldr,
            Type genericParent, 
            IEnumerable<IProtoFieldAccessor> fields,
            Object? exampleObject,
            IDictionary<IProtoFieldAccessor, FieldBuilder> childProxies,
            Boolean canSetValuesInline);
    }
}