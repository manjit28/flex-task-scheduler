using Indubit.FlexTaskScheduler.Data;
using Indubit.FlexTaskScheduler.StockDataConsumer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StockDataConsumer.Services;

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

                services.AddScoped<TaskExecutionHistoryRepository>();


                services.AddHostedService<KafkaConsumerService>();
            });

    private static void ValidateAppSettings(AppSettings appSettings)
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