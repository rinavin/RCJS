using System;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.data;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.richclient.local.application;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.unipaas;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.local.commands
{
   /// <summary>
   /// local command to open task
   /// </summary>
   class LocalRunTimeCommandOpenTask : LocalRunTimeCommandBase
   {
      internal TaskDefinitionId TaskDefinitionId { get; set; }
      internal ArgumentsList ArgList { get; set; }
      internal Field ReturnValueField { get; set; }
      protected bool ForceModal { get; set; }
      internal string CallingTaskTag { get; set; }
      internal string PathParentTaskTag { get; set; }
      internal int SubformDitIdx { get; set; }
      internal String SubformCtrlName { get; set; }

      public LocalRunTimeCommandOpenTask(TaskDefinitionId taskDefinitionId)
      {
         this.TaskDefinitionId = taskDefinitionId;
         ForceModal = false;
         SubformDitIdx = Int32.MinValue;
      }

      /// <summary>
      /// 
      /// </summary>
      internal override void Execute()
      {
         IClientCommand cmd = null;

         bool canExecute = ClientManager.Instance.LocalManager.ApplicationDefinitions.TaskDefinitionIdsManager.CanExecuteTask(TaskDefinitionId);
         if (canExecute)
         {
            // create the command
            string taskUrl = getXmlTaskURL(ClientManager.Instance.LocalManager.ApplicationDefinitions.TaskDefinitionIdsManager, TaskDefinitionId);
            if (taskUrl != null)
               cmd = CommandFactory.CreateOpenTaskCommand(taskUrl, ArgList, ReturnValueField, ForceModal, CallingTaskTag,
                                                   PathParentTaskTag, SubformDitIdx, SubformCtrlName);
            else
            {
               // TODO - inform of error
            }

            base.Execute(cmd);
         }
         else
         {
            // User is not authorized - show message in statusbar.
            Task task = (Task)MGDataCollection.Instance.GetTaskByID(CallingTaskTag);
            Manager.WriteToMessagePanebyMsgId((Task)task.GetContextTask(), MsgInterface.CSTIO_STR_ERR2, ClientManager.StartedFromStudio);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      static public string getXmlTaskURL(TaskDefinitionIdsManager taskIdsManager, TaskDefinitionId taskDefinitionId)
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;

         string taskFileName = taskIdsManager.GetXmlId(taskDefinitionId);
         if (taskFileName == null)
            return null;
         string defaultTagList = taskIdsManager.GetDefaultTagList(taskDefinitionId);


         StringBuilder response = new StringBuilder();
         response.Append("<");
         response.Append(ConstInterface.MG_TAG_TASKURL);
         response.Append(" xmlId=\"");
         response.Append(taskFileName);
         response.Append("\" ");

         response.Append("tagList");
         response.Append("=\"");
         response.Append(defaultTagList);
         response.Append("\">");

         response.Append("</");
         response.Append(ConstInterface.MG_TAG_TASKURL);
         response.Append(">");

         return response.ToString();
      }
   }
}
