using System;
using System.Threading.Tasks;

namespace Das.Serializer.Properties;

public interface IPropertyActionAware
{
   FieldAction FieldAction { get; }
}