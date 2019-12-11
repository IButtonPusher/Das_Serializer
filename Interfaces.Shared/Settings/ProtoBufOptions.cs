using System;

namespace Das.Serializer
{
    public class ProtoBufOptions<TPropertyAttribute> 
        where  TPropertyAttribute : Attribute
    {
        public Func<TPropertyAttribute, Int32> GetIndex { get; }

        public ProtoBufOptions(Func<TPropertyAttribute, Int32> getIndex)
        {
            GetIndex = getIndex;
        }
    }
}
