using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Das.Types;
using Serializer;
using Serializer.Core;

namespace Das.Serializer
{
    internal class DynamicFacade : ISerializationCore
    {
        public ITextParser TextParser { get; }
        public IDynamicTypes DynamicTypes { get; }
        public IInstantiator ObjectInstantiator { get; }
        public ITypeInferrer TypeInferrer { get; }
        public ITypeManipulator TypeManipulator { get; }

        public IAssemblyList AssemblyList { get; }
        public IObjectManipulator ObjectManipulator { get; }
        public IDictionary<Type, Type> Surrogates { get; }

        public DynamicFacade(ISerializerSettings settings)
            : this(settings, new ConcurrentDictionary<Type, Type>())
        {
        }

        public DynamicFacade(ISerializerSettings settings,
            IDictionary<Type, Type> typeSurrogates)
        {
            var assemblyList = new AssemblyList();
            AssemblyList = assemblyList;

            TextParser = new CoreTextParser();

            var typeManipulator = new TypeManipulator(settings);
            TypeManipulator = typeManipulator;

            var manipulator = new ObjectManipulator(typeManipulator);
            ObjectManipulator = manipulator;

            var dynamicTypes = new DasTypeBuilder(settings, typeManipulator, manipulator);
            DynamicTypes = dynamicTypes;

            var typeInferrer = new TypeInference(dynamicTypes, assemblyList, settings);
            TypeInferrer = typeInferrer;

            ObjectInstantiator = new ObjectInstantiator(typeInferrer,
                typeManipulator, typeSurrogates, manipulator, dynamicTypes);
            Surrogates = typeSurrogates is ConcurrentDictionary<Type, Type> conc
                ? conc
                : new ConcurrentDictionary<Type, Type>(typeSurrogates);
        }
    }
}