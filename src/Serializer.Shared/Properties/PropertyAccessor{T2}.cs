using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.Properties;

public class PropertyAccessor<TObject, TProperty> : PropertyAccessorCore,
                                                    IPropertyAccessor<TObject, TProperty>,
                                                    IPropertySetter<TObject, TProperty>
{
   public PropertyAccessor(PropertyInfo propertyInfo,
                           Func<TObject, TProperty>? getter,
                           Action<TObject, TProperty>? setter,
                           String propertyPath)
      : base(getter != null, typeof(TObject), propertyInfo, propertyPath)
   {
      _getter = getter;
      _setter = setter;

      CanWrite = setter != null;
   }

   public override Boolean CanWrite { get; }

   public TProperty GetPropertyValue(ref TObject targetObj)
   {
      if (_getter is { } g)
         return g(targetObj);

      throw new NotSupportedException();
   }

   public bool SetPropertyValue(ref TObject targetObj,
                                TProperty? propVal)
   {
      if (_setter is not { })
         return false;

      _setter(targetObj, propVal!);

      return true;
   }

   private readonly Func<TObject, TProperty>? _getter;
   private readonly Action<TObject, TProperty>? _setter;
}