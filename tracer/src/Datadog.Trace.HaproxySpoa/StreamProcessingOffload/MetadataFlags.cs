// <copyright file="MetadataFlags.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="MetadataFlags.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections;

namespace HAProxy.StreamProcessingOffload.Agent
{
    internal class MetadataFlags
    {
        public bool Fin { get; set; }

        public bool Abort { get; set; }

        public byte[] Bytes
        {
            get
            {
                BitArray bits = new BitArray(32);
                bits[0] = this.Fin;
                bits[1] = this.Abort;

                for (int i = 2; i < 32; i++)
                {
                    bits[i] = false;
                }

                var bytes = new byte[4];
                bits.CopyTo(bytes, 0);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }

                return bytes;
            }
        }
    }
}
