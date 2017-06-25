using System;
using System.Diagnostics;
using System.Threading;
using RunTimeEvent = com.magicsoftware.richclient.events.RunTimeEvent;
using com.magicsoftware.util;
#if PocketPC
using Monitor = com.magicsoftware.richclient.mobile.util.Monitor;
using ThreadInterruptedException = com.magicsoftware.richclient.mobile.util.ThreadInterruptedException;
#endif

namespace com.magicsoftware.richclient.util
{
   /// <summary> Implements Priority and blocking queue. It simulates JDK 1.5 Priority Queue + Blocking queue. The elements
   /// (RT Events)are inserted depending on its Priority. If one or more events have same priority then time of
   /// the event is used. Further more this class supplies blocking retrieval operations. While this queue is
   /// logically unbounded, attempted additions may fail due to resource exhaustion (causing
   /// <tt>OutOfMemoryError</tt>). This class does not permit <tt>null</tt> elements. (doing so results in
   /// <tt>ClassCastException</tt>).
   /// </summary>
   internal class MgPriorityBlockingQueue : MgBlockingQueue
   {
      private readonly MgPriorityQueue _queue;
      private long _timeFirstEvent; // time of first event in sequence

      /// <summary> Creates a <tt>PriorityBlockingQueue</tt> with the default initial capacity (11) that orders its
      /// elements according to the RT events Priority
      /// </summary>
      internal MgPriorityBlockingQueue()
      {
         _queue = new MgPriorityQueue();
      }

      /// <summary> return true if event has lowest priority it means that this is supporting event that should be executed
      /// in thread's spear time, execution of such event should not cause shells disabling
      /// </summary>
      /// <param name="o"> </param>
      /// <returns> <tt>true</tt> if it is background event, <tt>false</tt> otherwise </returns>
      private bool isBackgroundEvent(Object o)
      {
         bool ret = false;
         if (o is RunTimeEvent)
         {
            var rtEvt = (RunTimeEvent)o;
            if (rtEvt.getPriority() == Priority.LOWEST)
               ret = true;
         }
         return ret;
      }

      /// <summary> Inserts the specified element into the priority queue.The inserting of element is done in
      /// synchronization When the element is inserted successfully then it notifies the waiting thread
      /// </summary>
      /// <param name="o">the element to add </param>
      /// <returns> true </returns>
      public bool offer(Object o)
      {
         if (o == null)
            throw new NullReferenceException();
         Monitor.Enter(this);
         try
         {
            bool ok = _queue.offer(o);
            if (_timeFirstEvent == 0)
            // adding first event in sequence
            {
               // background event doesn't count
               if (!isBackgroundEvent(o))
                  _timeFirstEvent = Misc.getSystemMilliseconds();
            }
            Debug.Assert(ok);
            Monitor.Pulse(this);
            return true;
         }
         finally
         {
            Monitor.Exit(this);
         }
      }

      /// <summary> Adds the specified element to this priority queue. 
      /// As the queue is unbounded this method will never block.
      /// </summary>
      /// <param name="o">the element to add </param>
      public void put(Object o)
      {
         offer(o); // never need to block
      }

      /// <summary> This method (implemented from MgBlockingQueue) offers the blocking mechanism. Any thread entering in
      /// this method will wait if the queue is empty and it will be notified when an element is inserted in the
      /// queue
      /// </summary>
      public void waitForElement()
      {
         Monitor.Enter(this);
         try
         {
            // we finished handling of previous events, and there is no other events in queue
            // the sequence is finished
            if (_queue.Size == 0)
            {
               _timeFirstEvent = 0;
            }

            while (_queue.Size == 0)
               Monitor.Wait(this);
         }
         catch (ThreadInterruptedException)
         {
            Monitor.Pulse(this); // propagate to non-interrupted thread
            throw new ApplicationException("In waitForElement");
         }
         finally
         {
            Monitor.Exit(this);
         }
      }

      /// <summary>
      ///  Return time of first event in sequence
      /// </summary>
      /// <returns> </returns>
      internal long GetTime()
      {
         Monitor.Enter(this);
         try
         {
            return _timeFirstEvent;
         }
         finally
         {
            Monitor.Exit(this);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal Object poll()
      {
         Monitor.Enter(this);
         try
         {
            Object e = _queue.poll();
            // background event priority is lowest , executing this event means that there are no
            // real events in the queue
            if (isBackgroundEvent(e))
               _timeFirstEvent = 0;

            return e;
         }
         finally
         {
            Monitor.Exit(this);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal Object peek()
      {
         Monitor.Enter(this);
         try
         {
            return _queue.peek();
         }
         finally
         {
            Monitor.Exit(this);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal int size()
      {
         Monitor.Enter(this);
         try
         {
            return _queue.Size;
         }
         finally
         {
            Monitor.Exit(this);
         }
      }

      /// <summary> Always returns <tt>Integer.MAX_VALUE</tt> because a <tt>PriorityBlockingQueue</tt> is not capacity
      /// constrained.
      /// </summary>
      /// <returns> <tt>Integer.MAX_VALUE</tt>
      /// </returns>
      internal int remainingCapacity()
      {
         Monitor.Enter(this);
         try
         {
            return Int32.MaxValue;
         }
         finally
         {
            Monitor.Exit(this);
         }
      }

      /// <summary> Atomically removes all of the elements from this queue. The queue will be empty after this call returns.</summary>
      internal void clear()
      {
         Monitor.Enter(this);
         try
         {
            _queue.clear();
         }
         finally
         {
            Monitor.Exit(this);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal bool isEmpty()
      {
         Monitor.Enter(this);
         try
         {
            return _queue.isEmpty();
         }
         finally
         {
            Monitor.Exit(this);
         }
      }
   }
}