#ifndef DD_SHARED_MACROS_H_
#define DD_SHARED_MACROS_H_

#include <corhlpr.h>
#include <fstream>

#define RETURN_IF_FAILED(EXPR)                                                                                         \
    do                                                                                                                 \
    {                                                                                                                  \
        hr = (EXPR);                                                                                                   \
        if (FAILED(hr))                                                                                                \
        {                                                                                                              \
            return (hr);                                                                                               \
        }                                                                                                              \
    } while (0)

#define RETURN_OK_IF_FAILED(EXPR)                                                                                      \
    do                                                                                                                 \
    {                                                                                                                  \
        hr = (EXPR);                                                                                                   \
        if (FAILED(hr))                                                                                                \
        {                                                                                                              \
            return S_OK;                                                                                               \
        }                                                                                                              \
    } while (0)

#define IfFalseRetFAIL(EXPR)                                                                                           \
    do                                                                                                                 \
    {                                                                                                                  \
        if ((EXPR) == false) return E_FAIL;                                                                            \
    } while (0)


#define CheckIfTrue(EXPR)                                                                                              \
    static int sValue = -1;                                                                                            \
    if (sValue == -1)                                                                                                  \
    {                                                                                                                  \
        const auto envValue = EXPR;                                                                                    \
        sValue = envValue == WStr("1") || envValue == WStr("true") ? 1 : 0;                                            \
    }                                                                                                                  \
    return sValue == 1;

#define CheckIfFalse(EXPR)                                                                                             \
    static int sValue = -1;                                                                                            \
    if (sValue == -1)                                                                                                  \
    {                                                                                                                  \
        const auto envValue = EXPR;                                                                                    \
        sValue = envValue == WStr("0") || envValue == WStr("false") ? 1 : 0;                                           \
    }                                                                                                                  \
    return sValue == 1;

#define ToBooleanWithDefault(EXPR, DEFAULT)                                                                            \
    static int sValue = -1;                                                                                            \
    if (sValue == -1)                                                                                                  \
    {                                                                                                                  \
        const auto envValue = EXPR;                                                                                    \
        if (envValue == WStr("1") || envValue == WStr("true"))                                                         \
        {                                                                                                              \
            sValue = 1;                                                                                                \
        }                                                                                                              \
        else if (envValue == WStr("0") || envValue == WStr("false"))                                                   \
        {                                                                                                              \
            sValue = 0;                                                                                                \
        }                                                                                                              \
        else                                                                                                           \
        {                                                                                                              \
            sValue = DEFAULT;                                                                                          \
        }                                                                                                              \
    }                                                                                                                  \
    return sValue == 1;


#endif // DD_SHARED_MACROS_H_

