using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                AddTask();
            }
        }

        private void AddTask()
        {
            var taskText = TaskInputBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(taskText))
            {
                var newTask = new TaskItem(taskText);
                newTask.PropertyChanged += Task_PropertyChanged;
                Tasks.Add(newTask);
                TaskInputBox.Text = string.Empty;
                SaveTasks();
                ApplyFilter();
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

        private async void SaveTasks()
        {
            await SettingsManager.Instance.SaveTasksToFileAsync(Tasks);
        }

        private async void ShowStatusBar(string message, bool success = true)
        {
            StatusBar.Text = message;
            StatusBar.Foreground = success ? new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Green) : new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);
            StatusBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            await System.Threading.Tasks.Task.Delay(2500);
            StatusBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is TaskItem task)
            {
                task.CompletedAt = DateTime.Now;
                SaveTasks();
                // ApplyFilter будет вызван через PropertyChanged
                bool hideCompleted = SettingsManager.Instance.GetHideCompletedTasks();
                if (hideCompleted)
                    ShowStatusBar("Задача выполнена и скрыта");
                else
                    ShowStatusBar("Задача выполнена");
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is TaskItem task)
            {
                task.CompletedAt = null;
                SaveTasks();
                // ApplyFilter будет вызван через PropertyChanged
                ShowStatusBar("Задача возвращена в активные");
            }
        }

        private void SubtaskCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is SubTaskItem subtask)
            {
                subtask.IsCompleted = true;
                SaveTasks();
                bool hideCompleted = SettingsManager.Instance.GetHideCompletedTasks();
                if (hideCompleted)
                    ShowStatusBar("Подзадача выполнена и скрыта");
                else
                    ShowStatusBar("Подзадача выполнена");
            }
        }

        private void SubtaskCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is SubTaskItem subtask)
            {
                subtask.IsCompleted = false;
                SaveTasks();
                ShowStatusBar("Подзадача возвращена в активные");
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
