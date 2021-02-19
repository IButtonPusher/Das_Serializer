using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface ISerializationCore
    {
        IAssemblyList AssemblyList { get; }

        IDynamicTypes DynamicTypes { get; }

        INodeTypeProvider NodeTypeProvider { get; }

        IInstantiator ObjectInstantiator { get; }

        IObjectManipulator ObjectManipulator { get; }

        INodeManipulator ScanNodeManipulator { get; }

        IDictionary<Type, Type> Surrogates { get; }

        ITextParser TextParser { get; }

        ITypeInferrer TypeInferrer { get; }

        ITypeManipulator TypeManipulator { get; }
    }
}
