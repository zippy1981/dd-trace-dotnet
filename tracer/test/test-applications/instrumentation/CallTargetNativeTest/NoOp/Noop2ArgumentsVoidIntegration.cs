using System;
using Datadog.Trace.ClrProfiler.CallTarget;

namespace CallTargetNativeTest.NoOp
{
    /// <summary>
    /// NoOp Integration for 2 Arguments and Void Return
    /// </summary>
    public static class Noop2ArgumentsVoidIntegration
    {
        public static CallTargetState OnMethodBegin<TTarget, TArg1, TArg2>(TTarget instance, TArg1 arg1, TArg2 arg2)
        {
            CallTargetState returnValue = CallTargetState.GetDefault();
            Console.WriteLine($"ProfilerOK: BeginMethod(2)<{typeof(Noop2ArgumentsVoidIntegration)}, {typeof(TTarget)}, {typeof(TArg1)}, {typeof(TArg2)}>({instance}, {arg1}, {arg2})");
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
            Console.WriteLine($"ProfilerOK: EndMethod(0)<{typeof(Noop2ArgumentsVoidIntegration)}, {typeof(TTarget)}>({instance}, {exception?.ToString() ?? "(null)"}, {state})");
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
