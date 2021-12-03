// <copyright file="SamplingDecision.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

namespace Datadog.Trace.Sampling
{
    internal readonly struct SamplingDecision
    {
        /// <summary>
        /// The default sampling decision used as a fall back when there are no matching sampling rates or rules.
        /// </summary>
        public static SamplingDecision Default = new(SamplingPriority.AutoKeep, SamplingMechanism.None, rate: null);

        public SamplingDecision(SamplingPriority priority, SamplingMechanism mechanism, float? rate)
        {
            Priority = priority;
            Mechanism = mechanism;
            Rate = rate;
        }

        public SamplingPriority Priority { get; }

        public SamplingMechanism Mechanism { get; }

        public float? Rate { get; }

        public void Deconstruct(out SamplingPriority samplingPriority, out SamplingMechanism samplingMechanism, out float? rate)
        {
            samplingPriority = Priority;
            samplingMechanism = Mechanism;
            rate = Rate;
        }
    }
}
