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
   using CbChannelStrip.Graph;
   using CbChannelStrip.GaAnimator;
   using CbChannelStrip.GraphWiz;
   using CbMaxClrAdapter;
   using CbMaxClrAdapter.Jitter;
   using CbMaxClrAdapter.MGraphics;
   using System.Data.SqlClient;
   using System.ComponentModel;
   using System.Threading;
   using CbChannelStripTest;

   internal abstract class CGwDiagramLayout
   {
      internal DirectoryInfo GraphWizInstallDir { get =>new DirectoryInfo(@"C:\Program Files (x86)\Graphviz2.38\"); }
      internal virtual bool GetIncludeInDiagram(CRouting aRouting) => aRouting.IsLinkedToSomething;
   }


   internal sealed class CCsWorkerResult : CGaWorkerResult
   {
      internal CCsWorkerResult(CChannelStrip aChannelStrip, BackgroundWorker aBackgroundWorker, CCsState aNewState) : base(aBackgroundWorker, aNewState)
      {
         this.ChannelStrip = aChannelStrip;
         this.NewState = aNewState;
      }
      internal readonly new CCsState NewState;
      internal readonly CChannelStrip ChannelStrip;
      internal override void ReceiveResult()
      {
         base.ReceiveResult();
         this.ChannelStrip.ChangeState(this.NewState);
      }
   }

   internal sealed class CCsState : CGaState
   {
      internal CCsState(CGaAnimator aGaAnimator, CChannelStrip aChannelStrip, CFlowMatrix aFlowMatrix) : base(aGaAnimator)
      {
         this.ChannelStrip = aChannelStrip;
         this.FlowMatrix = aFlowMatrix;         
         this.Init();
      }
      internal CCsState(CGaAnimator aGaAnimator, CChannelStrip aChannelStrip, CCsState aOldState, CFlowMatrix aFlowMatrix):base(aGaAnimator, aOldState)
      {
         this.ChannelStrip = aChannelStrip;
         this.FlowMatrix = aFlowMatrix;
         this.Init();
      }

      internal readonly CFlowMatrix FlowMatrix;
      internal readonly CChannelStrip ChannelStrip;

      internal override CGwGraph GwGraph => this.FlowMatrix.Routings.GwDiagramBuilder.GwGraph;
      internal override bool GetIsFocused(CGaShape aShape)
      {
         var aFocusedConnector = this.ChannelStrip.Connectors.FocusedConnector;
         if (aShape is CGaNode
         && aFocusedConnector is object)
         {          
            var aRouting = aFocusedConnector.Routing;
            var aFocused = aShape.Name == aRouting.NameForInput
                        || aShape.Name == aRouting.NameForOutput
                         ;
            return aFocused;
         }
         return base.GetIsFocused(aShape);
      }
   }

   internal sealed class CCsWorkerArgs : CGaWorkerArgs
   {
      internal CCsWorkerArgs(CChannelStrip aChannelStrip, CCsState aOldState) : base(aOldState) 
      { 
         this.ChannelStrip = aChannelStrip;
         this.OldState = aOldState;
         this.NewMatrix = (from aRow in aChannelStrip.Rows from aCell in aRow select aCell).ToArray();
         this.DiagramLayout = this.ChannelStrip.NewDiagramLayout();
         this.IoCount = aChannelStrip.IoCount;
      }

      internal readonly new CCsState OldState;

      internal readonly CChannelStrip ChannelStrip;
      private readonly int[] NewMatrix;
      private readonly CGwDiagramLayout DiagramLayout;
      private readonly int IoCount;

      private CFlowMatrix NewFlowMatrix()
      {
         var aFlowMatrix = new CFlowMatrix(this.ChannelStrip.WriteLogInfoMessage, this.DiagramLayout, this.IoCount, this.NewMatrix);
         return aFlowMatrix;
      }
      private CFlowMatrix FlowMatrixM;
      private CFlowMatrix FlowMatrix { get => CLazyLoad.Get(ref this.FlowMatrixM, () => this.NewFlowMatrix()); }
      private CCsState NewStateM;
      private CCsState NewState { get => CLazyLoad.Get(ref this.NewStateM, () => new CCsState(this.OldState.GaAnimator, this.ChannelStrip, this.OldState, this.FlowMatrix)); }
      internal override CGaWorkerResult NewWorkerResult(BackgroundWorker aBackgroundWorker) => new CCsWorkerResult(this.ChannelStrip, aBackgroundWorker, this.NewState);  
   }

   internal abstract class CCsConnector
   {
      internal CCsConnector(CCsConnectors aConnectors, int aNumber)
      {
         this.Conncectors = aConnectors;
         this.Number = aNumber;
         this.InMatrix = new CCsChannelInMatrx(this);
         this.OutMatrix = new CCsChannelOutMatrx(this);
      }
         
      internal readonly CCsConnectors Conncectors;

      /// <summary>
      /// 0            = Input
      /// 1..IoCount-1 = Channel
      /// IoCount      = Output
      /// </summary>
      internal readonly int Number;

      private COutlet Outlet { get => this.Conncectors.ChannelStrip.ToChannelsOut; }

      internal void SendToChannel(params object[] aValues)
      {
         var aWithPrefix = new object[] { "to_channel", this.Number }.Concat(aValues).ToArray();
         this.Outlet.SendValues(aWithPrefix);
      }

      internal void Receive(CList aList)=>this.Receive(aList.Value.ToArray());
      internal void Receive(params object[] aValues)
      {
         if(aValues.Length >= 3)
         {
            if(aValues[0].Equals("from_channel")
            && aValues[1].Equals((Int32)this.Number))
            {
               switch(aValues[2].ToString())
               {
                  case "from":
                     this.Conncectors.FocusedConnector = this;
                     break;

                  case "to":
                     this.Conncectors.FocusedConnector.ConnectTo(this);
                     break;

                  case "inputs":
                  case "outputs":
                     {
                        var aIsOutput = aValues[2].Equals("outputs");
                        var aIsInput = aValues[2].Equals("inputs");
                        if (aValues.Length >= 4)
                        {
                           var aOutlet = aValues[3];
                           if (aOutlet.Equals(0))
                           {
                              if (aValues.Length == 7)
                              {
                                 var aIo = Convert.ToInt32(aValues[4]);
                                 var aActive = CChannelStrip.GetBool(aValues[6]);
                                 if(aIsInput)
                                 {
                                    bool aOk = !aActive || this.GetInputEnabled(aIo);
                                    if(aOk)
                                    {
                                       this.SetInputActive(aIo, aActive);
                                    }
                                    else
                                    {
                                       this.InMatrix.SendActive(aIo);
                                       this.InMatrix.SetEnabled(aIo, false);
                                    }
                                 }
                                 if(aIsOutput)
                                 {
                                    bool aOk = !aActive || this.GetOutputEnabled(aIo);
                                    if(aOk)
                                    {
                                       this.SetOutputActive(aIo, aActive);
                                    }
                                    else
                                    {
                                       this.OutMatrix.SendActive(aIo);
                                       this.OutMatrix.SetEnabled(aIo, false);
                                    }
                                 }
                              }
                           }
                        }
                     }
                     break;

               }
            }
         }
      }

      internal bool GetOutputActive(int aOutputIdx) => this.ChannelStrip.GetOutputActive(this.Number, aOutputIdx);
      internal void SetOutputActive(int aOutputIdx, bool aActive) => this.ChannelStrip.SetOutputActive(this.Number, aOutputIdx, aActive);
      internal bool GetInputActive(int aOutputIdx) => this.ChannelStrip.GetInputActive(this.Number, aOutputIdx);
      internal void SetInputActive(int aInputIdx, bool aActive)=> this.ChannelStrip.SetInputActive(this.Number, aInputIdx, aActive);


      private Int32 FocusPanelBorder
      {
         set
         {
            this.SendToChannel("panel", "focus", "border", value);
         }
      }

      internal void Focus()
      {
         this.FocusPanelBorder = 4;
         this.ChannelStrip.Paint();
      }

      internal void Unfocus()
      {
         this.FocusPanelBorder = 0;
      }

      private bool? EnabledM;
      internal bool Enabled
      {
         get
         {
            return this.EnabledM.GetValueOrDefault(false);
         }
         set
         {
            if(!this.EnabledM.HasValue || this.EnabledM.Value != value)
            {
               var aAlpha = value ? 0 : 0.75;
               this.EnabledM = value; 
               this.SendToChannel("panel", "enable", "bgfillcolor", 1.0d, 1.0d, 1.0d, aAlpha);               
            }
         }
      }

      internal CChannelStrip ChannelStrip { get => this.Conncectors.ChannelStrip; }
      internal bool IsOutput { get => this.Number == this.ChannelStrip.IoCount; }
      internal CFlowMatrix FlowMatrix { get => this.ChannelStrip.FlowMatrix; }
      internal CRoutings Routings { get => this.FlowMatrix.Routings; }
      internal int IoCount { get => this.FlowMatrix.IoCount; }

      internal  CRouting Routing
      {
         get
         {
            var aRoutings = this.Routings;
            return this.IsOutput
                  ? aRoutings.Routings[0]
                  : aRoutings.Routings[this.Number]
                  ;
         }
      }

      internal bool CalcEnabled()
      {
         var aRouting = this.Routing;
         var aEnabled = aRouting.IsLinkedToInput
                     && aRouting.IsLinkedToOutput
                     ;
         return aEnabled;
      }

      internal bool GetOutputEnabled(Int32 aOutput) => this.ChannelStrip.GetRoutingEnabled(this.Number, aOutput);
      internal bool GetInputEnabled(Int32 aInput) => this.ChannelStrip.GetRoutingEnabled(aInput, this.Number);
      private void ConnectTo(CCsConnector aConnector)
      {
         if(this.GetOutputActive(aConnector.Number))
         {
            this.SetOutputActive(aConnector.Number, false);
         }
         else if(this.GetOutputEnabled(aConnector.Number))
         {
            this.SetOutputActive(aConnector.Number, true);
         }
      }

      internal virtual void UpdateRoutings()
      {
         this.Enabled = this.CalcEnabled();
       
         var aIoCount = this.IoCount;
         var aFlowMatrix = this.FlowMatrix;
         var aChannelNr = this.Number;
         for (var aIo = 0; aIo < aIoCount; ++aIo)
         {
            { // Input
               var aRow = aIo;
               var aCol = aChannelNr;
               var aCellIdx = aFlowMatrix.GetCellIdx(aCol, aRow);
               var aActive = aFlowMatrix.Actives[aCellIdx];
               var aEnabled = aFlowMatrix.Enables[aCellIdx];
               this.InMatrix.SetActive(aIo, aActive);
               this.InMatrix.SetEnabled(aIo, aEnabled);
            }
            { // Output:
               var aRow = aChannelNr;
               var aCol = aIo;
               var aCellIdx = aFlowMatrix.GetCellIdx(aCol, aRow);
               var aActive = aFlowMatrix.Actives[aCellIdx];
               var aEnabled = aFlowMatrix.Enables[aCellIdx];
               this.OutMatrix.SetActive(aIo, aActive);
               this.OutMatrix.SetEnabled(aIo, aEnabled);
            }
         }
   }

      internal void SendInitialValues()
      {
         this.Unfocus();
         this.Enabled = false;
      }

      internal readonly CCsChannelInMatrx InMatrix;
      internal readonly CCsChannelOutMatrx OutMatrix;


   }

   internal sealed class CCsChannel : CCsConnector
   {
      internal CCsChannel(CCsConnectors aConnectors, int aNumber) :base(aConnectors, aNumber)
      {

      }
   }

   internal sealed class CCsMainIo : CCsConnector
   {
      internal CCsMainIo(CCsConnectors aConnectors) :base(aConnectors, 0)
      {
      }
   }



   internal abstract class CCsChannelIoMatrix
   {
      internal CCsChannelIoMatrix(CCsConnector aConnector)
      {
         this.Connector = aConnector;
         this.Enableds = new bool?[aConnector.IoCount];
         this.Actives = new bool?[aConnector.IoCount];
      }

      internal readonly CCsConnector Connector;
      private readonly bool?[] Enableds;
      private readonly bool?[] Actives;

      internal abstract string MatrixName { get; }
      internal bool GetEnabled(int aIdx) => this.Enableds[aIdx].GetValueOrDefault(false);
      internal void SetEnabled(int aIdx, bool aValue)
      {
         if (!this.Enableds[aIdx].HasValue || this.Enableds[aIdx].Value != aValue)
         {
            this.Enableds[aIdx] = aValue;
            var aMsg = aValue ? "enablecell" : "disablecell";
            this.Connector.SendToChannel(this.MatrixName, aMsg,  aIdx, 0);
         }
      }
      internal void SendActive(int aIdx)
      {
         this.Connector.SendToChannel(this.MatrixName, "set", aIdx, 0, this.Actives[aIdx].GetValueOrDefault(false) ? 1 : 0);
      }

      internal bool GetActive(int aIdx) => this.Enableds[aIdx].GetValueOrDefault(false);
      internal void SetActive(int aIdx, bool aValue)
      {
         if (!this.Actives[aIdx].HasValue || this.Actives[aIdx].Value != aValue)
         {
            this.Actives[aIdx] = aValue;
            this.SendActive(aIdx);
         }
      }

   }

   internal sealed class CCsChannelInMatrx:  CCsChannelIoMatrix
   {
      internal CCsChannelInMatrx(CCsConnector aConnector) :base(aConnector)
      {

      }
      internal override string MatrixName => "inputs";

   }

   internal sealed class CCsChannelOutMatrx : CCsChannelIoMatrix
   {
      internal CCsChannelOutMatrx(CCsConnector aConnector) : base(aConnector)
      {

      }
      internal override string MatrixName => "outputs";
   }


   internal sealed class CCsConnectors
   {
      internal CCsConnectors(CChannelStrip aChannelStrip)
      {
         this.ChannelStrip = aChannelStrip;
         var aIoCount = aChannelStrip.IoCount;
         var aMainIn = new CCsMainIo(this);
         var aChannels = new List<CCsChannel>(Math.Max(0, aIoCount - 1));
         for (var aIdx = 1; aIdx < aIoCount; ++aIdx)
         {
            var aChannel = new CCsChannel(this, aIdx);
            aChannels.Add(aChannel);
         }
         this.MainIo = aMainIn;
         this.Channels = aChannels.ToArray();   

         foreach (var aConnector in this.Connectors)
            aConnector.SendInitialValues();        
      }

      internal readonly CChannelStrip ChannelStrip;
      internal readonly CCsMainIo MainIo;
      internal readonly CCsChannel[] Channels;

      internal CCsConnector GetConnectorByIdx(int aIdx) => this.Connectors.ElementAt(aIdx);
      internal IEnumerable<CCsConnector> Connectors
      {
         get
         {
            yield return this.MainIo;
            foreach (var aChannel in this.Channels)
               yield return aChannel;
         }
      }

      internal int? FocusedConnectorIdx
      {
         get
         {
            var aFocused = this.FocusedConnector;
            var aIdx = aFocused is object ? new int?(aFocused.Number) : default(int?);
            return aIdx;
         }
      }
      private CCsConnector FocusedConnectorM;
      internal CCsConnector FocusedConnector
      {
         get => this.FocusedConnectorM;
         set
         {
            if (!object.ReferenceEquals(this.FocusedConnectorM, value))
            {
               bool aNextGraph = false;
               if(this.FocusedConnectorM is object)
               {
                  if (!this.FocusedConnectorM.Routing.IsLinkedToSomething)
                     aNextGraph = true;
                  this.FocusedConnectorM.Unfocus();
               }
               if(value is object)
               {
                  this.FocusedConnectorM = value;
                  this.FocusedConnectorM.Focus();
                  if (!this.FocusedConnectorM.Routing.IsLinkedToSomething)
                     aNextGraph = true;
               }
               if(aNextGraph)
               {
                  this.ChannelStrip.NextGraph();
               }
            }
         }
      }

      internal void UpdateRoutings()
      {
         foreach(var aChannel in this.Connectors)
         {
            aChannel.UpdateRoutings();
         }
      }

      internal void Receive(CList aList)
      {
         foreach (var aConnector in this.Connectors)
            aConnector.Receive(aList);
      }

      internal CCsConnector GetConnectorByName(string aName) => (from aTest in this.Connectors where aTest.Routing.NameForInput == aName || aTest.Routing.NameForOutput == aName select aTest).Single();
   }

   internal sealed class CCsDiagramLayout : CGwDiagramLayout
   {
      internal CCsDiagramLayout(int? aFocusedRouting)
      {
         this.FocusedRouting = aFocusedRouting;
      }
      private readonly int? FocusedRouting;
      internal override bool GetIncludeInDiagram(CRouting aRouting) => base.GetIncludeInDiagram(aRouting) || this.FocusedRouting == aRouting.InputIdx;
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
         this.PWindowInOut = new CMultiTypeOutlet(this);
         this.PWindowInOut.Support(CMessageTypeEnum.List);
         this.PWindowInOut.Support(CMessageTypeEnum.Matrix);
         this.Vector2dOut = new CMultiTypeOutlet(this);
         this.Vector2dOut.Support(CMessageTypeEnum.List);
         this.Vector2dOut.Support(CMessageTypeEnum.Bang);
         this.Vector2dDumpIn = new CListInlet(this);
         this.GaAnimator = new CGaAnimator(this.WriteLogErrorMessage,
                                               this.OnGraphAvailable,
                                               this.OnGraphRequestPaint,
                                               aAnimator=>this.SetNewState(aAnimator, 2),
                                               this.WriteLogInfoMessage
                                               );
         this.GaAnimator.NotifyEndAnimation = this.OnEndAnimation;
         this.PWindow2InOut = new CListOutlet(this);
         this.PWindow2InOut.Support(CMessageTypeEnum.List);
         this.FromChannelsIn = new CListInlet(this);
         this.FromChannelsIn.Action = this.OnFromChannelsIn;
         this.ToChannelsOut = new CListOutlet(this);         
         this.ControlIn = new CListInlet(this);
         this.ControlIn.SetPrefixedListAction("mouse", this.OnMouseIn);
         this.ControlIn.SetPrefixedListAction("key", this.OnKeyIn);
      }

      private volatile CCsState CsState;
      internal void ChangeState(CCsState aNewState)
      {
         this.CsState = aNewState;
         this.SendMatrixEnabledStates();
         this.SendRoutingMatrix();         
      }

      private void OnEndAnimation()
      {
         this.InvokeInMainTask(delegate ()
         {
            this.Connectors.UpdateRoutings();
         });
      }

      private CCsState SetNewState(CGaAnimator aAnimator, int aIoCount)
      {
         var aMatrix = new bool[aIoCount * aIoCount];
         var aDiagramLayout = this.NewDiagramLayout();
         var aFlowMatrix = new CFlowMatrix(this.WriteLogInfoMessage, aDiagramLayout, aIoCount, aMatrix);
         this.CsState = new CCsState(aAnimator, this, aFlowMatrix);
         return this.CsState;
      }

      private CGwDiagramBuilder GwDiagramBuilder { get => this.FlowMatrix.Routings.GwDiagramBuilder; }

      private readonly CListInlet MatrixCtrlLeftOutIn;
      private readonly CListInlet MatrixCtrlRightOutIn;

      private readonly CListOutlet MatrixCtrlLeftInOut;
      private readonly CMultiTypeOutlet PWindowInOut;

      private readonly CMultiTypeOutlet Vector2dOut;
      private readonly CListInlet Vector2dDumpIn;
      private readonly CListOutlet PWindow2InOut;

      private readonly CListInlet FromChannelsIn;
      internal readonly CListOutlet ToChannelsOut;
      private readonly CListInlet ControlIn;

      private CCsConnectors ConnectorsM;
      internal CCsConnectors Connectors { get => CLazyLoad.Get(ref this.ConnectorsM, () => new CCsConnectors(this)); private set { this.ConnectorsM = value; } }

      internal Int32 IoCount;
      private bool RequestRowsPending;
      private Int32 RequestRowIdx;
      internal Int32[][] Rows;
      internal CFlowMatrix FlowMatrix { get => this.CsState.FlowMatrix; }

      private readonly CGaAnimator GaAnimator;

      private void OnGraphRequestPaint()
      {
         this.InvokeInMainTask(delegate ()
         {
            this.Paint();
            this.GaAnimator.OnPaintDone();
         });
      }
      
      private void OnGraphAvailable()
      {
         this.InvokeInMainTask(delegate ()
         {
            this.GaAnimator.ProcessNewGraph();
         });
      }

      private void OnInit(CInlet aInlet, string aFirstItem, CReadonlyListData aParams)
      {
         var aIoCount = Convert.ToInt32(aParams.ElementAt(0));
         var aRows = new Int32[aIoCount][];
         for (var aIdx = 0; aIdx < aIoCount; ++aIdx)
         {
            aRows[aIdx] = new int[aIoCount];
         }
         aRows[0][0] = 1;
         this.GaAnimator.State = this.SetNewState(this.GaAnimator, aIoCount);
         this.Rows = aRows;
         this.IoCount = aIoCount;
         this.Connectors = new CCsConnectors(this);
         this.UpdateMatrix();
         this.SendRoutingMatrix();
         this.SendMatrixEnabledStates();
         this.Connectors.UpdateRoutings();
         this.Connectors.FocusedConnector = this.Connectors.MainIo;
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

      internal void SendRoutingMatrix()
      {
         foreach (var aRowIdx in Enumerable.Range(0, this.FlowMatrix.IoCount))
         {
            foreach (var aColIdx in Enumerable.Range(0, this.FlowMatrix.IoCount))
            {
               this.MatrixCtrlLeftInOut.SendValues("set", aColIdx, aRowIdx, this.Rows[aRowIdx][aColIdx]);
            }
         }
      }

      private void UpdateMatrix()
      {
         this.NextGraph();
      }

      internal void SendMatrixEnabledStates()
      {
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
      }
      internal void NextGraph()
      {
         var aWorkerArgs = new CCsWorkerArgs(this, this.CsState);
         this.GaAnimator.NextGraph(aWorkerArgs);
      }

      private void SendGraphBitmap()
      { 
         //return; // TODO         
         var aBitmap = this.FlowMatrix.Routings.GwDiagramBuilder.Bitmap;
         var aSizeList = this.PWindowInOut.GetMessage<CList>().Value;
         aSizeList.Clear();
         aSizeList.Add("size");
         aSizeList.Add(aBitmap.Width);
         aSizeList.Add(aBitmap.Height);
         this.PWindowInOut.Send(CMessageTypeEnum.List);
         this.PWindowInOut.GetMessage<CMatrix>().Value.SetImage(aBitmap);
         this.PWindowInOut.Send(CMessageTypeEnum.Matrix);
      }

      private CPoint ImageSize { get => this.GaAnimator.Size; }
      private CPoint CanvasSize = new CPoint(1000, 1000);

      private void SendPWindow2Size()
      {
         var aGraphOverlay = this.GaAnimator;
         var aSize = this.ImageSize + this.Translate; 
         var aSizeList = this.PWindow2InOut.GetMessage<CList>().Value;
         aSizeList.Clear();
         aSizeList.Add("size");
         aSizeList.Add(aSize.X );
         aSizeList.Add(aSize.Y );
         this.PWindow2InOut.Send();
      }

      private CPoint Translate { get => new CPoint(10,10); }
      private CPoint Scale { get => this.CanvasSize / this.ImageSize; } 
      internal void Paint()
      {
         var aPainter = new CVector2dPainter(this.Vector2dDumpIn, this.Vector2dOut);
         aPainter.Clear();
         this.SendPWindow2Size();
         aPainter.Translate(this.Translate);
         aPainter.Scale(this.Scale);
         this.GaAnimator.Paint(aPainter);
         this.Vector2dOut.Send(CMessageTypeEnum.Bang);
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
         var aY = Convert.ToInt32(aList.Value.ElementAt(0));
         var aX = Convert.ToInt32(aList.Value.ElementAt(1));
         var aCellState = Convert.ToInt32(aList.Value.ElementAt(2));
         this.Rows[aX][aY] = aCellState;
         this.UpdateMatrix();
      } 

      internal void SetInputActive(int aChannelNr, int aInputIdx, bool aActive)
      {
         if(!aActive            
         || this.GetRoutingEnabled(aInputIdx, aChannelNr))
         {
            this.Rows[aInputIdx][aChannelNr] = aActive ? 1 : 0;
            this.UpdateMatrix();
         }
      }
      internal void SetOutputActive(int aChannelNr, int aOutputIdx, bool aActive)
      {
         if(!aActive
         ||this.GetRoutingEnabled(aChannelNr, aOutputIdx))
         {
            this.Rows[aChannelNr][aOutputIdx] = aActive ? 1 : 0;
            this.UpdateMatrix();
         }
      }
      internal bool GetOutputActive(int aChannelNr, int aOutputIdx)=> this.Rows[aChannelNr][aOutputIdx] == 1;
      internal bool GetInputActive(int aChannelNr, int aInputIdx) => this.Rows[aInputIdx][aChannelNr] == 1;

      private void OnFromChannelsIn(CInlet aInlet, CList aList)
      {
         this.Connectors.Receive(aList);
      }

      public static void Test(Action<string> aFailAction, Action<string> aDebugPrint)
      {
         CFlowMatrix.Test(aFailAction, aDebugPrint);
         CGwDiagramBuilder.Test(aFailAction, aDebugPrint);
      }

      protected override void OnShutdown()
      {
         base.OnShutdown();

         this.GaAnimator.Shutdown();
      }

      internal bool GetRoutingEnabled(int aInput, int aOutput) => this.FlowMatrix.Enables[this.FlowMatrix.GetCellIdx(aOutput, aInput)];

      private CPoint MousePos;

      private IEnumerable<CGaShape> GetShapes(CPoint aPoint)=> this.GaAnimator.State.GaTransition.MorphGraph.GetShapes(aPoint);

      CPoint CursorPos { get => this.GaAnimator.CursorPos; set => this.GaAnimator.CursorPos = value; }

      private void OnKeyIn(CInlet aInlet, string aFirstItem, CReadonlyListData aRemainingItems)
      {
         var aValues = aRemainingItems.ToArray();
         this.InvokeInMainTask(delegate ()
         {
         if (aValues.Length >= 1)
         {
            var aEvent = aValues[0];
            switch (aEvent)
            {
               case "press":
                  if (aValues.Length >= 3)
                  {
                        var aModifier = Convert.ToInt32(aValues[1]);
                        var aInputOrOutput = (aModifier & 512) > 0;
                        var aKey = Convert.ToInt32(aValues[2]);
                        var aNorm0 = 48;
                        var aNorm9 = 57;
                        var aPad0 = -33;
                        var aPad9 = -42;
                        var aIsNorm = aKey >= aNorm0 && aKey <= aNorm9;
                        var aIsPad = aKey <= aPad0 && aKey >= aPad9;
                        var aOffset = aIsNorm
                                    ? new int?(aNorm0)
                                    : aIsPad
                                    ? new int?(aPad0)
                                    : new int?()
                                    ;
                        if (aOffset.HasValue)
                        {
                           var aIoIdx = aKey - aOffset.Value;
                           var aConnectors = this.Connectors;
                           var aFocused = aConnectors.GetConnectorByIdx(aIoIdx);
                           aConnectors.FocusedConnector = aFocused;                  
                        }              
                     }
                     break;
               }
            }
         });
      }
      private void OnMouseIn(CInlet aInlet, string aFirstItem, CReadonlyListData aRemainingItems)
      {
         var aValues = aRemainingItems.ToArray();
         this.InvokeInMainTask(delegate ()
         {
            if (aValues.Length >= 1)
            {
               if (aValues[0].Equals("move")
                  && aValues.Length >= 3)
               {
                  var aX = Convert.ToDouble(aValues[1]);
                  var aY = Convert.ToDouble(aValues[2]);
                  this.MousePos = new CPoint(aX, aY);
                  this.GaAnimator.CursorPos = this.MousePos;
                  this.Paint();
               }
               else if (aValues[0].Equals("button")
                     && aValues.Length >= 3)
               {
                  var aButton = Convert.ToInt32(aValues[1]);
                  var aEvent = (CMouseButtonEventEnum)Enum.Parse(typeof(CMouseButtonEventEnum), aValues[2].ToString(), true);
                  switch (aEvent)
                  {
                     case CMouseButtonEventEnum.Down:
                        {
                           var aShapes = this.GetShapes(this.MousePos);
                           if(!aShapes.IsEmpty())
                           {
                              var aShape = aShapes.Last();
                              if (aShape is CGaNode)
                              {
                                 this.Connectors.FocusedConnector = this.Connectors.GetConnectorByName(aShape.Name);
                              }
                           }
                        }
                        break;

                     case CMouseButtonEventEnum.Up:
                        break;
                  }
               }
            }
         });
      }

      internal CGwDiagramLayout NewDiagramLayout() => new CCsDiagramLayout(this.ConnectorsM is object ? this.Connectors.FocusedConnectorIdx : default(int?));

      private enum CMouseButtonEventEnum
      {
         Up,
         Down
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
