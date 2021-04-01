// Client.cs from https://docs.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/y6dc64f2(v=vs.100)
using System;
using System.Runtime.Remoting;
using Datadog.Trace;

namespace Remoting.DeserializationLevelCrash.Client
{
    public class Program
    {
        public static void Main()
        {
            using (Tracer.Instance.StartActive("Client"))
            {
                RemotingConfiguration.Configure("Remoting.DeserializationLevelCrash.Client.exe.config");
                RemotableType remoteObject = new RemotableType();
                Console.WriteLine(remoteObject.SayHello());
            }
        }
    }
}
