// <copyright file="ListOfActionsPayload.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="ListOfActionsPayload.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------
using System.Collections.Generic;
using System.Text;

namespace HAProxy.StreamProcessingOffload.Agent.Payloads
{
    internal class ListOfActionsPayload : Payload
    {
        public ListOfActionsPayload()
            : base(PayloadType.ListOfActions)
        {
            this.Actions = new List<SpoeAction>();
        }

        public IList<SpoeAction> Actions { get; set; }

        public override byte[] Bytes
        {
            get
            {
                var bytes = new List<byte>();

                foreach (var action in this.Actions)
                {
                    bytes.AddRange(action.Bytes);
                }

                return bytes.ToArray();
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var action in this.Actions)
            {
                sb.AppendLine(string.Format(action.ToString()));
            }

            return sb.ToString();
        }

        internal override void Parse(byte[] buffer, ref int offset)
        {
            throw new System.NotImplementedException();
        }
    }
}
