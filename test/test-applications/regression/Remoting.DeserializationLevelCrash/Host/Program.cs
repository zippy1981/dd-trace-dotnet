// Listener.cs from https://docs.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/ecc85927(v=vs.100)
using System;
using System.Runtime.Remoting;

namespace Remoting.DeserializationLevelCrash.Host
{
    public class Program
    {
        public static void Main()
        {
            RemotingConfiguration.Configure("Remoting.DeserializationLevelCrash.Host.exe.config", false);
            Console.WriteLine("Listening for requests. Press enter to exit...");
            Console.ReadLine();
        }

    }
}
