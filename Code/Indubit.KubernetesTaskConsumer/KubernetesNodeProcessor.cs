using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace KubernetesTaskConsumer.Services
{
    public class KubernetesNodeProcessor
    {
        private readonly ILogger<KubernetesNodeProcessor> _logger;

        public KubernetesNodeProcessor(ILogger<KubernetesNodeProcessor> logger)
        {
            _logger = logger;
        }

        public void Process(TaskParameters taskParameters)
        {
            _logger.LogInformation($"Processing Kubernetes node for cluster '{taskParameters.ClusterName}' in environment '{taskParameters.Environment}'");

            // Parse additional parameters specific to node processing
            if (taskParameters.AdditionalParameters != null)
            {
                // Example: Parse specific node parameters
                // var nodeParam = taskParameters.AdditionalParameters.GetProperty("nodeParam").GetString();
            }
        }
    }
}