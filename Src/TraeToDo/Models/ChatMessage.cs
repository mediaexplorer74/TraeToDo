using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TraeToDo.Models
{
    /// <summary>
    /// Represents a message in the chat conversation
    /// </summary>
    public class ChatMessage : INotifyPropertyChanged
    {
        private string _content;
        private bool _isUserMessage;
        private DateTime _timestamp;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the content of the message
        /// </summary>
        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        /// <summary>
        /// Gets or sets whether the message is from the user (true) or AI (false)
        /// </summary>
        public bool IsUserMessage
        {
            get => _isUserMessage;
            set => SetProperty(ref _isUserMessage, value);
        }

        /// <summary>
        /// Gets or sets the timestamp when the message was created
        /// </summary>
        public DateTime Timestamp
        {
            get => _timestamp;
            set
            {
                if (SetProperty(ref _timestamp, value))
                {
                    OnPropertyChanged(nameof(FormattedTime));
                }
            }
        }

        /// <summary>
        /// Gets the formatted timestamp string for display
        /// </summary>
        public string FormattedTime => Timestamp.ToString("HH:mm");

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Creates a new instance of a ChatMessage
        /// </summary>
        /// <param name="content">The message content</param>
        /// <param name="isUserMessage">Whether the message is from the user</param>

        public ChatMessage(string content, bool isUserMessage)
        {
            Content = content;
            IsUserMessage = isUserMessage;
            Timestamp = DateTime.Now;
        }
    }
}