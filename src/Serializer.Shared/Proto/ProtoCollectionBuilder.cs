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
