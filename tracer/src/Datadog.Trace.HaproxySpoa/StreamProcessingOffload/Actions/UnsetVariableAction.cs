// <copyright file="UnsetVariableAction.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="UnsetVariableAction.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------
using System.Collections.Generic;
using System.Text;

namespace HAProxy.StreamProcessingOffload.Agent.Actions
{
    internal class UnsetVariableAction : SpoeAction
    {
        private const byte NumberOfArgs = 2;
        private VariableScope variableScope;
        private string variableName;

        public UnsetVariableAction(VariableScope variableScope, string variableName)
            : base(ActionType.UnsetVar)
        {
            this.variableScope = variableScope;
            this.variableName = variableName;
        }

        public override byte[] Bytes
        {
            get
            {
                var bytes = new List<byte>();
                bytes.Add((byte)this.Type);
                bytes.Add(NumberOfArgs);

                // argument 1: variable scope
                bytes.Add((byte)this.variableScope);

                // argument 2: variable name
                VariableInt lengthOfName = VariableInt.EncodeVariableInt(this.variableName.Length);
                bytes.AddRange(lengthOfName.Bytes);
                bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(this.variableName));

                return bytes.ToArray();
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("(action) {0}", this.Type.ToString()));
            sb.AppendLine(this.variableName);
            return sb.ToString();
        }
    }
}
