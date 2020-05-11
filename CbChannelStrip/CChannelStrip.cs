using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbChannelStrip
{
   using System.Drawing;
   using System.IO;
   using System.Security.AccessControl;
   using CbMaxClrAdapter;
   using CbMaxClrAdapter.Jitter;

   internal sealed class CFlowMatrix
   {
      internal CFlowMatrix(Int32 aIoCount, params int[] aMatrix) : this(aIoCount, (from aItem in aMatrix select aItem != 0).ToArray())
      {
      }
      internal CFlowMatrix(Int32 aIoCount, params bool[] aMatrix)
      {
         if (aMatrix.Length != aIoCount * aIoCount)
            throw new ArgumentException("Can not understand this list. Length must be IoCount^2.");
         this.IoCount = aIoCount;
         this.Matrix = aMatrix;
      }
      internal static CFlowMatrix New(IEnumerable<object> aSpec)
      {
         try
         {
            var aIoCount = Convert.ToInt32(aSpec.ElementAt(0));
            var aMatrix = from aItem in aSpec.Skip(1) select Convert.ToInt32(aItem);
            return new CFlowMatrix(aIoCount, aMatrix.ToArray());
         }
         catch(Exception aExc)
         {
            throw new Exception("Can not understand this list. " + aExc.Message);
         }
      }

      internal readonly Int32 IoCount;
      private Int32 CellCount { get => this.IoCount * this.IoCount; }
      internal readonly bool[] Matrix;

      private int GetIndex(int aColumn, int aRow) => aRow * this.IoCount + aColumn;

      public bool this[int aColumn, int aRow] { get => this.Matrix[this.GetIndex(aColumn, aRow)]; }

      internal IEnumerable<int[]> GetFeedbackLoops(int aColumn, int aRow)
      {
         var aFeedbackLoops = new List<int[]>();
         this.GetFeedbackLoops( aColumn, aRow, aColumn, new List<int>(), aFeedbackLoops);
         return aFeedbackLoops.ToArray();
      }

      private void GetFeedbackLoops(int aColumn, int aRow, int aCurrRow, List<int> aPath, List<int[]> aFeedbackLoops)
      {
         if(aPath.Contains(aCurrRow))
         {
            aFeedbackLoops.Add(aPath.ToArray());
         }
         else if(aRow == 0 && aColumn == 0)
         {
            // In=>Out
         }
         else
         {
            var aPathIdx = aPath.Count;
            aPath.Add(aCurrRow);
            try
            {
               for (var aCurrCol = 0; aCurrCol < this.IoCount; ++aCurrCol)
               {
                  if ((aRow == aCurrRow && aCurrCol == aColumn)
                  || this[aCurrCol, aCurrRow])
                  {
                     this.GetFeedbackLoops(aColumn, aRow, aCurrCol, aPath, aFeedbackLoops);
                  }
               }
            }
            finally
            {
               aPath.RemoveAt(aPathIdx);
            }
         }
      }

      private bool[] CalcEnables()
      {
         var aEnables = new bool[this.IoCount * this.IoCount];
         for(var aColIdx = 0; aColIdx < this.IoCount; ++aColIdx)
         {
            for(var aRowIdx = 0; aRowIdx < this.IoCount; ++aRowIdx)
            {               
               var aEnabled  = this.GetFeedbackLoops(aColIdx, aRowIdx).IsEmpty();
               var aCellIdx = this.GetIndex(aColIdx, aRowIdx);
               aEnables[aCellIdx] = aEnabled;
            }
         }
         return aEnables;
      }

      private bool[] EnablesM;
      public bool[] Enables
      {
         get
         {
            if(!(this.EnablesM is object))
            {
               this.EnablesM = this.CalcEnables();
            }
            return this.EnablesM;
         }
      }
      public IEnumerable<int> EnableInts { get => from aEnable in this.Enables select aEnable ? 1 : 0; }

      private Tuple<bool, bool[]> CalcCoercedPair()
      {
         var aCellCount = this.CellCount;
         var aCoerced = new bool[aCellCount];
         var aNeedsCoerce = false;
         var aEnables = this.Enables;
         var aMatrix = this.Matrix;
         for(var aCellIdx = 0; aCellIdx < aCellCount; ++aCellIdx)
         {
            if(aMatrix[aCellIdx]
            && !aEnables[aCellIdx])
            {
               aCoerced[aCellIdx] = false;
               aNeedsCoerce = true;
            }
            else
            {
               aCoerced[aCellIdx] = aMatrix[aCellIdx];
            }
         }
         return new Tuple<bool, bool[]>(aNeedsCoerce, aCoerced);
      }
      private Tuple<bool, bool[]> CoercedPairM;
      private Tuple<bool, bool[]> CoercedPair
      {
         get
         {
            if(!(this.CoercedPairM is object))
            {
               this.CoercedPairM = this.CalcCoercedPair();
            }
            return this.CoercedPairM;
         }         
      }

      public bool NeedsCoerce { get => this.CoercedPair.Item1; }
      public bool[] Coerced { get => this.CoercedPair.Item2; }

      #region Test
      private static bool IsEqual(int[] aLhs, int[] aRhs)
      {
         if (aLhs.Length == aRhs.Length)
         {
            for (var aIdx2 = 0; aIdx2 < aLhs.Length; ++aIdx2)
            {
               if (aLhs[aIdx2] != aRhs[aIdx2])
               {
                  return false;
               }
            }
            return true;
         }
         else
         {
            return false;
         }
      }

      private static void Test(string aTestId, int[] aActual, int[] aExpected, Action<string> aFail)
      {
         if(!IsEqual(aActual, aExpected))
         {
            aFail(aTestId);
         }
      }

      private static void Test(string aTestId, IEnumerable<int[]> aActual, IEnumerable<int[]> aExpected, Action<string> aFail)
      {
         if(aActual.Count() == aExpected.Count())
         {            
            var aCount = aActual.Count();
            for (var aIdx = 0; aIdx < aCount; ++aIdx)
            {
               var aLhs = aActual.ElementAt(aIdx);
               var aRhs = aExpected.ElementAt(aIdx);
               if (!IsEqual(aLhs, aRhs))
               {
                  aFail(aTestId);
                  return;
               }
            }
         }
         else
         {
            aFail(aTestId);
         }
      }
      public static void Test(Action<string> aFailTest)
      {
         Test("f02dd08f-8c78-4118-bf4b-680e08681ef9", new CFlowMatrix(5,
                                                                     0, 0, 0, 0, 0, 
                                                                     0, 0, 1, 1, 1, 
                                                                     0, 0, 0, 0, 1, 
                                                                     0, 0, 0, 0, 0, 
                                                                     0, 0, 0, 0, 0)
            .EnableInts.ToArray(), new int[] { 1, 1, 1, 1, 1, 
                                               1, 0, 1, 1, 1, 
                                               1, 0, 0, 1, 1, 
                                               1, 0, 1, 0, 1, 
                                               1, 0, 0, 1, 0 }, aFailTest);
      }
      #endregion


   }


   public sealed class CChannelStrip : CMaxObject
   {
      public CChannelStrip()
      {
         this.IntInlet = new CIntInlet(this)
         {
            Action = this.OnIntInlet
         };
         this.IntOutlet = new CIntOutlet(this);

         this.ListInlet = new CListInlet(this)
         {
            Action = this.OnListInlet
         };
         this.ListOutlet = new CListOutlet(this);

         this.MatrixInlet = new CMatrixInlet(this)
         {
            Action = this.OnMatrixInlet
         };
         this.MatrixOutlet = new CMatrixOutlet(this);

         /// this.LeftInlet.SingleItemListEnabled = true; TODO-TestMe
         this.LeftInlet.Support(CMessageTypeEnum.Symbol);
         this.LeftInlet.SetSymbolAction("clear_matrix", this.OnClearMatrix);
         this.LeftInlet.Support(CMessageTypeEnum.List);
         this.LeftInlet.SetPrefixedListAction("load_image", this.OnLoadImage);
      }

      public static void Test(Action<string> aFailTest)
      {
         CFlowMatrix.Test(aFailTest);
      }

      internal readonly CIntInlet IntInlet;
      internal readonly CIntOutlet IntOutlet;

      internal readonly CListInlet ListInlet;
      internal readonly CListOutlet ListOutlet;

      internal readonly CMatrixInlet MatrixInlet;
      internal readonly CMatrixOutlet MatrixOutlet;

      private void OnLoadImage(CInlet aInlet, string aSymbol, CReadonlyListData aParams)
      {
         var aFileInfo = new FileInfo(aParams.ElementAt(0).ToString().Replace("/", "\\"));
         this.MatrixOutlet.Message.Value.SetImage(Image.FromFile(aFileInfo.FullName)); 
         this.MatrixOutlet.Message.Value.PrintMatrixInfo(this, "ImageMatrix");
         this.MatrixOutlet.Send();
      }
       
      private void OnClearMatrix(CInlet aInlet, CSymbol aSymbol)
      {
         this.MatrixOutlet.Message.Value.Clear();
         this.MatrixOutlet.Send();
      }

      private void OnIntInlet(CInlet aInlet, CInt aInt)
      {
         this.IntOutlet.Message.Value = aInt.Value;
         this.IntOutlet.Send();
      }

      private void OnListInlet(CInlet aInlet, CList aList)
      {
         this.ListOutlet.Message.Value = aList.Value;
         this.ListOutlet.Send();
      }

      private void OnMatrixInlet(CInlet aInlet, CMatrix aMatrix)
      {
         this.MatrixOutlet.Message.Value = aMatrix.Value;
         this.MatrixOutlet.Send();
      }
   }
}
