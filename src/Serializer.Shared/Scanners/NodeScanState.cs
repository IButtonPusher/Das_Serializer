using System;

namespace Das.Serializer
{
    public enum NodeScanState
    {
        Invalid = -1,
        
        /// <summary>
        /// Before the opening of the node.  It is possible that there isn't even a node here as we haven't advanced
        /// far enough to be sure
        /// </summary>
        None = 0,

        /// <summary>
        /// There is a node but we have only passed the opening tag character.
        /// </summary>
        JustOpened,

        EncodingNodeOpened,

        /// <summary>
        /// The name has been read but there could still be attributes
        /// </summary>
        ReadNodeName,

        AttributeNameRead,

        AttributeValueRead,

        /// <summary>
        /// The node will only have properties set from attributes within its opening tag
        /// </summary>
        NodeSelfClosed,
        
        /// <summary>
        /// The name and attributes have been read.  There is likey to be a value if this is xml.  A closing tag
        /// is expected
        /// </summary>
        EndOfNodeOpen,
        
        StartOfNodeClose,

        EndOfNodeClose,

        EncodingNodeClose,

        /// <summary>
        /// We ran out of markup... Not a good state...
        /// </summary>
        EndOfMarkup,
        
        

    }
}
