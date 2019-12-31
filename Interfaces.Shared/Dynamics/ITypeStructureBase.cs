using System;
using System.Collections.Generic;

// ReSharper disable UnusedMemberInSuper.Global

namespace Das.Serializer
{
    public interface ITypeStructureBase
    {
		Type Type { get; }
		
		/// <exception cref="KeyNotFoundException"></exception>
        void SetPropertyValueUnsafe(String propName, ref Object targetObj, Object propVal);
	}
}