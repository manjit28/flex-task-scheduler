namespace Indubit.FlexTaskScheduler.TaskDispatcher
{
    public class AppSettings
    {
        public DatabaseConnections DatabaseConnections { get; set; } = new DatabaseConnections();
        public KafkaSettings Kafka { get; set; } = new KafkaSettings();
    }

    public class DatabaseConnections
    {
        public string TaskDatabase { get; set; } = "";
    }

    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
    }
}