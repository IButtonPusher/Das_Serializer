using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer;

public class DasProperty : DasMember
{
   public DasProperty(PropertyInfo prop)
   : base(prop.Name, prop.PropertyType)
   {
      Attributes = _emptyAttribs;
   }

   public DasProperty(String name,
                      Object value)
   : base(name, value.GetType(), value)
   {
      Attributes = _emptyAttribs;
   }

   public DasProperty(String name,
                      Type type) : base(name, type)
   {
      Attributes = _emptyAttribs;
   }

   public DasProperty(String name,
                      Type type,
                      Object? value)
      : base(name, type, value)
   {
      Attributes = _emptyAttribs;
   }

   public DasProperty(String name,
                      Type type,
                      DasAttribute[] attributes)
      : base(name, type)
   {
      Attributes = attributes;
   }

   public DasAttribute[] Attributes { get; }

   private static readonly DasAttribute[] _emptyAttribs =
      #if NET40
                     new DasAttribute[0];
      #else
      Array.Empty<DasAttribute>();
   #endif
}