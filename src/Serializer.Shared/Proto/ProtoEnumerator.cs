#if GENERATECODE

using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer
{
    public class ProtoEnumerator<TState> where TState : ProtoStateBase
    {
        public ProtoEnumerator(TState s,
                               Type ienumerableType,
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

        protected ProtoEnumerator(TState s,
                                  Type ienumerableType)
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
                _enumeratorDisposeMethod = typeof(IDisposable).GetMethodOrDie(
                    nameof(IDisposable.Dispose));
            else
                _enumeratorMoveNext = getEnumeratorMethod.ReturnType.GetMethodOrDie(
                    nameof(IEnumerator.MoveNext));

            _enumeratorCurrent = getEnumeratorMethod.ReturnType.GetterOrDie(
                nameof(IEnumerator.Current), out _);

            _enumeratorLocal = _il.DeclareLocal(getEnumeratorMethod.ReturnType);
            _enumeratorType = _enumeratorLocal.LocalType ?? throw new InvalidOperationException();
            _enumeratorCurrentValue = _il.DeclareLocal(_enumeratorCurrent.ReturnType);
        }


        public void ForEach(Action<LocalBuilder, TState, ILGenerator, Byte[]> action,
                            Byte[] headerBytes)
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

                /////////////////////////////////////////////////////////////
                action(_enumeratorCurrentValue, _protoBuildState, _il, headerBytes);
                /////////////////////////////////////////////////////////////

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

        private readonly MethodInfo _enumeratorCurrent;
        private readonly LocalBuilder _enumeratorCurrentValue;
        private readonly MethodInfo? _enumeratorDisposeMethod;
        private readonly LocalBuilder _enumeratorLocal;
        private readonly MethodInfo _enumeratorMoveNext;
        private readonly Type _enumeratorType;
        private readonly ILGenerator _il;
        private readonly TState _protoBuildState;
    }
}

#endif
