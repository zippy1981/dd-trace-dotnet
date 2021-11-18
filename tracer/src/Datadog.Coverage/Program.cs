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
            var coverageProcessor = new CoverageProcessor(@"C:\Users\danielredondo\source\repos\ConsoleApp16\ConsoleApp16\bin\Debug");
            coverageProcessor.Process();
        }
    }
}
