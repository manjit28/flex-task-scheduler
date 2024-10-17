using Indubit.FlexTaskScheduler.Data;
using Indubit.FlexTaskScheduler.Models;

namespace Indubit.FlexTaskScheduler.TaskDispatcher
{
    public class TaskProcessor
    {
        private readonly ScheduledTaskRepository _taskRepository;
        private readonly ILogger<TaskProcessor> _logger;
        private readonly TaskProducer _taskProducer;

        public TaskProcessor(ScheduledTaskRepository taskRepository, ILogger<TaskProcessor> logger, TaskProducer taskProducer)
        {
            _taskRepository = taskRepository;
            _logger = logger;
            _taskProducer = taskProducer;
        }

        /// <summary>
        /// Asynchronously processes scheduled tasks that are pending and due for execution.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ProcessTasksAsync()
        {
            var tasks = await _taskRepository.GetAllAsync();
            //foreach (var task in tasks.Where(t => t.TaskStatus == TaskState.Pending && t.ScheduledTime <= DateTime.UtcNow))
            foreach (var task in tasks)
            {
                try
                {
                    if (task.TaskStatus != TaskState.Pending || task.ScheduledTime > DateTime.UtcNow)
                    {
                        continue;
                    }
                    task.TaskStatus = TaskState.Processing;
                    await _taskRepository.UpdateAsync(task);

                    // Produce task details to Kafka
                    await _taskProducer.ProduceAsync(task);

                    task.TaskStatus = TaskState.Done;
                    task.ProcessedAt = DateTime.UtcNow;
                    await _taskRepository.UpdateAsync(task);

                    var nextTime = task.GetNextScheduledTime();
                    if (nextTime.HasValue)
                    {
                        task.ScheduledTime = nextTime.Value;
                        task.TaskStatus = TaskState.Pending;
                        await _taskRepository.UpdateAsync(task);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing task {task.Id}");
                    task.TaskStatus = TaskState.Pending; // Reset status on failure
                    await _taskRepository.UpdateAsync(task);
                }
            }
        }
    }
}



