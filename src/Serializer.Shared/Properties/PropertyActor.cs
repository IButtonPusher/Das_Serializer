using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.Properties;

public class PropertyActor : PropertyInfoBase,
                             IPropertyActionAware,
                             IIndexedProperty
{
   public PropertyActor(String name,
                        Type type,
                        MethodInfo getMethod,
                        MethodInfo? setMethod,
                        FieldAction fieldAction,
                        Int32 index) 
      : base(name, type, getMethod, setMethod)
   {
      FieldAction = fieldAction;
      Index = index;
   }

   public FieldAction FieldAction { get; }

   public Int32 Index { get; }
}