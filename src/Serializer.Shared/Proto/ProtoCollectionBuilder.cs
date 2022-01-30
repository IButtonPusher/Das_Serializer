#if GENERATECODE

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Serializer.Proto;

namespace Das.Serializer.ProtoBuf
{
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        //private void PrintArray(IProtoPrintState s,
        //                        Action<LocalBuilder, IProtoPrintState, ILGenerator, Byte[]> action)
        //{
        //    var pv = s.CurrentField;
        //    var ienum = new ProtoEnumerator<IProtoPrintState>(s, pv.Type,
        //        pv.GetMethod, _types, this);

        //    ienum.ForLoop(s, PrintEnumeratorCurrent);
        //}

        //private void PrintByteArray(IProtoPrintState s)
        //{
        //    PrintHeaderBytes(s.CurrentFieldHeader, s);

        //    var il = s.IL;

        //    s.LoadParentToStack();

        //    il.Emit(OpCodes.Call, s.CurrentField.GetMethod);
        //    il.Emit(OpCodes.Stloc, s.LocalBytes);

        //    il.Emit(OpCodes.Ldloc, s.LocalBytes);
        //    il.Emit(OpCodes.Call, _getArrayLength);
        //    s.WriteInt32();

        //    il.Emit(OpCodes.Ldloc, s.LocalBytes);

        //    il.Emit(OpCodes.Ldarg_2);
        //    il.Emit(OpCodes.Call, _writeBytes);
        //}

        //private void PrintCollection(IProtoPrintState s,
        //                             Action<LocalBuilder, IProtoPrintState, ILGenerator, Byte[]> action)
        //{
        //    var pv = s.CurrentField;
        //    var ienum = new ProtoEnumerator<IProtoPrintState>(s, pv.Type, pv.GetMethod, _types);

        //    ienum.ForEach(action, s.CurrentFieldHeader);
        //}

        //private void PrintEnumeratorCurrent(LocalBuilder enumeratorCurrentValue,
        //                                    LocalBuilder currentIndex,
        //                                    IProtoPrintState s)
        //{
        //    var germane = _types.GetGermaneType(s.CurrentField.Type);
        //    var subAction = GetProtoFieldAction(germane);

        //    switch (subAction)
        //    {
        //        case FieldAction.ChildObject:
        //            PrintChildObject(s, s.CurrentFieldHeader,
        //                ilg => ilg.Emit(OpCodes.Ldloc, enumeratorCurrentValue),
        //                germane);
        //            break;

        //        case FieldAction.String:
        //            PrintString(s,
        //                xs => xs.IL.Emit(OpCodes.Ldloc, enumeratorCurrentValue));
        //            break;

        //        default:
        //            throw new NotImplementedException();
        //    }
        //}


        //private void PrintEnumeratorCurrent(LocalBuilder enumeratorCurrentValue,
        //                                    LocalBuilder currentIndex,
        //                                    IProtoPrintState s,
        //                                    ILGenerator il,
        //                                    Byte[] headerBytes)
        //{
        //    var germane = _types.GetGermaneType(s.CurrentField.Type);
        //    var subAction = GetProtoFieldAction(germane);

        //    switch (subAction)
        //    {
        //        case FieldAction.ChildObject:
        //            PrintChildObject(s, headerBytes,
        //                ilg => ilg.Emit(OpCodes.Ldloc, enumeratorCurrentValue),
        //                germane);
        //            break;

        //        case FieldAction.String:
        //            PrintString(s,
        //                xs => xs.IL.Emit(OpCodes.Ldloc, enumeratorCurrentValue));
        //            break;

        //        default:
        //            throw new NotImplementedException();
        //    }
        //}

        //private static void PrintKeyValuePair(LocalBuilder enumeratorCurrentValue,
        //                                      IProtoPrintState s,
        //                                      ILGenerator il,
        //                                      Byte[] headerBytes)
        //{
        //    s.PrintFieldViaProxy(//s.CurrentField,
        //        ilg => ilg.Emit(OpCodes.Ldloc, enumeratorCurrentValue));
        //}


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
                                    IProtoScanState s)
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
