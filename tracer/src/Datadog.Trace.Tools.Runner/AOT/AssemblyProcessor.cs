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
using Datadog.Trace.ClrProfiler;
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
                ReadWrite = true,
            });

            var currentAssemblyName = assemblyDefinition.Name;

            bool isDirty = false;
            var version = new IntegrationVersionRange();
            foreach (var instrumentation in _instrumentation)
            {
                // Check the assembly name
                var asmName = instrumentation.AssemblyNames[0];
                if (asmName != currentAssemblyName.Name)
                {
                    continue;
                }

                version.MinimumVersion = instrumentation.MinimumVersion;
                version.MaximumVersion = instrumentation.MaximumVersion;
                var minimumVersion = new Version(version.MinimumMajor, version.MinimumMinor, version.MinimumPatch);
                var maximumVersion = new Version(version.MaximumMajor, version.MaximumMinor, version.MaximumPatch);

                // Check version (minimum version)
                if (currentAssemblyName.Version < minimumVersion)
                {
                    continue;
                }

                // Check version (maximum version)
                if (currentAssemblyName.Version > maximumVersion)
                {
                    continue;
                }

                // Process integration
                isDirty |= SelectAndProcessIntegration(assemblyDefinition, instrumentation);
            }

            if (isDirty)
            {
                Console.WriteLine($"Done: {filePath}");

                try
                {
                    assemblyDefinition.Write(new WriterParameters
                    {
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine(ex.ToString());
                }

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
            }
        }

        private bool SelectAndProcessIntegration(AssemblyDefinition assemblyDefinition, InstrumentMethodStruct instrumentation)
        {
            var hadInstrumented = false;
            var mainModule = assemblyDefinition.MainModule;
            foreach (var moduleType in mainModule.GetTypes())
            {
                // Check FullType name
                if (moduleType.FullName != instrumentation.TypeName)
                {
                    continue;
                }

                foreach (var typeMethod in moduleType.Methods)
                {
                    // Check Method name
                    if (typeMethod.Name != instrumentation.MethodName)
                    {
                        continue;
                    }

                    // Check Return type
                    if (instrumentation.ReturnTypeName != "_" && typeMethod.ReturnType.FullName != instrumentation.ReturnTypeName)
                    {
                        continue;
                    }

                    // Check methods parameters length
                    if (typeMethod.Parameters.Count != (instrumentation.ParameterTypeNames?.Length ?? 0))
                    {
                        continue;
                    }

                    // Check methods parameters types
                    bool isValid = true;
                    for (int i = 0; i < typeMethod.Parameters.Count; i++)
                    {
                        if (instrumentation.ParameterTypeNames[i] == "_")
                        {
                            continue;
                        }

                        if (instrumentation.ParameterTypeNames[i] != typeMethod.Parameters[i].ParameterType.FullName)
                        {
                            isValid = false;
                            break;
                        }
                    }

                    if (isValid)
                    {
                        // Process method with integration
                        hadInstrumented |= ProcessMethodWithIntegration(typeMethod, ref instrumentation);
                    }
                }
            }

            return hadInstrumented;
        }

        private bool ProcessMethodWithIntegration(MethodDefinition methodDefinition, ref InstrumentMethodStruct instrumentation)
        {
            Console.WriteLine("     " + $"{instrumentation.ReturnTypeName} {instrumentation.TypeName}.{instrumentation.MethodName}({string.Join(", ", instrumentation.ParameterTypeNames ?? Array.Empty<string>())})");
            Console.WriteLine("     " + instrumentation.CallTargetTypeName);
            Console.WriteLine();

            return true;
        }
    }
}
