using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TraeToDo.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace TraeToDo
{
    public sealed partial class AIChatPage : Page
    {
        private const string MessagesKey = "SavedMessages";
        private readonly DeepSeekService _deepSeekService;
        private bool _isRequestInProgress = false;
        private DispatcherTimer _soloModeTimer;
        private int _lastQuestionIndex = 0;
        private readonly string[] _soloQuestions = new[]
        {
            "Please give more information",
            "Please extend your answer",
            "Please continue and tell me more",
        };

        public ObservableCollection<ChatMessage> Messages { get; set; } = new ObservableCollection<ChatMessage>();

        public AIChatPage()
        {
            this.InitializeComponent();
            _deepSeekService = new DeepSeekService();
            System.Diagnostics.Debug.WriteLine($"[AIChatPage] Конструктор: Messages={(Messages != null ? Messages.Count.ToString() : "null")}");
            //LoadMessages();
            
            UpdateSoloIndicator();
            if (SettingsManager.Instance.GetSoloModeEnabled())
            {
                InitializeSoloModeTimer();
            }
            //AddWelcomeMessage();
            CheckApiKeyConfiguration();
        }

        private void UpdateDebugInfo()
        {
            try
            {
                var debugInfo = new System.Text.StringBuilder();
                debugInfo.AppendLine($"Messages count: {Messages?.Count ?? 0}");

                if (Messages != null && Messages.Count > 0)
                {
                    debugInfo.AppendLine("Last message:");
                    var lastMessage = Messages.Last();
                    debugInfo.AppendLine($"- [{lastMessage.Timestamp:HH:mm:ss}] {(lastMessage.IsUserMessage ? "User" : "AI")}: {lastMessage.Content.Substring(0, Math.Min(50, lastMessage.Content.Length))}...");
                }

                debugInfo.AppendLine($"ListView ItemsSource: {(MessagesListView.ItemsSource != null ? "Set" : "NULL")}");
                debugInfo.AppendLine($"ListView Items count: {MessagesListView.Items?.Count ?? 0}");

                //DebugInfoText.Text = debugInfo.ToString();
                Debug.WriteLine(debugInfo.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIChatPage] UpdateDebugInfo: Exception: {ex.Message}");
            }            
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                System.Diagnostics.Debug.WriteLine("[AIChatPage] OnNavigatedTo: Starting...");
                UpdateDebugInfo();
                
                // Load messages from file
                var loadedMessages = await SettingsManager.Instance.LoadMessagesFromFileAsync();
                System.Diagnostics.Debug.WriteLine($"[AIChatPage] OnNavigatedTo: Loaded {loadedMessages?.Count ?? 0} messages from file");
                
                // Update the UI on the UI thread
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Messages.Clear();
                    if (loadedMessages != null)
                    {
                        foreach (var msg in loadedMessages)
                        {
                            Messages.Add(msg);
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"[AIChatPage] OnNavigatedTo: Added {Messages.Count} messages to UI collection");
                    
                    // Force UI update
                    MessagesListView.UpdateLayout();
                    if (Messages.Count > 0)
                    {
                        MessagesListView.ScrollIntoView(Messages[Messages.Count - 1]);
                    }
                    
                    MessageTextBox.Focus(FocusState.Programmatic);
                });
                
                // Initialize SOLO mode if needed
                UpdateSoloIndicator();
                if (SettingsManager.Instance.GetSoloModeEnabled())
                {
                    InitializeSoloModeTimer();
                }
                else if (_soloModeTimer != null)
                {
                    _soloModeTimer.Stop();
                    _soloModeTimer = null;
                }
                
                System.Diagnostics.Debug.WriteLine("[AIChatPage] OnNavigatedTo: Completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIChatPage] OnNavigatedTo ERROR: {ex}");
                // Initialize empty collection if loading fails
                Messages = new ObservableCollection<ChatMessage>();
            }
        }

        private void AddWelcomeMessage()
        {
            var welcomeMessage = new ChatMessage(
                "Hello! I'm just a computer program, so I don't have feelings, but I'm here and ready to help! How can I assist you today? 😊", 
                false);
            Messages.Add(welcomeMessage);
        }

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

        private void UpdateSoloIndicator()
        {
            bool soloOn = SettingsManager.Instance.GetSoloModeEnabled();
            if (SoloIndicatorIcon != null)
            {
                SoloIndicatorIcon.Foreground = soloOn ? new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.LimeGreen)
                                                       : new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Gray);
            }
            if (SoloIndicatorToolTip != null)
            {
                SoloIndicatorToolTip.Content = soloOn ? "SOLO mode is ON" : "SOLO mode is OFF";
            }
        }

        private void InitializeSoloModeTimer()
        {
            if (_soloModeTimer != null)
            {
                _soloModeTimer.Stop();
                _soloModeTimer.Tick -= SoloModeTimer_Tick;
            }
            int interval = SettingsManager.Instance.GetSoloModeInterval();
            if (interval <= 0) interval = 5;
            _soloModeTimer = new DispatcherTimer();
            _soloModeTimer.Tick += SoloModeTimer_Tick;
            _soloModeTimer.Interval = TimeSpan.FromMinutes(interval);
            _soloModeTimer.Start();
        }

        private async void SoloModeTimer_Tick(object sender, object e)
        {
            if (_isRequestInProgress) return;
            if (Messages.Count > 0 && _lastQuestionIndex < _soloQuestions.Length)
            {
                await SendMessage(_soloQuestions[_lastQuestionIndex]);
                _lastQuestionIndex = (_lastQuestionIndex + 1) % _soloQuestions.Length;
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage(MessageTextBox.Text);
        }

        private async void MessageTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && !string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                await SendMessage(MessageTextBox.Text);
            }
        }

        private async Task SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || _isRequestInProgress)
            {
                System.Diagnostics.Debug.WriteLine("[AIChatPage] SendMessage: Message is empty or request is already in progress");
                return;
            }

            try
            {
                _isRequestInProgress = true;
                LoadingIndicator.Visibility = Visibility.Visible;
                
                // Debug output
                System.Diagnostics.Debug.WriteLine($"[AIChatPage] SendMessage: Starting with message: {message}");
                System.Diagnostics.Debug.WriteLine($"[AIChatPage] Messages collection is null: {Messages == null}");
                
                // Create and add user message
                var userMessage = new ChatMessage(message.Trim(), true);
                System.Diagnostics.Debug.WriteLine($"[AIChatPage] Created user message. IsUserMessage: {userMessage.IsUserMessage}, Content: {userMessage.Content}");
                
                // Add to UI on UI thread
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    try
                    {
                        Messages.Add(userMessage);
                        MessageTextBox.Text = string.Empty;
                        MessagesListView.UpdateLayout();
                        MessagesListView.ScrollIntoView(userMessage);
                        System.Diagnostics.Debug.WriteLine($"[AIChatPage] Added user message to UI. Messages count: {Messages.Count}");
                        UpdateDebugInfo();
                        
                        // Save to file after UI update
                        await SettingsManager.Instance.SaveMessagesToFileAsync(Messages);
                        System.Diagnostics.Debug.WriteLine("[AIChatPage] Saved messages to file after user message");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AIChatPage] Error in UI update for user message: {ex}");
                        throw;
                    }
                });

                // Get AI response
                System.Diagnostics.Debug.WriteLine("[AIChatPage] Getting AI response...");
                var response = await _deepSeekService.GetResponseAsync(message, Messages.ToList());
                
                if (string.IsNullOrEmpty(response))
                {
                    System.Diagnostics.Debug.WriteLine("[AIChatPage] Received empty response from AI service");
                    response = "I'm sorry, I couldn't process your request. Please try again.";
                }
                
                var botMessage = new ChatMessage(response, false);
                System.Diagnostics.Debug.WriteLine($"[AIChatPage] Received AI response. Content length: {response?.Length ?? 0}");

                // Add AI response to UI on UI thread
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    try
                    {
                        Messages.Add(botMessage);
                        MessagesListView.UpdateLayout();
                        MessagesListView.ScrollIntoView(botMessage);
                        System.Diagnostics.Debug.WriteLine($"[AIChatPage] Added AI message to UI. Messages count: {Messages.Count}");
                        UpdateDebugInfo();
                        
                        // Save to file after UI update
                        await SettingsManager.Instance.SaveMessagesToFileAsync(Messages);
                        System.Diagnostics.Debug.WriteLine("[AIChatPage] Saved messages to file after AI message");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AIChatPage] Error in UI update for AI message: {ex}");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                var errorMessage = new ChatMessage($"Error: {ex.Message}", false);
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        Messages.Add(errorMessage);
                    });
                System.Diagnostics.Debug.WriteLine($"[AIChatPage] SendMessage: exception, Messages.Count={Messages?.Count ?? 0}");
            }
            finally
            {
                _isRequestInProgress = false;
                LoadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private async void Message_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is TraeToDo.Models.ChatMessage msg/* && !msg.IsUserMessage*/)
            {
                // Парсинг сообщения на пункты (1., 2., •, -, —, *, ●, ○, →, etc.)
                var lines = msg.Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var checklist = new List<string>();
                var bulletPatterns = new[] { "^- ", "^– ", "^— ", "^• ", "^\\* ", "^● ", "^○ ", "^→ ", "^\\d+[\\).] ", "^[a-zA-Z][\\).] " };
                var bulletRegex = new System.Text.RegularExpressions.Regex(string.Join("|", bulletPatterns), System.Text.RegularExpressions.RegexOptions.Compiled);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (bulletRegex.IsMatch(trimmed))
                    {
                        // Удаляем маркер
                        var text = bulletRegex.Replace(trimmed, "").Trim();
                        if (!string.IsNullOrWhiteSpace(text)) checklist.Add(text);
                    }
                }
                // Название задачи — первые 100 символов сообщения
                var taskTitle = msg.Content.Length > 100 ? msg.Content.Substring(0, 100) + "..." : msg.Content;
                var newTask = new TraeToDo.Models.TaskItem(taskTitle);
                foreach (var item in checklist)
                {
                    newTask.Subtasks.Add(new TraeToDo.Models.SubTaskItem(item));
                }
                // Добавляем задачу в TaskList (через SettingsManager)
                var tasks = await TraeToDo.Models.SettingsManager.Instance.LoadTasksFromFileAsync();
                tasks.Add(newTask);
                await TraeToDo.Models.SettingsManager.Instance.SaveTasksToFileAsync(tasks);
                if (checklist.Count > 0)
                    ShowStatusBar($"Добавлена задача с чек-листом: {checklist.Count} пунктов", true);
                else
                    ShowStatusBar($"Добавлена задача без чек-листа", true);

                await SettingsManager.Instance.SaveMessagesToFileAsync(Messages);
            }
        }

        private async void ShowStatusBar(string message, bool success)
        {
            StatusBar.Text = message;
            StatusBar.Foreground = success ? new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Green) : new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
            StatusBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            await System.Threading.Tasks.Task.Delay(2500);
            StatusBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

       


        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Навигация на SettingsPage с передачей информации о текущей странице
            Frame.Navigate(typeof(SettingsPage), "AIChatPage");
        }

        private void TaskListButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(TaskListPage));
        }
    }
}
