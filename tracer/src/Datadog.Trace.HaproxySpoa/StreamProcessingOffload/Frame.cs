// <copyright file="Frame.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="Frame.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace HAProxy.StreamProcessingOffload.Agent
{
    internal abstract class Frame
    {
        private int length;

        private int bufferOffset;

        protected Frame(FrameType frameType)
        {
            this.length = 0;
            this.bufferOffset = 0;
            this.Type = frameType;
            this.Metadata = new Metadata();
            this.Status = Status.Normal;
        }

        public FrameType Type { get; protected set; }

        public Metadata Metadata { get; protected set; }

        public Payload Payload { get; protected set; }

        public Status Status { get; protected set; }

        public int Length
        {
            get
            {
                if (this.length == 0)
                {
                    var bytes = this.Bytes;
                }

                return this.length;
            }
        }

        public byte[] Bytes
        {
            get
            {
                var bytes = new List<byte>();
                bytes.Add((byte)this.Type);
                bytes.AddRange(this.Metadata.Bytes);
                bytes.AddRange(this.Payload.Bytes);

                int length = bytes.Count;
                this.length = length;
                byte[] lengthBytes = BitConverter.GetBytes(length);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(lengthBytes);
                }

                bytes.InsertRange(0, lengthBytes);
                return bytes.ToArray();
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("Type: {0}", this.Type.ToString()));
            sb.AppendLine(string.Format("Length: {0}", this.Length));
            sb.AppendLine(string.Format("Flags - Fin: {0}", this.Metadata.Flags.Fin));
            sb.AppendLine(string.Format("Flags - Abort: {0}", this.Metadata.Flags.Abort));
            sb.AppendLine(string.Format("StreamID: {0}", this.Metadata.StreamId.Value));
            sb.AppendLine(string.Format("FrameID: {0}", this.Metadata.FrameId.Value));
            sb.Append(this.Payload.ToString());
            return sb.ToString();
        }

        internal void Parse(byte[] buffer)
        {
            this.Metadata.Parse(buffer, ref this.bufferOffset);
            this.Payload.Parse(buffer, ref this.bufferOffset);
        }
    }
}
