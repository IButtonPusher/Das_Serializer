using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Das.Extensions;
using Das.Serializer.Proto;

namespace Das.Serializer.ProtoBuf
{
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        private Boolean TryScanAsDictionary(ProtoScanState s)
        {
            var il = s.IL;
            var currentProp = s.CurrentField;

            if (!typeof(IDictionary).IsAssignableFrom(currentProp.Type))
                return false;

            var info = new ProtoDictionaryInfo(currentProp.Type, _types, this);

            if (!currentProp.Type.TryGetMethod(nameof(IList.Add),
                    out var adder, info.KeyType, info.ValueType) &&
                !currentProp.Type.TryGetMethod(
                    nameof(ConcurrentDictionary<Object, Object>.TryAdd),
                    out adder, info.KeyType, info.ValueType))
                throw new InvalidOperationException();

            //discard total kvp length
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt32);
            il.Emit(OpCodes.Pop);

            //discard key header?
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt32);
            il.Emit(OpCodes.Pop);

            LocalBuilder keyLocal = null;
            LocalBuilder valueLocal = null;

            var holdCurrentProp = s.CurrentField;

            s.CurrentField = info.Key;

            PopulateScanField(s, ref keyLocal);


            //discard value header?
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt32);
            il.Emit(OpCodes.Pop);

            s.CurrentField = info.Value;

            PopulateScanField(s, ref valueLocal);

            s.CurrentField = holdCurrentProp;
               
            //
            var getter = s.ParentType.GetterOrDie(currentProp.Name, out _);
            //
            s.LoadCurrentValueOntoStack(il);
            il.Emit(OpCodes.Callvirt, getter);

            il.Emit(OpCodes.Ldloc, keyLocal);
            il.Emit(OpCodes.Ldloc, valueLocal);
            il.Emit(OpCodes.Call, adder);
            if (adder.ReturnType != typeof(void))
                il.Emit(OpCodes.Pop);

            return true;

        }

         private Boolean TryPrintAsDictionary(ProtoPrintState s,
             LocalBuilder enumeratorCurrentValue)
        {

            var pv = s.CurrentField;
            var il = s.IL;

            if (!typeof(IDictionary).IsAssignableFrom(pv.Type)) 
                return false;

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, _push);
            il.Emit(OpCodes.Pop);
            s.HasPushed = true;

            var info = new ProtoDictionaryInfo(pv.Type, _types, this);

            /////////////////////////////////////
            // PRINT KEY'S HEADER / KEY'S VALUE
            /////////////////////////////////////
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, info.KeyHeader);
            il.Emit(OpCodes.Callvirt, _writeInt32);

            var holdProp = s.CurrentField;

            s.CurrentField = info.Key;

            AddGettableValueToPrintMethod(s, info.KeyGetter,
                ilg => ilg.Emit(OpCodes.Ldloca, enumeratorCurrentValue));
            /////////////////////////////////////
                

            /////////////////////////////////////
            // PRINT VALUE'S HEADER / VALUE'S VALUE
            /////////////////////////////////////
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, info.ValueHeader);
            il.Emit(OpCodes.Callvirt, _writeInt32);

            s.CurrentField = info.Value;

            AddGettableValueToPrintMethod(s, info.ValueGetter,
                ilg => ilg.Emit(OpCodes.Ldloca, enumeratorCurrentValue));

            s.CurrentField = holdProp;

            return true;

        }

         private void PrintDictionary(ProtoPrintState s)
         {
             var pv = s.CurrentField;

             var ienum = new ProtoEnumerator<ProtoPrintState>(s, pv.Type, pv.GetMethod);
             var shallPush = true;

             ienum.ForEach(PrintKeyValuePair, pv.HeaderBytes);
         }

         private void PrintCollection(ProtoPrintState s)
         {
             var pv = s.CurrentField;
             var ienum = new ProtoEnumerator<ProtoPrintState>(s, pv.Type, pv.GetMethod);

             ienum.ForEach(PrintEnumeratorCurrent, pv.HeaderBytes);
         }

         private void PrintKeyValuePair(LocalBuilder enumeratorCurrentValue, 
             ProtoPrintState s,
             ILGenerator il,
             //ref Boolean isArrayMade,
             Byte[] headerBytes)
         {

             var pv = s.CurrentField;
             


             il.Emit(OpCodes.Ldarg_0);
             il.Emit(OpCodes.Callvirt, _push);
             il.Emit(OpCodes.Pop);
             s.HasPushed = true;

             var info = new ProtoDictionaryInfo(pv.Type, _types, this);

             /////////////////////////////////////
             // PRINT KEY'S HEADER / KEY'S VALUE
             /////////////////////////////////////
             il.Emit(OpCodes.Ldarg_0);
             il.Emit(OpCodes.Ldc_I4, info.KeyHeader);
             il.Emit(OpCodes.Callvirt, _writeInt32);

             var holdProp = s.CurrentField;

             s.CurrentField = info.Key;

             AddGettableValueToPrintMethod(s, info.KeyGetter,
                 ilg => ilg.Emit(OpCodes.Ldloca, enumeratorCurrentValue));
             //il, ref isArrayMade, s.ByteBufferField,
             //    ref s.localBytes,
             //    ilg => ilg.Emit(OpCodes.Ldloca, enumeratorCurrentValue),
             //    ref localString, utfField,
             //    Type.GetTypeCode(info.KeyType), info.KeyWireType, info.KeyType,
             //    info.KeyGetter, ref hasPushed);
             /////////////////////////////////////


             /////////////////////////////////////
             // PRINT VALUE'S HEADER / VALUE'S VALUE
             /////////////////////////////////////
             il.Emit(OpCodes.Ldarg_0);
             il.Emit(OpCodes.Ldc_I4, info.ValueHeader);
             il.Emit(OpCodes.Callvirt, _writeInt32);

             s.CurrentField = info.Value;

             AddGettableValueToPrintMethod(s, info.ValueGetter,
                 ilg => ilg.Emit(OpCodes.Ldloca, enumeratorCurrentValue));
             //il, ref isArrayMade, s.ByteBufferField,
             //ref localBytes,
             //ilg => ilg.Emit(OpCodes.Ldloca, enumeratorCurrentValue),
             //ref localString, utfField,
             //Type.GetTypeCode(info.ValueType), info.ValueWireType,
             //info.ValueType, info.ValueGetter, ref hasPushed);


             s.CurrentField = holdProp;
         }

       

          private void PrintEnumeratorCurrent(
            LocalBuilder enumeratorCurrentValue, 
            ProtoPrintState s,
            ILGenerator il,
            Byte[] headerBytes)
        {
            PrintChildObject(s, headerBytes, 
                ilg => ilg.Emit(OpCodes.Ldloc, enumeratorCurrentValue));
        }
       

         /// <summary>
        /// ICollection[TProperty] where TProperty : ProtoContract
        /// for a collection of proto contracts by way of a property of a parent contract
        /// </summary>
        private Boolean TryScanAsNestedCollection(ProtoScanState s)
         {
             var il = s.IL;
             var pv = s.CurrentField;

             var canAddRange = pv.Type.TryGetMethod(nameof(List<object>.AddRange),
                 out var addRange);
             var canAdd = _types.TryGetAddMethod(pv.Type, out var adder);

             if (canAddRange)
             {
                 s.LoadCurrentValueOntoStack(il);
                 il.Emit(OpCodes.Callvirt, pv.GetMethod);
             }

             var germane = _types.GetGermaneType(pv.Type);

             var coreMethod = typeof(ProtoDynamicBase).GetMethod(nameof(ProtoDynamicBase.GetChildren));
             var getChildrenMethod = coreMethod.MakeGenericMethod(germane);

             var proxyLocal = s.ChildProxies[s.CurrentField];

             il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);  //arg1 = input stream!
            il.Emit(OpCodes.Ldloc, proxyLocal);
            il.Emit(OpCodes.Call, getChildrenMethod);
            

            if (canAddRange)
                il.Emit(OpCodes.Callvirt, addRange);
            
            else if (canAdd)
            {
                var returnRef = il.DeclareLocal(getChildrenMethod.ReturnType);

                var enumerator = new ProtoEnumerator<ProtoScanState>(s, getChildrenMethod.ReturnType, returnRef);

                enumerator.ForEach((current, ss, ilg, hdr) => 
                {
                    s.LoadCurrentValueOntoStack(ilg);
                    ilg.Emit(OpCodes.Ldloc, current);
                    ilg.Emit(OpCodes.Callvirt, adder);

                }, s.CurrentField.HeaderBytes);
            }
            

            
            
            
            /////////////////////////////////////////////////////////
            //// CREATE A LOCAL FIELD FOR THE PROXY AND SET ITS VALUE
            /////////////////////////////////////////////////////////
            //var proxyType = typeof(IProtoProxy<>).MakeGenericType(germane);
            
            //var localProxyRef = il.DeclareLocal(proxyType);
            
            ////_proxyProvider.GetProtoProxy<T>(false);
            //var getProxyInstance = _getProtoProxy.MakeGenericMethod(germane);
            //il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Ldfld, _proxyProviderField);
            //il.Emit(OpCodes.Ldc_I4_0);
            //il.Emit(OpCodes.Callvirt, getProxyInstance);
            
            ////var localProxyRef = _proxyProvider.GetProtoProxy<T>(false);
            //il.Emit(OpCodes.Stloc, localProxyRef);


            /////////////////////////////////////////////////////////
            //// SET THE PROXY'S OUTSTREAM TO OUR OUTSTREAM
            /////////////////////////////////////////////////////////

            //var streamSetter = proxyType.SetterOrDie(nameof(IProtoProxy<Object>.OutStream));


            //il.Emit(OpCodes.Ldloc, localProxyRef);
            
            //il.Emit(OpCodes.Ldarg_1);
            //il.Emit(OpCodes.Callvirt, streamSetter);

            return true;

            var wire = ProtoBufSerializer.GetWireType(germane);
            var isValidLengthDelim = wire == ProtoWireTypes.LengthDelimited
                                     && germane != Const.StrType
                                     && germane != Const.ByteArrayType;

            var isCollection = isValidLengthDelim && _types.IsCollection(germane);

            var fieldAction = GetProtoFieldAction(germane);

            var asGermane = new ProtoField(String.Empty,
                germane, wire, 0, 0, null, Type.GetTypeCode(germane),
                _types.IsLeaf(germane, false), isCollection, fieldAction, new Byte[0], null);

            LocalBuilder itemLocal = null;

            var parentType = s.ParentType;
            //var fieldByteArray = s.ByteBufferField;
            


            var setter = parentType.SetterOrDie(pv.Name);
            s.SetCurrentValue = ill => ill.Emit(OpCodes.Callvirt, setter);

            PopulateScanField(s, ref itemLocal);
                //il, asGermane, fieldByteArray,
                //lastByte, 
                
                //loadObject,
                //ill => ill.Emit(OpCodes.Callvirt, setter),
                //currentProp.Type, arrayCounters, exampleObject);

            var getter = parentType.GetterOrDie(pv.Name, out _);
            //
            s.LoadCurrentValueOntoStack(il);
            il.Emit(OpCodes.Callvirt, getter);

            il.Emit(OpCodes.Ldloc, itemLocal);

            il.Emit(OpCodes.Call, adder);
            if (adder.ReturnType != typeof(void))
                il.Emit(OpCodes.Pop);

            return true;
        }

        private Boolean TryPrintAsArray(ProtoPrintState s, Byte[] headerBytes)
            
            //IProtoField pv, ILGenerator il, Byte[] headerBytes,
            //ref Boolean isArrayMade, LocalBuilder fieldByteArray, MethodInfo getMethod,
            //ref LocalBuilder? localBytes, ref LocalBuilder localString, FieldInfo utfField,
            //ref Boolean hasPushed)
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


            if (s.SetCurrentValue != null)
            {
                il.Emit(OpCodes.Ldloc, holdForSet);
                s.SetCurrentValue(il);
            }

        }

        private void PrintByteArray(ProtoPrintState s)
        {
            PrintHeaderBytes(s.CurrentField.HeaderBytes, s);

            var il = s.IL;

            il.Emit(OpCodes.Call, s.CurrentField.GetMethod);
            il.Emit(OpCodes.Stloc, s.LocalBytes);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, s.LocalBytes);
            il.Emit(OpCodes.Call, _getArrayLength);
            il.Emit(OpCodes.Call, _writeInt32);

            il.Emit(OpCodes.Ldloc, s.LocalBytes);
            il.Emit(OpCodes.Call, _writeBytes);
        }
       
    }
}
