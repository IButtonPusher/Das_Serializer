using System;
using System.Collections;
using Das.Serializer.Objects;

namespace Das.Serializer
{
    public interface INodePool
    {
        IPrintNode GetPrintNode(INamedValue namedValue);

        IPrintNode GetPrintNode(INamedValue namedValue, Object overrideValue);

        INamedValue GetNamedValue(String name, Object value, Type type);

        INamedValue GetNamedValue(DictionaryEntry entry);

        IProperty GetProperty(String propertyName, Object propertyValue,
            Type propertyType, Type declaringType);
    }
}
