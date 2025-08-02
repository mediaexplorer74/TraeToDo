using System;
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
        public SettingsPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Loads the API key from settings when the page is navigated to
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Load the API key from settings
            string apiKey = SettingsManager.Instance.GetApiKey();
            if (!string.IsNullOrEmpty(apiKey))
            {
                ApiKeyPasswordBox.Password = apiKey;
            }
            
            // Load SOLO mode and interval settings
            SoloModeToggle.IsOn = SettingsManager.Instance.GetSoloModeEnabled();
            IntervalTextBox.Text = SettingsManager.Instance.GetSoloModeInterval().ToString();
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
    }
}