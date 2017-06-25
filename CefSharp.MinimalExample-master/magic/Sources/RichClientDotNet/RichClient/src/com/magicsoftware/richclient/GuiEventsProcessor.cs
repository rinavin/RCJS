using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using com.magicsoftware.richclient.events;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.http;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.exp;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.util;
using RichClient.Properties;
using Task = com.magicsoftware.richclient.tasks.Task;
using RuntimeContext = com.magicsoftware.unipaas.management.RuntimeContextBase;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient
{
   /// <summary>
   /// this class centralizes the RIA processing of events exposed by the MgGui.dll
   /// </summary>
   internal class GuiEventsProcessor : EventsProcessor
   {
      /// <summary>
      /// register handlers in MgxpaRIA.exe for events exposed but not handled by MgGui.dll
      /// </summary>
      protected override void RegisterSubclassHandlers()
      {
         Events.FocusEvent += processFocus;
         Events.EditNodeEvent += processEditNode;
         Events.EditNodeExitEvent += processEditNodeExit;
         Events.CollapseEvent += processCollapse;
         Events.ExpandEvent += processExpand;
         Events.TableReorderEvent += processTableReorder;
         Events.MouseUpEvent += processMouseUp;
         Events.BrowserStatusTxtChangeEvent += processBrowserStatusTxtChangeEvent;
         Events.BrowserExternalEvent += processBrowserExternalEvent;
         Events.DotNetEvent += ProcessDotNetEvent;
         Events.SelectionEvent += processSelection;
         Events.WideEvent += processWide;
         Events.DisposeEvent += processDispose;
         Events.TimerEvent += ProcessTimer;
         Events.TableResizeEvent += processTableResize;
         Events.GetRowsDataEvent += processGetRowsData;
         Events.EnableCutCopyEvent += processEnableCutCopy;
         Events.EnablePasteEvent += processEnablePaste;
         Events.NonParkableLastParkedCtrlEvent += OnNonParkableLastParkedCtrl;
#if !PocketPC
         Events.DisplaySessionStatisticsEvent += DisplaySessionStatistics;
         Events.IsFormLockAllowedEvent += isFormLockAllowed;
         Events.ShowSessionStatisticsEvent += ShowSessionStatistics;
#endif
         Events.RefreshTablesEvent += ClientManager.Instance.EventsManager.refreshTables;

         Events.MenuProgramSelectionEvent += OnMenuProgramSelection;
         Events.MenuEventSelectionEvent += OnMenuEventSelection;
         Events.MenuOSCommandSelectionEvent += OnMenuOSCommandSelection;
         Events.BeforeContextMenuEvent += OnBeforeContextMenu;
         Events.OnOpenContextMenuEvent += AddOpenContextMenuEvent;

         Events.WriteExceptionToLogEvent += Logger.Instance.WriteExceptionToLog;
         Events.WriteErrorToLogEvent += Logger.Instance.WriteErrorToLog;
         Events.WriteWarningToLogEvent += Logger.Instance.WriteWarningToLog;
         Events.ShouldLogEvent += Logger.Instance.ShouldLog;
         Events.WriteGuiToLogEvent += Logger.Instance.WriteGuiToLog;
         Events.WriteDevToLogEvent += Logger.Instance.WriteDevToLog;

         Events.GetLocalFileNameEvent += GetLocalFileName;
         Events.GetDNAssemblyFileEvent += GetDNAssemblyFile;
         Events.GetContentEvent += ClientManager.Instance.GetRemoteContent;
         Events.GetDataViewContentEvent += ClientManager.Instance.EventsManager.GetDataViewContent;

         Events.TranslateEvent += ClientManager.Instance.getLanguageData().translate;
         Events.CloseTasksOnParentActivateEvent += ClientManager.Instance.getEnvironment().CloseTasksOnParentActivate;
         Events.InIncrementalLocateEvent += ClientManager.Instance.InIncrementalLocate;
         Events.PeekEndOfWorkEvent += peekEndOfWork;
         Events.GetResourceObjectEvent += getResourceObject;

         Events.TranslateLogicalNameEvent += ClientManager.Instance.getEnvParamsTable().translate;
         Events.IsRelativeCacheRequestURLEvent += HttpManager.IsRelativeRequestURL;
         Events.GetMainProgramEvent += getMainProgram;
         Events.GetMessageStringEvent += ClientManager.Instance.getMessageString;

         Events.CtrlFocusEvent += processCtrlFocus;
         Events.GetCurrentTaskEvent += ClientManager.Instance.getCurrTask;
         Events.GetRuntimeContextEvent += GetRuntimeContext;
         Events.SaveLastClickedCtrlEvent += SaveLastClickedControlName;
         Events.SaveLastClickInfoEvent += SaveLastClickInfo;
         Events.DNControlValueChangedEvent += ProcessDNControlValueChangedEvent;
         Events.PressEvent += ProcessPress;

         Events.OnIsLogonRTLEvent += IsLogonRTL;
         Events.ShouldAddEnterAsKeyEvent += ShouldAddEnterAsKeyEvent;
         Events.CanTreeClickBeSkippedOnMouseDownEvent += CanTreeClickBeSkippedOnMouseDownEvent;

         //events invoked from system's default context menu.
         Events.CutEvent += OnCut;
         Events.CopyEvent += OnCopy;
         Events.PasteEvent += OnPaste;
         Events.ClearEvent += OnClear;
         Events.UndoEvent += OnUndo;
      }

      /// <summary> </summary>
      /// <param name="iTask"></param>
      /// <param name="mgControl"></param>
      private void processCtrlFocus(ITask iTask, MgControlBase mgControl)
      {
         GUIManager.setLastFocusedControl((Task)iTask, mgControl);
      }

      #if !PocketPC
      /// <summary>Put ACT_CTRL_FOCUS, ACT_CTRL_HIT and MG_ACT_BEGIN_DROP to Runtime thread.</summary>
      protected override void processBeginDrop(GuiMgForm guiMgForm,GuiMgControl guiMgCtrl, int line)
      {
         MgControl mgControl = (MgControl) guiMgCtrl;

         if (mgControl != null && (mgControl.Type == MgControlType.CTRL_TYPE_TEXT || mgControl.Type == MgControlType.CTRL_TYPE_TREE))
            ClientManager.Instance.EventsManager.addGuiTriggeredEvent(mgControl, line, InternalInterface.MG_ACT_CTRL_FOCUS, false);

         base.processBeginDrop(guiMgForm, guiMgCtrl, line);
      }
      #endif

      /// <summary>
      /// This function handles DN control value changed event.
      /// </summary>
      /// <param name="guiMgCtrl">DN control</param>
      /// <param name="line">the line of multi line control</param>
      private void ProcessDNControlValueChangedEvent(GuiMgControl guiMgCtrl, int line)
      {
         Debug.Assert(Misc.IsGuiThread());

         //Get the value of the control i.e value of property specified in PropInterface.PROP_TYPE_DN_CONTROL_VALUE_PROPERTY.
         object ctrlVal = ((MgControl)guiMgCtrl).GetDNControlValue();

         //Put action to update the field with property value.
         var rtEvt = new RunTimeEvent((MgControl)guiMgCtrl, line, true);
         rtEvt.setInternal(InternalInterface.MG_ACT_UPDATE_DN_CONTROL_VALUE);
         rtEvt.DotNetArgs = new object[] { ctrlVal };
         ClientManager.Instance.EventsManager.addToTail(rtEvt);
      }

      /// <summary>process "focus" event</summary>
      /// <param name = "value">the value of the control</param>
      /// <param name = "line">the line of multiline control</param>
      /// <returns> TRUE if Magic internal rules allow us to perform the focus.
      ///   sometimes, Magic forces us to return to the original control thus FALSE will be returned.
      /// </returns>
      internal void processFocus(GuiMgControl guiMgCtrl, int line, bool isProduceClick, bool onMultiMark)
      {
         var mgControl = (MgControl) guiMgCtrl;
         ClientManager.Instance.EventsManager.addGuiTriggeredEvent(mgControl, line,
                                                                   InternalInterface.MG_ACT_CTRL_FOCUS,
                                                                   isProduceClick);
      }

      /// <summary>edit tree node</summary>
      ///<param name = "guiMgCtrl"></param>
      /// <param name = "line"></param>
      internal void processEditNode(GuiMgControl guiMgCtrl, int line)
      {
         var mgControl = (MgControl) guiMgCtrl;
         ClientManager.Instance.EventsManager.addGuiTriggeredEvent(mgControl, line,
                                                                   InternalInterface.MG_ACT_TREE_RENAME_RT, false);
      }

      /// <summary>edit tree node</summary>
      ///<param name = "guiMgCtrl"></param>
      /// <param name = "line"></param>
      internal void processEditNodeExit(GuiMgControl guiMgCtrl, int line)
      {
         var mgControl = (MgControl) guiMgCtrl;
         ClientManager.Instance.EventsManager.addGuiTriggeredEvent(mgControl, line,
                                                                   InternalInterface.MG_ACT_TREE_RENAME_RT_EXIT, false);
      }

      /// <summary>process "collapse" event</summary>
      ///<param name = "guiMgCtrl">the control</param>
      internal void processCollapse(GuiMgControl guiMgCtrl, int line)
      {
         var mgControl = (MgControl) guiMgCtrl;
         ClientManager.Instance.EventsManager.addGuiTriggeredEvent(mgControl, line,
                                                                   InternalInterface.MG_ACT_TREE_COLLAPSE, false);
      }

      /// <summary>
      ///   process "collapse" event
      /// </summary>
      ///<param name = "guiMgCtrl">the control</param>
      internal void processExpand(GuiMgControl guiMgCtrl, int line)
      {
         var mgControl = (MgControl) guiMgCtrl;
         ClientManager.Instance.EventsManager.addGuiTriggeredEvent(mgControl, line,
                                                                        InternalInterface.MG_ACT_TREE_EXPAND, false);
      }

      /// <summary>process table reorder event</summary>
      ///<param name = "guiMgCtrl"> table control</param>
      /// <param name = "tabOrderList">list of table children ordered by automatic tab order</param>
      internal void processTableReorder(GuiMgControl guiMgCtrl, List<GuiMgControl> guiTabOrderList)
      {
         var mgControl = (MgControl) guiMgCtrl;

         var tabOrderList = new List<MgControlBase>();
         foreach (GuiMgControl guiCtrl in guiTabOrderList)
            tabOrderList.Add((MgControl) guiCtrl);

         ClientManager.Instance.EventsManager.addGuiTriggeredEvent(mgControl, InternalInterface.MG_ACT_TBL_REORDER,
                                                                        tabOrderList);
      }

      /// <summary>process "click" event</summary>
      ///<param name = "guiMgCtrl">the control</param>
      /// <param name = "line"> the line of the multiline control</param>
      /// <param name = "value">the value of the control</param>
      internal void processMouseUp(GuiMgControl guiMgCtrl, int line)
      {
         var mgControl = (MgControl) guiMgCtrl;
         var rtEvt = new RunTimeEvent(mgControl, line, true);
         bool produceClick = true;
         if (mgControl.Type == MgControlType.CTRL_TYPE_BUTTON &&
             ((CtrlButtonTypeGui)mgControl.getProp(PropInterface.PROP_TYPE_BUTTON_STYLE).getValueInt()) ==
             CtrlButtonTypeGui.Hypertext)
            produceClick = false;
         rtEvt.setProduceClick(produceClick);
         rtEvt.setInternal(InternalInterface.MG_ACT_CTRL_MOUSEUP);
         ClientManager.Instance.EventsManager.addToTail(rtEvt);
      }

      /// <summary>process browser events</summary>
      /// <param name = "guiMgCtrl">browser control</param>
      /// <param name = "evt">the event code from InternalInterface</param>
      /// <param name = "text">the text of the event</param>
      internal void processBrowserEvent(GuiMgControl guiMgCtrl, int evt, String text)
      {
         var browserCtrl = (MgControl) guiMgCtrl;
         var rtEvt = new RunTimeEvent(browserCtrl, browserCtrl.getDisplayLine(false), true);
         rtEvt.setInternal(evt);

         var argsList = new GuiExpressionEvaluator.ExpVal[1];
         //create the string for the blob
         string blobString = BlobType.createFromString(text, BlobType.CONTENT_TYPE_UNICODE);
         argsList[0] = new GuiExpressionEvaluator.ExpVal(StorageAttribute.BLOB, false, blobString);
         var args = new ArgumentsList(argsList);
         rtEvt.setArgList(args);

         ClientManager.Instance.EventsManager.addToTail(rtEvt);
      }
      internal void processBrowserStatusTxtChangeEvent(GuiMgControl guiMgCtrl, String text)
      {
         processBrowserEvent(guiMgCtrl, InternalInterface.MG_ACT_BROWSER_STS_TEXT_CHANGE, text);
      }

      internal void processBrowserExternalEvent(GuiMgControl guiMgCtrl, String text)
      {
         processBrowserEvent(guiMgCtrl, InternalInterface.MG_ACT_EXT_EVENT, text);
      }

      /// <summary>
      ///   Release references to mgData and to the task in order to let the gc to free the memory.
      ///   This code was moved from processClose in order to clean also when the "open window = NO" (non interactive tasks).
      /// </summary>
      /// <param name = "form"></param>
      internal void processDispose(GuiMgForm guiMgForm)
      {
         var form = (MgForm)guiMgForm;
         var task = (Task)form.getTask();
         MGData mgd = task.getMGData();

         RCTimer.StopAll(mgd);

         if (mgd.IsAborting)
            MGDataCollection.Instance.deleteMGDataTree(mgd.GetId());
      }

      /// <summary>process "timer" event</summary>
      /// <param name = "mgTimer">object of 'MgTimer' class</param>
      internal void ProcessTimer(MgTimer mgTimer)
      {
         MGData mgd = ((RCTimer)mgTimer).GetMgdata();
         var task = mgd.getFirstTask();

         // MgTimer/RCTimer uses interval in milliseconds but RuntimeEvent uses interval in seconds
         // so convert it to seconds.
         int seconds = (((RCTimer)mgTimer).TimerIntervalMiliSeconds)/1000;
         bool isIdle = ((RCTimer)mgTimer).IsIdleTimer;

         if (mgd.IsAborting)
            return;

         var rtEvt = new RunTimeEvent(task, true);
         rtEvt.setTimer(seconds, mgd.GetId(), isIdle);
         rtEvt.setMainPrgCreator(rtEvt.getTask());
         if (!isIdle)
            rtEvt.setCtrl((MgControl)task.getLastParkedCtrl());
         rtEvt.setInternal(InternalInterface.MG_ACT_TIMER);
         ClientManager.Instance.EventsManager.addToTail(rtEvt);
      }

      /// <summary>process Resize of the page, for table controls only</summary>
      /// <param name = "guiMgCtrl"></param>
      /// <param name = "newRowsInPage"></param>
      internal void processTableResize(GuiMgControl guiMgCtrl, int newRowsInPage)
      {
         var mgControl = (MgControl) guiMgCtrl;
         if (mgControl != null)
         {
            var rtEvt = new RunTimeEvent(mgControl, newRowsInPage, true);
            rtEvt.setInternal(InternalInterface.MG_ACT_RESIZE);
            ClientManager.Instance.EventsManager.addToTail(rtEvt);
         }
      }

      /// <summary>process set data for row</summary>
      ///<param name = "guiMgCtrl">table control</param>
      /// <param name = "sendAll">if true , always send all records</param>
      internal void processGetRowsData(GuiMgControl guiMgCtrl, int desiredTopIndex, bool sendAll,
                                       LastFocusedVal lastFocusedVal)
      {
         var mgControl = (MgControl)guiMgCtrl;
         if (mgControl != null && mgControl.Type == MgControlType.CTRL_TYPE_TABLE)
         {
            var rtEvt = new RunTimeEvent(mgControl, desiredTopIndex, true);
            rtEvt.setInternal(InternalInterface.MG_ACT_ROW_DATA_CURR_PAGE);
            rtEvt.setSendAll(sendAll);
            rtEvt.LastFocusedVal = lastFocusedVal;
            ClientManager.Instance.EventsManager.addToTail(rtEvt);
         }
      }

      /// <summary>triggered by gui. enable/disable a given act list.</summary>
      /// <param name = "guiMgCtrl"></param>
      /// <param name = "actList"></param>
      /// <param name = "enable"></param>
      internal void processEnableActs(GuiMgControl guiMgCtrl, int[] actList, bool enable)
      {
         var mgControl = (MgControl) guiMgCtrl;
         var rtEvt = new RunTimeEvent(mgControl, true);
         rtEvt.setInternal(enable
                              ? InternalInterface.MG_ACT_ENABLE_EVENTS
                              : InternalInterface.MG_ACT_DISABLE_EVENTS);
         rtEvt.setActEnableList(actList);
         ClientManager.Instance.EventsManager.addToTail(rtEvt);
      }

      /// <summary>Enable/Disable cut/copy</summary>
      /// <param name = "guiMgCtrl"></param>
      /// <param name = "enable"></param>
      internal void processEnableCutCopy(GuiMgControl guiMgCtrl, bool enable)
      {
         var mgControl = (MgControl) guiMgCtrl;
         var actCutCopy = new[] {InternalInterface.MG_ACT_CUT, InternalInterface.MG_ACT_CLIP_COPY};
         processEnableActs(guiMgCtrl, actCutCopy, enable);
         Manager.MenuManager.refreshMenuActionForTask(mgControl.getForm().getTask());
      }

      /// <summary>Enable/Disable paste.</summary>
      /// <param name = "guiMgCtrl"></param>
      /// <param name = "enable"></param>
      internal void processEnablePaste(GuiMgControl guiMgCtrl, bool enable)
      {
         var mgControl = (MgControl) guiMgCtrl;
         if (mgControl != null && mgControl.isTextOrTreeEdit() && mgControl.isModifiable())
         {
            var actPaste = new[] {InternalInterface.MG_ACT_CLIP_PASTE};

            processEnableActs(guiMgCtrl, actPaste, enable);
            Manager.MenuManager.refreshMenuActionForTask(mgControl.getForm().getTask());
         }
      }

      /// <summary>
      /// If current control becomes invisible/disabled/non parkable, then put MG_ACT_TBL_NXTFLD into queue.
      /// </summary>
      /// <param name="ctrl">control whose property is changed.</param>
      internal void OnNonParkableLastParkedCtrl(GuiMgControl ctrl)
      {
          MgControlBase mgControl = (MgControlBase)ctrl;

          //If task is already in exiting edit state, do not add MG_ACT_TBL_NXTFLD. (Defect 67647)
          if (ClientManager.Instance.EventsManager.getForceExit() != ForceExit.Editing)
          {
              RunTimeEvent rtEvt = new RunTimeEvent((MgControl)mgControl, mgControl.getDisplayLine(false), false);
              rtEvt.setInternal(InternalInterface.MG_ACT_TBL_NXTFLD);

              ClientManager.Instance.EventsManager.addToTail(rtEvt);
          }
      }

      /// <summary>process "selection" event</summary>
      /// <param name = "val">the value of the control </param>
      /// <param name = "guiMgCtrl">the control </param>
      /// <param name = "line"> the line of the multiline control </param>
      /// <param name = "produceClick">TODO </param>
      internal void processSelection(String val, GuiMgControl guiMgCtrl, int line, bool produceClick)
      {
         var mgControl = (MgControl) guiMgCtrl;
         if (mgControl.Type == MgControlType.CTRL_TYPE_BUTTON && mgControl.getForm().getTask().getLastParkedCtrl() != mgControl)
            produceClick = true;

         var rtEvt = new RunTimeEvent(mgControl, line, true);
         rtEvt.setInternal(InternalInterface.MG_ACT_SELECTION);
         rtEvt.setValue(val);
         rtEvt.setProduceClick(produceClick);
         ClientManager.Instance.EventsManager.addToTail(rtEvt);
      }

      /// <summary></summary>
      /// <param name = "guiMgForm"></param>
      /// <param name = "guiMgCtrl"></param>
      /// <param name = "modifier"></param>
      /// <param name = "keyCode"></param>
      /// <param name = "start"></param>
      /// <param name = "end"></param>
      /// <param name = "text"></param>
      /// <param name = "im"></param>
      /// <param name = "isActChar"></param>
      /// <param name = "suggestedValue"></param>
      /// <param name = "dotNetArgs"></param>
      /// <param name = "comboIsDropDown"></param>
      /// <param name="handled">boolean variable event is handled or not.</param>
      /// <returns> true only if we have handled the KeyDown event (otherwise the CLR should handle). If true magic will handle else CLR will handle.</returns>
      protected override bool processKeyDown(GuiMgForm guiMgForm, GuiMgControl guiMgCtrl, Modifiers modifier,
                                             int keyCode,
                                             int start, int end,
                                             String text, ImeParam im, bool isActChar, String suggestedValue,
                                             bool comboIsDropDown, bool handled)
      {
         // time of last user action for IDLE function
         ClientManager.Instance.LastActionTime = Misc.getSystemMilliseconds();

         // DOWN or UP invoked on a SELECT/RADIO control
         return base.processKeyDown(guiMgForm, guiMgCtrl, modifier, keyCode,
                             start, end, text, im, isActChar, suggestedValue, comboIsDropDown, handled);
      }

      /// <summary>send wide action to the control</summary>
      /// <param name = "guiMgCtrl"></param>
      internal void processWide(GuiMgControl guiMgCtrl)
      {
         var mgControl = (MgControl) guiMgCtrl;
         if (mgControl != null && !mgControl.isWideControl() && !mgControl.getForm().wideIsOpen())
            processWide(mgControl);
      }

      /// <summary>send wide action to the control</summary>
      ///<param name = "guiMgCtrl"></param>
      internal void processWide(MgControl ctrl)
      {
         ClientManager.Instance.EventsManager.addGuiTriggeredEvent(ctrl, InternalInterface.MG_ACT_WIDE, 0);
      }

      /// <summary>process a dot net event.</summary>
      /// <param name="sender">the object from which the event was raised.</param>
      /// <param name="mgControl">an (optional) control for which the variable was selected. 
      /// if null, the invoked object is not a control on a form.</param>
      /// <param name="eventName"></param>
      /// <param name="parameters">parameters passed to the event handler.</param>
      internal void ProcessDotNetEvent(Object sender, GuiMgControl mgControl, String eventName, object[] parameters)
      {
         if (mgControl != null)
         {
            var rtEvt = new RunTimeEvent((MgControl)mgControl, 0, true);
            rtEvt.setDotNet(eventName);
            rtEvt.DotNetArgs = parameters;
            ClientManager.Instance.EventsManager.addToTail(rtEvt);
         }
         else //.net events on objects that are not controls and not connected to any task
         {
            //QCR #926669 , For sibling tasks, events order will be random. But when there is hierarchy, the lower task in the subtask tree should be performed first.
            List<Task> tasks = ClientManager.Instance.getTasksByObject(sender);
            tasks.Sort(ClientManager.CompareByDepth);
            foreach (Task item in tasks)
            {
               var rtEvt = new RunTimeEvent(item, true);
               rtEvt.setDotNet(eventName);
               rtEvt.DotNetObject = sender;
               rtEvt.DotNetArgs = parameters;
               ClientManager.Instance.EventsManager.addToTail(rtEvt);
            }
         }
      }

#if !PocketPC
      /// <summary></summary>
      private void DisplaySessionStatistics()
      {
         if (ClientManager.Instance.getDisplayStatisticInfo())
         {
            var sessionStatistics = new SessionStatisticsForm();
            sessionStatistics.WriteToInternalLog();
            
            // the studio is able to direct the session statistics to its activity monitor, 
            // therefore popping up the session statistics form was chosen to be disabled, in order to prevent annoyance during executions from the studio.
            if (!ClientManager.StartedFromStudio)
            {
               Application.UseWaitCursor = false;
               sessionStatistics.ShowDialog();
            }
         }
      }
#endif

      /// <summary>
      ///   check is we are allowed to lock the form due to long worker thread activity
      /// </summary>
      /// <returns></returns>
      private bool isFormLockAllowed()
      {
         if (ClientManager.Instance.isDelayInProgress())
            //if we are here because of execution of delay operation, do not change cursor
            return false;
         return ClientManager.Instance.EventsManager.AllowFormsLock;
      }

      /// <summary>
      /// Handles menu selection
      /// </summary>
      /// <param name="contextID">active/target context (irelevant for RC)</param>
      /// <param name="menuEntry"></param>
      /// <param name="activeForm"></param>
      /// <param name="activatedFromMDIFrame"></param>
      private void OnMenuProgramSelection(Int64 contextID, MenuEntryProgram menuEntryProgram, GuiMgForm activeForm, bool activatedFromMdiFrame)
      {
         MenuManager.onProgramMenuSelection(contextID, menuEntryProgram, (MgForm)activeForm, activatedFromMdiFrame);
      }

      /// <summary>
      /// Handles menu selection
      /// </summary>
      /// <param name="contextID">active/target context (irelevant for RC)</param>
      /// <param name = "menuEntry"></param>
      /// <param name = "activeForm"></param>
      /// <param name = "activatedFromMDIFrame"></param>
      private void OnMenuOSCommandSelection(Int64 contextID, MenuEntryOSCommand osCommandMenuEntry, GuiMgForm activeForm)
      {
         MenuManager.onOSMenuSelection(contextID, osCommandMenuEntry, (MgForm)activeForm);
      }

      /// <summary>
      /// Handles menu selection
      /// </summary>
      /// <param name="contextID">active/target context (irelevant for RC)</param>
      /// <param name = "menuEntry"></param>
      /// <param name = "activeForm"></param>
      /// <param name = "activatedFromMDIFrame"></param>
      private void OnMenuEventSelection(Int64 contextID, MenuEntryEvent menuEntryEvent, GuiMgForm activeForm, int ctlIdx)
      {
         MenuManager.onEventMenuSelection(menuEntryEvent, (MgForm)activeForm, ctlIdx);
      }

      /// <summary> This function is called when right click is pressed before opening the context menu. </summary>
      /// <param name = "guiMgControl"></param>
      /// <param name="line"></param>
      /// <returns></returns>
      private bool OnBeforeContextMenu(GuiMgControl guiMgControl, int line, bool onMultiMark)
      {
         //When right click is pressed on any control, we might need to move focus to that control
         //properly, I mean, we should also execute its CP, etc.
         bool focusChanged = false;

         if (guiMgControl is MgControl)
         {
            var mgControl = (MgControl) guiMgControl;

            // Click on a subform control is as a click on a form without control
            if (!guiMgControl.isSubform())
            {
               ClientManager.Instance.RuntimeCtx.CurrentClickedCtrl = mgControl;
               processFocus(guiMgControl, line, false, onMultiMark);
               focusChanged = true;
            }
         }

         return focusChanged;
      }

      /// <summary> Add Open Context Menu event on right click on form.</summary>
      /// <param name = "guiMgControl">control </param>
      /// <param name = "guiMgForm">code of internal event </param>
      /// <param name = "left">code of internal event </param>
      /// <param name = "top">code of internal event </param>
      /// <param name = "line">code of internal event </param>
      public void AddOpenContextMenuEvent(GuiMgControl guiMgControl, GuiMgForm guiMgForm, int left, int top, int line)
      {
         MgControl ctrl = null;
         Task task = null;

         if (guiMgControl != null)
         {
            if (guiMgControl is MgControl)
            {
               ctrl = (MgControl)guiMgControl;
               if (ctrl.Type == MgControlType.CTRL_TYPE_SUBFORM)
                  task = (Task)ctrl.GetSubformMgForm().getTask();
               else
                  task = (Task)((MgControlBase)guiMgControl).getForm().getTask();
            }
         }
         else
            task = (Task)((MgFormBase)guiMgForm).getTask();

            // Prepare event for MG_ACT_CONTEXT_MENU to open context menu.
            var rtEvt = new RunTimeEvent(task);
         rtEvt.setInternal(InternalInterface.MG_ACT_CONTEXT_MENU);

         rtEvt.setCtrl(ctrl);
         //Prepare an argument list for left and top co-ordinate.
         var argsList = new GuiExpressionEvaluator.ExpVal[3];

         argsList[0] = new GuiExpressionEvaluator.ExpVal(StorageAttribute.ALPHA, false, left.ToString());
         argsList[1] = new GuiExpressionEvaluator.ExpVal(StorageAttribute.ALPHA, false, top.ToString());
         argsList[2] = new GuiExpressionEvaluator.ExpVal(StorageAttribute.ALPHA, false, line.ToString());
         var args = new ArgumentsList(argsList);
         rtEvt.setArgList(args);

         ClientManager.Instance.EventsManager.addToTail(rtEvt);
      }

      /// <summary>get the file name in the local file system of a given url.</summary>
      /// <param name="url">a url (absolute or relative) to retrieve thru the web server.</param>
      /// <returns>file name in the local file system.</returns>
      private String GetLocalFileName(String url, TaskBase task)
      {
         // retrieve the file from the server (if found in the client's cache - no access will be made to the server).
         return ((Task)task).CommandsProcessor.GetLocalFileName (url, (Task)task, false);
      }

      /// <summary>
      /// Get .Net assembly file name specified by url in local file system
      /// </summary>
      /// <param name="url"></param>
      /// <returns></returns>
      private String GetDNAssemblyFile(String url)
      {
         // Since .NET assemblies are loaded dynamically in ReflectionServices, it is difficult to get the task that is
         // trying to load the assembly. However, getCurrMGData().getFirstTask() serves our purpose as the task is used
         // only to get commands processor
         TaskBase task = MGDataCollection.Instance.getCurrMGData().getFirstTask();

         // At the time of loading .net assembly for initializing the .net objects in main program, getFirstTask () returns
         // null if the main program does not have MDI frame. Then use main program to get the commands processor.
         if (task == null)
         {
            MGData mgd = MGDataCollection.Instance.getMGData(0);
            task = mgd.getMainProg(0);
         }

         return (GetLocalFileName(url, task));
      }

      /// <summary>peek the value of the "end of work" flag.</summary>
      private bool peekEndOfWork()
      {
         return ClientManager.Instance.EventsManager.getEndOfWork();
      }

      /// <summary> </summary>
      /// <param name="resourceName"></param>
      /// <returns></returns>
      private Object getResourceObject(String resourceName)
      {
         return Resources.ResourceManager.GetObject(resourceName);
      }

      /// <summary>This method returns Main program for a ctl</summary>
      /// <param name="contextID">active/target context (irelevant for RC)</param>
      /// <param name="ctlIdx"></param>
      /// <returns></returns>
      private Task getMainProgram(Int64 contextID, int ctlIdx)
      {
         return (Task)MGDataCollection.Instance.GetMainProgByCtlIdx(contextID, ctlIdx);
      }

      /// <summary>
      /// Get the RuntimeContext of ClientManager.
      /// </summary>
      /// <returns></returns>
      private RuntimeContext GetRuntimeContext(Int64 contextID)
      {
         return ClientManager.Instance.RuntimeCtx;
      }

      /// <summary>
      /// Saves the last clicked control in a respective RuntimeContextBase.
      /// </summary>
      /// <param name="guiMgControl"></param>
      /// <param name="ctrlName"></param>
      private void SaveLastClickedControlName(GuiMgControl guiMgControl, String ctrlName)
      {
         ClientManager.Instance.RuntimeCtx.LastClickedCtrlName = ctrlName;
      }

      /// <summary>
      /// Saves the last clicked information.
      /// </summary>
      /// <param name="guiMgForm"></param>
      /// <param name="controlName"></param>
      /// <param name="X"></param>
      /// <param name="Y"></param>
      /// <param name="offsetX"></param>
      /// <param name="offsetY"></param>
      /// <param name="LastClickCoordinatesAreInPixels"></param>
      private void SaveLastClickInfo(GuiMgForm guiMgForm, String controlName, int X, int Y, int offsetX, int offsetY, bool LastClickCoordinatesAreInPixels)
      {
         ClientManager.Instance.RuntimeCtx.SaveLastClickInfo(controlName, X, Y, offsetX, offsetY, LastClickCoordinatesAreInPixels);
      }

      /// <summary>
      /// Process press event. Move Focus and raise internal press event
      /// </summary>
      /// <param name="guiMgForm"></param>
      /// <param name="guiMgCtrl"></param>
      /// <param name="line"></param>
      private void ProcessPress(GuiMgForm guiMgForm, GuiMgControl guiMgCtrl, int line)
      {
         MgControlBase mgControl = (MgControlBase)guiMgCtrl;

         // The internal press event is fired if press event is fired on form or subform
         if (mgControl == null)
            Manager.EventsManager.addGuiTriggeredEvent(((MgFormBase)guiMgForm).getTask(), InternalInterface.MG_ACT_HIT);
         else if (mgControl.isSubform())
            mgControl.OnSubformClick();
         else
            Manager.EventsManager.addGuiTriggeredEvent(mgControl, InternalInterface.MG_ACT_CTRL_HIT, line);

         Manager.EventsManager.addGuiTriggeredEvent(mgControl, InternalInterface.MG_ACT_PRESS, line);
      }

      /// <summary>
      /// Should we show logon window RTL or not.
      /// </summary>
      /// <returns></returns>
      private bool IsLogonRTL()
      {
         return ClientManager.Instance.IsLogonRTL();
      }

      /// <summary>
      /// </summary>
      private void ShowSessionStatistics()
      {
         if (ClientManager.Instance.getDisplayStatisticInfo())
         {
            var sessionStatisticsForm = new SessionStatisticsForm();
            sessionStatisticsForm.Show();
            sessionStatisticsForm.WriteToInternalLog();
         }
      }

      /// <summary>
      /// In RIA, enter will cause a keyboard event according to the OS.
      /// This is mostly important for default button, where ENTER on any control triggers click event on the button.
      /// </summary>
      /// <returns></returns>
      private bool ShouldAddEnterAsKeyEvent()
      {
         return false;
      }

      /// <summary>
      /// In RIA , we button down on tree will do click event. In online it might be skipped.
      /// </summary>
      /// <returns></returns>
      private bool CanTreeClickBeSkippedOnMouseDownEvent()
      {
         return false;
      }

      /// <summary>
      /// Handles external event recieved via WM_CUT
      /// </summary>
      /// <param name="guiMgCtrl">ctrl to which this message is received</param>
      private void OnCut(GuiMgControl guiMgCtrl)
      {
         var rtEvt = new RunTimeEvent((MgControl)guiMgCtrl, ((MgControl)guiMgCtrl).getDisplayLine(false), true);
         rtEvt.setInternal(InternalInterface.MG_ACT_CUT);
         ClientManager.Instance.EventsManager.addToTail(rtEvt);
      }

      /// <summary>
      /// Handles external event recieved via WM_COPY
      /// </summary>
      /// <param name="guiMgCtrl">ctrl to which this message is received</param>
      private void OnCopy(GuiMgControl guiMgCtrl)
      {
         var rtEvt = new RunTimeEvent((MgControl)guiMgCtrl, ((MgControl)guiMgCtrl).getDisplayLine(false), true);
         rtEvt.setInternal(InternalInterface.MG_ACT_CLIP_COPY);
         ClientManager.Instance.EventsManager.addToTail(rtEvt);
      }

      /// <summary>
      /// Handles external event recieved via WM_PASTE
      /// </summary>
      /// <param name="guiMgCtrl">ctrl to which this message is received</param>
      private void OnPaste(GuiMgControl guiMgCtrl)
      {
         var rtEvt = new RunTimeEvent((MgControl)guiMgCtrl, ((MgControl)guiMgCtrl).getDisplayLine(false), true);
         rtEvt.setInternal(InternalInterface.MG_ACT_CLIP_PASTE);
         ClientManager.Instance.EventsManager.addToTail(rtEvt);
      }

      /// <summary>
      /// Handles external event recieved via WM_CLEAR
      /// </summary>
      /// <param name="guiMgCtrl">ctrl to which this message is received</param>
      private void OnClear(GuiMgControl guiMgCtrl)
      {
         var rtEvt = new RunTimeEvent((MgControl)guiMgCtrl, ((MgControl)guiMgCtrl).getDisplayLine(false), true);
         rtEvt.setInternal(InternalInterface.MG_ACT_EDT_DELCURCH);
         ClientManager.Instance.EventsManager.addToTail(rtEvt);
      }

      /// <summary>
      /// Handles external event recieved via WM_UNDO
      /// </summary>
      /// <param name="guiMgCtrl">ctrl to which this message is received</param>
      private void OnUndo(GuiMgControl guiMgCtrl)
      {
         var rtEvt = new RunTimeEvent((MgControl)guiMgCtrl, ((MgControl)guiMgCtrl).getDisplayLine(false), true);
         rtEvt.setInternal(InternalInterface.MG_ACT_EDT_UNDO);
         ClientManager.Instance.EventsManager.addToTail(rtEvt);
      }


   }
}
