// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.

#pragma once

// from dotnet coreclr includes
#include "cor.h"
#include "corprof.h"
// end

#include <vector>
#include <cstdint>

/// <summary>
/// Allocating when a thread is suspended can lead to deadlocks.
/// This container holds a buffer that is used while walking stacks to temporarily hold results.
/// If we wanted to walk stacks of more than one thread concurrently, there would need to be more then once instance of this class.
/// However, since StackFrameCollector implementations currently all walk one stack at a time, they use one instance of this class each.
/// </summary>
class StackSnapshotResultBuffer
{
public:
    StackSnapshotResultBuffer() = delete;

    inline std::uint64_t GetUnixTimeUtc(void) const;
    inline std::uint64_t SetUnixTimeUtc(std::uint64_t value);

    inline std::uint64_t GetRepresentedDurationNanoseconds(void) const;
    inline std::uint64_t SetRepresentedDurationNanoseconds(std::uint64_t value);

    inline AppDomainID GetAppDomainId(void) const;
    inline AppDomainID SetAppDomainId(AppDomainID appDomainId);

    inline std::uint64_t GetLocalRootSpanId() const;
    inline std::uint64_t SetLocalRootSpanId(std::uint64_t value);

    inline std::uint64_t GetSpanId() const;
    inline std::uint64_t SetSpanId(std::uint64_t value);

    inline std::size_t GetFramesCount(void) const;
    inline void CopyInstructionPointers(std::vector<std::uintptr_t>& ips) const;

    inline void DetermineAppDomain(ThreadID threadId, ICorProfilerInfo4* pCorProfilerInfo);

protected:
    explicit StackSnapshotResultBuffer(std::uint16_t initialCapacity);
    virtual ~StackSnapshotResultBuffer();

protected:

    std::uint64_t _unixTimeUtc;
    std::uint64_t _representedDurationNanoseconds;
    AppDomainID _appDomainId;
    std::vector<uintptr_t> _instructionPointers;
    std::uint16_t _currentCapacity;
    std::uint16_t _nextResetCapacity;
    std::uint16_t _currentFramesCount;

    std::uint64_t _localRootSpanId;
    std::uint64_t _spanId;
};

// ----------- ----------- ----------- ----------- ----------- ----------- ----------- ----------- -----------

class StackSnapshotResultReusableBuffer : public StackSnapshotResultBuffer
{
    static constexpr std::uint16_t MaxSnapshotStackDepth_Limit = 2049;

public:
    StackSnapshotResultReusableBuffer() :
        StackSnapshotResultBuffer(MaxSnapshotStackDepth_Limit)
    {
    }
    ~StackSnapshotResultReusableBuffer() override = default;

    void Reset(void);

    inline bool AddFrame(std::uintptr_t ip);
    inline bool AddFakeFrame();
};

// ----------- ----------- ----------- ----------- ----------- ----------- ----------- ----------- -----------

inline std::uint64_t StackSnapshotResultBuffer::GetUnixTimeUtc(void) const
{
    return _unixTimeUtc;
}

inline std::uint64_t StackSnapshotResultBuffer::SetUnixTimeUtc(std::uint64_t value)
{
    std::uint64_t prevValue = _unixTimeUtc;
    _unixTimeUtc = value;
    return prevValue;
}

inline std::uint64_t StackSnapshotResultBuffer::GetRepresentedDurationNanoseconds(void) const
{
    return _representedDurationNanoseconds;
}

inline std::uint64_t StackSnapshotResultBuffer::SetRepresentedDurationNanoseconds(std::uint64_t value)
{
    std::uint64_t prevValue = _representedDurationNanoseconds;
    _representedDurationNanoseconds = value;
    return prevValue;
}

inline AppDomainID StackSnapshotResultBuffer::GetAppDomainId(void) const
{
    return _appDomainId;
}

inline AppDomainID StackSnapshotResultBuffer::SetAppDomainId(AppDomainID value)
{
    AppDomainID prevValue = _appDomainId;
    _appDomainId = value;
    return prevValue;
}

inline std::uint64_t StackSnapshotResultBuffer::GetLocalRootSpanId() const
{
    return _localRootSpanId;
}

inline std::uint64_t StackSnapshotResultBuffer::SetLocalRootSpanId(std::uint64_t value)
{
    std::uint64_t prevValue = _localRootSpanId;
    _localRootSpanId = value;
    return prevValue;
}

inline std::uint64_t StackSnapshotResultBuffer::GetSpanId() const
{
    return _spanId;
}

inline std::uint64_t StackSnapshotResultBuffer::SetSpanId(std::uint64_t value)
{
    std::uint64_t prevValue = _spanId;
    _spanId = value;
    return prevValue;
}

inline std::size_t StackSnapshotResultBuffer::GetFramesCount(void) const
{
    return _instructionPointers.size();
}

inline void StackSnapshotResultBuffer::CopyInstructionPointers(std::vector<std::uintptr_t>& ips) const
{
    ips.reserve(_instructionPointers.size());

    // copy the instruction pointer to the out-parameter
    ips = _instructionPointers;
}

inline void StackSnapshotResultBuffer::DetermineAppDomain(ThreadID threadId, ICorProfilerInfo4* pCorProfilerInfo)
{
    // Determine the AppDomain currently running the sampled thread:
    //
    // (Note: On Windows, the target thread is still suspended and the AddDomain ID will be correct.
    // However, on Linux the signal handler that performed the stack walk has finished and the target
    // thread is making progress again.
    // So, it is possible that since we walked the stack, the thread's AppDomain changed and the AppDomain ID we
    // read here does not correspond to the stack sample. In practice we expect this to occur very rarely,
    // so we accept this for now.
    // If, however, this is observed frequently enough to present a problem, we will need to move the AppDomain
    // ID read logic into _pStackFramesCollector->CollectStackSample(). Collectors that suspend the target thread
    // will be able to read the ID at any time, but non-suspending collectors (e.g. Linux) will need to do it from
    // within the signal handler. An example for this is the
    // StackFramesCollectorBase::TryApplyTraceContextDataFromCurrentCollectionThreadToSnapshot() API which exists
    // to address the same synchronization issue with TraceContextTracking-related data.
    // There is an additional complexity with the AppDomain case, because it is likely not safe to call
    // _pCorProfilerInfo->GetThreadAppDomain() from the collector's signal handler directly (needs to be investigated).
    // To address this, we will need to do it via a SynchronousOffThreadWorkerBase-based mechanism, similar to how
    // the SymbolsResolver uses a Worker and synchronously waits for results to avoid calling
    // symbol resolution APIs on a CLR thread.)
    AppDomainID appDomainId;
    HRESULT hr = pCorProfilerInfo->GetThreadAppDomain(threadId, &appDomainId);
    if (SUCCEEDED(hr))
    {
        SetAppDomainId(appDomainId);
    }
}

// ----------- ----------- ----------- ----------- ----------- ----------- ----------- ----------- -----------

inline bool StackSnapshotResultReusableBuffer::AddFrame(std::uintptr_t ip)
{
    const auto nextIdx = _instructionPointers.size();

    if (nextIdx >= MaxSnapshotStackDepth_Limit)
    {
        return false;
    }

    const auto lastIdx = MaxSnapshotStackDepth_Limit - 1;
    if (nextIdx == lastIdx)
    {
        _instructionPointers.push_back(0);
        return false;
    }

    _instructionPointers.push_back(ip);
    return true;
}

inline bool StackSnapshotResultReusableBuffer::AddFakeFrame()
{
    return AddFrame(0);
}
