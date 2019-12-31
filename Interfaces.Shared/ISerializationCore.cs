using System;
using System.Collections.Generic;

namespace Das.Serializer
{
    public interface ISerializationCore
    {
        ITextParser TextParser { get; }

        IDynamicTypes DynamicTypes { get; }

        IInstantiator ObjectInstantiator { get; }

        ITypeInferrer TypeInferrer { get; }

        ITypeManipulator TypeManipulator { get; }

        IAssemblyList AssemblyList { get; }

        IObjectManipulator ObjectManipulator { get; }

        IDictionary<Type, Type> Surrogates { get; }

        INodeTypeProvider NodeTypeProvider { get; }

        INodePool PrintNodePool { get; }

        INodeManipulator ScanNodeManipulator { get; }
    }
}