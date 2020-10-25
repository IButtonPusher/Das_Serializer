using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Das.Serializer.Types;
using Das.Types;

namespace Das.Serializer
{
    public class CoreFacade : ISerializationCore
    {
        public CoreFacade(ISerializerSettings settings)
            : this(settings, new ConcurrentDictionary<Type, Type>())
        {
        }

        public CoreFacade(ISerializerSettings settings,
                          IDictionary<Type, Type> typeSurrogates)
        {
            var assemblyList = new AssemblyList();
            AssemblyList = assemblyList;

            var typeCore = new TypeCore(settings);
            var nodeTypeProvider = new NodeTypeProvider(typeCore, settings);

            PrintNodePool = new NodePool(settings, nodeTypeProvider);

            TextParser = new CoreTextParser();

            var typeManipulator = new CoreTypeManipulator(settings, PrintNodePool);
            TypeManipulator = typeManipulator;

            var manipulator = new ObjectManipulator(typeManipulator, settings);
            ObjectManipulator = manipulator;

            var dynamicTypes = new NullTypeBuilder();
            DynamicTypes = dynamicTypes;

            var typeInferrer = new TypeInference(dynamicTypes, assemblyList, settings);
            TypeInferrer = typeInferrer;

            ObjectInstantiator = new CoreInstantiator();
            Surrogates = typeSurrogates is ConcurrentDictionary<Type, Type> conc
                ? conc
                : new ConcurrentDictionary<Type, Type>(typeSurrogates);


            NodeTypeProvider = nodeTypeProvider;

            ScanNodeManipulator = new NodeManipulator(this, settings);
        }

        public ITextParser TextParser { get; }

        public virtual IDynamicTypes DynamicTypes { get; }

        public virtual IInstantiator ObjectInstantiator { get; }

        public virtual ITypeInferrer TypeInferrer { get; }

        public ITypeManipulator TypeManipulator { get; }

        public IAssemblyList AssemblyList { get; }

        public IObjectManipulator ObjectManipulator { get; }

        public IDictionary<Type, Type> Surrogates { get; }

        public INodeTypeProvider NodeTypeProvider { get; }

        public INodePool PrintNodePool { get; }

        public INodeManipulator ScanNodeManipulator { get; }
    }
}
    

