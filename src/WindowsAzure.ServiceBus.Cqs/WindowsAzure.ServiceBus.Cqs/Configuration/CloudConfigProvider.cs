using System.Configuration;
using Microsoft.WindowsAzure;

namespace WindowsAzure.ServiceBus.Cqs.Configuration
{
    /// <summary>
    /// Uses <c>CloudConfigurationManager</c>.
    /// </summary>
    public class CloudConfigProvider : ISettingsProvider
    {
        /// <summary>
        /// Get an application setting
        /// </summary>
        /// <param name="name">Name of the configuration setting</param>
        /// <returns>
        /// Name
        /// </returns>
        /// <exception cref="System.Configuration.ConfigurationErrorsException"></exception>
        public string GetAppSetting(string name)
        {
            var value= CloudConfigurationManager.GetSetting(name);
            if (value == null)
                throw new ConfigurationErrorsException(name + " as not found in config.");

            return value;
        }
    }
}