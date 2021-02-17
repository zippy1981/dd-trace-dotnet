using System;

namespace Datadog.AutoInstrumentation.ManagedLoader
{
    using ManagedLoaderLog = Datadog.AutoInstrumentation.ManagedLoader.Log;

    internal static class LogConfigurator
    {
        public static void Setup()
        {
            ManagedLoaderLog.Configure.Info(null);
        }
    }
}
