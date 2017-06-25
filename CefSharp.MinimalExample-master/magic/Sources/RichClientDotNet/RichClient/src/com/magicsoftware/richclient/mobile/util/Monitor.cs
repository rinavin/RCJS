using System;
using System.Collections.Generic;

namespace com.magicsoftware.richclient.mobile.util
{
   internal static class Monitor
   {
      private static readonly Dictionary<Object, MonitorEx> _monitorTable = new Dictionary<Object, MonitorEx>();
      private static readonly object _lockObject = new object(); // for a local monitor, to supervise monitor list access

      /// <summary>
      /// 
      /// </summary>
      /// <param name="value"></param>
      /// <returns></returns>
      private static MonitorEx add(Object value)
      {
         if (value == null)
            throw new ArgumentNullException();

         lock (_lockObject)
         {
            MonitorEx localMonitor;

            // verify monitor was not just added by another thread
            if (!_monitorTable.TryGetValue(value, out localMonitor))
            {
               // Add the new monitor - create the new arrays, copy old data, assign to members and assign data
               localMonitor = new MonitorEx();
               _monitorTable.Add(value, localMonitor);
            }

            return localMonitor;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="value"></param>
      /// <returns></returns>
      private static MonitorEx get(Object value)
      {
         lock (_lockObject)
         {
            MonitorEx localMonitor;
            if (_monitorTable.TryGetValue(value, out localMonitor))
               return localMonitor;
            return null;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      internal static void Pulse(Object obj)
      {
         get(obj).Pulse();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      internal static bool Wait(Object obj)
      {
         get(obj).Wait();
         return true;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="ts"></param>
      /// <returns></returns>
      internal static bool Wait(Object obj, TimeSpan ts)
      {
         return get(obj).Wait((int)ts.TotalMilliseconds, false);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="millisecondsTimeout"></param>
      /// <returns></returns>
      internal static bool Wait(Object obj, int millisecondsTimeout)
      {
         return get(obj).Wait(millisecondsTimeout, false);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      internal static void Enter(Object obj)
      {
         MonitorEx monitor = get(obj);
         if(monitor == null)
            monitor = add(obj);
 
         monitor.Enter();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      internal static void Exit(Object obj)
      {
         get(obj).Exit();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      internal static void PulseAll(Object obj)
      {
         get(obj).PulseAll();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      internal static bool TryEnter(Object obj)
      {
         return get(obj).TryEnter();
      }
   }
}