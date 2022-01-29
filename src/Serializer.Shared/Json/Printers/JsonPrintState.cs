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

            _appendEscaped = typeof(JsonPrinter).GetMethod(nameof(JsonPrinter.AppendEscaped),
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

        public void PrintChildObjectField(Action<ILGenerator> loadObject,
                                     Type fieldType)
        {
            PrintCurrentFieldHeader();

        PrintChildObjectValue(loadObject, fieldType);

            //var proxy = GetProxy(fieldType);
            //var proxyField = proxy.ProxyField;

            //_il.Emit(OpCodes.Ldarg_0);
            //_il.Emit(OpCodes.Ldfld, proxyField);
            //loadObject(_il);
            //_il.Emit(OpCodes.Ldarga, 2);

            //_il.Emit(OpCodes.Call, proxy.PrintMethod);

            //AppendChar('}');
        }

        private void PrintChildObjectValue(Action<ILGenerator> loadObject,
                                           Type fieldType)
        {
            var proxy = GetProxy(fieldType);
            var proxyField = proxy.ProxyField;

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, proxyField);
            loadObject(_il);
            
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

        //public void PrintPrimitive()
        //{
        //    var primitiveType = CurrentField.Type;

        //    switch (CurrentField.TypeCode)
        //    {
        //        case TypeCode.Single:
        //        case TypeCode.Double:
        //        case TypeCode.Decimal:

        //            var realValTmp = _il.DeclareLocal(primitiveType);
        //            _il.Emit(OpCodes.Ldarga, 2);

        //            //loadPrimitiveValue(il);
        //            LoadCurrentFieldValueToStack();
        //            _il.Emit(OpCodes.Stloc, realValTmp);
        //            _il.Emit(OpCodes.Ldloca, realValTmp);

        //            _il.Emit(OpCodes.Ldsfld, _invariantCulture);
        //            _il.Emit(OpCodes.Callvirt,
        //                primitiveType.GetMethod(nameof(ToString), new[] { typeof(IFormatProvider) })!);

        //            _il.Emit(OpCodes.Constrained, _tWriter);
        //            _il.Emit(OpCodes.Callvirt, _writeString);

        //            break;

        //        case TypeCode.DateTime:

        //            _il.Emit(OpCodes.Ldarga, 2);

        //            LoadCurrentFieldValueToStack();

        //            _il.Emit(OpCodes.Constrained, _tWriter);
        //            _il.Emit(OpCodes.Callvirt, _printDateTime);


        //            break;

        //        default:

        //            if (_types.TryGetNullableType(primitiveType, out var baseType))
        //            {
        //                PrintNullableValueType(primitiveType, baseType, _il,
        //                    _ => LoadCurrentFieldValueToStack());
        //                break;
        //            }   


        //            var ifNull = _il.DefineLabel();
        //            var eof = _il.DefineLabel();

        //            var valTmp = _il.DeclareLocal(primitiveType);

        //            _il.Emit(OpCodes.Ldarga, 2);
        //            LoadCurrentFieldValueToStack();
        //            //loadPrimitiveValue(il);

        //            _il.Emit(OpCodes.Stloc, valTmp);

        //            var canBeNull = true;

        //            if (primitiveType.IsValueType)
        //            {
        //                //don't check for null
        //                canBeNull = false;
        //            }
        //            else
        //            {
        //                _il.Emit(OpCodes.Ldloc, valTmp);
        //                _il.Emit(OpCodes.Ldnull);
        //                _il.Emit(OpCodes.Ceq);
        //                _il.Emit(OpCodes.Brtrue, ifNull);
        //            }

        //            //not null

        //            _il.Emit(OpCodes.Ldloca, valTmp);

        //            _il.Emit(OpCodes.Callvirt,
        //                primitiveType.GetMethod(nameof(ToString), Type.EmptyTypes)!);


        //            if (canBeNull)
        //            {
        //                _il.Emit(OpCodes.Br, eof);
        //                _il.MarkLabel(ifNull);
        //                _il.Emit(OpCodes.Ldstr, "null");
        //                _il.MarkLabel(eof);
        //            }

        //            _il.Emit(OpCodes.Constrained, _tWriter);
        //            _il.Emit(OpCodes.Callvirt, _writeString);

        //            break;
        //    }
        //}

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

            //PrintChar('"');

            _il.Emit(OpCodes.Ldarga, 2);

            //var conv = TypeDescriptor.GetConverter(CurrentField.Type);
            //var convType = conv.GetType();

            //var emptyCtor = convType.GetConstructor(Type.EmptyTypes);
            //_il.Emit(OpCodes.Newobj, emptyCtor!);

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

            //_il.Emit(OpCodes.Stloc, str);


            //_il.Emit(OpCodes.Ldarga, 2);
            //_il.Emit(OpCodes.Ldloc, str);

            _il.Emit(OpCodes.Constrained, _tWriter);
            _il.Emit(OpCodes.Callvirt, _writeString);

            PrintChar('"');
        }

        public void PrintEnum()
        {
            //_il.Emit(OpCodes.Ldarga, 2);

            //LoadCurrentFieldValueToStack();
            //_il.Emit(OpCodes.Conv_I4);

            //_il.Emit(OpCodes.Constrained, _tWriter);

            //_il.Emit(OpCodes.Call, _writeInt32);


            ////////////////////////////////////

            //var fld = CurrentField;
            //var fldType = fld.Type;

            //var propValTmp = GetLocal(fldType);


            _il.Emit(OpCodes.Ldarga, 2);
            LoadCurrentFieldValueToStack();

            //_il.Emit(OpCodes.Stloc, propValTmp);
            //_il.Emit(OpCodes.Ldloca, propValTmp);

            //var toString = fldType.GetMethodOrDie(nameof(ToString), Type.EmptyTypes);

            //_il.Emit(OpCodes.Constrained, fldType);
            //_il.Emit(OpCodes.Callvirt, toString);


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
                                               Action<IDynamicState, TData> loadValue)
        {
            _il.Emit(OpCodes.Ldarga, 2);

            loadValue(this, data);

            _il.Emit(OpCodes.Constrained, _tWriter);
        }

        public void PrintStringField()
        {
            PrintCurrentFieldHeader();

            //PrintChar('"');

            _il.Emit(OpCodes.Ldarga, 2);

            LoadCurrentFieldValueToStack();

            _il.Emit(OpCodes.Constrained, _tWriter);
            _il.Emit(OpCodes.Callvirt, _writeString);

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



        //public void AppendValue<TData>(TData data,
        //                               Action<IDynamicState, TData> loadValue)
        //{
        //    TODO_IMPLEMENT_ME();
        //}

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


        //protected String PrintPrimitive(Type primitiveType,
        //                                ILGenerator il,
        //                                //ISerializerSettings settings,
        //                                Action<ILGenerator> loadPrimitiveValue)
        //    //FieldInfo invariantCulture,
        //    //Type tWriter)
        //{
        //    String res;
        //    var code = Type.GetTypeCode(primitiveType);


        //    switch (code)
        //    {
        //        case TypeCode.Boolean:

        //            var ifFalse = il.DefineLabel();
        //            var afterPrint = il.DefineLabel();

        //            il.Emit(OpCodes.Ldarga, 2);

        //            loadPrimitiveValue(il);
        //            il.Emit(OpCodes.Brfalse, ifFalse);

        //            il.Emit(OpCodes.Ldstr, "true");

        //            il.Emit(OpCodes.Constrained, _tWriter);
        //            il.Emit(OpCodes.Callvirt, _writeString);

        //            il.Emit(OpCodes.Br, afterPrint);

        //            il.MarkLabel(ifFalse);
        //            il.Emit(OpCodes.Ldstr, "false");

        //            il.Emit(OpCodes.Constrained, _tWriter);
        //            il.Emit(OpCodes.Callvirt, _writeString);

        //            il.MarkLabel(afterPrint);

        //            res = string.Empty;

        //            break;

        //        case TypeCode.Char:

        //            il.Emit(OpCodes.Ldarga, 2);
        //            loadPrimitiveValue(il);

        //            il.Emit(OpCodes.Constrained, _tWriter);
        //            il.Emit(OpCodes.Callvirt, _writeChar);

        //            res = string.Empty;
        //            break;

        //        case TypeCode.SByte:
        //        case TypeCode.Byte:
        //        case TypeCode.Int16:
        //        case TypeCode.UInt16:
        //        case TypeCode.Int32:
        //        case TypeCode.UInt32:
        //        case TypeCode.Int64:
        //        case TypeCode.UInt64:

        //            res = PrintValueType(primitiveType, il,
        //                primitiveType.GetMethod(nameof(ToString), Type.EmptyTypes)!,
        //                () => loadPrimitiveValue(il));
        //            break;

        //        case TypeCode.Single:
        //        case TypeCode.Double:
        //        case TypeCode.Decimal:

        //            var realValTmp = il.DeclareLocal(primitiveType);
        //            il.Emit(OpCodes.Ldarga, 2);

        //            loadPrimitiveValue(il);
        //            il.Emit(OpCodes.Stloc, realValTmp);
        //            il.Emit(OpCodes.Ldloca, realValTmp);

        //            il.Emit(OpCodes.Ldsfld, _invariantCulture);
        //            il.Emit(OpCodes.Callvirt,
        //                primitiveType.GetMethod(nameof(ToString), new[] { typeof(IFormatProvider) })!);

        //            il.Emit(OpCodes.Constrained, _tWriter);
        //            il.Emit(OpCodes.Callvirt, _writeString);
        //            res = string.Empty;

        //            break;

        //        case TypeCode.DateTime:

        //            il.Emit(OpCodes.Ldarga, 2);

        //            loadPrimitiveValue(il);

        //            il.Emit(OpCodes.Constrained, _tWriter);
        //            il.Emit(OpCodes.Callvirt, _printDateTime);


        //            res = "\"";

        //            break;

        //        case TypeCode.String:

        //            il.Emit(OpCodes.Ldarg_2);

        //            loadPrimitiveValue(il);

        //            var gAppendEscaped = _appendEscaped.MakeGenericMethod(_tWriter);

        //            il.Emit(OpCodes.Call, gAppendEscaped);

        //            res = "\"";

        //            break;

        //        default:

        //            if (_types.TryGetNullableType(primitiveType, out var baseType))
        //            {
        //                res = PrintNullableValueType(primitiveType, baseType, il,
        //                    loadPrimitiveValue);
        //                break;
        //            }


        //            var ifNull = il.DefineLabel();
        //            var eof = il.DefineLabel();

        //            var valTmp = il.DeclareLocal(primitiveType);

        //            il.Emit(OpCodes.Ldarga, 2);
        //            loadPrimitiveValue(il);

        //            il.Emit(OpCodes.Stloc, valTmp);

        //            var canBeNull = true;

        //            if (primitiveType.IsValueType)
        //            {
        //                //don't check for null
        //                canBeNull = false;
        //            }
        //            else
        //            {
        //                il.Emit(OpCodes.Ldloc, valTmp);
        //                il.Emit(OpCodes.Ldnull);
        //                il.Emit(OpCodes.Ceq);
        //                il.Emit(OpCodes.Brtrue, ifNull);
        //            }

        //            //not null

        //            il.Emit(OpCodes.Ldloca, valTmp);

        //            il.Emit(OpCodes.Callvirt,
        //                primitiveType.GetMethod(nameof(ToString), Type.EmptyTypes)!);


        //            if (canBeNull)
        //            {
        //                il.Emit(OpCodes.Br, eof);
        //                il.MarkLabel(ifNull);
        //                il.Emit(OpCodes.Ldstr, "null");
        //                il.MarkLabel(eof);
        //            }

        //            il.Emit(OpCodes.Constrained, _tWriter);
        //            il.Emit(OpCodes.Callvirt, _writeString);

        //            res = canBeNull ? String.Empty : "\"";
        //            break;
        //    }

        //    return res;
        //}

        //private void PrintString(Action loadValue)
        //{
        //    PrintCurrentFieldHeader();

        //    PrintChar('"');

        //    _il.Emit(OpCodes.Ldarga, 2);

        //    loadValue();

        //    _il.Emit(OpCodes.Constrained, _tWriter);
        //    _il.Emit(OpCodes.Callvirt, _writeString);

        //    PrintChar('"');
        //}

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
                PrintChildObjectValue(il => il.Emit(OpCodes.Ldloc, enumeratorCurrentValue), itemType);
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

        //private String PrintNullableValueType(Type primitiveType,
        //                                      Type baseType,
        //                                      ILGenerator il,
        //                                      Action<ILGenerator> loadPrimitiveValue)
        //    //ISerializerSettings settings,
        //    //FieldInfo invariantCulture,
        //    //Type tWriter)
        //{
        //    var tmpVal = il.DeclareLocal(primitiveType);

        //    var hasValue = primitiveType.GetProperty(
        //        nameof(Nullable<Int32>.HasValue))!.GetGetMethod();

        //    var ifNull = il.DefineLabel();
        //    var eof = il.DefineLabel();

        //    loadPrimitiveValue(il);
        //    il.Emit(OpCodes.Stloc, tmpVal);
        //    il.Emit(OpCodes.Ldloca, tmpVal);

        //    il.Emit(OpCodes.Call, hasValue);
        //    il.Emit(OpCodes.Brfalse, ifNull);

        //    //not null

        //    PrintChar('"');

        //    PrintPrimitive(baseType, il, //settings,
        //        g => GetNullableLocalValue(g, tmpVal)); //, invariantCulture, tWriter);

        //    PrintChar('"');

        //    il.Emit(OpCodes.Br, eof);

        //    il.MarkLabel(ifNull);

        //    //null
        //    il.Emit(OpCodes.Ldarga, 2);
        //    il.Emit(OpCodes.Ldstr, "null");
        //    il.Emit(OpCodes.Constrained, _tWriter);
        //    il.Emit(OpCodes.Callvirt, _writeString);

        //    il.MarkLabel(eof);

        //    return string.Empty;
        //}

        //private static void GetNullableLocalValue(ILGenerator il,
        //                                          LocalBuilder tmpVal)
        //{
        //    var getValue = tmpVal.LocalType!.GetProperty(
        //        nameof(Nullable<Int32>.Value))!.GetGetMethod();

        //    il.Emit(OpCodes.Ldloca, tmpVal);
        //    il.Emit(OpCodes.Call, getValue);
        //}

        private static readonly MethodInfo _writeChar;
        private static readonly MethodInfo _writeString;

        private static readonly MethodInfo _appendEscaped;
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
