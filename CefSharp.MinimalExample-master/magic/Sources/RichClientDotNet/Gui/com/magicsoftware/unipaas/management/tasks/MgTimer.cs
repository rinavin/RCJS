using com.magicsoftware.util;
using TTimer = System.Threading.Timer;

namespace com.magicsoftware.unipaas.management.tasks
{
   /// <summary>This class acts as base class for the timers implemented in RC and Merlin.</summary>
   public abstract class MgTimer
   {
      //member variables.
      protected int _timerIntervalMilliSeconds;
      private TTimer _threadTimer;

      ///<summary>Constructor</summary>
      ///<param name="timerIntervalMilliSeconds">Timer Interval in milliseconds</param>
      public MgTimer(int timerIntervalMilliSeconds)
      {
         _timerIntervalMilliSeconds = timerIntervalMilliSeconds;
      }

      /// <summary>Call back method of threading timer.</summary>
      /// <param name="state"></param>
      static internal void Run(object state)
      {
         Misc.MarkTimerThread();
         Events.OnTimer((MgTimer)state);
      }

      /// <summary>Starts the timer thread with the interval passed in milliseconds.</summary>
      internal void Start()
      {
         _threadTimer = new TTimer(Run, this, _timerIntervalMilliSeconds, _timerIntervalMilliSeconds);
      }

      /// <summary>Stops the thread.</summary>
      internal void Stop()
      {
         _threadTimer.Dispose();
      }
   }
}
