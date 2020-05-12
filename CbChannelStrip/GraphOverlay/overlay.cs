using CbChannelStrip.GraphWiz;
using CbMaxClrAdapter;
using CbMaxClrAdapter.Jitter;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media;

namespace CbChannelStrip.GraphOverlay
{

   internal sealed class COvShape
   {
      internal COvShape(CGwNode aGwShape)
      {
         this.GwShape = aGwShape;
         this.OriginalShapeMatrix = aGwShape.NewShapeMatrix();
         this.ShapeMatrix = aGwShape.NewShapeMatrix();
         this.TransformMatrix = aGwShape.NewTransformMatrix();
      }

      internal readonly CGwNode GwShape;
      internal string Name { get => this.GwShape.Name; }
      internal readonly CMatrixData OriginalShapeMatrix;
      internal readonly CMatrixData TransformMatrix;
      internal readonly CMatrixData ShapeMatrix;
      private int[] mTmpDim = new int[1];

      internal void SetShapeMatrix(double aScale)
      {
         var aDims = this.mTmpDim;
         var aDimSize = this.OriginalShapeMatrix.DimensionSizes[0];
         for(var aDim = 0; aDim < aDimSize; ++aDim)
         {
            aDims[0] = aDim;
            for (var aPlane =  0; aPlane < 2; ++aPlane) 
            {
               var aSrcVal = this.OriginalShapeMatrix.GetCellFloat(aDims, aPlane);
               var aDstVal = aSrcVal * aScale;
               this.ShapeMatrix.SetCellFloat(aDims, aPlane, aDstVal);
            }            
         }
      }
   }

   internal sealed class COvGraph
   {
      internal COvGraph(CGwGraph aGwGraph)
      {
         this.GwGraph = aGwGraph;

         foreach(var aGwShape in aGwGraph.Nodes)
         {
            var aOvShape = new COvShape(aGwShape);
            this.ShapesDic.Add(aOvShape.Name, aOvShape);
         }

      }
      internal readonly CGwGraph GwGraph;

      internal Dictionary<string, COvShape> ShapesDic = new Dictionary<string, COvShape>();
   }

   internal abstract class CMatrixMorph
   {
      internal CMatrixMorph(CMatrixData aOldMatrix, CMatrixData aNewMatrix, CMatrixData aMorphed)
      {
         this.OldMatrix = aOldMatrix;
         this.NewMatrix = aNewMatrix;
         this.Morphed = aMorphed;
      }

      internal readonly CMatrixData OldMatrix;
      internal readonly CMatrixData NewMatrix;
      internal readonly CMatrixData Morphed;

      protected abstract double MorphDouble(int[] aDimensions, int aPlane, double aPercent);

      internal void Morph(double aPercent)
      {
         if(this.OldMatrix.DimensionCount == this.NewMatrix.DimensionCount
         && this.OldMatrix.DimensionSizes.SequenceEqual(this.NewMatrix.DimensionSizes))
         {
            var aDimensions = new int[this.OldMatrix.DimensionCount];
            throw new NotImplementedException();
         }
         else
         {
            throw new Exception("MatrixSize missmatch.");
         }
      }

   }

   internal sealed class CLinearMatrixMorph : CMatrixMorph
   {
      internal CLinearMatrixMorph(CMatrixData aOldMatrix, CMatrixData aNewMatrix, CMatrixData aMarphed) :base(aOldMatrix, aNewMatrix, aMarphed)
      {
      }
      protected override double MorphDouble(int[] aDimensions, int aPlane, double aPercent)
      {
         var aOldValue = this.OldMatrix.GetCellFloat(aDimensions, aPlane);
         var aNewValue = this.NewMatrix.GetCellFloat(aDimensions, aPlane);
         if (aOldValue > aNewValue)
         {
            return aNewValue + (aOldValue - aNewValue) * aPercent;
         }
         else if (aOldValue < aNewValue)
         {
            return aOldValue - (aNewValue - aOldValue) * aPercent;

         }
         else
         {
            return aOldValue;
         }
      }
   }





   internal sealed class CGraphTransition
   {
      internal CGraphTransition(COvGraph aOldGraph, COvGraph aNewGraph)
      {
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
         var aMorphings = new Dictionary<string, CMatrixMorph>();
         foreach (var aKey in aMorphingKeys)
         {
            if(!aMorphings.ContainsKey(aKey))
            {
               var aOldShape = aOldGraph.ShapesDic[aKey];
               var aNewShape = aNewGraph.ShapesDic[aKey];
               aMorphings[aKey] = new CLinearMatrixMorph(aOldShape.OriginalShapeMatrix, aNewShape.OriginalShapeMatrix, aOldShape.ShapeMatrix);
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
         this.OldGraph = aOldGraph;
         this.NewGraph = aNewGraph;
         this.Moprhings = aMorphings;
         this.Disappearings = aDisappearings;
         this.Appearings = aAppearings;
      }

      internal readonly COvGraph OldGraph; 
      internal readonly COvGraph NewGraph;
      internal readonly Dictionary<string, CMatrixMorph> Moprhings = new Dictionary<string, CMatrixMorph>();
      internal readonly List<COvShape> Disappearings = new List<COvShape>();
      internal readonly List<COvShape> Appearings = new List<COvShape>();
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

   internal sealed class CWorkerPool
   {
      internal CWorkerPool(Action<Exception> aOnExc)
      {
         this.OnExc = aOnExc;
      }

      private readonly Action<Exception> OnExc;

      private List<BackgroundWorker> Free = new List<BackgroundWorker>();
      private List<BackgroundWorker> CancelationPending = new List<BackgroundWorker>();

      internal BackgroundWorker Allocate()
      {
         BackgroundWorker aWorker;
         lock (this)
         {
            if (this.Free.IsEmpty())
            {
               aWorker = default;
            }
            else
            {
               aWorker = this.Free.First();
            }
         }
         if (aWorker is object)
            return aWorker;
         var aNewWorker = new BackgroundWorker();
         aNewWorker.WorkerSupportsCancellation = true;
         return aNewWorker;
      }

      internal void Deallocate(BackgroundWorker aWorker)
      {
         lock(this)
         {
            this.Free.Add(aWorker);
         }
      }

      internal void Cancel(BackgroundWorker aWorker)
      {
         aWorker.CancelAsync();
         aWorker.RunWorkerCompleted += this.OnWorkerCompleted;
      }

      private void OnWorkerCompleted(object aSender, RunWorkerCompletedEventArgs aArgs)
      {
         try
         {
            var aWorker = (BackgroundWorker)aSender;
            aWorker.RunWorkerCompleted -= this.OnWorkerCompleted;
            this.Deallocate(aWorker);
         }
         catch(Exception aExc)
         {
            this.OnExc(aExc);  
         }
      }
   }

   internal sealed class CGraphMorph
   {
      internal CGraphMorph(Action<Exception> aOnExc, COvGraph aOvGraph, Func<CGwGraph> aCalcNewGraph)
      {
         this.OnExc = aOnExc;      
         this.WorkerPool = new CWorkerPool(aOnExc);
         this.State = new CState(this, aOvGraph);
         this.AnimationThread = new System.Threading.Thread(RunAnimationThread);
         this.CalcNewGraph = aCalcNewGraph;
      }

      private Action<Exception> OnExc;
      private Action Paint;
      private readonly Func<CGwGraph> CalcNewGraph;    
      private CWorkerPool WorkerPool;
      private BackgroundWorker Worker;


      internal bool Repaint { get; private set; }
      internal void OnRepaintDone()
      {
         this.Repaint = false;
      }

      private sealed class CState
      {
         internal CState(CGraphMorph aMorphGraph, COvGraph aGraph)
         {
            this.GraphMorph = aMorphGraph;
            this.OldGraph = aGraph;
            this.NewGraph = aGraph;
            this.GraphTransition = new CGraphTransition(aGraph, aGraph);
            this.WorkingAnimation = new CWorkingAnimation(this);
            this.AppearAnimationNullable = default;
            this.MoveAnimationNullable = default;
            this.AppearAnimationNullable = default;
         }

         internal CState(CGraphMorph aGraphMorph, CState aOldState, COvGraph aNewGraph)
         {
            this.GraphMorph = aGraphMorph;
            this.OldGraph = aOldState.NewGraph;
            this.NewGraph = aNewGraph;
            this.GraphTransition = new CGraphTransition(this.OldGraph, this.NewGraph);
            this.WorkingAnimation = new CWorkingAnimation(this);
            this.FadeOutAnimationNullable = new CDisappearAnimation(this);
            this.MoveAnimationNullable = new CMoveAnimation(this);
            this.AppearAnimationNullable = new CAppearAnimation(this);            
         }

         internal readonly CGraphMorph GraphMorph;
         internal readonly COvGraph OldGraph;
         internal readonly COvGraph NewGraph;
         internal readonly CGraphTransition GraphTransition;
         internal readonly CWorkingAnimation WorkingAnimation;
         internal readonly CDisappearAnimation FadeOutAnimationNullable;
         internal readonly CMoveAnimation MoveAnimationNullable;
         internal readonly CAppearAnimation AppearAnimationNullable;
         //internal CAnimState AnimState;

         internal IEnumerable<CAnimation> Animations
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

      }

      private CState State;

      private void CancelWorkerOnDemand()
      {
         lock(this)
         {
            if(this.Worker is object)
            {
               lock (this.Worker)
               {
                  if (this.Worker.IsBusy)
                  {
                     this.Worker.RunWorkerCompleted -= this.BackgroundWorkerRunWorkerCompleted;
                     this.WorkerPool.Cancel(this.Worker);
                  }
               }
            }
         }
      }
      private void StartWorker()
      {
         this.CancelWorkerOnDemand();
         this.Worker = this.WorkerPool.Allocate();
         this.Worker.RunWorkerCompleted += this.BackgroundWorkerRunWorkerCompleted;
         this.State.WorkingAnimation.Start();

      }

      private void BackgroundWorkerDoWork(object aSender, DoWorkEventArgs aArgs)
      {
         var aNewGwGraph = this.CalcNewGraph();
         var aNewOvGraph = new COvGraph(aNewGwGraph);
         var aNewState = new CState(this, aNewOvGraph);
         aArgs.Result = aNewState;
      }

      private void BackgroundWorkerRunWorkerCompleted(object aSender, RunWorkerCompletedEventArgs aArgs)
      {
         CState aWorkerResult;
         lock (this)
         {
            if(aArgs.Error is object)
            {
               this.OnExc(new Exception("Error calculating GraphMorph.NewGraph. " + aArgs.Error.Message, aArgs.Error));
               aWorkerResult = default;
            }
            else if(aArgs.Cancelled)
            {
               // nix.
               aWorkerResult = default;
            }
            else if(aArgs.Result is object)
            {
               aWorkerResult = (CState)aArgs.Result;               
            }
            else
            {
               this.OnExc(new Exception("No result when calculating GraphMorph.NewGraph."));
               aWorkerResult = default;
            }
         }
         if(aWorkerResult is object)
         {
            this.ReceiveWorkerResult(aWorkerResult);
         }
      }

      private void ReceiveWorkerResult(CState aNewState)
      {
         this.State.WorkingAnimation.Stop();
         this.State.WorkingAnimation.Finish();
         this.State = aNewState;
         aNewState.FadeOutAnimationNullable.Start();
         
      }

      private readonly System.Threading.Thread AnimationThread;

      internal void Stop()
      {
         this.CancelWorkerOnDemand();
         this.StopAnimationThread = true;
         this.AnimationThread.Join();
      }

      private bool StopAnimationThread;
      private void RunAnimationThread(object aObj)
      {
         try
         {
            var aStopWatch = new Stopwatch();
            aStopWatch.Start();
            while (!this.StopAnimationThread)
            {
               System.Threading.Thread.Sleep(33);
               var aElapsed = aStopWatch.ElapsedMilliseconds;
               aStopWatch.Stop();
               aStopWatch.Start();
               this.Animate(aStopWatch.ElapsedMilliseconds);
            }
         }
         catch(Exception)
         {
            // TODO
         }
      }

      private bool Animate(long aElapsedMilliseconds)
      {
         var aBusy = false;
         var aState = this.State;
         foreach(var aAnimation in aState.Animations)
         {
            aAnimation.Animate(aElapsedMilliseconds);
            aBusy = true;
         }
         return aBusy;
      }

      private abstract class CAnimation
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
            this.Stop();
            this.OnFinish();
         }

         internal virtual void OnFinish()
         {
         }


         internal bool RepaintIsPending { get => this.State.GraphMorph.Repaint; }
         private Stopwatch Stopwatch = new Stopwatch();

         internal void Repaint()
         {
            this.State.GraphMorph.Repaint = true;
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
               if (aMaxDuration.HasValue)
                  return Math.Min(aMaxDuration.Value, aElapsed);
               return aElapsed;
            }
         }
         internal double TotalElapsedPercent { get => ((double)this.TotalElapsed) / ((double)this.MaxDuration.Value); }

         internal void Animate(long aElapsed)
         {
            if (this.RepaintIsPending)
            {
               this.FrameLen += aElapsed;
            }
            else
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
               this.Repaint();

               if(aDone)
               {
                  this.Finish();
               }
            }

         }
      }

      private sealed class CWorkingAnimation : CAnimation
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
            foreach (var aShape in this.State.OldGraph.ShapesDic.Values)
            {
               aShape.SetShapeMatrix(aWobble);
            }            
         }
      }

      private sealed class CDisappearAnimation : CAnimation
      {
         internal CDisappearAnimation(CState aState) : base(aState)
         {
         }

         internal override void OnStart()
         {
            base.OnStart();
            this.State.GraphTransition.AnimState = CAnimStateEnum.Disappear;
         }
         internal override long? MaxDuration => 1000;
         internal override void OnAnimate(long aFrameLen)
         {
            var aShapeMatrixSize = 1.0d - this.TotalElapsedPercent;
            foreach (var aShape in this.State.GraphTransition.Disappearings)
            {
               aShape.SetShapeMatrix(aShapeMatrixSize);
            }            
         }
         internal override void OnFinish()
         {
            base.OnFinish();
            if(this.State.MoveAnimationNullable is object)
               this.State.MoveAnimationNullable.Start();
         }
      }

      private sealed class CMoveAnimation : CAnimation
      {
         internal CMoveAnimation(CState aState) : base(aState)
         {
         }

         internal override void OnStart()
         {
            base.OnStart();
            this.State.GraphTransition.AnimState = CAnimStateEnum.Move;
         }

         internal override long? MaxDuration => MaxDurationDefault;
         internal override void OnAnimate(long aElapsedMilliseconds)
         {
            var aProgress = this.TotalElapsedPercent;
            foreach (var aMatrixMorph in this.State.GraphTransition.Moprhings.Values)
            {
               aMatrixMorph.Morph(aProgress);
            }            
         }
         internal override void OnFinish()
         {
            base.OnFinish();
            if (this.State.AppearAnimationNullable is object)
               this.State.AppearAnimationNullable.Start();
         }
      }

      private sealed class CAppearAnimation : CAnimation
      {
         internal CAppearAnimation(CState aState) : base(aState)
         {
         }

         internal override long? MaxDuration => MaxDurationDefault;

         private void SetProgress(double aProgress)
         {
            foreach (var aShape in this.State.GraphTransition.Appearings)
            {
               aShape.SetShapeMatrix(aProgress);
            }
         }

         internal override void OnStart()
         {
            base.OnStart();
            this.SetProgress(0);
            this.State.GraphTransition.AnimState = CAnimStateEnum.Appear;
         }

         internal override void OnAnimate(long aFrameLen)
         {
            this.SetProgress(this.TotalElapsedPercent);
            this.Repaint();
         }
      }
   }
}
