using System;
using Das.Serializer.Annotations;

namespace Das.Serializer
{
    public interface IBinaryNodeProvider : INodeProvider<IBinaryNode>
    {
        INodeSealer<IBinaryNode> Sealer { get; }

        IBinaryNode Get(String name, [NotNull]IBinaryNode parent, Type type);


        void ResolveCircularReference(IBinaryNode node, ref Byte distanceFromParent);
    }
}