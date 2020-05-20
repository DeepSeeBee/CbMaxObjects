#include "pch.h"
#include "./CClrObject.hpp"

CClrObject::CClrObject(const SClrObject_New* aArgsPtr)
   :
     mNewArgsPtr(0)
   , mModuleHandle(NULL)
   , mNewFunc(0)
   , mFreeFunc(0)
   , mInitFunc(0)
   , mHandle(0)
{
   HMODULE aModuleHandle = NULL;
   const TCHAR* aFuncName = TEXT("ClrObject_New");
   GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, (LPCWSTR)aFuncName, &aModuleHandle);
   const int aPathMaxLen = MAX_PATH;
   TCHAR aPath[aPathMaxLen];
   GetModuleFileName(aModuleHandle, aPath, sizeof(aPath));  
   int aPathLen = lstrlen(aPath);
   int aIdx = 0;
   int aBackslashIdx = -1;
   while (aIdx < aPathLen)
   {
      if (aPath[aIdx] == '\\')
         aBackslashIdx = aIdx;
      ++aIdx;
   }
   if (aBackslashIdx >= 0)
      aPath[aBackslashIdx] = 0;
   lstrcat(aPath, TEXT("\\CbMaxClrAdapter.dll"));   
   this->mModuleHandle = ::LoadLibraryW(aPath);
   if (this->mModuleHandle != NULL)
   {
      this->mNewFunc = reinterpret_cast<CNewFunc>(::GetProcAddress(this->mModuleHandle, "Object_New"));
      this->mFreeFunc = reinterpret_cast<CFreeFunc>(::GetProcAddress(this->mModuleHandle, "Object_Free"));
      this->mInitFunc = reinterpret_cast<CInitFunc>(::GetProcAddress(this->mModuleHandle, "Object_Init"));
   }

   if (this->mNewFunc
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

void CClrObject::Init()
{
   if (this->mHandle
   && this->mInitFunc)
   {
      this->mInitFunc(this->mHandle);
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
