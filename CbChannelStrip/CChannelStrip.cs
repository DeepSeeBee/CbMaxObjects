using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbChannelStrip
{
   using System.Collections;
   using System.Data;
   using System.Data.Common;
   using System.Diagnostics;
   using System.Diagnostics.Contracts;
   using System.Drawing;
   using System.IO;
   using System.Security.AccessControl;
   using System.Windows.Media.Imaging;
   using CbMaxClrAdapter;
   using CbMaxClrAdapter.Jitter;


   internal sealed class CSettings
   {
      internal DirectoryInfo GraphWizInstallDir { get =>new DirectoryInfo(@"C:\Program Files (x86)\Graphviz2.38\"); }
   }


   internal sealed class CFlowMatrix
   {
      internal CFlowMatrix(CSettings aSettings, Int32 aIoCount, params int[] aMatrix) : this(aSettings, aIoCount, (from aItem in aMatrix select aItem != 0).ToArray())
      {
      }
      internal CFlowMatrix(CSettings aSettings, Int32 aIoCount, params bool[] aMatrix)
      {
         if (aMatrix.Length != aIoCount * aIoCount)
            throw new ArgumentException("Can not understand this list. Length must be IoCount^2.");
         this.Settings = aSettings;
         this.IoCount = aIoCount;
         this.Matrix = aMatrix;
      }
      internal static CFlowMatrix New(CSettings aSettings, IEnumerable<object> aSpec)
      {
         try
         {
            var aIoCount = Convert.ToInt32(aSpec.ElementAt(0));
            var aMatrix = from aItem in aSpec.Skip(1) select Convert.ToInt32(aItem);
            return new CFlowMatrix(aSettings, aIoCount, aMatrix.ToArray());
         }
         catch(Exception aExc)
         {
            throw new Exception("Can not understand this list. " + aExc.Message);
         }
      }

      internal readonly CSettings Settings;
      internal readonly Int32 IoCount;
      internal Int32 CellCount { get => this.IoCount * this.IoCount; }
      internal readonly bool[] Matrix;

      internal int GetCellIdx(int aColumn, int aRow) => aRow * this.IoCount + aColumn;

      public bool this[int aColumn, int aRow] { get => this.Matrix[this.GetCellIdx(aColumn, aRow)]; }

      internal IEnumerable<int[]> GetFeedbackLoops(int aColumn, int aRow)
      {
         var aFeedbackLoops = new List<int[]>();
         foreach (var aCurrRow in Enumerable.Range(0, this.IoCount))
         {
            this.GetFeedbackLoops(aColumn, aRow, aCurrRow, new List<int>(), aFeedbackLoops);
         }
         return aFeedbackLoops.ToArray();
      }

      private void GetFeedbackLoops(int aColumn, int aRow, int aCurrRow, List<int> aPath, List<int[]> aFeedbackLoops)
      {
         foreach(var aCurrCol in Enumerable.Range(0, this.IoCount))
         {
            if (aCurrCol == 0)
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
                  this.GetFeedbackLoops(aColumn, aRow, aCurrCol, aPath, aFeedbackLoops);
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
               var aCellIdx = this.GetCellIdx(aColIdx, aRowIdx);
               var aEnabled  = this.GetFeedbackLoops(aColIdx, aRowIdx).IsEmpty();
               
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
         var aSettings = new CSettings();

         Test("c6090373-ca31-409c-968b-cc954900d29f", new CFlowMatrix(aSettings, 5,
                                                                     0, 0, 0, 0, 0,
                                                                     0, 0, 0, 0, 0,
                                                                     0, 0, 0, 0, 0,
                                                                     0, 0, 0, 0, 0,
                                                                     0, 0, 0, 0, 0)
            .EnableInts.ToArray(), new int[] { 1, 1, 1, 1, 1,
                                               1, 0, 1, 1, 1,
                                               1, 1, 0, 1, 1,
                                               1, 1, 1, 0, 1,
                                               1, 1, 1, 1, 0 }, aFailTest);

         Test("4bb437c9-db94-49e1-bf85-e285aa2dc8e2", new CFlowMatrix(aSettings, 5,
                                                                     0, 1, 0, 0, 0,
                                                                     0, 0, 0, 0, 0,
                                                                     0, 0, 0, 0, 0,
                                                                     0, 0, 0, 0, 0,
                                                                     0, 0, 0, 0, 0)
            .EnableInts.ToArray(), new int[] { 1, 1, 1, 1, 1,
                                               1, 0, 1, 1, 1,
                                               1, 1, 0, 1, 1,
                                               1, 1, 1, 0, 1,
                                               1, 1, 1, 1, 0 }, aFailTest);



         Test("c459dacd-ce57-4490-b44e-22f59a922179", new CFlowMatrix(aSettings, 5,
                                                                     0, 1, 0, 0, 0,
                                                                     0, 0, 1, 0, 0,
                                                                     0, 0, 0, 0, 0,
                                                                     0, 0, 0, 0, 0,
                                                                     0, 0, 0, 0, 0)
            .EnableInts.ToArray(), new int[] { 1, 1, 1, 1, 1,
                                               1, 0, 1, 1, 1,
                                               1, 0, 0, 1, 1,
                                               1, 1, 1, 0, 1,
                                               1, 1, 1, 1, 0 }, aFailTest);

         Test("f02dd08f-8c78-4118-bf4b-680e08681ef9", new CFlowMatrix(aSettings, 5,
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
      internal CGraphWizDiagram(CSettings aSettings)
      {
         this.Settings = aSettings;
      }

      private CSettings Settings;

      private readonly List<string> Rows = new List<string>();
      public IEnumerator<string> GetEnumerator() => this.Rows.GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

      private int Indent;
      private void AddLine(string aCode) => this.Rows.Add(new string(' ', this.Indent) + aCode);

      private void AddLines(CRouting aRouting, string aName, int aLatency)
      {
         this.AddLine("\"" + aName + "\"" + "[" + "label" + "=" + "\"" + aName + " l=" + aLatency + "\"" + "]" + ";");
      }

      public override void Visit(CRoutings aRoutings)
      {
         this.AddLine("digraph G");
         this.AddLine("{");
         ++this.Indent;

         var aWithOutputs = from aTest in aRoutings
                            where aTest.InputIdx == 0
                               || aTest.IsLinkedToOutput 
                            select aTest;

         foreach(var aRouting in aWithOutputs)
         {
            if(aRouting.InputIdx == 0)
            {
               this.AddLines(aRouting, this.GetInName(aRouting), 0);
               this.AddLines(aRouting, this.GetOutName(aRouting), aRouting.FinalOutputLatency);
            }
            else
            {
               this.AddLines(aRouting, this.GetInName(aRouting), aRouting.OutputLatency); 
            }           
         }

         base.Visit(aRoutings);
         --this.Indent;
         this.AddLine("}");
      }
       
      private string GetName(CRouting aRouting) => "R" + aRouting.InputIdx;

      private string GetInName(CRouting aRouting) => aRouting.InputIdx == 0 ? "in" : this.GetName(aRouting);
      private string GetOutName(CRouting aRouting) => aRouting.InputIdx == 0 ? "out" : this.GetName(aRouting);

      private void VisitNonNull(CNonNullRouting aRouting)
      {
         if(aRouting.IsLinkedToOutput)
         {
            foreach (var aOutput in aRouting.Outputs)
            {
               if(aOutput.IsLinkedToOutput)
               {
                  this.AddLine(this.GetInName(aRouting) + " -> " + this.GetOutName(aOutput) + ";");
               }
            }
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
      private static void Test(string aId, CGraphWizDiagram aDiagram, Action<string> aFailAction)
      {
         aDiagram.Bitmap.Save(@"C:\Program Files\Cycling '74\Max 8\packages\max-sdk-8.0.3\source\charly_beck\CbChannelStrip\m4l\Test\graph.png");
         var aLinesText = aDiagram.JoinString(Environment.NewLine);

      }

      internal static void Test(Action<string> aFailAction)
      {
         var aSettings = new CSettings();
         Test("", new CFlowMatrix(aSettings, 7,
                                  0, 1, 1, 0, 0, 0, 0,
                                  0, 0, 0, 0, 0, 0, 1,
                                  0, 0, 0, 1, 1, 0, 0,
                                  0, 0, 0, 0, 0, 1, 0,
                                  0, 0, 0, 0, 0, 1, 0,
                                  0, 0, 0, 0, 0, 0, 1,
                                  1, 0, 0, 0, 0, 0, 0                                  
                                  ).Routings.GraphWizDiagram, aFailAction);
      }
      #endregion
      private void NewGraph(IEnumerable<string> aLines,
                              string aGraphoutputType,
                              Stream aStream)
      {
         var aErmGraphNodeVm = this;
         var aDotProcess = new ProcessStartInfo();
         var aInstallDir = this.Settings.GraphWizInstallDir;
         var aBinDir = new DirectoryInfo(Path.Combine(aInstallDir.FullName, "bin"));
         var aDir = aBinDir;
         aDotProcess.FileName = Path.Combine(aDir.FullName, "dot.exe");
         aDotProcess.Arguments = aGraphoutputType.IsEmpty() ? string.Empty : "-T" + aGraphoutputType;
         aDotProcess.RedirectStandardInput = true;
         aDotProcess.RedirectStandardOutput = true;
         aDotProcess.RedirectStandardError = true;
         aDotProcess.UseShellExecute = false;
         aDotProcess.WindowStyle = ProcessWindowStyle.Hidden;
         aDotProcess.CreateNoWindow = true;
         var aText = aLines.JoinString("\n");
         var aCompatibleText = aText;
         var aProcess = Process.Start(aDotProcess);
         aProcess.StandardInput.Write(aCompatibleText);
         aProcess.StandardInput.Close();
         var aReader = new BinaryReader(aProcess.StandardOutput.BaseStream);
         var aBuf = new byte[1024];
         var aByteCount = 0;
         do
         {
            aByteCount = aReader.Read(aBuf, 0, aBuf.Length);
            aStream.Write(aBuf, 0, aByteCount);
         }
         while (aByteCount != 0);
         var aError1 = aProcess.StandardError.ReadToEnd();
         var aError2 = aError1 is object ? aError1.ToString() : string.Empty;
         var aError = aError2;
         if (aError != string.Empty)
         {
            var aExc = new Exception(aError);
            throw aExc;
         }
      }

      private Bitmap NewBitmap(IEnumerable<string> aLines)
      {
         var aGraphoutputType = "bmp";
         var aImageStream = new MemoryStream();
         try
         {
            this.NewGraph(aLines, aGraphoutputType, aImageStream);
         }
         catch (Exception aExc)
         {
            throw new Exception("Could not create graph. " + aExc.Message, aExc);
         }
         aImageStream.Seek(0, SeekOrigin.Begin);
         var aBitmap = new Bitmap(aImageStream);
         return aBitmap;
      }

      private Bitmap BitmapM;
      public Bitmap Bitmap
      {
         get
         {
            if(!(this.BitmapM is object))
            {
               this.BitmapM = this.NewBitmap(this);
            }
            return this.BitmapM;
         }
      }
   }





   internal sealed class CRoutings : IEnumerable<CRouting>
   {
      internal CRoutings(CFlowMatrix aFlowMatrix)
      {
         var aRoutings = new CRouting[aFlowMatrix.IoCount];
         for (var aRow = 0; aRow < aFlowMatrix.IoCount; ++aRow)
         {
            var aOutputs = new List<int>();
            for (var aCol = 0; aCol < aFlowMatrix.IoCount; ++aCol)
            {
               if (aFlowMatrix.Coerced[aFlowMatrix.GetCellIdx(aCol, aRow)])
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

      private CGraphWizDiagram GraphWizDiagramM;
      internal CGraphWizDiagram GraphWizDiagram
      {
         get
         {
            if (!(this.GraphWizDiagramM is object))
            {
               var aDiagram = new CGraphWizDiagram(this.FlowMatrix.Settings);
               aDiagram.Visit(this);
               this.GraphWizDiagramM = aDiagram;
            }
            return this.GraphWizDiagramM;
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
            if (!(this.OutputsM is object))
            {
               this.OutputsM = (from aIdx in Enumerable.Range(0, this.OutputIdxs.Length) select this.Routings.ElementAt(this.OutputIdxs[aIdx])).ToArray();
            }
            return this.OutputsM;
         }
      }

      internal int NodeLatency { get => this.InputIdx; } // TODO.

      private IEnumerable<CRouting> InputsM;
      internal IEnumerable<CRouting> Inputs
      {
         get
         {
            if (!(this.InputsM is object))
            {
               this.InputsM = (from aTest in this.Routings where aTest.Outputs.Contains(this) select aTest).ToArray();
            }
            return this.InputsM;
         }
      }

      internal int? FinalOutputLatencyM;
      internal int FinalOutputLatency
      {
         get
         {
            if(!this.FinalOutputLatencyM.HasValue)
            {
               var aLatencies = (from aInput in this.Inputs select aInput.OutputLatency);
               var aLatency = aLatencies.IsEmpty() ? 0 : aLatencies.Max();
               this.FinalOutputLatencyM = aLatency;
            }
            return this.FinalOutputLatencyM.Value;
         }
      }

      private int? InputLatencyM;
      internal int InputLatency 
      {
         get
         {
            if(!this.InputLatencyM.HasValue)
            {
               if(this.InputIdx == 0)
               {
                  this.InputLatencyM = 0;
               }
               else
               {
                  this.InputLatencyM = this.FinalOutputLatency;
               }
            }
            return this.InputLatencyM.Value;
         }
      }
      internal int OutputLatency { get => this.InputLatency + this.NodeLatency; }

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

      private bool? IsLinkedToOutputM;
      internal bool IsLinkedToOutput
      {
         get
         {
            if(!this.IsLinkedToOutputM.HasValue)
            {
               this.IsLinkedToOutputM = this.InputIdx == 0
                                      ? true
                                      : (from aOutput in this.Outputs select aOutput.IsLinkedToOutput).Contains(true);
            }
            return this.IsLinkedToOutputM.Value;
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
         this.LeftInlet.Support(CMessageTypeEnum.Bang);   
         this.LeftInlet.Support(CMessageTypeEnum.List);
         this.LeftInlet.SetPrefixedListAction("init", this.OnInit);
         this.MatrixCtrlLeftOutIn = new CListInlet(this);
         this.MatrixCtrlLeftOutIn.Action = this.OnMatrixCtrlLeftOutIn;
         this.MatrixCtrlRightOutIn = new CListInlet(this);
         this.MatrixCtrlRightOutIn.Action = this.OnMatrixCtrlRightOutIn;
         this.MatrixCtrlLeftInOut = new CListOutlet(this);
         this.GraphBitmapOut = new CMatrixOutlet(this);
      }

      private readonly CListInlet MatrixCtrlLeftOutIn;
      private readonly CListInlet MatrixCtrlRightOutIn;

      private readonly CListOutlet MatrixCtrlLeftInOut;
      private readonly CMatrixOutlet GraphBitmapOut;

      private Int32 IoCount;
      private bool RequestRowsPending;
      private Int32 RequestRowIdx;
      private Int32[][] Rows;
      private CFlowMatrix FlowMatrix;

      private void OnInit(CInlet aInlet, string aFirstItem, CReadonlyListData aParams)
      {
         var aIoCount = Convert.ToInt32(aParams.ElementAt(0));         
         var aRows = new Int32[aIoCount][];
         for (var aIdx = 0; aIdx < aIoCount; ++aIdx)
         {
            aRows[aIdx] = new int[aIoCount];
         }
         this.Rows = aRows;
         this.IoCount = aIoCount;
         this.RequestRows();         
      }
      
      private void RequestRows()
      {
         if(this.RequestRowsPending)
         {
            throw new InvalidOperationException();
         }
         else
         {
            this.RequestRowsPending = true;
            try
            {
               for (var aRow = 0; aRow < this.IoCount; ++aRow)
               {
                  this.RequestRowIdx = aRow;
                  this.MatrixCtrlLeftInOut.Message.Value.Clear();
                  this.MatrixCtrlLeftInOut.Message.Value.Add("getrow");
                  this.MatrixCtrlLeftInOut.Message.Value.Add(aRow);
                  this.MatrixCtrlLeftInOut.Send();
               }
            }
            finally
            {
               this.RequestRowsPending = false;
            }
            this.UpdateMatrix();
         }
      }

      private readonly CSettings Settings = new CSettings();

      private void UpdateMatrix()
      {
         var aMatrix = (from aRow in this.Rows from aCell in aRow select aCell).ToArray();         
         this.FlowMatrix = new CFlowMatrix(this.Settings, this.IoCount, aMatrix);

         //this.WriteLogInfoMessage("Matrix", aMatrix.Cast<object>());
         //this.WriteLogInfoMessage("Enables", this.FlowMatrix.EnableInts.Cast<object>());

         foreach (var aRowIdx in Enumerable.Range(0, this.FlowMatrix.IoCount))
         {
            foreach (var aColIdx in Enumerable.Range(0, this.FlowMatrix.IoCount))
            {
               var aEnabled = this.FlowMatrix.Enables[this.FlowMatrix.GetCellIdx(aColIdx, aRowIdx)];
               this.MatrixCtrlLeftInOut.Message.Value.Clear();
               this.MatrixCtrlLeftInOut.Message.Value.Add(aEnabled ? "enablecell" : "disablecell");               
               this.MatrixCtrlLeftInOut.Message.Value.Add(aColIdx);
               this.MatrixCtrlLeftInOut.Message.Value.Add(aRowIdx);
               this.MatrixCtrlLeftInOut.Send();
            }
         }
         this.GraphBitmapOut.Message.Value.SetImage(this.FlowMatrix.Routings.GraphWizDiagram.Bitmap);
         this.GraphBitmapOut.Send();
      }

      private void OnMatrixCtrlRightOutIn(CInlet aInlet, CList aList)
      {
         if(this.RequestRowsPending)
         {
            for(var aY = 0; aY < this.IoCount; ++aY)
            {
               this.Rows[this.RequestRowIdx][aY] = Convert.ToInt32(aList.Value.ElementAt(aY));
            }
         }
      }

      private void OnMatrixCtrlLeftOutIn(CInlet aInlet, CList aList)
      {
         var aX = Convert.ToInt32(aList.Value.ElementAt(0));
         var aY = Convert.ToInt32(aList.Value.ElementAt(1));
         var aCellState = Convert.ToInt32(aList.Value.ElementAt(2));
         this.Rows[aY][aX] = aCellState;
         this.UpdateMatrix();
      }
      public static void Test(Action<string> aFailAction)
      {
         CFlowMatrix.Test(aFailAction);
         CGraphWizDiagram.Test(aFailAction);
      }

   }




   public sealed class CTestObject : CMaxObject
   {
      public CTestObject()
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
