#if GENERATECODE

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Printers;
using Das.Serializer.CodeGen;
using Das.Serializer.Properties;
using Das.Serializer.State;
using Reflection.Common;

namespace Das.Serializer.Json.Printers
{
    public class JsonPrintState : JsonStateBase,
                                  IDynamicPrintState<PropertyActor, String>
    {
        static JsonPrintState()
        {
            var writer = typeof(ITextRemunerable);
            var iWriter = typeof(IIntRemunerable);

            _writeChar = writer.GetMethodOrDie<Char>(nameof(ITextRemunerable.Append));

            _writeString = typeof(IStringRemunerable).GetMethodOrDie<String>(
                nameof(IStringRemunerable.Append));

            _writeStringEscaped = typeof(JsonPrinter).GetMethod(nameof(JsonPrinter.AppendEscaped),
                BindingFlags.Static | BindingFlags.Public)!;

            _printDateTime = typeof(IStringRemunerable).GetMethod(nameof(IStringRemunerable.Append),
                new[] { typeof(DateTime) })!;


            _writeBoolean = writer.GetMethodOrDie<Boolean>(nameof(ITextRemunerable.Append));
            _writeInt8 = iWriter.GetMethodOrDie<Byte>(nameof(ITextRemunerable.Append));

            _writeInt16 = iWriter.GetMethodOrDie<Int16>(nameof(ITextRemunerable.Append));
            _writeUInt16 = iWriter.GetMethodOrDie<UInt16>(nameof(ITextRemunerable.Append));

            _writeInt32 = iWriter.GetMethodOrDie<Int32>(nameof(ITextRemunerable.Append));
            _writeUInt32 = iWriter.GetMethodOrDie<UInt32>(nameof(ITextRemunerable.Append));

            _writeInt64 = iWriter.GetMethodOrDie<Int64>(nameof(ITextRemunerable.Append));
            _writeUInt64 = iWriter.GetMethodOrDie<UInt64>(nameof(ITextRemunerable.Append));

            _writeSingle = iWriter.GetMethodOrDie<Single>(nameof(ITextRemunerable.Append));
            _writeDouble = iWriter.GetMethodOrDie<Double>(nameof(ITextRemunerable.Append));
            _writeDecimal = iWriter.GetMethodOrDie<Decimal>(nameof(ITextRemunerable.Append));
        }

        public JsonPrintState(Type type,
                              ILGenerator il,
                              ITypeManipulator types,
                              Action<ILGenerator>? loadCurrentValueOntoStack,
                              Type tWriter,
                              FieldInfo invariantCulture,
                              IDictionary<Type, ProxiedInstanceField> proxies,
                              IEnumerable<PropertyActor> properties,
                              ISerializerSettings settings,
                              ITypeInferrer typeInferrer,
                              IFieldActionProvider actionProvider,
                              Dictionary<PropertyActor, FieldInfo> converterFields)
            : base(type, il, types, loadCurrentValueOntoStack,
                invariantCulture, proxies, properties, settings, actionProvider)
        {
            _tWriter = tWriter;
            _typeInferrer = typeInferrer;
            _converterFields = converterFields;
        }
        
        public void PrintChildObjectField(Action loadObject,
                                          Type fieldType)
        {
            PrintCurrentFieldHeader();

            PrintChildObjectValue(loadObject, fieldType);
        }

        private void PrintChildObjectValue(Action loadObject,
                                           Type fieldType)
        {
            var proxy = GetProxy(fieldType);
            var proxyField = proxy.ProxyField;

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, proxyField);
            loadObject();
            
            _il.Emit(OpCodes.Ldarg, 2);
            _il.Emit(OpCodes.Call, proxy.PrintMethod);

            //AppendChar('}');
        }

        public void WriteInt32()
        {
            PrintValueType(CurrentField.Type, _il,
                CurrentField.Type.GetMethod(nameof(ToString), Type.EmptyTypes)!,
                LoadCurrentFieldValueToStack);
        }

        public void PrintVarIntField()
        {
            PrintCurrentFieldHeader();
            PrepareToPrintValue();

            PrintValueType(CurrentField.Type, _il,
                CurrentField.Type.GetMethod(nameof(ToString), Type.EmptyTypes)!,
                LoadCurrentFieldValueToStack);
        }

        public void AppendPrimitive<TData>(TData data,
                                           TypeCode typeCode,
                                           Action<IDynamicPrintState, TData> loadValue)
        {
            var printQuotes = typeCode is TypeCode.DateTime or TypeCode.String;

            if (printQuotes)
                AppendChar('"');

            _il.Emit(OpCodes.Ldarga, 2);
            loadValue(this, data);
            _il.Emit(OpCodes.Constrained, _tWriter);
            _actionProvider.AppendPrimitive(this, typeCode);

            if (printQuotes)
                AppendChar('"');
        }

        public void PrepareToPrintValue()
        {
            _il.Emit(OpCodes.Ldarga, 2);

            LoadCurrentFieldValueToStack();

            _il.Emit(OpCodes.Constrained, _tWriter);
        }

        public void PrintFallback()
        {
            var converter = _converterFields[_properties[_currentPropertyIndex]];

            PrintCurrentFieldHeader();

            _il.Emit(OpCodes.Ldarga, 2);

            var convType = converter.FieldType;

            var tostring = convType
                .GetMethodOrDie(nameof(TypeConverter.ConvertToInvariantString),
                    typeof(Object));

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, converter);

            
            LoadCurrentFieldValueToStack();

            if (CurrentField.Type.IsValueType)
                _il.Emit(OpCodes.Box, CurrentField.Type);

            _il.Emit(OpCodes.Call, tostring);

            _il.Emit(OpCodes.Constrained, _tWriter);
            _il.Emit(OpCodes.Callvirt, _writeString);

            PrintChar('"');
        }

        public void PrintEnum()
        {
            _il.Emit(OpCodes.Ldarga, 2);
            LoadCurrentFieldValueToStack();

            _il.Emit(OpCodes.Constrained, _tWriter);
            _il.Emit(OpCodes.Callvirt, _writeInt32);

            /////////////////////////////////

            //var fld = CurrentField;
            //var fldType = fld.Type;

            //var propValTmp = GetLocal(fldType);

            //PrintChar('"', _il, _tWriter);

            //_il.Emit(OpCodes.Ldarga, 2);
            //LoadCurrentFieldValueToStack();

            //_il.Emit(OpCodes.Stloc, propValTmp);
            //_il.Emit(OpCodes.Ldloca, propValTmp);

            //var toString = fldType.GetMethodOrDie(nameof(ToString), Type.EmptyTypes);

            //_il.Emit(OpCodes.Constrained, fldType);
            //_il.Emit(OpCodes.Callvirt, toString);


            //_il.Emit(OpCodes.Constrained, _tWriter);
            //_il.Emit(OpCodes.Callvirt, _printStringMethod);

            //PrintChar('"', _il, _tWriter);
        }

        public void PrepareToPrintValue<TData>(TData data,
                                               Action<IDynamicPrintState, TData> loadValue)
        {
            _il.Emit(OpCodes.Ldarga, 2);

            loadValue(this, data);

            _il.Emit(OpCodes.Constrained, _tWriter);
        }

        public void PrintStringField()
        {
            PrintCurrentFieldHeader();

            _il.Emit(OpCodes.Ldarg, 2);

            LoadCurrentFieldValueToStack();
            //_il.Emit(OpCodes.Ldarga, 2);

            //_il.Emit(OpCodes.Constrained, _tWriter);
            _il.Emit(OpCodes.Call, _writeStringEscaped);

            PrintChar('"');
        }

        public void PrintPrimitiveCollection()
        {
            PrintCollectionFieldImpl();
        }

        public void PrintObjectCollection()
        {
            PrintCollectionFieldImpl();
        }

        public void PrintIntCollection()
        {
            PrintArrayFieldImpl();
        }

        public void PrintByteArrayField()
        {
            PrintArrayFieldImpl();
        }

        public void PrintObjectArray()
        {
            PrintArrayFieldImpl();
        }

        public void PrintPrimitiveArray()
        {
            PrintArrayFieldImpl();
        }

        public void PrintDateTimeField()
        {
            PrintCurrentFieldHeader();

            //_il.Emit(OpCodes.Callvirt, _printDateTime);

            //PrintChar('"');

            _il.Emit(OpCodes.Ldarga, 2);

            LoadCurrentFieldValueToStack();

            _il.Emit(OpCodes.Constrained, _tWriter);
            AppendDateTime();
            //_il.Emit(OpCodes.Callvirt, _printDateTime);

            PrintChar('"');
        }


        public void PrintDictionary()
        {
            PrintCollectionFieldImpl();
        }

        /// <summary>
        ///     Property name and maybe a comma if it's not the first property
        /// </summary>
        public void PrintCurrentFieldHeader()
        {
            var prop = _properties[_currentPropertyIndex];
            var propNameStr = prop.Index > 0
                ? ","
                : "{";

            propNameStr += "\"";

            switch (_settings.PrintPropertyNameFormat)
            {
                case PropertyNameFormat.Default:
                    propNameStr += prop.Name;
                    break;

                case PropertyNameFormat.PascalCase:
                    propNameStr += _typeInferrer.ToPascalCase(prop.Name);
                    break;

                case PropertyNameFormat.CamelCase:
                    propNameStr += _typeInferrer.ToCamelCase(prop.Name);
                    break;

                case PropertyNameFormat.SnakeCase:
                    propNameStr += _typeInferrer.ToSnakeCase(prop.Name);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            propNameStr += "\":";

            switch (prop.FieldAction)
            {
                case FieldAction.ByteArray:
                case FieldAction.PackedArray:
                case FieldAction.ChildObjectCollection:
                case FieldAction.ChildObjectArray:
                case FieldAction.ChildPrimitiveCollection:
                case FieldAction.ChildPrimitiveArray:
                    case FieldAction.Dictionary:
                    propNameStr += "[";
                    break;

                case FieldAction.ChildObject:
                    propNameStr += "{";
                    break;

                case FieldAction.String:
                case FieldAction.FallbackSerializable:
                case FieldAction.DateTime:
                    propNameStr += "\"";
                    break;

            }

            PrintString(propNameStr);
        }

        
        public void AppendDateTime()
        {
            _il.Emit(OpCodes.Callvirt, _printDateTime);
        }

        public void AppendBoolean()
        {
            _il.Emit(OpCodes.Callvirt, _writeBoolean);
        }


        public void AppendInt8()
        {
            _il.Emit(OpCodes.Callvirt, _writeInt8);
        }

        public void AppendInt16()
        {
            _il.Emit(OpCodes.Callvirt, _writeInt16);
        }

        public void AppendUInt16()
        {
            _il.Emit(OpCodes.Callvirt, _writeUInt16);
        }

        public void AppendInt32()
        {
            _il.Emit(OpCodes.Callvirt, _writeInt32);
        }

        public void AppendUInt32()
        {
            _il.Emit(OpCodes.Callvirt, _writeUInt32);
        }

        public void AppendInt64()
        {
            _il.Emit(OpCodes.Callvirt, _writeInt64);
        }

        public void AppendUInt64()
        {
            _il.Emit(OpCodes.Callvirt, _writeUInt64);
        }

        public void AppendSingle()
        {
            _il.Emit(OpCodes.Callvirt, _writeSingle);
        }

        public void AppendDouble()
        {
            _il.Emit(OpCodes.Callvirt, _writeDouble);
        }

        public void AppendDecimal()
        {
            _il.Emit(OpCodes.Callvirt, _writeDecimal);
        }


        public void AppendNull()
        {
            _il.Emit(OpCodes.Ldarga, 2);

            _il.Emit(OpCodes.Ldstr, "null");
            _il.Emit(OpCodes.Constrained, _tWriter);
            _il.Emit(OpCodes.Callvirt, _writeString);
        }

        public void AppendChar()
        {
            _il.Emit(OpCodes.Callvirt, _writeChar);
        }

        public void AppendChar(Char c)
        {
            _il.Emit(OpCodes.Ldarga, 2);

            _il.Emit(OpCodes.Ldc_I4, Convert.ToInt32(c));
            _il.Emit(OpCodes.Constrained, _tWriter);
            _il.Emit(OpCodes.Callvirt, _writeChar);
        }

        IEnumerator<IDynamicPrintState<PropertyActor, string>> IEnumerable<IDynamicPrintState<PropertyActor, string>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<IDynamicPrintState<PropertyActor, string>> GetEnumerator()
        {
            for (var c = 0; c < _properties.Length; c++)
            {
                _currentPropertyIndex = c;
                //_currentField = Fields[c];
                yield return this;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        public void LoadWriter()
        {
            _il.Emit(OpCodes.Ldarga, 2);
        }

        private void PrintCollectionFieldImpl()
        {
            PrintCurrentFieldHeader();
            var ienum = new DynamicEnumerator<JsonPrintState>(this,
                CurrentField, _types, _actionProvider);

            ienum.ForEach(PrintForLoopCurrent);

            PrintChar(']');
        }

        private void PrintArrayFieldImpl()
        {
            PrintCurrentFieldHeader();
            var ienum = new DynamicEnumerator<JsonPrintState>(this,
                CurrentField, _types, _actionProvider);

            ienum.ForLoop(PrintForLoopCurrent);

            PrintChar(']');
        }

        private void PrintForLoopCurrent(LocalBuilder enumeratorCurrentValue,
                                         LocalBuilder currentIndex,
                                         Type itemType,
                                         FieldAction fieldAction)
        {
            var not0 = _il.DefineLabel();

            _il.Emit(OpCodes.Ldloc, currentIndex);
            _il.Emit(OpCodes.Brfalse, not0);

            PrintChar(',');

            _il.MarkLabel(not0);

            if (fieldAction == FieldAction.ChildObject)
                PrintChildObjectValue(() => _il.Emit(OpCodes.Ldloc, enumeratorCurrentValue), itemType);
            else
            {
                _il.Emit(OpCodes.Ldarga, 2);
                _il.Emit(OpCodes.Ldloc, enumeratorCurrentValue);
                _actionProvider.AppendPrimitive(this, CurrentField.TypeCode);
            }
        }

        private void PrintString(String str)
        {
            _il.Emit(OpCodes.Ldarga, 2);

            _il.Emit(OpCodes.Ldstr, str);
            _il.Emit(OpCodes.Constrained, _tWriter);
            _il.Emit(OpCodes.Callvirt, _writeString);
        }

        private String PrintValueType(Type primitiveType,
                                      ILGenerator il,
                                      MethodInfo toString,
                                      Action loadPrimitiveValue)
        {
            var propValTmp = GetLocal(primitiveType);

            il.Emit(OpCodes.Ldarga, 2);
            loadPrimitiveValue();

            il.Emit(OpCodes.Stloc, propValTmp);
            il.Emit(OpCodes.Ldloca, propValTmp);

            if (primitiveType.IsEnum)
            {
                il.Emit(OpCodes.Constrained, primitiveType);
                il.Emit(OpCodes.Callvirt, toString);
            }
            else
            {
                il.Emit(OpCodes.Call, toString);
            }

            il.Emit(OpCodes.Constrained, _tWriter);
            il.Emit(OpCodes.Callvirt, _writeString);

            return primitiveType.IsEnum ? "\"" : string.Empty;
        }

        private void PrintChar(Char c)
        {
            _il.Emit(OpCodes.Ldarga, 2);

            _il.Emit(OpCodes.Ldc_I4, c);
            _il.Emit(OpCodes.Constrained, _tWriter);
            _il.Emit(OpCodes.Callvirt, _writeChar);
        }

      

        private static readonly MethodInfo _writeChar;
        private static readonly MethodInfo _writeString;

        private static readonly MethodInfo _writeStringEscaped;
        private static readonly MethodInfo _printDateTime;

        private static readonly MethodInfo _writeBoolean;
        private static readonly MethodInfo _writeDecimal;
        private static readonly MethodInfo _writeDouble;

        private static readonly MethodInfo _writeInt16;

        private static readonly MethodInfo _writeInt32;

        private static readonly MethodInfo _writeInt64;
        private static readonly MethodInfo _writeInt8;

        private static readonly MethodInfo _writeSingle;
        private static readonly MethodInfo _writeUInt16;
        private static readonly MethodInfo _writeUInt32;
        private static readonly MethodInfo _writeUInt64;
        private readonly Type _tWriter;
        private readonly ITypeInferrer _typeInferrer;
        private readonly Dictionary<PropertyActor, FieldInfo> _converterFields;
    }
}


#endif
