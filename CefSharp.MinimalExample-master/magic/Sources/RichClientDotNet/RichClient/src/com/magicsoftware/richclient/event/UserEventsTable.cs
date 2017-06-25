using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.events
{
   //User events table:<userevents> event …</userevents>
   internal class UserEventsTable
   {
      private readonly List<Event> _userEventsTab;

      /// <summary>
      ///   CTOR
      /// </summary>
      internal UserEventsTable()
      {
         _userEventsTab = new List<Event>();
      }

      /// <summary>
      ///   parse input string and fill inner data : Vector userEventsTab
      /// </summary>
      internal void fillData(Task tsk)
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         while (initInnerObjects(parser, parser.getNextTag(), tsk))
         {
         }
      }

      /// <summary>
      ///   allocate and fill inner objects of the class
      /// </summary>
      /// <param name = "foundTagName">possible  tag name, name of object, which need be allocated </param>
      private bool initInnerObjects(XmlParser parser, String foundTagName, Task tsk)
      {
         if (foundTagName == null)
            return false;

         switch (foundTagName)
         {
            case ConstInterface.MG_TAG_EVENT:
               {
                  Event evt = new Event();
                  evt.fillData(parser, tsk);
                  _userEventsTab.Add(evt);
                  break;
               }

            case ConstInterface.MG_TAG_USER_EVENTS:
               parser.setCurrIndex(
                  parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
               //end of outer tag and its ">"
               break;

            case ConstInterface.MG_TAG_USER_EVENTS_END:
               parser.setCurrIndex2EndOfTag();
               return false;

            default:
               Logger.Instance.WriteExceptionToLog("There is no such tag in UserEventsTable.initInnerObjects : " +
                                             foundTagName);
               return false;
         }
         return true;
      }

      /// <summary>
      ///   get user event by its index in the vector
      /// </summary>
      /// <param name = "idx">the index of the requested event </param>
      internal Event getEvent(int idx)
      {
         if (idx < 0 || _userEventsTab == null || idx >= _userEventsTab.Count)
            return null;
         return _userEventsTab[idx];
      }

      /// <summary>
      ///   get idx of the event in the events table
      /// </summary>
      /// <param name = "event">to find its idx </param>
      protected internal int getIdxByEvent(Event evt)
      {
         return _userEventsTab.IndexOf(evt);
      }

      /// <summary>
      ///   get size of the user events table
      /// </summary>
      protected internal int getSize()
      {
         if (_userEventsTab == null)
            return 0;
         return _userEventsTab.Count;
      }
   }
}
