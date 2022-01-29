#if GENERATECODE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer.CodeGen;
using Das.Serializer.Properties;

namespace Das.Serializer.State
{
    public abstract class DynamicStateBase : IDynamicState
    {
        static DynamicStateBase()
        {
            _getArrayLength = typeof(Array).GetterOrDie(nameof(Array.Length), out _);
        }

        protected DynamicStateBase(ILGenerator il,
                                   ITypeManipulator types,
                                   Type parentType,
                                   Action<ILGenerator>? loadCurrentValueOntoStack,
                                   IDictionary<Type, ProxiedInstanceField> proxies,
                                   IFieldActionProvider actionProvider)
        {
            ParentType = parentType;
            _types = types;
            _localsByType = new Dictionary<Type, LocalBuilder>();
            _il = il;
            LoadCurrentValueOntoStack = loadCurrentValueOntoStack;
            _proxies = proxies;
            _actionProvider = actionProvider;
        }

        //public abstract Label VerifyValueIsNonDefault();

        public ProxiedInstanceField GetProxy(Type type)
        {
            return _proxies[type];
        }

        /// <summary>
        ///     Leaves the field on the stack
        /// </summary>
        public FieldInfo LoadFieldProxy(INamedField field)
        {
            var germane = _types.GetGermaneType(field.Type);

            var proxy = GetProxy(germane);
            var proxyField = proxy.ProxyField;

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, proxyField);

            return proxyField;
        }

        public virtual Label VerifyShouldPrintValue()
        {
            var propertyType = CurrentField.Type;

            var gotoIfFalse = _il.DefineLabel();

            if (propertyType.IsPrimitive)
            {
                LoadCurrentFieldValueToStack();

                if (propertyType == typeof(Double))
                {
                    _il.Emit(OpCodes.Ldc_R8, 0.0);
                    _il.Emit(OpCodes.Ceq);
                    _il.Emit(OpCodes.Brtrue, gotoIfFalse);
                }
                else
                    _il.Emit(OpCodes.Brfalse, gotoIfFalse);

                goto done;
            }

            var countProp = propertyType.GetProperty(nameof(IList.Count));
            if (countProp == null || countProp.GetGetMethod() is not { } countGetter)
                goto done;

            LoadCurrentFieldValueToStack();

            _il.Emit(OpCodes.Callvirt, countGetter);
            _il.Emit(OpCodes.Brfalse, gotoIfFalse);

            done:
            return gotoIfFalse;
        }

        public ILGenerator IL => _il;

        public abstract IPropertyInfo CurrentField { get; }

        //public abstract IPropertyInfo CurrentField { get; }

        public abstract void LoadCurrentFieldValueToStack();

        public LocalBuilder GetLocal(Type localType)
        {
            if (!_localsByType.TryGetValue(localType, out var local))
            {
                local = _il.DeclareLocal(localType);
                _localsByType.Add(localType, local);
            }

            return local;
        }

        public LocalBuilder GetLocal<T>()
        {
            return GetLocal(typeof(T));
        }

        protected void LoadNullableFieldValueToStack()
        {
            var nullableType = CurrentField.Type;

            if (!_types.TryGetNullableType(nullableType, out var baseType))
                throw new InvalidOperationException();

            var tmpVal = GetLocal(nullableType);

            var hasValue = nullableType.GetProperty(
                nameof(Nullable<Int32>.HasValue))!.GetGetMethod();

            var ifNull = _il.DefineLabel();
            var eof = _il.DefineLabel();

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Callvirt, CurrentField.GetMethod);

            _il.Emit(OpCodes.Stloc, tmpVal);
            _il.Emit(OpCodes.Ldloca, tmpVal);

            _il.Emit(OpCodes.Call, hasValue);
            _il.Emit(OpCodes.Brfalse, ifNull);

            // not null
            var getValue = tmpVal.LocalType!.GetProperty(
                nameof(Nullable<Int32>.Value))!.GetGetMethod();

            _il.Emit(OpCodes.Ldloca, tmpVal);
            _il.Emit(OpCodes.Call, getValue);
            
            _il.Emit(OpCodes.Br, eof);

            _il.MarkLabel(ifNull);

            _il.Emit(OpCodes.Ldnull);

            _il.MarkLabel(eof);
        }

        
        

        //public void LoadCurrentFieldValueToStack()
        //{
        //    LoadParentToStack();
        //    var call = ParentType.IsValueType ? OpCodes.Call : OpCodes.Callvirt;
        //    IL.Emit(call, CurrentField.GetMethod);
        //}


        public void LoadParentToStack()
        {
            var lode = LoadCurrentValueOntoStack
                       ?? throw new NullReferenceException(nameof(LoadCurrentValueOntoStack));
            lode(IL);
        }

        public abstract FieldAction CurrentFieldAction { get; }


        protected static TypeCode GetPackedArrayTypeCode(Type propertyType)
        {
            if (typeof(IEnumerable<Int32>).IsAssignableFrom(propertyType))
                return TypeCode.Int32;

            if (typeof(IEnumerable<Int16>).IsAssignableFrom(propertyType))
                return TypeCode.Int16;

            return typeof(IEnumerable<Int64>).IsAssignableFrom(propertyType)
                ? TypeCode.Int64
                : TypeCode.Empty;
        }

        protected static readonly MethodInfo _getArrayLength;
        protected readonly ILGenerator _il;

        protected readonly ITypeManipulator _types;
        private readonly Dictionary<Type, LocalBuilder> _localsByType;

        protected readonly Action<ILGenerator>? LoadCurrentValueOntoStack;

        protected readonly Type ParentType;

        private readonly IDictionary<Type, ProxiedInstanceField> _proxies;

        protected readonly IFieldActionProvider _actionProvider;
    }
}


#endif