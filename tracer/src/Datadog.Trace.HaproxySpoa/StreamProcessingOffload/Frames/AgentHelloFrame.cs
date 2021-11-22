// <copyright file="AgentHelloFrame.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="AgentHelloFrame.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------
using HAProxy.StreamProcessingOffload.Agent.Payloads;

namespace HAProxy.StreamProcessingOffload.Agent.Frames
{
    internal class AgentHelloFrame : Frame
    {
        public AgentHelloFrame(string supportedSpopVersion, uint maxFrameSize, string[] capabilities)
            : base(FrameType.AgentHello)
        {
            this.Metadata.Flags.Fin = true;
            this.Metadata.Flags.Abort = false;
            this.Metadata.StreamId = VariableInt.EncodeVariableInt(0);
            this.Metadata.FrameId = VariableInt.EncodeVariableInt(0);

            var payload = new KeyValueListPayload();
            payload.KeyValueItems.Add("version", new TypedData(DataType.String, supportedSpopVersion));
            payload.KeyValueItems.Add("max-frame-size", new TypedData(DataType.Uint32, maxFrameSize));
            payload.KeyValueItems.Add("capabilities", new TypedData(DataType.String, string.Join(",", capabilities)));
            this.Payload = payload;
        }
    }
}
