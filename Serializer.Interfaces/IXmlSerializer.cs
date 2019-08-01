using System;
using System.IO;

// ReSharper disable UnusedMember.Global

namespace Das.Serializer
{
    public interface IXmlSerializer
    {
        /////////////////////////////////////////////////////////////////
        //////////////////////////   XML  ///////////////////////////////
        /////////////////////////////////////////////////////////////////

        T FromXml<T>(String xml);

        T FromXml<T>(FileInfo file);

        T FromXml<T>(Stream stream);

        Object FromXml(String xml);

        Object FromXml(Stream stream);


        /////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create an XML string from any object.  For more options set the Settings
        /// property of the serializer instance or the factory on which this is invoked
        /// </summary>
        /// <param name="o">The object to serialize</param>
        String ToXml(Object o);

        String ToXml<TObject>(TObject o);

        /// <summary>
        /// Serialize up or down.  Only the properties of TTarget will be serialized
        /// </summary>
        String ToXml<TTarget>(Object o);

        /// <summary>
        /// User friendly/less performant save to disk.  Keeps whole serialized string in memory then
        /// dumps to file when ready.  Creates the directory for the file if it doesn't
        /// already exist
        /// </summary>
        void ToXml(Object o, FileInfo file);

        /// <summary>
        /// Tries to ensure no empty files if the process cuts off during invocation.
        /// Recommended for small files like config
        /// The downside is that if this is a big object then it's all going into memory.
        /// For a lighter, more dangerous way of saving xml to disk use XmlToStream
        /// with a FileStream
        /// </summary>
        void ToXml<TTarget>(Object o, FileInfo fileName);
    }
}