using Microsoft.Extensions.Configuration;
using Inkluzitron.Extensions;

namespace Inkluzitron.Models.Settings
{
    public class ImageCacheSettings
    {
        public const string SectionName = "ImageCache";

        public ImageCacheSettings(IConfiguration config)
        {
            var section = config.GetSection(SectionName);
            section.AssertExists();

            DirectoryPath = section.GetRequired<string>(nameof(DirectoryPath));
        }

        public string DirectoryPath { get; }
    }
}
