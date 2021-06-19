using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.Properties
{
    public class SimplePropertyAccessor : PropertyAccessorBase,
                                          IPropertyAccessor
    {
        public SimplePropertyAccessor(Type declaringType,
                                      String propertyName,
                                      Func<object, object>? getter,
                                      PropertySetter? setter,
                                      PropertyInfo propInfo)
            : base(declaringType, propertyName, getter, propInfo)
        {
            _setter = setter;
            CanWrite = _setter != null;
        }


        public override Boolean CanWrite { get; }

        public Boolean SetPropertyValue(ref Object targetObj,
                                        Object? propVal)
        {
            if (_setter == null)
                return false;
            _setter(ref targetObj!, propVal);
            return true;
        }

        protected readonly PropertySetter? _setter;
    }
}
