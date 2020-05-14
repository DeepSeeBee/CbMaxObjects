﻿using System;
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
   using CbChannelStrip.GraphOverlay;
   using CbChannelStrip.GraphWiz;
   using CbMaxClrAdapter;
   using CbMaxClrAdapter.Jitter;
   using CbMaxClrAdapter.MGraphics;

   internal sealed class CSettings
   {
      internal DirectoryInfo GraphWizInstallDir { get =>new DirectoryInfo(@"C:\Program Files (x86)\Graphviz2.38\"); }
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
         this.FlowMatrix = new CFlowMatrix(this.WriteLogInfoMessage, this.Settings, 2, new bool[] { false, false, false, false });
         this.GraphOverlay = new CGraphOverlay(this.WriteLogErrorMessage,
                                               () => this.GwDiagramBuilder.GwGraph,
                                               this.OnGraphAvailable,
                                               this.OnGraphRequestPaint,
                                               this.WriteLogInfoMessage
                                               );
         this.PWindow2InOut = new CListOutlet(this);
         this.PWindow2InOut.Support(CMessageTypeEnum.List);
         this.PWindow2UpdateEnabledOut = new CIntOutlet(this);
      }

      private CGwDiagramBuilder GwDiagramBuilder { get => this.FlowMatrix.Routings.GwDiagramBuilder; }

      private readonly CListInlet MatrixCtrlLeftOutIn;
      private readonly CListInlet MatrixCtrlRightOutIn;

      private readonly CListOutlet MatrixCtrlLeftInOut;
      private readonly CMultiTypeOutlet PWindowInOut;

      private readonly CMultiTypeOutlet Vector2dOut;
      private readonly CListInlet Vector2dDumpIn;
      private readonly CListOutlet PWindow2InOut;

      private readonly CIntOutlet PWindow2UpdateEnabledOut;

      private Int32 IoCount;
      private bool RequestRowsPending;
      private Int32 RequestRowIdx;
      private Int32[][] Rows;
      private volatile CFlowMatrix FlowMatrix;

      private readonly CGraphOverlay GraphOverlay;

      private void OnGraphRequestPaint()
      {
         //this.WriteLogInfoMessage("OnGraphRequestPaint");

         this.RequestMainTaskActivity(delegate ()
         {
            //this.WriteLogInfoMessage("->SendGraphOverlay");
            this.SendGraphOverlay();
            this.GraphOverlay.OnPaintDone();
         });
      }
      
      private void OnGraphAvailable()
      {
         this.RequestMainTaskActivity(delegate ()
         {
            this.GraphOverlay.ProcessNewGraph();
         });
      }

      private bool PWindow2UpdateEnabled { get => this.PWindow2UpdateEnabledOut.Message.Value != 0; set { this.PWindow2UpdateEnabledOut.Message.Value = value ? 1 : 0; this.PWindow2UpdateEnabledOut.Send(); } }

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
         this.FlowMatrix = new CFlowMatrix(this.WriteLogInfoMessage, this.Settings, this.IoCount, aMatrix);

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
         this.SendGraphBitmap();
         this.GraphOverlay.NextGraph();
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


      private void SendPWindow2Size()
      {
         var aGraphOverlay = this.GraphOverlay;
         var aSize = aGraphOverlay.Size;        
         var aSizeList = this.PWindow2InOut.GetMessage<CList>().Value;
         aSizeList.Clear();
         aSizeList.Add("size");
         aSizeList.Add(aSize.X);
         aSizeList.Add(aSize.Y);
         this.PWindow2InOut.Send();
      }

      private void SendGraphOverlay()
      {
         var aPainter = new CVector2dPainter(this.Vector2dDumpIn, this.Vector2dOut);
         aPainter.Clear();
         //this.PWindow2UpdateEnabled = false;
         //try
         //{
            var aGraphOverlay = this.GraphOverlay;            
            var aSize = aGraphOverlay.Size;
            var aImageSurfaceSize = new CPoint(1000, 1000); // aPainter.ImageSurfaceSize;
            var aScale = aImageSurfaceSize / aSize;            
            this.SendPWindow2Size();
            aPainter.Scale(aScale);
            this.GraphOverlay.Paint(aPainter);
            this.Vector2dOut.Send(CMessageTypeEnum.Bang);
         //}
         //finally
         //{
         //   this.PWindow2UpdateEnabled = true;
         //}
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
      public static void Test(Action<string> aFailAction, Action<string> aDebugPrint)
      {
         CFlowMatrix.Test(aFailAction, aDebugPrint);
         CGwDiagramBuilder.Test(aFailAction, aDebugPrint);
      }

      protected override void OnShutdown()
      {
         base.OnShutdown();

         this.GraphOverlay.Shutdown();
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
