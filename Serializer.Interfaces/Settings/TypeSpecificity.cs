using System;

namespace Das.Serializer
{
    public enum TypeSpecificity
    {
        /// <summary>
        /// Selecting this option will cause type specifications to never be included in 
        /// serialized data.  Doing this may make it impossible to deserialize your data if your
        /// classes have properties of type Object, abstract classes, or interfaces.
        /// </summary>
        None,

        /// <summary>
        /// Selecting this option will embed type data only for data that is typed as Object or
        /// as an interface/abstract class.  Selecting this option will create somewhat of a 
        /// proprietary data format that may cause problems being consumed by other applications
        /// but is necessary in most cases to deserialize.  This is the default setting
        /// </summary>
        Discrepancy,

        /// <summary>
        /// Selecting this option will embed type information into every piece of data.  
        /// This option will generate the largest amount of bytes and be the slowest. However, 
        /// choosing this option allows for deserialization under nearly all circumstances -
        /// even when dynamic types have to be generated.
        /// </summary>
        All,
    }
}