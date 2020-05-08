#include "pch.h"
#include "./CClrObject.hpp"

CClrObject::CClrObject(const SClrObject_New* aArgsPtr)
:
    mNewArgsPtr(0)
,   mModuleHandle(NULL)
,   mNewFunc(0)
,   mFreeFunc(0)
,   mHandle(0)
{
    this->mModuleHandle = ::LoadLibraryA("C:\\Program Files\\Cycling '74\\Max 8\\packages\\max-sdk-8.0.3\\externals\\CbMaxClrAdapter.dll");
    if(this->mModuleHandle != NULL)
    {
        this->mNewFunc = reinterpret_cast<CNewFunc>(::GetProcAddress(this->mModuleHandle, "Object_New"));
        this->mFreeFunc = reinterpret_cast<CFreeFunc>(::GetProcAddress(this->mModuleHandle, "Object_Free"));
    }

    if(this->mNewFunc
    && this->mFreeFunc)
    {
        this->mHandle = this->mNewFunc(*aArgsPtr);
    }
}

CClrObject::~CClrObject()
{
    if (this->mFreeFunc)
    {
        this->mFreeFunc(this->mHandle);
    }

}

//CClrObject::Size CClrObject::GetInletCount()
//{
//    return this->mGetInletCountFunc ? this->mGetInletCountFunc(this->mHandle) : 0;
//}
//
//CClrObject::Size CClrObject::GetOutletCount()
//{
//    return this->mGetOutletCountFunc ? this->mGetOutletCountFunc(this->mHandle) : 0;
//}
