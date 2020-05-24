using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbMaxClrAdapter.Patcher
{
   public abstract class CPatConnector
   {
      internal CPatConnector(CPatBase aPatBase, int aIndex)
      {
         this.PatBase = aPatBase;
         this.Index = aIndex;
      }
      internal readonly CPatBase PatBase;
      internal readonly Int32 Index;
   }

   public sealed class CPatInlet : CPatConnector
   {
      internal CPatInlet(CPatBase aPatBase, Int32 aIndex) : base(aPatBase, aIndex)
      {
      }
      public void ConnectTo(CPatOutlet aOutlet) => aOutlet.ConnectTo(this);
   }

   public sealed class CPatOutlet : CPatConnector
   {
      internal CPatOutlet(CPatBase aPatBase, Int32 aIndex) : base(aPatBase, aIndex)
      {
      }
      public void ConnectTo(CPatInlet aInlet) => this.PatBase.Marshal.PatOutlet_ConnectTo(this, aInlet);

   }

   public sealed class CPatInlets
   {
      internal CPatInlets(CPatBase aPatBase)
      {
         this.PatBase = aPatBase;
      }
      internal readonly CPatBase PatBase;
      public CPatInlet this[Int32 aIndex] { get => new CPatInlet(this.PatBase, aIndex); }

      
   }
   public sealed class CPatOutlets
   {
      internal CPatOutlets(CPatBase aPatBase)
      {
         this.PatBase = aPatBase;
      }
      internal readonly CPatBase PatBase;
      public CPatOutlet this[Int32 aIndex] { get => new CPatOutlet(this.PatBase, aIndex); }

   }



   public abstract class CPatBase
   {
      internal CPatBase(CMarshal aMarshal, IntPtr aPtr)
      {
         this.Marshal = aMarshal;
         this.Ptr = aPtr;
         this.Inlets = new CPatInlets(this);
         this.Outlets = new CPatOutlets(this);
      }
      internal IntPtr Ptr;
      internal readonly CMarshal Marshal;

      public string ClassName { get => this.Marshal.Obj_GetClassName(this.Ptr); }

      public void Delete()
      {
         //this.Marshal.Max_Log_Write(nameof(this.Delete) + ".Ptr=" + this.Ptr.ToString("X16"), false);
         this.Marshal.PatBase_Delete(this);
         this.Ptr = IntPtr.Zero;
      }
      public CPatInlets Inlets { get; private set; }
      public CPatOutlets Outlets { get; private set; }
   }

   public sealed class CPatPatcher:  CPatBase
   {
      internal CPatPatcher(CMarshal aMarshal, IntPtr aPtr):base(aMarshal, aPtr)
      {
      }
      internal static CPatPatcher New(CMarshal aMarshal, IntPtr aIntPtr) => new CPatPatcher(aMarshal, aIntPtr);

      private CPatBox Load(string aName) 
      {
         var aIntPtr = this.Marshal.Patcher_GetBoxPtr(this, aName);
         var aObject = new CPatBox(this, aIntPtr);
         return aObject;
      }

      public CPatBox GetBox(string aScriptingName) => this.Load(aScriptingName);
      public CPatPatcher GetSubPatcher(string aScriptingName) => (CPatPatcher) this.GetBox(aScriptingName).Object;

      public CPatBox Add(string aBoxText, string aObjectName)
      {
         var aPtr = this.Marshal.Patcher_Add(this, aBoxText, aObjectName);
         var aObject = new CPatBox(this, aPtr);
         return aObject;
      }

      public bool GetContainsObject(string aObjectName) => this.Marshal.Patcher_GetContainsObject(this, aObjectName);

   }

   internal sealed class CClassRegistry
   {
      private delegate CPatBase CNewFunc(CMarshal aMarshal, IntPtr aIntPtr);
      private readonly Dictionary<string, CNewFunc> NewFuncs = new Dictionary<string, CNewFunc>();
      private CClassRegistry ()
      {
         this.NewFuncs.Add("jpatcher", CPatPatcher.New);
      }

      internal static readonly CClassRegistry Singleton = new CClassRegistry();

      private CNewFunc GetNewFunc(string aClassName) => this.NewFuncs.ContainsKey(aClassName) ? this.NewFuncs[aClassName] : CPatObject.New;

      internal CPatBase New(string aClassName, CMarshal aMarshal, IntPtr aIntPtr) => GetNewFunc(aClassName)(aMarshal, aIntPtr);

   }

   public sealed class CPatObject: CPatBase
   {
      #region ctor
      internal CPatObject(CMarshal aMarshal, IntPtr aPtr):base(aMarshal, aPtr)
      {
      }
      internal static CPatObject New(CMarshal aMarshal, IntPtr aPtr) => new CPatObject(aMarshal, aPtr);
      #endregion
   }

   public sealed class CPatBox : CPatBase
   {
      internal CPatBox(CPatPatcher aParentPatcher, IntPtr aBoxPtr) : base(aParentPatcher.Marshal, aBoxPtr)
      {
         this.ParentPatcher = aParentPatcher;
         this.Object = this.NewObject();
      }
      internal IntPtr ObjectPtr { get => this.Marshal.Box_GetObjectPtr(this.Ptr); }
      internal string ObjectClassName { get => this.Marshal.Obj_GetClassName(this.ObjectPtr); }
      private CPatBase NewObject() => CClassRegistry.Singleton.New(this.ObjectClassName, this.Marshal, this.ObjectPtr);
      public CPatBase Object { get; private set; }

      public CPatPatcher ParentPatcher
      {
         get;
         internal set;
      }

   }

}
