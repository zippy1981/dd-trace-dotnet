// <copyright file="VariableInt.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="VariableInt.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;

namespace HAProxy.StreamProcessingOffload.Agent
{
    internal class VariableInt
    {
        private VariableInt(long value, int length, byte[] bytes)
        {
            this.Value = value;
            this.Length = length;
            this.Bytes = bytes;
        }

        private VariableInt(ulong value, int length, byte[] bytes)
        {
            this.UnsignedValue = value;
            this.Length = length;
            this.Bytes = bytes;
        }

        public long Value { get; private set; }

        public ulong UnsignedValue { get; private set; }

        public int Length { get; private set; }

        public byte[] Bytes { get; private set; }

        public static VariableInt DecodeVariableInt(byte[] buffer)
        {
            long value = buffer[0];
            int length = 0;
            byte[] valueBytes = new byte[0];

            if (value < 240)
            {
                length++;
                valueBytes = buffer.Take(length).ToArray();
            }
            else
            {
                int shift = 4;

                do
                {
                    length++;
                    long nextByte = buffer.Skip(length).First();
                    value += nextByte << shift;
                    shift += 7;
                }
                while (buffer.Skip(length).First() >= 128);

                length++;

                valueBytes = buffer.Take(length).ToArray();
            }

            return new VariableInt(value, length, valueBytes);
        }

        public static VariableInt DecodeVariableUnsignedInt(byte[] buffer)
        {
            var varint = DecodeVariableInt(buffer);
            return new VariableInt((ulong)varint.Value, varint.Length, varint.Bytes);
        }

        public static VariableInt EncodeVariableInt(long value)
        {
            byte[] valueBytes = CalculateBytes(value);
            return new VariableInt(value, valueBytes.Length, valueBytes);
        }

        public static VariableInt EncodeVariableUint(ulong value)
        {
            byte[] valueBytes = CalculateUnsignedIntBytes(value);
            return new VariableInt(value, valueBytes.Length, valueBytes);
        }

        private static byte[] CalculateBytes(long value)
        {
            IList<byte> bytes = new List<byte>();

            if (value < 240)
            {
                bytes.Add((byte)value);
                return bytes.ToArray();
            }

            bytes.Add((byte)(value | 240));
            value = (value - 240) >> 4;

            while (value >= 128)
            {
                bytes.Add((byte)(value | 128));
                value = (value - 128) >> 7;
            }

            bytes.Add((byte)value);
            return bytes.ToArray();
        }

        private static byte[] CalculateUnsignedIntBytes(ulong value)
        {
            return CalculateBytes((long)value);
        }
    }
}
