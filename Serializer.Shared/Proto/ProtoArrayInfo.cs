using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Das.Extensions;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoArrayInfo
    {
        private readonly MethodInfo _getStreamPosition;
        private readonly MethodInfo _getPositiveInt32;
        private readonly ITypeManipulator _types;
        
        private readonly ConstructorInfo _listInt32Ctor;
        private readonly ConstructorInfo _listInt64Ctor;

        private readonly MethodInfo _addToInt32List;
        private readonly MethodInfo _addToInt64List;

        public Dictionary<IProtoField, LocalBuilder> InstancesCounter { get; }

        public Dictionary<IProtoField, LocalBuilder> StartIndexesCounter { get; }

        public Dictionary<IProtoField, LocalBuilder> LengthsCounter { get; }

        public ProtoArrayInfo(MethodInfo getStreamPosition, ITypeManipulator types, 
            MethodInfo getPositiveInt32)
        {
            _getStreamPosition = getStreamPosition;
            _types = types;
            _getPositiveInt32 = getPositiveInt32;
            InstancesCounter = new Dictionary<IProtoField, LocalBuilder>();
            StartIndexesCounter= new Dictionary<IProtoField, LocalBuilder>();
            LengthsCounter = new Dictionary<IProtoField, LocalBuilder>();

            _listInt32Ctor = typeof(List<Int32>).GetConstructor(Type.EmptyTypes);
            _listInt64Ctor = typeof(List<Int64>).GetConstructor(Type.EmptyTypes);

            _addToInt32List = typeof(List<Int32>).GetMethodOrDie(nameof(List<Int32>.Add));
            _addToInt64List = typeof(List<Int64>).GetMethodOrDie(nameof(List<Int64>.Add));
        }

        public void Add(IProtoField field, ILGenerator il)
        {
            var arrayCounter = il.DeclareLocal(typeof(Int32));
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, arrayCounter);
            InstancesCounter[field] = arrayCounter;

            var startCounter = il.DeclareLocal(typeof(List<Int64>));
            il.Emit(OpCodes.Newobj, _listInt64Ctor);
            il.Emit(OpCodes.Stloc, startCounter);
            StartIndexesCounter[field] = startCounter;

            var lengthsCounter = il.DeclareLocal(typeof(List<Int32>));
            il.Emit(OpCodes.Newobj, _listInt32Ctor);
            il.Emit(OpCodes.Stloc, lengthsCounter);
            LengthsCounter[field] = lengthsCounter;

        }

        public void Increment(IProtoField field, ILGenerator il)
        {
            //COUNT++
            il.Emit(OpCodes.Ldloc, InstancesCounter[field]);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, InstancesCounter[field]);

            
            il.Emit(OpCodes.Ldloc, StartIndexesCounter[field]);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, _getStreamPosition);
            il.Emit(OpCodes.Callvirt, _addToInt64List);
            

            var germane = _types.GetGermaneType(field.Type);
            if (ProtoBufSerializer.GetWireType(germane) == ProtoWireTypes.LengthDelimited)
            {
                il.Emit(OpCodes.Ldloc, LengthsCounter[field]);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, _getPositiveInt32);
                il.Emit(OpCodes.Callvirt, _addToInt32List);
            }
        }
    }
}
