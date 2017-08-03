using System;
using System.Threading;
using System.Windows.Forms;
using com.magicsoftware.unipaas.env;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.management;
using com.magicsoftware.unipaas.management.env;
using com.magicsoftware.unipaas.management.events;
using com.magicsoftware.unipaas.management.exp;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using System.Diagnostics;
using com.magicsoftware.support;
using com.magicsoftware.win32;

#if !PocketPC
using System.Drawing;
#endif

namespace com.magicsoftware.unipaas
{
   /// <summary>
   /// MgGui.dll's manager - assembly level properties and methods.
   /// </summary>
   public static class Manager
   {
      private static readonly Object _delayWait = new Object(); //this object is used for execution and interrupting delay command

      public static bool UseWindowsXPThemes { get; set; } // execution property 

      public static bool CanOpenInternalConsoleWindow { get; set; } // execution property - open a Console if the user presses CTRL+SHIFT+J

      public static String DefaultProtocol { get; set; }   // default protocol (http/https) and server name to be used for any relative url
      public static String DefaultServerName { get; set; } //

      private static readonly FontsTable _fontsTable;
      private static readonly ColorsTable _colorsTable;

      public static UtilImeJpn UtilImeJpn { get; private set; } // JPN: IME support

      public static IEnvironment Environment { get; set; }
      public static IEventsManager EventsManager { get; set; }
      public static IMGDataTable MGDataTable { get; set; } //global for the gui namespace

      private static String _clipboardData = ""; //Data to be written to the clipboard.
      public static MenuManager MenuManager { get; private set; }

      public static readonly TextMaskEditor TextMaskEditor;

#if !PocketPC
      // This will hold the contextID for a current thread.
      // Thread static is a separate for each thread (i.e.: value of a static field is unique for each thread).
      // http://msdn.microsoft.com/en-us/library/system.threadstaticattribute.aspx
      [ThreadStaticAttribute]
#endif
      private static Int64 _currentContextID;

      /// <summary>
      /// CTOR
      /// </summary>
      static Manager()
      {
         _fontsTable = new FontsTable();
         _colorsTable = new ColorsTable();

         MenuManager = new MenuManager();

         TextMaskEditor = new TextMaskEditor();

         UtilImeJpn = (UtilStrByteMode.isLocaleDefLangJPN()
                         ? new UtilImeJpn()
                         : null);

         ImageUtils.ImageLoader = new ImageLoader();
      }

      /// <summary>(public)
      /// Set the context ID for a current thread
      /// </summary>
      /// <param name="contextID"></param>
      public static void SetCurrentContextID(Int64 contextID)
      {
         _currentContextID = contextID;
      }

      /// <summary>
      /// Get the context ID for a current thread
      /// </summary>
      /// <returns>current thread's contextID</returns>
      public static Int64 GetCurrentContextID()
      {
         return _currentContextID;
      }

      /// <summary>
      /// helper class for setting/resetting the current context ID.
      /// We require this class specifically for Modal windows. When we open a modal window then 
      /// it waits in GuiThread. Mean while GuiThread can receive the events of other context and
      /// after processing these events, we should reset the contextID of the Modal windows because 
      /// it will be used for logging the events when we close the modal window.
      /// </summary>
      internal class ContextIDGuard : IDisposable
      {
         private Int64 _prevContextID;
#if DEBUG && !PocketPC
         // For debugging purpose only. 
         // It will hold the file name from which the object of ContextIDGuard is created.
         private String _fileName;
#endif

         /// <summary>
         /// </summary>
         internal ContextIDGuard()
         {
            _prevContextID = GetCurrentContextID();
#if DEBUG && !PocketPC
            SaveFilename();
#endif
         }

         /// <summary>
         /// </summary>
         /// <param name="contextID">contextID</param>
         internal ContextIDGuard(Int64 contextID)
         {
            SetCurrent(contextID);
#if DEBUG && !PocketPC
            SaveFilename();
#endif
         }

         /// <summary>
         /// sets the context ID to 'guard'.
         /// </summary>
         /// <param name="contextID">new context ID to 'guard'.</param>
         internal void SetCurrent(Int64 contextID)
         {
            if (contextID != _prevContextID)
            {
               _prevContextID = contextID;
               SetCurrentContextID(contextID);
            }
         }

#if DEBUG && !PocketPC
         /// <summary>
         /// Get the file name from which the ContextIDGuard is being created.
         /// </summary>
         private void SaveFilename()
         {
            var callStack = new StackFrame(1, true);
            _fileName = callStack.GetFileName();
         }
#endif

         /// <summary>
         /// </summary>
         ~ContextIDGuard()
         {
#if DEBUG  && !PocketPC
            Debug.Assert(false, "An instance of ContextIDGuard was created but not disposed : " + _fileName);
#endif
         }

         /// <summary>
         /// </summary>
         public void Dispose()
         {
            if (_prevContextID != GetCurrentContextID())
               SetCurrentContextID(_prevContextID);

            GC.SuppressFinalize(this);
         }
      }

      /// <summary>
      ///   get object that is used for execution and interrupting delay command.
      /// </summary>
      /// <returns></returns>
      public static Object GetDelayWait()
      {
         return _delayWait;
      }

      /// <summary>
      ///   check if dot net control can be focused
      /// </summary>
      /// <param name = "guiMgControl"></param>
      /// <returns></returns>
      public static bool CanFocus(GuiMgControl guiMgControl)
      {
         return GuiUtilsBase.canFocus(guiMgControl);
      }

#if !PocketPC
      /// <summary>
      /// </summary>
      /// <param name = "urlStr"></param>
      /// <returns></returns>
      public static Icon GetIcon(String urlStr)
      {
         return IconsCache.GetInstance().Get(urlStr);
      }
#endif

      /// <summary>
      ///   get the 'form' object
      /// </summary>
      /// <param name = "control"></param>
      /// <returns></returns>
      public static Form FindForm(Control control)
      {
         return GuiUtilsBase.FindForm(control);
      }

      /// <summary>
      /// Sets the current process as dots per inch (dpi) aware.
      /// Scaling by Dpi is useful when you want to size a control or form relative to the screen.
      /// For example, you may want to use dots per inch (DPI) scaling on a control displaying a chart or other graphic
      /// so that it always occupies a certain percentage of the screen.
      /// </summary>
      public static void SetProcessDPIAwareness()
      {  
         if (System.Environment.OSVersion.Version.Major >= 6)
         {
            //If the OS is windows 8.1 or higher i.e windows 10. The minor version is 2.
            if (System.Environment.OSVersion.Version.Minor >= 2)
               NativeWindowCommon.SetProcessDpiAwareness(NativeWindowCommon.PROCESS_DPI_AWARENESS.Process_System_DPI_Aware);
            else //The version is vista or later(e.g. Windows 7). On windows 7, the minor version is 1
               NativeWindowCommon.SetProcessDPIAware();
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="startupImageFileName">the image that is to be shown as the splash.</param>
      public static void Init(String startupImageFileName)
      {
         GUIMain.getInstance().init(startupImageFileName);
      }

#if !PocketPC
      /// <summary>
      /// 
      /// </summary>
      /// <param name="useWindowsXPThemes"></param>
      /// <param name="startupImageFileName">the image that is to be shown as the splash.</param>
      public static void Init(bool useWindowsXPThemes, String startupImageFileName)
      {
         GUIMain.getInstance().init(useWindowsXPThemes, startupImageFileName);
      }
#endif

      public static void Exit()
      {
         GUIMain.getInstance().Exit();
      }

      public static void MessageLoop()
      {
         GUIMain.getInstance().messageLoop();
      }

      public static bool IsInitialized()
      {
         return GUIMain.getInstance().Initialized;
      }

      /// <summary>(public)
      /// finds position of control (in pixels) on the form</summary>
      /// <param name = "task">which consists the control </param>
      /// <param name = "opCode">operation code(EXP_OP_CTRL_LEFT|TOP|WIDTH|HEIGHT) </param>
      /// <param name = "ctrlName">name of the control, null if the controls name is not known </param>
      /// <returns> get the parameter of the control.If the control isn't defined yet, the 0 returned. </returns>
      public static int GetControlFocusedData(TaskBase task, int opCode, String ctrlName)
      {
         int res = 0;

         if (task.getForm() != null)
         {
            MgControlBase lastFocCtrl;
            if (string.IsNullOrEmpty(ctrlName))
            {
               lastFocCtrl = task.getLastParkedCtrl();
               if (lastFocCtrl == null)
                  return 0;
               // get real, HTML name of control, (if repeatable -> with "_Number_of_line" suffix)
               ctrlName = lastFocCtrl.getName();
            }

            lastFocCtrl = task.getForm().GetCtrl(ctrlName);
            if (lastFocCtrl != null)
            {
               var controlRect = new MgRectangle(0, 0, 0, 0);
               Commands.getBounds(lastFocCtrl, controlRect);

               // Return the value wanted
               switch (opCode)
               {
                  case ExpressionInterface.EXP_OP_CTRL_CLIENT_CX:
                     res = task.getForm().pix2uom(controlRect.x, true);
                     break;

                  case ExpressionInterface.EXP_OP_CTRL_CLIENT_CY:
                     res = task.getForm().pix2uom(controlRect.y, false);
                     break;

                  case ExpressionInterface.EXP_OP_CTRL_WIDTH:
                     res = task.getForm().pix2uom(controlRect.width, true);
                     break;

                  case ExpressionInterface.EXP_OP_CTRL_HEIGHT:
                     res = task.getForm().pix2uom(controlRect.height, false);
                     break;
               }
            }
         }
         return res;
      }

      /// <summary>(public)
      /// gets size properties of window
      /// </summary>
      /// <param name = "form">form</param>
      /// <param name = "prop">dimension needed: 'X', 'Y', 'W', 'H' </param>
      /// <returns> size in pixels </returns>
      public static int WinPropGet(MgFormBase form, char prop)
      {
         int res = 0;

         var controlRect = new MgRectangle(0, 0, 0, 0);

         if (form.isSubForm())
         {
            Object ctrl = form.getSubFormCtrl();
            Commands.getBounds(ctrl, controlRect);
         }
         else if (!form.Opened)
            controlRect = Property.getOrgRect(form);
         else
            Commands.getClientBounds(form, controlRect, false);

         // Return the value wanted
         switch (prop)
         {
            case 'X':
               res = form.pix2uom(controlRect.x, true);
               break;
            case 'Y':
               res = form.pix2uom(controlRect.y, false);
               break;
            case 'W':
               res = form.pix2uom(controlRect.width, true);
               break;
            case 'H':
               res = form.pix2uom(controlRect.height, false);
               break;
         }

         return res;
      }

      /// <summary>(public)
      /// retrieve the currently selected text
      /// </summary>
      /// <param name = "ctrl">the control which is assumed to contain the selected text </param>
      public static String MarkedTextGet(MgControlBase ctrl)
      {
         String markedText = null;

         if (ctrl.isTextOrTreeEdit())
         {
            // get the selected boundary
            MgPoint selection = SelectionGet(ctrl);

            // is there any selected text ?
            if (selection.x != selection.y)
            {
               // get the text from the ctrl and the marked text.
               String text = GetCtrlVal(ctrl);
               if (ctrl.isRichEditControl())
                  text = StrUtil.GetPlainTextfromRtf(text);
               markedText = text.Substring(selection.x, (selection.y) - (selection.x));
            }
         }

         return markedText;
      }

      /// <summary>(public)
      /// replace the content of a marked text within an edit control
      /// </summary>
      /// <param name = "ctrl">the control to operate upon </param>
      /// <param name = "str">text to replace </param>
      /// <returns>succeed or failed </returns>
      public static bool MarkedTextSet(MgControlBase ctrl, String str)
      {
         if (!ctrl.isTextOrTreeEdit())
            return false;

         MgPoint selection = SelectionGet(ctrl);
         // return if no text is selected
         if (selection.x
             ==
             selection.y)
            return false;

         bool successful = true;
         if (ctrl.isRichEditControl())
         {
#if !PocketPC
            Commands.setMarkedTextOnRichEdit(ctrl, ctrl.getDisplayLine(true), str);
#endif
         }
         else
             successful = TextMaskEditor.MarkedTextSet(ctrl, str);

         return successful;
      }

      /// <summary>
      ///  close the task, if the wide is open, then close is before close the task
      /// </summary>
      /// <param name = "form"> </param>
      /// <param name="mainPrgTask"></param>
      public static void Abort(MgFormBase form, TaskBase mainPrgTask)
      {
         ApplicationMenus menus = MenuManager.getApplicationMenus(mainPrgTask);

         MgFormBase topMostForm = form.getTopMostForm();
         if (topMostForm.wideIsOpen())
            topMostForm.closeWide();
 
         // Context menu will not automatically dispose when form is dispose. we need to dispose it menually.
         if (menus != null)
            menus.disposeFormContexts(form);

         // remove the instantiatedToolbar as this is not removed from Dispose handler.
         MgMenu mgMenu = form.getPulldownMenu();
         if (mgMenu != null)
            mgMenu.removeInstantiatedToolbar(form);

         if (form.getSubFormCtrl() == null)
         {
            // if the closing form is 'opened as a modal' then decrement the modal count on its frame window.
            if (form.isDialog())
            {
               MgFormBase topMostFrameForm = form.getTopMostFrameForm();
               if (topMostFrameForm != null)
                  topMostFrameForm.UpdateModalFormsCount(form, false);
            }

#if !PocketPC
            // If the form was added into WindowMenu then remove it from list before closing it.
            if (form.IsValidWindowTypeForWidowList)
            {
               Property prop = form.GetComputedProperty(PropInterface.PROP_TYPE_SHOW_IN_WINDOW_MENU);
               if (prop != null && prop.GetComputedValueBoolean())
                  MenuManager.WindowList.Remove(form);
            }
#endif
            JSBridge.Instance.CloseForm(form.getTask().getTaskTag());
            Commands.addAsync(CommandType.CLOSE_FORM, form);

            // QCR# 307199/302197: When a task closes, its form is closed, where it activates topmost form of
            // ParentForm. If Print Preview is activated, this is not required. Instead Print Preview form
            // should be activated.
            if (!PrintPreviewFocusManager.GetInstance().ShouldPrintPreviewBeFocused)
            {
               /* QCR #939802. This bug is since 1.9 after the check-in of version 41 of GuiCommandsQueue.cs   */
               /* From that version onwards, we started to set the owner of the Floating window.               */
               /* Now, when we close a form, the .Net Framework activates the form which spawned this form.    */
               /* But in this special case of the QCR, while closing the 3rd form, framework activated the     */
               /* 1st form instead of the 2nd.                                                                 */
               /* So, when pressing Esc, the 1st form got closed (and of course, because of this all forms got */
               /* closed.                                                                                      */
               /* The solution is to explicitly activate the parent form when a form is closed.                */

               // If interactive offline task calls, non interactive non offline task, after closing the parent task
               // focus is not switched back to caller task, but it is set on MDI Frame. To avoid this, activate parent
               // form only if called task's form is opened.
               MgFormBase formToBeActivated = form.ParentForm;
               if (form.FormToBoActivatedOnClosingCurrentForm != null)
                  formToBeActivated = form.FormToBoActivatedOnClosingCurrentForm;

               if (formToBeActivated != null
                  && form.getTask() != null && !form.getTask().IsParallel //not for parallel
                  && !MgFormBase.isMDIChild(form.ConcreteWindowType) //not for mdi children Defect 128265
                  && form.Opened)
               {
                  Commands.addAsync(CommandType.ACTIVATE_FORM, formToBeActivated.getTopMostForm());

                  if (form.ConcreteWindowType == WindowType.ChildWindow)
                  {
                     TaskBase task = formToBeActivated.getTask();
                     MgControlBase mgControl = task.getLastParkedCtrl();
                     if (mgControl != null)
                        Commands.addAsync(CommandType.SET_FOCUS, mgControl, mgControl.getDisplayLine(false), true);
                  }
               }
            }
         }
         else
         {
            // Defect 115062. For frames remove controls also from frame control.
            if (form.getContainerCtrl() != null)
               Commands.addAsync(CommandType.REMOVE_SUBFORM_CONTROLS, form.getContainerCtrl());
            Commands.addAsync(CommandType.REMOVE_SUBFORM_CONTROLS, form.getSubFormCtrl());
         }

         Commands.beginInvoke();
         Thread.Sleep(10);
      }

      /// <summary>
      /// returns a menu from a ctl
      /// </summary>
      /// <param name="contextID">active/target context</param>
      /// <param name="ctlIdx"></param>
      /// <param name="menuIndex"></param>
      /// <param name="menuStyle"></param>
      /// <param name="form"></param>
      /// <param name="createIfNotExist">This decides if menu is to be created or not.</param>
      /// <returns></returns>
      internal static MgMenu GetMenu(Int64 contextID, int ctlIdx, int menuIndex, MenuStyle menuStyle, MgFormBase form,
                                     bool createIfNotExist)
      {
         TaskBase mainProg = Events.GetMainProgram(contextID, ctlIdx);
         return MenuManager.getMenu(mainProg, menuIndex, menuStyle, form, createIfNotExist);
      }

      /// <summary>
      ///   get FontsTable object
      /// </summary>
      public static FontsTable GetFontsTable()
      {
         return _fontsTable;
      }

      /// <summary>
      ///   get the application ColorTable object from ColorTableList
      /// </summary>
      public static ColorsTable GetColorsTable()
      {
         return _colorsTable;
      }

      /// <summary>
      ///   according to the startup mode style return the style
      /// </summary>
      /// <returns> </returns>
      internal static int GetWindowStateByStartupModeStyle(StartupMode startupMode)
      {
         int style = 0;

         if (startupMode == StartupMode.Default)
         {
         }
         else if (startupMode == StartupMode.Maximize)
            style = Styles.WINDOW_STATE_MAXIMIZE;
         else if (startupMode == StartupMode.Minimize)
            style = Styles.WINDOW_STATE_MINIMIZE;

         return style;
      }

      /// <summary>
      ///   returns the current value of the control
      /// </summary>
      /// <param name = "ctrl">control to get value </param>
      /// <returns> value of the control </returns>
      public static String GetCtrlVal(MgControlBase ctrl)
      {
         // QCR #745117: Make sure that if the contents of a control were changed
         // then the changes are applied before examining its value.
         return Commands.getValue(ctrl, ctrl.getDisplayLine(true));
      }

      /// <summary>
      ///   Set read only for controls
      /// </summary>
      /// <param name = "ctrl">the control to change its property </param>
      /// <param name = "isReadOnly">boolean </param>
      internal static void SetReadOnlyControl(MgControlBase ctrl, bool isReadOnly)
      {
         if (ctrl.isTextOrTreeControl() || ctrl.isRichEditControl() || ctrl.isRichText()
             || ctrl.IsRepeatable) // Defect 131802: Set ReadOnly for Rich Text control in Table Header and defect 131704: in general for Rich Text.
         {
            // JPN: IME support (enable IME in query mode)
            if (UtilStrByteMode.isLocaleDefLangDBCS() && !ctrl.isTreeControl() && !ctrl.isMultiline())
            {
               if (ctrl.getForm().getTask().checkProp(PropInterface.PROP_TYPE_ALLOW_LOCATE_IN_QUERY, false))
                  return;
            }
            Commands.addAsync(CommandType.PROP_SET_READ_ONLY, ctrl, ctrl.getDisplayLine(false), isReadOnly);
            Commands.beginInvoke();
         }
      }

      /// <summary>
      ///   SetSelection : Call Gui to set the selection on the text in the control
      /// </summary>
      /// <param name = "ctrl"> </param>
      /// <param name = "start"> </param>
      /// <param name = "end"> </param>
      /// <param name="caretPos"></param>
      public static void SetSelection(MgControlBase ctrl, int start, int end, int caretPos)
      {
         if (ctrl.isTextOrTreeEdit())
         {
            Commands.setSelection(ctrl, ctrl.getDisplayLine(true), start, end, caretPos);
         }
      }

      /// <summary>
      ///   mark  text in the control
      /// </summary>
      /// <param name = "ctrl">the destination control</param>
      /// <param name="start"></param>
      /// <param name="end"></param>
      internal static void SetMark(MgControlBase ctrl, int start, int end)
      {
         if (ctrl.isTextOrTreeEdit())
            Commands.addAsync(CommandType.SELECT_TEXT, ctrl, ctrl.getDisplayLine(true),
                              (int) MarkMode.MARK_SELECTION_TEXT, start, end);
      }

      /// <summary>
      /// mark a specific range of characters within a control
      /// </summary>
      /// <param name="ctrl">the control to mark</param>
      /// <param name="startIdx">start position (0 = first char)</param>
      /// <param name="len">length of section to mark</param>
      public static void MarkText(MgControlBase ctrl, int startIdx, int len)
      {
         if (ctrl.isTextOrTreeEdit())
         {
            int endPos = startIdx + len;
            SetSelection(ctrl, startIdx, endPos, endPos);
         }
      }

      /// <summary>
      ///   set the focus to the specified control
      /// </summary>
      /// <param name = "ctrl"></param>
      /// <param name = "line"></param>
      public static void SetFocus(MgControlBase ctrl, int line)
      {
         SetFocus(ctrl.getForm().getTask(), ctrl, line, true);
      }

      /// <summary>
      ///   set the focus to the specified control
      /// </summary>
      /// <param name = "itask"></param>
      /// <param name = "ctrl"></param>
      /// <param name = "line"></param>
      /// <param name="activateForm">activate a form or not</param>
      public static void SetFocus(ITask itask, MgControlBase ctrl, int line, bool activateForm)
      {
         var task = (TaskBase) itask;
         if (task.isAborting())
            return;

         Events.OnCtrlFocus(itask, ctrl);

         if (ctrl != null)
         {
            if (!ctrl.isParkable(true, false))
               return;
            if (ctrl.isTreeControl())
               ctrl.TmpEditorIsShow = false;
            Commands.addAsync(CommandType.SET_FOCUS, ctrl, (line >= 0
                                                               ? line
                                                               : ctrl.getDisplayLine(false)), activateForm);
            JSBridge.Instance.SetFocus(task.getTaskTag(), ctrl.UniqueWebId);
         }
         else
         {
            Object formObject = (task.IsSubForm
                                    ? task.getForm().getSubFormCtrl()
                                    : (Object) task.getForm());

            Commands.addAsync(CommandType.SET_FOCUS, formObject, 0, activateForm);
         }
      }

      /// <summary>
      ///   check if a character at the given position is a mask char or not.
      /// </summary>
      /// <param name = "pic"></param>
      /// <param name = "strText"></param>
      /// <param name = "pos"></param>
      /// <returns> true if the character is mask char</returns>
      private static bool charIsMask(PIC pic, String strText, int pos)
      {
          if (pic.isAttrAlphaOrDate())
          {
              if (pic.picIsMask(UtilStrByteMode.convPos(strText, pic.getMask(), pos, false)))
                  return true;
          }
          else if (pic.picIsMask(pos))
              return true;

          return false;
      }

      /// <summary>
      ///   select all the text in the control, but do it immediately using guiInteractive (not the queue).
      /// </summary>
      /// <param name = "ctrl"> </param>
      public static void SetSelect(MgControlBase ctrl)
      {
         if (ctrl.isTextOrTreeEdit())
         {
            // if the control is modifiable, select the whole text in the control,
            // otherwise, unselect all.
            if (ctrl.isModifiable())
            {
               PIC pic = ctrl.getPIC();
               if ( (UtilStrByteMode.isLocaleDefLangJPN() && !pic.isAttrBlob() && pic.getMaskChars() > 0 && !ctrl.isMultiline()) ||
                    (pic.isAttrDateOrTime() && ctrl.isAllBlanks(ctrl.Value)) )
               {
                  String strText = ctrl.Value;
                  int startPos = 0;
                  int endPos = strText.Length;

                  while (startPos < endPos)
                  {
                     if (strText[startPos] == ' ')
                        break;
                     else
                     {
                        if (!charIsMask(pic, strText, startPos))
                            break;
                     }
                     startPos++;
                  }

                  while (startPos < endPos)
                  {
                     if (strText[endPos - 1] != ' ')
                     {
                        if (!charIsMask(pic, strText, endPos - 1))
                           break;
                     }
                     endPos--;
                  }

                  SetSelection(ctrl, 0, 0, 0);  // Cancel the selection to prevent the caret moving to the end of the field.
                  if (endPos != 0)
                     SetSelection(ctrl, startPos, endPos, 0);   // Select the input text exclude the mask characters.
               }
               else
               SetSelection(ctrl, 0, -1, -1);
            }
            else
               SetSelection(ctrl, 0, 0, 0);
         }
      }

      /// <summary>
      ///   unSelect the text in the control
      /// </summary>
      /// <param name = "ctrl">the destination control </param>
      public static void SetUnselect(MgControlBase ctrl)
      {
         if (ctrl.isTextOrTreeEdit())
            Commands.addAsync(CommandType.SELECT_TEXT, ctrl, ctrl.getDisplayLine(true),
                              (int)MarkMode.UNMARK_ALL_TEXT, 0, 0);
      }

      /// <summary>
      ///   return the selection on the control
      /// </summary>
      /// <param name = "ctrl"> </param>
      /// <returns> </returns>
      public static MgPoint SelectionGet(MgControlBase ctrl)
      {
         var point = new MgPoint(0, 0);

         if (ctrl != null && ctrl.isTextOrTreeEdit())
            Commands.selectionGet(ctrl, ctrl.getDisplayLine(true), point);

         return point;
      }

      /// <summary> Open the form. </summary>
      /// <param name="mgForm">form to be opened.</param>
      public static void OpenForm(MgFormBase mgForm)
      {
         mgForm.startupPosition();

         // non interactive can choose not to open the window.
         if (!mgForm.Opened && (mgForm.getTask().IsInteractive || mgForm.getTask().isOpenWin()))
         {
            if (!mgForm.isSubForm())
            {
               // if the form is to be opened as a modal, then increment the modal count on its frame window.
               if (mgForm.isDialog())
               {
                  MgFormBase topMostFrameForm = mgForm.getTopMostFrameForm();
                  if (topMostFrameForm != null)
                     topMostFrameForm.UpdateModalFormsCount(mgForm, true);
               }

               if (mgForm.ParentForm != null)
                  mgForm.ParentForm.ApplyChildWindowPlacement(mgForm);

               Commands.addAsync(CommandType.INITIAL_FORM_LAYOUT, mgForm, mgForm.isDialog(), mgForm.Name);
               ApplyformUserState(mgForm);
            }
            else
            { 
               //layout for subform already was resumend
               if (!mgForm.getTask().ShouldResumeSubformLayout)
                  ApplyformUserState(mgForm);
            }
            

            if (!mgForm.isSubForm())
            {
               Commands.addAsync(CommandType.SHOW_FORM, mgForm, mgForm.isDialog(), false, mgForm.Name);
            }

            // expand does not executed until form is opened, when form is opened tree sends expand events on all expanded nodes.
            // we need to perform this before we perform ensureSelection on the selected node - to make sure that selected node is visible
            // QCR #764980
            // for RTE the mgTree might not exist yet, so skip it. 
            if (mgForm.hasTree() && mgForm.getMgTree() != null)
            {
               mgForm.getMgTree().updateExpandStates(1);
               mgForm.SelectRow(true);
            }

            mgForm.Opened = true;

            Commands.beginInvoke();
         }
      }

      public static void ApplyformUserState(MgFormBase mgForm)
      {
         if (!FormUserState.GetInstance().IsDisabled)
            FormUserState.GetInstance().Apply(mgForm);
      }

      /// <summary> Performs first refresh for the table control </summary>
      public static void DoFirstRefreshTable(MgFormBase mgForm)
      {
         if (mgForm != null && !mgForm.ignoreFirstRefreshTable)
         {
            mgForm.ignoreFirstRefreshTable = true;
            mgForm.firstTableRefresh();
         }
      }

      /// <summary>
      ///   retrieve the location of the caret, within the currently selected text
      /// </summary>
      /// <param name = "ctrl">the control which is assumed to contain the selected text </param>
      public static int CaretPosGet(MgControlBase ctrl)
      {
         int caretPos = 0;

         if (ctrl.isTextOrTreeEdit())
            caretPos = Commands.caretPosGet(ctrl, ctrl.getDisplayLine(true));

         return caretPos;
      }


      /// <summary>
      /// Clear the status bar depending on the control specific status message or form specific status message
      /// </summary>
      /// <param name="task">task</param>
      public static void ClearStatusBar(TaskBase task)
      {
         MgControlBase currentParkedControl = task.getLastParkedCtrl();

         if (currentParkedControl != null && !String.IsNullOrEmpty(currentParkedControl.PromptHelp))
            WriteToMessagePane(task, currentParkedControl.PromptHelp, false);
         else
            CleanMessagePane(task);
      }

      /// <summary>
      ///   clean the status bar
      /// </summary>
      /// <param name = "task"></param>
      public static void CleanMessagePane(TaskBase task)
      {
         WriteToMessagePane(task, GetCurrentRuntimeContext().DefaultStatusMsg, false);
      }

      /// <summary>
      ///   write a message to status bar
      /// </summary>
      /// <param name = "task"></param>
      /// <param name = "msgId">id of message to be written</param>
      /// <param name="soundBeep"></param>
      public static void WriteToMessagePanebyMsgId(TaskBase task, string msgId, bool soundBeep)
      {
         String msg = Events.GetMessageString(msgId);
         WriteToMessagePane(task, msg, soundBeep);
      }

      /// <summary>
      ///   Message string to be displayed on status bar or in Message box
      /// </summary>
      /// <param name = "msg"> input message string </param>
      /// <returns> output message string </returns>
      internal static string GetMessage(string msg)
      {
         if (msg == null)
            msg = "";
         else if (msg.Length > 0)
         {
            int idxOfCarriage = msg.IndexOf("\r");
            if (idxOfCarriage != -1)
               msg = msg.Substring(0, idxOfCarriage);

            int idxOfNewLine = msg.IndexOf("\n");
            if (idxOfNewLine != -1)
               msg = msg.Substring(0, idxOfNewLine);
         }

         return msg;
      }

      /// <summary> Sets the message on the message pane of status bar.</summary>
      /// <param name="task">task</param>
      /// <param name="msg">message to be shown</param>
      /// <param name="soundBeep">to beep or not</param>
      public static void WriteToMessagePane(TaskBase task, String msg, bool soundBeep)
      {
#if !PocketPC
         task.getForm().UpdateStatusBar(Constants.SB_MSG_PANE_LAYER, msg, soundBeep);
#else
         msg = GetMessage(msg);
         if (msg != "")
            Commands.messageBox(null, Events.GetMessageString(MsgInterface.WARNING_STR_WINDOW_TITLE), msg, Styles.MSGBOX_BUTTON_OK);
#endif
         if (!task.isStarted())
            task.setSaveStatusText(msg);
      }

      /// <summary>
      ///   sound a beep
      /// </summary>
      public static void Beep()
      {
         Commands.addAsync(CommandType.BEEP);
         Commands.beginInvoke();
      }

      /// <param name = "val"></param>
      /// <returns> </returns>
      public static bool ClipboardAdd(String val)
      {
         if (val == null)
            return true;

         if (_clipboardData == null)
            _clipboardData = "";

         _clipboardData = _clipboardData + val;
         return true;
      }

      /// <summary>
      /// </summary>
      /// <param name = "currTask"> </param>
      /// <returns> </returns>
      public static bool ClipboardWrite(TaskBase currTask)
      {
         // set a String to the clip.
         ClipboardWrite(null, _clipboardData);
         if (currTask.getLastParkedCtrl() != null)
            currTask.ActionManager.checkPasteEnable(currTask.getLastParkedCtrl());
         return true;
      }

      /// <summary>
      ///   write the clipData string to the clipboard
      ///   if ctrl was passed, use it : copy will be done from selected area on the control to the clip.
      ///   if a String was passed, set it to the clipboard.
      /// </summary>
      /// <param name = "ctrl"> </param>
      /// <param name = "clipData"> </param>
      public static void ClipboardWrite(MgControlBase ctrl, String clipData)
      {
         // both null, do nothing.
         if (ctrl == null && string.IsNullOrEmpty(clipData))
            return;

         // set currRow in any case.
         int currRow = (ctrl == null
                           ? 0
                           : ctrl.getDisplayLine(true));

         Commands.clipboardWrite(ctrl, currRow, clipData);
         _clipboardData = "";
      }

      /// <summary>
      ///   return the content of the clipboard
      /// </summary>
      /// <returns> </returns>
      public static String ClipboardRead()
      {
         return Commands.clipboardRead();
      }

      /// <summary>
      ///   (Korean IME) Send IME Message
      /// </summary>
      /// <param name = "ctrl"> </param>
      /// <param name="im"></param>
      /// <returns> </returns>
      public static int SendImeMessage(MgControlBase ctrl, ImeParam im)
      {
         if (ctrl.isTextControl() && im != null)
            return Commands.sendImeMsg(ctrl, ctrl.getDisplayLine(true), im);

         return 0;
      }

      /// <summary>
      ///   Used to put the text on a text control
      /// </summary>
      /// <param name = "ctrl"> </param>
      /// <param name = "text"> </param>
      public static void SetEditText(MgControlBase ctrl, String text)
      {
         Commands.setEditText(ctrl, ctrl.getDisplayLine(true), text);
      }

      /// <summary>
      /// Insert text to a text control at a given position
      /// </summary>
      /// <param name="ctrl"></param>
      /// <param name="startPosition"></param>
      /// <param name="textToInsert"></param>
      public static void InsertEditText(MgControlBase ctrl, int startPosition, String textToInsert)
      {
         Commands.insertEditText(ctrl, ctrl.getDisplayLine(true), startPosition, textToInsert);
      }

      /// <summary>
      ///   return TooltipTimeout
      /// </summary>
      /// <returns> returns TooltipTimeout</returns>
      public static int GetTooltipTimeout()
      {
         return Environment.GetTooltipTimeout();
      }

        /// <summary>
        /// return SpecialEditLeftAlign
        /// </summary>
        /// <returns></returns>
        public static bool GetSpecialEditLeftAlign()
        {
            return Environment.GetSpecialEditLeftAlign();
        }
      /// <summary>
      ///   Returns default date format depending on date mode and date separator.
      /// </summary>
      /// <returns> String default date format</returns>
        public static String GetDefaultDateFormat()
      {
         String dateFormat = "";
         char dateSeperator = Environment.GetDate();

         // Arrange date format according to the date mode.
         switch (Environment.GetDateMode(0))
         {
            case 'S': // Scandinavian
               dateFormat = "YYYY" + dateSeperator + "MM" + dateSeperator + "DD";
               break;

            case 'B': // Buddhist

            case 'E': // European
               dateFormat = "DD" + dateSeperator + "MM" + dateSeperator + "YYYY";
               break;

            case 'A': // American
               dateFormat = "MM" + dateSeperator + "DD" + dateSeperator + "YYYY";
               break;
         }
         return dateFormat;
      }

      /// <summary>
      ///   Return default time format using time separator.
      /// </summary>
      /// <returns></returns>
      public static String GetDefaultTimeFormat()
      {
         char timeSeperator = Environment.GetTime();
         string timeFormat = "HH" + timeSeperator + "MM" + timeSeperator + "SS";
         return timeFormat;
      }

      /// <summary>
      /// Gets the runtime context belongs to the current thread.
      /// </summary>
      /// <returns>RuntimeContextBase</returns>
      public static RuntimeContextBase GetCurrentRuntimeContext()
      {
         return Events.GetRuntimeContext(GetCurrentContextID());
      }

      /// <summary>
      /// Gets the contextID using the GuiMgForm.
      /// </summary>
      /// <param name="mgObject">Magic Object - GuiMgForm/GuiMgControl/Logical control</param>
      /// <returns>contextID</returns>
      internal static Int64 GetContextID(Object mgObject)
      {
         GuiMgForm guiMgForm = null;
         if (mgObject is LogicalControl)
            guiMgForm = ((LogicalControl)mgObject).GuiMgControl.GuiMgForm;
         else if (mgObject is GuiMgControl)
            guiMgForm = ((GuiMgControl)mgObject).GuiMgForm;
         else if (mgObject is GuiMgForm)
            guiMgForm = (GuiMgForm)mgObject;

         return Events.GetContextID(guiMgForm);
      }

#if !PocketPC
      /// <summary>
      /// Activates a next or previous MDI child
      /// </summary>
      /// <param name="nextWindow">indicates whether to activate next window or not</param>
      public static void ActivateNextOrPreviousMDIChild(bool nextWindow)
      {
         Commands.ActivateNextOrPreviousMDIChild(nextWindow);
      }

      /// <summary>
      ///   Put commands to Gui thread : 
      ///         - PERFORM_DRAGDROP : To initiate the Drag by calling DoDragDrop.
      /// </summary>
      /// <param name="ctrl">Control for which drag operation started</param>
      /// <param name="lineNo">line no on which control resides</param>
      /// <returns></returns>
      public static void BeginDrag(MgControlBase ctrl, MgFormBase mgForm, int lineNo)
      {
         // call DoDragDrop, from GUI Thread.
         Commands.addAsync(CommandType.PERFORM_DRAGDROP, ctrl, lineNo, mgForm);
         Commands.beginInvoke();
      }

      /// <summary>
      ///   Put SETDATA_FOR_DRAG, to set currently selected text as a draggedData
      /// </summary>
      /// <param name="ctrl">Control for which drag operation started</param>
      /// <param name="lineNo">line no on which control resides</param>
      public static void DragSetData(MgControlBase ctrl, int lineNo)
      {
         Commands.addAsync(CommandType.SETDATA_FOR_DRAG, ctrl, lineNo, "", 0);
         Commands.beginInvoke();
      }
#endif
      /// <summary>
      /// Creates a ToolStripMenuItem for menuEntry.
      /// </summary>
      /// <param name="menuReference"></param>
      /// <param name="windowMenuEntry"></param>
      /// <param name="mgForm"></param>
      /// <param name="menuStyle"></param>
      /// <param name="index"></param>
      public static void CreateMenuItem(MenuReference menuReference, GuiMenuEntry menuEntry, GuiMgForm mgForm, MenuStyle menuStyle, int index)
      {
         GuiCommandQueue.getInstance().createMenuItem(menuEntry, menuReference, menuStyle, true, index, mgForm);
      }

      /// <summary>
      ///  Delete a ToolStripMenuItem.
      /// </summary>
      /// <param name="menuReference"></param>
      public static void DeleteMenuItem(MenuReference menuReference)
      {
#if !PocketPC
         GuiUtils.DeleteMenuItem(menuReference);
#endif
      }

      /// <summary>
      /// Shows windows help.
      /// </summary>
      /// <param name="filePath">path of the help file.</param>
      /// <param name="helpCmd">help command.</param>
      /// <param name="helpKey">search key word.</param>
      public static void ShowWindowHelp(string filePath, HelpCommand helpCmd, string helpKey)
      {
#if !PocketPC
         helpKey = helpKey.Trim();
         switch (helpCmd)
         {
            case HelpCommand.Context:
               HelpNavigator navigator = HelpNavigator.TopicId;
               Help.ShowHelp(null, filePath, navigator, helpKey);
               break;
            case HelpCommand.Contents:
               navigator = HelpNavigator.TableOfContents;
               Help.ShowHelp(null, filePath, navigator, helpKey);
               break;
            case HelpCommand.Setcontents:
               navigator = HelpNavigator.TableOfContents;
               Help.ShowHelp(null, filePath, navigator, helpKey);
               break;
            case HelpCommand.Contextpopup:
               navigator = HelpNavigator.Index;
               Help.ShowHelp(null, filePath, navigator, helpKey);
               break;
            case HelpCommand.Key:
               navigator = HelpNavigator.KeywordIndex;
               Help.ShowHelp(null, filePath, navigator, helpKey);
               break;
            case HelpCommand.Command:
               navigator = HelpNavigator.Index;
               Help.ShowHelp(null, filePath, navigator, helpKey);
               break;
            case HelpCommand.Forcefile:
               navigator = HelpNavigator.TableOfContents;
               Help.ShowHelp(null, filePath, navigator, helpKey);
               break;
            case HelpCommand.Helponhelp:
               navigator = HelpNavigator.Index;
               Help.ShowHelp(null, filePath, navigator, helpKey);
               break;
            case HelpCommand.Quit:
               break;
         }
#endif
       }
   }
}
