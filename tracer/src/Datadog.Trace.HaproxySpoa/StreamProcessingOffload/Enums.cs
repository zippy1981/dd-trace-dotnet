// <copyright file="Enums.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

//-----------------------------------------------------------------------
// <copyright file="Enums.cs" company="HAProxy Technologies">
//     The contents of this file are Copyright (c) 2019. HAProxy Technologies.
//     All rights reserved. This file is subject to the terms and conditions
//     defined in file 'LICENSE', which is part of this source code package.
// </copyright>
//-----------------------------------------------------------------------
namespace HAProxy.StreamProcessingOffload.Agent
{
    /// <summary>
    /// Type of action for HAProxy to apply.
    /// </summary>
    internal enum ActionType
    {
        SetVar = 1,
        UnsetVar = 2
    }

    /// <summary>
    /// A type of data.
    /// </summary>
    internal enum DataType
    {
        Null = 0,
        Boolean = 1,
        Int32 = 2,
        Uint32 = 3,
        Int64 = 4,
        Uint64 = 5,
        Ipv4 = 6,
        Ipv6 = 7,
        String = 8,
        Binary = 9
    }

    /// <summary>
    /// The type of frame.
    /// </summary>
    internal enum FrameType
    {
        Unset = 0,
        HaproxyHello = 1,
        HaproxyDisconnect = 2,
        Notify = 3,
        AgentHello = 101,
        AgentDisconnect = 102,
        Ack = 103
    }

    /// <summary>
    /// The type of payload.
    /// </summary>
    internal enum PayloadType
    {
        ListOfMessages,
        ListOfActions,
        KeyValueList,
        RawData
    }

    /// <summary>
    /// When either HAProxy or the agent disconnect, it may contain a status.
    /// </summary>
    internal enum Status
    {
        Normal = 0,
        IOError = 1,
        Timeout = 2,
        FrameTooBig = 3,
        InvalidFrame = 4,
        VersionMissing = 5,
        MaxFrameSizeMissing = 6,
        CapabilitiesMissing = 7,
        UnsupportedVersion = 8,
        MaxFrameSizeTooBigOrSmall = 9,
        FragmentationNotSupported = 10,
        InvalidInterlacedFrames = 11,
        FrameIdMissing = 12,
        ResourceAllocationError = 13,
        UnknownError = 99,
        AgentError = 100,
    }

    /// <summary>
    /// When applying a set-var or unset-var action,
    /// this defines the scope of the variable.
    /// </summary>
    internal enum VariableScope
    {
        Process = 0,
        Session = 1,
        Transaction = 2,
        Request = 3,
        Response = 4
    }
}
