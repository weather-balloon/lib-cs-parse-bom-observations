# Parse AU BoM Observations

![](https://github.com/weather-balloon/lib-cs-parse-bom-observations/workflows/Build/badge.svg)

C# library for parsing AU BoM observation data


## Building

From the base project dir:

    dotnet build

### Docker

    docker build -t observationloader .

    docker run --rm -ti observationloader

## Running

### Locally with Docker Compose

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

### Locally

    export DOTNET_ENVIRONMENT=Development
    cat ./local-config.json | dotnet user-secrets set
    dotnet user-secrets set "observations:ConnectionString" "<YOUR CONNECTION STRING>"

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
