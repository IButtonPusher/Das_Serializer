﻿using System;
using Das.Serializer.Annotations;

namespace Das.Serializer
{
    public interface INodeSealer<in TNode> : ISettingsUser
        where TNode : INode<TNode>
    {
        void Imbue([NotNull]TNode node, String propertyName, Object value);

        void Imbue(TNode childNode);

        //Boolean TryGetPropertyValue(TNode node, String key, Type propertyType, out Object val);

        void CloseNode(TNode node);
    }
}