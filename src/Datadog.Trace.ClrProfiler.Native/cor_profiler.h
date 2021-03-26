#ifndef DD_CLR_PROFILER_COR_PROFILER_H_
#define DD_CLR_PROFILER_COR_PROFILER_H_

#include <atomic>
#include <mutex>
#include <vector>
#include <string>
#include <unordered_map>
#include "cor.h"
#include "corprof.h"

#include "cor_profiler_base.h"
#include "environment_variables.h"
#include "integration.h"
#include "module_metadata.h"
#include "pal.h"
#include "il_rewriter.h"
#include "rejit_handler.h"

namespace trace {

typedef struct _MethodReplacementItem {
  WCHAR* callerAssembly;
  WCHAR* callerType;
  WCHAR* callerMethod;
  WCHAR* targetAssembly;
  WCHAR* targetType;
  WCHAR* targetMethod;
  WCHAR** signatureTypes;
  USHORT signatureTypesLength;
  USHORT targetMinimumMajor;
  USHORT targetMinimumMinor;
  USHORT targetMinimumPatch;
  USHORT targetMaximumMajor;
  USHORT targetMaximumMinor;
  USHORT targetMaximumPatch;
  WCHAR* wrapperAssembly;
  WCHAR* wrapperType;
  WCHAR* wrapperMethod;
  WCHAR* wrapperSignature;
  WCHAR* wrapperAction;
} MethodReplacementItem;

class CorProfiler : public CorProfilerBase {
 private:
  std::atomic_bool is_attached_ = {false};
  RuntimeInformation runtime_information_;
  std::vector<IntegrationMethod> integration_methods_;

  // Startup helper variables
  bool first_jit_compilation_completed = false;

  bool instrument_domain_neutral_assemblies = false;
  bool corlib_module_loaded = false;
  AppDomainID corlib_app_domain_id = 0;
  bool managed_profiler_loaded_domain_neutral = false;
  std::unordered_set<AppDomainID> managed_profiler_loaded_app_domains;
  std::unordered_set<AppDomainID> first_jit_compilation_app_domains;
  bool in_azure_app_services = false;
  bool is_desktop_iis = false;

  //
  // CallTarget Members
  //
  RejitHandler* rejit_handler = nullptr;

  // Cor assembly properties
  AssemblyProperty corAssemblyProperty{};

  //
  // OpCodes helper
  //
  std::vector<std::string> opcodes_names;

  //
  // Module helper variables
  //
  std::mutex module_id_to_info_map_lock_;
  std::unordered_map<ModuleID, ModuleMetadata*> module_id_to_info_map_;

  //
  // Helper methods
  //
  bool GetWrapperMethodRef(ModuleMetadata* module_metadata,
                           ModuleID module_id,
                           const MethodReplacement& method_replacement,
                           mdMemberRef& wrapper_method_ref,
                           mdTypeRef& wrapper_type_ref);
  HRESULT ProcessReplacementCalls(ModuleMetadata* module_metadata,
                                         const FunctionID function_id,
                                         const ModuleID module_id,
                                         const mdToken function_token,
                                         const FunctionInfo& caller,
                                         const std::vector<MethodReplacement> method_replacements);
  HRESULT ProcessInsertionCalls(ModuleMetadata* module_metadata,
                                         const FunctionID function_id,
                                         const ModuleID module_id,
                                         const mdToken function_token,
                                         const FunctionInfo& caller,
                                         const std::vector<MethodReplacement> method_replacements);
  bool ProfilerAssemblyIsLoadedIntoAppDomain(AppDomainID app_domain_id);
  std::string GetILCodes(const std::string& title, ILRewriter* rewriter,
                         const FunctionInfo& caller,
                         ModuleMetadata* module_metadata);
  //
  // Startup methods
  //
  HRESULT RunILStartupHook(const ComPtr<IMetaDataEmit2>&,
                             const ModuleID module_id,
                             const mdToken function_token);
  HRESULT GenerateVoidILStartupMethod(const ModuleID module_id,
                           mdMethodDef* ret_method_token);
  HRESULT AddIISPreStartInitFlags(const ModuleID module_id,
                           const mdToken function_token);

  //
  // CallTarget Methods
  //
  size_t CallTarget_RequestRejitForModule(
    ModuleID module_id, ModuleMetadata* module_metadata,
    const std::vector<IntegrationMethod>& filtered_integrations);
  HRESULT CallTarget_RewriterCallback(RejitHandlerModule* moduleHandler, RejitHandlerModuleMethod* methodHandler);

 public:
  CorProfiler() = default;

  bool IsAttached() const;

  void GetAssemblyAndSymbolsBytes(BYTE** pAssemblyArray, int* assemblySize,
                                 BYTE** pSymbolsArray, int* symbolsSize) const;

  void SetIntegrations(MethodReplacementItem* items, int size) {
    Info("SetIntegrations received from managed side: ", size, " integrations.");
    if (items != nullptr) {
      for (int i = 0; i < size; i++) {
        Info("  MethodReplacementItem:");
        const MethodReplacementItem current = items[i];
        if (current.callerAssembly != nullptr) {
          Info("    CallerAssembly: ", WSTRING(current.callerAssembly));
        }
        if (current.callerType != nullptr) {
          Info("    CallerType: ", WSTRING(current.callerType));
        }
        if (current.callerMethod != nullptr) {
          Info("    CallerMethod: ", WSTRING(current.callerMethod));
        }
        if (current.targetAssembly != nullptr) {
          Info("    TargetAssembly: ", WSTRING(current.targetAssembly));
        }
        if (current.targetType != nullptr) {
          Info("    TargetType: ", WSTRING(current.targetType));
        }
        if (current.targetMethod != nullptr) {
          Info("    TargetMethod: ", WSTRING(current.targetMethod));
        }
        Info("    SignatureTypes: ", current.signatureTypesLength);
        for (int sIdx = 0; sIdx < current.signatureTypesLength; sIdx++) {
          const auto currentSignature = current.signatureTypes[sIdx];
          if (currentSignature != nullptr) {
            const WSTRING signatureType = WSTRING(currentSignature);
            Info("       - ", signatureType);
          }
        }
        Info("    TargetMinimumMajor: ", current.targetMinimumMajor);
        Info("    TargetMinimumMinor: ", current.targetMinimumMinor);
        Info("    TargetMinimumPatch: ", current.targetMinimumPatch);
        Info("    TargetMaximumMajor: ", current.targetMaximumMajor);
        Info("    TargetMaximumMinor: ", current.targetMaximumMinor);
        Info("    TargetMaximumPatch: ", current.targetMaximumPatch);
        if (current.wrapperAssembly != nullptr) {
          Info("    WrapperAssembly: ", WSTRING(current.wrapperAssembly));
        }
        if (current.wrapperType != nullptr) {
          Info("    WrapperType: ", WSTRING(current.wrapperType));
        }
        if (current.wrapperMethod != nullptr) {
          Info("    WrapperMethod: ", WSTRING(current.wrapperMethod));
        }
        if (current.wrapperSignature != nullptr) {
          Info("    WrapperSignature: ", WSTRING(current.wrapperSignature));
        }
        if (current.wrapperAction != nullptr) {
          Info("    WrapperAction: ", WSTRING(current.wrapperAction));
        }
      }
    }
  }

  //
  // ICorProfilerCallback methods
  //
  HRESULT STDMETHODCALLTYPE
  Initialize(IUnknown* cor_profiler_info_unknown) override;

  HRESULT STDMETHODCALLTYPE AssemblyLoadFinished(AssemblyID assembly_id,
                                                 HRESULT hr_status) override;

  HRESULT STDMETHODCALLTYPE ModuleLoadFinished(ModuleID module_id,
                                               HRESULT hr_status) override;

  HRESULT STDMETHODCALLTYPE ModuleUnloadStarted(ModuleID module_id) override;

  HRESULT STDMETHODCALLTYPE
  JITCompilationStarted(FunctionID function_id, BOOL is_safe_to_block) override;

  HRESULT STDMETHODCALLTYPE Shutdown() override;

  HRESULT STDMETHODCALLTYPE ProfilerDetachSucceeded() override;

  HRESULT STDMETHODCALLTYPE JITInlining(FunctionID callerId,
                                        FunctionID calleeId,
                                        BOOL* pfShouldInline) override;
  //
  // ReJIT Methods
  //

  HRESULT STDMETHODCALLTYPE ReJITCompilationStarted(
      FunctionID functionId, ReJITID rejitId, BOOL fIsSafeToBlock) override;

  HRESULT STDMETHODCALLTYPE
  GetReJITParameters(ModuleID moduleId, mdMethodDef methodId,
                     ICorProfilerFunctionControl* pFunctionControl) override;

  HRESULT STDMETHODCALLTYPE ReJITCompilationFinished(
      FunctionID functionId, ReJITID rejitId, HRESULT hrStatus,
      BOOL fIsSafeToBlock) override;

  HRESULT STDMETHODCALLTYPE ReJITError(ModuleID moduleId, mdMethodDef methodId,
                                       FunctionID functionId,
                                       HRESULT hrStatus) override;

  //
  // ICorProfilerCallback6 methods
  //
  HRESULT STDMETHODCALLTYPE GetAssemblyReferences(
      const WCHAR* wszAssemblyPath,
      ICorProfilerAssemblyReferenceProvider* pAsmRefProvider) override;
};

// Note: Generally you should not have a single, global callback implementation,
// as that prevents your profiler from analyzing multiply loaded in-process
// side-by-side CLRs. However, this profiler implements the "profile-first"
// alternative of dealing with multiple in-process side-by-side CLR instances.
// First CLR to try to load us into this process wins; so there can only be one
// callback implementation created. (See ProfilerCallback::CreateObject.)
extern CorProfiler* profiler;  // global reference to callback object

}  // namespace trace

#endif  // DD_CLR_PROFILER_COR_PROFILER_H_
