#pragma once
#include <Windows.h>
#include "CbMaxClrAdapterStaticDll.h"

class CClrObject
{
	typedef void (CCallback)();

	public: CClrObject(const SClrObject_New* aArgsPtr);
	public: ~CClrObject();

	private: const SClrObject_New* mNewArgsPtr;
	private: HMODULE mModuleHandle;

	private: typedef UINT64 CObjectHandle;
	public: typedef INT64 Size;

	private: using CNewFunc = CObjectHandle(__stdcall*)(SClrObject_New aArgs);
	private: CNewFunc mNewFunc;

	private: using CFreeFunc = CObjectHandle(__stdcall*)(CObjectHandle aObjectHandle);
	private: CFreeFunc mFreeFunc;

	private: typedef UINT64 CClrObjectHandle;

	private: CClrObjectHandle mHandle;

};

