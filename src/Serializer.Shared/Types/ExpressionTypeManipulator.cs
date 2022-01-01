// ReSharper disable RedundantUsingDirective

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer;


namespace Das.Serializer
{
    public partial class TypeManipulator
    {
        #if !GENERATECODE
        public Func<Object, Object> CreatePropertyGetter(Type targetType,
                                                                PropertyInfo propertyInfo)
        {
            return CreateExpressionPropertyGetter(targetType, propertyInfo);
        }

        public static Func<object, object> CreatePropertyGetter(Type targetType, 
                                                                  String propertyName,
                                                                  out PropertyInfo propInfo)
        {
            return CreateExpressionPropertyGetter(targetType, propertyName, out propInfo);
        }

 

       

        public PropertySetter CreateSetMethod(MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case PropertyInfo prop:
                    return CreateExpressionPropertySetter(prop);
         

                case FieldInfo field:
                    PropertySetter fieldSetter = (ref Object? target, Object? value) =>
                    {
                        field.SetValue(target, value);
                    };
                    return fieldSetter;
            }

            throw new NotImplementedException();
        }


        public PropertySetter? CreateSetMethod(Type targetType, 
                                               String memberName)
        {
            return CreateExpressionPropertySetter(targetType, memberName);
        }

        public Func<Object, Object> CreateFieldGetter(FieldInfo fieldInfo)
        {
            var input = Expression.Parameter(fieldInfo.DeclaringType 
                                             ?? throw new InvalidOperationException());
            return Expression.Lambda<Func<Object, Object>>(Expression.PropertyOrField(
                input, fieldInfo.Name), input).Compile();
        }

        public Action<Object, Object?> CreateFieldSetter(FieldInfo fieldInfo)
        {

            Action<Object, Object?> bob = fieldInfo.SetValue;
            return bob;
        }

        public static VoidMethod CreateMethodCaller(MethodInfo method)
        {
            VoidMethod bobbith = (target, paramValues) =>
            {
                method.Invoke(target, paramValues);
                //bob.DynamicInvoke(target, paramValues);
            };


            return bobbith;
        }

        //VoidMethod ITypeManipulator.CreateMethodCaller(MethodInfo method)
        //{
        //    return CreateMethodCaller(method);
        //    //VoidMethod bobbith = (target, paramValues) =>
        //    //{
        //    //    method.Invoke(target, paramValues);
        //    //    //bob.DynamicInvoke(target, paramValues);
        //    //};


        //    //return bobbith;
        //}


          public static Func<object, object> CreateExpressionPropertyGetter(Type targetType,
                                                                          PropertyInfo propertyInfo)
        {
            _singlePropFairy ??= new PropertyInfo[1];
            _singlePropFairy[0] = propertyInfo;
            return CreateExpressionPropertyGetterImpl(targetType, _singlePropFairy, out _);
        }

        private static Func<Object, Object> CreateExpressionPropertyGetterImpl(Type targetType,
                                                                               PropertyInfo[] propChainArr,
                                                                               out PropertyInfo propInfo)
        {
            propInfo = default!;

            var targetObj = Expression.Parameter(typeof(object), "t");
            //convert arg0 from obj to the declaring type so the getter can be called

            // if this is a parameterized property (e.g. public Byte this[Int32 i] { get; } )
            // we need to add more parameters
            //var finalGetter = propChainArr[propChainArr.Length - 1].GetGetMethod();
            //var finalGetterParams = finalGetter.GetParameters();
            //if (finalGetterParams.Length > 0)
            //{

            //}

            var arg0 = Expression.Convert(targetObj, targetType);

            Expression currentPropVal = arg0;

            var doneOrNullValue = Expression.Label();
            Expression propValObj = Expression.Constant(null);

            for (var c = 0; c < propChainArr.Length; c++)
            {
                propInfo = propChainArr[c];

                var getter = propInfo.GetGetMethod();

                currentPropVal = Expression.Call(currentPropVal, getter);

                //have to convert it so it can be the return value of our delegate
                propValObj = Expression.Convert(currentPropVal, typeof(object));

                if (c < propChainArr.Length - 1)
                {
                    var isNull = Expression.ReferenceEqual(
                        Expression.Constant(null), propValObj);

                    Expression.IfThen(isNull,
                        Expression.Return(doneOrNullValue));
                }
            }

            Expression.Label(doneOrNullValue);

            var lambda = Expression.Lambda<Func<object, object>>(propValObj, targetObj);

            var action = lambda.Compile();
            return action;
        }

        public static PropertySetter<T>? CreateSetMethod<T>(MemberInfo memberInfo)
        {
            var propChainArr = new[] { memberInfo };

            _paramTypeFairy ??= new Type[2];
            _paramTypeFairy[0] = typeof(T).MakeByRefType();
            _paramTypeFairy[1] = typeof(Object);

            return CreateSetterImpl<PropertySetter<T>>(typeof(T),
                _paramTypeFairy, memChainArr);
        }

        public PropertySetter<T>? CreateSetMethod<T>(String memberName)
        {
            _paramTypeFairy ??= new Type[2];
            _paramTypeFairy[0] = typeof(T).MakeByRefType();
            _paramTypeFairy[1] = typeof(Object);

            var propChainArr = GetPropertyChain(typeof(T), memberName).ToArray();
            return CreateSetterImpl<PropertySetter<T>>(typeof(T), _paramTypeFairy, propChainArr);
        }

        private static TSetter CreateSetterImpl<TSetter>(Type declaringType,
                                                        Type[] paramTypes,
                                                        PropertyInfo[] propChainArr)
        where TSetter : Delegate
        {
            //var propChainArr = GetPropertyChain(targetType, memberName).ToArray();

            var objArg0 = Expression.Parameter(paramTypes[0].MakeByRefType(), "arg0");
            var objVal = Expression.Parameter(paramTypes[1], "val");
            var val = Expression.Convert(objVal, propChainArr[propChainArr.Length - 1].GetMemberType());
            //convert arg0 from obj to the declaring type so the getter can be called
            var arg0 = Expression.Convert(objArg0, declaringType);

            Expression currentPropVal = arg0;

            var doneOrNullValue = Expression.Label();
            Expression propValObj; // = Expression.Constant(null);

            PropertyInfo propInfo;
            var c = 0;

            for (; c < propChainArr.Length - 1; c++)
            {
                propInfo = propChainArr[c];

                var getter = propInfo.GetGetMethod();

                currentPropVal = Expression.Call(currentPropVal, getter);

                //have to convert it so it can be the return value of our delegate
                propValObj = Expression.Convert(currentPropVal, typeof(object));

                if (c < propChainArr.Length - 1)
                {
                    var isNull = Expression.ReferenceEqual(
                        Expression.Constant(null), propValObj);

                    Expression.IfThen(isNull,
                        Expression.Return(doneOrNullValue));
                }
            }

            propInfo = propChainArr[c];

            var setter = propInfo.GetSetMethod();
            var setterCall = Expression.Call(currentPropVal, setter, val);
            Expression.Label(doneOrNullValue);
            return Expression.Lambda<TSetter>(setterCall, objArg0, objVal)
                             .Compile();
        }


        public static PropertySetter? CreateExpressionPropertySetter(Type targetType,
                                                                     String memberName)
        {
            var propChainArr = GetPropertyChain(targetType, memberName).ToArray();

            var objArg0 = Expression.Parameter(typeof(object).MakeByRefType(), "arg0");
            var objVal = Expression.Parameter(typeof(object), "val");
            var val = Expression.Convert(objVal, propChainArr[propChainArr.Length - 1].PropertyType);
            //convert arg0 from obj to the declaring type so the getter can be called
            var arg0 = Expression.Convert(objArg0, targetType);

            Expression currentPropVal = arg0;

            var doneOrNullValue = Expression.Label();
            Expression propValObj; // = Expression.Constant(null);

            PropertyInfo propInfo;
            var c = 0;

            for (; c < propChainArr.Length - 1; c++)
            {
                propInfo = propChainArr[c];

                var getter = propInfo.GetGetMethod();

                currentPropVal = Expression.Call(currentPropVal, getter);

                //have to convert it so it can be the return value of our delegate
                propValObj = Expression.Convert(currentPropVal, typeof(object));

                if (c < propChainArr.Length - 1)
                {
                    var isNull = Expression.ReferenceEqual(
                        Expression.Constant(null), propValObj);

                    Expression.IfThen(isNull,
                        Expression.Return(doneOrNullValue));
                }
            }

            propInfo = propChainArr[c];

            var setter = propInfo.GetSetMethod();

            if (setter == null)
                return default;

            var setterCall = Expression.Call(currentPropVal, setter, val);
            Expression.Label(doneOrNullValue);
            return Expression.Lambda<PropertySetter>(setterCall, objArg0, objVal)
                             .Compile();
        }


        public static Func<object, object> CreateExpressionPropertyGetter(Type targetType,
                                                                          String propertyName,
                                                                          out PropertyInfo propInfo)
        {
            var propChainArr = GetPropertyChain(targetType, propertyName).ToArray();
            return CreateExpressionPropertyGetterImpl(targetType, propChainArr, out propInfo);
        }

        public static PropertySetter CreateExpressionPropertySetter(PropertyInfo propInfo)
        {
            var objArg0 = Expression.Parameter(typeof(object).MakeByRefType(), "arg0");
            var arg0 = Expression.Convert(objArg0, propInfo.DeclaringType!);

            var objVal = Expression.Parameter(typeof(object), "val");
            var val = Expression.Convert(objVal, propInfo.PropertyType);

            var setter = propInfo.GetSetMethod();

            var setterCall = Expression.Call(arg0, setter, val);

            return Expression.Lambda<PropertySetter>(setterCall, objArg0, objVal)
                             .Compile();
        }

        #endif

        #if !GENERATECODE || TEST_NO_CODEGENERATION


        #endif
    }
}
