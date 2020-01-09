using System;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extracts the value with the specified key and converts it to type TConfig 
    /// </summary>
    public static class ConfigurationExtensions
    {
        public static TConfig GetConfigValue<TConfig>(this IConfiguration configuration, string key) where TConfig : class, new()
        {
            if(configuration == null)
            {
                throw new ArgumentException(nameof(configuration));
            }

            if(string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(nameof(key));
            }

            var configurationSection =  configuration.GetSection(key);

            if(configurationSection == null)
            {
                throw new ArgumentException($"{nameof(configurationSection)} could not be loaded.");
            }

            var configurationObject = new TConfig();
            configurationSection.Bind(configurationObject);
            return configurationObject;
        }
    }
}