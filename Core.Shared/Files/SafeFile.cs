using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Das.Serializer.Files
{
    public class SafeFile : IDisposable
    {
        public SafeFile(FileInfo fi)
        {
            if (fi.DirectoryName == null)
                throw new InvalidDataException("Path of specified file is not valid");

            if (!Directory.Exists(fi.DirectoryName))
                Directory.CreateDirectory(fi.DirectoryName);

            _protector = new Mutex(false, fi.FullName.Replace(
                Path.DirectorySeparatorChar, '_'));

            _protector.WaitOne();
        }

        public void Dispose()
        {
            _protector.ReleaseMutex();
        }

        private readonly Mutex _protector;
    }
}