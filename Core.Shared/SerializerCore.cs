using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Das.Serializer;

namespace Serializer.Core
{
    public abstract class SerializerCore : TypeCore, ISerializationCore
    {
        protected SerializerCore(ISerializationCore dynamicFacade, ISerializerSettings settings)
            : base(settings)
        {
            TextParser = dynamicFacade.TextParser;
            DynamicTypes = dynamicFacade.DynamicTypes;
            ObjectInstantiator = dynamicFacade.ObjectInstantiator;
            TypeInferrer = dynamicFacade.TypeInferrer;
            TypeManipulator = dynamicFacade.TypeManipulator;
            AssemblyList = dynamicFacade.AssemblyList;
            ObjectManipulator = dynamicFacade.ObjectManipulator;

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

        protected readonly ConcurrentDictionary<Type, Type> Surrogates;

        IDictionary<Type, Type> ISerializationCore.Surrogates => Surrogates;


    }
}