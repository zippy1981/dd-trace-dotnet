FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

ARG past_tracer_version=1.27.1
ARG next_tracer_version=1.98.0
ARG future_tracer_version=1.99.0

# TODO:
# - download and extract https://github.com/DataDog/dd-trace-dotnet/releases/download/v1.27.1/windows-tracer-home.zip
# - download https://www.nuget.org/api/v2/package/Datadog.Trace/1.27.1
# - update tracer version in source code

# Directory.Build.props
COPY Directory.Build.props Directory.Build.props
COPY src/Directory.Build.props src/Directory.Build.props
COPY tools/Directory.Build.props tools/Directory.Build.props

COPY GlobalSuppressions.cs GlobalSuppressions.cs
COPY src/GlobalSuppressions.cs src/GlobalSuppressions.cs
COPY tools/GlobalSuppressions.cs tools/GlobalSuppressions.cs

COPY Datadog.Trace.snk Datadog.Trace.snk
COPY stylecop.json stylecop.json

# the projects and dependencies we need
COPY src/Datadog.Trace src/Datadog.Trace
COPY tools/Datadog.Core.Tools tools/Datadog.Core.Tools
COPY build/tools/PrepareRelease build/tools/PrepareRelease

# additional dependencies on pre-merge versions only
COPY src/Datadog.Trace.ClrProfiler.Managed.Core src/Datadog.Trace.ClrProfiler.Managed.Core
COPY src/Datadog.Trace.ClrProfiler.Managed src/Datadog.Trace.ClrProfiler.Managed
COPY src/Datadog.Trace.AspNet src/Datadog.Trace.AspNet
COPY src/Datadog.Trace.MSBuild src/Datadog.Trace.MSBuild
COPY src/Datadog.Trace.Ci.Shared src/Datadog.Trace.Ci.Shared

# TODO: update tools/Datadog.Core.Tools/TracerVersion.cs
RUN dotnet build build/tools/PrepareRelease/PrepareRelease.csproj -c release -p:PublishRepositoryUrl=false

COPY test/test-applications/regression/AssemblyVersionMismatch/WeatherServiceDotNetCore test/test-applications/regression/AssemblyVersionMismatch/WeatherServiceDotNetCore
# WORKDIR /source/test/test-applications/regression/AssemblyVersionMismatch/WeatherServiceDotNetCore
# RUN dotnet publish -c release -f net5.0 -o /app
