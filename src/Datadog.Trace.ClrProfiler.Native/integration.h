#ifndef DD_CLR_PROFILER_INTEGRATION_H_
#define DD_CLR_PROFILER_INTEGRATION_H_

#include <corhlpr.h>
#include <iomanip>
#include <sstream>
#include <vector>

#include "cor/clr_helpers.h"
#include "strings/str_util.h"
#include "util.h"

namespace trace
{

struct MethodReference
{
    const AssemblyReference assembly;
    const WSTRING type_name;
    const WSTRING method_name;
    const WSTRING action;
    const MethodSignature method_signature;
    const Version min_version;
    const Version max_version;
    const std::vector<WSTRING> signature_types;

    MethodReference() :
        min_version(Version(0, 0, 0, 0)), max_version(Version(USHRT_MAX, USHRT_MAX, USHRT_MAX, USHRT_MAX))
    {
    }

    MethodReference(const WSTRING& assembly_name, WSTRING type_name, WSTRING method_name, WSTRING action,
                    Version min_version, Version max_version, const std::vector<BYTE>& method_signature,
                    const std::vector<WSTRING>& signature_types) :
        assembly(assembly_name),
        type_name(type_name),
        method_name(method_name),
        action(action),
        method_signature(method_signature),
        min_version(min_version),
        max_version(max_version),
        signature_types(signature_types)
    {
    }

    inline WSTRING get_type_cache_key() const
    {
        return WStr("[") + assembly.name + WStr("]") + type_name + WStr("_vMin_") + min_version.str() + WStr("_vMax_") +
               max_version.str();
    }

    inline WSTRING get_method_cache_key() const
    {
        return WStr("[") + assembly.name + WStr("]") + type_name + WStr(".") + method_name + WStr("_vMin_") +
               min_version.str() + WStr("_vMax_") + max_version.str();
    }

    inline bool operator==(const MethodReference& other) const
    {
        return assembly == other.assembly && type_name == other.type_name && min_version == other.min_version &&
               max_version == other.max_version && method_name == other.method_name &&
               method_signature == other.method_signature;
    }
};

struct MethodReplacement
{
    const MethodReference caller_method;
    const MethodReference target_method;
    const MethodReference wrapper_method;

    MethodReplacement()
    {
    }

    MethodReplacement(MethodReference caller_method, MethodReference target_method, MethodReference wrapper_method) :
        caller_method(caller_method), target_method(target_method), wrapper_method(wrapper_method)
    {
    }

    inline bool operator==(const MethodReplacement& other) const
    {
        return caller_method == other.caller_method && target_method == other.target_method &&
               wrapper_method == other.wrapper_method;
    }
};

struct Integration
{
    const WSTRING integration_name;
    std::vector<MethodReplacement> method_replacements;

    Integration() : integration_name(WStr("")), method_replacements({})
    {
    }

    Integration(WSTRING integration_name, std::vector<MethodReplacement> method_replacements) :
        integration_name(integration_name), method_replacements(method_replacements)
    {
    }

    inline bool operator==(const Integration& other) const
    {
        return integration_name == other.integration_name && method_replacements == other.method_replacements;
    }
};

struct IntegrationMethod
{
    const WSTRING integration_name;
    MethodReplacement replacement;

    IntegrationMethod() : integration_name(WStr("")), replacement({})
    {
    }

    IntegrationMethod(WSTRING integration_name, MethodReplacement replacement) :
        integration_name(integration_name), replacement(replacement)
    {
    }

    inline bool operator==(const IntegrationMethod& other) const
    {
        return integration_name == other.integration_name && replacement == other.replacement;
    }
};

// FilterIntegrationsByName removes integrations whose names are specified in
// disabled_integration_names
std::vector<Integration> FilterIntegrationsByName(const std::vector<Integration>& integrations,
                                                  const std::vector<WSTRING>& disabled_integration_names);

// FlattenIntegrations flattens integrations to per method structures
std::vector<IntegrationMethod> FlattenIntegrations(const std::vector<Integration>& integrations,
                                                   bool is_calltarget_enabled);

// FilterIntegrationsByCaller removes any integrations which have a caller and
// its not set to the module
std::vector<IntegrationMethod> FilterIntegrationsByCaller(const std::vector<IntegrationMethod>& integration_methods,
                                                          const AssemblyInfo assembly);

// FilterIntegrationsByTarget removes any integrations which have a target not
// referenced by the module's assembly import
std::vector<IntegrationMethod> FilterIntegrationsByTarget(const std::vector<IntegrationMethod>& integration_methods,
                                                          const ComPtr<IMetaDataAssemblyImport>& assembly_import);

// FilterIntegrationsByTargetAssemblyName removes any integrations which target any
// of the specified assemblies
std::vector<IntegrationMethod>
FilterIntegrationsByTargetAssemblyName(const std::vector<IntegrationMethod>& integration_methods,
                                       const std::vector<WSTRING>& excluded_assembly_names);

namespace
{

    WSTRING GetNameFromAssemblyReferenceString(const WSTRING& wstr);
    Version GetVersionFromAssemblyReferenceString(const WSTRING& wstr);
    WSTRING GetLocaleFromAssemblyReferenceString(const WSTRING& wstr);
    PublicKey GetPublicKeyFromAssemblyReferenceString(const WSTRING& wstr);

} // namespace

} // namespace trace

#endif // DD_CLR_PROFILER_INTEGRATION_H_
