// <copyright file="RawDataPayload.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="RawDataPayload.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------
using System.Linq;

namespace HAProxy.StreamProcessingOffload.Agent.Payloads
{
    internal class RawDataPayload : Payload
    {
        private byte[] payloadData;

        public RawDataPayload()
            : base(PayloadType.RawData)
        {
            this.payloadData = new byte[0];
        }

        public override byte[] Bytes
        {
            get
            {
                return this.payloadData;
            }
        }

        public override string ToString()
        {
            return System.Text.Encoding.ASCII.GetString(this.payloadData);
        }

        internal override void Parse(byte[] buffer, ref int offset)
        {
            this.payloadData = buffer.Skip(offset).ToArray();
        }
    }
}
