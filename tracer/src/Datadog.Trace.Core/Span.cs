// <copyright file="Span.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.Tagging;

namespace Datadog.Trace
{
    public abstract class Span : IDisposable
    {
        internal Span(ISpanContextBase context)
        {
            Context = context;
        }

        /// <summary>
        /// Gets the span's unique identifier.
        /// </summary>
        public abstract ulong SpanId { get; }

        /// <summary>
        /// Gets the trace's unique identifier.
        /// </summary>
        public abstract ulong TraceId { get; }

        /// <summary>
        /// Gets or sets the resource name.
        /// </summary>
        public abstract string ResourceName { get; set; }

        /// <summary>
        /// Gets or sets operation name.
        /// </summary>
        public abstract string OperationName { get; set; }

        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public abstract string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the type of request this span represents (ex: web, db).
        /// Not to be confused with span kind.
        /// </summary>
        public abstract string Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this span represents an error.
        /// </summary>
        public abstract bool Error { get; set; }

        internal abstract ISpanContextBase Context { get; set; }

        internal abstract ITags Tags { get; }

        internal abstract DateTimeOffset StartTime { get; set; }

        internal abstract TimeSpan Duration { get; set; }

        internal abstract bool IsRootSpan { get; }

        internal abstract bool IsTopLevel { get; }

        internal abstract bool IsFinished { get; set; }

        /// <summary>
        /// Sets a string tag in this span using the specified key and value pair.
        /// </summary>
        /// <param name="key">The tag's key.</param>
        /// <param name="value">The tag's value.</param>
        /// <returns>This span instance to allow method chaining.</returns>
        public abstract Span SetTag(string key, string value);

        /// <summary>
        /// Sets a numeric tag in this span using the specified key and value pair.
        /// </summary>
        /// <param name="key">The tag's key.</param>
        /// <param name="value">The tag's value.</param>
        /// <returns>This span instance to allow method chaining.</returns>
        internal abstract Span SetTag(string key, double? value);

        public string GetTag(string key) => GetStringTag(key);

        internal Span SetMetric(string key, double? value) => SetTag(key, value);

        internal abstract string GetStringTag(string key);

        internal abstract double? GetDoubleTag(string key);

        /// <summary>
        /// Sets the <see cref="Error"/> flag and adds error tags to the span
        /// using the specified <paramref name="exception"/> object.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public abstract void SetException(Exception exception);

        /// <summary>
        /// Record the end time of the span.
        /// </summary>
        public abstract void Finish();

        /// <summary>
        /// Record the end time of the span using the specified value;
        /// </summary>
        /// <param name="finishTimestamp">The time stamp to use as the end time of the span.</param>
        public abstract void Finish(DateTimeOffset finishTimestamp);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Finish();
            }
        }

        /// <summary>
        /// Record the end time of the span.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
