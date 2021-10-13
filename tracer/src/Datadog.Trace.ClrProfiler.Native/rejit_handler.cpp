#include "rejit_handler.h"

#include "dd_profiler_constants.h"
#include "logger.h"
#include "stats.h"

namespace trace
{

//
// RejitItem
//

RejitItem::RejitItem() : m_type(-1), m_length(0), m_modulesId(nullptr), m_methodDefs(nullptr), m_promise(nullptr)
{
}

RejitItem::RejitItem(int length,
    std::unique_ptr<ModuleID[]>&& modulesId,
    std::unique_ptr<mdMethodDef[]>&& methodDefs,
    std::unique_ptr<std::vector<IntegrationMethod>>&& integrationMethods,
    std::promise<int>* promise)
{
    m_length = length;
    m_modulesId = std::move(modulesId);
    if (methodDefs != nullptr)
    {
        m_type = 0;
        m_methodDefs = std::move(methodDefs);
    }
    else
    {
        m_type = 1;
        m_integrationMethods = std::move(integrationMethods);
    }
    m_promise = promise;
}

std::unique_ptr<RejitItem> RejitItem::CreateEndRejitThread()
{
    return std::make_unique<RejitItem>();
}

//
// RejitHandlerModuleMethod
//

RejitHandlerModuleMethod::RejitHandlerModuleMethod(mdMethodDef methodDef, RejitHandlerModule* module)
{
    m_methodDef = methodDef;
    m_pFunctionControl = nullptr;
    m_module = module;
    m_functionInfo = nullptr;
    m_methodReplacement = nullptr;
}

mdMethodDef RejitHandlerModuleMethod::GetMethodDef()
{
    return m_methodDef;
}

RejitHandlerModule* RejitHandlerModuleMethod::GetModule()
{
    return m_module;
}

ICorProfilerFunctionControl* RejitHandlerModuleMethod::GetFunctionControl()
{
    return m_pFunctionControl;
}

void RejitHandlerModuleMethod::SetFunctionControl(ICorProfilerFunctionControl* pFunctionControl)
{
    m_pFunctionControl = pFunctionControl;
}

FunctionInfo* RejitHandlerModuleMethod::GetFunctionInfo()
{
    return m_functionInfo.get();
}

void RejitHandlerModuleMethod::SetFunctionInfo(const FunctionInfo& functionInfo)
{
    m_functionInfo = std::make_unique<FunctionInfo>(functionInfo);
}

MethodReplacement* RejitHandlerModuleMethod::GetMethodReplacement()
{
    return m_methodReplacement.get();
}

void RejitHandlerModuleMethod::SetMethodReplacement(const MethodReplacement& methodReplacement)
{
    m_methodReplacement = std::make_unique<MethodReplacement>(methodReplacement);
}

void RejitHandlerModuleMethod::RequestRejitForInlinersInModule(ModuleID moduleId)
{
    Logger::Debug("RejitHandlerModuleMethod::RequestRejitForInlinersInModule: ", moduleId);
    std::lock_guard<std::mutex> guard(m_ngenModulesLock);

    // We check first if we already processed this module to skip it.
    auto find_res = m_ngenModules.find(moduleId);
    if (find_res != m_ngenModules.end())
    {
        return;
    }

    // Enumerate all inliners and request rejit
    ModuleID currentModuleId = m_module->GetModuleId();
    mdMethodDef currentMethodDef = m_methodDef;
    RejitHandler* handler = m_module->GetHandler();
    ICorProfilerInfo6* pInfo = handler->GetCorProfilerInfo6();

    if (pInfo != nullptr)
    {
        // Now we enumerate all methods that inline the current methodDef
        BOOL incompleteData = false;
        ICorProfilerMethodEnum* methodEnum;

        HRESULT hr = pInfo->EnumNgenModuleMethodsInliningThisMethod(moduleId, currentModuleId, currentMethodDef,
                                                                    &incompleteData, &methodEnum);
        std::ostringstream hexValue;
        hexValue << std::hex << hr;
        if (SUCCEEDED(hr))
        {
            COR_PRF_METHOD method;
            unsigned int total = 0;
            std::vector<ModuleID> modules;
            std::vector<mdMethodDef> methods;
            while (methodEnum->Next(1, &method, NULL) == S_OK)
            {
                Logger::Debug("NGEN:: Asking rewrite for inliner [ModuleId=", method.moduleId,
                              ",MethodDef=", method.methodId, "]");
                modules.push_back(method.moduleId);
                methods.push_back(method.methodId);
                total++;
            }
            methodEnum->Release();
            methodEnum = nullptr;
            if (total > 0)
            {
                handler->EnqueueForRejit(modules, methods);
                Logger::Info("NGEN:: Processed with ", total, " inliners [ModuleId=", currentModuleId,
                             ",MethodDef=", currentMethodDef, "]");
            }

            if (!incompleteData)
            {
                m_ngenModules[moduleId] = true;
            }
            else
            {
                Logger::Warn("NGen inliner data for module '", moduleId, "' is incomplete.");
            }
        }
        else if (hr == E_INVALIDARG)
        {
            Logger::Info("NGEN:: Error Invalid arguments in [ModuleId=", currentModuleId,
                         ",MethodDef=", currentMethodDef, ", HR=", hexValue.str(), "]");
        }
        else if (hr == CORPROF_E_DATAINCOMPLETE)
        {
            Logger::Info("NGEN:: Error Incomplete data in [ModuleId=", currentModuleId, ",MethodDef=", currentMethodDef,
                         ", HR=", hexValue.str(), "]");
        }
        else if (hr == CORPROF_E_UNSUPPORTED_CALL_SEQUENCE)
        {
            Logger::Info("NGEN:: Unsupported call sequence error in [ModuleId=", currentModuleId, ",MethodDef=", currentMethodDef,
                         ", HR=", hexValue.str(), "]");
        }
        else
        {
            Logger::Info("NGEN:: Error in [ModuleId=", currentModuleId, ",MethodDef=", currentMethodDef,
                         ", HR=", hexValue.str(), "]");
        }
    }
}

//
// RejitHandlerModule
//

RejitHandlerModule::RejitHandlerModule(ModuleID moduleId, RejitHandler* handler)
{
    m_moduleId = moduleId;
    m_metadata = nullptr;
    m_handler = handler;
}

ModuleID RejitHandlerModule::GetModuleId()
{
    return m_moduleId;
}

RejitHandler* RejitHandlerModule::GetHandler()
{
    return m_handler;
}

ModuleMetadata* RejitHandlerModule::GetModuleMetadata()
{
    return m_metadata.get();
}

void RejitHandlerModule::SetModuleMetadata(ModuleMetadata* metadata)
{
    m_metadata = std::unique_ptr<ModuleMetadata>(metadata);
}

RejitHandlerModuleMethod* RejitHandlerModule::GetOrAddMethod(mdMethodDef methodDef)
{
    std::lock_guard<std::mutex> guard(m_methods_lock);

    auto find_res = m_methods.find(methodDef);
    if (find_res != m_methods.end())
    {
        return find_res->second.get();
    }

    RejitHandlerModuleMethod* methodHandler = new RejitHandlerModuleMethod(methodDef, this);
    m_methods[methodDef] = std::unique_ptr<RejitHandlerModuleMethod>(methodHandler);
    return methodHandler;
}

bool RejitHandlerModule::ContainsMethod(mdMethodDef methodDef)
{
    std::lock_guard<std::mutex> guard(m_methods_lock);
    return m_methods.find(methodDef) != m_methods.end();
}

void RejitHandlerModule::RequestRejitForInlinersInModule(ModuleID moduleId)
{
    std::lock_guard<std::mutex> guard(m_methods_lock);
    for (const auto& method : m_methods)
    {
        method.second.get()->RequestRejitForInlinersInModule(moduleId);
    }
}

//
// RejitHandler
//

void RejitHandler::EnqueueThreadLoop(RejitHandler* handler)
{
    auto queue = handler->m_rejit_queue.get();
    auto profilerInfo = handler->m_profilerInfo;
    auto profilerInfo10 = handler->m_profilerInfo10;

    Logger::Info("Initializing ReJIT request thread.");
    HRESULT hr = profilerInfo->InitializeCurrentThread();
    if (FAILED(hr))
    {
        Logger::Warn("Call to InitializeCurrentThread fail.");
    }

    while (true)
    {
        const auto item = queue->pop();

        if (item->m_type == -1)
        {
            // *************************************
            // Exit ReJIT thread
            // *************************************

            break;
        }
        else if (item->m_type == 0)
        {
            // *************************************
            // Request ReJIT
            // *************************************

            if (profilerInfo10 != nullptr)
            {
                // RequestReJITWithInliners is currently always failing with `Fatal error. Internal CLR error.
                // (0x80131506)` more research is required, meanwhile we fallback to the normal RequestReJIT and manual
                // track of inliners.

                /*hr = profilerInfo10->RequestReJITWithInliners(COR_PRF_REJIT_BLOCK_INLINING, (ULONG) item->m_length,
                                                              item->m_modulesId.get(), item->m_methodDefs.get());
                if (FAILED(hr))
                {
                    Warn("Error requesting ReJITWithInliners for ", item->m_length,
                         " methods, falling back to a normal RequestReJIT");
                    hr = profilerInfo10->RequestReJIT((ULONG) item->m_length, item->m_modulesId.get(),
                                                      item->m_methodDefs.get());
                }*/

                hr = profilerInfo10->RequestReJIT((ULONG) item->m_length, item->m_modulesId.get(),
                                                  item->m_methodDefs.get());
            }
            else
            {
                hr = profilerInfo->RequestReJIT((ULONG) item->m_length, item->m_modulesId.get(),
                                                item->m_methodDefs.get());
            }
            if (SUCCEEDED(hr))
            {
                Logger::Info("Request ReJIT done for ", item->m_length, " methods");
            }
            else
            {
                Logger::Warn("Error requesting ReJIT for ", item->m_length, " methods");
            }

            // Request for NGen Inliners
            handler->RequestRejitForNGenInliners();
        }
        else if (item->m_type == 1)
        {
            // *************************************
            // Checks if there are integrations for the modules and enqueue a ReJIT request
            // *************************************

            if (item->m_length > 0 && item->m_integrationMethods->size() > 0)
            {
                auto pIntegrations = item->m_integrationMethods.get();
                auto pModuleId = item->m_modulesId.get();

                std::vector<ModuleID> vtModules;
                std::vector<mdMethodDef> vtMethodDefs;

                // Preallocate with size => 15 due this is the current max of method interceptions in a single module
                // (see InstrumentationDefinitions.Generated.cs)
                vtModules.reserve(15);
                vtMethodDefs.reserve(15);

                for (int i = 0; i < item->m_length; i++)
                {
                    auto _ = trace::Stats::Instance()->CallTargetRequestRejitMeasure();
                    const ModuleInfo& moduleInfo = GetModuleInfo(profilerInfo, *pModuleId);
                    Logger::Debug("Requesting Rejit for Module: ", moduleInfo.assembly.name);

                    ComPtr<IUnknown> metadataInterfaces;
                    ComPtr<IMetaDataImport2> metadataImport;
                    ComPtr<IMetaDataEmit2> metadataEmit;
                    ComPtr<IMetaDataAssemblyImport> assemblyImport;
                    ComPtr<IMetaDataAssemblyEmit> assemblyEmit;
                    std::unique_ptr<AssemblyMetadata> assemblyMetadata = nullptr;

                    for (const IntegrationMethod& integration : *pIntegrations)
                    {
                        // If the integration mode is not CallTarget we skip.
                        if (integration.replacement.wrapper_method.action != calltarget_modification_action)
                        {
                            continue;
                        }

                        // If the integration is not for the current assembly we skip.
                        if (integration.replacement.target_method.assembly.name != moduleInfo.assembly.name)
                        {
                            continue;
                        }

                        if (assemblyMetadata == nullptr)
                        {
                            Logger::Debug("  Loading Assembly Metadata...");
                            auto hr = profilerInfo->GetModuleMetaData(moduleInfo.id, ofRead | ofWrite, IID_IMetaDataImport2, metadataInterfaces.GetAddressOf());
                            if (FAILED(hr))
                            {
                                Logger::Warn("CallTarget_RequestRejitForModule failed to get metadata interface for ",
                                             moduleInfo.id, " ", moduleInfo.assembly.name);
                                break;
                            }

                            metadataImport = metadataInterfaces.As<IMetaDataImport2>(IID_IMetaDataImport);
                            metadataEmit = metadataInterfaces.As<IMetaDataEmit2>(IID_IMetaDataEmit);
                            assemblyImport = metadataInterfaces.As<IMetaDataAssemblyImport>(IID_IMetaDataAssemblyImport);
                            assemblyEmit = metadataInterfaces.As<IMetaDataAssemblyEmit>(IID_IMetaDataAssemblyEmit);
                            assemblyMetadata = std::make_unique<AssemblyMetadata>(GetAssemblyImportMetadata(assemblyImport));
                            Logger::Debug("  Assembly Metadata loaded for: ", assemblyMetadata->name, "(",
                                          assemblyMetadata->version.str(), ").");
                        }

                        // Check min version
                        if (integration.replacement.target_method.min_version > assemblyMetadata->version)
                        {
                            continue;
                        }

                        // Check max version
                        if (integration.replacement.target_method.max_version < assemblyMetadata->version)
                        {
                            continue;
                        }

                        // We are in the right module, so we try to load the mdTypeDef from the integration target type name.
                        mdTypeDef typeDef = mdTypeDefNil;
                        auto foundType = FindTypeDefByName(integration.replacement.target_method.type_name, moduleInfo.assembly.name, metadataImport, typeDef);
                        if (!foundType)
                        {
                            continue;
                        }

                        Logger::Debug("  Looking for '", integration.replacement.target_method.type_name, ".",
                                      integration.replacement.target_method.method_name, "(",
                                      (integration.replacement.target_method.signature_types.size() - 1),
                                      " params)' method.");

                        // Now we enumerate all methods with the same target method name. (All overloads of the method)
                        auto enumMethods = Enumerator<mdMethodDef>(
                            [&metadataImport, &integration, typeDef](HCORENUM* ptr, mdMethodDef arr[], ULONG max,
                                                                   ULONG* cnt) -> HRESULT {
                                return metadataImport->EnumMethodsWithName(
                                    ptr, typeDef, integration.replacement.target_method.method_name.c_str(), arr, max,
                                    cnt);
                            },
                            [&metadataImport](HCORENUM ptr) -> void { metadataImport->CloseEnum(ptr); });

                        auto enumIterator = enumMethods.begin();
                        while (enumIterator != enumMethods.end())
                        {
                            auto methodDef = *enumIterator;

                            // Extract the function info from the mdMethodDef
                            const auto caller = GetFunctionInfo(metadataImport, methodDef);
                            if (!caller.IsValid())
                            {
                                Logger::Warn("    * The caller for the methoddef: ", TokenStr(&methodDef),
                                             " is not valid!");
                                enumIterator = ++enumIterator;
                                continue;
                            }

                            // We create a new function info into the heap from the caller functionInfo in the stack, to
                            // be used later in the ReJIT process
                            auto functionInfo = FunctionInfo(caller);
                            auto hr = functionInfo.method_signature.TryParse();
                            if (FAILED(hr))
                            {
                                Logger::Warn("    * The method signature: ", functionInfo.method_signature.str(),
                                             " cannot be parsed.");
                                enumIterator = ++enumIterator;
                                continue;
                            }

                            // Compare if the current mdMethodDef contains the same number of arguments as the
                            // instrumentation target
                            const auto numOfArgs = functionInfo.method_signature.NumberOfArguments();
                            if (numOfArgs != integration.replacement.target_method.signature_types.size() - 1)
                            {
                                Logger::Debug("    * The caller for the methoddef: ",
                                              integration.replacement.target_method.method_name,
                                              " doesn't have the right number of arguments (", numOfArgs,
                                              " arguments).");
                                enumIterator = ++enumIterator;
                                continue;
                            }

                            // Compare each mdMethodDef argument type to the instrumentation target
                            bool argumentsMismatch = false;
                            const auto methodArguments = functionInfo.method_signature.GetMethodArguments();
                            Logger::Debug("    * Comparing signature for method: ",
                                          integration.replacement.target_method.type_name, ".",
                                          integration.replacement.target_method.method_name);
                            for (unsigned int i = 0; i < numOfArgs; i++)
                            {
                                const auto argumentTypeName = methodArguments[i].GetTypeTokName(metadataImport);
                                const auto integrationArgumentTypeName = integration.replacement.target_method.signature_types[i + 1];
                                Logger::Debug("        -> ", argumentTypeName, " = ", integrationArgumentTypeName);
                                if (argumentTypeName != integrationArgumentTypeName && integrationArgumentTypeName != WStr("_"))
                                {
                                    argumentsMismatch = true;
                                    break;
                                }
                            }
                            if (argumentsMismatch)
                            {
                                Logger::Debug("    * The caller for the methoddef: ",
                                              integration.replacement.target_method.method_name,
                                              " doesn't have the right type of arguments.");
                                enumIterator = ++enumIterator;
                                continue;
                            }

                            // As we are in the right method, we gather all information we need and stored it in to the
                            // ReJIT handler.
                            auto moduleHandler = handler->GetOrAddModule(moduleInfo.id);
                            if (moduleHandler->GetModuleMetadata() == nullptr)
                            {
                                Logger::Debug("Creating ModuleMetadata...");

                                const auto moduleMetadata =
                                    new ModuleMetadata(metadataImport, metadataEmit, assemblyImport, assemblyEmit,
                                                       moduleInfo.assembly.name, moduleInfo.assembly.app_domain_id,
                                                       handler->m_pCorAssemblyProperty);

                                Logger::Info("ReJIT handler stored metadata for ", moduleInfo.id, " ",
                                             moduleInfo.assembly.name, " AppDomain ", moduleInfo.assembly.app_domain_id,
                                             " ", moduleInfo.assembly.app_domain_name);

                                moduleHandler->SetModuleMetadata(moduleMetadata);
                            }

                            auto methodHandler = moduleHandler->GetOrAddMethod(methodDef);
                            if (methodHandler->GetFunctionInfo() == nullptr)
                            {
                                methodHandler->SetFunctionInfo(functionInfo);
                            }
                            if (methodHandler->GetMethodReplacement() == nullptr)
                            {
                                methodHandler->SetMethodReplacement(integration.replacement);
                            }

                            // Store module_id and methodDef to request the ReJIT after analyzing all integrations.
                            vtModules.push_back(moduleInfo.id);
                            vtMethodDefs.push_back(methodDef);

                            Logger::Debug("    * Enqueue for ReJIT [ModuleId=", moduleInfo.id,
                                          ", MethodDef=", TokenStr(&methodDef),
                                          ", AppDomainId=", moduleHandler->GetModuleMetadata()->app_domain_id,
                                          ", Assembly=", moduleHandler->GetModuleMetadata()->assemblyName,
                                          ", Type=", caller.type.name,
                                          ", Method=", caller.name, "(", numOfArgs,
                                          " params), Signature=", caller.signature.str(), "]");
                            enumIterator = ++enumIterator;
                        }
                    }

                    pModuleId++;
                }

                // Request the ReJIT for all integrations found in the module.
                if (!vtMethodDefs.empty())
                {
                    handler->EnqueueForRejit(vtModules, vtMethodDefs);
                }

                if (item->m_promise != nullptr)
                {
                    item->m_promise->set_value(vtMethodDefs.size());
                }
            }
        }
    }
    Logger::Info("Exiting ReJIT request thread.");
}

void RejitHandler::RequestRejitForInlinersInModule(ModuleID moduleId)
{
    if (m_profilerInfo6 != nullptr)
    {
        std::lock_guard<std::mutex> guard(m_modules_lock);
        for (const auto& mod : m_modules)
        {
            mod.second->RequestRejitForInlinersInModule(moduleId);
        }
    }
}

RejitHandler::RejitHandler(ICorProfilerInfo4* pInfo,
                           std::function<HRESULT(RejitHandlerModule*, RejitHandlerModuleMethod*)> rewriteCallback)
{
    m_profilerInfo = pInfo;
    m_profilerInfo6 = nullptr;
    m_profilerInfo10 = nullptr;
    m_rewriteCallback = rewriteCallback;
    m_rejit_queue = std::make_unique<UniqueBlockingQueue<RejitItem>>();
    m_rejit_queue_thread = std::make_unique<std::thread>(EnqueueThreadLoop, this);
}

RejitHandler::RejitHandler(ICorProfilerInfo6* pInfo,
                           std::function<HRESULT(RejitHandlerModule*, RejitHandlerModuleMethod*)> rewriteCallback)
{
    m_profilerInfo = pInfo;
    m_profilerInfo6 = pInfo;
    m_profilerInfo10 = nullptr;
    m_rewriteCallback = rewriteCallback;
    m_rejit_queue = std::make_unique<UniqueBlockingQueue<RejitItem>>();
    m_rejit_queue_thread = std::make_unique<std::thread>(EnqueueThreadLoop, this);
}

RejitHandler::RejitHandler(ICorProfilerInfo10* pInfo,
                           std::function<HRESULT(RejitHandlerModule*, RejitHandlerModuleMethod*)> rewriteCallback)
{
    m_profilerInfo = pInfo;
    m_profilerInfo6 = pInfo;
    m_profilerInfo10 = pInfo;
    m_rewriteCallback = rewriteCallback;
    m_rejit_queue = std::make_unique<UniqueBlockingQueue<RejitItem>>();
    m_rejit_queue_thread = std::make_unique<std::thread>(EnqueueThreadLoop, this);
}

RejitHandlerModule* RejitHandler::GetOrAddModule(ModuleID moduleId)
{
    std::lock_guard<std::mutex> guard(m_modules_lock);

    auto find_res = m_modules.find(moduleId);
    if (find_res != m_modules.end())
    {
        return find_res->second.get();
    }

    RejitHandlerModule* moduleHandler = new RejitHandlerModule(moduleId, this);
    m_modules[moduleId] = std::unique_ptr<RejitHandlerModule>(moduleHandler);
    return moduleHandler;
}

bool RejitHandler::HasModuleAndMethod(ModuleID moduleId, mdMethodDef methodDef)
{
    std::lock_guard<std::mutex> guard(m_modules_lock);

    auto find_res = m_modules.find(moduleId);
    if (find_res != m_modules.end())
    {
        auto moduleHandler = find_res->second.get();
        return moduleHandler->ContainsMethod(methodDef);
    }

    return false;
}

void RejitHandler::RemoveModule(ModuleID moduleId)
{
    std::lock_guard<std::mutex> guard(m_modules_lock);
    m_modules.erase(moduleId);
}

void RejitHandler::AddNGenModule(ModuleID moduleId)
{
    std::lock_guard<std::mutex> guard(m_ngenModules_lock);
    m_ngenModules.push_back(moduleId);
    RequestRejitForInlinersInModule(moduleId);
}

void RejitHandler::EnqueueForRejit(const std::vector<ModuleID>& modulesVector, const std::vector<mdMethodDef>& modulesMethodDef)
{
    Logger::Debug("RejitHandler::EnqueueForReJIT");
    const size_t length = modulesMethodDef.size();

    auto moduleIds = std::make_unique<ModuleID[]>(length);
    std::copy(modulesVector.begin(), modulesVector.end(), moduleIds.get());

    auto mDefs = std::make_unique<mdMethodDef[]>(length);
    std::copy(modulesMethodDef.begin(), modulesMethodDef.end(), mDefs.get());

    // Create module and methods metadata.
    for (size_t i = 0; i < length; i++)
    {
        GetOrAddModule(moduleIds[i])->GetOrAddMethod(mDefs[i]);
    }

    // Enqueue rejit
    m_rejit_queue->push(std::make_unique<RejitItem>((int) length, std::move(moduleIds), std::move(mDefs), nullptr, nullptr));
}

void RejitHandler::EnqueueProcessModule(const std::vector<ModuleID>& modulesVector,
                                        const std::vector<IntegrationMethod>& integrations,
                                        std::promise<int>* promise)
{
    Logger::Debug("RejitHandler::EnqueueProcessModule");
    const size_t length = modulesVector.size();

    auto moduleIds = std::make_unique<ModuleID[]>(length);
    std::copy(modulesVector.begin(), modulesVector.end(), moduleIds.get());

    // Enqueue process module
    m_rejit_queue->push(std::make_unique<RejitItem>((int) length, std::move(moduleIds), nullptr,
                                                    std::make_unique<std::vector<IntegrationMethod>>(integrations),
                                                    promise));
}

void RejitHandler::Shutdown()
{
    Logger::Debug("RejitHandler::Shutdown");
    std::lock_guard<std::mutex> moduleGuard(m_modules_lock);
    std::lock_guard<std::mutex> ngenModuleGuard(m_ngenModules_lock);

    m_rejit_queue->push(RejitItem::CreateEndRejitThread());
    if (m_rejit_queue_thread->joinable())
    {
        m_rejit_queue_thread->join();
    }

    m_modules.clear();
    m_profilerInfo = nullptr;
    m_rewriteCallback = nullptr;
}

HRESULT RejitHandler::NotifyReJITParameters(ModuleID moduleId, mdMethodDef methodId,
                                            ICorProfilerFunctionControl* pFunctionControl)
{
    auto moduleHandler = GetOrAddModule(moduleId);
    auto methodHandler = moduleHandler->GetOrAddMethod(methodId);
    methodHandler->SetFunctionControl(pFunctionControl);

    if (methodHandler->GetMethodDef() == mdMethodDefNil)
    {
        Logger::Warn("NotifyReJITCompilationStarted: mdMethodDef is missing for "
                     "MethodDef: ",
                     methodId);
        return S_FALSE;
    }

    if (methodHandler->GetFunctionControl() == nullptr)
    {
        Logger::Warn("NotifyReJITCompilationStarted: ICorProfilerFunctionControl is missing "
                     "for "
                     "MethodDef: ",
                     methodId);
        return S_FALSE;
    }

    if (methodHandler->GetFunctionInfo() == nullptr)
    {
        Logger::Warn("NotifyReJITCompilationStarted: FunctionInfo is missing for "
                     "MethodDef: ",
                     methodId);
        return S_FALSE;
    }

    if (methodHandler->GetMethodReplacement() == nullptr)
    {
        Logger::Warn("NotifyReJITCompilationStarted: MethodReplacement is missing for "
                     "MethodDef: ",
                     methodId);
        return S_FALSE;
    }

    if (moduleHandler->GetModuleId() == 0)
    {
        Logger::Warn("NotifyReJITCompilationStarted: ModuleID is missing for "
                     "MethodDef: ",
                     methodId);
        return S_FALSE;
    }

    if (moduleHandler->GetModuleMetadata() == nullptr)
    {
        Logger::Warn("NotifyReJITCompilationStarted: ModuleMetadata is missing for "
                     "MethodDef: ",
                     methodId);
        return S_FALSE;
    }

    return m_rewriteCallback(moduleHandler, methodHandler);
}

HRESULT RejitHandler::NotifyReJITCompilationStarted(FunctionID functionId, ReJITID rejitId)
{
    return S_OK;
}

ICorProfilerInfo4* RejitHandler::GetCorProfilerInfo()
{
    return m_profilerInfo;
}

ICorProfilerInfo6* RejitHandler::GetCorProfilerInfo6()
{
    return m_profilerInfo6;
}

void RejitHandler::SetCorAssemblyProfiler(AssemblyProperty* pCorAssemblyProfiler)
{
    m_pCorAssemblyProperty = pCorAssemblyProfiler;
}

void RejitHandler::RequestRejitForNGenInliners()
{
    if (m_profilerInfo6 != nullptr)
    {
        std::lock_guard<std::mutex> guard(m_ngenModules_lock);
        for (const auto& mod : m_ngenModules)
        {
            RequestRejitForInlinersInModule(mod);
        }
    }
}

} // namespace trace