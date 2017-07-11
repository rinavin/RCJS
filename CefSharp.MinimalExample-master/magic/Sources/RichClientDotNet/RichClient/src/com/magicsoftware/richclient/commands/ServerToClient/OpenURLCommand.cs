using System;
using System.Collections.Generic;
using System.Diagnostics;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.data;

namespace com.magicsoftware.richclient.commands.ServerToClient
{
   class OpenURLCommand : ClientTargetedCommandBase
   {
      String _callingTaskTag;       // is the id of the task that called the new window
      String _pathParentTaskTag;    // is the id of path parent task tag
      String _subformCtrlName;      // Subform control name for Destination Subform
      int _ditIdx = Int32.MinValue;
      bool _isModal = true;         // Y|N
      string _transOwner;
      String _newId;
      bool _forceModal = false;
      String _key;
      ArgumentsList _varList;
      Field _returnVal;             // return value of verify opr
      internal String NewTaskXML { get; set; } 


      /// <summary>
      /// Empty constructor is needed for instantiation during deserialization.
      /// </summary>
      public OpenURLCommand()
      {}

      public OpenURLCommand(string key, string newTaskXML, ArgumentsList argList, Field retrnValueField,
                              bool forceModal, string callingTaskTag, string pathParentTaskTag,
                              int ditIdx, string subformCtrlName, string newId)
      {
         _key = key;
         _callingTaskTag = callingTaskTag;
         _newId = newId;
         NewTaskXML = newTaskXML;
         _forceModal = forceModal;
         _varList = argList;
         _returnVal = retrnValueField;
         _ditIdx = ditIdx;
         _subformCtrlName = subformCtrlName;
         _pathParentTaskTag = pathParentTaskTag;
      }

      public override void Execute(rt.IResultValue res)
      {
         MGData mgd = null;
         MGDataCollection mgDataTab = MGDataCollection.Instance;
         bool destinationSubformSucceeded = false;
         bool refreshWhenHidden = true;
         int mgdID = 0;
         MgControl subformCtrl = null;
         List<Int32> oldTimers = new List<Int32>(), newTimers = new List<Int32>();
         bool moveToFirstControl = true;
         Task guiParentTask;
         MGData guiParentMgData = null;
         Task callingTask = (_callingTaskTag != null ? (Task)mgDataTab.GetTaskByID(_callingTaskTag) : null);
         Task pathParentTask = (_pathParentTaskTag != null ? (Task)mgDataTab.GetTaskByID(_pathParentTaskTag) : null);
         MgForm formToBeActivatedOnClosingCurrentForm = null;

         Task lastFocusedTask = ClientManager.Instance.getLastFocusedTask();
         if (lastFocusedTask != null && lastFocusedTask.IsOffline)
            formToBeActivatedOnClosingCurrentForm = (MgForm)lastFocusedTask.getForm();

         // TODO (Ronak): It is wrong to set Parent task as a last focus task when 
         // non-offline task is being called from an offline task.

         //When a nonOffline task was called from an Offline task (of course via MP event),
         //the calling task id was not sent to the server because the server is unaware of the 
         //offline task. So, when coming back from the server, assign the correct calling task.
         //Task lastFocusedTask = ClientManager.Instance.getLastFocusedTask();
         //if (lastFocusedTask != null && lastFocusedTask.IsOffline)
         //   _callingTaskTag = lastFocusedTask.getTaskTag();

         guiParentTask = callingTask = (Task)mgDataTab.GetTaskByID(_callingTaskTag);
         if (callingTask != null)
            mgd = callingTask.getMGData();

         //QCR#712370: we should always perform refreshTables for the old MgData before before opening new window
         ClientManager.Instance.EventsManager.refreshTables();

         //ditIdx is send by server only for subform opening for refreshWhenHidden
         if ((_subformCtrlName != null) || (_ditIdx != Int32.MinValue))
         {
            subformCtrl = (_ditIdx != Int32.MinValue
                             ? (MgControl)callingTask.getForm().getCtrl(_ditIdx)
                             : ((MgForm)callingTask.getForm()).getSubFormCtrlByName(_subformCtrlName));
            if (subformCtrl != null)
            {
               var subformTask = subformCtrl.getSubformTask();
               guiParentTask = (Task)subformCtrl.getForm().getTask();
               mgdID = guiParentTask.getMgdID();
               guiParentMgData = guiParentTask.getMGData();
               if (guiParentMgData.getTimerHandlers() != null)
                  oldTimers = guiParentMgData.getTimerHandlers().getTimersVector();

               if (_ditIdx != Int32.MinValue) //for refresh when hidden
               {
                  refreshWhenHidden = false;
                  moveToFirstControl = false;
               }
               else //for destination
               {
                  destinationSubformSucceeded = true;
                  // Pass transaction ownership 
                  if (_transOwner != null)
                  {
                     var newTransOwnerTask = (Task)mgDataTab.GetTaskByID(_transOwner);
                     if (newTransOwnerTask != null)
                        newTransOwnerTask.setTransOwnerTask();
                  }

                  if (subformTask != null)
                  {
                     subformTask.setDestinationSubform(true);
                     subformTask.stop();
                  }

                  if (!ClientManager.Instance.validReturnToCtrl())
                     ClientManager.Instance.ReturnToCtrl = GUIManager.getLastFocusedControl();
               }

               subformCtrl.setSubformTaskId(_newId);
            }
         }

         MGData parentMgData;
         if (callingTask == null)
            parentMgData = MGDataCollection.Instance.getMGData(0);
         else
            parentMgData = callingTask.getMGData();

         if (!destinationSubformSucceeded && refreshWhenHidden)
         {
            mgdID = mgDataTab.getAvailableIdx();
            Debug.Assert(mgdID > 0);
            mgd = new MGData(mgdID, parentMgData, _isModal, _forceModal);
            mgd.copyUnframedCmds();
            MGDataCollection.Instance.addMGData(mgd, mgdID, false);

            MGDataCollection.Instance.currMgdID = mgdID;
         }

         Obj = _key;
         _key = null;

         try
         {
            // Large systems appear to consume a lot of memory, the garbage collector is not always
            // "quick" enough to catch it, so free memory before initiating a memory consuming job as
            // reading a new MGData.
            if (GC.GetTotalMemory(false) > 30000000)
               GC.Collect();

            ClientManager.Instance.ProcessResponse(NewTaskXML, mgdID, new OpeningTaskDetails(callingTask, pathParentTask, formToBeActivatedOnClosingCurrentForm), null);
         }
         finally
         {
            ClientManager.Instance.EventsManager.setIgnoreUnknownAbort(false);
         }

         if (callingTask != null && subformCtrl != null)
            callingTask.PrepareForSubform(subformCtrl);

         if (destinationSubformSucceeded || !refreshWhenHidden)
         {
            subformCtrl.initSubformTask();
            if (destinationSubformSucceeded)
            {
               var subformTask = subformCtrl.getSubformTask();
               moveToFirstControl = !callingTask.RetainFocus;
               subformTask.setIsDestinationCall(true);
            }
         }

         if (subformCtrl != null)
         {
            if (guiParentMgData.getTimerHandlers() != null)
               newTimers = guiParentMgData.getTimerHandlers().getTimersVector();
            guiParentMgData.changeTimers(oldTimers, newTimers);
         }

         Task nonInteractiveTask = ClientManager.Instance.StartProgram(destinationSubformSucceeded, moveToFirstControl, _varList, _returnVal,null);

         if (destinationSubformSucceeded || !refreshWhenHidden)
            guiParentTask.resetRcmpTabOrder();

         // in local tasks, ismodal is calculated after the main display, so we need to update the command member
         _isModal = mgd.IsModal;

         // If we have a non interactive task starting, we need to create an eventLoop for it , just like modal.
         // This is because we cannot allow the tasks above it to catch events.
         if (nonInteractiveTask == null)
         {
            // a non interactive parent will cause the called task to behave like modal by having its own events loop.
            // In case of main program caller (which is flagged as non interactive) we will have a new events loop for the called program
            // if the main program is without a form. If the main prog has a form , then the callee can be included in the main prog loop.
            if (callingTask != null && 
               ((_isModal && !destinationSubformSucceeded && refreshWhenHidden) || (!callingTask.IsInteractive && (!callingTask.isMainProg() || callingTask.getForm() == null))))
               ClientManager.Instance.EventsManager.EventsLoop(mgd);
         }
         else
            ClientManager.Instance.EventsManager.NonInteractiveEventsLoop(mgd, nonInteractiveTask);
      }

      public override bool IsBlocking
      {
         get
         {
            return _isModal && !WillReplaceWindow && (_subformCtrlName == null);
         }
      }

      public override void HandleAttribute(string attribute, string value)
      {
         switch (attribute)
         {
            case ConstInterface.MG_ATTR_CALLINGTASK:
               _callingTaskTag = value;
               break;

            case ConstInterface.MG_ATTR_PATH_PARENT_TASK:
               _pathParentTaskTag = value;
               break;

            case ConstInterface.MG_ATTR_SUBFORM_CTRL:
               _subformCtrlName = value;
               break;

            case XMLConstants.MG_ATTR_DITIDX:
               _ditIdx = XmlParser.getInt(value);
               break;

            case ConstInterface.MG_ATTR_MODAL:
               _isModal = (value[0] == '1');
               break;

            case ConstInterface.MG_ATTR_TRANS_OWNER:
               _transOwner = value;
               break;

            case ConstInterface.MG_ATTR_NEWID:
               _newId = value.TrimEnd(' ');
               break;

            case ConstInterface.MG_ATTR_KEY:
               _key = value;
               break;

            case ConstInterface.MG_ATTR_ARGLIST:
               _varList = new ArgumentsList();
               _varList.fillList(value, (Task)MGDataCollection.Instance.GetTaskByID(TaskTag));
               break;

            case ConstInterface.MG_ATTR_OBJECT:
               Obj = value;
               break;

            default:
               base.HandleAttribute(attribute, value);
               break;
         }
      }
   }
}
