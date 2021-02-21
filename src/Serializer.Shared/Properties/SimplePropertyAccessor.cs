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
            //DeclaringType = declaringType;
            //PropertyPath = propertyName;
            //PropertyType = propInfo.PropertyType;
            //_getter = getter;
            _setter = setter;
            //_propInfo = propInfo;

            //CanRead = _getter != null;
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

        //private readonly Func<object, object>? _getter;
        protected readonly PropertySetter? _setter;
        //private readonly PropertyInfo _propInfo;
    }
}
