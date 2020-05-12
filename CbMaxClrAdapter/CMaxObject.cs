using CbMaxClrAdapter.Jitter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static CbMaxClrAdapter.DllExports;

namespace CbMaxClrAdapter
{
   public enum CMessageTypeEnum
   {
      Bang = 0,
      Float = 1,
      Int = 2,
      List = 3,
      Symbol = 4,
      Null = 5,
      Matrix = 6,
      Any = 7
   }

   public sealed class CDataTypeAttribute : Attribute
   {
      public CDataTypeAttribute(CMessageTypeEnum aMessageTypeEnum)
      {
         this.DataTypeEnum = aMessageTypeEnum;
      }

      public static CMessageTypeEnum Get(Type aType) => aType.GetCustomAttribute<CDataTypeAttribute>().DataTypeEnum;

      public readonly CMessageTypeEnum DataTypeEnum;

   }

   public abstract class CSealable
   {
      private bool IsReadonly;

      internal virtual void Seal()
      {
         this.IsReadonly = true;
      }

      internal void CheckWrite(bool aInternalWrite = false)
      {
         if (!aInternalWrite
         && this.IsReadonly)
         {
            throw new InvalidOperationException("This object is readonly.");
         }
      }
   }

   public abstract class CMessage : CSealable
   {
      internal CMessage()
      {
      }
      public CMessageTypeEnum DataTypeEnum { get => CDataTypeAttribute.Get(this.GetType()); }
      internal abstract object Data { get; }
      internal abstract bool DataIsDefined { get; }
      internal abstract void AddTo(CEditableListData aList);

      internal abstract void Send(CMultiTypeOutlet aMultiTypeOutlet);
   }

   [CDataTypeAttribute(CMessageTypeEnum.Bang)]
   public sealed class CBang : CMessage
   {
      internal CBang()
      {
      }

      internal override object Data => throw new InvalidOperationException();
      internal override bool DataIsDefined => false;
      internal override void AddTo(CEditableListData value) => throw new InvalidOperationException();

      internal override void Send(CMultiTypeOutlet aMultiTypeOutlet)
      {
         aMultiTypeOutlet.MaxObject.Marshal.Send(aMultiTypeOutlet, this);
      }
   }

   public abstract class CValMessage : CMessage
   {
      internal CValMessage()
      {
      }
   }

   public abstract class CValMessage<T>
   :
       CValMessage
   {
      internal CValMessage()
      {
      }

      private T ValueM;
      public T Value { get => this.ValueM; set => this.Set(value, false); }
      internal void Set(T aValue, bool aInternalSet = true)
      {
         this.CheckWrite(aInternalSet);
         this.ValueM = aValue;
      }

      internal override void Seal()
      {
         base.Seal();
         var aValue = this.Value;
         if (aValue is CSealable)
         {
            ((CSealable)(object)aValue).Seal();
         }
      }

      internal override object Data => this.Value;
      internal override bool DataIsDefined => true;
   }

   [CDataTypeAttribute(CMessageTypeEnum.Symbol)]
   public sealed class CSymbol : CValMessage<string>
   {
      internal CSymbol()
      {
         this.Value = string.Empty;
      }
      internal override void AddTo(CEditableListData aListData) => aListData.Add(this.Value);
      internal override void Send(CMultiTypeOutlet aMultiTypeOutlet) => aMultiTypeOutlet.MaxObject.Marshal.Send(aMultiTypeOutlet, this);
   }

   [CDataTypeAttribute(CMessageTypeEnum.Int)]
   public sealed class CInt : CValMessage<Int32>
   {
      public CInt(Int32 aValue = 0)
      {
         this.Value = aValue;
      }
      internal override void AddTo(CEditableListData aList) => aList.Add(this.Value);
      internal override void Send(CMultiTypeOutlet aMultiTypeOutlet) => aMultiTypeOutlet.MaxObject.Marshal.Send(aMultiTypeOutlet, this);
   }

   [CDataTypeAttribute(CMessageTypeEnum.Float)]
   public sealed class CFloat : CValMessage<double>
   {
      public CFloat(double aValue = 0)
      {
         this.Value = aValue;
      }
      internal override void AddTo(CEditableListData aList) => aList.Add(this.Value);
      internal override void Send(CMultiTypeOutlet aMultiTypeOutlet) => aMultiTypeOutlet.MaxObject.Marshal.Send(aMultiTypeOutlet, this);
   }

   [CDataTypeAttribute(CMessageTypeEnum.List)]
   public sealed class CList : CValMessage<CEditableListData>
   {
      internal CList(CEditableListData aList)
      {
         this.Value = aList;
      }
      public bool Support(CMessageTypeEnum aMessageType) => aMessageType == CMessageTypeEnum.Int
                                                   || aMessageType == CMessageTypeEnum.Float
                                                   || aMessageType == CMessageTypeEnum.Symbol
                                                    ;
      internal override void AddTo(CEditableListData value) => throw new InvalidOperationException();
      internal override void Send(CMultiTypeOutlet aMultiTypeOutlet) => aMultiTypeOutlet.MaxObject.Marshal.Send(aMultiTypeOutlet, this);
   }

   public abstract class CConnector
   {
      internal CConnector(CMaxObject aMaxObject)
      {
         if (aMaxObject.Initialized)
         {
            throw new InvalidOperationException("Adding inlets/outlets at runtime is not supported.");
         }
         this.MaxObject = aMaxObject;
      }

      internal readonly CMaxObject MaxObject;
      internal abstract void Add();
      internal IntPtr Ptr;

      public Func<string> GetDescription;
      public string Description { get => object.ReferenceEquals(this.GetDescription, null) ? string.Empty : this.GetDescription(); }

      private readonly CMessage[] Messages = new CMessage[(from aEnum in Enum.GetValues(typeof(CMessageTypeEnum)).Cast<int>() select aEnum).Max() + 1];

      internal abstract int Index { get; }

      internal abstract bool IsReadonly { get; }

      private CMessage NewMessage(CMessageTypeEnum aMessageType)
      {
         var aMessage = this.NewMessage1(aMessageType);
         if (this.IsReadonly)
         {
            aMessage.Seal();
         }
         return aMessage;
      }
      private CMessage NewMessage1(CMessageTypeEnum aMessageType)
      {
         switch (aMessageType)
         {
            case CMessageTypeEnum.Bang:
               return new CBang();
            case CMessageTypeEnum.Float:
               return new CFloat();
            case CMessageTypeEnum.Int:
               return new CInt();
            case CMessageTypeEnum.List:
               return new CList(new CEditableListData());
            case CMessageTypeEnum.Matrix:
               return new CMatrix();
            case CMessageTypeEnum.Symbol:
               return new CSymbol();

            default:
               throw new ArgumentException();
         }
      }
      internal void SupportInternal(CMessageTypeEnum aMessageType)
      {
         if (!this.IsSupported(aMessageType))
         {
            this.Messages[(int)aMessageType] = this.NewMessage(aMessageType);
         }
      }
      public bool IsSupported(CMessageTypeEnum aMessageType) => this.Messages[(int)aMessageType] is object;
      internal virtual void CheckSupport(CMessageTypeEnum aMessageTypeEnum)
      {
         if (!this.IsSupported(aMessageTypeEnum))
         {
            throw new InvalidOperationException();
         }
      }

      public void Support(CMessageTypeEnum aMessageType)
      {
         // 20200512: Auskommentiert, macht doch keinen sinn ?! 
         // this.CheckSupport(aMessageType);
         this.SupportInternal(aMessageType);
      }
      internal bool GetSupport(CMessageTypeEnum aMessageType)
      {
         return !object.ReferenceEquals(null, this.Messages[(int)aMessageType]);
      }

      internal CMessage GetMessage(CMessageTypeEnum aMessageTypeEnum)
      {
         var aMessage = this.Messages[(int)aMessageTypeEnum];
         if (object.ReferenceEquals(null, aMessage))
         {
            throw this.MaxObject.NewDoesNotUnderstandExc(this, aMessageTypeEnum);
         }
         return aMessage;
      }
      public TMessage GetMessage<TMessage>() => (TMessage)(object)this.GetMessage(CDataTypeAttribute.Get(typeof(TMessage)));
      internal abstract void Delete();

   }

   public abstract class CInlet : CConnector
   {
      internal CInlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
         aMaxObject.Inlets.Add(this);
         this.IndexM = aMaxObject.Inlets.Count - 1;
      }

      internal override void Add()
      {
         this.Ptr = this.MaxObject.Marshal.AddInlet(this);
      }

      private readonly int IndexM;
      internal override int Index => this.IndexM;

      public bool SingleItemListEnabled { get; set; }

      internal CList List { get => (CList)this.GetMessage(CMessageTypeEnum.List); }

      protected abstract void Receive(CMessage aMessage);

      internal void Receive(CMessageTypeEnum aMessageType)
      {
         if (this.SingleItemListEnabled
         && aMessageType != CMessageTypeEnum.List
         && this.GetSupport(CMessageTypeEnum.List)
         && this.List.Support(aMessageType))
         {
            var aMessage = this.GetMessage(aMessageType);
            var aList = this.List;
            aList.Value.Editable.Clear();
            aMessage.AddTo(aList.Value.Editable);
            this.Receive(aList);
         }
         else if (aMessageType == CMessageTypeEnum.Symbol
              && this.Dispatch(this.GetMessage<CSymbol>()))
         {
            // Message dispatched.
         }
         else if (aMessageType == CMessageTypeEnum.List
             && this.Dispatch(this.GetMessage<CList>()))
         {
            // Message dispatched.
         }
         else
         {
            this.Receive(this.GetMessage(aMessageType));
         }
      }
      internal override void Delete() => this.MaxObject.Marshal.Delete(this);

      public delegate void CListAction(CInlet aInlet, CListData aListData);

      #region SymbolDispatching
      public delegate void CSymbolAction(CInlet aInlet, CSymbol aMessage);
      private readonly Dictionary<string, CSymbolAction> SymbolActions = new Dictionary<string, CSymbolAction>();
      public void SetSymbolAction(string aSymbol, CSymbolAction aAction)
      {
         this.CheckSupport(CMessageTypeEnum.Symbol);
         if (aAction is object)
         {
            this.SymbolActions[aSymbol] = aAction;
         }
         else if (this.SymbolActions.ContainsKey(aSymbol))
         {
            this.SymbolActions.Remove(aSymbol);
         }
      }
      internal bool Dispatch(CSymbol aMessage)
      {
         var aSymbolName = aMessage.Value;
         if (this.SymbolActions.ContainsKey(aSymbolName))

         {
            var aAction = this.SymbolActions[aSymbolName];
            if (aAction is object)
            {
               aAction(this, aMessage);
               return true;
            }
            else
            {
               return false;
            }
         }
         else
         {
            return false;
         }
      }
      #endregion
      #region ListDispatching
      public delegate void CPrefixedListAction(CInlet aInlet, string aFirstItem, CReadonlyListData aRemainingItems);
      private readonly Dictionary<string, CPrefixedListAction> RemainingItemsActions = new Dictionary<string, CPrefixedListAction>();
      public void SetPrefixedListAction(string aFirstListItem, CPrefixedListAction aAction)
      {
         this.CheckSupport(CMessageTypeEnum.List);
         if (aAction is object)
         {
            this.RemainingItemsActions[aFirstListItem] = aAction;
         }
         else if (this.RemainingItemsActions.ContainsKey(aFirstListItem))
         {
            this.RemainingItemsActions.Remove(aFirstListItem);
         }
      }
      private bool Dispatch(CList aList)
      {
         var aListData = aList.Value;
         if (!aListData.IsEmpty())
         {
            var aSymbol = aListData.First().ToString(); // TODO-Use .Symbol here.
            if (this.RemainingItemsActions.ContainsKey(aSymbol))
            {
               var aRemainingItems = new CReadonlyListData(aListData.Skip(1));
               var aAction = this.RemainingItemsActions[aSymbol];
               aAction(this, aSymbol, aRemainingItems);
               return true;
            } 
            return false;
         }
         return false;
      }
      #endregion
   }

   public abstract class CSingleTypeInlet<TMessage> : CInlet<TMessage> where TMessage : CMessage
   {
      internal CSingleTypeInlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
      }
      internal CMessageTypeEnum DataTypeEnum { get => CDataTypeAttribute.Get(typeof(TMessage)); }
      public TMessage Message { get => this.GetMessage<TMessage>(); }

      internal virtual void SupportInternal()
      {
         this.SupportInternal(this.DataTypeEnum);
      }

   }

   public abstract class CMultiTypeInletBase : CInlet<CMessage>
   {
      internal CMultiTypeInletBase(CMaxObject aMaxObject) : base(aMaxObject)
      {
      }
      internal override void CheckSupport(CMessageTypeEnum aMessageTypeEnum)
      {
         // Do not throw any error.
         // TODO-This overriding/behaviour deactivating method shall be obsolete.
      }

   }

   public sealed class CMultiTypeInlet : CMultiTypeInletBase
   {
      internal CMultiTypeInlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
      }
   }

   public abstract class CInlet<TMessage> : CInlet where TMessage : CMessage
   {
      public delegate void CAction(CInlet aInlet, TMessage cMessage);

      internal CInlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
      }
      internal override bool IsReadonly => true;

      public CAction Action;

      protected override void Receive(CMessage aMessage) => this.Receive((TMessage)aMessage);
      protected virtual void Receive(TMessage aMessage)
      {
         if (this.Action is object)
         {
            this.Action(this, aMessage);
         }
      }
   }

   public sealed class CBangInlet : CSingleTypeInlet<CBang>
   {
      public CBangInlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
         this.SupportInternal();
      }
   }

   public sealed class CIntInlet : CSingleTypeInlet<CInt>
   {
      public CIntInlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
         this.SupportInternal();
      }
   }

   public sealed class CSymbolInlet : CSingleTypeInlet<CSymbol>
   {
      public CSymbolInlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
         this.SupportInternal();
      }
   }

   public sealed class CFloatInlet : CSingleTypeInlet<CFloat>
   {
      public CFloatInlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
         this.SupportInternal();
      }
   }

   public sealed class CListInlet : CSingleTypeInlet<CList>
   {
      public CListInlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
         this.SupportInternal();
      }
   }

   public abstract class COutlet : CConnector
   {
      internal COutlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
         aMaxObject.Outlets.Add(this);
         this.IndexM = aMaxObject.Outlets.Count - 1;
      }

      internal void AddOutlet(CMessageTypeEnum aMessageTypeEnum)
      {
         this.Ptr = this.MaxObject.Marshal.AddOutlet(this, aMessageTypeEnum);
      }
      internal override void Delete() => this.MaxObject.Marshal.Delete(this);
      private readonly int IndexM;
      internal override int Index => this.IndexM;
      internal override bool IsReadonly => false;
   }

   public abstract class COutlet<TControlMessage> : COutlet where TControlMessage : CMessage
   {
      internal COutlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
      }
   }

   public abstract class CSingleTypeOutlet<TMessage> : COutlet<TMessage> where TMessage : CMessage
   {
      internal CSingleTypeOutlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
         this.SupportInternal();
      }
      internal CMessageTypeEnum DataTypeEnum { get => CDataTypeAttribute.Get(typeof(TMessage)); }
      public TMessage Message { get => this.GetMessage<TMessage>(); }
      internal void SupportInternal() => this.SupportInternal(this.DataTypeEnum);
      internal override void Add() => this.AddOutlet(this.DataTypeEnum);
      public abstract void Send();
   }

   public sealed class CMultiTypeOutlet : COutlet<CMessage> 
   {
      public CMultiTypeOutlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
      }
      public void Send(CMessageTypeEnum aMessageTypeEnum)
      {
         var aMessage = this.GetMessage(aMessageTypeEnum);
         aMessage.Send(this);
      }
      internal override void Add() => this.AddOutlet( CMessageTypeEnum.Any);
   }

   public sealed class CBangOutlet : CSingleTypeOutlet<CBang>
   {
      public CBangOutlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
         this.SupportInternal();

      }
      public override void Send() => this.MaxObject.Marshal.Send(this);
   }

   public sealed class CFloatOutlet : CSingleTypeOutlet<CFloat>
   {
      public CFloatOutlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
         this.SupportInternal();

      }

      public override void Send() => this.MaxObject.Marshal.Send(this);
   }


   public sealed class CIntOutlet : CSingleTypeOutlet<CInt>
   {
      public CIntOutlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
         this.SupportInternal();
      }
      public override void Send() => this.MaxObject.Marshal.Send(this);
   }

   public sealed class CSymbolOutlet : CSingleTypeOutlet<CSymbol>
   {
      public CSymbolOutlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
         this.SupportInternal();
      }
      public override void Send() => this.MaxObject.Marshal.Send(this);
   }

   public sealed class CListOutlet : CSingleTypeOutlet<CList>
   {
      public CListOutlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
         this.SupportInternal();
      }
      public override void Send()
      {
         this.MaxObject.Marshal.Send(this);
      }
   }

   public sealed class CLeftInlet : CMultiTypeInletBase
   {
      internal CLeftInlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
      }

      internal override void Add()
      {
         // We don't add it because
         // this inlet is automatically created by max.
      }
   }

   public static class CEnumerateableExtensions
   {
      public static bool IsEmpty(this IEnumerable aEnumerable)
      {
         return !aEnumerable.GetEnumerator().MoveNext();
      }

      public static bool ContainsManyElements(this IEnumerable aEnumerable)
      {
         var aEn = aEnumerable.GetEnumerator();
         return aEn.MoveNext() && aEn.MoveNext();
      }
      public static bool ContainsOneElements(this IEnumerable aEnumerable)
      {
         var aEn = aEnumerable.GetEnumerator();
         return aEn.MoveNext() && !aEn.MoveNext();
      }
   }

   public abstract class CListData
   :
       CSealable
   ,   IEnumerable<object>
   {

      internal abstract IEnumerable<object> Enumerable { get; }
      public CEditableListData Copy()
      {
         var aListData = new CEditableListData();
         aListData.List.AddRange(this.Enumerable);
         return aListData;
      }

      public abstract CEditableListData Editable { get; }

      public IEnumerator<object> GetEnumerator() => this.WithoutListPrefix.GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

      /// <summary>
      /// Max has under certain circumstance a "list" symbol added in front of the list.
      /// This collection  contains the original list received from max which may or may not contain the prefix.
      /// </summary>
      public IEnumerable<object> MaybeWithListPrefix { get => this.Enumerable; }

      /// <summary>
      /// Max has under certain circumstance a "list" symbol added in front of the list.
      /// This collection does not contain this prefix.
      /// </summary>
      public IEnumerable<object> WithoutListPrefix { get => this.StartsWithListPrefix ? this.MaybeWithListPrefix.Skip(1) : this.MaybeWithListPrefix; }

      /// <summary>
      /// Max has under certain circumstance a "list" symbol added in front of the list.
      /// This property defines wether this prefix is included.
      /// </summary>
      public bool StartsWithListPrefix { get => !this.MaybeWithListPrefix.IsEmpty() && this.MaybeWithListPrefix.First().Equals(ListPrefix); }

      /// <summary>
      /// Max has under certain circumstance a "list" symbol added in front of the list.
      /// This constant defines this symbol.
      /// </summary>
      private const string ListPrefix = "list";

      /// <summary>
      /// Max has under certain circumstance a "list" symbol added in front of the list.
      /// This property contains the list with a list prefix symbol added.
      /// </summary>
      public bool NeedsListPrefix { get => !this.MaybeWithListPrefix.IsEmpty() && (!IsSymbol(this.MaybeWithListPrefix.First()) || this.MaybeWithListPrefix.First().ToString() == ListPrefix); }

      /// <summary>
      /// This property designates wether the given value represents a max symbol
      /// </summary>
      private bool IsSymbol(object aElement) => aElement.GetType().Equals(typeof(string));

      /// <summary>
      /// Max has under certain circumstance a "list" symbol added in front of the list.
      /// This enumerable contains the list prefix in any case.
      /// </summary>
      private IEnumerable<object> WithListPrefix { get => new object[] { ListPrefix }.Concat(this.WithoutListPrefix); }

      /// <summary>
      /// Max has under certain circumstance a "list" symbol added in front of the list.
      /// This enumerable contains the list prefix if one is required.
      /// </summary>
      private IEnumerable<object> WithListPrefixIfReqired { get => this.NeedsListPrefix ? this.WithListPrefix : this.WithoutListPrefix; }

      /// <summary>
      /// Max has under certain circumstance a "list" symbol added in front of the list.
      /// This contains the first item of the list as extra symbol.
      /// (In the max c sdk it is required to send this symbol as an aextra data field)
      /// </summary>
      public string Symbol { get => this.IsEmpty() ? ListPrefix : this.NeedsListPrefix ? ListPrefix : this.WithoutListPrefix.First().ToString(); }

      /// <summary>
      /// This does not contain the symbol at the beginning.
      /// Thus it is the list how it should be sent through max.
      /// </summary>
      public IEnumerable<object> WithoutSymbol { get => this.IsEmpty() ? new object[] { } : IsSymbol(this.WithListPrefixIfReqired.First()) ? this.WithListPrefixIfReqired.Skip(1) : this.WithListPrefixIfReqired; }

      public static void Test(Action<string> aFailAction)
      {
         {
            var aList = new CEditableListData();
            aList.Add("a");
            aList.Add("b");
            Test("dc6a5d4a-0cfc-4cc9-a708-e10c406a2241", aList.WithListPrefixIfReqired.Count() == 2, aFailAction);
            Test("77b1cefb-3f3a-432c-805d-e78dc90a10e5", aList.Symbol == "a", aFailAction);
            Test("7391281d-44f2-4ef6-86bc-4f7fd2ca070d", aList.WithoutSymbol.Count() == 1, aFailAction);
            Test("b9c80347-6db6-4f46-a31b-47b3e9bb9758", aList.WithoutSymbol.Single().ToString() == "b", aFailAction);
         }

         {
            var aList = new CEditableListData();
            aList.Add("list");
            aList.Add(1);
            aList.Add(2);
            Test("5d8a489d-b399-451d-9339-3b07d44a34ca", aList.WithListPrefixIfReqired.Count() == 3, aFailAction);
            Test("dd5c3d80-1027-406d-a4e3-a7a4878e12a0", aList.WithListPrefixIfReqired.ElementAt(0).ToString() == "list", aFailAction);
            Test("c2042fd4-e9ea-4cab-b29a-bd0f71f695e4", aList.WithListPrefixIfReqired.ElementAt(1).ToString() == "1", aFailAction);
            Test("075605b1-11d0-4ec7-b014-6a23e652c02d", aList.WithListPrefixIfReqired.ElementAt(2).ToString() == "2", aFailAction);
            Test("e9538d36-b6a9-4d5e-806a-757ad96489b2", aList.WithoutListPrefix.Count() == 2, aFailAction);
            Test("2a3ab0d2-dd4e-4146-bc32-0bd8faa245f8", aList.WithoutListPrefix.ElementAt(0).ToString() == "1", aFailAction);
            Test("2a8779f5-1889-43e0-bfa1-4f7ac9de026c", aList.WithoutListPrefix.ElementAt(1).ToString() == "2", aFailAction);
            Test("e97adb6e-ee74-49ab-91d8-8c1ac10f3b78", aList.Symbol == "list", aFailAction);
            Test("35966a35-a626-4e24-806b-df29cc0f6929", aList.WithoutSymbol.Count() == 2, aFailAction);
            Test("2adfd153-6d20-4fbd-907e-0a73ae69462f", aList.WithoutSymbol.ElementAt(0).ToString() == "1", aFailAction);
            Test("71c1bf9c-a29d-41d3-b229-98c9d91c2bdf", aList.WithoutSymbol.ElementAt(1).ToString() == "2", aFailAction);
         }

         {
            var aList = new CEditableListData();
            aList.Add(1);
            aList.Add(2);
            Test("81473524-054d-4b1c-abab-0e9fe0d5a932", aList.WithListPrefixIfReqired.Count() == 3, aFailAction);
            Test("43731ce7-2c38-447f-8e10-fed6a9339183", aList.WithListPrefixIfReqired.ElementAt(0).ToString() == "list", aFailAction);
            Test("83a08072-5a61-4537-ba07-286129715042", aList.WithListPrefixIfReqired.ElementAt(1).ToString() == "1", aFailAction);
            Test("8886fdf8-7fce-4495-bd8a-489d5b0e4005", aList.WithListPrefixIfReqired.ElementAt(2).ToString() == "2", aFailAction);
            Test("b423d13a-2440-4da6-8a2b-12f9f198b11a", aList.WithoutListPrefix.Count() == 2, aFailAction);
            Test("7ac4ca03-14ec-4e71-990f-5d3b75d775d9", aList.WithoutListPrefix.ElementAt(0).ToString() == "1", aFailAction);
            Test("fbb1a25e-22f9-4102-85c2-df5b04ce1d27", aList.WithoutListPrefix.ElementAt(1).ToString() == "2", aFailAction);
            Test("2ee88781-d907-4836-b1e6-e533ad3af97c", aList.Symbol == "list", aFailAction);
            Test("4b248c61-62cc-4081-8777-d5dbe06e4afa", aList.WithoutSymbol.Count() == 2, aFailAction);
            Test("6390757e-1728-4f62-aabf-143de5a3f11f", aList.WithoutSymbol.ElementAt(0).ToString() == "1", aFailAction);
            Test("abf93779-3111-4974-9ca0-728a690e81a7", aList.WithoutSymbol.ElementAt(1).ToString() == "2", aFailAction);
         }
         {
            var aList = new CEditableListData();
            aList.Add("list");
            aList.Add("list");
            Test("fd12cc18-590c-4c7c-aade-db5bd9785583", aList.WithoutListPrefix.Count() == 1, aFailAction);
            Test("6768d81b-646b-4973-9122-997ed1fd9337", aList.WithoutListPrefix.ElementAt(0).ToString() == "list", aFailAction);
            Test("3d08edd7-682b-41b0-b393-70845b740b42", aList.WithListPrefix.Count() == 2, aFailAction);
            Test("3603bd78-dae5-47e9-b0b9-8ce8b71b74e7", aList.WithListPrefix.ElementAt(0).ToString() == "list", aFailAction);
            Test("faa84a06-2a8a-4aa4-94de-1835120ef4d6", aList.WithListPrefix.ElementAt(1).ToString() == "list", aFailAction);
            Test("55e66063-74e1-45d9-95a1-f8770ff00738", aList.Symbol == "list", aFailAction);
            Test("9afc7724-84b5-48f2-86a8-36ab9eef6713", aList.WithoutSymbol.Count() == 1, aFailAction);
            Test("6661672c-a276-4019-8479-5ce0dd1c31c7", aList.WithoutSymbol.ElementAt(0).ToString() == "list", aFailAction);
         }
      }

      private static void Test(string aTestCaseId, bool aOk, Action<string> aFail)
      {
         if (!aOk)
         {
            aFail(aTestCaseId);
         }
      }
   }

   public sealed class CReadonlyListData : CListData
   {
      internal CReadonlyListData(IEnumerable<object> aEnumerable)
      {
         this.EnumerableM = aEnumerable;
      }

      private IEnumerable<object> EnumerableM;
      internal override IEnumerable<object> Enumerable => this.EnumerableM;

      public override CEditableListData Editable => throw new InvalidOperationException("This list is readonly");
   }

   public sealed class CEditableListData : CListData
   {
      internal CEditableListData()
      {
      }

      public override CEditableListData Editable => this;
      internal List<object> List = new List<object>();
      internal override IEnumerable<object> Enumerable => this.List;

      internal void ClearInternal()
      {
         this.List.Clear();
      }

      public void Clear()
      {
         this.CheckWrite();
         this.ClearInternal();
      }

      internal void AddInternal(Int32 aLong)
      {
         this.List.Add(aLong);
      }
      public void Add(Int32 aLong)
      {
         this.CheckWrite();
         this.AddInternal(aLong);
      }
      internal void AddInternal(string aSymbol)
      {
         this.List.Add(aSymbol);
      }
      public void Add(string aSymbol)
      {
         this.CheckWrite();
         this.AddInternal(aSymbol);
      }
      internal void AddInternal(double aFloat)
      {
         this.List.Add(aFloat);
      }
      public void Add(double aFloat)
      {
         this.CheckWrite();
         this.AddInternal(aFloat);
      }
      public void Add(CMessage aMessage) => aMessage.AddTo(this);
   }

   public abstract class CMaxObject
   {
      #region ctor
      protected CMaxObject()
      {
         this.Marshal = new CMarshal(this);
         this.LeftInlet = new CLeftInlet(this);
      }
      internal void Init(SObject_New aArgs)
      {
         this.NewArgs = aArgs;
         this.Marshal.Init();
         foreach (var aInlet in this.Inlets.AsEnumerable().Reverse())
         {
            aInlet.Add();
         }
         foreach (var aOutlet in this.Outlets.AsEnumerable().Reverse())
         {
            aOutlet.Add();
         }
         this.Initialized = true;
      }
      private bool InitializedM;
      internal bool Initialized { get => this.InitializedM; private set => this.InitializedM = value; }
      #endregion
      #region Marshall
      internal readonly CMarshal Marshal;
      #endregion
      #region Outlets
      internal readonly List<COutlet> Outlets = new List<COutlet>();
      #endregion
      #region Inlets
      internal readonly List<CInlet> Inlets = new List<CInlet>();


      #endregion
      #region LeftInlet
      private CLeftInlet LeftInletM;
      public CLeftInlet LeftInlet { get => this.LeftInletM; private set => this.LeftInletM = value; }

      #endregion
      #region NewArgs
      internal IntPtr Ptr { get => this.NewArgs.mObjectPtr; }
      internal SObject_New NewArgs { get; private set; }
      #endregion
      #region Log
      public void WriteLogErrorMessage(CConnector aConnector, string aMsg)
      {
         this.WriteLogErrorMessage(aConnector.GetType().Name + ": " + aMsg);
      }
      public void WriteLogErrorMessage(string aMsg)
      {
         this.Marshal.Max_Log_Write(aMsg, true);
      }
      public void WriteLogErrorMessage(Exception aExc)
      {
         this.WriteLogErrorMessage(aExc.ToString());
      }
      public void WriteLogInfoMessage(string aMsg)
      {
         this.Marshal.Max_Log_Write(aMsg, false);
      }
      public void WriteLogInfoMessage(string aVarName, string aVarValue)
      {
         this.WriteLogInfoMessage(aVarName + "=" + aVarValue);
      }
      public void WriteLogInfoMessage(string aVarName, IEnumerable<object> aVals)
      {
         this.WriteLogInfoMessage(aVarName, "[" + (from aVal in aVals select Convert.ToString(aVal)).JoinString(", ") + "]");
      }
      internal Exception NewDoesNotUnderstandExc(CConnector aConnector, string aWhat) => new Exception(aConnector.GetType().Name + " does not understand " + aWhat);
      internal Exception NewDoesNotUnderstandExc(CConnector aConnector, CMessageTypeEnum aMessageTypeEnum) => this.NewDoesNotUnderstandExc(aConnector, aMessageTypeEnum.ToString());
      #endregion
   }


   public static class CStringExtensions
   {
      public static string JoinString(this IEnumerable<string> aStrings, string aLimiter)
      {
         var aStringBuilder = new StringBuilder();
         var aOpen = false;
         foreach (var aString in aStrings)
         {
            if (aOpen)
               aStringBuilder.Append(aLimiter);
            aStringBuilder.Append(aString);
            aOpen = true;
         }
         return aStringBuilder.ToString();
      }
   }
}
