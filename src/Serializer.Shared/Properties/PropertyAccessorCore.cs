using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.Properties
{
    public abstract class PropertyAccessorCore : IPropertyBase
    {
        protected PropertyAccessorCore(Boolean canRead,
                                       Type declaringType,
                                       PropertyInfo propertyInfo,
                                       String propertyPath)
        {
           Name = propertyInfo.Name;
            Type = propertyInfo.PropertyType;

            //Logger.WriteDebug("propacc " + propertyInfo.DeclaringType?.Name + "->" + Name +
            //                                            (Interlocked.Add(ref _counter, 1)));

            CanRead = canRead;
            DeclaringType = declaringType;
            PropertyInfo = propertyInfo;
            PropertyPath = propertyPath;
            PropertyType = propertyInfo.PropertyType;

            _attributes = new Dictionary<Type, object>();

            var attrs = propertyInfo.GetCustomAttributes(true);

            foreach (var attr in attrs)
            {
                _attributes.Add(attr.GetType(), attr);
            }
        }

        //private static Int32 _counter;

        public String Name { get; }

        public Type Type { get; }

        public Boolean CanRead { get; }

        public abstract Boolean CanWrite { get; }

        public Type DeclaringType { get; }

        public PropertyInfo PropertyInfo { get; }

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
