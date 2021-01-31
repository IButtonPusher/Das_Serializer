using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class ProtoBufOptions<TPropertyAttribute> : IProtoBufOptions<TPropertyAttribute> 
        where TPropertyAttribute : Attribute
    {
        public ProtoBufOptions(Func<TPropertyAttribute, Int32> getIndex)
        {
            GetIndex = getIndex;
        }

        public Func<TPropertyAttribute, Int32> GetIndex { get; }
    }

    public class ProtoBufOptions : ProtoBufOptions<IndexedMemberAttribute>
    {
        public static readonly ProtoBufOptions Default = new ProtoBufOptions();

        private ProtoBufOptions() : base(p => p.Index)
        {
        }
    }
}
