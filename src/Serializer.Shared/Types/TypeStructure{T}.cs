using System;
using System.Collections.Generic;

namespace Das.Serializer.Types
{
    public class TypeStructure<T> : TypeStructure,
                                    ITypeStructure<T>
    {
       public TypeStructure(Type type,
                            ITypeManipulator state,
                            IEnumerable<IPropertyAccessor> propertyAccessors) 
            : base(type, state, propertyAccessors)
        {
            Properties = new IPropertyAccessor<T>[base.Properties.Length];
            for (var c = 0; c < Properties.Length; c++)
            {
                Properties[c] = (IPropertyAccessor<T>) base.Properties[c];
            }
        }

        public new IPropertyAccessor<T>[] Properties { get; }
    }
}
