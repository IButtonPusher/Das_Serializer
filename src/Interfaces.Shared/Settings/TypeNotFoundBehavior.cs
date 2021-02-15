using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public enum TypeNotFoundBehavior
    {
        /// <summary>
        ///     Attempt to generate a dynamic type at runtime
        /// </summary>
        GenerateRuntime,

        /// <summary>
        ///     Throw an exception if no type can be determined from any piece of data
        /// </summary>
        ThrowException,
        NullValue
    }
}
