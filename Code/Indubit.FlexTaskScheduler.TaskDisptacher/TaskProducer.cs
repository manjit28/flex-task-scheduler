using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Indubit.FlexTaskScheduler.Models;

namespace Indubit.FlexTaskScheduler.TaskDispatcher
{
    public class TaskProducer : IDisposable
    {
        private readonly IProducer<string, string> _producer;

        public TaskProducer(string bootstrapServers, string userName, string password)
        {
            var config = new ProducerConfig { 
                BootstrapServers = bootstrapServers,
                SaslMechanism = SaslMechanism.Plain,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                ClientId = Dns.GetHostName(),
                SaslUsername = userName,
                SaslPassword = password,
            };
            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        /// <summary>
        /// Asynchronously produces a message to a Kafka topic based on the provided scheduled task details.
        /// </summary>
        /// <param name="task">The scheduled task containing details to be serialized and sent as a message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ProduceAsync(ScheduledTask task)
        {
            var taskDetails = new
            {
                task.Id,
                task.TaskParameters,
                TaskTypeName = task.TaskType.TypeName,
                CategoryName = task.TaskType.TaskCategory.CategoryName,
                task.ScheduledTime,
                task.TaskStatus,
                task.ProcessedBy,
                task.ProcessedAt
            };

            var message = new Message<string, string>
            {
                Key = $"{task.TaskType.TaskCategory.CategoryName}-{task.TaskType.TypeName}",
                Value = JsonSerializer.Serialize(taskDetails)
            };

            try
            {
                var deliveryResult = await _producer.ProduceAsync(task.TaskType.TaskCategory.CategoryName, message);
                Console.WriteLine($"Delivered '{deliveryResult.Value}' to '{deliveryResult.TopicPartitionOffset}' with key '{deliveryResult.Key}'");
            }
            catch (ProduceException<string, string> e)
            {
                Console.WriteLine($"Delivery failed: {e.Error.Reason}");
            }
        }

        public void Dispose()
        {
            _producer?.Dispose();
        }
    }
}