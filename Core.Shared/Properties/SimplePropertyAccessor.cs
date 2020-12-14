using System;

namespace Das.Serializer
{
    public class SimplePropertyAccessor : IPropertyAccessor
    {
        private readonly Func<object, object>? _getter;
        private readonly PropertySetter? _setter;

        public SimplePropertyAccessor(Type declaringType,
                                      String propertyName,
                                      Func<object, object>? getter,
                                      PropertySetter? setter)
        {
            DeclaringType = declaringType;
            PropertyPath = propertyName;
            _getter = getter;
            _setter = setter;

            CanRead = _getter != null;
            CanWrite = _setter != null;
        }

        public Boolean CanRead { get; }

        public Boolean CanWrite { get; }

        public Type DeclaringType { get; }

        public String PropertyPath { get; }

        public bool SetPropertyValue(ref Object targetObj,
                                     Object? propVal)
        {
            if (_setter == null)
                return false;
            _setter(ref targetObj!, propVal);
            return true;
        }

        public bool TryGetPropertyValue(Object obj,
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

        public object? GetPropertyValue(Object obj)
        {
            if (!(_getter is { } getter))
                throw new MemberAccessException();

            return getter(obj);
        }

        public override string ToString()
        {
            return DeclaringType.Name + "->" + PropertyPath + "( read: "
                   + CanRead + " write: " + CanWrite + " )";
        }
    }
}
