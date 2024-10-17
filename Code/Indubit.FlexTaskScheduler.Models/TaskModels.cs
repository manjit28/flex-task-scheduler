using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Cronos;

namespace Indubit.FlexTaskScheduler.Models
{
    [Table("TaskCategory")]
    public class TaskCategory
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty; 

        public ICollection<TaskType> TaskTypes { get; set; } = new List<TaskType>(); 
    }

    [Table("TaskType")]
    public class TaskType
    {
        public int Id { get; set; }
        public string TypeName { get; set; } = string.Empty; 

        public int TaskCategoryId { get; set; }
        public TaskCategory TaskCategory { get; set; } = null!; 

        public ICollection<ScheduledTask> ScheduledTasks { get; set; } = new List<ScheduledTask>(); 
    }

    [Table("ScheduledTask")]
    public class ScheduledTask
    {
        public int Id { get; set; }
        public string? TaskParameters { get; set; } = string.Empty;
        public DateTime ScheduledTime { get; set; }
        public string? CronExpression { get; set; } = string.Empty;
        public string Status { get; set; } = "";
        public TaskState TaskStatus { get; set; } = TaskState.Pending;
        public string? ProcessedBy { get; set; } = string.Empty;
        public DateTime? ProcessedAt { get; set; }
        public int? IntervalInSeconds { get; set; }

        public int TaskTypeId { get; set; }
        public TaskType TaskType { get; set; } = null!;

        public DateTime? GetNextScheduledTime()
        {
            if (IntervalInSeconds.HasValue)
            {
                // Calculate the next scheduled time based on the last processed time or the scheduled time
                var lastRunTime = ProcessedAt ?? ScheduledTime;
                return lastRunTime.AddSeconds(IntervalInSeconds.Value);
            }

            if (string.IsNullOrWhiteSpace(CronExpression))
                return null;

            var cronExpression = Cronos.CronExpression.Parse(CronExpression);
            return cronExpression.GetNextOccurrence(DateTime.UtcNow);
        }
    }
    [Table("TaskExecutionHistory")]
    public class TaskExecutionHistory
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public ScheduledTask ScheduledTask { get; set; } = null!;
        public string? TaskParameters { get; set; }
        public DateTime? ExecutionStartTime { get; set; }
        public DateTime? ExecutionEndTime { get; set; }
        public string Status { get; set; } = string.Empty; // Processing, Completed, Failed
        public string? ProcessedBy { get; set; }
        public string? Result { get; set; }
    }

    public enum TaskState
    {
        Pending = 1,
        Processing = 2,
        Done = 3
    }
}