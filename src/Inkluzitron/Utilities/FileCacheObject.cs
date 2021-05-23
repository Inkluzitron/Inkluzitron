using System.IO;
using System.Linq;

namespace Inkluzitron.Utilities
{
    public class FileCacheObject
    {
        private readonly DirectoryInfo _parentDir;
        private readonly string _fileName;
        private readonly string _deletionPattern;
        private readonly string _lookupPattern;

        public FileCacheObject(DirectoryInfo parentDir, string fileName, string deletionPattern, string lookupPattern)
        {
            _parentDir = parentDir;
            _fileName = fileName;
            _deletionPattern = deletionPattern;
            _lookupPattern = lookupPattern;
        }

        /// <summary>
        /// Tries to find the file in cache.
        /// </summary>
        public bool TryFind(out string filePath)
        {
            filePath = _parentDir
                .EnumerateFiles(_lookupPattern, SearchOption.TopDirectoryOnly)
                .Select(f => f.FullName)
                .FirstOrDefault();

            return filePath != null;
        }

        private void RemoveLeftovers()
        {
            foreach (var leftoverFile in _parentDir.EnumerateFiles(_deletionPattern, SearchOption.TopDirectoryOnly))
            {
                try
                {
                    leftoverFile.Delete();
                }
                catch
                {
                    // An attempt was made.
                }
            }
        }

        /// <summary>
        /// Composes the appropriate file path for this cache object.
        /// This also removes all cache entries with the same unique identifiers.
        /// </summary>
        public string GetPathForWriting(string extension)
        {
            RemoveLeftovers();
            return Path.Combine(_parentDir.FullName, $"{_fileName}.{extension}");
        }
    }
}
