#ifndef DD_SHARED_PAL_H_
#define DD_SHARED_PAL_H_

#ifdef _WIN32

#include <windows.h>
#include <filesystem>
#include <process.h>
#include <string>

#else

#include <fstream>
#include <unistd.h>

#endif

#if MACOS
#include <libproc.h>
#endif

#include "util.h"
#include "../Datadog.Shared.Native/pal.h"

namespace trace
{

inline WSTRING GetCurrentProcessName()
{
#ifdef _WIN32
    const DWORD length = 260;
    WCHAR buffer[length]{};

    const DWORD len = GetModuleFileName(nullptr, buffer, length);
    const WSTRING current_process_path(buffer);
    return std::filesystem::path(current_process_path).filename();
#elif MACOS
    const int length = 260;
    char* buffer = new char[length];
    proc_name(getpid(), buffer, length);
    return ToWSTRING(std::string(buffer));
#else
    std::fstream comm("/proc/self/comm");
    std::string name;
    std::getline(comm, name);
    return ToWSTRING(name);
#endif
}

inline int GetPID()
{
#ifdef _WIN32
    return _getpid();
#else
    return getpid();
#endif
}

} // namespace trace

#endif // DD_SHARED_PAL_H_
