using System;
using System.Collections;
using System.Collections.Generic;
using Das.Serializer.Objects;

namespace Das.Serializer
{
    public class CollectionTypeStructure : TypeStructure
    {
        //private Boolean _isIndexable;
        private readonly Type _germaneType;

        public CollectionTypeStructure(Type type, ISerializerSettings settings,
             INodePool nodePool) 
            : base(type, settings, nodePool)
        {
            _germaneType = GetGermaneType(type);
        }

        public override IPropertyValueIterator<IProperty> GetPropertyValues(Object o, 
            ISerializationDepth depth)
        {
            var res = PropertyValues;

            var collection = (IEnumerable)o;

            foreach (var item in collection)
            {
                var pv = _nodePool.GetProperty(String.Empty, item, _germaneType, Type);
                res.Add(pv);
            }

            return res;
        }

        
    }
}
