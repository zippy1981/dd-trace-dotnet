#if NETCOREAPP
using System.Reflection;
using System.Runtime.Loader;

namespace Datadog.AutoInstrumentation.ManagedLoader
{
    internal class ManagedProfilerAssemblyLoadContext : AssemblyLoadContext
    {
        public static readonly AssemblyLoadContext SingeltonInstance = new ManagedProfilerAssemblyLoadContext();

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}
#endif
