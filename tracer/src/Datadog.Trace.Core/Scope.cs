// <copyright file="Scope.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;

namespace Datadog.Trace
{
    public sealed class Scope : IDisposable
    {
        private readonly IScopeManager _scopeManager;
        private readonly bool _finishOnClose;

        internal Scope(Scope parent, Span span, IScopeManager scopeManager, bool finishOnClose)
        {
            Parent = parent;
            Span = span;
            _scopeManager = scopeManager;
            _finishOnClose = finishOnClose;
        }

        /// <summary>
        /// Gets the span associated with this scope.
        /// </summary>
        public Span Span { get; }

        /// <summary>
        /// Gets the parent scope or null if there is no parent.
        /// </summary>
        internal Scope Parent { get; }

        internal Scope Root => Parent?.Root ?? this;

        /// <summary>
        /// Closes the current scope. If it is was the active scope,
        /// its parent becomes the new active scope.
        /// </summary>
        public void Close()
        {
            _scopeManager.Close(this);

            if (_finishOnClose)
            {
                Span.Finish();
            }
        }

        /// <summary>
        /// Closes the current scope. If it is was the active scope,
        /// its parent becomes the new active scope.
        /// </summary>
        public void Dispose()
        {
            try
            {
                Close();
            }
            catch
            {
                // Ignore disposal exceptions here...
                // TODO: Log? only in test/debug? How should Close() concerns be handled (i.e. independent?)
            }
        }
    }
}
