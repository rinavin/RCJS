using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.win32;
using util.com.magicsoftware.util;
using Gui.com.magicsoftware.unipaas.management.gui;
using System.IO;
using System.Globalization;
#if !PocketPC
using RuntimeDesigner.Serialization;
#endif
namespace com.magicsoftware.unipaas.management.exp
{
   public abstract class GuiExpressionEvaluator
   {
      protected const int TRIGGER_TASK = 500000;

      enum ControlsPersistencyClearOption
      {
         All = 1,
         CurrentForm = 2
      }

      protected TaskBase ExpTask { get; set; }

      protected abstract ExpVal GetItemVal(int itm);
      protected abstract void SetItemVal(int itm, Object valueToSet);
      protected abstract ITask GetLastFocusedTask();
      protected abstract List<MgFormBase> GetTopMostForms(Int64 contextId);
      protected abstract bool HandleControlGoto(ITask task, MgControlBase ctrl, int rowNo);
      protected abstract string Translate(string name);
      protected abstract void EditGet(MgControlBase ctrl, ref ExpVal isNull);

      #region Expression Evaluation

      /// <summary>(protected)
      ///   finds the handle of the window control associated with the magic control.
      /// </summary>
      /// <param name = "resVal"> Contains the value passed by the user.</param>
      /// <param name = "ctrlName">Contains return value.</param>
      protected void eval_op_ctrlhandle(ExpVal resVal, ExpVal ctrlName)
      {
         int ctrlHandle = 0;

         //Getting magic form.
         MgFormBase form = ((TaskBase)ExpTask.GetContextTask()).getForm();

         if (form != null)
         {
            //Getting control on that form.
            MgControlBase ctrl = form.GetCtrl(ctrlName.ToMgVal());
            if (ctrl != null)
               ctrlHandle = Commands.getCtrlHandle(ctrl, ctrl.getDisplayLine(true));
         }

         //Assigning handle value to resVal.
         ConstructMagicNum(resVal, ctrlHandle, StorageAttribute.NUMERIC);
      }

      /// <summary>(protected)
      /// returns the position(X/Y) of the last click, relative to a control or window according to opCode.
      /// The value is in pixels and not in Magic Units
      /// </summary>
      protected void eval_op_lastclick(ExpVal resVal, int opcode)
      {
         resVal.Attr = StorageAttribute.NUMERIC;
         resVal.MgNumVal = new NUM_TYPE();
         int uom = -1;
         //Getting magic form.
         TaskBase currTask = ((TaskBase)ExpTask.GetContextTask());
         MgFormBase form = currTask.getForm();

         if (form == null)
         {
            currTask = (TaskBase)currTask.GetAncestorTaskContainingForm();
            form = (currTask != null) ? currTask.getForm() : null;
         }

         RuntimeContextBase runtimeContext = Manager.GetCurrentRuntimeContext();
         if (form != null)
         {
            switch (opcode)
            {
               case ExpressionInterface.EXP_OP_CLICKWX:
                  uom = form.pix2uom(runtimeContext.GetClickProp(0), true);
                  break;

               case ExpressionInterface.EXP_OP_CLICKWY:
                  uom = form.pix2uom(runtimeContext.GetClickProp(1), false);
                  break;

               case ExpressionInterface.EXP_OP_CLICKCX:
                  uom = runtimeContext.GetClickProp(2);
                  if (runtimeContext.LastClickCoordinatesAreInPixels)
                     uom = form.pix2uom(uom, true);
                  break;

               case ExpressionInterface.EXP_OP_CLICKCY:
                  uom = runtimeContext.GetClickProp(3);
                  if (runtimeContext.LastClickCoordinatesAreInPixels)
                     uom = form.pix2uom(uom, false);
                  break;
            }
         }
         resVal.MgNumVal.NUM_4_LONG(uom);
      }


      /// <summary>(protected)
      /// return control size
      /// </summary>
      /// <param name="resVal">result </param>
      /// <param name="val1">control name </param>
      /// <param name="val2">generation </param>
      /// <param name="opCode">opcode which specifies required size (height/width/top/left etc)</param>
      protected void GetCtrlSize(ExpVal resVal, ExpVal val1, ExpVal val2, int opCode)
      {
         if (val2.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.NUMERIC);
            return;
         }

         String ctrlName = StrUtil.ZstringMake(val1.StrVal, val1.StrVal.Length);
         int generation = val2.MgNumVal.NUM_2_LONG();

         TaskBase ancestorTask = GetContextTask(generation);

         int res = Manager.GetControlFocusedData(ancestorTask, opCode, ctrlName);
         ConstructMagicNum(resVal, res, StorageAttribute.NUMERIC);
      }

      /// <summary>(protected)
      /// sets the status bar text with the given text and
      /// sets the result with the last text that was set to the status bar by the use of this function
      /// </summary>
      /// <param name = "resVal">last text of status bar</param>
      /// <param name = "statusBarText">the text to be set at status bar</param>
      protected void eval_op_statusbar_set_text(ExpVal resVal, ExpVal statusBarText)
      {
         resVal.Attr = StorageAttribute.UNICODE;
         resVal.StrVal = Manager.GetCurrentRuntimeContext().DefaultStatusMsg;
         if (Manager.GetCurrentRuntimeContext().DefaultStatusMsg == null)
            resVal.StrVal = "";
         else
            resVal.StrVal = Manager.GetCurrentRuntimeContext().DefaultStatusMsg;
         resVal.IsNull = false;

         if ((statusBarText.Attr != StorageAttribute.ALPHA) &&
             (statusBarText.Attr != StorageAttribute.UNICODE))
            return;

         if (!statusBarText.IsNull)
         {
            string text = statusBarText.StrVal;
            TaskBase task = (TaskBase)ExpTask.GetContextTask();

            // In case of non interactive tasks, it is possible that task has open window = No, So display the status bar msg on frame window.
            if (task.getForm() == null && Manager.GetCurrentRuntimeContext().FrameForm != null)
               task = Manager.GetCurrentRuntimeContext().FrameForm.getTask();

            Manager.WriteToMessagePane(task, text, false);
            Manager.GetCurrentRuntimeContext().DefaultStatusMsg = text;
         }
      }

      /// <summary>(protected)
      /// returns the handle of the window form depending on the generation user has specified
      /// </summary>
      /// <param name = "resVal">return value(window handle)</param>
      /// <param name = "generation">generation</param>
      protected void eval_op_formhandle(ExpVal resVal, ExpVal generation)
      {
         int handle = 0;

         if (generation.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.NUMERIC);
            return;
         }

         int parent = generation.MgNumVal.NUM_2_LONG();

         var currTask = (TaskBase)ExpTask.GetContextTask();
         if ((parent >= 0 && parent < currTask.GetTaskDepth()) || parent == TRIGGER_TASK)
         {
            //Getting the parent task of context task based on generation.
            TaskBase tsk = GetContextTask(parent);

            //Getting form associated with the task.
            MgFormBase form = tsk.getForm();

            if (form != null)
            {
               //If form is a sub form then we have to get the the handle of the control as subform is a control on the main form.
               if (form.isSubForm())
               {
                  MgControlBase ctrl = form.getSubFormCtrl();
                  handle = Commands.getCtrlHandle(ctrl, ctrl.getDisplayLine(true));
               }
               else
                  handle = Commands.getFormHandle(form);
            }
         }

         //Assigning handle value to resVal.
         ConstructMagicNum(resVal, handle, StorageAttribute.NUMERIC);
      }

      /// <summary>(protected)
      /// sets the cursor shape
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="cursorShapeNo">cursor shape number</param>
      protected void eval_op_setcrsr(ExpVal resVal, ExpVal cursorShapeNo)
      {
         if (cursorShapeNo.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.BOOLEAN);
            return;
         }

         int val = cursorShapeNo.MgNumVal.NUM_2_LONG();
         //this values are allowed by magic
         bool ret = Commands.setCursor((MgCursors)val);

         resVal.BoolVal = ret;
         resVal.Attr = StorageAttribute.BOOLEAN;
      }

      protected virtual bool IsParallel(TaskBase task)
      {
         return false;
      }
      /// <summary>(protected)
      ///  returns a window dimension, X position, Y position, Width or Height.
      /// </summary>
      /// <param name="resVal">window dimension</param>
      /// <param name="val1">generation</param>
      /// <param name="val2">dimension char (W/H/X/Y)</param>
      protected virtual void eval_op_win_box(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         if (val1.MgNumVal == null || val2.StrVal == null)
         {
            SetNULL(resVal, StorageAttribute.NUMERIC);
            return;
         }

         int parent = val1.MgNumVal.NUM_2_LONG();
         ConstructMagicNum(resVal, 0, StorageAttribute.NUMERIC);

         // Check Overflow
         if (!val1.MgNumVal.num_is_zero() && parent == 0)
            return;

         int len = val2.StrVal.Length;
         var currTask = (TaskBase)ExpTask.GetContextTask();

         if (((parent >= 0 && parent < currTask.GetTaskDepth()) || parent == TRIGGER_TASK) && len == 1)
         {
            string temp = val2.StrVal;
            char s = Char.ToUpper(temp[0]);
            TaskBase tsk = GetContextTask(parent);
         
            // if the task's window wasn't opened - return 0
            //if we get to the main program of parallel task - return 0 as in 1.9
            if (tsk == null || tsk.getForm() == null || ( tsk.isMainProg() && IsParallel(tsk)))
               resVal.MgNumVal.NUM_SET_ZERO();
            else
            {
             
               if (s == 'X' || s == 'Y' || s == 'W' || s == 'H')
                  len = Manager.WinPropGet(tsk.getForm(), s);
               else
                  return;
               resVal.MgNumVal.NUM_4_LONG(len);
            }
         }
      }

      /// <summary>(protected)
      /// set a window state (minimize/maximize/restore) for a form
      /// the behavior will be by following:
      /// Minimized: will minimized all forms that exist until the SDI
      /// Restore\Maximized: will maximized on the SDI only.
      /// </summary>
      /// <param name = "opCode">opcode</param>
      protected bool SetWindowState(int opCode)
      {
         bool addCommand = false;
         var currTask = (TaskBase)ExpTask.GetContextTask();
         MgFormBase form = currTask.getForm();

         if (form != null)
         {
            int windowType = Styles.WINDOW_STATE_RESTORE;
            if (opCode == ExpressionInterface.EXP_OP_MAXMAGIC)
               windowType = Styles.WINDOW_STATE_MAXIMIZE;
            else if (opCode == ExpressionInterface.EXP_OP_MINMAGIC)
               windowType = Styles.WINDOW_STATE_MINIMIZE;

#if PocketPC
            addCommand = true;
            Commands.addAsync(CommandType.SET_WINDOW_STATE, form, 0, windowType);
#else
            // the minimized will minimized all the forms, and the SDI
            // restore\maximized will work only for SDI/MDI state            
            form = currTask.getForm().getTopMostFrameForm();
            if (form != null)
            {
               if (windowType == Styles.WINDOW_STATE_MINIMIZE)
               {
                  // If we try to minimize the SDI/MDI frame window from modal program then it closes the modal window(.net behavior).
                  // Minimize the frame window when there is no modal window opened as a child.
                  // #178324. If the current task is SDI one then add a command since minimizing an SDIFrame doesn't close 
                  // it although its opened as a modal (using ShowDialog).
                  if (currTask.getForm().IsSDIFrame || form.ModalFormsCount == 0)
                     addCommand = true;
               }
               else
               {
                  if (form.IsMDIOrSDIFrame)
                     addCommand = true;
               }

               if (addCommand)
                  Commands.addAsync(CommandType.SET_WINDOW_STATE, form, 0, windowType);
            }
#endif
         }

         return addCommand;
      }

      /// <summary>
      ///   for Browser Control only: get the text from the browser Control
      /// </summary>
      protected void eval_op_browserGetContent(ExpVal resVal, ExpVal controlName)
      {
         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = "";

         //Find the browser control by the control name
         MgFormBase form = (MgFormBase)((TaskBase)ExpTask).getForm();
         string sCtrlName = StrUtil.rtrim(controlName.StrVal);
         MgControlBase control = ((TaskBase)ExpTask).getForm().GetCtrl(sCtrlName);
         if (control != null) //TODO :&& browserControl.getType() == CTRL_TYPE_BROWSER)     
            resVal.StrVal = Commands.getBrowserText(control);
      }


      /// <summary>
      ///   for Browser Control only: set the text on the browser Control
      /// </summary>
      protected void eval_op_browserSetContent(ExpVal resVal, ExpVal controlName, ExpVal text)
      {
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = false;

         //Find the browser control by the control name
         MgFormBase form = (MgFormBase)((TaskBase)ExpTask).getForm();
         string sCtrlName = StrUtil.rtrim(controlName.StrVal);
         MgControlBase control = ((TaskBase)ExpTask).getForm().GetCtrl(sCtrlName);
         if (control != null) //TODO :&& browserControl.getType() == CTRL_TYPE_BROWSER)      
         {
            resVal.BoolVal = Commands.setBrowserText(control, text.StrVal);
         }
      }


      protected void eval_op_browserExecute_DO(ExpVal resVal, ExpVal controlName, ExpVal text, ExpVal sync, String language)
      {
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = false;

         //Find the browser control by the control name
         MgFormBase form = (MgFormBase)((TaskBase)ExpTask).getForm();
         string sCtrlName = StrUtil.rtrim(controlName.StrVal);
         MgControlBase control = ((TaskBase)ExpTask).getForm().GetCtrl(sCtrlName);
         if (control != null && control.isBrowserControl())
            resVal.BoolVal = Commands.browserExecute(control, text.StrVal, sync.BoolVal, language);
      }
  
      /// <summary>(protected)
      /// return the control name of last clicked control
      /// </summary>
      protected void eval_op_ctrl_name(ExpVal resVal)
      {
         String szName = Manager.GetCurrentRuntimeContext().LastClickedCtrlName;
         resVal.StrVal = (string.IsNullOrEmpty(szName)
                            ? ""
                            : szName);
         resVal.Attr = StorageAttribute.ALPHA;
      }

      /// <summary>(protected)
      /// returns name of the control on which the user was last parked in a task
      /// </summary>
      /// <param name="resVal">control name</param>
      /// <param name="val1">generation</param>
      protected void eval_op_last_parked(ExpVal resVal, ExpVal val1)
      {
         var currTask = (TaskBase)ExpTask.GetContextTask();

         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = "";

         String str = "";
         int generation = val1.MgNumVal.NUM_2_LONG();
         if (generation != TRIGGER_TASK)
         {
            if (generation < currTask.GetTaskDepth())
               str = currTask.GetLastParkedCtrlName(generation);
         }
         else // THIS() used.
         {
            TaskBase triggeredTsk = GetContextTask(generation); //find triggered task
            if (triggeredTsk != null)
               str = triggeredTsk.GetLastParkedCtrlName(0);
         }

         if (!string.IsNullOrEmpty(str))
            resVal.StrVal = str;
      }

      /// <summary>(protected)
      /// returns current caret position
      /// </summary>
      /// <param name="res">caret position</param>
      protected void eval_op_caretPosGet(ExpVal res)
      {
         //result is logical
         res.Attr = StorageAttribute.NUMERIC;
         res.IsNull = false;
         res.MgNumVal = new NUM_TYPE();

         var lastFocusedTask = (TaskBase)GetLastFocusedTask();

         if (lastFocusedTask == null || (ExpTask.ContextID != lastFocusedTask.ContextID))
            return;

         MgControlBase currCtrl = lastFocusedTask.getLastParkedCtrl();

         // Make sure a control exists, the task is in the control level and that we are allowed to update it
         if (currCtrl != null && currCtrl.isTextOrTreeEdit() && currCtrl.InControl)
            res.MgNumVal.NUM_4_LONG(Manager.CaretPosGet(currCtrl) + 1);
      }

      /// <summary>(protected)
      /// marks the text of control
      /// </summary>
      /// <param name="startPosVal">starting position in text to be marked</param>
      /// <param name="lenVal">number of characters to be marked</param>
      /// <param name="res">number of characters marked</param>
      protected void eval_op_markText(ExpVal startPosVal, ExpVal lenVal, ExpVal res)
      {
         // Result is logical
         res.Attr = StorageAttribute.NUMERIC;
         res.IsNull = false;
         res.MgNumVal = new NUM_TYPE();

         //null inputs are not allowed
         if (startPosVal.IsNull || lenVal.IsNull)
            return;

         var lastFocusedTask = (TaskBase)GetLastFocusedTask();

         if (lastFocusedTask == null || (ExpTask.ContextID != lastFocusedTask.ContextID))
            return;

         MgControlBase currCtrl = lastFocusedTask.getLastParkedCtrl();

         int startPos = startPosVal.MgNumVal.NUM_2_LONG();
         int len = lenVal.MgNumVal.NUM_2_LONG();

         //Select in "reverse" mode (backwards instead of forward)
         if (len < 0)
         {
            startPos = startPos + len;
            len = -len;
         }
         //Make sure the selection is in the control's boundaries and that the control is of the right type
         if (currCtrl != null && startPos > 0 && len != 0 && currCtrl.isTextOrTreeEdit() && currCtrl.InControl)
         {
            String val = Manager.GetCtrlVal(currCtrl);
            int ctrlLen = val.Length;
            if (startPos <= ctrlLen)
            {
               if (startPos + len - 1 > ctrlLen)
                  len = ctrlLen - startPos + 1;

               Manager.MarkText(currCtrl, startPos - 1, len);
               res.MgNumVal.NUM_4_LONG(len);
            }
         }
      }

      /// <summary>(protected)
      /// replaces the marked text with a specified string
      /// </summary>
      /// <param name="strToSet">new text</param>
      /// <param name="res">returns marked text has been replaced or not</param>
      protected void eval_op_markedTextSet(ExpVal strToSet, ExpVal res)
      {
         //result is logical
         res.Attr = StorageAttribute.BOOLEAN;
         res.IsNull = false;
         res.BoolVal = false;

         //null input not allowed
         if (strToSet.IsNull || strToSet.StrVal == null)
            return;

         var lastFocusedTask = (TaskBase)GetLastFocusedTask();

         if (lastFocusedTask == null || (ExpTask.ContextID != lastFocusedTask.ContextID))
            return;

         MgControlBase currCtrl = lastFocusedTask.getLastParkedCtrl();

         String str = strToSet.StrVal;

         //Make sure a control exists, the task is in the control level and that we are allowed to update it
         if (currCtrl != null && currCtrl.isTextOrTreeEdit() && currCtrl.InControl && currCtrl.IsParkable(false) &&
             currCtrl.isModifiable())
         {
            res.BoolVal = Manager.MarkedTextSet(currCtrl, str);
         }
      }

      /// <summary>(protected)
      /// returns marked text
      /// </summary>
      /// <param name="res">marked text</param>
      protected void eval_op_markedTextGet(ExpVal res)
      {
         //result is logical
         res.Attr = StorageAttribute.ALPHA;
         res.IsNull = true;
         res.StrVal = null;

         var lastFocusedTask = (TaskBase)GetLastFocusedTask();

         if (lastFocusedTask == null || (ExpTask.ContextID != lastFocusedTask.ContextID))
            return;

         MgControlBase currCtrl = lastFocusedTask.getLastParkedCtrl();

         //Make sure a control exists, the task is in the control level and that we are allowed to update it
         if (currCtrl != null && currCtrl.isTextOrTreeEdit() && currCtrl.InControl)
         {
            res.StrVal = Manager.MarkedTextGet(currCtrl);
            if (!string.IsNullOrEmpty(res.StrVal))
               res.IsNull = false;
            else
               res.StrVal = null;
         }
      }

      /// <summary>(protected)
      /// returns the value of a last focused control
      /// <param name="resVal">control value</param>
      /// </summary>
      protected void eval_op_editget(ExpVal resVal)
      {
         MgControlBase currCtrl = null;
         var lastFocusedTask = (TaskBase)GetLastFocusedTask();

         if (lastFocusedTask != null)
            currCtrl = lastFocusedTask.getLastParkedCtrl();

         // Needs to check context because in case of parallel program, if handler(eg. timer) that executing 
         // the expression is running in different context than active context, then function should not work.
         if (currCtrl == null || !currCtrl.InControl || (ExpTask.ContextID != lastFocusedTask.ContextID))
         {
            // Return blank
            resVal.Attr = StorageAttribute.ALPHA;
            resVal.StrVal = "";
            resVal.IsNull = false;
            return;
         }

         EditGet(currCtrl, ref resVal);
      }

      /// <summary>(protected)
      ///   updates the value of a last focused control while in edit mode
      /// </summary>
      protected void eval_op_editset(ExpVal val1, ExpVal resVal)
      {
         resVal.Attr = StorageAttribute.BOOLEAN;

         MgControlBase currCtrl = null;
         var lastFocusedTask = (TaskBase)GetLastFocusedTask();

         if (lastFocusedTask != null)
            currCtrl = lastFocusedTask.getLastParkedCtrl();

         // Needs to check context because in case of parallel program, if handler(eg. timer) that executing 
         // the expression is running in different context than active context, then function should not work.
         if (currCtrl == null || !currCtrl.InControl || (ExpTask.ContextID != lastFocusedTask.ContextID))
         {
            resVal.BoolVal = false;
            return;
         }

         if (val1.Attr == StorageAttribute.NONE)
         {
            Events.WriteExceptionToLog("ExpressionEvaluator.eval_op_editset() there is no such type of variable");
            resVal.BoolVal = false;
            return;
         }

         Field currFld = currCtrl.getField();
         ConvertExpVal(val1, currFld.getType());

         if (val1.IsNull || StorageAttributeCheck.isTheSameType(val1.Attr, currFld.getType()))
         {
            String mgVal = val1.ToMgVal();

            //For all choice controls, the VC should be executed immediately. So, we should directly 
            //update the variable's value (In 1.9 OL, we called eval_op_varset())
            if (currCtrl.isSelectionCtrl() || currCtrl.isTabControl() || currCtrl.isRadio() || currCtrl.isCheckBox())
               lastFocusedTask.UpdateFieldValueAndStartRecompute(currFld, mgVal, val1.IsNull);
            else
            {
               //If the new value is null, get the null/default value of the field and set it on the control.
               //VC will be fired when we leave the control.
               if (val1.IsNull)
                  mgVal = currCtrl.getField().getDefaultValue();

               currCtrl.getField().updateDisplay(mgVal, val1.IsNull, true);
               currCtrl.ModifiedByUser = true;
            }

            resVal.BoolVal = true;
         }
         else
            resVal.BoolVal = false;
      }
      /// <summary>
      /// PixelsToFormUnits
      /// </summary>
      /// <param name="pixels"></param>
      /// <param name="isX"></param>
      /// <param name="resVal"></param>
      protected void eval_op_PixelsToFormUnits(ExpVal pixels, ExpVal isX, ExpVal resVal)
      {
         double pix = pixels.MgNumVal.to_double();
         bool isXcoordinate = isX.BoolVal;
         resVal.MgNumVal = new NUM_TYPE();

         resVal.Attr = StorageAttribute.NUMERIC;

         MgFormBase currentForm = ExpTask.getForm();
          pix = pix * ((float)Commands.getResolution(currentForm.getMapObject()).x / 96);

         double uom = (double)currentForm.pix2uom(pix, isXcoordinate);
         resVal.MgNumVal = NUM_TYPE.from_double(isXcoordinate ? uom / (double)currentForm.getHorizontalFactor() : uom / (double)currentForm.getVerticalFactor());
      }

      /// <summary>
      /// FormUnitsToPixels
      /// </summary>
      /// <param name="uom"></param>
      /// <param name="isX"></param>
      /// <param name="resVal"></param>
      protected void eval_op_FormUnitsToPixels(ExpVal uom, ExpVal isX, ExpVal resVal)
      {
         double inUOM = uom.MgNumVal.to_double();
         bool isXcoordinate = isX.BoolVal;
         resVal.MgNumVal = new NUM_TYPE();

         resVal.Attr = StorageAttribute.NUMERIC;

         MgFormBase currentForm = ExpTask.getForm();
         inUOM = inUOM / ((float)Commands.getResolution(currentForm.getMapObject()).x / 96);

         double pix = (double)currentForm.uom2pix(inUOM, isXcoordinate);
         resVal.MgNumVal = NUM_TYPE.from_double(isXcoordinate ? pix * (double)currentForm.getHorizontalFactor() : pix * (double)currentForm.getVerticalFactor());
      }
      

      /// <summary>
      /// 
      /// </summary>
      /// <param name="optionExpVal"></param>
      /// <param name="resVal"></param>
      protected void eval_op_ControlsPersistencyClear(ExpVal optionExpVal, ExpVal restoreDeletedControlsExpVal, ExpVal resVal)
      {
         EnvControlsPersistencyPath envControlsPersistencyPath = EnvControlsPersistencyPath.GetInstance();
         bool success = false;
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = success;
#if !PocketPC
         int option = optionExpVal.MgNumVal.NUM_2_LONG();
         bool restoreDeletedControl = restoreDeletedControlsExpVal.BoolVal;

         if (option == (int)ControlsPersistencyClearOption.All || option == (int)ControlsPersistencyClearOption.CurrentForm)
         {
            ControlsPersistencyClearOption optionControlsPersistency = (ControlsPersistencyClearOption)option;

            success = true;
            switch (optionControlsPersistency)
            {
               case ControlsPersistencyClearOption.All:
                  {
                     String fullPath = envControlsPersistencyPath.GetFullPath(this.ExpTask.ApplicationGuid);

                     if (restoreDeletedControl)
                     {
                        if (HandleFiles.isExists(fullPath))
                        {
                           if (!HandleFiles.deleteDir(fullPath))
                              success = false;
                        }
                     }
                     else
                     {
                        String[] directories = Directory.GetDirectories(fullPath);
                        foreach (String directory in directories)
                        {
                           DirectoryInfo directoryInfo = new DirectoryInfo(directory);
                           // if the directory started with prg_ it is magic directory
                           if (directoryInfo.Name.StartsWith(EnvControlsPersistencyPath.PreffixControlsPersistencyProgramDirectory))
                           {
                              String[] files = Directory.GetFiles(directory);
                              foreach (String fullFileName in files)
                              {
                                 String fileName = Path.GetFileName(fullFileName);
                                 if (fileName.StartsWith(EnvControlsPersistencyPath.PreffixControlsPersistencyFileName))
                                    success = success & RuntimeDesignerSerializer.ControlsPersistencyClearPropertiesAndSaveVisiblePropertyOnly(fullFileName);
                              }
                           }
                        }
                     }
                  } break;

               case ControlsPersistencyClearOption.CurrentForm:
                  {
                     String fileName = envControlsPersistencyPath.GetFullControlsPersistencyFileName(this.ExpTask.getForm());
                     if (restoreDeletedControl)
                     {
                        if (File.Exists(fileName))
                           if (!(HandleFiles.deleteFile(fileName)))
                              success = false;
                     }
                     else
                        success = RuntimeDesignerSerializer.ControlsPersistencyClearPropertiesAndSaveVisiblePropertyOnly(fileName);
                  } break;

               default:
                  success = false;
                  break;
            }

         }
#endif
         resVal.BoolVal = success;

      }


      /// <summary>(protected)
      ///   Clears the form user state
      /// </summary>
      /// <param name="formName">form name (current form('') /all forms(*)) </param>
      /// <param name="resVal">succeed or failed</param>
      protected void eval_op_formStateClear(ExpVal formName, ExpVal resVal)
      {
         const String ALL_FORMS = "*";
         const String CURRENT_FORM = "";

         FormUserState formUserState = FormUserState.GetInstance();

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = false;

         // if the userState is disabled, we should not execute this function.
         if (formUserState.IsDisabled)
            return;

         String formNameStr = formName.ToMgVal();

         if (formNameStr == CURRENT_FORM)
         {
            var lastFocussedTask = (TaskBase)GetLastFocusedTask();
            MgFormBase currForm = lastFocussedTask.getForm();

            if (!String.IsNullOrEmpty(currForm.UserStateId))
            {
               // delete userState of current form
               formUserState.Delete(currForm.UserStateId);

               // restore the state of current form
               formUserState.ApplyDefault(currForm);

               resVal.BoolVal = true;
            }
         }
         else if (formNameStr == ALL_FORMS)
         {
            // delete all forms
            formUserState.DeleteAll();

            // restore the state of all opened forms
            List<MgFormBase> forms = GetTopMostForms(ExpTask.ContextID); // get all the forms in MgDataTable

            foreach (MgFormBase form in forms)
            {
               if (!String.IsNullOrEmpty(form.UserStateId))
                  formUserState.ApplyDefault(form);
            }

            resVal.BoolVal = true;
         }
      }

      /// <summary>(protected)
      /// Moves the caret to specified control
      /// </summary>
      /// <param name = "ctrlName">the name of the destination control</param>
      /// <param name = "rowNum">the row in the table  0 represents current row</param>
      /// <param name = "generation">the task generation</param>
      /// <param name = "retVal">the result</param>
      protected void eval_op_gotoCtrl(ExpVal ctrlName, ExpVal rowNum, ExpVal generation, ExpVal retVal)
      {
         retVal.Attr = StorageAttribute.BOOLEAN;
         retVal.BoolVal = false;

         // get the required task
         TaskBase task = GetContextTask(generation.MgNumVal.NUM_2_LONG());

         if (task == null || task.getForm() == null)
            return;

         int iRowNum = rowNum.MgNumVal.NUM_2_LONG();
         string sCtrlName = StrUtil.rtrim(ctrlName.StrVal);
         MgControlBase ctrl = task.getForm().GetCtrl(sCtrlName);

         retVal.BoolVal = HandleControlGoto(task, ctrl, iRowNum);

         return;
      }

      /// <summary>(protected)
      /// shows the Directory Dialog box and returns the selected directory
      /// </summary>      
      /// <param name = "descriptionVal">description shown in Directory Dialog Box. It can be blank.</param>
      /// <param name = "initDir">the initial path to be shown.</param>
      /// <param name = "showNew">should the dialog show the new folder button.</param>
      /// <param name = "resVal">selected directory</param>
      protected void eval_op_client_dir_dlg(ExpVal descriptionVal, ExpVal initDir, ExpVal showNew, ExpVal resVal)
      {
         resVal.Attr = StorageAttribute.ALPHA;

         // create a gui interactive object to interact with gui and get the result
         String description = exp_build_string(descriptionVal);
         String path = exp_build_ioname(initDir);
         Boolean showNewFolder = showNew.BoolVal;
         resVal.StrVal = Commands.directoryDialogBox(description, path, showNewFolder);
      }

      #region Helper functions for expression evaluation

      /// <summary>(protected)
      /// get context task by a specified generation
      /// </summary>
      /// <param name = "currTask">current task</param>
      /// <param name = "generation">task generation</param>
      /// <returns> task at specified generation</returns>
      public static TaskBase GetContextTask(TaskBase currTask, int generation)
      {
         TaskBase task = null;

         var contextTask = (TaskBase)currTask.GetContextTask();
         if (generation == TRIGGER_TASK) //THIS() function
            task = contextTask;
         else if (generation < contextTask.GetTaskDepth())
            task = (TaskBase)contextTask.GetTaskAncestor(generation);

         return task;
      }

      /// <summary>(protected)
      /// get context task by a specified generation
      /// </summary>
      /// <param name = "generation">task generation</param>
      /// <returns> task at specified generation</returns>
      protected TaskBase GetContextTask(int generation)
      {
         return GetContextTask((TaskBase)ExpTask, generation);
      }


      /// <summary>(protected)
      /// Create a NUM_TYPE with the value of i and set the ExpVal with it
      /// </summary>
      protected void ConstructMagicNum(ExpVal resVal, int i, StorageAttribute attr)
      {
         resVal.MgNumVal = new NUM_TYPE();
         resVal.MgNumVal.NUM_4_LONG(i);
         resVal.Attr = attr;
      }

      /// <summary>(public)
      /// set null value to Expression Evaluator
      /// </summary>
      /// <param name = "resVal">to set it NULL</param>
      /// <param name = "attr">attribute of ExpValue</param>
      public void SetNULL(ExpVal resVal, StorageAttribute attr)
      {
         resVal.IsNull = true;
         resVal.Attr = attr;
         switch (attr)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
               resVal.StrVal = null;
               break;

            case StorageAttribute.TIME:
            case StorageAttribute.DATE:
            case StorageAttribute.NUMERIC:
               resVal.MgNumVal = null;
               break;

            case StorageAttribute.BOOLEAN:
               resVal.BoolVal = false;
               break;

            default:
               break;
         }
      }

      /// <summary>
      ///   validate value of the control
      /// </summary>
      /// <param name = "currCtrl">control whose value need to be evaluated</param>
      /// <param name = "oldValue">old value of the control</param>
      /// <param name = "newValue">new value of the control</param>
      internal String GetValidatedValue(MgControlBase currCtrl, String oldValue, String newValue)
      {
         String ctrlValue;

         //the value needs to be evaluated
         ValidationDetails vd = currCtrl.buildCopyPicture(oldValue, newValue);
         vd.evaluate();

         // The validation of the fields value was wrong
         if (vd.ValidationFailed)
         {
            Field currFld = currCtrl.getField();
            ctrlValue = currFld.getType() == StorageAttribute.BLOB_VECTOR
                           ? currFld.getCellDefualtValue()
                           : currFld.getDefaultValue();
         }
         else
            ctrlValue = vd.getDispValue(); //value after the evaluation

         return ctrlValue;
      }

      /// <summary>
      ///   set expValue of needed type . the value is inited by value
      /// </summary>
      /// <param name = "resVal">need be evaluated/initialized</param>
      /// <param name = "type">of resVal (Alpha|Numeric|Logical|Date|Time|Blob|Vector|Dotnet)</param>
      /// <param name = "val">string/hexa string</param>
      /// <param name = "pic">for string evaluation</param>
      public void SetVal(ExpVal resVal, StorageAttribute type, String val, PIC pic)
      {
         switch (type)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.BLOB:
            case StorageAttribute.BLOB_VECTOR:
            case StorageAttribute.UNICODE:
               resVal.Attr = type;
               resVal.StrVal = val;

               if (type == StorageAttribute.BLOB || type == StorageAttribute.BLOB_VECTOR)
                  resVal.IncludeBlobPrefix = true;
               break;

            case StorageAttribute.NUMERIC:
            case StorageAttribute.DATE:
            case StorageAttribute.TIME:
               resVal.Attr = type;
               if (val == null)
                  resVal.MgNumVal = null;
               else if (pic == null)
                  resVal.MgNumVal = new NUM_TYPE(val);
               else
                  resVal.MgNumVal = new NUM_TYPE(val, pic, (ExpTask).getCompIdx());
               break;

            case StorageAttribute.BOOLEAN:
               resVal.Attr = type;
               resVal.BoolVal = DisplayConvertor.toBoolean(val);
               break;

            case StorageAttribute.DOTNET:
               {
                  resVal.Attr = type;
                  int key = BlobType.getKey(val);
                  resVal.DnMemberInfo = DNManager.getInstance().CreateDNMemberInfo(key);
                  resVal.IsNull = (resVal.DnMemberInfo.value == null);
               }
               break;

            default:
               SetNULL(resVal, type);
               Events.WriteExceptionToLog("ExpressionEvaluator.SetVal() there is no such type : " + type);
               //ClientManager.Instance.WriteErrorToLog("ExpressionEvaluator.SetVal() there is no such type : " + type);
               break;
         }
      }

      /// <summary>(protected)
      /// trims and returns the string.
      /// </summary>
      /// <param name = "val">containing string to be trimmed (strVal_ member of ExpVal)</param>
      /// <returns>trimmed string</returns>
      protected static String exp_build_string(ExpVal val)
      {
         String name = "";

         if (val.StrVal != null)
         {
            long len = val.StrVal.Length;
            if (len > 0)
               name = StrUtil.ZstringMake(val.StrVal, (int)len);
         }
         return name;
      }

      /// <summary>(protected)
      /// translates a filepath (supports logical name)
      /// </summary>
      /// <param name = "val">containing filepath (strVal_ member of ExpVal)</param>
      /// <returns> trimmed and translated string.</returns>
      protected String exp_build_ioname(ExpVal val)
      {
         String name = "";

         if (val.StrVal != null)
         {
            long len = Math.Min(val.StrVal.Length, XMLConstants.FILE_NAME_SIZE - 1);
            if (len > 0)
            {
               String tempName = StrUtil.ZstringMake(val.StrVal, (int)len);
               name = Translate(tempName);
            }
         }
         return name;
      }

      #endregion

      #endregion

      /// <summary>
      /// checks if the object is instance of ExpVal
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      internal static bool isExpVal(Object obj)
      {
         return (obj is ExpVal);
      }

      /// <summary>
      /// converts ExpVal to dotnet object
      /// </summary>
      /// <param name="obj">ExpVal object</param>
      /// <param name="dotNetType">Dotnet type to convert</param>
      /// <returns></returns>
      internal static Object convertExpValToDotNet(Object obj, Type dotNetType)
      {
         Object dnObj = null;

         if (obj is ExpVal)
         {
            var expVal = (ExpVal)obj;

            if (expVal.Attr == StorageAttribute.DOTNET)
               dnObj = expVal.DnMemberInfo.value;
            else
            {
               // get the type in which the ConvertMagicToDotNet can convert expVal.strVal_ into.
               Type typeToConvert = (DNConvert.canConvert(expVal.Attr, dotNetType)
                                        ? dotNetType
                                        : DNConvert.getDefaultDotNetTypeForMagicType(expVal.ToMgVal(), expVal.Attr));
               dnObj = ConvertMagicToDotNet(expVal, typeToConvert);
            }
         }

         return dnObj;
      }

      /// <summary> Converts ExpVal
      /// 1. converts dotnet ExpVal to non-dotnet ExpVal
      /// 2. converts non-dotnet ExpVal to dotnet ExpVal
      /// 3. handle conversions between blob and unicode/alpha
      /// </summary>
      /// <param name="val"></param>
      /// <param name="expectedType"></param>
      protected virtual void ConvertExpVal(ExpVal val, StorageAttribute expectedType)
      {
         if (val.Attr != StorageAttribute.DOTNET && expectedType == StorageAttribute.DOTNET)
         {
            //Convert Magic To DotNet
            object dotNetObj = null;

            if (!val.IsNull)
               dotNetObj = ConvertMagicToDotNet(val,
                                                DNConvert.getDefaultDotNetTypeForMagicType(val.ToMgVal(),
                                                                                           val.Attr));
            val.Nullify();

            var dnMemberInfo = new DNMemberInfo(null, dotNetObj, null, -1, null);
            val.UpdateFromDNMemberInfo(dnMemberInfo);
         }
         else if (val.Attr == StorageAttribute.DOTNET && expectedType != StorageAttribute.DOTNET
                  && expectedType != StorageAttribute.NONE)
         {
            //Convert DotNet to Magic
            String magicVal = null;
            bool isNull = val.IsNull;

            if (!val.IsNull && val.DnMemberInfo.value != null)
            {
               magicVal = DNConvert.convertDotNetToMagic(val.DnMemberInfo.value, expectedType);
               isNull = (magicVal == null);
            }
            else if (ExpTask != null && ExpTask.getNullArithmetic() == Constants.NULL_ARITH_USE_DEF)
            {
               magicVal = FieldDef.getMagicDefaultValue(expectedType);
               isNull = false;
            }

            val.Nullify();

            val.Init(expectedType, isNull, magicVal);
         }
         else if (StorageAttributeCheck.StorageFldAlphaUnicodeOrBlob(val.Attr, expectedType))
            BlobStringConversion(val, expectedType);
      }

      /// <summary>(private)
      /// handle conversions between blob and unicode/alpha
      /// </summary>
      /// <param name="val"></param>
      /// <param name="expectedType"></param>
      private void BlobStringConversion(ExpVal val, StorageAttribute expectedType)
      {
         char contentType;

         if (StorageAttributeCheck.IsTypeAlphaOrUnicode(expectedType))
         {
            if (val.Attr == StorageAttribute.BLOB)
            {
               if (val.IncludeBlobPrefix)
               {
                  contentType = BlobType.getContentType(val.StrVal);

                  val.StrVal = BlobType.getString(val.StrVal);
                  val.IncludeBlobPrefix = false;

                  //QCR#922509: For Rtf text, Binary BLOB to Ansi/Unicode -> should keep the decoration.
                  if (contentType != BlobType.CONTENT_TYPE_BINARY && Rtf.isRtf(val.StrVal))
                     val.StrVal = StrUtil.GetPlainTextfromRtf(val.StrVal);
               }

               val.Attr = expectedType;
            }
            else if (val.Attr == StorageAttribute.BLOB_VECTOR)
            {
               if (val.IncludeBlobPrefix)
               {
                  val.StrVal = BlobType.removeBlobPrefix(val.StrVal);
                  val.IncludeBlobPrefix = false;
               }
               val.Attr = expectedType;
            }
         }
         else if (expectedType == StorageAttribute.BLOB)
         {
            if (StorageAttributeCheck.IsTypeAlphaOrUnicode(val.Attr))
            {
               contentType = val.Attr == StorageAttribute.ALPHA
                                ? BlobType.CONTENT_TYPE_ANSI
                                : BlobType.CONTENT_TYPE_UNICODE;

               val.StrVal = BlobType.createFromString(val.StrVal, contentType);
               val.IncludeBlobPrefix = true;
               val.Attr = expectedType;
            }
         }
      }

      /// <summary> DotNet Member </summary>
      /// <param name = "resVal">Result ExpVal</param>
      /// <param name = "val1">Member reference</param>
      /// <param name = "val2">MemberName</param>
      /// <param name = "val3"></param>
      protected void eval_op_dn_member(ExpVal resVal, ExpVal val1, ExpVal val2, ExpVal val3)
      {
         FieldInfo fieldInfo = null;

         try
         {
            DNMemberInfo parentDNMemberInfo = val1.DnMemberInfo;

            // get the class type
            Type classType;
            if (parentDNMemberInfo.memberInfo is FieldInfo)
               classType = ((FieldInfo)parentDNMemberInfo.memberInfo).FieldType;
            else
               classType = parentDNMemberInfo.value.GetType();

            // get the member
            MemberInfo memberInfo = ReflectionServices.GetMemeberInfo(classType, val3.StrVal, false,
                                                                      val2.MgNumVal.NUM_2_LONG());

            if (memberInfo is FieldInfo)
               fieldInfo = (FieldInfo)memberInfo;

            // get the value of this member
            object fieldVal = ReflectionServices.GetFieldValue(fieldInfo, parentDNMemberInfo.value);

            // construct the currDNMemberInfo
            var currDNMemberInfo = new DNMemberInfo(memberInfo, fieldVal, null, -1, parentDNMemberInfo);
            resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);

            WriteDotnetExceptionToLog(dnException.get(), val1.DnMemberInfo, "Member", val3.StrVal);

            throw dnException;
         }
      }

      /// <summary>
      ///   DotNet Static Member
      /// </summary>
      /// <param name = "resVal">Result ExpVal</param>
      /// <param name = "val1"></param>
      /// <param name = "val2"></param>
      /// <param name = "val3"></param>
      protected void eval_op_dn_static_member(ExpVal resVal, ExpVal val1, ExpVal val2, ExpVal val3)
      {
         FieldInfo fieldInfo = null;

         try
         {
            // get the class type
            var classType = (Type)val1.DnMemberInfo.value;

            // get the member
            MemberInfo memberInfo = ReflectionServices.GetMemeberInfo(classType, val3.StrVal, true,
                                                                      val2.MgNumVal.NUM_2_LONG());

            if (memberInfo is FieldInfo)
               fieldInfo = (FieldInfo)memberInfo;

            // get the value of this member
            object fieldVal = ReflectionServices.GetFieldValue(fieldInfo, null);

            // construct the currDNMemberInfo
            var currDNMemberInfo = new DNMemberInfo(memberInfo, fieldVal, null, -1, null);
            resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);

            WriteDotnetExceptionToLog(dnException.get(), val1.DnMemberInfo, "StaticMember", val3.StrVal);

            throw dnException;
         }
      }

      /// <summary>
      ///   Creates a arguement array for Dotnet methods and Ctors
      /// </summary>
      /// <param name = "parameters"></param>
      /// <param name = "paramInfos"></param>
      /// <returns></returns>
      private Object[] CreateArgArray(Object[] parameters, ParameterInfo[] paramInfos)
      {
         var argArray = new Object[paramInfos.Length];
         Array paramObj = null;

         for (int i = 0; i < parameters.Length; i++)
         {
            object parameter = parameters[i];
            bool isRef = false;
            bool isParam = false;

            Type typeToConvert;
            if (i >= (paramInfos.Length - 1) && ReflectionServices.IsParams(paramInfos[paramInfos.Length - 1]))
            {
               isParam = true;
               typeToConvert = ReflectionServices.GetType(paramInfos[paramInfos.Length - 1]);
            }
            else if (paramInfos[i].ParameterType.IsByRef)
            {
               isRef = true;
               typeToConvert = ReflectionServices.GetType(paramInfos[i]);
            }
            else
               typeToConvert = paramInfos[i].ParameterType;

            try
            {
               // if numtype and is out or ref, get the value as ExpVal
               if (((ExpVal)parameters[i]).Attr == StorageAttribute.NUMERIC && isRef)
               {
                  int itm = ((ExpVal)parameters[i]).MgNumVal.NUM_2_LONG();
                  parameter = GetItemVal(itm);
               }

               Object objToCast = null;
               if (parameter is ExpVal)
               {
                  var expVal = (ExpVal)parameter;
                  if (expVal.Attr == StorageAttribute.DOTNET)
                     objToCast = expVal.DnMemberInfo.value;
               }
               if (isParam && objToCast != null && objToCast.GetType().IsArray &&
                   typeToConvert.IsAssignableFrom(objToCast.GetType().GetElementType()))
               {
                  argArray[i] = objToCast;
               }
               else if (isParam)
               {
                  // create the param object
                  if (i == (paramInfos.Length - 1))
                  {
                     paramObj = ReflectionServices.CreateArrayInstance(typeToConvert,
                                                                       new[] { parameters.Length - paramInfos.Length + 1 });
                     argArray[i] = paramObj;
                  }

                  // set value to this object
                  Debug.Assert(paramObj != null, "paramObj != null");
                  paramObj.SetValue(DNConvert.doCast(parameter, typeToConvert), i - paramInfos.Length + 1);
               }
               else
                  argArray[i] = DNConvert.doCast(parameter, typeToConvert);
            }
            catch (Exception exception)
            {
               DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
               dnException.set(exception);
               throw dnException;
            }
         }

         return argArray;
      }

      /// <summary>
      ///   update all ref and out parameters
      /// </summary>
      /// <param name = "parameters">original parameter passed to MethodBase</param>
      /// <param name = "paramInfos">ParameterInfos of MethodBase</param>
      /// <param name = "arguments">updated arguements of MethodBase</param>
      private void UpdateRefOut(Object[] parameters, ParameterInfo[] paramInfos, Object[] arguments)
      {
         for (int i = 0; i < paramInfos.Length; i++)
         {
            ParameterInfo paramInfo = paramInfos[i];

            try
            {
               // if out or ref
               if (paramInfo.ParameterType.IsByRef)
               {
                  if (((ExpVal)parameters[i]).Attr == StorageAttribute.NUMERIC)
                  {
                     int itm = ((ExpVal)parameters[i]).MgNumVal.NUM_2_LONG();
                     SetItemVal(itm, arguments[i]);
                  }
                  else if (((ExpVal)parameters[i]).Attr == StorageAttribute.DOTNET)
                  {
                     UpdateParentMembers(((ExpVal)parameters[i]).DnMemberInfo, arguments[i], false);
                  }
               }
            }
            catch (Exception exception)
            {
               DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
               dnException.set(exception);
               throw dnException;
            }
         }
      }

      /// <summary>
      ///   handles all the updation up the list and into the DNObjectsCollection
      /// </summary>
      /// <param name = "list">dnMemberInfo list</param>
      /// <param name = "valToUpdate">Obj to update</param>
      /// <param name = "mustWrite"></param>
      private void UpdateParentMembers(DNMemberInfo list, object valToUpdate, bool mustWrite)
      {
         if (list == null) // valToUpdate can be null
            return;

         DNMemberInfo currDNMemberInfo = list;
         Type memberType = ReflectionServices.GetType(currDNMemberInfo);

         if (memberType != null)
         {
            // cast the valToUpdate into type of memberInfo
            Object castedObj = DNConvert.doCast(valToUpdate, memberType);

            // set castedObj onto currDNMemberInfo.value
            currDNMemberInfo.value = castedObj;
         }
         else
            currDNMemberInfo.value = valToUpdate;

         // get parent dnMemberInfo
         DNMemberInfo parentDNMemberInfo = currDNMemberInfo.parent;
         Object parentObj = null;

         // calling setValue for Indexer or property
         if (currDNMemberInfo.memberInfo != null && currDNMemberInfo.memberInfo is PropertyInfo)
         {
            var propertyInfo = (PropertyInfo)currDNMemberInfo.memberInfo;

            try
            {
               // For Static members, parentObj will be ignored.
               if (parentDNMemberInfo != null)
               {
                  // get the parentObj object
                  parentObj = parentDNMemberInfo.value;
               }
               
               // indexers - currDNMemberInfo.indexes will have an Object[] of indexes
               // property - will have currDNMemberInfo.indexes as null.
               if ((propertyInfo.CanWrite && propertyInfo.PropertyType.IsValueType && !propertyInfo.PropertyType.IsPrimitive) || mustWrite)
                  ReflectionServices.SetPropertyValue(propertyInfo, parentObj, currDNMemberInfo.indexes,
                                                      currDNMemberInfo.value);

                // recursively call UpdateParentMembers to update parent lists.
                UpdateParentMembers(parentDNMemberInfo, parentObj, false);              
            }
            catch (Exception exception)
            {
               DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
               dnException.set(exception);
               throw dnException;
            }

            return;
         }

         parentDNMemberInfo = currDNMemberInfo.parent;
         object currObj = currDNMemberInfo.value;
         parentObj = parentDNMemberInfo != null
                        ? parentDNMemberInfo.value
                        : null;

         try
         {
            if (parentDNMemberInfo != null)
            {
               if (currDNMemberInfo.indexes != null) // ARRAY_ELEMENT
               {
                  var arrObj = (Array)parentObj;
                  var indexes = new int[currDNMemberInfo.indexes.Length];

                  for (int i = 0; i < currDNMemberInfo.indexes.Length; i++)
                     indexes[i] = int.Parse(currDNMemberInfo.indexes[i].ToString());

                  ReflectionServices.SetArrayElement(arrObj, currObj, indexes);
               }
               else if (currDNMemberInfo.memberInfo is FieldInfo) // FIELDS
               {
                  var fieldinfo = (FieldInfo)currDNMemberInfo.memberInfo;
                  ReflectionServices.SetFieldValue(fieldinfo, parentObj, currObj);
               }

               if (!(parentDNMemberInfo.memberInfo is PropertyInfo))
                  // recursively call UpdateParentMembers to update parent lists.
                  UpdateParentMembers(parentDNMemberInfo, parentObj, false);
            }
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);
            throw dnException;
         }

         if (currDNMemberInfo.dnObjectCollectionIsn != -1)
         {
            int key = currDNMemberInfo.dnObjectCollectionIsn;
            Type dotNetType = DNManager.getInstance().DNObjectsCollection.GetDNType(key);

            // new object must be assignable to the field
            currObj = DNConvert.doCast(currObj, dotNetType);

            DNManager.getInstance().DNObjectsCollection.Update(key, currObj);
         }

         return;
      }

      /// <summary>
      ///   DotNet Constructor
      /// </summary>
      /// <param name = "resVal">result ExpVal</param>
      /// <param name = "Exp_params">ExpVals with information about Ctor</param>
      protected void eval_op_dn_ctor(ExpVal resVal, ExpVal[] Exp_params)
      {
         MemberInfo memberInfo = null;
         ConstructorInfo ctorInfo = null;

         try
         {
            // get the class type
            var classType = (Type)Exp_params[0].DnMemberInfo.value;

            // get the hashcode
            int? hashcode = Exp_params[1].MgNumVal.NUM_2_LONG();

            // If the type is structure, we cannot evaluate the hashcode for the default ctor. So, zero is sent as
            // hashcode. In such senario, we should avoid using ConstructorInfo and simply invoke default ctor (#807200)
            Object newConstructedObj;
            if (classType.IsValueType && hashcode == 0)
            {
               // invoke the default ctor
               newConstructedObj = ReflectionServices.CreateInstance(classType, null, null);
            }
            else
            {
               // get the member
               memberInfo = ReflectionServices.GetMemeberInfo(classType, ".ctor", false, hashcode);

               if (memberInfo is ConstructorInfo)
                  ctorInfo = (ConstructorInfo)memberInfo;

               ParameterInfo[] paramInfos = ReflectionServices.GetParameters(ctorInfo);
               Object[] arguements = null;
               Object[] parameters = null;

               if (paramInfos.Length > 0)
               {
                  parameters = new Object[Exp_params.Length - 2];

                  // fill paramters - Exp_params index 2 onwards.
                  for (int i = 2; i < Exp_params.Length; i++)
                     parameters[i - 2] = Exp_params[i];

                  arguements = CreateArgArray(parameters, paramInfos);
               }

               // invoke the ctor and get the constructed object
               newConstructedObj = ReflectionServices.CreateInstance(classType, ctorInfo, arguements);

               // update ref out paramters
               if (paramInfos.Length > 0)
                  UpdateRefOut(parameters, paramInfos, arguements);
            }

            // construct the currDNMemberInfo
            var currDNMemberInfo = new DNMemberInfo(memberInfo, newConstructedObj, null, -1, null);
            resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);

            WriteDotnetExceptionToLog(dnException.get(), null, "ctor", null);

            throw dnException;
         }
      }

      /// <summary>
      ///   DotNet Method
      /// </summary>
      /// <param name = "resVal">result ExpVal</param>
      /// <param name = "expParams">ExpVals with information about Ctor</param>
      protected void eval_op_dn_method(ExpVal resVal, ExpVal[] expParams)
      {
         MethodInfo methodInfo = null;

         try
         {
            DNMemberInfo parentDNMemberInfo = expParams[0].DnMemberInfo;

            Type classType = GetType(parentDNMemberInfo);

            // get the member
            MemberInfo memberInfo = ReflectionServices.GetMemeberInfo(classType, expParams[2].StrVal, false,
                                                                      expParams[1].MgNumVal.NUM_2_LONG());
            if (memberInfo is MethodInfo)
               methodInfo = (MethodInfo)memberInfo;

            ParameterInfo[] paramInfos = ReflectionServices.GetParameters(methodInfo);
            Object[] arguements = null;
            Object[] parameters = null;

            if (paramInfos.Length > 0)
            {
               parameters = new Object[expParams.Length - 3];

               // fill paramters - Exp_params index 3 onwards.
               for (int i = 3; i < expParams.Length; i++)
                  parameters[i - 3] = expParams[i];

               arguements = CreateArgArray(parameters, paramInfos);
            }

            // invoke the ctor and get the constructed object
            Object newReturnObj = ReflectionServices.InvokeMethod(methodInfo, parentDNMemberInfo.value, arguements,
                                                                  false);

            // update parents
            UpdateParentMembers(parentDNMemberInfo, parentDNMemberInfo.value, false);

            // update ref out paramters
            if (paramInfos.Length > 0)
               UpdateRefOut(parameters, paramInfos, arguements);

            // construct the currDNMemberInfo
            var currDNMemberInfo = new DNMemberInfo(memberInfo, newReturnObj, null, -1, null);
            resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);

            WriteDotnetExceptionToLog(dnException.get(), expParams[0].DnMemberInfo, "Method", expParams[2].StrVal);

            throw dnException;
         }
      }

      /// <summary>
      /// gets type from dnMemberInfo
      /// </summary>
      /// <param name="parentDNMemberInfo"></param>
      /// <returns></returns>
      private static Type GetType(DNMemberInfo parentDNMemberInfo)
      {
         // get the class type
         Type classType = ReflectionServices.GetType(parentDNMemberInfo.memberInfo);

         return classType;
      }

      /// <summary>
      ///   DotNet Static Method
      /// </summary>
      /// <param name = "resVal">result ExpVal</param>
      /// <param name = "expParams">ExpVals with information about Ctor</param>
      protected void eval_op_dn_static_method(ExpVal resVal, ExpVal[] expParams)
      {
         MethodInfo methodInfo = null;

         try
         {
            // get the class type
            var classType = (Type)expParams[0].DnMemberInfo.value;

            // get the member
            MemberInfo memberInfo = ReflectionServices.GetMemeberInfo(classType, expParams[2].StrVal, true,
                                                                      expParams[1].MgNumVal.NUM_2_LONG());

            if (memberInfo is MethodInfo)
               methodInfo = (MethodInfo)memberInfo;

            ParameterInfo[] paramInfos = ReflectionServices.GetParameters(methodInfo);
            Object[] arguements = null;
            Object[] parameters = null;

            if (paramInfos.Length > 0)
            {
               parameters = new Object[expParams.Length - 3];

               // fill paramters - Exp_params index 3 onwards.
               for (int i = 3; i < expParams.Length; i++)
                  parameters[i - 3] = expParams[i];

               arguements = CreateArgArray(parameters, paramInfos);
            }

            // invoke the ctor and get the constructed object
            Object newReturnObj = ReflectionServices.InvokeMethod(methodInfo, null, arguements, false);

            // update ref out paramters
            if (paramInfos.Length > 0)
               UpdateRefOut(parameters, paramInfos, arguements);

            // construct the currDNMemberInfo
            var currDNMemberInfo = new DNMemberInfo(memberInfo, newReturnObj, null, -1, null);
            resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);

            WriteDotnetExceptionToLog(dnException.get(), expParams[0].DnMemberInfo, "StaticMethod",
                                      expParams[2].StrVal);

            throw dnException;
         }
      }

      /// <summary>
      ///   DotNet Array Constructor
      /// </summary>
      /// <param name = "resVal">result ExpVal</param>
      /// <param name = "expParams">ExpVals with information about Ctor</param>
      protected void eval_op_dn_array_ctor(ExpVal resVal, ExpVal[] expParams)
      {
         try
         {
            // get the class type
            var classType = (Type)expParams[0].DnMemberInfo.value;

            int[] arguements = null;
            int paramLength = expParams.Length - 1;

            if (paramLength > 0)
            {
               arguements = new int[paramLength];

               // fill parameters - Exp_params index 1 onwards.
               for (int i = 1; i < expParams.Length; i++)
                  arguements[i - 1] = (int)DNConvert.doCast(expParams[i], typeof(int));
            }

            // invoke the ctor and get the constructed object
            Object newArrayObj = ReflectionServices.CreateArrayInstance(classType, arguements);

            // construct the currDNMemberInfo
            var currDNMemberInfo = new DNMemberInfo(null, newArrayObj, null, -1, null);
            resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);

            WriteDotnetExceptionToLog(dnException.get(), null, "arrayCtor", null);

            throw dnException;
         }
      }

      /// <summary>
      ///   DotNet Set Array Element
      /// </summary>
      /// <param name = "resVal">result ExpVal</param>
      /// <param name = "expParams">ExpVals with information about Ctor</param>
      protected void eval_op_dn_array_element(ExpVal resVal, ExpVal[] expParams)
      {
         try
         {
            DNMemberInfo parentDNMemberInfo = expParams[0].DnMemberInfo;

            var arrObj = (Array)parentDNMemberInfo.value;
            int[] arguements = null;
            Object[] indexes = null;
            int paramLength = expParams.Length - 1;

            if (paramLength > 0)
            {
               arguements = new int[paramLength];
               indexes = new Object[paramLength];

               // fill paramters - Exp_params index 1 onwards.
               for (int i = 1; i < expParams.Length; i++)
               {
                  var index = (int)DNConvert.doCast(expParams[i], typeof(int));
                  arguements[i - 1] = index;
                  indexes[i - 1] = index;
               }
            }

            // invoke the ctor and get the constructed object
            Object arrayElement = ReflectionServices.GetArrayElement(arrObj, arguements);


            // construct the currDNMemberInfo
            var currDNMemberInfo = new DNMemberInfo(null, arrayElement, indexes, -1, parentDNMemberInfo);
            resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);

            WriteDotnetExceptionToLog(dnException.get(), expParams[0].DnMemberInfo, "ArrayElement", null);

            throw dnException;
         }
      }

      /// <summary>
      ///   DotNet Property Get
      /// </summary>
      /// <param name = "resVal">Result ExpVal</param>
      /// <param name = "val1">Member reference</param>
      /// <param name = "val2">PropertyName</param>
      /// <param name = "val3"></param>
      protected void eval_op_dn_prop_get(ExpVal resVal, ExpVal val1, ExpVal val2, ExpVal val3)
      {
         PropertyInfo propertyInfo = null;

         try
         {
            DNMemberInfo parentDNMemberInfo = val1.DnMemberInfo;
            object parentObj = parentDNMemberInfo.value;

            // get the class type
            Type classType = GetType(parentDNMemberInfo);

            // get the member
            MemberInfo memberInfo = ReflectionServices.GetMemeberInfo(classType, val3.StrVal, false,
                                                                      val2.MgNumVal.NUM_2_LONG());

            if (memberInfo is PropertyInfo)
               propertyInfo = (PropertyInfo)memberInfo;

            // invoke the ctor and get the constructed object
            Object newReturnObj = ReflectionServices.GetPropertyValue(propertyInfo, parentObj, null);

            // update parents
            UpdateParentMembers(parentDNMemberInfo, parentObj, false);

            // construct the currDNMemberInfo
            var currDNMemberInfo = new DNMemberInfo(memberInfo, newReturnObj, null, -1, parentDNMemberInfo);
            resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);

            WriteDotnetExceptionToLog(dnException.get(), val1.DnMemberInfo, "PropGet", val3.StrVal);

            throw dnException;
         }
      }

      /// <summary>
      ///   DotNet Static Property Get
      /// </summary>
      /// <param name = "resVal">Result ExpVal</param>
      /// <param name = "val1"></param>
      /// <param name = "val2"></param>
      /// <param name = "val3"></param>
      protected void eval_op_dn_static_prop_get(ExpVal resVal, ExpVal val1, ExpVal val2, ExpVal val3)
      {
         PropertyInfo propertyInfo = null;

         try
         {
            // get the class type
            var classType = (Type)val1.DnMemberInfo.value;

            // get the member
            MemberInfo memberInfo = ReflectionServices.GetMemeberInfo(classType, val3.StrVal, true,
                                                                      val2.MgNumVal.NUM_2_LONG());

            if (memberInfo is PropertyInfo)
               propertyInfo = (PropertyInfo)memberInfo;

            // invoke the ctor and get the constructed object
            Object newReturnObj = ReflectionServices.GetPropertyValue(propertyInfo, (Object)null, null);

            // construct the currDNMemberInfo
            var currDNMemberInfo = new DNMemberInfo(memberInfo, newReturnObj, null, -1, null);
            resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);

            WriteDotnetExceptionToLog(dnException.get(), val1.DnMemberInfo, "StaticPropGet", val3.StrVal);

            throw dnException;
         }
      }

      /// <summary>
      ///   DotNet Indexer
      /// </summary>
      /// <param name = "resVal">result ExpVal</param>
      /// <param name = "expParams">ExpVals with information about Ctor</param>
      protected void eval_op_dn_indexer(ExpVal resVal, ExpVal[] expParams)
      {
         PropertyInfo propertyInfo = null;

         try
         {
            DNMemberInfo parentDNMemberInfo = expParams[1].DnMemberInfo;
            object parentObj = parentDNMemberInfo.value;

            // get the class type
            Type classType = GetType(parentDNMemberInfo);

            // get the member
            MemberInfo memberInfo = ReflectionServices.GetMemeberInfo(classType, "Item", false,
                                                                      expParams[0].MgNumVal.NUM_2_LONG());

            if (memberInfo is PropertyInfo)
               propertyInfo = (PropertyInfo)memberInfo;

            Object[] indexes = null;
            ParameterInfo[] paramInfos = propertyInfo.GetIndexParameters();

            if (paramInfos.Length > 0)
            {
               // indexers cannot have refs, but can have params
               var parameters = new Object[expParams.Length - 2];

               // fill parameters - Exp_params index 2 onwards.
               for (int i = 2; i < expParams.Length; i++)
                  parameters[i - 2] = expParams[i];

               indexes = CreateArgArray(parameters, paramInfos);
            }

            // invoke the ctor and get the constructed object
            Object newReturnObj = ReflectionServices.GetPropertyValue(propertyInfo, parentObj, indexes);

            // update parent
            UpdateParentMembers(parentDNMemberInfo, parentObj, false);

            // construct the currDNMemberInfo
            var currDNMemberInfo = new DNMemberInfo(memberInfo, newReturnObj, indexes, -1, parentDNMemberInfo);
            resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);

            WriteDotnetExceptionToLog(dnException.get(), expParams[1].DnMemberInfo, "indexer", null);

            throw dnException;
         }
      }


      /// <summary>
      ///   DotNet Enum
      /// </summary>
      /// <param name = "resVal">Result ExpVal</param>
      /// <param name = "val1"></param>
      /// <param name = "val2"></param>
      protected void eval_op_dn_enum(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         FieldInfo fieldInfo = null;

         try
         {
            // get the class type
            var classType = (Type)val1.DnMemberInfo.value;

            // get the member
            MemberInfo memberInfo = ReflectionServices.GetMemeberInfo(classType, val2.StrVal, true, null);

            if (memberInfo is FieldInfo)
               fieldInfo = (FieldInfo)memberInfo;

            // get the Enum value
            Object enumVal = ReflectionServices.GetFieldValue(fieldInfo, null);

            // construct the currDNMemberInfo
            var currDNMemberInfo = new DNMemberInfo(null, enumVal, null, -1, null);

            resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);

            WriteDotnetExceptionToLog(dnException.get(), val1.DnMemberInfo, "Enum", val2.StrVal);

            throw dnException;
         }
      }

      /// <summary>
      ///   DotNet Cast
      /// </summary>
      /// <param name = "resVal"></param>
      /// <param name = "val1"></param>
      /// <param name = "val2"></param>
      protected void eval_op_dn_cast(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         DNMemberInfo currDNMemberInfo = null;

         try
         {
            var typeToCast = (Type)val2.DnMemberInfo.value;
            Object castedObj = DNConvert.doCast(val1, typeToCast);

            if (castedObj != null)
            {
               // construct the currDNMemberInfo
               currDNMemberInfo = new DNMemberInfo(null, castedObj, null, -1, val1.DnMemberInfo);
            }

            resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);

            WriteDotnetExceptionToLog(dnException.get(), val2.DnMemberInfo, "DnCast", null);

            throw dnException;
         }
      }

      /// <summary>
      ///   DotNet Ref
      /// </summary>
      /// <param name = "resVal"></param>
      /// <param name = "val1"></param>
      protected static void eval_op_dn_ref(ExpVal resVal, ExpVal val1)
      {
         // set val1 into resVal, and return         
         resVal.Copy(val1);
      }

      /// <summary>
      ///   DotNet Set
      /// </summary>
      /// <param name = "resVal"></param>
      /// <param name = "val1"></param>
      /// <param name = "val2"></param>
      protected void eval_op_dn_set(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         Object objToSet = null;

         try
         {
            if (val2.Attr == StorageAttribute.DOTNET)
               objToSet = val2.DnMemberInfo.value;
            else
            {
               // if val1 attr is numeric, then the first parameter is field. Hence, we convert the val2 (which
               // is non-dotnet type) to default dotnet type, so that this can be used in SetItemVal()
               // and also as an object in return ExpVal
               Type dnTypeToConvert = val1.Attr ==
                                      StorageAttribute.NUMERIC
                                         ? DNConvert.getDefaultDotNetTypeForMagicType(val2.ToMgVal(), val2.Attr)
                                         : ReflectionServices.GetType(val1.DnMemberInfo);

               objToSet = ConvertMagicToDotNet(val2, dnTypeToConvert);
            }

            // the first parameter is field
            DNMemberInfo currDNMemberInfo;
            if (val1.Attr == StorageAttribute.NUMERIC)
            {
               int itm = val1.MgNumVal.NUM_2_LONG();

               SetItemVal(itm, objToSet);

               // Create a DNMemberInfo for objToSet (to be used in return ExpVal)
               currDNMemberInfo = new DNMemberInfo(null, objToSet, null, -1, null);
            }
            else
            {
               currDNMemberInfo = val1.DnMemberInfo;

               // Set and Update all the parent members.
               UpdateParentMembers(currDNMemberInfo, objToSet, true);
            }

            resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);

            if (val1.Attr == StorageAttribute.NUMERIC) // is a Field
               WriteDotnetExceptionToLog(dnException.get(), val1.MgNumVal.NUM_2_LONG(), "DnSet", null);
            else
               WriteDotnetExceptionToLog(dnException.get(), val1.DnMemberInfo, "DnSet",
                                         (val1.DnMemberInfo.memberInfo != null
                                             ? val1.DnMemberInfo.memberInfo.Name
                                             : null));

            throw dnException;
         }
      }

#if !PocketPC
      /// <summary>
      ///   DataView to DN DataTable
      /// </summary>
      /// <param name = "resVal"></param>
      /// <param name = "val1"></param>
      /// <param name = "val2"></param>
      /// <param name = "val3"></param>
      protected void eval_op_dataview_to_dn_datatable(ExpVal resVal, ExpVal val1, ExpVal val2, ExpVal val3)
      {
         if (val1.MgNumVal == null || val2.StrVal == null || val3.StrVal == null)
         {
            SetNULL(resVal, StorageAttribute.BLOB);
            return;
         }

         int generation = val1.MgNumVal.NUM_2_LONG();
         TaskBase ancestorTask = null;

         if (generation >= 0)
            ancestorTask = GetContextTask(generation);

         if (ancestorTask == null)
         {
            SetNULL(resVal, StorageAttribute.BLOB);
            return;
         }

         var taskVariableNames = StrUtil.ZstringMake(val2.StrVal, val2.StrVal.Length);
         var displayNames = StrUtil.ZstringMake(val3.StrVal, val3.StrVal.Length);

         try
         {
            var dataTable = new DNDataTable();

            var columnsList = dataTable.PrepareColumnsList(ancestorTask.getTaskTag(), taskVariableNames, displayNames);
            if (columnsList.Count > 0)
            {
               // add the columns.
               dataTable.AddColumns();

               // get dataview and add the rows.
               String dataViewContent = Events.GetDataViewContent(ExpTask, generation, taskVariableNames);
               if (dataViewContent != null)
                  dataTable.AddRows(dataViewContent);
            }

            var currDNMemberInfo = new DNMemberInfo(null, dataTable.DataTblObj, null, -1, null);
            resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);
            throw dnException;
         }
      }
#endif

      /// <summary>
      /// </summary>
      /// <param name = "resVal"></param>
      /// <param name = "val1"></param>
      /// <param name = "val2"></param>
      protected void eval_op_dntype(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         try
         {
            Type classType = ReflectionServices.GetType(val1.MgNumVal.NUM_2_LONG(), val2.StrVal);

            var currDNMemberInfo = new DNMemberInfo(null, classType, null, -1, null);
            resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
         }
         catch (Exception exception)
         {
            DNException dnException = Manager.GetCurrentRuntimeContext().DNException;
            dnException.set(exception);
            throw dnException;
         }
      }

      /// <summary>
      /// last exception occurred by a .NET operation
      /// </summary>
      /// <param name="resVal"></param>
      protected void eval_op_dn_exception(ExpVal resVal)
      {
         var currDNMemberInfo = new DNMemberInfo(null, Manager.GetCurrentRuntimeContext().DNException.get(), null, -1,
                                                 null);

         //reset the DotNetException to null
         Manager.GetCurrentRuntimeContext().DNException.reset();
         resVal.UpdateFromDNMemberInfo(currDNMemberInfo);
      }

      /// <summary>
      /// checks if an error occurred during the last .NET operation
      /// </summary>
      /// <param name="resVal"></param>
      protected void eval_op_dn_exception_occured(ExpVal resVal)
      {
         // add result into boolVal of resVal
         resVal.BoolVal = Manager.GetCurrentRuntimeContext().DNException.hasExceptionOcurred();
         resVal.Attr = StorageAttribute.BOOLEAN;
      }

      /// <summary>(protected)
      /// prepares ExpVal from controls value(in edit mode)
      /// it validates the value and copy the actual value (excluding format)
      /// to ExpVal if valid. Other wise copy default value.
      /// </summary>
      /// <param name="ctrl">control</param>
      /// <param name="resVal">out: resultant ExpVal</param>
      /// <returns></returns>
      protected void GetValidatedMgValue(MgControlBase ctrl, ref ExpVal resVal)
      {
         // Get current value of control
         string currValOfcontrol = Manager.GetCtrlVal(ctrl);
         string orgCurrValOfControl = currValOfcontrol;

         // Get previous value of control (new value is defined in control, but focus is yet on control.
         // so the control._val is still have previous value)
         string prevValOfControl = ctrl.isRichEditControl()
                                      ? ctrl.getRtfVal()
                                      : ctrl.Value;

         // Validate the controls value
         // validate in order to get warnings here ..but as we are here for EditGet() , display the 
         // ctrl's value as is and not the validated value.
         currValOfcontrol = GetValidatedValue(ctrl, prevValOfControl, currValOfcontrol);

         currValOfcontrol = ctrl.getMgValue(orgCurrValOfControl);

         // Update the result val
         SetVal(resVal, ctrl.DataType, currValOfcontrol, null);
         resVal.IsNull = ctrl.IsNull;
      }

      /// <summary>
      /// writes dotnet exception to log
      /// </summary>
      /// <param name="exception"></param>
      /// <param name="obj">if dnMemberInfo, extracts the field from first node. Otherwise, must be a field</param>
      /// <param name="opcode"></param>
      /// <param name="memberName"></param>
      private void WriteDotnetExceptionToLog(Exception exception, Object obj, string opcode, string memberName)
      {
         String errStr = "";

         try
         {
            if (obj is DNMemberInfo)
            {
               var currDNMemberInfo = (DNMemberInfo)obj;

               // get the first node
               while (currDNMemberInfo != null && currDNMemberInfo.parent != null)
                  currDNMemberInfo = currDNMemberInfo.parent;

               // add isn info
               if (currDNMemberInfo != null && currDNMemberInfo.dnObjectCollectionIsn != -1)
                  errStr += String.Format("DNObjectCollectionIsn: \"{0}\" ", currDNMemberInfo.dnObjectCollectionIsn);
            }
            else
            {
               // add field info
               errStr += String.Format("FieldIdx: \"{0}\" ", obj);
            }

            // add opcode with memberName (if any)
            if (memberName != null)
               errStr += String.Format("{0}: \"{1}\" ", opcode, memberName);
            else
               errStr += String.Format("{0} ", opcode);

            // add exception
            errStr += String.Format("Exception: \"{0}\"", exception);

            // write to log
            Events.WriteExceptionToLog(errStr);
         }
         catch (Exception)
         {
            Events.WriteExceptionToLog("Exception while logging the exception.");
         }
      }

      /// <summary>converts expression value into a corresponding dotnet object, as defined by the table below</summary>
      /// <returns></returns>
      public static Object ConvertMagicToDotNet(ExpVal expVal, Type dotNetType)
      {
         return (expVal.IsNull
                   ? null
                   : (DNConvert.convertMagicToDotNet(expVal.ToMgVal(), expVal.Attr, dotNetType)));
      }

#if !PocketPC
      /// <summary>
      /// SetWindow Focus.
      /// </summary>
      /// <param name="expVal"></param>
      /// <param name="resVal"></param>
      protected void eval_op_setwindow_focus(ExpVal expVal, ExpVal resVal)
      {
         String formName = expVal.StrVal;
         resVal.BoolVal = false;

         if (!String.IsNullOrEmpty(formName))
         {
            // Iterate & find a form from WindowList and if found activate it and put act_hit
            MgFormBase mgFormBase = Manager.MenuManager.WindowList.GetByName(formName);
            if (mgFormBase != null)
            {
               Commands.addAsync(CommandType.ACTIVATE_FORM, (Object)mgFormBase, true);
               Commands.beginInvoke();
               Manager.EventsManager.addGuiTriggeredEvent(mgFormBase.getTask(), InternalInterface.MG_ACT_HIT);

               resVal.BoolVal = true;
            }
            else
            {
               IntPtr hWnd = NativeWindowCommon.FindWindowW(null, formName);
               if (hWnd != null)
                  resVal.BoolVal = NativeWindowCommon.SetForegroundWindow(hWnd);
            }
         }
      }
#endif

      #region DRAG And DROP

#if !PocketPC
      /// <summary>
      /// Set data to be dragged.
      /// </summary>
      public static void eval_op_DragSetData(ExpVal resVal, ExpVal expData, ExpVal expFormat, ExpVal expUserFormat)
      {
         var format = (ClipFormats)expFormat.MgNumVal.NUM_2_LONG();
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = false;

         // Format should be supported && function is valid only under DragBegin.
         if (expData.StrVal != null && DroppedData.IsFormatSupported(format) && Commands.IsBeginDrag())
         {
            // Remove blobPrefix when we have a blob in DragSetData.
            if (StorageAttributeCheck.isTypeBlob(expData.Attr) && expData.IncludeBlobPrefix)
               expData.StrVal = BlobType.removeBlobPrefix(expData.StrVal);

            Commands.addAsync(CommandType.SETDATA_FOR_DRAG, null, 0, expData.StrVal.TrimEnd(),
                              (expUserFormat != null
                                  ? expUserFormat.StrVal
                                  : null), expFormat.MgNumVal.NUM_2_LONG());
            resVal.BoolVal = true;
         }
      }

      /// <summary>
      /// Get the dropped data, from GuiUtils.DroppedData according to the format.
      /// </summary>
      public static void eval_op_DropGetData(ExpVal resVal, ExpVal expFormat, ExpVal expUserFormat)
      {
         var format = (ClipFormats)expFormat.MgNumVal.NUM_2_LONG();
         resVal.Attr = StorageAttribute.UNICODE;
         resVal.StrVal = null;

         if (DroppedData.IsFormatSupported(format))
         {
            string str = Commands.GetDroppedData(format, (expUserFormat != null
                                                             ? expUserFormat.StrVal
                                                             : null));
            resVal.StrVal = (str.Length > 0
                               ? str
                               : "");
         }
      }

      /// <summary>
      /// Check whether the format is present in dropped data or not.
      /// </summary>
      public static void eval_op_DropFormat(ExpVal resVal, ExpVal expFormat, ExpVal expUserFormat)
      {
         var format = (ClipFormats)expFormat.MgNumVal.NUM_2_LONG();
         resVal.BoolVal = false;
         resVal.Attr = StorageAttribute.BOOLEAN;

         if (DroppedData.IsFormatSupported(format))
            resVal.BoolVal = Commands.CheckDropFormatPresent(format, (expUserFormat != null
                                                                         ? expUserFormat.StrVal
                                                                         : null));
      }

      /// <summary>
      /// return the drop mouse as uom
      /// </summary>
      /// <returns></returns>
      private int GetDropMouseAsUom(bool isXaxis)
      {
         int dropInt = isXaxis ? Commands.GetDroppedX() : Commands.GetDroppedY();

         MgFormBase form = ((TaskBase)ExpTask.GetContextTask()).getForm();
         RuntimeContextBase runtimeContext = Manager.GetCurrentRuntimeContext();
         int dropIntUom = form.pix2uom(dropInt, isXaxis);

         return dropIntUom;
      }

      /// <summary>
      /// Get the X coordinate relative to form where drop occurs.
      /// </summary>
      public void eval_op_GetDropMouseX(ExpVal resVal)
      {
         resVal.MgNumVal = new NUM_TYPE();

         int uom = GetDropMouseAsUom(true);

         resVal.MgNumVal.NUM_4_LONG(uom);
         resVal.Attr = StorageAttribute.NUMERIC;
      }

      /// <summary>
      /// Get the Y coordinate relative to form where drop occurs.
      /// </summary>
      public void eval_op_GetDropMouseY(ExpVal resVal)
      {
         resVal.MgNumVal = new NUM_TYPE();

         int uomY = GetDropMouseAsUom(false);

         resVal.MgNumVal.NUM_4_LONG(uomY);
         resVal.Attr = StorageAttribute.NUMERIC;
      }

      /// <summary>
      /// Set the Cursor for Drag operation.
      /// </summary>
      /// <param name="resVal">bool whether cursor is set or not.</param>
      /// <param name="val1">It will contain FileName of a cursor.</param>
      /// <param name="val2">Cursor type : Copy / None </param>
      public static void eval_op_DragSetCursor(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         resVal.BoolVal = false;
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = GuiUtilsBase.DraggedData.SetCursor(val2.StrVal, (CursorType)val1.MgNumVal.NUM_2_LONG());
      }

#endif

      #endregion

      #region Nested type: ExpVal

      /// <summary>
      ///   The class holds data of a basic element (constant or variable) that
      ///   appears or are result of execution of operator
      /// </summary>
      public class ExpVal
      {
         /// <summary>
         ///   default CTOR, used by global params table
         /// </summary>
         public ExpVal()
         {
            Attr = StorageAttribute.NONE;
         }

         /// <summary>
         ///   CTOR
         /// </summary>
         public ExpVal(StorageAttribute attr, bool isNull, String mgVal)
         {
            Init(attr, isNull, mgVal);
         }

         /// <summary>
         /// Ctor for Dotnet Objects
         /// </summary>
         /// <param name = "dnMemberInfo"></param>
         public ExpVal(DNMemberInfo dnMemberInfo)
         {
            UpdateFromDNMemberInfo(dnMemberInfo);
         }

         public StorageAttribute Attr { get; set; }
         public bool IsNull { get; set; } // TRUE if current value (after NULL_ARITH setting was accunted) is NULL
         public NUM_TYPE MgNumVal { get; set; }
         public String StrVal { get; set; }
         public bool BoolVal { get; set; }
         public DNMemberInfo DnMemberInfo { get; set; } // to hold DotNet object
         public bool IncludeBlobPrefix { get; set; }
         public bool OriginalNull { get; set; } //TRUE if underlaying value was null
         public Field VectorField { get; set; }

         public void Copy(ExpVal src)
         {
            Attr = src.Attr;
            BoolVal = src.BoolVal;
            IncludeBlobPrefix = src.IncludeBlobPrefix;
            IsNull = src.IsNull;
            MgNumVal = src.MgNumVal;
            StrVal = src.StrVal;
            VectorField = src.VectorField;
            OriginalNull = src.OriginalNull;
            DnMemberInfo = src.DnMemberInfo;
         }

         /// <summary>
         /// nullify
         /// </summary>
         public void Nullify()
         {
            Init(StorageAttribute.NONE, true, null);
         }

         /// <summary>
         ///   initialize
         /// </summary>
         public void Init(StorageAttribute attr, bool isNull, String mgVal)
         {
            Attr = attr;
            IsNull = isNull;

            switch (Attr)
            {
               case StorageAttribute.ALPHA:
               case StorageAttribute.UNICODE:
                  StrVal = mgVal;
                  break;

               case StorageAttribute.BLOB:
               case StorageAttribute.BLOB_VECTOR:
                  StrVal = mgVal;
                  IncludeBlobPrefix = true;
                  break;

               case StorageAttribute.NUMERIC:
               case StorageAttribute.DATE:
               case StorageAttribute.TIME:
                  MgNumVal = (mgVal != null
                                ? new NUM_TYPE(mgVal)
                                : null);
                  break;

               case StorageAttribute.BOOLEAN:
                  BoolVal = mgVal != null && mgVal.Equals("1");
                  break;

               case StorageAttribute.NONE:
                  BoolVal = false;
                  StrVal = null;
                  MgNumVal = null;
                  OriginalNull = true;
                  VectorField = null;
                  IncludeBlobPrefix = false;
                  DnMemberInfo = null;
                  break;

               case StorageAttribute.DOTNET:
                  // For dotnet objects, ExpVal should be constructed using overloaded Constructor.
                  // If it is a blobPrefix, it should be done here.
                  StrVal = mgVal;
                  break;

               default:
                  throw new ApplicationException("in ExpVal.ExpVal() illegal attribute: '" + Attr + "'");
            }
         }

         public void UpdateFromDNMemberInfo(DNMemberInfo dnMemberInfo)
         {
            DnMemberInfo = dnMemberInfo;
            Attr = StorageAttribute.DOTNET;

            if (DnMemberInfo != null)
               IsNull = (DnMemberInfo.value == null);
         }

         /// <summary>
         ///   Gets the string form of the value in the ExpVal class
         /// </summary>
         public String ToMgVal()
         {
            String str;

            switch (Attr)
            {
               case StorageAttribute.ALPHA:
               case StorageAttribute.BLOB:
               case StorageAttribute.BLOB_VECTOR:
               case StorageAttribute.UNICODE:
               case StorageAttribute.DOTNET:
                  str = StrVal;
                  break;

               case StorageAttribute.NUMERIC:
               case StorageAttribute.DATE:
               case StorageAttribute.TIME:
                  str = MgNumVal.toXMLrecord();
                  break;

               case StorageAttribute.BOOLEAN:
                  str = BoolVal
                           ? "1"
                           : "0";
                  break;

               default:
                  str = "[illegal attribute: " + Attr + ']';
                  break;
            }

            return str;
         }

         /// <summary>
         /// return true if the value is an empty string
         /// </summary>
         /// <returns></returns>
         public Boolean isEmptyString()
         {
            return ((Attr == StorageAttribute.ALPHA || Attr == StorageAttribute.UNICODE) &&
                    StrVal == "");
         }
      }

      #endregion

      // JPN ZIMEREAD
      /// <summary>(protected)
      /// returns the composition string from the last IME operation
      /// </summary>
      /// <param name="resVal">composition string</param>
      protected void eval_op_zimeread(ExpVal resVal)
      {
         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = "";

         UtilImeJpn utilImeJpn = Manager.UtilImeJpn;
         if (utilImeJpn != null)
         {
            String str = utilImeJpn.StrImeRead;
            if (!string.IsNullOrEmpty(str))
               resVal.StrVal = str;
         }
      }

      /// <summary>(protected)
      /// open a help file  
      /// </summary>
      /// <param name="expFilePath">path of the help file.</param>
      /// <param name="expHelpCmd">help command.</param>
      /// <param name="expHelpKey">search key word.</param>
      /// <param name="resVal">succeed or failed</param>
      protected void eval_op_win_help(ExpVal expFilePath, ExpVal expHelpCmd, ExpVal expHelpKey, ExpVal resVal)
      {
         string filePath = expFilePath.StrVal;
         HelpCommand helpCmd = (HelpCommand)expHelpCmd.MgNumVal.NUM_2_LONG();
         string helpKey = expHelpKey.StrVal;
         resVal.Attr = StorageAttribute.BOOLEAN;
         try
         {
            Manager.ShowWindowHelp(filePath, helpCmd, helpKey);
            resVal.BoolVal = true;
         }
         catch
         {
            resVal.BoolVal = false;
         }
      }

      /// <summary>
      /// change the color values in the color table
      /// </summary>
      /// <param name="colorNumber"></param>
      /// <param name="foregroundColor"></param>
      /// <param name="backgroundColor"></param>
      /// <param name="resVal"></param>
      protected void eval_op_ColorSet(ExpVal colorNumber, ExpVal foregroundColor, ExpVal backgroundColor, ExpVal resVal)
      {
         int index = colorNumber.MgNumVal.NUM_2_LONG();
         int foreColor = Int32.Parse(foregroundColor.StrVal, NumberStyles.HexNumber);
         int backColor = Int32.Parse(backgroundColor.StrVal, NumberStyles.HexNumber);

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = Manager.GetColorsTable().SetColor(index, foreColor, backColor);

         if (resVal.BoolVal)
         {
            MgFormBase form = ((TaskBase)ExpTask.GetContextTask()).getForm().getTopMostForm();
            if (form != null)
               form.UpdateColorValues();
         }        
      }

      /// <summary>
      /// change the values of the font in the font table
      /// </summary>
      /// <param name="fontNumberVal"></param>
      /// <param name="fontNameVal"></param>
      /// <param name="sizeVal"></param>
      /// <param name="scriptCodeVal"></param>
      /// <param name="orientationVal"></param>
      /// <param name="boldVal"></param>
      /// <param name="italicVal"></param>
      /// <param name="strikeVal"></param>
      /// <param name="underlineVal"></param>
      /// <param name="resVal"></param>
      protected void eval_op_FontSet(ExpVal fontNumberVal, ExpVal fontNameVal, ExpVal sizeVal, ExpVal scriptCodeVal, 
         ExpVal orientationVal, ExpVal boldVal, ExpVal italicVal, ExpVal strikeVal, ExpVal underlineVal, ExpVal resVal)
      {
         int index = fontNumberVal.MgNumVal.NUM_2_LONG();
         string font = fontNameVal.StrVal;
         int size = sizeVal.MgNumVal.NUM_2_LONG();
         int scriptCode = scriptCodeVal.MgNumVal.NUM_2_LONG();
         int orientation = orientationVal.MgNumVal.NUM_2_LONG();
         bool bold = boldVal.BoolVal;
         bool italic = italicVal.BoolVal;
         bool strike = strikeVal.BoolVal;
         bool underline = underlineVal.BoolVal;

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = Manager.GetFontsTable().SetFont(index, font, size, scriptCode, orientation, bold, italic, strike, underline);

         if (resVal.BoolVal)
         {
            MgFormBase form = ((TaskBase)ExpTask.GetContextTask()).getForm().getTopMostForm();
            if(form != null)
               form.UpdateFontValues();
         }      
      }
   }
}
