// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full
// license information.

#include "class_factory.h"

#include "environment_variables.h"
#include "logging.h"

#include "pal.h"
#include "cor_profiler.h"
#include "version.h"

// profiler entry point
HRESULT STDMETHODCALLTYPE TracerClassFactory::OnCreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppvObject,
                                                               HINSTANCE dllInstance)
{
    // check if debug mode is enabled
    trace::Logger::Instance()->Initialize(trace::environment::IsDebugEnabled(),
                                          [](const std::string& suffix) { return trace::DatadogLogFilePath(suffix); });

    trace::Info("Datadog CLR Profiler ", PROFILER_VERSION, " on",

#ifdef _WIN32
                " Windows"
#elif MACOS
                " macOS"
#else
                " Linux"
#endif

#ifdef AMD64
                ,
                " (amd64)"
#elif X86
                ,
                " (x86)"
#elif ARM64
                ,
                " (arm64)"
#elif ARM
                , " (arm)"
#endif
    );
    trace::Debug("ClassFactory::CreateInstance");

    auto profiler = new trace::CorProfiler(dllInstance);
    return profiler->QueryInterface(riid, ppvObject);
}
