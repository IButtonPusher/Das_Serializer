﻿using System;

// ReSharper disable UnusedMember.Global

namespace Das.Serializer
{
    public interface ISerializationDepth
    {
        /// <summary>
        /// In Xml/Json only.  0 for integers, false for booleans, and any
        /// val == default will be ommitted from the markup		
        /// </summary>
        Boolean IsOmitDefaultValues { get; }

        /// <summary>
        /// Allows to set whether properties without setters and whether private fields 
        /// will be serialized.  Default is GetSetProperties
        /// </summary>
        SerializationDepth SerializationDepth { get; }

        Boolean IsRespectXmlIgnore { get; }
    }
}