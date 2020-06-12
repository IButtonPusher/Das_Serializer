﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer.Proto
{
    public class ProtoPrintState : ProtoStateBase, IEnumerable<ProtoPrintState>
    {
        public ProtoPrintState(
            ProtoPrintState s,
            IEnumerable<IProtoFieldAccessor> subFields,
            Type parentType,
            Action<ILGenerator> loadObject,
            ITypeCore typeCore,
            MethodInfo writeInt32,
            IStreamAccessor streamAccessor)
            : this(s.IL, s.IsArrayMade, //s.LocalString, s.FieldByteArray,
                subFields, parentType, //s.UtfField, 
                loadObject, s.HasPushed, typeCore, writeInt32, streamAccessor, s.ChildProxies)
        {
            LocalString = s.LocalString;
            FieldByteArray = s.FieldByteArray;
        }

        public ProtoPrintState(
            ILGenerator il,
            Boolean isArrayMade,
            //LocalBuilder? localString,
            //LocalBuilder fieldByteArray,
            IEnumerable<IProtoFieldAccessor> fields,
            Type parentType,
            //FieldInfo utfField, 
            Action<ILGenerator> loadObject,
            Boolean hasPushed,
            ITypeCore typeCore,
            MethodInfo writeInt32,
            IStreamAccessor streamAccessor,
            IDictionary<IProtoFieldAccessor, FieldBuilder> childProxies)
            : base(il, typeCore, childProxies, parentType, loadObject)
        {
            _loadObject = loadObject;
            _typeCore = typeCore;

            IsArrayMade = isArrayMade;
            // LocalString = localString;
            FieldByteArray = il.DeclareLocal(typeof(Byte[]));
            LocalBytes = il.DeclareLocal(typeof(Byte[]));
            //ParentType = parentType;
            //UtfField = utfField;
            HasPushed = hasPushed;
            _writeInt32 = writeInt32;
            _streamAccessor = streamAccessor;
            Fields = fields.ToArray();

            if (ChildProxies.Count > 0 && childProxies == null)
            {
                typeCore.TryGetEmptyConstructor(typeof(MemoryStream), out var ctor);
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Stloc, ChildObjectStream);
            }
        }

        public IEnumerator<ProtoPrintState> GetEnumerator()
        {
            for (var c = 0; c < Fields.Length; c++)
            {
                CurrentField = Fields[c];
                yield return this;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public LocalBuilder ChildObjectStream => _childObjectStream ??=
            DeclareAndInstantiateChildStream();

        public LocalBuilder FieldByteArray { get; }

        public IProtoFieldAccessor[] Fields { get; }

        //public Type ParentType { get; }

        //public FieldInfo UtfField { get; }

        public Boolean HasPushed { get; set; }

        public Boolean IsArrayMade { get; set; }

        public LocalBuilder? LocalBytes { get; set; }

        public LocalBuilder? LocalString { get; set; }


        private LocalBuilder DeclareAndInstantiateChildStream()
        {
            var local = _il.DeclareLocal(typeof(MemoryStream));

            _typeCore.TryGetEmptyConstructor(typeof(MemoryStream), out var ctor);
            _il.Emit(OpCodes.Newobj, ctor);
            _il.Emit(OpCodes.Stloc, local);

            return local;
        }


        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void EnsureChildObjectStream()
        {
            var s = ChildObjectStream;
            if (s == null)
                throw new NullReferenceException(nameof(ChildObjectStream));
        }

        private void LoadOutputStream()
        {
            _il.Emit(OpCodes.Ldarg_2);
        }

        public void MergeLocals(ProtoPrintState s)
        {
            IsArrayMade |= s.IsArrayMade;
            HasPushed |= s.HasPushed;
            LocalBytes ??= s.LocalBytes;
            LocalString ??= s.LocalString;
        }

        public void PrintFieldHeader(IProtoFieldAccessor protoField)
        {
            _il.Emit(OpCodes.Ldc_I4, protoField.Header);
            WriteInt32();
        }


        public void PrintFieldViaProxy(IProtoFieldAccessor protoField,
            Action<ILGenerator> loadFieldValue)
        {

            PrintFieldHeader(protoField);

            var fieldInfo = LoadFieldProxy(protoField);
            var proxyType = fieldInfo.FieldType;

            var printMethod = proxyType.GetMethodOrDie(nameof(IProtoProxy<Object>.Print));

            /////////////////////////////////////////////////////
            // CALL THE PRINT METHOD ON THE PROXY WHICH
            // LEAVES THE CHILD STREAM WITH THE SERIALIZED BYTES
            /////////////////////////////////////////////////////
            //LoadCurrentFieldValueToStack();
            loadFieldValue(_il);
            _il.Emit(OpCodes.Ldloc, ChildObjectStream);
            //LoadOutputStream();
            _il.Emit(OpCodes.Call, printMethod);


            ////////////////////////////////////////////
            // PRINT LENGTH OF CHILD STREAM
            ////////////////////////////////////////////
            //il.Emit(OpCodes.Ldarg_0);
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

            //il.Emit(OpCodes.Ldc_I4, 4096);
            _il.Emit(OpCodes.Call, _streamAccessor.CopyMemoryStream);
            //il.Emit(OpCodes.Callvirt, _copyStreamTo);


            _il.Emit(OpCodes.Ldloc, ChildObjectStream);
            _il.Emit(OpCodes.Ldc_I8, 0L);
            _il.Emit(OpCodes.Callvirt, _streamAccessor.SetStreamLength);
        }

        public void WriteInt32()
        {
            IL.Emit(OpCodes.Ldarg_2);
            IL.Emit(OpCodes.Call, _writeInt32);
        }

        private readonly Action<ILGenerator> _loadObject;

        private readonly IStreamAccessor _streamAccessor;
        private readonly ITypeCore _typeCore;

        /// <summary>
        ///     public static void ProtoBufWriter->WriteInt32(Int32 value, Stream _outStream)
        /// </summary>
        private readonly MethodInfo _writeInt32;

        private LocalBuilder _childObjectStream;
    }
}