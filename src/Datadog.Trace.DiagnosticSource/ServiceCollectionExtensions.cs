using System;
using System.Collections.Generic;
using Datadog.Trace.DiagnosticListeners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Datadog.Trace.DiagnosticSource
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatadogTracing(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddDatadogTracing(Tracer.Instance);
            return services;
        }

        public static IServiceCollection AddDatadogTracing(this IServiceCollection services, Tracer tracer)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (tracer == null)
            {
                throw new ArgumentNullException(nameof(tracer));
            }

            if (!ReferenceEquals(Tracer.Instance, tracer))
            {
                Tracer.Instance = tracer;
            }

            List<DiagnosticObserver> observers = new()
            {
                new AspNetCoreDiagnosticObserver()
            };

            DiagnosticManager.Instance = new DiagnosticManager(observers);
            DiagnosticManager.Instance.Start();

            services.TryAddSingleton(tracer);
            return services;
        }
    }
}
