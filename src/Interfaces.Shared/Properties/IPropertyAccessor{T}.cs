using System;
using System.Threading.Tasks;

namespace Das.Serializer;

public interface IPropertySetter<TParent, in TProperty> //: IPropertyAccessor
{
   Boolean SetPropertyValue(ref TParent targetObj,
                            TProperty? propVal);
}

public interface IPropertyAccessor<TParent, out TProperty> : IPropertyBase
{
   TProperty GetPropertyValue(ref TParent targetObj);
}

public interface IPropertyAccessor<TParent> : IPropertyAccessor
{
   Boolean SetPropertyValue(ref TParent targetObj,
                            Object? propVal);
}