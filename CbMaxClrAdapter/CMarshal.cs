using CbMaxClrAdapter.Jitter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CbMaxClrAdapter
{
   internal sealed class CMarshal
   {
      internal CMarshal(CMaxObject aMaxObject)
      {
         this.MaxObject = aMaxObject;
      }

      private readonly CMaxObject MaxObject;

      internal void Init()
      {
         var aArgs = this.MaxObject.NewArgs;
         DllImports.Object_Delete_Func_Set(aArgs.mObjectPtr, this.Delete);
         DllImports.Object_In_Bang_Func_Set(aArgs.mObjectPtr, this.OnInBang);
         DllImports.Object_In_Float_Func_Set(aArgs.mObjectPtr, this.OnInFloat);
         DllImports.Object_In_Int_Func_Set(aArgs.mObjectPtr, this.OnInInt);
         DllImports.Object_In_Symbol_Func_Set(aArgs.mObjectPtr, this.OnInSymbol);
         DllImports.Memory_Delete_Func_Set(aArgs.mObjectPtr, this.Delete);
         DllImports.Object_Assist_GetString_Func_Set(aArgs.mObjectPtr, this.GetAssistString);
         DllImports.Object_In_Receive_Func_Set(aArgs.mObjectPtr, this.In_Receive);
         DllImports.Object_In_List_Clear_Func_Set(aArgs.mObjectPtr, this.In_List_Clear);
         DllImports.Object_In_List_Add_Float_Func_Set(aArgs.mObjectPtr, this.In_List_AddFloat);
         DllImports.Object_In_List_Add_Int_Func_Set(aArgs.mObjectPtr, this.In_List_AddInt);
         DllImports.Object_In_List_Add_Symbol_Func_Set(aArgs.mObjectPtr, this.In_List_AddSymbol);
         DllImports.Object_Out_List_Symbol_Get_Func_Set(aArgs.mObjectPtr, this.Object_Out_List_Symbol_Get);
         DllImports.Object_Out_List_Element_Count_Get_Func_Set(aArgs.mObjectPtr, this.Object_Out_List_Element_Count_Get);
         DllImports.Object_Out_List_Element_Type_Get_Func_Set(aArgs.mObjectPtr, this.Object_Out_List_Element_Type_Get);
         DllImports.Object_Out_List_Element_Float_Get_Func_Set(aArgs.mObjectPtr, this.Object_Out_List_Element_Float_Get);
         DllImports.Object_Out_List_Element_Int_Get_Func_Set(aArgs.mObjectPtr, this.Object_Out_List_Element_Int_Get);
         DllImports.Object_Out_List_Element_Symbol_Get_Func_Set(aArgs.mObjectPtr, this.Object_Out_List_Element_Symbol_Get);
         DllImports.Object_In_Matrix_Receive_Func_Set(aArgs.mObjectPtr, this.Object_In_Matrix_Receive);
      }
      private void Delete(IntPtr aHGlobalMem)
      {
         if (aHGlobalMem != IntPtr.Zero)
         {
            Marshal.FreeHGlobal(aHGlobalMem);
         }
      }
      private IntPtr AllocExportString(string aString)
      {
         IntPtr aPtr = Marshal.StringToHGlobalAnsi(aString);
         return aPtr;
      }
      private void Delete()
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
            return aFunc();
         }
         catch (Exception aExc)
         {
            if (this.MaxObject is object)
            {
               this.MaxObject.WriteLogErrorMessage(aExc);
            }
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

      private void OnInBang(int aInletIdx)
      {
         this.WithCatch(delegate () { this.MaxObject.Inlets[aInletIdx].Receive(CMessageTypeEnum.Bang); });
      }

      private void OnInFloat(int aInletIdx, double aValue)
      {
         this.WithCatch(delegate ()
         {
            var aInlet = this.MaxObject.Inlets[aInletIdx];
            var aMessage = aInlet.GetMessage<CFloat>();
            aMessage.Set(aValue);
            aInlet.Receive(CMessageTypeEnum.Float);
         });
      }

      private void OnInInt(Int32 aInletIdx, Int32 aValue)
      {
         this.WithCatch(delegate ()
         {
            var aInlet = this.MaxObject.Inlets[aInletIdx];
            var aMessage = aInlet.GetMessage<CInt>();
            aMessage.Set(aValue);
            aInlet.Receive(CMessageTypeEnum.Int);
         });
      }

      private void OnInSymbol(Int32 aInletIdx, string aSymbolName)
      {
         var aInlet = this.MaxObject.Inlets[(int)aInletIdx];
         var aMessage = aInlet.GetMessage<CSymbol>();
         aMessage.Set(aSymbolName);
         aInlet.Receive(CMessageTypeEnum.Symbol);
      }

      private void In_Receive(Int32 aInletIdx, Int32 aDataTypeI64)
      {
         this.WithCatch(delegate ()
         {
            var aDataTypeEnum = (CMessageTypeEnum)aDataTypeI64;
            this.MaxObject.Inlets[(int)aInletIdx].Receive((CMessageTypeEnum)aDataTypeEnum);
         });
      }

      private void In_List_Clear(Int32 aInletIdx)
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

      internal void Send(CMatrixOutlet aMatrixOutlet)
      {
         var aObjectPtr = this.MaxObject.NewArgs.mObjectPtr;
         var aIndex = aMatrixOutlet.Index;
         var aMatrixData = aMatrixOutlet.Message.Value;
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
                  DllImports.Object_Out_Matrix_Send(aObjectPtr, aMatrixOutlet.Ptr, aSize, aCellType, aDimensionCount, aDimensionSizesPtr, aDimensionStridesPtr, aPlaneCount, aDataPtr);
               }
               finally
               {
                  this.Delete(aDataPtr);
               }
            }
            finally
            {
               this.Delete(aDimensionStridesPtr);
            }
         }
         finally
         {
            this.Delete(aDimensionSizesPtr);
         }
      }

      private void In_List_AddFloat(Int32 aInletIdx, double aFloat)
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

      private void In_List_AddInt(Int32 aInletIdx, Int32 aInt)
      {
         this.WithCatch(delegate ()
         {
            this.MaxObject.Inlets[(int)aInletIdx].GetMessage<CList>().Value.Editable.AddInternal(aInt);
         });
      }
      private void In_List_AddSymbol(Int32 aInletIdx, IntPtr aSymbol)
      {
         this.WithCatch(delegate ()
         {
            this.MaxObject.Inlets[(int)aInletIdx].GetMessage<CList>().Value.Editable.AddInternal(Marshal.PtrToStringAnsi(aSymbol));
         });
      }

      private IntPtr GetAssistString(IntPtr aObjectPtr, int aStringProvider, int aIndex)
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
         if (object.ReferenceEquals(null, aElement))
            return CMessageTypeEnum.Null;
         else if (aElement is string)
            return CMessageTypeEnum.Symbol;
         else if (aElement is double)
            return CMessageTypeEnum.Float;
         else if (aElement is Int32)
            return CMessageTypeEnum.Int;
         else
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

      internal void Send(CBangOutlet aBangOutlet) => DllImports.Object_Out_Bang_Send(aBangOutlet.MaxObject.Ptr, aBangOutlet.Ptr);

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

      internal void Send(CSymbolOutlet aSymbolOutlet) => DllImports.Object_Out_Symbol_Send(aSymbolOutlet.MaxObject.Ptr, aSymbolOutlet.Ptr, aSymbolOutlet.Message.Value);
      internal void Send(CFloatOutlet aFloatOutlet) => DllImports.Object_Out_Float_Send(this.MaxObject.Ptr, aFloatOutlet.Ptr, aFloatOutlet.Message.Value);
      internal void Send(CIntOutlet aIntOutlet) => DllImports.Object_Out_Int_Send(this.MaxObject.Ptr, aIntOutlet.Ptr, aIntOutlet.Message.Value);
      internal void Object_In_Matrix_Receive(Int32 aInletIdx, string aObjectName) => DllImports.Object_In_Matrix_Receive(this.MaxObject.NewArgs.mObjectPtr, aInletIdx, aObjectName);
      internal void Send(CListOutlet aListOutlet) => DllImports.Object_Out_List_Send(this.MaxObject.Ptr, aListOutlet.Ptr, aListOutlet.Index);
      internal IntPtr AddOutlet(COutlet aOutlet, CMessageTypeEnum aDataTypeEnum) => DllImports.Object_Out_Add(this.MaxObject.NewArgs.mObjectPtr, (int)aDataTypeEnum, aOutlet.Index);
      internal void Max_Log_Write(string aMsg, bool aIsError) => DllImports.Max_Log_Write(this.MaxObject.NewArgs.mObjectPtr, aMsg, aIsError ? 1 : 0);
   }
}
