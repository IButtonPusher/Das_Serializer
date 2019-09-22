namespace Das.Serializer
{
    public interface INodeProvider<in TNode> : INodeProvider
        where TNode : INode
    {
        void Put(TNode node);
    }

    public interface INodeProvider
    {
        INodeManipulator TypeProvider { get; }
    }
}