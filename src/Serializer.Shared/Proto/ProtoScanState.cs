#if GENERATECODE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer.CodeGen;
using Das.Serializer.Properties;
using Das.Serializer.Proto;
using Reflection.Common;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoScanState : ProtoStateBase,
                                  IProtoScanState
    {
        public ProtoScanState(ILGenerator il,
                              IProtoFieldAccessor[] fields,
                              IProtoFieldAccessor currentField,
                              Type parentType,
                              Action<ILGenerator>? loadReturnValueOntoStack,
                              LocalBuilder lastByteLocal,
                              IStreamAccessor streamAccessor,
                              FieldInfo readBytesField,
                              ITypeManipulator types,
                              IInstantiator instantiator,
                              IDictionary<Type, ProxiedInstanceField> proxies,
                              IFieldActionProvider actionProvider)
            : base(il, currentField, parentType,
                loadReturnValueOntoStack, proxies, types, actionProvider)
        {
            LocalFieldValues = new Dictionary<IProtoFieldAccessor, LocalBuilder>();

            Fields = fields;
            _currentField = currentField;

            LastByteLocal = lastByteLocal;

            _loadReturnValueOntoStack = loadReturnValueOntoStack;
            _streamAccessor = streamAccessor;

            _readBytesField = readBytesField;
            _instantiator = instantiator;

            EnsureLocalFieldsForProperties(fields);
        }

        //static ProtoScanState()
        //{
        //    _dateFromFileTime = typeof(DateTime).GetPublicStaticMethodOrDie(
        //        nameof(DateTime.FromFileTime), typeof(Int64));
        //}

        public LocalBuilder LastByteLocal { get; }

        public void LoadNextString()
        {
            _il.Emit(OpCodes.Ldsfld, _streamAccessor.Utf8);

            _il.Emit(OpCodes.Ldsfld, _readBytesField);
            _il.Emit(OpCodes.Ldc_I4_0);
            LoadNextBytesIntoTempArray();

            _il.Emit(OpCodes.Call, _streamAccessor.GetStringFromBytes);
        }

        public IProtoFieldAccessor[] Fields { get; }

        /// <summary>
        ///     Creates a local variable for every field.  Use only when there is no parameterless ctor
        /// </summary>
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

        IProtoFieldAccessor IProtoScanState.CurrentField
        {
            get => _currentField;
            set => _currentField = value;
        }

        public Action<IProtoFieldAccessor, IProtoScanState> GetFieldSetCompletion(IProtoFieldAccessor field,
            Boolean canSetValueInline,
            Boolean isValuePreInitialized)
        {
            Action<IProtoFieldAccessor, IProtoScanState> res;

            switch (field.FieldAction)
            {
                case FieldAction.PackedArray:

                    if (isValuePreInitialized && TryScanAndAddPackedArray(field, out var r)
                                              && !ReferenceEquals(null, r))
                        return r;

                    goto asPrimitive;

                case FieldAction.Primitive:
                case FieldAction.VarInt:
                case FieldAction.String:
                case FieldAction.ChildObject:
                case FieldAction.ByteArray:
                case FieldAction.DateTime:
                case FieldAction.FallbackSerializable:
                case FieldAction.HasSpecialProperty:

                    asPrimitive:

                    if (canSetValueInline)
                        res = (_,
                               s) => s.IL.Emit(OpCodes.Callvirt, field.SetMethod ??
                                                                 throw new MissingMethodException(field.Name));
                    else
                    {
                        var local = GetLocalForField(field);
                        res = (_,
                               s) => s.IL.Emit(OpCodes.Stloc, local);
                        return res;
                    }

                    return res;

                case FieldAction.ChildObjectCollection:
                case FieldAction.ChildPrimitiveCollection:
                    if (canSetValueInline)
                        res = (_,
                               s) =>
                        {
                            if (!s.TryGetAdderForField(field, out var adder))
                                throw new NotSupportedException();

                            s.IL.Emit(OpCodes.Callvirt, adder);
                        };
                    else
                        res = (_,
                               s) =>
                        {
                            var local = GetLocalForField(field);
                            if (!_types.TryGetAddMethod(local.LocalType!, out var adder))
                                throw new NotSupportedException();

                            s.IL.Emit(OpCodes.Callvirt, adder);
                        };

                    return res;

                case FieldAction.Dictionary:
                    return AddKeyValuePair;

                case FieldAction.ChildObjectArray:
                case FieldAction.ChildPrimitiveArray:

                    res = (_,
                           s) =>
                    {
                        var local = GetLocalForField(field);
                        if (!_types.TryGetAddMethod(local.LocalType!, out var adder))
                            throw new NotSupportedException();

                        s.IL.Emit(OpCodes.Callvirt, adder);
                    };

                    return res;

                //case FieldAction.DateTime:
                //    if (canSetValueInline)
                //        res = (_,
                //               s) => s.IL.Emit(OpCodes.Callvirt, field.SetMethod ??
                //                                                 throw new MissingMethodException(field.Name));
                //    else
                //    {
                //        var local = GetLocalForField(field);
                //        res = (_,
                //               s) => s.IL.Emit(OpCodes.Stloc, local);
                //        return res;
                //    }

                //    return res;
                //    break;

                //case FieldAction.HasSpecialProperty:
                //case FieldAction.FallbackSerializable:
                

                //    if (!_actionProvider.TryGetSpecialProperty(field.Type, out var spatial))
                //        throw new NotImplementedException();

                //    var ctor = field.Type.GetConstructorOrDie(new Type[] { spatial.PropertyType });

                //    if (canSetValueInline)
                //        res = (_,
                //               s) =>
                //        {
                            
                                
                //                s.IL.Emit(OpCodes.Newobj, ctor);
                            

                //            s.IL.Emit(OpCodes.Callvirt, field.SetMethod ??
                //                                        throw new MissingMethodException(field.Name));
                //        };
                //    else
                //    {
                //        var local = GetLocalForField(field);
                        
                //        res = (_,
                //               s) =>
                //        {
                //            s.IL.Emit(OpCodes.Newobj, ctor);
                //            s.IL.Emit(OpCodes.Stloc, local);
                //        };
                //        return res;
                //    }

                //    return res;

                
                case FieldAction.NullableValueType:
                case FieldAction.Enum:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     - For settable non-collections, loads the parent instance onto the stack so that once the data is on
        ///     the stack, the setter can be called
        ///     - For non-array collections loads the value so that the 'Add' method can be called
        ///     For non-packed arrays, loads a local List
        /// </summary>
        public Action<IProtoFieldAccessor, IProtoScanState> GetFieldSetInit(IProtoFieldAccessor field,
                                                                            Boolean canSetValueInline)
        {
            Action<IProtoFieldAccessor, IProtoScanState> res;

            switch (field.FieldAction)
            {
                case FieldAction.Primitive:
                case FieldAction.VarInt:
                case FieldAction.String:
                case FieldAction.ChildObject:
                case FieldAction.ByteArray:
                case FieldAction.PackedArray:
                
                case FieldAction.DateTime:
                case FieldAction.NullableValueType:
                case FieldAction.HasSpecialProperty:
                case FieldAction.FallbackSerializable:
                case FieldAction.Enum:

                    if (canSetValueInline)
                        res = (_,
                               s) => _loadReturnValueOntoStack!(s.IL);
                    else
                        res = (_,
                               _) =>
                        {
                        };

                    return res;


                case FieldAction.ChildObjectCollection:
                case FieldAction.ChildPrimitiveCollection:
                case FieldAction.Dictionary:

                    if (canSetValueInline)
                        res = (_,
                               _) => LoadCurrentFieldValueToStack();
                    else
                    {
                        var local = GetLocalForField(field);
                        res = (_,
                               s) => s.IL.Emit(OpCodes.Ldloc, local);
                    }

                    return res;


                case FieldAction.ChildObjectArray:
                case FieldAction.ChildPrimitiveArray:

                    res = (_,
                           s) =>
                    {
                        var loko = GetLocalForField(field);
                        s.IL.Emit(OpCodes.Ldloc, loko);
                    };

                    return res;

               
               
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public void LoadPositiveInt32()
        {
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Call, _streamAccessor.GetPositiveInt32);
        }

        public Boolean TryGetAdderForField(IProtoFieldAccessor field,
                                           out MethodInfo adder)
        {
            var fieldType = field.Type;

            if (!_types.IsCollection(fieldType))
            {
                adder = default!;
                return false;
            }

            if (fieldType.IsArray && field.FieldAction != FieldAction.PackedArray)
            {
                var useLocal = GetLocalForField(field);
                fieldType = useLocal.LocalType;
            }

            return _types.TryGetAddMethod(fieldType!, out adder);
        }

        public LocalBuilder GetLocalForField(IProtoFieldAccessor field)
        {
            if (LocalFieldValues.TryGetValue(field, out var local))
                return local;

            var localType = field.Type.IsArray &&
                            field.FieldAction != FieldAction.PackedArray
                ? ArrayTypeToListOf(field.Type)
                : field.Type;

            local = _il.DeclareLocal(localType);

            if (_instantiator.TryGetDefaultConstructor(localType, out var fieldCtor)
                && !ReferenceEquals(null, fieldCtor))
            {
                _il.Emit(OpCodes.Newobj, fieldCtor);
                _il.Emit(OpCodes.Stloc, local);
            }

            LocalFieldValues.Add(field, local);

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
        ///     Leaves the # of bytes read on the stack!
        /// </summary>
        public void LoadNextBytesIntoTempArray()
        {
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Ldsfld, _readBytesField);
            _il.Emit(OpCodes.Ldc_I4_0);

            LoadPositiveInt32();

            _il.Emit(OpCodes.Callvirt, _streamAccessor.ReadStreamBytes);
        }

        private void AddKeyValuePair(IProtoFieldAccessor pv,
                                     IProtoScanState s)
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

        private Type ArrayTypeToListOf(Type arrayType)
        {
            if (!arrayType.IsArray)
                throw new TypeAccessException(nameof(arrayType));

            var germane = _types.GetGermaneType(arrayType);

            return typeof(List<>).MakeGenericType(germane);
        }

        private void EnsureLocalFieldsForProperties(IEnumerable<IProtoFieldAccessor> fields)
        {
            foreach (var field in fields)
            {
                switch (field.FieldAction)
                {
                    case FieldAction.ChildObjectArray:
                    case FieldAction.ChildPrimitiveArray:
                        var local = GetLocalForField(field);
                        if (local == null)
                            throw new InvalidOperationException();
                        break;
                }
            }
        }

        private Boolean TryScanAndAddPackedArray(IProtoFieldAccessor field,
                                                 out Action<IProtoFieldAccessor, IProtoScanState>? res)
        {
            res = default;

            if (!field.Type.IsGenericType)
                return false;

            var germane = _types.GetGermaneType(field.Type);
            var iColl = typeof(ICollection<>).MakeGenericType(germane);
            if (!iColl.IsAssignableFrom(field.Type))
                return false;

            MethodInfo? baseAdd;


            switch (Type.GetTypeCode(germane))
            {
                case TypeCode.Int16:
                    baseAdd = typeof(ProtoDynamicBase).GetMethodOrDie(
                        nameof(ProtoDynamicBase.AddPacked16));
                    break;

                case TypeCode.Int32:
                    baseAdd = typeof(ProtoDynamicBase).GetMethodOrDie(
                        nameof(ProtoDynamicBase.AddPacked32));
                    break;

                case TypeCode.Int64:
                    baseAdd = typeof(ProtoDynamicBase).GetMethodOrDie(
                        nameof(ProtoDynamicBase.AddPacked64));
                    break;

                default:
                    return false;
            }

            var adder = baseAdd.MakeGenericMethod(field.Type);

            res = (_,
                   s) =>
            {
                s.IL.Emit(OpCodes.Callvirt, field.GetMethod);

                s.IL.Emit(OpCodes.Ldarg_1);
                s.LoadPositiveInt32();

                s.IL.Emit(OpCodes.Call, adder);
            };

            return true;
        }


        /// <summary>
        ///     For values that will be ctor injected
        /// </summary>
        public Dictionary<IProtoFieldAccessor, LocalBuilder> LocalFieldValues { get; }

        private readonly IInstantiator _instantiator;

        private readonly Action<ILGenerator>? _loadReturnValueOntoStack;

        private readonly FieldInfo _readBytesField;
        private readonly IStreamAccessor _streamAccessor;

        //private static readonly MethodInfo _dateFromFileTime;
    }
}

#endif
