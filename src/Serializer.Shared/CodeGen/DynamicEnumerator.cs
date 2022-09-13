#if GENERATECODE

using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer.Properties;
using Das.Serializer.State;
using Reflection.Common;

namespace Das.Serializer
{
    /// <summary>
    ///     Generates dynamic foreach code block
    /// </summary>
    public class DynamicEnumerator<TState> where TState : IDynamicState
    {
        public DynamicEnumerator(TState s,
                                 IPropertyInfo prop,
                                 ITypeManipulator types,
                                 IFieldActionProvider actionProvider)
        : this(s, prop.Type, prop.GetMethod, types, actionProvider)
        {
            
        }

        public DynamicEnumerator(TState s,
                                 Type ienumerableType,
                                 MethodInfo getMethod,
                                 ITypeManipulator types,
                                 IFieldActionProvider actionProvider) 
            : this(s, ienumerableType, types, actionProvider)
        {
            _il = s.IL;
            _buildState = s;
            var getEnumeratorMethod = ienumerableType.GetMethodOrDie(nameof(IEnumerable.GetEnumerator));

            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Call, getMethod);
            _il.Emit(OpCodes.Callvirt, getEnumeratorMethod);
            _il.Emit(OpCodes.Stloc, _enumeratorLocal);
        }

        protected DynamicEnumerator(TState s,
                                    Type ienumerableType,
                                    ITypeManipulator types,
                                    IFieldActionProvider actionProvider)
        {
            _il = s.IL;
            _buildState = s;
            _types = types;
            _actionProvider = actionProvider;
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

            _enumeratorLocal = s.GetLocal(getEnumeratorMethod.ReturnType);
            //_il.DeclareLocal(getEnumeratorMethod.ReturnType);
            _enumeratorType = _enumeratorLocal.LocalType ?? throw new InvalidOperationException();
            _enumeratorCurrentValue = s.GetLocal(_enumeratorCurrent.ReturnType);
        }

        public void ForEach(OnIndexedValueReady action)
        {
            ForEachImpl(null, action);
        }

        public void ForEach(OnValueReady action)
        {
            ForEachImpl(action, null);
        }

        private void ForEachImpl(OnValueReady? action,
                                 OnIndexedValueReady? indexedAction)
        {
            if (action == null && indexedAction == null)
                throw new InvalidOperationException();

            var germane = _types.GetGermaneType(_buildState.CurrentField.Type);
            var subAction = _actionProvider.GetProtoFieldAction(germane);
            

            var allDone = _il.DefineLabel();

            var actionIndex = indexedAction != null
                ? _il.DeclareLocal(typeof(Int32))
                : default;

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

                if (action is { } notIndexed)
                    notIndexed(_enumeratorCurrentValue, germane, subAction);
                else
                {
                    indexedAction!(_enumeratorCurrentValue, actionIndex!, germane, subAction);
                    _il.Emit(OpCodes.Ldloc, actionIndex!);
                    _il.Emit(OpCodes.Ldc_I4_1);
                    _il.Emit(OpCodes.Add);
                    _il.Emit(OpCodes.Stloc, actionIndex!);

                }

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

        public void ForLoop(OnIndexedValueReady action)
        {
            var pv = _buildState.CurrentField;

            var germane = _types.GetGermaneType(pv.Type);
            var subAction = _actionProvider.GetProtoFieldAction(germane);

            var getLength = pv.Type.GetterOrDie(nameof(Array.Length), out _);
            var arrLength = _il.DeclareLocal(Const.IntType);
            _buildState.LoadCurrentFieldValueToStack();
            _il.Emit(OpCodes.Callvirt, getLength);
            _il.Emit(OpCodes.Stloc, arrLength);

            // for (var c = 0;
            var fore = _il.DefineLabel();
            var breakLoop = _il.DefineLabel();

            var c = _il.DeclareLocal(Const.IntType);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Stloc, c);
            _il.MarkLabel(fore);

            // c < arr.Length
            _il.Emit(OpCodes.Ldloc, c);
            _il.Emit(OpCodes.Ldloc, arrLength);
            _il.Emit(OpCodes.Bge, breakLoop);

            // var current = array[c];
            
            var current = _il.DeclareLocal(germane);

            _buildState.LoadCurrentFieldValueToStack();
            _il.Emit(OpCodes.Ldloc, c);
            _il.Emit(OpCodes.Ldelem_Ref);
            _il.Emit(OpCodes.Stloc, current);

            ///////////////////////////////////////////////////////////////
            action(current, c, germane, subAction);
            ///////////////////////////////////////////////////////////////

            // c++
            _il.Emit(OpCodes.Ldloc, c);
            _il.Emit(OpCodes.Ldc_I4_1);
            _il.Emit(OpCodes.Add);
            _il.Emit(OpCodes.Stloc, c);
            _il.Emit(OpCodes.Br, fore);


            _il.MarkLabel(breakLoop);
        }

        private readonly MethodInfo _enumeratorCurrent;
        private readonly LocalBuilder _enumeratorCurrentValue;
        private readonly MethodInfo? _enumeratorDisposeMethod;
        private readonly LocalBuilder _enumeratorLocal;
        private readonly MethodInfo _enumeratorMoveNext;
        private readonly Type _enumeratorType;
        private readonly ILGenerator _il;
        private readonly TState _buildState;
        protected readonly ITypeManipulator _types;
        private readonly IFieldActionProvider _actionProvider;
    }

    public delegate void OnValueReady(LocalBuilder enumeratorCurrentValue,
                                      Type itemType,
                                      FieldAction fieldAction);

    public delegate void OnIndexedValueReady(LocalBuilder currentValue,
                                             LocalBuilder currentIndex,
                                      Type itemType,
                                      FieldAction fieldAction);

}

#endif
