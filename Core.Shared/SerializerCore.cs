using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public abstract class SerializerCore : TypeCore, ISerializationCore
    {
        protected SerializerCore(ISerializationCore dynamicFacade,
                                 ISerializerSettings settings)
            : base(settings)
        {
            TextParser = dynamicFacade.TextParser;
            DynamicTypes = dynamicFacade.DynamicTypes;
            ObjectInstantiator = dynamicFacade.ObjectInstantiator;
            TypeInferrer = dynamicFacade.TypeInferrer;
            TypeManipulator = dynamicFacade.TypeManipulator;
            AssemblyList = dynamicFacade.AssemblyList;
            ObjectManipulator = dynamicFacade.ObjectManipulator;
            NodeTypeProvider = dynamicFacade.NodeTypeProvider;
            PrintNodePool = dynamicFacade.PrintNodePool;
            ScanNodeManipulator = dynamicFacade.ScanNodeManipulator;

            Surrogates = dynamicFacade.Surrogates is ConcurrentDictionary<Type, Type> conc
                ? conc
                : new ConcurrentDictionary<Type, Type>(Surrogates);
        }

        public ITextParser TextParser { get; }

        public IDynamicTypes DynamicTypes { get; }

        public IInstantiator ObjectInstantiator { get; }

        public ITypeInferrer TypeInferrer { get; }

        public ITypeManipulator TypeManipulator { get; }

        public IAssemblyList AssemblyList { get; }

        public IObjectManipulator ObjectManipulator { get; }

        IDictionary<Type, Type> ISerializationCore.Surrogates => Surrogates;

        public INodeTypeProvider NodeTypeProvider { get; }

        public INodePool PrintNodePool { get; }

        public INodeManipulator ScanNodeManipulator { get; }

        protected readonly ConcurrentDictionary<Type, Type> Surrogates;
    }
}