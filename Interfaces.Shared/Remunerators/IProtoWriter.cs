using System;
using System.Collections.Generic;
using System.Text;

namespace Das.Serializer.Remunerators
{
    public interface IProtoWriter : IBinaryWriter
    {
        IProtoWriter Push();
    }
}
