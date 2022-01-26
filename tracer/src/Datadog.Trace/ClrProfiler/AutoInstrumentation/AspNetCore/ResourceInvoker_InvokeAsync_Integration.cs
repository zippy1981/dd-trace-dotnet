// <copyright file="ResourceInvoker_InvokeAsync_Integration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETFRAMEWORK

using System;
using System.ComponentModel;
using Datadog.Trace.AppSec;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.Configuration;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AspNet
{
    /// <summary>
    /// System.Web.Http.ExceptionHandling.ExceptionHandlerExtensions calltarget instrumentation
    /// This instrumentation is based off the ASP.NET Web API 2 error handling design that is documented here:
    /// https://docs.microsoft.com/en-us/aspnet/web-api/overview/error-handling/web-api-global-error-handling
    /// </summary>
    [InstrumentMethod(
        AssemblyName = "Microsoft.AspNetCore.Mvc.Core",
        TypeName = "Microsoft.AspNetCore.Mvc.Internal.ResourceInvoker",
        MethodName = "InvokeAsync",
        ReturnTypeName = ClrNames.Task,
        ParameterTypeNames = new string[0],
        MinimumVersion = Major2,
        MaximumVersion = Major2,
        IntegrationName = IntegrationName)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ResourceInvoker_InvokeAsync_Integration
    {
        private const string Major2 = "2";
        private const string IntegrationName = nameof(IntegrationId.AspNetCore);
        private const IntegrationId IntegrationId = Configuration.IntegrationId.AspNetCore;
        private const string MvcOperationName = "aspnet_core_mvc.request";

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <returns>Calltarget state value</returns>
        internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
        {
            var tracer = Tracer.Instance;
            var security = Security.Instance;

            var shouldTrace = tracer.Settings.IsIntegrationEnabled(IntegrationId);
            var shouldSecure = security.Settings.Enabled;

            if (!shouldTrace && !shouldSecure)
            {
                return CallTargetState.GetDefault();
            }

            Scope scope = null;
            Span parentSpan = tracer.InternalActiveScope?.Span;
            /*

            if (parentSpan != null && arg.TryDuckCast<BeforeActionStruct>(out var typedArg))
            {
                HttpContext httpContext = typedArg.HttpContext;
                HttpRequest request = httpContext.Request;

                // NOTE: This event is the start of the action pipeline. The action has been selected, the route
                //       has been selected but no filters have run and model binding hasn't occurred.
                Span span = null;
                if (shouldTrace)
                {
                    if (!tracer.Settings.RouteTemplateResourceNamesEnabled)
                    {
                        SetLegacyResourceNames(typedArg, parentSpan);
                    }
                    else
                    {
                        span = StartMvcCoreSpan(tracer, parentSpan, typedArg, httpContext, request);
                    }
                }

                if (shouldSecure)
                {
                    security.InstrumentationGateway.RaiseMvcBeforeAction(httpContext, httpContext.Request, span ?? parentSpan, typedArg.RouteData);
                }
            }
            */

            if (shouldTrace)
            {
                if (!tracer.Settings.RouteTemplateResourceNamesEnabled)
                {
                    // SetLegacyResourceNames(typedArg, parentSpan);
                }
                else
                {
                    // span = StartMvcCoreSpan(tracer, parentSpan, typedArg, httpContext, request);
                    var mvcSpanTags = new AspNetCoreMvcTags();
                    scope = tracer.StartActiveInternal(MvcOperationName, tags: mvcSpanTags);
                }
            }

            return new CallTargetState(scope);
        }

        /// <summary>
        /// OnAsyncMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TResponse">Type of the response, in an async scenario will be T of Task of T</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="responseMessage">HttpResponse message instance</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        internal static TResponse OnAsyncMethodEnd<TTarget, TResponse>(TTarget instance, TResponse responseMessage, Exception exception, in CallTargetState state)
        {
            state.Scope.DisposeWithException(exception);
            return responseMessage;
        }
    }
}
#endif