﻿using System;

namespace Das.Serializer
{
    public interface IStateProvider : ISerializationContext, IConverterProvider
    {
        IBinaryLoaner BorrowBinary(ISerializerSettings settings);

        IBinaryLoaner BorrowProto<T>(ISerializerSettings settings, ProtoBufOptions<T> options)
            where T : Attribute;

        IXmlLoaner BorrowXml(ISerializerSettings settings);

        IJsonLoaner BorrowJson(ISerializerSettings settings);

        ITextContext XmlContext { get; }

        ITextContext JsonContext { get; }

        IBinaryContext BinaryContext { get; }
    }
}