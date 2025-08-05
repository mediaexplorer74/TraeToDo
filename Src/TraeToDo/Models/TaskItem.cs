using System;
using System.Collections.ObjectModel;

namespace TraeToDo.Models
{
    /// <summary>
    /// Represents a task item in the to-do list
    /// </summary>
    public class TaskItem : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<SubTaskItem> Subtasks { get; set; } = new ObservableCollection<SubTaskItem>();
        /// <summary>
        /// Gets or sets the task description
        /// </summary>
        private string _description;
        public string Description
        {
            get => _description;
            set { if (_description != value) { _description = value; OnPropertyChanged(nameof(Description)); } }
        }

        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged(nameof(IsCompleted));
                }
            }
        }

        public DateTime CreatedAt { get; set; }

        private DateTime? _completedAt;
        public DateTime? CompletedAt
        {
            get => _completedAt;
            set
            {
                if (_completedAt != value)
                {
                    _completedAt = value;
                    OnPropertyChanged(nameof(CompletedAt));
                    IsCompleted = _completedAt != null;
                }
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Gets the formatted creation time for display
        /// </summary>
        public string FormattedCreatedAt => CreatedAt.ToString("MMM dd, yyyy HH:mm");

        /// <summary>
        /// Creates a new task item
        /// </summary>
        /// <param name="description">The task description</param>
        public TaskItem() { }

        public TaskItem(string description)
        {
            Description = description;
            IsCompleted = false;
            CreatedAt = DateTime.Now;
        }
    }

    public class SubTaskItem : System.ComponentModel.INotifyPropertyChanged
    {
        private string _description;
        public string Description
        {
            get => _description;
            set { if (_description != value) { _description = value; OnPropertyChanged(nameof(Description)); } }
        }

        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set { if (_isCompleted != value) { _isCompleted = value; OnPropertyChanged(nameof(IsCompleted)); } }
        }

        public SubTaskItem() { }

        public SubTaskItem(string description)
        {
            Description = description;
            IsCompleted = false;
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

    }
}