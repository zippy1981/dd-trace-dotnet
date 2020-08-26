# https://hub.docker.com/_/microsoft-dotnet-core

# cache this first since it will rarely change
FROM mcr.microsoft.com/dotnet/core/runtime:3.1 as final
WORKDIR /app

ARG DD_TRACER_VERSION=1.18.4-prerelease
RUN mkdir -p /opt/datadog
RUN mkdir -p /var/log/datadog/dotnet
#RUN curl -L https://github.com/DataDog/dd-trace-dotnet/releases/download/v$DD_TRACER_VERSION/datadog-dotnet-apm-$DD_TRACER_VERSION.tar.gz | tar xzf - -C /opt/datadog
RUN curl -L https://github.com/DataDog/dd-trace-dotnet/releases/download/v1.18.4-prerelease/datadog-dotnet-apm-1.18.4.tar.gz  | tar xzf - -C /opt/datadog
#RUN ln -sf /dev/stdout /var/log/datadog/dotnet/dotnet-profiler.log
#RUN ln -sf /dev/stdout /var/log/datadog/dotnet/dotnet-tracer-dotnet-20200826.log

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY NpgsqlTest.Cli/*.csproj NpgsqlTest.Cli/
COPY NpgsqlTest.DataAccess/*.csproj NpgsqlTest.DataAccess/
RUN dotnet restore NpgsqlTest.Cli/

# copy everything else and build app
COPY . .
RUN dotnet publish NpgsqlTest.Cli/ -c release -f netcoreapp3.1 -o /app --no-restore

# final stage/image
FROM final

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH_64=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
ENV DD_DOTNET_TRACER_HOME=/opt/datadog
ENV DD_INTEGRATIONS=/opt/datadog/integrations.json

COPY --from=build /app ./
ENTRYPOINT ["dotnet", "NpgsqlTest.Cli.dll"]
