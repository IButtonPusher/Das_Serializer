using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Das.Extensions;
using Das.Serializer.Remunerators;

namespace Das.Serializer.ProtoBuf
{
    public partial class ProtoDynamicProvider<TPropertyAttribute> : 
        IProtoProvider where TPropertyAttribute : Attribute
    {
        private const string AssemblyName = "BOB.Stuff";
#if NET45 || NET40
        // ReSharper disable once StaticMemberInGenericType
        private static readonly String SaveFile = $"{AssemblyName}.dll";
#endif

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly AssemblyBuilder _asmBuilder;
        private readonly ModuleBuilder _moduleBuilder;

        private readonly ProtoBufOptions<TPropertyAttribute> _protoSettings;

        private readonly ITypeManipulator _types;
        private readonly IInstantiator _instantiator;
        private readonly Dictionary<Type, ProtoDynamicBase> _objects;
        private readonly Dictionary<Type, Type> _proxiesByDtoType;
        private readonly ReaderWriterLockSlim _lookupLock;

        private readonly MethodInfo _writeInt8;
        private readonly MethodInfo _writeInt16;
        private readonly MethodInfo _writeInt32;
        private readonly MethodInfo _writeInt64;
        private readonly MethodInfo _writeBytes;
        private readonly MethodInfo _writeSomeBytes;

        private readonly MethodInfo _writePacked16;
        private readonly MethodInfo _writePacked32;
        private readonly MethodInfo _writePacked64;

        private readonly MethodInfo _getPackedInt32Length;
        private readonly MethodInfo _getPackedInt16Length;
        private readonly MethodInfo _getPackedInt64Length;

        private readonly MethodInfo _getSingleBytes;
        private readonly MethodInfo _getDoubleBytes;
        private readonly MethodInfo _getStringBytes;

        private readonly MethodInfo _getArrayLength;

        private readonly MethodInfo _push;
        private readonly MethodInfo _pop;
        private readonly MethodInfo _flush;

        
        ////////////////////////////////////////////////
        // READ
        ////////////////////////////////////////////////

        private readonly MethodInfo _getStreamLength;
        private readonly MethodInfo _getStreamPosition;
        private readonly MethodInfo _setStreamPosition;
        private readonly MethodInfo _readStreamByte;
        private readonly MethodInfo _writeStreamByte;
        private readonly MethodInfo _unsafeStackByte;
        private readonly MethodInfo _readStreamBytes;
        private readonly MethodInfo _copyStreamTo;
        private readonly MethodInfo _setStreamLength;

        private readonly MethodInfo _getPositiveInt32;
        private readonly MethodInfo _getPositiveInt64;
        private readonly MethodInfo _getColumnIndex;
        
        private readonly MethodInfo _getInt32;
        private readonly MethodInfo _getInt64;
        private readonly MethodInfo _bytesToString;

        private readonly MethodInfo _extractPackedInt16Itar;
        private readonly MethodInfo _extractPackedInt32Itar;
        private readonly MethodInfo _extractPackedInt64Itar;

        private readonly MethodInfo _bytesToSingle;
        private readonly MethodInfo _bytesToDouble;
        private readonly FieldInfo _utf8;
        private readonly FieldInfo _readBytes;
        //private readonly FieldInfo _outStreamField;
        private readonly FieldInfo _stackDepthField;

        private readonly FieldInfo _proxyProviderField;

        private readonly MethodInfo _getProtoProxy;

        private const MethodAttributes MethodOverride = MethodAttributes.Public |
                                                        MethodAttributes.HideBySig |
                                                        MethodAttributes.Virtual |
                                                        MethodAttributes.CheckAccessOnOverride
                                                        | MethodAttributes.Final;

      

        // ReSharper disable once NotAccessedField.Local
        private readonly MethodInfo _debugWriteline;

        public ProtoDynamicProvider(ProtoBufOptions<TPropertyAttribute> protoSettings,
            ITypeManipulator typeManipulator, IInstantiator instantiator)
        {
            _protoSettings = protoSettings;
            _types = typeManipulator;
            _instantiator = instantiator;
            _lookupLock = new ReaderWriterLockSlim();
            _objects = new Dictionary<Type, ProtoDynamicBase>();
            _proxiesByDtoType = new Dictionary<Type, Type>();

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

            _writeInt8 = writer.GetMethodOrDie(nameof(IProtoWriter.WriteInt8), typeof(Byte));
            _writeInt16 = writer.GetMethodOrDie(nameof(IProtoWriter.WriteInt16), typeof(Int16));
            _writeInt32 = writer.GetMethodOrDie(nameof(IProtoWriter.WriteInt32), typeof(Int32));
            _writeInt64 = writer.GetMethodOrDie(nameof(IProtoWriter.WriteInt64), typeof(Int64));
            _writeBytes = writer.GetMethodOrDie(nameof(IProtoWriter.Write), typeof(Byte[]));
            _writeSomeBytes = writer.GetMethodOrDie(nameof(IProtoWriter.Write), typeof(Byte[]), 
                typeof(Int32));
            
            _writePacked16 = writer.GetMethodOrDie(nameof(IProtoWriter.WritePacked16));
            _writePacked32 = writer.GetMethodOrDie(nameof(IProtoWriter.WritePacked32));
            _writePacked64 = writer.GetMethodOrDie(nameof(IProtoWriter.WritePacked64));

            _getPackedInt32Length= writer.GetMethodOrDie(nameof(IProtoWriter.GetPackedArrayLength32));
            _getPackedInt16Length= writer.GetMethodOrDie(nameof(IProtoWriter.GetPackedArrayLength16));
            _getPackedInt64Length= writer.GetMethodOrDie(nameof(IProtoWriter.GetPackedArrayLength64));

            _push = writer.GetMethodOrDie(nameof(IProtoWriter.Push), Type.EmptyTypes);
            _pop = writer.GetMethodOrDie(nameof(IProtoWriter.Pop), Type.EmptyTypes);
            _flush = writer.GetMethodOrDie(nameof(IProtoWriter.Flush), Type.EmptyTypes);

            var protoDynBase = typeof(ProtoDynamicBase);

            _utf8 = protoDynBase.GetStaticFieldOrDie("Utf8");

            //_outStreamField = writer.GetInstanceFieldOrDie("_outStream");
            _stackDepthField= writer.GetInstanceFieldOrDie("_stackDepth");
            _proxyProviderField = protoDynBase.GetInstanceFieldOrDie("_proxyProvider");
            _getProtoProxy = typeof(IProtoProvider).GetMethod(nameof(IProtoProvider.GetProtoProxy));

            _getSingleBytes = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.GetBytes),
                typeof(Single));

            _getDoubleBytes = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.GetBytes),
                typeof(Double));

            _getStringBytes = typeof(UTF8Encoding).GetMethodOrDie(nameof(UTF8Encoding.GetBytes), 
                typeof(String));

            _getArrayLength = typeof(Array).GetterOrDie(nameof(Array.Length), out _);

            var protoBase = typeof(ProtoDynamicBase);

            var stream = typeof(Stream);

            _getStreamLength = stream.GetterOrDie(nameof(Stream.Length), out _);
            _copyStreamTo = stream.GetMethodOrDie(nameof(Stream.CopyTo), stream);
            _setStreamLength = stream.GetMethodOrDie(nameof(Stream.SetLength));
            _getStreamPosition = stream.GetterOrDie(nameof(Stream.Position), out _);
            _setStreamPosition = stream.SetterOrDie(nameof(Stream.Position));

            _readStreamByte = stream.GetMethodOrDie(nameof(Stream.ReadByte));
            _readStreamBytes = stream.GetMethodOrDie(nameof(Stream.Read));

            _writeStreamByte = stream.GetMethodOrDie(nameof(Stream.WriteByte));

            _unsafeStackByte = writer.GetMethodOrDie(
                nameof(ProtoBufWriter.UnsafeStackByte), Const.PublicInstance);

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

            _readBytes = protoDynBase.GetStaticFieldOrDie(nameof(_readBytes));

            _extractPackedInt16Itar =protoDynBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.ExtractPacked16));
            _extractPackedInt32Itar = protoDynBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.ExtractPacked32));
            _extractPackedInt64Itar = protoDynBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.ExtractPacked64));

        }

        //private static MethodInfo GetOrDie<TTYpe>(String property, BindingFlags flags = Const.PublicInstance)
        //{
        //    return typeof(TTYpe).GetProperty(property, flags)?.GetGetMethod() ??
        //           throw new InvalidOperationException();
        //}


        public IProtoProxy<T> GetProtoProxy<T>(Boolean allowReadOnly) 
            where T: class
        {
            var forType = typeof(T);

            _lookupLock.EnterUpgradeableReadLock();

            try
            {
                if (_proxiesByDtoType.TryGetValue(forType, out var proxyType))
                    return InstantiateProxyInstance<T>(proxyType);

                _lookupLock.EnterWriteLock();

                var dynamicType = CreateProxyType<T>(allowReadOnly);
                _proxiesByDtoType[forType] = dynamicType;
                return InstantiateProxyInstance<T>(dynamicType);

            }
            finally
            {
                if (_lookupLock.IsWriteLockHeld)
                    _lookupLock.ExitWriteLock();
                _lookupLock.ExitUpgradeableReadLock();
            }

            //if (!_objects.TryGetValue(forType, out var found) || 
            //    found.IsReadOnly&& !allowReadOnly)
            //{
            //    _lookupLock.EnterWriteLock();
            //    if (!_instantiator.TryGetDefaultConstructorDelegate<T>(out var ctor) &&
            //        !allowReadOnly)
            //    {
            //        throw new InvalidProgramException($"No valid constructor found for {typeof(T)}");
            //    }

            //    found = BuildProtoDynamicObject(ctor)!;
            //    _objects[forType] = found;
            //    _lookupLock.ExitWriteLock();
            //}
            //_lookupLock.ExitUpgradeableReadLock();
            //return (ProtoDynamicBase<T>)found!;
        }

        private Type CreateProxyType<T>(Boolean allowReadOnly)
            where T: class
        {
            var type = typeof(T);
            var fields = GetProtoFields(type);
            var typeName = type.FullName ?? throw new InvalidOperationException();

            if (!_instantiator.TryGetDefaultConstructor<T>(out var ctor) &&
                !allowReadOnly)
            {
                throw new InvalidProgramException($"No valid constructor found for {typeof(T)}");
            }

            var bldr = _moduleBuilder.DefineType(typeName.Replace(".", "_"),
                TypeAttributes.Public | TypeAttributes.Class);

            var utf = bldr.DefineField("_utf8", typeof(Encoding), FieldAttributes.Private);

            var genericParent = typeof(ProtoDynamicBase<>).MakeGenericType(type);

            AddConstructor(bldr, utf, genericParent, ctor);

            AddPrintMethod(type, bldr, genericParent, utf, fields);

            if (ctor != null)
            {
                var example = ctor.Invoke(new Object[0]);
                AddScanMethod(type, bldr, genericParent, fields, example!);
                AddDtoInstantiator<T>(type, bldr, genericParent, ctor);
            }

            bldr.SetParent(genericParent);

            var dType =  bldr.CreateType();

            //DumpProxies();
            

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

        private ProtoDynamicBase<TDto> InstantiateProxyInstance<TDto>(Type proxyType)
        {
            var instance = (ProtoDynamicBase<TDto>)Activator.CreateInstance(proxyType, this);
            return instance;
        }


//        private ProtoDynamicBase? BuildProtoDynamicObject<T>(Func<T>? ctor)
//        {
//            var type = typeof(T);
//            var fields = GetProtoFields(type);
//            var typeName = type.FullName ?? throw new InvalidOperationException();

//            var bldr = _moduleBuilder.DefineType(typeName.Replace(".", "_"),
//                TypeAttributes.Public | TypeAttributes.Class);

//            var utf = bldr.DefineField("_utf8", typeof(Encoding), FieldAttributes.Private);

//            var genericParent = typeof(ProtoDynamicBase<>).MakeGenericType(type);

//            AddConstructor(bldr, utf, genericParent);

//            AddPrintMethod(type, bldr, genericParent, utf, fields);

//            if (ctor != null)
//            {
//                var example = ctor();
//                AddScanMethod(type, bldr, genericParent, fields, example!);
//                AddDtoInstantiator(type, bldr, genericParent, ctor);
//            }

//            bldr.SetParent(genericParent);

//            var dynamicType = bldr.CreateType();


//            ////////////////////////////////
////#if NET45 || NET40
//            //_asmBuilder.Save("protoTest.dll");
////#endif
//            ////////////////////////////////


//            if (dynamicType == null)
//                return default;

//            return (ProtoDynamicBase)Activator.CreateInstance(dynamicType, ctor)!;
//        }

       

     

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
                    var current = (Byte)(value | 128);
                    value >>= 7;
                    yield return current;
                }
                foreach (var b in _negative32Fill)
                    yield return b;
            }
        }

        // ReSharper disable once StaticMemberInGenericType
        private static readonly Byte[] _negative32Fill = { Byte.MaxValue, Byte.MaxValue, 
            Byte.MaxValue, Byte.MaxValue, 1};

        

        private List<IProtoFieldAccessor> GetProtoFields(Type type)
        {
            var res = new List<IProtoFieldAccessor>();
            foreach (var prop in _types.GetPublicProperties(type))
            {
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
            }

            return res;
        }

        public Boolean TryGetProtoField(PropertyInfo prop, Boolean isRequireAttribute,
            out ProtoField field)
        {
            var index = 0;

            if (isRequireAttribute)
            {
                var attribs = prop.GetCustomAttributes(typeof(TPropertyAttribute), true)
                    .OfType<TPropertyAttribute>().ToArray();
                if (attribs.Length == 0)
                {
                    field = default!;
                    return false;
                }

                index = _protoSettings.GetIndex(attribs[0]);
            }

            var pType = prop.PropertyType;

            var isCollection = _types.IsCollection(pType);
            

               
            var wire = ProtoBufSerializer.GetWireType(pType);

            var header = (Int32)wire + (index << 3);
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

        public ProtoFieldAction GetProtoFieldAction(Type pType)
        {
            if (pType.IsPrimitive)
            {
                return pType == typeof(Int32) ||
                       pType == typeof(Int16) ||
                       pType == typeof(Int64)
                    ? ProtoFieldAction.VarInt
                    : ProtoFieldAction.Primitive;
            }

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
                return pType.IsArray 
                    ? ProtoFieldAction.ChildObjectArray 
                    : ProtoFieldAction.ChildObjectCollection;
                
            }

            return ProtoFieldAction.ChildObject;

        }
    }
}
