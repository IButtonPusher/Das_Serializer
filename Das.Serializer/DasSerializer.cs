using System;
using Das.Serializer;

namespace Das
{
    public class DasSerializer : DasCoreSerializer
    {
        public DasSerializer(IStateProvider stateProvider)
            : base(stateProvider)
        {
        }

        public DasSerializer(ISerializerSettings settings)
            : base(new DefaultStateProvider(settings))
        {
        }

        public DasSerializer() : base(new DefaultStateProvider())
        {
        }
    }
}