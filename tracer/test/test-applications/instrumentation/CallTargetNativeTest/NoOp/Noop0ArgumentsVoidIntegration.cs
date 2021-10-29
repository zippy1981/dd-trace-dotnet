using System;
using Datadog.Trace.ClrProfiler.CallTarget;

namespace CallTargetNativeTest.NoOp
{
    /// <summary>
    /// NoOp Integration for 0 Arguments and Void Return
    /// </summary>
    public static class Noop0ArgumentsVoidIntegration
    {
        public static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
        {
            CallTargetState returnValue = CallTargetState.GetDefault();
            Console.WriteLine($"ProfilerOK: BeginMethod(0)<{typeof(Noop0ArgumentsVoidIntegration)}, {typeof(TTarget)}>({instance})");
            if (instance?.GetType().Name.Contains("ThrowOnBegin") == true)
            {
                Console.WriteLine("Exception thrown.");
                throw new Exception();
            }
            else if (instance?.GetType().Name.Contains("ThrowUnhandledOnBegin") == true)
            {
                Console.WriteLine($"{nameof(UnhandledOnBeginException)} thrown.");
                throw new UnhandledOnBeginException();
            }

            return returnValue;
        }

        public static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, CallTargetState state)
        {
            CallTargetReturn returnValue = CallTargetReturn.GetDefault();
            Console.WriteLine($"ProfilerOK: EndMethod(0)<{typeof(Noop0ArgumentsVoidIntegration)}, {typeof(TTarget)}>({instance}, {exception?.ToString() ?? "(null)"}, {state})");
            if (instance?.GetType().Name.Contains("ThrowOnEnd") == true)
            {
                Console.WriteLine("Exception thrown.");
                throw new Exception();
            }
            else if (instance?.GetType().Name.Contains("ThrowUnhandledOnEnd") == true)
            {
                Console.WriteLine($"{nameof(UnhandledOnEndException)} thrown.");
                throw new UnhandledOnEndException();
            }

            return returnValue;
        }

        // Omit a OnCallTargetExceptionThrow method to demonstrate that exceptions will always be caught when we do not specifically handle exceptions
    }
}
