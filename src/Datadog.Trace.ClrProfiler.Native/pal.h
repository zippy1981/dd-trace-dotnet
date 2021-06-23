#ifndef DD_CLR_PROFILER_PAL_H_
#define DD_CLR_PROFILER_PAL_H_

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

#include "environment_variables.h"
#include "util.h"
#include "../Datadog.Shared.Native/pal.h"

namespace trace
{

inline WSTRING DatadogLogFilePath(const std::string& file_name_suffix)
{
    WSTRING directory = GetEnvironmentValue(environment::log_directory);

    if (directory.length() > 0)
    {
        return directory +
#ifdef _WIN32
               WStr('\\') +
#else
               WStr('/') +
#endif
               ToWSTRING("dotnet-tracer-native" + file_name_suffix + ".log");
    }

    WSTRING path = GetEnvironmentValue(environment::log_path);

    if (path.length() > 0)
    {
        return path;
    }

#ifdef _WIN32
    char* p_program_data;
    size_t length;
    const errno_t result = _dupenv_s(&p_program_data, &length, "PROGRAMDATA");
    std::string program_data;

    if (SUCCEEDED(result) && p_program_data != nullptr && length > 0)
    {
        program_data = std::string(p_program_data);
    }
    else
    {
        program_data = R"(C:\ProgramData)";
    }

    return ToWSTRING(program_data + R"(\Datadog .NET Tracer\logs\dotnet-tracer-native)" + file_name_suffix + ".log");
#else
    return ToWSTRING("/var/log/datadog/dotnet/dotnet-tracer-native" + file_name_suffix + ".log");
#endif
}

} // namespace trace

#endif // DD_CLR_PROFILER_PAL_H_
