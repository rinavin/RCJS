using System;
using System.Collections;
using System.Collections.Specialized;
using com.magicsoftware.unipaas;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.local.application
{
   public delegate void TaskInfoHandler(TaskDefinitionId taskDefinitionId, string xmlId, string defaultTagList, int executionRightIdx);

   /// <summary>
   /// SAX handler for parsing XML serialized TaskDefinitionId (TDID) table. The parser will parse all
   /// TDID entries, invoking an assigned delegate for each one it parses.
   /// </summary>
   public class TaskInfoTableSaxHandler : MgSAXHandlerInterface
   {
      /// <summary>
      /// Handler to be invoked after parsing each TaskInfo entry.
      /// </summary>
      private readonly TaskInfoHandler _newTaskDefinitionIdHandler;

      TaskDefinitionId taskDefinitionId;

      TaskDefinitionIdTableSaxHandler childHandler;

      /// <summary>
      /// Creates and starts a SAX parser with this handler as the SAX handler, to parse
      /// a task definition ID table.
      /// </summary>
      /// <param name="newTaskDefintionIdHandler">A handler that will be called for each parsed entry.</param>
      /// <param name="xmlSerializedTaskDefinitionsTable">The XML serialized data.</param>
      public TaskInfoTableSaxHandler(TaskInfoHandler newTaskDefintionIdHandler)
      {
         _newTaskDefinitionIdHandler = newTaskDefintionIdHandler;
         childHandler = new TaskDefinitionIdTableSaxHandler(SetTaskDefinitionId);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="xmlSerializedTaskDefinitionsTable"></param>
      public void parse(byte[] xmlSerializedTaskDefinitionsTable)
      {
         MgSAXHandler mgSAXHandler = new MgSAXHandler(this);
         mgSAXHandler.parse(xmlSerializedTaskDefinitionsTable);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="elementName"></param>
      /// <param name="elementValue"></param>
      /// <param name="attributes"></param>
      public void endElement(String elementName, String elementValue, NameValueCollection attributes)
      {
         if (elementName == ConstInterface.MG_TAG_TASK_INFO)
         {
            string escapedTaskUrl = "";
            string escapedDefaultTagList = "";
            string defaultTagList = "";
            String taskUrl = "";
            int executionRightIdx = 0;

            IEnumerator enumerator = attributes.GetEnumerator();
            while (enumerator.MoveNext())
            {
               String attr = (String)enumerator.Current;
               switch (attr)
               {
                  case ConstInterface.MG_TAG_DEFAULT_TAG_LIST:
                     escapedDefaultTagList = attributes[attr];
                     defaultTagList = XmlParser.unescape(escapedDefaultTagList);
                     break;

                  case ConstInterface.MG_TAG_TASKURL:
                     escapedTaskUrl = attributes[attr];
                     taskUrl = XmlParser.unescape(escapedTaskUrl);
                     break;

                  case ConstInterface.MG_TAG_EXECUTION_RIGHT:
                     executionRightIdx = XmlParser.getInt(attributes[attr]);
                     break;

                  default:
                     Events.WriteDevToLogEvent(
                        "There is no such tag in TaskInfoTable class. Insert case to TaskInfoTable.endElement() for: " + attr);
                     break;
               }
            }

            // Invoke the handler with the parsed information.
            _newTaskDefinitionIdHandler(taskDefinitionId, taskUrl, defaultTagList, executionRightIdx);
         }
         else
            childHandler.endElement(elementName, elementValue, attributes);
      }

      /// <summary>
      /// callback to internal handler, to get the TaskDefinitionID
      /// </summary>
      /// <param name="taskDefinitionId"></param>
      void SetTaskDefinitionId(TaskDefinitionId taskDefinitionId)
      {
         this.taskDefinitionId = taskDefinitionId;
      }

   }
}
