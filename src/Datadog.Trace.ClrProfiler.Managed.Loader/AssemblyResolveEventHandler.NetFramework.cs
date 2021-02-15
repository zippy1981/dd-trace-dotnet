#if NETFRAMEWORK

using System;
using System.Reflection;

namespace Datadog.AutoInstrumentation.ManagedLoader
{
    /// <summary>
    /// See main description in <c>AssemblyLoader.cs</c>
    /// </summary>
    internal partial class AssemblyResolveEventHandler
    {
        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = ParseAssemblyName(args?.Name);
            if (ShouldLoadAssemblyFromProfilerDirectory(assemblyName) && TryFindAssemblyInProfilerDirectory(assemblyName, out string assemblyPath))
            {
                StartupLogger.Debug($"Assembly.LoadFrom(\"{assemblyPath}\")");
                return Assembly.LoadFrom(assemblyPath);
            }

            return null;
        }
    }
}

#endif
