using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Das.Serializer.Concurrency;

namespace Das.Serializer
{
    /// <summary>
    ///     Creates a system wide mutex on a string based on the file's name
    /// </summary>
    public class SafeFile : IDisposable
    {
        static SafeFile()
        {
            _staScheduler = new StaScheduler("SafeFile synchronization");
        }

        public SafeFile(FileInfo fi)
        {
            if (fi.DirectoryName == null)
                throw new InvalidDataException("Path of specified file is not valid");

            if (!Directory.Exists(fi.DirectoryName))
                Directory.CreateDirectory(fi.DirectoryName);

            _protector = new Mutex(false, fi.FullName.Replace(
                Path.DirectorySeparatorChar, '_'));

            _staScheduler.Invoke(() => _protector.WaitOne());

            //var doth = _protector.WaitOne();
        }

        public void Dispose()
        {
            _staScheduler.Invoke(() => _protector.ReleaseMutex());
            //_protector.ReleaseMutex();
        }

        private static readonly StaScheduler _staScheduler;

        private readonly Mutex _protector;
    }
}
