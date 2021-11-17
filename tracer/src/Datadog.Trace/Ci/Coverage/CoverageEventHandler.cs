// <copyright file="CoverageEventHandler.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Datadog.Trace.Ci.Coverage
{
    /// <summary>
    /// Coverage event handler
    /// </summary>
    public sealed class CoverageEventHandler
    {
        private readonly AsyncLocal<CoverageContextContainer> _asyncContext = new();

        /// <summary>
        /// On session finished
        /// </summary>
        public event EventHandler<CoverageInstruction[]> OnSessionFinished;

        /// <summary>
        /// Start session
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartSession()
        {
            _asyncContext.Value = new CoverageContextContainer();
        }

        /// <summary>
        /// Gets if there is an active session for the current context
        /// </summary>
        /// <returns>True if a session is active; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSessionActiveForCurrentContext()
        {
            return _asyncContext.Value?.Enabled ?? false;
        }

        /// <summary>
        /// Enable coverage for current context (An active coverage session is required)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnableCoverageForCurrentContext()
        {
            var contextContainer = _asyncContext.Value;
            if (contextContainer != null)
            {
                contextContainer.Enabled = true;
            }
        }

        /// <summary>
        /// Disable coverage for current context (An active coverage session is required)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DisableCoverageForCurrentContext()
        {
            var contextContainer = _asyncContext.Value;
            if (contextContainer != null)
            {
                contextContainer.Enabled = false;
            }
        }

        /// <summary>
        /// End async session
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndSession()
        {
            var context = _asyncContext.Value;
            if (context != null)
            {
                _asyncContext.Value = null;
                OnSessionFinished?.Invoke(this, context.GetPayload());
            }
        }

        /// <summary>
        /// Gets the scope to report coverage data
        /// </summary>
        /// <param name="methodDef">MethodDef metadata token</param>
        /// <param name="scope">Coverage scope instance</param>
        /// <returns>True if an scope can be collected; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetScope(uint methodDef, out CoverageScope scope)
        {
            var context = _asyncContext.Value;
            if (context != null)
            {
                scope = new CoverageScope(methodDef, context);
                return true;
            }

            scope = default;
            return false;
        }
    }
}
