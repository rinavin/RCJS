using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.events;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using EventHandler = com.magicsoftware.richclient.events.EventHandler;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   ///   the handlers table
   /// </summary>
   internal class HandlersTable
   {
      private readonly List<EventHandler> _handlers;

      /// <summary>
      ///   CTOR
      /// </summary>
      internal HandlersTable()
      {
         _handlers = new List<EventHandler>();
      }

      /// <summary>
      ///   parse the handlers of a task
      /// </summary>
      internal void fillData(Task taskRef)
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         while (initInnerObjects(parser, parser.getNextTag(), taskRef))
         {
         }
      }

      /// <summary>
      ///   To allocate and fill inner objects of the class
      /// </summary>
      /// <param name = "foundTagName">name of tag, of oject, which need be allocated </param>
      private bool initInnerObjects(XmlParser parser, String foundTagName, Task taskRef)
      {
         if (foundTagName == null)
            return false;

         if (foundTagName.Equals(ConstInterface.MG_TAG_EVENTHANDLERS))
            parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
         else if (foundTagName.Equals(ConstInterface.MG_TAG_HANDLER))
         {
            EventHandler currHandler = new EventHandler();
            currHandler.fillData(taskRef);
            _handlers.Add(currHandler);
         }
         else if (foundTagName.Equals('/' + ConstInterface.MG_TAG_EVENTHANDLERS))
         {
            parser.setCurrIndex2EndOfTag();
            return false;
         }
         else
         {
            Logger.Instance.WriteExceptionToLog(
               "There is no such tag in HandlersTable. Insert else if to HandlersTable.initInnerObjects for " +
               foundTagName);
            return false;
         }
         return true;
      }

      /// <summary>
      ///   add a new event handler to the table
      /// </summary>
      /// <param name = "handler">the new event handler </param>
      internal void add(EventHandler handler)
      {
         _handlers.Add(handler);
      }

      /// <summary>
      /// calculate control by control name
      /// </summary>
      internal void CalculateControlFormControlName()
      {
         foreach (EventHandler item in _handlers)
         {
            item.calculateCtrlFromControlName(item.getTask());
            
         }
      }
      /// <summary>
      ///   removes the handler at idx place
      /// </summary>
      /// <param name = "idx"> </param>
      internal void remove(int idx)
      {
         _handlers.RemoveAt(idx);
      }

      /// <summary>
      ///   insert a new event handler to the table after the idx's element
      /// </summary>
      /// <param name = "handler">the new event handler </param>
      /// <param name = "idx">index of the element to add the new element after </param>
      internal void insertAfter(EventHandler handler, int idx)
      {
         _handlers.Insert(idx, handler);
      }

      /// <summary>
      ///   returns the number of handlers in the table
      /// </summary>
      internal int getSize()
      {
         return _handlers.Count;
      }

      /// <summary>
      ///   get a handler by its index
      /// </summary>
      /// <param name = "idx">the index of the handler in the table </param>
      internal EventHandler getHandler(int idx)
      {
         if (idx < 0 || idx >= _handlers.Count)
            return null;
         return _handlers[idx];
      }

      /// <summary>
      ///   start timers for timer events
      /// </summary>
      internal void startTimers(MGData mgd)
      {
         List<Int32> timers;
         GUIManager guiManager = GUIManager.Instance;

         if (_handlers != null)
         {
            timers = getTimersVector();
            for (int i = 0;
                 i < timers.Count;
                 i++)
               guiManager.startTimer(mgd, timers[i], false);
         }
      }

      /// <summary>
      ///   Returns a list of the number of seconds for each timer
      ///   If there is more than one timer handler with the same number of seconds
      ///   this number will appear in the list only once.
      /// </summary>
      internal List<Int32> getTimersVector()
      {
         int i, j;
         bool timerExists;
         Event timerEvt;
         List<Int32> timers = new List<Int32>();
         int sec = 0;

         // scan the timers
         for (i = 0;
              i < _handlers.Count;
              i++)
         {
            timerEvt = getHandler(i).getEvent();

            if (timerEvt.getType() == ConstInterface.EVENT_TYPE_TIMER)
               sec = timerEvt.getSeconds();
               // EVENT_TYPE_USER 
            else
               sec = timerEvt.getSecondsOfUserEvent();
            timerExists = false;
            for (j = 0;
                 j < timers.Count;
                 j++)
            {
               // skip existing timers
               if (sec == (timers[j]))
               {
                  timerExists = true;
                  break;
               }
            }

            if (!timerExists)
               timers.Add(sec);
         }
         return timers;
      }
   }
}
