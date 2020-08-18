using System;
using System.Threading.Tasks;

namespace Das.Serializer.Objects
{
    public interface IProperty : INamedValue
    {
        Type DeclaringType { get; }
    }
}