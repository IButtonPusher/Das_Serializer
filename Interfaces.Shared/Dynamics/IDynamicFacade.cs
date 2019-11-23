﻿using System;
using System.Collections.Generic;
using Serializer;

namespace Das.Serializer
{
    public interface IDynamicFacade
    {
        ITextParser TextParser { get; }

        IDynamicTypes DynamicTypes { get; }

        IInstantiator ObjectInstantiator { get; }

        ITypeInferrer TypeInferrer { get; }

        ITypeManipulator TypeManipulator { get; }

        IAssemblyList AssemblyList { get; }

        IObjectManipulator ObjectManipulator { get; }

        IDictionary<Type, Type> Surrogates { get; }
    }
}