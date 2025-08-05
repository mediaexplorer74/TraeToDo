using System;
using System.Linq;
using System.Threading.Tasks;
using TraeToDo.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace TraeToDo
{
    /// <summary>
    /// Settings page for configuring the application
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private string _previousPage = null;
        private const string MessagesKey = "SavedMessages";
        private const string TasksKey = "SavedTasks";

        public SettingsPage()
        {
            this.InitializeComponent();
        }

        private async void ClearChatButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Clear chat messages from file storage
                await SettingsManager.Instance.SaveMessagesToFileAsync(new System.Collections.ObjectModel.ObservableCollection<ChatMessage>());
                ShowStatus("Chat history cleared", true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Error clearing chat: {ex}");
                ShowStatus("Error clearing chat history", false);
            }
        }

        private async void ClearTasksButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Clear tasks from file storage
                await SettingsManager.Instance.SaveTasksToFileAsync(new System.Collections.ObjectModel.ObservableCollection<TaskItem>());
                ShowStatus("Tasks cleared", true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsPage] Error clearing tasks: {ex}");
                ShowStatus("Error clearing tasks", false);
            }
        }

        /// <summary>
        /// Loads the API key from settings when the page is navigated to
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Синхронизация состояния переключателя HideCompletedTasks
            if (HideCompletedTasksToggle != null)
                HideCompletedTasksToggle.IsOn = SettingsManager.Instance.GetHideCompletedTasks();
            base.OnNavigatedTo(e);
            // Сохраняем параметр навигации (название предыдущей страницы)
            if (e.Parameter is string prevPage)
            {
                _previousPage = prevPage;
            }
            
            // Load the API key from settings
            string apiKey = SettingsManager.Instance.GetApiKey();
            if (!string.IsNullOrEmpty(apiKey))
            {
                ApiKeyPasswordBox.Password = apiKey;
            }
            
            // Load SOLO mode and interval settings
            SoloModeToggle.IsOn = SettingsManager.Instance.GetSoloModeEnabled();
            IntervalTextBox.Text = SettingsManager.Instance.GetSoloModeInterval().ToString();
            
            // Load the start page setting
            string startPage = SettingsManager.Instance.GetStartPage();
            if (startPage == "TaskList")
            {
                StartPageComboBox.SelectedIndex = 1;
            }
            else
            {
                StartPageComboBox.SelectedIndex = 0; // Default to AI Chat
            }
        }

        /// <summary>
        /// Handles the back button click to navigate back to the main page
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
            else if (!string.IsNullOrEmpty(_previousPage))
            {
                if (_previousPage == "AIChatPage")
                {
                    Frame.Navigate(typeof(AIChatPage));
                }
                else if (_previousPage == "TaskListPage")
                {
                    Frame.Navigate(typeof(TaskListPage));
                }
            }
        }

        /// <summary>
        /// Saves the API key and settings when the save button is clicked
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = ApiKeyPasswordBox.Password.Trim();
            
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ShowStatus("Please enter a valid API key", false);
                return;
            }
            
            // Save all settings
            SettingsManager.Instance.SaveApiKey(apiKey);
            SettingsManager.Instance.SaveSoloModeEnabled(SoloModeToggle.IsOn);
            
            // Save the selected start page
            if (StartPageComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string startPage)
            {
                SettingsManager.Instance.SaveStartPage(startPage);
            }
            
            if (int.TryParse(IntervalTextBox.Text, out int interval) && interval > 0)
            {
                SettingsManager.Instance.SaveSoloModeInterval(interval);
                ShowStatus("Settings saved successfully", true);
            }
            else
            {
                ShowStatus("Interval must be a positive number", false);
            }
        }
        
        /// <summary>
        /// Shows a status message to the user
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="isSuccess">Whether the message indicates success</param>
        private void ShowStatus(string message, bool isSuccess)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = isSuccess ? 
                new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Green) : 
                new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
            StatusTextBlock.Visibility = Visibility.Visible;
        }
        private void HideCompletedTasksToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (HideCompletedTasksToggle != null)
            {
                SettingsManager.Instance.SetHideCompletedTasks(HideCompletedTasksToggle.IsOn);
            }
        }
    }
}