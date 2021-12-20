// <copyright file="InstrumentMethodStruct.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.Tools.Runner.AOT
{
    [DuckCopy]
    internal struct InstrumentMethodStruct : IEquatable<InstrumentMethodStruct>
    {
        public string[] AssemblyNames;
        public string TypeName;
        public string MethodName;
        public string ReturnTypeName;
        public string[] ParameterTypeNames;
        public string MinimumVersion;
        public string MaximumVersion;
        public string IntegrationName;
        public Type CallTargetType;
        [DuckIgnore]
        public string CallTargetTypeName;

        private bool Equals(InstrumentMethodStruct other) =>
            IntegrationName == other.IntegrationName &&
            TypeName == other.TypeName &&
            MethodName == other.MethodName &&
            ReturnTypeName == other.ReturnTypeName &&
            AssemblyNames?.Length == other.AssemblyNames?.Length &&
            ParameterTypeNames?.Length == other.ParameterTypeNames?.Length &&
            MinimumVersion == other.MinimumVersion &&
            MaximumVersion == other.MaximumVersion &&
            IntegrationName == other.IntegrationName &&
            CallTargetType.FullName == other.CallTargetType.FullName &&
            string.Join(',', AssemblyNames ?? Array.Empty<string>()) == string.Join(',', other.AssemblyNames ?? Array.Empty<string>()) &&
            string.Join(',', ParameterTypeNames ?? Array.Empty<string>()) == string.Join(',', other.ParameterTypeNames ?? Array.Empty<string>());

        public override bool Equals(object obj)
        {
            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((InstrumentMethodStruct)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = TypeName?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (MethodName?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (ReturnTypeName?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (MinimumVersion?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (MaximumVersion?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (IntegrationName?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        bool IEquatable<InstrumentMethodStruct>.Equals(InstrumentMethodStruct other)
        {
            if (other.GetType() != this.GetType())
            {
                return false;
            }

            return Equals(other);
        }
    }
}
