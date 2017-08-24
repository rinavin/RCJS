using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.remote;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.management;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using util.com.magicsoftware.util;
using DataView = com.magicsoftware.richclient.data.DataView;
using Field = com.magicsoftware.richclient.data.Field;
using Record = com.magicsoftware.richclient.data.Record;
using System.Web.Script.Serialization;

namespace com.magicsoftware.richclient.gui
{
   /// <summary>
   ///   data for <form>...</form>
   /// </summary>
   internal class MgForm : MgFormBase
   {
      private const int TIME_LIMIT = 50; //time limit for one transfer operation (in milli seconds) 

      internal bool IsMovingInView { get; private set; }
      internal int PrevDisplayLine { get; set; } //display identifier of prev line

      private MgControl _ctrlOrderHead; // {tabbing order
      private MgControl _ctrlOrderTail; // {

      internal bool IgnoreFirstRecordCycle { get; set; }
      internal bool MovedToFirstControl { get; set; } //true when move to first control should be executed on the form

      private bool _wideActBegNxtLineWasEnabled; // before the wide, need to save if action MG_ACT_EDT_BEGNXTLINE

      private char _recomputeTabOrder = (char)0; // If 'Y' this ownerTask has to rcmp tabbing
      private bool _suffixDone;

      private List<int> hiddenControlsIsnsList = null;
      public bool InitializationFinished { get; set; }


      internal bool InRestore
      {
         get
         {
            return _inRestore;
         }
      }

      /// <summary>
      /// Get the task dataview, if the task exist
      /// </summary>
      /// <returns>the task dataview</returns>
      private DataView GetDataview()
      {
         DataView dv = null;
         if (_task != null)
            dv = (DataView)_task.DataView;
         return (dv);
      }

      /// <summary>
      ///   move one page/row up/down or to the beginning/end of table
      /// </summary>
      /// <param name = "unit">to move into</param>
      /// <param name = "Direction">of moving</param>
      internal void moveInView(char unit, char direction)
      {
         moveInView(unit, direction, true);
      }

      /// <summary>
      ///   move one page/row up/down or to the beginning/end of table
      /// </summary>
      /// <param name = "unit">to move into</param>
      /// <param name = "Direction">of moving</param>
      /// <param name = "returnToCtrl">should the cursor return to the last parked control</param>
      private void moveInView(char unit, char direction, bool returnToCtrl)
      {
         int oldRecId = Int32.MinValue;
         MgControl lastParkedCtrl;
         Record currRec = (Record)GetDataview().getCurrRec();
         char oldTaskMode = ' ';
         bool oldMoveByTab = ClientManager.Instance.setMoveByTab(false);
         bool returnToVisibleLine = false;
         bool recordOutOfView = false;
         int visibleLine = 0;

         try
         {
            IsMovingInView = true;
            int oldDisplayLine = DisplayLine;
            if (_mgTree != null)
            {
               if (!moveInTree(unit, direction))
                  return;
            }
            // table movements (first / last record)
            else if (unit == Constants.MOVE_UNIT_TABLE)
            {
               try
               {
                  // BEGINING OF TABLE
                  if (direction == Constants.MOVE_DIRECTION_BEGIN)
                  {
                     GetDataview().setCurrRecByIdx(Constants.MG_DATAVIEW_FIRST_RECORD, true, false, true,
                                        DataView.SET_DISPLAYLINE_BY_DV);
                     // check if there is a table in the form
                     if (isLineMode())
                     {
                        GetDataview().setTopRecIdx(0);
                        setCurrRowByDisplayLine(0, false, false);                        
                     }
                  }
                  // END OF TABLE
                  else
                  {
                     GetDataview().setCurrRecByIdx(Constants.MG_DATAVIEW_LAST_RECORD, true, false, true,
                                        DataView.SET_DISPLAYLINE_BY_DV);
                     // check if there is a table in the form
                     if (isLineMode())
                     {
                        GetDataview().setTopRecIdx(GetDataview().getCurrRecIdx() - _rowsInPage + 1);
                        if (GetDataview().getTopRecIdx() >= 0)
                           setCurrRowByDisplayLine(GetDataview().getCurrRecIdx(), false, false);
                        else
                        {
                           GetDataview().setTopRecIdx(0);
                           setCurrRowByDisplayLine(GetDataview().getCurrRecIdx(), false, false);
                        }
                     }
                  }
                  updateDisplayLineByDV();

                  RefreshDisplay(Constants.TASK_REFRESH_FORM);
               }
               catch (RecordOutOfDataViewException ex)
               {
                  Logger.Instance.WriteExceptionToLog(ex);
               }
            }
            // page and row movements
            else
            {
               getTopIndexFromGUI();
               // calculate the amount of records to skip
               int size;
               if (unit == Constants.MOVE_UNIT_PAGE && isLineMode())
               {
                  switch (direction)
                  {
                     case Constants.MOVE_DIRECTION_BEGIN:
                        size = GetDataview().getCurrRecIdx() - GetDataview().getTopRecIdx();
                        break;
                     case Constants.MOVE_DIRECTION_END:
                        int last = GetDataview().getTopRecIdx() + _rowsInPage - 1;
                        last = Math.Min(last, GetDataview().getSize() - 1);
                        size = last - GetDataview().getCurrRecIdx();
                        break;
                     default:
                        returnToVisibleLine = true;
                        size = _rowsInPage;
                        break;
                  }
               }
               else
                  size = 1;

               size = (direction == Constants.MOVE_DIRECTION_PREV ||
                       direction == Constants.MOVE_DIRECTION_BEGIN
                       ? -size
                       : size);

               if (isLineMode())
               {
                  visibleLine = getVisibleLine();
                  if (visibleLine < 0)
                  {
                     visibleLine = 0;
                     recordOutOfView = true;
                  }
                  if (visibleLine > _rowsInPage + 1)
                  {
                     visibleLine = _rowsInPage;
                     recordOutOfView = true;
                  }
                  // scrolling the table one page or one row up or down
                  if (unit == Constants.MOVE_UNIT_PAGE &&
                      (direction == Constants.MOVE_DIRECTION_NEXT ||
                       direction == Constants.MOVE_DIRECTION_PREV) ||
                      unit == Constants.MOVE_UNIT_ROW &&
                      (direction == Constants.MOVE_DIRECTION_NEXT && visibleLine == _rowsInPage - 1 ||
                       direction == Constants.MOVE_DIRECTION_PREV && visibleLine == 0))
                  {
                     // if we move to the previous row but we are at the first row of the table and data view
                     // then do nothing
                     if (direction == Constants.MOVE_DIRECTION_PREV &&
                         (unit == Constants.MOVE_UNIT_ROW || unit == Constants.MOVE_UNIT_PAGE) &&
                         visibleLine == 0 && GetDataview().getCurrRecIdx() == 0 && GetDataview().IncludesFirst())
                        return;

                     // if we move to the next row but we are at the last row of the table and the data view
                     // then do nothing
                     if (direction == Constants.MOVE_DIRECTION_NEXT && unit == Constants.MOVE_UNIT_PAGE &&
                         visibleLine == getLastValidRow() && GetDataview().getCurrRecIdx() == GetDataview().getSize() - 1 && GetDataview().IncludesLast())
                        return;

                     oldRecId = GetDataview().getCurrRec().getId();
                     GetDataview().setTopRecIdx(GetDataview().getTopRecIdx() + size);
                     ((data.DataView)GetDataview()).setTopRecIdxModified(true);
                     try
                     {
                        _suffixDone = false;

                        setCurrRowByDisplayLine(GetDataview().getCurrRecIdx() + size, true, false);
                        ((data.DataView)GetDataview()).setTopRecIdxModified(false);
                        RefreshDisplay(Constants.TASK_REFRESH_FORM);
                     }
                     catch (RecordOutOfDataViewException ex)
                     {
                        ((data.DataView)GetDataview()).setTopRecIdxModified(false);
                        if (ClientManager.Instance.EventsManager.GetStopExecutionFlag())
                        {
                           ((data.DataView)GetDataview()).restoreTopRecIdx();
                           restoreOldDisplayLine(oldDisplayLine);
                           return;
                        }
                        if (GetDataview().getTopRecIdx() < 0)
                        {
                           GetDataview().setTopRecIdx(0);
                           try
                           {
                              setCurrRowByDisplayLine(0, true, false);
                           }
                           catch (Exception)
                           {
                              // QCR #995496: when the record suffix failed go back to the
                              // original row
                              if (ClientManager.Instance.EventsManager.GetStopExecutionFlag())
                              {
                                 ((data.DataView)GetDataview()).restoreTopRecIdx();
                                 restoreOldDisplayLine(oldDisplayLine);
                                 return;
                              }
                           }
                           RefreshDisplay(Constants.TASK_REFRESH_FORM);
                        }
                        else if (unit != Constants.MOVE_UNIT_ROW && ((data.DataView)GetDataview()).recExists(GetDataview().getTopRecIdx()))
                        {
                           // QCR #992101: if we reached this point it means that the rec suffix
                           // wasn't executed so we must handle it before we go to a new row
                           int newRecId = GetDataview().getCurrRec().getId();
                           if (newRecId == oldRecId && !_suffixDone)
                           // QCR 758639
                           {
                              ClientManager.Instance.EventsManager.handleInternalEvent(_task, InternalInterface.MG_ACT_REC_SUFFIX);
                              if (ClientManager.Instance.EventsManager.GetStopExecutionFlag())
                              {
                                 ((data.DataView)GetDataview()).restoreTopRecIdx();
                                 restoreOldDisplayLine(oldDisplayLine);
                                 return;
                              }
                              try
                              {
                                 RefreshDisplay(Constants.TASK_REFRESH_FORM);
                                 setCurrRowByDisplayLine(GetDataview().getTopRecIdx() + getLastValidRow(), false, true);
                              }
                              catch (Exception)
                              {
                              }
                           }
                        }
                        else
                        {
                           ((data.DataView)GetDataview()).restoreTopRecIdx();
                           restoreOldDisplayLine(oldDisplayLine);
                           if (unit == Constants.MOVE_UNIT_ROW && direction == Constants.MOVE_DIRECTION_NEXT)
                           {
                              // non interactive, end of data means end of ownerTask, unless we are in create mode.
                              if (!_task.IsInteractive && _task.getMode() != Constants.TASK_MODE_CREATE)
                                 ((Task)_task).setExecEndTask();
                              else
                                 ClientManager.Instance.EventsManager.handleInternalEvent(_task, InternalInterface.MG_ACT_CRELINE);
                           }
                           return;
                        }

                        Logger.Instance.WriteExceptionToLog(ex);
                     }
                  }
                  // moving the marking one row up or down
                  else
                  {
                     try
                     {
                        oldTaskMode = _task.getMode();

                        oldRecId = GetDataview().getCurrRec().getId();
                        setCurrRowByDisplayLine(oldDisplayLine + size, true, false);

                        // if the record was deleted in Run Time by Force Delete for example
                        if (!((data.DataView)GetDataview()).recExistsById(oldRecId))
                        {
                           if (size > 0 || oldTaskMode != Constants.TASK_MODE_CREATE)
                              if (size != -1 || ((data.DataView)GetDataview()).recExists(oldDisplayLine))
                                 // QCR #293668
                                 setCurrRowByDisplayLine(oldDisplayLine, false, false);
                              else
                                 Logger.Instance.WriteDevToLog(string.Format("skipped setcurrRow for row {0}", oldDisplayLine));
                           RefreshDisplay(Constants.TASK_REFRESH_FORM);
                        }
                        else if (ClientManager.Instance.EventsManager.GetStopExecutionFlag())
                        {
                           // performance improvement: if everything is OK, curr rec will later be refreshed in
                           // REC_PREFIX
                           // thus, refresh only in case of error.
                           RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);
                        }
                     }
                     catch (RecordOutOfDataViewException ex)
                     {
                        // Sometimes the error is because the old-rec was removed (cancelEdit'ed), so refresh form.
                        if (((data.DataView)GetDataview()).recExistsById(oldRecId))
                        {
                           restoreOldDisplayLine(oldDisplayLine);

                           // QCR #775309: if leaving the new created record failed then restore
                           // all the attributes of the a new record so the next action would
                           // "know" that it is still a new record.
                           // Defect # 81627 : If task is having a subform, then while moving to subform, newly created record 
                           // is synced and it is inserted in _modifiedTab as 'insert' record. So, any request to server inserts 
                           // record to server. Even if we dont go to server, it is there in _modifiedTab as new record. Inserting 
                           // here already synced record causes insertion of record twice. So, If current record is not synced then only insert.

                           if (!currRec.Synced && oldTaskMode == Constants.TASK_MODE_CREATE)
                           {
                              _task.setMode(Constants.TASK_MODE_CREATE);
                              currRec.clearMode();
                              currRec.setMode(DataModificationTypes.Insert);
                              currRec.setNewRec();
                           }
                           // QCR #980528: Refresh the current row after cancel edit to reflect
                           // the restored values
                           RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);
                           if (_task.getLastParkedCtrl() != null)
                              Manager.SetSelect(_task.getLastParkedCtrl());
                        }
                        else
                        {
                           if (oldTaskMode == Constants.TASK_MODE_CREATE && oldDisplayLine > 0)
                              restoreOldDisplayLine(oldDisplayLine - 1);
                           RefreshDisplay(Constants.TASK_REFRESH_FORM);
                        }

                        if (size <= 0 || ClientManager.Instance.EventsManager.GetStopExecutionFlag())
                           Logger.Instance.WriteExceptionToLog(ex);
                        else
                        {
                           // non interactive, end of data means end of ownerTask, unless we are in create mode.
                           if (!_task.IsInteractive && _task.getMode() != Constants.TASK_MODE_CREATE)
                           {
                              ((Task)_task).setExecEndTask();
                              return;
                           }
                           else
                           {
                              if (_task.ActionManager.isEnabled(InternalInterface.MG_ACT_CRELINE))
                                 ClientManager.Instance.EventsManager.handleInternalEvent(_task, InternalInterface.MG_ACT_CRELINE);
                              else
                              {
                                 // fixed bug #:941763, while create isn't allowed need to do suffix and prefix of the current record
                                 bool doRecordSuffix = true;
                                 if (!ClientManager.Instance.EventsManager.DoTaskLevelRecordSuffix((Task)_task, ref doRecordSuffix))
                                    ClientManager.Instance.EventsManager.DoTaskLevelRecordPrefix((Task)_task, this, false);
                              }
                           }
                        }
                        if (_task.getLastParkedCtrl() != null)
                           Manager.SetSelect(_task.getLastParkedCtrl()); // QCR 248681

                        return;
                     }
                  }
               }
               // if the form does not have a table, move to one row in the chosen Direction
               else
               {
                  try
                  {
                     GetDataview().setCurrRecByIdx(GetDataview().getCurrRecIdx() + size, true, false, true, DataView.SET_DISPLAYLINE_BY_DV);
                     RefreshDisplay(Constants.TASK_REFRESH_FORM);
                  }
                  catch (RecordOutOfDataViewException ex)
                  {
                     if (size > 0)
                     {
                        // non interactive, end of data means end of ownerTask, unless we are in create mode.
                        if (!_task.IsInteractive && _task.getMode() != Constants.TASK_MODE_CREATE)
                           ((Task)_task).setExecEndTask();
                        else
                           ClientManager.Instance.EventsManager.handleInternalEvent(_task, InternalInterface.MG_ACT_CRELINE);
                     }
                     else
                        Logger.Instance.WriteExceptionToLog(ex);
                     return;
                  }
                  catch (ServerError e)
                  {
                     ((Task)_task).stop();
                     ((Task)_task).abort();
                     ClientManager.Instance.ProcessAbortingError(e);
                  }
                  catch (Exception ex)
                  {
                     Logger.Instance.WriteExceptionToLog(ex.Message);
                     return;
                  }
               }
            }

            if (returnToVisibleLine)
            {
               if (recordOutOfView)
               {
                  if (visibleLine == 0)
                     GetDataview().setTopRecIdx(GetDataview().getCurrRecIdx());
                  else
                     GetDataview().setTopRecIdx(GetDataview().getCurrRecIdx() - _rowsInPage);
                  SetTableTopIndex();
               }
               else
               {
                  SetTableTopIndex();
                  setCurrRowByDisplayLine(GetDataview().getTopRecIdx() + visibleLine, false, true);
               }
            }
            else
               SetTableTopIndex();

            ClientManager.Instance.EventsManager.handleInternalEvent(_task, InternalInterface.MG_ACT_REC_PREFIX);

            if (returnToCtrl)
            {
               lastParkedCtrl = (MgControl)_task.getLastParkedCtrl();
               if (lastParkedCtrl != null)
               {
                  bool cursorMoved = lastParkedCtrl.invoke();
                  if (!cursorMoved)
                  {
                     cursorMoved = moveInRow(null, Constants.MOVE_DIRECTION_NEXT);
                     if (!cursorMoved)
                        ClientManager.Instance.EventsManager.HandleNonParkableControls(_task);
                  }
               }
            }
         }
         catch (RecordOutOfDataViewException e)
         {
            // TODO Auto-generated catch block
            Misc.WriteStackTrace(e, Console.Error);
         }
         finally
         {
            ClientManager.Instance.setMoveByTab(oldMoveByTab);

            SelectRow();
            IsMovingInView = false;
         }
      }

      /// <summary>
      ///   move to appropriate line in tree depending unit and Direction
      /// </summary>
      /// <param name = "unit"></param>
      /// <param name = "Direction"></param>
      /// <returns> false if we fail to move to desired line</returns>
      private bool moveInTree(char unit, char direction)
      {
         int newLine = _mgTree.calculateLine(direction, unit, 0);
         try
         {
            setCurrRowByDisplayLine(newLine, true, false);
            RefreshDisplay(Constants.TASK_REFRESH_FORM);
         }
         catch (RecordOutOfDataViewException e)
         {
            if (e.noRecord())
               return ((MgTree)_mgTree).handleNoRecordException(newLine);
            return false;
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(ex.Message);
            return false;
         }
         return true;
      }

      /// <summary>
      ///   move the focus in the Row and returns true if the move succeeded
      /// </summary>
      /// <param name = "Direction">of moving: MOVE_DIRECTION_BEGIN, MOVE_DIRECTION_END, MOVE_DIRECTION_PREV, MOVE_DIRECTION_NEXT</param>
      internal bool moveInRow(MgControl ctrl, char direction)
      {
         MgControl lastParkedControl, clickedControl;
         MgControl nextCtrl, prevCtrl;
         bool cursorMoved = false;

         _task.setLastMoveInRowDirection(direction);

         lastParkedControl = (MgControl)_task.getLastParkedCtrl();
         clickedControl = (MgControl)_task.getClickedControl();
         if (lastParkedControl != null)
         {
            ClientManager.Instance.EventsManager.handleInternalEvent(lastParkedControl, InternalInterface.MG_ACT_CTRL_SUFFIX);
            if (ClientManager.Instance.EventsManager.GetStopExecutionFlag())
            {
               _task.setLastMoveInRowDirection(Constants.MOVE_DIRECTION_NONE);
               return cursorMoved;
            }
         }
         else
            lastParkedControl = (MgControl)ClientManager.Instance.EventsManager.getStopExecutionCtrl();

         switch (direction)
         {
            case Constants.MOVE_DIRECTION_BEGIN:
               cursorMoved = moveToFirstParkableCtrl(ClientManager.Instance.MoveByTab);
               break;

            case Constants.MOVE_DIRECTION_END:
               cursorMoved = moveToLastParkbleCtrl(lastParkedControl);
               break;

            case Constants.MOVE_DIRECTION_PREV:
               if (ctrl != null && ctrl == clickedControl &&
                   (!ctrl.IsParkable(false) || !ctrl.allowedParkRelatedToDirection()))
               {
                  prevCtrl = getPrevControlToParkAccordingToTabbingCycle(ctrl);

                  if (prevCtrl == null)
                     ctrl = getNextControlToParkAccordingToTabbingCycle(ctrl);
                  else
                     ctrl = prevCtrl;

                  if (ctrl != null)
                     cursorMoved = ctrl.invoke(ClientManager.Instance.MoveByTab, true);
                  break;
               }

               if (ctrl == null)
                  ctrl = lastParkedControl;
               if (ctrl == null)
                  break;

               cursorMoved =
               (moveToNextControlAccordingToTabbingCycle(ctrl, lastParkedControl, cursorMoved,
                                                         Constants.MOVE_DIRECTION_PREV));
               break;

            case Constants.MOVE_DIRECTION_NEXT:
               if (ctrl != null && ctrl == clickedControl &&
                   (!ctrl.IsParkable(false) || !ctrl.allowedParkRelatedToDirection()))
               {
                  nextCtrl = getNextControlToParkAccordingToTabbingCycle(ctrl);

                  if (nextCtrl == null)
                     ctrl = getPrevControlToParkAccordingToTabbingCycle(ctrl);
                  else
                     ctrl = nextCtrl;

                  if (ctrl != null)
                     cursorMoved = ctrl.invoke(ClientManager.Instance.MoveByTab, true);
                  break;
               }

               if (ctrl == null)
                  ctrl = lastParkedControl;

               if (ctrl == null)
                  break;

               cursorMoved =
               (moveToNextControlAccordingToTabbingCycle(ctrl, lastParkedControl, cursorMoved,
                                                         Constants.MOVE_DIRECTION_NEXT));
               break;

            default:
               Logger.Instance.WriteExceptionToLog(string.Format("in Form.moveInRow() illigal move direction: {0}", direction));
               _task.setLastMoveInRowDirection(Constants.MOVE_DIRECTION_NONE);
               return cursorMoved;
         }
         if (!cursorMoved && lastParkedControl != null)
         {
            // we might have parked here, if Direction of all control have failed. So, while returning back to this ctrl allowdir must be ignored.
            lastParkedControl.IgnoreDirectionWhileParking = true;
            cursorMoved = lastParkedControl.invoke();
            lastParkedControl.IgnoreDirectionWhileParking = false;
         }

         _task.setLastMoveInRowDirection(Constants.MOVE_DIRECTION_NONE);

         return cursorMoved;
      }

      /// <summary>
      ///  move to next control according to the ownerTask property Tabbing Cycle.
      /// </summary>
      /// <param name="currentCtrl"></param>
      /// <param name="lastParkedControl"></param>
      /// <param name="cursorMoved"></param>
      /// <param name="Direction"></param>
      /// <returns></returns>
      private bool moveToNextControlAccordingToTabbingCycle(MgControl currentCtrl, MgControl lastParkedControl,
                                                            bool cursorMoved, char direction)
      {
         bool forceMoveToParent = checkForceMoveToParent(lastParkedControl);
         bool moveToParentTask = isTabbingCycleMoveToParentTask();
         bool remainInCurrentRecord = isTabbingCycleRemainInCurrentRecord();

         if (direction == Constants.MOVE_DIRECTION_NEXT)
            currentCtrl = getNextControlToParkAccordingToTabbingCycle(currentCtrl);
         else if (direction == Constants.MOVE_DIRECTION_PREV)
            currentCtrl = getPrevControlToParkAccordingToTabbingCycle(currentCtrl);
         else
            Debug.Assert(false);

         // while the lookup control and the last parked control do not overlap
         while (currentCtrl != lastParkedControl)
         {
            // check for the end of the controls list
            if (currentCtrl == null)
            {
               if (moveToParentTask || forceMoveToParent || remainInCurrentRecord)
               {
                  // for sub form while tabbingCycle="Move to parent ownerTask" we need to call this method by recursively
                  currentCtrl = getFirstParkableCtrl();
                  if (moveToParentTask || forceMoveToParent)
                  {
                     // if we get to the last control and it is subform, then we need to back to the parent
                     if (isSubForm())
                     {
                        // call this method again for the parent recursively
                        cursorMoved = ((MgForm)getSubFormCtrl().getForm()).moveToNextControlAccordingToTabbingCycle(
                           (MgControl)getSubFormCtrl(),
                           lastParkedControl,
                           cursorMoved, direction);
                        // if we moved(succeeded) to control on the cycle control then we don't need to move again to other control
                        // that came by the recursively method. 
                        if (cursorMoved)
                           return cursorMoved;
                     }
                  }
                  // get the prev control to be park on it
                  if (currentCtrl != null)
                     currentCtrl = currentCtrl.getControlToFocus();
               }
               else //moveToNextRecord
               {
                  // MoveToNextRecord: we need to move to the next record in the ownerTask
                  ClientManager.Instance.EventsManager.handleInternalEvent(lastParkedControl.getForm().getTask(),
                                                            InternalInterface.MG_ACT_CTRL_SUFFIX);
                  if (!ClientManager.Instance.EventsManager.GetStopExecutionFlag())
                  {
                     if (direction == Constants.MOVE_DIRECTION_NEXT)
                     {
                        // execute CV from lastparked control's next ctrl to the last control
                        ClientManager.Instance.EventsManager.executeVerifyHandlersTillLastCtrl(lastParkedControl.getForm().getTask(), lastParkedControl);

                        if (!ClientManager.Instance.EventsManager.GetStopExecutionFlag())
                        {
                           // go to the beginning of the next row
                           moveInView(Constants.MOVE_UNIT_ROW, Constants.MOVE_DIRECTION_NEXT, false);
                           if (ClientManager.Instance.EventsManager.GetStopExecutionFlag())
                              return cursorMoved;

                           // QCR#483721 curserMoved Must be changed if a parkable control was found in the current
                           // row
                           // if ownerTask is in create mode then CP is executed from createline in moveinview called above.
                           if (_task.getMode() == Constants.TASK_MODE_CREATE)
                              cursorMoved = true;
                           else
                              cursorMoved = moveInRow(currentCtrl, Constants.MOVE_DIRECTION_BEGIN);
                        }
                     }
                     else
                     {
                        // Shift+Tab is not allowed from the first parkable control. Hence, it is reset to first
                        // parkable control.
                        currentCtrl = getFirstParkableCtrl();
                     }

                     if (ClientManager.Instance.EventsManager.GetStopExecutionFlag())
                        return cursorMoved;
                  }
                  break;
               }
            }
            bool checkSubFormTabIntoProperty = lastParkedControl != null &&
                                               !lastParkedControl.onTheSameSubFormControl(currentCtrl);
            if (currentCtrl != null)
               cursorMoved = currentCtrl.invoke(ClientManager.Instance.MoveByTab, checkSubFormTabIntoProperty);

            if (cursorMoved)
               break;

            //QCR #781204, if currentCtrl is null - there is no parkable controls
            if (currentCtrl == null)
               return false;

            // QCR 745527 when looking for the next parkable control in the row we must avoid loops
            if (direction == Constants.MOVE_DIRECTION_NEXT)
            {
               if (currentCtrl != lastParkedControl)
               {
                  currentCtrl = currentCtrl.getNextCtrl();
                  //when get to the end of the list (after pass all controls) we need to exit from the while.
                  if (currentCtrl == null)
                     break;
               }
            }
            else
            {
               if (currentCtrl != lastParkedControl)
               {
                  currentCtrl = currentCtrl.getPrevCtrl();
                  //when get to the first of the list (after pass all controls) we need to exit from the while.
                  if (currentCtrl == null)
                     break;
               }
            }
         }
         return cursorMoved;
      }

      /// <summary>
      /// </summary>
      /// <param name = "lastParkedControl"></param>
      /// <returns></returns>
      private bool checkForceMoveToParent(MgControl lastParkedControl)
      {
         bool forceMoveToParent = false;
         bool LastControlIsNotParkOnSubForm = (lastParkedControl != null && isSubForm() &&
                                               !lastParkedControl.IsParkable(true));
         if (LastControlIsNotParkOnSubForm)
         {
            bool remainInCurrentRecord = isTabbingCycleRemainInCurrentRecord();
            bool moveToNextRecord = isTabbingCycleMoveToNextRecord();
            if (remainInCurrentRecord || moveToNextRecord)
            {
               MgControl prevMgControl = getFirstParkableCtrl();
               if (prevMgControl == null)
                  forceMoveToParent = true;
            }
         }
         return forceMoveToParent;
      }

      /// <summary>
      ///   set the value of the current field to be the same as the value of this field in the previous record
      /// </summary>
      internal void ditto()
      {
         Field currFld = (Field)_task.getCurrField();
         if (currFld == null)
            return;
         string[] val = ((data.DataView)GetDataview()).getDitto(currFld);
         if (val != null)
         {
            // value[1] is the real value of the field
            // value[0] is a null flag ("1" means a null value)
            currFld.setValueAndStartRecompute(val[1], val[0].Equals("1"), true, true, false);
            currFld.updateDisplay();
         }
      }

      /// <summary>
      ///   add a record to the form and make it the current record
      /// </summary>
      internal void addRec(bool doSuffix, int parentLine, int prevLine)
      {
         int newLine = 1;
         bool activeTree = _mgTree != null && !_task.DataView.isEmptyDataview();
         int currLine = -1;

         if (!_task.DataView.isEmptyDataview())
            currLine = getVisibleLine();
         if (activeTree)
            ((MgTree)_mgTree).setNodeInCreation(parentLine, prevLine);
         int newRecIdx = ((data.DataView)GetDataview()).addRecord(doSuffix, false);
         if (activeTree)
            ((MgTree)_mgTree).removeNodeInCreation();
         if (newRecIdx > -1)
         {
            // forms with a table
            if (isLineMode())
            {
               // there are no records in the table so the new row is created after
               // setting the top record to 0 and the current row to 0
               int newCurrRow;
               if (GetDataview().getSize() == 0 || GetDataview().getTopRecIdx() == Int32.MinValue)
               {
                  GetDataview().setTopRecIdx(0);
                  newCurrRow = 0;
               }
               // the cursor is parking on the last row of the displayed table so the
               // new row is created after incrementing the top record index
               else if (currLine == _rowsInPage - 1)
               {
                  GetDataview().setTopRecIdx(GetDataview().getTopRecIdx() + 1);
                  newCurrRow = getVisibleLine();
               }
               // the new row is created without changing the top record
               else
                  newCurrRow = currLine + 1;

               if (_mgTree != null)
               {
                  //add new node to tree
                  if (!_task.DataView.isEmptyDataview())
                     newLine = DisplayLine;
                  else
                     _treeMgControl.updateArrays(newLine + 1);
               }
               else
               {
                  newLine = GetDataview().getTopRecIdx() + newCurrRow;

                  //Increment the TotalRecordsCount as record being added into the view.
                  if (isTableWithAbsoluteScrollbar())
                     GetDataview().TotalRecordsCount += 1;

                  // invalidates all records after newCurrRow
                  removeRecordsAfterIdx(GetDataview().getTopRecIdx() + newCurrRow);
               }

               try
               {
                  // invalidates all records after newCurrRow
                  if (!_task.DataView.isEmptyDataview())
                     _task.setMode(Constants.TASK_MODE_CREATE);
                  setCurrRowByDisplayLine(newLine, false, false);
               }
               catch (RecordOutOfDataViewException ex)
               {
                  // no exception should really be caught here
                  Logger.Instance.WriteExceptionToLog(string.Format("in Form.addRec() {0}", ex.Message));
               }
               if (activeTree)
               {
                  RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);

                  // create editor for new line
                  ClientManager.Instance.EventsManager.addInternalEvent(_treeMgControl, DisplayLine, InternalInterface.MG_ACT_TREE_RENAME_RT);
               }
               else
                  RefreshDisplay(Constants.TASK_REFRESH_TABLE);
            }
         }
      }

      /// <summary>
      ///   delete the current record
      /// </summary>
      internal void delCurrRec()
      {
         bool topTableRow = (isLineMode() && GetDataview().getTopRecIdx() == GetDataview().getCurrRecIdx());

         ((data.DataView)GetDataview()).removeCurrRec();

         if (GetDataview().isEmpty() && !((Task)_task).IsTryingToStop)
         {
            if (_task.DataView.isEmptyDataview())
            {
               // if (!hasTree())
               addRec(false, 0, 0);
            }
            else
            {
               if (_task.checkProp(PropInterface.PROP_TYPE_ALLOW_CREATE, true) && !hasTree())
               {
                  // change to create mode
                  if (_task.getMode() != Constants.TASK_MODE_CREATE)
                     _task.setMode(Constants.TASK_MODE_CREATE);

                  // MG_ACT_CRELINE may be disabled .i.e. PROP_TYPE_ALLOW_CREATE expression can be changed
                  ((Task)_task).enableCreateActs(true);
                  ClientManager.Instance.EventsManager.handleInternalEvent(_task, InternalInterface.MG_ACT_CRELINE);
               }
               // exit from the ownerTask
               else
                  ClientManager.Instance.EventsManager.handleInternalEvent(_task, InternalInterface.MG_ACT_EXIT);
            }
         }

         // the emptiness of the DV must be checked again because the "create line" may fail
         if (!GetDataview().isEmpty())
         {
            if (topTableRow)
               GetDataview().setTopRecIdx(GetDataview().getCurrRecIdx());

            if (isLineMode())
            {
               if (HasTable())
               {
                  //Decrement the TotalRecordsCount as record being deleted.
                  if (isTableWithAbsoluteScrollbar())
                     GetDataview().TotalRecordsCount -= 1;
                  
                  removeRecordsAfterIdx(GetDataview().getCurrRecIdx());
                  RefreshDisplay(Constants.TASK_REFRESH_FORM);
               }
               else
                  RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);
            }
            // screen mode
            else
            {
               try
               {
                  GetDataview().setCurrRecByIdx(GetDataview().getCurrRecIdx(), true, true, true, DataView.SET_DISPLAYLINE_BY_DV);
               }
               catch (RecordOutOfDataViewException)
               {
               }
               RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);
            }
         }
      }

      /// <summary>
      ///   cancel the editing of the current record and restore the old values of the fields
      /// </summary>
      /// <param name = "isActCancel">true if called for ACT_CANCEL, false otherwise</param>
      /// <param name = "isQuitEvent"></param>
      internal void cancelEdit(bool isActCancel, bool isQuitEvent)
      {
         char mode, newMode;
         int row = 0;
         int topRecIdx, currRecIdx;
         bool execRecPrefix = false;

         mode = _task.getMode();

         // We return to create mode only if there is no record to go to after the cancel action
         if (GetDataview().getSize() == 1 && mode == Constants.TASK_MODE_CREATE)
            newMode = Constants.TASK_MODE_CREATE;
         else
            newMode = Constants.TASK_MODE_MODIFY;

         /**********************************************************************/
         //997073 & 931021.
         //Expected behavior during cancel event, for a new record:
         //If the original ownerTask mode is not create, delete the new record and go back to the previous record.
         //If the original ownerTask mode is create, stay on the new record.

         //dv.cancelEdit() removes the new record under some condition.
         //So, in case of orgTaskMode=C, since we want to stay on the new record, we need to re-create a record.

         //But as said, dv.cancelEdit() removes the new record ONLY under some condition.
         //The condition is as follows:
         //The record is removed only if the dv size is greater than 1. So, if the dv size is 1, dv.cancelEdit() 
         //does not remove the record and hence in this case, form.cancelEdit() should not create a new record.
         //For this reason, the signature of dv.cancelEdit() is changed to indicate whether the record was removed or not.

         //This is one part.

         //Now, after cancelEdit, RP has to be executed. This was done in EventsManager.commonHandler()->case MG_ACT_CANCEL.
         //But what happens is, if form.cancelEdit() created a new line, RP was already executed from there. And from 
         //EventsManager.commonHandler(), RP is executed for the second time.
         //So, moved the code of executing RP from EventsManager.commonHandler()->case MG_ACT_CANCEL to form.cancelEdit() 
         //and that will be done only if the create line was not executed.

         //Now, we have one more issue.

         //Until now, the RP was executed after form.cancelEdit().
         //So, when the code is moved to form.cancelEdit(), it should be the last thing to be executed.
         /**********************************************************************/
         bool recRemoved = ((data.DataView)GetDataview()).cancelEdit(false, false);
         ((data.DataView)GetDataview()).setPrevCurrRec(null);
         if ((GetDataview().isEmpty() || (isActCancel && recRemoved && ((Task)_task).getOriginalTaskMode() == Constants.TASK_MODE_CREATE)) &&
             !((Task)_task).IsTryingToStop)
         {
            // Set Cancel indication on the task for to not perform control value validation in the Create line action.
            bool orgCancelWasRaised = ((Task)_task).cancelWasRaised();
            ((Task)_task).setCancelWasRaised(isActCancel);
            ClientManager.Instance.EventsManager.handleInternalEvent(_task, InternalInterface.MG_ACT_CRELINE);
            ((Task)_task).setCancelWasRaised(orgCancelWasRaised);
         }
         else if (isActCancel)
            execRecPrefix = true;

         // the emptiness of the DV must be checked again because the "create line" may fail
         if (!GetDataview().isEmpty())
         {
            if (isLineMode())
            {
               try
               {
                  if (HasTable())
                  {
                     row = getVisibleLine();
                     if (row < 0 || row >= _rowsInPage)
                     {
                        topRecIdx = GetDataview().getTopRecIdx();
                        currRecIdx = GetDataview().getCurrRecIdx();
                        if (topRecIdx > currRecIdx)
                           GetDataview().setTopRecIdx(currRecIdx);
                        row = 0;
                     }
                     setCurrRowByDisplayLine(GetDataview().getTopRecIdx() + row, false, true);
                  }
               }
               catch (RecordOutOfDataViewException ex)
               {
                  // no exception should really be caught here.
                  if (row != 0)
                     Logger.Instance.WriteExceptionToLog(string.Format("in Form.cancelEdit() {0}", ex.Message));
               }
               if (HasTable())
               {
                  removeRecordsAfterIdx(GetDataview().getCurrRecIdx());

                  //Decrement the TotalRecordsCount as record being deleted.
                  if (isTableWithAbsoluteScrollbar() && recRemoved)
                     GetDataview().TotalRecordsCount -= 1;
               }
            }
            // screen mode
            else
            {
               try
               {
                  GetDataview().setCurrRecByIdx(GetDataview().getCurrRecIdx(), false, true, true, DataView.SET_DISPLAYLINE_BY_DV);
               }
               catch (RecordOutOfDataViewException)
               {
                  Logger.Instance.WriteExceptionToLog("in Form.cancelEdit() error in Screen mode for Current Record");
               }
            }
            _task.setMode(newMode);

            //TODO: Kaushal. Check if we can move this condition in RefreshDisplay() itself.
            if (!((Task)_task).InEndTask)
               RefreshDisplay(Constants.TASK_REFRESH_FORM);

            //TODO: Kaushal. Check if we can move this condition in RefreshDisplay() itself.
            if (!((Task)_task).InEndTask)
               RefreshDisplay(Constants.TASK_REFRESH_CURR_REC);
         }

         if (execRecPrefix)
         {
            MgControl lastParkedCtrl;

            // QCR #925646, #454167: exit to the ownerTask level in order to skip control suffix
            // and record suffix of the canceled new record and make sure that the
            // next record perfix would be executed
            _task.setLevel(Constants.TASK_LEVEL_TASK);

            // we rollback a parent ownerTask while focused on subform/ownerTask. We do not want the ctrl on the subform to do ctrl suffix.
            lastParkedCtrl = GUIManager.getLastFocusedControl();
            if (lastParkedCtrl != null)
            {
               if (((Task)lastParkedCtrl.getForm().getTask()).pathContains((Task)_task))
                  for (var parent = (Task)lastParkedCtrl.getForm().getTask();
                       parent != null && parent != _task;
                       parent = (Task)parent.getParent())
                  {
                     parent.setLevel(Constants.TASK_LEVEL_TASK);
                     ((DataView)parent.DataView).getCurrRec().resetModified();
                  }
               else
                  lastParkedCtrl.getForm().getTask().setLevel(Constants.TASK_LEVEL_TASK);
            }

            if (!isQuitEvent)
            {
               ClientManager.Instance.EventsManager.handleInternalEvent(_task, InternalInterface.MG_ACT_REC_PREFIX);
               _task.setCurrVerifyCtrl(null);
               ((MgForm)_task.getForm()).moveInRow(null, Constants.MOVE_DIRECTION_BEGIN);
            }
         }
      }

      /// <summary>
      ///   return a url of the help for this form
      /// </summary>
      internal Property getHelpUrlProp()
      {
         if (_propTab == null)
            return null;
         Property helpProp = _propTab.getPropById(PropInterface.PROP_TYPE_HELP_SCR);
         if (helpProp == null)
         {
            if (isSubForm())
            {
               var parentTask = ((Task)getTask()).getParent();
               if (parentTask != null && parentTask.getForm() != null)
                  helpProp = ((MgForm)parentTask.getForm()).getHelpUrlProp();
            }
         }
         return helpProp;
      }

      /// <summary>
      ///   return a url of the help for this form
      /// </summary>
      internal String getHelpUrl()
      {
         Property helpProp;

         if (_propTab == null)
            return null;
         helpProp = getHelpUrlProp();
         if (helpProp == null)
            return null;
         return helpProp.getValue();
      }

      /// <summary>
      ///   scan all controls in the form. Every control which has a property that is an expression will be
      ///   refreshed. This is done to ensure the control's display is synched with the expressin's value
      /// </summary>
      internal void refreshOnExpressions()
      {
         for (int i = 0; i < CtrlTab.getSize(); i++)
         {
            MgControl mgControl = CtrlTab.getCtrl(i) as MgControl;
            if (mgControl != null)
               mgControl.refreshOnExpression();
            else
               Debug.Assert(CtrlTab.getCtrl(i) is MgStatusBar);
         }

         // refresh the form properties
         refreshPropsOnExpression();
      }

      /// <summary>
      ///   Call getFirstParkableCtrl to search also in subform.
      /// </summary>
      /// <returns></returns>
      internal MgControl getFirstParkableCtrl()
      {
         return getFirstParkableCtrl(true);
      }

      /// <summary>
      ///   Return the first parkable control. If the currCtrl is a subform then return the first parkable child
      ///   control of the subform (recursively).
      ///   if IncludingSubForm is false, do not look inside subform for the ctrl.
      /// </summary>
      internal MgControl getFirstParkableCtrl(bool IncludingSubForm)
      {
         MgControl currCtrl = _ctrlOrderHead;
         // QCR #316850. For subforms always enter into the loop for to find it's really first control.
         while (currCtrl != null && (currCtrl.isSubform() || !currCtrl.IsParkable(false)))
         {
            if (IncludingSubForm)
            {
               if (currCtrl.isSubform() && currCtrl.getSubformTask() != null)
                  return (((MgForm)currCtrl.getSubformTask().getForm()).getFirstParkableCtrl());
            }
            currCtrl = currCtrl.getNextCtrl();
         }
         return currCtrl;
      }

      /// <summary>
      ///   return the next\prev control on which the cursor can park after\before the current control
      /// </summary>
      /// <param name = "currCtrl">the current control</param>
      /// <param name = "Next">True if get the next parkable control, otherwise return the prev parkable control</param>
      private MgControl getNextCtrlByDirection(MgControl currCtrl, bool Next)
      {
         object parentControl = null;
         MgControl ctrl = currCtrl;
         if (currCtrl != null)
            parentControl = currCtrl.getParent();
         bool skippCtrl = false;
         do
         {
            skippCtrl = false;
            if (Next)
               currCtrl = currCtrl.getNextCtrl();
            else
               currCtrl = currCtrl.getPrevCtrl();

            if (currCtrl != null && parentControl == currCtrl.getParent())
            {
               // It is possible that .Net control will not have any field attached.
               if (currCtrl.IsDotNetControl() && currCtrl.getField() == null)
               {
                  //do nothing. skippCtrl is already false.
               }
               // if controls have same data attached, it must be skipped
               else if (ctrl.getField() == currCtrl.getField())
                  skippCtrl = true;
            }
         } while (skippCtrl ||
                  (currCtrl != null &&
                   (!currCtrl.IsParkable(ClientManager.Instance.MoveByTab) ||
                    !currCtrl.allowedParkRelatedToDirection())));

         //fixed bug #:800951, 933525, 763887, 978023, 998172 ,get the focus control
         return (currCtrl != null
                 ? currCtrl.getControlToFocus()
                 : currCtrl);
      }

      /// <summary>
      ///   return the next control on which the cursor can park after the current control
      /// </summary>
      /// <param name = "currCtrl">the current control</param>
      internal MgControl getNextParkableCtrl(MgControl currCtrl)
      {
         return (getNextCtrlByDirection(currCtrl, true));
      }

      /// <summary>
      ///   return the prev control on which the cursor can park before the current control
      /// </summary>
      /// <param name = "currCtrl">the current control</param>
      internal MgControl getPrevParkableCtrl(MgControl currCtrl)
      {
         return (getNextCtrlByDirection(currCtrl, false));
      }

      /// <summary>
      ///   return the non subform control on the form which the cursor can park after
      ///   the current control in 'Direction'
      /// </summary>
      /// <param name = "currCtrl"></param>
      /// <param name = "Direction"></param>
      /// <returns></returns>
      internal MgControl getNextParkableNonSubformCtrl(MgControl currCtrl, Direction direction)
      {
         do
         {
            currCtrl = getNextCtrlIgnoreSubforms(currCtrl, direction);
         } while (currCtrl != null && !currCtrl.IsParkable(false));

         return currCtrl;
      }

      /// <summary>
      /// </summary>
      /// <param name = "windowType"></param>
      /// <returns></returns>
      internal WindowType getInhertitedWindowType(WindowType windowType)
      {
         if (windowType != WindowType.Modal)
         {
            Task parentTask = (getTask() != null
                                  ? ((Task)getTask()).getParent()
                                  : null);

            if (parentTask != null && parentTask.getForm() != null)
            {
               Object parentObject = (parentTask.IsSubForm && parentTask.getForm() != null
                                      ? parentTask.getForm().getSubFormCtrl()
                                      : (Object)parentTask.getForm());
               if (parentObject is MgControl)
                  parentObject = ((MgControl)(parentObject)).getForm();

               WindowType parentWindowType = ((MgForm)parentObject).ConcreteWindowType;
               // if the parent of the task is Modal\floating\tool, then his child will be also the same window type.
               if (parentWindowType == WindowType.Modal)
                  windowType = parentWindowType;
            }
         }

         return windowType;
      }

      /// <summary>
      ///   combines the tabbing order of this form with its subforms
      /// </summary>
      /// <param name = "parentSubformCtrl">a reference to the subform control in the parent task of this subform ownerTask</param>
      internal void combineTabbingOrder(MgControl parentSubformCtrl)
      {
         MgControlBase ctrl;
         MgControl prevCtrl, nextCtrl;
         MgForm subform;
         bool dynamicRecompute = false;
         Property prop;
         int i;

         // if tabbing order is static, and it was already calculated, no need to re-calculate it
         if (_recomputeTabOrder != 'N')
         {
            if (_ctrlOrderHead == null)
               buildTabbingOrder();

            // if this task is a subform then combine its order list into its parent order list
            if (_task.IsSubForm)
            {
               // no control in form in subform, remove this subfrom from tabbing order.
               if (_ctrlOrderHead == null && _ctrlOrderTail == null)
                  removeSubformFromTabbingOrder(parentSubformCtrl);
               else
               {
                  prevCtrl = parentSubformCtrl.getPrevCtrl();
                  nextCtrl = parentSubformCtrl.getNextCtrl();

                  if (_ctrlOrderHead != null)
                     _ctrlOrderHead.setPrevCtrl(prevCtrl);
                  if (_ctrlOrderTail != null)
                     _ctrlOrderTail.setNextCtrl(nextCtrl);
                  if (prevCtrl != null)
                     prevCtrl.setNextCtrl(_ctrlOrderHead);
                  if (nextCtrl != null)
                     nextCtrl.setPrevCtrl(_ctrlOrderTail);
               }
            }

            // scan this form controls and recursively combine their tabbing order list
            // into the unified list
            for (i = 0; i < CtrlTab.getSize(); i++)
            {
               ctrl = CtrlTab.getCtrl(i);
               if (ctrl.isSubform())
               {
                  if (ctrl.GetSubformMgForm() != null)
                  {
                     subform = (MgForm)ctrl.GetSubformMgForm();
                     subform.combineTabbingOrder((MgControl)ctrl);
                  }
                  else
                     // if subform doesn't contain a ownerTask, it must be removed from tabbing order
                     removeSubformFromTabbingOrder((MgControl)ctrl);
               }
            }

            // If this is the first combine, check if there is any dynamic element in it, and mark it on the
            // the form. This way, the recompute tabbing order will be skipped for tasks in which it is static
            if (parentSubformCtrl == null && _recomputeTabOrder == (char)0)
            {
               ctrl = _ctrlOrderHead;
               if (ctrl != null)
               {
                  if (ctrl.isSubform() && (ctrl.GetSubformMgForm() != null))
                     ctrl = ((MgForm)ctrl.GetSubformMgForm())._ctrlOrderHead;
                  while (ctrl != null && !dynamicRecompute)
                  {
                     prop = ctrl.getProp(PropInterface.PROP_TYPE_TAB_ORDER);
                     if (prop != null && prop.isExpression())
                        dynamicRecompute = true;

                     if (ctrl == _ctrlOrderTail)
                        break;
                     ctrl = ((MgControl)ctrl).getNextCtrl();
                  }
                  ctrl = _ctrlOrderHead;
                  if (ctrl.isSubform() && ctrl.checkProp(PropInterface.PROP_TYPE_TAB_IN, true) &&
                      (ctrl.GetSubformMgForm() != null))
                     ctrl = ((MgForm)ctrl.GetSubformMgForm())._ctrlOrderHead;
                  while (ctrl != null)
                  {
                     ((MgForm)ctrl.getForm())._recomputeTabOrder = (dynamicRecompute
                                                                    ? 'Y'
                                                                    : 'N');
                     if (ctrl == _ctrlOrderTail)
                        break;
                     ctrl = ((MgControl)ctrl).getNextCtrl();
                  }
               }
            }
         }
      }

      /// <summary>
      ///   remove the subform from tabbing order
      /// </summary>
      /// <param name = "subformCtrl"></param>
      private static void removeSubformFromTabbingOrder(MgControl subformCtrl)
      {
         MgControl prevCtrl, nextCtrl;

         Debug.Assert(subformCtrl.isSubform());

         prevCtrl = subformCtrl.getPrevCtrl();
         nextCtrl = subformCtrl.getNextCtrl();

         if (prevCtrl != null)
            prevCtrl.setNextCtrl(nextCtrl);
         if (nextCtrl != null)
            nextCtrl.setPrevCtrl(prevCtrl);
      }

      /// <summary>
      ///   Remove current form from its parents control tabbing order
      /// </summary>
      internal void removeFromParentsTabbingOrder()
      {
         if (!_task.IsSubForm)
            return;

         if (_ctrlOrderHead != null && _ctrlOrderHead.getPrevCtrl() != null)
         {
            MgControl nextCtrl = ((_ctrlOrderTail != null)
                                  ? _ctrlOrderTail.getNextCtrl()
                                  : null);
            _ctrlOrderHead.getPrevCtrl().setNextCtrl(nextCtrl);
            _ctrlOrderHead.setPrevCtrl(null);
         }

         if (_ctrlOrderTail != null && _ctrlOrderTail.getNextCtrl() != null)
         {
            MgControl prevCtrl = ((_ctrlOrderHead != null)
                                  ? _ctrlOrderHead.getPrevCtrl()
                                  : null);
            _ctrlOrderTail.getNextCtrl().setPrevCtrl(prevCtrl);
            _ctrlOrderTail.setNextCtrl(null);
         }
      }

      /// <summary>
      ///  This method is build the tabbing order for this form.
      ///  if this form is subform then it need to build the tabbing order from the topmost form and down.
      /// </summary>
      /// <param name = "first"></param>
      internal void RecomputeTabbingOrder(bool first)
      {
         // QCR #429987. Do not recompute tabbing order until task doesn't finish it's start process.
         // Because in offline tasks subform programs are loaded after the first record prefix of the parent, 
         // and we need combine the subform tabbing order with the parent.
         if (!getTask().isStarted() || ((Task)getTask()).InStartProcess)
            return;

         var form = (getTask().IsSubForm
                       ? (MgForm)getTask().getTopMostForm()
                       : this);
         form.RecomputeTabbingOrderThisFormAndDown(first);
      }

      /// <summary>
      /// build the tabbing order from this form and all his subform
      /// </summary>
      /// <param name="first"></param>
      /// <returns></returns>
      private void RecomputeTabbingOrderThisFormAndDown(bool first)
      {
         if (!getTask().isStarted())
            return;

         // if this task starts the recompute cycle, then create a dummy control, which holds a reference to the
         // place where the current order-list was inserted into our ancestor's list.
         MgControl tmpCtrl;
         if (first && getTask().IsSubForm)
         {
            tmpCtrl = new MgControl();
            tmpCtrl.setPrevCtrl(_ctrlOrderHead.getPrevCtrl());
            tmpCtrl.setNextCtrl(_ctrlOrderTail.getNextCtrl());
         }
         else
            tmpCtrl = null;

         // build my order-list
         buildTabbingOrder();

         // build order-list of all my subforms (recursively)
         for (int i = 0; i < CtrlTab.getSize(); i++)
         {
            var ctrl = CtrlTab.getCtrl(i);
            if (ctrl.isSubform() && ctrl.GetSubformMgForm() != null)
            {
               var subform = (MgForm)ctrl.GetSubformMgForm();
               subform.RecomputeTabbingOrderThisFormAndDown(false);
            }
         }

         // The ownerTask on which the rcmp was initiated will combine the tabbing order of all its descendant subforms
         if (first)
            combineTabbingOrder(tmpCtrl);
      }

      /// <summary>
      ///   moves the cursor to the first parkable control in the task or in the subtree (i.e.: including subforms)
      /// </summary>
      /// <param name = "formFocus">if true and if no control found - focus on form</param>
      internal bool moveToFirstCtrl(bool moveByTab, bool formFocus)
      {
         // try to park on the first parkable control
         bool cursorMoved = moveToFirstParkableCtrl(moveByTab);

         // there is no where to park. error (till no parkable controls will be implemented).
         if (!cursorMoved && formFocus)
            ClientManager.Instance.EventsManager.HandleNonParkableControls(_task);
         return cursorMoved;
      }

      /// <summary>
      ///   return first control that is not subform
      /// </summary>
      /// <returns></returns>
      internal MgControl getFirstNonSubformCtrl()
      {
         MgControl ctrl = _ctrlOrderHead;
         while (ctrl != null && ctrl.isSubform())
            ctrl = ctrl.getNextCtrl();
         return ctrl;
      }

      /// <summary>
      ///   return first parkable control that is not subform
      /// </summary>
      /// <returns></returns>
      internal MgControl getFirstParkNonSubformCtrl()
      {
         MgControl ctrl = _ctrlOrderHead;
         while (ctrl != null && ctrl.isSubform() && !ctrl.IsParkable(true))
            ctrl = ctrl.getNextCtrl();
         return ctrl;
      }

      /// <summary>
      ///   return Last parkable control that is not subform
      /// </summary>
      /// <returns></returns>
      internal MgControl getLastParkNonSubformCtrl()
      {
         MgControl ctrl = _ctrlOrderTail;
         while (ctrl != null && ctrl.isSubform() && !ctrl.IsParkable(true))
            ctrl = ctrl.getPrevCtrl();
         return ctrl;
      }

      /// <summary>
      ///   moves the cursor to the first parkable control beginning at the given control
      /// </summary>
      /// <param name="moveByTab"></param>
      /// <returns></returns>
      private bool moveToFirstParkableCtrl(bool moveByTab)
      {
         bool cursorMoved = false;
         var parkableInfo = new ParkableControlInfo(this, Constants.MOVE_DIRECTION_BEGIN);
         MgControl ctrl = getParkableControlInSubTree(parkableInfo, ClientManager.Instance.MoveByTab);
         MgControl leastFailedCtrl = null; // ctrl to park incase no parkable ctrl

         while (ctrl != null)
         {
            MgControl parkCtrl = ctrl.getControlToFocus();
            cursorMoved = parkCtrl.invoke(moveByTab, true);
            if (cursorMoved)
               break;
            if (ctrl.IsParkable(moveByTab) && !ctrl.allowedParkRelatedToDirection())
               leastFailedCtrl = ctrl;
            ctrl = ctrl.getNextCtrl();
         }

         if (!cursorMoved)
         {
            // if there is no parkable ctrl, find the last ctrl with Direction failed and park on it.
            if (leastFailedCtrl != null)
            {
               leastFailedCtrl.IgnoreDirectionWhileParking = true;
               cursorMoved = leastFailedCtrl.invoke(moveByTab, true);
               leastFailedCtrl.IgnoreDirectionWhileParking = false;
            }
         }

         return cursorMoved;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="lastParkedControl"></param>
      /// <returns></returns>
      private bool moveToLastParkbleCtrl(MgControl lastParkedControl)
      {
         bool cursorMoved = false;
         var parkableInfo = new ParkableControlInfo(this, Constants.MOVE_DIRECTION_END);
         MgControl ctrl = getParkableControlInSubTree(parkableInfo, ClientManager.Instance.MoveByTab);

         while (ctrl != null)
         {
            bool checkSubFormTabIntoProperty =
            !(lastParkedControl == null || lastParkedControl.onTheSameSubFormControl(ctrl));
            cursorMoved = ctrl.invoke(ClientManager.Instance.MoveByTab, checkSubFormTabIntoProperty);
            if (cursorMoved)
               break;
            ctrl = ctrl.getPrevCtrl();
         }

         return cursorMoved;
      }

      /// <summary>
      ///   remove references to the controls of this form
      /// </summary>
      public override void removeRefsToCtrls()
      {
         // if last focused control was from this task - zero it.
         var lastFocusedCtrl = GUIManager.getLastFocusedControl();
         if (lastFocusedCtrl != null && (lastFocusedCtrl.getForm() == this))
            GUIManager.setLastFocusedControl((Task)_task, null);

         base.removeRefsToCtrls();
      }

      /// <summary>
      ///   Return the control with a given name
      /// </summary>
      /// <param name = "ctrlName">the of the requested control</param>
      /// <returns> a reference to the control with the given name or null if does not exist</returns>
      internal MgControl getCtrl(String ctrlName)
      {
         return (MgControl)CtrlTab.getCtrl(ctrlName);
      }

      /// <summary>
      ///   Return the subform control with the typed name from all parents
      /// </summary>
      /// <param name = "ctrlName"> the requested subform control</param>
      /// <returns>a reference to the control with the given name or null if does not exist</returns>
      internal MgControl getSubFormCtrlByName(String ctrlName)
      {
         Task guiParentTask;
         MgControl destSubForm = null;

         for (guiParentTask = (Task)_task;
              destSubForm == null && guiParentTask != null && guiParentTask.getForm() != null;
         guiParentTask = (Task)guiParentTask.getParent())
         {
            destSubForm = (MgControl)guiParentTask.getForm().getCtrlByName(ctrlName, MgControlType.CTRL_TYPE_SUBFORM);
            if (destSubForm == null)
            {
               if (!guiParentTask.IsSubForm ||
                   guiParentTask.getForm().getSubFormCtrl().Name.Equals(ctrlName,
                                                                        StringComparison.
                                                                        InvariantCultureIgnoreCase))
                  break;
            }
         }

         return destSubForm;
      }

      /// <summary>
      ///   sets suffixDone parameter to true
      /// </summary>
      internal void setSuffixDone()
      {
         _suffixDone = true;
      }

      internal int getDestinationRow()
      {
         return _destTblRow;
      }

      /// <summary>
      ///   find the next control in the tabbing order, according to a requested Direction of movement. 1. if we try
      ///   to move back of the first control, null is returned. 2. if we try to move past the last control, and
      ///   record cycle is not allowed - null is returned. 3. However, if we can cycle then the first control will
      ///   be returned. 4. We rely on the fact that the tab-order head and tail belong to this task (not
      ///   Subforms/containers) but in between them, controls from other tasks might appear.
      /// </summary>
      /// <param name = "curr">control. We find the control adjacent to this one. Null represents the location prior to the first control.</param>
      /// <param name = "forward">do we move forward of backward</param>
      /// <returns> to the next control or null if we passed the edge of the record</returns>
      internal MgControl getNextTabCtrl(MgControl curr, bool forward)
      {
         //bool cycleRecMain = ownerTask.checkProp(PropInterface.PROP_TYPE_CYCLE_RECORD_MAIN, true);
         bool cycleRecMain = isRecordCycle(); //isTabbingCycleMoveToParentTask();
         MgControl realTail = _ctrlOrderTail;
         MgControl realHead = _ctrlOrderHead;

         while (realTail != null && realTail.isSubform())
            realTail = realTail.getPrevCtrl();

         while (realHead != null && realHead.isSubform())
            realHead = realHead.getNextCtrl();

         // what happens if we are positioned just before the first control
         if (curr == null)
         {
            if (forward)
               curr = _ctrlOrderHead;
         }
         else if (curr == realTail && forward)
         // Try to move past the end of record
         {
            if (cycleRecMain)
               curr = realHead;
            else
               curr = null;
         }
         else if (curr == realHead && !forward)
         // try to move past the beginning of the record
         {
            curr = null;
         }
         else
         {
            if (forward)
               curr = curr.getNextCtrl();
            else
               curr = curr.getPrevCtrl();
         }

         return curr;
      }

      /// <summary>
      ///   return the last control in the tabbing order of this task.
      /// </summary>
      internal MgControl getLastTabbedCtrl()
      {
         MgControl ctrl = _ctrlOrderTail;
         while (ctrl.isSubform())
            ctrl = ctrl.getPrevCtrl();
         return ctrl;
      }

      /// <summary>
      ///   return the index of a control in the tabbing order. Note that we only search the tabbing order of this
      ///   ownerTask
      /// </summary>
      /// <param name = "dst">control to search for its index</param>
      /// <returns> -1 if control not found or index (starting from 0)</returns>
      internal int ctrlTabOrderIdx(MgControl dst)
      {
         int idx = -1;
         MgControl curr, subFormCtrl, lastSubFormCtrl;

         if (dst == null)
            return idx;

         curr = _ctrlOrderHead;

         // #710425 - if first ctrl is a subform, get the first ctrl in the subform. repeat until a ctrl is found
         while (curr != null && curr.isSubform() && (curr.getSubformTask() != null))
            curr = ((MgForm)curr.getSubformTask().getForm())._ctrlOrderHead;

         lastSubFormCtrl = null;
         for (idx = 0; curr != null && curr != _ctrlOrderTail && curr != dst; curr = curr.getNextCtrl())
         {
            subFormCtrl = (MgControl)curr.getForm().getSubFormCtrl();
            if (subFormCtrl != null && lastSubFormCtrl != subFormCtrl && subFormCtrl.getForm().getTask() == _task)
            {
               // increment on entering a new subform on the ownerTask
               idx++;
               lastSubFormCtrl = subFormCtrl;
            }
            else if (curr.getForm().getTask() == _task)
               idx++;
         }

         // increment once more if dst ctrl is same as currCtrl
         if (curr == dst)
         {
            // for same ownerTask
            if (dst.getForm().getTask() == _task)
               idx++;
            else
            {
               // #919560 - if currCtrl and destCtrl is the first ctrl on the subform, we haven't increment
               // for the new subform.
               subFormCtrl = (MgControl)curr.getForm().getSubFormCtrl();
               if (subFormCtrl != null && ((MgForm)curr.getForm())._ctrlOrderHead == curr
                   && curr.getPrevCtrl() != null && curr.getPrevCtrl().getForm().getTask() != curr.getForm().getTask())
                  idx++;
            }
         }

         // if dst is on parent form, and we have move down to nested subform 'curr' would not be equal
         // to ctrlOrderTail, in such a case we move past the last ctrl of innermost form and 'curr'
         // is null. In this case too, -1 should be returned.
         if (curr == null || (curr == _ctrlOrderTail && curr != dst))
            return -1;

         return idx;
      }

      /// <param name = "subFormCtrl">the subFormCtrl to set</param>
      internal void setSubFormCtrl(MgControl subFormCtrl)
      {
         Debug.Assert(_subFormCtrl == null && subFormCtrl != null);
         _subFormCtrl = subFormCtrl;
      }

      /// <summary>
      ///   returns a subform control that points to a task whose id is given in taskId
      /// </summary>
      /// <param name = "taskId">the id of requested subform ownerTask</param>
      internal MgControl getSubFormCtrl(String taskId)
      {
         MgControl curr = null;
         ControlTable parentCtrlTab;
         MgFormBase guiParentForm = null;
         MgControl destSubForm = null;

         for (guiParentForm = this; guiParentForm != null; guiParentForm = guiParentForm.ParentForm)
         {
            parentCtrlTab = guiParentForm.CtrlTab;
            for (int i = 0; i < parentCtrlTab.getSize(); i++)
            {
               curr = parentCtrlTab.getCtrl(i) as MgControl;
               if (curr != null && curr.isSubform() && (curr.getSubformTaskId() != null))
                  if (curr.getSubformTaskId().Equals(taskId))
                  {
                     destSubForm = curr;
                     break;
                  }
            }

            if (destSubForm != null)
               break;
         }

         return destSubForm;
      }

      /// <summary>
      ///   rearranges tabbing order after table reorder event if form has automatic tabbing order
      /// </summary>
      /// <param name = "list"></param>
      internal void applyTabOrderChangeAfterTableReorder(List<MgControlBase> list)
      {
         var num = new NUM_TYPE();
         int tabOrderIdx = 0;

         for (int i = 0; i < list.Count; i++)
         {
            // only those controls which have TabOrder property should be re-adjusted.
            Property tabOrderProp = list[i].getProp(PropInterface.PROP_TYPE_TAB_ORDER);
            if (tabOrderProp == null)
               continue;

            num.NUM_4_LONG(_firstTableTabOrder + tabOrderIdx++);
            list[i].setProp(PropInterface.PROP_TYPE_TAB_ORDER, num.toXMLrecord());
         }
         resetRcmpTabOrder();
         RecomputeTabbingOrder(true);
      }

      /// <summary>
      ///   recreate records between idx and end of dataview
      /// </summary>
      /// <param name = "idx"></param>
      internal void removeRecordsAfterIdx(int idx)
      {
         bool removeAll = false;

         //in some cases invisible -1 row (that exist only for scrollbar effects) becomes visible.
         //To prevent this we'll remove all records. This will not cause perfromance problems since
         //we will add maximum rowInPage records more then we intended in the begining
         if (idx <= _rowsInPage + 1 && GetDataview().getSize() > _rowsInPage)
            idx = 0;
         if (idx == 0)
            removeAll = true;

         // invalidates all records after newCurrRow
         SetTableItemsCount(idx, removeAll);
         // updates table item's count    
         SetTableItemsCount(false);
      }

      /// <summary>
      ///   returns true if row is valid
      /// </summary>
      /// <param name = "idx"></param>
      /// <returns></returns>
      internal bool isRowValidated(int idx)
      {
         if (Rows.Count <= idx || idx < 0)
            return false;
         var row = (Row)Rows[idx];
         if (row == null)
            return false;
         return row.Created && row.Validated;
      }

      /// <summary>
      ///   runs in Worker thread when the thread is free and send to GUI thread controls data of all rows
      /// </summary>
      internal void transferDataToGui()
      {
         //Debug.Assert(!Misc.IsGuiThread());

         int saveRowIdx = GetDataview().getCurrRecIdx();
         long maxTime = Misc.getSystemMilliseconds() + 500;// TIME_LIMIT;
         bool updated = false;
         MgControlBase currentEditingControl = getTask().CurrentEditingControl;

         try
         {
            Record bkpRecord = ((DataView)GetDataview()).backupCurrent();
            //this limitation is needed so this request will never take too much time
            while (Misc.getSystemMilliseconds() < maxTime && RefreshRepeatableAllowed)
            {
               if (_lastRowSent + 1 >= GetDataview().getSize())
               {
                  //all rows already sent
                  break;
               }
               _lastRowSent++;
               AllowedSubformRecompute = false;
               if (!isRowValidated(_lastRowSent))
               //row needs to be sent
               {
                  //send row data
                  checkAndCreateRow(_lastRowSent);
                  if (_lastRowSent != saveRowIdx)
                  {
                     getTask().CurrentEditingControl = null;//reset the flag
                     //irrelevant for tree
                     setCurrRowByDisplayLine(_lastRowSent, false, true);
                     refreshControls(true);
                     updated = true;
                  }
               }
               AllowedSubformRecompute = true;
            }
            //restore previous current record
            if (updated)
            {
               restoreBackup(saveRowIdx, bkpRecord);
               refreshControls(true);
               getTask().CurrentEditingControl = currentEditingControl;//set the flag to the original value
               RefreshTableUI();
            }
         }
         catch (RecordOutOfDataViewException ex)
         {
            Logger.Instance.WriteWarningToLog(ex);
         }
      
       
         _transferingData = false;
         checkAndCreateRowsEvent();
      }

      /// <summary>
      ///   check if any rows need to be sent to GUI thread
      ///   if there are, puts event in event queue
      /// </summary>
      private void checkAndCreateRowsEvent()
      {
         if (_tableMgControl != null && Opened && RefreshRepeatableAllowed)
         {
            int size = GetDataview().getSize();
            if (_lastRowSent >= Rows.Count)
               //when we decrease size of rows array adjust lastRowSent
               _lastRowSent = Rows.Count - 1;
            if (_lastRowSent < size - 1 && !_transferingData)
            {
               //there are rows to send and there is now request for rows yet
               //add request for rows
               ClientManager.Instance.EventsManager.addInternalEvent(_tableMgControl, InternalInterface.MG_ACT_DV_TO_GUI, Priority.LOWEST);
               _transferingData = true;
            }
         }
      }

      /// <summary>
      ///   gets row data for the page
      /// </summary>
      /// <param name = "desiredTopIndex">desired top index if desiredTopIndex is Integer.MIN_VALUE, top index is cecieved form dv</param>
      /// <param name = "sendAll">if true, send all records, even if they are created and valid</param>
      /// <param name="lastFocusedVal"></param>
      internal void setRowData(int desiredTopIndex, bool sendAll, LastFocusedVal lastFocusedVal)
      {
         // dv.getTopRecIdx() may change in setCurrRow, so we save topindex + currTblRow
         int saveRowIdx = GetDataview().getCurrRecIdx();
         int diff = 0;
         bool getPrevChunk = false;
         bool getNextChunk = false;
         bool updated = false;

         if (desiredTopIndex != Int32.MinValue)
         {
            if (desiredTopIndex < 0)
            {
               // this is request for line or page before current chunk
               // in this case the top index is always 0
               getPrevChunk = true;
               GetDataview().setTopRecIdx(0);
            }
            else if (desiredTopIndex + _rowsInPage + 1 >= GetDataview().getSize())
               // pagedown or arrowdown at end of the dataview
               getNextChunk = true;
         }
         else
            desiredTopIndex = GetDataview().getTopRecIdx();

         Record bkpRecord = ((DataView)GetDataview()).backupCurrent();

         AllowedSubformRecompute = false;
         updated = refreshRows(desiredTopIndex, sendAll, saveRowIdx, out diff);
         if ((getPrevChunk && diff != 0) || getNextChunk)
         {
            // if diff != 0 a new chunk was brought to the begining of the dv
            // set top index to first requested index
            // if next chunk was brought, top index may also be updated
            GetDataview().setTopRecIdx(desiredTopIndex + diff);
            SetTableTopIndex();
            if (_topIndexUpdated)
            {
               //if top index was updated in the setTableTopIndex - we might have rows that were not refreshed
               _topIndexUpdated = false;
            }
         }
         AllowedSubformRecompute = true;

         if (updated)
         // any row was updated
         {
            // restore current record
            restoreBackup(saveRowIdx + diff, bkpRecord);
            refreshControls(true);
         }
         // ask table to refresh the page

         SelectRow();
         if (getPrevChunk)
         {
            var lastFocusedControl = GUIManager.getLastFocusedControl();
            //check if this is focused
            if (lastFocusedControl != null && lastFocusedControl.getParentTable() == _tableMgControl)
            {
               if (lastFocusedVal != null && lastFocusedControl == lastFocusedVal.guiMgControl
                   && saveRowIdx == lastFocusedVal.Line)
               {
                  //QCR #925367 need to restore value of edit control, since we did not save it anywere before the scroll
                  Commands.addAsync(CommandType.PROP_SET_TEXT, lastFocusedControl, saveRowIdx + diff, lastFocusedVal.Val,
                                    0);
               }
               Manager.SetFocus(lastFocusedControl, DisplayLine);
            }
            else
            {
               // previous chunk was brought, table items were distroyed and recteated,
               // update index of tmp editor
               Commands.addAsync(CommandType.UPDATE_TMP_EDITOR_INDEX, _tableMgControl);
               //RefreshTableUI();
               Commands.addAsync(CommandType.REFRESH_TABLE, _tableMgControl, 0, true);
            }
         }
         else if (updated)
            //RefreshTableUI();
            Commands.addAsync(CommandType.REFRESH_TABLE, _tableMgControl, 0, true);
      }

      /// <summary>
      ///   refreshes rows in the page
      /// </summary>
      /// <param name = "desiredTopIndex"></param>
      /// <param name = "sendAll"></param>
      /// <param name = "saveRowIdx"></param>
      /// <param name = "diff"></param>
      /// <returns></returns>
      private bool refreshRows(int desiredTopIndex, bool sendAll, int saveRowIdx, out int diff)
      {
         int prevTopIdx;
         int index = 0;
         bool updated = false;
         diff = 0;
         bool orgIncludesFirst = GetDataview().IncludesFirst();
         bool orgIncludesLast = GetDataview().IncludesLast();

         for (int i = 0; i < _rowsInPage + 2; i++)
         {
            int idx = desiredTopIndex + i;
            prevTopIdx = GetDataview().getTopRecIdx();
            try
            //if we fail to get one record, we may succeed to get the next one
            {
               index = idx + diff;
               if (!isRowValidated(idx) || sendAll)
               {
                  if (index >= 0)
                     checkAndCreateRow(index);
                  // dv.getTopRecIdx() may change in setCurrRow

                  if (saveRowIdx + diff != index) //not current line
                  {
                     //irrelevant for tree
                     setCurrRowByDisplayLine(index, false, true);
                     refreshControls(true);
                  }

                  // if a new chunk is brought top index will be changed
                  updated = true;
               }
            }
            catch (RecordOutOfDataViewException)
            {
               //creation of row failed, mark the row as not created
               if (isRowCreated(index))
               {
                  markRowNOTCreated(index);
               }
               break;
            }
            finally
            {
               //QCR #261055, even so required record may not be found, it is possible that a new chunk has been sent
               //and diff must be updated
               diff += (GetDataview().getTopRecIdx() - prevTopIdx);
               prevTopIdx = GetDataview().getTopRecIdx();
               if (!updated && GetDataview().DataviewBoundriesAreChanged(orgIncludesFirst, orgIncludesLast))
                  updated = true;
            }
         }
         return updated;
      }

      /// <summary>
      /// </summary>
      /// <returns> row in the page</returns>
      internal int getVisibleLine()
      {
         int currRecIdx = 0;
         if (GetDataview().getCurrRecIdx() != Int32.MinValue)
            currRecIdx = GetDataview().getCurrRecIdx();
         return currRecIdx - Math.Max(GetDataview().getTopRecIdx(), 0);
      }

      /// <summary>
      ///   asserts that current record is in the view, if it is not on the page, adjusts top index, so that current
      ///   record will be on the visible page
      /// </summary>
      internal void bringRecordToPage()
      {
         if ((_tableMgControl != null) && !_task.DataView.isEmptyDataview())
         {
            int topIndex = Commands.getTopIndex(_tableMgControl);
            int currIdx = GetDataview().getCurrRecIdx();
            int newTopIndex = _prevTopIndex = topIndex;
            if (topIndex > currIdx)
            {
               _topIndexUpdated = true;
               newTopIndex = currIdx;
            }
            else if (topIndex + _rowsInPage - 1 < currIdx)
            {
               _topIndexUpdated = true;
               // newTopIndex = currIdx;
               if (_rowsInPage > 0)
                  newTopIndex = currIdx - _rowsInPage + 1;
               else
                  newTopIndex = currIdx;
            } // TODO::take care of Center Screen in online parameter

            // set top index to be current line
            if (GetDataview().getTopRecIdx() != newTopIndex || _topIndexUpdated)
            {
               GetDataview().setTopRecIdx(newTopIndex);
               SetTableTopIndex();
               setRowData(Int32.MinValue, false, null);
               _topIndexUpdated = false;
            }
         }
      }

      /// <summary>
      ///   invalidate table
      /// </summary>
      internal void invalidateTable()
      {
         for (int i = 0; i < Rows.Count; i++)
         {
            var row = (Row)Rows[i];
            if (row != null)
               row.Validated = false;
         }
         if (_tableMgControl != null)
            Commands.addAsync(CommandType.INVALIDATE_TABLE, _tableMgControl);
         _lastRowSent = -1;
         checkAndCreateRowsEvent();
      }

      /// <summary>
      ///   get the wide form
      /// </summary>
      /// <returns></returns>
      internal MgForm getWideForm()
      {
         MgForm wideForm = null;
         if (wideIsOpen())
            wideForm = (MgForm)_wideControl.getForm();

         return wideForm;
      }

      /// <summary>
      ///   resets recomputeTabOrder for Destination Subform call
      /// </summary>
      internal void resetRcmpTabOrder()
      {
         _recomputeTabOrder = (char)0;
      }


      /// <summary>
      ///   change Tab control selection to next/previous
      /// </summary>
      /// <param name = "ctrl">current parked control</param>
      /// <param name = "Direction">next/previous</param>
      internal void changeTabSelection(MgControl ctrl, char direction)
      {
         MgControl tabControl = (MgControl)getTabControl(ctrl);
         if (tabControl != null)
            tabControl.changeSelection(direction);
      }

      /**
      * get the next control to park on for next field event according to the task property: tabbing cycle
      *
      * when parking on the last control of the form and Tabbing cycle is:
      * ==========================================
      * "Remain In Current Record" or "Move To Next Record": return null
      * "Move To Parent Task":                               the next control in the list
      *
      * if there is no control then it return null.
      * @param currentCtrl : the current control.
      * @return the next control to park on it.
      */

      private MgControl getNextControlToParkAccordingToTabbingCycle(MgControl currentCtrl)
      {
         MgControl retNextControl = null;

         retNextControl = getNextParkableCtrl(currentCtrl);
         var parkableInfo = new ParkableControlInfo((MgForm)currentCtrl.getForm(),
                                                    Constants.MOVE_DIRECTION_END);
         MgControl lastParkbleControlByTab = getParkableControlInSubTree(parkableInfo, true);
         MgControl lastParkbleControlByClick = getParkableControlInSubTree(parkableInfo, false);
         // if we on the last control in the current task (subform or root) it mean that we need to move 
         // to the next control in the current form (and dowm)
         if ((currentCtrl == lastParkbleControlByTab || currentCtrl == lastParkbleControlByClick) &&
             (isTabbingCycleRemainInCurrentRecord() || isTabbingCycleMoveToNextRecord()))
            retNextControl = null;
         return retNextControl;
      }

      /**
      * inner class for method  getParkableControlInSubTree
      */

      ///// <summary>
      ///// get the last parkble control in the current sub tree according to ClientManager.Instance.MoveByTab
      ///// </summary>
      ///// <param name="parkableInfo"></param>
      ///// <returns></returns>
      //private MgControl getParkableControlInSubTree(ParkableControlInfo parkableInfo)
      //{
      //   return (getParkableControlInSubTree(parkableInfo, ClientManager.Instance.MoveByTab));
      //}
      /**
      * get the last parkable control in the current sub tree
      * @param Direction == MOVE_DIRECTION_END is return the last parkble control in the sub tree
      * @param Direction == MOVE_DIRECTION_BEGIN is return the first parkble control in the sub tree
      * @return
      */

      private MgControl getParkableControlInSubTree(ParkableControlInfo parkableInfo, bool moveByTab)
      {
         MgControl checkParkableControl = null;
         MgControl subformParkableControl = null;

         if (parkableInfo.Direction == Constants.MOVE_DIRECTION_END)
            checkParkableControl = _ctrlOrderTail;
         else if (parkableInfo.Direction == Constants.MOVE_DIRECTION_BEGIN)
            checkParkableControl = _ctrlOrderHead;
         else
            Debug.Assert(false);

         if (checkParkableControl != null)
         {
            //find the prev\next parkable control on the same sub tree
            while (checkParkableControl != null && !parkableInfo.ParkableControlWasFound &&
                   checkParkableControl.isDescendent(parkableInfo.BaseForm))
            {
               // QCR #438241. For a subform control check its parkability and parkability of all its controls.
               if (checkParkableControl.IsParkable(moveByTab))
               {
                  if (checkParkableControl.isSubform() && (checkParkableControl.getSubformTask() != null))
                  {
                     subformParkableControl = ((MgForm)checkParkableControl.getSubformTask().getForm()).getParkableControlInSubTree(parkableInfo,
                                                                                                                                  moveByTab);
                     if (subformParkableControl != null)
                     {
                        parkableInfo.ParkableControlWasFound = true;
                        checkParkableControl = subformParkableControl;
                     }
                  }
                  else
                     // mgValue.bool that say that we found the last parkable control on the sub tree 
                     parkableInfo.ParkableControlWasFound = true;
               }

               if (!parkableInfo.ParkableControlWasFound)
               {
                  if (parkableInfo.Direction == Constants.MOVE_DIRECTION_END)
                     checkParkableControl = checkParkableControl.getPrevCtrl();
                  else if (parkableInfo.Direction == Constants.MOVE_DIRECTION_BEGIN)
                     checkParkableControl = checkParkableControl.getNextCtrl();
               }
            }
         }
         return checkParkableControl;
      }

      /**
      * get the prev control to park on for prev field event according to the task property: tabbing cycle
      *
      * when parking on the first control of the form and Tabbing cycle is:
      * ==========================================
      * "Remain In Current Record" or "Move To Next Record": return null
      * "Move To Parent Task":                               the prev control in the list
      *
      * if there is no control then it return null.
      * @param currentCtrl : the current control.
      * @return the next control to park on it.
      */

      private MgControl getPrevControlToParkAccordingToTabbingCycle(MgControl currentCtrl)
      {
         MgControl retPrevControl = getPrevParkableCtrl(currentCtrl);
         var parkableInfo = new ParkableControlInfo((MgForm)currentCtrl.getForm(),
                                                    Constants.MOVE_DIRECTION_BEGIN);
         MgControl FirstParkbleControlByTab = getParkableControlInSubTree(parkableInfo, true);
         MgControl FirstParkbleControlByClick = getParkableControlInSubTree(parkableInfo, false);

         // if we on the first control in the current task (subform or root) it mean that we need to move 
         // to the prev control in the current form (and dowm)         
         if ((currentCtrl == FirstParkbleControlByTab || currentCtrl == FirstParkbleControlByClick) &&
             (isTabbingCycleRemainInCurrentRecord() || isTabbingCycleMoveToNextRecord()))
            retPrevControl = null;
         return retPrevControl;
      }

      /// <summary>
      ///   get the prev control to be park on it(with not the same field)
      /// </summary>
      /// <param name = "control"></param>
      /// <returns></returns>
      internal MgControl getPrevControlToFocus(MgControl control)
      {
         MgControl retPrevControl = control.getPrevCtrl();

         while (retPrevControl != null && retPrevControl.getField() == control.getField())
            retPrevControl = retPrevControl.getPrevCtrl();

         return (retPrevControl == null
                 ? null
                 : retPrevControl.getControlToFocus());
      }

      /// <summary>
      ///   return previous visible line
      /// </summary>
      /// <param name = "line"></param>
      /// <returns></returns>
      internal int getPrevLine(int line)
      {
         return _mgTree != null
                   ? _mgTree.getPrev(line)
                   : line - 1;
      }

      /// <summary>
      ///   get the default button control of the form, if exist, otherwise return NULL
      /// </summary>
      /// <returns></returns>
      internal MgControl getDefaultButton(bool checkVisibleEnable)
      {
         MgControl defaultButton = null;

         Property prop = getProp(PropInterface.PROP_TYPE_DEFAULT_BUTTON);
         if (prop != null)
         {
            defaultButton = (MgControl)getCtrlByName(prop.getValue(), MgControlType.CTRL_TYPE_BUTTON);
            if (checkVisibleEnable && defaultButton != null)
            {
               if (!defaultButton.isVisible() || !defaultButton.isEnabled())
                  defaultButton = null;
            }
         }

         return defaultButton;
      }

      /**
      * the task is define as record cycle while the tabbing cycle is set to 
      * remain in current record OR move to parent task
      * @return
      */

      internal bool isRecordCycle()
      {
         return (isTabbingCycleRemainInCurrentRecord() || isTabbingCycleMoveToParentTask());
      }

      /**
      * return true is the task is define as cycle record that mean:
      * When the property TABBING CYCLE is set to "Move to parent task" : 
      * pressing Tab (or raising a next field event) from the last parkable control in a task, 
      * will move the caret to the next parkable control in the parent task.
      * This behavior is affective if the current task runs as a subfrom or a frame.
      * If the ownerTask is not running as a subfrom or a frame, the behavior will be as defined for the default value (?emain in current record..
      * This behavior is similar to the current RC task behavior.
      * it is the same as isCycleRecordMain
      */

      private bool isTabbingCycleMoveToParentTask()
      {
         Property prop = _task.getProp(PropInterface.PROP_TYPE_TABBING_CYCLE);
         return ((prop != null && prop.getValue()[0] == GuiConstants.TABBING_CYCLE_MOVE_TO_PARENT_TASK));
      }

      /**
      * When this new property is set to ?emain in current record. pressing Tab 
      * (or raising a next field event) from the last parkable control in a task, 
      * will move the caret to the first parkable control in the same record of the task.
      * This behavior is the same when the caret is positioned in a parent task or in a subfrom/frame task.
      * This behavior is similar to online ownerTask with ?ycle on record.?es.
      * @return
      */

      private bool isTabbingCycleRemainInCurrentRecord()
      {
         Property prop = _task.getProp(PropInterface.PROP_TYPE_TABBING_CYCLE);
         return ((prop != null && prop.getValue()[0] == GuiConstants.TABBING_CYCLE_REMAIN_IN_CURRENT_RECORD));
      }

      /**
      * When this new property is set to ?ove to next record. pressing Tab (
      * or raising a next field event) from the last parkable control in a task, 
      * will move the caret to the first parkable control in the next record of the task.
      * Note: on modify mode, tabbing from the last control in the last record will open a new record.
      * This behavior is the same when the caret is positioned in a parent task or in a subfrom/frame task.
      * This behavior is similar to online task with ?ycle on record.?o.
      * @return
      */

      private bool isTabbingCycleMoveToNextRecord()
      {
         Property prop = _task.getProp(PropInterface.PROP_TYPE_TABBING_CYCLE);
         return ((prop != null && prop.getValue()[0] == GuiConstants.TABBING_CYCLE_MOVE_TO_NEXT_RECORD));
      }
      /// <summary>
      ///   returns the last non subform ctrl
      /// </summary>
      /// <returns></returns>
      internal MgControl getLastNonSubformCtrl()
      {
         MgControl firstCtrl = getFirstNonSubformCtrl();
         MgControl lastCtrl = null;
         MgControl currCtrl = null;
         MgControl subFormCtrl = null;

         if (firstCtrl != null)
         {
            lastCtrl = firstCtrl;
            currCtrl = firstCtrl.getNextCtrl();
            if (currCtrl != null)
               subFormCtrl = (MgControl)currCtrl.getForm().getSubFormCtrl();
         }

         while (currCtrl != null)
         {
            while (currCtrl != null && subFormCtrl != null && firstCtrl.onDiffForm(currCtrl))
            {
               subFormCtrl = (MgControl)currCtrl.getForm().getSubFormCtrl();
               if (subFormCtrl != null)
                  currCtrl = subFormCtrl.getNextCtrl();
            }

            if (currCtrl != null)
            {
               // #919260 - we should set lastCtrl only if currCtrl is not subform
               if (!currCtrl.isSubform() && lastCtrl.getForm().getTask() == currCtrl.getForm().getTask())
                  lastCtrl = currCtrl;
               currCtrl = currCtrl.getNextCtrl();
               if (currCtrl != null)
                  subFormCtrl = (MgControl)currCtrl.getForm().getSubFormCtrl();
            }
         }

         return lastCtrl;
      }

      /// <summary>
      ///   Depending on 'Direction', it returns next ctrl on the form ignoring subforms. 
      ///   'ctrl' can be a ctrl inside a subformCtrl.
      /// </summary>
      /// <param name = "ctrl"></param>
      /// <param name = "Direction"></param>
      /// <returns></returns>
      internal MgControl getNextCtrlIgnoreSubforms(MgControl ctrl, Direction direction)
      {
         MgControl firstCtrl = getFirstNonSubformCtrl();

         MgControl subformCtrl = null;
         MgControl nextCtrl = null;
         MgControl nextNonSubformCtrl = null;

         if (ctrl == null)
            return null;

         if (direction == Direction.FORE)
            nextCtrl = ctrl.getNextCtrl();
         else
            nextCtrl = ctrl.getPrevCtrl();

         // #756675 - we should not exit if nextCtrl is on diff form as we need to find the next ctrl on this form.
         if (nextCtrl == null)
            return null;

         nextNonSubformCtrl = nextCtrl;

         if (firstCtrl.onDiffForm(nextNonSubformCtrl))
         {
            subformCtrl = (MgControl)nextNonSubformCtrl.getForm().getSubFormCtrl();
            if (subformCtrl != null)
            {
               nextNonSubformCtrl = subformCtrl;

               while (nextNonSubformCtrl != null && nextNonSubformCtrl.isSubform())
               {
                  nextCtrl = ((MgForm)nextNonSubformCtrl.getForm()).getNextTabCtrl(nextNonSubformCtrl,
                                                                                    (direction == Direction.FORE));
                  if (nextCtrl != null && firstCtrl.onDiffForm(nextCtrl))
                  {
                     subformCtrl = (MgControl)nextCtrl.getForm().getSubFormCtrl();

                     // finds subformCtrl on same form as firstCtrl
                     while (subformCtrl != null && firstCtrl.onDiffForm(subformCtrl))
                        subformCtrl = (MgControl)subformCtrl.getForm().getSubFormCtrl();

                     if (subformCtrl != null)
                        nextCtrl = subformCtrl;
                     else
                        nextCtrl = null;
                  }
                  nextNonSubformCtrl = nextCtrl;
               }
            }
         }
         else if (nextNonSubformCtrl.isSubform())
         {
            while ((nextNonSubformCtrl = nextNonSubformCtrl.getNextCtrlOnDirection(direction)) != null
                   && nextNonSubformCtrl.isSubform())
            {
            }
         }

         return nextNonSubformCtrl;
      }

      /// <summary>
      ///   This function clears the sort mark(delta) from the column.
      /// </summary>
      /// <param name = "clearSortMark"></param>
      internal void clearTableColumnSortMark(bool clearSortMark)
      {
         if (clearSortMark)
         {
            var table = (MgControl)getTableCtrl();
            Commands.addAsync(CommandType.CLEAR_TABLE_COLUMNS_SORT_MARK, table);
         }
      }

      /// <summary>
      ///   update the display, the controls and their properties
      /// </summary>
      internal bool RefreshDisplay(char refreshType)
      {
         Task task = (Task)_task;

         if (task.isAborting())
            return false;

         Record currRec = GetDataview().getCurrRec();

         // Take another approach to the solution of QCRs #984999, #933639 & #699821:
         // Now, the display would be refreshed only if the current record is NOT an old
         // record which is being computed. The result of this change is that during the
         // compute process of a record the display is not refreshed (which is good for us).
         // The display would be refreshed anyway AFTER the compute process is done.
         // (Ehud 21-MAY-2002)
         if (currRec != null && currRec.InCompute && !currRec.isNewRec())
            return false;

         if (_inRefreshDisp)
            return false;
         _inRefreshDisp = true;
         if (task.getRefreshType() == Constants.TASK_REFRESH_TREE_AND_FORM)
            refreshType = task.getRefreshType();

         try
         {
            if (refreshType == Constants.TASK_REFRESH_FORM || refreshType == Constants.TASK_REFRESH_TABLE)
            {
               // check if there is a table in the form and refresh it
               if (HasTable() && Opened)
                  refreshTable();
            }
            if (refreshType == Constants.TASK_REFRESH_TREE_AND_FORM)
               if (_mgTree != null)
               {
                  GetDataview().setTopRecIdx(0);
                  updateDisplayLineByDV();
                  ((MgTree)_mgTree).createGUINodes(1);
                  if (Opened)
                     _mgTree.updateExpandStates(1);
                  SelectRow(true);
               }

            if (refreshType == Constants.TASK_REFRESH_FORM || refreshType == Constants.TASK_REFRESH_CURR_REC ||
                refreshType == Constants.TASK_REFRESH_TREE_AND_FORM)
            {
               if (refreshType == Constants.TASK_REFRESH_CURR_REC && hasTableOrTree())
               {
                  SetTableTopIndex();
                  SelectRow();
               }
               // refresh the form properties
               refreshProps();
               // refresh the current record/Row
               refreshControls(false);
            }
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(string.Format("{0} : {1} in task {2}", ex.GetType(), ex.Message,
                                                                 ((Task)getTask()).PreviouslyActiveTaskId));
         }
         finally
         {
            _inRefreshDisp = false;
         }
         RefreshUI();

         if (task.getRefreshType() == Constants.TASK_REFRESH_TREE_AND_FORM)
            task.resetRefreshType();

         // Signal we completed the first cycle of refreshing the form
         if (refreshType == Constants.TASK_REFRESH_CURR_REC && (!isLineMode() || _tableRefreshed))
            FormRefreshed = true;
         return true;
      }

      public void RefreshUI()
      {
         if (!getTask().isMainProg() /*&& InitializationFinished*/)
         {
            if (!ScreenControlsData.IsEmpty())
               JSBridge.Instance.RefreshUI(getTask().getTaskTag(), SerializeControls());


         }
      }



      public String SerializeControls()
      {
         JavaScriptSerializer serializer = new JavaScriptSerializer();
         ScreenControlsData.ClearForSerialization();
         string result = serializer.Serialize(ScreenControlsData);
         ScreenControlsData = new ControlsData();
         return result;
      }

      public class TableUpdate
      {
         public bool fullRefresh;
         public Dictionary<string, ControlsData> rows = new Dictionary<string, ControlsData>();
      }     

      public override void RefreshTableUI()
      {
         string result = "";
         TableUpdate tableUpdate = new TableUpdate();
         if (HasTable())// && InitializationFinished)
         {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
           
            for (int i = 0; i < Rows.Count; i++)
            {
               Row row = (Row)Rows[i];
               if (row != null && row.ControlsData != null && !row.ControlsData.IsEmpty())
               {
                  row.ControlsData.ClearForSerialization();
                  tableUpdate.rows[i.ToString()] = row.ControlsData;
               }
            }
            tableUpdate.fullRefresh = rowsWereChanged;
            rowsWereChanged = false;
            if (tableUpdate.rows.Count > 0)
            {
               result = serializer.Serialize(tableUpdate);
               JSBridge.Instance.RefreshTableUI(getTask().getTaskTag(), result);
               foreach (Row item in Rows)
               {
                  if (item != null)
                     item.ControlsData = new ControlsData();
               }
            }
         }
      }

      /// <summary>
      ///   refresh forms with tables
      /// </summary>
      private void refreshTable()
      {
         //for now - always show all records
         setRowsInPage(GetDataview().getSize());
         int i, oldCurrRow, currRecIdx;
         Logger.Instance.WriteGuiToLog("Start form.refreshTable()");

         // save the old current row
         updateDisplayLineByDV();
         oldCurrRow = DisplayLine;
         currRecIdx = GetDataview().getCurrRecIdx();

         // For the first time we refresh the table - set curr-row and top-rec-idx to be the
         // ones requested by the dataview.
         // In the following table refreshes - we change curr-row and top-rec only if the table
         // becomes empty.
         if (!_tableRefreshed && currRecIdx >= 0)
         {
            oldCurrRow = GetDataview().getCurrRecIdx();

            _tableMgControl.getProp(PropInterface.PROP_TYPE_HEIGHT).RefreshDisplay(false);

            if (oldCurrRow + 1 > _rowsInPage)
               GetDataview().setTopRecIdx(oldCurrRow - (_rowsInPage / 2) + 1);
            else
               GetDataview().setTopRecIdx(0);
         }
         else
         {
            if (oldCurrRow == Int32.MinValue)
               oldCurrRow = 0;
            if (GetDataview().getTopRecIdx() == Int32.MinValue)
               GetDataview().setTopRecIdx(currRecIdx);
         }

         // When TopRecIdx < 0 it means we shold not continue the refresh since we are in temporary situation.
         // Another refresh will be done soon with correct values.
         // This is can happen when :
         // In fetchChunkFromServer we go to the server and the client expects new records.
         // While in the process of fetchingChunk, we might go again to the server, before the server sends us the chunk.
         // Going to the server in the get chunk process , can be for example: record suffix, or any server side op in rec suff or VC.
         // Usually, during this part, we do not refresh the table. 
         // But, for example in QCR #698217, a call was made to refreshTable with TopRecIdx == -1.
         // The reason was, that in the VC there was a server op that updated a var in the task. As a result, the 'modi' flag
         // from the server was on, which caused the "temp" refreshTable before the real chunk arrived.
         // This in turn caused a change in the curr rec id, which was wrong.
         // After the chunk arrives, the refresh sets the correct values.
         if (GetDataview().getTopRecIdx() < 0)
            return;

         oldCurrRow -= GetDataview().getTopRecIdx(); // save oldCurrRow relatively to the top index
         Record bkpRecord = ((DataView)GetDataview()).backupCurrent();
         _tableRefreshed = true;
         AllowedSubformRecompute = false;
         //QCR #998067, we must not refresh subforms for any rows that are not our real current row 

         // refresh all the rows BUT not the old current row
         for (i = 0; i < _rowsInPage + 2; i++)
         {
            // when there is no record for a row just disable it and vice versa
            int topIndex = GetDataview().getTopRecIdx();
            if (topIndex < 0)
            {
               GetDataview().setTopRecIdx(0);
               topIndex = 0;
            }
            int idx = topIndex + i;
            try
            {
               checkAndCreateRow(idx);

               // skip the old current row. the refresh for this row is done by the caller
               // and refresh only valid lines that were not disabled
               if (i != oldCurrRow)
               {
                  setCurrRowByDisplayLine(idx, false, true);
                  refreshControls(true);
                  
               }
            }
            catch (RecordOutOfDataViewException)
            {
               //creation of row failed, mark the row as not created
               if (isRowCreated(idx))
                  markRowNOTCreated(idx);
               break;
            }
         }
         AllowedSubformRecompute = true;
         restoreBackup(oldCurrRow + GetDataview().getTopRecIdx(), bkpRecord);
         refreshControls(true);
         Logger.Instance.WriteGuiToLog("End form.refreshTable()");
         //???!!!
         RefreshTableUI ();

      }

      /// <summary>
      ///   set display line by record line
      /// </summary>
      internal void updateDisplayLineByDV()
      {
         DisplayLine = recordIdx2DisplayLine(GetDataview().getCurrRecIdx());
      }

      /// <summary>
      ///   set the current row
      /// </summary>
      /// <param name = "displayLine">is the new current display line</param>
      /// <param name = "doSuffix">do the record suffix for the previous record</param>
      /// <param name = "ignoreCurrRec">do the actions as if there is no current record</param>
      internal void setCurrRowByDisplayLine(int displayLine, bool doSuffix, bool ignoreCurrRec)
      {
         int recIdx = displayLine2RecordIdx(displayLine);
         setCurrRowByRecIdx(recIdx, doSuffix, ignoreCurrRec, displayLine);
         if (HasTable())
            updateDisplayLineByDV();
         else
            DisplayLine = displayLine;
      }

      /// <summary>
      ///   set the current row
      /// </summary>
      /// <param name = "rowNum">is the new current row number</param>
      /// <param name = "doSuffix">do the record suffix for the previous record</param>
      /// <param name = "ignoreCurrRec">do the actions as if there is no current record</param>
      private void setCurrRowByRecIdx(int rowNum, bool doSuffix, bool ignoreCurrRec, int displayLine)
      {
         if (GetDataview() == null || (!hasTableOrTree() && rowNum != 0))
            throw new ApplicationException("in Form.setCurrRow() no table ");

         try
         {
            _destTblRow = rowNum;
            GetDataview().setCurrRecByIdx(rowNum, doSuffix, ignoreCurrRec, true, displayLine);
         }
         finally
         {
            _destTblRow = rowNum - GetDataview().getTopRecIdx();
         }
      }

      /// <summary>
      ///   returns the last valid row in the table
      /// </summary>
      protected internal int getLastValidRow()
      {
         int line = Math.Min(_rowsInPage - 1, GetDataview().getSize() - 1 - GetDataview().getTopRecIdx());
         return line;
      }

      /// <summary>
      ///   translate record line to display line
      /// </summary>
      /// <param name = "recIdx"></param>
      /// <returns></returns>
      internal int recordIdx2DisplayLine(int recIdx)
      {
         return _mgTree != null
                ? ((MgTree)_mgTree).getFirstNodeIdByRecIdx(recIdx)
                : recIdx;
      }

      /// <summary>
      ///   translate display line to record line
      /// </summary>
      /// <param name = "displayLine"></param>
      /// <returns></returns>
      internal int displayLine2RecordIdx(int displayLine)
      {
         return _mgTree != null
                ? ((MgTree)_mgTree).getRecIdx(displayLine)
                : displayLine;
      }

      /// <summary>
      ///   restore the old current row using the parameter and ignoring any exception
      /// </summary>
      /// <param name = "displayLine">the old current row number to be restored</param>
      internal void restoreOldDisplayLine(int displayLine)
      {
         try
         {
            _inRestore = true;
            setCurrRowByDisplayLine(displayLine, false, true);
         }
         catch (RecordOutOfDataViewException)
         {
            // do nothing
         }
         finally
         {
            _inRestore = false;
         }
      }

      /// <summary>
      ///   gets record from gui level
      /// </summary>
      public override int getTopIndexFromGUI()
      {
         int topDisplayLine = base.getTopIndexFromGUI();

         if (hasTableOrTree())
            GetDataview().setTopRecIdx(displayLine2RecordIdx(topDisplayLine));

         return topDisplayLine;
      }

      /// <summary>
      /// returns the current record position in the form.
      /// </summary>
      /// <returns></returns>
      public int getCurrRecPosInForm()
      {
         // when the selected record is outside the view, topIndex and DisplayLine will be 0
         // resulting in currRecPosInForm == 1
         int topDisplayLine = base.getTopIndexFromGUI();
         int currRecPosInForm = DisplayLine - topDisplayLine + 1;

         return currRecPosInForm;
      }

      /// <summary>
      ///   restores current record
      /// </summary>
      /// <param name = "oldDisplayLine">old current record idx</param>
      /// <param name = "bkpRecord">saved record</param>
      internal void restoreBackup(int oldDisplayLine, Record bkpRecord)
      {
         // QCR #942878, if record is in process of insert, delete or modify we must perform restoreOldCurrRow
         // that will perform all necessary actions for the record, otherwise we can just restore it from backup
         if (bkpRecord.getId() >= 0 && GetDataview().getRecIdx(bkpRecord.getId()) >= 0 && oldDisplayLine >= 0 &&
             oldDisplayLine < GetDataview().getSize() && GetDataview().getCurrRecIdx() >= 0 && bkpRecord.getMode() == DataModificationTypes.None)
         {
            ((DataView)GetDataview()).restoreCurrent(bkpRecord);
            updateDisplayLineByDV();
         }
         else
            restoreOldDisplayLine(oldDisplayLine);
      }

      /// <summary>
      ///   build the controls tabbing order of this form
      /// </summary>
      protected override void buildTabbingOrder()
      {
         // if tabbing order is static, and it was already calculated, no need to re-calculate it
         if (_recomputeTabOrder != 'N')
         {
            // make sure that the head & tail are null
            _ctrlOrderHead = null;
            _ctrlOrderTail = null;

            // create a temp arrayList to hold the controls
            var ctrlArray = new List<MgControlBase>();

            // get the sorted ctrls based on tab order property
            getSortedControls(ctrlArray);

            if (isSubForm())
            {
               MgFormBase parentForm = getTopMostForm();
               //QCR #241403 If tabbing order of any subform is changed, topmostform must combine Tabbing Order again 
               ((MgForm)parentForm)._recomputeTabOrder = 'Y';
            }

            // loop on the sorted array and build a list of controls. Only controls which have a field from the
            // current task, or subform controls into which we are tabbing into, will be added to the list.
            for (int i = 0; i < ctrlArray.Count; i++)
            {
               var ctrl = (MgControl)ctrlArray[i];
               if (!ctrl.isStatic() && !ctrl.isFrameSet())
               {
                  if (_ctrlOrderHead == null)
                  {
                     _ctrlOrderHead = _ctrlOrderTail = ctrl;
                     _ctrlOrderHead.setNextCtrl(null);
                     _ctrlOrderTail.setPrevCtrl(null);
                  }
                  else
                  {
                     _ctrlOrderTail.setNextCtrl(ctrl);
                     ctrl.setPrevCtrl(_ctrlOrderTail);
                     _ctrlOrderTail = ctrl;
                     _ctrlOrderTail.setNextCtrl(null);
                  }
               }
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      protected override void UpdateTabbingOrderFromRuntimeDesigner()
      {
         buildTabbingOrder();
      }

      /// <summary>
      ///   return the sorted ctrls based on tab order property
      /// </summary>
      /// <param name = "ctrlArray">outParam to hold the sortedCtrlArray</param>
      private void getSortedControls(List<MgControlBase> ctrlArray)
      {
         MgControlBase ctrlToSort, sortedCtrl;
         Property ctrlToSortProp, sortedCtrlProp;
         int ctrlToSortPropVal, sortedCtrlPropVal;
         int ctrlIdx, sortArrayIdx, shiftIdx;
         int insertPos;

         for (ctrlIdx = 0; ctrlIdx < CtrlTab.getSize(); ctrlIdx++)
         {
            ctrlToSort = CtrlTab.getCtrl(ctrlIdx);
            if (ctrlToSort == null)
               continue;

            // only ctrls with taborder property other than label, group and richText should be included in list
            ctrlToSortProp = ctrlToSort.getProp(PropInterface.PROP_TYPE_TAB_ORDER);
            if (ctrlToSortProp == null)
               continue;

            ctrlToSortPropVal = ctrlToSortProp.getValueInt();
            insertPos = 0;

            // Find the position of insertion in sortedList
            for (sortArrayIdx = 0; sortArrayIdx < ctrlArray.Count; sortArrayIdx++)
            {
               sortedCtrl = ctrlArray[sortArrayIdx];
               sortedCtrlProp = sortedCtrl.getProp(PropInterface.PROP_TYPE_TAB_ORDER);
               sortedCtrlPropVal = sortedCtrlProp.getValueInt();

               if (ctrlToSortPropVal == sortedCtrlPropVal)
               {
                  // if ctrlToSort is an Exp and sortedCtrl in not an Exp, we need to shift sortedCtrl.
                  // OtherWise, we need to insert after sortedCtrl
                  insertPos = ctrlToSortProp.isExpression() && !sortedCtrlProp.isExpression()
                              ? sortArrayIdx
                              : sortArrayIdx + 1;
               }
               else if (ctrlToSortPropVal > sortedCtrlPropVal)
                  insertPos = sortArrayIdx + 1;
            }

            // append an entry
            ctrlArray.Add(null);

            //shift all the entries in ctrlArray from 'insertPos' by 1.
            for (shiftIdx = ctrlArray.Count - 1; shiftIdx > insertPos; shiftIdx--)
               ctrlArray[shiftIdx] = ctrlArray[shiftIdx - 1];

            // add the ctrl at 'insertPos'
            ctrlArray[insertPos] = ctrlToSort;
         }
      }

      /// <summary>
      /// constructs an object of MgControl
      /// </summary>
      /// <returns></returns>
      public override MgControlBase ConstructMgControl()
      {
         return new MgControl();
      }

      /// <summary>
      /// constructs an object of MgControl
      /// </summary>
      /// <param name="type"></param>
      /// <param name="task"></param>
      /// <param name="parentControl"></param>
      /// <returns></returns>
      public override MgControlBase ConstructMgControl(MgControlType type, TaskBase task, int parentControl)
      {
         return new MgControl(type, (Task)task, parentControl);
      }

      /// <summary>constructs an object of MgControl</summary>
      /// <param name="type"></param>
      /// <param name="parentMgForm"></param>
      /// <param name="parentControlIdx"></param>
      /// <returns></returns>
      protected override MgControlBase ConstructMgControl(MgControlType type, MgFormBase parentMgForm, int parentControlIdx)
      {
         return new MgControl(type, parentMgForm, parentControlIdx);

      }
      /// <summary>Enables MLE actions and call openWide on base class</summary>
      /// <param name="parentControl"></param>
#if PocketPC
      protected override internal void openWide(MgControlBase parentControl)
#else
      protected override void openWide(MgControlBase parentControl)
#endif
      {
         //enable the MLE actions & state (the parent wide can't be MLE, so we are not checking the parent MLE property)
         _wideActBegNxtLineWasEnabled = _task.ActionManager.isEnabled(InternalInterface.MG_ACT_EDT_BEGNXTLINE);
         _task.ActionManager.enableMLEActions(true);

         //disable the action of the begin next line so the CR will not be enable 
         _task.ActionManager.enable(InternalInterface.MG_ACT_EDT_BEGNXTLINE, false);

         UpdateStatusBar(GuiConstants.SB_WIDEMODE_PANE_LAYER, ConstInterface.MG_NORMAL, false);

         //call base class functionality 
         base.openWide(parentControl);
      }

      /// <summary>Disables MLE actions and call openWide on base class</summary>
#if PocketPC
      public override void closeWide()
#else
      public override void closeWide()
#endif
      {
         //disable the MLE actions & state (the parent wide can't be MLE, so we are not checking the parent MLE property)
         _task.ActionManager.enableMLEActions(false);

         //enable the action MG_ACT_EDT_BEGNXTLINE, if this action was enable before the wide
         _task.ActionManager.enable(InternalInterface.MG_ACT_EDT_BEGNXTLINE, _wideActBegNxtLineWasEnabled);

         //call base class functionality
         base.closeWide();

         UpdateStatusBar(GuiConstants.SB_WIDEMODE_PANE_LAYER, ConstInterface.MG_WIDE, false);
      }

      /// <summary>
      ///   Returns true if already moved to the first control on one of the MgData
      /// </summary>
      internal bool alreadyMovedToFirstControl()
      {
         return ((Task)_task).getMGData().AlreadyMovedToFirstControl();
      }

      /// <summary>
      ///   refresh table first time we enter the ownerTask
      ///   get rows number form gui level
      /// </summary>
      public override void firstTableRefresh()
      {
         if (_tableMgControl != null)
         {
            base.firstTableRefresh();
            SetTableItemsCount(false);
            refreshTable();
            SelectRow();
         }
      }

      /// <summary>
      ///   update table item's count according to dataview
      /// </summary>
      internal void SetTableItemsCount(bool removeAll)
      {
         int tableItemsCount = 0;         

         //For absolute scrollbar, set the virtual items count accordingly.
         if (isTableWithAbsoluteScrollbar() && GetDataview().TotalRecordsCount > 0)
            tableItemsCount = GetDataview().TotalRecordsCount;
         else
            tableItemsCount = GetDataview().getSize();

         SetTableItemsCount(tableItemsCount, removeAll);
      }

      /// <summary>
      /// inits the table control
      /// </summary>
      protected override void InitTableControl()
      {
         base.InitTableControl();
         SetTableTopIndex();
      }

      bool rowsWereChanged;
      /// <summary>
      /// inits table control's rows to 'size' count
      /// </summary>
      /// <param name="size"></param>
      /// <param name="removeAll"></param>
      protected override void InitTableControl(int size, bool removeAll)
      {
         if (_tableMgControl != null)
         {
            if (removeAll)
            {
               Commands.addAsync(CommandType.SET_TABLE_INCLUDES_FIRST, _tableMgControl, 0, true);
               Commands.addAsync(CommandType.SET_TABLE_INCLUDES_LAST, _tableMgControl, 0, true);
               rowsWereChanged = true;
            }
            else
            {
               Commands.addAsync(CommandType.SET_TABLE_INCLUDES_FIRST, _tableMgControl, 0, (GetDataview().IncludesFirst() || ((DataView)GetDataview()).IsOneWayKey));
               Commands.addAsync(CommandType.SET_TABLE_INCLUDES_LAST, _tableMgControl, 0, GetDataview().IncludesLast());
            }

            base.InitTableControl(size, removeAll);

            checkAndCreateRowsEvent();
         }
      }

      /// <summary>
      ///   set top index
      /// </summary>
      internal void SetTableTopIndex()
      {
         if (_tableMgControl != null)
         {
            int index = GetDataview().getTopRecIdx();
            if (index == Int32.MinValue)
               index = 0;
            if (GetDataview().IncludesLast())
            {
               // table now behaves like listview, it can not show empty lines
               // so we must update top index
               if (index + _rowsInPage > GetDataview().getSize())
               {
                  //but we do show empty lines if includeFirst = false 
                  //and we have less records then rows on screen QCR #!!!!!!
                  if (GetDataview().IncludesFirst() || GetDataview().getSize() > _rowsInPage + 1)
                  {
                     index = Math.Max(GetDataview().getSize() - _rowsInPage, 0);
                     GetDataview().setTopRecIdx(index);
                     _topIndexUpdated = true;
                  }
               }
            }
            if (index != _prevTopIndex)
            {
               if (isTableWithAbsoluteScrollbar())
                  SetRecordsBeforeCurrentView(GetDataview().RecordsBeforeCurrentView);
               
               Commands.addAsync(CommandType.SET_TABLE_TOP_INDEX, _tableMgControl, 0, index);              
               _prevTopIndex = index;
            }
         }
      }

      protected override object FindParent(RuntimeContextBase runtimeContext)
      {
         Object parent = base.FindParent(runtimeContext);
         //defect 122145, modal window must have owner to prevent behavior described in the defect
         //see article:
         //http://stackoverflow.com/questions/6712026/problem-with-showdialog-when-showintaskbar-is-false
         if (parent == null && ConcreteWindowType == WindowType.Modal)
            parent = CalculateOwnerForm(GUIManager.LastFocusMgdID);
         return parent;
      }

      /// <summary>
      /// find owner window fro new form  
      /// </summary>
      /// <returns></returns>
      private static MgFormBase CalculateOwnerForm(int lastFocusMgdID)
      {
         MgFormBase form = null;
         MGData mgData = MGDataCollection.Instance.getCurrMGData();
         while (form == null && mgData != null)
         {
            Task task = ClientManager.Instance.getLastFocusedTask(mgData.GetId());
            //get current task
            if (task != null)
               form = task.getForm();
            //if it is subfrom - get its form
            if (form != null && form.Opened)
               form = form.getTopMostForm();
            //if not found - look for previous windows
            if (form == null)
               mgData = mgData.getParentMGdata();
         }

         // form is null in all task parents. last chance, try the last focused mgData
         if (form == null)
         {
            Task task = ClientManager.Instance.getLastFocusedTask(lastFocusMgdID);
            if (task != null)
               form = task.getForm();

            if (form != null && form.Opened)
               form = form.getTopMostForm();
         }

         return form;
      }
      /// <summary>
      ///   Creates the window defined by this form and its child controls
      /// </summary>
      protected override void createForm()
      {
         base.createForm();
         ClientManager.Instance.CreatedForms.add(this);
         base.SetActiveHighlightRowState(false);

      }
      /// <summary>
      /// prepare controls 
      /// </summary>
      /// <param name="task"></param>
      /// <returns></returns>
      public override void InitFormBeforeRefreshDisplay()
      {
         Task task = (Task)getTask();

         if (task.IsOffline)
         {

            // the all images send by studio value and we need to update the value by the studio value of the property 
            if (checkIfExistProp(PropInterface.PROP_TYPE_WALLPAPER))
               Property.UpdateValByStudioValue(this, PropInterface.PROP_TYPE_WALLPAPER);

            for (int i = 0; i < CtrlTab.getSize(); i++)
            {
               MgControlBase ctrl = CtrlTab.getCtrl(i);
               if (ctrl.checkIfExistProp(PropInterface.PROP_TYPE_IMAGE_FILENAME))
                  Property.UpdateValByStudioValue(ctrl, PropInterface.PROP_TYPE_IMAGE_FILENAME);
            }
         }
      }

      #region Nested type: ParkableControlInfo

      /// <summary>
      /// 
      /// </summary>
      private class ParkableControlInfo
      {
         internal MgForm BaseForm { get; private set; }
         internal char Direction { get; private set; }
         internal bool ParkableControlWasFound { get; set; }

         internal ParkableControlInfo(MgForm baseForm, char direction)
         {
            BaseForm = baseForm;
            Direction = direction;
         }
      }
      #endregion

      /// <summary> Initialize the status bar</summary>
      protected override bool InitStatusBar(bool createAllPanes)
      {
         createAllPanes = ClientManager.Instance.getEnvironment().getSpecialShowStatusBarPanes();
         //Create status bar and get its index.
         bool statusBarCreated = base.InitStatusBar(createAllPanes);

         if (statusBarCreated)
         {
            //Add the panes to the status bar specific to RC.
            int imagePaneIdx = (createAllPanes ?
                                 GuiConstants.TOTAL_TASKINFO_PANES + ConstInterface.SB_IMG_PANE_LAYER + 1 :
                                 ConstInterface.SB_IMG_PANE_LAYER);

            //Add image pane for showing icon or image.
            _statusBar.AddPane(MgControlType.CTRL_TYPE_SB_IMAGE, imagePaneIdx, ConstInterface.SB_IMG_PANE_WIDTH, false);

            //The image of this pane should always be from client resources.
            MgControlBase imagePane = _statusBar.getPane(imagePaneIdx);
            var num = new NUM_TYPE();
            num.NUM_4_LONG((int)ExecOn.Client);
            imagePane.setProp(PropInterface.PROP_TYPE_LOAD_IMAGE_FROM, num.toXMLrecord());
         }

         return statusBarCreated;
      }

      /// <summary>Writes the string or sets the image on the pane with the specified index.</summary>
      /// <param name="paneIdx">Index of the pane to be updated</param>
      /// <param name="strInfo">Message</param>
      /// <param name="soundBeep">Sound beep or not</param>
      public override void UpdateStatusBar(int paneIdx, string strInfo, bool soundBeep)
      {
         if (paneIdx == Constants.SB_MSG_PANE_LAYER || ClientManager.Instance.getEnvironment().getSpecialShowStatusBarPanes())
            base.UpdateStatusBar(paneIdx, strInfo, soundBeep);
      }

      /// <summary> returns the string to be displayed in status bar Panes</summary>
      /// <param name="paneIdx"></param>
      /// <param name="on"></param>
      internal String GetMessgaeForStatusBar(int paneIdx, bool on)
      {
         String displayStr = String.Empty;
         switch (paneIdx)
         {
            case GuiConstants.SB_WIDEMODE_PANE_LAYER:
               if (on)
                  displayStr = ConstInterface.MG_WIDE;
               break;

            case GuiConstants.SB_ZOOMOPTION_PANE_LAYER:
               if (on)
                  displayStr = ConstInterface.MG_ZOOM;
               break;

            case GuiConstants.SB_INSMODE_PANE_LAYER:
               if (on)
                  displayStr = ConstInterface.MG_INSERT;
               else
                  displayStr = ConstInterface.MG_OVERWRITE;
               break;
         }
         return displayStr;
      }

      /// <summary> refresh all panes of status bar.</summary>
      public void RefreshStatusBar()
      {
         UpdateStatusBar(GuiConstants.SB_TASKMODE_PANE_LAYER, ((Task)_task).getModeString(), false);

         UpdateStatusBar(GuiConstants.SB_ZOOMOPTION_PANE_LAYER, GetMessgaeForStatusBar(GuiConstants.SB_ZOOMOPTION_PANE_LAYER,
                                _task.ActionManager.isEnabled(InternalInterface.MG_ACT_ZOOM)), false);

         UpdateStatusBar(GuiConstants.SB_WIDEMODE_PANE_LAYER, GetMessgaeForStatusBar(GuiConstants.SB_WIDEMODE_PANE_LAYER,
                                _task.ActionManager.isEnabled(InternalInterface.MG_ACT_WIDE)), false);
      }


      /// <summary>
      /// init the recompute table for that form
      /// </summary>
      /// <param name="foundTagName"></param>
      /// <returns></returns>
      protected override bool initInnerObjects(string foundTagName)
      {
         if (foundTagName == null)
            return false;

         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         if (foundTagName.Equals(XMLConstants.MG_TAG_RECOMPUTE))
         {
            Logger.Instance.WriteDevToLog("goes to recompute form");
            ((Task)_task).RecomputeFillData();
            return true;
         }

         return base.initInnerObjects(foundTagName);
      }

      public override string ToString()
      {
         return "{" + GetType().Name + ": task=" + _task + ", Id=" + this.UserStateId + "}";
      }

      /// <summary>
      /// build the XML entry of the hidden controls
      /// </summary>
      /// <returns></returns>
      public string GetHiddenControlListXML()
      {
         StringBuilder xml = new StringBuilder();

         if (hiddenControlsIsnsList != null && hiddenControlsIsnsList.Count > 0)
         {
            //write the id
            xml.Append("<" + ConstInterface.MG_TAG_HIDDEN_CONTOLS + " " + ConstInterface.MG_ATTR_ISNS + "=\"");

            // loop on all changed variables
            foreach (int change in hiddenControlsIsnsList)
            {
               xml.Append(change + ",");
            }
            xml.Remove(xml.Length - 1, 1);

            xml.Append("\"/>");

            hiddenControlsIsnsList = null;
         }

         return xml.ToString();
      }

      /// <summary>
      /// build the list of controls hidden by the runtime designer, to be sent to the server later
      /// </summary>
      protected override void UpdateHiddenControlsList()
      {
         hiddenControlsIsnsList = new List<int>();
         foreach (KeyValuePair<MgControlBase, Dictionary<string, object>> item in DesignerInfoDictionary)
         {
            if (item.Value.ContainsKey(Constants.WinPropVisible))
               hiddenControlsIsnsList.Add(item.Key.ControlIsn);
         }
      }

      /// <summary>
      /// Should this form behave as Modal although its WindowType != Modal? 
      /// For RIA, Non Interactive task's and its all child task should behave as a Modal.
      /// </summary>
      /// <returns></returns>
      protected override bool ShouldBehaveAsModal()
      {
#if PocketPC
         // On Mobile, mixing modal and modeless forms may result in bad behavior - forms may be hidden/closed
         // when they shouldn't. Workaround: always make the forms modal. Since the forms always cover the
         // entire desktop area, and there is no way to get to the forms behind the current form, this change 
         // shouldn't cause problems.
         return true;
#else
         bool shouldBeModal = false;

         if (!IsHelpWindow)
         {
            //QCR #802495, non interactive tasks and its children will be opened using show dialog to prevent events on their ancestors
            if (!_task.IsInteractive && !_task.isMainProg())
               shouldBeModal = ((Task)_task).ShouldNonInteractiveBeModal();
            else
            {
               //if (ParentForm == null)// The parent task is non interactive
               //{
               //   if (!getTask().isParentMainPrg())
               //      isDialog = true;
               //}
               //else
               {
                  Task parentTask = ((Task)_task).ParentTask;
                  if (parentTask != null && !parentTask.IsInteractive && !parentTask.isMainProg())
                     shouldBeModal = parentTask.ShouldNonInteractiveChildBeModal();
               }
            }
         }

         return shouldBeModal;
#endif
      }

      /// <summary>
      /// Check whether table being used with absolute scrollbar thumb.
      /// </summary>      
      public bool isTableWithAbsoluteScrollbar()
      {
         bool tableWithAbsoluteScrollbar = false;

         if (_tableMgControl != null)
            tableWithAbsoluteScrollbar = ((MgControl)_tableMgControl).IsTableWithAbsoluteScrollbar();

         return tableWithAbsoluteScrollbar;
      }

      /// <summary>
      /// Update recordsBeforeCurrentView
      /// </summary>
      /// <param name="value"></param>
      private void SetRecordsBeforeCurrentView(int value)
      {
         if (_tableMgControl != null)
         {
            Commands.addAsync(CommandType.SET_RECORDS_BEFORE_CURRENT_VIEW, _tableMgControl, value, 0);
         }
      }
   }
}
