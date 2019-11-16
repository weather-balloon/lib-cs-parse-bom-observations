FROM wbcontainerregistrydev.azurecr.io/builder-dotnet-core:3.0

COPY Observations/*.csproj ./Observations/
COPY Observations.Tests/*.csproj ./Observations.Tests/
COPY ObservationLoader/*.csproj ./ObservationLoader/
COPY *.sln ./
RUN dotnet restore

COPY Observations/* ./Observations/
COPY Observations.Tests/* ./Observations.Tests/
COPY ObservationLoader/* ./ObservationLoader/

RUN dotnet test

WORKDIR /src/ObservationLoader
RUN dotnet publish -c Release -r linux-musl-x64 -o out --self-contained true /p:PublishTrimmed=true

FROM mcr.microsoft.com/dotnet/core/runtime:3.0-alpine AS runtime
WORKDIR /app
COPY --from=build /src/ObservationLoader/out ./
ENTRYPOINT ["./ObservationLoader"]

# Based on: https://github.com/dotnet/dotnet-docker/blob/master/samples/dotnetapp/Dockerfile.alpine-x64
