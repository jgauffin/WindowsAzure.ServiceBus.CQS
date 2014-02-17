using System.Configuration;

namespace WindowsAzure.ServiceBus.Cqs.Configuration
{
    /// <summary>
    /// Reads configuration settings from <c>ConfigurationManager</c> (i.e. web/app.config);
    /// </summary>
    public class AppConfigProvider : ISettingsProvider
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
            var value = ConfigurationManager.AppSettings["name"];
            if (value == null)
                throw new ConfigurationErrorsException(name + " was not found in config.");

            return value;
        }
    }
}
