#ifndef _CbMaxClrAdapterStaticDll_h_6009b2ba_361e_4976_9076_d7bf92508649_
#define _CbMaxClrAdapterStaticDll_h_6009b2ba_361e_4976_9076_d7bf92508649_

#include <Windows.h>

#ifdef __cplusplus
extern "C" {
#endif
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	typedef void* CClrObjectVoidPtr;
	typedef INT64 Size;

	typedef struct _SClrObject_New
	{
		void* mObjectPtr;
		const TCHAR* mAssemblyName;
		const TCHAR* mTypeName;		
	} SClrObject_New;


	CClrObjectVoidPtr ClrObject_New(const SClrObject_New* aArgsPtr);
	void ClrObject_Init(CClrObjectVoidPtr aClrObjectVoidPtr);
	void ClrObject_Free(CClrObjectVoidPtr aClrObjectVoidPtr);




	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#ifdef __cplusplus
}
#endif
#endif
