using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IPropertyProvider
    {
        IPropertyAccessor GetPropertyAccessor(Type declaringType,
                                              String propertyName);
    }
}
