using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global

namespace Reflection.Common
{
    public static class ReflectionExtensions
    {
        [Pure]
        public static MethodInfo GetMethodOrDie(this Type classType,
                                                String methodName)
        {
            var wot = classType.GetMethod(methodName, Type.EmptyTypes) ??
                      classType.GetMethod(methodName);
            if (wot == null && classType.IsInterface)
                foreach (var @interface in classType.GetInterfaces())
                {
                    wot = @interface.GetMethod(methodName);
                    if (wot != null)
                        break;
                }

            return wot ?? throw new MissingMethodException(classType.FullName, methodName);
        }

        [Pure]
        public static MethodInfo GetMethodOrDie(this Type classType,
                                                string name,
                                                BindingFlags bindingAttr,
                                                Binder? binder,
                                                Type[] types,
                                                ParameterModifier[]? modifiers)
        {
            return classType.GetMethod(name, bindingAttr, binder, types, modifiers)
                   ?? throw new MissingMethodException();
        }

        [Pure]
        public static MethodInfo GetMethodOrDie(this Type classType,
                                                String methodName,
                                                BindingFlags flags)
        {
            return classType.GetMethod(methodName, flags) ?? Die(classType, methodName);
        }

        [Pure]
        public static MethodInfo GetMethodOrDie(this Type classType,
                                                String methodName,
                                                BindingFlags flags,
                                                Type[] parameters)
        {
            var res = classType.GetMethod(methodName, flags, null,
                parameters, null);
            //?? Die(classType, methodName);

            if (res != null)
                return res;

            if (classType.IsInterface)
            {
                var impl = classType.GetInterfaces();

                foreach (var type in impl)
                {
                    res = type.GetMethod(methodName, flags, null,
                        parameters, null);
                    if (res != null)
                        return res;
                }
            }

            return Die(classType, methodName);
        }

        [Pure]
        public static MethodInfo GetMethodOrDie(this Type classType,
                                                String methodName,
                                                BindingFlags flags,
                                                Type p1)
        {
            return classType.GetMethod(methodName, flags, null,
                       new[] { p1 }, null)
                   ?? Die(classType, methodName);
        }

        [Pure]
        public static MethodInfo GetMethodOrDie(this Type classType,
                                                String methodName,
                                                BindingFlags flags,
                                                Type p1,
                                                Type p2)
        {
            return classType.GetMethod(methodName, flags, null,
                       new[] { p1, p2 }, null)
                   ?? Die(classType, methodName);
        }

        [Pure]
        public static MethodInfo GetMethodOrDie(this Type classType,
                                                String methodName,
                                                Type[] parameters)
        {
            return GetMethodOrDie(classType, methodName,
                BindingFlags.Instance | BindingFlags.Public, parameters);
        }

        [Pure]
        public static MethodInfo GetMethodOrDie<TParam>(this Type classType,
                                                String methodName)
        {
            return GetMethodOrDie(classType, methodName,
                BindingFlags.Instance | BindingFlags.Public, new[] { typeof(TParam) });
        }

        [Pure]
        public static MethodInfo GetMethodOrDie(this Type classType,
                                                String methodName,
                                                Type p1)
        {
            return GetMethodOrDie(classType, methodName,
                BindingFlags.Instance | BindingFlags.Public, new[] { p1 });
        }

        [Pure]
        public static MethodInfo GetMethodOrDie(this Type classType,
                                                String methodName,
                                                Type p1,
                                                Type p2)
        {
            return GetMethodOrDie(classType, methodName,
                BindingFlags.Instance | BindingFlags.Public, new[] { p1, p2 });
        }

        [Pure]
        public static MethodInfo GetMethodOrDie(this Type classType,
                                                String methodName,
                                                Type p1,
                                                Type p2,
                                                Type p3)
        {
            return GetMethodOrDie(classType, methodName,
                BindingFlags.Instance | BindingFlags.Public,
                new[] { p1, p2, p3 });
        }

        [Pure]
        public static MethodInfo GetPublicStaticMethodOrDie(this Type classType,
                                                            String methodName)
        {
            return classType.GetMethod(methodName, PublicStatic,
                       null, Type.EmptyTypes, null)
                   ?? classType.GetMethod(methodName, PublicStatic)
                   ?? Die(classType, methodName);
        }

        [Pure]
        public static MethodInfo GetPublicStaticMethodOrDie(this Type classType,
                                                            String methodName,
                                                            params Type[] prms)

        {
            return classType.GetMethod(methodName, PublicStatic,
                       null, prms, null)
                   ?? Die(classType, methodName);
        }

        [Pure]
        public static ConstructorInfo GetConstructorOrDie(this Type t,
                                                          Type[] argTypes)
        {
            return t.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                null, argTypes, null) ?? throw new MissingMethodException(t.Name, "ctor");
        }

        [Pure]
        public static FieldInfo GetFieldOrDie(this Type t,
                                              String fieldName)
        {
            var useType = t;
            while (useType != null)
            {
                var field = t.GetField(fieldName, AnyInstance);
                if (field != null)
                    return field;

                useType = t.BaseType;
            }

            return Die<FieldInfo>(t, fieldName);
        }

        [Pure]
        public static FieldInfo GetStaticFieldOrDie(this Type classType,
                                                    String fieldName)
        {
            return classType.GetField(fieldName, AnyStatic)
                   ?? Die<FieldInfo>(classType, fieldName);
        }

        [Pure]
        public static ConstructorInfo GetDefaultConstructorOrDie(this Type classType)
        {
            return classType.GetConstructor(BindingFlags.Instance | BindingFlags.Public
                                                                  | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null) ?? throw new MissingMethodException(
                classType.FullName, "ctor");
        }

        [Pure]
        public static MethodInfo GetPropertyGetterOrDie(this Type classType,
                                                        String propertyName)
        {
            return GetPropertyGetterOrDie(classType, propertyName, PublicInstance);
        }

        [Pure]
        public static MethodInfo GetPropertyGetterOrDie(this Type classType,
                                                        String propertyName,
                                                        BindingFlags flags,
                                                        Type returnType,
                                                        params Type[] propParams)
        {
            var prop = GetPropertyOrDie(classType, propertyName, flags, returnType, propParams);
            return prop.GetGetMethod() ?? Die(classType, propertyName);
        }

        [Pure]
        public static MethodInfo GetPropertyGetterOrDie(this Type classType,
                                                        String propertyName,
                                                        Type returnType,
                                                        params Type[] propParams)
        {
            return GetPropertyGetterOrDie(classType, propertyName, PublicInstance,
                returnType, propParams);
        }

        [Pure]
        public static MethodInfo GetPropertySetterOrDie(this Type classType,
                                                        String propertyName)
        {
            return GetPropertyGetterOrDie(classType, propertyName, PublicInstance);
        }

        [Pure]
        public static MethodInfo GetPropertySetterOrDie(this Type classType,
                                                        String propertyName,
                                                        BindingFlags flags,
                                                        Type returnType,
                                                        params Type[] propParams)
        {
            var prop = GetPropertyOrDie(classType, propertyName, flags, returnType, propParams);
            return prop.GetSetMethod() ?? Die(classType, propertyName);
        }

        [Pure]
        public static MethodInfo GetPropertySetterOrDie(this Type classType,
                                                        String propertyName,
                                                        Type returnType,
                                                        params Type[] propParams)
        {
            return GetPropertySetterOrDie(classType, propertyName, PublicInstance,
                returnType, propParams);
            
        }

        [Pure]
        public static MethodInfo GetPropertyGetterOrDie(this Type classType,
                                                        String propertyName,
                                                        BindingFlags flags)
        {
            var prop = GetPropertyOrDie(classType, propertyName, flags);
            return prop.GetGetMethod() ?? Die(classType, propertyName);
        }

        [Pure]
        public static MethodInfo GetPropertySetterOrDie(this Type classType,
                                                        String propertyName,
                                                        BindingFlags flags)
        {
            var prop = GetPropertyOrDie(classType, propertyName, flags);
            return prop.GetSetMethod() ?? Die(classType, propertyName);
        }

        [Pure]
        public static PropertyInfo GetPropertyOrDie(this Type classType,
                                                    String propertyName)
        {
            return GetPropertyOrDie(classType, propertyName, PublicInstance);
        }

        [Pure]
        public static PropertyInfo GetPropertyOrDie(this Type classType,
                                                    String propertyName,
                                                    BindingFlags flags)
        {
            var wot = classType.GetProperty(propertyName);
            if (wot == null && classType.IsInterface)
                foreach (var @interface in classType.GetInterfaces())
                {
                    wot = @interface.GetProperty(propertyName, flags);
                    if (wot != null)
                        break;
                }

            return wot ?? throw new MissingMemberException(classType.FullName, propertyName);
        }

        [Pure]
        public static PropertyInfo GetPropertyOrDie(this Type classType,
                                                    String propertyName,
                                                    BindingFlags flags,
                                                    Type returnType,
                                                    params Type[] propParams)
        {
            var wot = classType.GetProperty(propertyName, flags, null, 
                returnType, propParams, null);
            if (wot == null && classType.IsInterface)
                foreach (var @interface in classType.GetInterfaces())
                {
                    wot = @interface.GetProperty(propertyName, flags, null,
                        returnType, propParams, null);
                    if (wot != null)
                        break;
                }

            return wot ?? throw new MissingMemberException(classType.FullName, propertyName);
        }


        private static T Die<T>(Type classType,
                                String memberName)
        {
            throw new MissingMemberException(classType.Name + "->" + memberName);
        }

        private static MethodInfo Die(Type classType,
                                      String methodName)
        {
            return Die<MethodInfo>(classType, methodName);
        }

        private const BindingFlags PublicStatic = BindingFlags.Static | BindingFlags.Public;
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic 
                                               | BindingFlags.FlattenHierarchy;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

        private const BindingFlags NonPublicInstance = BindingFlags.Instance |
                                                       BindingFlags.NonPublic;

        private const BindingFlags AnyInstance = PublicInstance | NonPublicInstance;
    }
}
