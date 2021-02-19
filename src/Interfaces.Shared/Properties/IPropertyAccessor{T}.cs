using System;

namespace Das.Serializer.Properties
{
    public interface IPropertyAccessor<TParent, TProperty> : IPropertyAccessor
    {
        Boolean SetPropertyValue(ref TParent targetObj,
                                 TProperty? propVal);
    }

    public interface IPropertyAccessor<TParent> : IPropertyAccessor
    {
        Boolean SetPropertyValue(ref TParent targetObj,
                                 Object? propVal);
    }
}
