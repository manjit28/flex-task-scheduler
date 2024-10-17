using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Indubit.FlexTaskScheduler.Models;
using Indubit.FlexTaskScheduler.Data;
using System.Net;

namespace KubernetesTaskConsumer.Services
{
    /// <summary>
    /// Service for consuming messages from Kafka.
    /// </summary>
    public class KafkaConsumerService : BackgroundService
    {
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaConsumerService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="serviceProvider">The service provider.</param>
        public KafkaConsumerService(
            ILogger<KafkaConsumerService> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var consumer = CreateConsumer();
                var topics = _configuration.GetSection("Kafka:Topics").Get<string[]>();
                _logger.LogInformation($"Subscribing to topics: [{string.Join(", ", topics)}]");
                consumer.Subscribe(topics);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await ProcessMessagesAsync(consumer, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown, log if needed
                _logger.LogInformation("Kafka consumer service is shutting down.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in the Kafka consumer service.");
                // Depending on your error handling strategy, you might want to restart the service or perform other actions
            }
        }

        /// <summary>
        /// Creates a Kafka consumer with the specified configuration.
        /// </summary>
        /// <returns>An instance of <see cref="IConsumer{TKey, TValue}"/> configured for consuming messages from Kafka.</returns>
        private IConsumer<string, string> CreateConsumer()
        {
            // Create a new consumer configuration
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

            // Create a consumer builder with the specified configuration
            return new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => _logger.LogError($"Kafka error: {e.Reason}")) // Set error handler
                .SetPartitionsAssignedHandler((c, partitions) =>
                {
                    _logger.LogInformation($"Assigned partitions: [{string.Join(", ", partitions)}]"); // Log assigned partitions
                })
                .SetPartitionsRevokedHandler((c, partitions) =>
                {
                    _logger.LogInformation($"Revoking assignment: [{string.Join(", ", partitions)}]"); // Log revoked partitions
                })
                .Build(); // Build and return the consumer
        }

        private async Task ProcessMessagesAsync(IConsumer<string, string> consumer, CancellationToken stoppingToken)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);

                if (consumeResult == null) return;

                _logger.LogInformation($"Consumed message '{consumeResult.Message.Value}' from topic '{consumeResult.Topic}'");

                await ProcessMessageAsync(consumeResult);

                try
                {
                    consumer.Commit(consumeResult);
                    _logger.LogInformation($"Committed offset: {consumeResult.TopicPartitionOffset}");
                }
                catch (KafkaException ex)
                {
                    _logger.LogError($"Error committing offset: {ex.Message}");
                    // Consider implementing retry logic or custom error handling here
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError($"Error consuming message: {ex.Message}");
                // Implement your retry or dead-letter queue logic here
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error processing message: {ex.Message}");
                // Implement your retry or error handling logic here
            }
        }

        private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult)
        {
            using var scope = _serviceProvider.CreateScope();
            var historyRepository = scope.ServiceProvider.GetRequiredService<TaskExecutionHistoryRepository>();
            var executionHistory = new TaskExecutionHistory
            {
                ExecutionStartTime = DateTime.UtcNow,
                Status = "Processing",
                ProcessedBy = Environment.MachineName,

            };

            try
            {
                var scheduledTask = DeserializeScheduledTask(consumeResult.Message.Value);
                executionHistory.TaskParameters = scheduledTask.TaskParameters;
                var taskParameters = DeserializeTaskParameters(scheduledTask.TaskParameters);

                executionHistory.TaskId = scheduledTask.Id;
                await historyRepository.AddAsync(executionHistory);

                await ProcessTaskByTopicAsync(consumeResult.Message.Key, taskParameters, scope);

                executionHistory.ExecutionEndTime = DateTime.UtcNow;
                executionHistory.Status = "Completed";
                executionHistory.Result = "Success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                executionHistory.ExecutionEndTime = DateTime.UtcNow;
                executionHistory.Status = "Failed";
                executionHistory.Result = ex.Message;
                throw;
            }
            finally
            {
                await historyRepository.UpdateAsync(executionHistory);
            }
        }

        private ScheduledTask DeserializeScheduledTask(string messageValue)
        {
            var scheduledTask = JsonSerializer.Deserialize<ScheduledTask>(messageValue);
            if (scheduledTask == null || string.IsNullOrEmpty(scheduledTask.TaskParameters))
            {
                throw new InvalidOperationException("Invalid task parameters.");
            }
            return scheduledTask;
        }

        private TaskParameters DeserializeTaskParameters(string taskParameters)
        {
            var parameters = JsonSerializer.Deserialize<TaskParameters>(taskParameters);
            if (parameters == null || string.IsNullOrEmpty(parameters.ClusterName) || string.IsNullOrEmpty(parameters.Environment))
            {
                throw new InvalidOperationException("Invalid task parameters. Expected JSON format: {cluster-name:\"\", environment: \"\"}");
            }
            return parameters;
        }

        private async Task ProcessTaskByTopicAsync(string topic, TaskParameters taskParameters, IServiceScope scope)
        {
            switch (topic.ToLower())
            {
                case "kubernetes-healthcheck":
                    scope.ServiceProvider.GetRequiredService<KubernetesHealthCheckProcessor>().Process(taskParameters);
                    break;
                case "kubernetes-podprocessor":
                    scope.ServiceProvider.GetRequiredService<KubernetesPodProcessor>().Process(taskParameters);
                    break;
                case "kubernetes-nodeprocessor":
                    scope.ServiceProvider.GetRequiredService<KubernetesNodeProcessor>().Process(taskParameters);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown topic: {topic}");
            }
            await Task.CompletedTask; // Ensure the method is async
        }
    }

/// <summary>
    /// Represents the parameters for a task.
    /// </summary>
    public class TaskParameters
    {
        /// <summary>
        /// Gets or sets the cluster name.
        /// </summary>
        public string ClusterName { get; set; } = "";

        /// <summary>
        /// Gets or sets the environment.
        /// </summary>
        public string Environment { get; set; } = "";

        /// <summary>
        /// Gets or sets additional parameters.
        /// </summary>
        public JsonElement? AdditionalParameters { get; set; }
    }
}