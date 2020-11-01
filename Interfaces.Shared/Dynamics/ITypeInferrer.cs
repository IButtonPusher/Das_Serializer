using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface ITypeInferrer : ITypeCore
    {
        Int32 BytesNeeded(Type typ);

        void ClearCachedNames();

        /// <summary>
        ///     Leaves no stone unturned in searching for a type from a String without having to be
        ///     as specific as with Type.GetType()
        /// </summary>
        /// <example>String</example>
        /// <example>Sysytem.String</example>
        /// <example>MyProduct.MyNamespace.MyTypeName</example>
        /// <example>
        ///     MyAssembly.dll, MyProduct.MyNamespace.MyTypeName which would be faster than
        ///     not specifying the assembly name
        /// </example>
        Type? GetTypeFromClearName(String clearName, 
                                   Boolean isTryGeneric = false);

        Boolean IsDefaultValue<T>(T value);

        String ToClearName(Type type, 
                           Boolean isOmitAssemblyName);

        /// <summary>
        ///     Pascal cases the string
        /// </summary>
        String ToPropertyStyle(String name);
    }
}