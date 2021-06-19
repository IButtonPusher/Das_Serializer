using System;
using System.Collections.Generic;

namespace Das.Serializer.Types
{
    public abstract class TypeStructureBase
    {

        protected abstract IEnumerable<T> BuildPropertyAccessors<T>(Type type)
            where T : IPropertyAccessor;
    }

    
}
