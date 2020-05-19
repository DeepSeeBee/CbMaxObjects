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
   using System.Windows.Forms;
   using CbMaxClrAdapter.Timer;
   using System.Windows.Input;

   internal abstract class CGwDiagramLayout
   {
      internal static readonly DirectoryInfo GraphWizInstallDirDefault = new DirectoryInfo(@"C:\Program Files (x86)\Graphviz2.38\");
      private DirectoryInfo GraphWizInstallDirM = GraphWizInstallDirDefault;
      internal DirectoryInfo GraphWizInstallDir { get => this.GraphWizInstallDirM; set => this.GraphWizInstallDirM = value; }
      internal virtual bool GetIncludeInDiagram(CChannel aChannel) => aChannel.IsLinkedToSomething;
      internal virtual CPoint DiagramSize { get => new CPoint(1600, 600); }

   }

   internal sealed class CCsWorkerResult : CGaNewStateWorkerResult
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

         Exception aExc = this.NewState.NewGraphWithExc.Item1;
         if (aExc is object)
         {
            this.ChannelStrip.WriteLogErrorMessage(aExc.Message);
         }
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
      internal override Tuple<Exception, CGwGraph> GwGraph => this.FlowMatrix.Channels.GwDiagramBuilder.GwGraph;
      internal override bool GetIsFocused(CGaShape aShape)
      {
         var aFocusedConnector = this.ChannelStrip.Connectors.FocusedConnector;
         if (aShape is CGaNode
         && aFocusedConnector is object)
         {          
            var aChannel = aFocusedConnector.Channel;
            var aFocused = aShape.Name == aChannel.NameForInput
                        || aShape.Name == aChannel.NameForOutput
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

      private COutlet Outlet { get => this.Conncectors.ChannelStrip.ControlOut; }

      internal void SendToChannel(params object[] aValues)
      {
         var aWithPrefix = new object[] { "to_channel", this.Number }.Concat(aValues).ToArray();
         this.Outlet.SendValuesO(aWithPrefix);
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

                  case "vst":
                     if(aValues[3].Equals("param"))
                     {
                        var aParamIdx = Convert.ToInt32(aValues[4]);
                        var aParamValue = Convert.ToInt32(aValues[5]);
                        //this.ChannelStrip.WriteLogInfoMessage("VstParam.Id", aParamIdx);
                        //this.ChannelStrip.WriteLogInfoMessage("VstParam.Value", aParamValue);
                        switch (aParamIdx)
                        {
                           case LatencyParamIdx:
                              this.ReceiveLatency(aParamValue);
                              break;
                        }
                     }
                     break;

                  case "init":
                     this.SendInitialValues();
                     break;

                  case "focus":
                     this.Conncectors.FocusedConnector = this;
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

      internal bool CalcFocused() => object.ReferenceEquals(this, this.Conncectors.FocusedConnector);
      internal void Focus(bool aFocused)
      {
         if (aFocused)
            this.Focus();
         else
            this.Unfocus();
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

      private void Enable(bool aEnabled)
      {
         var aAlpha = aEnabled ? 0 : 0.75;
         this.SendToChannel("panel", "enable", "bgfillcolor", 1.0d, 1.0d, 1.0d, aAlpha);
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
               this.EnabledM = value;
               this.Enable(value);
            }
         }
      }

      internal CChannelStrip ChannelStrip { get => this.Conncectors.ChannelStrip; }
      internal bool IsOutput { get => this.Number == this.ChannelStrip.IoCount; }
      internal CFlowMatrix FlowMatrix { get => this.ChannelStrip.FlowMatrix; }
      internal CChannels Channels { get => this.FlowMatrix.Channels; }
      internal int IoCount { get => this.FlowMatrix.IoCount; }

      internal  CChannel Channel
      {
         get
         {
            var aChannels = this.Channels;
            return this.IsOutput
                  ? aChannels.Channels[0]
                  : aChannels.Channels[this.Number]
                  ;
         }
      }

      internal abstract bool IsChannel { get; }

      internal bool CalcEnabled()
      {
         var aChannel = this.Channel;
         var aEnabled = aChannel.IsLinkedToInput
                     && aChannel.IsLinkedToOutput
                     ;
         return aEnabled;
      }

      internal bool GetOutputEnabled(Int32 aOutput) => this.ChannelStrip.GetChannelEnabled(this.Number, aOutput);
      internal bool GetInputEnabled(Int32 aInput) => this.ChannelStrip.GetChannelEnabled(aInput, this.Number);
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

      internal virtual void UpdateChannels()
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
         this.EnabledM = default(bool?);       
         this.Enabled = this.CalcEnabled();
         this.Focus(this.CalcFocused());
      }

      internal readonly CCsChannelInMatrx InMatrix;
      internal readonly CCsChannelOutMatrx OutMatrix;

      #region Latency
      private const int LatencyParamIdx = -10;
      internal void RequestNewLatency()
      {
         this.SendToChannel("vst", "get", LatencyParamIdx);
      }

      private int NodeLatencyM;
      internal int NodeLatency { get => this.NodeLatencyM;
         set
         {
            this.NodeLatencyM = value;
            this.UpdateNodeLatency();
         }
      }
      internal bool IsMainOut { get => this.Number == 0; } // TODO

      internal int? NewNodeLatency;
      internal void CommitNewNodeLatency()
      {
         if(this.NewNodeLatency.HasValue
         && this.NewNodeLatency.Value != this.NodeLatency)
         {
            this.NodeLatency = this.NewNodeLatency.Value;
            
            
         }
      }
      internal void UpdateNodeLatency()
      {
         this.Channel.NodeLatency = this.NodeLatency;
      }
      private void ReceiveLatency(int aLatency)
      {
         this.NewNodeLatency = aLatency;
      }
      private double SamplesToMs(int aSamples)
      {
         var aSampleRate = this.FlowMatrix.SampleRate;
         double aMs = (aSampleRate > 0)
                    ? ((double)aSamples) / ((double)aSampleRate) * 1000.0d
                    : 0
                    ;
         return aMs;
      }

      #region OutLatencySamples
      private void SendOutLatencySamples(int aLatency)
      {
         this.SendToChannel("latency", "out", "samples", aLatency);
         this.OutLatencySamplesSent = aLatency;
      }
      private int? OutLatencySamplesSent;
      private void SendOutLatencySamplesOnDemand()
      {
         var aLatency = this.Channel.OutLatency;
         if (!this.OutLatencySamplesSent.HasValue
         || this.OutLatencySamplesSent.Value != aLatency)
         {
            this.SendOutLatencySamples(aLatency);
         }
      }
      #endregion
      #region OutLatencyMs
      private void SendOutLatencyMs(double aLatency)
      {
         this.SendToChannel("latency", "out", "ms", aLatency);
         this.OutLatencyMsSent = aLatency;
      }
      private double? OutLatencyMsSent;
      private void SendOutLatencyMsOnDemand()
      {
         var aLatency = this.SamplesToMs(this.Channel.OutLatency);
         if (!this.OutLatencyMsSent.HasValue
         || this.OutLatencyMsSent.Value != aLatency)
         {
            this.SendOutLatencyMs(aLatency);
         }
      }
      #endregion
      #region NodeLatencySamples
      private void SendNodeLatencySamples(int aLatency)
      {
         this.SendToChannel("latency", "node", "samples", aLatency);
         this.NodeLatencySamplesSent = aLatency;
      }
      private int? NodeLatencySamplesSent;
      private void SendNodeLatencySamplesOnDemand()
      {
         var aLatency = this.Channel.NodeLatency;
         if (!this.NodeLatencySamplesSent.HasValue
         || this.NodeLatencySamplesSent.Value != aLatency)
         {
            this.SendNodeLatencySamples(aLatency);
         }
      }
      #endregion
      #region NodeLatencyMs
      private void SendNodeLatencyMs(double aLatency)
      {
         this.SendToChannel("latency", "node", "ms", aLatency);
         this.NodeLatencyMsSent = aLatency;
      }
      private double? NodeLatencyMsSent;
      private void SendNodeLatencyMsOnDemand()
      {
         var aLatency = this.SamplesToMs(this.Channel.NodeLatency);
         if (!this.NodeLatencyMsSent.HasValue
         || this.NodeLatencyMsSent.Value != aLatency)
         {
            this.SendNodeLatencyMs(aLatency);
         }
      }
      #endregion
      internal void SendLatenciesOnDemand()
      {         
         this.SendOutLatencySamplesOnDemand();
         this.SendNodeLatencySamplesOnDemand();
         this.SendOutLatencyMsOnDemand();
         this.SendNodeLatencyMsOnDemand();
      }
      #endregion
      #region LatencyCompensation
      internal void SendMixDelays()
      {
         // TODO_OPT
         var aChannel = this.Channel;
         var aDelays = (from aIoIdx in Enumerable.Range(0, this.IoCount)
                       select aChannel.InputIoIdxs.Contains(aIoIdx)
                            ? aChannel.LatencyCompensations[aChannel.GetInputIdxByIoIdx(aIoIdx)]
                            : 0);
         var aValues = new object[] { "mix", "delays", }.Concat(aDelays.Cast<object>()).ToArray();
         this.SendToChannel(aValues);
      }
      internal void SendMixInMatrix()
      { // TODO_OPT
         var aChannel = this.Channel;
         var aIoCount = this.IoCount;
         var aActives = (from aIoIdx in Enumerable.Range(0, aIoCount) select aChannel.InputIoIdxs.Contains(aIoIdx) ? 1 : 0).ToArray();
         foreach(var aIoIdx in Enumerable.Range(0, aIoCount))
         {
            var aActive = aActives[aIoIdx];
            this.SendToChannel("mix", "inmatrix", aIoIdx, 0, aActive);
         }         
      }
      private void  SendMixOutMatrix(bool aActivate)
      {// TODO_OPT
         var aChannel = this.Channel;
         var aIoCount = this.IoCount;
         var aActives = (from aIoIdx in Enumerable.Range(0, aIoCount) select (aActivate && aChannel.OutputIdxs.Contains(aIoIdx)) ? 1 : 0).ToArray();
         foreach (var aIoIdx in Enumerable.Range(0, aIoCount))
         {
            var aActive = aActives[aIoIdx];
            this.SendToChannel("mix", "outmatrix", 0, aIoIdx, aActive);
         }
      }
      internal void SendMixDisable()
      {
         this.SendMixOutMatrix(false);
      }
      internal void SendMixEnable()
      {
         this.SendMixOutMatrix(true);
      }
      #endregion
      #region Vst
      internal void VstOpen()
      {
         this.SendToChannel("vst", "open");
      }
      #endregion

   }

   internal sealed class CCsChannel : CCsConnector
   {
      internal CCsChannel(CCsConnectors aConnectors, int aNumber) :base(aConnectors, aNumber)
      {

      }
      internal override bool IsChannel => true;
   }

   internal sealed class CCsMainIo : CCsConnector
   {
      internal CCsMainIo(CCsConnectors aConnectors) :base(aConnectors, 0)
      {
      }
      internal override bool IsChannel => false;
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
                  if (!this.FocusedConnectorM.Channel.IsLinkedToSomething)
                     aNextGraph = true;
                  this.FocusedConnectorM.Unfocus();
               }
               if(value is object)
               {
                  this.FocusedConnectorM = value;
                  this.FocusedConnectorM.Focus();
                  if (!this.FocusedConnectorM.Channel.IsLinkedToSomething)
                     aNextGraph = true;
               }
               if(aNextGraph)
               {
                  this.ChannelStrip.NextGraph();
               }
            }
         }
      }     
      internal void UpdateChannels()
      {
         foreach(var aChannel in this.Connectors)
         {
            aChannel.UpdateChannels();
         }
      }

      internal void Receive(CList aList)
      {
         foreach (var aConnector in this.Connectors)
            aConnector.Receive(aList);
      }

      internal CCsConnector GetConnectorByName(string aName) => (from aTest in this.Connectors where aTest.Channel.NameForInput == aName || aTest.Channel.NameForOutput == aName select aTest).Single();

      internal void CommitNewNodeLatencies()
      {
         foreach (var aConnector in this.Connectors)
         {
            aConnector.CommitNewNodeLatency();
         }
         this.UpdateNodeLatencies();         
      }

      internal void UpdateNodeLatencies()
      {
         foreach (var aConnector in this.Connectors)
         {
            aConnector.UpdateNodeLatency();
         }
         this.SendLatenciesOnDemand();
      }

      internal void SendLatenciesOnDemand()
      {
         foreach (var aConnector in this.Connectors)
         {
            aConnector.SendMixDisable();
         }
         foreach (var aConnector in this.Connectors)
         {
            aConnector.SendLatenciesOnDemand();
         }
         foreach (var aConnector in this.Connectors)
         {
            aConnector.SendMixDelays();
            aConnector.SendMixInMatrix();

         }
         foreach (var aConnector in this.Connectors)
         {
            aConnector.SendMixEnable();
         }
      }

   }

   internal sealed class CCsDiagramLayout : CGwDiagramLayout
   {
      internal CCsDiagramLayout(int? aFocusedChannel)
      {
         this.FocusedChannel = aFocusedChannel;
      }
      private readonly int? FocusedChannel;
      internal override bool GetIncludeInDiagram(CChannel aChannel) => base.GetIncludeInDiagram(aChannel) || this.FocusedChannel == aChannel.IoIdx;
   }

   public sealed class CChannelStrip : CMaxObject
   {
      #region ctor
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
         this.ControlOut = new CListOutlet(this);         
         this.ControlIn = new CListInlet(this);
         this.ControlIn.SetPrefixedListAction("mouse1", this.OnMouse1In);
         this.ControlIn.SetPrefixedListAction("mouse2", this.OnMouse2In);
         this.ControlIn.SetPrefixedListAction("key", this.OnKeyIn);
         this.ControlIn.SetPrefixedListAction("samplerate", this.OnSampleRate);
         this.ControlIn.SetPrefixedListAction("graph_wiz_folder", this.OnGraphWizFolder);
         this.TimerThread = new CTimerThread(this);
         { // InitLatencyUpdateTimer
            var aRunInMainThread = true;
            var aInterval = new TimeSpan(0, 0, 0, 0, 500);
            var aPriority = System.Windows.Threading.DispatcherPriority.Background;
            this.LatencyUpdateTimer = new CTimer(this.TimerThread, 
                                                 aInterval, aPriority,
                                                 this.OnLatencyUpdateTimer, 
                                                 aRunInMainThread);
         }
         this.SignalMatrixOut = new CListOutlet(this);
         this.DebugInlet = new CListInlet(this);
         this.InitDebugInlet();
      }
      protected override void OnInitialized()
      {
         base.OnInitialized();

         this.InitGraph(11);
      }
      private void OnInit(CInlet aInlet, string aFirstItem, CReadonlyListData aParams)
      {
         var aIoCount = Convert.ToInt32(aParams.ElementAt(0));
         this.InitGraph(aIoCount);
      }
      private void InitGraph(int aIoCount)
      {
         var aRows = new Int32[aIoCount][];
         for (var aIdx = 0; aIdx < aIoCount; ++aIdx)
         {
            aRows[aIdx] = new int[aIoCount];
         }
         if(aIoCount > 0)
         {
            aRows[0][0] = 1;
         }
         //if(aIoCount > 1)
         //{
         //   aRows[0][1] = 1;
         //   aRows[1][0] = 1;
         //}
         this.GaAnimator.State = this.SetNewState(this.GaAnimator, aIoCount);
         this.Rows = aRows;
         this.IoCount = aIoCount;
         this.Connectors = new CCsConnectors(this);
         this.UpdateMatrix();
         this.SendChannelMatrix();
         this.SendMatrixEnabledStates();
         this.Connectors.UpdateChannels();
         this.Connectors.FocusedConnector = this.Connectors.MainIo;
         this.LatencyUpdateTimer.Start();
         this.SendSignalMatrix();
         this.SendGraphWizFolder();
      }
      #endregion
      #region Debug
      private readonly CListInlet DebugInlet;
      private void InitDebugInlet()
      {
         this.DebugInlet.SetPrefixedListAction("next_graph", this.OnDebugNextGraph);
      }
      private void OnDebugNextGraph(CInlet aInlet, string aPrefix, CReadonlyListData aRemainingItems)
      {
         this.NextGraph();
      }
      #endregion
      #region Control 
      private readonly CListInlet ControlIn;
      #endregion
      #region Connectors
      private CCsConnectors ConnectorsM;
      internal CCsConnectors Connectors { get => CLazyLoad.Get(ref this.ConnectorsM, () => new CCsConnectors(this)); private set { this.ConnectorsM = value; } }
      #endregion
      #region Graph
      private readonly CGaAnimator GaAnimator;
      private void OnGraphRequestPaint()
      {
         this.BeginInvokeInMainTask(delegate ()
         {
            this.Paint();
            this.GaAnimator.OnPaintDone();
         });
      }
      private void OnGraphAvailable()
      {
         this.BeginInvokeInMainTask(delegate ()
         {
            // First:
            this.GaAnimator.ProcessNewGraph();

            // RefreshChangedState:
            this.UpdateFlowMatrixSampleRate();
            this.Connectors.UpdateNodeLatencies();            
         });
      }
      private volatile CCsState CsState;
      internal void ChangeState(CCsState aNewState)
      {
         this.CsState = aNewState;
         this.SendMatrixEnabledStates();
         this.SendChannelMatrix();
      }
      private void OnEndAnimation()
      {
         this.BeginInvokeInMainTask(delegate ()
         {
            this.Connectors.UpdateChannels();
            this.SendSignalMatrix();
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
      internal void NextGraph()
      {
         var aWorkerArgs = new CCsWorkerArgs(this, this.CsState);
         this.GaAnimator.NextGraph(aWorkerArgs);
      }
      internal CGwDiagramLayout NewDiagramLayout()
      {
         var aFocusedChannelIdx = this.ConnectorsM is object ? this.Connectors.FocusedConnectorIdx : default(int?);
         var aDiagramLayout = new CCsDiagramLayout(aFocusedChannelIdx);
         aDiagramLayout.GraphWizInstallDir = this.GraphWizDirectoryInfo;
         return aDiagramLayout;
      }
      #endregion
      #region Matrix
      private readonly CListInlet MatrixCtrlLeftOutIn;
      private readonly CListInlet MatrixCtrlRightOutIn;
      private readonly CListOutlet MatrixCtrlLeftInOut;
      internal Int32 IoCount;
      internal Int32[][] Rows;
      internal CFlowMatrix FlowMatrix { get => this.CsState.FlowMatrix; }
      internal void SendChannelMatrix()
      {
         foreach (var aRowIdx in Enumerable.Range(0, this.FlowMatrix.IoCount))
         {
            foreach (var aColIdx in Enumerable.Range(0, this.FlowMatrix.IoCount))
            {
               this.MatrixCtrlLeftInOut.SendValuesO("set", aColIdx, aRowIdx, this.Rows[aRowIdx][aColIdx]);
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
      private void OnMatrixCtrlRightOutIn(CInlet aInlet, CList aList)
      {
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
         if (!aActive
         || this.GetChannelEnabled(aInputIdx, aChannelNr))
         {
            this.Rows[aInputIdx][aChannelNr] = aActive ? 1 : 0;
            this.UpdateMatrix();
         }
      }
      internal void SetOutputActive(int aChannelNr, int aOutputIdx, bool aActive)
      {
         if (!aActive
         || this.GetChannelEnabled(aChannelNr, aOutputIdx))
         {
            this.Rows[aChannelNr][aOutputIdx] = aActive ? 1 : 0;
            this.UpdateMatrix();
         }
      }
      internal bool GetOutputActive(int aChannelNr, int aOutputIdx) => this.Rows[aChannelNr][aOutputIdx] == 1;
      internal bool GetInputActive(int aChannelNr, int aInputIdx) => this.Rows[aInputIdx][aChannelNr] == 1;

      #endregion
      #region GraphDisplay
      private readonly CListInlet Vector2dDumpIn;
      private readonly CMultiTypeOutlet Vector2dOut;
      private readonly CListOutlet PWindow2InOut;

      private CPoint ImageSize { get => this.GaAnimator.Size; }
      private CPoint CanvasSize = new CPoint(1000, 1000);

      private void SendPWindow2Size()
      {
         var aGraphOverlay = this.GaAnimator;
         var aSize = this.ImageSize + this.Translate; 
         //this.WriteLogInfoMessage("SendPWindow2Size.Size=", aSize);
         var aSizeList = this.PWindow2InOut.GetMessage<CList>().Value;
         aSizeList.Clear();
         aSizeList.Add("size");
         aSizeList.Add(aSize.x);
         aSizeList.Add(aSize.y);
         this.PWindow2InOut.Send();
      }

      private CPoint Translate { get => new CPoint(10, 10); }
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
      #endregion
      #region GraphBitmap
      private readonly CMultiTypeOutlet PWindowInOut;
      private void SendGraphBitmap()
      { 
         //return; // TODO         
         var aBitmap = this.FlowMatrix.Channels.GwDiagramBuilder.Bitmap;
         var aSizeList = this.PWindowInOut.GetMessage<CList>().Value;
         aSizeList.Clear();
         aSizeList.Add("size");
         aSizeList.Add(aBitmap.Width);
         aSizeList.Add(aBitmap.Height);
         this.PWindowInOut.Send(CMessageTypeEnum.List);
         this.PWindowInOut.GetMessage<CMatrix>().Value.SetImage(aBitmap);
         this.PWindowInOut.Send(CMessageTypeEnum.Matrix);
      }
      #endregion
      #region Channels
      private readonly CListInlet FromChannelsIn;
      internal readonly CListOutlet ControlOut;
      private void OnFromChannelsIn(CInlet aInlet, CList aList)
      {
         this.Connectors.Receive(aList);
      }

      internal bool GetChannelEnabled(int aInput, int aOutput) => this.FlowMatrix.Enables[this.FlowMatrix.GetCellIdx(aOutput, aInput)];
      #endregion
      #region Test
      public static void Test(Action<string> aFailAction, Action<string> aDebugPrint)
      {
         CFlowMatrix.Test(aFailAction, aDebugPrint);
         CGwDiagramBuilder.Test(aFailAction, aDebugPrint);
      }
      #endregion
      #region Shutdown
      protected override void OnShutdown()
      {
         base.OnShutdown();
         this.GaAnimator.Shutdown();
      }
      #endregion
      #region Keyboard
      private void OnKeyIn(CInlet aInlet, string aFirstItem, CReadonlyListData aRemainingItems)
      {
         var aValues = aRemainingItems.ToArray();
         this.BeginInvokeInMainTask(delegate ()
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
                        var aIsModifier = (aModifier & 512) > 0;
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
                           var aNewConnector = aConnectors.GetConnectorByIdx(aIoIdx);
                           if (!aIsModifier)
                           {
                              aConnectors.FocusedConnector = aNewConnector;
                           }
                           else if (aConnectors.FocusedConnector is object)
                           {
                              var aOldActive = aConnectors.FocusedConnector.GetOutputActive(aNewConnector.Number);
                              var aNewActive = !aOldActive;
                              aConnectors.FocusedConnector.SetOutputActive(aNewConnector.Number, aNewActive);
                              if (aNewActive)
                              {
                                 aConnectors.FocusedConnector = aNewConnector;
                              }
                           }
                           else
                           {
                              aConnectors.FocusedConnector = aNewConnector;
                           }
                        }              
                     }
                     break;
               }
            }
         });
      }
      #endregion
      #region Mouse
      private IEnumerable<CGaShape> GetShapes(CPoint aPoint) => this.GaAnimator.State.GaTransition.MorphGraph.GetShapes(aPoint);      
      private CPoint MousePos;
      private int MouseButton;
      private void OnMouse1In(CInlet aInlet, string aFirstItem, CReadonlyListData aRemainingItems)
      {
         var aValues = aRemainingItems.ToArray();
         this.BeginInvokeInMainTask(delegate ()
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
      private CGaNode GetNodeNullable(CPoint aPos) =>this.GetShapes(aPos).OfType<CGaNode>().LastOrDefault();
      private CGaEdge GetEdgeNullable(CPoint aPos) => this.GetShapes(aPos).OfType<CGaEdge>().LastOrDefault();      
      private readonly Stopwatch DoubleClickStopWatch = new Stopwatch();

      private List<CGaShape> MouseHoverings = new List<CGaShape>();
      private bool IsDragging { get=> this.GaAnimator.DragEdgeVisible; set=> this.GaAnimator.DragEdgeVisible = value; }

      private Tuple<CCsConnector, CCsConnector> GetDropInputAndOutput(CGaNode aDragNode, CGaNode aDropNode)
      {
         var aConnectors = this.Connectors;
         var aDragConnector = aConnectors.GetConnectorByName(aDragNode.Name);
         var aDropConnector = aConnectors.GetConnectorByName(aDropNode.Name);
         if(aDragConnector.IsChannel && aDropConnector.IsChannel)
         {
            if (aDragConnector.GetOutputActive(aDropConnector.Number))
            {
               // remove
               return new Tuple<CCsConnector, CCsConnector>(aDragConnector, aDropConnector);
            }
            else if(aDropConnector.GetOutputActive(aDragConnector.Number))
            {
               // remove
               return new Tuple<CCsConnector, CCsConnector>(aDropConnector, aDragConnector);
            }
            else
            {
               // add
               return new Tuple<CCsConnector, CCsConnector>(aDragConnector, aDropConnector); 
            }
         }
         else
         {
            var aIsInvert = aDragNode.Name == CChannel.OutName
                        ||  aDropNode.Name == CChannel.InName
                           ;
            var aOutput = !aIsInvert ? aDropConnector : aDragConnector;
            var aInput = !aIsInvert ? aDragConnector : aDropConnector;
            var aTuple = new Tuple<CCsConnector, CCsConnector>(aInput, aOutput);
            return aTuple;
         }
      }

      private void OnMouse2In(CInlet aInlet, string aFirstItem, CReadonlyListData aRemainingItems)
      {
         var aValues = aRemainingItems.ToArray();
         this.BeginInvokeInMainTask(delegate ()
         {
            if (aValues.Length >= 3)
            {
               var aX = Convert.ToInt32(aValues[0]);
               var aY = Convert.ToInt32(aValues[1]);
               var aPos = new CPoint(aX, aY);
               var aButton = Convert.ToInt32(aValues[2]);
               this.MousePos = aPos;
               this.GaAnimator.CursorPos = this.MousePos;

               { // Announce
                  var aDragNode = this.IsDragging
                                ? this.GetNodeNullable(this.GaAnimator.DragEdgeP1)
                                : default(CGaNode)
                                ;
                  var aHoverings = this.GetShapes(this.MousePos).OfType<CGaNode>();
                  foreach (var aHovering in aHoverings)
                  {
                     CDropEffectEnum aDropEffectEnum;
                     if(this.IsDragging)
                     {
                        var aConnectors = this.Connectors;
                        var aDropNode = aHovering;
                        var aDragConnector = aConnectors.GetConnectorByName(aDragNode.Name);
                        var aInputAndOutput = this.GetDropInputAndOutput(aDragNode, aDropNode);
                        var aInput = aInputAndOutput.Item1;
                        var aOutput = aInputAndOutput.Item2;
                        if (aDragNode.Name == aDropNode.Name)
                        {
                           aDropEffectEnum = CDropEffectEnum.None;
                        }
                        else
                        {
                           var aOldActive = aInput.GetOutputActive(aOutput.Number);
                           var aNewActive = !aOldActive;
                           aDropEffectEnum = aNewActive
                                           ? CDropEffectEnum.Add
                                           : CDropEffectEnum.Remove;
                        }
                     }
                     else
                     {
                        aDropEffectEnum = CDropEffectEnum.Focus;
                     }

                     aHovering.DropEffectEnum = aDropEffectEnum;
                     this.MouseHoverings.Add(aHovering);
                  }
                  var aOldHoverings = from aTest in this.MouseHoverings
                                      where !aHoverings.Contains(aTest)
                                      select aTest;
                  foreach (var aOldHovering in aOldHoverings.ToArray())
                  {
                     aOldHovering.DropEffectEnum = default(CDropEffectEnum?);
                     this.MouseHoverings.Remove(aOldHovering);
                  }
               }

               
               if (aButton != 0 && this.MouseButton == 0)
               {
                  if (this.DoubleClickStopWatch.IsRunning
                  && this.DoubleClickStopWatch.ElapsedMilliseconds <= SystemInformation.DoubleClickTime)
                  {
                     // DoubleClick
                     this.DoubleClickStopWatch.Stop();
                     var aEdgeNullable = this.GetEdgeNullable(this.MousePos);
                     if (aEdgeNullable is object)
                     {
                        // Deactivated by Edge.IsHitTestEnabled = false
                        var aConnectors = this.Connectors;
                        var aFromNode = aEdgeNullable.GaNode1;
                        var aToNode = aEdgeNullable.GaNode2;
                        var aFromConnector = aConnectors.GetConnectorByName(aFromNode.Name);
                        var aToConnector = aConnectors.GetConnectorByName(aToNode.Name);
                        aFromConnector.SetOutputActive(aToConnector.Number, false);
                     }
                     else
                     {
                        var aNodeNullable = this.GetNodeNullable(this.MousePos);
                        if(aNodeNullable is object)
                        {
                           var aConnector = this.Connectors.GetConnectorByName(aNodeNullable.Name);
                           aConnector.VstOpen();
                        }
                     }
                  }
                  else
                  {
                     this.DoubleClickStopWatch.Restart();
                     // ButtonDown                  
                     if (this.GetNodeNullable(this.MousePos) is object)
                     {
                        this.IsDragging = true;
                        this.GaAnimator.DragEdgeP1 = this.MousePos;
                        this.GaAnimator.DragEdgeP2 = this.MousePos;
                     }
                  }
                  this.MouseButton = aButton;
               }
               else if(aButton == 1 && this.MouseButton == 1)
               { // Dragging
                  this.GaAnimator.DragEdgeP2 = this.MousePos;
               }
               else if(aButton == 0 && this.MouseButton == 1)
               { // Drop/ButtonUp
                  bool aHandled;
                  if (this.IsDragging)
                  {
                     this.IsDragging = false;
                     var aDragNode = this.GetNodeNullable(this.GaAnimator.DragEdgeP1);
                     var aDropNode = this.GetNodeNullable(this.GaAnimator.DragEdgeP2);
                     if (aDragNode is object
                     && aDropNode is object
                     && aDragNode.Name != aDropNode.Name)
                     {
                        //var aConnectors = this.Connectors;
                        //var aDragConnector = aConnectors.GetConnectorByName(aDragNode.Name);
                        //var aDropConnector = aConnectors.GetConnectorByName(aDropNode.Name);
                        //var aDragNodeIsOut = aDragNode.Name == CChannel.OutName;
                        //var aOutput = !aDragNodeIsOut ? aDropConnector : aDragConnector;
                        //var aInput = !aDragNodeIsOut ? aDragConnector : aDropConnector;
                        var aInputAndOutput = this.GetDropInputAndOutput(aDragNode, aDropNode);
                        var aInput = aInputAndOutput.Item1;
                        var aOutput = aInputAndOutput.Item2;
                        var aOldActive = aInput.GetOutputActive(aOutput.Number);
                        var aNewActive = !aOldActive;
                        aInput.SetOutputActive(aOutput.Number, aNewActive);
                        var aConnectors = this.Connectors;
                        var aDropConnector = aConnectors.GetConnectorByName(aDropNode.Name);
                        this.Connectors.FocusedConnector = aDropConnector;
                        aHandled = true;
                     }
                     else
                     {
                        aHandled = false;
                     }
                  }
                  else
                  {
                     aHandled = false;
                  }                 
                  if(!aHandled)
                  {
                     var aDragNode = this.GetNodeNullable(this.GaAnimator.DragEdgeP1);
                     if (aDragNode is object)
                     {
                        this.Connectors.FocusedConnector = this.Connectors.GetConnectorByName(aDragNode.Name);
                     }
                  }
                  this.MouseButton = aButton;
               }
               if (!this.GaAnimator.NextGraphIsPending)
               {
                  this.Paint();
               }
            }
         });
      }

      /// <summary>
      /// TODO: Obsolete
      /// </summary>
      private enum CMouseButtonEventEnum
      {
         Up,
         Down
      }
      #endregion
      #region SampleRate
      internal int SampleRate;
      private void UpdateFlowMatrixSampleRate()
      {
         this.FlowMatrix.SampleRate = this.SampleRate;
      }
      private void OnSampleRate(CInlet aInlet, string aPrefix, CReadonlyListData aList)
      {
         var aSampleRate = Convert.ToInt32(aList.ElementAt(0));
         this.SampleRate = aSampleRate;
         this.UpdateFlowMatrixSampleRate();
         this.SendLatenciesOnDemand();
      }
      #endregion
      #region TimerThread
      private readonly CTimerThread TimerThread;
      #endregion
      #region Latency
      private readonly CTimer LatencyUpdateTimer;
      private void OnLatencyUpdateTimer(object aSender, EventArgs aArgs)
      {         
         this.LatencyUpdateTimer.Stop();
         try
         {
            var aConnectors = this.Connectors;
            var aLatencyChanged = false;
            foreach(var aConnector in aConnectors.Connectors)
            {
               aConnector.RequestNewLatency();
               if (aConnector.NewNodeLatency.HasValue
               && aConnector.NewNodeLatency.Value != aConnector.NodeLatency)
               {
                  aLatencyChanged = true;
               }
            }
            if(aLatencyChanged)
            {
               this.Connectors.CommitNewNodeLatencies(); 
               this.SendLatenciesOnDemand();
            }
         }
         finally
         { 
            this.LatencyUpdateTimer.Start();
         }
      }
      private void SendLatenciesOnDemand()
      {
         foreach (var aConnector in this.Connectors.Connectors)
         {
            aConnector.SendLatenciesOnDemand();
         }
      }
      #endregion
      #region SignalMatrixOut
      private readonly CListOutlet SignalMatrixOut;
      private void SendSignalMatrix()
      {
         this.SendSignalMatrix(true);
         this.SendSignalMatrix(false);
      }
      private void SendSignalMatrix(bool aRestOrSet)
      {
         var aIoCount = this.IoCount;
         var aListData = this.SignalMatrixOut.Message.Value;
         for (var aInIdx = 0; aInIdx < aIoCount; ++aInIdx)
         { 
            for(var aOutIdx = 0; aOutIdx < aIoCount; ++aOutIdx)
            {
               var aCellIdx = this.FlowMatrix.GetCellIdx(aInIdx, aOutIdx);
               bool aSend;
               bool aValue;
               if(aRestOrSet)
               {
                  aSend = true;
                  aValue = false;
               }
               else
               {
                  aValue = this.FlowMatrix.Actives[aCellIdx];
                  aSend = aValue;                  
               }
               if(aSend)
               {
                  var aValueInt = aValue ? 1 : 0;
                  this.SignalMatrixOut.SendValuesI(aInIdx, aOutIdx, aValueInt);
               }
            }
         }
      }
      #endregion
      #region GraphWizFolder
      private DirectoryInfo ConfigDir
      {
         get
         {
            var aDir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), typeof(CChannelStrip).FullName));
            aDir.Create();
            return aDir;
         }
      }
      private FileInfo GraphWizDirectoryConfigFileInfo { get => new FileInfo(Path.Combine(this.ConfigDir.FullName, "GraphWizDir.txt")); }
      private DirectoryInfo PersistentGraphWizDirectoryInfo
      {
         get => new DirectoryInfo(File.ReadAllText(this.GraphWizDirectoryConfigFileInfo.FullName).Trim());
         set => File.WriteAllText(this.GraphWizDirectoryConfigFileInfo.FullName,value.FullName);
      }
      private DirectoryInfo NewGraphWizDirectoryInfo()
      {
         try
         {
            return this.PersistentGraphWizDirectoryInfo;
         }
         catch(Exception)
         {
            var aDir = CGwDiagramLayout.GraphWizInstallDirDefault;
            try
            {               
               this.PersistentGraphWizDirectoryInfo = aDir;
            }
            catch (Exception) { }
            return aDir;
         }
      }
      private DirectoryInfo GraphWizDirectoryInfoM;
      private DirectoryInfo GraphWizDirectoryInfo
      {
         get => CLazyLoad.Get(ref this.GraphWizDirectoryInfoM, this.NewGraphWizDirectoryInfo);
         set
         {
            this.GraphWizDirectoryInfoM = value;
            try
            {
               this.PersistentGraphWizDirectoryInfo = value;
            }
            catch(Exception aExc)
            {
               this.WriteLogErrorMessage(aExc);
            }
         }
      }
      private void SendGraphWizFolder()
      {
         this.ControlOut.SendValuesS("graph_wiz_folder", this.GraphWizDirectoryInfo.FullName);
      }
      private void OnGraphWizFolder(CInlet aInlet, string aPrefix, CReadonlyListData aRemainingItems)
      {
         var aDirectoryInfo = new DirectoryInfo(aRemainingItems.ElementAt(0).ToString());
         this.GraphWizDirectoryInfo = aDirectoryInfo;
         this.NextGraph();
         this.SendGraphWizFolder();
      }
      #endregion
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
