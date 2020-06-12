using Das.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Das.Serializer.Proto;

namespace Das.Serializer.ProtoBuf
{
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        private void ScanPackedAddRange(ProtoScanState s, 
            Action<ILGenerator> setCurrentValue, 
            MethodInfo addRange)
        {
            var il = s.IL;
            var currentProp = s.CurrentField;

            if (!(GetPackedArrayType(currentProp.Type) is {} packType))
                return ;

            var willAddToExisting = TryLoadTargetReference(s, setCurrentValue);

            //if (!willAddToExisting)
            //    holdForSet ??= il.DeclareLocal(currentProp.Type);

            ////////////////////////////////////////////////////////
            // EXTRACT COLLECTION FROM STREAM
            ////////////////////////////////////////////////////////

            //Get the number of bytes we will be using
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt32);

            if (packType == typeof(Int32))
                il.Emit(OpCodes.Call, _extractPackedInt32Itar);

            else if (packType == typeof(Int16))
                il.Emit(OpCodes.Call, _extractPackedInt16Itar);

            else if (packType == typeof(Int64))
                il.Emit(OpCodes.Call, _extractPackedInt64Itar);

            ////////////////////////////////////////////////////////

            il.Emit(OpCodes.Callvirt, addRange);
        }

        private void ScanPackedAdd(ProtoScanState s, MethodInfo add)
        {
            var il = s.IL;
            var currentProp = s.CurrentField;

            if (!(GetPackedArrayType(currentProp.Type) is {} packType))
                return ;

            //if (!willAddToExisting)
            //    holdForSet ??= il.DeclareLocal(currentProp.Type);

            ////////////////////////////////////////////////////////
            // EXTRACT COLLECTION FROM STREAM
            ////////////////////////////////////////////////////////

            //Get the number of bytes we will be using
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt32);

            if (packType == typeof(Int32))
                il.Emit(OpCodes.Call, _extractPackedInt32Itar);

            else if (packType == typeof(Int16))
                il.Emit(OpCodes.Call, _extractPackedInt16Itar);

            else if (packType == typeof(Int64))
                il.Emit(OpCodes.Call, _extractPackedInt64Itar);

            ////////////////////////////////////////////////////////


            var myIEnum = typeof(IEnumerable<>).MakeGenericType(packType);

            var returnRef = il.DeclareLocal(myIEnum);

            il.Emit(OpCodes.Stloc, returnRef);

            var enumerator = new ProtoEnumerator<ProtoScanState>(s, myIEnum, returnRef);

            enumerator.ForEach((current, ss, ilg, hdr) => 
            {
                ss.LoadCurrentValueOntoStack(ilg);
                il.Emit(OpCodes.Callvirt, ss.CurrentField.GetMethod);

                ilg.Emit(OpCodes.Ldloc, current);
                //ilg.Emit(OpCodes.Pop);
                //ilg.Emit(OpCodes.Pop);
                //il.Emit(OpCodes.Callvirt, ss.CurrentField.GetMethod);

            
                    
                //ilg.Emit(OpCodes.Ldloc, current);
                ilg.Emit(OpCodes.Callvirt, add);

                if (add.ReturnType != null)
                    il.Emit(OpCodes.Pop);


            }, s.CurrentField.HeaderBytes);


            //il.Emit(OpCodes.Callvirt, addRange);
        }

        private Boolean TryScanAsPackedArray(ProtoScanState s,
            Action<ILGenerator>? setCurrentValue,
                ref LocalBuilder holdForSet)
        {
            var currentProp = s.CurrentField;
            var pv = currentProp;

            if (!(GetPackedArrayType(currentProp.Type) is {} packType))
                return false;

            if (pv.Type.TryGetMethod(nameof(List<object>.AddRange),
                out var addRange2))
            {
                ScanPackedAddRange(s, setCurrentValue, addRange2);
                return true;
            }

            if (_types.TryGetAddMethod(pv.Type, out var adder2))
            {
                ScanPackedAdd(s, adder2);
                return true;
            }

            var il = s.IL;

            //var canAddRange = pv.Type.TryGetMethod(nameof(List<object>.AddRange),
            //    out var addRange);
            var canAdd = _types.TryGetAddMethod(pv.Type, out var adder);

            //////////////////////////////////////////////////////////
            //// GET OUR IEnumerable[TPack] ONTO THE STACK if it's an updatable type
            //////////////////////////////////////////////////////////
            var willAddToExisting = TryLoadTargetReference(s, setCurrentValue);

            if (!willAddToExisting)
                holdForSet ??= il.DeclareLocal(currentProp.Type);

            ////////////////////////////////////////////////////////
            // EXTRACT COLLECTION FROM STREAM
            ////////////////////////////////////////////////////////

            //Get the number of bytes we will be using
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt32);

            if (packType == typeof(Int32))
                il.Emit(OpCodes.Call, _extractPackedInt32Itar);

            else if (packType == typeof(Int16))
                il.Emit(OpCodes.Call, _extractPackedInt16Itar);

            else if (packType == typeof(Int64))
                il.Emit(OpCodes.Call, _extractPackedInt64Itar);

            ////////////////////////////////////////////////////////

            if (currentProp.Type.IsArray)
            {
                //  if it's an array we have to assign rather than update/add

                var linqToArray = typeof(Enumerable).GetMethod(
                    nameof(Enumerable.ToArray), Const.PublicStatic);
                linqToArray = linqToArray.MakeGenericMethod(packType);

                //il.Emit(OpCodes.Ldloc, ienum);
                il.Emit(OpCodes.Call, linqToArray);

                if (willAddToExisting && setCurrentValue != null)
                    setCurrentValue(il);
                else
                    il.Emit(OpCodes.Stloc, holdForSet);

                return true;
            }

            var myIEnum = typeof(IEnumerable<>).MakeGenericType(packType);

            //if (canAddRange)
            //{
            //    ////////////////////////////////////////////////////////
            //    // ADDRANGE TO EXISTING ?
            //    ////////////////////////////////////////////////////////

            //    if (willAddToExisting)
            //    {
            //        //if (s.ParentType.IsInstanceOfType(s.ExampleObject))
            //        {
            //            //var getter = s.ExampleObject.GetType().GetterOrDie(currentProp.Name, out _);
            //            //var exampleValue = getter.Invoke(s.ExampleObject, null);

            //            //if (exampleValue != null)
            //            {
            //                il.Emit(OpCodes.Callvirt, addRange);
            //                return true;
            //            }
            //        }
            //    }
            //    else
            //    {
            //        il.Emit(OpCodes.Stloc, holdForSet);
            //        return true;
            //    }
            //}

            //else if (canAdd)
            //{
            //    var returnRef = il.DeclareLocal(myIEnum);

            //    il.Emit(OpCodes.Stloc, returnRef);

            //    var enumerator = new ProtoEnumerator<ProtoScanState>(s, myIEnum, returnRef);

            //    enumerator.ForEach((current, ss, ilg, hdr) => 
            //    {
            //        ss.LoadCurrentValueOntoStack(ilg);
            //        il.Emit(OpCodes.Callvirt, ss.CurrentField.GetMethod);

            //        il.Emit(OpCodes.Callvirt, ss.CurrentField.GetMethod);
                    
            //        ilg.Emit(OpCodes.Ldloc, current);
            //        ilg.Emit(OpCodes.Callvirt, adder);

            //    }, s.CurrentField.HeaderBytes);

            //    return true;
            //}

            ////////////////////////////////////////////////////////
            // ASSIGN = NEW CollectionType(IEnumerable<T>)?
            ////////////////////////////////////////////////////////
            var ctorMaybe = currentProp.Type.GetConstructor(new
                [] {myIEnum});

            if (ctorMaybe != null)
            {
                il.Emit(OpCodes.Newobj, ctorMaybe);
            }
            else
                throw new InvalidOperationException(
                    "Unable to reconstruct from packed repeated");


            if (setCurrentValue != null)
                setCurrentValue(il);
            else
                il.Emit(OpCodes.Stloc, holdForSet);

            return true;
        }

        private void PrintAsPackedArray(ProtoPrintState s)
        {
            var type = s.CurrentField.Type;
            var il = s.IL;

            PrintHeaderBytes(s.CurrentField.HeaderBytes, s);
            
            s.LoadParentToStack();

            if (!(GetPackedArrayType(type) is {} packType)) 
                throw new InvalidOperationException("Cannot print " + type + " as a packed repeated field");


            /////////////////////////////////////
            // var arrayLocalField = obj.Property;
            /////////////////////////////////////
            var arrayLocalField = il.DeclareLocal(type);

            il.Emit(OpCodes.Call, s.CurrentField.GetMethod);
            il.Emit(OpCodes.Stloc, arrayLocalField);
            /////////////////////////////////////

                

            /////////////////////////////////////
            // WriteInt32(GetPackedArrayLength(ienum)); 
            /////////////////////////////////////
            MethodInfo getPackedArrayLength = null!;

            if (packType == typeof(Int32))
                getPackedArrayLength = _getPackedInt32Length.MakeGenericMethod(type);
            else if (packType == typeof(Int16))
                getPackedArrayLength = _getPackedInt16Length.MakeGenericMethod(type);
            else if (packType == typeof(Int64))
                getPackedArrayLength = _getPackedInt64Length.MakeGenericMethod(type);

            //il.Emit(OpCodes.Ldarg_0);


            //il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, arrayLocalField);
            il.Emit(OpCodes.Call, getPackedArrayLength);
                
            s.WriteInt32();
            //il.Emit(OpCodes.Call, _writeInt32);

            /////////////////////////////////////

            /////////////////////////////////////
            // WriteInt32(GetPackedArrayLength(ienum)); 
            /////////////////////////////////////
            MethodInfo writePackedArray = null!;
                
            if (packType == typeof(Int32))
                writePackedArray = _writePacked32.MakeGenericMethod(type);
            else if (packType == typeof(Int16))
                writePackedArray = _writePacked16.MakeGenericMethod(type);
            else if (packType == typeof(Int64))
                writePackedArray = _writePacked64.MakeGenericMethod(type);

                
            il.Emit(OpCodes.Ldloc, arrayLocalField);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, writePackedArray);

            return;


            /////////////////////////////////////
            // WriteInt32(GetPackedArrayLength(ienum)); 
            /////////////////////////////////////
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, arrayLocalField);

                
            //il.Emit(OpCodes.Ldarg_1);
            //il.Emit(OpCodes.Call, getPackedArrayLength);
            //il.Emit(OpCodes.Call, _writeInt32);

            // WritePacked(ienum);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, arrayLocalField);

            if (typeof(IEnumerable<Int32>).IsAssignableFrom(type))
            {
                var methos = _writePacked32.MakeGenericMethod(type);
                il.Emit(OpCodes.Call, methos);
            }

            else if (typeof(IEnumerable<Int16>).IsAssignableFrom(type))
            {
                var methos = _writePacked16.MakeGenericMethod(type);
                il.Emit(OpCodes.Call, methos);
            }

            else if (typeof(IEnumerable<Int64>).IsAssignableFrom(type))
            {
                var methos = _writePacked64.MakeGenericMethod(type);
                il.Emit(OpCodes.Call, methos);
            }

            //return true;

            //return false;
        }

        private static Type? GetPackedArrayType(Type propertyType)
        {
            if (typeof(IEnumerable<Int32>).IsAssignableFrom(propertyType))
                return typeof(Int32);

            if (typeof(IEnumerable<Int16>).IsAssignableFrom(propertyType))
                return typeof(Int16);

            if (typeof(IEnumerable<Int64>).IsAssignableFrom(propertyType))
                return typeof(Int64);

            return default;
        }

        /// <summary>
        /// Puts the value of a property onto the stack if it can be updated
        /// </summary>
        private Boolean TryLoadTargetReference(ProtoScanState s, 
            Action<ILGenerator>? setCurrentValue)
        {
            if (setCurrentValue == null || String.IsNullOrEmpty(s.CurrentField?.Name) || 
                s.LoadCurrentValueOntoStack == null)
                return false;

            s.LoadCurrentValueOntoStack(s.IL);
            if (s.CurrentField.Type.IsArray)
            {
                //no point in loading a reference to an array since we will have to set the 
                //property value again anyways
                return true;
            }

            //var getter = s.ParentType.GetterOrDie(s.CurrentField.Name, out _);
            s.IL.Emit(OpCodes.Callvirt, s.CurrentField.GetMethod);
            //prop ref is now on the stack.

            return true;

        }

    }
}
