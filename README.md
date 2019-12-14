# Parse and stored AU BoM Observations

![](https://github.com/weather-balloon/lib-cs-parse-bom-observations/workflows/Build/badge.svg)

C# library for parsing AU BoM observation data and inserting them into
a Cosmos DB using the Mongo DB API

## General approach

Cosmos DB is not Mongo - it allows us to use the Mongo API
but there are a few differences we need to be aware of:

* Cosmos DB provides automatic indexing so I don't bother to define any
* Trying to create a user via Mongo's `db.createUser` doesn't work so I just use
the Cosmos DB connection string. There is a
[`create user`](https://docs.microsoft.com/en-us/rest/api/cosmos-db/create-a-user)
operation in the Cosmos API and this is
[described in the docs](https://docs.microsoft.com/en-us/azure/cosmos-db/secure-access-to-data#users)
but, for now, I'll keep it simple.
* Cosmos uses rate limiting (provisioned throughput) that means we need a retry mechanism
for bulk updates. The new [autopilot mode](https://docs.microsoft.com/en-us/azure/cosmos-db/provision-throughput-autopilot) could help shaping the allowed throughput rather than having
requests having to wait.

## Building

From the base project dir:

    dotnet restore
    dotnet build

### Docker

    docker build -t observationloader .

    docker run --rm -ti observationloader

## Running

The tool provides a veritable buffet of configuration options:

1. `appsettings.json`

1. `appsettings.{env}.json` - _where `env` is the [environment](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-3.0#environments) set in the `DOTNET_ENVIRONMENT` environment variable._

        export DOTNET_ENVIRONMENT=Development

1. [User secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.0&tabs=linux#secret-manager)
(for `Development` environment only)

1. Environment variables set with the `OBS_` prefix - for example:
    * `export OBS_observations__ObservationService__Product=IDV60920`
    * Configure logging: `OBS_Logging__LogLevel__Default=Debug`
    * Set the KeyVault: `OBS_KeyVault=wb-keyvault-dev`

1. Command-line variables - for example:
    * `dotnet run observations:ObservationService:Product=IDV60920`

1. An [Azure App Configuration](https://docs.microsoft.com/en-us/azure/azure-app-configuration/overview)
resource can be provided by setting `ConnectionStrings:AppConfig`.

1. If a [`KeyVault`](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-3.0#secret-storage-in-the-production-environment-with-azure-key-vault)
section is provided and contains a `vault` value then the KeyVault will provide secrets.

The `appsettings.json` file provides the baseline config layout and is a handy place to
determine the sections and values.

Note that:

* Values in KeyVault are set using `--` to delineate sections and values
 - e.g. `observations--DataStore--ConnectionString`.

 * You can add the App Config connection string to your environment with something like:

        export OBS_ConnectionStrings__AppConfig="$(az appconfig credential list -n wb-config-dev -g wb-management-dev --query "[?name=='Primary Read Only'].connectionString" --out tsv)"


### Locally with Mongo DB

The `docker-compose.yml` file provides a full test environment for development work that includes:

- A Mongo DB server
- An FTP server to provide sample observations

To spin up the environment:

    docker-compose up -d --build

You can then connect to the Mongo DB using `mongodb://dba:mongo@127.0.0.1:27017`. You can
use the [`Cosmos DB` extension for VS Code](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-cosmosdb) to connect and browse the MongoDB server.
Alternatively try [Robo 3T](https://robomongo.org/).

The test FTP server will be available at <ftp://localhost:2021/>.

To stop it and remove the containers:

    docker-compose stop
    docker-compose rm

### Locally against Cosmos DB

In order to test against Cosmos DB running in Azure
(or the [Cosmos emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator))
you need to prepare your environment as follows:

    dotnet restore

    export COSMOSDB_RG=wb-datalake-dev
    export COSMOSDB_NAME=wb-cosmos-dev
    export DOTNET_ENVIRONMENT=Development

    dotnet user-secrets set "observations:DataStore:Username" "$COSMOSDB_NAME"
    dotnet user-secrets set "observations:DataStore:Server" "$COSMOSDB_NAME.mongo.cosmos.azure.com:10255"
    dotnet user-secrets set "observations:DataStore:Password" \
        "$(az cosmosdb keys list --resource-group $COSMOSDB_RG --name $COSMOSDB_NAME --type keys --query primaryMasterKey --out tsv)"
    dotnet user-secrets set "observations:DataStore:UseTls" true

Next, start up the local FTP container:

    docker-compose up -d --build ftp

You can then launch the data loader:

    cd ObservationLoader/
    dotnet run

## Notes

* Tests are written for the [xUnit](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test)
framework.
* CI tooling through [GitHub Actions](https://help.github.com/en/categories/automating-your-workflow-with-github-actions)
* Build deployable using the [Linux RID](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#linux-rids): e.g. `dotnet build -r linux-musl-x64`
* [.NET Core Docker Sample](https://github.com/dotnet/dotnet-docker/tree/master/samples/dotnetapp)
* [Running .NET Core Unit Tests with Docker](https://github.com/dotnet/dotnet-docker/blob/master/samples/dotnetapp/dotnet-docker-unit-testing.md).
Note that I use the "in-build" test approach as the code is really just a "script" and I don't
want the image to be built if any tests fail.
* [Use Key Vault from App Service with Managed Service Identity](https://docs.microsoft.com/en-us/samples/azure-samples/app-service-msi-keyvault-dotnet/keyvault-msi-appservice-sample/)
* [Azure Key Vault Configuration Provider in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-3.0)
* [Safe storage of app secrets in development in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
* [Logging in .NET Core](https://visualstudiomagazine.com/articles/2019/03/22/logging-in-net-core.aspx)
* [Logging in .Net Core Console Apps](https://www.blinkingcaret.com/2018/02/14/net-core-console-logging/)
