using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using Das.Serializer.Proto;

namespace Das.Serializer.ProtoBuf
{
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        private static void PrintCollection(ProtoPrintState s,
             Action<LocalBuilder, ProtoPrintState, ILGenerator, Byte[]> action)
         {
             var pv = s.CurrentField;
             var ienum = new ProtoEnumerator<ProtoPrintState>(s, pv.Type, pv.GetMethod);


             ienum.ForEach(action, pv.HeaderBytes);
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


         private void PrintEnumeratorCurrent(
             LocalBuilder enumeratorCurrentValue,
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
                         ilg => ilg.Emit(OpCodes.Ldloc, enumeratorCurrentValue));
                     break;

                 case ProtoFieldAction.String:
                     PrintString(s,
                         xs => xs.IL.Emit(OpCodes.Ldloc, enumeratorCurrentValue));
                     break;

                 default:
                     throw new NotImplementedException();
             }
         }

         /// <summary>
         /// ICollection[TProperty] where TProperty : ProtoContract
         /// for a collection of proto contracts by way of a property of a parent contract
         /// </summary>
         private void ScanCollection(
             Type type,
             IValueExtractor s)
         {
             var il = s.IL;

             var germane = _types.GetGermaneType(type);
             var action = GetProtoFieldAction(germane);
             var wireType = ProtoBufSerializer.GetWireType(germane);
             var tc = Type.GetTypeCode(germane);

             ScanValueToStack(s, il, germane, tc, wireType, action);
         }


         private void ScanByteArray(
            ILGenerator il, 
            LocalBuilder lastByteLocal)
        {
            il.Emit(OpCodes.Ldarg_1);
            var holdForSet = il.DeclareLocal(typeof(Byte[]));

            //Get length of the array
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt32);

            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Stloc, lastByteLocal);
            il.Emit(OpCodes.Newarr, typeof(Byte));
            il.Emit(OpCodes.Stloc, holdForSet);

            //read bytes into buffer field
            il.Emit(OpCodes.Ldloc, holdForSet);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldloc, lastByteLocal);
            il.Emit(OpCodes.Callvirt, _readStreamBytes);

            il.Emit(OpCodes.Pop);

            il.Emit(OpCodes.Ldloc, holdForSet);
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
       
    }
}
