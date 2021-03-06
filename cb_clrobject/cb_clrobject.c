#include "ext.h"
#include "ext_obex.h"
#include "jit.common.h"
#include "CbMaxClrAdapterStaticDll.h"
#include "DllExports.h"
//#include "jit.matrix.util.h"

typedef struct _SCbClrObject
{
   // Always the first member of the struct. (Otherwise memoryaccessviolation!)
   t_object mObject;

   SClrObject_New mClrObjectNewArgs;
   CClrObjectVoidPtr mClrObjectPtr;
   t_symbol* mNamePtr;
   void* mQelemPtr;

   CObject_DeleteFunc* mObjectDeleteFuncPtr;
   CObject_In_Bang_Func* mInBangFuncPtr;
   CObject_In_Float_Func* mInFloatFuncPtr;
   CObject_In_Int_Func* mInIntFuncPtr;
   CObject_In_Symbol_Func* mInSymbolFuncPtr;
   CMemory_Delete_Func* mMemoryDeleteFuncPtr;
   CGetAssistStringFunc* mGetAssistStringFuncPtr;
   CObject_In_Receive* mObjectInReceiveFuncPtr;
   CObject_In_List_Clear* mObjectInListClearFuncPtr;
   CObject_In_List_Add_Int* mObjectInListAddIntFuncPtr;
   CObject_In_List_Add_Float* mObjectInListAddFloatFuncPtr;
   CObject_In_List_Add_Symbol* mObjectInListAddSymbolFuncPtr;	
   CObject_Out_List_Symbol_Get_Func* mObjectOutListSymbolGetFuncPtr;
   CObject_Out_List_Element_Count_Get_Func* mObjectOutListElementCountGetFuncPtr;
   CObject_Out_List_Element_Type_Get_Func* mObjectOutListElementTypeGetFuncPtr;
   CObject_Out_List_Element_Float_Get_Func* mObjectOutListElementFloatGetFuncPtr;
   CObject_Out_List_Element_Int_Get_Func* mObjectOutListElementIntGetFuncPtr;
   CObject_Out_List_Element_Symbol_Get_Func* mObjectOutListElementSymbolGetFuncPtr;
   CObject_In_Matrix_Receive_Func* mObjectInMatrixReceiveFuncPtr;
   CObject_MainTask_Func* mObjectMainTaskFuncPtr;
   CObject_Shutdown_Func* mObjectShutdownFuncPtr;

   long mInletIdx;


} SCbClrObject;

static t_class* gCbClrObjectClassPtr = 0;

#define StringHack(x) (x)
//char* StringHack(char* aSource)
//{ // TODO
//   return aSource;
//   int aLen = strlen(aSource);
//   char* aCopy = malloc(sizeof(char) * aLen);
//   strcpy(aCopy, aSource);
//   return aCopy;
//}

void __stdcall Object_Delete_Func_Set(SCbClrObject* aSCbClrObjectPtr, CObject_DeleteFunc* aFuncPtr)
{
   aSCbClrObjectPtr->mObjectDeleteFuncPtr = aFuncPtr;
}

void* __stdcall Object_In_Add(SCbClrObject* aObjectPtr, int aType, int aIdx)
{
   if(aIdx >= 0)
   {
      void* aProxyPtr = proxy_new(&aObjectPtr->mObject, aIdx, &aObjectPtr->mInletIdx);
      return aProxyPtr;
   }
   return 0;
}

void __stdcall In_Delete(void* aProxy)
{
   proxy_delete(aProxy);
}

void* __stdcall Object_Out_Add(void* aObjectPtr, int aType, int aPos)
{
   return outlet_new(aObjectPtr, NULL);
}

void __stdcall Out_Delete(void* aOutletPtr)
{
   outlet_delete(aOutletPtr);
}

void cb_clrobject_qelem_callback(SCbClrObject* aObjectPtr)
{
   //Max_Log_Write(aObjectPtr, "cb_clrobject_qelem_callback", 0);
   if(aObjectPtr
   && aObjectPtr->mObjectMainTaskFuncPtr)
   {
      aObjectPtr->mObjectMainTaskFuncPtr();
   }
   if (aObjectPtr
      && aObjectPtr->mQelemPtr)
   {
      qelem_unset(aObjectPtr->mQelemPtr);
   }
}

const char* GetText(long argc, t_atom* argv, long idx)
{
   if (idx < argc)
   {
      t_symbol* aSymbolPtr = atom_getsym(&argv[idx]);
      const char* aTextPtr = aSymbolPtr ? aSymbolPtr->s_name : 0;
      return aTextPtr;
   }
   return 0;
}

void* cb_clrobject_new(t_symbol* s, long argc, t_atom* argv)
{
   SCbClrObject* aObjectPtr = 0;

   aObjectPtr = (SCbClrObject*)object_alloc(gCbClrObjectClassPtr);

   if (aObjectPtr)
   {
      aObjectPtr->mClrObjectNewArgs.mObjectPtr = aObjectPtr;
      aObjectPtr->mClrObjectNewArgs.mAssemblyName = GetText(argc, argv, 0);
      aObjectPtr->mClrObjectNewArgs.mTypeName = GetText(argc, argv, 1);
      aObjectPtr->mClrObjectPtr = ClrObject_New(&aObjectPtr->mClrObjectNewArgs);
      aObjectPtr->mQelemPtr = qelem_new(aObjectPtr, cb_clrobject_qelem_callback);
   }

   if (aObjectPtr) 
   {
      aObjectPtr->mNamePtr = gensym("");

      if (argc && argv)
      {
         aObjectPtr->mNamePtr = atom_getsym(argv);
      }

      if(!aObjectPtr->mNamePtr
      || aObjectPtr->mNamePtr == gensym(""))
      {
         aObjectPtr->mNamePtr = symbol_unique();
      }		

      if (aObjectPtr->mClrObjectPtr)
      {
         ClrObject_Init(aObjectPtr->mClrObjectPtr);
      }
   }
   return aObjectPtr;
}

void cb_clrobject_free(SCbClrObject* aObjectPtr)
{
   if (aObjectPtr
   && aObjectPtr->mObjectShutdownFuncPtr)
   {
      aObjectPtr->mObjectShutdownFuncPtr();
   }

   if(aObjectPtr
   && aObjectPtr->mObjectDeleteFuncPtr)
   {
      // TODO: Max Crashes on delete proxy when >0 messages where received.
      // aObjectPtr->mObjectDeleteFuncPtr();
   }
   if (aObjectPtr)
   {
      qelem_free(aObjectPtr->mQelemPtr);
      ClrObject_Free(aObjectPtr->mClrObjectPtr);
   }	
}

void cb_clrobject_in_anything(SCbClrObject* aObjectPtr, t_symbol* s, long ac, t_atom* av)
{
   int aInletIdx = proxy_getinlet(&aObjectPtr->mObject);
   if (aInletIdx >= 0
      && aObjectPtr->mObjectInListClearFuncPtr)
   {
      if (aObjectPtr->mObjectInListClearFuncPtr)
      {
         aObjectPtr->mObjectInListClearFuncPtr(aInletIdx);
      }
      if (aObjectPtr->mObjectInListAddSymbolFuncPtr)
      {
         aObjectPtr->mObjectInListAddSymbolFuncPtr(aInletIdx, s->s_name);
      }
      int aListOk = 1;
      for (int aIdx = 0; aIdx < ac; ++aIdx)
      {
         int aOk = 0;
         t_atom* aAtomPtr = &av[aIdx];
         switch (aAtomPtr->a_type)
         {
         case A_FLOAT:
            if (aObjectPtr->mObjectInListAddFloatFuncPtr)
            {
               aObjectPtr->mObjectInListAddFloatFuncPtr(aInletIdx, atom_getfloat(aAtomPtr));
               aOk = 1;
            }
            else
            {
               aOk = 0;
            }
            break;

         case A_LONG:
            if (aObjectPtr->mObjectInListAddIntFuncPtr)
            {
               aObjectPtr->mObjectInListAddIntFuncPtr(aInletIdx, atom_getlong(aAtomPtr));
               aOk = 1;
            }
            else
            {
               aOk = 0;
            }
            break;

         case A_SYM:
            if (aObjectPtr->mObjectInListAddSymbolFuncPtr)
            {
               t_symbol* aSymbolPtr = atom_getsym(aAtomPtr);
               aObjectPtr->mObjectInListAddSymbolFuncPtr(aInletIdx, aSymbolPtr->s_name);
               aOk = 1;
            }
            else
            {
               aOk = 0;
            }
            break;

         default:
            aOk = 0;
         }
         if (aOk == 0)
         {
            Max_Log_Write(aObjectPtr, "cb_clrobject_in_anything: AtomType not supported.", 1);
            aListOk = 0; 
         }
      }
      if (aListOk)
      {
         if (aObjectPtr->mObjectInReceiveFuncPtr)
         {
            int aDataType = 3; // List.
            aObjectPtr->mObjectInReceiveFuncPtr(aInletIdx, aDataType);
         }
      }
      else
      {
         if (aObjectPtr->mObjectInListClearFuncPtr)
         {
            aObjectPtr->mObjectInListClearFuncPtr(aInletIdx);
         }
      }
   }
}

void cb_clrobject_in_bang(SCbClrObject* aObjectPtr)
{
   if (aObjectPtr)
   {
      int aInletNr = proxy_getinlet(&aObjectPtr->mObject);
      if (aObjectPtr->mInBangFuncPtr)
      {
         aObjectPtr->mInBangFuncPtr(aInletNr - 1);
      }
   }
}

void cb_clrobject_in_symbol(SCbClrObject* aObjectPtr, t_symbol* aSymbolPtr)
{
   if (aObjectPtr)
   {
      int aInletNr = proxy_getinlet(&aObjectPtr->mObject);
      if (aObjectPtr->mInSymbolFuncPtr
      && aSymbolPtr)
      {
         aObjectPtr->mInSymbolFuncPtr(aInletNr - 1, StringHack(aSymbolPtr->s_name));
      }
   }
}

void cb_clrobject_in_float(SCbClrObject* aObjectPtr, double f)
{
   if (aObjectPtr)
   {
      int aInletNr = proxy_getinlet(&aObjectPtr->mObject);
      if (aObjectPtr->mInFloatFuncPtr)
      {
         aObjectPtr->mInFloatFuncPtr(aInletNr - 1, f);
      }
   }
}

void cb_clrobject_in_int(SCbClrObject* aObjectPtr, long n)
{
   if (aObjectPtr)
   {
      long aInletNrI64 = proxy_getinlet(&aObjectPtr->mObject);
      if (aObjectPtr->mInIntFuncPtr)
      {
         long aIndex0BaseI64 = aInletNrI64;
         aObjectPtr->mInIntFuncPtr((long*)&aIndex0BaseI64, (long*)&n);
      }
   }
}

void __stdcall Object_In_Bang_Func_Set(SCbClrObject* aObjectPtr, CObject_In_Bang_Func* aInBangFuncPtr)
{
   if(aObjectPtr)
   {
      aObjectPtr->mInBangFuncPtr = aInBangFuncPtr;
   }	
}

void __stdcall Object_In_Float_Func_Set(SCbClrObject* aObjectPtr, CObject_In_Float_Func* aInFloatFuncPtr)
{
   if(aObjectPtr)
   {
      aObjectPtr->mInFloatFuncPtr = aInFloatFuncPtr;
   }
}

void __stdcall Object_In_Int_Func_Set(SCbClrObject* aObjectPtr, CObject_In_Int_Func* aInIntFuncPtr)
{
   if (aObjectPtr)
   {
      aObjectPtr->mInIntFuncPtr = aInIntFuncPtr;
   }
}

void __stdcall Object_In_Symbol_Func_Set(SCbClrObject* aObjectPtr, CObject_In_Symbol_Func* aFuncPtr)
{
   if (aObjectPtr)
   {
      aObjectPtr->mInSymbolFuncPtr = aFuncPtr;
   }
}

void __stdcall Object_Out_Bang_Send(SCbClrObject* aObjectPtr, void* aOutletPtr)
{
   if(aOutletPtr)
   {
      outlet_bang(aOutletPtr);
   }
}


void __stdcall Object_Out_Float_Send(SCbClrObject* aObjectPtr, void* aOutletPtr, double aValue)
{
   if (aOutletPtr)
   {
      outlet_float(aOutletPtr, aValue);
   }
}

void __stdcall Object_Out_Int_Send(SCbClrObject* aObjectPtr, void* aOutletPtr, long aValue)
{
   if (aOutletPtr)
   {
      outlet_int(aOutletPtr, aValue);
   }
}

void __stdcall Object_Out_Symbol_Send(SCbClrObject* aSCbClrObjectPtr, void* aOutletPtr, const TCHAR* aSymbolName)
{
   if (aOutletPtr
   && aSymbolName)
   {
      t_symbol* aSymbolPtr = gensym(StringHack(aSymbolName));
      outlet_anything(aOutletPtr, aSymbolPtr, 0, 0);
   }
}

void __stdcall Memory_Delete_Func_Set(SCbClrObject* aObjectPtr, CMemory_Delete_Func* aMemoryDeleteFuncPtr)
{
   if (aObjectPtr)
   {
      aObjectPtr->mMemoryDeleteFuncPtr = aMemoryDeleteFuncPtr;
   }
}

void __stdcall Object_Assist_GetString_Func_Set(SCbClrObject* aObjectPtr, CGetAssistStringFunc* aGetAssistStringFuncPtr)
{
   aObjectPtr->mGetAssistStringFuncPtr = aGetAssistStringFuncPtr;
}

void cb_clrobject_assist(SCbClrObject* aObjectPtr, void* b, long aStringProvider, long aIndex, char* aStringPtr)
{
   const TCHAR* aSourceStringPtr = aObjectPtr && aObjectPtr->mGetAssistStringFuncPtr && aObjectPtr->mMemoryDeleteFuncPtr
                          ? aObjectPtr->mGetAssistStringFuncPtr(aObjectPtr, aStringProvider, aIndex)
                                : 0
                          ;
   if(aSourceStringPtr)
   {
      sprintf(aStringPtr, aSourceStringPtr);
      if (aObjectPtr->mMemoryDeleteFuncPtr)
      {
         aObjectPtr->mMemoryDeleteFuncPtr(aSourceStringPtr);
      }
   }
}

void __stdcall Max_Log_Write(SCbClrObject* aObjectPtr, const TCHAR* aMessagePtr, int aError)
{
   if (aError)
   {
      if (aObjectPtr)
      {
         object_error(&aObjectPtr->mObject, aMessagePtr ? StringHack(aMessagePtr) : "");
      }
      else
      {
         error(aMessagePtr ? StringHack(aMessagePtr) : "");
      }	
   }
   else
   {
      if (aObjectPtr)
      {
         object_post(&aObjectPtr->mObject, aMessagePtr ? StringHack(aMessagePtr) : "");
      }
      else
      {
         post(aMessagePtr ? StringHack(aMessagePtr) : "");
      }
   }
}

void __stdcall Object_In_Receive_Func_Set(SCbClrObject* aSCbClrObjectPtr, CObject_In_Receive* aFuncPtr)
{
   if (aSCbClrObjectPtr)
   {
      aSCbClrObjectPtr->mObjectInReceiveFuncPtr = aFuncPtr;
   }
}

void __stdcall Object_In_List_Clear_Func_Set(SCbClrObject* aSCbClrObjectPtr, CObject_In_List_Clear* aFuncPtr)
{
   if (aSCbClrObjectPtr)
   {
      aSCbClrObjectPtr->mObjectInListClearFuncPtr = aFuncPtr;
   }
}

void __stdcall Object_In_List_Add_Int_Func_Set(SCbClrObject* aSCbClrObjectPtr, CObject_In_List_Add_Int* aFuncPtr)
{
   if (aSCbClrObjectPtr)
   {
      aSCbClrObjectPtr->mObjectInListAddIntFuncPtr = aFuncPtr;
   }
}

void __stdcall Object_In_List_Add_Float_Func_Set(SCbClrObject* aSCbClrObjectPtr, CObject_In_List_Add_Float* aFuncPtr)
{
   if (aSCbClrObjectPtr)
   {
      aSCbClrObjectPtr->mObjectInListAddFloatFuncPtr = aFuncPtr;
   }
}

void __stdcall Object_In_List_Add_Symbol_Func_Set(SCbClrObject* aSCbClrObjectPtr, CObject_In_List_Add_Symbol* aFuncPtr)
{
   if (aSCbClrObjectPtr)
   {
      aSCbClrObjectPtr->mObjectInListAddSymbolFuncPtr = aFuncPtr;
   }
} 

void __stdcall Object_Out_List_Send(SCbClrObject* aSCbClrObjectPtr, void* aOutletPtr, long aOutletIdx)
{
   if (aSCbClrObjectPtr
   && aSCbClrObjectPtr->mObjectOutListSymbolGetFuncPtr
   && aSCbClrObjectPtr->mObjectOutListElementCountGetFuncPtr
   && aSCbClrObjectPtr->mObjectOutListElementTypeGetFuncPtr
   && aSCbClrObjectPtr->mMemoryDeleteFuncPtr
   && aSCbClrObjectPtr->mObjectOutListElementFloatGetFuncPtr
   && aSCbClrObjectPtr->mObjectOutListElementIntGetFuncPtr
   && aSCbClrObjectPtr->mObjectOutListElementSymbolGetFuncPtr
      )
   {
      const TCHAR* aSymbolNamePtr = aSCbClrObjectPtr->mObjectOutListSymbolGetFuncPtr(aOutletIdx);
      long aCount = aSCbClrObjectPtr->mObjectOutListElementCountGetFuncPtr(aOutletIdx);
      long aCountAllocated = 0;
      t_atom* aAtomsPtr = 0;
      char aAllocated = 0;
      if(aCount != 0)
         atom_alloc_array(aCount, &aCountAllocated, &aAtomsPtr, &aAllocated);
      if (aAllocated
      || aCount == 0)
      {
         if (aCountAllocated >= aCount)
         {
            char aListOk = 1;
            for (long aElementIdx = 0; aElementIdx < aCount && aListOk; ++aElementIdx)
            {
               t_atom* aAtomPtr = &aAtomsPtr[aElementIdx];
               long aType = aSCbClrObjectPtr->mObjectOutListElementTypeGetFuncPtr(aOutletIdx, aElementIdx);
               switch (aType)
               {
               case 1: // Float
                  atom_setfloat(aAtomPtr, aSCbClrObjectPtr->mObjectOutListElementFloatGetFuncPtr(aOutletIdx, aElementIdx));
                  break;

               case 2: // Int
                  atom_setlong(aAtomPtr, aSCbClrObjectPtr->mObjectOutListElementIntGetFuncPtr(aOutletIdx, aElementIdx));
                  break;

               case 4: // Symbol
                  {
                     const TCHAR* aElementSymbolNamePtr = aSCbClrObjectPtr->mObjectOutListElementSymbolGetFuncPtr(aOutletIdx, aElementIdx);
                     atom_setsym(aAtomPtr, gensym(aElementSymbolNamePtr));
                     aSCbClrObjectPtr->mMemoryDeleteFuncPtr(aElementSymbolNamePtr);
                  }				
                  break;

               default:
                  aListOk = 0;
                  Max_Log_Write(aSCbClrObjectPtr, "InternalError: AtomTypeNotSupported.", 1);
               }
            }
            if (aListOk)
            {
               t_symbol* aSymbolPtr = gensym(aSymbolNamePtr);
               outlet_anything(aOutletPtr, aSymbolPtr, aCount, aAtomsPtr);
            }
            else
            {
               sysmem_freeptr(aAtomsPtr);
            }
         }
      }
      if (aSymbolNamePtr)
      {
         aSCbClrObjectPtr->mMemoryDeleteFuncPtr(aSymbolNamePtr);
      }
   }
}

void __stdcall Object_Out_List_Symbol_Get_Func_Set(SCbClrObject* aSCbClrObjectPtr, CObject_Out_List_Symbol_Get_Func* aFuncPtr)
{
   if (aSCbClrObjectPtr)
   {
      aSCbClrObjectPtr->mObjectOutListSymbolGetFuncPtr = aFuncPtr;
   }
}

void __stdcall Object_Out_List_Element_Count_Get_Func_Set(SCbClrObject* aSCbClrObjectPtr, CObject_Out_List_Element_Count_Get_Func* aFuncPtr)
{
   if (aSCbClrObjectPtr)
   {
      aSCbClrObjectPtr->mObjectOutListElementCountGetFuncPtr = aFuncPtr;
   }
}

void __stdcall Object_Out_List_Element_Type_Get_Func_Set(SCbClrObject* aSCbClrObjectPtr, CObject_Out_List_Element_Count_Get_Func* aFuncPtr)
{
   if (aSCbClrObjectPtr)
   {
      aSCbClrObjectPtr->mObjectOutListElementTypeGetFuncPtr = aFuncPtr;
   }
}

void __stdcall Object_Out_List_Element_Float_Get_Func_Set(SCbClrObject* aSCbClrObjectPtr, CObject_Out_List_Element_Float_Get_Func* aFuncPtr)
{
   if (aSCbClrObjectPtr)
   {
      aSCbClrObjectPtr->mObjectOutListElementFloatGetFuncPtr = aFuncPtr;
   }
}

void __stdcall Object_Out_List_Element_Int_Get_Func_Set(SCbClrObject* aSCbClrObjectPtr, CObject_Out_List_Element_Int_Get_Func* aFuncPtr)
{
   if (aSCbClrObjectPtr)
   {
      aSCbClrObjectPtr->mObjectOutListElementIntGetFuncPtr = aFuncPtr;
   }
}

void __stdcall Object_Out_List_Element_Symbol_Get_Func_Set(SCbClrObject* aSCbClrObjectPtr, CObject_Out_List_Element_Symbol_Get_Func* aFuncPtr)
{
   if (aSCbClrObjectPtr)
   {
      aSCbClrObjectPtr->mObjectOutListElementSymbolGetFuncPtr = aFuncPtr;
   }
}

void __stdcall Object_In_Matrix_Receive_Func_Set(SCbClrObject* aSCbClrObjectPtr, CObject_In_Matrix_Receive_Func* aFuncPtr)
{
   if (aSCbClrObjectPtr)
   {
      aSCbClrObjectPtr->mObjectInMatrixReceiveFuncPtr = aFuncPtr;
   }
}

void __stdcall Object_Out_Matrix_Send(SCbClrObject* aSCbClrObjectPtr, 
                                      void* aOutletPtr, 
                                      long aSize, 
                                      const TCHAR* aCellTypePtrArg,
                                      long aDimensionCount, 
                                      long* aDimensionSizesI32s, 
                                      long* aDimensionStridesI32s, 
                                      long aPlaneCount, 
                                      void* aDataPtr)
{
   const TCHAR* aCellTypePtr = StringHack(aCellTypePtrArg);
   if(aSCbClrObjectPtr)
   {
      void* aMatrixPtr = jit_object_new(gensym("jit_matrix"));
      if(aMatrixPtr)
      {	
         t_jit_matrix_info aMatrixInfo;
         memset(&aMatrixInfo, 0, sizeof(aMatrixInfo));
         aMatrixInfo.size = aSize;
         aMatrixInfo.type = gensym(aCellTypePtr); 
         aMatrixInfo.dimcount = aDimensionCount;
         for (int aIdx = 0; aIdx < aDimensionCount; ++aIdx)
            aMatrixInfo.dim[aIdx] = aDimensionSizesI32s[aIdx];
         for (int aIdx = 0; aIdx < aDimensionCount; ++aIdx)
            aMatrixInfo.dimstride[aIdx] = aDimensionStridesI32s[aIdx];
         aMatrixInfo.planecount = aPlaneCount;
         
         jit_object_method(aMatrixPtr, _jit_sym_setinfo, &aMatrixInfo);
         void* aMatrixDataPtr = 0; 
         jit_object_method(aMatrixPtr, _jit_sym_getdata, &aMatrixDataPtr);
         if (aMatrixDataPtr)
            memcpy(aMatrixDataPtr, aDataPtr, aSize);
         t_symbol* aMatrixObjectSymbolPtr = jit_symbol_unique();
         jit_object_method(aMatrixPtr, _jit_sym_register, aMatrixObjectSymbolPtr);
         t_atom* aAtomPtr = 0;
         char aAllocated = 0;
         long aCount = 0;
         atom_alloc(&aCount, &aAtomPtr, &aAllocated);
         if (aAllocated == 1)
         {
            assert(aCount == 1);
            atom_setsym(aAtomPtr, aMatrixObjectSymbolPtr);
            outlet_anything(aOutletPtr, _jit_sym_jit_matrix, 1, aAtomPtr);
            sysmem_freeptr(aAtomPtr);
         }

         if (aMatrixPtr)
         {
            object_unregister(aMatrixPtr);
            jit_object_free(aMatrixPtr);
         }            
      }	
   }
}

void __stdcall Object_In_Matrix_Receive(SCbClrObject* aSCbClrObjectPtr, long aInletIdx, const TCHAR* aMatrixObjectNameArg)
{
   const TCHAR* aMatrixObjectName = StringHack(aMatrixObjectNameArg);
   if (aSCbClrObjectPtr)
   {
      t_symbol* aMatrixObjectSymbol = gensym(aMatrixObjectName);
      t_jit_object* aMatrixPtr = jit_object_findregistered(aMatrixObjectSymbol);
      t_jit_matrix_info aMatrixInfo;		
      {
         memset(&aMatrixInfo, 0, sizeof(aMatrixInfo));
         const TCHAR* aMethodName = "getinfo";
         t_symbol* aMethodSymbolPtr = gensym(aMethodName);
         method aMethod = jit_object_getmethod(aMatrixPtr, aMethodSymbolPtr);
         t_jit_err aErr = (t_jit_err)aMethod(aMatrixPtr, &aMatrixInfo);
      }		 
      void* aDataPtr = 0;
      {
         const TCHAR* aMethodName = "getdata";
         t_symbol* aMethodSymbolPtr = gensym(aMethodName);
         method aMethod = jit_object_getmethod(aMatrixPtr, aMethodSymbolPtr);
         t_jit_err aErr = (t_jit_err)aMethod(aMatrixPtr, &aDataPtr);
      }

      if (aSCbClrObjectPtr->mObjectInMatrixReceiveFuncPtr)
      {
         aSCbClrObjectPtr->mObjectInMatrixReceiveFuncPtr(aInletIdx, 
                                             aMatrixInfo.size,
                                             StringHack(aMatrixInfo.type->s_name),
                                             aMatrixInfo.dimcount,
                                             aMatrixInfo.dim,
                                             aMatrixInfo.dimstride,
                                             aMatrixInfo.planecount,
                                             aDataPtr
                                             );
      }
   }
}

void __stdcall Object_MainTask_Func_Set(SCbClrObject* aSCbClrObjectPtr, CObject_MainTask_Func* aFuncPtr)
{
   if (aSCbClrObjectPtr)
   {
      aSCbClrObjectPtr->mObjectMainTaskFuncPtr = aFuncPtr;
   }
}

void __stdcall Object_MainTask_Request(SCbClrObject* aSCbClrObjectPtr)
{
   if(aSCbClrObjectPtr
   && aSCbClrObjectPtr->mQelemPtr)
   {
      qelem_set(aSCbClrObjectPtr->mQelemPtr); 
   }
}

void __stdcall Object_Shutdown_Func_Set(SCbClrObject* aSCbClrObjectPtr, CObject_Shutdown_Func* aFuncPtr)
{
   if (aSCbClrObjectPtr)
   {
      aSCbClrObjectPtr->mObjectShutdownFuncPtr = aFuncPtr;
   }
}

CPatcherVoidPtr __stdcall Object_GetParentPatcherPtr(SCbClrObject* aSCbClrObjectPtr)
{
   t_object* aPatcherPtr;
   object_obex_lookup(aSCbClrObjectPtr, gensym("#P"), &aPatcherPtr);
   return aPatcherPtr;
}
 
void* __stdcall Patcher_GetBoxPtr(t_object* aPatcherPtr, const TCHAR* aNamePtr)
{
   t_symbol* aNameSymPtr = gensym(aNamePtr);
   t_object* aBoxPtr = 0;
   t_object* aObjectPtr = 0;
   t_symbol* aVarNameSymPtr = gensym("varname");

   for (aBoxPtr = jpatcher_get_firstobject(aPatcherPtr);
        aBoxPtr;
        aBoxPtr = jbox_get_nextobject(aBoxPtr))
   {
      aObjectPtr = jbox_get_object(aBoxPtr);
      t_symbol* aVarNameValue1SymPtr = aObjectPtr ? object_attr_getsym(aObjectPtr, aVarNameSymPtr) : 0;
      t_symbol* aVarNameValue2SymPtr = aBoxPtr ? object_attr_getsym(aBoxPtr, aVarNameSymPtr) : 0;
      if((aVarNameValue1SymPtr && aVarNameValue1SymPtr == aNameSymPtr)
      || (aVarNameValue2SymPtr && aVarNameValue2SymPtr == aNameSymPtr) )
      {
         return aBoxPtr;
      }
   }
   return 0;
}

void* __stdcall Box_GetObjectPtr(t_object* aBoxPtr)
{
   return jbox_get_object(aBoxPtr);
}

CPatcherObjectVoidPtr __stdcall Patcher_Add(CPatcherVoidPtr aPatcherPtr, 
                                            const TCHAR* aBoxTextPtr, 
                                            const TCHAR* aObjectNamePtr)
{
   t_object* aExistingPtr = 0;
   object_obex_lookup(aPatcherPtr, gensym(aObjectNamePtr), &aExistingPtr);
   if (0 == aExistingPtr)
   {
      // newobject_fromboxtext
      t_object* aObjectPtr = newobject_fromboxtext(aPatcherPtr, aBoxTextPtr);

      t_symbol* aClassNamePtr = object_classname(aObjectPtr);

      if (aObjectPtr)
      {
         t_symbol* aVarNameSymPtr = gensym("varname");
         t_symbol* aVarNameValSymPtr = gensym(aObjectNamePtr);
         t_max_err aSetNameErr = object_attr_setsym(aObjectPtr, aVarNameSymPtr, aVarNameValSymPtr);
         if (0 != aSetNameErr)
         {
            Max_Log_Write(0, "Error setting object varname.", 1);
            object_free(aObjectPtr);
            aObjectPtr = 0;
         }
         return aObjectPtr;
      }
      else
      {
         return 0;
      }
   }
   return 0;
}

long __stdcall Patcher_GetContainsObject(CPatcherVoidPtr aPatcherPtr, const TCHAR* aObjectNamePtr)
{
   t_object* aBoxPtr = Patcher_GetBoxPtr(aPatcherPtr, aObjectNamePtr);
   return aBoxPtr != 0;
}

void __stdcall PatBase_Delete(t_object* aPatBasePtr)
{
   object_free(aPatBasePtr);
}

Int64 __stdcall PatBase_ConnectTo(t_object* aFromPtr, long aOutletIdx, t_object* aToPtr, long aInletIdx)
{
   t_object* aPatcherPtr;
   object_obex_lookup(aFromPtr, gensym("#P"), &aPatcherPtr);
   if (aPatcherPtr)
   {
      t_atom aMsg[4];
      t_atom aResult;
      atom_setobj(&aMsg[0], aFromPtr);
      atom_setlong(&aMsg[1], aOutletIdx);
      atom_setobj(&aMsg[2], aToPtr);
      atom_setlong(&aMsg[3], aInletIdx);
      t_max_err aErr1 = object_method_typed(aPatcherPtr, gensym("connect"), 4, aMsg, &aResult);
      t_atom_long aErr2 = aErr1 == 0 ? atom_getlong(&aResult) : aErr1;
      return aErr2;
   }
   else
   {
      return -1;
   }
}

const TCHAR* __stdcall Obj_GetClassName(t_object* aObjPtr)
{
   t_symbol* aClassSymPtr = object_classname(aObjPtr);
   const TCHAR* aClassNamePtr = aClassSymPtr ? aClassSymPtr->s_name : "";
   return aClassNamePtr;
}

void ext_main(void* r)
{
   t_class* aClassPtr = 0;

   aClassPtr = class_new("cb_clrobject", (method)cb_clrobject_new, (method)cb_clrobject_free, (long)sizeof(SCbClrObject), 0L, A_GIMME, 0);

   class_addmethod(aClassPtr, (method)cb_clrobject_in_anything, "anything", A_GIMME, 0);
   class_addmethod(aClassPtr, (method)cb_clrobject_in_int, "int", A_LONG, 0); 
   class_addmethod(aClassPtr, (method)cb_clrobject_in_float, "float", A_FLOAT, 0);
   class_addmethod(aClassPtr, (method)cb_clrobject_in_bang, "bang", 0);
   class_addmethod(aClassPtr, (method)cb_clrobject_in_symbol, "symbol", A_SYM, 0); 

   class_register(CLASS_BOX, aClassPtr);
   gCbClrObjectClassPtr = aClassPtr;
}

