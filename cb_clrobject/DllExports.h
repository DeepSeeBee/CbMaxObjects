#pragma once

typedef void* CMaxObjectVoidPtr;

typedef void(__stdcall CObject_DeleteFunc)();
__declspec(dllexport) void __stdcall Object_Delete_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_DeleteFunc* aFuncPtr);

__declspec(dllexport) void* __stdcall Object_In_Add(CMaxObjectVoidPtr aSCbClrObjectPtr, long aType, long aPos);
__declspec(dllexport) void __stdcall In_Delete(void* aInletPtr);
__declspec(dllexport) void* __stdcall Object_Out_Add(CMaxObjectVoidPtr aSCbClrObjectPtr, long aType, long aPos);
__declspec(dllexport) void __stdcall Out_Delete(void* aInletPtr);

typedef void(__stdcall CObject_In_Bang_Func)(long aInletIdx);
__declspec(dllexport) void __stdcall Object_In_Bang_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_In_Bang_Func* aInBangFuncPtr);

typedef void(__stdcall CObject_In_Float_Func)(long aInletIdx, double aValue);
__declspec(dllexport) void __stdcall Object_In_Float_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_In_Float_Func* aInFloatFuncPtr);

typedef void(__stdcall CObject_In_Int_Func)(long aInletIdx, void* aValueI64);
__declspec(dllexport) void __stdcall Object_In_Int_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_In_Int_Func* aInIntFuncPtr);

typedef void(__stdcall CObject_In_Symbol_Func)(long aInletIdx, const TCHAR* aSymbolNamePTr);
__declspec(dllexport) void __stdcall Object_In_Symbol_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_In_Symbol_Func* aInIntFuncPtr);

__declspec(dllexport) void __stdcall Object_Out_Bang_Send(CMaxObjectVoidPtr aSCbClrObjectPtr, void* aOutletPtr);
__declspec(dllexport) void __stdcall Object_Out_Float_Send(CMaxObjectVoidPtr aSCbClrObjectPtr, void* aOutletPtr, double aValue);
__declspec(dllexport) void __stdcall Object_Out_Int_Send(CMaxObjectVoidPtr aSCbClrObjectPtr, void* aOutletPtr, long aValue);
__declspec(dllexport) void __stdcall Object_Out_Symbol_Send(CMaxObjectVoidPtr aSCbClrObjectPtr, void* aOutletPtr, const TCHAR* aSymbolName);
__declspec(dllexport) void __stdcall Object_Out_List_Send(CMaxObjectVoidPtr aSCbClrObjectPtr, void* aOutletPtr, long aOutletIdx);

typedef const TCHAR* (__stdcall CObject_Out_List_Symbol_Get_Func)(long aOutletIdx);
__declspec(dllexport) void __stdcall Object_Out_List_Symbol_Get_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_Out_List_Symbol_Get_Func* aFuncPtr);
typedef long(__stdcall CObject_Out_List_Element_Count_Get_Func)(long aOutletIdx);
__declspec(dllexport) void __stdcall Object_Out_List_Element_Count_Get_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_Out_List_Element_Count_Get_Func* aFuncPtr);
typedef long(__stdcall CObject_Out_List_Element_Type_Get_Func)(long aOutletIdx, long aElementIdx);
__declspec(dllexport) void __stdcall Object_Out_List_Element_Type_Get_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_Out_List_Element_Count_Get_Func* aFuncPtr);
typedef double(__stdcall CObject_Out_List_Element_Float_Get_Func)(long aOutletIdx, long aElementIdx);
__declspec(dllexport) void __stdcall Object_Out_List_Element_Float_Get_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_Out_List_Element_Float_Get_Func* aFuncPtr);
typedef long(__stdcall CObject_Out_List_Element_Int_Get_Func)(long aOutletIdx, long aElementIdx);
__declspec(dllexport) void __stdcall Object_Out_List_Element_Int_Get_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_Out_List_Element_Int_Get_Func* aFuncPtr);
typedef const TCHAR*(__stdcall CObject_Out_List_Element_Symbol_Get_Func)(long aOutletIdx, long aElementIdx);
__declspec(dllexport) void __stdcall Object_Out_List_Element_Symbol_Get_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_Out_List_Element_Symbol_Get_Func* aFuncPtr);


typedef void(__stdcall CMemory_Delete_Func) (void* aPtr);
__declspec(dllexport) void __stdcall Memory_Delete_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CMemory_Delete_Func* aFreeStringFuncPtr);

typedef const TCHAR* (__stdcall CGetAssistStringFunc)(CMaxObjectVoidPtr aSCbClrObjectPtr, long aStringProvider, long aIndex);
__declspec(dllexport) void __stdcall Object_Assist_GetString_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CGetAssistStringFunc* aGetAssistStringFuncPtr);

__declspec(dllexport) void __stdcall Max_Log_Write(CMaxObjectVoidPtr aSCbClrObjectPtr, void* aMessagePtr, long aError);

typedef void(__stdcall CObject_In_Receive)(long aInletIdx, long aDataType);
__declspec(dllexport) void __stdcall Object_In_Receive_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_In_Receive* aFuncPtr);

typedef void(__stdcall CObject_In_List_Clear)(long aInletIdx);
__declspec(dllexport) void __stdcall Object_In_List_Clear_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_In_List_Clear* aFuncPtr);

typedef void (__stdcall CObject_In_List_Add_Int)(long aInletIdx, long aInt);
__declspec(dllexport) void __stdcall Object_In_List_Add_Int_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_In_List_Add_Int* aFuncPtr);

typedef void(__stdcall CObject_In_List_Add_Float)(long aInletIdx, double aFloat);
__declspec(dllexport) void __stdcall Object_In_List_Add_Float_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_In_List_Add_Float* aFuncPtr);

typedef void (__stdcall CObject_In_List_Add_Symbol)(long aInletIdx, const TCHAR* aSymbol);
__declspec(dllexport) void __stdcall Object_In_List_Add_Symbol_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_In_List_Add_Symbol* aFuncPtr);


__declspec(dllexport) void __stdcall Object_In_Matrix_Receive(CMaxObjectVoidPtr aSCbClrObjectPtr, long aInletIdx, const TCHAR* aMatrixObjectName);

typedef long(__stdcall CObject_In_Matrix_Receive_Func)(long aInletIdx, long aSize, const TCHAR* aCellType, long aDimensionCount, void* aDimensionSizesI32s, void* aDimensionStridesI32s, long aPlaneCount, void* aMatrixDataPtr);
__declspec(dllexport) void __stdcall Object_In_Matrix_Receive_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_In_Matrix_Receive_Func* aFuncPtr);

__declspec(dllexport) void __stdcall Object_Out_Matrix_Send(CMaxObjectVoidPtr aSCbClrObjectPtr, void* aOutletPtr, long aSize, const TCHAR* aCellTypePtr, long aDimensionCount, void* aDimensionSizesI32s, void* aDimensionStridesI32s, long aPlaneCount, void* aMatrixDataPtr);

typedef void(__stdcall CObject_MainTask_Func)();
__declspec(dllexport) void __stdcall Object_MainTask_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_MainTask_Func* aFuncPtr);

__declspec(dllexport) void __stdcall Object_MainTask_Request(CMaxObjectVoidPtr aSCbClrObjectPtr);

typedef void(__stdcall CObject_Shutdown_Func)();
__declspec(dllexport) void __stdcall Object_Shutdown_Func_Set(CMaxObjectVoidPtr aSCbClrObjectPtr, CObject_Shutdown_Func* aFuncPtr);

typedef void* CPatcherVoidPtr;
__declspec(dllexport) CPatcherVoidPtr __stdcall Object_GetParentPatcherPtr(CMaxObjectVoidPtr aSCbClrObjectPtr);

typedef void* CPatcherObjectVoidPtr;
__declspec(dllexport) void* __stdcall Patcher_GetBoxPtr(CPatcherVoidPtr aPatcherPtr, const TCHAR* aNamePtr);

__declspec(dllexport) void* __stdcall Box_GetObjectPtr(void* aBoxPtr);

__declspec(dllexport) CPatcherObjectVoidPtr __stdcall Patcher_Add(CPatcherVoidPtr aPatcherPtr, 
                                                                  const TCHAR* aBoxTextPtr, 
                                                                  const TCHAR* aObjectNamePtr
                                                                  );

__declspec(dllexport) long __stdcall Patcher_GetContainsObject(CPatcherVoidPtr aPatcherPtr, const TCHAR* aObjectNamePtr);

typedef void* CPatBaseVoidPtr;
__declspec(dllexport) void __stdcall PatBase_Delete(CPatBaseVoidPtr aPatBasePtr);

typedef __int64 Int64;
__declspec(dllexport) Int64 __stdcall PatBase_ConnectTo(CPatBaseVoidPtr aFromPtr, long aOutletIdx, CPatBaseVoidPtr aToPtr, long aInletIdx);

__declspec(dllexport) const TCHAR* __stdcall Obj_GetClassName(void* aObjPtr);
