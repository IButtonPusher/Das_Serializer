using System;
using System.Threading.Tasks;


#if !GENERATECODE
using Das.Serializer;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
#endif

namespace Das.Types
{
    public partial class TypeManipulator
    {
        #if !GENERATECODE
        public sealed override Func<Object, Object> CreatePropertyGetter(Type targetType,
                                                                PropertyInfo propertyInfo)
        {
            return CreateExpressionPropertyGetter(targetType, propertyInfo);
        }

        public override Func<object, object> CreatePropertyGetter(Type targetType, 
                                                                  String propertyName)
        {
            return CreateExpressionPropertyGetter(targetType, propertyName);
        }

        public override PropertySetter CreateSetMethod(MemberInfo memberInfo)
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

        public override PropertySetter CreateSetMethod(Type targetType, 
                                                       String memberName)
        {
            return CreateExpressionPropertySetter(targetType, memberName);
        }

        public sealed override Func<Object, Object> CreateFieldGetter(FieldInfo fieldInfo)
        {
            var input = Expression.Parameter(fieldInfo.DeclaringType 
                                             ?? throw new InvalidOperationException());
            return Expression.Lambda<Func<Object, Object>>(Expression.PropertyOrField(
                input, fieldInfo.Name), input).Compile();
        }

        public sealed override Action<Object, Object?> CreateFieldSetter(FieldInfo fieldInfo)
        {

            Action<Object, Object?> bob = fieldInfo.SetValue;
            return bob;
        }

        public override VoidMethod CreateMethodCaller(MethodInfo method)
        {
            VoidMethod bobbith = (target, paramValues) =>
            {
                method.Invoke(target, paramValues);
                //bob.DynamicInvoke(target, paramValues);
            };


            return bobbith;
        }


        #endif

        #if !GENERATECODE || TEST_NO_CODEGENERATION


        public static Func<object, object> CreateExpressionPropertyGetter(Type targetType,
                                                                          PropertyInfo propertyInfo)
        {
            _singlePropFairy ??= new PropertyInfo[1];
            _singlePropFairy[0] = propertyInfo;
            return CreateExpressionPropertyGetterImpl(targetType, _singlePropFairy);
        }

        private static Func<Object, Object> CreateExpressionPropertyGetterImpl(
            Type targetType,
            PropertyInfo[] propChainArr)
        {
            var targetObj = Expression.Parameter(typeof(object), "t");
            //convert arg0 from obj to the declaring type so the getter can be called
            var arg0 = Expression.Convert(targetObj, targetType);

            Expression currentPropVal = arg0;

            var doneOrNullValue = Expression.Label();
            Expression propValObj = Expression.Constant(null);

            for (var c = 0; c < propChainArr.Length; c++)
            {
                var propInfo = propChainArr[c];

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

        public static PropertySetter CreateExpressionPropertySetter(Type targetType,
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
            var setterCall = Expression.Call(currentPropVal, setter, val);
            Expression.Label(doneOrNullValue);
            return Expression.Lambda<PropertySetter>(setterCall, objArg0, objVal)
                             .Compile();
        }


        public static Func<object, object> CreateExpressionPropertyGetter(Type targetType,
                                                                          String propertyName)
        {
            var propChainArr = GetPropertyChain(targetType, propertyName).ToArray();
            return CreateExpressionPropertyGetterImpl(targetType, propChainArr);
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
    }
}
