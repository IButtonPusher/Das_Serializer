using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer;

public interface ITypeInferrer : ITypeCore
{
   Int32 BytesNeeded(Type typ);

   void ClearCachedNames();

   /// <summary>
   ///     Leaves no stone unturned in searching for a type from a String without having to be
   ///     as specific as with Type.GetType()
   /// </summary>
   /// <example>String</example>
   /// <example>System.String</example>
   /// <example>MyProduct.MyNamespace.MyTypeName</example>
   /// <example>
   ///     MyAssembly.dll, MyProduct.MyNamespace.MyTypeName which would be faster than
   ///     not specifying the assembly name
   /// </example>
   Type? GetTypeFromClearName(String clearName,
                              Boolean isTryGeneric = false);


   /// <summary>
   ///     Leaves no stone unturned in searching for a type from a String without having to be
   ///     as specific as with Type.GetType()
   /// </summary>
   /// <param name="clearName">A string that can be a simple name or include namespaces.  See examples</param>
   /// <param name="nameSpaceAssemblySearch">
   ///     Keys are namespaces, values are assembly names.
   ///     These can make searching for runtime known types much faster and safer
   /// </param>
   /// <param name="isTryGeneric">
   ///     Tries to parse the type name as a generic.  If it's known not
   ///     to be, it's faster to set this to false
   /// </param>
   /// <returns></returns>
   /// <example>String</example>
   /// <example>System.String</example>
   /// <example>List[String]</example>
   /// <example>MyProduct.MyNamespace.MyTypeName</example>
   /// <example>
   ///     MyAssembly.dll, MyProduct.MyNamespace.MyTypeName which would be faster than
   ///     not specifying the assembly name
   /// </example>
   Type? GetTypeFromClearName(String clearName,
                              IDictionary<String, String> nameSpaceAssemblySearch,
                              Boolean isTryGeneric = false);

   Boolean IsDefaultValue<T>(T value);

   String ToCamelCase(String name);

   String ToClearName(Type type,
                      TypeNameOption options = TypeNameOption.AssemblyName |
                                               TypeNameOption.Namespace);

   //String ToClearName(Type type,
   //                   Boolean isOmitAssemblyName);

   //String ToClearNameNoGenericArgs(Type type,
   //                                Boolean isOmitAssemblyName);

   /// <summary>
   ///     Pascal cases the string
   /// </summary>
   String ToPascalCase(String name);

   String ToSnakeCase(String name);
}