using System;
using System.Threading.Tasks;
using Windows.Storage;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

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
        private const string START_PAGE = "StartPage";

        // Singleton instance
        private static SettingsManager _instance;

        private const string HIDE_COMPLETED_TASKS = "HideCompletedTasks";

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
            return 5; // Default to 5 minutes
        }

        /// <summary>
        /// Saves the SOLO mode interval in minutes
        /// </summary>
        /// <param name="minutes">The interval in minutes</param>
        public void SaveSoloModeInterval(int minutes)
        {
            _localSettings.Values[SOLO_MODE_INTERVAL] = minutes;
        }

        /// <summary>
        /// Gets the start page setting
        /// </summary>
        /// <returns>The start page ("AIChat" or "TaskList")</returns>
        public string GetStartPage()
        {
            if (_localSettings.Values.ContainsKey(START_PAGE))
            {
                return _localSettings.Values[START_PAGE] as string;
            }
            return "AIChat"; // Default to AIChat
        }

        /// <summary>
        /// Saves the start page setting
        /// </summary>
        /// <param name="startPage">The start page ("AIChat" or "TaskList")</param>
        public void SaveStartPage(string startPage)
        {
            if (startPage == "AIChat" || startPage == "TaskList")
            {
                _localSettings.Values[START_PAGE] = startPage;
            }
        }

        /// <summary>
        /// Loads a value from local settings
        /// </summary>
        /// <typeparam name="T">The type of the value to load</typeparam>
        /// <param name="key">The key of the value to load</param>
        /// <returns>The loaded value, or null if not found</returns>
        public T Load<T>(string key)
        {
            if (_localSettings.Values.ContainsKey(key))
            {
                var value = _localSettings.Values[key];
                if (typeof(T) == typeof(ObservableCollection<ChatMessage>) || typeof(T) == typeof(ObservableCollection<TaskItem>))
                {
                    if (value is string json && !string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            return JsonConvert.DeserializeObject<T>(json);
                        }
                        catch (JsonException)
                        {
                            // Старый формат или поврежденные данные — очищаем ключ и возвращаем пустую коллекцию
                            _localSettings.Values.Remove(key);
                            if (typeof(T) == typeof(ObservableCollection<ChatMessage>))
                                return (T)(object)new ObservableCollection<ChatMessage>();
                            if (typeof(T) == typeof(ObservableCollection<TaskItem>))
                                return (T)(object)new ObservableCollection<TaskItem>();
                        }
                    }
                    return default(T);
                }
                try
                {
                    return (T)value;
                }
                catch { }
            }
            return default(T);
        }

        public async System.Threading.Tasks.Task SaveMessagesToFileAsync(System.Collections.ObjectModel.ObservableCollection<ChatMessage> messages)
        {
            string json = JsonConvert.SerializeObject(messages);
            var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync("traetodo_chat.json", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(file, json);
        }

        public async System.Threading.Tasks.Task<System.Collections.ObjectModel.ObservableCollection<ChatMessage>> LoadMessagesFromFileAsync()
        {
            try
            {
                var file = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync("traetodo_chat.json");
                string json = await Windows.Storage.FileIO.ReadTextAsync(file);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    return JsonConvert.DeserializeObject<System.Collections.ObjectModel.ObservableCollection<ChatMessage>>(json) ?? new System.Collections.ObjectModel.ObservableCollection<ChatMessage>();
                }
            }
            catch { }
            return new System.Collections.ObjectModel.ObservableCollection<ChatMessage>();
        }

        public async Task SaveTasksToFileAsync(ObservableCollection<TaskItem> tasks)
        {
            string json = JsonConvert.SerializeObject(tasks);
            var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync("traetodo_data.json", Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteTextAsync(file, json);
        }

        public async Task<ObservableCollection<TaskItem>> LoadTasksFromFileAsync()
        {
            try
            {
                var file = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync("traetodo_data.json");
                string json = await Windows.Storage.FileIO.ReadTextAsync(file);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    return JsonConvert.DeserializeObject<ObservableCollection<TaskItem>>(json) ?? new ObservableCollection<TaskItem>();
                }
            }
            catch { }
            return new ObservableCollection<TaskItem>();
        }

        /// <summary>
        /// Gets the Hide Completed Tasks setting
        /// </summary>
        /// <returns>True if completed tasks should be hidden</returns>
        public bool GetHideCompletedTasks()
        {
            if (_localSettings.Values.ContainsKey(HIDE_COMPLETED_TASKS))
            {
                return _localSettings.Values[HIDE_COMPLETED_TASKS] is bool b && b;
            }
            return false;
        }

        /// <summary>
        /// Sets the Hide Completed Tasks setting
        /// </summary>
        /// <param name="hide">True to hide completed tasks</param>
        public void SetHideCompletedTasks(bool hide)
        {
            _localSettings.Values[HIDE_COMPLETED_TASKS] = hide;
        }
    }
}