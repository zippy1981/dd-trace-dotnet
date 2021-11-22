// <copyright file="SetVariableAction.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="SetVariableAction.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------
using System.Collections.Generic;
using System.Text;

namespace HAProxy.StreamProcessingOffload.Agent.Actions
{
    internal class SetVariableAction : SpoeAction
    {
        private const byte NumberOfArgs = 3;
        private VariableScope variableScope;
        private string variableName;
        private TypedData value;

        public SetVariableAction(VariableScope variableScope, string variableName, TypedData value)
            : base(ActionType.SetVar)
        {
            this.variableScope = variableScope;
            this.variableName = variableName;
            this.value = value;
        }

        /// <summary>
        /// Gets the bytes for this action.
        /// </summary>
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

                // argument 3: variable value
                bytes.AddRange(this.value.Bytes);

                return bytes.ToArray();
            }
        }

        /// <summary>
        /// Gets a string representing the action.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("(action) {0}", this.Type.ToString()));
            sb.AppendLine(string.Format("{0} = {1}", this.variableName, this.value.ToString()));
            return sb.ToString();
        }
    }
}
