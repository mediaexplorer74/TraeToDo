using System;

namespace TraeToDo.Models
{
    /// <summary>
    /// Represents a task item in the to-do list
    /// </summary>
    public class TaskItem
    {
        /// <summary>
        /// Gets or sets the task description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets whether the task is completed
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the task was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the task was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets the formatted creation time for display
        /// </summary>
        public string FormattedCreatedAt => CreatedAt.ToString("MMM dd, yyyy HH:mm");

        /// <summary>
        /// Creates a new task item
        /// </summary>
        /// <param name="description">The task description</param>
        public TaskItem(string description)
        {
            Description = description;
            IsCompleted = false;
            CreatedAt = DateTime.Now;
        }
    }
}