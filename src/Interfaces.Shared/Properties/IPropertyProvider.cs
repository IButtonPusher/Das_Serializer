using System;
using System.Threading.Tasks;
using Das.Serializer.Properties;

namespace Das.Serializer
{
    public interface IPropertyProvider
    {
        // ReSharper disable once UnusedMember.Global - it is used...
        IPropertyAccessor GetPropertyAccessor(Type declaringType,
                                              String propertyName);

        IPropertyAccessor<T> GetPropertyAccessor<T>(String propertyName);
    }
}
