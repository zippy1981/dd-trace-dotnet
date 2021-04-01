// RemotableType.cs from https://docs.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/txct33xt(v=vs.100)
using System;

public class RemotableType : MarshalByRefObject
{
    public string SayHello()
    {
        Console.WriteLine("RemotableType.SayHello() was called!");
        return "Hello, world";
    }
}
