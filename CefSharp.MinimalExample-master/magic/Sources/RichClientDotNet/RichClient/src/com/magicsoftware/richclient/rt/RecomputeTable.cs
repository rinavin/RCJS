using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.data;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using Task = com.magicsoftware.richclient.tasks.Task;
using com.magicsoftware.richclient.tasks;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   ///   Data for <recompute>....</recompute>
   ///   this class is used only to create the Recompute objects but do not
   ///   hold any data
   /// </summary>
   internal class RecomputeTable
   {
      /// <summary>
      ///   get the recompute attributes
      /// </summary>
      private Task fillAttributes(XmlParser parser)
      {
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex());
         int Index = parser.getXMLdata().IndexOf(XMLConstants.MG_TAG_RECOMPUTE, parser.getCurrIndex()) +
                  XMLConstants.MG_TAG_RECOMPUTE.Length;
         Task task = null;

         List<string> tokensVector = XmlParser.getTokens(parser.getXMLdata().Substring(Index, endContext - Index), "\"");

         for (int j = 0; j < tokensVector.Count; j += 2)
         {
            string attribute = (tokensVector[j]);
            string valueStr = (tokensVector[j + 1]);

            if (attribute.Equals(XMLConstants.MG_ATTR_TASKID))
               task = (Task)MGDataCollection.Instance.GetTaskByID(valueStr);
            else
               Logger.Instance.WriteExceptionToLog(string.Format("Unrecognized attribute: '{0}'", attribute));
         }
         parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) + 1);
         //start of <fld ...>

         return task;
      }

      /// <summary>
      /// 
      /// </summary>
      internal void fillData()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;

         Task task = fillAttributes(parser);

         if (task != null)
         {
            Logger.Instance.WriteDevToLog("goes to refill recompute");
            fillData((DataView)task.DataView, task);
         }
         else
            throw new ApplicationException("in RecomputeTable.fillData() invalid task id: "); //+ valueStr);
      }

      /// <summary>
      ///   To parse input string and fill inner data : Vector props
      /// </summary>
      internal void fillData(DataView dataView, Task task)
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         while (initInnerObjects(parser, parser.getNextTag(), dataView, task))
         {
         }
      }

      /// <summary>
      ///   Fill Recompute Object, gives its reference to Field and reference of Field to him
      /// </summary>
      private bool initInnerObjects(XmlParser parser, String nameOfFound, DataView dataView, Task task)
      {
         if (nameOfFound == null)
            return false;

         if (nameOfFound.Equals(XMLConstants.MG_TAG_RECOMPUTE))
            parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) +
                                   1);
         //satrt of <fld ...>
         else if (nameOfFound.Equals(XMLConstants.MG_TAG_FLD))
         {
            Recompute recompute = new Recompute();
            recompute.fillData(dataView, task); //get reference to DataView, to make linked ref. : Recompute<-Field
            //taskReference for Recompute.props=> ControlTable.Control.task
         }
         else if (nameOfFound.Equals('/' + XMLConstants.MG_TAG_RECOMPUTE))
         {
            parser.setCurrIndex2EndOfTag();
            return false;
         }
         else
         {
            Logger.Instance.WriteExceptionToLog(
               "There is no such tag in <recompute>, add case to RecomputeTable.initInnerObjects for " + nameOfFound);
            return false;
         }
         return true;
      }
   }
}
