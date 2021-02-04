using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IPropertyProvider
    {
        // ReSharper disable once UnusedMember.Global - it is used...
        IPropertyAccessor GetPropertyAccessor(Type declaringType,
                                              String propertyName);
    }
}
