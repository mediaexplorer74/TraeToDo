using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TraeToDo.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace TraeToDo
{
    public sealed partial class TaskListPage : Page
    {
        // Для хранения состояния раскрытия чек-листов (по TaskItem)
        private Dictionary<TaskItem, bool> _expandedStates = new Dictionary<TaskItem, bool>();
        private const string TasksKey = "SavedTasks";
        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<TaskItem> FilteredTasks { get; } = new ObservableCollection<TaskItem>();

        public TaskListPage()
        {
            this.InitializeComponent();
            System.Diagnostics.Debug.WriteLine($"[TaskListPage] Конструктор: Tasks.Count={Tasks.Count}, FilteredTasks.Count={FilteredTasks.Count}");
            _ = LoadTasksAsync();
            Tasks.CollectionChanged += Tasks_CollectionChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ApplyFilter();
            TaskInputBox.Focus(FocusState.Programmatic);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage), "TaskListPage");
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            AddTask();
        }

        private void TaskInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && !string.IsNullOrWhiteSpace(TaskInputBox.Text))
            {
                AddTaskButton_Click(sender, e);
            }
        }

        private async void AddTask()
        {
            if (string.IsNullOrWhiteSpace(TaskInputBox.Text)) return;

            try
            {
                var task = new TaskItem
                {
                    Description = TaskInputBox.Text.Trim(),
                    CreatedAt = DateTime.Now,
                    IsCompleted = false
                };

                Tasks.Add(task);
                TaskInputBox.Text = string.Empty;
                await SaveTasksAsync();
                ShowStatus("Task added successfully!");
                
                // Auto-scroll to the new task
                if (TaskListView.Items.Count > 0)
                {
                    var lastItem = TaskListView.Items[TaskListView.Items.Count - 1];
                    TaskListView.ScrollIntoView(lastItem);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Error adding task: {ex.Message}", true);
            }
        }

        private async System.Threading.Tasks.Task LoadTasksAsync()
        {
            System.Diagnostics.Debug.WriteLine("[TaskListPage] LoadTasksAsync: start");
            var savedTasks = await SettingsManager.Instance.LoadTasksFromFileAsync();
            System.Diagnostics.Debug.WriteLine($"[TaskListPage] LoadTasksAsync: loaded {savedTasks?.Count ?? 0} tasks from file");
            if (savedTasks != null)
            {
                foreach (var task in savedTasks)
                {
                    Tasks.Add(task);
                }
            }
            System.Diagnostics.Debug.WriteLine($"[TaskListPage] LoadTasksAsync: Tasks.Count after load = {Tasks.Count}");
            foreach (var task in Tasks)
            {
                task.PropertyChanged += Task_PropertyChanged;
            }
            ApplyFilter();
            System.Diagnostics.Debug.WriteLine($"[TaskListPage] LoadTasksAsync: FilteredTasks.Count after ApplyFilter = {FilteredTasks.Count}");
        }

        private async System.Threading.Tasks.Task SaveTasksAsync()
        {
            await SettingsManager.Instance.SaveTasksToFileAsync(Tasks);
        }

        private void ShowStatus(string message, bool isError = false)
        {
            try
            {
                Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    StatusBar.Visibility = Visibility.Visible;
                    StatusBar.Background = isError ? 
                        new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 235, 238)) : // Light Red
                        new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 227, 242, 253));  // Light Blue
                    
                    var statusText = StatusBar.FindName("StatusText") as TextBlock;
                    if (statusText != null)
                    {
                        statusText.Text = message;
                        statusText.Foreground = isError ? 
                            new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 198, 40, 40)) : // Dark Red
                            new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 13, 71, 161));  // Dark Blue
                    }

                    // Hide status after 3 seconds
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                    timer.Tick += (s, e) =>
                    {
                        StatusBar.Visibility = Visibility.Collapsed;
                        timer.Stop();
                    };
                    timer.Start();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskListPage] Error in ShowStatus: {ex}");
            }
        }

        private void TaskItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                if (e.OriginalSource is TextBlock || sender is Grid)
                {
                    var grid = sender as Grid;
                    if (grid?.DataContext is TaskItem task)
                    {
                        ToggleTaskCompletion(task);
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskListPage] Error in TaskItem_Tapped: {ex}");
            }
        }

        private void SubtaskItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                if (e.OriginalSource is TextBlock || sender is StackPanel)
                {
                    var panel = sender as StackPanel;
                    if (panel?.DataContext is SubTaskItem subtask)
                    {
                        ToggleSubtaskCompletion(subtask);
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskListPage] Error in SubtaskItem_Tapped: {ex}");
            }
        }

        private async void ToggleTaskCompletion(TaskItem task)
        {
            try
            {
                bool newState = !task.IsCompleted;
                task.IsCompleted = newState;
                task.CompletedAt = newState ? DateTime.Now : (DateTime?)null;
                
                // Update all subtasks to match the parent task's state
                foreach (var subtask in task.Subtasks)
                {
                    subtask.IsCompleted = newState;
                }

                await SaveTasksAsync();
                ShowStatus(newState ? "Task completed!" : "Task marked as incomplete");

                // Apply filter if needed
                bool hideCompleted = SettingsManager.Instance.GetHideCompletedTasks();
                if (hideCompleted && newState)
                {
                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskListPage] Error in ToggleTaskCompletion: {ex}");
                ShowStatus("Error updating task status", true);
            }
        }

        private async void ToggleSubtaskCompletion(SubTaskItem subtask)
        {
            try
            {
                subtask.IsCompleted = !subtask.IsCompleted;
                await SaveTasksAsync();
                
                // Check if all subtasks are completed to update parent task
                if (subtask.ParentTask != null)
                {
                    bool allSubtasksCompleted = subtask.ParentTask.Subtasks.All(st => st.IsCompleted);
                    if (subtask.ParentTask.IsCompleted != allSubtasksCompleted)
                    {
                        subtask.ParentTask.IsCompleted = allSubtasksCompleted;
                        subtask.ParentTask.CompletedAt = allSubtasksCompleted ? DateTime.Now : (DateTime?)null;
                        await SaveTasksAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaskListPage] Error in ToggleSubtaskCompletion: {ex}");
                ShowStatus("Error updating subtask status", true);
            }
        }

        private void AIChatButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AIChatPage));
        }

        private void Tasks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[TaskListPage] Tasks_CollectionChanged: Tasks.Count={Tasks.Count}, FilteredTasks.Count={FilteredTasks.Count}");
            if (e.NewItems != null)
            {
                foreach (TaskItem item in e.NewItems)
                    item.PropertyChanged += Task_PropertyChanged;
            }
            if (e.OldItems != null)
            {
                foreach (TaskItem item in e.OldItems)
                    item.PropertyChanged -= Task_PropertyChanged;
            }
            ApplyFilter();
            System.Diagnostics.Debug.WriteLine($"[TaskListPage] Tasks_CollectionChanged: after ApplyFilter: FilteredTasks.Count={FilteredTasks.Count}");
        }

        private void Task_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskItem.IsCompleted) || e.PropertyName == nameof(TaskItem.CompletedAt))
            {
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            bool hideCompleted = SettingsManager.Instance.GetHideCompletedTasks();
            FilteredTasks.Clear();
            foreach (var task in Tasks)
            {
                if (!hideCompleted || !task.IsCompleted)
                {
                    FilteredTasks.Add(task);
                }
            }
            System.Diagnostics.Debug.WriteLine($"[TaskListPage] ApplyFilter: FilteredTasks.Count={FilteredTasks.Count}, hideCompleted={hideCompleted}");
        }

        private void ExpanderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var container = FindAncestor<ListViewItem>(btn);
                if (container?.DataContext is TaskItem task)
                {
                    // Найти ItemsControl (SubtasksList) и FontIcon (ExpanderIcon) внутри StackPanel
                    var subtasksList = FindDescendantByName<ItemsControl>(container, "SubtasksList");
                    var icon = FindDescendantByName<FontIcon>(btn, "ExpanderIcon");
                    bool expanded = false;
                    _expandedStates.TryGetValue(task, out expanded);
                    expanded = !expanded;
                    _expandedStates[task] = expanded;
                    if (subtasksList != null)
                        subtasksList.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
                    if (icon != null)
                        icon.Glyph = expanded ? "\xE70E" : "\xE70D"; // E70D: ChevronRight, E70E: ChevronDown
                }
            }
        }

        // Вспомогательный поиск по визуальному дереву
        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null && !(current is T))
                current = Windows.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
            return current as T;
        }
        private static T FindDescendantByName<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            for (int i = 0; i < Windows.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = Windows.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T t && (child as FrameworkElement)?.Name == name)
                    return t;
                var result = FindDescendantByName<T>(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
