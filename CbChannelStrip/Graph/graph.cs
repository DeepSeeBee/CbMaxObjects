using CbVirtualMixerMatrix.GraphViz;
using CbChannelStripTest;
using CbMaxClrAdapter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbVirtualMixerMatrix.Graph
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

      private static void Test(string aTestId, bool aOk, Action<string> aFail)
      {
         if(!aOk)
         {
            aFail(aTestId);
         }
      }

      private static void Test(string aTestId, int[] aActual, int[] aExpected, Action<string> aFail)
      {
         Test(aTestId, IsEqual(aActual, aExpected), aFail);
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
      public static void Test(Action<string> aFailAction, Action<string> aDebugPrint)
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
                                               1, 1, 1, 1, 0 }, aFailAction);

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
                                               1, 1, 1, 1, 0 }, aFailAction);



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
                                               1, 1, 1, 1, 0 }, aFailAction);

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
                                               1, 0, 0, 1, 0 }, aFailAction);




         { // TestLatencyOfMainOut
            var aFlowMatrix = new CFlowMatrix(aDebugPrint, aSettings, 3,
                                                                      0, 1, 0,
                                                                      1, 0, 0,
                                                                      0, 0, 0);
            var aL1 = 2;
            var aL2a = 3;
            var aL2b = 4;
            aFlowMatrix.Channels.ElementAt(0).NodeLatency = aL1;
            aFlowMatrix.Channels.ElementAt(1).NodeLatency = aL2a;
            Test("a68f0ada-eee1-45f3-a8f1-8ac456e75443", aFlowMatrix.Channels.Channels[0].OutLatency == aL1 + aL2a, aFailAction);
            aFlowMatrix.Channels.ElementAt(1).NodeLatency = aL2b;
            Test("26612543-fef0-4249-985e-c6ae523186d8", aFlowMatrix.Channels.Channels[0].OutLatency == aL1 + aL2b, aFailAction);
         }

         { // TestIsLinkedToSomething
            var aFlowMatrix = new CFlowMatrix(aDebugPrint, aSettings, 3,
                                                                      0, 1, 0,
                                                                      1, 0, 0,
                                                                      0, 0, 0);
            Test("64581276-03f6-44b0-8c38-4ea3d768830e", aFlowMatrix.Channels.ElementAt(0).IsLinkedToSomething, aFailAction);            
            Test("6a87894b-5f1c-4415-bc94-2a105b2c4e7c", aFlowMatrix.Channels.ElementAt(1).IsLinkedToSomething, aFailAction);
            Test("88967060-5308-4d16-ad70-c7da4dec9d6f", !aFlowMatrix.Channels.ElementAt(2).IsLinkedDirectlyToMainOut, aFailAction);
            Test("97a59359-63fe-4849-99ca-3e2d406d56f4", !aFlowMatrix.Channels.ElementAt(2).IsLinkedToSomething, aFailAction);
         }

         { // TestLatencyCompensation
            // TestData\b2c37369-f79a-475d-847d-d8a10cb4e26a.jpg
            var aFlowMatrix = new CFlowMatrix(aDebugPrint, aSettings, 5,
                                                                      0, 1, 1, 0, 1,
                                                                      0, 0, 0, 1, 0,
                                                                      0, 0, 0, 1, 0,
                                                                      1, 0, 0, 0, 0,
                                                                      0, 0, 1, 0, 0
                                                                      );
            aFlowMatrix.Channels.ElementAt(0).NodeLatency = 0;
            aFlowMatrix.Channels.ElementAt(1).NodeLatency = 1;
            aFlowMatrix.Channels.ElementAt(2).NodeLatency = 2;
            aFlowMatrix.Channels.ElementAt(3).NodeLatency = 3;
            aFlowMatrix.Channels.ElementAt(4).NodeLatency = 4;            
            Test("8f00ccd1-b43b-42d7-9d7c-b93ebff250e7", aFlowMatrix.Channels.ElementAt(1).LatencyCompensations.Length == 1, aFailAction);
            Test("b7b21b14-6409-43f9-9366-d24e7c8ccd0e", aFlowMatrix.Channels.ElementAt(1).LatencyCompensations[0] == 0, aFailAction);
            Test("4283e8e7-c401-4d2b-9be3-a8016b5b4454", aFlowMatrix.Channels.ElementAt(2).LatencyCompensations.Length == 2, aFailAction);
            Test("229b3844-37c4-4c40-ae90-10445e38f103", aFlowMatrix.Channels.ElementAt(2).LatencyCompensations[0] == 4, aFailAction);
            Test("adc0407c-ed90-4395-9315-e4554b805a66", aFlowMatrix.Channels.ElementAt(2).LatencyCompensations[1] == 0, aFailAction);
            Test("a5400cd6-1fa4-433b-97a0-f99fa3f7a59f", aFlowMatrix.Channels.ElementAt(3).LatencyCompensations.Length == 2, aFailAction);
            Test("0891c1f8-dd81-433e-ac38-50ab71f1b01e", aFlowMatrix.Channels.ElementAt(3).LatencyCompensations[0] == 5, aFailAction);
            Test("ef2ceac4-c2a2-4016-9003-a15b5a08ab43", aFlowMatrix.Channels.ElementAt(3).LatencyCompensations[1] == 0, aFailAction);
            Test("c0cbac5c-b4ba-40ea-b399-f1c8ce559eb1", aFlowMatrix.Channels.ElementAt(4).LatencyCompensations.Length == 1, aFailAction);
            Test("3b5d1781-7ce2-43ba-a2cd-ee0055ad4883", aFlowMatrix.Channels.ElementAt(4).LatencyCompensations[0] == 0, aFailAction);
            Test("3c4f2ccd-a36c-47cc-96f4-2b241687382f", aFlowMatrix.Channels.ElementAt(0).LatencyCompensations.Length == 1, aFailAction);
            Test("c2c2b99a-3a6c-452a-9da7-b808d55046b2", aFlowMatrix.Channels.ElementAt(0).LatencyCompensations[0] == 0, aFailAction);
         }

         { // TestLatencyCompenstation2
            // TestData\71521865-e4de-48cb-9c60-3cf61c3878a8.JPG
            var aFlowMatrix = new CFlowMatrix(aDebugPrint, aSettings, 3,
                                                          0, 1, 1, 
                                                          1, 0, 0,
                                                          1, 1, 0
                                                          );
            aFlowMatrix.Channels.ElementAt(0).NodeLatency = 0;
            aFlowMatrix.Channels.ElementAt(1).NodeLatency = 1;
            aFlowMatrix.Channels.ElementAt(2).NodeLatency = 2;
            Test("", aFlowMatrix.Channels.ElementAt(1).InputIoIdxs.Count() == 2, aFailAction);
            Test("", aFlowMatrix.Channels.ElementAt(1).InputIoIdxs.ElementAt(0) == 0, aFailAction);
            Test("", aFlowMatrix.Channels.ElementAt(1).InputIoIdxs.ElementAt(1) == 2, aFailAction);
            Test("", aFlowMatrix.Channels.ElementAt(1).LatencyCompensations[0] == 2, aFailAction);
            Test("", aFlowMatrix.Channels.ElementAt(1).LatencyCompensations[1] == 0, aFailAction);
            Test("", aFlowMatrix.Channels.ElementAt(2).InputIoIdxs.Count() == 1, aFailAction);
            Test("", aFlowMatrix.Channels.ElementAt(2).InputIoIdxs.ElementAt(0) == 0, aFailAction);
            Test("", aFlowMatrix.Channels.ElementAt(2).LatencyCompensations[0] == 0, aFailAction);

            Test("", aFlowMatrix.Channels.ElementAt(0).InputIoIdxs.Count() == 2, aFailAction);
            Test("", aFlowMatrix.Channels.ElementAt(0).InputIoIdxs.ElementAt(0) == 1, aFailAction);
            Test("", aFlowMatrix.Channels.ElementAt(0).InputIoIdxs.ElementAt(1) == 2, aFailAction);
            Test("", aFlowMatrix.Channels.ElementAt(0).LatencyCompensations[0] == 0, aFailAction);
            Test("", aFlowMatrix.Channels.ElementAt(0).LatencyCompensations[1] == 1, aFailAction);

         }

         {

            var aFlowMatrix = new CFlowMatrix(aDebugPrint, aSettings, 11,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
                                                                      );
            var aGraph = aFlowMatrix.Channels.GwDiagramBuilder.GwGraph;


         }
         {
            var aFlowMatrix = new CFlowMatrix(aDebugPrint, aSettings, 11,
                                                                      0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
                                                                      );
            var aGraph = aFlowMatrix.Channels.GwDiagramBuilder.GwGraph;
         }

         {
            var aFlowMatrix = new CFlowMatrix(aDebugPrint, aSettings, 11,
                                                                      0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0
                                                                      );
            var aGraph = aFlowMatrix.Channels.GwDiagramBuilder.GwGraph;
         }
         {
            var aFlowMatrix = new CFlowMatrix(aDebugPrint, aSettings, 5,
                                                          0, 1, 1, 0, 1,
                                                          0, 0, 0, 1, 0,
                                                          0, 0, 0, 1, 0,
                                                          1, 0, 0, 0, 0,
                                                          0, 0, 1, 0, 0
                                                          );
            var aGraph = aFlowMatrix.Channels.GwDiagramBuilder.GwGraph;
         }
         { // SaveReferenceBitmap
            var aFlowMatrix = new CFlowMatrix(aDebugPrint, aSettings, 11,
                                                                      0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                                                                      0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
                                                                      );
            var aGraph = aFlowMatrix.Channels.GwDiagramBuilder.GwGraph;
            aFlowMatrix.Channels.GwDiagramBuilder.Bitmap.Save(@"C:\Program Files\Cycling '74\Max 8\packages\max-sdk-8.0.3\source\charly_beck\CbChannelStrip\m4l\Test\graph.png");
         }

      }
      #endregion
      private volatile CChannels ChannelsM;
      public CChannels Channels
      {
         get
         {
            if (!(this.ChannelsM is object))
            {
               this.ChannelsM = new CChannels(this);
            }
            return this.ChannelsM;
         }
      }

      internal int SampleRate;

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


   internal abstract class CChannelVisitor
   {

      public virtual void Visit(CChannels aChannels)
      {
         foreach (var aChannel in aChannels)
         {
            aChannel.Accept(this);
         }
      }
      public abstract void Visit(CParalellChannel aParalellChannel);
      public abstract void Visit(CDirectChannel aDirectChannel);
      public abstract void Visit(CNullChannel aNullChannel);
      public abstract void Visit(CMainOut aMainOut);
   }

   internal sealed class CChannels : IEnumerable<CChannel>
   {
      internal CChannels(CFlowMatrix aFlowMatrix)
      {
         var aChannels = new CChannel[aFlowMatrix.IoCount];
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
            var aChannel = CChannel.New(this, aRow, aOutputs);
            aChannels[aRow] = aChannel;
         }
         var aMainOut = new CMainOut(this);
         this.Channels = aChannels;
         this.MainOut = aMainOut;
         this.FlowMatrix = aFlowMatrix;
      }

      internal readonly CFlowMatrix FlowMatrix;

      internal readonly CChannel[] Channels;

      internal readonly CChannel MainOut;

      public IEnumerator<CChannel> GetEnumerator() => this.Channels.AsEnumerable().GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

      private CChannel[][] JoinsM;
      internal CChannel[][] Joins
      {
         get
         {
            if (!(this.JoinsM is object))
            {
               var aJoins = new CChannel[this.FlowMatrix.IoCount][];
               for (var aRow = 0; aRow < this.FlowMatrix.IoCount; ++aRow)
               {
                  aJoins[aRow] = (from aIdx in this.FlowMatrix.Joins[aRow] select this.Channels[aIdx]).ToArray();
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

      internal enum CChannelUseCase
      {
         In,
         Out,
         Channel
      }

      internal Tuple<CChannelUseCase, CChannel> GetByName(string aName)
      {
         if (aName == CChannel.InName)
            return new Tuple<CChannelUseCase, CChannel>(CChannelUseCase.In, this.Channels[0]);
         else if (aName == CChannel.OutName)
            return new Tuple<CChannelUseCase, CChannel>(CChannelUseCase.Out, this.Channels[0]);
         else
            return new Tuple<CChannelUseCase, CChannel>(CChannelUseCase.Channel, this.Channels[int.Parse(aName.TrimStart(CChannel.ChannelNamePrefix))]);
      }

   }

   internal sealed class CMainOut : CChannel
   {
      internal CMainOut(CChannels aChannels):base(aChannels, 0, new int[] { })
      {
      }

      internal override int NodeLatency { get => this.Channels.Channels[0].NodeLatency; set => throw new InvalidOperationException(); }

      public override void Accept(CChannelVisitor aVisitor) { aVisitor.Visit(this); } // Not needed atm.

      internal override bool IsMainOut => true;

   }

   internal abstract class CChannel
   {

      internal CChannel(CChannels aChannels, int aInputIdx, int[] aOutputIdxs)
      {
         this.Channels = aChannels;
         this.IoIdx = aInputIdx;
         this.OutputIdxs = aOutputIdxs;
      }
      internal readonly CChannels Channels;
      internal readonly int IoIdx;
      internal readonly int[] OutputIdxs;
      internal IEnumerable<CChannel> Outputs { get => this.OutputsWithMainOut; }

      private IEnumerable<CChannel> OutputsWithMainOut
      {
         get
         {
            foreach (var aOutput in this.OutputsWithoutMainOut)
               yield return aOutput;
            if (this.IsLinkedDirectlyToMainOut)
               yield return this.Channels.MainOut;
         }
      }

      #region Depth
      private int? InputDepthM;
      internal int InputDepth
      {
         get
         {
            if (!this.InputDepthM.HasValue)
            {
               if (this.IoIdx == 0)
               {
                  this.InputDepthM = this.InternalInputDepth;
               }
               else if (!this.IsLinkedToOutput
                    || !this.IsLinkedToInput)
               {
                  this.InputDepthM = 0;
               }
               else
               {
                  this.InputDepthM = this.InternalInputDepth;
               }
            }
            return this.InputDepthM.Value;
         }
      }
      internal int NodeDepth { get => 1; }
      private int InternalOutDepth { get => this.InputDepth + this.NodeDepth; }
      internal int? InternalInputDepthM;
      internal int InternalInputDepth
      {
         get
         {
            if (!this.InternalInputDepthM.HasValue)
            {
               var aDepths = (from aInput in this.Inputs
                                 where aInput.IoIdx != 0
                                 where aInput.IsLinkedToInput
                                 where aInput.IsLinkedToOutput
                                 select aInput.InternalOutDepth);
               var aDepth = aDepths.IsEmpty() ? 0 : aDepths.Max();
               this.InternalInputDepthM = aDepth;
            }
            return this.InternalInputDepthM.Value;
         }
      }
      internal int OutDepth { get => this.IoIdx == 0 ? this.Channels.MainOut.InternalOutDepth : this.InternalOutDepth; }
      internal int[] DepthCompensations { get => (from aInput in this.Inputs select -((aInput.IoIdx == 0 ? 0 : aInput.OutDepth) - this.InputDepth)).ToArray(); }
      #endregion

      internal bool IsLinkedDirectlyToMainOut { get => this.Channels.FlowMatrix.Actives[this.Channels.FlowMatrix.GetCellIdx(0, this.IoIdx)]; }

      private CChannel[] OutputsM;
      internal CChannel[] OutputsWithoutMainOut
      {
         get
         {
            if (!(this.OutputsM is object))
            {
               this.OutputsM = (from aIdx in Enumerable.Range(0, this.OutputIdxs.Length) select this.Channels.ElementAt(this.OutputIdxs[aIdx])).ToArray();
            }
            return this.OutputsM;
         }
      }

      internal const char ChannelNamePrefix = 'C';
      internal const string InName = "in";
      internal const string OutName = "out";
      internal string Name { get => ChannelNamePrefix + this.IoIdx.ToString(); }
      internal string NameForInput { get => this.IoIdx == 0 ? InName : this.Name; }
      internal string NameForOutput { get => this.IoIdx == 0 ? OutName : this.Name; }

      private int NodeLatencyM;
      internal virtual int NodeLatency 
      { 
         get => this.NodeLatencyM; 
         set
         {
            if(this.NodeLatencyM != value)
            {
               this.NodeLatencyM = value;
               this.RefreshOutputLatency();
            }
         }
      }

      internal IEnumerable<CChannel> NewInputs() => (from aTest in this.Channels where aTest.OutputsWithMainOut.Contains(this) select aTest).ToArray();

      private IEnumerable<CChannel> InputsM;
      internal IEnumerable<CChannel> Inputs
      {
         get
         {
            if (!(this.InputsM is object))
            {
               this.InputsM = this.NewInputs();
            }
            return this.InputsM;
         }
      }
      internal IEnumerable<int> InputIoIdxs { get => from aInput in this.Inputs select aInput.IoIdx; }
      internal int GetInputIdxByIoIdx(int aIoIdx) => (from aIdx in Enumerable.Range(0, this.Inputs.Count())
                                                      where this.Inputs.ElementAt(aIdx).IoIdx == aIoIdx
                                                      select aIdx).Single();

      internal int? InternalInputLatencyM;
      internal int InternalInputLatency
      {
         get
         {
            if (!this.InternalInputLatencyM.HasValue)
            {
               var aLatencies = (from aInput in this.Inputs
                                 where aInput.IoIdx != 0
                                 where aInput.IsLinkedToInput
                                 where aInput.IsLinkedToOutput
                                 select aInput.InternalOutLatency);
               var aLatency = aLatencies.IsEmpty() ? 0 : aLatencies.Max();
               this.InternalInputLatencyM = aLatency;
            }
            return this.InternalInputLatencyM.Value;
         }
      }
      private void RefreshOutputLatency()
      {         
         if (this.IoIdx != 0)
         {            
            foreach (var aOutput in this.OutputsWithMainOut)
            {
               aOutput.RefreshInputLatency();
            }
         }
      }
      internal void RefreshInputLatency()
      {
         this.InputLatencyM = default(int?);
         this.InternalInputLatencyM = default(int?);
         if (this.IoIdx != 0)
         {            
            this.RefreshOutputLatency();
         }
      }

      private int? InputLatencyM;
      internal int InputLatency
      {
         get
         {
            if (!this.InputLatencyM.HasValue)
            {
               if (this.IoIdx == 0)
               {
                  this.InputLatencyM = this.InternalInputLatency; 
               }
               else if (!this.IsLinkedToOutput
                    || !this.IsLinkedToInput)
               {
                  this.InputLatencyM = 0;
               }
               else
               {
                  this.InputLatencyM = this.InternalInputLatency;
               }
            }
            return this.InputLatencyM.Value;
         }
      }
      private int InternalOutLatency { get => this.InputLatency + this.NodeLatency; }

      internal int OutLatency { get => this.IoIdx == 0 ? this.Channels.MainOut.InternalOutLatency : this.InternalOutLatency; }


      internal int[] LatencyCompensations { get => (from aInput in this.Inputs select - ((aInput.IoIdx == 0 ? 0 : aInput.OutLatency) - this.InputLatency)).ToArray(); }

      internal CChannel[] Joins { get => this.Channels.Joins[this.IoIdx]; }

      public abstract void Accept(CChannelVisitor aVisitor);

      internal static CChannel New(CChannels aChannels, int aInputIdx, IEnumerable<int> aOutputs)
      {
         if (aOutputs.IsEmpty())
         {
            return new CNullChannel(aChannels, aInputIdx);
         }
         else if (aOutputs.ContainsOneElements())
         {
            return new CDirectChannel(aChannels, aInputIdx, aOutputs.Single());
         }
         else
         {
            return new CParalellChannel(aChannels, aInputIdx, aOutputs.ToArray());
         }
      }

      private bool? IsLinkedToOutputM;
      internal bool IsLinkedToOutput
      {
         get
         {
            if (!this.IsLinkedToOutputM.HasValue)
            {
               this.IsLinkedToOutputM = this.IoIdx == 0
                                      ? true
                                      : (from aOutput in this.OutputsWithMainOut select aOutput.IsLinkedToOutput).Contains(true);
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
               this.IsLinkedToInputM = this.IoIdx == 0
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
               this.IsLinkedToSomethingM = this.IoIdx == 0
                                         ? true
                                         : (!this.Inputs.IsEmpty() || !this.OutputsWithMainOut.IsEmpty());
            }
            return this.IsLinkedToSomethingM.Value;
         }
      }

      internal virtual bool IsMainOut { get => false; }


   }

   internal sealed class CNullChannel : CChannel
   {
      internal CNullChannel(CChannels aChannels, int aInputIdx) : base(aChannels, aInputIdx, new int[] { }) { }
      public override void Accept(CChannelVisitor aVisitor) => aVisitor.Visit(this);
   }

   internal abstract class CNonNullChannel : CChannel
   {
      internal CNonNullChannel(CChannels aChannels, int aInputIdx, int[] aOutputIdx) : base(aChannels, aInputIdx, aOutputIdx) { }

   }

   internal sealed class CParalellChannel : CNonNullChannel
   {
      internal CParalellChannel(CChannels aChannels, int aInputIdx, int[] aOutputIdx) : base(aChannels, aInputIdx, aOutputIdx) { }
      public override void Accept(CChannelVisitor aVisitor) => aVisitor.Visit(this);
   }

   internal sealed class CDirectChannel : CNonNullChannel
   {
      internal CDirectChannel(CChannels aChannels, int aInputIdx, int aOutputIdx) : base(aChannels, aInputIdx, new int[] { aOutputIdx }) { }
      public override void Accept(CChannelVisitor aVisitor) => aVisitor.Visit(this);

      public CChannel Output { get => this.Outputs.Single(); }

   }

}
