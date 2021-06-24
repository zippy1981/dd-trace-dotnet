#ifndef DD_CLR_PROFILER_TRACER_CLASS_FACTORY_H_
#define DD_CLR_PROFILER_TRACER_CLASS_FACTORY_H_

#include "class_factory_base.h"

class TracerClassFactory : public ClassFactory
{
private:
    HRESULT STDMETHODCALLTYPE OnCreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppvObject,
                                               HINSTANCE dllInstance) override;
};

#endif // DD_CLR_PROFILER_TRACER_CLASS_FACTORY_H_
