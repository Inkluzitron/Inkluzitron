using System;
using System.IO;

namespace Inkluzitron.Utilities
{
    public sealed class TemporaryFile : IDisposable
    {
        public string Path { get; }

        public TemporaryFile(string extension)
        {
            if (extension is null)
                throw new ArgumentNullException(nameof(extension));

            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName() + "." + extension);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            try
            {
                File.Delete(Path);
            }
            catch
            {
                // An attempt was made.
            }
        }
    }
}
