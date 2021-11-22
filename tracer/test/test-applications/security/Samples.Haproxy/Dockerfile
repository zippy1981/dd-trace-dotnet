ARG DOTNET_VERSION=3.1

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} as runtime

ARG TRACER_VERSION=1.30.0

WORKDIR /app

RUN mkdir -p /opt/datadog && mkdir -p /var/log/datadog \
    && apt-get update && apt-get -y --no-install-recommends install curl libc-dev libc6-dev libfontconfig libgdiplus libssl-dev openssl wkhtmltopdf xvfb  \
    && curl -LO https://github.com/DataDog/dd-trace-dotnet/releases/download/v${TRACER_VERSION}/datadog-dotnet-apm-${TRACER_VERSION}.tar.gz \
    && tar -xf datadog-dotnet-apm-${TRACER_VERSION}.tar.gz -C /opt/datadog \
    && rm datadog-dotnet-apm-${TRACER_VERSION}.tar.gz
	
RUN cp /opt/datadog/Datadog.Trace.ClrProfiler.Native.so .
RUN cp /opt/datadog/libddwaf.so .

COPY Datadog.Trace.HaproxySpoa/bin/Release/netcoreapp3.1 .

ENTRYPOINT ["dotnet", "Datadog.Trace.HaproxySpoa.dll"]