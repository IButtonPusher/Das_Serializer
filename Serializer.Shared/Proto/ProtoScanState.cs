using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Das.Extensions;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer.Proto
{
    public class ProtoScanState : ProtoStateBase
    {
        public ProtoScanState(ILGenerator il, IProtoFieldAccessor[] fields, 
            IProtoFieldAccessor currentField, 
            Type parentType, Action<ILGenerator> loadReturnValueOntoStack, 
            LocalBuilder byteBufferField, 
            LocalBuilder lastByteLocal, Object exampleObject, ProtoArrayInfo arrayCounters, 
            //LocalBuilder streamLengthField, 
            FieldInfo utfField, 
            ITypeCore typeCore) : base(il, fields, typeCore, null)
        {
            Fields = fields;
            CurrentField = currentField;
            ParentType = parentType;
            LoadCurrentValueOntoStack = loadReturnValueOntoStack;
            //ByteBufferField = byteBufferField;
            LastByteLocal = lastByteLocal;
            ExampleObject = exampleObject;
            ArrayCounters = arrayCounters;
            //StreamLengthField = streamLengthField;
            UtfField = utfField;

           
        }

        public IProtoFieldAccessor[] Fields { get; }

        public IProtoFieldAccessor CurrentField { get; set; }

        public ProtoArrayInfo ArrayCounters { get; }

        public Type ParentType { get; }

        public Action<ILGenerator> LoadCurrentValueOntoStack { get; }

        public Action<ILGenerator> SetCurrentValue { get; set; }

        //public LocalBuilder ByteBufferField { get; }

        public LocalBuilder LastByteLocal { get; }

        public LocalBuilder? LocalString { get; set; }

        public LocalBuilder? LocalBytes { get; set; }

        public FieldInfo UtfField { get; }


        //public LocalBuilder StreamLengthField { get; }

        public Object ExampleObject { get; }
    }
}
