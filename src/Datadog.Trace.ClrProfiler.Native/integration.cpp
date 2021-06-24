
#include "integration.h"

#ifdef _WIN32
#include <regex>
#else
#include <re2/re2.h>
#endif
#include <sstream>

#include "cor/clr_helpers.h"
#include "dd_profiler_constants.h"
#include "util.h"

namespace trace
{

AssemblyReference::AssemblyReference(const WSTRING& str) :
    name(GetNameFromAssemblyReferenceString(str)),
    version(GetVersionFromAssemblyReferenceString(str)),
    locale(GetLocaleFromAssemblyReferenceString(str)),
    public_key(GetPublicKeyFromAssemblyReferenceString(str))
{
}

std::vector<Integration> FilterIntegrationsByName(const std::vector<Integration>& integrations,
                                                  const std::vector<WSTRING>& disabled_integration_names)
{
    std::vector<Integration> enabled;

    for (auto& i : integrations)
    {
        bool disabled = false;
        for (auto& disabled_integration : disabled_integration_names)
        {
            if (i.integration_name == disabled_integration)
            {
                // this integration is disabled, skip it
                disabled = true;
                break;
            }
        }

        if (!disabled)
        {
            enabled.push_back(i);
        }
    }

    return enabled;
}

std::vector<IntegrationMethod> FlattenIntegrations(const std::vector<Integration>& integrations,
                                                   bool is_calltarget_enabled)
{
    std::vector<IntegrationMethod> flattened;

    for (auto& i : integrations)
    {
        for (auto& mr : i.method_replacements)
        {
            const auto isCallTargetIntegration = mr.wrapper_method.action == calltarget_modification_action;

            if (is_calltarget_enabled && isCallTargetIntegration)
            {
                flattened.emplace_back(i.integration_name, mr);
            }
            else if (!is_calltarget_enabled && !isCallTargetIntegration)
            {
                flattened.emplace_back(i.integration_name, mr);
            }
        }
    }

    return flattened;
}

std::vector<IntegrationMethod> FilterIntegrationsByCaller(const std::vector<IntegrationMethod>& integration_methods,
                                                          const AssemblyInfo assembly)
{
    std::vector<IntegrationMethod> enabled;

    for (auto& i : integration_methods)
    {
        if (i.replacement.caller_method.assembly.name.empty() ||
            i.replacement.caller_method.assembly.name == assembly.name)
        {
            enabled.push_back(i);
        }
    }

    return enabled;
}

bool AssemblyMeetsIntegrationRequirements(const AssemblyMetadata metadata, const MethodReplacement method_replacement)
{
    const auto target = method_replacement.target_method;

    if (target.assembly.name != metadata.name)
    {
        // not the expected assembly
        return false;
    }

    if (target.min_version > metadata.version)
    {
        return false;
    }

    if (target.max_version < metadata.version)
    {
        return false;
    }

    return true;
}

std::vector<IntegrationMethod> FilterIntegrationsByTarget(const std::vector<IntegrationMethod>& integration_methods,
                                                          const ComPtr<IMetaDataAssemblyImport>& assembly_import)
{
    std::vector<IntegrationMethod> enabled;

    const auto assembly_metadata = GetAssemblyImportMetadata(assembly_import);

    for (auto& i : integration_methods)
    {
        bool found = false;
        if (AssemblyMeetsIntegrationRequirements(assembly_metadata, i.replacement))
        {
            found = true;
        }
        else
        {
            for (auto& assembly_ref : EnumAssemblyRefs(assembly_import))
            {
                const auto metadata_ref = GetReferencedAssemblyMetadata(assembly_import, assembly_ref);
                if (AssemblyMeetsIntegrationRequirements(metadata_ref, i.replacement))
                {
                    found = true;
                    break;
                }
            }
        }

        if (found)
        {
            enabled.push_back(i);
        }
    }

    return enabled;
}

std::vector<IntegrationMethod>
FilterIntegrationsByTargetAssemblyName(const std::vector<IntegrationMethod>& integration_methods,
                                       const std::vector<WSTRING>& excluded_assembly_names)
{
    std::vector<IntegrationMethod> methods;

    for (auto& i : integration_methods)
    {
        bool assembly_excluded = false;

        for (auto& excluded_assembly_name : excluded_assembly_names)
        {
            if (i.replacement.target_method.assembly.name == excluded_assembly_name)
            {
                assembly_excluded = true;
                break;
            }
        }

        if (!assembly_excluded)
        {
            methods.emplace_back(i);
        }
    }

    return methods;
}

namespace
{

    WSTRING GetNameFromAssemblyReferenceString(const WSTRING& wstr)
    {
        WSTRING name = wstr;

        auto pos = name.find(WStr(','));
        if (pos != WSTRING::npos)
        {
            name = name.substr(0, pos);
        }

        // strip spaces
        pos = name.rfind(WStr(' '));
        if (pos != WSTRING::npos)
        {
            name = name.substr(0, pos);
        }

        return name;
    }

    Version GetVersionFromAssemblyReferenceString(const WSTRING& str)
    {
        unsigned short major = 0;
        unsigned short minor = 0;
        unsigned short build = 0;
        unsigned short revision = 0;

#ifdef _WIN32

        static auto re = std::wregex(WStr("Version=([0-9]+)\\.([0-9]+)\\.([0-9]+)\\.([0-9]+)"));

        std::wsmatch match;
        if (std::regex_search(str, match, re) && match.size() == 5)
        {
            WSTRINGSTREAM(match.str(1)) >> major;
            WSTRINGSTREAM(match.str(2)) >> minor;
            WSTRINGSTREAM(match.str(3)) >> build;
            WSTRINGSTREAM(match.str(4)) >> revision;
        }

#else

        static re2::RE2 re("Version=([0-9]+)\\.([0-9]+)\\.([0-9]+)\\.([0-9]+)", RE2::Quiet);
        re2::RE2::FullMatch(ToString(str), re, &major, &minor, &build, &revision);

#endif

        return {major, minor, build, revision};
    }

    WSTRING GetLocaleFromAssemblyReferenceString(const WSTRING& str)
    {
        WSTRING locale = WStr("neutral");

#ifdef _WIN32

        static auto re = std::wregex(WStr("Culture=([a-zA-Z0-9]+)"));
        std::wsmatch match;
        if (std::regex_search(str, match, re) && match.size() == 2)
        {
            locale = match.str(1);
        }

#else

        static re2::RE2 re("Culture=([a-zA-Z0-9]+)", RE2::Quiet);

        std::string match;
        if (re2::RE2::FullMatch(ToString(str), re, &match))
        {
            locale = ToWSTRING(match);
        }

#endif

        return locale;
    }

    PublicKey GetPublicKeyFromAssemblyReferenceString(const WSTRING& str)
    {
        BYTE data[8] = {0};

#ifdef _WIN32

        static auto re = std::wregex(WStr("PublicKeyToken=([a-fA-F0-9]{16})"));
        std::wsmatch match;
        if (std::regex_search(str, match, re) && match.size() == 2)
        {
            for (int i = 0; i < 8; i++)
            {
                auto s = match.str(1).substr(i * 2, 2);
                unsigned long x;
                WSTRINGSTREAM(s) >> std::hex >> x;
                data[i] = BYTE(x);
            }
        }

#else

        static re2::RE2 re("PublicKeyToken=([a-fA-F0-9]{16})");
        std::string match;
        if (re2::RE2::FullMatch(ToString(str), re, &match))
        {
            for (int i = 0; i < 8; i++)
            {
                auto s = match.substr(i * 2, 2);
                unsigned long x;
                std::stringstream(s) >> std::hex >> x;
                data[i] = BYTE(x);
            }
        }

#endif

        return PublicKey(data);
    }

} // namespace

} // namespace trace
