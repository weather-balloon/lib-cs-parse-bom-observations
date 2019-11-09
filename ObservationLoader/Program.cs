using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeatherBalloon.Observations;

namespace WeatherBalloon.ObservationLoader
{
    class Program
    {
        public const string CONFIG_SECTION_NAME = "observations";

        public const string CONFIG_DATASTORE_SECTION_NAME = "DataStore";

        public const string CONFIG_OBSERVICE_SECTION_NAME = "ObservationService";
        public const string ENVVAR_PREFIX = "OBS_";

        public static IConfiguration GetConfiguration()
        {
            string env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json",
                    optional: true,
                    reloadOnChange: false);

            builder.AddJsonFile($"appsettings.{env}.json",
                optional: true,
                reloadOnChange: false);

            if (env == Environments.Development)
            {
                builder.AddUserSecrets<DataStoreConfiguration>();
                builder.AddUserSecrets<FtpServiceConfiguration>();
            }

            builder.AddEnvironmentVariables(ENVVAR_PREFIX);

            // Lets us use: dotnet run observations:ObservationService:Product=IDD60920
            builder.AddCommandLine(Environment.GetCommandLineArgs()[1..]);

            var config = builder.Build();

            if (!string.IsNullOrEmpty(config.GetConnectionString("AppConfig")))
            {
                // Uses ConnectionStrings:AppConfig
                // See: https://docs.microsoft.com/en-us/azure/azure-app-configuration/quickstart-aspnet-core-app
                builder.AddAzureAppConfiguration(config.GetConnectionString("AppConfig"), optional: true);
            }

            config = builder.Build();

            string vault = config.GetValue<string>("KeyVault");

            if (!string.IsNullOrEmpty(vault))
            {
                Console.WriteLine(vault);
                builder.AddAzureKeyVault($"https://{vault}.vault.azure.net/");
            }

            return builder.Build();
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            /*
                Acknowledging https://github.com/PioneerCode/pioneer-console-boilerplate/tree/master/src/Pioneer.Console.Boilerplate
             */

            // build configuration
            var configuration = GetConfiguration();

            serviceCollection.AddOptions();

            serviceCollection.Configure<DataStoreConfiguration>(
                configuration.GetSection(CONFIG_SECTION_NAME).GetSection(CONFIG_DATASTORE_SECTION_NAME));

            serviceCollection.Configure<FtpServiceConfiguration>(
                configuration.GetSection(CONFIG_SECTION_NAME).GetSection(CONFIG_OBSERVICE_SECTION_NAME));

            // add logging
            serviceCollection.AddSingleton(LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
                builder.AddConfiguration(configuration.GetSection("Logging"));
            }));
            serviceCollection.AddLogging();

            // add services
            serviceCollection.AddSingleton<IObservationService, BomFtpService>();
            serviceCollection.AddSingleton<IDataLoader, MongoDataLoader>();

        }

        static int Main(string[] args)
        {
            // create service collection
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            // create service provider
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var dataStore = serviceProvider.GetService<IDataLoader>();

            IEnumerable<WeatherStationObservation> observations = null;

            if (!dataStore.connect())
            {
                Console.Error.WriteLine($"Error: Failed to connect to the datastore");
                return 1;
            }

            try
            {
                observations = serviceProvider.GetService<IObservationService>().loadObservations();
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine($"Error: {e.Message}");
                return 1;
            }

            if (observations != null)
            {
                if (dataStore.upsertMany(observations, maxRetries: 10, carriedOverErrors: null))
                {
                    return 0;
                }
            }

            Console.Error.WriteLine("Error: Failed to complete - please check logs");
            return 1;

        }

    }
}
