using Indubit.FlexTaskScheduler.Data;
using Indubit.FlexTaskScheduler.TaskDispatcher;
using Microsoft.EntityFrameworkCore;
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;

                // Bind and validate AppSettings
                var appSettings = new AppSettings();
                configuration.Bind(appSettings);
                ValidateAppSettings(appSettings);

                // Configure services
                services.AddDbContext<TaskDbContext>(options =>
                    options.UseSqlServer(appSettings.DatabaseConnections.TaskDatabase));

                services.AddScoped<ScheduledTaskRepository>(); 
                services.AddScoped<TaskExecutionHistoryRepository>(); 

                services.AddScoped<TaskProducer>(provider =>
                    new TaskProducer(appSettings.Kafka.BootstrapServers, appSettings.Kafka.UserName, appSettings.Kafka.Password));

                services.AddScoped<TaskProcessor>();
                services.AddHostedService<Worker>();
            });

    internal static void ValidateAppSettings(AppSettings appSettings)
    {
        if (string.IsNullOrEmpty(appSettings.DatabaseConnections?.TaskDatabase))
        {
            throw new InvalidOperationException("Database connection string for 'TaskDatabase' is not configured.");
        }

        if (string.IsNullOrEmpty(appSettings.Kafka?.BootstrapServers))
        {
            throw new InvalidOperationException("Kafka bootstrap servers are not configured.");
        }
    }
}