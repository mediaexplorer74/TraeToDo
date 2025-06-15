using System;

namespace TraeToDo.Models
{
    /// <summary>
    /// Represents a message in the chat conversation
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Gets or sets the content of the message
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets whether the message is from the user (true) or AI (false)
        /// </summary>
        public bool IsUserMessage { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the message was created
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets the formatted timestamp string for display
        /// </summary>
        public string FormattedTime => Timestamp.ToString("HH:mm");

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