using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Das.Extensions;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer.Proto
{
    public class ProtoScanState : ProtoStateBase
    {
        public ProtoScanState(ILGenerator il, IProtoFieldAccessor[] fields, 
            IProtoFieldAccessor? currentField, 
            Type parentType, 
            Action<ILGenerator>? loadReturnValueOntoStack,
            LocalBuilder lastByteLocal, Object? exampleObject, 
            ProtoArrayInfo arrayCounters,
            IDictionary<IProtoFieldAccessor, FieldBuilder> childProxies,
            IStreamAccessor streamAccessor,
            ITypeCore typeCore,
            
            FieldInfo readBytesField
            ) 
            
            : base(il, typeCore, childProxies, parentType, loadReturnValueOntoStack)
        {
            LocalFieldValues = new Dictionary<IProtoFieldAccessor, LocalBuilder>();

            Fields = fields;
            CurrentField = currentField;
            //ParentType = parentType;
            //LoadCurrentValueOntoStack = loadReturnValueOntoStack;
            LastByteLocal = lastByteLocal;
            ExampleObject = exampleObject;
            ArrayCounters = arrayCounters;
            
            UtfField = streamAccessor.Utf8;
            _streamAccessor = streamAccessor;
            //_utf8 = utf8;
            //_getPositiveInt32 = getPositiveInt32;
            _readBytesField = readBytesField;
            //_protoProvider = protoProvider;

            _proxyProviderField = typeof(ProtoDynamicBase).GetInstanceFieldOrDie("_proxyProvider");

            //_readStreamBytes = readStreamBytes;
            //_bytesToString = bytesToString;
        }

       
        public void LoadPositiveInt32()
        {
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Call, _streamAccessor.GetPositiveInt32);
        }

        /// <summary>
        /// Leaves the # of bytes read on the stack!
        /// </summary>
        public void LoadNextBytesIntoTempArray()
        {
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldsfld, _readBytesField);
            _il.Emit(OpCodes.Ldc_I4_0);

            LoadPositiveInt32();

            _il.Emit(OpCodes.Callvirt, _streamAccessor.ReadStreamBytes);
        }

        public void LoadNextString()
        {
            _il.Emit(OpCodes.Ldsfld, _streamAccessor.Utf8);
            
            _il.Emit(OpCodes.Ldsfld, _readBytesField);
            _il.Emit(OpCodes.Ldc_I4_0);
            LoadNextBytesIntoTempArray();

            _il.Emit(OpCodes.Call, _streamAccessor.GetStringFromBytes);



        }

        private readonly IStreamAccessor _streamAccessor;
        
        //private readonly FieldInfo _utf8;
        //private readonly MethodInfo _getPositiveInt32;
        private readonly FieldInfo _readBytesField;
        private readonly FieldInfo _proxyProviderField;

        
        //private readonly MethodInfo _readStreamBytes;
        //private readonly MethodInfo _bytesToString;

        public IProtoFieldAccessor[] Fields { get; }

        

        public ProtoArrayInfo ArrayCounters { get; }


        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void EnsureLocalFields()
        {
            foreach (var field in Fields)
            {
                var local = GetLocalForField(field);
                if (local == null)
                    throw new InvalidOperationException(nameof(EnsureLocalFields));
            }
        }

        public LocalBuilder GetLocalForField(IProtoFieldAccessor field)
        {
            if (!LocalFieldValues.TryGetValue(field, out var local))
            {
                local = _il.DeclareLocal(field.Type);

                var buildDefault = typeof(IProtoProvider).
                    GetMethodOrDie(nameof(IProtoProvider.BuildDefaultValue));

                var buildDefaultClosed = buildDefault.MakeGenericMethod(field.Type);

                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, _proxyProviderField);
                _il.Emit(OpCodes.Callvirt, buildDefaultClosed);
                _il.Emit(OpCodes.Stloc, local);

                LocalFieldValues.Add(field, local);
            }

            return local;
        }

        public LocalBuilder GetLocalForParameter(ParameterInfo prm)
        {
            foreach (var kvp in LocalFieldValues.Where(k => k.Key.Equals(prm)))
            {
                return kvp.Value;
            }
            
            throw new KeyNotFoundException(prm.Name);
        }

        
        /// <summary>
        /// For values that will be ctor injected
        /// </summary>
        public Dictionary<IProtoFieldAccessor, LocalBuilder> LocalFieldValues { get; }
        

        private Action<ILGenerator> SetCurrentValue { get; set; }

        //public LocalBuilder ByteBufferField { get; }

        public LocalBuilder LastByteLocal { get; }

        public LocalBuilder? LocalString { get; set; }

        public LocalBuilder? LocalBytes { get; set; }

        public FieldInfo UtfField { get; }


        

        public Object? ExampleObject { get; }
    }
}
