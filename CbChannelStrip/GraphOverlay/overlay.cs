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

namespace CbChannelStrip.GraphOverlay
{
   using CWorkerResult = Tuple<BackgroundWorker, CGraphOverlay.CState>;

   internal abstract class COvShape
   {
      internal CGraphOverlay GraphOverlay;
      internal COvShape(CGraphOverlay aGraphOverlay)
      {
         this.GraphOverlay = aGraphOverlay;
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
   }

   internal sealed class COvNode : COvShape
   {
      internal COvNode(CGraphOverlay aGraphOverlay, CGwNode aGwNode):base(aGraphOverlay)
      {
         this.GwNode = aGwNode;
         this.Pos = new CPoint(aGwNode.X, aGwNode.Y);
      }
      
      internal COvNode CopyNode() => new COvNode(this.GraphOverlay, this.GwNode);

      internal readonly CGwNode GwNode;
      internal override string Name => this.GwNode.Name;
       
      public CPoint Pos { get; set; }

      private volatile object ScaleM = (double)1.0d;
      internal double Scale { get => (double)this.ScaleM; set => this.ScaleM = value; }

      internal override void AnimateAppear(double aPercent)
      {
         base.AnimateAppear(aPercent);

         this.GraphOverlay.DebugPrint("AnimateAppear.Percent=" + aPercent);
         this.Scale = aPercent;
      }

      internal override void AnimateDisappear(double aPercent)
      {
         base.AnimateDisappear(aPercent);
         this.Scale = 1.0d - aPercent;
      }

      internal override void Paint(CVector2dPainter aOut)
      {
         //this.GraphOverlay.DebugPrint("COvNode.Paint.Begin.");
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
         var aBaseColor = Color.Black;
         var aColor = Color.FromArgb((int)(aAlpha * 255.0d), aBaseColor);

         //this.GraphOverlay.DebugPrint("Alpha=" + aColor.A);
         //this.GraphOverlay.DebugPrint("Scale=" + aScale);

         aOut.SetSourceRgba(aColor);

         

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
            aOut.Text(aText, aRect);
         }
         //this.GraphOverlay.DebugPrint("COvNode.Paint.End.");
      }
   }

   internal sealed class COvGraph : IEnumerable<COvShape>
   {
      internal COvGraph(CGraphOverlay aGraphOverlay, CPoint aSize, IEnumerable<COvShape> aShapes)
      {
         //aGraphOverlay.DebugPrint("COvGraph.COvGraph(): Shapes.Count=" + aShapes.Count());

         this.Size = aSize;
         this.GraphOverlay = aGraphOverlay;
         foreach (var aShape in aShapes)
         {
            this.ShapesDic.Add(aShape.Name, aShape);
         }
      }

      internal COvGraph(CGraphOverlay aGraphOverlay, CGwGraph aGwGraph)
      {
         //aGraphOverlay.DebugPrint("COvGraph.COvGraph(): GwGraph.Nodes.Count=" + aGwGraph.Nodes.Count());
         this.Size = aGwGraph.Size;
         this.GraphOverlay = aGraphOverlay;

         foreach (var aGwShape in aGwGraph.Nodes)
         {
            var aOvShape = new COvNode(this.GraphOverlay, aGwShape);
            this.ShapesDic.Add(aOvShape.Name, aOvShape);
         }
      }

      internal readonly CPoint Size;

      private CGraphOverlay GraphOverlay;
      internal COvGraph(CGraphOverlay aGraphOverlay) :this(aGraphOverlay, new CGwGraph())
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

   internal sealed class COvNodeMorph
   {
      internal COvNodeMorph(COvNode aOldNode, COvNode aNewNode)
      {
         this.OldNode = aOldNode;
         this.NewNode = aNewNode;
         this.MorphNode = aOldNode.CopyNode();
      }

      internal readonly COvNode OldNode;
      internal readonly COvNode NewNode;
      internal readonly COvNode MorphNode;

      internal double MorphPercent { get; set; }

      internal void Morph()
      {
         var aOldPos = this.OldNode.Pos;
         var aNewPos = this.NewNode.Pos;
         var aDeltaPos = aNewPos - aOldPos;
         var aPercent = new CPoint(this.MorphPercent, this.MorphPercent);
         var aMorphPos = aOldPos + aDeltaPos * aPercent;
         this.MorphNode.Pos = aMorphPos;
      }
   }


   internal sealed class CGraphTransition
   {
      internal CGraphTransition(CGraphOverlay aGraphOverlay, CPoint aSize, COvGraph aOldGraph, COvGraph aNewGraph)
      {
         this.GraphOverlay = aGraphOverlay;
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
         var aMorphings = new Dictionary<string, COvNodeMorph>();
         foreach (var aKey in aMorphingKeys)
         {
            if(!aMorphings.ContainsKey(aKey))
            {
               var aOldShape = aOldGraph.ShapesDic[aKey];
               var aNewShape = aNewGraph.ShapesDic[aKey];
               if(aOldShape is COvNode
               && aNewShape is COvNode)
               {
                  aMorphings[aKey] = new COvNodeMorph((COvNode)aOldShape, (COvNode)aNewShape);
               }
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
         var aMorphShapes1 = (from aMorph in aMorphings.Values select aMorph.MorphNode).AsEnumerable<COvShape>().ToArray();
         var aMorphShapes2 = (aDisappearings.Concat(aAppearings).Concat(aMorphShapes1)).ToArray();

         foreach(var aAppearing in aAppearings)
         {
            aAppearing.AnimateAppear(0.0d);
         }
         foreach(var aDisappearing in aDisappearings)
         {
            aDisappearing.AnimateDisappear(0.0d);
         }
         this.OldGraph = aOldGraph;
         this.NewGraph = aNewGraph;
         this.MorphGraph = new COvGraph(this.GraphOverlay, aSize, aMorphShapes2);
         this.Morphings = aMorphings;
         this.Disappearings = aDisappearings; 
         this.Appearings = aAppearings;
      }

      internal readonly CGraphOverlay GraphOverlay;
      internal readonly COvGraph OldGraph; 
      internal readonly COvGraph NewGraph;
      internal readonly COvGraph MorphGraph;
      internal readonly Dictionary<string, COvNodeMorph> Morphings;
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

   public sealed class CGraphOverlay
   {
      internal CGraphOverlay(Action<Exception> aOnExc,
                             Func<CGwGraph> aCalcNewGraph,
                             Action aNotifyResult,
                             Action aNotifyPaint,
                             Action<string> aDebugPrint)
      {
         this.ExternDebugPrint = aDebugPrint;
         this.OnExc = aOnExc;      
         this.State = new CState(this, new COvGraph(this)); 
         this.AnimationThread = new System.Threading.Thread(RunAnimationThread);
         this.CalcNewGraph = aCalcNewGraph;
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
         var aCalcNewGraph = new Func<CGwGraph>(() => CGwGraph.NewTestGraph(aDebugPrint));
         var aGraphOverlay = default(CGraphOverlay);
         var aNotifyResult = new Action(delegate () { aDispatcher.BeginInvoke(new Action(delegate () { aGraphOverlay.ProcessNewGraph(); })); });
         var aNotifyPaint = new Action(delegate () { aDispatcher.BeginInvoke(new Action(delegate () { aGraphOverlay.OnPaintDone(); })); });
         aGraphOverlay = new CGraphOverlay(aOnExc, aCalcNewGraph, aNotifyResult, aNotifyPaint, aDebugPrint);
         aGraphOverlay.NextGraph();
         System.Console.WriteLine("press any key to shutdown CGraphOverlay test");
         System.Console.ReadKey();
         aGraphOverlay.Shutdown();
         aDispatcherFrame.Continue = false;
      }

      private Action<string> ExternDebugPrint;
      internal void DebugPrint(string aMsg) => this.ExternDebugPrint(aMsg); // System.Diagnostics.Debug.Print(aMsg);
      private Action NotifyResult;
      private Action NotifyPaint;
      private Action<Exception> OnExc;
      private readonly Func<CGwGraph> CalcNewGraph;    
      private BackgroundWorker WorkerNullable;
      private volatile bool PaintIsPending;      
      internal void OnPaintDone()
      {
         this.PaintIsPending = false;
      }

      internal sealed class CState
      {
         internal CState(CGraphOverlay aGraphOverlay, COvGraph aGraph)
         {
            this.GraphOverlay = aGraphOverlay;
            this.OldGraph = aGraph;
            this.NewGraph = aGraph;
            this.GraphTransition = new CGraphTransition(aGraphOverlay, this.Size, aGraph, aGraph);
            this.WorkingAnimation = new CWorkingAnimation(this);
            this.AppearAnimationNullable = default;
            this.MoveAnimationNullable = default;
            this.AppearAnimationNullable = default;
         }

         internal CState(CGraphOverlay aGraphOverlay, CState aOldState, COvGraph aNewGraph)
         {
            this.GraphOverlay = aGraphOverlay;
            this.OldGraph = aOldState.NewGraph;
            this.NewGraph = aNewGraph;
            this.GraphTransition = new CGraphTransition(aGraphOverlay, this.Size, this.OldGraph, this.NewGraph);
            this.WorkingAnimation = new CWorkingAnimation(this);
            this.FadeOutAnimationNullable = new CDisappearAnimation(this);
            this.MoveAnimationNullable = new CMoveAnimation(this);
            this.AppearAnimationNullable = new CAppearAnimation(this);            
         }

         internal readonly CGraphOverlay GraphOverlay;
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
         //this.DebugPrint("CGraphOverlay.CancelWorkerOnDemand.Begin");
         lock (this)
         {
            var aWorker = this.WorkerNullable;
            if(aWorker is object)
            {
               this.WorkerNullable = default;
               this.RemoveWorkerCallbacks(aWorker);
            }
         }
         //this.DebugPrint("CGraphOverlay.CancelWorkerOnDemand.End");
      }
      private bool IsCurrentWorker(BackgroundWorker aWorker)
      {
         return object.ReferenceEquals(this.WorkerNullable, aWorker);
      }

      private void StartWorker()
      {
         //this.DebugPrint("CGraphOverlay.StartWorker");
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
         //this.DebugPrint("CGraphOverlay.BackgroundWorkerDoWork.Begin");
         var aOldState = (CState)aArgs.Argument;
         var aNewGwGraph = this.CalcNewGraph();
         var aNewOvGraph = new COvGraph(this, aNewGwGraph);
         var aNewState = new CState(this, aOldState, aNewOvGraph);
         aArgs.Result = aNewState;
         //this.DebugPrint("CGraphOverlay.BackgroundWorkerDoWork.End");
      }


      private void BackgroundWorkerRunWorkerCompleted(object aSender, RunWorkerCompletedEventArgs aArgs)
      {
         //this.DebugPrint("CGraphOverlay.BackgroundWorkerRunWorkerCompleted");
         CState aNewState;
         var aWorker = (BackgroundWorker)aSender;
         if(aArgs.Error is object)
         {
            //this.OnExc(new Exception("Error calculating GraphMorph.NewGraph. " + aArgs.Error.Message, aArgs.Error));
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
         //this.DebugPrint("CGraphOverlay.AddWorkerResult.Begin");
         lock (this.WorkerResults)
         {
            this.WorkerResults.Add(aWorkerResult);
            this.NotifyResult();
         }
         //this.DebugPrint("CGraphOverlay.AddWorkerResult.End");
      }


      private readonly List<CWorkerResult> WorkerResults = new List<Tuple<BackgroundWorker, CState>>();
      private CWorkerResult PeekWorkerResultNullable()
      {
         //this.DebugPrint("CGraphOverlay.PeekWorkerResultNullable.Begin");
         lock (this.WorkerResults)
         {
            if(!this.WorkerResults.IsEmpty())
            {
               var aWorkerResult = this.WorkerResults.Last();
               this.WorkerResults.Clear();
               return aWorkerResult;
            }
         }
         //this.DebugPrint("CGraphOverlay.PeekWorkerResultNullable.End");
         return default;
      }
      internal void ProcessNewGraph()
      {
         //this.DebugPrint("CGraphOverlay.ProcessNewGraph");
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
            //this.State.GraphOverlay.DebugPrint(this.GetType().Name + "started.");
         }

         internal void Stop()
         {
            this.IsRunning = false;
            //this.State.GraphOverlay.DebugPrint(this.GetType().Name + "stopped.");
         }
          
         internal void Finish()
         {
          //  this.Animate(0);
            this.Stop();
            this.OnFinish();            
            this.State.GraphOverlay.DebugPrint(this.GetType().Name + "finished.");
         }

         internal virtual void OnFinish()
         {
         }


         internal  bool RepaintIsPending { get => this.State.GraphOverlay.PaintIsPending; }
         private Stopwatch Stopwatch = new Stopwatch();

         internal void Paint()
         {
            this.State.GraphOverlay.Paint();
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
         //this.State.GraphMorph.DebugPrint("CGraphOverlay.Paint.Begin.");
         foreach (var aShape in this.State.GraphTransition.MorphGraph)
         {
            aShape.Paint(aOut);
         }
         //this.State.GraphMorph.DebugPrint("CGraphOverlay.Paint.End.");
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
            //this.State.GraphOverlay.DebugPrint("Disappear.Opacity=" + aOpacity);
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
            this.State.GraphOverlay.DebugPrint("CAppearAnimation.OnAnimate.Percent=" + aPercent);            
            foreach (var aShape in this.State.GraphTransition.Appearings)
            {
               aShape.AnimateAppear(aPercent);
            }
         }
      }
   }
}
