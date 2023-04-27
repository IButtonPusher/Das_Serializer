using System;
using System.Threading.Tasks;

namespace Das.Serializer;

public class SerializeAsTypeAttribute : Attribute
{
   public SerializeAsTypeAttribute(Type type)
   {
      TargetType = type;
   }

   public Type TargetType { get; }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class IndexedMemberAttribute : Attribute
{
   // ReSharper disable once UnusedAutoPropertyAccessor.Global
   public Int32 Index { get; set; }
}