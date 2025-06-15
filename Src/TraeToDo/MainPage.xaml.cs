using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TraeToDo.Models;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace TraeToDo
{
    /// <summary>
    /// Main page with chat interface
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Collection of chat messages
        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();
        
        // Service for communicating with DeepSeek API
        private readonly DeepSeekService _deepSeekService;
        
        // Flag to track if a request is in progress
        private bool _isRequestInProgress = false;
        
        /// <summary>
        /// Initializes the main page
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            
            // Initialize the DeepSeek service
            _deepSeekService = new DeepSeekService();
            
            // Add welcome message
            AddWelcomeMessage();
            
            // Check if API key is configured
            CheckApiKeyConfiguration();
        }
        
        /// <summary>
        /// Adds a welcome message to the chat
        /// </summary>
        private void AddWelcomeMessage()
        {
            var welcomeMessage = new ChatMessage(
                "Hello! I'm just a computer program, so I don't have feelings, but I'm here and ready to help! How can I assist you today? 😊", 
                false);
            Messages.Add(welcomeMessage);
        }
        
        /// <summary>
        /// Checks if the API key is configured and shows a message if not
        /// </summary>
        private void CheckApiKeyConfiguration()
        {
            if (!SettingsManager.Instance.IsApiKeyConfigured())
            {
                var apiKeyMessage = new ChatMessage(
                    "Please set your OpenRouter API key in the settings to use the DeepSeek AI model. Click the settings icon in the top right corner.", 
                    false);
                Messages.Add(apiKeyMessage);
            }
        }
        
        /// <summary>
        /// Handles the send button click
        /// </summary>
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }
        
        /// <summary>
        /// Handles key presses in the message text box
        /// </summary>
        private async void MessageTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)// && !e.IsKeyReleased)
            {
                e.Handled = true;
                await SendMessageAsync();
            }
        }
        
        /// <summary>
        /// Sends the user's message to the DeepSeek API and displays the response
        /// </summary>
        private async Task SendMessageAsync()
        {
            // Get the message text
            string messageText = MessageTextBox.Text.Trim();
            
            // Check if the message is empty or a request is already in progress
            if (string.IsNullOrWhiteSpace(messageText) || _isRequestInProgress)
            {
                return;
            }
            
            try
            {
                // Set the request in progress flag
                _isRequestInProgress = true;
                
                // Add the user's message to the chat
                var userMessage = new ChatMessage(messageText, true);
                Messages.Add(userMessage);
                
                // Clear the message text box
                MessageTextBox.Text = string.Empty;
                
                // Scroll to the bottom of the list
                MessagesListView.ScrollIntoView(userMessage);
                
                // Show the loading indicator
                LoadingIndicator.Visibility = Visibility.Visible;
                
                // Send the message to the DeepSeek API
                string response = await _deepSeekService.SendMessageAsync(messageText, new System.Collections.Generic.List<ChatMessage>(Messages));
                
                // Add the AI's response to the chat
                var aiMessage = new ChatMessage(response, false);
                Messages.Add(aiMessage);
                
                // Scroll to the bottom of the list
                MessagesListView.ScrollIntoView(aiMessage);
            }
            catch (Exception ex)
            {
                // Add an error message to the chat
                var errorMessage = new ChatMessage($"An error occurred: {ex.Message}", false);
                Messages.Add(errorMessage);
                
                // Scroll to the bottom of the list
                MessagesListView.ScrollIntoView(errorMessage);
            }
            finally
            {
                // Hide the loading indicator
                LoadingIndicator.Visibility = Visibility.Collapsed;
                
                // Reset the request in progress flag
                _isRequestInProgress = false;
            }
        }
        
        /// <summary>
        /// Navigates to the settings page when the settings button is clicked
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }
    }
}