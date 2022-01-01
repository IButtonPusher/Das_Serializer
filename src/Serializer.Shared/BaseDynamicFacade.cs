using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Das.Types;

namespace Das.Serializer
{
   public abstract class BaseDynamicFacade : ISerializationCore
   {
      public BaseDynamicFacade(ISerializerSettings settings)
            : this(settings, new ConcurrentDictionary<Type, Type>())
        {
        }

        public BaseDynamicFacade(ISerializerSettings settings,
                                 IDictionary<Type, Type> typeSurrogates)
        {
            var assemblyList = new AssemblyList();
            AssemblyList = assemblyList;

            var typeCore = new TypeCore(settings);
            var nodeTypeProvider = new NodeTypeProvider(typeCore, settings);

            //PrintNodePool = new NodePool(settings, nodeTypeProvider);

            //TextParser = new CoreTextParser();

            var typeManipulator = new TypeManipulator(settings); //, PrintNodePool);
            TypeManipulator = typeManipulator;

            var manipulator = new ObjectManipulator(typeManipulator, settings);
            ObjectManipulator = manipulator;

            #if GENERATECODE

            var dynamicTypes = new DasTypeBuilder(settings, typeManipulator, manipulator);

            #else
            var dynamicTypes = new InvalidTypeBuilder(assemblyList);

            #endif
            DynamicTypes = dynamicTypes;

            var typeInferrer = new TypeInference(dynamicTypes, assemblyList, settings);
            TypeInferrer = typeInferrer;

            ObjectInstantiator = new ObjectInstantiator(typeInferrer,
                typeManipulator, typeSurrogates, manipulator, dynamicTypes);
            Surrogates = typeSurrogates is ConcurrentDictionary<Type, Type> conc
                ? conc
                : new ConcurrentDictionary<Type, Type>(typeSurrogates);


            NodeTypeProvider = nodeTypeProvider;
        }

        //public ITextParser TextParser { get; }


        public IDynamicTypes DynamicTypes { get; }


        public IInstantiator ObjectInstantiator { get; }

        public ITypeInferrer TypeInferrer { get; }

        public ITypeManipulator TypeManipulator { get; }

        public IAssemblyList AssemblyList { get; }

        public IObjectManipulator ObjectManipulator { get; }

        public IDictionary<Type, Type> Surrogates { get; }

        public INodeTypeProvider NodeTypeProvider { get; }

        //public INodePool PrintNodePool { get; }

        public abstract INodeManipulator ScanNodeManipulator { get; }
   }
}
