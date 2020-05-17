﻿using CbChannelStrip.GraphWiz;
using CbMaxClrAdapter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbChannelStrip.Graph
{

   internal sealed class CFlowMatrix
   {
      internal readonly Action<string> DebugPrint;
      internal CFlowMatrix(Action<string> aDebugPrint, CGwDiagramLayout aSettings, Int32 aIoCount, params int[] aMatrix) : this(aDebugPrint, aSettings, aIoCount, (from aItem in aMatrix select aItem != 0).ToArray())
      {
         this.DebugPrint = aDebugPrint;
      }
      internal CFlowMatrix(Action<string> aDebugPrint, CGwDiagramLayout aSettings, Int32 aIoCount, params bool[] aMatrix)
      {
         this.DebugPrint = aDebugPrint;
         if (aMatrix.Length != aIoCount * aIoCount)
            throw new ArgumentException("Can not understand this list. Length must be IoCount^2.");
         this.Settings = aSettings;
         this.IoCount = aIoCount;
         this.Actives = aMatrix;
      }
      internal readonly CGwDiagramLayout Settings;
      internal readonly Int32 IoCount;
      internal Int32 CellCount { get => this.IoCount * this.IoCount; }
      internal readonly bool[] Actives;

      internal int GetCellIdx(int aColumn, int aRow) => aRow * this.IoCount + aColumn;

      public bool this[int aColumn, int aRow] { get => this.Actives[this.GetCellIdx(aColumn, aRow)]; }

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
         foreach (var aCurrCol in Enumerable.Range(0, this.IoCount))
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
         for (var aColIdx = 0; aColIdx < this.IoCount; ++aColIdx)
         {
            for (var aRowIdx = 0; aRowIdx < this.IoCount; ++aRowIdx)
            {
               var aCellIdx = this.GetCellIdx(aColIdx, aRowIdx);
               var aEnabled = this.GetFeedbackLoops(aColIdx, aRowIdx).IsEmpty();

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
            if (!(this.EnablesM is object))
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
         var aMatrix = this.Actives;
         for (var aCellIdx = 0; aCellIdx < aCellCount; ++aCellIdx)
         {
            if (aMatrix[aCellIdx]
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
            if (!(this.CoercedPairM is object))
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
            if (!(this.JoinsM is object))
            {
               var aJoinss = new int[this.IoCount][];
               for (var aCol = 0; aCol < this.IoCount; ++aCol)
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
         if (!IsEqual(aActual, aExpected))
         {
            aFail(aTestId);
         }
      }

      private static void Test(string aTestId, IEnumerable<int[]> aActual, IEnumerable<int[]> aExpected, Action<string> aFail)
      {
         if (aActual.Count() == aExpected.Count())
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
      private sealed class CTestDiagramLayout  : CGwDiagramLayout
      {
         internal CTestDiagramLayout() { }
      }
      public static void Test(Action<string> aFailTest, Action<string> aDebugPrint)
      {
         var aSettings = new CTestDiagramLayout();
         Test("c6090373-ca31-409c-968b-cc954900d29f", new CFlowMatrix(aDebugPrint, aSettings, 5,
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

         Test("4bb437c9-db94-49e1-bf85-e285aa2dc8e2", new CFlowMatrix(aDebugPrint, aSettings, 5,
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



         Test("c459dacd-ce57-4490-b44e-22f59a922179", new CFlowMatrix(aDebugPrint, aSettings, 5,
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

         Test("f02dd08f-8c78-4118-bf4b-680e08681ef9", new CFlowMatrix(aDebugPrint, aSettings, 5,
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
      private volatile CRoutings RoutingsM;
      public CRoutings Routings
      {
         get
         {
            if (!(this.RoutingsM is object))
            {
               this.RoutingsM = new CRoutings(this);
            }
            return this.RoutingsM;
         }
      }


      internal static CFlowMatrix NewTestFlowMatrix1(Action<string> aDebugPrint) => new CFlowMatrix(aDebugPrint, new CTestDiagramLayout(), 7,
                                                                      0, 1, 1, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 1,
                                                                      0, 0, 0, 1, 1, 0, 0,
                                                                      0, 0, 0, 0, 0, 1, 0,
                                                                      0, 0, 0, 0, 0, 1, 0,
                                                                      0, 0, 0, 0, 0, 0, 1,
                                                                      1, 0, 0, 0, 0, 0, 0
                                                                      );
      internal static CFlowMatrix NewTestFlowMatrix2(Action<string> aDebugPrint) => new CFlowMatrix(aDebugPrint, new CTestDiagramLayout(), 7,
                                                                1, 1, 0, 0, 0, 0, 0,
                                                                1, 0, 0, 0, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 0
                                                                );

      internal static CFlowMatrix NewTestFlowMatrix3(Action<string> aDebugPrint) => new CFlowMatrix(aDebugPrint, new CTestDiagramLayout(), 7,
                                                                1, 1, 0, 0, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 0
                                                                );
      internal static CFlowMatrix NewTestFlowMatrix4(Action<string> aDebugPrint) => new CFlowMatrix(aDebugPrint, new CTestDiagramLayout(), 7,
                                                                1, 1, 0, 0, 0, 1, 0,
                                                                1, 0, 0, 1, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 1,
                                                                1, 0, 0, 0, 0, 1, 0,
                                                                1, 0, 0, 0, 0, 0, 1,
                                                                1, 0, 0, 0, 0, 0, 0,
                                                                1, 0, 0, 0, 0, 0, 0
                                                                );
      internal static CFlowMatrix NewTestFlowMatrix5(Action<string> aDebugPrint) => new CFlowMatrix(aDebugPrint, new CTestDiagramLayout(), 7,
                                                                1, 1, 0, 0, 1, 1, 0,
                                                                1, 0, 0, 1, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 0,
                                                                0, 0, 0, 0, 0, 0, 0,
                                                                1, 0, 0, 0, 0, 0, 0
                                                                );
   }


   internal abstract class CRoutingVisitor
   {

      public virtual void Visit(CRoutings aRoutings)
      {
         foreach (var aRouting in aRoutings)
         {
            aRouting.Accept(this);
         }
      }
      public abstract void Visit(CParalellRouting aParalellRouting);
      public abstract void Visit(CDirectRouting aDirectRouting);
      public abstract void Visit(CNullRouting aNullRouting);
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

      internal readonly CRouting[] Routings;

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

      private volatile CGwDiagramBuilder GwDiagramBuilderM;
      internal CGwDiagramBuilder GwDiagramBuilder
      {
         get
         {
            if (!(this.GwDiagramBuilderM is object))
            {
               var aDiagram = new CGwDiagramBuilder(this.FlowMatrix.DebugPrint, this.FlowMatrix.Settings);
               aDiagram.Visit(this);
               this.GwDiagramBuilderM = aDiagram;
            }
            return this.GwDiagramBuilderM;
         }
      }

      internal enum CRoutingUseCase
      {
         In,
         Out,
         Channel
      }

      internal Tuple<CRoutingUseCase, CRouting> GetByName(string aName)
      {
         if (aName == CRouting.InName)
            return new Tuple<CRoutingUseCase, CRouting>(CRoutingUseCase.In, this.Routings[0]);
         else if (aName == CRouting.OutName)
            return new Tuple<CRoutingUseCase, CRouting>(CRoutingUseCase.Out, this.Routings[0]);
         else
            return new Tuple<CRoutingUseCase, CRouting>(CRoutingUseCase.Channel, this.Routings[int.Parse(aName.TrimStart(CRouting.ChannelNamePrefix))]);
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

      internal const char ChannelNamePrefix = 'C';
      internal const string InName = "in";
      internal const string OutName = "out";
      internal string Name { get => ChannelNamePrefix + this.InputIdx.ToString(); }
      internal string NameForInput { get => this.InputIdx == 0 ? InName : this.Name; }
      internal string NameForOutput { get => this.InputIdx == 0 ? OutName : this.Name; }

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
            if (!this.FinalOutputLatencyM.HasValue)
            {
               var aLatencies = (from aInput in this.Inputs
                                 where aInput.IsLinkedToInput
                                 where aInput.IsLinkedToOutput
                                 select aInput.OutputLatency);
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
            if (!this.InputLatencyM.HasValue)
            {
               if (this.InputIdx == 0)
               {
                  this.InputLatencyM = 0;
               }
               else if (!this.IsLinkedToOutput
                    || !this.IsLinkedToInput)
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
         if (aOutputs.IsEmpty())
         {
            return new CNullRouting(aRoutings, aInputIdx);
         }
         else if (aOutputs.ContainsOneElements())
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
            if (!this.IsLinkedToOutputM.HasValue)
            {
               this.IsLinkedToOutputM = this.InputIdx == 0
                                      ? true
                                      : (from aOutput in this.Outputs select aOutput.IsLinkedToOutput).Contains(true);
            }
            return this.IsLinkedToOutputM.Value;
         }
      }

      private bool? IsLinkedToInputM;
      internal bool IsLinkedToInput
      {
         get
         {
            if (!this.IsLinkedToInputM.HasValue)
            {
               this.IsLinkedToInputM = this.InputIdx == 0
                                     ? true
                                     : (from aInput in this.Inputs select aInput.IsLinkedToInput).Contains(true);
            }
            return this.IsLinkedToInputM.Value;
         }
      }

      private bool? IsLinkedToSomethingM;
      internal bool IsLinkedToSomething
      {
         get
         {
            if (!this.IsLinkedToSomethingM.HasValue)
            {
               this.IsLinkedToSomethingM = this.InputIdx == 0
                                         ? true
                                         : (!this.Inputs.IsEmpty() || !this.Outputs.IsEmpty());
            }
            return this.IsLinkedToSomethingM.Value;
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
      internal CDirectRouting(CRoutings aRoutings, int aInputIdx, int aOutputIdx) : base(aRoutings, aInputIdx, new int[] { aOutputIdx }) { }
      public override void Accept(CRoutingVisitor aVisitor) => aVisitor.Visit(this);

      public CRouting Output { get => this.Outputs.Single(); }

   }

}
