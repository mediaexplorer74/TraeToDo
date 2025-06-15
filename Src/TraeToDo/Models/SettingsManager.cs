using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace TraeToDo.Models
{
    /// <summary>
    /// Manages application settings and secure storage of API keys
    /// </summary>
    public class SettingsManager
    {
        // Constants for settings keys
        private const string API_KEY_SETTING = "ApiKey";
        
        // Singleton instance
        private static SettingsManager _instance;
        
        // Local settings container
        private readonly ApplicationDataContainer _localSettings;
        
        /// <summary>
        /// Gets the singleton instance of the SettingsManager
        /// </summary>
        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SettingsManager();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private SettingsManager()
        {
            _localSettings = ApplicationData.Current.LocalSettings;
        }
        
        /// <summary>
        /// Gets the stored API key
        /// </summary>
        /// <returns>The API key or empty string if not set</returns>
        public string GetApiKey()
        {
            if (_localSettings.Values.ContainsKey(API_KEY_SETTING))
            {
                return _localSettings.Values[API_KEY_SETTING] as string;
            }
            return string.Empty;
        }
        
        /// <summary>
        /// Saves the API key to local settings
        /// </summary>
        /// <param name="apiKey">The API key to save</param>
        public void SaveApiKey(string apiKey)
        {
            _localSettings.Values[API_KEY_SETTING] = apiKey;
        }
        
        /// <summary>
        /// Checks if the API key is configured
        /// </summary>
        /// <returns>True if the API key is set, otherwise false</returns>
        public bool IsApiKeyConfigured()
        {
            return !string.IsNullOrWhiteSpace(GetApiKey());
        }
    }
}