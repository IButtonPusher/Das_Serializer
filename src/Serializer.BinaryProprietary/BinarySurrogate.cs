using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Serializer;
using Das.Serializer.Remunerators;
using Reflection.Common;

namespace Serializer.BinaryProprietary;

public static class BinarySurrogate<T>
{
   static BinarySurrogate()
   {
      var statMethods = typeof(T).GetMethods(BindingFlags.Static | BindingFlags.Public);

      foreach (var statMethod in statMethods)
      {
         if (ReferenceEquals(statMethod, null))
            continue;

         var smName = statMethod.Name;

         var statMethodArgs = statMethod.GetParameters();
         if (statMethodArgs.Length != 1)
            continue;

         var scanArgType = statMethodArgs[0].ParameterType;
         if (!IsPrimistring(scanArgType))
            continue;

         if (smName.StartsWith("From"))
         {
            var fromWhat = smName.Substring(4);
            var fromWhatProp = typeof(T).GetProperty(fromWhat, Const.PublicInstance);
            
            if (fromWhatProp != null &&
                fromWhatProp.PropertyType is { } fromPropType &&
                fromPropType == scanArgType)
            {
               ScanFunc = CreateScanFromMember(statMethod,  fromPropType);
               PrintFunc = CreatePrintFromMember(fromWhatProp, fromPropType);
               return;
            }

            var fromWhatField = typeof(T).GetField(fromWhat, Const.PublicInstance);
            if (fromWhatField != null && fromWhatField.FieldType is { } fromFieldType &&
                fromFieldType == scanArgType)
            {
               ScanFunc = CreateScanFromMember(statMethod,  fromFieldType);
               PrintFunc = CreatePrintFromMember(fromWhatField, fromFieldType);
               return;
            }

            var toMethod = typeof(T).GetMethod($"To{fromWhat}", Const.PublicInstance, null, CallingConventions.Any,
               Type.EmptyTypes, null);
            if (toMethod != null && toMethod.ReturnType == scanArgType &&
                toMethod.GetParameters().Length == 0)
            {
               ScanFunc = CreateScanFromMember(statMethod,  toMethod.ReturnType);
               PrintFunc = CreatePrintFromMember(toMethod,toMethod.ReturnType);
               return;
            }
         }

         if (smName == "Parse" && scanArgType == typeof(String))
         {
            ScanFunc = CreateScanFromMember(statMethod,  scanArgType);
            PrintFunc = CreatePrintFromMember(typeof(T).GetMethodOrDie(nameof(ToString)), scanArgType);
            return;
         }
      }
   }

   private static Boolean IsPrimistring(Type type) => type.IsPrimitive || type == typeof(String);

   private static Func<Byte[], IInstantiator, T> CreateScanFromMember(MethodInfo scanMethod,
                                                                      Type memberType)
   {
      var createPrimitiveMethod = typeof(IInstantiator).GetMethodOrDie(
                                                          nameof(IInstantiator.CreatePrimitiveObject), typeof(Byte[]))
                                                       .MakeGenericMethod(memberType);
      #if GENERATECODE

      var dynMeth = new DynamicMethod(string.Empty, typeof(T), new[]
      {
         typeof(Byte[]),
         typeof(IInstantiator)
      });
      var il = dynMeth.GetILGenerator();

      il.Emit(OpCodes.Ldarg_1);
      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Callvirt, createPrimitiveMethod);

      il.Emit(OpCodes.Call, scanMethod);

      il.Emit(OpCodes.Ret);

      var deleme = (Func<Byte[], IInstantiator, T>)dynMeth.CreateDelegate(typeof(Func<Byte[], IInstantiator, T>));
      return deleme;


      #else
       return ScanFromMemberThing;

      T ScanFromMemberThing(Byte[] arr,
                                  IInstantiator instantiator)
      {
         var args = new Object[1];

         args[0] = arr;
         var primVal = createPrimitiveMethod!.Invoke(instantiator, args);

         args[0] = primVal;

         var res = scanMethod.Invoke(null, args);
         if (res is T good)
            return good;

         return default!;
      }

      #endif
   }

   private static Action<T, IBinaryPrimitivePrinter, IBinaryWriter> CreatePrintFromMember(MemberInfo valueMember,
         Type memberType)
   {
      var stype = typeof(T);

      //void PrintPrimitive<T>(T o,IBinaryWriter bWriter);
      var printPrimitiveMethod = typeof(IBinaryPrimitivePrinter).GetMethodOrDie(
                                                                   nameof(IBinaryPrimitivePrinter.PrintPrimitive))
                                                                .MakeGenericMethod(memberType);

      #if GENERATECODE

      

      var dynMeth = new DynamicMethod(string.Empty, typeof(void), new[]
      {
         stype,
         typeof(IBinaryPrimitivePrinter),
         typeof(IBinaryWriter)
      });
      var il = dynMeth.GetILGenerator();


      // IBinaryPrimitivePrinter
      il.Emit(OpCodes.Ldarg_1);


      {
         // T
         il.Emit(stype.IsValueType ? OpCodes.Ldarga : OpCodes.Ldarg, 0);

         // Prop or Field or Method()
         switch (valueMember)
         {
            case PropertyInfo prop:
               il.Emit(stype.IsValueType ? OpCodes.Call : OpCodes.Callvirt, prop.GetGetMethod());
               break;

            case FieldInfo fi:
               il.Emit(stype.IsValueType ? OpCodes.Ldflda : OpCodes.Ldfld, fi);

               break;

            case MethodInfo minfo:
               il.Emit(stype.IsValueType ? OpCodes.Call : OpCodes.Callvirt, minfo);
               break;
         }
         ////
      }
   
      //IBinaryWriter
      il.Emit(OpCodes.Ldarg_2);

      il.Emit(OpCodes.Callvirt, printPrimitiveMethod);

      il.Emit(OpCodes.Ret);

      var deleme = (Action<T, IBinaryPrimitivePrinter, IBinaryWriter>)
         dynMeth.CreateDelegate(typeof(Action<T, IBinaryPrimitivePrinter, IBinaryWriter>));
      return deleme;


      #else

      return PrintFromMemberThing;

         void PrintFromMemberThing(T val,
                                IBinaryPrimitivePrinter primWriter,
                                IBinaryWriter writer)
      {
         Object? memberValue;

         switch (valueMember)
         {
            case PropertyInfo prop:
               memberValue = prop.GetValue(val);
               break;

            case FieldInfo fi:
               memberValue = fi.GetValue(val);

               break;

            case MethodInfo minfo:
               memberValue = minfo.Invoke(val, Const.EmptyObjectList);
               
               break;

            default:
               throw new NotSupportedException(valueMember.ToString());
         }

         printPrimitiveMethod!.Invoke(primWriter, new Object?[] { memberValue, writer });
      }

      #endif

   }

   public static readonly Func<Byte[], IInstantiator, T>? ScanFunc;
   

   public static readonly Action<T, IBinaryPrimitivePrinter, IBinaryWriter>? PrintFunc;

   //public static readonly PrintDelegate<TWriter>? PrintFunc2;

   public delegate void PrintDelegate<in TWriter>(T value,
                                                  TWriter writer) 
      where TWriter : IBinaryWriter<TWriter>;

}


