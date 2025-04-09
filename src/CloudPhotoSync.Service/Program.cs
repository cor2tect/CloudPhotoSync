using System;
using System.Collections.Generic;
using CloudPhotoSync.Service.services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CloudPhotoSync.Service
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(Serilog.Events.LogEventLevel.Verbose)
            .WriteTo.File(AppDomain.CurrentDomain.BaseDirectory + "\\logs\\log-.log",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();

            try
            {              
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }            
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .UseSerilog()
                .ConfigureLogging(
                    logging => logging.AddConsole()
                )
                .ConfigureAppConfiguration(
                    config => config.AddEnvironmentVariables()
                )
                .ConfigureServices((hostContext, services) =>
                {
                    var config = hostContext.Configuration;

                    services
                        .AddSingleton(_ => GetSyncServiceOptions(config))
                        .AddSingleton(_ => GetStorageOptions(config))
                        .AddSingleton(_ => GetServiceBusOptions(config))
                        .AddSingleton<BlobObjectStoreFactory>()
                        .AddSingleton<ServiceBusFactory>()
                        .AddSingleton<CameraController>()
                        .AddHostedService<SyncService>();
                });

        private static CloudStorageOptions GetStorageOptions(IConfiguration configuration)
        {
            string resolve(string value) => configuration
                .GetSection("CloudStorage")
                .ResolveValue(value);

            return new CloudStorageOptions(
                ConnectionString: resolve("ConnectionString")
            );
        }

        private static ServiceBusOptions GetServiceBusOptions(IConfiguration configuration)
        {
            string resolve(string value) => configuration
                .GetSection("ServiceBus")
                .ResolveValue(value);

            return new ServiceBusOptions(
                ConnectionString: resolve("ConnectionString")
            );
        }

        private static SyncServiceOptions GetSyncServiceOptions(IConfiguration configuration)
        {
            string Resolve(string key)
            {
                var section = configuration.GetSection("SyncService");

                section.TryResolveValue(key, out var value);
                return string.IsNullOrEmpty(value) ? "" : value;
            }

            IEnumerable<string> ResolveList(string key)
            {
                var section = configuration.GetSection("SyncService:" + key);
                return section.Get<List<string>>();
            }

            return new SyncServiceOptions(
                RequestTopic: Resolve("RequestTopic"),
                ResponseTopic: Resolve("ResponseTopic"),
                Container: Resolve("Container"),
                Subscription: Environment.MachineName,
                SyncPrefix: Resolve("SyncPrefix"),
                SyncFolder: Resolve("SyncFolder")
            );
        }
    }
}
