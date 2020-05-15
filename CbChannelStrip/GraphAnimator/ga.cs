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

namespace CbChannelStrip.GaAnimator
{
   using CWorkerResult = Tuple<BackgroundWorker, CGaAnimator.CState>;

   internal abstract class CGaShape
   {
      internal static readonly Color DefaultColor = Color.Black;

      internal readonly CGaGraph GaGraph;
      internal readonly CGaAnimator GaAnimator;
      internal CGaShape(CGaGraph aGaGraph)
      {
         this.GaGraph = aGaGraph;
         this.GaAnimator = aGaGraph.GaAnimator;
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
      internal abstract CGaMorph NewMorph(CGaTransition aGaTransition, CGaNode aNewNode);
      internal abstract CGaMorph NewMorph(CGaTransition aGaTransition, CGaEdge aNewEdge);
      internal abstract CGaMorph AcceptNewMorph(CGaTransition aGaTransition, CGaShape aOldShape);
      internal virtual void Init() { }

      internal virtual void Animate(CGaAnimator.CAnnounceAnimation aAnnounceAnimation)
      {
      }
   }

   internal sealed class CGaEdge : CGaShape
   {
      internal CGaEdge(CGaGraph aGaGraph, CGwEdge aGwEdge, CGaNode aGaNode1, CGaNode aGaNode2) :base(aGaGraph)
      {
         this.GwEdge = aGwEdge;
         this.Splines = (from aSpline in aGwEdge.Splines select new CPoint(aSpline.Item1, aSpline.Item2)).ToArray();
         this.GaNode1 = aGaNode1;
         this.GaNode2 = aGaNode2;
         this.Color = aGwEdge.Color;
      }
      internal readonly CGwEdge GwEdge;
      internal override string Name { get => this.GwEdge.Name; }
      internal CGaEdge CopyEdge() => new CGaEdge(this.GaGraph, this.GwEdge, this.GaNode1, this.GaNode2);

      internal readonly CGaNode GaNode1;
      internal readonly CGaNode GaNode2;
      private object ColorM = default(Color?);
      internal Color? Color { get => (Color?)this.ColorM; set => this.ColorM = value; }
      internal override void Paint(CVector2dPainter aOut)
      {
         var aBezier = this.Splines;
         var aSplines = aBezier.Skip(1);
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
      internal override CGaMorph NewMorph(CGaTransition aGaTransition, CGaEdge aNewEdge) => new CGaEdgeMorph(aGaTransition, this, aNewEdge);
      internal override CGaMorph NewMorph(CGaTransition aGaTransition, CGaNode aNewNode) => throw new InvalidOperationException();
      internal override CGaMorph AcceptNewMorph(CGaTransition aGaTransition, CGaShape aOldShape) => aOldShape.NewMorph(aGaTransition, this);

      internal volatile CPoint[] Splines;
   }

   internal sealed class CGaNode : CGaShape
   {
      internal CGaNode(CGaGraph aGraph, CGwNode aGwNode):base(aGraph)
      {
         this.GwNode = aGwNode;
         this.Pos = new CPoint(aGwNode.X, aGwNode.Y);
         this.Color = aGwNode.Color;
         this.FontColor = aGwNode.FontColor;
      }
      
      internal CGaNode CopyNode() => new CGaNode(this.GaGraph, this.GwNode);

      internal readonly CGwNode GwNode;
      internal override string Name => this.GwNode.Name;
       
      public CPoint Pos { get; set; }

      private volatile object DisappearScaleM = (double)1.0d;
      internal double DisappearScale { get => (double)this.DisappearScaleM; set => this.DisappearScaleM = value; }

      private volatile object AppearScaleM = (double)1.0d;
      internal double AppearScale { get => (double)this.AppearScaleM; set => this.AppearScaleM = value; }

      private volatile object WorkingScaleM = (double)1.0d;
      internal double WorkingScale { get => (double)this.WorkingScaleM; set => this.WorkingScaleM = value; }

      private volatile object AnnounceScaleM = (double)1.0d;
      internal double AnnounceScale { get => (double)this.AnnounceScaleM; set => this.AnnounceScaleM = value; }
      internal double Scale { get { return this.WorkingScale * this.AnnounceScale * this.AppearScale * this.DisappearScale; } }

      private object ColorM = default(Color?);
      internal Color? Color { get => (Color?)this.ColorM; set => this.ColorM = value; }
      private object FontColorM = default(Color?);
      internal Color? FontColor { get => (Color?)this.FontColorM; set => this.FontColorM = value; }

      internal override void AnimateAppear(double aPercent)
      {
         base.AnimateAppear(aPercent);
         this.AppearScale = aPercent;
      }

      internal override void AnimateDisappear(double aPercent)
      {
         base.AnimateDisappear(aPercent);
         this.DisappearScale = 1.0d - aPercent;
      }

      internal override void Paint(CVector2dPainter aOut)
      {
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
      }

      internal override CGaMorph NewMorph(CGaTransition aGaTransition, CGaEdge aNewEdge) => throw new InvalidOperationException();
      internal override CGaMorph NewMorph(CGaTransition aGaTransition, CGaNode aNewNode) => new CGaNodeMorph(aGaTransition, this, aNewNode);
      internal override CGaMorph AcceptNewMorph(CGaTransition aGaTransition, CGaShape aOldShape) => aOldShape.NewMorph(aGaTransition, this);

      internal override void Animate(CGaAnimator.CAnnounceAnimation aAnnounceAnimation)
      {
         base.Animate(aAnnounceAnimation);
         this.AnnounceScale = aAnnounceAnimation.Wobble;
      }
   }

   internal sealed class CGaGraph : IEnumerable<CGaShape> 
   {
      internal CGaGraph(CGaAnimator aGaAnimator, CPoint aSize, IEnumerable<CGaShape> aShapes)
      {
         this.Size = aSize;
         this.GaAnimator = aGaAnimator;
         foreach (var aShape in aShapes)
         {
            this.ShapesDic.Add(aShape.Name, aShape);
         }
      }

      internal CGaGraph(CGaAnimator aGaAnimator, CGwGraph aGwGraph)
      {
         this.Size = new CPoint(aGwGraph.Size);
         this.GaAnimator = aGaAnimator;

         foreach (var aGwNode in aGwGraph.Nodes)
         {
            var aGaNode = new CGaNode(this, aGwNode);
            this.ShapesDic.Add(aGaNode.Name, aGaNode);
         }
         foreach (var aGwEdge in aGwGraph.Edges)
         {
            var aGaEdge = new CGaEdge(this, 
                                      aGwEdge, 
                                      (CGaNode)this.ShapesDic[aGwEdge.Node1Name], 
                                      (CGaNode)this.ShapesDic[aGwEdge.Node2Name]);
            this.ShapesDic.Add(aGaEdge.Name, aGaEdge);
         }
         foreach (var aShape in this.ShapesDic.Values)
            aShape.Init();
      }

      internal readonly CPoint Size;

      internal readonly CGaAnimator GaAnimator;
      internal CGaGraph(CGaAnimator aGaAnimator) :this(aGaAnimator, new CGwGraph())
      {
      }

      internal Dictionary<string, CGaShape> ShapesDic = new Dictionary<string, CGaShape>();
      public IEnumerator<CGaShape> GetEnumerator() => this.ShapesDic.Values.GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
   }

   internal abstract class CGaMorph
   {
      internal CGaMorph(CGaTransition aGaTransition)
      {
         this.GaTransition = aGaTransition;
         var aOldSize = this.GaTransition.GaAnimatorState.OldGraph.Size;
         var aNewSize = this.GaTransition.GaAnimatorState.NewGraph.Size;
         var aMaxSize = new CPoint(Math.Max(aOldSize.X, aNewSize.X),
                                   Math.Max(aOldSize.Y, aNewSize.Y));
         var aDiag = Math.Sqrt(Math.Pow(aMaxSize.X, 2) + Math.Pow(aMaxSize.Y, 2));
         this.MaxDistance = aDiag;
      }
      internal readonly CGaTransition GaTransition;
      internal readonly double MaxDistance;

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
            throw new ArgumentException("CGaMorph.MorpPoints: Points.Length missmatch.");
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
      internal TimeSpan GetMoveDuration(CPoint aOldPoint, CPoint aNewPoint)
      {
         var aDelta = CPointUtil.GetDelta(aOldPoint, aNewPoint);
         var aDistance = Math.Sqrt(Math.Pow(aDelta.X, 2) + Math.Pow(aDelta.Y, 2));
         var aMaxDistance = this.MaxDistance;
         var aMaxDuration = this.MaxDuration.TotalMilliseconds;
         // aDistance         aDuration 
         // -------------- =  --------------
         // aMaxDistance      aMaxDuration
         // aDistance * aMaxDuration = aMaxDistance * aDuration
         var aDuration = (aDistance * aMaxDuration) / aMaxDistance;
         return new TimeSpan(0, 0, 0, 0, (int) aDuration);
      }

      internal TimeSpan GetMoveDuration(CPoint[] aOldPoints, CPoint[] aNewPoints)
      {         
         if(aOldPoints.Length != aNewPoints.Length)
         {
            throw new ArgumentException("Points.Length missmatch.");
         }
         else if(aOldPoints.IsEmpty())
         {
            throw new ArgumentException("Points.IsEmpty.");
         }
         else
         {
            return (from aIdx in Enumerable.Range(0, aOldPoints.Length) select this.GetMoveDuration(aOldPoints[aIdx], aNewPoints[aIdx])).Max();
         }
      }

      private readonly TimeSpan MaxDuration = new TimeSpan(0, 0, 0,0, 1500);
      internal abstract bool CalcAnnounce();
      internal abstract void Morph();
      internal abstract CGaShape MorphedShape { get; }
      internal abstract TimeSpan Duration { get; }
      internal abstract CGaShape OldShape { get; }

      internal virtual void Animate(CGaAnimator.CAnnounceAnimation aAnnounceAnimation)
      {
         this.MorphedShape.Animate(aAnnounceAnimation);         
      }
   }

   internal static class CPointUtil
   {
      internal static CPoint GetDelta(CPoint a1, CPoint a2) => 
                           new CPoint(Math.Max(a1.X, a2.X) - Math.Min(a1.X, a2.X),
                                      Math.Max(a1.Y, a2.Y) - Math.Min(a1.Y, a2.Y));

   }


   internal sealed class CGaNodeMorph : CGaMorph
   {
      internal CGaNodeMorph(CGaTransition aGaTransition, CGaNode aOldNode, CGaNode aNewNode):base(aGaTransition)
      {
         this.OldNode = aOldNode;
         this.NewNode = aNewNode;
         this.MorphedNode = aOldNode.CopyNode();
      }
      internal readonly CGaNode OldNode;
      internal readonly CGaNode NewNode;
      internal readonly CGaNode MorphedNode;
      internal override CGaShape OldShape => this.OldNode;
      internal override CGaShape MorphedShape => this.MorphedNode;
      internal override void Morph()
      {
         this.MorphedNode.Pos = this.MorphPoint(this.OldNode.Pos, this.NewNode.Pos);
         this.MorphedNode.Color = this.MorphColor(this.OldNode.Color, this.NewNode.Color);
         this.MorphedNode.FontColor = this.MorphColor(this.OldNode.FontColor, this.NewNode.FontColor);
      }
      internal override TimeSpan Duration => this.GetMoveDuration(this.OldNode.Pos, this.NewNode.Pos);
      internal override bool CalcAnnounce() => this.OldNode.Pos != this.NewNode.Pos;
      internal override void Animate(CGaAnimator.CAnnounceAnimation aAnnounceAnimation)
      {
         base.Animate(aAnnounceAnimation);
         this.MorphedNode.Animate(aAnnounceAnimation);         
      }
   }
   internal sealed class CGaEdgeMorph : CGaMorph
   {
      internal CGaEdgeMorph(CGaTransition aGaTransition, CGaEdge aOldEdge, CGaEdge aNewEdge) : base(aGaTransition)
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
      internal readonly CGaEdge OldEdge;
      internal readonly CPoint[] OldPoints;
      internal readonly CGaEdge NewEdge;
      internal readonly CPoint[] NewPoints;
      internal readonly CGaEdge MorphedEdge;
      internal override CGaShape OldShape => this.OldEdge;
      internal override CGaShape MorphedShape => this.MorphedEdge;
      internal override void Morph()
      {
         this.MorphedEdge.Splines = this.MorphPoints(this.OldPoints, this.NewPoints);
         this.MorphedEdge.Color = this.MorphColor(this.OldEdge.Color, this.NewEdge.Color);
      }
      internal override TimeSpan Duration => this.GetMoveDuration(this.OldPoints, this.NewPoints);
      internal override bool CalcAnnounce() => !this.OldPoints.SequenceEqual(this.NewPoints);
   }

   internal sealed class CGaTransition
   {
      internal CGaTransition(CGaAnimator.CState aGaAnimatorState, CPoint aSize, CGaGraph aOldGraph, CGaGraph aNewGraph)
      {
         this.GaAnimatorState = aGaAnimatorState;
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
         var aMorphings = new Dictionary<string, CGaMorph>();
         foreach (var aKey in aMorphingKeys)
         {
            if(!aMorphings.ContainsKey(aKey))
            {
               var aOldShape = aOldGraph.ShapesDic[aKey];
               var aNewShape = aNewGraph.ShapesDic[aKey];
               var aMorph = aNewShape.AcceptNewMorph(this, aOldShape);
               aMorphings[aKey] = aMorph;
            }
         }
         var aMorphDuration = aMorphings.IsEmpty() ? default(TimeSpan) : (from aMorph in aMorphings.Values select aMorph.Duration).Max();



         var aDisappearings = new List<CGaShape>();
         foreach (var aKey in aDisappearingKeys)
         {
            aDisappearings.Add(aOldGraph.ShapesDic[aKey]);
         }
         var aAppearings = new List<CGaShape>();
         foreach (var aKey in aAppearingKeys)
         {
            aAppearings.Add(aNewGraph.ShapesDic[aKey]);
         }

         var aChangedPos = (from aMorph in aMorphings.Values
                            where aMorph.CalcAnnounce()
                            select aMorph).ToArray();

         var aMorphShapes1 = (from aMorph in aMorphings.Values select aMorph.MorphedShape);
         var aMorphShapes2 = (aDisappearings.Concat(aAppearings).Concat(aMorphShapes1));
         var aMorpShapes = (from aGroup in aMorphShapes2.GroupBy(aShape => aShape) select aGroup.Key).ToArray();
         var aMorphGraph = new CGaGraph(this.GaAnimatorState.GaAnimator, aSize, aMorpShapes);
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
         this.MorphDuration = aMorphDuration;
         this.ChangedPos = aChangedPos;
      }

      internal readonly CGaAnimator.CState GaAnimatorState;
      internal readonly CGaGraph OldGraph; 
      internal readonly CGaGraph NewGraph;
      internal readonly CGaGraph MorphGraph;
      internal readonly Dictionary<string, CGaMorph> Morphings;
      internal readonly List<CGaShape> Disappearings;
      internal readonly List<CGaShape> Appearings;
      internal readonly TimeSpan MorphDuration;
      internal readonly CGaMorph[] ChangedPos;
   }

   public sealed class CGaAnimator
   {
      internal CGaAnimator(Action<Exception> aOnExc,
                             Func<CGwGraph> aCalcNewGwGraph,
                             Action aNotifyResult,
                             Action aNotifyPaint,
                             Action<string> aDebugPrint)
      {
         this.ExternDebugPrint = aDebugPrint;
         this.OnExc = aOnExc;      
         this.State = new CState(this, new CGaGraph(this)); 
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
         var aGraphNr = 0;
         var aOnExc = new Action<Exception>(delegate (Exception aExc) { aDebugPrint(aExc.ToString()); });
         var aGaAnimator = default(CGaAnimator);
         var aNotifyResult = new Action(delegate () { aDispatcher.BeginInvoke(new Action(delegate () { aGaAnimator.ProcessNewGraph(); })); });
         var aNotifyPaint = new Action(delegate () { aDispatcher.BeginInvoke(new Action(delegate () { aGaAnimator.OnPaintDone(); })); });
         var aCalcNewGwGraph = new Func<CGwGraph>(() =>
         {
            switch (aGraphNr % 5)
            {
               case 0:
                  return CFlowMatrix.NewTestFlowMatrix1(aDebugPrint).Routings.GwDiagramBuilder.GwGraph;
               case 1:
                  return CFlowMatrix.NewTestFlowMatrix2(aDebugPrint).Routings.GwDiagramBuilder.GwGraph;
               case 2:
                  return CFlowMatrix.NewTestFlowMatrix3(aDebugPrint).Routings.GwDiagramBuilder.GwGraph;
               case 3:
                  return CFlowMatrix.NewTestFlowMatrix4(aDebugPrint).Routings.GwDiagramBuilder.GwGraph;
               default:
                  return CFlowMatrix.NewTestFlowMatrix5(aDebugPrint).Routings.GwDiagramBuilder.GwGraph;
            }
         });
         aGaAnimator = new CGaAnimator(aOnExc, aCalcNewGwGraph, aNotifyResult, aNotifyPaint, aDebugPrint);
         var aDone = false;
         do
         {
            System.Console.WriteLine("CGaAnimatorTest: press nr or anything else to exit.");
            var aKey = System.Console.ReadKey();
            var aKeyText = aKey.KeyChar.ToString();
            int aNr = 0;
            if (int.TryParse(aKeyText, out aNr))
            {
               aGraphNr = aNr;
               aGaAnimator.NextGraph();
            }
            else
            {
               aDone = true;
            }
         }
         while (!aDone);
         aGaAnimator.Shutdown();
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
         internal CState(CGaAnimator aGaAnimator, CGaGraph aGraph)
         {
            this.GaAnimator = aGaAnimator;
            this.OldGraph = aGraph;
            this.NewGraph = aGraph;            
            this.WorkingAnimation = new CWorkingAnimation(this);
            this.AnnounceAnimation = new CAnnounceAnimation(this);
            this.DisappearAnimation = new CDisappearAnimation(this);
            this.MoveAnimation = new CMoveAnimation(this);
            this.AppearAnimation = new CAppearAnimation(this);
            this.GaTransition = new CGaTransition(this, this.Size, aGraph, aGraph);
         }

         internal CState(CGaAnimator aGaAnimator, CState aOldState, CGaGraph aNewGraph)
         {
            this.OldStateNullable = aOldState;
            this.GaAnimator = aGaAnimator;
            this.OldGraph = aOldState.NewGraph;
            this.NewGraph = aNewGraph;            
            this.WorkingAnimation = new CWorkingAnimation(this);
            this.AnnounceAnimation = new CAnnounceAnimation(this);
            this.DisappearAnimation = new CDisappearAnimation(this);
            this.MoveAnimation = new CMoveAnimation(this);
            this.AppearAnimation = new CAppearAnimation(this);
            this.GaTransition = new CGaTransition(this, this.Size, this.OldGraph, this.NewGraph);
         }

         internal readonly CState OldStateNullable;
         internal readonly CGaAnimator GaAnimator;
         internal readonly CGaGraph OldGraph;
         internal readonly CGaGraph NewGraph;
         internal readonly CGaTransition GaTransition;
         internal readonly CWorkingAnimation WorkingAnimation;
         internal readonly CAnnounceAnimation AnnounceAnimation;
         internal readonly CDisappearAnimation DisappearAnimation;
         internal readonly CMoveAnimation MoveAnimation;
         internal readonly CAppearAnimation AppearAnimation;
         
         //internal CAnimState AnimState;

         internal IEnumerable<CAnimation> RunningAnimations
         {
            get
            {
               if (this.OldStateNullable is object
               && this.OldStateNullable.WorkingAnimation.IsRunning)
                  yield return this.OldStateNullable.WorkingAnimation;
               else if (this.WorkingAnimation.IsRunning)
                  yield return this.WorkingAnimation;
               if (this.AnnounceAnimation.IsRunning)
                  yield return this.AnnounceAnimation;
               if (this.DisappearAnimation.IsRunning)
                  yield return this.DisappearAnimation;
               if (this.MoveAnimation.IsRunning)
                  yield return this.MoveAnimation;
               if (this.AppearAnimation.IsRunning)
                  yield return this.AppearAnimation;
            }
         }

         internal CPoint Size { get => new CPoint(Math.Max(this.OldGraph.Size.X, this.NewGraph.Size.X),
                                                  Math.Max(this.OldGraph.Size.Y, this.NewGraph.Size.Y)); }
      }

      internal volatile CState State;

      private void CancelWorkerOnDemand()
      {
         var aWorker = this.WorkerNullable;
         if(aWorker is object)
         {
            this.WorkerNullable = default;
            this.RemoveWorkerCallbacks(aWorker);
         }
      }
      private bool IsCurrentWorker(BackgroundWorker aWorker)
      {
         return object.ReferenceEquals(this.WorkerNullable, aWorker);
      }

      private void StartWorker()
      {
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
         try
         {
            aWorker.DoWork -= this.BackgroundWorkerDoWork;
            aWorker.RunWorkerCompleted -= this.BackgroundWorkerRunWorkerCompleted;
         }
         catch(Exception aExc)
         {
            this.OnExc(aExc);
         }
      }

      internal void NextGraph()
      {
         this.StartWorker();
      }

      private void BackgroundWorkerDoWork(object aSender, DoWorkEventArgs aArgs)
      {
         System.Threading.Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
         //System.Threading.Thread.Sleep(3000);
         var aOldState = (CState)aArgs.Argument;
         var aNewGwGraph = this.CalcNewGwGraph();
         var aNewGaGraph = new CGaGraph(this, aNewGwGraph);
         var aNewState = new CState(this, aOldState, aNewGaGraph);
         aArgs.Result = aNewState;
      }


      private void BackgroundWorkerRunWorkerCompleted(object aSender, RunWorkerCompletedEventArgs aArgs)
      {
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
         lock (this.WorkerResults)
         {
            this.WorkerResults.Add(aWorkerResult);
            this.NotifyResult();
         }
      }


      private readonly List<CWorkerResult> WorkerResults = new List<Tuple<BackgroundWorker, CState>>();
      private CWorkerResult PeekWorkerResultNullable()
      {
         lock (this.WorkerResults)
         {
            if(!this.WorkerResults.IsEmpty())
            {
               var aWorkerResult = this.WorkerResults.Last();
               this.WorkerResults.Clear();
               return aWorkerResult;
            }
         }
         return default;
      }
      internal void ProcessNewGraph()
      {
         var aResult = this.PeekWorkerResultNullable();
         if(aResult is object
         && this.IsCurrentWorker(aResult.Item1))
         {
            this.WorkerNullable = default;
            this.RemoveWorkerCallbacks(aResult.Item1);
            //this.State.WorkingAnimation.Finish();
            this.State = aResult.Item2;
            //this.State.FadeOutAnimationNullable.Start();
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
         }

         internal void Stop()
         {
            this.IsRunning = false;
         }
          
         internal void Finish()
         {
          //  this.Animate(0);
            this.Stop();
            this.OnFinish();            
         }

         internal virtual void OnFinish()
         {
         }


         internal  bool RepaintIsPending { get => this.State.GaAnimator.PaintIsPending; }
         private Stopwatch Stopwatch = new Stopwatch();

         internal void Paint()
         {
            this.State.GaAnimator.Paint();
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
         internal double PercentLin { get => this.MaxDuration.Value == 0 ? 1.0d : ((double)Math.Min(this.MaxDuration.Value, this.TotalElapsed)) / ((double)this.MaxDuration.Value); }
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
         aOut.SetLineWidth(2.0d);
         foreach (var aShape in this.State.GaTransition.MorphGraph)
         {
            aShape.Paint(aOut);
         }
      }

      internal sealed class CWorkingAnimation : CAnimation
      {
         internal CWorkingAnimation(CState aState) : base(aState)
         {
         }

         private readonly long Intervall = 250;

         internal override void OnAnimate(long aFrameLen)
         {
            base.OnAnimate(aFrameLen);
            //this.Wobble(0);
            if (this.TotalElapsed >= this.Intervall
            && !object.ReferenceEquals(this.State, this.State.GaAnimator.State))
            {
               this.Finish();
               this.State.GaAnimator.State.AnnounceAnimation.Start();
            }
         }
      }

      internal sealed class CAnnounceAnimation : CAnimation
      {
         internal CAnnounceAnimation(CState aState) : base(aState)
         {
         }

         private double CalcWobble(long aTime, long aIntervall, double aRange)
         {
            var aCycle = (aTime % aIntervall) / 1000.0d;
            var a1 = (aCycle * Math.PI * 2) + Math.PI / 2.0d;
            var aWobble1 = Math.Sin(a1);
            var aWobble2 = (aWobble1 * aRange) + 1.0d;
            return aWobble2;
         }

         private readonly long Intervall = 250;

         internal double Wobble { get => this.IsRunning ? this.CalcWobble(this.TotalElapsed, this.Intervall, 0.075) : 1.0d; }

         internal override void OnAnimate(long aFrameLen)
         {
            base.OnAnimate(aFrameLen);
            foreach (var aMorph in this.State.GaTransition.ChangedPos)
            {
               aMorph.Animate(this);
            }
            foreach(var aDisappearing in this.State.GaTransition.Disappearings)
            {
               aDisappearing.Animate(this);
            }
            if (this.TotalElapsed >= this.Intervall)
            {
               this.Finish();
               this.State.GaAnimator.State.DisappearAnimation.Start();
            }
         }

         internal override void OnFinish()
         {
            base.OnFinish();
            foreach (var aMorph in this.State.GaTransition.ChangedPos)
            {
               aMorph.Animate(this);
            }
         }
      }


      internal sealed class CDisappearAnimation : CAnimation
      {
         internal CDisappearAnimation(CState aState) : base(aState)
         {
         }

         internal override long? MaxDuration => 333;
         internal override void OnAnimate(long aFrameLen)
         {
            var aShapes = this.State.GaTransition.Disappearings;
            if (aShapes.IsEmpty())
            {
               this.Finish();
            }
            else
            {
               var aPercent = this.Percent;
               foreach (var aShape in aShapes)
               {
                  aShape.AnimateDisappear(aPercent);
               }
            }
         }
         internal override void OnFinish()
         {
            base.OnFinish();
            this.State.MoveAnimation.Start();
         }
      }

      internal sealed class CMoveAnimation : CAnimation
      {
         internal CMoveAnimation(CState aState) : base(aState)
         {
         }


         internal override long? MaxDuration => (long)this.State.GaTransition.MorphDuration.TotalMilliseconds;
         internal override void OnAnimate(long aElapsedMilliseconds)
         {
            var aProgress = this.Percent;
            foreach (var aMatrixMorph in this.State.GaTransition.Morphings.Values)
            {
               aMatrixMorph.MorphPercent = aProgress;
               aMatrixMorph.Morph();
            }            
         }
         internal override void OnFinish()
         {
            base.OnFinish();
            this.State.AppearAnimation.Start();
         }
      }

      internal sealed class CAppearAnimation : CAnimation
      {
         internal CAppearAnimation(CState aState) : base(aState)
         {
         }

         internal override long? MaxDuration => 333;

         internal override void OnAnimate(long aFrameLen)
         {             
            var aShapes = this.State.GaTransition.Appearings;
            if(aShapes.IsEmpty())
            {
               this.Finish();
            }
            else
            {
               var aPercent = this.Percent;
               foreach (var aShape in aShapes)
               {
                  aShape.AnimateAppear(aPercent);
               }
            }
         }
      }
   }
}
