// <copyright file="AOTProcessor.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Tools.Runner.AOT
{
    internal class AOTProcessor
    {
        private static readonly string InstrumentMethodAttributeName = typeof(Datadog.Trace.ClrProfiler.InstrumentMethodAttribute).FullName;
        private static readonly HashSet<InstrumentMethodStruct> Instrumentations = new HashSet<InstrumentMethodStruct>();
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(AOTProcessor));

        static AOTProcessor()
        {
            string runnerFolder = Program.RunnerFolder;
            string tracerHome = Utils.DirectoryExists("Home", Path.Combine(runnerFolder, "..", "..", "..", "home"), Path.Combine(runnerFolder, "home"));
            string[] managedLibs = Directory.GetFiles(tracerHome, "Datadog.Trace.dll", SearchOption.AllDirectories);

            foreach (var path in managedLibs)
            {
                var assemblyLoadContext = new CustomAssemblyLoadContext();
                var assembly = assemblyLoadContext.LoadFromAssemblyPath(path);

                var instrumentationAssemblyAttributes = from attribute in assembly.GetCustomAttributes(inherit: false)
                                                        where InheritsFrom(attribute.GetType(), InstrumentMethodAttributeName)
                                                        let ims = attribute.DuckCast<InstrumentMethodStruct>()
                                                        from assemblyName in ims.AssemblyNames
                                                        select new InstrumentMethodStruct
                                                        {
                                                            AssemblyNames = new[] { assemblyName },
                                                            CallTargetType = ims.CallTargetType,
                                                            IntegrationName = ims.IntegrationName,
                                                            MaximumVersion = ims.MaximumVersion,
                                                            MethodName = ims.MethodName,
                                                            MinimumVersion = ims.MinimumVersion,
                                                            ParameterTypeNames = ims.ParameterTypeNames,
                                                            ReturnTypeName = ims.ReturnTypeName,
                                                            TypeName = ims.TypeName,
                                                            CallTargetTypeName = ims.CallTargetType.FullName,
                                                        };
                AddRange(Instrumentations, instrumentationAssemblyAttributes);

                // Extract all InstrumentMethodAttribute from the classes
                var classesInstrumentMethodAttributes = from wrapperType in GetLoadableTypes(assembly)
                                                        let attributes = wrapperType.GetCustomAttributes(inherit: false)
                                                                                    .Where(a => InheritsFrom(a.GetType(), InstrumentMethodAttributeName))
                                                                                    .Select(i => i.DuckCast<InstrumentMethodStruct>())
                                                        from attribute in attributes
                                                        from assemblyName in attribute.AssemblyNames
                                                        select new InstrumentMethodStruct
                                                        {
                                                            AssemblyNames = new[] { assemblyName },
                                                            CallTargetType = wrapperType,
                                                            IntegrationName = attribute.IntegrationName,
                                                            MaximumVersion = attribute.MaximumVersion,
                                                            MethodName = attribute.MethodName,
                                                            MinimumVersion = attribute.MinimumVersion,
                                                            ParameterTypeNames = attribute.ParameterTypeNames,
                                                            ReturnTypeName = attribute.ReturnTypeName,
                                                            TypeName = attribute.TypeName,
                                                            CallTargetTypeName = wrapperType.FullName,
                                                        };
                AddRange(Instrumentations, classesInstrumentMethodAttributes);
            }

            return;

            static bool InheritsFrom(Type type, string baseType)
            {
                if (type.FullName == baseType)
                {
                    return true;
                }

                if (type.BaseType is null)
                {
                    return false;
                }

                return InheritsFrom(type.BaseType, baseType);
            }

            static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    // Ignore types that cannot be loaded. In particular, TracingHttpModule inherits from
                    // IHttpModule, which is not available to the nuke builds because they run on net5.0.
                    return e.Types.Where(t => t != null);
                }
            }

            static void AddRange(HashSet<InstrumentMethodStruct> hashSet, IEnumerable<InstrumentMethodStruct> items)
            {
                foreach (var item in items)
                {
                    hashSet.Add(item);
                }
            }
        }

        public void Execute(string folder)
        {
            var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file)?.ToLowerInvariant() ?? string.Empty;
                if (extension == string.Empty || extension == ".dll" || extension == ".exe")
                {
                    if (Path.GetFileName(file) == "Datadog.Trace.dll")
                    {
                        continue;
                    }

                    try
                    {
                        var asmProcessor = new AssemblyProcessor(file, Instrumentations);
                        asmProcessor.ProcessAndSaveTo();
                    }
                    catch (BadImageFormatException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
        }
    }
}
