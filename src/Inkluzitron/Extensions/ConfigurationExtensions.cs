using Microsoft.Extensions.Configuration;
using System;

namespace Inkluzitron.Extensions
{
    static public class ConfigurationExtensions
    {
        static public T GetRequired<T>(this IConfigurationSection configSection, string key)
        {
            var itemSection = configSection.GetSection(key);
            itemSection.AssertExists();
            return itemSection.Get<T>();
        }

        static public void AssertExists(this IConfigurationSection configSection)
        {
            if (!configSection.Exists())
                throw new InvalidOperationException($"Missing required configuration value with key {configSection.Path}");
        }
    }
}
