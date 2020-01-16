namespace Das.Serializer.ProtoBuf
{
    public interface IProtoProvider
    {
        IProtoProxy<T> GetProtoProxy<T>() where T: class;
    }
}