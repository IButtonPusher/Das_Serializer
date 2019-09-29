using System;

namespace Das.Serializer
{
    public interface ITypeInferrer : ITypeCore
    {
        /// <summary>
        /// Leaves no stone unturned in searching for a type from a String without having to be 
        /// as specific as with Type.GetType()
        /// </summary>
        /// <example>String</example>
        /// <example>Sysytem.String</example>
        /// <example>MyProduct.MyNamespace.MyTypeName</example>
        /// <example>MyAssembly.dll, MyProduct.MyNamespace.MyTypeName which would be faster than
        /// not specifying the assembly name</example>
        Type GetTypeFromClearName(String clearName);

        String ToClearName(Type type, Boolean isOmitAssemblyName);

        /// <summary>
        /// if this is a generic collection of T or T[] it will return typeof(T)
        /// otherwise returns the same type
        /// </summary>
        Type GetGermaneType(Type ownerType);

        void ClearCachedNames();

        Int32 BytesNeeded(Type typ);

        Boolean IsDefaultValue(Object o);

        /// <summary>
        /// Pascal cases the string
        /// </summary>
        String ToPropertyStyle(String name);
    }
}