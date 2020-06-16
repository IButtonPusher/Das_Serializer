using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using Das.Extensions;
using Das.Serializer.Proto;

namespace Das.Serializer.ProtoBuf
{
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        //private Boolean TryScanAsDictionary(ProtoScanState s, Action<ILGenerator>? setCurrentValue)
        //{
        //    var il = s.IL;
        //    var currentProp = s.CurrentField;

        //    if (!typeof(IDictionary).IsAssignableFrom(currentProp.Type))
        //        return false;

        //    return false;

        //    var info = new ProtoDictionaryInfo(currentProp.Type, _types, this);

        //    if (!currentProp.Type.TryGetMethod(nameof(IList.Add),
        //            out var adder, info.KeyType, info.ValueType) &&
        //        !currentProp.Type.TryGetMethod(
        //            nameof(ConcurrentDictionary<Object, Object>.TryAdd),
        //            out adder, info.KeyType, info.ValueType))
        //        throw new InvalidOperationException();

        //    //discard total kvp length
        //    il.Emit(OpCodes.Ldarg_1);
        //    il.Emit(OpCodes.Call, _getPositiveInt32);
        //    il.Emit(OpCodes.Pop);

        //    //discard key header?
        //    il.Emit(OpCodes.Ldarg_1);
        //    il.Emit(OpCodes.Call, _getPositiveInt32);
        //    il.Emit(OpCodes.Pop);

        //    LocalBuilder keyLocal = null!;
        //    LocalBuilder valueLocal = null!;

        //    var holdCurrentProp = s.CurrentField;

        //    s.CurrentField = info.Key;

        //    PopulateScanField(s, setCurrentValue, ref keyLocal);


        //    //discard value header?
        //    il.Emit(OpCodes.Ldarg_1);
        //    il.Emit(OpCodes.Call, _getPositiveInt32);
        //    il.Emit(OpCodes.Pop);

        //    s.CurrentField = info.Value;

        //    PopulateScanField(s, setCurrentValue, ref valueLocal);

        //    s.CurrentField = holdCurrentProp;
               
        //    //
        //    var getter = s.ParentType.GetterOrDie(currentProp.Name, out _);
        //    //
        //    s.LoadCurrentValueOntoStack(il);
        //    il.Emit(OpCodes.Callvirt, getter);

        //    il.Emit(OpCodes.Ldloc, keyLocal);
        //    il.Emit(OpCodes.Ldloc, valueLocal);
        //    il.Emit(OpCodes.Call, adder);
        //    if (adder.ReturnType != typeof(void))
        //        il.Emit(OpCodes.Pop);

        //    return true;

        //}

        // private Boolean TryPrintAsDictionary(ProtoPrintState s,
        //     LocalBuilder enumeratorCurrentValue)
        //{

        //    var pv = s.CurrentField;
        //    var il = s.IL;

        //    if (!typeof(IDictionary).IsAssignableFrom(pv.Type)) 
        //        return false;

        //    il.Emit(OpCodes.Ldarg_0);
        //    il.Emit(OpCodes.Callvirt, _push);
        //    il.Emit(OpCodes.Pop);
        //    s.HasPushed = true;

        //    var info = new ProtoDictionaryInfo(pv.Type, _types, this);

        //    /////////////////////////////////////
        //    // PRINT KEY'S HEADER / KEY'S VALUE
        //    /////////////////////////////////////
        //    il.Emit(OpCodes.Ldarg_0);
        //    il.Emit(OpCodes.Ldc_I4, info.KeyHeader);
        //    il.Emit(OpCodes.Callvirt, _writeInt32);

        //    var holdProp = s.CurrentField;

        //    s.CurrentField = info.Key;

        //    AddGettableValueToPrintMethod(s, info.KeyGetter,
        //        ilg => ilg.Emit(OpCodes.Ldloca, enumeratorCurrentValue));
        //    /////////////////////////////////////
                

        //    /////////////////////////////////////
        //    // PRINT VALUE'S HEADER / VALUE'S VALUE
        //    /////////////////////////////////////
        //    il.Emit(OpCodes.Ldarg_0);
        //    il.Emit(OpCodes.Ldc_I4, info.ValueHeader);
        //    il.Emit(OpCodes.Callvirt, _writeInt32);

        //    s.CurrentField = info.Value;

        //    AddGettableValueToPrintMethod(s, info.ValueGetter,
        //        ilg => ilg.Emit(OpCodes.Ldloca, enumeratorCurrentValue));

        //    s.CurrentField = holdProp;

        //    return true;

        //}

         //private void PrintDictionary(ProtoPrintState s)
         //{
         //    var pv = s.CurrentField;

         //    var ienum = new ProtoEnumerator<ProtoPrintState>(s, pv.Type, pv.GetMethod);
         //    //var shallPush = true;

         //    ienum.ForEach(PrintKeyValuePair, pv.HeaderBytes);
         //}

         private void PrintCollection(ProtoPrintState s,
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

          private void LoadLocalListForArray(ProtoScanState s, 
              IProtoFieldAccessor pv)
          {
              var local = s.GetLocalForField(pv);

              s.IL.Emit(OpCodes.Ldloc, local);
          }

          private void AddSingleValue(ProtoScanState s, 
              IProtoFieldAccessor pv)
          {
              if (!s.TryGetAdderForField(pv, out var adder))
                  throw new NotSupportedException();


              s.IL.Emit(OpCodes.Callvirt, adder);
          }

          private void AddKeyValuePair(ProtoScanState s, IProtoFieldAccessor pv)
          {
              var il = s.IL;

              var canAdd = _types.TryGetAddMethod(pv.Type, out var adder);

              if (!canAdd)
                  throw new NotImplementedException();


              var germane = _types.GetGermaneType(pv.Type);

              var kvp = il.DeclareLocal(germane);

              il.Emit(OpCodes.Stloc, kvp);

              var getKey = germane.GetterOrDie(nameof(KeyValuePair<object, object>.Key), out _);
              var getValue = germane.GetterOrDie(nameof(KeyValuePair<object, object>.Value), out _);

              il.Emit(OpCodes.Ldloca, kvp);
              il.Emit(OpCodes.Call, getKey);

              il.Emit(OpCodes.Ldloca, kvp);
              il.Emit(OpCodes.Call, getValue);

              il.Emit(OpCodes.Callvirt, adder);
          }

          /// <summary>
          /// ICollection[TProperty] where TProperty : ProtoContract
          /// for a collection of proto contracts by way of a property of a parent contract
          /// </summary>
          private Boolean TryScanAsNestedCollection(
              ProtoScanState s,
              Action<ProtoScanState, IProtoFieldAccessor> loadValue,
              Action<ProtoScanState, IProtoFieldAccessor> addValue)
          {
              var il = s.IL;
              var pv = s.CurrentField;

            //if (!s.TryGetAdderForField(pv, out var adder))
            //    throw new NotSupportedException();

              //var canAdd = _types.TryGetAddMethod(pv.Type, out var adder);

              //if (!canAdd)
              //    throw new NotImplementedException();

              var germane = _types.GetGermaneType(pv.Type);
              var action = GetProtoFieldAction(germane);

              switch (action)
              {

                  case ProtoFieldAction.String:

                      loadValue(s, pv);
                      //s.LoadCurrentValueOntoStack(il);


                      //s.IL.Emit(OpCodes.Callvirt, s.CurrentField.GetMethod);

                      s.LoadNextString();
                    
                      //il.Emit(OpCodes.Callvirt, adder);
                      addValue(s, pv);

                      return true;

                  case ProtoFieldAction.ChildObject:

                      loadValue(s, pv);

                      //s.LoadCurrentFieldValueToStack();

                      //s.LoadCurrentValueOntoStack(il);

                      var fieldInfo = s.LoadFieldProxy(s.CurrentField);
                      var proxyType = fieldInfo.FieldType;

                      var scanMethod = proxyType.GetMethodOrDie(nameof(IProtoProxy<Object>.Scan),
                          typeof(Stream), typeof(Int64));


                      il.Emit(OpCodes.Ldarg_1); //arg1 = input stream!

                      il.Emit(OpCodes.Ldarg_1);
                      il.Emit(OpCodes.Call, _getPositiveInt64);

                      il.Emit(OpCodes.Call, scanMethod);

                      addValue(s, pv);

                      break;
              }

              return true;

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
              //var pv = s.CurrentField;

            //if (!s.TryGetAdderForField(pv, out var adder))
            //    throw new NotSupportedException();

              //var canAdd = _types.TryGetAddMethod(pv.Type, out var adder);

              //if (!canAdd)
              //    throw new NotImplementedException();

              var germane = _types.GetGermaneType(type);
              var action = GetProtoFieldAction(germane);
              var wireType = ProtoBufSerializer.GetWireType(germane);
              var tc = Type.GetTypeCode(germane);

            PopulateScanField(s, il, germane, tc, wireType, action);


              //switch (action)
              //{

              //    case ProtoFieldAction.String:

              //        loadValue(s, pv);
              //        //s.LoadCurrentValueOntoStack(il);


              //        //s.IL.Emit(OpCodes.Callvirt, s.CurrentField.GetMethod);

              //        s.LoadNextString();
                    
              //        //il.Emit(OpCodes.Callvirt, adder);
              //        addValue(s, pv);

              //        return true;

              //    case ProtoFieldAction.ChildObject:

              //        loadValue(s, pv);

              //        //s.LoadCurrentFieldValueToStack();

              //        //s.LoadCurrentValueOntoStack(il);

              //        var fieldInfo = s.LoadFieldProxy(s.CurrentField);
              //        var proxyType = fieldInfo.FieldType;

              //        var scanMethod = proxyType.GetMethodOrDie(nameof(IProtoProxy<Object>.Scan),
              //            typeof(Stream), typeof(Int64));


              //        il.Emit(OpCodes.Ldarg_1); //arg1 = input stream!

              //        il.Emit(OpCodes.Ldarg_1);
              //        il.Emit(OpCodes.Call, _getPositiveInt64);

              //        il.Emit(OpCodes.Call, scanMethod);

              //        addValue(s, pv);

              //        break;
              //}

              //return true;

          }

          private Boolean TryPrintAsArray(ProtoPrintState s, Byte[] headerBytes)
          {
            var il = s.IL;
            var pv = s.CurrentField;

            var pvProp = s.ParentType.GetProperty(pv.Name) ?? throw new InvalidOperationException();
            var getMethod = pvProp.GetGetMethod();

            if (!pv.Type.IsArray) 
                return false;

            var germane = _types.GetGermaneType(pv.Type);
            var arr = il.DeclareLocal(pv.Type);
            var info = new ProtoCollectionItem(pv.Type, _types, pv.Index);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, getMethod);
            il.Emit(OpCodes.Stloc, arr);


            var getLength = pv.Type.GetterOrDie(nameof(Array.Length), out _);
            var arrLength = il.DeclareLocal(typeof(Int32));
            il.Emit(OpCodes.Ldloc, arr);
            il.Emit(OpCodes.Call, getLength);
            il.Emit(OpCodes.Stloc, arrLength);


            var i = il.DeclareLocal(typeof(Int32));
            var current = il.DeclareLocal(germane);
            //
            var endOfLoop = il.DefineLabel();
            var nextLoop = il.DefineLabel();
            //
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, i);
            //

            //
            il.MarkLabel(nextLoop);
            il.Emit(OpCodes.Ldloc, i);
            il.Emit(OpCodes.Ldloc, arrLength);
            il.Emit(OpCodes.Bge, endOfLoop);
            //
            // ///////////////////////
            //
            il.Emit(OpCodes.Ldloc, arr);
            il.Emit(OpCodes.Ldloc, i);
            il.Emit(OpCodes.Ldelem, germane);
            il.Emit(OpCodes.Stloc, current);
            //

            PrintHeaderBytes(headerBytes, s);
                //il, ref isArrayMade, fieldByteArray, null);

                AddObtainableValueToPrintMethod(s, ilg => ilg.Emit(OpCodes.Ldloc, current));
                //il, ref isArrayMade, fieldByteArray,
                //ref localBytes,
                //ref localString, utfField,
                //info.TypeCode, info.WireType, info.Type,
                //ilg => ilg.Emit(OpCodes.Ldloc, current),
                //ilg => ilg.Emit(OpCodes.Ldloc, current),
                //ref hasPushed);

            // ///////////////////////
            //
            //
            il.Emit(OpCodes.Ldloc, i);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, i);
            il.Emit(OpCodes.Br, nextLoop);
            //
            il.MarkLabel(endOfLoop);

            return true;

        }

        private void ScanByteArray(ProtoScanState s, ref LocalBuilder holdForSet)
        {
            var il = s.IL;

            il.Emit(OpCodes.Ldarg_1);
            holdForSet ??= il.DeclareLocal(typeof(Byte[]));

            //Get length of the array
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt32);

            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Stloc, s.LastByteLocal);
            il.Emit(OpCodes.Newarr, typeof(Byte));
            il.Emit(OpCodes.Stloc, holdForSet);

            //read bytes into buffer field
            il.Emit(OpCodes.Ldloc, holdForSet);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldloc, s.LastByteLocal);
            il.Emit(OpCodes.Callvirt, _readStreamBytes);

            il.Emit(OpCodes.Pop);


            //if (s.SetCurrentValue != null)
            //{
            //    s.LoadCurrentValueOntoStack(il);

            //    il.Emit(OpCodes.Ldloc, holdForSet);
            //    s.SetCurrentValue(il);
            //}

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


            //il.Emit(OpCodes.Ldarg_1);

            ////Get length of the array
            //il.Emit(OpCodes.Ldarg_1);
            //il.Emit(OpCodes.Call, _getPositiveInt32);

            //il.Emit(OpCodes.Dup);
            //il.Emit(OpCodes.Stloc, lastByteLocal);
            //il.Emit(OpCodes.Newarr, typeof(Byte));
            

            ////read bytes into buffer field
            //il.Emit(OpCodes.Ldc_I4_0);
            //il.Emit(OpCodes.Ldloc, lastByteLocal);
            //il.Emit(OpCodes.Callvirt, _readStreamBytes);

            //il.Emit(OpCodes.Pop);
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
