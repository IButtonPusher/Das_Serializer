using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer.Proto
{
    public class ProtoScanState : ProtoStateBase, IValueExtractor
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
            IDictionary<Type, FieldBuilder> proxies)
            : base(il, currentField, parentType, 
                loadReturnValueOntoStack, proxies, types)
        {
            LocalFieldValues = new Dictionary<IProtoFieldAccessor, LocalBuilder>();

            Fields = fields;
            CurrentField = currentField;

            LastByteLocal = lastByteLocal;

            _loadReturnValueOntoStack = loadReturnValueOntoStack;
            _streamAccessor = streamAccessor;

            _readBytesField = readBytesField;
            _types = types;
            _instantiator = instantiator;

            EnsureLocalFieldsForProperties(fields);
        }

        private void EnsureLocalFieldsForProperties(IEnumerable<IProtoFieldAccessor> fields)
        {
            foreach (var field in fields)
            {
                switch (field.FieldAction)
                {
                    case ProtoFieldAction.ChildObjectArray:
                    case ProtoFieldAction.ChildPrimitiveArray:
                        var local = GetLocalForField(field);
                        if (local == null)
                            throw new InvalidOperationException();
                        break;
                }
            }
        }

        /// <summary>
        /// - For settable non-collections, loads the parent instance onto the stack so that once the data is on
        /// the stack, the setter can be called
        /// 
        /// - For non-array collections loads the value so that the 'Add' method can be called
        /// For non-packed arrays, loads a local List 
        /// </summary>
        public Action<IProtoFieldAccessor, ProtoScanState> GetFieldSetInit(IProtoFieldAccessor field,
            Boolean canSetValueInline)
        {
            Action<IProtoFieldAccessor, ProtoScanState> res;

            switch (field.FieldAction)
            {
                case ProtoFieldAction.Primitive:
                case ProtoFieldAction.VarInt:
                case ProtoFieldAction.String:
                case ProtoFieldAction.ChildObject:
                case ProtoFieldAction.ByteArray:
                case ProtoFieldAction.PackedArray:

                    if (canSetValueInline)
                        res = (f, s) => _loadReturnValueOntoStack!(s.IL);
                    else
                        res = (f, s) => { };

                    return res;


                case ProtoFieldAction.ChildObjectCollection:
                case ProtoFieldAction.ChildPrimitiveCollection:
                case ProtoFieldAction.Dictionary:

                    if (canSetValueInline)
                        res = (f,s) => LoadCurrentFieldValueToStack();
                    else
                    {
                        var local = GetLocalForField(field);
                        res = (f, s) => s.IL.Emit(OpCodes.Ldloc, local);
                    }

                    return res;
                
                
                case ProtoFieldAction.ChildObjectArray:
                case ProtoFieldAction.ChildPrimitiveArray:

                    res = (f, s) =>
                    {
                        var loko = GetLocalForField(field);
                        s.IL.Emit(OpCodes.Ldloc, loko);
                    };


                    return res;


                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        public Action<IProtoFieldAccessor, ProtoScanState> GetFieldSetCompletion(IProtoFieldAccessor field,
            Boolean canSetValueInline)
        {
            Action<IProtoFieldAccessor, ProtoScanState> res;

            switch (field.FieldAction)
            {
                case ProtoFieldAction.Primitive:
                case ProtoFieldAction.VarInt:
                case ProtoFieldAction.String:
                case ProtoFieldAction.ChildObject:
                case ProtoFieldAction.ByteArray:
                case ProtoFieldAction.PackedArray:
                    
                    if (canSetValueInline)
                        res = (f, s) => s.IL.Emit(OpCodes.Callvirt, field.SetMethod!);
                    else
                    {
                        var local = GetLocalForField(field);
                        res = (f, s) => s.IL.Emit(OpCodes.Stloc, local);
                        return res;
                    }

                    return res;


                case ProtoFieldAction.ChildObjectCollection:
                case ProtoFieldAction.ChildPrimitiveCollection:
                    if (canSetValueInline)
                        res = (f, s) =>
                        {
                            if (!s.TryGetAdderForField(field, out var adder))
                                throw new NotSupportedException();
                            
                            s.IL.Emit(OpCodes.Callvirt, adder);
                        };
                    else
                    {
                        res = (f, s) =>
                        {
                            var local = GetLocalForField(field);
                            if (!_types.TryGetAddMethod(local.LocalType, out var adder))
                                throw new NotSupportedException();

                            s.IL.Emit(OpCodes.Callvirt, adder);
                        };
                    }

                    return res;

                case ProtoFieldAction.Dictionary:
                    return AddKeyValuePair;


                case ProtoFieldAction.ChildObjectArray:
                case ProtoFieldAction.ChildPrimitiveArray:

                    res = (f, s) =>
                    {
                        var local = GetLocalForField(field);
                        if (!_types.TryGetAddMethod(local.LocalType, out var adder))
                            throw new NotSupportedException();

                        s.IL.Emit(OpCodes.Callvirt, adder);
                    };


                    return res;


                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AddKeyValuePair(IProtoFieldAccessor pv, ProtoScanState s)
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


        public IProtoFieldAccessor[] Fields { get; }



        public LocalBuilder LastByteLocal { get; }


        /// <summary>
        ///     For values that will be ctor injected
        /// </summary>
        public Dictionary<IProtoFieldAccessor, LocalBuilder> LocalFieldValues { get; }


        /// <summary>
        /// Creates a local variable for every field.  Use only when there is no parameterless ctor
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

        public Boolean TryGetAdderForField(IProtoFieldAccessor field, out MethodInfo adder)
        {
            var fieldType = field.Type;

            if (!_types.IsCollection(fieldType))
            {
                adder = default!;
                return false;
            }

            if (fieldType.IsArray && field.FieldAction != ProtoFieldAction.PackedArray)
            {
                var useLocal = GetLocalForField(field);
                fieldType = useLocal.LocalType;
            }

            return _types.TryGetAddMethod(fieldType, out adder);
        }


        public LocalBuilder GetLocalForField(IProtoFieldAccessor field)
        {
            if (LocalFieldValues.TryGetValue(field, out var local)) 
                return local;

            var localType = field.Type.IsArray && 
                            field.FieldAction != ProtoFieldAction.PackedArray
                ? ArrayTypeToListOf(field.Type)
                : field.Type;

            local = _il.DeclareLocal(localType);

            if (_instantiator.TryGetDefaultConstructor(localType, out var fieldCtor))
            {
                _il.Emit(OpCodes.Newobj, fieldCtor);
                _il.Emit(OpCodes.Stloc, local);
            }

            LocalFieldValues.Add(field, local);

            return local;
        }

        private Type ArrayTypeToListOf(Type arrayType)
        {
            if (!arrayType.IsArray)
                throw new TypeAccessException(nameof(arrayType));

            var germane = _types.GetGermaneType(arrayType);

            return typeof(List<>).MakeGenericType(germane);

        }

        public LocalBuilder GetLocalForParameter(ParameterInfo prm)
        {
            foreach (var kvp in LocalFieldValues.Where(k => k.Key.Equals(prm))) return kvp.Value;

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

        public void LoadNextString()
        {
            _il.Emit(OpCodes.Ldsfld, _streamAccessor.Utf8);

            _il.Emit(OpCodes.Ldsfld, _readBytesField);
            _il.Emit(OpCodes.Ldc_I4_0);
            LoadNextBytesIntoTempArray();

            _il.Emit(OpCodes.Call, _streamAccessor.GetStringFromBytes);
        }


        public void LoadPositiveInt32()
        {
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Call, _streamAccessor.GetPositiveInt32);
        }

        private readonly FieldInfo _readBytesField;
        private readonly ITypeManipulator _types;

        private readonly Action<ILGenerator>? _loadReturnValueOntoStack;
        private readonly IStreamAccessor _streamAccessor;
        private readonly IInstantiator _instantiator;
    }
}