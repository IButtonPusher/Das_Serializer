using System;
using System.Collections.Generic;

namespace Das.Serializer
{
    public class ProtoStructure<TPropertyAttribute> : TypeStructure, IProtoStructure
        where TPropertyAttribute : Attribute
    {
        public ProtoStructure(Type type, ISerializationDepth depth, 
            ITypeManipulator state, ProtoBufOptions<TPropertyAttribute> options) 
            : base(type, true, depth, state)
        {
            FieldMap = new Dictionary<Int32, INamedField>();
            var propsMaybe = GetMembersToSerialize(depth);

            foreach (var prop in propsMaybe)
            {
                if (!TryGetAttribute<TPropertyAttribute>(prop.Name, out var attributes))
                    continue;

                var index = options.GetIndex(attributes[0]);
                FieldMap[index] = prop;
            }
        }

        public Dictionary<Int32, INamedField> FieldMap { get; }
    }
}
