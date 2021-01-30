using System;
using System.Collections;
using System.Threading.Tasks;
using Das.Serializer.Objects;

namespace Das.Serializer
{
    public interface INodePool
    {
        INamedValue GetNamedValue(String name,
                                  Object? value,
                                  Type type);

        INamedValue GetNamedValue(DictionaryEntry entry);

        IPrintNode GetPrintNode(INamedValue namedValue);

        IPrintNode GetPrintNode(INamedValue namedValue,
                                Object? overrideValue);

        IProperty GetProperty(String propertyName,
                              Object? propertyValue,
                              Type propertyType,
                              Type declaringType);
    }
}
