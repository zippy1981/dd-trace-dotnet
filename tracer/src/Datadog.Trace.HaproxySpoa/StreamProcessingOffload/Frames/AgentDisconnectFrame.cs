// <copyright file="AgentDisconnectFrame.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="AgentDisconnectFrame.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------
using HAProxy.StreamProcessingOffload.Agent.Payloads;

namespace HAProxy.StreamProcessingOffload.Agent.Frames
{
    internal class AgentDisconnectFrame : Frame
    {
        public AgentDisconnectFrame(Status status, string message)
            : base(FrameType.AgentDisconnect)
        {
            this.Metadata.Flags.Fin = true;
            this.Metadata.Flags.Abort = false;
            this.Metadata.StreamId = VariableInt.EncodeVariableInt(0);
            this.Metadata.FrameId = VariableInt.EncodeVariableInt(0);

            var payload = new KeyValueListPayload();
            payload.KeyValueItems.Add("status-code", new TypedData(DataType.Uint32, (uint)status));
            payload.KeyValueItems.Add("message", new TypedData(DataType.String, message));
            this.Payload = payload;
            this.Status = status;
        }
    }
}
