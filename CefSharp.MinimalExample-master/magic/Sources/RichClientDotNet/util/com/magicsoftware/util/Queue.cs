using System;
using System.Collections.Generic;

namespace com.magicsoftware.util
{
   /// <summary> implementation of a general queue</summary>
   public class Queue<T>
   {
      private readonly List<T> _queueVec; // Can't use List<T> - may use ActivityItem or GuiCommand

      /// <summary> Create a new queue with the specified maximum size</summary>
      /// <param name="size">the queue maximum size - 0 or negative means no limit</param>
      public Queue()
      {
         _queueVec = new List<T>();
      }

      /// <summary> returns the first object in the queue or waits till there is an object</summary>
      /// <param name="immidiate">if true returns immidiatly without waiting
      /// </param>
      public Object get()
      {
         lock (this)
         {
            Object returnValue = null;

            if (_queueVec.Count > 0)
            {
               returnValue = _queueVec[0];
               _queueVec.RemoveAt(0);
            }

            return returnValue;
         }
      }

      /// <summary> add an object to the end of the queue</summary>
      /// <param name="obj">the object to add
      /// </param>
      public void put(T obj)
      {
         lock (this)
         {
            _queueVec.Add(obj);
         }
      }

      /// <summary> remove all the objects from the queue</summary>
      public void clear()
      {
         lock (this)
         {
            _queueVec.Clear();
         }
      }

      /// <summary> returns true if the queue is empty</summary>
      public bool isEmpty()
      {
         return _queueVec.Count == 0;
      }

      /// <summary> returns size of the queue</summary>
      public int Size()
      {
         return _queueVec.Count;
      }
   }
}