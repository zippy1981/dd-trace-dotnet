// <copyright file="CoverageProcessor.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.IO;

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

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    var asmProcessor = new AssemblyProcessor(dllFile);
                    asmProcessor.ProcessAndSaveTo(asmProcessor.FilePath + ".new.dll");
                }
                catch
                {
                    // .
                }
            }

            foreach (var exeFile in exeFiles)
            {
                try
                {
                    var asmProcessor = new AssemblyProcessor(exeFile);
                    asmProcessor.ProcessAndSaveTo(asmProcessor.FilePath + ".new.dll");
                }
                catch
                {
                    // .
                }
            }
        }
    }
}
