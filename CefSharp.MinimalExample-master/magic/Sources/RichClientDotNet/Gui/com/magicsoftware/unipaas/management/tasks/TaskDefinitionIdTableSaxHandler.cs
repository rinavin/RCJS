using System;
using System.Collections;
using System.Collections.Specialized;
using com.magicsoftware.unipaas;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using util.com.magicsoftware.util;

namespace com.magicsoftware.unipaas.management.tasks
{
   public delegate void TaskDefinitionIdHandler(TaskDefinitionId taskDefinitionId);

   /// <summary>
   /// SAX handler for parsing XML serialized TaskDefinitionId (TDID) table. The parser will parse all
   /// TDID entries, invoking an assigned delegate for each one it parses.
   /// </summary>
   public class TaskDefinitionIdTableSaxHandler : MgSAXHandlerInterface
   {
      /// <summary>
      /// Handler to be invoked after parsing each TDID entry.
      /// </summary>
      private readonly TaskDefinitionIdHandler _newTaskDefinitionIdHandler;

      /// <summary>
      /// Creates a SAX parser with this handler as the SAX handler, to parse
      /// a task definition ID table.
      /// </summary>
      /// <param name="newTaskDefintionIdHandler">A handler that will be called for each parsed entry.</param>
      /// <param name="xmlSerializedTaskDefinitionsTable">The XML serialized data.</param>
      public TaskDefinitionIdTableSaxHandler(TaskDefinitionIdHandler newTaskDefintionIdHandler)
      {
         _newTaskDefinitionIdHandler = newTaskDefintionIdHandler;
      }

      /// <summary>
      /// parse the supplied buffer using the previously supplied handler
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
         if (elementName == XMLConstants.MG_TAG_TASKDEFINITIONID_ENTRY)
         {
            int ctlIndex = 0;
            bool isPrg = false;
            int PrgIsn = 0;
            int TaskIsn = 0;

            IEnumerator enumerator = attributes.GetEnumerator();
            while (enumerator.MoveNext())
            {
               String attr = (String)enumerator.Current;
               switch (attr)
               {
                  case XMLConstants.MG_ATTR_CTL_IDX:
                     ctlIndex = Int32.Parse(attributes[attr]);
                     break;
                  
                  case XMLConstants.MG_ATTR_PROGRAM_ISN:
                     PrgIsn = Int32.Parse(attributes[attr]);
                     break;
                  
                  case XMLConstants.MG_ATTR_TASK_ISN:
                     TaskIsn = Int32.Parse(attributes[attr]);
                     break;

                  case XMLConstants.MG_ATTR_ISPRG:
                     isPrg = Int32.Parse(attributes[attr]) == 1;
                     break;

                  default:
                     Events.WriteDevToLogEvent(
                        "There is no such tag in TaskDefinitionIdTable class. Insert case to TaskDefinitionIdTable.endElement() for: " + attr);
                     break;
               }
            }

            TaskDefinitionId taskDefinitionId = new TaskDefinitionId(ctlIndex, PrgIsn, TaskIsn, isPrg);

            // Invoke the handler with the parsed information.
            _newTaskDefinitionIdHandler(taskDefinitionId);
         }
      }
   }
}
