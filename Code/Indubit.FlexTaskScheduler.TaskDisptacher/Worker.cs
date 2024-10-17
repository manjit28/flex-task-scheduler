using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Indubit.FlexTaskScheduler.TaskDispatcher;
using Microsoft.Extensions.DependencyInjection;

namespace Indubit.FlexTaskScheduler.TaskDispatcher
{
    public class Worker : BackgroundService
    {
        private const int DelayIntervalMilliseconds = 15000;

        private readonly ILogger<Worker> _logger;
        //private readonly TaskProcessor _taskProcessor;
        private readonly IServiceProvider _serviceProvider;

        //public Worker(ILogger<Worker> logger, TaskProcessor taskProcessor)
        //{
        //    _logger = logger;
        //    _taskProcessor = taskProcessor;
        //}

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                var stopwatch = Stopwatch.StartNew();

                using (var scope = _serviceProvider.CreateScope())
                {
                    var taskProcessor = scope.ServiceProvider.GetRequiredService<TaskProcessor>();

                    try
                    {
                        await taskProcessor.ProcessTasksAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while processing tasks.");
                    }
                }

                stopwatch.Stop();
                _logger.LogInformation("Task processing took {duration} ms", stopwatch.ElapsedMilliseconds);

                var delay = Math.Max(0, DelayIntervalMilliseconds - stopwatch.ElapsedMilliseconds);
                await Task.Delay((int)delay, stoppingToken);
            }
        }
    }
}