﻿// <auto-generated />
// This .CS file is automatically generated. If you modify its contents, your changes will be overwritten.
// Modify the respective T4 templates if changes are required.

// <auto-generated />
// ----------- ----------- ----------- ----------- -----------
// The source code below is included via a T4 template.
//  * The template calling MUST specify the value of the <c>LogNamespaceName</c> meta-variable.
//  * The template calling MAY specify the value of the <c>LogClassAccessibilityLevel</c> meta-variable,
//    however its default value "public" is the appropriate choice in most cases
//    (other values would prevent composability by other assemblies).
// ----------- ----------- ----------- ----------- -----------

using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Datadog.Logging.Emission;

namespace Datadog.Logging.Demo.EmitterLib
{
    /// <summary>
    /// Leightweight Log stub for Logging-SDK-agnostic logging.
    /// Users of this library can use this class as a leighweight redirect to whatever log technology is used for output.
    /// An absolute minimum of dependencies is required: 3 small static classes that are included as source code (no library dependency).
    /// The only namespaces importand by those 3 static classes are (see also <c>Datadog.Logging.Emission.props</c>):
    ///  - System
    ///  - System.Collections.Generic
    ///  - System.Diagnostics (only to get Process.GetCurrentProcess().Id)
    ///  - System.Runtime.CompilerServices (on;y for [MethodImpl(MethodImplOptions.AggressiveInlining)])
    ///  - System.Text
    /// 
    /// This allows to avoid creating complex logging abstractions (or taking dependencies on ILogger or other logging libraries).
    /// This class is re-generated in each project wants to use it using T4. The only thing that T4 does is using a user-specified namespace.
    /// Projects that wish to avoid using T4, can copy this file and hard-code the namespace (beware for source-forking).
    /// 
    /// <para>EMITTING LOGS.</para>
    /// <para>
    /// For example:
    /// 
    /// Library "Datadog.AutoInstrumentation.Profiler.Managed.dll" gets a copy of this file with the adjusted namespace:
    /// 
    /// <code>
    ///   namespace Datadog.AutoInstrumentation.Profiler.Managed
    ///   {
    ///       public static class Log
    ///       {
    ///       . . .
    ///       }
    ///   }
    /// </code>
    /// 
    /// Library "Datadog.AutoInstrumentation.Tracer.Managed.dll" also gets a copy of this file with the adjusted namespace:
    /// 
    /// <code>
    ///   namespace Datadog.AutoInstrumentation.Tracer.Managed
    ///   {
    ///       public static class Log
    ///       {
    ///       . . .
    ///       }
    ///   }
    /// </code>  
    /// 
    /// Each library can now make Log statements, for example:
    /// 
    /// <code>
    ///   Log.Info("DataExporter", "Data transport started", "size", _size, "otherAttribute", _otherAttribute);
    /// </code>  
    /// </para>
    /// 
    /// <para>COMPOSING AND PERSISTING LOGS.</para>
    /// <para>
    /// To continue the above example, assume that the entrypoint of the application is another library "Datadog.AutoInstrumentation.TracerAndProfilerLoader.dll".
    /// It uses the the two above libraries and it wants to direct the logs to some particular logging destnation (sink).
    /// For that, the TracerAndProfilerLoader takes a dependencty on a few additional source files.
    /// Those are also small, do not have any non-framework dependencies and run on Net Fx and Core Fx (see also <c>Datadog.Logging.Composition.props</c>).
    /// It creates a trivial adaper and configures the indirection.
    /// If short, the redirection happens as shown below.
    /// A fully rubust example is in the <c>Datadog.Logging.Demo</c> project, and log sinks are included for
    ///  - Console
    ///  - Files (with optional rotation)
    ///  - COmposing multiple sinks together
    /// 
    /// <code>
    ///   namespace Datadog.AutoInstrumentation.TracerAndProfilerLoader
    ///   {
    ///       using ComposerLogAdapter = Datadog.AutoInstrumentation.ProductComposer.LogAdapter;
    ///       using ProfilerLog = Datadog.AutoInstrumentation.Profiler.Managed.Log;
    ///       using TracerLog = Datadog.AutoInstrumentation.Tracer.Managed.Log;
    ///       
    ///       internal static class LogComposer
    ///       {
    ///           public const bool IsDebugLoggingEnabled = true;
    ///           
    ///           static LogComposer()
    ///           {
    ///               ProfilerLog.Log.Configure.Error((component, msg, ex, data) => LogError("Profiler", component, msg, ex, data));
    ///               ProfilerLog.Log.Configure.Info((component, msg, data) => LogInfo("Profiler", component, msg, data));
    ///               ProfilerLog.Log.Configure.Debug((component, msg, data) => LogDebug("Profiler", component, msg, data));
    ///               ProfilerLog.Log.Configure.DebugLoggingEnabled(IsDebugLoggingEnabled);
    ///               
    ///               TracerLog.Log.Configure.Error((component, msg, ex, data) => LogError("Tracer", component, msg, ex, data));
    ///               TracerLog.Log.Configure.Info((component, msg, data) => LogInfo("Tracer", component, msg, data));
    ///               TracerLog.Log.Configure.Debug((component, msg, data) => LogDebug("Tracer", component, msg, data));
    ///               TracerLog.Log.Configure.DebugLoggingEnabled(IsDebugLoggingEnabled);
    ///           }
    ///   
    ///           private static void LogError(string logGroupMoniker, string logComponentMoniker, string message, Exception error, IEnumerable<object> dataNamesAndValues)
    ///           {
    ///               // Prepare a log line in any appropriate way. For example:
    ///               string logLine = DefaultFormat.ConstructLogLine(DefaultFormat.LogLevelMoniker_Error,
    ///                                                               logGroupMoniker,
    ///                                                               logComponentMoniker,
    ///                                                               useUtcTimestamp: false,
    ///                                                               DefaultFormat.ConstructErrorMessage(message, error),
    ///                                                               dataNamesAndValues)
    ///                                             .ToString());
    ///               // Persist logLine to file...
    ///           }
    ///
    ///           private static void LogInfo(string logGroupMoniker, string logComponentMoniker, string message, IEnumerable<object> dataNamesAndValues)
    ///           {
    ///               // Prepare a log line (e.g. like above) and persist it to file...
    ///           }
    ///
    ///           private static void LogDebug(string logGroupMoniker, string logComponentMoniker, string message, IEnumerable<object> dataNamesAndValues)
    ///           {
    ///               if (IsDebugLoggingEnabled)
    ///               {
    ///                   // Prepare a log line (e.g. like above) and persist it to file...
    ///               }
    ///           }
    ///       }
    ///   }
    /// </code>
    /// </para>
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Use statements like <c>Log.Configure.Info(YourHandler)</c> to redirect logging to your destination.
        /// </summary>
        public static class Configure
        {
            /// <summary>
            /// Sets the handler delegate for processing Error log events.
            /// If <c>null</c> is specified, then Error log events will be ignored.
            /// </summary>
            public static void Error(Action<string, string, Exception, IEnumerable<object>> logEventHandler)
            {
                s_errorLogEventHandler = logEventHandler;
            }

            /// <summary>
            /// Sets the handler delegate for processing Info log events.
            /// If <c>null</c> is specified, then Error log events will be ignored.
            /// </summary>
            public static void Info(Action<string, string, IEnumerable<object>> logEventHandler)
            {
                s_infoLogEventHandler = logEventHandler;
            }

            /// <summary>
            /// Sets the handler delegate for processing Debug log events.
            /// If <c>null</c> is specified, then Error log events will be ignored.
            /// </summary>
            public static void Debug(Action<string, string, IEnumerable<object>> logEventHandler)
            {
                s_debugLogEventHandler = logEventHandler;
            }

            /// <summary>
            /// Sets whether Debug log events should be processed or ignored.
            /// </summary>
            public static void DebugLoggingEnabled(bool isDebugLoggingEnabled)
            {
                s_isDebugLoggingEnabled = isDebugLoggingEnabled;
            }
        }  // class Log.Configure

        private static Action<string, string, Exception, IEnumerable<object>> s_errorLogEventHandler = SimpleConsoleSink.Error;
        private static Action<string, string, IEnumerable<object>> s_infoLogEventHandler = SimpleConsoleSink.Info;
        private static Action<string, string, IEnumerable<object>> s_debugLogEventHandler = SimpleConsoleSink.Debug;
        private static bool s_isDebugLoggingEnabled = SimpleConsoleSink.IsDebugLoggingEnabled;

        internal static LogSourceInfo WithCallInfo(string logSourceName,
                                                   [CallerLineNumber] int callLineNumber = 0,
                                                   [CallerMemberName] string callMemberName = null)
        {
            return new LogSourceInfo(namePart1: null, namePart2: logSourceName, callLineNumber, callMemberName, callFileName: null);
        }

        internal static LogSourceInfo WithCallInfo(LogSourceInfo logSourceInfo,
                                                   [CallerLineNumber] int callLineNumber = 0,
                                                   [CallerMemberName] string callMemberName = null)
        {
            return logSourceInfo.WithCallInfo(callLineNumber, callMemberName);
        }

        /// <summary>
        /// Gets whether debug log messages should be processed or ignored.
        /// Consider wrapping debug message invocations into IF statements that check for this
        /// value in order to avoid unnecessarily constructing debug message strings.
        /// </summary>
        public static bool IsDebugLoggingEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return s_isDebugLoggingEnabled; }
        }

        /// <summary>
        /// Logs an error.
        /// These need to be persisted well, so that the info is available for support cases.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(string componentName, string message, params object[] dataNamesAndValues)
        {
            Error(componentName, message, exception: null, dataNamesAndValues);
        }

        /// <summary>
        /// Logs an error.
        /// These need to be persisted well, so that the info is available for support cases.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(string componentName, Exception exception, params object[] dataNamesAndValues)
        {
            Error(componentName, message: null, exception, dataNamesAndValues);
        }

        /// <summary>
        /// This method logs and rethrows the exception. It is typed to return the exception, to enable writing concise code like:
        /// <code>
        ///   try
        ///   {
        ///       // ...
        ///   }
        ///   catch (Exception ex)
        ///   {
        ///       throw Log.ErrorRethrow("...", ex);
        ///   }
        /// </code>
        /// This is becasue the compiler does not know that this method throws and it may otherwise require code blow to
        /// add no-op return statements and similar.
        /// </summary>
        /// <returns>Either <c>null</c> if the specified <c>exception</c> is <c>null</c>, or nothing at all,
        /// because the specified <c>exception</c> is rethrown.</returns>
        public static Exception ErrorRethrow(string componentName, Exception exception, params object[] dataNamesAndValues)
        {
            Error(componentName, message: null, exception, dataNamesAndValues);

            if (exception != null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            return exception;
        }

        /// <summary>
        /// Logs an error.
        /// These need to be persisted well, so that the info is available for support cases.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(string componentName, string message, Exception exception, params object[] dataNamesAndValues)
        {
            Action<string, string, Exception, object[]> logEventHandler = s_errorLogEventHandler;
            if (logEventHandler != null)
            {
                logEventHandler(componentName, message, exception, dataNamesAndValues);
            }
        }

        /// <summary>
        /// This method logs and rethrows the exception. It is typed to return the exception, to enable writing concise code like:
        /// <code>
        ///   try
        ///   {
        ///       // ...
        ///   }
        ///   catch (Exception ex)
        ///   {
        ///       throw Log.ErrorRethrow("...", ex);
        ///   }
        /// </code>
        /// This is becasue the compiler does not know that this method throws and it may otherwise require code blow to
        /// add no-op return statements and similar.
        /// </summary>
        /// <returns>Either <c>null</c> if the specified <c>exception</c> is <c>null</c>, or nothing at all,
        /// because the specified <c>exception</c> is rethrown.</returns>
        public static Exception ErrorRethrow(string componentName, string message, Exception exception, params object[] dataNamesAndValues)
        {
            Error(componentName, message: null, exception, dataNamesAndValues);

            if (exception != null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            return exception;
        }

        /// <summary>
        /// Logs an important info message.
        /// These need to be persisted well, so that the info is available for support cases.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(string componentName, string message, params object[] dataNamesAndValues)
        {
            Action<string, string, object[]> logEventHandler = s_infoLogEventHandler;
            if (logEventHandler != null)
            {
                logEventHandler(componentName, message, dataNamesAndValues);
            }
        }

        /// <summary>
        /// Logs a non-critical info message. Mainly used for for debugging during prototyping.
        /// These messages can likely be dropped in production.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Debug(string componentName, string message, params object[] dataNamesAndValues)
        {
            if (IsDebugLoggingEnabled)
            { 
                Action<string, string, object[]> logEventHandler = s_debugLogEventHandler;
                if (logEventHandler != null)
                {
                    logEventHandler(componentName, message, dataNamesAndValues);
                }
            }
        }
    }  // class Log
}  // namespace
