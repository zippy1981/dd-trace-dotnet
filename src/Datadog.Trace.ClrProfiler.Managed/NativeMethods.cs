using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

// ReSharper disable MemberHidesStaticFromOuterClass
namespace Datadog.Trace.ClrProfiler
{
    internal static class NativeMethods
    {
        private static readonly bool IsWindows = string.Equals(FrameworkDescription.Instance.OSPlatform, "Windows", StringComparison.OrdinalIgnoreCase);

        public static bool IsProfilerAttached()
        {
            if (IsWindows)
            {
                return Windows.IsProfilerAttached();
            }

            return NonWindows.IsProfilerAttached();
        }

        public static void SetIntegrations()
        {
            var sw = Stopwatch.StartNew();

            var attributesFromAssembly = typeof(Instrumentation).Assembly.GetCustomAttributes(inherit: false)
                                                            .Select(a => a as InstrumentMethodAttribute)
                                                            .Where(a => a != null);

            var assemblyTypes = typeof(Instrumentation).Assembly.GetTypes();

            // Extract all InstrumentMethodAttribute at assembly scope level
            var assemblyInstrumentMethodAttributes = from attribute in attributesFromAssembly
                                                     let callTargetClassCheck = attribute.CallTargetType
                                                        ?? throw new NullReferenceException($"The usage of InstrumentMethodAttribute[Type={attribute.TypeName}, Method={attribute.MethodName}] in assembly scope must define the CallTargetType property.")
                                                     select attribute;

            // Extract all InstrumentMethodAttribute from the classes
            var classesInstrumentMethodAttributes = from wrapperType in assemblyTypes
                                                    let attributes = wrapperType.GetCustomAttributes(inherit: false)
                                                            .Select(a => a as InstrumentMethodAttribute)
                                                            .Where(a => a != null)
                                                            .Select(a =>
                                                            {
                                                                a.CallTargetType = wrapperType;
                                                                return a;
                                                            }).ToList()
                                                    from attribute in attributes
                                                    select attribute;

            // combine all InstrumentMethodAttributes
            // and create objects that will generate correct JSON schema
            var callTargetIntegrations = from attribute in assemblyInstrumentMethodAttributes.Concat(classesInstrumentMethodAttributes)
                                         let integrationName = attribute.IntegrationName
                                         let assembly = attribute.CallTargetType.Assembly
                                         let wrapperType = attribute.CallTargetType
                                         orderby integrationName
                                         group new
                                         {
                                             assembly,
                                             wrapperType,
                                             attribute
                                         }
                                         by integrationName into g
                                         from item in g
                                         from assembly in item.attribute.AssemblyNames
                                         select new CallTargetDefinition(
                                             assembly,
                                             item.attribute.TypeName,
                                             item.attribute.MethodName,
                                             new string[] { item.attribute.ReturnTypeName }.Concat(item.attribute.ParameterTypeNames ?? Enumerable.Empty<string>()).ToArray(),
                                             item.attribute.VersionRange.MinimumMajor,
                                             item.attribute.VersionRange.MinimumMinor,
                                             item.attribute.VersionRange.MinimumPatch,
                                             item.attribute.VersionRange.MaximumMajor,
                                             item.attribute.VersionRange.MaximumMinor,
                                             item.attribute.VersionRange.MaximumPatch,
                                             item.assembly.FullName,
                                             item.wrapperType.FullName);

            var integrationsArray = callTargetIntegrations.ToArray();

            Console.Write(sw.Elapsed.TotalMilliseconds);

            if (IsWindows)
            {
                Windows.SetIntegrations(integrationsArray, integrationsArray.Length);
            }
            else
            {
                NonWindows.SetIntegrations(integrationsArray, integrationsArray.Length);
            }

            foreach (var item in integrationsArray)
            {
                item.Dispose();
            }
        }

        private static string GetIntegrationName(Type wrapperType)
        {
            const string integrations = "Integration";
            var typeName = wrapperType.Name;

            if (typeName.EndsWith(integrations, StringComparison.OrdinalIgnoreCase))
            {
                return typeName.Substring(startIndex: 0, length: typeName.Length - integrations.Length);
            }

            return typeName;
        }

        private static string GetMethodSignature(MethodInfo method, InterceptMethodAttribute attribute)
        {
            var returnType = method.ReturnType;
            var parameters = method.GetParameters().Select(p => p.ParameterType).ToArray();

            var requiredParameterTypes = new[] { typeof(int), typeof(int), typeof(long) };
            var lastParameterTypes = parameters.Skip(parameters.Length - requiredParameterTypes.Length);

            if (attribute.MethodReplacementAction == MethodReplacementActionType.ReplaceTargetMethod)
            {
                if (!lastParameterTypes.SequenceEqual(requiredParameterTypes))
                {
                    throw new Exception(
                        $"Method {method.DeclaringType.FullName}.{method.Name}() does not meet parameter requirements. " +
                        "Wrapper methods must have at least 3 parameters and the last 3 must be of types Int32 (opCode), Int32 (mdToken), and Int64 (moduleVersionPtr).");
                }
            }
            else if (attribute.MethodReplacementAction == MethodReplacementActionType.InsertFirst)
            {
                if (attribute.CallerAssembly == null || attribute.CallerType == null || attribute.CallerMethod == null)
                {
                    throw new Exception(
                        $"Method {method.DeclaringType.FullName}.{method.Name}() does not meet InterceptMethodAttribute requirements. " +
                        "Currently, InsertFirst methods must have CallerAssembly, CallerType, and CallerMethod defined. " +
                        $"Current values: CallerAssembly=\"{attribute.CallerAssembly}\", CallerType=\"{attribute.CallerType}\", CallerMethod=\"{attribute.CallerMethod}\"");
                }
                else if (parameters.Any())
                {
                    throw new Exception(
                        $"Method {method.DeclaringType.FullName}.{method.Name}() does not meet parameter requirements. " +
                        "Currently, InsertFirst methods must have zero parameters.");
                }
                else if (returnType != typeof(void))
                {
                    throw new Exception(
                        $"Method {method.DeclaringType.FullName}.{method.Name}() does not meet return type requirements. " +
                        "Currently, InsertFirst methods must have a void return type.");
                }
            }

            var signatureHelper = SignatureHelper.GetMethodSigHelper(method.CallingConvention, returnType);
            signatureHelper.AddArguments(parameters, requiredCustomModifiers: null, optionalCustomModifiers: null);
            var signatureBytes = signatureHelper.GetSignature();

            if (method.IsGenericMethod)
            {
                // if method is generic, fix first byte (calling convention)
                // and insert a second byte with generic parameter count
                const byte IMAGE_CEE_CS_CALLCONV_GENERIC = 0x10;
                var genericArguments = method.GetGenericArguments();

                var newSignatureBytes = new byte[signatureBytes.Length + 1];
                newSignatureBytes[0] = (byte)(signatureBytes[0] | IMAGE_CEE_CS_CALLCONV_GENERIC);
                newSignatureBytes[1] = (byte)genericArguments.Length;
                Array.Copy(signatureBytes, 1, newSignatureBytes, 2, signatureBytes.Length - 1);

                signatureBytes = newSignatureBytes;
            }

            return string.Join(" ", signatureBytes.Select(b => b.ToString("X2")));
        }

        // the "dll" extension is required on .NET Framework
        // and optional on .NET Core
        private static class Windows
        {
            [DllImport("Datadog.Trace.ClrProfiler.Native.dll")]
            public static extern bool IsProfilerAttached();

            [DllImport("Datadog.Trace.ClrProfiler.Native.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int SetIntegrations([In, Out] CallTargetDefinition[] methodArrays, int size);
        }

        // assume .NET Core if not running on Windows
        private static class NonWindows
        {
            [DllImport("Datadog.Trace.ClrProfiler.Native")]
            public static extern bool IsProfilerAttached();

            [DllImport("Datadog.Trace.ClrProfiler.Native", CallingConvention = CallingConvention.Cdecl)]
            public static extern int SetIntegrations([In, Out] CallTargetDefinition[] methodArrays, int size);
        }

#pragma warning disable SA1201 // Elements should appear in the correct order
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CallTargetDefinition
        {
            public string TargetAssembly;

            public string TargetType;

            public string TargetMethod;

            public IntPtr TargetSignatureTypes;

            public ushort TargetSignatureTypesLength;

            public ushort TargetMinimumMajor;

            public ushort TargetMinimumMinor;

            public ushort TargetMinimumPatch;

            public ushort TargetMaximumMajor;

            public ushort TargetMaximumMinor;

            public ushort TargetMaximumPatch;

            public string WrapperAssembly;

            public string WrapperType;

            public CallTargetDefinition(
                string targetAssembly,
                string targetType,
                string targetMethod,
                string[] targetSignatureTypes,
                ushort targetMinimumMajor,
                ushort targetMinimumMinor,
                ushort targetMinimumPatch,
                ushort targetMaximumMajor,
                ushort targetMaximumMinor,
                ushort targetMaximumPatch,
                string wrapperAssembly,
                string wrapperType)
            {
                TargetAssembly = targetAssembly;
                TargetType = targetType;
                TargetMethod = targetMethod;
                TargetSignatureTypes = IntPtr.Zero;
                if (targetSignatureTypes?.Length > 0)
                {
                    TargetSignatureTypes = Marshal.AllocHGlobal(targetSignatureTypes.Length * Marshal.SizeOf(typeof(IntPtr)));
                    var ptr = TargetSignatureTypes;
                    for (var i = 0; i < targetSignatureTypes.Length; i++)
                    {
                        Marshal.WriteIntPtr(ptr, Marshal.StringToHGlobalUni(targetSignatureTypes[i]));
                        ptr += Marshal.SizeOf(typeof(IntPtr));
                    }
                }

                TargetSignatureTypesLength = (ushort)(targetSignatureTypes?.Length ?? 0);
                TargetMinimumMajor = targetMinimumMajor;
                TargetMinimumMinor = targetMinimumMinor;
                TargetMinimumPatch = targetMinimumPatch;
                TargetMaximumMajor = targetMaximumMajor;
                TargetMaximumMinor = targetMaximumMinor;
                TargetMaximumPatch = targetMaximumPatch;
                WrapperAssembly = wrapperAssembly;
                WrapperType = wrapperType;
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal(TargetSignatureTypes);
            }
        }
    }
}
