using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IValueNode
    {
        Type? Type { get; set; }

        Object? Value { get; }
    }
}