using System;
using Datadog.Trace.ClrProfiler.CallTarget;

namespace CallTargetNativeTest.NoOp
{
    /// <summary>
    /// NoOp Integration for 4 Arguments and Void Return
    /// </summary>
    public static class Noop4ArgumentsVoidIntegration
    {
        public static CallTargetState OnMethodBegin<TTarget, TArg1, TArg2, TArg3, TArg4>(TTarget instance, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            CallTargetState returnValue = CallTargetState.GetDefault();
            Console.WriteLine($"ProfilerOK: BeginMethod(4)<{typeof(Noop4ArgumentsVoidIntegration)}, {typeof(TTarget)}, {typeof(TArg1)}, {typeof(TArg2)}, {typeof(TArg3)}, {typeof(TArg4)}>({instance}, {arg1}, {arg2}, {arg3}, {arg4})");
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
            Console.WriteLine($"ProfilerOK: EndMethod(0)<{typeof(Noop4ArgumentsVoidIntegration)}, {typeof(TTarget)}>({instance}, {exception?.ToString() ?? "(null)"}, {state})");
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
