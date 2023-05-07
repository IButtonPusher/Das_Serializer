#if GENERATECODE

using System;
using System.Reflection;
using System.Threading.Tasks;
using Das.Serializer.Properties;
using Das.Serializer.Proto;

namespace Das.Serializer
{
    public class ProtoEnumerator<TState> : DynamicEnumerator<TState>
        where TState : IProtoPrintState
    {
        public ProtoEnumerator(TState s,
                               Type ienumerableType,
                               MethodInfo getMethod,
                               ITypeManipulator types,
                               IFieldActionProvider actionProvider) 
            : base(s, ienumerableType, getMethod, types, actionProvider)
        {
        }

        protected ProtoEnumerator(TState s,
                                  Type ienumerableType,
                                  ITypeManipulator types,
                                  IFieldActionProvider actionProvider) 
            : base(s, ienumerableType, types, actionProvider)
        {
        }
    }

  
}

#endif
