using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using System.Diagnostics;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.util;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.commands.ServerToClient
{
   class AbortCommand : ClientTargetedCommandBase
   {
      private string _transOwner;

      public AbortCommand() {}

      public AbortCommand(string taskTag)
      {
         TaskTag = taskTag;
      }

      public override void Execute(IResultValue res)
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;
         int oldMgdID = MGDataCollection.Instance.currMgdID;
         MGData mgd = null;

         var task = (Task)mgDataTab.GetTaskByID(TaskTag);

         // Pass transaction ownership 
         if (_transOwner != null)
         {
            var newTransOwnerTask = (Task)mgDataTab.GetTaskByID(_transOwner);
            if (newTransOwnerTask != null)
               newTransOwnerTask.setTransOwnerTask();
         }

         // On special occasions, the server may send abort commands on tasks which were not parsed yet
         if (task == null && ClientManager.Instance.EventsManager.ignoreUnknownAbort())
            return;
         Debug.Assert(task != null);
         mgd = task.getMGData();
         task.stop();
         mgd.abort();

         MGDataCollection.Instance.currMgdID = mgd.GetId();
         GUIManager.Instance.abort((MgForm)task.getForm());
         MGDataCollection.Instance.currMgdID = (mgd.GetId() != oldMgdID || mgd.getParentMGdata() == null
                                                   ? oldMgdID
                                                   : mgd.getParentMGdata().GetId());

         if (!ClientManager.Instance.validReturnToCtrl())
         {
            MgControl mgControl = GUIManager.getLastFocusedControl();
            ClientManager.Instance.ReturnToCtrl = mgControl;
            if (mgControl != null)// Refresh the status bar.
               ((MgForm)mgControl.getForm()).RefreshStatusBar();
         }
      }

      public override void HandleAttribute(string attribute, string value)
      {
         switch (attribute)
         {
            case ConstInterface.MG_ATTR_TRANS_OWNER:
               _transOwner = value;
               break;

            default:
               base.HandleAttribute(attribute, value);
               break;
         }

         // Signal the server has aborted the task.
         if (attribute == XMLConstants.MG_ATTR_TASKID)
         {
            var abortingTask = (Task)MGDataCollection.Instance.GetTaskByID(TaskTag);
            if (abortingTask != null)
               abortingTask.resetKnownToServer();
         }
      }
   }
}
