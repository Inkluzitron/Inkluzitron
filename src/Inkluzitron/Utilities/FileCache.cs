using System;
using System.Collections.Concurrent;
using System.IO;

namespace Inkluzitron.Utilities
{
    public class FileCache
    {
        private DirectoryInfo RootDirectory { get; }
        private ConcurrentDictionary<string, DirectoryInfo> Subdirectories { get; } = new();

        public FileCache(string directoryPath)
        {
            if (directoryPath == null)
                throw new ArgumentNullException(nameof(directoryPath));

            RootDirectory = new DirectoryInfo(directoryPath);
        }

        private DirectoryInfo GetSubdirectory(string subdirectoryName)
            => Subdirectories.GetOrAdd(subdirectoryName, RootDirectory.CreateSubdirectory);

        public FileCacheObjectBuilder WithCategory(string categoryName)
        {
            if (categoryName is null)
                throw new ArgumentNullException(nameof(categoryName));

            return new FileCacheObjectBuilder(GetSubdirectory(categoryName));
        }
    }
}
