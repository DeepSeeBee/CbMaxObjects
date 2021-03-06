﻿using CbMaxClrAdapter.Jitter;
using CbMaxClrAdapter.Patcher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CbMaxClrAdapter
{

   /// <summary>
   /// Todo-This might become a blop...
   /// </summary>
   internal sealed class CMarshal
   {
      internal CMarshal(CMaxObject aMaxObject)
      {
         this.MaxObject = aMaxObject;
      }

      private readonly CMaxObject MaxObject;

      private List<object> Funcs = new List<object>();
      
      internal void Init()
      {
         var aArgs = this.MaxObject.NewArgs;
         var aObjectDeleteFunc = new DllImports.CObjectDeleteFunc(this.Object_Delete);
         var aObjectOnInBangFunc = new DllImports.CObject_In_Bang_Func(this.Object_In_Bang);
         var aObjectOnInFloatFunc = new DllImports.CObject_In_Float_Func(this.Object_In_Float);
         var aObjectOnInIntFunc = new DllImports.CObject_In_Int_Func(this.Object_In_Int);
         var aObjectOnInSymbol = new DllImports.CObject_In_Symbol_Func(this.Object_In_Symbol);
         var aMemoryDeleteFunc = new DllImports.CMemory_Delete_Func(this.Memory_Delete);
         var aObjectAssistStringGetFunc = new DllImports.CObject_Assist_GetString_Func(this.Object_Assist_String_Get);
         var aObjectInReceiveFunc = new DllImports.CObject_In_Receive_Func(this.Object_In_Receive);
         var aObjectInListClearFunc = new DllImports.CObject_In_List_ClearFunc(this.Object_In_List_Clear);
         var aObjectInListAddFloatFunc = new DllImports.CObject_In_List_Add_Float_Func(this.Object_In_List_AddFloat);
         var aObjectInListAddIntFunc = new DllImports.CObject_In_List_Add_Int_Func(this.Object_In_List_AddInt);
         var aObjectInListAddSymbolFunc = new DllImports.CObject_In_List_Add_Symbol_Func(this.Object_In_List_AddSymbol);
         var aObjectOutListSymbolGetFunc = new DllImports.CObject_Out_List_Symbol_Get_Func(this.Object_Out_List_Symbol_Get);
         var aObjectOutListElementCountFunc = new DllImports.CObject_Out_List_Element_Count_Get_Func(this.Object_Out_List_Element_Count_Get);
         var aObjectOutListElementTypeGetFunc = new DllImports.CObject_Out_List_Element_Type_Get_Func(this.Object_Out_List_Element_Type_Get);
         var aObjectOutListElementFloatGetFunc = new DllImports.CObject_Out_List_Element_Float_Get_Func(this.Object_Out_List_Element_Float_Get);
         var aObjectOutListElementIntGetFunc = new DllImports.CObject_Out_List_Element_Int_Get_Func(this.Object_Out_List_Element_Int_Get);
         var aObjectOutListElementSymbolGetFunc = new DllImports.CObject_Out_List_Element_Symbol_Get_Func(this.Object_Out_List_Element_Symbol_Get);
         var aObjectInMatrixReceiveFunc = new DllImports.CObject_In_Matrix_Receive_Func(this.Object_In_Matrix_Receive);
         var aObjectMainTask = new DllImports.CObject_MainTask_Func(this.Object_MainTask);
         var aObjectShutdownFunc = new DllImports.CObject_Shutdown_Func(this.Object_Shutdown);

         this.Funcs.Add(aObjectDeleteFunc);
         this.Funcs.Add(aObjectOnInBangFunc);
         this.Funcs.Add(aObjectOnInFloatFunc);
         this.Funcs.Add(aObjectOnInIntFunc);
         this.Funcs.Add(aObjectOnInSymbol);
         this.Funcs.Add(aMemoryDeleteFunc);
         this.Funcs.Add(aObjectAssistStringGetFunc);
         this.Funcs.Add(aObjectInReceiveFunc);
         this.Funcs.Add(aObjectInListClearFunc);
         this.Funcs.Add(aObjectInListAddFloatFunc);
         this.Funcs.Add(aObjectInListAddIntFunc);
         this.Funcs.Add(aObjectInListAddSymbolFunc);
         this.Funcs.Add(aObjectOutListSymbolGetFunc);
         this.Funcs.Add(aObjectOutListElementCountFunc);
         this.Funcs.Add(aObjectOutListElementTypeGetFunc);
         this.Funcs.Add(aObjectOutListElementFloatGetFunc);
         this.Funcs.Add(aObjectOutListElementIntGetFunc);
         this.Funcs.Add(aObjectOutListElementSymbolGetFunc);
         this.Funcs.Add(aObjectInMatrixReceiveFunc);
         this.Funcs.Add(aObjectMainTask);
         this.Funcs.Add(aObjectShutdownFunc);

         DllImports.Object_Delete_Func_Set(aArgs.mObjectPtr, aObjectDeleteFunc);
         DllImports.Object_In_Bang_Func_Set(aArgs.mObjectPtr, aObjectOnInBangFunc);
         DllImports.Object_In_Float_Func_Set(aArgs.mObjectPtr, aObjectOnInFloatFunc);
         DllImports.Object_In_Int_Func_Set(aArgs.mObjectPtr, aObjectOnInIntFunc);
         DllImports.Object_In_Symbol_Func_Set(aArgs.mObjectPtr, aObjectOnInSymbol);
         DllImports.Memory_Delete_Func_Set(aArgs.mObjectPtr, aMemoryDeleteFunc);
         DllImports.Object_Assist_GetString_Func_Set(aArgs.mObjectPtr, aObjectAssistStringGetFunc);
         DllImports.Object_In_Receive_Func_Set(aArgs.mObjectPtr, aObjectInReceiveFunc);
         DllImports.Object_In_List_Clear_Func_Set(aArgs.mObjectPtr, aObjectInListClearFunc);
         DllImports.Object_In_List_Add_Float_Func_Set(aArgs.mObjectPtr, aObjectInListAddFloatFunc);
         DllImports.Object_In_List_Add_Int_Func_Set(aArgs.mObjectPtr, aObjectInListAddIntFunc);
         DllImports.Object_In_List_Add_Symbol_Func_Set(aArgs.mObjectPtr, aObjectInListAddSymbolFunc);
         DllImports.Object_Out_List_Symbol_Get_Func_Set(aArgs.mObjectPtr, aObjectOutListSymbolGetFunc);
         DllImports.Object_Out_List_Element_Count_Get_Func_Set(aArgs.mObjectPtr, aObjectOutListElementCountFunc);
         DllImports.Object_Out_List_Element_Type_Get_Func_Set(aArgs.mObjectPtr, aObjectOutListElementTypeGetFunc);
         DllImports.Object_Out_List_Element_Float_Get_Func_Set(aArgs.mObjectPtr, aObjectOutListElementFloatGetFunc);
         DllImports.Object_Out_List_Element_Int_Get_Func_Set(aArgs.mObjectPtr,    aObjectOutListElementIntGetFunc);
         DllImports.Object_Out_List_Element_Symbol_Get_Func_Set(aArgs.mObjectPtr, aObjectOutListElementSymbolGetFunc); 
         DllImports.Object_In_Matrix_Receive_Func_Set(aArgs.mObjectPtr, aObjectInMatrixReceiveFunc);
         DllImports.Object_MainTask_Func_Set(aArgs.mObjectPtr, aObjectMainTask);
         DllImports.Object_Shutdown_Func_Set(aArgs.mObjectPtr, aObjectShutdownFunc);
      }
      private void Memory_Delete(IntPtr aHGlobalMem)
      {
         if (aHGlobalMem != IntPtr.Zero)
         { 
            Marshal.FreeHGlobal(aHGlobalMem); // TODO
         }
      }
      private IntPtr AllocExportString(string aString)
      {
         IntPtr aPtr = Marshal.StringToHGlobalAnsi(aString); 
         return aPtr;
      }
      private void Object_Delete()
      {
         foreach (var aInlet in this.MaxObject.Inlets)
         {
            aInlet.Delete();
         }
         foreach (var aOutlet in this.MaxObject.Outlets)
         {
            aOutlet.Delete();
         }
      }

      private void WithCatch(Action aAction)
      {
         this.WithCatch<object>(() => { aAction(); return default; });
      }

      private T WithCatch<T>(Func<T> aFunc)
      {
         return this.WithCatch(aFunc, () => default);
      }

      private T WithCatch<T>(Func<T> aFunc, Func<T> aExcValue)
      {
         try
         {
            var aResult = aFunc();
            //GC.Collect();
            return aResult;

         }
         catch (Exception aExc)
         {
            if (this.MaxObject is object)
            {
               this.MaxObject.WriteLogErrorMessage(aExc);
            }
            GC.Collect();
            if (aExcValue is object)
            {
               return aExcValue();
            }
            else
            {
               return default(T);
            }

         }
      }

      private void Object_In_Bang(int aInletIdx)
      {
         this.WithCatch(delegate () { this.MaxObject.Inlets[aInletIdx].Receive(CMessageTypeEnum.Bang); });
      }

      private void Object_In_Float(int aInletIdx, double aValue)
      {
         this.WithCatch(delegate ()
         {
            var aInlet = this.MaxObject.Inlets[aInletIdx];
            var aMessage = aInlet.GetMessage<CFloat>();
            aMessage.Set(aValue);
            aInlet.Receive(CMessageTypeEnum.Float);
         });
      }

      private void Object_In_Int(Int32 aInletIdx, Int32 aValue)
      {
         this.WithCatch(delegate ()
         {
            var aInlet = this.MaxObject.Inlets[aInletIdx];
            var aMessage = aInlet.GetMessage<CInt>();
            aMessage.Set(aValue);
            aInlet.Receive(CMessageTypeEnum.Int);
         });
      }

      private void Object_In_Symbol(Int32 aInletIdx, string aSymbolName)
      {
         var aInlet = this.MaxObject.Inlets[(int)aInletIdx];
         var aMessage = aInlet.GetMessage<CSymbol>();
         aMessage.Set(aSymbolName);
         aInlet.Receive(CMessageTypeEnum.Symbol);
      }

      private void Object_In_Receive(Int32 aInletIdx, Int32 aDataTypeI64)
      {
         this.WithCatch(delegate ()
         {
            var aDataTypeEnum = (CMessageTypeEnum)aDataTypeI64;
            this.MaxObject.Inlets[(int)aInletIdx].Receive((CMessageTypeEnum)aDataTypeEnum);
         });
      }

      private void Object_In_List_Clear(Int32 aInletIdx)
      {
         this.WithCatch(delegate ()
         {
            var aInletIdxI32 = Convert.ToInt32(aInletIdx);
            var aInlet = this.MaxObject.Inlets[aInletIdxI32];
            var aMessage = aInlet.GetMessage<CList>();
            aMessage.Value.Editable.ClearInternal();
         });
      }

      private IntPtr Alloc(int aSize) => Marshal.AllocHGlobal(aSize);



      internal void Send(COutlet aOutlet, CMatrix aMatrix)
      {
         var aMatrixData = aMatrix.Value;
         var aObjectPtr = this.MaxObject.NewArgs.mObjectPtr;
         var aIndex = aOutlet.Index;         
         var aSize = aMatrixData.ByteCount;
         var aDimensionCount = aMatrixData.DimensionCount;
         var aDimensionSizesPtr = this.Alloc(aDimensionCount * sizeof(Int32));
         var aCellType = aMatrixData.CellTypeEnum.ToString().ToLower();
         try
         {
            var aDimensionStridesPtr = this.Alloc(aDimensionCount * sizeof(Int32));
            try
            {
               var aDataPtr = this.Alloc(aSize);
               try
               {
                  var aDimensionSizes = aMatrixData.DimensionSizes;
                  for (var aIdx = 0; aIdx < aDimensionCount; ++aIdx)
                     Marshal.WriteInt32(aDimensionSizesPtr, aIdx * sizeof(Int32), aDimensionSizes[aIdx]);
                  var aDimensionStrides = aMatrixData.DimensionStrides;
                  for (var aIdx = 0; aIdx < aDimensionCount; ++aIdx)
                     Marshal.WriteInt32(aDimensionStridesPtr, aIdx * sizeof(Int32), aDimensionStrides[aIdx]);
                  var aPlaneCount = aMatrixData.PlaneCount;
                  var aBuffer = aMatrixData.Buffer;
                  Marshal.Copy(aBuffer, 0, aDataPtr, aSize);
                  DllImports.Object_Out_Matrix_Send(aObjectPtr, aOutlet.Ptr, aSize, aCellType, aDimensionCount, aDimensionSizesPtr, aDimensionStridesPtr, aPlaneCount, aDataPtr);                 
               }
               finally
               {
                   this.Memory_Delete(aDataPtr); 
               }
            }
            finally
            {
               this.Memory_Delete(aDimensionStridesPtr);
            }
         }
         finally
         {
            this.Memory_Delete(aDimensionSizesPtr);
         }
      }

      internal void Send(CMatrixOutlet aMatrixOutlet)=> this.Send(aMatrixOutlet, aMatrixOutlet.Message);
      private void Object_In_List_AddFloat(Int32 aInletIdx, double aFloat)
      {
         this.WithCatch(delegate ()
         {
            this.MaxObject.Inlets[(int)aInletIdx].GetMessage<CList>().Value.Editable.AddInternal(aFloat);
         });
      }

      internal IntPtr AddInlet(CInlet aInlet)
      {
         if (aInlet.Ptr == IntPtr.Zero)
         {
            return DllImports.Object_In_Add(this.MaxObject.NewArgs.mObjectPtr, 0, aInlet.Index);
         }
         else
         {
            throw new InvalidOperationException();
         }
      }

      private void Object_In_List_AddInt(Int32 aInletIdx, Int32 aInt)
      {
         this.WithCatch(delegate ()
         {
            this.MaxObject.Inlets[(int)aInletIdx].GetMessage<CList>().Value.Editable.AddInternal(aInt);
         });
      }
      private void Object_In_List_AddSymbol(Int32 aInletIdx, IntPtr aSymbol)
      {
         this.WithCatch(delegate ()
         {
            this.MaxObject.Inlets[(int)aInletIdx].GetMessage<CList>().Value.Editable.AddInternal(Marshal.PtrToStringAnsi(aSymbol));
         });
      }

      private IntPtr Object_Assist_String_Get(IntPtr aObjectPtr, int aStringProvider, int aIndex)
      {
         return this.WithCatch(() =>
         {
            switch (aStringProvider)
            {
               case 1: // Inlet
                  if (aIndex >= 0
                  && aIndex < this.MaxObject.Inlets.Count)
                     return this.AllocExportString(this.MaxObject.Inlets[aIndex].Description);
                  else
                     return IntPtr.Zero;

               case 2: // outlet
                  if (aIndex >= 0
                  && aIndex < this.MaxObject.Outlets.Count)
                     return this.AllocExportString(this.MaxObject.Outlets[aIndex].Description);
                  else
                     return IntPtr.Zero;

               default:
                  return IntPtr.Zero;
            }
         });
      }

      internal void Delete(CInlet aInlet) => DllImports.In_Delete(aInlet.Ptr);
      private IntPtr Object_Out_List_Symbol_Get(Int32 aOutletIdx)
      {
         return this.WithCatch(() =>
         {
            var aListData = this.MaxObject.Outlets[(int)aOutletIdx].GetMessage<CList>().Value;
            var aSymbol = aListData.Symbol;
            var aSymbolPtr = this.AllocExportString(aSymbol);
            return aSymbolPtr;
         });
      }

      private Int32 Object_Out_List_Element_Count_Get(Int32 aOutletIdx)
      {
         return this.WithCatch(() =>
         {
            var aListData = this.MaxObject.Outlets[(int)aOutletIdx].GetMessage<CList>().Value;
            var aElements = aListData.WithoutSymbol;
            var aCount = aElements.Count();
            return aCount;
         });
      }

      private Int32 Object_Out_List_Element_Type_Get(Int32 aOutletIdx, Int32 aElementIdx)
      {
         return this.WithCatch(() =>
         {
            var aListData = this.MaxObject.Outlets[(int)aOutletIdx].GetMessage<CList>().Value;
            var aElements = aListData.WithoutSymbol;
            var aElement = aElements.ElementAt((int)aElementIdx);
            var aElementType = GetDataType(aElement);
            return Convert.ToInt32(aElementType);
         },
         () => Convert.ToInt32(CMessageTypeEnum.Null)
         );
      }

      private CMessageTypeEnum GetDataType(object aElement)
      {
         if (!(aElement is object))
            return CMessageTypeEnum.Null;
         else if (aElement is string)
            return CMessageTypeEnum.Symbol;
         else if (aElement is double)
            return CMessageTypeEnum.Float;
         else if (aElement is Int32)
            return CMessageTypeEnum.Int;
         else
            this.MaxObject.WriteLogErrorMessage("Unknown ElementType: " + aElement.GetType().Name);
            return CMessageTypeEnum.Null;
      }

      private double Object_Out_List_Element_Float_Get(Int32 aOutletIdx, Int32 aElementIdx)
      {
         return this.WithCatch(() =>
         {
            var aListData = this.MaxObject.Outlets[(int)aOutletIdx].GetMessage<CList>().Value;
            var aElements = aListData.WithoutSymbol;
            var aElement = aElements.ElementAt((int)aElementIdx);
            var aFloat = Convert.ToDouble(aElement);
            return aFloat;
         });
      }

      private Int32 Object_Out_List_Element_Int_Get(Int32 aOutletIdx, Int32 aElementIdx)
      {
         return this.WithCatch(() =>
         {
            var aListData = this.MaxObject.Outlets[(int)aOutletIdx].GetMessage<CList>().Value;
            var aElements = aListData.WithoutSymbol;
            var aElement = aElements.ElementAt((int)aElementIdx);
            var aInt = Convert.ToInt32(aElement);
            return aInt;
         });
      }

      internal void Delete(COutlet aOutlet) => DllImports.Out_Delete(aOutlet.Ptr);

      private IntPtr Object_Out_List_Element_Symbol_Get(Int32 aOutletIdx, Int32 aElementIdx)
      {
         return this.WithCatch(() =>
         {
            var aListData = this.MaxObject.Outlets[(int)aOutletIdx].GetMessage<CList>().Value;
            var aElements = aListData.WithoutSymbol;
            var aElement = aElements.ElementAt((int)aElementIdx);
            var aSymbol = aElement.ToString();
            var aSymbolPtr = this.AllocExportString(aSymbol);
            return aSymbolPtr;
         });
      }

      private int[] GetI32s(IntPtr aPtr, int aSize)
      {
         var aIntArray = new int[aSize];
         for (var aIdx = 0; aIdx < aSize; ++aIdx)
         {
            aIntArray[aIdx] = (int)(Marshal.ReadInt32(aPtr, aIdx * 4));
         }
         return aIntArray;
      }

      internal void Send(COutlet aOutlet, CBang aBang) => DllImports.Object_Out_Bang_Send(aOutlet.MaxObject.Ptr, aOutlet.Ptr);

      internal void Send(CBangOutlet aBangOutlet) => this.Send(aBangOutlet, aBangOutlet.Message);

      private Int32 Object_In_Matrix_Receive(Int32 aInletIdx, Int32 aSize, string aCellType, Int32 aDimensionCount, IntPtr aDimensionSizesI64Ptr, IntPtr aDimensionStridesI64Ptr, Int32 aPlaneCount, IntPtr aMatrixDataU8Ptr)
      {
         return this.WithCatch(() =>
         {
            var aInlet = this.MaxObject.Inlets[(int)aInletIdx];
            var aMatrix = aInlet.GetMessage<CMatrix>();
            var aMatrixData = aMatrix.Value;
            var aDimensionSizes = this.GetI32s(aDimensionSizesI64Ptr, (int)aDimensionCount);
            var aDimensionStrides = this.GetI32s(aDimensionStridesI64Ptr, (int)aDimensionCount);
            var aCellTypeEnum = (CMatrixData.CCellTypeEnum)Enum.Parse(typeof(CMatrixData.CCellTypeEnum), aCellType, true);
            aMatrixData.ReallocateInternal((int)aSize,
                                           aCellTypeEnum,
                                           (int)aDimensionCount,
                                           aDimensionSizes,
                                           aDimensionStrides,
                                           (int)aPlaneCount
                                           );
            Marshal.Copy(aMatrixDataU8Ptr, aMatrixData.Buffer, (int)0, (int)aSize);
            aInlet.Receive(CMessageTypeEnum.Matrix);
            return 0;
         },
         () => -1
         );
      }

      internal void Send(COutlet aOutlet, CSymbol aSymbol) => DllImports.Object_Out_Symbol_Send(aOutlet.MaxObject.Ptr, aOutlet.Ptr, aSymbol.Value);
      internal void Send(CSymbolOutlet aSymbolOutlet) => this.Send(aSymbolOutlet, aSymbolOutlet.Message);
      internal void Send(COutlet aOutlet, CFloat aFloat) => DllImports.Object_Out_Float_Send(this.MaxObject.Ptr, aOutlet.Ptr, aFloat.Value);
      internal void Send(CFloatOutlet aFloatOutlet) => this.Send(aFloatOutlet, aFloatOutlet.Message);
      internal void Send(COutlet aOutlet, CInt aInt) => DllImports.Object_Out_Int_Send(this.MaxObject.Ptr, aOutlet.Ptr, aInt.Value);
      internal void Send(CIntOutlet aIntOutlet) => this.Send(aIntOutlet, aIntOutlet.Message);
      internal void Object_In_Matrix_Receive(Int32 aInletIdx, string aObjectName) => DllImports.Object_In_Matrix_Receive(this.MaxObject.NewArgs.mObjectPtr, aInletIdx, aObjectName);
      internal void Send(COutlet aOutlet, CList aList) => DllImports.Object_Out_List_Send(aOutlet.MaxObject.Ptr, aOutlet.Ptr, aOutlet.Index);
      internal void Send(CSingleTypeOutlet<CList> aListOutlet) => this.Send(aListOutlet, aListOutlet.Message);
      internal IntPtr AddOutlet(COutlet aOutlet, CMessageTypeEnum aDataTypeEnum) => DllImports.Object_Out_Add(this.MaxObject.NewArgs.mObjectPtr, (int)aDataTypeEnum, aOutlet.Index);
      internal void Max_Log_Write(string aMsg, bool aIsError) => DllImports.Max_Log_Write(this.MaxObject.NewArgs.mObjectPtr, aMsg, aIsError ? 1 : 0);
      internal void Object_MainTask_Request() => DllImports.Object_MainTask_Request(this.MaxObject.NewArgs.mObjectPtr);
      private void Object_MainTask() => this.WithCatch(delegate () { this.MaxObject.OnMainTask(); });
      private void Object_Shutdown() => this.WithCatch(this.MaxObject.Shutdown);
      private void CheckPtr(IntPtr aPtr, Func<Exception> aNewExc)
      {
         if(aPtr == IntPtr.Zero)
         {
            throw aNewExc();
         }
      }
      internal IntPtr Object_GetParentPatcherPtr(CMaxObject aMaxObject)
      {
         var aPtr = DllImports.Object_GetParentPatcherPtr(this.MaxObject.Ptr);
         this.CheckPtr(aPtr, () => new Exception("Object_GetParentPatcherPtr failed."));
         return aPtr;
      }

      internal IntPtr Patcher_GetBoxPtr(CPatPatcher aPatcher, string aName)
      {
         var aPtr = DllImports.Patcher_GetBoxPtr(aPatcher.Ptr, aName);
         this.CheckPtr(aPtr, () => new Exception("Patcher_GetObjectPtr failed for Name='" + aName + "'."));
         return aPtr;
      }

  //    internal IntPtr Box_GetObjectPtr()
//
      internal IntPtr Patcher_Add(CPatPatcher aPatcher, string aBoxText, string aObjectName)
      {
         var aPtr = DllImports.Patcher_Add(aPatcher.Ptr, aBoxText, aObjectName);
         this.CheckPtr(aPtr, () => new Exception("Patcher_Add failed for BoxText='" + aBoxText + "' ObjectName='" + aObjectName + "'."));
         return aPtr;
      }

      internal bool Patcher_GetContainsObject(CPatPatcher aPatcher, string aObjectName) => DllImports.Patcher_GetContainsObject(aPatcher.Ptr, aObjectName) != 0;

      internal void PatBase_Delete(CPatBase aObjectPtr) => DllImports.PatBase_Delete(aObjectPtr.Ptr);

      private void CheckNoError(Int64 aErrorCode)
      {
         if(0 != aErrorCode)
         {
            throw new Exception("Error #" + aErrorCode.ToString());
         }
      }

      internal void PatOutlet_ConnectTo(CPatOutlet aOutlet, CPatInlet aInlet)=> this.CheckNoError(DllImports.PatBase_ConnectTo(aOutlet.PatBase.Ptr, aOutlet.Index, aInlet.PatBase.Ptr, aInlet.Index));

      private string GetString(IntPtr aPtr)
      {
         var aStringBuilder = new StringBuilder();
         var aIdx = 0;
         while(true)
         {
            var aChar = (char)Marshal.ReadByte(aPtr, aIdx);
            if (aChar == '\0')
               return aStringBuilder.ToString();
            aStringBuilder.Append(aChar);
            ++aIdx;
         }
      }

      internal string Obj_GetClassName(IntPtr aObjPtr) => this.GetString(DllImports.Obj_GetClassName(aObjPtr));

      internal IntPtr Box_GetObjectPtr(IntPtr aBoxPtr) => DllImports.Box_GetObjectPtr(aBoxPtr);
   }
}
