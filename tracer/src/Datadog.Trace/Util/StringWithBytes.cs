// <copyright file="StringWithBytes.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Runtime.CompilerServices;
using Datadog.Trace.Vendors.MessagePack;

namespace Datadog.Trace.Util
{
    /// <summary>
    /// String with same value in bytes
    /// </summary>
    public readonly struct StringWithBytes
    {
        internal readonly string Value;
        internal readonly byte[] ValueInBytes;

        internal StringWithBytes(string value)
        {
            Value = value;
            if (value is null)
            {
                ValueInBytes = Array.Empty<byte>();
            }
            else
            {
                ValueInBytes = StringEncoding.UTF8.GetBytes(value);
            }
        }

        /// <summary>
        /// Gets the string value
        /// </summary>
        /// <param name="value">StringWithBytes value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator string(StringWithBytes value) => value.Value;

        /// <summary>
        /// Gets the StringWithBytes value
        /// </summary>
        /// <param name="value">String value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StringWithBytes(string value) => new StringWithBytes(value);

        /// <summary>
        /// Equality operator
        /// </summary>
        /// <param name="value1">Value 1</param>
        /// <param name="value2">Value 2</param>
        /// <returns>True if equal</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(StringWithBytes value1, StringWithBytes value2)
        {
            return value1.Value == value2.Value;
        }

        /// <summary>
        /// Inequality operator
        /// </summary>
        /// <param name="value1">Value 1</param>
        /// <param name="value2">Value 2</param>
        /// <returns>True if equal</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(StringWithBytes value1, StringWithBytes value2)
        {
            return value1.Value != value2.Value;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj is StringWithBytes swb)
            {
                return Value == swb.Value;
            }

            return false;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return Value;
        }
    }
}
