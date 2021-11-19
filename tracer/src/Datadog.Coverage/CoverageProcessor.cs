// <copyright file="CoverageProcessor.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.IO;
using System.Threading.Tasks;

namespace Datadog.Coverage
{
    internal class CoverageProcessor
    {
        private readonly string _folderPath;

        public CoverageProcessor(string folderPath)
        {
            _folderPath = folderPath;
        }

        public void Process()
        {
            var dllFiles = Directory.GetFiles(_folderPath, "*.dll");
            var exeFiles = Directory.GetFiles(_folderPath, "*.exe");

            Parallel.ForEach(dllFiles, dllFile =>
            {
                try
                {
                    var asmProcessor = new AssemblyProcessor(dllFile);
                    asmProcessor.ProcessAndSaveTo();
                }
                catch (Datadog.Trace.Ci.Coverage.Exceptions.PdbNotFoundException)
                {
                    Console.WriteLine($"{dllFile} ignored by symbols.");
                }
            });

            Parallel.ForEach(exeFiles, exeFile =>
            {
                try
                {
                    var asmProcessor = new AssemblyProcessor(exeFile);
                    asmProcessor.ProcessAndSaveTo();
                }
                catch (Datadog.Trace.Ci.Coverage.Exceptions.PdbNotFoundException)
                {
                    Console.WriteLine($"{exeFile} ignored by symbols.");
                }
            });
        }
    }
}
