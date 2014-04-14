using System.Configuration;

namespace WindowsAzure.ServiceBus.Cqs.Configuration
{
    /// <summary>
    /// Do abstract away where the settings are stored.
    /// </summary>
    public interface ISettingsProvider
    {
        /// <summary>
        /// Get an application setting
        /// </summary>
        /// <param name="name">Name of the configuration setting</param>
        /// <returns>Name</returns>
        /// <exception cref="ConfigurationErrorsException">The specified setting was not found.</exception>
        string GetAppSetting(string name);
    }
}