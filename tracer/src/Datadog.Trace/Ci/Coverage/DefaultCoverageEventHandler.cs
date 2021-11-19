// <copyright file="DefaultCoverageEventHandler.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Datadog.Trace.Ci.Coverage.Models;

namespace Datadog.Trace.Ci.Coverage
{
    internal sealed class DefaultCoverageEventHandler : CoverageEventHandler
    {
        protected override object OnSessionFinished(CoverageInstruction[] coverageInstructions)
        {
            if (coverageInstructions == null || coverageInstructions.Length == 0)
            {
                return null;
            }

            var coverageSession = new CoverageSession
            {
                TraceId = CorrelationIdentifier.TraceId,
                SpanId = CorrelationIdentifier.SpanId
            };

            foreach (var boundariesPerFile in coverageInstructions.GroupBy(i => i.FilePath))
            {
                var fileName = boundariesPerFile.Key;
                var coverageFileName = new FileCoverage
                {
                    Filename = fileName
                };

                coverageSession.Files.Add(coverageFileName);

                foreach (var rangeGroup in boundariesPerFile.GroupBy(i => i.Range))
                {
                    var range = rangeGroup.Key;
                    var endColumn = (ushort)(range & 0xFFFFFF);
                    var endLine = (ushort)((range >> 16) & 0xFFFFFF);
                    var startColumn = (ushort)((range >> 32) & 0xFFFFFF);
                    var startLine = (ushort)((range >> 48) & 0xFFFFFF);
                    var num = rangeGroup.Count();
                    coverageFileName.Boundaries.Add(new uint[] { startLine, startColumn, endLine, endColumn, (uint)num });
                }

                coverageFileName.Boundaries.Sort((a, b) =>
                {
                    var res = a[0].CompareTo(b[0]);
                    if (res == 0)
                    {
                        res = a[1].CompareTo(b[1]);
                    }

                    return res;
                });
            }

            return coverageSession;
        }
    }
}
