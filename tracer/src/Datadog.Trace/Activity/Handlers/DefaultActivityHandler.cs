// <copyright file="DefaultActivityHandler.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using Datadog.Trace.Activity.DuckTypes;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Activity.Handlers
{
    /// <summary>
    /// The default handler catches an activity and creates a datadog span from it.
    /// </summary>
    internal class DefaultActivityHandler : IActivityHandler
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(DefaultActivityHandler));
        private static readonly Dictionary<object, Scope> ActivityScope = new();
        private static readonly string[] IgnoreOperationNamesStartingWith =
        {
            "System.Net.Http.",
            "Microsoft.AspNetCore.",
        };

        public bool ShouldListenTo(string sourceName, string version)
        {
            return true;
        }

        public void ActivityStarted<T>(string sourceName, T activity)
            where T : IActivity
        {
            // Propagate Trace and Parent Span ids
            if (activity.Parent is null && activity is IActivity5 activity5)
            {
                var span = Tracer.Instance.ActiveScope?.Span;
                if (span is not null)
                {
                    activity5.TraceId = span.TraceId.ToString("x32");
                    activity5.ParentSpanId = span.SpanId.ToString("x");
                }
            }

            try
            {
                Log.Debug($"DefaultActivityHandler.ActivityStarted: [Source={sourceName}, Id={activity.Id}, RootId={activity.RootId}, OperationName={{OperationName}}, StartTimeUtc={{StartTimeUtc}}, Duration={{Duration}}]", activity.OperationName, activity.StartTimeUtc, activity.Duration);

                foreach (var ignoreSourceName in IgnoreOperationNamesStartingWith)
                {
                    if (activity.OperationName?.StartsWith(ignoreSourceName) == true)
                    {
                        return;
                    }
                }

                lock (ActivityScope)
                {
                    if (!ActivityScope.TryGetValue(activity.Instance, out _))
                    {
                        var scope = Tracer.Instance.StartActiveInternal(activity.OperationName, startTime: activity.StartTimeUtc, finishOnClose: false);
                        ActivityScope[activity.Instance] = scope;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing the OnActivityStarted callback");
            }
        }

        public void ActivityStopped<T>(string sourceName, T activity)
            where T : IActivity
        {
            try
            {
                var hasActivity = activity?.Instance is not null;
                if (hasActivity)
                {
                    foreach (var ignoreSourceName in IgnoreOperationNamesStartingWith)
                    {
                        if (activity.OperationName?.StartsWith(ignoreSourceName) == true)
                        {
                            return;
                        }
                    }
                }

                lock (ActivityScope)
                {
                    if (hasActivity && ActivityScope.TryGetValue(activity.Instance, out var scope) && scope?.Span is not null)
                    {
                        // We have the exact scope associated with the Activity
                        Log.Debug($"DefaultActivityHandler.ActivityStopped: [Source={sourceName}, Id={activity.Id}, RootId={activity.RootId}, OperationName={{OperationName}}, StartTimeUtc={{StartTimeUtc}}, Duration={{Duration}}]", activity.OperationName, activity.StartTimeUtc, activity.Duration);
                        CloseActivityScope(sourceName, activity, scope);
                        ActivityScope.Remove(activity.Instance);
                    }
                    else
                    {
                        // The listener didn't send us the Activity or the scope instance was not found
                        // In this case we are going go through the dictionary to check if we have an activity that
                        // has been closed and then close the associated scope.
                        if (hasActivity)
                        {
                            Log.Information($"DefaultActivityHandler.ActivityStopped: MISSING SCOPE [Source={sourceName}, Id={activity.Id}, RootId={activity.RootId}, OperationName={{OperationName}}, StartTimeUtc={{StartTimeUtc}}, Duration={{Duration}}]", activity.OperationName, activity.StartTimeUtc, activity.Duration);
                        }
                        else
                        {
                            Log.Information($"DefaultActivityHandler.ActivityStopped: [Missing Activity]");
                        }

                        List<object> toDelete = null;
                        foreach (var item in ActivityScope)
                        {
                            var activityObject = item.Key;
                            var hasClosed = false;

                            if (activityObject.TryDuckCast<IActivity6>(out var activity6))
                            {
                                if (activity6.Duration != TimeSpan.Zero)
                                {
                                    CloseActivityScope(sourceName, activity6, item.Value);
                                    hasClosed = true;
                                }
                            }
                            else if (activityObject.TryDuckCast<IActivity5>(out var activity5))
                            {
                                if (activity5.Duration != TimeSpan.Zero)
                                {
                                    CloseActivityScope(sourceName, activity5, item.Value);
                                    hasClosed = true;
                                }
                            }
                            else if (activityObject.TryDuckCast<IActivity>(out var activity4))
                            {
                                if (activity4.Duration != TimeSpan.Zero)
                                {
                                    CloseActivityScope(sourceName, activity4, item.Value);
                                    hasClosed = true;
                                }
                            }

                            if (hasClosed)
                            {
                                toDelete ??= new List<object>();
                                toDelete.Add(activityObject);
                            }
                        }

                        if (toDelete is not null)
                        {
                            foreach (var item in toDelete)
                            {
                                ActivityScope.Remove(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing the DefaultActivityHandler.ActivityStopped callback");
            }

            static void CloseActivityScope<TInner>(string sourceName, TInner activity, Scope scope)
                where TInner : IActivity
            {
                var span = scope.Span;
                foreach (var activityTag in activity.Tags)
                {
                    span.SetTag(activityTag.Key, activityTag.Value);
                }

                foreach (var activityBag in activity.Baggage)
                {
                    span.SetTag(activityBag.Key, activityBag.Value);
                }

                if (activity is IActivity6 { Status: ActivityStatusCode.Error } activity6)
                {
                    span.Error = true;
                    span.SetTag(Tags.ErrorMsg, activity6.StatusDescription);
                }

                if (activity is IActivity5 activity5)
                {
                    if (!string.IsNullOrWhiteSpace(sourceName))
                    {
                        span.SetTag("source", sourceName);
                        span.ResourceName = $"{sourceName}.{span.OperationName}";
                    }
                    else
                    {
                        span.ResourceName = span.OperationName;
                    }

                    switch (activity5.Kind)
                    {
                        case ActivityKind.Client:
                            span.SetTag(Tags.SpanKind, "client");
                            break;
                        case ActivityKind.Consumer:
                            span.SetTag(Tags.SpanKind, "consumer");
                            break;
                        case ActivityKind.Internal:
                            span.SetTag(Tags.SpanKind, "internal");
                            break;
                        case ActivityKind.Producer:
                            span.SetTag(Tags.SpanKind, "producer");
                            break;
                        case ActivityKind.Server:
                            span.SetTag(Tags.SpanKind, "server");
                            break;
                    }
                }

                span.Finish(activity.StartTimeUtc.Add(activity.Duration));
                scope.Close();
            }
        }
    }
}
