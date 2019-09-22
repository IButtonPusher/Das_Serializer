using System;
using System.IO;
using System.Threading.Tasks;

// ReSharper disable UnusedMember.Global

namespace Das.Serializer
{
    public interface IJsonSerializer : ISerializationCore
    {
        /////////////////////////////////////////////////////////////////
        //////////////////////////  JSON  ///////////////////////////////
        /////////////////////////////////////////////////////////////////

        T FromJson<T>(String json);

        T FromJson<T>(FileInfo file);

        T FromJson<T>(Stream stream);

        Task<T> FromJsonAsync<T>(Stream stream);

        Object FromJson(String json);

        /////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create a Json string from any object.  For more options set the Settings
        /// property of the serializer instance or the factory on which this is invoked
        /// </summary>
        /// <param name="o">The object to serialize</param>
        String ToJson(Object o);

        String ToJson<TObject>(TObject o);

        /// <summary>
        /// Serialize up or down.  Only the properties of TTarget will be serialized
        /// </summary>
        String ToJson<TTarget>(Object o);

        void ToJson(Object o, FileInfo fileName);

        void ToJson<TTarget>(Object o, FileInfo fileName);
    }
}