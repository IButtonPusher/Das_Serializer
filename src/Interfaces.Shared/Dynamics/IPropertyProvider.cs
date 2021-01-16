using System;

namespace Das.Serializer
{
    public interface IPropertyProvider
    {
        IPropertyAccessor GetPropertyAccessor(Type declaringType,
                                              String propertyName);
    }
}
