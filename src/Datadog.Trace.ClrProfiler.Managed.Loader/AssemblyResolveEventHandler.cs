using System;
using System.Collections.Generic;
using System.Reflection;

namespace Datadog.AutoInstrumentation.ManagedLoader
{
    /// <summary>
    /// See main description in <c>AssemblyLoader.cs</c>
    /// </summary>
    internal partial class AssemblyResolveEventHandler
    {
        private readonly IReadOnlyList<string> _assemblyNamesToLoad;
        private readonly IReadOnlyList<string> _managedProductBinariesDirectories;

        public AssemblyResolveEventHandler(IReadOnlyList<string> assemblyNamesToLoad, IReadOnlyList<string> managedProductBinariesDirectories)
        {
            _assemblyNamesToLoad = (assemblyNamesToLoad) == null ? new string[0] : assemblyNamesToLoad;
            _managedProductBinariesDirectories = (managedProductBinariesDirectories) == null ? new string[0] : managedProductBinariesDirectories;
        }

        internal IReadOnlyList<string> AssemblyNamesToLoad
        {
            get { return _assemblyNamesToLoad; }
        }

        internal IReadOnlyList<string> ManagedProductBinariesDirectories
        {
            get { return _managedProductBinariesDirectories; }
        }

        private static AssemblyName ParseAssemblyName(string fullAssemblyName)
        {
            if (string.IsNullOrEmpty(fullAssemblyName))
            {
                return null;
            }

            try
            {
                var assemblyName = new AssemblyName(fullAssemblyName);
                return assemblyName;
            }
            catch
            {
                return null;
            }
        }

        private bool ShouldLoadAssemblyFromProfilerDirectory(AssemblyName assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName?.Name) || string.IsNullOrEmpty(assemblyName?.FullName))
            {
                return false;
            }

            // The AssemblyResolveEventHandler will handle all assemblies that have these prefixes:
            // If the could not be found, we will look for them in the TRACER_HOME:

            bool shouldHandle = (assemblyName.Name.StartsWith("Datadog.Trace", StringComparison.OrdinalIgnoreCase) == true)
                    || (assemblyName.Name.StartsWith("Datadog.AutoInstrumentation", StringComparison.OrdinalIgnoreCase) == true);

            // If an assembly does not have the above prefix, but it has been specified as the actual startup assembly,
            // we will also look for it in TRACER_HOME:

            for (int i = 0; !shouldHandle && i < _assemblyNames.Length; i++)
            {
                shouldHandle = assemblyName.FullName.Equals(_assemblyNames[i], StringComparison.OrdinalIgnoreCase);
            }

            return shouldHandle;
        }

        private bool TryFindAssemblyInProfilerDirectory(AssemblyName assemblyName, out string fullPath)
        {
            fullPath = Path.Combine(s_managedProfilerDirectory, $"{assemblyName.Name}.dll");
            return File.Exists(fullPath);
        }
    }
}
