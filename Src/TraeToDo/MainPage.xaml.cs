using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TraeToDo.Models;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace TraeToDo
{
    /// <summary>
    /// Main page with chat interface
    /// </summary>
    public sealed partial class MainPage : Page
{
    public static MainPage Current { get; private set; }
    
    private const string MessagesKey = "SavedMessages";
    private const string TasksKey = "SavedTasks";

    // Current view mode (chat or tasks)
    private bool _isTaskView = false;
        
    // Collection of chat messages
    public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();
        
    // Collection of tasks
    public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        
    // Service for communicating with DeepSeek API
    private readonly DeepSeekService _deepSeekService;
        
    // Flag to track if a request is in progress
    private bool _isRequestInProgress = false;
        
    // Timer for automatic SOLO mode questions
    private DispatcherTimer _soloModeTimer;
    private int _lastQuestionIndex = 0;
    private readonly string[] _soloQuestions = new[]
    {
        "What task should I focus on right now?",
        "What's something small I can accomplish in 15 minutes?",
        "What's one task I've been procrastinating on?"
    };
        
    /// <summary>
    /// Initializes the main page
    /// </summary>
    public MainPage()
    {
        Current = this;
        this.InitializeComponent();
        
        // Load saved data
        LoadMessages();
        LoadTasks();
            
            // Initialize the DeepSeek service
            _deepSeekService = new DeepSeekService();
            
            // Initialize SOLO mode timer if enabled
            if (SettingsManager.Instance.GetSoloModeEnabled())
            {
                InitializeSoloModeTimer();
            }
            
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
        /// Handles the menu button click to show navigation options
        /// </summary>
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = new MenuFlyout();
            
            var chatItem = new MenuFlyoutItem { Text = "ChatToAI" };
            chatItem.Click += (s, args) => 
            {
                _isTaskView = false;
                MessagesListView.Visibility = Visibility.Visible;
                TaskListView.Visibility = Visibility.Collapsed;
                LoadingIndicator.Visibility = Visibility.Collapsed; // Ensure loading indicator is hidden when switching to chat
            };
            
            var taskItem = new MenuFlyoutItem { Text = "Task List" };
            taskItem.Click += (s, args) => 
            { 
                _isTaskView = true;
                MessagesListView.Visibility = Visibility.Collapsed;
                TaskListView.Visibility = Visibility.Visible;
                LoadingIndicator.Visibility = Visibility.Collapsed; // Hide loading indicator when switching to task view
            };
            
            menu.Items.Add(chatItem);
            menu.Items.Add(taskItem);
            
            menu.ShowAt(MenuButton);
        }


      
        /// <summary>
        /// Handles tapping on chat messages to add them as tasks
        /// </summary>
        private async void Message_ItemClick(object sender, ItemClickEventArgs e)
        {
          var message = e.ClickedItem as ChatMessage;
          if (message != null)
          {
            // Split response by newlines or numbered/bullet points
            var content = message.Content;


                //var tasksText = content.Split(new[] { "\n", "\r", "•", "-", "1.", "2.", "3." }, 
                //    StringSplitOptions.RemoveEmptyEntries);
                var tasksText = content.Split(new[] { 
                    "•", "-", 
                    "1.", "2.", "3.",
                    "4.", "5.", "6.",
                    "7.", "8.", "9.",
                },
                   StringSplitOptions.RemoveEmptyEntries);

                foreach (var taskText in tasksText)
            {
                var cleanText = taskText.Trim();
                if (!string.IsNullOrWhiteSpace(cleanText))
                {
                    var task = new TaskItem(cleanText);
                    Tasks.Add(task);
                
                    if (_isTaskView)
                    {
                        TaskListView.ScrollIntoView(task);
                    }
                }
            }

            SaveTasks();

            if (!_isTaskView)
            {
                var dialog = new MessageDialog($"{tasksText.Length} tasks added to your list!");
                await dialog.ShowAsync();
            }
          }
        }

        /// <summary>
        /// Handles the send button click
        /// </summary>
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();

            SaveMessages();
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

                SaveMessages();
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
                string response = await _deepSeekService.SendMessageAsync(messageText, 
                    new System.Collections.Generic.List<ChatMessage>(Messages));
                
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

                SaveMessages();
            }
        }
        
        /// <summary>
        /// Navigates to the settings page when the settings button is clicked
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }

        public void SaveMessages()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[MessagesKey] = string.Join("<|message|>", Messages.Select(m => $"{m.Content}<|isuser|>{m.IsUserMessage}"));           
        }


        public void SaveTasks()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[TasksKey] = string.Join("<|task|>", Tasks.Select(t => t.Description));
        }

        private void LoadMessages()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(MessagesKey))
            {
                var saved = localSettings.Values[MessagesKey] as string;
                Messages.Clear();
                foreach (var msg in saved.Split(new[] { "<|message|>" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = msg.Split(new[] { "<|isuser|>" }, StringSplitOptions.None);
                    if (parts.Length == 2 && bool.TryParse(parts[1], out bool isUser))
                        Messages.Add(new ChatMessage(parts[0], isUser));
                }
            }
        }
        
        private void LoadTasks()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(TasksKey))
            {
                var saved = localSettings.Values[TasksKey] as string;
                Tasks.Clear();
                foreach (var task in saved.Split(new[] { "<|task|>" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    Tasks.Add(new TaskItem(task));
                }
            }
        }
        
private async void SoloModeTimer_Tick(object sender, object e)
{
    if (Messages.Count > 0 && _isTaskView) 
        return;
        
    string question;
    
    if (Messages.Count == 1) {
        // First SOLO question
        _lastQuestionIndex = (_lastQuestionIndex + 1) % _soloQuestions.Length;
        question = _soloQuestions[_lastQuestionIndex];
    }
    else {
        // Use last AI response to generate new question
        var lastResponse = Messages.Last(m => !m.IsUserMessage);
        question = $"Дай более развернутый ответ: {lastResponse.Content}";
    }
    
    // Add as user message
    var userMessage = new ChatMessage(question, true);
    Messages.Add(userMessage);
    
    // Get AI response
    _isRequestInProgress = true;
    try
    {
        var response = await _deepSeekService.SendMessageAsync(question, new System.Collections.Generic.List<ChatMessage>(Messages));
        var aiMessage = new ChatMessage(response, false);
        Messages.Add(aiMessage);
        MessagesListView.ScrollIntoView(aiMessage);
            }
            catch (Exception ex)
            {
                var errorMessage = new ChatMessage($"An error occurred: {ex.Message}", false);
                Messages.Add(errorMessage);
                MessagesListView.ScrollIntoView(errorMessage);
            }
            finally
            {
                _isRequestInProgress = false;
            }
        }
        
        private void InitializeSoloModeTimer()
        {
            _soloModeTimer = new DispatcherTimer();
            _soloModeTimer.Tick += SoloModeTimer_Tick;
            _soloModeTimer.Interval = TimeSpan.FromMinutes(SettingsManager.Instance.GetSoloModeInterval());
            _soloModeTimer.Start();
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // Update timer if settings changed while away
            if (_soloModeTimer != null)
            {
                _soloModeTimer.Stop();
                _soloModeTimer.Interval = TimeSpan.FromMinutes(SettingsManager.Instance.GetSoloModeInterval());
                _soloModeTimer.Start();
            }
            else if (SettingsManager.Instance.GetSoloModeEnabled())
            {
                InitializeSoloModeTimer();
            }
        }
    }
}