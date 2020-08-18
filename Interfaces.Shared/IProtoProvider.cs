using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.ProtoBuf
{
    public interface IProtoProvider
    {
        T BuildDefaultValue<T>();

#if DEBUG

        void DumpProxies();

#endif

        /// <summary>
        ///     Builds a scan and/or print proxy for type T using all public properties that have setter methods
        ///     and/or are instantiated in the constructor via arguments
        ///     can be
        ///     specified in the applicable ProtoBufOptions{TPropertyAttribute} instance
        /// </summary>
        /// <typeparam name="T">The type of the class to serialize </typeparam>
        /// <param name="allowReadOnly">skips generating a scan method if no usable constructor is found</param>
        /// <exception cref="MissingMethodException">If allowReadOnly is false and no default constructor is found</exception>
        IProtoProxy<T> GetAutoProtoProxy<T>(Boolean allowReadOnly = false);

        ProtoFieldAction GetProtoFieldAction(Type pType);

        /// <summary>
        ///     Builds a scan and/or print proxy for type T where properties with the attribute
        ///     specified in the applicable ProtoBufOptions{TPropertyAttribute} instance
        /// </summary>
        /// <typeparam name="T">The type of the class to serialize </typeparam>
        /// <param name="allowReadOnly">skips generating a scan method if no usable constructor is found</param>
        /// <exception cref="MissingMethodException">If allowReadOnly is false and no default constructor is found</exception>
        IProtoProxy<T> GetProtoProxy<T>(Boolean allowReadOnly = false);

        Boolean TryGetProtoField(PropertyInfo prop, Boolean isRequireAttribute,
            out IProtoFieldAccessor field);
    }
}