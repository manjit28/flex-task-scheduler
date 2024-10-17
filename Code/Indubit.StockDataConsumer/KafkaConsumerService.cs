using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Indubit.FlexTaskScheduler.Models; 
using Indubit.FlexTaskScheduler.Data;
using System.Net;
using System.Text.Json;

namespace StockDataConsumer.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public KafkaConsumerService(ILogger<KafkaConsumerService> logger, IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Executes the Kafka consumer service, continuously consuming messages from specified topics
        /// and processing them until a cancellation is requested.
        /// </summary>
        /// <param name="stoppingToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation of consuming and processing messages.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"],
                GroupId = _configuration["Kafka:GroupId"],
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false, // Disable auto-commit
                MaxPollIntervalMs = 300000,
                SaslMechanism = SaslMechanism.Plain,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                ClientId = Dns.GetHostName(),
                SaslUsername = _configuration["Kafka:SaslUsername"],
                SaslPassword = _configuration["Kafka:SaslPassword"],
            };

            var topics = _configuration.GetSection("Kafka:Topics").Get<string[]>();

            using var consumer = new ConsumerBuilder<Ignore, string>(config)
                .SetErrorHandler((_, e) => _logger.LogError($"Error: {e.Reason}"))
                .SetPartitionsAssignedHandler((c, partitions) =>
                {
                    _logger.LogInformation($"Assigned partitions: [{string.Join(", ", partitions)}]");
                })
                .SetPartitionsRevokedHandler((c, partitions) =>
                {
                    _logger.LogInformation($"Revoking assignment: [{string.Join(", ", partitions)}]");
                })
                .Build();

            consumer.Subscribe(topics);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);

                        if (consumeResult == null) continue;

                        _logger.LogInformation($"Consumed message '{consumeResult.Message.Value}' from topic '{consumeResult.Topic}'");

                        await ProcessMessageAsync(consumeResult);

                        // Commit the offset after successful processing
                        try
                        {
                            consumer.Commit(consumeResult);
                            _logger.LogInformation($"Committed offset: {consumeResult.TopicPartitionOffset}");
                        }
                        catch (KafkaException ex)
                        {
                            _logger.LogError($"Error committing offset: {ex.Message}");
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError($"Error consuming message: {ex.Message}");
                        // Implement your retry or dead-letter queue logic here
                    }
                    catch (OperationCanceledException)
                    {
                        // Graceful shutdown
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Unexpected error: {ex.Message}");
                        // Implement your retry or error handling logic here
                    }
                }
            }
            finally
            {
                consumer.Close();
            }
        }

        /// <summary>
        /// Processes a consumed Kafka message by deserializing it into a scheduled task,
        /// updating the task execution history, and handling any processing errors.
        /// </summary>
        /// <param name="consumeResult">The result of the Kafka message consumption, containing the message to be processed.</param>
        /// <returns>A task that represents the asynchronous operation of processing the message.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the task parameters are invalid.</exception>
        /// <exception cref="Exception">Rethrows any exception encountered during processing to be handled in the main loop.</exception>
        private async Task ProcessMessageAsync(ConsumeResult<Ignore, string> consumeResult)
        {
            using var scope = _serviceProvider.CreateScope();
            var historyRepository = scope.ServiceProvider.GetRequiredService<TaskExecutionHistoryRepository>();

            TaskExecutionHistory? executionHistory = null;

            try
            {
                var scheduledTask = JsonSerializer.Deserialize<ScheduledTask>(consumeResult.Message.Value);
                if (scheduledTask == null || string.IsNullOrEmpty(scheduledTask.TaskParameters))
                {
                    throw new InvalidOperationException("Invalid task parameters.");
                }


                executionHistory = new TaskExecutionHistory
                {
                    TaskId = scheduledTask.Id,
                    ExecutionStartTime = DateTime.UtcNow,
                    Status = "Processing",
                    ProcessedBy = Environment.MachineName,
                    TaskParameters = scheduledTask.TaskParameters
                };

                await historyRepository.AddAsync(executionHistory);

                // Process the task based on the topic


                executionHistory.ExecutionEndTime = DateTime.UtcNow;
                executionHistory.Status = "Completed";
                executionHistory.Result = "Success";

                await historyRepository.UpdateAsync(executionHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");

                if (executionHistory != null)
                {
                    executionHistory.ExecutionEndTime = DateTime.UtcNow;
                    executionHistory.Status = "Failed";
                    executionHistory.Result = ex.Message;

                    await historyRepository.UpdateAsync(executionHistory);
                }

                // Rethrow the exception to be handled in the main loop
                throw;
            }
        }
    }

    public class TaskParameters
    {
    }
}