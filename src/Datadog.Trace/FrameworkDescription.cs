using System;
using System.Reflection;
using Datadog.Trace.Logging;

namespace Datadog.Trace
{
    internal partial class FrameworkDescription
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(FrameworkDescription));

        private static readonly Assembly RootAssembly = typeof(object).Assembly;

        private FrameworkDescription(
            string name,
            string productVersion,
            string osPlatform,
            string osArchitecture,
            string processArchitecture)
        {
            Name = name;
            ProductVersion = productVersion;
            OSPlatform = osPlatform;
            OSArchitecture = osArchitecture;
            ProcessArchitecture = processArchitecture;
        }

        public string Name { get; }

        public string ProductVersion { get; }

        public string OSPlatform { get; }

        public string OSArchitecture { get; }

        public string ProcessArchitecture { get; }

        public override string ToString()
        {
            // examples:
            // .NET Framework 4.8 x86 on Windows x64
            // .NET Core 3.0.0 x64 on Linux x64
            return $"{Name} {ProductVersion} {ProcessArchitecture} on {OSPlatform} {OSArchitecture}";
        }

        private static string GetVersionFromAssemblyAttributes()
        {
            string productVersion = null;

            try
            {
                // if we fail to extract version from assembly path, fall back to the [AssemblyInformationalVersion],
                var informationalVersionAttribute = (AssemblyInformationalVersionAttribute)RootAssembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));

                // split remove the commit hash from pre-release versions
                productVersion = informationalVersionAttribute?.InformationalVersion?.Split('+')[0];
            }
            catch (Exception e)
            {
                Log.Error(e, "Error getting framework version from [AssemblyInformationalVersion]");
            }

            if (productVersion == null)
            {
                try
                {
                    // and if that fails, try [AssemblyFileVersion]
                    var fileVersionAttribute = (AssemblyFileVersionAttribute)RootAssembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute));
                    productVersion = fileVersionAttribute?.Version;
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error getting framework version from [AssemblyFileVersion]");
                }
            }

            return productVersion;
        }
    }
}
