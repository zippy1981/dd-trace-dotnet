// <copyright file="Payload.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="Payload.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------

namespace HAProxy.StreamProcessingOffload.Agent
{
    internal abstract class Payload
    {
        protected Payload(PayloadType type)
        {
            this.Type = type;
        }

        public PayloadType Type { get; private set; }

        public abstract byte[] Bytes { get; }

        internal abstract void Parse(byte[] buffer, ref int offset);
    }
}
