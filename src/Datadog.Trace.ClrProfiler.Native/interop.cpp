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
EXTERN_C bool STDAPICALLTYPE pw_init(const char* ruleName, const char* wafRule, const PWConfig* config, char** errors)
{
  return pw_init(ruleName, wafRule, config, errors);
}

EXTERN_C PWVersion pw_getVersion(void)
{
  return pw_getVersion();
}

EXTERN_C PWHandle pw_initH(const char* wafRule, const PWConfig* config, char** errors)
{
	return pw_initH(wafRule, config, errors);
}

EXTERN_C void pw_clearRuleH(PWHandle wafHandle)
{
	return pw_clearRuleH(wafHandle);
}

EXTERN_C PWRet pw_runH(const PWHandle wafHandle, const PWArgs parameters, uint64_t timeLeftInUs)
{
	return pw_runH(wafHandle, parameters, timeLeftInUs);
}

EXTERN_C void pw_freeReturn(PWRet output)
{
	return pw_freeReturn(output);
}

EXTERN_C PWAddContext pw_initAdditiveH(const PWHandle powerwafHandle)
{
	return pw_initAdditiveH(powerwafHandle);
}

EXTERN_C PWRet pw_runAdditive(PWAddContext context, PWArgs newArgs, uint64_t timeLeftInUs)
{
	return pw_runAdditive(context, newArgs, timeLeftInUs);
}

EXTERN_C void pw_clearAdditive(PWAddContext context)
{
	return pw_clearAdditive(context);
}

EXTERN_C PWArgs pw_getInvalid(void)
{
	return pw_getInvalid();
}
EXTERN_C PWArgs pw_createStringWithLength(const char* string, uint64_t length)
{
	return pw_createStringWithLength(string, length);
}
EXTERN_C PWArgs pw_createString(const char* string)
{
	return pw_createString(string);
}
EXTERN_C PWArgs pw_createInt(int64_t value)
{
	return pw_createInt(value);
}
EXTERN_C PWArgs pw_createUint(uint64_t value)
{
	return pw_createUint(value);
}
EXTERN_C PWArgs pw_createArray(void)
{
	return pw_createArray();
}
EXTERN_C PWArgs pw_createMap(void)
{
	return pw_createMap();
}
EXTERN_C bool pw_addArray(PWArgs* array, PWArgs entry)
{
	return pw_addArray(array, entry);
}
EXTERN_C bool pw_addMap(PWArgs* map, const char* entryName, uint64_t entryNameLength, PWArgs entry)
{
	return pw_addMap(map, entryName, entryNameLength, entry);
}
EXTERN_C void pw_freeArg(PWArgs* input)
{
	return pw_freeArg(input);
}

#endif
