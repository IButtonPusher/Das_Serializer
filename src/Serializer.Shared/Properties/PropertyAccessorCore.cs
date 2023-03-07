using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Das.Extensions;

namespace Das.Serializer.Properties
{
    public abstract class PropertyAccessorCore : PropertyInfoBase,
                                                 IPropertyBase
    {
        protected PropertyAccessorCore(Boolean canRead,
                                       Type declaringType,
                                       PropertyInfo propertyInfo,
                                       String propertyPath)
        : base(propertyInfo.Name, propertyInfo.PropertyType,
            propertyInfo.GetGetMethod(),
            propertyInfo.CanWrite ? propertyInfo.GetSetMethod() : default)
        {
            IsMemberSerializable = propertyInfo.GetCustomAttribute<NonSerializedAttribute>() == null
                && propertyInfo.GetCustomAttribute<IgnoreDataMemberAttribute>() == null;

            //Name = propertyInfo.Name;
            //Type = propertyInfo.PropertyType;

            CanRead = canRead;
            DeclaringType = declaringType;
            PropertyInfo = propertyInfo;
            PropertyPath = propertyPath;
            PropertyType = propertyInfo.PropertyType;

            //GetMethod = propertyInfo.GetGetMethod();
            //SetMethod = propertyInfo.CanWrite ? propertyInfo.GetSetMethod() : default;

            //TypeCode = propertyInfo.PropertyType != null
            //    ? Type.GetTypeCode(propertyInfo.PropertyType)
            //    : TypeCode.Object;

            _attributes = new Dictionary<Type, object>();

            var attrs = propertyInfo.GetCustomAttributes(true);

            foreach (var attr in attrs)
            {
                _attributes.Add(attr.GetType(), attr);
            }
        }

        public Boolean IsMemberSerializable { get; }

        //public String Name { get; }

        //public Type Type { get; }

        public Boolean CanRead { get; }

        public abstract Boolean CanWrite { get; }

        public Type DeclaringType { get; }

        public PropertyInfo PropertyInfo { get; }

        //public MethodInfo GetMethod { get; }

        //public MethodInfo? SetMethod { get; }

        //public TypeCode TypeCode { get; }

        public String PropertyPath { get; }

        public Type PropertyType { get; }

        public Boolean TryGetAttribute<TAttribute>(out TAttribute value)
            where TAttribute : Attribute
        {
            if (_attributes.TryGetValue(typeof(TAttribute), out var items)
                && items is TAttribute attr)
            {
                value = attr;
                return true;
            }

            value = default!;
            return false;
        }

        private readonly Dictionary<Type, Object> _attributes;
    }
}
