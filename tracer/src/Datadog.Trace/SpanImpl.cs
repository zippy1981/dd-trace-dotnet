// <copyright file="SpanImpl.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Globalization;
using System.Text;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Logging;
using Datadog.Trace.Tagging;
using Datadog.Trace.Vendors.Serilog.Events;

namespace Datadog.Trace
{
    /// <summary>
    /// A Span represents a logical unit of work in the system. It may be
    /// related to other spans by parent/children relationships. The span
    /// tracks the duration of an operation as well as associated metadata in
    /// the form of a resource name, a service name, and user defined tags.
    /// </summary>
    internal class SpanImpl : Span
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<Span>();
        private static readonly bool IsLogLevelDebugEnabled = Log.IsEnabled(LogEventLevel.Debug);

        private readonly object _lock = new();

        public SpanImpl(ISpanContextBase context, DateTimeOffset? start, ITags tags = null)
            : base(context)
        {
            Tags = tags ?? new CommonTags();
            Context = context;
            StartTime = start ?? Context.TraceContext.UtcNow;

            Log.Debug(
                "Span started: [s_id: {SpanID}, p_id: {ParentId}, t_id: {TraceId}]",
                context.SpanId,
                context.Parent.SpanId,
                context.TraceId);
        }

        /// <summary>
        /// Gets the trace's unique identifier.
        /// </summary>
        public override ulong TraceId => Context.TraceId;

        /// <summary>
        /// Gets the span's unique identifier.
        /// </summary>
        public override ulong SpanId => Context.SpanId;

        /// <summary>
        /// Gets or sets operation name.
        /// </summary>
        public override string OperationName { get; set; }

        /// <summary>
        /// Gets or sets the resource name.
        /// </summary>
        public override string ResourceName { get; set; }

        /// <summary>
        /// Gets or sets the type of request this span represents (ex: web, db).
        /// Not to be confused with span kind.
        /// </summary>
        /// <seealso cref="SpanTypes"/>
        public override string Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this span represents an error.
        /// </summary>
        public override bool Error { get; set; }

        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public override string ServiceName
        {
            get => Context.ServiceName;
            set => Context.ServiceName = value;
        }

        /// <summary>
        /// Gets the trace's unique identifier.
        /// </summary>
        public ulong TraceId => Context.TraceId;

        /// <summary>
        /// Gets the span's unique identifier.
        /// </summary>
        public ulong SpanId => Context.SpanId;

        /// <summary>
        /// Gets <i>local root span id</i>, i.e. the <c>SpanId</c> of the span that is the root of the local, non-reentrant
        /// sub-operation of the distributed operation that is represented by the trace that contains this span.
        /// </summary>
        /// <remarks>
        /// <para>If the trace has been propagated from a remote service, the <i>remote global root</i> is not relevant for this API.</para>
        /// <para>A distributed operation represented by a trace may be re-entrant (e.g. service-A calls service-B, which calls service-A again).
        /// In such cases, the local process may be concurrently executing multiple local root spans.
        /// This API returns the id of the root span of the non-reentrant trace sub-set.</para></remarks>
        internal ulong RootSpanId
        {
            get
            {
                Span localRootSpan = Context.TraceContext?.RootSpan;
                return (localRootSpan == null || localRootSpan == this) ? SpanId : localRootSpan.SpanId;
            }
        }

        internal ITags Tags { get; set; }

        internal SpanContext Context { get; }

        internal DateTimeOffset StartTime { get; private set; }

        internal override bool IsRootSpan => Context.TraceContext?.RootSpan == this;

        internal override bool IsTopLevel => Context.Parent == null || Context.Parent.ServiceName != ServiceName;

        internal override ISpanContextBase Context { get; }

        internal override bool IsFinished { get; set; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"TraceId: {Context.TraceId}");
            sb.AppendLine($"ParentId: {Context.Parent.SpanId}");
            sb.AppendLine($"SpanId: {Context.SpanId}");
            sb.AppendLine($"Origin: {Context.Origin}");
            sb.AppendLine($"ServiceName: {ServiceName}");
            sb.AppendLine($"OperationName: {OperationName}");
            sb.AppendLine($"Resource: {ResourceName}");
            sb.AppendLine($"Type: {Type}");
            sb.AppendLine($"Start: {StartTime}");
            sb.AppendLine($"Duration: {Duration}");
            sb.AppendLine($"Error: {Error}");
            sb.AppendLine($"Meta: {Tags}");

            return sb.ToString();
        }

        /// <summary>
        /// Add tag to this span using the specified key and value pair.
        /// </summary>
        /// <param name="key">The tag's key.</param>
        /// <param name="value">The tag's value.</param>
        /// <returns>This span instance to allow method chaining.</returns>
        public override Span SetTag(string key, string value)
        {
            if (IsFinished)
            {
                Log.Warning("SetTag should not be called after the span was closed");
                return this;
            }

            // some tags have special meaning
            switch (key)
            {
                case Trace.Tags.Origin:
                    Context.Origin = value;
                    break;
                case Trace.Tags.SamplingPriority:
                    if (Enum.TryParse(value, out SamplingPriority samplingPriority) &&
                        Enum.IsDefined(typeof(SamplingPriority), samplingPriority))
                    {
                        // allow setting the sampling priority via a tag
                        Context.TraceContext.SamplingPriority = samplingPriority;
                    }

                    break;
#pragma warning disable CS0618 // Type or member is obsolete
                case Trace.Tags.ForceKeep:
                case Trace.Tags.ManualKeep:
                    if (value?.ToBoolean() == true)
                    {
                        // user-friendly tag to set UserKeep priority
                        Context.TraceContext.SamplingPriority = SamplingPriority.UserKeep;
                    }

                    break;
                case Trace.Tags.ForceDrop:
                case Trace.Tags.ManualDrop:
                    if (value?.ToBoolean() == true)
                    {
                        // user-friendly tag to set UserReject priority
                        Context.TraceContext.SamplingPriority = SamplingPriority.UserReject;
                    }

                    break;
#pragma warning restore CS0618 // Type or member is obsolete
                case Trace.Tags.Analytics:
                    if (string.IsNullOrEmpty(value))
                    {
                        // remove metric
                        SetTag(Trace.Tags.Analytics, (double?)null);
                        return this;
                    }

                    // value is a string and can represent a bool ("true") or a double ("0.5"),
                    // so try to parse both. note that "1" and "0" will parse as boolean, which is fine.
                    bool? analyticsSamplingRate = value.ToBoolean();

                    if (analyticsSamplingRate == true)
                    {
                        // always sample
                        SetTag(Trace.Tags.Analytics, 1.0);
                    }
                    else if (analyticsSamplingRate == false)
                    {
                        // never sample
                        SetTag(Trace.Tags.Analytics, 0.0);
                    }
                    else if (double.TryParse(
                        value,
                        NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                        CultureInfo.InvariantCulture,
                        out double analyticsSampleRate))
                    {
                        // use specified sample rate
                        SetTag(Trace.Tags.Analytics, analyticsSampleRate);
                    }
                    else
                    {
                        Log.Warning("Value {Value} has incorrect format for tag {TagName}", value, Trace.Tags.Analytics);
                    }

                    break;
                case Trace.Tags.Measured:
                    if (string.IsNullOrEmpty(value))
                    {
                        // Remove metric if value is null
                        SetTag(Trace.Tags.Measured, (double?)null);
                        return this;
                    }

                    bool? measured = value.ToBoolean();

                    if (measured == true)
                    {
                        // Set metric to true by passing the value of 1.0
                        SetTag(Trace.Tags.Measured, 1.0);
                    }
                    else if (measured == false)
                    {
                        // Set metric to false by passing the value of 0.0
                        SetTag(Trace.Tags.Measured, 0.0);
                    }
                    else
                    {
                        Log.Warning("Value {Value} has incorrect format for tag {TagName}", value, Trace.Tags.Measured);
                    }

                    break;
                default:
                    Tags.SetTag(key, value);
                    break;
            }

            return this;
        }

        internal override Span SetTag(string key, double? value)
        {
            Tags.SetMetric(key, value);
            return this;
        }

        /// <summary>
        /// Record the end time of the span and flushes it to the backend.
        /// After the span has been finished all modifications will be ignored.
        /// </summary>
        public override void Finish()
        {
            Finish(Context.TraceContext.ElapsedSince(StartTime));
        }

        /// <summary>
        /// Explicitly set the end time of the span and flushes it to the backend.
        /// After the span has been finished all modifications will be ignored.
        /// </summary>
        /// <param name="finishTimestamp">Explicit value for the end time of the Span.</param>
        public override void Finish(DateTimeOffset finishTimestamp)
        {
            Finish(finishTimestamp - StartTime);
        }

        /// <summary>
        /// Sets the <see cref="Error"/> flag and adds error tags to the span using the specified <paramref name="exception"/> object.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public override void SetException(Exception exception)
        {
            Error = true;

            if (exception != null)
            {
                // for AggregateException, use the first inner exception until we can support multiple errors.
                // there will be only one error in most cases, and even if there are more and we lose
                // the other ones, it's still better than the generic "one or more errors occurred" message.
                if (exception is AggregateException aggregateException && aggregateException.InnerExceptions.Count > 0)
                {
                    exception = aggregateException.InnerExceptions[0];
                }

                SetTag(Trace.Tags.ErrorMsg, exception.Message);
                SetTag(Trace.Tags.ErrorStack, exception.ToString());
                SetTag(Trace.Tags.ErrorType, exception.GetType().ToString());
            }
        }

        /// <summary>
        /// Gets the value of the string tag with the specified key, or <c>null</c> if the tag is not found.
        /// </summary>
        /// <param name="key">The tag's key.</param>
        /// <returns>The value of the tag with the specified key, or <c>null</c> if the tag is not found.</returns>
        internal override string GetStringTag(string key)
        {
            switch (key)
            {
                case Trace.Tags.SamplingPriority:
                    return ((int?)(Context.TraceContext?.SamplingPriority ?? Context.SamplingPriority))?.ToString();
                case Trace.Tags.Origin:
                    return Context.Origin;
                default:
                    return Tags.GetTag(key);
            }
        }

        internal override double? GetDoubleTag(string key)
        {
            return Tags.GetMetric(key);
        }

        private void Finish(TimeSpan duration)
        {
            var shouldCloseSpan = false;
            lock (_lock)
            {
                ResourceName ??= OperationName;

                if (!IsFinished)
                {
                    Duration = duration;
                    if (Duration < TimeSpan.Zero)
                    {
                        Duration = TimeSpan.Zero;
                    }

                    IsFinished = true;
                    shouldCloseSpan = true;
                }
            }

            if (shouldCloseSpan)
            {
                Context.TraceContext.CloseSpan(this);

                if (IsLogLevelDebugEnabled)
                {
                    Log.Debug(
                        "Span closed: [s_id: {SpanId}, p_id: {ParentId}, t_id: {TraceId}] for (Service: {ServiceName}, Resource: {ResourceName}, Operation: {OperationName}, Tags: [{Tags}])",
                        new object[]
                        {
                            Context.SpanId,
                            Context.Parent.SpanId,
                            Context.TraceId,
                            Context.ServiceName,
                            ResourceName,
                            OperationName,
                            Tags
                        });
                }
            }
        }

        internal void ResetStartTime()
        {
            StartTime = Context.TraceContext.UtcNow;
        }
    }
}
