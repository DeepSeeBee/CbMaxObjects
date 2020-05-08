using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq.Expressions;
using System.IO;
using RGiesecke.DllExport;

namespace CbMaxClrAdapter
{

    internal static class CLog
    {
        internal static void Write(string aMsg, bool aNewLine = true)
        {
            DllImports.Max_Log_Write(IntPtr.Zero, aMsg, 0);

            // Das crashte sporadisch:
            //var aThreadId = aNewLine ? System.Threading.Thread.CurrentThread.ManagedThreadId.ToString()  + ": " : string.Empty;
            //var aFileInfo = new FileInfo(new FileInfo(typeof(DllExports).Assembly.Location).FullName + ".log");
            //System.IO.File.AppendAllText(aFileInfo.FullName, aThreadId + aMsg + (aNewLine ? Environment.NewLine : string.Empty));
            //System.Threading.Thread.Sleep(10);
        }
         
        internal static void Write(Exception aExc)
        {
            Write(aExc.ToString());
        }

    }

    /// <summary>
    /// TODO: long wird als 32 Bit wert definiert. Habe überall Int64 verwendet.
    /// Das Marshalling gleicht das aus, aber iwann auf 32bit korrigieren.
    /// </summary>
    public static class DllImports
    {

        private const CharSet mCharset = CharSet.Ansi;
        private const string mDllName = "cb_clrobject.mxe64";

        public delegate void CObjectFreeFunc();
        [DllImport(mDllName)]
        public static extern void Object_Delete_Func_Set(IntPtr aSCbClrObjectPtr, CObjectFreeFunc aFunc);

        [DllImport(mDllName)]
        public static extern IntPtr Object_In_Add(IntPtr aSCbClrObjectPtr, Int32 aType, Int32 aPos);

        [DllImport(mDllName)]
        public static extern IntPtr In_Delete(IntPtr aInletPtr);

        [DllImport(mDllName)]
        public static extern IntPtr Object_Out_Add(IntPtr aSCbClrObjectPtr, Int32 aType, Int32 aPos);

        [DllImport(mDllName)]
        public static extern IntPtr Out_Delete(IntPtr aOutletPtr);

        public delegate void CObject_In_Bang_Func(Int32 aInletIdx);
        [DllImport(mDllName)]
        public static extern void Object_In_Bang_Func_Set(IntPtr aSCbClrObjectPtr, CObject_In_Bang_Func aFunc);

        public delegate void CObject_In_Float_Func(Int32 aInletIdx, double aValue);
        [DllImport(mDllName)]
        public static extern void Object_In_Float_Func_Set(IntPtr aSCbClrObjectPtr, CObject_In_Float_Func aFunc);

        public delegate void CObject_In_Int_Func(Int32 aInletIdx, Int32 aValue);
        [DllImport(mDllName)]
        public static extern IntPtr Object_In_Int_Func_Set(IntPtr aSCbClrObjectPtr, CObject_In_Int_Func aFunc);

        public delegate void CObject_In_Symbol_Func(Int32 aInletIdx, string aSymbolName);
        [DllImport(mDllName)]
        public static extern IntPtr Object_In_Symbol_Func_Set(IntPtr aSCbClrObjectPtr, CObject_In_Symbol_Func aFunc);

        [DllImport(mDllName)]
        public static extern void Object_Out_Bang_Send(IntPtr aSCbClrObjectPtr, IntPtr aOutletPtr);

        [DllImport(mDllName)]
        public static extern void Object_Out_Float_Send(IntPtr aSCbClrObjectPtr, IntPtr aOutletPtr, double aValue);

        [DllImport(mDllName)]
        public static extern void Object_Out_Int_Send(IntPtr aSCbClrObjectPtr, IntPtr aOutletPtr, Int32 aValue);

        [DllImport(mDllName, CharSet = mCharset)]
        public static extern void Object_Out_Symbol_Send(IntPtr aSCbClrObjectPtr, IntPtr aOutletPtr, string aSymbol);

        [DllImport(mDllName)]
        public static extern void Object_Out_List_Send(IntPtr aSCbClrObjectPtr, IntPtr aOutletPtr, Int32 aOutletIdx);

        public delegate IntPtr CObject_Out_List_Symbol_Get_Func(Int32 aOutletIdx);
        [DllImport(mDllName)]
        public static extern void Object_Out_List_Symbol_Get_Func_Set(IntPtr aSCbClrObjectPtr, CObject_Out_List_Symbol_Get_Func aFunc);

        public delegate Int32 CObject_Out_List_Element_Count_Get_Func(Int32 aOutletIdx);
        [DllImport(mDllName)]
        public static extern void Object_Out_List_Element_Count_Get_Func_Set(IntPtr aSCbClrObjectPtr, CObject_Out_List_Element_Count_Get_Func aFunc);

        public delegate Int32 CObject_Out_List_Element_Type_Get_Func(Int32 aOutletIdx, Int32 aElementIdx);
        [DllImport(mDllName)]
        public static extern void Object_Out_List_Element_Type_Get_Func_Set(IntPtr aSCbClrObjectPtr, CObject_Out_List_Element_Type_Get_Func aFunc);

        public delegate double CObject_Out_List_Element_Float_Get_Func(Int32 aOutletIdx, Int32 aElementIdx);
        [DllImport(mDllName)]
        public static extern void Object_Out_List_Element_Float_Get_Func_Set(IntPtr aSCbClrObjectPtr, CObject_Out_List_Element_Float_Get_Func aFunc);

        public delegate Int32 CObject_Out_List_Element_Int_Get_Func(Int32 aOutletIdx, Int32 aElementIdx);
        [DllImport(mDllName)]
        public static extern void Object_Out_List_Element_Int_Get_Func_Set(IntPtr aSCbClrObjectPtr, CObject_Out_List_Element_Int_Get_Func aFunc);

        public delegate IntPtr CObject_Out_List_Element_Symbol_Get_Func(Int32 aOutletIdx, Int32 aElementIdx);
        [DllImport(mDllName)]
        public static extern void Object_Out_List_Element_Symbol_Get_Func_Set(IntPtr aSCbClrObjectPtr, CObject_Out_List_Element_Symbol_Get_Func aFunc);


        public delegate void CMemory_Delete_Func(IntPtr aPtr);

        /// <summary>
        /// TODO: Remove aSCbClrObjectPtr
        /// </summary>
        /// <param name="aSCbClrObjectPtr"></param>
        /// <param name="aFreeStringFunc"></param>
        [DllImport(mDllName)]
        public static extern void Memory_Delete_Func_Set(IntPtr aSCbClrObjectPtr, CMemory_Delete_Func aDeleteFunc);

        public delegate IntPtr CObject_Assist_GetString_Func(IntPtr aSCbClrObjectPtr, Int32 aStringProvider, Int32 aIndex);

        [DllImport(mDllName)]
        public static extern void Object_Assist_GetString_Func_Set(IntPtr aSCbClrObjectPtr, CObject_Assist_GetString_Func aGetAssistStringFunc);

        [DllImport(mDllName, CharSet= mCharset)]
        public static extern void Max_Log_Write(IntPtr aSCbClrObjectPtr, string aMessage, Int32 aError);

        public delegate void CObject_In_Receive(Int32 aInletIdx, Int32 aDataTypeEnum);
        [DllImport(mDllName)]
        public static extern void Object_In_Receive_Func_Set(IntPtr aSCbClrObjectPtr, CObject_In_Receive aFunc);

        public delegate void CObject_In_List_ClearFunc(Int32 aInletIdx);
        [DllImport(mDllName)]
        public static extern void Object_In_List_Clear_Func_Set(IntPtr aSCbClrObjectPtr, CObject_In_List_ClearFunc aFunc);

        public delegate void CObject_In_List_Add_Float_Func(Int32 aInletIdx, double aFloat);
        [DllImport(mDllName)]
        public static extern void Object_In_List_Add_Float_Func_Set(IntPtr aSCbClrObjectPtr, CObject_In_List_Add_Float_Func aFunc);

        public delegate void CObject_In_List_Add_Int_Func(Int32 aInletIdx, Int32 aInt);
        [DllImport(mDllName)]
        public static extern void Object_In_List_Add_Int_Func_Set(IntPtr aSCbClrObjectPtr, CObject_In_List_Add_Int_Func aFunc);


        public delegate void CObject_In_List_Add_Symbol_Func(Int32 aInletIdx, IntPtr aSymbol);
        [DllImport(mDllName)]
        public static extern void Object_In_List_Add_Symbol_Func_Set(IntPtr aSCbClrObjectPtr, CObject_In_List_Add_Symbol_Func aFunc);


        public delegate Int32 CObject_In_Matrix_Receive_Func(Int32 aInletIdx, Int32 aSize, string aCellType, Int32 aDimensionCount, IntPtr aDimensionSizesI64Ptr, IntPtr aDimensionStridesI64Ptr, Int32 aPlaneCount, IntPtr aMatrixDataU8Ptr);
        [DllImport(mDllName)]
        public static extern void Object_In_Matrix_Receive_Func_Set(IntPtr aSCbClrObjectPtr, CObject_In_Matrix_Receive_Func aFunc);

        [DllImport(mDllName, CharSet = mCharset)]
        public static extern void Object_Out_Matrix_Send(IntPtr aSCbClrObjectPtr, IntPtr aOutletPtr, Int32 aSize, string aCellType, Int32 aDimensionCount, IntPtr aDimensionSizesI32s, IntPtr aDimensionStridesI32s, Int32 aPlaneCount, IntPtr aMatrixDataPtr);

        [DllImport(mDllName, CharSet = mCharset)]
        public static extern void Object_In_Matrix_Receive(IntPtr aSCbClrObjectPtr, Int32 aInletIdx, string aObjectName);



    }




    public static class DllExports
    {

        private static object mIdLock = new object();
        private static UInt64 mNewId = 1;
        private static UInt64 NewId()
        {
            lock(mIdLock)
            {
                var aId = mNewId;
                ++mNewId;
                return aId;
            }
        }


        private static void Catch(Exception aExc)
        {
            CLog.Write(aExc.ToString());
            if(System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        private static Dictionary<UInt64, object> Objects = new Dictionary<UInt64, object>();

        public struct SObject_New
        {
            public IntPtr mObjectPtr;
            public string mAssemblyName;
            public string mTypeName;
        }

        private static string GetAssemblyName(string aName)
        {
            try
            {
                var aAssemblyDir = new FileInfo(typeof(DllExports).Assembly.Location).Directory;
                var aFileInfo = new FileInfo(Path.Combine(aAssemblyDir.FullName, aName));
                return aFileInfo.FullName;
            }
            catch (Exception)
            {
                return aName;
            }
        }
        
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static UInt64 Object_New(SObject_New aArgs) 
        {
            try
            {
                //CLog.Write("Object_New");
                var aAssemblyName = GetAssemblyName(aArgs.mAssemblyName);
                //Log(aAssemblyName);
                var aAssembly =  Assembly.LoadFrom(aAssemblyName);
                var aType = aAssembly.GetType(aArgs.mTypeName);
                var aObject = Activator.CreateInstance(aType);
                GetObject<CMaxObject>(aObject).Init(aArgs);
                var aId = NewId();
                Objects.Add(aId, aObject);
                return aId; 
            }
            catch(Exception aExc)
            {
                Catch(aExc);
                return 0;
            }
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static void Object_Free(UInt64 aObjectHandle)
        {
            try
            {
                Objects.Remove(aObjectHandle);
            }
            catch (Exception aExc)
            {
                Catch(aExc);
            }
        }

        private static TObject GetObject<TObject>(object aObject)
        {
            return (TObject)aObject;
        }

        private static TObject GetObject<TObject>(UInt64 aHandle)
        {
            return GetObject<TObject>(Objects[aHandle]);
        }


    }
}
