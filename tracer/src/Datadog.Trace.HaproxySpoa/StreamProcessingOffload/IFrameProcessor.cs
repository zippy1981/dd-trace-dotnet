// <copyright file="IFrameProcessor.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="IFrameProcessor.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using HAProxy.StreamProcessingOffload.Agent.Frames;

namespace HAProxy.StreamProcessingOffload.Agent
{
    internal interface IFrameProcessor
    {
        uint MaxFrameSize { get; set; }

        bool EnableLogging { get; set; }

        Action<string> LogFunc { get; set; }

        void HandleStream(Stream stream, Func<NotifyFrame, IList<SpoeAction>> notifyHandler);

        void CancelStream(Stream stream);
    }
}
