namespace Das.Serializer
{
    public interface IStateProvider : ISerializationContext, IConverterProvider
    {
        IBinaryLoaner BorrowBinary(ISerializerSettings settings);

        IXmlLoaner BorrowXml(ISerializerSettings settings);

        IJsonLoaner BorrowJson(ISerializerSettings settings);

        ITextContext XmlContext { get; }

        ITextContext JsonContext { get; }

        IBinaryContext BinaryContext { get; }
    }
}