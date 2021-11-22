// <copyright file="UnsetFrame.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="UnsetFrame.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------
using HAProxy.StreamProcessingOffload.Agent.Payloads;

namespace HAProxy.StreamProcessingOffload.Agent.Frames
{
    internal class UnsetFrame : Frame
    {
        public UnsetFrame()
            : base(FrameType.Unset)
        {
            this.Payload = new RawDataPayload();
        }

        public UnsetFrame(long streamId, long frameId, bool fin, bool abort)
            : this()
        {
            this.Metadata.StreamId = VariableInt.EncodeVariableInt(streamId);
            this.Metadata.FrameId = VariableInt.EncodeVariableInt(frameId);
            this.Metadata.Flags.Fin = fin;
            this.Metadata.Flags.Abort = abort;
        }
    }
}
