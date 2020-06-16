using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class ProtoBufOptions<TPropertyAttribute>
        where TPropertyAttribute : Attribute
    {
        public ProtoBufOptions(Func<TPropertyAttribute, Int32> getIndex)
        {
            GetIndex = getIndex;
        }

        public Func<TPropertyAttribute, Int32> GetIndex { get; }
    }
}