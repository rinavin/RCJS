using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;
using com.magicsoftware.win32;
using com.magicsoftware.controls;
using com.magicsoftware.unipaas.env;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.gui;
#if !PocketPC
using System.Text;
using System.Runtime.InteropServices;
using com.magicsoftware.support;
#else
using SendKeys = OpenNETCF.Windows.Forms.SendKeys;
using System.Collections.Generic;
using Monitor = com.magicsoftware.richclient.mobile.util.Monitor;
using PointF = com.magicsoftware.mobilestubs.PointF;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
#if !PocketPC
   using System.IO;
   using System.Collections.Generic;
   using System.Collections;
   using Gui.com.magicsoftware.unipaas.management.gui;

   /// <summary> GuiUtils class provides methods for retrieving information from the GUI layer</summary>
   internal sealed class GuiInteractive : GuiInteractiveBase
   {
      /// methods confirmed as different between the GuiInteractive classes of the standard/compact frameworks
      #region DifferentiationConfirmed

      /// <summary> handle the message box</summary>
      internal override void onMessageBox()
      {
         Boolean newForm = false;
         Form form = null;
         // if the object is null then get the active shell, otherwise get the Shell from the ControlsMap
         if (_obj2 == null)
         {
            form = GuiUtils.getActiveForm();
            //If the Startup screen is open, it means that it is only form open as of now.
            //So, the Active form will be the same StartupScreen.
            //But, StartupScreen doesn't have taskbar icon. So, it is used for displaying the 
            //messagebox, there will not be any taskbar icon.
            //Solution: Create the dummy form even in this case.
            if (form == null || GUIMain.getInstance().IsStartupScreenOpen())
            {
               form = GuiUtils.createDummyForm();
               newForm = true;
            }
         }
         else
         {
            Control control;
            ControlsMap controlsMap = ControlsMap.getInstance();

            control = (Control)controlsMap.object2Widget(_obj2);

            form = GuiUtils.FindForm(control);
         }

         if (!(form is Form))
            throw new ApplicationException("in GuiCommandQueue.writeToMessageBox(): control is not instanceof Shell");
         else
            writeToMessageBox(form, _mgValue.title, _mgValue.str, _mgValue.style);
         if (newForm)
            form.Dispose();
      }


      /// <param name="form"></param>
      /// <param name="title"></param>
      /// <param name="str"></param>
      /// <param name="mode"></param>
      private void writeToMessageBox(Form form, String title, String str, int mode)
      {
         form = GuiUtils.FindForm(form);
         bool newForm = false;
         if (!form.Visible)
         {
            form = GuiUtils.createDummyForm();
            newForm = true;
         }

         MessageBoxButtons Buttons = Mode2MsgBoxButtons(mode);
         MessageBoxIcon Icon = Mode2MsgBoxIcon(mode);
         MessageBoxDefaultButton DefaultButton = Mode2MsgBoxDefaultBtn(mode);

         DialogResult Result = DialogResult.Cancel;

         // fixed defect #135138 : Text and image in verify message need to displayeright to left in Hebrew
         if (Manager.Environment != null && Manager.Environment.Language == 'H')
            Result = MessageBox.Show(form, str, title, Buttons, Icon, DefaultButton, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
         else
            Result = MessageBox.Show(form, str, title, Buttons, Icon, DefaultButton);

         _mgValue.number = DialogResult2MsgBoxResult(Result);
         if (newForm)
            form.Dispose();
      }

      /// <summary> Remove the ValueChangedHandler. </summary>
      protected override void OnRemoveDNControlValueChangedHandler()
      {
         Control dotNetControl = (Control)ControlsMap.getInstance().object2Widget(_obj2);

         ReflectionServices.RemoveDNControlValueChangedHandler(dotNetControl);
      }

      /// <summary> Add the ValueChangedHandler. </summary>
      protected override void OnAddDNControlValueChangedHandler()
      {
         Control dotNetControl = (Control)ControlsMap.getInstance().object2Widget(_obj2);

         ReflectionServices.AddDNControlValueChangedHandler(dotNetControl);
      }

      /// <summary> Gets the RTF value of the RTF edit control which was set before entering it. </summary>
      protected override void OnGetRtfValueBeforeEnteringControl()
      {
         Object obj = ControlsMap.getInstance().object2Widget(_obj2, _line);
         MgRichTextBox mgRichTextBox = null;

         _mgValue.str = String.Empty;

         if (obj is MgRichTextBox)
            mgRichTextBox = (MgRichTextBox)obj;
         else if (obj is LogicalControl)
            mgRichTextBox = (MgRichTextBox)((LogicalControl)obj).getEditorControl();

         if (mgRichTextBox != null)
            _mgValue.str = ((TagData)mgRichTextBox.Tag).RtfValueBeforeEnteringControl;
      }

      #endregion //DifferentiationConfirmed

      /// methods suspected as different between the GuiInteractive classes of the standard/compact frameworks
      #region DifferentiationSuspected

      /// <summary> </summary>
      internal override void onDesktopBounds()
      {
         Control control = null;

         if (_obj2 != null)
         {
            control = (Control)ControlsMap.getInstance().object2Widget(_obj2);
         }
         else
         {
            //check for the MDI window
            foreach (Form item in Application.OpenForms)
            {
               if (item.IsMdiContainer)
               {
                  control = item;
                  break;
               }
            }
         }

         Screen screen = control != null ? Screen.FromControl(control) : Screen.PrimaryScreen;
         _rect = screen.Bounds;
      }

      /// <summary> get the text(html) on the browser control</summary>
      internal override void onGetBrowserText()
      {
         Object obj = ControlsMap.getInstance().object2Widget(_obj2);
         if (obj is WebBrowser)
            _mgValue.str = ((WebBrowser)obj).DocumentText;
      }

      /// <summary> browser execute</summary>
      internal override void onBrowserExecute()
      {
         WebBrowser browser = ControlsMap.getInstance().object2Widget(_obj2) as WebBrowser;
         string language = (string)_mgValue.obj;

         _mgValue.boolVal = false;

         if (browser != null && browser.Document != null && browser.ReadyState == WebBrowserReadyState.Complete)
         {
            int hashCode = ReflectionServices.GetHashCode("Microsoft.mshtml");
            Type type = ReflectionServices.GetType(hashCode, "mshtml.IHTMLWindow2");
            if (type != null)
            {
               MemberInfo memberInfo = ReflectionServices.GetMemeberInfo(type, "execScript", false, null);

               if (memberInfo is MethodInfo)
               {
                  MethodInfo methodInfo = (MethodInfo)memberInfo;

                  if (methodInfo != null)
                  {
                     var argArray = new Object[2];
                     argArray[0] = _mgValue.str;
                     argArray[1] = language;

                     try
                     {
                        ReflectionServices.InvokeMethod(methodInfo, browser.Document.Window.DomWindow, argArray, true);
                        _mgValue.boolVal = true;
                     }
                     catch (Exception exception)
                     {
                        Events.WriteExceptionToLog("Exception in BrowserScriptExecute(): " + exception.Message);
                     }
                  }
               }
            }
            else
               Events.WriteErrorToLog("Execution of BrowserScriptExecute() failed because Microsoft.MSHTML assembly could not be loaded.");
         }
      }

      /// <summary> Handle the directory dialog box</summary>
      internal override void onDirectoryDialogBox()
      {
         _mgValue.str = "";

         MgFolderBrowserDialog dirDlg = new MgFolderBrowserDialog();
         dirDlg.ShowNewFolderButton = _mgValue.bool1;
         // Can't change the caption, set the description string
         dirDlg.Description = _mgValue.caption;

         // If path is null, or does not exist, use current directory
         if (!String.IsNullOrEmpty(_mgValue.path))
         {
            DirectoryInfo di = new DirectoryInfo(_mgValue.path);
            if (di.Exists)
               dirDlg.SelectedPath = _mgValue.path;
         }
         if (String.IsNullOrEmpty(dirDlg.SelectedPath))
            dirDlg.SelectedPath = Directory.GetCurrentDirectory();

         if (dirDlg.ShowDialog() == DialogResult.OK)
            _mgValue.str = dirDlg.SelectedPath;
      }

      #endregion //DifferentiationSuspected
   }

#else //PocketPC
   /// <summary> GuiUtils class provides methods for retrieving information from the GUI layer</summary>
   internal sealed class GuiInteractive : GuiInteractiveBase
   {
      /// methods confirmed as different between the GuiInteractive classes of the standard/compact frameworks
   #region DifferentiationConfirmed

      internal override void onMessageBox()
      {
         MessageBoxButtons Buttons = Mode2MsgBoxButtons(_mgValue.style);
         MessageBoxIcon Icon = Mode2MsgBoxIcon(_mgValue.style);
         MessageBoxDefaultButton DefaultButton = Mode2MsgBoxDefaultBtn(_mgValue.style);

         DialogResult Result = MessageBox.Show(_mgValue.str, _mgValue.title, Buttons, Icon, DefaultButton);

         _mgValue.number = DialogResult2MsgBoxResult(Result);
      }

   #endregion //DifferentiationConfirmed

      /// methods suspected as different between the GuiInteractive classes of the standard/compact frameworks
   #region DifferentiationSuspected

      internal override void onGetBrowserText()
      {
         Object obj = ControlsMap.getInstance().object2Widget(_obj2);
         if (obj is MgWebBrowser)
            Events.WriteErrorToLog("onGetBrowserText - Not Implemented Yet");
      }

      internal override void onBrowserExecute()
      {
         MgWebBrowser browser = ControlsMap.getInstance().object2Widget(_obj2) as MgWebBrowser;

         if (browser != null)
         {
            Events.WriteErrorToLog("onBrowserExecute - Not Implemented Yet");
         }
      }

      internal override void onDirectoryDialogBox()
      {
         throw new NotImplementedException();
      }

      internal override void onDesktopBounds()
      {
#if !PocketPC
         Control control = null;
         
         if (_obj2 != null)
         {
            control = (Control)ControlsMap.getInstance().object2Widget(_obj2);
            Screen screen = control != null ? Screen.FromControl(control) : Screen.PrimaryScreen;
            _rect = screen.Bounds;
         }
         else
#endif
         {
            _rect.Width = Screen.PrimaryScreen.Bounds.Width;
            _rect.Height = Screen.PrimaryScreen.Bounds.Height;
         }
      }

   #endregion //DifferentiationSuspected
   }
#endif

   /// <summary> GuiUtils class provides methods for retrieving information from the GUI layer</summary>
   internal abstract class GuiInteractiveBase
   {
      private CommandType _commandType;

      private int _x;
      private int _y;
      private Char _char;
      private String _str;
      private int _intVal1;
      protected int _line;
      private Object _obj1;
      private MemberInfo _memberInfo;
      private Object[] _parameters;
      protected bool _boolVal;
      protected Rectangle _rect;
      protected Object _obj2;
      protected MgValue _mgValue;
      protected bool _setValue;
      protected Int64 _contextID;
      protected PointF _pointF;

      static Object lockObject = new Object();
      private enum CommandType
      {
         GET_FONT_METRICS,
         GET_RESOLUTION,
         GET_BOUNDS,
         GET_DESKTOP_BOUNDS,
         GET_VALUE,
         SET_BROWSER_TEXT,
         GET_BROWSER_TEXT,
         BROWSER_EXECUTE,
         GET_TOP_INDEX,
         MESSAGE_BOX,
         DIRECTORY_DIALOG_BOX,
         GET_CLIENT_BOUNDS,
         GET_BOUNDS_RELATIVE_TO,
         SET_EDIT_TEXT,
         INSERT_EDIT_TEXT,
         SET_SELECTION,
         GET_SELECTION,
         GET_CARET_POS,
         GET_IS_TOP_OF_TEXTBOX,
         GET_IS_END_OF_TEXTBOX,
         CLIPBOARD_GET_CONTENT,
         CLIPBOARD_SET_CONTENT,
         CLIPBOARD_PASTE,
         POST_KEY_EVENT,
         POST_CHAR_EVENT,
         SET_CURSOR,
         CHECK_AUTO_WIDE,
         DISPOSE_ALL_FORMS,
         GET_ROWS_IN_PAGE,
         GET_HIDDEN_ROWS_COUNT_IN_TABLE,
         FILE_OPEN_DIALOG_BOX,
         FILE_SAVE_DIALOG_BOX,
         CREATE_DIALOG,
         OPEN_DIALOG,
         CLOSE_DIALOG,
         SET_GET_SUGGESTED_VALUE_FOR_CHOICE_CONTROL_ON_TAGDATA,
#if PocketPC
         GET_ACCUMULATED_TEXT,
#endif
         REFLECTION_INVOKE,
         REFLECTION_SET,
         GET_LAST_WINDOW_STATE,
         GET_FRAMES_BOUNDS,
         GET_LINKED_PARENT_IDX,
         GET_FORM_BOUNDS,
         GET_COLUMNS_STATE,
         GET_FORM_HANDLE,
         GET_CTRL_HANDLE,
         POINT_TO_CLIENT,
         POINT_IN_MONITOR,
         GET_LEFT_TOP_FORM_MONITOR,
#if !PocketPC
         IS_FORM_ACTIVE,
         GET_DROPPED_DATA,
         GET_DROPPED_POINT,
         GET_DROPPED_SELECTION,
         DROP_FORMAT_SUPPORTED,
         GET_IS_BEGIN_DRAG,
         SET_MARKED_TEXT_ON_RICH_EDIT,
         ACTIVATE_NEXT_OR_PREVIOUS_MDI_CHILD,
#endif
         SEND_IME_MSG,
         CAN_FOCUS,
         INVOKE_OSCOMMAND,
         GET_SELECTED_INDICE,
         SET_SUGGESTED_VALUE,
         MAP_WIDGET_TO_GUI_OBJECT,
         REMOVE_DN_CONTROL_VALUE_CHANGED_HANDLER,
         ADD_DN_CONTROL_VALUE_CHANGED_HANDLER,
         POINT_TO_SCREEN,
         GET_MDI_CLIENT_BOUNDS,
         GET_RTF_VALUE_BEFORE_ENTERING_CONTROL,
         ATTACH_DNKEY_TO_OBJECT,
         GET_MDI_CHILD_COUNT,
         GET_DVCONTROL_POSITION_ISN,
         CLEAR_DATA_TABLE,
         SET_DATA_SOURCE_TO_DVCONTROL,
         ACTIVATE_FORM,
         ENABLE_MENU_ENTRY,
         SHOW_CONTEXT_MENU,
         IS_COMBO_DROPPED_DOWN,
         OPEN_FORM_DESIGNER,
         INVOKE_UDPCOMMAND,
         GET_HASINDENT
      }

      internal delegate void GuiInteractiveDelegate();

      /// <summary>
      /// Constructor : sets the contextID for the current command.
      /// </summary>
      public GuiInteractiveBase()
      {
         _contextID = Manager.GetCurrentContextID();
      }

      /// <summary> Returns whether control is focusable or not. </summary>
      /// <param name="guiMgControl"> the control which needs to be checked. </param>
      /// <returns></returns>
      internal bool canFocus(GuiMgControl guiMgControl)
      {
         _commandType = CommandType.CAN_FOCUS;
         _obj1 = guiMgControl;

         _mgValue = new MgValue();

         Invoke();

         return _mgValue.boolVal;
      }

      /// <summary> /// Returns the comma separated string for selected indice of list control./// </summary>
      /// <param name="guiMgControl"></param>
      /// <returns>comma separated string for selected indice of list control</returns>
      internal string GetSelectedIndice(GuiMgControl guiMgControl)
      {
         _commandType = CommandType.GET_SELECTED_INDICE;
         _obj1 = guiMgControl;

         _mgValue = new MgValue();

         Invoke();

         return _mgValue.str;
      }

      /// <summary>
      /// Returns whether the indent has been applied to Rich Edit
      /// </summary>
      /// <param name="guiMgControl"></param>
      /// <returns></returns>
      internal bool GetHasIndent(GuiMgControl guiMgControl)
      {
         _commandType = CommandType.GET_HASINDENT;
         _obj1 = guiMgControl;
         Invoke();
         return _boolVal;
      }

      /// <summary> getFontMetrics() calculates the font size</summary>
      /// <param name="fontIdx">the index of the font in the fonts table</param>
      /// <param name="object">a reference to the relevant object</param>
      /// <param name="fontSize">a reference to the object that on return will contain the requested values, where x is the
      /// font average width and y is the font height</param>
      internal MgPointF getFontMetrics(MgFont mgFont, Object obj)
      {
         _commandType = CommandType.GET_FONT_METRICS;
         _obj1 = mgFont;
         _obj2 = obj;
         Invoke();
         return new MgPointF(_pointF.X, _pointF.Y);
      }

      /// <summary> Gets the resolution of the control. </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      internal MgPoint getResolution(Object obj)
      {
         _commandType = CommandType.GET_RESOLUTION;
         _obj1 = obj;
         Invoke();

         return new MgPoint(_x, _y);
      }

      /// <summary> getBounds() get the bounds of the object</summary>
      /// <param name="object">a reference to the relevant object</param>
      /// <param name="rect">a reference to the object that on return will contain the requested values</param>
      internal void getBounds(Object obj, MgRectangle rect)
      {
         _commandType = CommandType.GET_BOUNDS;
         _obj2 = obj;
         Invoke();
         rect.x = _rect.X;
         rect.y = _rect.Y;
         rect.width = _rect.Width;
         rect.height = _rect.Height;
      }

      /// <summary>Functions returns the handle of the form.
      /// </summary>
      /// <param name="mgForm">Object of the magic form whose window handle is required.</param>
      /// <returns>handle of the form. </returns>
      internal int getFormHandle(GuiMgForm guiMgForm)
      {
         _commandType = CommandType.GET_FORM_HANDLE;
         _obj2 = guiMgForm;
         Invoke();

         return _intVal1;
      }

      /// <summary>Functions returns the handle of the control.
      /// </summary>
      /// <param name="mgControl">Object of the magic control whose window handle is required.</param>
      /// <returns>handle of the control</returns>
      internal int getCtrlHandle(GuiMgControl guiMgControl, int line)
      {
         _commandType = CommandType.GET_CTRL_HANDLE;
         _obj2 = guiMgControl;
         _line = line;
         Invoke();

         return _intVal1;
      }

      /// <summary>set cursor according to cursor shape</summary>
      /// <param name="shape"></param>
      /// <returns></returns>
      internal bool setCursor(MgCursors shape)
      {
         _commandType = CommandType.SET_CURSOR;
         _intVal1 = (int)shape;
         _mgValue = new MgValue();

         Invoke();
         return _mgValue.boolVal;
      }

      /// <summary>
      /// This methode is set TRUE\FALSE that GuiUtiles\GetValue() method will be use
      /// this falg say if to return the : (true) suggested value or (false)the real value
      /// this method is use for MG_ACT_CTRL_MODIFY
      /// </summary>
      /// <param name="ctrl"></param>
      /// <param name="retSuggestedValue"></param>
      internal void setGetSuggestedValueOfChoiceControlOnTagData(GuiMgControl ctrl, int line, bool retSuggestedValue)
      {
         _commandType = CommandType.SET_GET_SUGGESTED_VALUE_FOR_CHOICE_CONTROL_ON_TAGDATA;
         _obj2 = ctrl;
         _line = line;
         _boolVal = retSuggestedValue;
         Invoke();
      }

      /// <summary> getBoundsReletiveToShell() get the bounds of the object retetive to shell</summary>
      /// <param name="object">a reference to the relevant object</param>
      /// <param name="rect">a reference to the object that on return will contain the requested values</param>
      /// <param name="relativeTo">the relative to whom ?, if relativeTo is null it return relative to desktop</param
      internal void getBoundsRelativeTo(Object obj, int line, MgRectangle rect, Object relativeTo)
      {
         _commandType = CommandType.GET_BOUNDS_RELATIVE_TO;
         _obj2 = obj;
         _obj1 = relativeTo;
         _line = line;
         Invoke();
         rect.x = _rect.X;
         rect.y = _rect.Y;
         rect.width = _rect.Width;
         rect.height = _rect.Height;
      }

      ///  PointToClient() convert point to client of the relativeTo control
      /// <relativeTo:>  the relativ to hom ?, if relativeTo is null it return relative to desktop </relativeTo:>
      internal void PointToClient(Object relativeTo, MgPoint convrtPoint)
      {
         _commandType = CommandType.POINT_TO_CLIENT;
         _obj2 = relativeTo;
         _x = convrtPoint.x;
         _y = convrtPoint.y;
         Invoke();
         convrtPoint.x = _x;
         convrtPoint.y = _y;
      }

      /// <summary> converts relative point into screen point</summary>
      /// <param name="relativeTo">Reference to the relevant object</param>
      /// <param name="convrtPoint"></param>
      internal void PointToScreen(Object relativeTo, MgPoint convrtPoint)
      {
         _commandType = CommandType.POINT_TO_SCREEN;
         _obj2 = relativeTo;
         _x = convrtPoint.x;
         _y = convrtPoint.y;
         Invoke();
         convrtPoint.x = _x;
         convrtPoint.y = _y;
      }

      /// <summary>
      /// Returns minimum location considering all monitors
      /// </summary>
      /// <param name="point"></param>
      /// <returns></returns>
      internal bool IsPointInMonitor(MgPoint point)
      {
         _commandType = CommandType.POINT_IN_MONITOR;
         _x = point.x;
         _y = point.y;
         Invoke();
         return _boolVal;
      }

      /// <summary>
      /// Returns LeftTop location of monitor containing point passed as parameter
      /// </summary>
      /// <param name="point"></param>
      /// <returns></returns>
      internal MgPoint GetLeftTopLocationFormMonitor(MgFormBase parentForm)
      {
         _commandType = CommandType.GET_LEFT_TOP_FORM_MONITOR;
         _obj1 = parentForm;
         Invoke();
         return new MgPoint(_x, _y);
      }

      /// <summary>
      /// Get bounds of MdiClient
      /// </summary>
      /// <returns>ClientRectangle of MdiClient</returns>
      internal MgRectangle GetMdiClientBounds()
      {
         _commandType = CommandType.GET_MDI_CLIENT_BOUNDS;
         Invoke();
         return new MgRectangle(_rect.X, _rect.Y, _rect.Width, _rect.Height);
      }

      /// <summary> getClientBounds() get the client bounds of the object</summary>
      /// <param name="object">a reference to the relevant object</param>
      /// <param name="rect">a reference to the object that on return will contain the requested values</param>
      internal void getClientBounds(Object obj, MgRectangle rect, bool clientPanelOnly)
      {
         _commandType = CommandType.GET_CLIENT_BOUNDS;
         _obj2 = obj;
         _boolVal = clientPanelOnly;
         Invoke();
         rect.x = _rect.X;
         rect.y = _rect.Y;
         rect.width = _rect.Width;
         rect.height = _rect.Height;
      }

      /// <summary> getBounds() get the bounds of the object</summary>
      /// <param name="rect">a reference to the object that on return will contain the requested values</param>
      /// <param name="form"> form</param>
      internal void getDesktopBounds(MgRectangle rect, object form)
      {
         _commandType = CommandType.GET_DESKTOP_BOUNDS;
         _obj2 = form;
         Invoke();
         rect.x = _rect.X;
         rect.y = _rect.Y;
         rect.width = _rect.Width;
         rect.height = _rect.Height;
      }

      /// <summary> Get value of the control</summary>
      /// <param name="object">a reference to the relevant object</param>
      /// <param name="rect">a reference to the object that on return will contain the requested values on string</param>
      internal String getValue(Object obj, int line)
      {
         _commandType = CommandType.GET_VALUE;
         _obj2 = obj;
         _line = line;
         _mgValue = new MgValue();
         Invoke();
         return _mgValue.str;
      }

      /// <summary> </summary>
      internal bool setBrowserText(GuiMgControl browserControl, String text)
      {
         _commandType = CommandType.SET_BROWSER_TEXT;
         _obj2 = browserControl;
         _mgValue = new MgValue();
         _mgValue.str = text;
         Invoke();
         return _mgValue.boolVal;
      }

      /// <summary> </summary>
      internal String getBrowserText(GuiMgControl browserControl)
      {
         _commandType = CommandType.GET_BROWSER_TEXT;
         _obj2 = browserControl;
         _mgValue = new MgValue();
         Invoke();
         return _mgValue.str;
      }

      internal int getTopIndex(GuiMgControl tablecontrol)
      {
         _commandType = CommandType.GET_TOP_INDEX;
         _obj2 = tablecontrol;
         Invoke();
         return _intVal1;
      }

      /// <param name="syncExec">TODO</param>
      internal bool browserExecute(GuiMgControl browserControl, String text, bool syncExec, String language)
      {
         _commandType = CommandType.BROWSER_EXECUTE;
         _obj2 = browserControl;
         _mgValue = new MgValue();
         _mgValue.str = text;
         _mgValue.obj = language;
         //For async execution, return true if the command is passed to the gui thread.
         //For sync execution, return true if the command was executed by the gui thread. 
         if (syncExec)
         {
            Invoke();
            return _mgValue.boolVal;
         }
         else
         {
            GUIMain.getInstance().beginInvoke(new GuiInteractiveDelegate(Run));
            return true;
         }
      }

      /// <summary>Open message box with parent form</summary>
      /// <param name="obj">the parent form of the message box</param>
      /// <param name="title"></param>
      /// <param name="msg"></param>
      /// <param name="style">the style of the message box can be flags of Styles.MSGBOX_xxx </param>
      /// <returns> message box style from Styles.MSGBOX_XXX</returns>
      internal int messageBox(GuiMgForm topMostForm, String title, String msg, int style)
      {
         _commandType = CommandType.MESSAGE_BOX;
         _mgValue = new MgValue();
         _obj2 = topMostForm;
         _mgValue.title = title;
         _mgValue.str = (msg == null ? "" : msg);
         _mgValue.style = style;
         Events.RefreshTables();
         Invoke();
         return _mgValue.number;
      }

      ///<summary>
      /// Handles Invoke UDP operation from GUI thread
      ///</summary>
      ///<param name="contextId">Context id</param>
      internal int invokeUDP(double contextId)
      {
         _commandType = CommandType.INVOKE_UDPCOMMAND;
         _mgValue = new MgValue();
         _mgValue.obj = contextId;
         Invoke();
         return _mgValue.number;
      }

      /// <summary>
      /// invoke reflection command on gui thread
      /// </summary>
      /// <param name="memberInfo"></param>
      /// <param name="obj"></param>
      /// <param name="parameters"></param>
      /// <returns></returns>
      internal Object ReflectionInvoke(MemberInfo memberInfo, Object obj, Object[] parameters)
      {
         return ReflectionInvoke(memberInfo, obj, parameters, false);
      }

      /// <summary>
      /// Attach dotnet key to Object
      /// </summary>
      /// <param name="mgControl"></param>
      /// <param name="key"></param>
      internal void AttachDnKeyToObject(GuiMgControl mgControl, int key)
      {
         _commandType = CommandType.ATTACH_DNKEY_TO_OBJECT;
         _obj1 = mgControl;
         _intVal1 = key;
         Invoke();
      }

      /// <summary>
      /// invoke reflection command on gui thread
      /// </summary>
      /// <param name="memberInfo"></param>
      /// <param name="obj"></param>
      /// <param name="parameters"></param>
      /// <param name="isValueTypeDefaultCtor"></param>
      /// <returns></returns>
      internal Object ReflectionInvoke(MemberInfo memberInfo, Object obj, Object[] parameters, bool isValueTypeDefaultCtor)
      {
         bool prevAllowFormsLock = Events.AllowFormsLock();
         if (prevAllowFormsLock)
            Events.AllowFormsLock(false);

         _commandType = CommandType.REFLECTION_INVOKE;
         _obj2 = obj;
         _memberInfo = memberInfo;
         _parameters = parameters;
         _boolVal = isValueTypeDefaultCtor;

         _mgValue = new MgValue();
         Invoke();
         Events.AllowFormsLock(prevAllowFormsLock);
         return _mgValue.obj;
      }

      /// <summary>
      /// invoke reflection command on gui thread to set a value
      /// </summary>
      /// <param name="memberInfo"></param>
      /// <param name="obj"></param>
      /// <param name="parameters"></param>
      /// <returns></returns>
      internal Object ReflectionSet(MemberInfo memberInfo, Object obj, Object[] parameters, Object value)
      {
         bool prevAllowFormsLock = Events.AllowFormsLock();
         if (prevAllowFormsLock)
            Events.AllowFormsLock(false);

         _commandType = CommandType.REFLECTION_SET;
         _obj2 = obj;
         _memberInfo = memberInfo;
         _parameters = parameters;
         _setValue = true;
         _obj1 = value;

         _mgValue = new MgValue();
         Invoke();
         Events.AllowFormsLock(prevAllowFormsLock);
         return _mgValue.obj;
      }

      /// <summary> Directory Dialog Box
      /// 
      /// </summary>
      /// <param name="form"></param>
      /// <param name="caption">description for the dialog window</param>
      /// <param name="path">initial path to browse</param>
      /// <param name="bShowNewFolder">should show the new folder button?</param>
      /// <returns> directory path selected by user</returns>
      internal String directoryDialogBox(String caption, String path, Boolean bShowNewFolder)
      {
         _commandType = CommandType.DIRECTORY_DIALOG_BOX;

         _mgValue = new MgValue();
         _mgValue.caption = caption;
         _mgValue.path = path;
         _mgValue.bool1 = bShowNewFolder;

         Invoke();
         return _mgValue.str;
      }

      /// <summary>
      /// File Open Dialog Box
      /// </summary>
      /// <param name="title">Dialog window caption</param>
      /// <param name="initDir">Initial directory</param>
      /// <param name="filterNames">filter string</param>
      /// <param name="checkExists">verify opened file exists</param>
      /// <param name="multiSelect">enable selecting multiple files</param>
      /// <returns>file path selected by user</returns>
      internal String fileOpenDialogBox(String title, String dirName, String fileName, String filterNames,
                                        Boolean checkExists, Boolean multiSelect)
      {
         _commandType = CommandType.FILE_OPEN_DIALOG_BOX;

         _mgValue = new MgValue();
         _mgValue.title = title;
         _mgValue.path = dirName;
         _mgValue.str = fileName;
         _mgValue.filter = filterNames;
         _mgValue.boolVal = checkExists;
         _mgValue.bool1 = multiSelect;

         Invoke();
         return _mgValue.str;
      }

      /// <summary>
      /// File Save Dialog Box
      /// </summary>
      /// <param name="title">caption of the dialog window</param>
      /// <param name="initDir"> initial directory</param>
      /// <param name="filterNames">filter string</param>
      /// <param name="defaultExtension"> default extension for file name</param>
      /// <param name="overwritePrompt"> should prompt when overwriting an existing file?</param>
      /// <param name="retFile"></param>
      /// <returns>file path selected by user</returns>
      internal String fileSaveDialogBox(String title, String dirName, String fileName, String filterNames,
                                        String defaultExtension, Boolean overwritePrompt)
      {
         _commandType = CommandType.FILE_SAVE_DIALOG_BOX;

         _mgValue = new MgValue();
         _mgValue.title = title;
         _mgValue.path = dirName;
         _mgValue.str = fileName;
         _mgValue.filter = filterNames;
         _mgValue.caption = defaultExtension;
         _mgValue.bool1 = overwritePrompt;

         Invoke();
         return _mgValue.str;
      }

      /// <summary>Put Command to create dialog</summary>
      /// <param name="handle">reference to the dialog handlers</param>
      /// <param name="objType">parameters to be passed to objects constructor</param>
      /// <param name="parameters"></param>
      internal void createDialog(DialogHandler handle, Type objType, Object[] parameters)
      {
         _commandType = CommandType.CREATE_DIALOG;
         _parameters = parameters;
         _obj2 = handle;
         _obj1 = objType;

         Invoke();
      }

      /// <summary>Put Command to open dialog</summary>
      /// <param name="dialog"></param>
      internal DialogResult openDialog(DialogHandler handle)
      {
         _commandType = CommandType.OPEN_DIALOG;
         _obj2 = handle;

         Invoke();

         return (DialogResult)_obj1;
      }

      /// <summary>Put Command to close dialog</summary>
      /// <param name="dialog"></param>
      internal void closeDialog(DialogHandler handle)
      {
         _commandType = CommandType.CLOSE_DIALOG;
         _obj2 = handle;

         Invoke();
      }

      /// <summary> set the text to the the control
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="line"></param>
      /// <param name="text"></param>
      internal bool setEditText(GuiMgControl control, int line, String text)
      {
         _commandType = CommandType.SET_EDIT_TEXT;
         _obj2 = control;
         _line = line;
         _mgValue = new MgValue();
         _mgValue.str = text;

         Invoke();

         return _mgValue.boolVal;
      }

      /// <summary> insert the text to the the control at the given position
      /// 
      /// </summary>
      /// <param name="control"></param>
      /// <param name="line"></param>
      /// <param name="text"></param>
      internal bool insertEditText(GuiMgControl control, int line, int startPosition, String textToInsert)
      {
         _commandType = CommandType.INSERT_EDIT_TEXT;
         _obj2 = control;
         _line = line;
         _mgValue = new MgValue();
         _mgValue.str = textToInsert;
         _intVal1 = startPosition;

         Invoke();

         return _mgValue.boolVal;
      }
      /// <summary> set the text to the the control</summary>
      /// <param name="control"></param>
      /// <param name="line"></param>
      /// <param name="text"></param>
      internal void setSelection(GuiMgControl control, int line, int start, int end, int caretPos)
      {
         _commandType = CommandType.SET_SELECTION;
         _obj2 = control;
         _line = line;
         _x = start;
         _y = end;
         _mgValue = new MgValue();
         _mgValue.number = caretPos;

         Invoke();
      }

      /// <summary> set the text to the the control</summary>
      /// <param name="control"></param>
      /// <param name="line"></param>
      /// <param name="text"></param>
      internal void setSuggestedValue(GuiMgControl control, string suggestedValue)
      {
         _commandType = CommandType.SET_SUGGESTED_VALUE;
         _obj2 = control;
         _mgValue = new MgValue();
         _mgValue.str = suggestedValue;

         Invoke();
      }

      /// <summary> 
      /// get the position of the caret on the control
      /// </summary>
      /// <param name="control"></param>
      /// <param name="line"></param>
      internal int caretPosGet(GuiMgControl control, int line)
      {
         _commandType = CommandType.GET_CARET_POS;
         _obj2 = control;
         _line = line;

         _mgValue = new MgValue();
         Invoke();

         return _mgValue.number;
      }

      /// <summary> 
      /// check if the Carel is Positioned on the first line in TextBox.</summary>
      /// <param name="control"></param>
      /// <param name="line"></param>
      internal bool getIsTopOfTextBox(GuiMgControl control, int line)
      {
         _commandType = CommandType.GET_IS_TOP_OF_TEXTBOX;
         _obj2 = control;
         _line = line;

         Invoke();

         return _boolVal;
      }

      /// <summary> 
      /// check if the Carel is Positioned on the last line in TextBox.</summary>
      /// <param name="control"></param>
      /// <param name="line"></param>
      internal bool getIsEndOfTextBox(GuiMgControl control, int line)
      {
         _commandType = CommandType.GET_IS_END_OF_TEXTBOX;
         _obj2 = control;
         _line = line;

         Invoke();

         return _boolVal;
      }

      /// <summary> 
      /// get the selection on the given control</summary>
      /// <param name="control"></param>
      /// <param name="line"></param>
      /// <param name="point"></param>
      internal void selectionGet(GuiMgControl control, int line, MgPoint point)
      {
         _commandType = CommandType.GET_SELECTION;
         _obj2 = control;
         _line = line;

         Invoke();
         point.x = _x;
         point.y = _y;
      }

      /// <summary> 
      /// (Korean IME) Send IME Message to MgTextBox </summary>
      /// <param name="control"></param>
      /// <param name="line"></param>
      internal int sendImeMsg(GuiMgControl control, int ln, ImeParam im)
      {
         _commandType = CommandType.SEND_IME_MSG;
         _obj2 = control;
         _line = ln;
         _obj1 = (Object)im;

         Invoke();
         return _intVal1;
      }

#if PocketPC
      /// <summary> get the accumulated text from the control</summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal String accumulatedTextGet(GuiMgControl control)
      {
         _commandType = CommandType.GET_ACCUMULATED_TEXT;
         _obj2 = control;
         _mgValue = new MgValue();

         Invoke();
         return _mgValue.str;
      }
#endif

      /// <summary> 
      /// Write a string to the clipboard. The clip get get the data either from a control or from the passed string in _mgValue.
      /// </summary>
      /// <param name="_mgValue">has the string to set to the clipboard</param>
      internal void clipboardWrite(GuiMgControl control, int line, String clipData)
      {
         _commandType = CommandType.CLIPBOARD_SET_CONTENT;
         _str = clipData;
         _obj2 = control;
         _line = line;

         try
         {
            Invoke();
         }
         catch (System.Runtime.InteropServices.ExternalException e)
         {
            Events.WriteExceptionToLog("clipboardWrite: " + e.Message);
         }
      }

      /// <summary> 
      /// read from the clipboard to a string
      /// </summary>
      /// <returns> the string from the clipboard</returns>
      internal String clipboardRead()
      {
         _commandType = CommandType.CLIPBOARD_GET_CONTENT;
         _mgValue = new MgValue();
         try
         {
            Invoke();
         }
         catch (System.Runtime.InteropServices.ExternalException e)
         {
            Events.WriteExceptionToLog("clipboardRead: " + e.Message);
         }
         return _mgValue.str;
      }

      /// <summary>
      /// paste from clipboard to the control.
      /// </summary>
      /// <param name="control"></param>
      /// <param name="line"></param>
      internal void clipboardPaste(GuiMgControl control, int line)
      {
         _commandType = CommandType.CLIPBOARD_PASTE;
         _obj2 = control;
         _line = line;

         Invoke();
      }

      /// <summary> 
      /// Post a key event (emulate keys pressed by the user).
      /// </summary>
      /// <param name="control"></param>
      /// <param name="line"></param>
      /// <param name="keyCode"></param>
      /// <param name="stateMask"></param>
      internal void postKeyEvent(GuiMgControl control, int line, String keys, bool PostChar,
         bool forceLogicalControlTextUpdate)
      {
         _commandType = CommandType.POST_KEY_EVENT;
         _obj2 = control;
         _line = line;
         _mgValue = new MgValue();
         _mgValue.str = keys;
         _mgValue.bool1 = forceLogicalControlTextUpdate;
         _boolVal = PostChar;

         Invoke();
      }

      /// <summary> 
      /// Send WM_CHAR to the specified control via GUI thread.
      /// </summary>
      /// <param name="control"></param>
      /// <param name="line"></param>
      /// <param name="chr"></param>
      internal void postCharEvent(GuiMgControl control, int line, Char chr)
      {
         _commandType = CommandType.POST_CHAR_EVENT;
         _obj2 = control;
         _line = line;
         _char = chr;

         Invoke();
      }

      /// <summary> 
      /// check the auto wide
      /// </summary>
      /// <param name="object"></param>
      internal void checkAutoWide(GuiMgControl guiMgControl, int line, bool lenCheck)
      {
         _commandType = CommandType.CHECK_AUTO_WIDE;
         _obj2 = guiMgControl;
         _line = line;
         _boolVal = lenCheck;
         Invoke();
      }

      /// <summary> dispose all the shells. last dispose will close the display.</summary>
      internal void disposeAllForms()
      {
         _commandType = CommandType.DISPOSE_ALL_FORMS;
         Invoke();
      }

      /// <summary> return number of rows in the table</summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal int getRowsInPage(GuiMgControl control)
      {
         _commandType = CommandType.GET_ROWS_IN_PAGE;
         _obj2 = control;
         Invoke();
         return _intVal1;
      }

      /// <summary> return the number of hidden rows (partially or fully) in table</summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal int GetHiddenRowsCountInTable(GuiMgControl control)
      {
         _commandType = CommandType.GET_HIDDEN_ROWS_COUNT_IN_TABLE;
         _obj2 = control;
         Invoke();
         return _intVal1;
      }

      /// <summary>
      /// return the last window state
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      internal int getLastWindowState(GuiMgForm guiMgForm)
      {
         _commandType = CommandType.GET_LAST_WINDOW_STATE;
         _obj2 = guiMgForm;

         Invoke();
         return _intVal1;
      }

      /// <summary>
      /// gets height of all frames in frameset
      /// </summary>
      /// <param name="frameset"></param>
      /// <returns></returns>
      internal Object getFramesBounds(GuiMgControl frameset)
      {
         _commandType = CommandType.GET_FRAMES_BOUNDS;
         _obj2 = frameset;

         Invoke();
         return _obj1;
      }

      /// <summary>
      /// gets linked parent idx of frameset
      /// </summary>
      /// <param name="frameset"></param>
      /// <returns></returns>
      internal int getLinkedParentIdx(GuiMgControl frameset)
      {
         _commandType = CommandType.GET_LINKED_PARENT_IDX;
         _obj2 = frameset;

         Invoke();
         return _intVal1;
      }

      /// <summary>
      /// get form prop left
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      internal Rectangle getFormBounds(GuiMgForm guiMgForm)
      {
         _commandType = CommandType.GET_FORM_BOUNDS;
         _obj2 = guiMgForm;

         Invoke();
         return _rect;
      }

      /// <summary> 
      /// get the columns state --- layer, width and widthForFillTablePlacement
      /// </summary>
      /// <param name="index"></param>
      /// <returns></returns>
      internal List<int[]> getColumnsState(GuiMgControl tableCtrl)
      {
         _commandType = CommandType.GET_COLUMNS_STATE;
         _obj2 = tableCtrl;

         _mgValue = new MgValue();
         Invoke();
         return _mgValue.listOfIntArr;
      }

      /// <summary>
      /// Update the Control.TagData.MapData with the newObjectToSet
      /// </summary>
      /// <param name="newObjectToSet"></param>
      internal void MapWidget(Object newObjectToSet)
      {
         _commandType = CommandType.MAP_WIDGET_TO_GUI_OBJECT;
         _obj1 = newObjectToSet;

         Invoke();
      }

      /// <summary>
      /// Returns count of currently opened MDIChild.
      /// </summary>
      /// <returns></returns>
      internal int GetMDIChildCount()
      {
         _commandType = CommandType.GET_MDI_CHILD_COUNT;
         _mgValue = new MgValue();

         Invoke();
         return _mgValue.number;
      }

      /// <summary>
      /// Activate the form.
      /// </summary>
      /// <param name="guiMgForm"></param>
      internal void ActivateForm(GuiMgForm guiMgForm)
      {
         _commandType = CommandType.ACTIVATE_FORM;
         _obj1 = guiMgForm;
         Invoke();
      }

#if !PocketPC
      /// <summary>
      /// Activates a next or previous MDI child
      /// </summary>
      /// <param name="nextWindow">indicates whether to activate next window or not</param>
      internal void ActivateNextOrPreviousMDIChild(bool nextWindow)
      {
         _commandType = CommandType.ACTIVATE_NEXT_OR_PREVIOUS_MDI_CHILD;
         _boolVal = nextWindow;
         Invoke();
      }
#endif

      /// <summary>
      /// Enable/Disable MenuItem.
      /// </summary>
      /// <param name="mnuRef"></param>
      /// <param name="enable"></param>
      internal void EnableMenuEntry(MenuReference mnuRef, bool enable)
      {
         _commandType = CommandType.ENABLE_MENU_ENTRY;
         _obj1 = mnuRef;
         _boolVal = enable;

         Invoke();
      }

      /// <summary>
      /// ShowContextMenu.
      /// </summary>
      /// <param name="guiMgControl"></param>
      /// <param name="guiMgForm"></param>
      /// <param name="left"></param>
      /// <param name="top"></param>
      internal void onShowContextMenu(GuiMgControl guiMgControl, GuiMgForm guiMgForm, int left, int top, int line)
      {
         _commandType = CommandType.SHOW_CONTEXT_MENU;
         _obj1 = guiMgControl;
         _obj2 = guiMgForm;
         _line = line;
         _x = left;
         _y = top;

         Invoke();
      }

#if !PocketPC
      /// <summary>
      /// OPEN_FORM_DESIGNER.
      /// </summary>
      /// <param name="guiMgForm"></param>
      internal void OnOpenFormDesigner(GuiMgForm guiMgForm, Dictionary<object, ControlDesignerInfo> dict, bool adminMode, String controlsPersistencyPath)
      {
         _commandType = CommandType.OPEN_FORM_DESIGNER;
         _obj2 = guiMgForm;
         this._obj1 = dict;
         this._boolVal = adminMode;
         this._str = controlsPersistencyPath;
         Invoke();
      }

      /// <summary>
      /// returns if the passed form is active
      /// </summary>
      /// <param name="form"></param>
      /// <returns></returns>
      internal bool isFormActive(GuiMgForm guiMgForm)
      {
         _commandType = CommandType.IS_FORM_ACTIVE;
         _obj2 = guiMgForm;

         GUIMain.getInstance().invoke(new GuiInteractiveDelegate(Run));
         return _boolVal;
      }

      /// <summary>
      /// set the marked text on rich edit control
      /// </summary>
      /// <param name="control"></param>
      /// <param name="line"></param>
      /// <param name="text"></param>
      internal void setMarkedTextOnRichEdit(GuiMgControl control, int line, string text)
      {
         _commandType = CommandType.SET_MARKED_TEXT_ON_RICH_EDIT;
         _obj2 = control;
         _line = line;
         _str = text;

         Invoke();
      }

      /// <summary> Remove the ValueChangedHandler of a .Net control. </summary>
      /// <param name="guiMgControl"></param>
      internal void RemoveDNControlValueChangedHandler(GuiMgControl guiMgControl)
      {
         Debug.Assert(guiMgControl.IsDotNetControl());

         _commandType = CommandType.REMOVE_DN_CONTROL_VALUE_CHANGED_HANDLER;
         _obj2 = guiMgControl;

         Invoke();
      }

      /// <summary> Add the ValueChangedHandler of a .Net control. </summary>
      /// <param name="guiMgControl"></param>
      internal void AddDNControlValueChangedHandler(GuiMgControl guiMgControl)
      {
         Debug.Assert(guiMgControl.IsDotNetControl());

         _commandType = CommandType.ADD_DN_CONTROL_VALUE_CHANGED_HANDLER;
         _obj2 = guiMgControl;

         Invoke();
      }

      /// <summary> Gets the RTF value of the RTF edit control which was set before entering it. </summary>
      /// <param name="guiMgControl"></param>
      /// <param name="line"></param>
      /// <returns></returns>
      internal string GetRtfValueBeforeEnteringControl(GuiMgControl guiMgControl, int line)
      {
         _commandType = CommandType.GET_RTF_VALUE_BEFORE_ENTERING_CONTROL;
         _obj2 = guiMgControl;
         _line = line;

         _mgValue = new MgValue();

         Invoke();

         return _mgValue.str;
      }

      /// <summary> Get Position Isn of Row in DataTable attached to DV Control. </summary>
      /// <param name="dataTable"></param>
      /// <param name="line"></param>
      /// <returns></returns>
      internal int GetDVControlPositionIsn(Object dataTable, int line)
      {
         _commandType = CommandType.GET_DVCONTROL_POSITION_ISN;
         _obj1 = dataTable;
         _line = line;

         Invoke();
         return _intVal1;
      }

      /// <summary> Get Position Isn of Row in DataTable attached to DV Control. </summary>
      /// <param name="dataTable"></param>
      /// <param name="line"></param>
      /// <returns></returns>
      internal void ClearDatatable(GuiMgControl dvControl, Object dataTable)
      {
         _commandType = CommandType.CLEAR_DATA_TABLE;
         _obj1 = dvControl;
         _obj2 = dataTable;

         Invoke();
      }



      /// <summary> Set Datasource property of Dataview control. </summary>
      /// <param name="dvControl"></param>
      /// <param name="dataTable"></param>
      /// <param name="propertyName"></param>
      internal void SetDataSourceToDataViewControl(GuiMgControl dvControl, Object dataTable, string propertyName)
      {
         _commandType = CommandType.SET_DATA_SOURCE_TO_DVCONTROL;
         _obj1 = dvControl;
         _obj2 = dataTable;
         _str = propertyName;
         Invoke();
      }


      #region DRAG And DROP

      /// <summary> 
      /// Get the data for a specific format from dropped data.
      /// </summary>
      /// <param name="format"></param>
      /// <param name="userFormatStr">User defined format. It will be Null for internal formats.</param>
      /// <returns> string - Data for a specific format </returns>
      internal String GetDroppedData(ClipFormats format, String userFormatStr)
      {
         _commandType = CommandType.GET_DROPPED_DATA;
         _intVal1 = (int)format;
         _str = userFormatStr;

         _mgValue = new MgValue();
         Invoke();
         return _mgValue.str;
      }

      /// <summary> 
      /// get the dropped x & y from dropped data.
      /// </summary>
      /// <param name="point"> will be updated with the dropped position </param>
      /// <returns> void </returns>
      internal void GetDropPoint(MgPoint point)
      {
         _commandType = CommandType.GET_DROPPED_POINT;

         Invoke();
         point.x = _x;
         point.y = _y;
      }

      /// <summary> 
      /// get the SelectionStart and SelectionEnd from the dropped data.
      /// </summary>
      /// <returns> void </returns>
      internal void GetSelectionForDroppedControl(ref int selectionStart, ref int selectionLength)
      {
         _commandType = CommandType.GET_DROPPED_SELECTION;

         Invoke();
         selectionStart = _x;
         selectionLength = _y;
      }

      /// <summary> 
      /// Check whether the format is present in the dropped data or not.
      /// </summary>
      /// <param name="format"> format </param>
      /// <param name="userFormatStr">User defined format. It will be Null for internal formats.</param>
      /// <returns> bool - true, if format is present in dropped data. </returns>
      internal bool CheckDropFormatPresent(ClipFormats format, String userFormatStr)
      {
         _commandType = CommandType.DROP_FORMAT_SUPPORTED;
         _intVal1 = (int)format;
         _str = userFormatStr;

         _mgValue = new MgValue();
         Invoke();
         return _mgValue.boolVal;
      }

      /// <summary>
      /// Get the value of IsBeginDrag flag from DraggedData.
      /// </summary>
      /// <returns></returns>
      internal bool IsBeginDrag()
      {
         _commandType = CommandType.GET_IS_BEGIN_DRAG;

         _mgValue = new MgValue();
         Invoke();
         return _mgValue.boolVal;
      }
      #endregion
#endif

      ///<summary>
      ///  Check whether the combobox is in a DroppedDown state or not.
      ///</summary>
      ///<param name="comboBox">MgcomboBox control</param>
      ///<param name="line">!!.</param>
      ///<returns>bool</returns>
      internal bool IsComboBoxInDroppedDownState(GuiMgControl comboBox, int line)
      {
         _commandType = CommandType.IS_COMBO_DROPPED_DOWN;
         _obj1 = comboBox;
         _line = line;

         Invoke();
         return _boolVal;
      }

      /// <summary> Calls Run() synchronously to execute the command. 
      /// But before that, it executes the GuiCommandQueue.
      /// </summary>
      private void Invoke()
      {
         if (invokeRequired())
         {
            // Allow only one worker thread to enter GuiInteractive
            lock (lockObject)
            {
               if (!GuiCommandQueue.getInstance().GuiThreadIsAvailableToProcessCommands && GuiCommandQueue.getInstance().QueueSize > 0)
               {
                  try
                  {
                     // If Gui thread execution started here, we will have to recheck if Gui thread is processing commands after entering monitor.
                     // It may happen that the control will return back to worker thread only after gui thread has processed all the commands.
                     // In such situations, worker thread should not wait for command processing to start (it will wait forever since it will
                     // never be able to invoke gui thread again).

                     Monitor.Enter(GuiCommandQueue.GuiThreadCommandProcessingStart);

                     // Request gui thread to process existing commands in the queue
                     GuiCommandQueue.getInstance().beginInvoke();

                     // If gui thread execution starts after Monitor.Enter but before Monitor.Wait, following will be the flow:
                     // 1. gui thread will not enter the monitor while setting the value of GuiThreadIsAvailableToProcessCommands to true (because 
                     //    worker thread has already entered). 
                     // 2. Worker thread will eventually get back the control and it will call Monitor.Wait allowing Gui thread to resume. Gui thread 
                     //    will then pulse the monitor indicating that it has started commands execution.
                     // 3. Due to Pulse, worker thread will come out of Wait (for start).

                     // As mentioned in the comment above Monitor.Enter, re-check the condition 
                     if (!GuiCommandQueue.getInstance().GuiThreadIsAvailableToProcessCommands && GuiCommandQueue.getInstance().QueueSize > 0)
                        Monitor.Wait(GuiCommandQueue.GuiThreadCommandProcessingStart);
                  }
                  finally
                  {
                     Monitor.Exit(GuiCommandQueue.GuiThreadCommandProcessingStart);
                  }
               }

               // and then wait for the gui thread to finish processing the command
               try
               {
                  Monitor.Enter(GuiCommandQueue.GuiThreadCommandProcessingEnd);
                  if (GuiCommandQueue.getInstance().GuiThreadIsAvailableToProcessCommands)
                     Monitor.Wait(GuiCommandQueue.GuiThreadCommandProcessingEnd);
               }
               finally
               {
                  Monitor.Exit(GuiCommandQueue.GuiThreadCommandProcessingEnd);
               }

               // Finally, Gui thread is ready to handle GuiInteractive command
               GUIMain.getInstance().invoke(new GuiInteractiveDelegate(Run));
            }
         }
         else
            Run();
      }

      /// <summary> implements the Runnable run method for calling Display.syncExec()</summary>
      internal void Run()
      {
         // Sets the currentContextID
         var contextIDGuard = new Manager.ContextIDGuard(_contextID);
         try
         {
            switch (_commandType)
            {
               case CommandType.GET_FONT_METRICS:
                  onGetFontMetrics();
                  break;
               case CommandType.GET_RESOLUTION:
                  onGetResolution();
                  break;
               case CommandType.GET_BOUNDS:
                  onBounds();
                  break;
               case CommandType.GET_CLIENT_BOUNDS:
                  getClientBounds();
                  break;
               case CommandType.GET_BOUNDS_RELATIVE_TO:
                  onBoundsRelativeTo();
                  break;
               case CommandType.POINT_TO_CLIENT:
                  onPointToClient();
                  break;
               case CommandType.POINT_IN_MONITOR:
                  OnIsPointInMonitor();
                  break;
               case CommandType.GET_LEFT_TOP_FORM_MONITOR:
                  OnGetLeftTopOfFormMonitor();
                  break;
               case CommandType.GET_DESKTOP_BOUNDS:
                  onDesktopBounds();
                  break;
               case CommandType.GET_VALUE:
                  onValue();
                  break;
               case CommandType.SET_BROWSER_TEXT:
                  onSetBrowserText();
                  break;
               case CommandType.GET_BROWSER_TEXT:
                  onGetBrowserText();
                  break;
               case CommandType.BROWSER_EXECUTE:
                  onBrowserExecute();
                  break;
               case CommandType.GET_TOP_INDEX:
                  onGetTopIndex();
                  break;
               case CommandType.MESSAGE_BOX:
                  onMessageBox();
                  break;
               case CommandType.INVOKE_UDPCOMMAND:
                  onInvokeUDP();
                  break;
               case CommandType.DIRECTORY_DIALOG_BOX:
                  onDirectoryDialogBox();
                  break;
               case CommandType.SET_EDIT_TEXT:
                  onSetEditText();
                  break;
               case CommandType.INSERT_EDIT_TEXT:
                  onInsertEditText();
                  break;
               case CommandType.SET_SELECTION:
                  onSetSelection();
                  break;
               case CommandType.SET_SUGGESTED_VALUE:
                  onSetSuggestedValue();
                  break;

               case CommandType.GET_CARET_POS:
                  onCaretPosGet();
                  break;
               case CommandType.GET_IS_TOP_OF_TEXTBOX:
                  onGetIsTopOfTextBox();
                  break;
               case CommandType.GET_IS_END_OF_TEXTBOX:
                  onGetIsEndOfTextBox();
                  break;
               case CommandType.GET_SELECTION:
                  onSelectionGet();
                  break;
               case CommandType.CLIPBOARD_SET_CONTENT:
                  onClipboardWrite();
                  break;
               case CommandType.CLIPBOARD_GET_CONTENT:
                  onClipboardRead();
                  break;
               case CommandType.CLIPBOARD_PASTE:
                  onClipboardPaste();
                  break;
               case CommandType.POST_KEY_EVENT:
                  onPostKeyEvent();
                  break;
               case CommandType.POST_CHAR_EVENT:
                  onPostCharEvent();
                  break;
               case CommandType.SET_CURSOR:
                  onSetCursor();
                  break;
               case CommandType.CHECK_AUTO_WIDE:
                  onCheckAutoWide();
                  break;
               case CommandType.DISPOSE_ALL_FORMS:
                  onDisposeAllForms();
                  break;
               case CommandType.GET_ROWS_IN_PAGE:
                  onGetRowsInPage();
                  break;
               case CommandType.GET_HIDDEN_ROWS_COUNT_IN_TABLE:
                  onGetHiddenRowsCountInTable();
                  break;
               case CommandType.FILE_OPEN_DIALOG_BOX:
                  onFileOpenDialogBox();
                  break;
               case CommandType.FILE_SAVE_DIALOG_BOX:
                  onFileSaveDialogBox();
                  break;
               case CommandType.CREATE_DIALOG:
                  onCreateDialog();
                  break;
               case CommandType.OPEN_DIALOG:
                  onOpenDialog();
                  break;
               case CommandType.CLOSE_DIALOG:
                  onCloseDialog();
                  break;
               case CommandType.SET_GET_SUGGESTED_VALUE_FOR_CHOICE_CONTROL_ON_TAGDATA:
                  OnSetGetSuggestedValueOfChoiceControlOnTagData();
                  break;
               case CommandType.ATTACH_DNKEY_TO_OBJECT:
                  onAttachDnKeyToObject();
                  break;
               case CommandType.REFLECTION_INVOKE:
                  onReflectionInvoke();
                  break;
               case CommandType.REFLECTION_SET:
                  onReflectionSet();
                  break;
#if PocketPC
            case CommandType.GET_ACCUMULATED_TEXT:
               OnGetAccumulatedText();
               break;
#endif
               case CommandType.GET_LAST_WINDOW_STATE:
                  onGetLastWindowState();
                  break;

               case CommandType.GET_FRAMES_BOUNDS:
                  onGetFramesBounds();
                  break;

               case CommandType.GET_LINKED_PARENT_IDX:
                  onGetLinkedParentIdx();
                  break;

               case CommandType.GET_FORM_BOUNDS:
                  onGetFormBounds();
                  break;

               case CommandType.GET_COLUMNS_STATE:
                  onGetColumnsState();
                  break;

               case CommandType.GET_FORM_HANDLE:
                  onGetFormHandle();
                  break;

               case CommandType.GET_CTRL_HANDLE:
                  onGetCtrlHandle();
                  break;

               case CommandType.SEND_IME_MSG:
                  onSendImeMsg();
                  break;

               case CommandType.CAN_FOCUS:
                  onCanFocus();
                  break;
               case CommandType.GET_SELECTED_INDICE:
                  OnGetSelectedIndice();
                  break;

               case CommandType.GET_HASINDENT:
                  OnGetHasIndent();
                  break;

               case CommandType.MAP_WIDGET_TO_GUI_OBJECT:
                  MapWidget();
                  break;

#if !PocketPC
               case CommandType.IS_FORM_ACTIVE:
                  isFormActive();
                  break;

               case CommandType.GET_DROPPED_DATA:
                  onGetDroppedData();
                  break;

               case CommandType.GET_DROPPED_POINT:
                  onGetDroppedPoint();
                  break;

               case CommandType.DROP_FORMAT_SUPPORTED:
                  onCheckDropFormatPresent();
                  break;

               case CommandType.GET_DROPPED_SELECTION:
                  onGetDroppedSelection();
                  break;

               case CommandType.GET_IS_BEGIN_DRAG:
                  onGetIsBeginDrag();
                  break;

               case CommandType.SET_MARKED_TEXT_ON_RICH_EDIT:
                  onSetMarkedTextOnRichEdit();
                  break;

               case CommandType.REMOVE_DN_CONTROL_VALUE_CHANGED_HANDLER:
                  OnRemoveDNControlValueChangedHandler();
                  break;

               case CommandType.ADD_DN_CONTROL_VALUE_CHANGED_HANDLER:
                  OnAddDNControlValueChangedHandler();
                  break;

               case CommandType.GET_RTF_VALUE_BEFORE_ENTERING_CONTROL:
                  OnGetRtfValueBeforeEnteringControl();
                  break;
#endif

               case CommandType.POINT_TO_SCREEN:
                  OnPointToScreen();
                  break;

               case CommandType.GET_MDI_CLIENT_BOUNDS:
                  OnGetMdiClientBounds();
                  break;

               case CommandType.GET_MDI_CHILD_COUNT:
                  OnGetMDIChildCount();
                  break;

               case CommandType.GET_DVCONTROL_POSITION_ISN:
                  OnGetDVControlPositionIsn();
                  break;

               case CommandType.CLEAR_DATA_TABLE:
                  OnClearDVControlDataTable();
                  break;

               case CommandType.SET_DATA_SOURCE_TO_DVCONTROL:
                  OnSetDVControlDataSource();
                  break;

               case CommandType.ACTIVATE_FORM:
                  OnActivateForm();
                  break;

               case CommandType.ENABLE_MENU_ENTRY:
                  OnEnableMenuEntry();
                  break;

#if !PocketPC
               case CommandType.ACTIVATE_NEXT_OR_PREVIOUS_MDI_CHILD:
                  OnActivateNextOrPreviousMDIChild();
                  break;

               case CommandType.SHOW_CONTEXT_MENU:
                  OnShowContextMenu();
                  break;
               case CommandType.OPEN_FORM_DESIGNER:
                  OnOpenFormDesigner();
                  break;

#endif
               case CommandType.IS_COMBO_DROPPED_DOWN:
                  OnIsComboDroppedDowndState();
                  break;
            }
         }

         catch (Exception exception)
         {
                Events.WriteExceptionToLog(exception);
                throw exception;
         }

         finally
         {
            contextIDGuard.Dispose(); // Reset the current contextID.
         }
      }

      #region DifferentiationConfirmed

      /// <summary> Remove the ValueChangedHandler. </summary>
      protected virtual void OnRemoveDNControlValueChangedHandler()
      {
         Events.WriteExceptionToLog("RemoveDNControlValueChangedHandler - Not Implemented Yet");
      }

      /// <summary> Add the ValueChangedHandler. </summary>
      protected virtual void OnAddDNControlValueChangedHandler()
      {
         Events.WriteExceptionToLog("AddDNControlValueChangedHandler - Not Implemented Yet");
      }

      /// <summary> Gets the RTF value of the RTF edit control which was set before entering it. </summary>
      protected virtual void OnGetRtfValueBeforeEnteringControl()
      {
         Events.WriteExceptionToLog("OnGetOrgRtfValue - Not Implemented Yet");
      }

      #endregion

      /// methods suspected as different between the GuiInteractive classes of the standard/compact frameworks
      #region DifferentiationSuspected

      internal abstract void onDesktopBounds();

      internal abstract void onGetBrowserText();

      internal abstract void onBrowserExecute();

      internal abstract void onMessageBox();

      ///<summary>
      /// Handles Invoke UDP operation from GUI thread
      ///</summary>     
      internal void onInvokeUDP()
      {
         double contextId = (double)_mgValue.obj;

         _mgValue.number = Events.InvokeUDP(contextId);
      }

      internal abstract void onDirectoryDialogBox();

      /// <summary> Handle the file open dialog box
      /// 
      /// </summary>
      internal void onFileOpenDialogBox()
      {
         // Create the file dialog object with the given style
         OpenFileDialog fileDlg = new OpenFileDialog();

         fileDlg.InitialDirectory = _mgValue.path;//directory name is saved in _mgValue.path
         fileDlg.FileName = _mgValue.str;//filename is saved in _mgValue.str
         _mgValue.str = null;

         try
         {
            fileDlg.Filter = _mgValue.filter;
            if (String.IsNullOrEmpty(fileDlg.Filter))
               fileDlg.Filter = "All files (*.*)|*.*";
         }
         catch
         {
            Events.WriteErrorToLog("bad filter string: " + _mgValue.filter);
            fileDlg.Filter = "";
         }

#if !PocketPC
         // Following properties are not supported in Compact framework for OpenFileDialogbox
         fileDlg.Title = _mgValue.title;
         fileDlg.CheckFileExists = _mgValue.boolVal;
         fileDlg.Multiselect = _mgValue.bool1;

         DialogResult res = fileDlg.ShowDialog();

         //QCR # : 773796.
         //Fix:The processing on the files selected\open by the user must be done only after checking the button user has clicked.
         //If user has clicked OK then file names should be iterated else if user has clicked cancel then nothing should be done.
         if (res == DialogResult.OK)
         {
            StringBuilder allFiles = new StringBuilder();

            // Extract all files names into one string
            foreach (String s in fileDlg.FileNames)
               allFiles.Append(s + "|");
            // remove the last '|'
            allFiles.Remove(allFiles.Length - 1, 1);
            _mgValue.str = allFiles.ToString();
         }
#else
         DialogResult res = fileDlg.ShowDialog();
         if (res == DialogResult.OK)
            _mgValue.str = fileDlg.FileName;
#endif
      }

      /// <summary>
      /// invoke reflection command in GUI thread
      /// </summary>
      internal void onReflectionInvoke()
      {
         bool isValueTypeDefaultCtor = _boolVal;

         if (_memberInfo is FieldInfo)
            _mgValue.obj = ReflectionServices.GetFieldValue((FieldInfo)_memberInfo, _obj2);
         else if (_memberInfo is PropertyInfo)
            _mgValue.obj = ReflectionServices.GetPropertyValue((PropertyInfo)_memberInfo, _obj2, _parameters);
         else if (_memberInfo is MethodInfo)
            _mgValue.obj = ReflectionServices.InvokeMethod((MethodInfo)_memberInfo, _obj2, _parameters, true);
         else if (_memberInfo is ConstructorInfo || isValueTypeDefaultCtor)
            _mgValue.obj = ReflectionServices.CreateInstance((Type)_obj2, (ConstructorInfo)_memberInfo, _parameters);
      }

      /// <summary>
      /// Attach the dot net key with the given .net object associated with the mgControl.
      /// In the command we have the mgControl and the dot net key.
      /// The dot net key should be attached to the .net control (i.e. the object) that belongs to the mgControl.
      /// We use the gui thread for that because work thread cannot access ControlsMap.
      /// </summary>
      internal void onAttachDnKeyToObject()
      {
         // get the object from the ControlsMap
         Object obj = ControlsMap.getInstance().object2Widget(_obj1, 0);
         GuiUtils.AttachDNKeyToObject(_intVal1, obj);
      }

      /// <summary>
      /// invoke reflection command in GUI thread to set a value
      /// </summary>
      internal void onReflectionSet()
      {
         if (_memberInfo is FieldInfo)
            ReflectionServices.SetFieldValue((FieldInfo)_memberInfo, _obj2, _obj1);
         else if (_memberInfo is PropertyInfo)
         {
            if (_obj2 is GuiMgControl)
               _obj2 = ControlsMap.getInstance().object2Widget(_obj2);

            Debug.WriteLine("GuiInteractive.onReflectionSet(): " + ((PropertyInfo)_memberInfo).Name);
            ReflectionServices.SetPropertyValue((PropertyInfo)_memberInfo, _obj2, _parameters, _obj1);
         }
      }

      /// <summary> Handle the file save dialog box
      /// 
      /// </summary>
      internal void onFileSaveDialogBox()
      {
         // Create the file dialog object with the given style
         SaveFileDialog fileDlg = new SaveFileDialog();

#if !PocketPC
         fileDlg.InitialDirectory = _mgValue.path; //Directory name is stored in path
#endif
         fileDlg.FileName = _mgValue.str; //File name is stored in str

         try
         {
            fileDlg.Filter = _mgValue.filter;
         }
         catch
         {
            Events.WriteErrorToLog("bad filter string: " + _mgValue.filter);
            _mgValue.str = null;
            return;
         }
#if !PocketPC
         // Following properties are not supported in Compact framework for SaveDialogBox
         fileDlg.Title = _mgValue.title;
         fileDlg.DefaultExt = _mgValue.caption;
         fileDlg.OverwritePrompt = _mgValue.bool1;
#endif
         DialogResult res = fileDlg.ShowDialog();
         if (res != DialogResult.OK)
            _mgValue.str = String.Empty;
         else
            _mgValue.str = fileDlg.FileName;
      }

      /// <summary>Create dialog</summary>
      internal void onCreateDialog()
      {
         ((DialogHandler)_obj2).createDialog((Type)_obj1, _parameters);
      }

      /// <summary>Opens dialog</summary>
      internal void onOpenDialog()
      {
         _obj1 = ((DialogHandler)_obj2).openDialog();
      }

      /// <summary>Closes dialog</summary>
      internal void onCloseDialog()
      {
         ((DialogHandler)_obj2).closeDialog();
      }

#if !PocketPC

      /// <summary>This function returns if the form is active or not.
      /// </summary>
      private void isFormActive()
      {
         Object obj = ControlsMap.getInstance().object2Widget(_obj2);
         Form form = GuiUtils.getForm(obj);

         Form activeForm = GuiUtils.getActiveForm();
         _boolVal = (activeForm == null || form == GuiUtils.getActiveForm());
      }

      /// <summary>
      /// set the marked text on Rich Edit control
      /// </summary>
      internal void onSetMarkedTextOnRichEdit()
      {
         GuiMgControl guiMgCtrl = (GuiMgControl)_obj2;
         TextBoxBase textBox = null;

         Object obj = ControlsMap.getInstance().object2Widget(guiMgCtrl, _line);
         textBox = GuiUtils.getTextCtrl(obj);

         if (textBox != null)
         {
            Debug.Assert(textBox is RichTextBox);
            if (textBox is RichTextBox)
               textBox.SelectedText = _str;
         }
      }

      #region DRAG And DROP
      /// <summary>Get the string from DroppedData for a specific format</summary>
      private void onGetDroppedData()
      {
         ClipFormats format = (ClipFormats)_intVal1;

         _mgValue.str = GuiUtils.DroppedData.GetData(format, _str);
      }

      /// <summary>Get the point/location of the drop</summary>
      private void onGetDroppedPoint()
      {
         _x = GuiUtils.DroppedData.X;
         _y = GuiUtils.DroppedData.Y;
      }

      /// <summary>Check whether the format is present in DroppedData or not.</summary>
      private void onCheckDropFormatPresent()
      {
         ClipFormats format = (ClipFormats)_intVal1;
         _mgValue.boolVal = GuiUtils.DroppedData.CheckDropFormatPresent(format, _str);
      }

      /// <summary>Get the selection of a control for Drop</summary>
      private void onGetDroppedSelection()
      {
         _x = GuiUtils.DroppedData.SelectionStart;
         _y = GuiUtils.DroppedData.SelectionEnd;
      }

      /// <summary>Get the IsBeginDrag from DraggedData </summary>
      private void onGetIsBeginDrag()
      {
         _mgValue.boolVal = GuiUtils.DraggedData.IsBeginDrag;
      }
      #endregion // DRAG and DROP
#endif

      #endregion //DifferentiationSuspected

      /// methods common to the GuiInteractive classes of the standard/compact frameworks
      #region Common

      internal MessageBoxButtons Mode2MsgBoxButtons(int mode)
      {
         MessageBoxButtons Style = MessageBoxButtons.OK;

         // button format mask is in first nibble from right
         int buttonMode = mode & 0x0F;

         if (buttonMode == Styles.MSGBOX_BUTTON_ABORT_RETRY_IGNORE)
            Style = MessageBoxButtons.AbortRetryIgnore;
         else if (buttonMode == Styles.MSGBOX_BUTTON_OK_CANCEL)
            Style = MessageBoxButtons.OKCancel;
         else if (buttonMode == Styles.MSGBOX_BUTTON_RETRY_CANCEL)
            Style = MessageBoxButtons.RetryCancel;
         else if (buttonMode == Styles.MSGBOX_BUTTON_YES_NO)
            Style = MessageBoxButtons.YesNo;
         else if (buttonMode == Styles.MSGBOX_BUTTON_YES_NO_CANCEL)
            Style = MessageBoxButtons.YesNoCancel;

         return Style;
      }

      internal MessageBoxIcon Mode2MsgBoxIcon(int mode)
      {
         MessageBoxIcon Icon = MessageBoxIcon.None;

         // icon mask is in second nibble from right
         int iconMode = mode & 0xF0;

         if (iconMode == Styles.MSGBOX_ICON_ERROR)
            Icon = MessageBoxIcon.Hand;
         else if (iconMode == Styles.MSGBOX_ICON_QUESTION)
            Icon = MessageBoxIcon.Question;
         else if (iconMode == Styles.MSGBOX_ICON_WARNING)
            Icon = MessageBoxIcon.Exclamation;
         else if (iconMode == Styles.MSGBOX_ICON_INFORMATION)
            Icon = MessageBoxIcon.Asterisk;

         return Icon;
      }

      internal MessageBoxDefaultButton Mode2MsgBoxDefaultBtn(int mode)
      {
         MessageBoxDefaultButton Button = MessageBoxDefaultButton.Button1;

         // default button mask is in third nibble from right
         int defaultButtonMode = mode & 0xF00;

         if (defaultButtonMode == Styles.MSGBOX_DEFAULT_BUTTON_2)
            Button = MessageBoxDefaultButton.Button2;
         else if (defaultButtonMode == Styles.MSGBOX_DEFAULT_BUTTON_3)
            Button = MessageBoxDefaultButton.Button3;

         return Button;
      }

      internal int DialogResult2MsgBoxResult(DialogResult result)
      {
         int mode = 0;

         switch (result)
         {
            case DialogResult.OK:
               mode = Styles.MSGBOX_RESULT_OK;
               break;
            case DialogResult.Yes:
               mode = Styles.MSGBOX_RESULT_YES;
               break;
            case DialogResult.No:
               mode = Styles.MSGBOX_RESULT_NO;
               break;
            case DialogResult.Cancel:
               mode = Styles.MSGBOX_RESULT_CANCEL;
               break;
            case DialogResult.Abort:
               mode = Styles.MSGBOX_RESULT_ABORT;
               break;
            case DialogResult.Retry:
               mode = Styles.MSGBOX_RESULT_RETRY;
               break;
            case DialogResult.Ignore:
               mode = Styles.MSGBOX_RESULT_IGNORE;
               break;
         }

         return mode;
      }

      /// <summary> dispose all shells.</summary>
      private void onDisposeAllForms()
      {
         GUIMain.getInstance().disposeAllForms();
      }

      /// <summary> calculating the font metrics</summary>
      private void onGetFontMetrics()
      {
         Control control = (Control)ControlsMap.getInstance().object2Widget(_obj2);

         if (control != null)
         {
            _pointF = GuiUtils.GetFontMetrics(control, (MgFont)_obj1);
         }
      }

      /// <summary> Gets the resolution of the control. </summary>
      private void onGetResolution()
      {
         Control control = (Control)ControlsMap.getInstance().object2Widget(_obj1);

         if (control != null)
         {
            Point pt = GuiUtils.GetResolution(control);
            _x = pt.X;
            _y = pt.Y;
         }
      }

      /// <summary> return the caret position on the given control</summary>
      private void onCaretPosGet()
      {
         GuiMgControl guiMgCtrl = (GuiMgControl)_obj2;
         TextBoxBase textCtrl = null;

         Object obj = ControlsMap.getInstance().object2Widget(guiMgCtrl, _line);
         textCtrl = GuiUtils.getTextCtrl(obj);

         Point point;
         NativeWindowCommon.GetCaretPos(out point);
#if !PocketPC
         if (textCtrl is RichTextBox)
         {
            IntPtr pt = Marshal.AllocHGlobal(Marshal.SizeOf(point));
            try
            {
               Marshal.StructureToPtr(point, pt, true);
               IntPtr intptr = NativeHeader.SendMessage(textCtrl.Handle, NativeWindowCommon.EM_CHARFROMPOS, 0, pt);
               _mgValue.number = intptr.ToInt32();
            }
            finally
            {
               Marshal.FreeHGlobal(pt);
            }
         }
         else
#endif
            if (textCtrl is TextBox)
            {
               int lParam = NativeWindowCommon.MakeLong(point.X, point.Y);
               int pos = NativeWindowCommon.SendMessage(textCtrl.Handle, NativeWindowCommon.EM_CHARFROMPOS, 0, lParam);
               _mgValue.number = NativeWindowCommon.LoWord(pos);
            }
      }

      /// <summary> check if the caret is positioned on the first line of TextBox</summary>
      private bool onGetIsTopOfTextBox()
      {
         GuiMgControl guiMgCtrl = (GuiMgControl)_obj2;
         TextBoxBase textCtrl = null;
         _boolVal = false;

         Object obj = ControlsMap.getInstance().object2Widget(guiMgCtrl, _line);
         textCtrl = GuiUtils.getTextCtrl(obj);

         if (textCtrl is TextBoxBase)
         {
            if ((NativeHeader.SendMessage(textCtrl.Handle, NativeWindowCommon.EM_LINEINDEX, -1, 0) == 0))
               _boolVal = true;
            else
               _boolVal = false;
         }
         return _boolVal;
      }

      /// <summary> check if the caret is positioned on the last line of TextBox</summary>
      private bool onGetIsEndOfTextBox()
      {
         GuiMgControl guiMgCtrl = (GuiMgControl)_obj2;
         TextBoxBase textCtrl = null;
         _boolVal = false;

         Object obj = ControlsMap.getInstance().object2Widget(guiMgCtrl, _line);
         textCtrl = GuiUtils.getTextCtrl(obj);

         int noOfLines = NativeHeader.SendMessage(textCtrl.Handle, NativeWindowCommon.EM_GETLINECOUNT, 0, 0);

         if (NativeHeader.SendMessage(textCtrl.Handle, NativeWindowCommon.EM_LINEINDEX, noOfLines - 1, 0) ==
             NativeHeader.SendMessage(textCtrl.Handle, NativeWindowCommon.EM_LINEINDEX, -1, 0))
            _boolVal = true;
         else
            _boolVal = false;

         return _boolVal;
      }

      /// <summary> return the caret position on the given control</summary>
      private void onSelectionGet()
      {
         GuiMgControl guiMgCtrl = (GuiMgControl)_obj2;
         TextBoxBase textCtrl = null;

         Object obj = ControlsMap.getInstance().object2Widget(guiMgCtrl, _line);
         textCtrl = GuiUtils.getTextCtrl(obj);

         if (textCtrl != null)
         {
            _x = textCtrl.SelectionStart;
            _y = textCtrl.SelectionStart + textCtrl.SelectionLength;
         }
      }

#if PocketPC
      /// <summary> Get the accumulated text from the MgTextBox control
      /// </summary>
      private void OnGetAccumulatedText()
      {
         GuiMgControl guiMgCtrl = (GuiMgControl)_obj2;
         MgTextBox textCtrl = null;

         Object obj = ControlsMap.getInstance().object2Widget(guiMgCtrl, _line);
         textCtrl = (MgTextBox)GuiUtils.getTextCtrl(obj);

         if (textCtrl != null)
            _mgValue.str = textCtrl.getAccumulatedBuffer();
      }
#endif

      /// <summary> return the value of the control</summary>
      private void onValue()
      {
         Object control = ControlsMap.getInstance().object2Widget(_obj2, _line);
         _mgValue.str = GuiUtils.getValue(control);
      }

      /// <summary> set the text in the text control.</summary>
      private void onSetEditText()
      {
          GuiMgControl guiMgCtrl = (GuiMgControl)_obj2;
          TextBoxBase textCtrl = null;
          Object obj = ControlsMap.getInstance().object2Widget(guiMgCtrl, _line);
          LogicalControl logicalControl = null;

          if (obj is TextBoxBase)
              textCtrl = (TextBoxBase)obj;
          else if (obj is LogicalControl)
          {
              logicalControl = (LogicalControl)obj;
              textCtrl = (TextBoxBase)logicalControl.getEditorControl();
          }

          if (textCtrl != null)
          {
#if !PocketPC //temp - multiline
              int MleLastLineInView = 0;
              // In multiline we have a problem. When text is set to the control, the multiline scrolls to the top of the text.
              // After set text, we set selection (onSetSelection) and we scroll to caret. but it doesn't look good.
              // scrolling to caret on the position we need to be, will put the line with our selection on the bottom of the view.
              // For example : We page down on the multi (otherwise we r still on top) and type a char. suddenly the line we were typing is
              // at the end of the view.
              // The solution : Before setting new text, find the last line in the existing view.
              // In order for the lines not to move, this last line will be our focusing object after the text is changed.
              if (textCtrl.Multiline)
              {
                  // find the character on the bottom left of the multi.
                  int charIdx = textCtrl.GetCharIndexFromPosition(new Point(textCtrl.ClientRectangle.Left, (textCtrl.ClientRectangle.Bottom - (int)textCtrl.Font.GetHeight())));

                  // find the line of that char.
                  MleLastLineInView = textCtrl.GetLineFromCharIndex(charIdx);
              }
#endif

              // set the new text to the control.
              if (logicalControl != null)
                  logicalControl.Text = _mgValue.str;
              else
                  textCtrl.Text = _mgValue.str;

#if !PocketPC //temp - multiline
              if (textCtrl.Multiline)
              {
                  // after text is set we want to scroll to the last line we save before. That way, in onSetSelection, our pos 
                  // will already be in sight and now more movement will be needed.
                  // So, if the line index we saved still exists, focus on it. (if the text was shorten it might not be there).
                  // If there is no such line, our focus object will be the current last line of the multi control.
                  if (textCtrl.Lines.Length == 0)
                      textCtrl.SelectionStart = 0;
                  else
                  {
                      // when deleting, the line we had might no longer exist. check with the last physical line.
                      // we do not check against textCtrl.Lines.Length coz 1 logical line can hold 1, 2 or more physical lines.
                      // Find out the last physical line by finding the line of the last char in the text.
                      int LastPhysicalLineIdx = textCtrl.GetLineFromCharIndex(textCtrl.Text.Length - 1);

                      if (MleLastLineInView <= LastPhysicalLineIdx)
                          textCtrl.SelectionStart = textCtrl.GetFirstCharIndexFromLine(MleLastLineInView);
                      else
                          textCtrl.SelectionStart = textCtrl.GetFirstCharIndexFromLine(LastPhysicalLineIdx);
                  }
                  textCtrl.ScrollToCaret();
              }
              // set caret to end of text, otherwise it is set to 0 and very fast writing
              // can make problems.
              if (!UtilStrByteMode.isLocaleDefLangJPN())
                  textCtrl.Select(_mgValue.str.Length, 0);
#endif
          }
      }

      /// <summary> insert the text to the text control at a given position.</summary>
      private void onInsertEditText()
      {
         GuiMgControl guiMgCtrl = (GuiMgControl)_obj2;
         TextBoxBase textCtrl = null;
         Object obj = ControlsMap.getInstance().object2Widget(guiMgCtrl, _line);
         LogicalControl logicalControl = null;

         if (obj is TextBoxBase)
            textCtrl = (TextBoxBase)obj;
         else if (obj is LogicalControl)
         {
            logicalControl = (LogicalControl)obj;
            textCtrl = (TextBoxBase)logicalControl.getEditorControl();
         }

         // insert the sent text at the given position 
         if (textCtrl != null)
         {
            textCtrl.Select(_intVal1, 0);
            textCtrl.SelectedText = _mgValue.str;

            // set the new text to the logical control. 
            // we do it only after updating the text control to avoid unnecessary refreshes (that may bounce the caret at a multiline control)
            if (logicalControl != null)
               logicalControl.Text = textCtrl.Text;
         }

      }

      /// <summary> set the text(html) on the browser control</summary>
      internal void onSetBrowserText()
      {
         Object obj = ControlsMap.getInstance().object2Widget(_obj2);
         if (obj is MgWebBrowser)
         {
            ((MgWebBrowser)obj).DocumentText = _mgValue.str;
            _mgValue.boolVal = true;
         }
      }

      /// <summary> set the selection in the text control.</summary>
      internal void onSetSelection()
      {
         GuiMgControl mgCtrl = (GuiMgControl)_obj2;
         TextBoxBase textCtrl = null;

         Object obj = ControlsMap.getInstance().object2Widget(mgCtrl, _line);

         if (obj is TextBoxBase)
            textCtrl = (TextBoxBase)obj;
         else if (obj is LogicalControl)
         {
            LogicalControl logicalControl = (LogicalControl)obj;
            textCtrl = (TextBoxBase)logicalControl.getEditorControl();
         }

         if (textCtrl != null)
         {
            // Defect 133967 - set selection text caused to strange problem in RIA with hint text. When Text is empty and there is hint, there is no need to do select anyway 
            // so it will not be done in this case.
            if (!GuiUtils.IsEmptyHintTextBox(textCtrl))
            {
               //TODO: Kaushal. All the callers of this function sends y == caretPos.             
               // So, we do not need to send caretPos at all.
               //int caretPos = _mgValue.number;
               //----------------------------------------------------------------------------------
               // Ori : Callers might change. SWT had a problem that in a selection, the carent pos could be only 
               // set at the end of selection. If .Net support positioning caret at the begining of selection , then this
               // code should stay.
               //-----------------------------------------------------------------------------------
               if (_y == -1)
                  _y = textCtrl.TextLength;

               // set the selection. The caret position should be the 2nd selection parameter
               // it is needed in cases such as marking next/prev char.
#if PocketPC
            // On mobile, negative selection causes an exception
            if (_y < _x)
            textCtrl.Select(_y, _x - _y);
            else
#endif
               textCtrl.Select(_x, _y - _x);

               // caret in multiline might be outside view.
               if (textCtrl.Multiline)
                  textCtrl.ScrollToCaret();
            }
            // check id any actions need to be enable disable after marking change. (same as in guiCommandQueue).
            GuiUtils.enableDisableEvents(textCtrl, mgCtrl);
         }
      }

      /// <summary> set the suggested value for choice control.</summary>
      internal void onSetSuggestedValue()
      {
         GuiMgControl guiMgCtrl = (GuiMgControl)_obj2;
         Object obj = ControlsMap.getInstance().object2Widget(guiMgCtrl, _line);

         GuiUtils.setSuggestedValueOfChoiceControlOnTagData((Control)obj, _mgValue.str);
      }

      /// <summary> get number of rows in the table
      /// </summary>
      internal void onGetRowsInPage()
      {
         Control control = (Control)ControlsMap.getInstance().object2Widget(_obj2);
         if (control != null)
         {
            ItemsManager itemsManager = GuiUtils.getItemsManager(control);
            _intVal1 = itemsManager.getRowsInPage();
         }
      }

      /// <summary> return the number of hidden rows (partially or fully) in table
      /// </summary>
      internal void onGetHiddenRowsCountInTable()
      {
         Control control = (Control)ControlsMap.getInstance().object2Widget(_obj2);
         if (control != null)
         {
            TableManagerLimitedItems tableManager = (TableManagerLimitedItems)GuiUtils.getTableManager((TableControl)control);
            _intVal1 = tableManager.GetHiddenRowsCountInTable();
         }
      }

      /// <summary> get table top index
      /// </summary>
      internal void onGetTopIndex()
      {
         Control control = (Control)ControlsMap.getInstance().object2Widget(_obj2);
         if (control != null)
         {
            ItemsManager itemsManager = GuiUtils.getItemsManager(control);
            _intVal1 = itemsManager.getTopIndex();
         }
      }

      /// <summary> get the bounds of the object reletive to giving control</summary>
      internal void onBoundsRelativeTo()
      {
         Control parentCtrl = null;
         Object ctrlObject = ControlsMap.getInstance().object2Widget(_obj2, _line);
         Object relativeCtrlObject = null;

         // if the object1 is null the relative will be to the desktop
         if (_obj1 != null)
            relativeCtrlObject = ControlsMap.getInstance().object2Widget(_obj1);

         if (ctrlObject != null)
         {
            if (ctrlObject is LogicalControl)
            {
               parentCtrl = ((LogicalControl)ctrlObject).ContainerManager.mainControl;
               _rect = ((LogicalControl)ctrlObject).getRectangle();
            }
            else
            {
               parentCtrl = ((Control)ctrlObject).Parent;
               _rect = GuiUtils.getBounds((Control)ctrlObject);
            }

            GuiUtils.getRectRelatedTo(ref _rect, parentCtrl, relativeCtrlObject);
         }
      }

      /// <summary> get the Client bounds of the object</summary>
      internal void getClientBounds()
      {
         Control control = (Control)ControlsMap.getInstance().object2Widget(_obj2);
         Form form = GuiUtils.getForm(control);

         if (_boolVal) //we only need size of client panel
         {
            if (control is Form && form.Tag is TagData && ((TagData)form.Tag).ClientPanel != null)
               control = ((TagData)form.Tag).ClientPanel;
         }
         else if (form != null)
            control = form;

         if (control != null)
         {
            Rectangle clientRect = new Rectangle();
            _rect = GuiUtils.getBounds(control);

            if (control is Form)
            {
               clientRect = control.ClientRectangle;
               _rect.Width = clientRect.Width;
               _rect.Height = clientRect.Height;
            }
         }
      }

      /// <summary> check the auto wide control
      /// 
      /// </summary>
      internal void onCheckAutoWide()
      {
         GuiMgControl guiMgCtrl = (GuiMgControl)_obj2;
         Object obj = ControlsMap.getInstance().object2Widget(guiMgCtrl, _line);

         TextBoxBase textCtrl = GuiUtils.getTextCtrl(obj);

         if (textCtrl != null)
            GuiUtils.checkAutoWide(guiMgCtrl, textCtrl, GuiUtils.getValue(obj));
      }

      /// <summary> write a string to the clipboard. i did not use textCtrl.copy since onClipboardWrite can be used not only
      /// from a widget but also from a function
      /// </summary>
      internal void onClipboardWrite()
      {
         TextBoxBase textCtrl = null;
         GuiMgControl guiMgCtrl = null;

         // if object was passed, use it.
         // this way we get the correct behaviour in cases like the text is a password and copy is not allowed.
         if (_obj2 != null)
         {
            guiMgCtrl = (GuiMgControl)_obj2;
            Object widget = ControlsMap.getInstance().object2Widget(guiMgCtrl, _line);
            textCtrl = GuiUtils.getTextCtrl(widget);

            if (textCtrl != null)
            {
#if PocketPC
               // CF bug - selection can be negative, causes an exception when trying to get
               // selected text
               if (textCtrl.SelectionLength < 0)
               {
               Clipboard.SetDataObject(textCtrl.Text.Substring(textCtrl.SelectionStart + textCtrl.SelectionLength,
               -textCtrl.SelectionLength));
               }
               else
               Clipboard.SetDataObject(textCtrl.SelectedText);
#else
               Clipboard.SetDataObject(textCtrl.SelectedText, true);
#endif
            }
         }
         // just the text to put on the clip was passed.
         else
         {
#if PocketPC
            Clipboard.SetDataObject(_str);
#else
            Clipboard.SetDataObject(_str, true);
#endif
         }

         // check the paste enable. do not check the clip content (since the call is from set clip content).
         GuiUtils.checkPasteEnable(guiMgCtrl, false);
      }

      /// <summary> read from clipboard to a string
      /// </summary>
      internal void onClipboardRead()
      {
         if (GuiUtils.ClipboardDataExists(NativeWindowCommon.CF_UNICODETEXT))
            _mgValue.str = GuiUtils.GetClipboardData(NativeWindowCommon.CF_UNICODETEXT);
      }

      /// <summary>Paste content of clipboard to the selection in the control. Will be used for richedit controls.
      /// 
      /// </summary>
      internal void onClipboardPaste()
      {
         TextBoxBase textBox = null;
         GuiMgControl guiMgCtrl = (GuiMgControl)_obj2;
         Object obj = ControlsMap.getInstance().object2Widget(guiMgCtrl, _line);

         // get the correct control
         textBox = GuiUtils.getTextCtrl(obj);

         if (textBox != null)
         {
             if (GuiUtils.ClipboardDataExists(NativeWindowCommon.CF_UNICODETEXT))
               textBox.Text = GuiUtils.GetClipboardData(NativeWindowCommon.CF_UNICODETEXT); 
         }
      }

      /// <summary> post a key combination. This emulates keys pressed by the user. Setting the data in the widget to ignore
      /// key down, will cause the OS to handle the event instead of us.
      /// </summary>
      internal void onPostKeyEvent()
      {
         GuiMgControl guiMgCtrl = (GuiMgControl)_obj2;

         Object obj = ControlsMap.getInstance().object2Widget(guiMgCtrl, _line);

         // get the correct control
         TextBoxBase textBox = GuiUtils.getTextCtrl(obj);

#if !PocketPC //TMP
         bool supressKeyPressForRichEdit = _boolVal && (textBox is RichTextBox);
#else
         bool supressKeyPressForRichEdit = false;
#endif
         // set the ignore flag on the control, so that our 'posted' events will not be handled by us.
         ((TagData)textBox.Tag).IgnoreKeyDown = true;

         // supress the keypress handler for the richedit before sending the char.
         if (supressKeyPressForRichEdit)
            ((TagData)textBox.Tag).IgnoreKeyPress = true;

#if PocketPC
         SendKeys.Send(_mgValue.str);
#else
         SendKeys.SendWait(_mgValue.str);

         // Defect 120505 - In case we have pressed delte/backspace, we want to force an update 
         // to the logical control Text property in order to avoid incorrect update as seen in this defect.
         bool isDeleteOrBackspaceKey = _mgValue.bool1;
         if (obj is LogicalControl && textBox != null && isDeleteOrBackspaceKey)
         {
            ((LogicalControl)obj).Text = GuiUtils.getValue(obj);
         }
#endif
      }

      /// <summary> Send WM_CHAR message to ComboBox and ListBox. This emulates keys pressed by the user.
      /// </summary>
      internal void onPostCharEvent()
      {
         GuiMgControl guiMgCtrl = (GuiMgControl)_obj2;

         Object obj = ControlsMap.getInstance().object2Widget(guiMgCtrl, _line);

         if (obj is LgCombo || obj is MgComboBox || obj is MgListBox)
         {
            if (obj is LgCombo)
               obj = ((LgCombo)obj).getEditorControl();

            Control control = (Control)obj;
            ((TagData)control.Tag).IgnoreKeyPress = true;

            NativeWindowCommon.SendMessage(control.Handle, NativeWindowCommon.WM_CHAR, _char, 0);
         }
      }

      /// <summary> get the bounds of the object</summary>
      internal void onBounds()
      {
         Object ctrlObject = ControlsMap.getInstance().object2Widget(_obj2);

         if (ctrlObject != null)
         {
            if (ctrlObject is Control)
            {
               Rectangle? rect1 = GuiUtils.getSavedBounds((Control)ctrlObject);
               Control control = (Control)(ctrlObject);
               TagData tag = (TagData)(control.Tag);
               if (tag.IsClientPanel)
                  ctrlObject = GuiUtils.FindForm(control);

               if (rect1 == null)
                  _rect = GuiUtils.getBounds((Control)ctrlObject);
               else
                  _rect = (Rectangle)rect1;
            }
            else if (ctrlObject is LogicalControl)
               _rect = (((LogicalControl)ctrlObject)).getRectangle();
         }
      }

      /// <summary> convert point to client </summary>
      internal void onPointToClient()
      {
         Control control = (Control)ControlsMap.getInstance().object2Widget(_obj2);
         Point point = new Point(_x, _y);
         point = control.PointToClient(point);
         _x = point.X;
         _y = point.Y;
      }

      ///  <summary> converts point relative point to screen point</summary>
      internal void OnPointToScreen()
      {
         Control control = (Control)ControlsMap.getInstance().object2Widget(_obj2);
         Point point = new Point(_x, _y);
         point = control.PointToScreen(point);
         _x = point.X;
         _y = point.Y;
      }

      /// <summary>
      /// checks if Point is contained in any of the monitors
      /// </summary>
      internal void OnIsPointInMonitor()
      {
         MgPoint leftTopLocation;
         _boolVal = IsPointInMonitor(new MgPoint(_x, _y), out leftTopLocation);
      }

      /// <summary>
      /// 
      /// </summary>
      internal void OnGetLeftTopOfFormMonitor()
      {
# if !PocketPC
         Control control = null;
         if (_obj1 != null)
         {
            control = (Control)ControlsMap.getInstance().object2Widget(_obj1);
         }

         Screen screen = control != null ? Screen.FromControl(control) : Screen.PrimaryScreen;
         _x = screen.Bounds.X;
         _y = screen.Bounds.Y;
#else
         _x = 0;
         _y = 0;
#endif
      }

      /// <summary>
      /// Returns of point is in one of the monitors. If true, then it returns that monitor's LetTop location
      /// </summary>
      /// <param name="point"></param>
      /// <param name="leftTopLocation"></param>
      /// <returns></returns>
      bool IsPointInMonitor(MgPoint point, out MgPoint leftTopLocation)
      {
         leftTopLocation = new MgPoint(0, 0);
#if PocketPC
         return true;
#else

         bool isPointContainedInMonitor = false;

         foreach (Screen screen in Screen.AllScreens)
         {
            isPointContainedInMonitor = screen.Bounds.Contains(point.x, point.y);
            if (isPointContainedInMonitor)
            {
               leftTopLocation = new MgPoint(screen.Bounds.X, screen.Bounds.Y);
               break;
            }
         }

         return isPointContainedInMonitor;
#endif
      }

      /// <summary>
      /// Returns the bounds of MdiClient
      /// </summary>
      /// <returns>ClientRectangle of MdiClient</returns>
      internal void OnGetMdiClientBounds()
      {
         Object mdiFrame = Manager.GetCurrentRuntimeContext().FrameForm;
         Debug.Assert(mdiFrame != null);

         Control control = (Control)ControlsMap.getInstance().object2Widget(mdiFrame);
         _rect = control.ClientRectangle;
      }

      /// <summary>
      /// Returns count of currently opened MDIChild.
      /// </summary>
      internal void OnGetMDIChildCount()
      {
#if !PocketPC
         Object mdiFrame = Manager.GetCurrentRuntimeContext().FrameForm;
         MdiClient control = (MdiClient)ControlsMap.getInstance().object2Widget(mdiFrame);
         _mgValue.number = control.MdiChildren.GetLength(0);
#endif
      }

      /// <summary>
      /// 
      /// </summary>
      internal void OnSetGetSuggestedValueOfChoiceControlOnTagData()
      {
         GuiMgControl guiMgCtrl = (GuiMgControl)_obj2;
         Object obj = ControlsMap.getInstance().object2Widget(guiMgCtrl, _line);
         /* Combo can be placed on a table control as well and in this case, obj would be 
         * LgCombo. So, get the actual control before proceeding further.
         */
         if (obj is LgCombo)
            obj = ((LgCombo)obj).getEditorControl();
         if (obj is LgRadioContainer)
            obj = ((LgRadioContainer)obj).getEditorControl();

         if (obj != null)
         {
            Debug.Assert(obj is MgRadioPanel || obj is ComboBox || obj is TabControl || obj is ListBox);
            GuiUtils.setGetSuggestedValueOfChoiceControlOnTagData((Control)obj, _boolVal);
         }
      }

      /// <summary> (Korean IME) send MG_IME_XXX message to MgTextBox </summary>
      private void onSendImeMsg()
      {
         ImeParam im = (ImeParam)_obj1;
         _intVal1 = NativeWindowCommon.SendMessage(im.HWnd, im.Msg, (int)im.WParam, (int)im.LParam);
      }

      /// <summary> Convert from MgCursors (our internal enum) to Cursor
      /// 
      /// </summary>
      /// <param name="crsr"></param>
      /// <returns></returns>
      internal static Cursor MgCursorsToCursor(MgCursors crsr)
      {
         Cursor cursor = null;
         switch (crsr)
         {
            /*
            * 1 Standard arrow 2 Hourglass 3 Hand 4 Standard arrow and small hourglass 5 Crosshair 6 Arrow and
            * question mark 7 I-beam 8 Slashed circle 9 four-pointer arrow pointing north, south, east, and west.
            * 10 Double-pointed arrow pointing northeast and southwest 11 Double-pointed arrow pointing north and
            * south 12 Double-pointed arrow pointing northwest and southeast 13 Double-pointed arrow pointing west
            * and east 14 Vertical Arrow }
            */
            case MgCursors.ARROW:
#if !PocketPC
               cursor = Cursors.Arrow;
#else
               cursor = Cursors.Default;
#endif
               break;
            case MgCursors.WAITCURSOR:
               cursor = Cursors.WaitCursor;
               break;
#if !PocketPC
            case MgCursors.HAND:
               cursor = Cursors.Hand;
               break;
            case MgCursors.APPSTARTING:
               cursor = Cursors.AppStarting;
               break;
            case MgCursors.CROSS:
               cursor = Cursors.Cross;
               break;
            case MgCursors.HELP:
               cursor = Cursors.Help;
               break;
            case MgCursors.IBEAM:
               cursor = Cursors.IBeam;
               break;
            case MgCursors.NO:
               cursor = Cursors.No;
               break;
            case MgCursors.SIZEALL:
               cursor = Cursors.SizeAll;
               break;
            case MgCursors.SIZENESW:
               cursor = Cursors.SizeNESW;
               break;
            case MgCursors.SIZENS:
               cursor = Cursors.SizeNS;
               break;
            case MgCursors.SIZENWSE:
               cursor = Cursors.SizeNWSE;
               break;
            case MgCursors.SIZEWE:
               cursor = Cursors.SizeWE;
               break;
            case MgCursors.UPARROW:
               cursor = Cursors.UpArrow;
               break;
#endif
            default:
               Debug.Assert(false, "Not supported");
               break;
         }
         return cursor;
      }

      /// <summary>set cursor on forms</summary>
      internal void onSetCursor()
      {
         _mgValue.boolVal = false;
         Cursor cursor = MgCursorsToCursor((MgCursors)_intVal1);

         if (cursor != null)
         {
            _mgValue.boolVal = true;

#if !PocketPC
            FormCollection OpenForms = Application.OpenForms;
            for (int i = 0; i < OpenForms.Count; i++)
               GuiUtils.SetCursor(OpenForms[i], cursor);
#else
            // On Mobile we don't have the OpenForms - just set the current cursor
            Cursor.Current = cursor;
#endif
         }
      }

      /// <summary>true if the operation should be delegated to the GUI thread</summary>
      /// <returns></returns>
      private bool invokeRequired()
      {
         return (!Misc.IsGuiThread());
      }

      /// <summary>
      /// return windowState
      /// </summary>
      private void onGetLastWindowState()
      {
         Object obj = ControlsMap.getInstance().object2Widget(_obj2);
         Form form = GuiUtils.getForm(obj);
         TagData td = (TagData)form.Tag;

         switch (td.LastWindowState)
         {
            case FormWindowState.Maximized:
               _intVal1 = Styles.WINDOW_STATE_MAXIMIZE;
               break;

            default:
               _intVal1 = Styles.WINDOW_STATE_RESTORE;
               break;
         }
      }

      /// <summary>
      /// gets height of all childs in the frameset
      /// </summary>
      private void onGetFramesBounds()
      {
         Control control = (Control)ControlsMap.getInstance().object2Widget(_obj2);
         if (control != null && control is MgSplitContainer)
         {
            MgSplitContainer mgSplitContainer = (MgSplitContainer)control;
            Control[] ctrls = mgSplitContainer.getControls(false);

            List<Rectangle> arrayList = new List<Rectangle>();

            for (int index = 0; index < ctrls.Length; index++)
               arrayList.Add(new Rectangle(0, 0, ctrls[index].Width, ctrls[index].Height));

            _obj1 = arrayList;
         }
      }

      /// <summary>
      /// get parent link Idx of this frameset
      /// </summary>
      private void onGetLinkedParentIdx()
      {
         Control control = (Control)ControlsMap.getInstance().object2Widget(_obj2);

         _intVal1 = -1;

         if (control != null && control is MgSplitContainer)
         {
            MgSplitContainer mgSplitContainer = (MgSplitContainer)control;

            if (mgSplitContainer.Parent is MgSplitContainer)
            {
               MgSplitContainer parent = (MgSplitContainer)mgSplitContainer.Parent;
               Control[] ctrls = parent.getControls(false);

               _intVal1 = Array.IndexOf(ctrls, mgSplitContainer);
            }
         }
      }

      /// <summary>
      /// get form's Bounds
      /// </summary>
      private void onGetFormBounds()
      {
         Object obj = ControlsMap.getInstance().object2Widget(_obj2);
         Form form = GuiUtils.getForm(obj);

         if (form != null)
            _rect = ((TagData)form.Tag).DeskTopBounds;
      }

      /// <summary>get layer of the column
      /// </summary>
      private void onGetColumnsState()
      {
         GuiMgControl mgTableCtrl = (GuiMgControl)_obj2;
         TableControl tableControl = (TableControl)ControlsMap.getInstance().object2Widget(mgTableCtrl);
         TableManager tableManager = GuiUtils.getTableManager(tableControl);

         _mgValue.listOfIntArr = new List<int[]>();

         for (int idx = 0; idx < tableManager.ColumnsManager.ColumnsCount; idx++)
         {
            LgColumn lgColumn = tableManager.getColumn(idx);
            _mgValue.listOfIntArr.Add(new int[] { lgColumn.MgColumnIdx, lgColumn.getWidth(), lgColumn.WidthForFillTablePlacement });
         }
      }

      /// <summary>This function returns the handle of the window form depending on the MgForm.
      /// </summary>
      private void onGetFormHandle()
      {
         //Getting the form object.
         Form form = GuiUtils.getForm(ControlsMap.getInstance().object2Widget(_obj2));

         if (form != null)
            _intVal1 = (int)form.Handle;
      }

      /// <summary> Function finds the handle of the window control associated with the magic control.
      /// </summary>
      private void onGetCtrlHandle()
      {
         //Getting the object from controls map.
         Object objCtrl = ControlsMap.getInstance().object2Widget(_obj2, _line);
         Control ctrl = null;

         //Checking type as control may be actual control or logical control depending on the value of 'Allow testing Env'.
         if (objCtrl is Control)
            ctrl = (Control)objCtrl;
         else if (objCtrl is LogicalControl)
            ctrl = ((LogicalControl)objCtrl).getEditorControl();

         if (ctrl != null)
            _intVal1 = (int)(ctrl.Handle);
      }

      /// <summary> Indicates if a control can be selected. </summary>
      private void onCanFocus()
      {
         _mgValue.boolVal = GuiUtils.canFocus((GuiMgControl)_obj1);
      }

      /// <summary>
      /// Get the selected indice of the listbox control.
      /// </summary>
      private void OnGetSelectedIndice()
      {
         string selectedIndice = string.Empty;
         Object objCtrl = ControlsMap.getInstance().object2Widget(_obj1);

         if (objCtrl is ListBox)
            selectedIndice = GuiUtils.GetListBoxSelectedIndice((ListBox)objCtrl);

         _mgValue.str = selectedIndice;
      }

      /// <summary>
      /// Returns true if control has indent applied
      /// </summary>
      /// <returns></returns>
      private void OnGetHasIndent()
      {
         Object control = ControlsMap.getInstance().object2Widget(_obj1);

         if (control is RichTextBox)
            _boolVal = ((RichTextBox)control).SelectionIndent > 0;
      }

      /// <summary>
      /// Map Widget to Gui object (MgForm/MgStatusBar/MgStatusPane) for MDI frame for a parallel context
      /// </summary>
      private void MapWidget()
      {
         Object obj = ControlsMap.getInstance().object2Widget(_obj1);
         ControlsMap.getInstance().setMapData(_obj1, 0, obj);
      }

      /// <summary>
      ///   Get PositionIsn of DataRow From DataTable attached to DVControl.
      /// </summary>
      internal void OnGetDVControlPositionIsn()
      {
         DataTable dataTable = (DataTable)_obj1;
         int rowIdx = _line;

         _intVal1 = (int)dataTable.Rows[rowIdx][0];
      }

      /// <summary>
      ///   Clear DataTable attached to DataView control.
      /// </summary>
      /// <param name = "guiCommand"></param>
      internal void OnClearDVControlDataTable()
      {
         GuiMgControl DVControl = (GuiMgControl)_obj1;
         DataTable dataTable = (DataTable)_obj2;
         DataViewControlHandler.getInstance().RemoveHandler(DVControl, dataTable);

         dataTable.Clear();
         dataTable.AcceptChanges();
      }

      /// <summary>
      ///   Assign DataTable to DataSource property of DataView control.
      /// </summary>
      internal void OnSetDVControlDataSource()
      {
         DataTable dataTable = (DataTable)_obj2;

         Control ctrl = (Control)ControlsMap.getInstance().object2Widget(_obj1);
         String dataSourcePropertyName = _str;
         //Attach dataTable to DataSource property if DataView Control.
         PropertyInfo dataSourceProp = ReflectionServices.GetMemeberInfo(ctrl.GetType(), dataSourcePropertyName, false, null) as PropertyInfo;
         ReflectionServices.SetPropertyValue(dataSourceProp, ctrl, new object[] { }, dataTable);
      }

      /// <summary>
      /// Activate the form.
      /// </summary>
      internal void OnActivateForm()
      {
#if !PocketPC
         Object obj = ControlsMap.getInstance().object2Widget(_obj1);
         Form form = GuiUtils.getForm(obj);

         // #156619. minimized form should get restored while activating the same.
         if (form.WindowState == FormWindowState.Minimized)
         {
            form.WindowState = FormWindowState.Normal;
            ((TagData)form.Tag).Minimized = false;
         }

         if (form.IsMdiChild)
            form.MdiParent.Activate();

         form.Activate();
#endif
      }

      /// <summary>
      /// Enable/Disable MenuItem.
      /// </summary>
      internal void OnEnableMenuEntry()
      {
#if !PocketPC
         ArrayList arrayControl = ControlsMap.getInstance().object2WidgetArray(_obj1, 0);
         if (arrayControl != null)
         {
            Object obj = arrayControl[0];
            if (obj is ToolStripMenuItem)
            {
               foreach (ToolStripMenuItem mnuItem in arrayControl)
               {
                  GuiUtils.setEnabled(mnuItem, _boolVal);
               }
            }
            else if (obj is ToolStripButton)
            {
               foreach (ToolStripButton toolItem in arrayControl)
               {
                  GuiUtils.setEnabled(toolItem, _boolVal);
               }
            }
         }
#endif
      }

#if !PocketPC
      /// <summary>
      /// Activates a next or previous MDI child window.
      /// </summary>
      internal void OnActivateNextOrPreviousMDIChild()
      {
         if (Form.ActiveForm != null)
         {
            // Next window and Previous window (i.e. Ctrl+F6) should work only for MDI Children.
            Form topLevelForm = Form.ActiveForm.TopLevelControl as Form;
            if (topLevelForm != null && topLevelForm.IsMdiContainer)
            {
               foreach (Form form in Application.OpenForms)
               {
                  if (form.IsMdiContainer)
                  {
                     int nextWindow = (_boolVal == true ? 0 : 1);  // for WM_MDINEXT, 0 mean next window and 1 mean previous window.
                     MdiClient mdiClient = form.ActiveMdiChild.Parent as MdiClient;
                     NativeWindowCommon.SendMessage(mdiClient.Handle, NativeWindowCommon.WM_MDINEXT, 0, nextWindow);
                     break;
                  }
               }
            }
         }
      }

      internal void OnOpenFormDesigner()
      {
         if (_obj2 == null)
            return;

         Form form = GuiUtils.getForm(ControlsMap.getInstance().object2Widget(_obj2));
         if (form != null)
         {
            RuntimeDesignerBuilder runtimeDesignerManager = new RuntimeDesignerBuilder((Dictionary<object, ControlDesignerInfo>)_obj1, _boolVal, _str);
            using (Form designer = runtimeDesignerManager.Build(form))
            {
               designer.ShowDialog();
            }
            GC.Collect();
         }
      }

      /// <summary>
      /// Show Context Menu.
      /// </summary>
      internal void OnShowContextMenu()
      {
         Object obj = null;

         if (_obj1 == null && _obj2 == null)
            return;

         if (_obj1 != null)
            obj = ControlsMap.getInstance().object2Widget(_obj1, _line);


         int left = _x;
         int top = _y;

         Control control = null;
         if (obj != null)
         {
            if (obj is Control)
               control = (Control)obj;
            else
            {
               if (obj is LogicalControl)
               {
                  control = (Control)((LogicalControl)obj).getEditorControl();
                  if (control == null)//ContainerManager.mainControl;
                     control = ((LogicalControl)obj).ContainerManager.mainControl;
               }
               if (control == null) //when right click on label or currently not focussed edit control
                  control = (Control)ControlsMap.getInstance().object2Widget(_obj2);

            }
         }
         else
            control = (Control)ControlsMap.getInstance().object2Widget(_obj2);

         //For Table Control, call TableManager's handleContextMenu to evaluate column/header context menu.
         if (control is TableControl)
         {
            ContainerManager containerManager = ((TagData)(control.Tag)).ContainerManager;
            Point MousePos = Control.MousePosition;
            // get the relative location on the menu within the container.
            Point pt = control.PointToClient(new Point(MousePos.X, MousePos.Y));
            // set the correct context menu on the control                                         
            ((TableManager)containerManager).handleContextMenu((TableControl)control, pt);
         }
         else
            // set the correct context menu on the control                                         
            MgMenuHandler.getInstance().handleContext(control, (GuiMgControl)_obj1, (GuiMgForm)_obj2);

         // for textbox control if no context menu is attached, display system's default context menu.
         if (control is MgTextBox && control.ContextMenuStrip.Name == "Dummy")
         {
            ContextMenuStrip dummy = control.ContextMenuStrip;
            control.ContextMenuStrip = null;
            int lParam = NativeWindowCommon.MakeLong(left, top);
            NativeHeader.SendMessage(control.Handle, NativeWindowCommon.WM_CONTEXTMENU, control.Handle.ToInt32(), lParam);
            control.ContextMenuStrip = dummy;
         }
         else
         {
            ((TagData)(control.ContextMenuStrip.Tag)).ContextCanOpen = true;
            //// show the new context in the same coordinates as the opening one.
            control.ContextMenuStrip.Show(left, top);
            ((TagData)(control.ContextMenuStrip.Tag)).ContextCanOpen = false;
         }
      }
#endif

      ///<summary>
      ///  Check whether the combobox is in dropped down state.
      ///</summary>
      ///<returns>!!.</returns>
      internal void OnIsComboDroppedDowndState()
      {
         Object obj = ControlsMap.getInstance().object2Widget(_obj1, _line);
         MgComboBox comboBox = null;

         if (obj is LgCombo)
            comboBox = (MgComboBox)((LgCombo)obj).getEditorControl();
         else if (obj is ComboBox)
            comboBox = (MgComboBox)obj;

         _boolVal = comboBox != null && comboBox.DroppedDown;
      }


#endregion //Generic
   }
}
