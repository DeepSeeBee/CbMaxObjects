// CbMaxClrAdapterStaticDll.cpp : Hiermit werden die Funktionen für die statische Bibliothek definiert.
//

#include "pch.h"
#include "framework.h"
#include "CbMaxClrAdapterStaticDll.h"
#include "CClrObject.hpp"


extern "C"
{
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	static CClrObject* GetClrObjectPtr(CClrObjectVoidPtr aPtr)
	{
		return reinterpret_cast<CClrObject*>(aPtr);
	}

	CClrObjectVoidPtr ClrObject_New(const SClrObject_New* aArgsPtr)
	{
		return new CClrObject(aArgsPtr);
	}

	void ClrObject_Free(CClrObjectVoidPtr aObjectPtr)
	{
		delete GetClrObjectPtr(aObjectPtr);
	}

	void ClrObject_Init(CClrObjectVoidPtr aClrObjectVoidPtr)
	{
		CClrObject* aClrObjectPtr = GetClrObjectPtr(aClrObjectVoidPtr);
		if (aClrObjectPtr)
		{
			aClrObjectPtr->Init();
		}
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
