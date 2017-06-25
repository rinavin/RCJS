using com.magicsoftware.unipaas.management.tasks;
using System.Collections.Generic;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui;

namespace com.magicsoftware.richclient.tasks
{
#region Timer Object collection class

   ///<summary>mapping between each MgData to its list of timer objects.</summary>
   internal class TimerObjectCollection
   {
      static internal Dictionary<MGData, List<RCTimer>> MgDataToTimerObjList = new Dictionary<MGData, List<RCTimer>>();
   }
#endregion
   /// <summary>RC specific timer class</summary>
   class RCTimer : MgTimer
   {
      //member variables.
      private MGData _mgData;
      private bool _isIdle;

      ///<summary>Public property: set\get TimerInterval in seconds</summary>
      internal int TimerIntervalMiliSeconds
      {
         get
         {
            return _timerIntervalMilliSeconds;
         }
      }

      ///<summary>Public property returning true if timer is idle else false.</summary>
      internal bool IsIdleTimer
      {
         get
         {
            return _isIdle;
         }
         set
         {
            _isIdle = value;
         }
      }

      /// <summary> constructor </summary>
      /// <param name="MgData"></param>
      /// <param name="milliseconds">time interval, in milliseconds</param>
      /// <param name="isIdle"></param>
      internal RCTimer(MGData mgData, int milliseconds, bool isIdle)
         : base(milliseconds)
      {
         _mgData = mgData;
         IsIdleTimer = isIdle;

         if (!TimerObjectCollection.MgDataToTimerObjList.ContainsKey(_mgData))
            TimerObjectCollection.MgDataToTimerObjList.Add(_mgData, new List<RCTimer>());
         TimerObjectCollection.MgDataToTimerObjList[_mgData].Add(this);
      }

      /// <summary>returns MgData.</summary>
      /// <returns></returns>
      internal MGData GetMgdata()
      {
         return _mgData;
      }
     
      /// <summary>Stops the timer corresponding to MGData passed with the interval specified in seconds.</summary>
      /// <param name="mgData">MgData object</param>
      /// <param name="seconds">Timer interval</param>
      /// <param name="isIdle">Is idle timer or not</param>
      static internal void StopTimer(MGData mgData, int milliseconds, bool isIdle)
      {
         List<RCTimer> timers = null;

         if (TimerObjectCollection.MgDataToTimerObjList.ContainsKey(mgData))
         {
            timers = TimerObjectCollection.MgDataToTimerObjList[mgData];
            foreach (RCTimer rcTimer in timers)
            {
               if (rcTimer != null)
               {
                  if ((rcTimer._timerIntervalMilliSeconds == milliseconds) && (rcTimer._isIdle == isIdle))
                  {
                     //Placing STOP_TIMER command in queue.
                     Commands.addAsync(CommandType.STOP_TIMER, rcTimer);

                     timers.Remove(rcTimer);
                     break;
                  }
               }
            }
            Commands.beginInvoke();

            if (timers.Count == 0)
               TimerObjectCollection.MgDataToTimerObjList.Remove(mgData);
         }
      }

      /// <summary>Stops all the timers.</summary>
      /// <param name="MgData"></param>
      internal static void StopAll(MGData mgData)
      {
         List<RCTimer> timers = null;

         //Checking for the entry of the timer objects list for the respective key.
         if(TimerObjectCollection.MgDataToTimerObjList.ContainsKey(mgData))
            timers = TimerObjectCollection.MgDataToTimerObjList[mgData];

         if (timers != null)
         {
            foreach (RCTimer rcTimer in timers)
            {
               if (rcTimer != null)
               {
                  //Placing STOP_TIMER command in queue.
                  Commands.addAsync(CommandType.STOP_TIMER, rcTimer);
               }
            }

            Commands.beginInvoke();

            //Clearing the list, after all the timers are stopped.
            timers.Clear();

            //Removing the key from the hash table, after the list of timer objects are cleared.
            TimerObjectCollection.MgDataToTimerObjList.Remove(mgData);
         }
      }
   }
}
