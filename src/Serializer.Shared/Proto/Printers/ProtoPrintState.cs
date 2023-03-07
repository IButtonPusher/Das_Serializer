#if GENERATECODE

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer.CodeGen;
using Das.Serializer.Properties;
using Das.Serializer.Proto;
using Das.Serializer.Remunerators;
using Das.Serializer.State;
using Reflection.Common;

namespace Das.Serializer.ProtoBuf
{
    public partial class ProtoPrintState : ProtoStateBase, IEnumerable<ProtoPrintState>,
                                           IProtoPrintState
    {
        static ProtoPrintState()
        {
            var stream = typeof(Stream);
            var writer = typeof(ProtoBufWriter);
            var protoDynBase = typeof(ProtoDynamicBase);

            _writeInt8 = writer.GetPublicStaticMethodOrDie(
                nameof(ProtoBufWriter.WriteInt8), typeof(Byte), stream);
            _writeChar = writer.GetPublicStaticMethodOrDie(
                nameof(ProtoBufWriter.WriteChar), typeof(Char), stream);

            _writeInt16 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteInt16),
                typeof(Int16), stream);
            _writeUInt16 =
                writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteUInt16), typeof(UInt16), stream);
            _writeInt32 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteInt32), typeof(Int32), stream);
            _writeUInt32 =
                writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteUInt32), typeof(UInt32), stream);


            _writeBytes = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.Write), typeof(Byte[]), stream);
            _writeSomeBytes = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.Write),
                typeof(Byte[]), typeof(Int32), stream);

            _writePacked16 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WritePacked16));
            _writePacked32 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WritePacked32));
            _writePacked64 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WritePacked64));

            _writeInt64 = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteInt64), typeof(Int64), stream);
            _writeUInt64 =
                writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.WriteUInt64), typeof(UInt64), stream);

            _writeStreamByte = stream.GetMethodOrDie(nameof(Stream.WriteByte));

            _getStringBytes = typeof(UTF8Encoding).GetMethodOrDie(nameof(UTF8Encoding.GetBytes),
                typeof(String));

            _getStreamLength = stream.GetterOrDie(nameof(Stream.Length), out _);
            _setStreamPosition = stream.SetterOrDie(nameof(Stream.Position));
            _copyMemoryStream = protoDynBase.GetPublicStaticMethodOrDie(
                nameof(ProtoDynamicBase.CopyMemoryStream));
            _setStreamLength = stream.GetMethodOrDie(nameof(Stream.SetLength));

            var bitConverter = typeof(BitConverter);

            _getDecimalBytes = typeof(ExtensionMethods).GetPublicStaticMethodOrDie(
                nameof(ExtensionMethods.ToByteArray), typeof(Decimal));

            _getDoubleBytes = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.GetBytes),
                typeof(Double));

            _getSingleBytes = bitConverter.GetPublicStaticMethodOrDie(nameof(BitConverter.GetBytes),
                typeof(Single));

            _getPackedInt32Length = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.GetPackedArrayLength32));
            _getPackedInt16Length = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.GetPackedArrayLength16));
            _getPackedInt64Length = writer.GetPublicStaticMethodOrDie(nameof(ProtoBufWriter.GetPackedArrayLength64));

            _dateToFileTime = typeof(DateTime).GetMethodOrDie(nameof(DateTime.ToFileTime));
        

            Utf8 = protoDynBase.GetPrivateStaticFieldOrDie("Utf8");
        }

        public ProtoPrintState(ILGenerator il,
                               Boolean isArrayMade,
                               IEnumerable<IProtoFieldAccessor> fields,
                               Type parentType,
                               Action<ILGenerator> loadObject,
                               ITypeManipulator typeCore,
                               IStreamAccessor streamAccessor,
                               IProtoFieldAccessor currentField,
                               IDictionary<Type, ProxiedInstanceField> proxies,
                               IFieldActionProvider actionProvider)
            : base(il, currentField,
                parentType, loadObject, proxies, typeCore, actionProvider)
        {
            _typeCore = typeCore;

            IsArrayMade = isArrayMade;

            FieldByteArray = il.DeclareLocal(typeof(Byte[]));
            LocalBytes = il.DeclareLocal(typeof(Byte[]));

            _streamAccessor = streamAccessor;
            Fields = fields.OrderBy(f => f.Index).ToArray();
        }

        IEnumerator<IDynamicPrintState<IProtoFieldAccessor, bool>> IEnumerable<IDynamicPrintState<IProtoFieldAccessor, bool>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        //IEnumerator<IDynamicPrintState<IProtoFieldAccessor, bool, ILGenerator>> IEnumerable<IDynamicPrintState<IProtoFieldAccessor, bool, ILGenerator>>.GetEnumerator()
        //{
        //    return GetEnumerator();
        //}

        public IEnumerator<ProtoPrintState> GetEnumerator()
        {
            for (var c = 0; c < Fields.Length; c++)
            {
                _currentField = Fields[c];
                yield return this;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public void PrintFieldViaProxy(Action loadFieldValue)
        {
            var protoField = _currentField;

            PrintFieldHeader(protoField);

            var fieldInfo = LoadFieldProxy(protoField);
            var proxyType = fieldInfo.FieldType;

            var printMethod = proxyType.GetMethodOrDie(nameof(IProtoProxy<Object>.Print));

            /////////////////////////////////////////////////////
            // CALL THE PRINT METHOD ON THE PROXY WHICH
            // LEAVES THE CHILD STREAM WITH THE SERIALIZED BYTES
            /////////////////////////////////////////////////////

            //loadFieldValue(_il);
            loadFieldValue();
            _il.Emit(OpCodes.Ldloc, ChildObjectStream);
            _il.Emit(OpCodes.Call, printMethod);

            ////////////////////////////////////////////
            // PRINT LENGTH OF CHILD STREAM
            ////////////////////////////////////////////

            _il.Emit(OpCodes.Ldloc, ChildObjectStream);
            _il.Emit(OpCodes.Callvirt, _streamAccessor.GetStreamLength);
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Call, _streamAccessor.WriteInt64);

            ////////////////////////////////////////////
            // COPY CHILD STREAM TO MAIN
            ////////////////////////////////////////////
            //reset stream
            _il.Emit(OpCodes.Ldloc, ChildObjectStream);
            _il.Emit(OpCodes.Ldc_I8, 0L);
            _il.Emit(OpCodes.Callvirt, _streamAccessor.SetStreamPosition);


            _il.Emit(OpCodes.Ldloc, ChildObjectStream);

            _il.Emit(OpCodes.Ldarg_2);

            _il.Emit(OpCodes.Call, _streamAccessor.CopyMemoryStream);


            _il.Emit(OpCodes.Ldloc, ChildObjectStream);
            _il.Emit(OpCodes.Ldc_I8, 0L);
            _il.Emit(OpCodes.Callvirt, _streamAccessor.SetStreamLength);
        }

        public void WriteInt32()
        {
            IL.Emit(OpCodes.Ldarg_2);
            IL.Emit(OpCodes.Call, _writeInt32);
        }

        public LocalBuilder ChildObjectStream => _childObjectStream ??=
            DeclareAndInstantiateChildStream();

        public LocalBuilder FieldByteArray { get; }

        public Boolean IsArrayMade { get; set; }

        public LocalBuilder LocalBytes { get; }

        public Byte[] CurrentFieldHeader => _currentField.HeaderBytes;

        //public void PrintChildObjectField(Action<ILGenerator> loadObject,
        //                             Type fieldType)
        public void PrintChildObjectField(Action loadObject,
                                          Type fieldType)
        {
            var headerBytes = CurrentFieldHeader;
            PrintHeaderBytes(headerBytes);

            var proxy = GetProxy(fieldType);
            var proxyField = proxy.ProxyField;

            ////////////////////////////////////////////
            // PROXY->PRINT(CURRENT)
            ////////////////////////////////////////////

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, proxyField);
            //loadObject(_il);
            loadObject();

            _il.Emit(OpCodes.Ldloc, ChildObjectStream);

            _il.Emit(OpCodes.Call, proxy.PrintMethod);


            ////////////////////////////////////////////
            // PRINT LENGTH OF CHILD STREAM
            ////////////////////////////////////////////
            _il.Emit(OpCodes.Ldloc, ChildObjectStream);
            _il.Emit(OpCodes.Callvirt, _getStreamLength);
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Call, _writeInt64);

            ////////////////////////////////////////////
            // COPY CHILD STREAM TO MAIN
            ////////////////////////////////////////////
            //reset stream
            _il.Emit(OpCodes.Ldloc, ChildObjectStream);
            _il.Emit(OpCodes.Ldc_I8, 0L);
            _il.Emit(OpCodes.Callvirt, _setStreamPosition);

            _il.Emit(OpCodes.Ldloc, ChildObjectStream);

            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Call, _copyMemoryStream);


            _il.Emit(OpCodes.Ldloc, ChildObjectStream);
            _il.Emit(OpCodes.Ldc_I8, 0L);
            _il.Emit(OpCodes.Callvirt, _setStreamLength);
        }

        //public override void LoadCurrentFieldValueToStack()
        //{
        //    base.LoadCurrentFieldValueToStack();

        //    switch (CurrentField.TypeCode)
        //    {
        //        case TypeCode.Single:
        //            _il.Emit(OpCodes.Call, _getSingleBytes);
        //            break;

        //        case TypeCode.Double:
        //            _il.Emit(OpCodes.Call, _getDoubleBytes);
        //            break;
        //    }
        //}


        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void EnsureChildObjectStream()
        {
            var s = ChildObjectStream;
            if (s == null)
                throw new NullReferenceException(nameof(ChildObjectStream));
        }

        public void PrintFieldHeader(IProtoFieldAccessor protoField)
        {
            _il.Emit(OpCodes.Ldc_I4, protoField.Header);
            WriteInt32();
        }

        

       
        private void PrintHeaderBytes(Byte[] headerBytes)
        {
            var s = this;

            var il = s.IL;
            var fieldByteArray = s.FieldByteArray;

            if (headerBytes.Length > 1)
            {
                il.Emit(OpCodes.Ldarg_0);

                if (!s.IsArrayMade)
                {
                    il.Emit(OpCodes.Ldc_I4_3);
                    il.Emit(OpCodes.Newarr, typeof(Byte));
                    il.Emit(OpCodes.Stloc, fieldByteArray);
                    s.IsArrayMade = true;
                }

                il.Emit(OpCodes.Ldloc, fieldByteArray);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldc_I4_S, headerBytes[0]);
                il.Emit(OpCodes.Stelem_I1);

                for (var c = 1; c < headerBytes.Length; c++)
                {
                    il.Emit(OpCodes.Ldloc, fieldByteArray);
                    il.Emit(OpCodes.Ldc_I4, c);
                    il.Emit(OpCodes.Ldc_I4_S, headerBytes[c]);
                    il.Emit(OpCodes.Stelem_I1);
                }

                il.Emit(OpCodes.Ldloc, fieldByteArray);
                il.Emit(OpCodes.Ldc_I4, headerBytes.Length);
                il.Emit(OpCodes.Call, _writeSomeBytes);
            }
            else
                PrintConstByte(headerBytes[0], il);
        }

        private static void PrintConstByte(Byte constVal,
                                           ILGenerator il)
        {
            var noStackDepth = il.DefineLabel();
            var endOfPrintConst = il.DefineLabel();


            //notPushed:
            il.MarkLabel(noStackDepth);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldc_I4_S, constVal);
            il.Emit(OpCodes.Callvirt, _writeStreamByte);

            il.MarkLabel(endOfPrintConst);
        }


        private LocalBuilder DeclareAndInstantiateChildStream()
        {
            var local = _il.DeclareLocal(typeof(NaiveMemoryStream));

            _typeCore.TryGetEmptyConstructor(local.LocalType!, out var ctor);
            _il.Emit(OpCodes.Newobj, ctor);
            _il.Emit(OpCodes.Stloc, local);

            return local;
        }

        public IProtoFieldAccessor[] Fields { get; }

        private static readonly MethodInfo _writeBytes;

        private static readonly MethodInfo _writeInt16;

        /// <summary>
        ///     public static void ProtoBufWriter->WriteInt32(Int32 value, Stream _outStream)
        /// </summary>
        private static readonly MethodInfo _writeInt32;

        private static readonly MethodInfo _writeInt8;
        private static readonly MethodInfo _writeChar;

        private static readonly MethodInfo _writePacked16;
        private static readonly MethodInfo _writePacked32;
        private static readonly MethodInfo _writePacked64;
        private static readonly MethodInfo _writeSomeBytes;
        private static readonly MethodInfo _writeStreamByte;
        private static readonly MethodInfo _writeUInt16;
        private static readonly MethodInfo _writeUInt32;

        private static readonly MethodInfo _writeInt64;
        private static readonly MethodInfo _writeUInt64;
        private static readonly MethodInfo _getStringBytes;

        private static readonly MethodInfo _getStreamLength;
        private static readonly MethodInfo _setStreamPosition;
        private static readonly MethodInfo _copyMemoryStream;
        private static readonly MethodInfo _setStreamLength;

        private readonly IStreamAccessor _streamAccessor;
        
        private readonly ITypeCore _typeCore;

        protected static readonly MethodInfo _getSingleBytes;

        /// <summary>
        ///     BitConverter.GetBytes(Double)
        /// </summary>
        protected static readonly MethodInfo _getDoubleBytes;

        protected static readonly MethodInfo _getDecimalBytes;

        protected static readonly MethodInfo _getPackedInt16Length;

        protected static readonly MethodInfo _getPackedInt32Length;
        protected static readonly MethodInfo _getPackedInt64Length;

        protected static readonly MethodInfo _dateToFileTime;

        protected static readonly FieldInfo Utf8;


        //private readonly MethodInfo _writeInt32;

        private LocalBuilder? _childObjectStream;

        public void PrintCurrentFieldHeader()
        {
            PrintHeaderBytes(_currentField.HeaderBytes);
        }

       

        public void LoadWriter()
        {
            _il.Emit(OpCodes.Ldarg_2);
        }

       

        public void AppendBoolean()
        {
            _il.Emit(OpCodes.Call, _writeInt32);
        }

        public void AppendChar()
        {
            _il.Emit(OpCodes.Call, _writeChar);
        }

        public void AppendInt8()
        {
            _il.Emit(OpCodes.Call, _writeInt8);
        }

        public void AppendInt16()
        {
            _il.Emit(OpCodes.Call, _writeInt16);
        }

        public void AppendUInt16 ()
        {
            _il.Emit(OpCodes.Call, _writeUInt16);
        }

        public void AppendInt32()
        {
            _il.Emit(OpCodes.Call, _writeInt32);
        }

        public void AppendUInt32()
        {
            _il.Emit(OpCodes.Call, _writeUInt32);
        }

        public void AppendInt64()
        {
            _il.Emit(OpCodes.Call, _writeInt64);
        }

        public void AppendUInt64()
        {
            _il.Emit(OpCodes.Call, _writeUInt64);
        }

        public void AppendSingle()
        {
            //_il.Emit(OpCodes.Call, _getSingleBytes);
            //_il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Call, _writeBytes);
        }

        public void AppendDouble()
        {
            //_il.Emit(OpCodes.Call, _getDoubleBytes);
            //_il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Call, _writeBytes);
        }

        public void AppendDecimal()
        {
            //_il.Emit(OpCodes.Call, _getDoubleBytes);
            //_il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Call, _writeBytes);
        }
    }
}

#endif
