//---------------------------------------------------------------------------------------
// Exports that managed code from Datadog.Trace.ClrProfiler.Managed.dll will
// P/Invoke into
//
// NOTE: Must keep these signatures in sync with the DllImports in
// NativeMethods.cs!
//---------------------------------------------------------------------------------------

#include "cor_profiler.h"

#ifdef LINUX
#include <PowerWAF.h>
#endif

EXTERN_C BOOL STDAPICALLTYPE IsProfilerAttached() {
  return trace::profiler->IsAttached();
}

EXTERN_C VOID STDAPICALLTYPE GetAssemblyAndSymbolsBytes(BYTE** pAssemblyArray, int* assemblySize, BYTE** pSymbolsArray, int* symbolsSize) {
  return trace::profiler->GetAssemblyAndSymbolsBytes(pAssemblyArray, assemblySize, pSymbolsArray, symbolsSize);
}

#ifdef LINUX
void not_used(void)
{
  pw_getVersion();
}
#endif
