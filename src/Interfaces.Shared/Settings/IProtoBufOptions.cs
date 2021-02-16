using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IProtoBufOptions<in TPropertyAttribute> where TPropertyAttribute : Attribute
    {
        Func<TPropertyAttribute, Int32> GetIndex { get; }
    }
}
