using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Das.Extensions;
using Das.Serializer.Proto;

namespace Das.Serializer
{

    public class ProtoEnumerator<TState> where TState : ProtoStateBase
    {
        private readonly ILGenerator _il;
        private readonly LocalBuilder _enumeratorLocal;
        private readonly MethodInfo? _enumeratorDisposeMethod;
        private readonly Type _enumeratorType;
        private readonly MethodInfo _enumeratorMoveNext;
        private readonly MethodInfo _enumeratorCurrent;
        private readonly LocalBuilder _enumeratorCurrentValue;
        private readonly TState _protoBuildState;

        public ProtoEnumerator(TState s, Type ienumerableType,
            MethodInfo getMethod) : this(s, ienumerableType)
        {
            _il = s.IL;
            _protoBuildState = s;
            var getEnumeratorMethod = ienumerableType.GetMethodOrDie(nameof(IEnumerable.GetEnumerator));

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Call, getMethod);
            _il.Emit(OpCodes.Callvirt, getEnumeratorMethod);
            _il.Emit(OpCodes.Stloc, _enumeratorLocal);
        }

        protected ProtoEnumerator(TState s, Type ienumerableType)
        {
            _il = s.IL;
            _protoBuildState = s;
            var getEnumeratorMethod = ienumerableType.GetMethodOrDie(nameof(IEnumerable.GetEnumerator));
            _enumeratorDisposeMethod = getEnumeratorMethod.ReturnType.GetMethod(
                nameof(IDisposable.Dispose));

            _enumeratorMoveNext = typeof(IEnumerator).GetMethodOrDie(
                nameof(IEnumerator.MoveNext));

            var isExplicit = _enumeratorDisposeMethod == null;
            if (isExplicit && typeof(IDisposable).IsAssignableFrom(getEnumeratorMethod.ReturnType))
            {
                _enumeratorDisposeMethod = typeof(IDisposable).GetMethodOrDie(
                    nameof(IDisposable.Dispose));
            }
            else
            {
                _enumeratorMoveNext = getEnumeratorMethod.ReturnType.GetMethodOrDie(
                    nameof(IEnumerator.MoveNext));
            }

            _enumeratorCurrent = getEnumeratorMethod.ReturnType.GetterOrDie(
                nameof(IEnumerator.Current), out _);


            _enumeratorLocal = _il.DeclareLocal(getEnumeratorMethod.ReturnType);
            _enumeratorType = _enumeratorLocal.LocalType ?? throw new InvalidOperationException();
            _enumeratorCurrentValue = _il.DeclareLocal(_enumeratorCurrent.ReturnType);
        }

        //public delegate void OnCollectionItem(
        //    LocalBuilder enumeratorCurrentValue, 
        //    ProtoPrintState s,
        //    ILGenerator il,
        //    Byte[] headerBytes);

        public void ForEach(Action<LocalBuilder, TState, ILGenerator, Byte[]> action, Byte[] headerBytes)
        {
            var allDone = _il.DefineLabel();

            /////////////////////////////////////
            // TRY
            /////////////////////////////////////
            if (_enumeratorDisposeMethod != null)
                _il.BeginExceptionBlock();
            {
                var tryNext = _il.DefineLabel();
                _il.MarkLabel(tryNext);

                /////////////////////////////////////
                // !enumerator.HasNext() -> EXIT LOOP
                /////////////////////////////////////
                _il.Emit(_enumeratorType.IsValueType 
                    ? OpCodes.Ldloca 
                    : OpCodes.Ldloc, _enumeratorLocal);

                _il.Emit(OpCodes.Call, _enumeratorMoveNext);
                _il.Emit(OpCodes.Brfalse, allDone);

                _il.Emit(_enumeratorType.IsValueType 
                    ? OpCodes.Ldloca 
                    : OpCodes.Ldloc, _enumeratorLocal);

                _il.Emit(OpCodes.Callvirt, _enumeratorCurrent);

                _il.Emit(OpCodes.Stloc, _enumeratorCurrentValue);

                /////////////

                //var shallPush = true;

                action(_enumeratorCurrentValue, _protoBuildState, _il, headerBytes);

                //if (!TryPrintAsDictionary(pv, il, headerBytes, ref isArrayMade,
                //    fieldByteArray, getMethod, ref localBytes, ref localString, utfField,
                //    ref hasPushed, enumeratorCurrentValue))
                //{
                //    var info = new ProtoCollectionItem(pv.Type, _types, pv.Index);
                //    shallPush = info.WireType == ProtoWireTypes.LengthDelimited &&
                //                Const.StrType != info.Type;

                //    if (shallPush)
                //    {
                //        il.Emit(OpCodes.Ldarg_0);
                //        il.Emit(OpCodes.Callvirt, _push);
                //        il.Emit(OpCodes.Pop);
                //        hasPushed = true;

                //        il.Emit(OpCodes.Ldarg_0);
                //        il.Emit(OpCodes.Ldc_I4, info.Header);
                //        il.Emit(OpCodes.Callvirt, _writeInt32);
                //    }

                //    AddObtainableValueToPrintMethod(il, ref isArrayMade, fieldByteArray,
                //        ref localBytes,
                //        ref localString, utfField,
                //        info.TypeCode, info.WireType, info.Type,
                //        ilg => ilg.Emit(OpCodes.Ldloc, enumeratorCurrentValue),
                //        ilg => ilg.Emit(OpCodes.Ldloca, enumeratorCurrentValue),
                //        ref hasPushed);
                //}

                ///////////////

                //if (shallPush)
                //{
                //    il.Emit(OpCodes.Ldarg_0);
                //    il.Emit(OpCodes.Callvirt, _pop);
                //    il.Emit(OpCodes.Pop);
                //}

                _il.Emit(OpCodes.Br, tryNext);

                _il.MarkLabel(allDone);
            }

            if (_enumeratorDisposeMethod == null)
                return;

            /////////////////////////////////////
            // FINALLY
            /////////////////////////////////////
            _il.BeginFinallyBlock();
            {
                if (_enumeratorType.IsValueType)
                    _il.Emit(OpCodes.Ldloca, _enumeratorLocal);
                else
                    _il.Emit(OpCodes.Ldloc, _enumeratorLocal);
                _il.Emit(OpCodes.Call, _enumeratorDisposeMethod);
            }
            _il.EndExceptionBlock();
        }
    }
}
