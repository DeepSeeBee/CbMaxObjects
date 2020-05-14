using CbChannelStrip.Graph;
using CbChannelStrip.GraphWiz;
using CbMaxClrAdapter;
using CbMaxClrAdapter.Jitter;
using CbMaxClrAdapter.MGraphics;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Documents;
using System.Windows.Media.Converters;
using System.Windows.Threading;

namespace CbChannelStrip.GraphAnimator
{
   using CWorkerResult = Tuple<BackgroundWorker, CGraphAnimator.CState>;

   internal abstract class COvShape
   {
      internal static readonly Color DefaultColor = Color.Black;

      internal readonly COvGraph OvGraph;
      internal readonly CGraphAnimator GraphAnimator;
      internal COvShape(COvGraph aOvGraph)
      {
         this.OvGraph = aOvGraph;
         this.GraphAnimator = aOvGraph.GraphAnimator;
      }

      private volatile object OpacityM = (double)0.0d;
      internal double Opacity { get => (double)this.OpacityM; set => this.OpacityM = value; }
      internal abstract string Name { get; }
      internal abstract void Paint(CVector2dPainter aOut);

      internal virtual void AnimateAppear(double aPercent)
      {
         this.Opacity = 1.0d - aPercent;
      }

      internal virtual void AnimateDisappear(double aPercent)
      {
         this.Opacity = aPercent;
      }
      internal abstract COvMorph NewMorph(COvNode aNewNode);
      internal abstract COvMorph NewMorph(COvEdge aNewEdge);
      internal abstract COvMorph AcceptNewMorph(COvShape aOldShape);
      internal virtual void Init() { }
   }

   internal sealed class COvEdge : COvShape
   {
      internal COvEdge(COvGraph aOvGraph, CGwEdge aGwEdge, COvNode aOvNode1, COvNode aOvNode2) :base(aOvGraph)
      {
         this.GwEdge = aGwEdge;
         this.Splines = (from aSpline in aGwEdge.Splines select new CPoint(aSpline.Item1, aSpline.Item2)).ToArray();
         this.OvNode1 = aOvNode1;
         this.OvNode2 = aOvNode2;
         this.Color = aGwEdge.Color;
      }
      internal readonly CGwEdge GwEdge;
      internal override string Name { get => this.GwEdge.Name; }
      internal COvEdge CopyEdge() => new COvEdge(this.OvGraph, this.GwEdge, this.OvNode1, this.OvNode2);

      internal readonly COvNode OvNode1;
      internal readonly COvNode OvNode2;
      private object ColorM = default(Color?);
      internal Color? Color { get => (Color?)this.ColorM; set => this.ColorM = value; }
      internal override void Paint(CVector2dPainter aOut)
      {
         //this.GraphAnimator.DebugPrint("COvEdge.Paint //2");
         var aBezier = this.Splines;
         var aSplines = aBezier.Skip(1);
         //this.GraphAnimator.DebugPrint(aSplines);
         var aBaseColor = this.Color.GetValueOrDefault(DefaultColor);
         var aOpacity = this.Opacity;
         var aAlpha = 1.0d - aOpacity;
         var aColor = System.Drawing.Color.FromArgb((int)(aAlpha * 255.0d), aBaseColor);         
         aOut.NewPath();
         aOut.SetColor(aColor);
         var aFirst = true;
         foreach (var aPoint in aSplines)
         {
            if (aFirst)
               aOut.MoveTo(aPoint);
            else
               aOut.LineTo(aPoint);
            aFirst = false;
         }
         aOut.Stroke();

         { // DrawArrowTip
            var aTip = aBezier.First();
            var aP1 = aBezier.Last();
            var aP2 = aTip - aP1;
            var a90 = Math.PI / 2.0d;
            var aLen = new CPoint(0.75d, 0.75d);
            var aC1 = aP2.Rotate(a90) * aLen + aP1;
            var aC2 = aP2.Rotate(-a90) * aLen + aP1;
            aOut.NewPath();
            aOut.MoveTo(aTip);
            aOut.LineTo(aC1);
            aOut.LineTo(aC2);
            aOut.ClosePath();
            aOut.Fill();
         }
      }
      internal override COvMorph NewMorph(COvEdge aNewEdge) => new COvEdgeMorph(this, aNewEdge);
      internal override COvMorph NewMorph(COvNode aNewNode) => throw new InvalidOperationException();
      internal override COvMorph AcceptNewMorph(COvShape aOldShape) => aOldShape.NewMorph(this);

      internal volatile CPoint[] Splines;
   }

   internal sealed class COvNode : COvShape
   {
      internal COvNode(COvGraph aGraph, CGwNode aGwNode):base(aGraph)
      {
         this.GwNode = aGwNode;
         this.Pos = new CPoint(aGwNode.X, aGwNode.Y);
         this.Color = aGwNode.Color;
         this.FontColor = aGwNode.FontColor;
      }
      
      internal COvNode CopyNode() => new COvNode(this.OvGraph, this.GwNode);

      internal readonly CGwNode GwNode;
      internal override string Name => this.GwNode.Name;
       
      public CPoint Pos { get; set; }

      private volatile object ScaleM = (double)1.0d;
      internal double Scale { get => (double)this.ScaleM; set => this.ScaleM = value; }

      private object ColorM = default(Color?);
      internal Color? Color { get => (Color?)this.ColorM; set => this.ColorM = value; }
      private object FontColorM = default(Color?);
      internal Color? FontColor { get => (Color?)this.FontColorM; set => this.FontColorM = value; }

      internal override void AnimateAppear(double aPercent)
      {
         base.AnimateAppear(aPercent);

         this.GraphAnimator.DebugPrint("AnimateAppear.Percent=" + aPercent);
         this.Scale = aPercent;
      }

      internal override void AnimateDisappear(double aPercent)
      {
         base.AnimateDisappear(aPercent);
         this.Scale = 1.0d - aPercent;
      }

      internal override void Paint(CVector2dPainter aOut)
      {
         //this.GraphAnimator.DebugPrint("COvNode.Paint.Begin.");
         var aScale = this.Scale;
         var aDx = this.GwNode.Dx * aScale;
         var aDy = this.GwNode.Dy * aScale;
         var aX = this.Pos.X - aDx / 2.0d;
         var aY = this.Pos.Y - aDy / 2.0d;
         var aPos = new CPoint(aX, aY);
         var aRect = new CRectangle(aX, aY, aDx, aDy);
         var aText = this.Name;
         var aOpacity = this.Opacity;
         var aAlpha = 1.0d - aOpacity;
         var aDefaultColor = DefaultColor;
         var aBaseColor = this.Color.GetValueOrDefault(aDefaultColor);
         var aColor = System.Drawing.Color.FromArgb((int)(aAlpha * 255.0d), aBaseColor);
         aOut.SetColor(aColor);
         switch(this.GwNode.ShapeEnum)
         {
            case CGwNode.CShapeEnum.InvTriangle:
               aOut.NewPath();
               aOut.MoveTo(aX, aY);
               aOut.LineTo(aX + aDx, aY);
               aOut.LineTo(aX + aDx / 2.0d, aY + aDy);
               aOut.ClosePath();
               aOut.Stroke();
               break;

            case CGwNode.CShapeEnum.Triangle:
               aOut.NewPath();
               aOut.MoveTo(aX + aDx / 2.0d, aY); 
               aOut.LineTo(aX + aDx, aY + aDy);
               aOut.LineTo(aX, aY + aDy);
               aOut.ClosePath();
               aOut.Stroke();
               break;

            case CGwNode.CShapeEnum.MCircle:
               aOut.Ellipse(aX, aY, aDx, aDy);
               aOut.Stroke();
               aOut.MoveTo(aX, aY);
               break;

            case CGwNode.CShapeEnum.MSquare:
               aOut.Rectangle(aRect);
               break;
         }
         if (aScale >= 1.0d)
         {
            var aFontBaseColor = this.GwNode.FontColor.GetValueOrDefault(aDefaultColor);
            var aFontColor = System.Drawing.Color.FromArgb((int)(aAlpha * 255.0d), aBaseColor);
            aOut.SetColor(aFontColor);
            aOut.Text(aText, aRect);
         }
         //this.GraphAnimator.DebugPrint("COvNode.Paint.End.");
      }

      internal override COvMorph NewMorph(COvEdge aNewEdge) => throw new InvalidOperationException();
      internal override COvMorph NewMorph(COvNode aNewNode) => new COvNodeMorph(this, aNewNode);
      internal override COvMorph AcceptNewMorph(COvShape aOldShape) => aOldShape.NewMorph(this);
   }

   internal sealed class COvGraph : IEnumerable<COvShape>
   {
      internal COvGraph(CGraphAnimator aGraphAnimator, CPoint aSize, IEnumerable<COvShape> aShapes)
      {
         //aGraphAnimator.DebugPrint("COvGraph.COvGraph(): Shapes.Count=" + aShapes.Count());
         this.Size = aSize;
         this.GraphAnimator = aGraphAnimator;
         foreach (var aShape in aShapes)
         {
            this.ShapesDic.Add(aShape.Name, aShape);
         }
      }

      internal COvGraph(CGraphAnimator aGraphAnimator, CGwGraph aGwGraph)
      {
         //aGraphAnimator.DebugPrint("COvGraph.COvGraph(): GwGraph.Nodes.Count=" + aGwGraph.Nodes.Count());
         aGraphAnimator.DebugPrint("COvGraph.COvGraph(): GwGraph.Edges.Count=" + aGwGraph.Edges.Count());
         this.Size = new CPoint(aGwGraph.Size);
         this.GraphAnimator = aGraphAnimator;

         foreach (var aGwNode in aGwGraph.Nodes)
         {
            var aOvNode = new COvNode(this, aGwNode);
            this.ShapesDic.Add(aOvNode.Name, aOvNode);
         }
         foreach (var aGwEdge in aGwGraph.Edges)
         {
            var aOvEdge = new COvEdge(this, 
                                      aGwEdge, 
                                      (COvNode)this.ShapesDic[aGwEdge.Node1Name], 
                                      (COvNode)this.ShapesDic[aGwEdge.Node2Name]);
            this.ShapesDic.Add(aOvEdge.Name, aOvEdge);
         }
         foreach (var aShape in this.ShapesDic.Values)
            aShape.Init();
      }

      internal readonly CPoint Size;

      internal readonly CGraphAnimator GraphAnimator;
      internal COvGraph(CGraphAnimator aGraphAnimator) :this(aGraphAnimator, new CGwGraph())
      {
      }

      internal Dictionary<string, COvShape> ShapesDic = new Dictionary<string, COvShape>();
      public IEnumerator<COvShape> GetEnumerator() => this.ShapesDic.Values.GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
   }

   //internal abstract class CMatrixMorph
   //{
   //   internal CMatrixMorph(CMatrixData aOldMatrix, CMatrixData aNewMatrix, CMatrixData aMorphed)
   //   {
   //      this.OldMatrix = aOldMatrix;
   //      this.NewMatrix = aNewMatrix;
   //      this.Morphed = aMorphed;
   //      this.MatrixCellEnumerator = aOldMatrix.GetCellEnumerator();
   //      this.PlaneCount = aOldMatrix.PlaneCount;
   //      this.CheckCompatible();
   //   }

   //   public readonly int PlaneCount;
   //   public readonly CMatrixData OldMatrix;
   //   public readonly CMatrixData NewMatrix;
   //   public readonly CMatrixData Morphed;
   //   internal readonly CMatrixCellEnumerator MatrixCellEnumerator;

   //   private void CheckCompatible()
   //   {
   //      this.OldMatrix.CheckCompatible(this.NewMatrix);
   //      this.OldMatrix.CheckCompatible(this.Morphed);
   //   }

   //   protected abstract void Morph(int[] aPos);

   //   public double MorphPercent;

   //   internal void Morph()
   //   {
   //      this.CheckCompatible();
   //      var aPlaneCount = this.OldMatrix.PlaneCount;
   //      var aEnumerator = this.MatrixCellEnumerator;
   //      aEnumerator.Reset();
   //      while(aEnumerator.MoveNext())
   //      {
   //         this.Morph(aEnumerator.Pos);
   //      }
   //   }
   //}


   //internal sealed class CLinearMatrixMorphFloat64 : CMatrixMorph
   //{
   //   internal CLinearMatrixMorphFloat64(CMatrixData aOldMatrix, CMatrixData aNewMatrix, CMatrixData aMarphed) :base(aOldMatrix, aNewMatrix, aMarphed)
   //   {
   //   }
   //   protected override void Morph(int[] aPos)
   //   {
   //      var aPercent = this.MorphPercent;
   //      for(int aPlane = 0; aPlane < this.PlaneCount; ++aPlane)
   //      {
   //         var aOldValue = this.OldMatrix.GetCellFloat64(aPos, aPlane);
   //         var aNewValue = this.NewMatrix.GetCellFloat64(aPos, aPlane);
   //         double aMorphValue;
   //         if (aOldValue > aNewValue)
   //         {
   //            aMorphValue = aNewValue + (aOldValue - aNewValue) * aPercent;
   //         }
   //         else if (aOldValue < aNewValue)
   //         {
   //            aMorphValue = aOldValue - (aNewValue - aOldValue) * aPercent;
   //         }
   //         else
   //         {
   //            aMorphValue = aOldValue;
   //         }
   //         this.Morphed.SetCellFloat64(aPos, aPlane, aMorphValue);
   //      }
   //   }
   //}

   internal abstract class COvMorph
   {

      private object MorphPercentM = (double)0.0d;
      internal double MorphPercent { get => (double)this.MorphPercentM; set => this.MorphPercentM = value; }

      internal double MorphDouble(double aOld, double aNew)=>aOld + (aNew - aOld) * this.MorphPercent;
      internal int MorphInt(int aOld, int aNew) => (int)this.MorphDouble(aOld, aNew);
      internal CPoint MorphPoint(CPoint aOld, CPoint aNew) => new CPoint(this.MorphDouble(aOld.X, aNew.X), this.MorphDouble(aOld.Y, aNew.Y));
      
      internal CPoint[] MorphPoints(CPoint[] aOld, CPoint[] aNew)
      {
         if (aOld.Length == aNew.Length)
         {
            return (from aIdx in Enumerable.Range(0, aOld.Length) select this.MorphPoint(aOld[aIdx], aNew[aIdx])).ToArray();
         }
         else
         {
            throw new ArgumentException("COvMorph.MorpPoints: Points.Length missmatch.");
         }
      }
      internal Color? MorphColor(Color? aOld, Color? aNew) 
      {
         if (!aOld.HasValue
         || !aNew.HasValue)
            return default(Color?);
         else
            return Color.FromArgb(this.MorphInt(aOld.Value.A, aNew.Value.A),
                                  this.MorphInt(aOld.Value.R, aNew.Value.R),
                                  this.MorphInt(aOld.Value.G, aNew.Value.G),
                                  this.MorphInt(aOld.Value.B, aNew.Value.B)
                                  );
      }
      internal abstract void Morph();
      internal abstract COvShape MorphedShape { get; }
   }

   internal sealed class COvNodeMorph : COvMorph
   {
      internal COvNodeMorph(COvNode aOldNode, COvNode aNewNode)
      {
         this.OldNode = aOldNode;
         this.NewNode = aNewNode;
         this.MorphedNode = aOldNode.CopyNode();
      }
      internal readonly COvNode OldNode;
      internal readonly COvNode NewNode;
      internal readonly COvNode MorphedNode;
      internal override COvShape MorphedShape => this.MorphedNode;
      internal override void Morph()
      {
         this.MorphedNode.Pos = this.MorphPoint(this.OldNode.Pos, this.NewNode.Pos);
         this.MorphedNode.Color = this.MorphColor(this.OldNode.Color, this.NewNode.Color);
         this.MorphedNode.FontColor = this.MorphColor(this.OldNode.FontColor, this.NewNode.FontColor);
      }


   }
   internal sealed class COvEdgeMorph : COvMorph
   {
      internal COvEdgeMorph(COvEdge aOldEdge, COvEdge aNewEdge)
      {
         this.OldEdge = aOldEdge;
         this.NewEdge = aNewEdge;
         this.MorphedEdge = aOldEdge.CopyEdge();
         var aOld1 = this.OldEdge.Splines;
         var aNew1 = this.NewEdge.Splines;
         CPoint[] aOld2;
         CPoint[] aNew2;
         if (aOld1.Length > aNew1.Length)
         {
            if(aNew1.Length == 0)
            {
               throw new NotImplementedException();
            }
            else
            {
               var aDiff = aOld1.Length - aNew1.Length;
               var aAdd = from aIdx in Enumerable.Range(0, aDiff) select aNew1.Last();
               aOld2 = aOld1;
               aNew2 = aNew1.Concat(aAdd).ToArray();
            }
         }
         else if (aNew1.Length > aOld1.Length)
         {
            var aDiff = aNew1.Length - aOld1.Length;
            var aAdd = from aIdx in Enumerable.Range(0, aDiff) select aOld1.Last();
            aOld2 = aOld1.Concat(aAdd).ToArray();
            aNew2 = aNew1;
         }
         else
         {
            aOld2 = aOld1;
            aNew2 = aNew1;
         }
         this.OldPoints = aOld2;
         this.NewPoints = aNew2;
      }
      internal readonly COvEdge OldEdge;
      internal readonly CPoint[] OldPoints;
      internal readonly COvEdge NewEdge;
      internal readonly CPoint[] NewPoints;
      internal readonly COvEdge MorphedEdge;
      internal override COvShape MorphedShape => this.MorphedEdge;
      internal override void Morph()
      {
         this.MorphedEdge.Splines = this.MorphPoints(this.OldPoints, this.NewPoints);
         this.MorphedEdge.Color = this.MorphColor(this.OldEdge.Color, this.NewEdge.Color);
      }
   }

   internal sealed class CGraphTransition
   {
      internal CGraphTransition(CGraphAnimator aGraphAnimator, CPoint aSize, COvGraph aOldGraph, COvGraph aNewGraph)
      {
         this.GraphAnimator = aGraphAnimator;
         var aKeys = aOldGraph.ShapesDic.Keys.Concat(aNewGraph.ShapesDic.Keys);
         var aMorphingKeys = from aKey in aKeys
                           where aOldGraph.ShapesDic.ContainsKey(aKey)
                           where aNewGraph.ShapesDic.ContainsKey(aKey)
                           select aKey;
         var aDisappearingKeys = from aKey in aKeys
                                 where aOldGraph.ShapesDic.ContainsKey(aKey)
                                 where !aNewGraph.ShapesDic.ContainsKey(aKey)
                                 select aKey
                                 ;
         var aAppearingKeys = from aKey in aKeys
                              where !aOldGraph.ShapesDic.ContainsKey(aKey)
                              where aNewGraph.ShapesDic.ContainsKey(aKey)
                              select aKey
                                 ;
         var aMorphings = new Dictionary<string, COvMorph>();
         foreach (var aKey in aMorphingKeys)
         {
            if(!aMorphings.ContainsKey(aKey))
            {
               var aOldShape = aOldGraph.ShapesDic[aKey];
               var aNewShape = aNewGraph.ShapesDic[aKey];
               var aMorph = aNewShape.AcceptNewMorph(aOldShape);
               aMorphings[aKey] = aMorph;
            }
         }
         var aDisappearings = new List<COvShape>();
         foreach (var aKey in aDisappearingKeys)
         {
            aDisappearings.Add(aOldGraph.ShapesDic[aKey]);
         }
         var aAppearings = new List<COvShape>();
         foreach (var aKey in aAppearingKeys)
         {
            aAppearings.Add(aNewGraph.ShapesDic[aKey]);
         }
         var aMorphShapes1 = (from aMorph in aMorphings.Values select aMorph.MorphedShape);
         var aMorphShapes2 = (aDisappearings.Concat(aAppearings).Concat(aMorphShapes1));
         var aMorpShapes = (from aGroup in aMorphShapes2.GroupBy(aShape => aShape) select aGroup.Key).ToArray();
         var aMorphGraph = new COvGraph(this.GraphAnimator, aSize, aMorpShapes);
         foreach (var aAppearing in aAppearings)
         {
            aAppearing.AnimateAppear(0.0d);
         }
         foreach(var aDisappearing in aDisappearings)
         {
            aDisappearing.AnimateDisappear(0.0d);
         }
         this.OldGraph = aOldGraph;
         this.NewGraph = aNewGraph;
         this.MorphGraph = aMorphGraph;
         this.Morphings = aMorphings;
         this.Disappearings = aDisappearings; 
         this.Appearings = aAppearings;
      }

      internal readonly CGraphAnimator GraphAnimator;
      internal readonly COvGraph OldGraph; 
      internal readonly COvGraph NewGraph;
      internal readonly COvGraph MorphGraph;
      internal readonly Dictionary<string, COvMorph> Morphings;
      internal readonly List<COvShape> Disappearings;
      internal readonly List<COvShape> Appearings;
      internal CAnimStateEnum AnimState = CAnimStateEnum.OldGraph;

   }
   internal enum CAnimStateEnum
   {
      OldGraph,
      Working,
      Disappear,
      Move,
      Appear,
      NewGraph
   }

   public sealed class CGraphAnimator
   {
      internal CGraphAnimator(Action<Exception> aOnExc,
                             Func<CGwGraph> aCalcNewGwGraph,
                             Action aNotifyResult,
                             Action aNotifyPaint,
                             Action<string> aDebugPrint)
      {
         this.ExternDebugPrint = aDebugPrint;
         this.OnExc = aOnExc;      
         this.State = new CState(this, new COvGraph(this)); 
         this.AnimationThread = new System.Threading.Thread(RunAnimationThread);
         this.CalcNewGwGraph = aCalcNewGwGraph;
         this.NotifyResult = aNotifyResult;
         this.NotifyPaint = aNotifyPaint;         
         this.AnimationThread.Start();
      }
      
      public static void Test(Action<string> aFailAction, Action<string> aDebugPrint)
      {
         var aDispatcherFrame = default(DispatcherFrame);
         var aDispatcher = default(Dispatcher);
         var aBackgroundWorker = new BackgroundWorker();
         var aBackgroundWorkerReady = new AutoResetEvent(false);
         aBackgroundWorker.DoWork += new DoWorkEventHandler(delegate (object aSender, DoWorkEventArgs aArgs)
         {
            aDispatcher = Dispatcher.CurrentDispatcher;
            aDispatcherFrame = new DispatcherFrame();
            aBackgroundWorkerReady.Set();
            Dispatcher.PushFrame(aDispatcherFrame);
         });
         aBackgroundWorker.RunWorkerAsync();
         aBackgroundWorkerReady.WaitOne();
         var aOnExc = new Action<Exception>(delegate (Exception aExc) { aDebugPrint(aExc.Message); });
         var aCalcNewGwGraph = new Func<CGwGraph>(() => CFlowMatrix.NewTestFlowMatrix2(aDebugPrint).Routings.GwDiagramBuilder.GwGraph); //CGwGraph.NewTestGraph(aDebugPrint)); ;  ;
         var aGraphAnimator = default(CGraphAnimator);
         var aNotifyResult = new Action(delegate () { aDispatcher.BeginInvoke(new Action(delegate () { aGraphAnimator.ProcessNewGraph(); })); });
         var aNotifyPaint = new Action(delegate () { aDispatcher.BeginInvoke(new Action(delegate () { aGraphAnimator.OnPaintDone(); })); });
         aGraphAnimator = new CGraphAnimator(aOnExc, aCalcNewGwGraph, aNotifyResult, aNotifyPaint, aDebugPrint);
         aGraphAnimator.NextGraph();
         System.Console.WriteLine("press any key to shutdown CGraphAnimator test");
         System.Console.ReadKey();
         aGraphAnimator.Shutdown();
         aDispatcherFrame.Continue = false;
      }

      private Action<string> ExternDebugPrint;
      internal void DebugPrint(string aMsg) => this.ExternDebugPrint(aMsg); // System.Diagnostics.Debug.Print(aMsg);
      internal void DebugPrint(IEnumerable<CPoint> aPoints) => this.DebugPrint((from aPoint in aPoints select aPoint.X.ToString() + ", " + aPoint.Y.ToString()).JoinString(" "));
      private Action NotifyResult;
      private Action NotifyPaint;
      private Action<Exception> OnExc;
      private readonly Func<CGwGraph> CalcNewGwGraph;    
      private BackgroundWorker WorkerNullable;
      private volatile bool PaintIsPending;      
      internal void OnPaintDone()
      {
         this.PaintIsPending = false;
      }

      internal sealed class CState
      {
         internal CState(CGraphAnimator aGraphAnimator, COvGraph aGraph)
         {
            this.GraphAnimator = aGraphAnimator;
            this.OldGraph = aGraph;
            this.NewGraph = aGraph;
            this.GraphTransition = new CGraphTransition(aGraphAnimator, this.Size, aGraph, aGraph);
            this.WorkingAnimation = new CWorkingAnimation(this);
            this.AppearAnimationNullable = default;
            this.MoveAnimationNullable = default;
            this.AppearAnimationNullable = default;
         }

         internal CState(CGraphAnimator aGraphAnimator, CState aOldState, COvGraph aNewGraph)
         {
            this.GraphAnimator = aGraphAnimator;
            this.OldGraph = aOldState.NewGraph;
            this.NewGraph = aNewGraph;
            this.GraphTransition = new CGraphTransition(aGraphAnimator, this.Size, this.OldGraph, this.NewGraph);
            this.WorkingAnimation = new CWorkingAnimation(this);
            this.FadeOutAnimationNullable = new CDisappearAnimation(this);
            this.MoveAnimationNullable = new CMoveAnimation(this);
            this.AppearAnimationNullable = new CAppearAnimation(this);            
         }

         internal readonly CGraphAnimator GraphAnimator;
         internal readonly COvGraph OldGraph;
         internal readonly COvGraph NewGraph;
         internal readonly CGraphTransition GraphTransition;
         internal readonly CWorkingAnimation WorkingAnimation;
         internal readonly CDisappearAnimation FadeOutAnimationNullable;
         internal readonly CMoveAnimation MoveAnimationNullable;
         internal readonly CAppearAnimation AppearAnimationNullable;
         
         //internal CAnimState AnimState;

         internal IEnumerable<CAnimation> RunningAnimations
         {
            get
            {
               if (this.WorkingAnimation.IsRunning)
                  yield return this.WorkingAnimation;
               if (this.FadeOutAnimationNullable is object
               && this.FadeOutAnimationNullable.IsRunning)
                  yield return this.FadeOutAnimationNullable;
               if (this.MoveAnimationNullable is object
               && this.MoveAnimationNullable.IsRunning)
                  yield return this.MoveAnimationNullable;
                  if (this.AppearAnimationNullable is object
               && this.AppearAnimationNullable.IsRunning)
                  yield return this.AppearAnimationNullable;
            }
         }

         internal CPoint Size { get => new CPoint(Math.Max(this.OldGraph.Size.X, this.NewGraph.Size.X),
                                                  Math.Max(this.OldGraph.Size.Y, this.NewGraph.Size.Y)); }
      }

      internal volatile CState State;

      private void CancelWorkerOnDemand()
      {
         //this.DebugPrint("CGraphAnimator.CancelWorkerOnDemand.Begin");
         lock (this)
         {
            var aWorker = this.WorkerNullable;
            if(aWorker is object)
            {
               this.WorkerNullable = default;
               this.RemoveWorkerCallbacks(aWorker);
            }
         }
         //this.DebugPrint("CGraphAnimator.CancelWorkerOnDemand.End");
      }
      private bool IsCurrentWorker(BackgroundWorker aWorker)
      {
         return object.ReferenceEquals(this.WorkerNullable, aWorker);
      }

      private void StartWorker()
      {
         //this.DebugPrint("CGraphAnimator.StartWorker");
         this.CancelWorkerOnDemand();
         var aWorker = new BackgroundWorker();
         this.WorkerNullable = aWorker;
         this.AddWorkerCallbacks(aWorker);         
         this.State.WorkingAnimation.Start();
         aWorker.RunWorkerAsync(this.State);
      }

      private void AddWorkerCallbacks(BackgroundWorker aWorker)
      {
         aWorker.DoWork += this.BackgroundWorkerDoWork;
         aWorker.RunWorkerCompleted += this.BackgroundWorkerRunWorkerCompleted;
      }
      private void RemoveWorkerCallbacks(BackgroundWorker aWorker)
      {
         aWorker.DoWork -= this.BackgroundWorkerDoWork;
         aWorker.RunWorkerCompleted -= this.BackgroundWorkerRunWorkerCompleted;
      }

      internal void NextGraph()
      {
         this.StartWorker();
      }

      private void BackgroundWorkerDoWork(object aSender, DoWorkEventArgs aArgs)
      {
         System.Threading.Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
         //this.DebugPrint("CGraphAnimator.BackgroundWorkerDoWork.Begin");
         var aOldState = (CState)aArgs.Argument;
         var aNewGwGraph = this.CalcNewGwGraph();
         var aNewOvGraph = new COvGraph(this, aNewGwGraph);
         var aNewState = new CState(this, aOldState, aNewOvGraph);
         aArgs.Result = aNewState;
         //this.DebugPrint("CGraphAnimator.BackgroundWorkerDoWork.End");
      }


      private void BackgroundWorkerRunWorkerCompleted(object aSender, RunWorkerCompletedEventArgs aArgs)
      {
         //this.DebugPrint("CGraphAnimator.BackgroundWorkerRunWorkerCompleted");
         CState aNewState;
         var aWorker = (BackgroundWorker)aSender;
         if(aArgs.Error is object)
         {
            this.OnExc(new Exception("Error calculating GraphMorph.NewGraph. " + aArgs.Error.Message, aArgs.Error));
            aNewState = default;
         }
         else if(aArgs.Cancelled)
         {
            // nix.
            aNewState = default;
         }
         else if(aArgs.Result is object)
         {
            aNewState = (CState)aArgs.Result;               
         }
         else
         {
            //this.OnExc(new Exception("No result when calculating GraphMorph.NewGraph."));
            aNewState = default;
         }
         if(aNewState is object)
         {
            this.AddWorkerResult(new CWorkerResult(aWorker, aNewState));
         }
      }
      private void AddWorkerResult(CWorkerResult aWorkerResult)
      {
         //this.DebugPrint("CGraphAnimator.AddWorkerResult.Begin");
         lock (this.WorkerResults)
         {
            this.WorkerResults.Add(aWorkerResult);
            this.NotifyResult();
         }
         //this.DebugPrint("CGraphAnimator.AddWorkerResult.End");
      }


      private readonly List<CWorkerResult> WorkerResults = new List<Tuple<BackgroundWorker, CState>>();
      private CWorkerResult PeekWorkerResultNullable()
      {
         //this.DebugPrint("CGraphAnimator.PeekWorkerResultNullable.Begin");
         lock (this.WorkerResults)
         {
            if(!this.WorkerResults.IsEmpty())
            {
               var aWorkerResult = this.WorkerResults.Last();
               this.WorkerResults.Clear();
               return aWorkerResult;
            }
         }
         //this.DebugPrint("CGraphAnimator.PeekWorkerResultNullable.End");
         return default;
      }
      internal void ProcessNewGraph()
      {
         //this.DebugPrint("CGraphAnimator.ProcessNewGraph");
         var aResult = this.PeekWorkerResultNullable();
         if(aResult is object
         && this.IsCurrentWorker(aResult.Item1))
         {
            this.WorkerNullable = default;
            this.RemoveWorkerCallbacks(aResult.Item1);
            this.State.WorkingAnimation.Finish();
            this.State = aResult.Item2;
            this.State.FadeOutAnimationNullable.Start();
         }
      }

      private readonly System.Threading.Thread AnimationThread;

      internal void Shutdown()
      {
         this.CancelWorkerOnDemand();
         this.StopAnimationThread = true;
         this.AnimationThread.Join();
      }

      private bool StopAnimationThread;

      internal CPoint Size => this.State.Size;

      private void RunAnimationThread(object aObj)
      {
         System.Threading.Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
         var aStopWatch = new Stopwatch();
         aStopWatch.Start();
         while (!this.StopAnimationThread)
         {
            try
            {
               System.Threading.Thread.Sleep(100);
               if (!this.PaintIsPending)
               {
                  var aElapsed = aStopWatch.ElapsedMilliseconds;
                  aStopWatch.Stop();
                  aStopWatch.Start();
                  if (this.Animate(aStopWatch.ElapsedMilliseconds))
                  {
                     this.Paint();
                  }
               }
            }
            catch (Exception aExc)
            {
               this.DebugPrint("AnimationThread: " + aExc.Message);
            }
         }
      }

      private bool Animate(long aElapsedMilliseconds)
      {
         var aBusy = false;
         var aState = this.State;
         foreach(var aAnimation in aState.RunningAnimations)
         {
            aAnimation.Animate(aElapsedMilliseconds);
            aBusy = true;
         }
         return aBusy;
      }

      internal abstract class CAnimation
      {
         internal CAnimation(CState aState)
         {
            this.State = aState;
         }
         internal readonly CState State;
         
         internal bool IsRunning { get; private set; }

         internal virtual void OnStart()
         {
         }

         internal void Start()
         {
            this.FrameLen = 0;
            this.Stopwatch.Start();
            this.IsRunning = true;            
            this.OnStart();
            //this.State.GraphAnimator.DebugPrint(this.GetType().Name + "started.");
         }

         internal void Stop()
         {
            this.IsRunning = false;
            //this.State.GraphAnimator.DebugPrint(this.GetType().Name + "stopped.");
         }
          
         internal void Finish()
         {
          //  this.Animate(0);
            this.Stop();
            this.OnFinish();            
            this.State.GraphAnimator.DebugPrint(this.GetType().Name + "finished.");
         }

         internal virtual void OnFinish()
         {
         }


         internal  bool RepaintIsPending { get => this.State.GraphAnimator.PaintIsPending; }
         private Stopwatch Stopwatch = new Stopwatch();

         internal void Paint()
         {
            this.State.GraphAnimator.Paint();
         }

         private long FrameLen;

         internal virtual long? MaxDuration { get => default(long?); }
         internal const long MaxDurationDefault = 1000;
         internal virtual void OnAnimate(long aFrameLen)
         {
         }

         internal virtual long TotalElapsed 
         {
            get 
            {
               var aMaxDuration = this.MaxDuration;
               var aElapsed = this.Stopwatch.ElapsedMilliseconds;
               return aElapsed;
            }
         }
         internal double PercentLin { get => ((double)Math.Min(this.MaxDuration.Value, this.TotalElapsed)) / ((double)this.MaxDuration.Value); }
         internal double PercentExp { get => 1.0d - Math.Pow((1.0d - this.PercentLin) * 10, 2) / 100.0d; }
         internal double Percent { get => this.PercentExp; }
         internal void Animate(long aElapsed)
         {
            this.FrameLen += aElapsed;
            if (!this.RepaintIsPending)
            {
               var aMaxDuration = this.MaxDuration;
               var aTotalElapsed = this.TotalElapsed;
               bool aDone;
               long aFrameLen2;
               if (aMaxDuration.HasValue
               && aTotalElapsed > aMaxDuration)
               {
                  aFrameLen2 = aTotalElapsed - aMaxDuration.Value;
                  aDone = true;
               }
               else
               {
                  aFrameLen2 = this.FrameLen;
                  aDone = false;
               }
               this.OnAnimate(aFrameLen2);
               this.FrameLen = 0;
               this.Paint();

               if(aDone)
               {
                  this.Finish();
               }
            }

         }
      }

      private void Paint()
      {         
         this.PaintIsPending = true;
         this.NotifyPaint();
      }

      internal void Paint(CVector2dPainter aOut)
      {
         //this.State.GraphMorph.DebugPrint("CGraphAnimator.Paint.Begin.");
         aOut.SetLineWidth(2.0d);
         foreach (var aShape in this.State.GraphTransition.MorphGraph)
         {
            aShape.Paint(aOut);
         }
         //this.State.GraphMorph.DebugPrint("CGraphAnimator.Paint.End.");
      }

      internal sealed class CWorkingAnimation : CAnimation
      {
         internal CWorkingAnimation(CState aState) : base(aState)
         {
         }

         internal override void OnStart()
         {
            base.OnStart();
            this.State.GraphTransition.AnimState = CAnimStateEnum.Working;
         }

         internal override void OnAnimate(long aFrameLen)
         {
            base.OnAnimate(aFrameLen);
            var aMin = 0.7;
            var aWobble = Math.Sin(Math.PI * 2 * ((double)aFrameLen) / 1000.0d * 500.0d) * (1.0f - aMin) + aMin;                
            foreach (var aShape in this.State.OldGraph.ShapesDic.Values.OfType<COvNode>())
            {
               aShape.Scale = aWobble;
            }            
         }
      }

      internal sealed class CDisappearAnimation : CAnimation
      {
         internal CDisappearAnimation(CState aState) : base(aState)
         {
         }

         internal override void OnStart()
         {
            base.OnStart();
            this.State.GraphTransition.AnimState = CAnimStateEnum.Disappear;
         }
         internal override long? MaxDuration => 333;
         internal override void OnAnimate(long aFrameLen)
         {
            var aPercent = this.Percent;
            var aOpacity = aPercent;
            //this.State.GraphAnimator.DebugPrint("Disappear.Opacity=" + aOpacity);
            foreach (var aShape in this.State.GraphTransition.Disappearings)
            {
               aShape.AnimateDisappear(aPercent);
            }            
         }
         internal override void OnFinish()
         {
            base.OnFinish();
            if(this.State.MoveAnimationNullable is object)
               this.State.MoveAnimationNullable.Start();
         }
      }

      internal sealed class CMoveAnimation : CAnimation
      {
         internal CMoveAnimation(CState aState) : base(aState)
         {
         }

         internal override void OnStart()
         {
            base.OnStart();
            this.State.GraphTransition.AnimState = CAnimStateEnum.Move;
         }

         internal override long? MaxDuration => 500;
         internal override void OnAnimate(long aElapsedMilliseconds)
         {
            var aProgress = this.Percent;
            foreach (var aMatrixMorph in this.State.GraphTransition.Morphings.Values)
            {
               aMatrixMorph.MorphPercent = aProgress;
               aMatrixMorph.Morph();
            }            
         }
         internal override void OnFinish()
         {
            base.OnFinish();
            if (this.State.AppearAnimationNullable is object)
               this.State.AppearAnimationNullable.Start();
         }
      }

      internal sealed class CAppearAnimation : CAnimation
      {
         internal CAppearAnimation(CState aState) : base(aState)
         {
         }

         internal override long? MaxDuration => 333;

         internal override void OnStart()
         {
            base.OnStart();
            this.State.GraphTransition.AnimState = CAnimStateEnum.Appear;
         }

         internal override void OnAnimate(long aFrameLen)
         { 
            var aPercent = this.Percent;
            this.State.GraphAnimator.DebugPrint("CAppearAnimation.OnAnimate.Percent=" + aPercent);            
            foreach (var aShape in this.State.GraphTransition.Appearings)
            {
               aShape.AnimateAppear(aPercent);
            }
         }
      }
   }
}
