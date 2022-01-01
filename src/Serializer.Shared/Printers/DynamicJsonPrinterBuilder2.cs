#if GENERATECODE

using System;
using System.Reflection;
using System.Reflection.Emit;
using Das.Printers;

namespace Das.Serializer.Printers
{
    public class DynamicJsonPrinterBuilder2 : DynamicPrinterBuilderBase2<String>
    {
        public DynamicJsonPrinterBuilder2(ITypeInferrer typeInferrer,
                                          INodeTypeProvider nodeTypes,
                                          ITypeManipulator typeManipulator,
                                          ModuleBuilder moduleBuilder)
            : base(typeInferrer, nodeTypes, typeManipulator,
                moduleBuilder)
        {

        }

        static DynamicJsonPrinterBuilder2()
        {
            _printCharMethod = typeof(ITextRemunerable).GetMethod(nameof(ITextRemunerable.Append),
                new[] {typeof(Char)})!;

            _printStringMethod = typeof(IStringRemunerable).GetMethod(nameof(IStringRemunerable.Append),
                new[] {typeof(String)})!;

            _appendEscaped = typeof(JsonPrinter).GetMethod(nameof(JsonPrinter.AppendEscaped),
                BindingFlags.Static | BindingFlags.Public)!;

            _printDateTime = typeof(IStringRemunerable).GetMethod(nameof(IStringRemunerable.Append),
                new[] {typeof(DateTime)})!;
        }

        protected override String PrintProperty(IPropertyAccessor prop,
                                                ILGenerator il,
                                                Int32 index,
                                                ISerializerSettings settings,
                                                FieldInfo invariantCulture,
                                                String prepend,
                                                Type tWriter)
        {

            var nodeType = _nodeTypes.GetNodeType(prop.PropertyType);

            var isCheckCanPrint = index > 0 && settings.IsOmitDefaultValues;
            Label afterPrint = default;

            if (isCheckCanPrint)
            {
                afterPrint = il.DefineLabel();

                GetPropertyValue(prop, il);

                if (prop.PropertyType.IsPrimitive)
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                }
                else
                {
                    il.Emit(OpCodes.Ldnull);
                }

                il.Emit(OpCodes.Beq, afterPrint);
            }

            PrintPropertyNameEx(prop, nodeType, index, il,
                settings, prepend, tWriter);


            var res = PrintPropertyValue(prop, il, settings,
                invariantCulture, nodeType, tWriter);

            if (isCheckCanPrint)
                il.MarkLabel(afterPrint);

            return res;
        }

        private void PrintPropertyNameEx(IPropertyAccessor prop,
                                         NodeTypes nodeType,
                                         Int32 index,
                                         ILGenerator il,
                                         ISerializerSettings settings,
                                         String prepend,
                                         Type tWriter)
        {
            var propName2 = prepend;
            if (index > 0)
                propName2 += ",";
            else
                propName2 += "{";

            propName2 += "\"";

            switch (settings.PrintPropertyNameFormat)
            {
                case PropertyNameFormat.Default:
                    propName2 += prop.PropertyInfo.Name;
                    break;

                case PropertyNameFormat.PascalCase:
                    propName2 += _typeInferrer.ToPascalCase(prop.PropertyInfo.Name);
                    break;

                case PropertyNameFormat.CamelCase:
                    propName2 += _typeInferrer.ToCamelCase(prop.PropertyInfo.Name);
                    break;

                case PropertyNameFormat.SnakeCase:
                    propName2 += _typeInferrer.ToSnakeCase(prop.PropertyInfo.Name);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            propName2 += "\":";

            if (IsValueInQuotes(nodeType, prop.PropertyType))
                propName2 += "\"";

            PrintString(propName2, il, tWriter);
        }



        private static Boolean IsValueInQuotes(NodeTypes nodeType,
                                               Type valuesType)
        {
            if (nodeType == NodeTypes.Fallback)
                return true;

            if (nodeType != NodeTypes.Primitive)
                return false;

            var code = Type.GetTypeCode(valuesType);
            return code == TypeCode.DateTime || code == TypeCode.String ||
                   valuesType.IsEnum;
        }

        protected override String PrintPrimitive(Type primitiveType,
                                                 ILGenerator il,
                                                 ISerializerSettings settings,
                                                 Action<ILGenerator> loadPrimitiveValue,
                                                 FieldInfo invariantCulture,
                                                 Type tWriter)
        {
            String res;
            var code = Type.GetTypeCode(primitiveType);


            switch (code)
            {
                case TypeCode.Boolean:

                    var ifFalse = il.DefineLabel();
                    var afterPrint = il.DefineLabel();

                    il.Emit(OpCodes.Ldarga, 2);

                    loadPrimitiveValue(il);
                    il.Emit(OpCodes.Brfalse, ifFalse);

                    il.Emit(OpCodes.Ldstr, "true");

                    il.Emit(OpCodes.Constrained, tWriter);
                    il.Emit(OpCodes.Callvirt, _printStringMethod);

                    il.Emit(OpCodes.Br, afterPrint);

                    il.MarkLabel(ifFalse);
                    il.Emit(OpCodes.Ldstr, "false");

                    il.Emit(OpCodes.Constrained, tWriter);
                    il.Emit(OpCodes.Callvirt, _printStringMethod);

                    il.MarkLabel(afterPrint);

                    res = string.Empty;

                    break;

                case TypeCode.Char:

                    il.Emit(OpCodes.Ldarga, 2);
                    loadPrimitiveValue(il);

                    il.Emit(OpCodes.Constrained, tWriter);
                    il.Emit(OpCodes.Callvirt, _printCharMethod);

                    res = string.Empty;
                    break;

                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:

                    res = PrintValueType(primitiveType, il,
                        primitiveType.GetMethod(nameof(ToString), Type.EmptyTypes)!,
                        loadPrimitiveValue, tWriter);
                    break;

                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:

                    var realValTmp = il.DeclareLocal(primitiveType);
                    il.Emit(OpCodes.Ldarga, 2);

                    loadPrimitiveValue(il);
                    il.Emit(OpCodes.Stloc, realValTmp);
                    il.Emit(OpCodes.Ldloca, realValTmp);

                    il.Emit(OpCodes.Ldsfld, invariantCulture);
                    il.Emit(OpCodes.Callvirt,
                        primitiveType.GetMethod(nameof(ToString), new[] {typeof(IFormatProvider)})!);

                    il.Emit(OpCodes.Constrained, tWriter);
                    il.Emit(OpCodes.Callvirt, _printStringMethod);
                    res = string.Empty;

                    break;

                case TypeCode.DateTime:

                   il.Emit(OpCodes.Ldarga, 2);

                    loadPrimitiveValue(il);

                    il.Emit(OpCodes.Constrained, tWriter);
                    il.Emit(OpCodes.Callvirt, _printDateTime);


                   res = "\"";

                    break;

                case TypeCode.String:

                    il.Emit(OpCodes.Ldarg_2);

                    loadPrimitiveValue(il);

                    var gAppendEscaped = _appendEscaped.MakeGenericMethod(tWriter);

                    il.Emit(OpCodes.Call, gAppendEscaped);

                    res = "\"";

                    break;

                default:

                    if (_typeInferrer.TryGetNullableType(primitiveType, out var baseType))
                    {
                        res = PrintNullableValueType(primitiveType, baseType, il,
                            loadPrimitiveValue,
                            settings, invariantCulture, tWriter);
                        break;
                    }


                    var ifNull = il.DefineLabel();
                    var eof = il.DefineLabel();

                    var valTmp = il.DeclareLocal(primitiveType);

                    il.Emit(OpCodes.Ldarga, 2);
                    loadPrimitiveValue(il);

                    il.Emit(OpCodes.Stloc, valTmp);

                    var canBeNull = true;

                    if (primitiveType.IsValueType)
                    {
                        //don't check for null
                        canBeNull = false;
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldloc, valTmp);
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Brtrue, ifNull);
                    }

                    //not null

                    il.Emit(OpCodes.Ldloca, valTmp);

                    il.Emit(OpCodes.Callvirt,
                        primitiveType.GetMethod(nameof(ToString), Type.EmptyTypes)!);


                    if (canBeNull)
                    {
                        il.Emit(OpCodes.Br, eof);
                        il.MarkLabel(ifNull);
                        il.Emit(OpCodes.Ldstr, "null");
                        il.MarkLabel(eof);
                    }

                    il.Emit(OpCodes.Constrained, tWriter);
                    il.Emit(OpCodes.Callvirt, _printStringMethod);

                    res = canBeNull ? String.Empty : "\"";
                    break;

            }

            return res;
        }

        protected override String PrintFallback(IPropertyAccessor prop,
                                                ILGenerator il,
                                                ISerializerSettings settings,
                                                Action<ILGenerator> loadFallbackValue,
                                                FieldInfo invariantCulture,
                                                Type tWriter)
            => PrintPrimitive(prop.PropertyType, il, settings,
                loadFallbackValue, invariantCulture, tWriter);

        protected override void OpenObject(Type type,
                                           ILGenerator il,
                                           ISerializerSettings settings,
                                           Type tWriter)
        {
            PrintChar('{', il, tWriter);
        }

        private static void PrintChar(Char c,
                                      ILGenerator il,
                                      Type tWriter)
        {
            il.Emit(OpCodes.Ldarga, 2);

            il.Emit(OpCodes.Ldc_I4, c);
            il.Emit(OpCodes.Constrained, tWriter);
            il.Emit(OpCodes.Callvirt, _printCharMethod);
        }

        private static void PrintString(String str,
                                        ILGenerator il,
                                        Type tWriter)
        {
            il.Emit(OpCodes.Ldarga, 2);

            il.Emit(OpCodes.Ldstr, str);
            il.Emit(OpCodes.Constrained, tWriter);
            il.Emit(OpCodes.Callvirt, _printStringMethod);
        }

        protected override void CloseObject(ILGenerator il,
                                            ISerializerSettings settings,
                                            Type tWriter,
                                            String prepend)
        {
            if (prepend.Length > 0)
                PrintString(prepend + "}", il, tWriter);
            else
                PrintChar('}', il, tWriter);
        }


        private static String PrintValueType(Type primitiveType,
                                             ILGenerator il,
                                             MethodInfo toString,
                                             Action<ILGenerator> loadPrimitiveValue,
                                             Type tWriter)
        {
            var propValTmp = il.DeclareLocal(primitiveType);

            il.Emit(OpCodes.Ldarga, 2);
            loadPrimitiveValue(il);

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

            il.Emit(OpCodes.Constrained, tWriter);
            il.Emit(OpCodes.Callvirt, _printStringMethod);

            return primitiveType.IsEnum ? "\"" : string.Empty;
        }


        private String PrintNullableValueType(Type primitiveType,
                                              Type baseType,
                                              ILGenerator il,
                                              Action<ILGenerator> loadPrimitiveValue,
                                              ISerializerSettings settings,
                                              FieldInfo invariantCulture,
                                              Type tWriter)
        {
            var tmpVal = il.DeclareLocal(primitiveType);

            var hasValue = primitiveType.GetProperty(
                nameof(Nullable<Int32>.HasValue))!.GetGetMethod();

            var ifNull = il.DefineLabel();
            var eof = il.DefineLabel();

            loadPrimitiveValue(il);
            il.Emit(OpCodes.Stloc, tmpVal);
            il.Emit(OpCodes.Ldloca, tmpVal);

            il.Emit(OpCodes.Call, hasValue);
            il.Emit(OpCodes.Brfalse, ifNull);

            //not null

            PrintChar('"', il, tWriter);

            PrintPrimitive(baseType, il, settings,
                g => GetNullableLocalValue(g, tmpVal), invariantCulture, tWriter);

            PrintChar('"', il, tWriter);


            il.Emit(OpCodes.Br, eof);

            il.MarkLabel(ifNull);

            //null
            il.Emit(OpCodes.Ldarga, 2);
            il.Emit(OpCodes.Ldstr, "null");
            il.Emit(OpCodes.Constrained, tWriter);
            il.Emit(OpCodes.Callvirt, _printStringMethod);

            il.MarkLabel(eof);

            return string.Empty;
        }


        private static void GetNullableLocalValue(ILGenerator il,
                                                  LocalBuilder tmpVal)
        {

            var getValue = tmpVal.LocalType!.GetProperty(
                nameof(Nullable<Int32>.Value))!.GetGetMethod();

            il.Emit(OpCodes.Ldloca, tmpVal);
            il.Emit(OpCodes.Call, getValue);
        }

        private static readonly MethodInfo _printCharMethod;
        private static readonly MethodInfo _printStringMethod;
        private static readonly MethodInfo _appendEscaped;
        private static readonly MethodInfo _printDateTime;

    }
}


#endif