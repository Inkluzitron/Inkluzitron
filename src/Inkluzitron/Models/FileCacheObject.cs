using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Inkluzitron.Models
{
    public class FileCacheObject
    {
        private readonly DirectoryInfo _parentDir;
        private readonly string _fileName;
        private readonly string _deletionPattern;
        private readonly string _lookupPattern;

        protected FileCacheObject(DirectoryInfo parentDir, string fileName, string deletionPattern, string lookupPattern)
        {
            _parentDir = parentDir;
            _fileName = fileName;
            _deletionPattern = deletionPattern;
            _lookupPattern = lookupPattern;
        }

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

        public string GetPathForWriting(string extension)
        {
            RemoveLeftovers();
            return Path.Combine(_parentDir.FullName, $"{_fileName}.{extension}");
        }

        static public FileCacheObjectBuilder In(DirectoryInfo parentDir)
            => new FileCacheObjectBuilder(parentDir);

        public class FileCacheObjectBuilder
        {
            private readonly DirectoryInfo _parentDir;

            private readonly StringBuilder _paramBuilder = new();
            private readonly StringBuilder _uniqueBuilder = new();

            public FileCacheObjectBuilder(DirectoryInfo parentDir)
            {
                _parentDir = parentDir ?? throw new ArgumentNullException(nameof(parentDir));
            }

            public FileCacheObjectBuilder WithUnique(params object[] identifiers)
            {
                foreach (var id in identifiers)
                    WithConditionalUnique(id, true);

                return this;
            }

            public FileCacheObjectBuilder WithConditionalUnique(object id, bool condition)
            {
                if (condition)
                {
                    if (_uniqueBuilder.Length > 0)
                        _uniqueBuilder.Append('_');

                    _uniqueBuilder.Append(id);
                }

                return this;
            }

            public FileCacheObjectBuilder WithParam(params object[] identifiers)
            {
                foreach (var id in identifiers)
                {
                    _paramBuilder.Append('_');
                    _paramBuilder.Append(id);
                }

                return this;
            }

            public FileCacheObject Build()
            {
                if (_uniqueBuilder.Length == 0)
                    throw new InvalidOperationException("At least one unique parameter must be provided");

                var uniquePattern = _uniqueBuilder.ToString() + "*";
                _uniqueBuilder.Append(_paramBuilder);
                var lookupPattern = _uniqueBuilder.ToString() + "*";

                return new FileCacheObject(_parentDir, _uniqueBuilder.ToString(), uniquePattern, lookupPattern);
            }
        }
    }
}
