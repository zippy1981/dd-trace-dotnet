// <copyright file="AssemblyProcessor.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Datadog.Trace.Logging;
using Mono.Cecil;

namespace Datadog.Trace.Tools.Runner.AOT
{
    internal class AssemblyProcessor
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(AssemblyProcessor));

        private readonly string _assemblyFilePath;
        private readonly HashSet<InstrumentMethodStruct> _instrumentation;

        public AssemblyProcessor(string filePath, HashSet<InstrumentMethodStruct> instrumentation)
        {
            _assemblyFilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _instrumentation = instrumentation;

            if (!File.Exists(_assemblyFilePath))
            {
                throw new FileNotFoundException($"Assembly not found in path: {_assemblyFilePath}");
            }
        }

        public string FilePath => _assemblyFilePath;

        public void ProcessAndSaveTo()
        {
            var filePath = FilePath;

            using var assemblyDefinition = AssemblyDefinition.ReadAssembly(_assemblyFilePath, new ReaderParameters
            {
                ReadSymbols = false,
                ReadWrite = true
            });

            Console.WriteLine(assemblyDefinition.Name.FullName);

            bool isDirty = false;

            var instrumentations = _instrumentation.Where(i =>
            {
                var asmName = i.AssemblyNames[0];
                if (asmName != assemblyDefinition.Name.Name)
                {
                    return false;
                }

                return true;
            }).ToList();

            if (instrumentations.Count == 0)
            {
                return;
            }

            if (isDirty)
            {
                assemblyDefinition.Write(new WriterParameters
                {
                    WriteSymbols = true,
                });
            }

            Console.WriteLine($"Done: {filePath}");
        }
    }
}
