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
        private const string SOLO_MODE_ENABLED = "SoloModeEnabled";
        private const string SOLO_MODE_INTERVAL = "SoloModeInterval";
        
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
            return !string.IsNullOrEmpty(GetApiKey());
        }
        
        /// <summary>
        /// Gets the SOLO mode enabled state
        /// </summary>
        /// <returns>True if SOLO mode is enabled</returns>
        public bool GetSoloModeEnabled()
        {
            if (_localSettings.Values.ContainsKey(SOLO_MODE_ENABLED))
            {
                return (bool)_localSettings.Values[SOLO_MODE_ENABLED];
            }
            return false;
        }
        
        /// <summary>
        /// Saves the SOLO mode enabled state
        /// </summary>
        /// <param name="enabled">Whether SOLO mode is enabled</param>
        public void SaveSoloModeEnabled(bool enabled)
        {
            _localSettings.Values[SOLO_MODE_ENABLED] = enabled;
        }
        
        /// <summary>
        /// Gets the SOLO mode interval in minutes
        /// </summary>
        /// <returns>The interval in minutes</returns>
        public int GetSoloModeInterval()
        {
            if (_localSettings.Values.ContainsKey(SOLO_MODE_INTERVAL))
            {
                return (int)_localSettings.Values[SOLO_MODE_INTERVAL];
            }
            return 5; // Default interval
        }
        
        /// <summary>
        /// Saves the SOLO mode interval in minutes
        /// </summary>
        /// <param name="interval">The interval in minutes</param>
        public void SaveSoloModeInterval(int interval)
        {
            _localSettings.Values[SOLO_MODE_INTERVAL] = interval;
        }
    }
}