// <copyright file="Program.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.IO;

namespace Datadog.Coverage
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Console.WriteLine("Error: missing folder path.");
                return;
            }

            var coverageProcessor = new CoverageProcessor(args[0]);
            coverageProcessor.Process();
        }
    }
}
