using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public delegate void VoidMethod(Object target, params Object[] args);

    public delegate void PropertySetter(ref Object? target, Object value);
}