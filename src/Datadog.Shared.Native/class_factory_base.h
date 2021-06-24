#ifndef DD_SHARED_CLASS_FACTORY_H_
#define DD_SHARED_CLASS_FACTORY_H_

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full
// license information.

#include "unknwn.h"
#include <atomic>

class ClassFactory : public IClassFactory
{
private:
    std::atomic<int> refCount;
    HINSTANCE dllInstance;

    virtual HRESULT STDMETHODCALLTYPE OnCreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppvObject, HINSTANCE dllInstance) = 0;

public:
    ClassFactory();
    virtual ~ClassFactory();
    void STDMETHODCALLTYPE SetDllInstance(HINSTANCE instance);

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject) override;
    ULONG STDMETHODCALLTYPE AddRef(void) override;
    ULONG STDMETHODCALLTYPE Release(void) override;
    HRESULT STDMETHODCALLTYPE CreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppvObject) override;
    HRESULT STDMETHODCALLTYPE LockServer(BOOL fLock) override;
};

#endif // DD_SHARED_CLASS_FACTORY_H_
