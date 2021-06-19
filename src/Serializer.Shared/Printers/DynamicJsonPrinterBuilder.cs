#if GENERATECODE

using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Das.Printers;

namespace Das.Serializer.Printers
{
    public class DynamicJsonPrinterBuilder : DynamicPrinterBuilderBase
    {
        public DynamicJsonPrinterBuilder(ITypeInferrer typeInferrer,
                                         INodeTypeProvider nodeTypes,
                                         IObjectManipulator objectManipulator,
                                         ITypeManipulator typeManipulator,
                                         ModuleBuilder moduleBuilder)
            : base(typeInferrer, nodeTypes, objectManipulator, typeManipulator, moduleBuilder)
        {
            _printCharMethod = typeof(ITextRemunerable).GetMethod(nameof(ITextRemunerable.Append),
                new[] {typeof(Char)})!;
        }

        protected override void PrintProperty(IPropertyAccessor prop,
                                              ILGenerator il,
                                              Int32 index,
                                              ISerializerSettings settings,
                                              FieldInfo invariantCulture)
        {
            


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

            var propName = prop.PropertyPath;

            switch (settings.PrintPropertyNameFormat)
            {
                case PropertyNameFormat.Default:
                    break;

                case PropertyNameFormat.PascalCase:
                    propName = _typeInferrer.ToPascalCase(propName);
                    break;

                case PropertyNameFormat.CamelCase:
                    propName = _typeInferrer.ToCamelCase(propName);
                    break;

                case PropertyNameFormat.SnakeCase:
                    propName = _typeInferrer.ToSnakeCase(propName);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            propName = "\"" + propName + "\":";
            if (index > 0)
                propName = "," + propName;

            var printStringMethod = typeof(IStringRemunerable).GetMethod(nameof(IStringRemunerable.Append),
                new[] {typeof(String)});

            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldstr, propName);
            il.Emit(OpCodes.Callvirt, printStringMethod!);

            PrintPropertyValue(prop, il, settings, invariantCulture);


            if (isCheckCanPrint)
                il.MarkLabel(afterPrint);
        }

        protected override void PrintPrimitive(Type primitiveType,
                                               ILGenerator il,
                                               ISerializerSettings settings,
                                               Action<ILGenerator> loadPrimitiveValue,
                                               FieldInfo invariantCulture)
        {
            var printStringMethod = typeof(IStringRemunerable).GetMethod(nameof(IStringRemunerable.Append),
                new[] {typeof(String)})!;

            var getInvariant = typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture), 
                BindingFlags.Static | BindingFlags.Public)!.GetMethod;

            var toStringFormatted = primitiveType.GetMethod(nameof(ToString), 
                new[] { typeof(IFormatProvider) });

            MethodInfo toString = primitiveType.GetMethod(nameof(ToString),
                Type.EmptyTypes)!;

            var code = Type.GetTypeCode(primitiveType);
            switch (code)
            {
                case TypeCode.Boolean:
                    
                    var ifFalse = il.DefineLabel();
                    var afterPrint = il.DefineLabel();

                    il.Emit(OpCodes.Ldarg_2);
                    //GetPropertyValue(prop, il);
                    loadPrimitiveValue(il);
                    il.Emit(OpCodes.Brfalse, ifFalse);

                    il.Emit(OpCodes.Ldstr, "true");
                    il.Emit(OpCodes.Callvirt, printStringMethod!);
                    il.Emit(OpCodes.Br, afterPrint);

                    il.MarkLabel(ifFalse);
                    il.Emit(OpCodes.Ldstr, "false");
                    il.Emit(OpCodes.Callvirt, printStringMethod!);

                    il.MarkLabel(afterPrint);

                    break;

                case TypeCode.Char:
                    il.Emit(OpCodes.Ldarg_2);
                    //GetPropertyValue(prop, il);
                    loadPrimitiveValue(il);
                    printStringMethod = typeof(ITextRemunerable).GetMethod(nameof(ITextRemunerable.Append),
                        new[] {typeof(Char)})!;
                    il.Emit(OpCodes.Callvirt, printStringMethod);
                    break;
                
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    PrintValueType(primitiveType, il, toString, printStringMethod, loadPrimitiveValue);
                    break;

                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    var realValTmp = il.DeclareLocal(primitiveType);

                    il.Emit(OpCodes.Ldarg_2);
                    //GetPropertyValue(prop, il);
                    loadPrimitiveValue(il);
                    il.Emit(OpCodes.Stloc, realValTmp);
                    il.Emit(OpCodes.Ldloca, realValTmp);

                    il.Emit(OpCodes.Call, getInvariant);
                    il.Emit(OpCodes.Callvirt, toStringFormatted!);

                    il.Emit(OpCodes.Callvirt, printStringMethod);

                    break;

                case TypeCode.DateTime:

                    PrintChar('"', il);

                    toString = typeof(DateTime).GetMethod(nameof(ToString), new Type[]
                        {typeof(String), typeof(CultureInfo)})!;

                    var dtTmp = il.DeclareLocal(typeof(DateTime));

                    il.Emit(OpCodes.Ldarg_2);

                    loadPrimitiveValue(il);
                    
                    il.Emit(OpCodes.Stloc, dtTmp);
                    il.Emit(OpCodes.Ldloca, dtTmp);
                    

                    il.Emit(OpCodes.Ldstr, "s");
                    il.Emit(OpCodes.Ldsfld, invariantCulture);

                    il.Emit(OpCodes.Call, toString);

                    
                    il.Emit(OpCodes.Callvirt, printStringMethod!);

                    PrintChar('"', il);

                    break;

                case TypeCode.String:

                    PrintChar('"', il);

                    il.Emit(OpCodes.Ldarg_2);
                    //GetPropertyValue(prop, il);
                    loadPrimitiveValue(il);

                    var appendEscaped = typeof(JsonPrinter).GetMethod(nameof(JsonPrinter.AppendEscaped),
                        BindingFlags.Static | BindingFlags.Public)!;
                        //, null, new[]
                        //    {typeof(ITextRemunerable), typeof(String)}, null)!;

                    var gAppendEscaped = appendEscaped.MakeGenericMethod(typeof(ITextRemunerable));

                    il.Emit(OpCodes.Call, gAppendEscaped);
                    
                    PrintChar('"', il);

                    break;

                default:
                    if (_typeInferrer.TryGetNullableType(primitiveType, out var baseType))
                    {
                        PrintNullableValueType(primitiveType, baseType, il, 
                            toString, printStringMethod, loadPrimitiveValue, 
                            settings, invariantCulture);
                        break;
                    }


                    var ifNull = il.DefineLabel();
                    var eof = il.DefineLabel();

                    var valTmp = il.DeclareLocal(primitiveType);

                    il.Emit(OpCodes.Ldarg_2);
                    loadPrimitiveValue(il);
                    //GetPropertyValue(prop, il);
                    il.Emit(OpCodes.Stloc, valTmp);

                    var canBeNull = true;

                    //null check
                    //if (_typeInferrer.TryGetNullableType(prop.PropertyType, out _))
                    //{
                    //    var hasValue = prop.PropertyType.GetProperty(
                    //        nameof(Nullable<Int32>.HasValue))!.GetMethod;

                    //    il.Emit(OpCodes.Ldloca, valTmp);
                    //    il.Emit(OpCodes.Call, hasValue);
                    //    il.Emit(OpCodes.Brfalse, ifNull);
                    //}
                    //else 
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
                    PrintChar('"', il);

                    il.Emit(OpCodes.Ldloca, valTmp);
                    il.Emit(OpCodes.Constrained, primitiveType);
                    il.Emit(OpCodes.Callvirt, toString);
                    

                    if (canBeNull)
                    {
                        il.Emit(OpCodes.Br, eof);
                        il.MarkLabel(ifNull);
                        il.Emit(OpCodes.Ldstr, "null");
                        il.MarkLabel(eof);
                    }

                    il.Emit(OpCodes.Callvirt, printStringMethod);

                    if (!canBeNull)
                        PrintChar('"', il);

                    break;
                    //throw new ArgumentOutOfRangeException();
            }
        }

        protected override void PrintFallback(IPropertyAccessor prop,
                                              ILGenerator il,
                                              ISerializerSettings settings,
                                              Action<ILGenerator> loadFallbackValue,
                                              FieldInfo invariantCulture)
            => PrintPrimitive(prop.PropertyType, il, settings, loadFallbackValue, invariantCulture);

        protected override void OpenObject(Type type,
                                           ILGenerator il,
                                           ISerializerSettings settings)
        {
            PrintChar('{', il);
        }

        private void PrintChar(Char c,
                               ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_2);

            il.Emit(OpCodes.Ldc_I4, c);
            il.Emit(OpCodes.Callvirt, _printCharMethod);
        }

        protected override void CloseObject(ILGenerator il,
                                            ISerializerSettings settings)
        {
            PrintChar('}', il);
        }


        private void PrintValueType(//IPropertyAccessor prop,
            Type primitiveType,
                                    ILGenerator il,
                                    MethodInfo toString,
                                    MethodInfo printStringMethod,
                                    Action<ILGenerator> loadPrimitiveValue)
        {
            if (primitiveType.IsEnum)
                PrintChar('"', il);

            var propValTmp = il.DeclareLocal(primitiveType);

            il.Emit(OpCodes.Ldarg_2);
            loadPrimitiveValue(il);
            //GetPropertyValue(prop, il);

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

            il.Emit(OpCodes.Callvirt, printStringMethod);

            if (primitiveType.IsEnum)
                PrintChar('"', il);
        }

        //private void PrintDateTime(ILGenerator il,
        //                           Action<ILGenerator> loadPrimitiveValue)
        //{
        //    PrintChar('"', il);
        //    todo
        //}

        private void PrintNullableValueType(//IPropertyAccessor prop,
            Type primitiveType,
            Type baseType,
                                            ILGenerator il,
                                            MethodInfo toString,
                                            MethodInfo printStringMethod,
            Action<ILGenerator> loadPrimitiveValue,
            ISerializerSettings settings,
            FieldInfo invariantCulture)
        {
            var tmpVal = il.DeclareLocal(primitiveType);

            var hasValue = primitiveType.GetProperty(
                nameof(Nullable<Int32>.HasValue))!.GetMethod;

            var getValue = primitiveType.GetProperty(
                nameof(Nullable<Int32>.Value))!.GetMethod;

            var ifNull = il.DefineLabel();
            var eof = il.DefineLabel();

            //GetPropertyValue(prop, il);
            loadPrimitiveValue(il);
            il.Emit(OpCodes.Stloc, tmpVal);
            il.Emit(OpCodes.Ldloca, tmpVal);
            
            il.Emit(OpCodes.Call, hasValue);
            il.Emit(OpCodes.Brfalse, ifNull);

            //not null

            PrintPrimitive(baseType, il, settings, 
                g => GetNullableLocalValue(g, tmpVal), invariantCulture);

            //PrintChar('"', il);
            //il.Emit(OpCodes.Ldarg_2);
            //il.Emit(OpCodes.Ldloca, tmpVal);
            //il.Emit(OpCodes.Callvirt, toString);
            //il.Emit(OpCodes.Callvirt, printStringMethod);
            //PrintChar('"', il);
            il.Emit(OpCodes.Br, eof);

            il.MarkLabel(ifNull);

            //null
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldstr, "null");
            il.Emit(OpCodes.Callvirt, printStringMethod);

            il.MarkLabel(eof);
        }

        private static void GetNullableLocalValue(ILGenerator il,
                                                  LocalBuilder tmpVal)
        {
            
            var getValue = tmpVal.LocalType.GetProperty(
                nameof(Nullable<Int32>.Value))!.GetMethod;

            il.Emit(OpCodes.Ldloca, tmpVal);
            il.Emit(OpCodes.Call, getValue);
        }

        private readonly MethodInfo _printCharMethod;
    }
}


#endif