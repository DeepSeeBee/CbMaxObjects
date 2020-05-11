using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbChannelStrip
{
   using System.Collections;
   using System.Data;
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

      internal int GetCellIdx(int aColumn, int aRow) => aRow * this.IoCount + aColumn;

      public bool this[int aColumn, int aRow] { get => this.Matrix[this.GetCellIdx(aColumn, aRow)]; }

      internal IEnumerable<int[]> GetFeedbackLoops(int aColumn, int aRow)
      {
         var aFeedbackLoops = new List<int[]>();
         this.GetFeedbackLoops( aColumn, aRow, aColumn, new List<int>(), aFeedbackLoops);
         return aFeedbackLoops.ToArray();
      }

      private void GetFeedbackLoops(int aColumn, int aRow, int aCurrRow, List<int> aPath, List<int[]> aFeedbackLoops)
      {
         for (var aCurrCol = 0; aCurrCol < this.IoCount; ++aCurrCol)
         {
            if(aCurrCol == 0)
            {
               // out[0] is main out.
               // => Mapping to out[0] always possible. 
            }
            else if ((aRow == aCurrRow && aCurrCol == aColumn)
            || this[aCurrCol, aCurrRow])
            {
               if (aPath.Contains(aCurrCol))
               {
                  aFeedbackLoops.Add(aPath.ToArray());
               }
               else
               {
                  aPath.Add(aCurrCol);
                  this.GetFeedbackLoops(aColumn, aCurrRow, aCurrCol, aPath, aFeedbackLoops);
                  aPath.RemoveAt(aPath.Count - 1);
               }
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
               var aCellIdx = this.GetCellIdx(aColIdx, aRowIdx);
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

      private int[][] JoinsM;
      public int[][] Joins
      {
         get
         {
            if(!(this.JoinsM is object))
            {
               var aJoinss = new int[this.IoCount][];
               for(var aCol = 0; aCol < this.IoCount; ++aCol)
               {
                  var aJoins = new List<int>(this.IoCount);
                  for (var aRow = 0; aRow < this.IoCount; ++aRow)
                  {
                     if (this[aCol, aRow])
                     {
                        aJoins.Add(aRow);
                     }
                  }
                  aJoinss[aCol] = aJoins.ToArray();
               }
               this.JoinsM = aJoinss;
            }
            return this.JoinsM;
         }
      }


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
      private CRoutings RoutingsM;
      public CRoutings Routings
      {
         get
         {
            if(!(this.RoutingsM is object))
            {
               this.RoutingsM = new CRoutings(this);
            }
            return this.RoutingsM;
         }
      }

   }


   internal abstract class CRoutingVisitor
   {

      public virtual void Visit(CRoutings aRoutings)
      {
         foreach(var aRouting in aRoutings)
         {
            aRouting.Accept(this);
         }
      }
      public abstract void Visit(CParalellRouting aParalellRouting);
      public abstract void Visit(CDirectRouting aDirectRouting);
      public abstract void Visit(CNullRouting aNullRouting);
   }


   internal sealed class CGraphWizDiagram : CRoutingVisitor, IEnumerable<string>
   {
      private List<string> Rows = new List<string>();
      public IEnumerator<string> GetEnumerator() => this.Rows.GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

      private int Indent;
      private void AddLine(string aCode) => this.Rows.Add(new string(' ', this.Indent) + aCode);

      public override void Visit(CRoutings aRoutings)
      {
         this.AddLine("diagram D");
         this.AddLine("{");
         ++this.Indent;
         base.Visit(aRoutings);
         --this.Indent;
         this.AddLine("}");
      }

      private string GetName(CRouting aRouting) => "R" + aRouting.InputIdx;

      private string GetInName(CRouting aRouting) => aRouting.InputIdx == 0 ? "in" : this.GetName(aRouting);
      private string GetOutName(CRouting aRouting) => aRouting.InputIdx == 0 ? "out" : this.GetName(aRouting);

      private void VisitNonNull(CNonNullRouting aRouting)
      {
         foreach(var aOutput in aRouting.Outputs)
         {
            this.AddLine(this.GetInName(aRouting) + " -> " + this.GetOutName(aOutput) + ";");
         }
         
      }

      public override void Visit(CParalellRouting aParalellRouting)
      {
         this.VisitNonNull(aParalellRouting);
      }

      public override void Visit(CDirectRouting aDirectRouting)
      {
         this.VisitNonNull(aDirectRouting);
      }

      public override void Visit(CNullRouting aNullRouting)
      {
      }

      #region Test
      private static void Test(string aId, string[] aCode, Action<string> aFailAction)
      {
         var aLinesText = aCode.JoinString(Environment.NewLine);

      }

      internal static void Test(Action<string> aFailAction)
      {
         Test("", new CFlowMatrix(7,
                                  0, 1, 1, 0, 0, 0, 0,
                                  0, 0, 0, 0, 0, 0, 1,
                                  0, 0, 0, 1, 1, 0, 0,
                                  0, 0, 0, 0, 0, 1, 0,
                                  0, 0, 0, 0, 0, 1, 0,
                                  0, 0, 0, 0, 0, 0, 1,
                                  1, 0, 0, 0, 0, 0, 0                                  
                                  ).Routings.GraphWizCode, aFailAction);
      }
      #endregion

   }

   public static class CStringExtensions
   {
      public static string JoinString(this IEnumerable<string> aStrings, string aLimiter)
      {
         var aStringBuilder = new StringBuilder();
         var aOpen = false;
         foreach(var aString in aStrings)
         {
            if (aOpen)
               aStringBuilder.Append(aLimiter);
            aStringBuilder.Append(aString);
            aOpen = true;
         }
         return aStringBuilder.ToString();
      }
   }

   internal sealed class CRoutings : IEnumerable<CRouting>
   {
      internal CRoutings(CFlowMatrix aFlowMatrix)
      {
         var aRoutings = new CRouting[aFlowMatrix.IoCount];
         for(var aRow = 0; aRow < aFlowMatrix.IoCount; ++aRow)
         {
            var aOutputs = new List<int>();
            for(var aCol = 0; aCol <  aFlowMatrix.IoCount; ++aCol)
            {
               if(aFlowMatrix.Coerced[aFlowMatrix.GetCellIdx(aCol, aRow)])
               {
                  aOutputs.Add(aCol);
               }
            }
            var aRouting = CRouting.New(this, aRow, aOutputs);
            aRoutings[aRow] = aRouting;         
         }
         this.Routings = aRoutings;
         this.FlowMatrix = aFlowMatrix;
      }

      private readonly CFlowMatrix FlowMatrix;

      private readonly CRouting[] Routings;

      public IEnumerator<CRouting> GetEnumerator() => this.Routings.AsEnumerable().GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

      private CRouting[][] JoinsM;
      internal CRouting[][] Joins
      {
         get
         {
            if (!(this.JoinsM is object))
            {
               var aJoins = new CRouting[this.FlowMatrix.IoCount][];
               for (var aRow = 0; aRow < this.FlowMatrix.IoCount; ++aRow)
               {
                  aJoins[aRow] = (from aIdx in this.FlowMatrix.Joins[aRow] select this.Routings[aIdx]).ToArray();
               }
               this.JoinsM = aJoins;
            }
            return this.JoinsM;
         }
      }

      private string[] GraphWizCodeM;
      public string[] GraphWizCode 
      { 
         get
         {
            if (!(this.GraphWizCodeM is object))
            {
               var aGraphWizDiagram = new CGraphWizDiagram();
               aGraphWizDiagram.Visit(this);
               this.GraphWizCodeM = aGraphWizDiagram.ToArray();
            }
            return this.GraphWizCodeM;
         }
      }

   }

   internal abstract class CRouting
   {

      internal CRouting(CRoutings aRoutings, int aInputIdx, int[] aOutputIdxs)
      {
         this.Routings = aRoutings;
         this.InputIdx = aInputIdx;
         this.OutputIdxs = aOutputIdxs;
      }
      internal readonly CRoutings Routings;
      internal readonly int InputIdx;
      internal readonly int[] OutputIdxs;

      private CRouting[] OutputsM;
      internal CRouting[] Outputs
      {
         get
         {
            if(!(this.OutputsM is object))
            {
               this.OutputsM = (from aIdx in Enumerable.Range(0, this.OutputIdxs.Length) select this.Routings.ElementAt(this.OutputIdxs[aIdx])).ToArray();
            }
            return this.OutputsM;
         }
      }

      internal CRouting[] Joins { get => this.Routings.Joins[this.InputIdx]; }

      public abstract void Accept(CRoutingVisitor aVisitor);

      internal static CRouting New(CRoutings aRoutings, int aInputIdx, IEnumerable<int> aOutputs)
      {
         if(aOutputs.IsEmpty())
         {
            return new CNullRouting(aRoutings, aInputIdx);
         }
         else if(aOutputs.ContainsOneElements())
         {
            return new CDirectRouting(aRoutings, aInputIdx, aOutputs.Single());
         }
         else
         {
            return new CParalellRouting(aRoutings, aInputIdx, aOutputs.ToArray());
         }
      }

      


   }

   internal sealed class CNullRouting : CRouting
   {
      internal CNullRouting(CRoutings aRoutings, int aInputIdx) : base(aRoutings, aInputIdx, new int[] { }) { }
      public override void Accept(CRoutingVisitor aVisitor) => aVisitor.Visit(this);
   }

   internal abstract class CNonNullRouting : CRouting
   {
      internal CNonNullRouting(CRoutings aRoutings, int aInputIdx, int[] aOutputIdx) : base(aRoutings, aInputIdx, aOutputIdx) { }

   }

   internal sealed class CParalellRouting : CNonNullRouting
   {
      internal CParalellRouting(CRoutings aRoutings, int aInputIdx, int[] aOutputIdx) : base(aRoutings, aInputIdx, aOutputIdx) { }
      public override void Accept(CRoutingVisitor aVisitor) => aVisitor.Visit(this);
   }

   internal sealed class CDirectRouting : CNonNullRouting
   {
      internal CDirectRouting(CRoutings aRoutings, int aInputIdx, int aOutputIdx):base(aRoutings, aInputIdx, new int[] { aOutputIdx }) { }
      public override void Accept(CRoutingVisitor aVisitor) => aVisitor.Visit(this);

      public CRouting Output { get => this.Outputs.Single(); }

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

      public static void Test(Action<string> aFailAction)
      {
         //CFlowMatrix.Test(aFailAction);
         CGraphWizDiagram.Test(aFailAction);
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
