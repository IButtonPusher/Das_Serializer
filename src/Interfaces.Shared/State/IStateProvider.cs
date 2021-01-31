using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IStateProvider : ISerializationCore, 
                                      IConverterProvider
    {
        //IBinaryContext BinaryContext { get; }

        //ITextContext JsonContext { get; }

        //ITextContext XmlContext { get; }

        IBinaryLoaner BorrowBinary(ISerializerSettings settings);

        //IJsonLoaner BorrowJson(ISerializerSettings settings);

        //IXmlLoaner BorrowXml(ISerializerSettings settings);
    }
}
