using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.Properties
{
    public abstract class PropertyAccessorBase
    {
        protected PropertyAccessorBase(Type declaringType,
                                       String propertyName,
                                       Func<object, object>? getter,
                                       PropertyInfo propInfo)
        {
            DeclaringType = declaringType;
            PropertyPath = propertyName;
            PropertyType = propInfo.PropertyType;
            _getter = getter;

            CanRead = _getter != null;
        }

        public Boolean CanRead { get; }

        public abstract Boolean CanWrite { get; }

        public Type DeclaringType { get; }

        public String PropertyPath { get; }

        public Type PropertyType { get; }

        public Object? GetPropertyValue(Object obj)
        {
            if (!CanRead)
                throw new MemberAccessException();

            //if (!(_getter is { } getter))
            //    throw new MemberAccessException();

            return _getter!(obj);
        }

        public override string ToString()
        {
            return DeclaringType.Name + "->" + PropertyPath + "( read: "
                   + CanRead + " write: " + CanWrite + " )";
        }

        public Boolean TryGetPropertyValue(Object obj,
                                        out Object result)
        {
            if (_getter == null)
            {
                result = default!;
                return false;
            }

            result = _getter(obj);
            return true;
        }

        private readonly Func<object, object>? _getter;
    }
}
