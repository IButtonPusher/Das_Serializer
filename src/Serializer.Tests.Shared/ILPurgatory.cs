//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace UnitTestProject1
//{
//    class ILPurgatory
//    {
//        private readonly SortedList<String, VoidMethod> _setters;

//        public ILPurgatory()
//        {
//            _setters = new SortedList<string, VoidMethod>(cmp);
//        }

//        public VoidMethod CreatePropertySetter(PropertyInfo propertyInfo)
//        {
//            var setMethod = propertyInfo.GetSetMethod();
//            if (setMethod == null)
//                return null;

//            var set = CreateMethodCaller(setMethod, true);


//            var sp = _types.CreatePropertySetter(pi);
//            if (sp != null)
//            {
//                reallyWrite = true;
//                _setters.Add(pi.Name, sp);

//                return (VoidMethod)set.CreateDelegate(typeof(VoidMethod));

//                if (_setters.TryGetValue(propName, out var del))
//                {
//                    del(targetObj, propVal);
//                }

//        //VoidMethod CreatePropertySetter(PropertyInfo propertyInfo);

//#else
//generator.DeclareLocal(memberInfo.DeclaringType.MakeByRefType());
//generator.Emit(OpCodes.Unbox, memberInfo.DeclaringType);
//generator.Emit(OpCodes.Stloc_0);
//generator.Emit(OpCodes.Ldloc_0);
//#endif // UNSAFE_IL


//if (decType.IsValueType)
//{
//#if DEBUG
//                generator.Emit(OpCodes.Ldarg_0);
//                generator.Emit(OpCodes.Ldloc_0);
//                generator.Emit(OpCodes.Ldobj, memberInfo.DeclaringType);
//                generator.Emit(OpCodes.Box, memberInfo.DeclaringType);
//                generator.Emit(OpCodes.Stind_Ref);
//#endif // UNSAFE_IL
//}


//    }
//}

//https://devblogs.microsoft.com/premier-developer/dissecting-new-generics-constraints-in-c-7-3/
//using System;
//using System.Reflection.Emit;
//
//public static class EnumConverter
//{
//    public static Func<T, long> CreateConvertToLong<T>() where T : struct, Enum
//    {
//        var method = new DynamicMethod(
//            name: "ConvertToLong",
//            returnType: typeof(long),
//            parameterTypes: new[] { typeof(T) },
//            m: typeof(EnumConverter).Module,
//            skipVisibility: true);
//
//        ILGenerator ilGen = method.GetILGenerator();
//
//        ilGen.Emit(OpCodes.Ldarg_0);
//        ilGen.Emit(OpCodes.Conv_I8);
//        ilGen.Emit(OpCodes.Ret);
//        return (Func<T, long>)method.CreateDelegate(typeof(Func<T, long>));
//    }
//}

using System;
using System.Threading.Tasks;