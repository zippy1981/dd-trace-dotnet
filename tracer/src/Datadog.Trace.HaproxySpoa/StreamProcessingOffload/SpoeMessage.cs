// <copyright file="SpoeMessage.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="SpoeMessage.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------
using System.Collections.Generic;

namespace HAProxy.StreamProcessingOffload.Agent
{
    internal class SpoeMessage
    {
        public SpoeMessage(string name)
        {
            this.Name = name;
            this.Args = new Dictionary<string, TypedData>();
        }

        public string Name { get; private set; }

        public IDictionary<string, TypedData> Args { get; private set; }
    }
}
