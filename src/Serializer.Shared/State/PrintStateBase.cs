using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using Das.Serializer.CodeGen;
using Das.Serializer.Properties;

namespace Das.Serializer.State
{
    public abstract class PrintStateBase : DynamicStateBase
    {
        public PrintStateBase(ILGenerator il,
                 ITypeManipulator types,
                 Type parentType,
                 Action<ILGenerator>? loadCurrentValueOntoStack,
                 IDictionary<Type, ProxiedInstanceField> proxies,
                 IFieldActionProvider actionProvider) 
            : base(il, types, parentType, loadCurrentValueOntoStack, 
                proxies, actionProvider)
        {
        }

        
    }
}
