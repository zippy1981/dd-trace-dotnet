#include "util.h"

namespace trace
{

WSTRING GetEnvironmentValue(const WSTRING& name)
{
#ifdef _WIN32
    const size_t max_buf_size = 4096;
    WSTRING buf(max_buf_size, 0);
    auto len = GetEnvironmentVariable(name.data(), buf.data(), (DWORD)(buf.size()));
    return Trim(buf.substr(0, len));
#else
    auto cstr = std::getenv(ToString(name).c_str());
    if (cstr == nullptr)
    {
        return WStr("");
    }
    std::string str(cstr);
    auto wstr = ToWSTRING(str);
    return Trim(wstr);
#endif
}

std::vector<WSTRING> GetEnvironmentValues(const WSTRING& name, const wchar_t delim)
{
    std::vector<WSTRING> values;
    for (auto s : Split(GetEnvironmentValue(name), delim))
    {
        s = Trim(s);
        if (!s.empty())
        {
            values.push_back(s);
        }
    }
    return values;
}

std::vector<WSTRING> GetEnvironmentValues(const WSTRING& name)
{
    return GetEnvironmentValues(name, L';');
}

} // namespace trace
