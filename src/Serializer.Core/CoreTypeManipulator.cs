using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Das.Serializer
{
    public class CoreTypeManipulator : BaseTypeManipulator, ITypeManipulator
    {
        public CoreTypeManipulator(ISerializerSettings settings,
                                   INodePool nodePool) 
            : base(settings, nodePool)
        {
        }


        public override Func<object, object> CreateFieldGetter(FieldInfo fieldInfo)
        {
            throw new NotImplementedException();
        }

        public override Action<object, object?> CreateFieldSetter(FieldInfo fieldInfo)
        {
            throw new NotImplementedException();
        }

        public override Func<object, object[], object> CreateFuncCaller(MethodInfo method)
        {
            throw new NotImplementedException();
        }

        public override Func<object, object> CreatePropertyGetter(Type targetType, 
                                                                  PropertyInfo propertyInfo)
        {
            throw new NotImplementedException();
        }

        public override PropertySetter CreateSetMethod(MemberInfo memberInfo)
        {
            throw new NotImplementedException();
        }

        public override VoidMethod? GetAdder(Type collectionType, 
                                             Object exampleValue)
        {
            throw new NotImplementedException();
        }

        public override VoidMethod GetAdder(IEnumerable collection, 
                                            Type? collectionType = null)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo? GetAddMethod<T>(IEnumerable<T> collection)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo GetAddMethod(Type collectionType)
        {
            throw new NotImplementedException();
        }

        public override bool TryCreateReadOnlyPropertySetter(PropertyInfo propertyInfo, 
                                                             out Action<object, object?> setter)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetAddMethod(Type collectionType, 
                                             out MethodInfo addMethod)
        {
            throw new NotImplementedException();
        }

        public override VoidMethod CreateMethodCaller(MethodInfo method)
        {
            throw new NotImplementedException();
        }
    }
}
