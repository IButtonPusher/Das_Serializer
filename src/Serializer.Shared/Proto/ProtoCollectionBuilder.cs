#if GENERATECODE

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Extensions;


namespace Das.Serializer.ProtoBuf
{
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        private void PrintArray(ProtoPrintState s,
                                Action<LocalBuilder, ProtoPrintState, ILGenerator, Byte[]> action)
        {
            var il = s.IL;

            var pv = s.CurrentField;

            var getLength = pv.Type.GetterOrDie(nameof(Array.Length), out _);
            var arrLength = il.DeclareLocal(Const.IntType);
            s.LoadCurrentFieldValueToStack();
            il.Emit(OpCodes.Callvirt, getLength);
            il.Emit(OpCodes.Stloc, arrLength);

            // for (var c = 0;
            var fore = il.DefineLabel();
            var breakLoop = il.DefineLabel();

            var c = il.DeclareLocal(Const.IntType);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, c);
            il.MarkLabel(fore);

            // c < arr.Length
            il.Emit(OpCodes.Ldloc, c);
            il.Emit(OpCodes.Ldloc, arrLength);
            il.Emit(OpCodes.Bge, breakLoop);

            // var current = array[c];
            var germane = _types.GetGermaneType(pv.Type);
            var current = il.DeclareLocal(germane);

            s.LoadCurrentFieldValueToStack();
            il.Emit(OpCodes.Ldloc, c);
            il.Emit(OpCodes.Ldelem_Ref);
            il.Emit(OpCodes.Stloc, current);

            ///////////////////////////////////////////////////////////////
            action(current, s, il, pv.HeaderBytes);
            ///////////////////////////////////////////////////////////////

            // c++
            il.Emit(OpCodes.Ldloc, c);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, c);
            il.Emit(OpCodes.Br, fore);


            il.MarkLabel(breakLoop);
        }

        private void PrintByteArray(ProtoPrintState s)
        {
            PrintHeaderBytes(s.CurrentField.HeaderBytes, s);

            var il = s.IL;

            s.LoadParentToStack();

            il.Emit(OpCodes.Call, s.CurrentField.GetMethod);
            il.Emit(OpCodes.Stloc, s.LocalBytes);

            il.Emit(OpCodes.Ldloc, s.LocalBytes);
            il.Emit(OpCodes.Call, _getArrayLength);
            s.WriteInt32();

            il.Emit(OpCodes.Ldloc, s.LocalBytes);

            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, _writeBytes);
        }

        private static void PrintCollection(ProtoPrintState s,
                                            Action<LocalBuilder, ProtoPrintState, ILGenerator, Byte[]> action)
        {
            var pv = s.CurrentField;
            var ienum = new ProtoEnumerator<ProtoPrintState>(s, pv.Type, pv.GetMethod);

            ienum.ForEach(action, pv.HeaderBytes);
        }


        private void PrintEnumeratorCurrent(LocalBuilder enumeratorCurrentValue,
                                            ProtoPrintState s,
                                            ILGenerator il,
                                            Byte[] headerBytes)
        {
            var germane = _types.GetGermaneType(s.CurrentField.Type);
            var subAction = GetProtoFieldAction(germane);

            switch (subAction)
            {
                case ProtoFieldAction.ChildObject:
                    PrintChildObject(s, headerBytes,
                        ilg => ilg.Emit(OpCodes.Ldloc, enumeratorCurrentValue),
                        germane);
                    break;

                case ProtoFieldAction.String:
                    PrintString(s,
                        xs => xs.IL.Emit(OpCodes.Ldloc, enumeratorCurrentValue));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private static void PrintKeyValuePair(
            LocalBuilder enumeratorCurrentValue,
            ProtoPrintState s,
            ILGenerator il,
            Byte[] headerBytes)
        {
            s.PrintFieldViaProxy(s.CurrentField,
                ilg => ilg.Emit(OpCodes.Ldloc, enumeratorCurrentValue));
        }


        private void ScanByteArray(ILGenerator il,
                                   LocalBuilder lastByteLocal)
        {
            il.Emit(OpCodes.Ldarg_1);
            var holdForSet = il.DeclareLocal(typeof(Byte[]));

            //Get length of the array
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, GetPositiveInt32);

            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Stloc, lastByteLocal);
            il.Emit(OpCodes.Newarr, typeof(Byte));
            il.Emit(OpCodes.Stloc, holdForSet);

            //read bytes into buffer field
            il.Emit(OpCodes.Ldloc, holdForSet);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldloc, lastByteLocal);
            il.Emit(OpCodes.Callvirt, ReadStreamBytes);

            il.Emit(OpCodes.Pop);

            il.Emit(OpCodes.Ldloc, holdForSet);
        }

        /// <summary>
        ///     ICollection[TProperty] where TProperty : ProtoContract
        ///     for a collection of proto contracts by way of a property of a parent contract
        /// </summary>
        private void ScanCollection(Type type,
                                    IValueExtractor s)
        {
            var il = s.IL;

            var germane = _types.GetGermaneType(type);
            var action = GetProtoFieldAction(germane);
            var wireType = ProtoBufSerializer.GetWireType(germane);
            var tc = Type.GetTypeCode(germane);

            ScanValueToStack(s, il, germane, tc, wireType, action, false);
        }
    }
}

#endif