using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CbMaxClrAdapter.Timer
{
   public sealed class CTimerThread
   {
      public CTimerThread(CMaxObject aMaxObject)
      {
         if(aMaxObject.Initialized)
         {
            throw new InvalidOperationException("This has to be done in the contructor of CMaxObject child class.");
         }
         this.MaxObject = aMaxObject;
         this.Thread = new Thread(new ParameterizedThreadStart( this.Run));
         this.Thread.Start();
         this.ThreadStartedEvent.WaitOne();
         aMaxObject.ShutdownActions.Add(delegate () { this.Stop(); });
      }

      internal readonly CMaxObject MaxObject;
      private readonly Thread Thread;
      private readonly AutoResetEvent ThreadStartedEvent = new AutoResetEvent(false);
      internal Dispatcher ThreadDispatcher;
      private DispatcherFrame ThreadDispatcherFrame;
      private void Run(object aParam)
      {
         ThreadDispatcher = Dispatcher.CurrentDispatcher;
         ThreadDispatcherFrame = new DispatcherFrame();
         ThreadStartedEvent.Set();
         Dispatcher.PushFrame(ThreadDispatcherFrame);
      }

      private void Stop()
      {
         ThreadDispatcherFrame.Continue = false;
         Thread.Join();
      }

   }

   public sealed class CTimer
   {
      public CTimer(CTimerThread aTimerThread, TimeSpan aInterval, DispatcherPriority aPriority, EventHandler aTimerEventHandler, bool aRunInMainThread)
      {
         this.TimerThread = aTimerThread;
         this.TimerEventHandler = aTimerEventHandler;
         this.RunInMainThread = aRunInMainThread;
         this.TimerEventHandler = aTimerEventHandler;
         aTimerThread.ThreadDispatcher.Invoke(new Action(delegate()
         {
            this.DispatcherTimer = new DispatcherTimer(aInterval, aPriority, this.OnTimer, aTimerThread.ThreadDispatcher);
            this.DispatcherTimer.Stop();
            this.DispatcherTimer.IsEnabled = false;
         }));
      }
      private readonly CTimerThread TimerThread;

      private DispatcherTimer DispatcherTimer;

      private bool RunInMainThread;

      private readonly EventHandler TimerEventHandler;
      private void InvokeTimerEventHandler(EventArgs aArgs)
      {
         if (this.TimerEventHandler is object)
         {
            this.TimerEventHandler(this, aArgs);
         }
      }
      private void OnTimer(object aSender, EventArgs aArgs)
      {
         if(this.RunInMainThread)
         {
            this.TimerThread.MaxObject.BeginInvokeInMainTask(delegate () { this.InvokeTimerEventHandler(aArgs); });
         }
         else
         {
            this.InvokeTimerEventHandler(aArgs);
         }
      }

      public void Start()
      {
         this.DispatcherTimer.Start();
      }

      public void Stop()
      {
         this.DispatcherTimer.Stop();
      }

      public bool IsEnabled { get => this.DispatcherTimer.IsEnabled; set => this.DispatcherTimer.IsEnabled = value; }
      public TimeSpan Interval { get => this.DispatcherTimer.Interval; set => this.DispatcherTimer.Interval = value; }
   }

}
