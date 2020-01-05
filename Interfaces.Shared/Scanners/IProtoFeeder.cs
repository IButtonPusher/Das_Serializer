﻿using System;
using Das.Streamers;

namespace Das.Serializer
{
    public interface IProtoFeeder : IBinaryFeeder
    {
        void Push(Int32 length);

        void GetInt32(ref Int32 result);

        void DumpInt32();

        void Pop();
    }
}
