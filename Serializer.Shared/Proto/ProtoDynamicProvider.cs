using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
        private readonly ReaderWriterLockSlim _lookupLock;

        private readonly MethodInfo _writeInt8;
        private readonly MethodInfo _writeInt32;
        private readonly MethodInfo _writeInt64;
        private readonly MethodInfo _writeBytes;
        private readonly MethodInfo _writeSomeBytes;

        private readonly MethodInfo _getSingleBytes;
        private readonly MethodInfo _getDoubleBytes;
        private readonly MethodInfo _getStringBytes;

        private readonly MethodInfo _getArrayLength;

        private readonly MethodInfo _push;
        private readonly MethodInfo _pop;
        private readonly MethodInfo _flush;

        private readonly MethodInfo _getStreamLength;
        private readonly MethodInfo _getStreamPosition;
        private readonly MethodInfo _readStreamByte;
        private readonly MethodInfo _writeStreamByte;
        private readonly MethodInfo _unsafeStackByte;
        private readonly MethodInfo _readStreamBytes;
        private readonly MethodInfo _getPositiveInt32;
        private readonly MethodInfo _getPositiveInt64;
        private readonly MethodInfo _getColumnIndex;
        private readonly MethodInfo _getInt32;
        private readonly MethodInfo _getInt64;
        private readonly MethodInfo _bytesToString;

        private readonly MethodInfo _bytesToSingle;
        private readonly MethodInfo _bytesToDouble;
        private readonly FieldInfo _utf8;
        private readonly FieldInfo _outStreamField;
        private readonly FieldInfo _stackDepthField;

        

        private const MethodAttributes MethodOverride = MethodAttributes.Public |
                                                        MethodAttributes.HideBySig |
                                                        MethodAttributes.Virtual |
                                                        MethodAttributes.CheckAccessOnOverride
                                                        | MethodAttributes.Final;

        private const BindingFlags PublicStatic = BindingFlags.Static | BindingFlags.Public;
        private const BindingFlags NonPublicStatic = BindingFlags.Static | BindingFlags.NonPublic;

        private const BindingFlags Public = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags PrivateOrProtected = BindingFlags.Instance | BindingFlags.NonPublic;


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

            _writeInt8 = writer.GetMethodOrDie(nameof(IProtoWriter.WriteInt8), typeof(Byte));
            _writeInt32 = writer.GetMethodOrDie(nameof(IProtoWriter.WriteInt32), typeof(Int32));
            _writeInt64 = writer.GetMethodOrDie(nameof(IProtoWriter.WriteInt64), typeof(Int64));
            _writeBytes = writer.GetMethodOrDie(nameof(IProtoWriter.Write), typeof(Byte[]));
            _writeSomeBytes = writer.GetMethodOrDie(nameof(IProtoWriter.Write), typeof(Byte[]), 
                typeof(Int32));

            _push = writer.GetMethodOrDie(nameof(IProtoWriter.Push), Type.EmptyTypes);
            _pop = writer.GetMethodOrDie(nameof(IProtoWriter.Pop), Type.EmptyTypes);
            _flush = writer.GetMethodOrDie(nameof(IProtoWriter.Flush), Type.EmptyTypes);

            _utf8 = typeof(ProtoDynamicBase).GetField("Utf8", NonPublicStatic);

            _outStreamField = typeof(ProtoBufWriter).GetField("_outStream", PrivateOrProtected);
            _stackDepthField= typeof(ProtoBufWriter).GetField("_stackDepth", PrivateOrProtected);

            _getSingleBytes = typeof(BitConverter).GetMethodOrDie(nameof(BitConverter.GetBytes),
                PublicStatic, typeof(Single));

            _getDoubleBytes = typeof(BitConverter).GetMethodOrDie(nameof(BitConverter.GetBytes),
                PublicStatic, typeof(Double));

            _getStringBytes = typeof(UTF8Encoding).GetMethodOrDie(nameof(UTF8Encoding.GetBytes), 
                typeof(String));

            var arrayLengthProp = typeof(Array).GetProperty(nameof(Array.Length))
                                  ?? throw new InvalidOperationException();
            _getArrayLength = arrayLengthProp.GetGetMethod();

            var protoBase = typeof(ProtoDynamicBase);

            _getStreamLength = GetOrDie<Stream>(nameof(Stream.Length));
            _getStreamPosition = GetOrDie<Stream>(nameof(Stream.Position));

            _readStreamByte = typeof(Stream).GetMethodOrDie(nameof(Stream.ReadByte));
            _readStreamBytes = typeof(Stream).GetMethodOrDie(nameof(Stream.Read));

            _writeStreamByte = typeof(Stream).GetMethodOrDie(nameof(Stream.WriteByte));
            _unsafeStackByte = typeof(ProtoBufWriter).GetMethodOrDie(
                nameof(ProtoBufWriter.UnsafeStackByte), Public);

            _getPositiveInt32 = protoBase.GetMethodOrDie(nameof(ProtoDynamicBase.GetPositiveInt32),
                PublicStatic);
            _getPositiveInt64 = protoBase.GetMethodOrDie(nameof(ProtoDynamicBase.GetPositiveInt64),
                PublicStatic);
            _getInt32 = protoBase.GetMethodOrDie(nameof(ProtoDynamicBase.GetInt32),
                PublicStatic);
            _getInt64 = protoBase.GetMethodOrDie(nameof(ProtoDynamicBase.GetInt64),
                PublicStatic);
            _getColumnIndex = protoBase.GetMethodOrDie(nameof(ProtoDynamicBase.GetColumnIndex),
                PublicStatic);
             

            _bytesToSingle = typeof(BitConverter).GetMethodOrDie(nameof(BitConverter.ToSingle),
                PublicStatic, typeof(Byte[]), typeof(Int32));

            _bytesToDouble = typeof(BitConverter).GetMethodOrDie(nameof(BitConverter.ToDouble),
                PublicStatic, typeof(Byte[]), typeof(Int32));

            _bytesToString = typeof(Encoding).GetMethodOrDie(nameof(Encoding.GetString), 
                typeof(Byte[]), typeof(Int32), typeof(Int32));

            _debugWriteline = typeof(ProtoDynamicBase).GetMethodOrDie(
                nameof(ProtoDynamicBase.DebugWriteline));

        }

        public IProtoProxy<T> GetProtoProxy<T>() where T: class
        {
            var forType = typeof(T);

            _lookupLock.EnterUpgradeableReadLock();
            if (!_objects.TryGetValue(forType, out var found))
            {
                _lookupLock.EnterWriteLock();
                var ctor = _instantiator.GetDefaultConstructor<T>();
                found = BuildProtoDynamicObject(ctor);
                _objects[forType] = found;
                _lookupLock.ExitWriteLock();
            }
            _lookupLock.ExitUpgradeableReadLock();
            return (ProtoDynamicBase<T>)found;
        }

        private ProtoDynamicBase BuildProtoDynamicObject<T>(Func<T> ctor)
        {
            var type = typeof(T);
            var fields = GetProtoFields(type);
            var typeName = type.FullName ?? throw new InvalidOperationException();

            var bldr = _moduleBuilder.DefineType(typeName.Replace(".", "_"),
                TypeAttributes.Public | TypeAttributes.Class);

            var utf = bldr.DefineField("_utf8", typeof(Encoding), FieldAttributes.Private);

            var genericParent = typeof(ProtoDynamicBase<>).MakeGenericType(type);

            AddConstructor(bldr, utf, genericParent);

            

            AddPrintMethod(type, bldr, genericParent, utf, fields);
            AddScanMethod(type, bldr, genericParent, fields);
            

            bldr.SetParent(genericParent);

            var dynamicType = bldr.CreateType();


            ////////////////////////////////
#if NET45 || NET40
            //_asmBuilder.Save("protoTest.dll");
#endif
            ////////////////////////////////


            

            return (ProtoDynamicBase)Activator.CreateInstance(dynamicType, ctor);
        }

        private void AddConstructor(TypeBuilder bldr, FieldInfo utfField, Type genericBase)
        {
            var baseCtors = genericBase.GetConstructors(BindingFlags.Public |
                                                        BindingFlags.NonPublic |
                                                        BindingFlags.Instance |
                                                        BindingFlags.FlattenHierarchy);

            foreach (var ctor in baseCtors)
                BuildOverrideConstructor(ctor, bldr, utfField);
        }

        private void BuildOverrideConstructor(ConstructorInfo baseCtor, TypeBuilder builder,
            FieldInfo utfField)
        {
            var paramList = new List<Type>();
            paramList.AddRange(baseCtor.GetParameters().Select(p => p.ParameterType));

            var ctorParams = paramList.ToArray();

            var ctor = builder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                ctorParams);

            var il = ctor.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);

            il.Emit(OpCodes.Call, baseCtor);

            var getUtf8 = GetOrDie<Encoding>(nameof(Encoding.UTF8),
                PublicStatic);                
               
            var utf = il.DeclareLocal(typeof(Encoding));

            il.Emit(OpCodes.Call, getUtf8);
            il.Emit(OpCodes.Stloc, utf);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, utf);
            il.Emit(OpCodes.Stfld, utfField);


            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Nop);

            il.Emit(OpCodes.Ret);
        }

        private static MethodInfo GetOrDie<TTYpe>(String property, BindingFlags flags = Public)
        {
            return typeof(TTYpe).GetProperty(property, flags)?.GetGetMethod() ??
                   throw new InvalidOperationException();
        }

        private static MethodInfo GetOrDie(Type tType, String property, 
            BindingFlags flags = Public)
        {
            return tType.GetProperty(property, flags)?.GetGetMethod() ??
                   throw new InvalidOperationException();
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

        

        private IList<IProtoField> GetProtoFields(Type type)
        {
            var res = new List<IProtoField>();
            foreach (var prop in _types.GetPublicProperties(type))
            {
                var attribs = prop.GetCustomAttributes(typeof(TPropertyAttribute), true)
                    .OfType<TPropertyAttribute>().ToArray();
                if (attribs.Length == 0)
                    continue;

                var pType = prop.PropertyType;

                var wire = ProtoBufSerializer.GetWireType(pType);
                var index = _protoSettings.GetIndex(attribs[0]);
               
                var header = (Int32)wire + (index << 3);
                var tc = Type.GetTypeCode(pType);

                var isValidLengthDelim = wire == ProtoWireTypes.LengthDelimited
                                         && pType != Const.StrType 
                                         && pType != Const.ByteArrayType;

                var isCollection = isValidLengthDelim && _types.IsCollection(pType);

                var getter = _types.CreatePropertyGetter(type, prop);

                var protoField = new ProtoField(prop.Name, pType, wire, 
                    index, header, getter, tc,
                    _types.IsLeaf(pType, true), isCollection);

                res.Add(protoField);
            }

            return res;
        }
    }
}
