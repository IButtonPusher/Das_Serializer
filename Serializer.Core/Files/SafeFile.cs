using System;
using System.IO;
using System.Threading;

namespace Serializer.Core.Files
{
    public class SafeFile : IDisposable
    {
        private readonly Mutex _protector;

        public SafeFile(FileInfo fi)
        {
            if (fi.DirectoryName == null || !Uri.IsWellFormedUriString(
                    fi.FullName, UriKind.RelativeOrAbsolute))
                throw new InvalidDataException("Path of specified file is not valid");

            if (!Directory.Exists(fi.DirectoryName))
                Directory.CreateDirectory(fi.DirectoryName);

            _protector=  new Mutex(false, fi.FullName.Replace(
                Path.DirectorySeparatorChar, '_'));

            _protector.WaitOne();
        }

        public void Dispose()
        {
            _protector.ReleaseMutex();
        }
    }
}
