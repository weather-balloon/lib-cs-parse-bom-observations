//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Weather Balloon">
//     Copyright (c) Duncan Dickinson. BSD 2-Clause License.
// </copyright>
//-----------------------------------------------------------------------

namespace WeatherBalloon.ObservationLoader
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureAppConfiguration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using WeatherBalloon.ObservationLoader.Cosmos;
    using WeatherBalloon.ObservationLoader.Services.BoM;
    using WeatherBalloon.Observations.Model;

    /// <summary>CLI app.</summary>
    public class Program
    {
        /// <summary>General config section.</summary>
        public const string CONFIG_SECTION_NAME = "observations";

        /// <summary>Section name for the data store config.</summary>
        public const string CONFIG_DATASTORE_SECTION_NAME = "DataStore";

        /// <summary>Section name for the observation service config.</summary>
        public const string CONFIG_OBSERVICE_SECTION_NAME = "ObservationService";

        /// <summary>Environment variable prefix.</summary>
        public const string ENVVAR_PREFIX = "OBS_";

        /// <summary>Main method.</summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>0 if success, non-zero otherwise.</returns>
        public static int Main(string[] args)
        {
            // create service collection
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            // create service provider
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var dataStore = serviceProvider.GetService<IDataLoader>();

            IEnumerable<WeatherStationObservation> observations = null;

            if (!dataStore.Connect())
            {
                Console.Error.WriteLine($"Error: Failed to connect to the datastore");
                return 1;
            }

            try
            {
                observations = serviceProvider.GetService<IObservationService>().LoadObservations();
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine($"Error: {e.Message}");
                return 1;
            }

            if (observations != null)
            {
                if (dataStore.UpsertMany(observations, maxRetries: 10, carriedOverErrors: null))
                {
                    return 0;
                }
            }

            Console.Error.WriteLine("Error: Failed to complete - please check logs");
            return 1;
        }

        private static IConfiguration GetConfiguration()
        {
            string env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile(
                    "appsettings.json",
                    optional: true,
                    reloadOnChange: false);

            builder.AddJsonFile(
                $"appsettings.{env}.json",
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
    }
}
