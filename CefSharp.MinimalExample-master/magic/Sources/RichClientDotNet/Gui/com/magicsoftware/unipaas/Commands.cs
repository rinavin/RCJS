using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using com.magicsoftware.controls;
using com.magicsoftware.unipaas.env;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.gui;
using System.Windows.Forms;
using com.magicsoftware.support;

namespace com.magicsoftware.unipaas
{
   /// <summary>
   ///   Commands to the GUI thread.
   ///   There’re two types of commands (grouped in #regions):
   ///      1.	Commands that are executed immediately.
   ///      2.	Commands that are queued (overloaded methods ‘addAsync’) and then executed, 
   ///         either synchronously (method ‘invoke’) or asynchronously (method ‘beginInvoke’).
   /// </summary>
   public static class Commands
   {
      #region Commands that are executed immediately

      /// <summary>
      ///   Sync call to display message box
      /// </summary>
      /// <param name = "topMostForm"></param>
      /// <param name = "title"></param>
      /// <param name = "msg"></param>
      /// <param name = "style"></param>
      /// <returns></returns>
      public static int messageBox(GuiMgForm topMostForm, String title, String msg, int style)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.messageBox(topMostForm, title, msg, style);
      }

      ///<summary>
      /// Handles Invoke UDP operation from GUI thread
      ///</summary>
      ///<param name="contextId">Context id</param>
      public static int invokeUDP(double contextId)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.invokeUDP(contextId);
      }

      /// <summary>
      ///   Sync call to get the bounds of the object
      /// </summary>
      /// <param name = "obj"></param>
      /// <param name = "rect"></param>
      public static void getBounds(Object obj, MgRectangle rect)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.getBounds(obj, rect);
      }

      /// <summary>
      ///   Sync call to get the font metrics
      /// </summary>
      /// <param name = "mgFont"></param>
      /// <param name = "obj"></param>
      /// <param name = "fontSize"></param>
      public static MgPointF getFontMetrics(MgFont mgFont, Object obj)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.getFontMetrics(mgFont, obj);
      }

      /// <summary> Gets the resolution of the control. </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      public static MgPoint getResolution(Object obj)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.getResolution(obj);
      }

      /// <summary>
      ///   Sync call to get the handle of the form
      /// </summary>
      /// <param name = "guiMgForm"></param>
      /// <returns></returns>
      public static int getFormHandle(GuiMgForm guiMgForm)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.getFormHandle(guiMgForm);
      }

      /// <summary>
      ///   Sync call to get the handle of the control
      /// </summary>
      /// <param name = "guiMgControl"></param>
      /// <returns></returns>
      public static int getCtrlHandle(GuiMgControl guiMgControl, int line)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.getCtrlHandle(guiMgControl, line);
      }

      /// <summary>
      ///   Sync call to set cursor according to cursor shape
      /// </summary>
      /// <param name = "shape"></param>
      /// <returns></returns>
      public static bool setCursor(MgCursors shape)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.setCursor(shape);
      }

      /// <summary>
      ///   Sync call. This methode is set TRUE\FALSE that GuiUtiles\GetValue() method will be use
      ///   this falg say if to return the : (true) suggested value or (false)the real value
      ///   this method is use for MG_ACT_CTRL_MODIFY
      /// </summary>
      /// <param name = "ctrl"></param>
      /// <param name = "retSuggestedValue"></param>
      public static void setGetSuggestedValueOfChoiceControlOnTagData(GuiMgControl ctrl, int line, bool retSuggestedValue)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.setGetSuggestedValueOfChoiceControlOnTagData(ctrl, line, retSuggestedValue);
      }

      /// <summary>
      ///   Sync call to get the bounds of the object relative to shell
      /// </summary>
      /// <param name = "obj"></param>
      /// <param name = "line"></param>
      /// <param name = "rect"></param>
      /// <param name = "relativeTo"></param>
      public static void getBoundsRelativeTo(Object obj, int line, MgRectangle rect, Object relativeTo)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.getBoundsRelativeTo(obj, line, rect, relativeTo);
      }

      /// <summary>
      ///   Sync call to convert point to client of the relativeTo control
      /// </summary>
      /// <param name = "relativeTo">the relativ to hom ?, if relativeTo is null it return relative to desktop</param>
      /// <param name = "convrtPoint"></param>
      /// <param name = "ToSize"></param>
      public static void PointToClient(Object relativeTo, MgPoint convrtPoint)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.PointToClient(relativeTo, convrtPoint);
      }

      /// <summary>
      ///   Sync call to convert the Client co-ordinates of control relativeTo to screen co-ordinates
      /// </summary>
      /// <param name="relativeTo"></param>
      /// <param name="convrtPoint"></param>
      internal static void PointToScreen(Object relativeTo, MgPoint convrtPoint)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.PointToScreen(relativeTo, convrtPoint);
      }

      /// <summary>
      ///   returns if Point is contained in any of the monitors
      /// </summary>
      public static bool IsPointInMonitor(MgPoint point)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.IsPointInMonitor(point);
      }

      /// <summary>
      ///   returns if Point is contained in any of the monitors
      /// </summary>
      public static MgPoint GetLeftTopLocationFormMonitor(MgFormBase parentForm)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.GetLeftTopLocationFormMonitor(parentForm);
      }

      /// <summary>
      ///   Get bounds of MdiClient
      /// </summary>
      /// <returns>ClientRectangle of MdiClient</returns>
      internal static MgRectangle GetMDIClientBounds()
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         MgRectangle rect = guiInteractive.GetMdiClientBounds();
         return rect;
      }

      /// <summary>
      ///   Sync call to get the client bounds of the object
      /// </summary>
      /// <param name = "obj"></param>
      /// <param name = "rect"></param>
      public static void getClientBounds(Object obj, MgRectangle rect, bool clientPanelOnly)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.getClientBounds(obj, rect, clientPanelOnly);
      }

      /// <summary>
      ///   Sync call to get the bounds of the object
      /// </summary>
      /// <param name = "rect"></param>
      /// <param name = "AllMonitors"></param>
      public static void getDesktopBounds(MgRectangle rect, MgFormBase form)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.getDesktopBounds(rect, form);
      }

      /// <summary>
      ///   Sync call to get value of the control
      /// </summary>
      /// <param name = "obj"></param>
      /// <param name = "mgValue"></param>
      /// <param name = "line"></param>
      public static String getValue(Object obj, int line)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.getValue(obj, line);
      }

      /// <summary>
      ///   Sync call to set the text(html) on the browser control
      /// </summary>
      /// <param name = "browserControl"></param>
      /// <param name = "text"></param>
      /// <returns></returns>
      public static bool setBrowserText(GuiMgControl browserControl, String text)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.setBrowserText(browserControl, text);
      }

      /// <summary>
      ///   Sync call to get the text(html) on the browser control
      /// </summary>
      /// <param name = "browserControl"></param>
      /// <param name = "mgValue"></param>
      public static String getBrowserText(GuiMgControl browserControl)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.getBrowserText(browserControl);
      }

      /// <summary>
      ///   Sync call to get table top index
      /// </summary>
      /// <param name = "tablecontrol"></param>
      /// <returns></returns>
      public static int getTopIndex(GuiMgControl tablecontrol)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.getTopIndex(tablecontrol);
      }

      /// <summary>
      ///   Sync call to execute browser
      /// </summary>
      /// <param name = "browserControl"></param>
      /// <param name = "text"></param>
      /// <param name = "syncExec"></param>
      /// <param name = "language"></param>
      public static bool browserExecute(GuiMgControl browserControl, String text, bool syncExec, String language)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.browserExecute(browserControl, text, syncExec, language);
      }

      /// <summary>
      ///   Sync call to invoke reflection command
      /// </summary>
      /// <param name = "memberInfo"></param>
      /// <param name = "obj"></param>
      /// <param name = "parameters"></param>
      /// <returns></returns>
      public static Object ReflectionInvoke(MemberInfo memberInfo, Object obj, Object[] parameters)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.ReflectionInvoke(memberInfo, obj, parameters);
      }

      /// <summary>
      ///   Sync call to invoke reflection command to set a value
      /// </summary>
      /// <param name = "memberInfo"></param>
      /// <param name = "obj"></param>
      /// <param name = "parameters"></param>
      /// <param name = "value"></param>
      /// <returns></returns>
      public static Object ReflectionSet(MemberInfo memberInfo, Object obj, Object[] parameters, Object value)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.ReflectionSet(memberInfo, obj, parameters, value);
      }

      /// <summary>
      ///   Sync call to Directory Dialog Box
      /// </summary>
      /// <param name = "caption">description for the dialog window</param>
      /// <param name = "path">initial path to browse</param>
      /// <param name = "bShowNewFolder">should show the new folder button?</param>
      /// <returns>directory path selected by user</returns>
      public static String directoryDialogBox(String caption, String path, Boolean bShowNewFolder)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.directoryDialogBox(caption, path, bShowNewFolder);
      }

      /// <summary>
      ///   Sync call to File Open Dialog Box
      /// </summary>
      /// <param name = "title">Dialog window caption</param>
      /// <param name = "dirName">Initial directory</param>
      /// <param name = "fileName"></param>
      /// <param name = "filterNames">filter string</param>
      /// <param name = "checkExists">verify opened file exists</param>
      /// <param name = "multiSelect">enable selecting multiple files</param>
      /// <returns>file path selected by user</returns>
      public static String fileOpenDialogBox(String title, String dirName, String fileName, String filterNames,
                                             Boolean checkExists, Boolean multiSelect)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.fileOpenDialogBox(title, dirName, fileName, filterNames, checkExists, multiSelect);
      }

      /// <summary>
      ///   Sync call to File Save Dialog Box
      /// </summary>
      /// <param name = "title">caption of the dialog window</param>
      /// <param name = "dirName">initial directory</param>
      /// <param name = "fileName"></param>
      /// <param name = "filterNames">filter string</param>
      /// <param name = "defaultExtension">default extension for file name</param>
      /// <param name = "overwritePrompt">should prompt when overwriting an existing file?</param>
      /// <returns>file path selected by user</returns>
      public static String fileSaveDialogBox(String title, String dirName, String fileName, String filterNames,
                                             String defaultExtension, Boolean overwritePrompt)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.fileSaveDialogBox(title, dirName, fileName, filterNames, defaultExtension,
                                                 overwritePrompt);
      }

      /// <summary>
      ///   Sync call to Put Command to create dialog
      /// </summary>
      /// <param name = "handle">reference to the dialog handlers</param>
      /// <param name = "objType">parameters to be passed to objects constructor</param>
      /// <param name = "parameters"></param>
      public static void createDialog(DialogHandler handle, Type objType, Object[] parameters)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.createDialog(handle, objType, parameters);
      }

      /// <summary>
      ///   Sync call to Put Command to open dialog
      /// </summary>
      /// <param name = "handle"></param>
      public static void openDialog(DialogHandler handle)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.openDialog(handle);
      }

      /// <summary>
      ///   Sync call to Put Command to close dialog
      /// </summary>
      /// <param name = "handle"></param>
      public static void closeDialog(DialogHandler handle)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.closeDialog(handle);
      }

      /// <summary>
      ///   Sync call to set the text to the the control
      /// </summary>
      /// <param name = "control"></param>
      /// <param name = "line"></param>
      /// <param name = "text"></param>
      /// <returns></returns>
      public static bool setEditText(GuiMgControl control, int line, String text)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.setEditText(control, line, text);
      }

      /// <summary>
      ///   Insert text to a text control at a given position
      /// </summary>
      /// <param name = "control"></param>
      /// <param name = "line"></param>
      /// <param name = "startPosition">where to add the text</param>
      /// <param name = "textToInsert">the text to add</param>
      /// <returns></returns>
      public static bool insertEditText(GuiMgControl control, int line, int startPosition, String textToInsert)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.insertEditText(control, line, startPosition, textToInsert);
      }

      /// <summary>
      ///   Sync call to set the text to the the control
      /// </summary>
      /// <param name = "control"></param>
      /// <param name = "line"></param>
      /// <param name = "start"></param>
      /// <param name = "end"></param>
      /// <param name = "caretPos"></param>
      public static void setSelection(GuiMgControl control, int line, int start, int end, int caretPos)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.setSelection(control, line, start, end, caretPos);
      }

      /// <summary>
      ///   set suggested value of choice control
      /// </summary>
      /// <param name = "control"></param>
      /// <param name = "suggestedValue"></param>
      public static void setSuggestedValue(GuiMgControl control, string suggestedValue)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.setSuggestedValue(control, suggestedValue);
      }

      /// <summary>
      ///   Sync call to get the position of the caret on the control
      /// </summary>
      /// <param name = "control"></param>
      /// <param name = "line"></param>
      /// <param name = "mgValue"></param>
      public static int caretPosGet(GuiMgControl control, int line)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.caretPosGet(control, line);
      }

      /// <summary>
      ///   Get if Caret is at the top of TextBox
      /// </summary>
      /// <param name = "control"></param>
      /// <param name = "line"></param>
      public static bool getIsTopOfTextBox(GuiMgControl control, int line)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.getIsTopOfTextBox(control, line);
      }

      /// <summary>
      ///   Get if Caret is at the end of TextBox
      /// </summary>
      /// <param name = "control"></param>
      /// <param name = "line"></param>
      public static bool getIsEndOfTextBox(GuiMgControl control, int line)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.getIsEndOfTextBox(control, line);
      }

      /// <summary>
      ///   Attach Dnkey to Object
      /// </summary>
      /// <param name = "control"></param>
      /// <param name = "key"></param>
      public static void AttachDnKeyToObject(GuiMgControl control, int key)
      {
         GuiInteractive guiUtils = new GuiInteractive();
         guiUtils.AttachDnKeyToObject(control, key);
      }

      /// <summary>
      ///   Sync call to get the selection on the given control
      /// </summary>
      /// <param name = "control"></param>
      /// <param name = "line"></param>
      /// <param name = "point"></param>
      public static void selectionGet(GuiMgControl control, int line, MgPoint point)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.selectionGet(control, line, point);
      }

      /// <summary>
      ///   Sync call to (Korean IME) Send IME Message to MgTextBox
      /// </summary>
      /// <param name = "control"></param>
      /// <param name = "ln"></param>
      /// <param name = "im"></param>
      /// <returns></returns>
      public static int sendImeMsg(GuiMgControl control, int ln, ImeParam im)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.sendImeMsg(control, ln, im);
      }

      /// <summary>
      ///   Sync call to write a string to the clipboard. The clip get get the data 
      ///   either from a control or from the passed string in mgValue.
      /// </summary>
      /// <param name = "control"></param>
      /// <param name = "line"></param>
      /// <param name = "mgValue"></param>
      public static void clipboardWrite(GuiMgControl control, int line, String clipData)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.clipboardWrite(control, line, clipData);
      }

      /// <summary>
      ///   Sync call to read from the clipboard to a string
      /// </summary>
      /// <returns></returns>
      public static String clipboardRead()
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.clipboardRead();
      }

      /// <summary>
      ///   Sync call to paste from clipboard to the control.
      /// </summary>
      /// <param name = "control"></param>
      /// <param name = "line"></param>
      public static void clipboardPaste(GuiMgControl control, int line)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.clipboardPaste(control, line);
      }

      /// <summary>
      ///   Sync call to Post a key event (emulate keys pressed by the user).
      /// </summary>
      /// <param name = "control"></param>
      /// <param name = "line"></param>
      /// <param name = "keys"></param>
      /// <param name = "PostChar"></param>
      public static void postKeyEvent(GuiMgControl control, int line, String keys,
         bool PostChar, bool forceLogicalControlTextUpdate)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.postKeyEvent(control, line, keys, PostChar, forceLogicalControlTextUpdate);
      }

      /// <summary>
      ///   Sends WM_CHAR to the specified control via GUI thread.
      /// </summary>
      /// <param name = "control"></param>
      /// <param name = "line"></param>
      /// <param name = "chr"></param>
      public static void postCharEvent(GuiMgControl control, int line, Char chr)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.postCharEvent(control, line, chr);
      }

      /// <summary>
      ///   Sync call to check auto wide
      /// </summary>
      /// <param name = "guiMgControl"></param>
      /// <param name = "line"></param>
      /// <param name = "lenCheck"></param>
      public static void checkAutoWide(GuiMgControl guiMgControl, int line, bool lenCheck)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.checkAutoWide(guiMgControl, line, lenCheck);
      }

      /// <summary>
      ///   Sync call to dispose all the shells. last dispose will close the display.
      /// </summary>
      public static void disposeAllForms()
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.disposeAllForms();
      }

      /// <summary>
      /// Clear all images loaded to MgGui's cache (the volatile cache, i.e. not the files system cache of the RC itself).
      /// </summary>
      public static void ClearImagesCache()
      {
         ImagesCache.GetInstance().Clear();
      }

      /// <summary>
      ///   Sync call to get number of rows in the table
      /// </summary>
      /// <param name = "control"></param>
      /// <returns></returns>
      public static int getRowsInPage(GuiMgControl control)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.getRowsInPage(control);
      }

      /// <summary>
      ///   Sync call to get the number of hidden rows (partially or fully) in table
      /// </summary>
      /// <param name = "control"></param>
      /// <returns></returns>
      public static int GetHiddenRowsCountInTable(GuiMgControl control)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.GetHiddenRowsCountInTable(control);
      }

      /// <summary>
      ///   Sync call to get the last window state
      /// </summary>
      /// <param name = "guiMgForm"></param>
      /// <returns></returns>
      public static int getLastWindowState(GuiMgForm guiMgForm)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.getLastWindowState(guiMgForm);
      }

      /// <summary>
      ///   Sync call to get height of all frames in frameset
      /// </summary>
      /// <param name = "frameset"></param>
      /// <returns></returns>
      public static Object getFramesBounds(GuiMgControl frameset)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.getFramesBounds(frameset);
      }

      /// <summary>
      ///   Sync call to get linked parent idx of frameset
      /// </summary>
      /// <param name = "frameset"></param>
      /// <returns></returns>
      public static int getLinkedParentIdx(GuiMgControl frameset)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.getLinkedParentIdx(frameset);
      }

      /// <summary>
      ///   Sync call to get form bounds
      /// </summary>
      /// <param name = "guiMgForm"></param>
      /// <returns></returns>
      public static Rectangle getFormBounds(GuiMgForm guiMgForm)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.getFormBounds(guiMgForm);
      }

      /// <summary>
      ///   Sync call to get the columns state --- layer, width and widthForFillTablePlacement
      /// </summary>
      /// <param name = "tableCtrl"></param>
      /// <returns></returns>
      public static List<int[]> getColumnsState(GuiMgControl tableCtrl)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.getColumnsState(tableCtrl);
      }

      #region DRAG And DROP
#if !PocketPC
      /// <summary> 
      /// get the data for a specific format from dropped data.
      /// </summary>
      /// <param name="format">format - for which we want to retrieve the data. </param>
      /// <param name="userFormatStr">User defined format. It will be Null for internal formats.</param>
      /// <returns> string </returns>
      public static String GetDroppedData (ClipFormats format, String userFormatStr)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.GetDroppedData(format, userFormatStr);
      }

      /// <summary> 
      /// Get the point X, where drop occurs.
      /// </summary>
      /// <returns> int - X co-ordinate of the dropped point </returns>
      public static int GetDroppedX ()
      {
         MgPoint point = new MgPoint(0,0);
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.GetDropPoint(point);
         return point.x;
      }

      /// <summary> 
      /// Get the point Y, where drop occurs.
      /// </summary>
      /// <returns> int - Y co-ordinates of the dropped point </returns>
      public static int GetDroppedY()
      {
         MgPoint point = new MgPoint(0, 0);
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.GetDropPoint(point);
         return point.y;
      }

      /// <summary> 
      /// Check whether the specified format is available in Dropped data.
      /// <param name="format"> format - which we want to check.</param>
      /// <param name="userFormatStr">User defined format. It will be Null for internal formats.</param>
      /// </summary>
      /// <returns> true if format is present in DroppedData, otherwise false. </returns>
      public static bool CheckDropFormatPresent (ClipFormats format, String userFormatStr)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.CheckDropFormatPresent(format, userFormatStr);
      }

      /// <summary> 
      /// Get the SelectionStart and SelectionEnd from DroppedData.
      /// Relevant only when drop occurs on an Edit control.
      /// </summary>
      /// <param name="selectionStart">Starting index of selection for edit control when drop occurs on Edit</param>
      /// <param name="selectionLength">Length of the selection in edit control.</param>
      public static void GetSelectionForDroppedControl(ref int selectionStart, ref int selectionLength)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.GetSelectionForDroppedControl(ref selectionStart, ref selectionLength);
      }

      /// <summary>
      /// To get the drag status. 
      /// TRUE : Between -->  Initiate Drag operation(i.e. MOUSE_MOVE) to Actual drag (i.e. control.DoDragDrop).
      /// </summary>
      /// <returns>bool</returns>
      public static bool IsBeginDrag ()
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.IsBeginDrag();
      }
#endif    // !PocketPC
      #endregion

#if PocketPC
      // <summary> Sync call to get the accumulated text </summary>
      // <param name="guiMgControl"></param>
      // <returns></returns>
      public static String accumulatedTextGet(GuiMgControl guiMgControl)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.accumulatedTextGet(guiMgControl);
      }
#endif

      /// <summary> Returns whether control is focusable or not. </summary>
      /// <param name="guiMgControl"> the control which needs to be checked. </param>
      /// <returns></returns>
      public static bool canFocus(GuiMgControl guiMgControl)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.canFocus(guiMgControl);
      }

      /// <summary>
      /// Set Cursor for Print Preview
      /// </summary>
      /// <param name="printPreviewDataPtr"></param>
      public static void printPreviewSetCursor(IntPtr printPreviewDataPtr)
      {
         PrintPreview printPreview = new PrintPreview();
         printPreview.SetCursor(printPreviewDataPtr);
      }

      /// <summary>
      /// Creates Print Preview Form
      /// </summary>
      /// <param name="contextId">context id</param>
      /// <param name="ioPtr">pointer to current IORT object</param>
      /// <param name="copies">number of copies</param>
      /// <param name="enablePDlg">indicates whether to enable Print dialog</param>
      /// <param name="callerForm">caller form of Print Preview</param>
      public static void printPreviewStart(Int64 contextID, IntPtr ioPtr, int copies, bool enablePDlg, MgFormBase callerForm)
      {
#if !PocketPC
         PrintPreview printPreview = new PrintPreview();
         Form callerWindow = null;
         if (callerForm != null)
         {
            ControlsMap controlsMap = ControlsMap.getInstance();
            callerWindow = GuiUtils.FindForm((Control)controlsMap.object2Widget(callerForm));
         }
         printPreview.Start(contextID, ioPtr, copies, enablePDlg, callerWindow);
#endif
      }

      /// <summary>
      /// Update Print Preview
      /// </summary>
      /// <param name="prnPrevData">print preview data</param>
      public static void printPreviewUpdate(IntPtr prnPrevData)
      {
#if !PocketPC
         PrintPreview printPreview = new PrintPreview();
         printPreview.Update(prnPrevData);
#endif
      }

      /// <summary>
      /// Create Rich Edit
      /// </summary>
      /// <param name="contextId"></param>
      /// <param name="ctrlPtr"></param>
      /// <param name="prmPtr"></param>
      /// <param name="style"></param>
      /// <param name="dwExStyle"></param>
      public static void CreateRichWindow(Int64 contextID, IntPtr ctrlPtr, IntPtr prmPtr, uint style, uint dwExStyle)
      {
#if !PocketPC
         PrintPreview printPreview = new PrintPreview();
         printPreview.createRichWindow(contextID, ctrlPtr, prmPtr, style, dwExStyle);
#endif
      }

      /// <summary>
      /// Closes Print Preview form
      /// </summary>
      /// <param name="hWnd"> Print Preview data</param>
      /// <param name="hWnd"> Handle of Print Preview form</param>
      public static void printPreviewClose(IntPtr printPreviewData, IntPtr hWnd)
      {
#if !PocketPC
         PrintPreview printPreview = new PrintPreview();
         printPreview.Close(printPreviewData, hWnd);
#endif
      }

      /// <summary>
      /// Get Active form
      /// </summary>
      /// <returns></returns>
      public static IntPtr printPreviewGetActiveForm()
      {
#if !PocketPC
         PrintPreview printPreview = new PrintPreview();
         return printPreview.GetActiveForm();
#endif
      }

      ///<summary>
      ///  Creates a window using win32 API from GUI Thread.
      ///</summary>
      ///<param name="exStyle">!!.</param>
      ///<param name="className">!!.</param>
      ///<param name="windowName">!!.</param>
      ///<param name="style">!!.</param>
      ///<param name="x">!!.</param>
      ///<param name="y">!!.</param>
      ///<param name="width">!!.</param>
      ///<param name="height">!!.</param>
      ///<param name="hwndParent">!!.</param>
      ///<param name="hMenu">!!.</param>
      ///<param name="hInstance">!!.</param>
      ///<param name="lParam">!!.</param>
      ///<returns>handle of window</returns>
      public static IntPtr CreateGuiWindow(uint exStyle, String className, String windowName, uint style, int x, int y, int width, int height,
                                           IntPtr hwndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lParam)
      {
#if PocketPC
         return IntPtr.Zero;
#else

         PrintPreview printPreview = new PrintPreview();
         return printPreview.CreateGuiWindow(exStyle, className, windowName, style, x, y, width, height, hwndParent, hMenu, hInstance, lParam);
#endif
      }

      /// <summary>
      /// Destroy a window
      /// </summary>
      /// <param name="hWndPtr">Handle of window</param>
      public static void DestroyGuiWindow(IntPtr hWndPtr)
      {
#if !PocketPC
         PrintPreview printPreview = new PrintPreview();
         printPreview.DestroyGuiWindow(hWndPtr);
#endif
      }

      /// <summary>
      ///  Shows Print Preview Form
      /// </summary>
      /// <param name="hWnd"> Handle of Print Preview form</param>
      public static void printPreviewShow(IntPtr ioPtr)
      {
#if !PocketPC
         PrintPreview printPreview = new PrintPreview();
         printPreview.Show(ioPtr);
#endif
      }

      /// <summary>
      /// Show Print Dialog
      /// </summary>
      /// <param name="prnPrevData"></param>
      public static int showPrintDialog(IntPtr gpd)
      {
#if PocketPC
         return 0;
#else
         PrintPreview printPreview = new PrintPreview();
         return printPreview.showPrintDialog(gpd);
#endif
      }

      /// <summary>
      /// Gets the selected indice from the listbox control.
      /// </summary>
      /// <param name="guiMgControl">GUI mgControl object corresponding to listbox.</param>
      /// <returns>comma seperated string of selected indice</returns>
      public static string getSelectedIndice(GuiMgControl guiMgControl)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.GetSelectedIndice(guiMgControl);
      }

      /// <summary>
      /// Returns whether the indent has been applied to Rich Edit
      /// </summary>
      /// <param name="guiMgControl"></param>
      /// <returns></returns>
      public static bool getHasIndent(GuiMgControl guiMgControl)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.GetHasIndent(guiMgControl);
      }

      /// <summary>
      /// Update the Control.TagData.MapData of a GuiControl with the newObjectToSet
      /// </summary>
      /// <param name="newObjectToSet"></param>
      public static void MapWidget(Object newObjectToSet)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.MapWidget(newObjectToSet);
      }

      /// <summary>
      /// Returns count of currently opened MDIChilds.
      /// </summary>
      /// <returns></returns>
      public static int GetMDIChildCount()
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.GetMDIChildCount();
      }

#if !PocketPC
      /// <summary>
      /// Activates a next or previous MDI child
      /// </summary>
      /// <param name="nextWindow">indicates whether to activate next window or not</param>
      public static void ActivateNextOrPreviousMDIChild(bool nextWindow)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.ActivateNextOrPreviousMDIChild(nextWindow);
      }

      /// <summary> Remove the ValueChangedHandler of a .Net control. </summary>
      /// <param name="guiMgControl"></param>
      public static void RemoveDNControlValueChangedHandler(GuiMgControl guiMgControl)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.RemoveDNControlValueChangedHandler(guiMgControl);
      }

      /// <summary> Add the ValueChangedHandler of a .Net control. </summary>
      /// <param name="guiMgControl"></param>
      public static void AddDNControlValueChangedHandler(GuiMgControl guiMgControl)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.AddDNControlValueChangedHandler(guiMgControl);
      }

      /// <summary> Gets the RTF value of the RTF edit control which was set before entering it. </summary>
      /// <param name="guiMgControl"></param>
      /// <param name="line"></param>
      /// <returns></returns>
      internal static string GetRtfValueBeforeEnteringControl(GuiMgControl guiMgControl, int line)
      {
         Debug.Assert(guiMgControl.isRichEditControl());

         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.GetRtfValueBeforeEnteringControl(guiMgControl, line);
      }
#endif

#if !PocketPC

      /// <summary> Clear the data table. </summary>
      /// <param name="dvControl"></param>
      /// <param name="dataTable"></param>
      internal static void ClearDatatable(GuiMgControl dvControl, Object dataTable)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.ClearDatatable(dvControl, dataTable);
      }

      /// <summary> Set datasource property of dataview control. </summary>
      /// <param name="dvControl"></param>
      /// <param name="dataTable"></param>
      /// <param_name="propertyName"></param>
      internal static void SetDataSourceToDataViewControl(GuiMgControl dvControl, Object dataTable, string propertyName)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.SetDataSourceToDataViewControl(dvControl, dataTable, propertyName);
      }

      /// <summary>
      ///   Get Row Position of DataTable attached to DV Control.
      /// </summary>
      /// <param name = "dataTable"></param> 
      /// <param name = "line"></param>
      internal static int GetDVControlPositionIsn(Object dataTable, int line)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.GetDVControlPositionIsn(dataTable, line);
      }
#endif
      #endregion // Commands that are executed immediately


      #region Commands that are queued and then executed, either synchronously or asynchronously

      /// <summary>
      ///   BEEP
      /// </summary>
      public static void addAsync(CommandType commandType)
      {
         GuiCommandQueue.getInstance().add(commandType);
      }

      /// <summary>
      ///   OPEN_FORM, DISPOSE_OBJECT REMOVE_CONTROLS EXECUTE_LAYOUT CLOSE_SHELL, REMOVE_ALL_TABLE_ITEMS,
      ///   REMOVE_CONTROLS, INVALIDATE_TABLE, SET_SB_LAYOUT_DATA, SET_WINDOW_ACTIVE
      ///   SET_FRAMESET_LAYOUT_DATA, RESUME_LAYOUT, UPDATE_MENU_VISIBILITY
      ///   ORDER_MG_SPLITTER_CONTAINER_CHILDREN, CLEAR_TABLE_COLUMNS_SORT_MARK, START_TIMER
      /// </summary>
      public static void addAsync(CommandType commandType, Object obj)
      {
         GuiCommandQueue.getInstance().add(commandType, obj);
      }

      /// <summary>
      ///   OPEN_FORM, OPEN HELP FORM
      /// </summary>
      public static void addAsync(CommandType commandType, Object obj, bool boolVal, String formName)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, boolVal, formName);
      }

      /// <summary>
      ///  SHOW_FORM
      /// </summary>
      public static void addAsync(CommandType commandType, Object obj, bool boolVal, bool isHelpWindow, String formName)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, boolVal, isHelpWindow, formName);
      }

      /// <summary>
      ///   EXECUTE_LAYOUT, REORDER_FRAME, PROP_SET_SHOW_ICON, SET_FORMSTATE_APPLIED, PROP_SET_FILL_WIDTH
      /// </summary>
      public static void addAsync(CommandType commandType, Object obj, bool boolVal)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, boolVal);
      }

      /// <summary>
      ///   ADD_DVCONTROL_HANDLER, REMOVE_DVCONTROL_HANDLER
      /// </summary>
      public static void addAsync(CommandType commandType, Object obj, Object obj1)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, obj1);
      }


      /// <summary>
      ///   PROP_SET_DEFAULT_BUTTON style : not relevant PROP_SET_SORT_COLUMN
      /// </summary>
      /// <param name = "line">TODO CREATE_RADIO_BUTTON PROP_SET_SORT_COLUMN layer, line,style isn't relevant parentObject:
      ///   must to be the table control object: must to be the Column control
      /// </param>
      public static void addAsync(CommandType commandType, Object parentObject, Object obj, int layer, int line,
                                  int style)
      {
         GuiCommandQueue.getInstance().add(commandType, parentObject, obj, layer, line, style);
      }

      /// <summary>
      ///   SELECT_TEXT
      /// </summary>
      public static void addAsync(CommandType commandType, Object obj, int line, int num1, int num2, int num3)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, line, num1, num2, num3);
      }

      /// <summary>
      ///   CREATE_FORM, CREATE_HELP_FORM
      /// </summary>
      /// <param name = "commandType"></param>
      /// <param name = "parentObject"></param>
      /// <param name = "obj"></param>
      /// <param name = "windowType"></param>
      /// <param name = "formName"></param>
      /// <param name = "isHelpWindow"></param>
      public static void addAsync(CommandType commandType, Object parentObject, Object obj, WindowType windowType,
                                  String formName, bool isHelpWindow, bool createInternalFormForMDI, bool shouldBlock)
      {
         GuiCommandQueue.getInstance().add(commandType, parentObject, obj, windowType, formName, isHelpWindow, createInternalFormForMDI, shouldBlock);
      }

      /// <summary>
      ///   CREATE_LABEL, CREATE_EDIT, CREATE_BUTTON, CREATE_COMBO_BOX, CREATE_LIST_BOX,
      ///   CREATE_RADIO_BOX, CREATE_IMAGE, CREATE_CHECK_BOX, CREATE_TAB, CREATE_TABLE, CREATE_SUB_FORM,
      ///   CREATE_BROWSER, CREATE_GROUP, CREATE_STATUS_BAR, CREATE_TREE, CREATE_FRAME,
      /// </summary>
      /// <param name = "line">TODO
      ///   PROP_SET_SORT_COLUMN layer, line,style isn't relevant parentObject: must to be the table control object:
      ///   must to be the Column control- not support.
      /// </param>
      /// <param name = "bool">TODO
      /// </param>
      public static void addAsync(CommandType commandType, Object parentObject, Object obj, int line, int style,
                                  List<String> stringList, List<GuiMgControl> ctrlList, int columnCount, bool boolVal,
                                  bool boolVal1, int number1, Type type, int number2, Object obj1)
      {
         GuiCommandQueue.getInstance().add(commandType, parentObject, obj, line, style, stringList, ctrlList,
                                           columnCount,
                                           boolVal, boolVal1, number1, type, number2, obj1);
      }

      /// <summary>
      ///  Creates edit control inside help window.
      /// </summary>
      public static void addAsync(CommandType commandType, Object parentObject, Object obj, int line, int style,
                                  List<String> stringList, List<GuiMgControl> ctrlList, int columnCount, bool boolVal,
                                  bool boolVal1,
                                  int number1, Type type, int number2, Object obj1, bool isParentHelpWindow, DockingStyle dockingStyle)
      {

         GuiCommandQueue.getInstance().add(commandType, parentObject, obj, line, style, stringList, ctrlList,
                                           columnCount,
                                           boolVal, boolVal1, number1, type, number2, obj1, isParentHelpWindow, dockingStyle);
      }

      /// <summary>
      ///   Applies for: REFRESH_TABLE, SELECT_TEXT, PROP_SET_READ_ONLY, PROP_SET_MODIFIABLE, PROP_SET_ENABLE,
      ///   PROP_SET_CHECKED (Table): PROP_SET_LINE_VISIBLE, PROP_SET_RESIZABLE, SET_FOCUS, PROP_SET_MOVEABLE
      ///   SET_VERIFY_IGNORE_AUTO_WIDE, PROP_SET_AUTO_WIDE, PROP_SET_SORTABLE_COLUMN 
      ///   PROP_SET_MENU_DISPLAY, PROP_SET_TOOLBAR_DISPLAY PROP_HORIZONTAL_PLACEMENT, PROP_VERTICAL_PLACEMENT
      ///   PROP_SET_MULTILINE, PROP_SET_PASSWORD_EDIT, PROP_SET_MULTILINE_VERTICAL_SCROLL, PROP_SET_BORDER, 
      ///   CHANGE_COLUMN_SORT_MARK.
      /// </summary>
      /// <param name = "commandType"></param>
      /// <param name = "obj"></param>
      /// <param name = "number"> 
      ///   If command type is <code>CHANGE_COLUMN_SORT_MARK</code> then number means direction.
      ///   Otherwise it means line.
      /// </param>
      /// <param name = "boolVal">
      ///   If command type is <code>CHANGE_COLUMN_SORT_MARK</code> this value is ignored.
      /// </param>
      public static void addAsync(CommandType commandType, Object obj, int number, bool boolVal)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, number, boolVal);
      }

      /// <summary>
      ///   PROP_SET_VISIBLE, SET_ACTIVETE_KEYBOARD_LAYOUT
      /// </summary>
      public static void addAsync(CommandType commandType, Object obj, int number, bool boolVal,
                                  bool executeParentLayout)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, number, boolVal, executeParentLayout);
      }

      /// <summary>
      ///    PROP_SET_PLACEMENT
      ///   subformAsControl isn't relevant, need to be false
      /// </summary>
      /// <param name="x"></param>
      /// <param name="y"></param>
      /// <param name="width"></param>
      /// <param name="height"></param>
      /// <param name="boolVal"></param>
      /// <param name="bool1"></param>
      public static void addAsync(CommandType commandType, Object obj, int line, int x, int y, int width, int height,
                                  bool boolVal, bool bool1, int? runtimeDesignerXDiff, int? runtimeDesignerYDiff)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, line, x, y, width, height, boolVal, bool1, runtimeDesignerXDiff, runtimeDesignerYDiff);
      }

      /// <summary>
      ///   PROP_SET_BOUNDS, PROP_SET_COLUMN_WIDTH, PROP_SET_SB_PANE_WIDTH, 
      ///   subformAsControl isn't relevant, need to be false
      /// </summary>
      /// <param name="x"></param>
      /// <param name="y"></param>
      /// <param name="width"></param>
      /// <param name="height"></param>
      /// <param name="boolVal"></param>
      /// <param name="bool1"></param>
      public static void addAsync(CommandType commandType, Object obj, int line, int x, int y, int width, int height,
                                  bool boolVal, bool bool1)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, line, x, y, width, height, boolVal, bool1, 0, 0);
      }

      /// <summary>
      /// REGISTER_DN_CTRL_VALUE_CHANGED_EVENT
      /// </summary>
      /// <param name="commandType"></param>
      /// <param name="obj"></param>
      /// <param name="eventName"></param>
      public static void addAsync(CommandType commandType, Object obj, string eventName)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, eventName);
      }

      /// <summary>
      ///   PROP_SET_SELECTION PROP_SET_TEXT_SIZE_LIMIT, PROP_SET_VISIBLE_LINES, PROP_SET_MIN_WIDTH, PROP_SET_MIN_HEIGHT,
      ///   SET_WINDOW_STATE, VALIDATE_TABLE_ROW, SET_ORG_COLUMN_WIDTH, PROP_SET_COLOR_BY,
      ///   PROP_SET_TRANSLATOR, PROP_SET_HORIZANTAL_ALIGNMENT, PROP_SET_MULTILINE_WORDWRAP_SCROLL
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="commandType"></param>
      /// <param name="number"></param>
      public static void addAsync(CommandType commandType, Object obj, int line, int number)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, line, number);
      }

      /// <summary>
      ///   INSERT_ROWS, REMOVE_ROWS
      /// </summary>
      /// <param name = "commandType"></param>
      /// <param name = "obj"></param>
      /// <param name = "line"></param>
      /// <param name = "objectValue1"></param>
      /// <param name = "objectValue2"></param>
      /// <param name = "boolVal"></param>
      public static void addAsync(CommandType commandType, Object obj, int line, Object objectValue1,
                                  Object objectValue2, bool boolVal)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, line, objectValue1, objectValue2, boolVal);
      }

      /// <summary>
      /// PROP_SET_FOCUS_COLOR, PROP_SET_HOVERING_COLOR, PROP_SET_VISITED_COLOR
      /// </summary>
      /// <param name="commandType"></param>
      /// <param name="obj"></param>
      /// <param name="line"></param>
      /// <param name="objectValue1"></param>
      /// <param name="objectValue2"></param>
      /// <param name="intVal"></param>
      public static void addAsync(CommandType commandType, Object obj, int line, Object objectValue1,
                                  Object objectValue2, int intVal)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, line, objectValue1, objectValue2, intVal);
      }

      /// <summary>
      ///   PROP_SET_VISITED_COLOR, PROP_SET_HOVERING_COLOR, PROP_SET_GRADIENT_COLOR, PROP_SET_FOCUS_COLOR
      /// </summary>
      /// <param name = "commandType"></param>
      /// <param name = "obj"></param>
      /// <param name = "line"></param>
      /// <param name = "objectValue1"></param>
      /// <param name = "objectValue2"></param>
      public static void addAsync(CommandType commandType, Object obj, int line, Object objectValue1,
                                  Object objectValue2)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, line, objectValue1, objectValue2);
      }

      /// <summary>
      ///   PROP_SET_BACKGOUND_COLOR, PROP_SET_FOREGROUND_COLOR, PROP_SET_FONT, PROP_SET_ALTENATING_COLOR
      ///   PROP_SET_STARTUP_POSITION, CREATE_ENTRY_IN_CONTROLS_MAP
      /// </summary>
      /// <param name = "line">TODO PROP_SET_ROW_HIGHLIGHT_COLOR, PROP_SET_ROW_HIGHLIGHT_FGCOLOR : line not relevant
      ///   PROP_SET_FORM_BORDER_STYLE,SET_ALIGNMENT, SET_FRAMES_WIDTH, SET_FRAMES_HEIGHT, REORDER_COLUMNS
      /// </param>
      public static void addAsync(CommandType commandType, Object obj, int line, Object objectValue)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, line, objectValue);
      }

      /// <summary>
      ///   PROP_SET_TOOLTIP, PROP_SET_TEXT style: not relevant PROP_SET_WALLPAPER PROP_SET_IMAGE_FILE_NAME
      ///   PROP_SET_URL, PROP_SET_ICON_FILE_NAME : style isn't relevant
      /// </summary>
      /// <param name = "line">TODO </param>
      public static void addAsync(CommandType commandType, Object obj, int line, String str, int style)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, line, str, style);
      }

      /// <summary>
      /// DRAG_SET_DATA.
      /// </summary>
      public static void addAsync(CommandType commandType, Object obj, int line, String str, String userDropFormat, int style)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, line, str, userDropFormat, style);
      }

      /// <summary>
      ///   PROP_SET_IMAGE_DATA
      /// </summary>
      /// <param name = "line">TODO </param>
      public static void addAsync(CommandType commandType, Object obj, int line, byte[] byteArray, int style)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, line, byteArray, style);
      }

      /// <summary>
      ///   PROP_SET_ITEMS_LIST
      /// </summary>
      /// <param name = "line">TODO </param>
      public static void addAsync(CommandType commandType, Object obj, int line, String[] displayList, bool bool1)
      {
         GuiCommandQueue.getInstance().add(commandType, obj, line, displayList, bool1);
      }

      /// <summary>
      ///   PROP_SET_MENU, REFRESH_MENU_ACTIONS
      /// </summary>
      public static void addAsync(CommandType commandType, Object parentObj, GuiMgForm containerForm,
                                  MenuStyle menuStyle, GuiMgMenu guiMgMenu, bool parentTypeForm)
      {
         GuiCommandQueue.getInstance().add(commandType, parentObj, containerForm, menuStyle, guiMgMenu, parentTypeForm);
      }

      /// <summary>
      ///   CREATE_MENU
      /// </summary>
      public static void addAsync(CommandType commandType, Object parentObj, GuiMgForm containerForm,
                                  MenuStyle menuStyle, GuiMgMenu guiMgMenu, bool parentTypeForm,
                                  bool shouldShowPulldownMenu)
      {
         GuiCommandQueue.getInstance().add(commandType, parentObj, containerForm, menuStyle, guiMgMenu, parentTypeForm,
                                           shouldShowPulldownMenu);
      }

      /// <summary>
      ///   CREATE_MENU_ITEM
      /// </summary>
      /// <param name = "commandType"> </param>
      /// <param name = "parentObj"> </param>
      /// <param name = "menuStyle"> </param>
      /// <param name = "menuEntry"> </param>
      /// <param name = "guiMgForm"> </param>
      /// <param name = "index"> </param>
      public static void addAsync(CommandType commandType, Object parentObj, MenuStyle menuStyle,
                                  GuiMenuEntry menuEntry, GuiMgForm guiMgForm, int index)
      {
         GuiCommandQueue.getInstance().add(commandType, parentObj, menuStyle, menuEntry, guiMgForm, index);
      }

      /// <summary>
      ///   PROP_SET_CHECKED PROP_SET_ENABLE PROP_SET_VISIBLE PROP_SET_MENU_ENABLE PROP_SET_MENU_VISIBLE Above
      ///   properties for menu entry
      /// </summary>
      /// <param name = "commandType"> </param>
      /// <param name = "mnuRef"></param>
      /// <param name = "menuEntry"> </param>
      /// <param name = "val"> </param>
      public static void addAsync(CommandType commandType, MenuReference mnuRef, GuiMenuEntry menuEntry, Object val)
      {
         GuiCommandQueue.getInstance().add(commandType, mnuRef, menuEntry, val);
      }

      /// <summary>
      ///   DELETE_MENU_ITEM
      /// </summary>
      /// <param name = "commandType"> </param>
      /// <param name = "parentObj"> </param>
      /// <param name = "menuStyle"> </param>
      /// <param name = "menuEntry"> </param>
      public static void addAsync(CommandType commandType, Object parentObj, MenuStyle menuStyle,
                                  GuiMenuEntry menuEntry)
      {
         GuiCommandQueue.getInstance().add(commandType, parentObj, menuStyle, menuEntry);
      }

      /// <summary>
      ///   CREATE_TOOLBAR, REFRESH_TOOLBAR
      /// </summary>
      /// <param name = "commandType"> </param>
      /// <param name = "form"> </param>
      /// <param name = "newToolbar"> </param>
      public static void addAsync(CommandType commandType, GuiMgForm form, Object newToolbar)
      {
         GuiCommandQueue.getInstance().add(commandType, form, newToolbar);
      }

      /// <summary>
      ///   CREATE_TOOLBAR_ITEM, DELETE_TOOLBAR_ITEM
      /// </summary>
      /// <param name = "commandType"> </param>
      /// <param name = "toolbar">is the ToolBar to which we add a new item (placed in parentObject) </param>
      /// <param name = "menuEntry">is the menuEntry for which we create this toolitem </param>
      /// <param name = "index">is the index of the new object in the toolbar (placed in line) </param>
      public static void addAsync(CommandType commandType, Object toolbar, GuiMgForm form, GuiMenuEntry menuEntry,
                                  int index)
      {
         GuiCommandQueue.getInstance().add(commandType, toolbar, form, menuEntry, index);
      }

      /// <summary>
      ///   execute all pending commands, asynchronously
      /// </summary>
      public static void beginInvoke()
      {
         GuiCommandQueue.getInstance().beginInvoke();
      }

      /// <summary>
      ///   execute all pending commands, synchronously
      /// </summary>
      public static void invoke()
      {
         GuiCommandQueue.getInstance().invoke();
      }

#if !PocketPC
      // <summary> returns if passed form is active </summary>
      // <param name="guiMgForm"></param>
      // <returns></returns>
      public static bool isFormActive(GuiMgForm guiMgForm)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.isFormActive(guiMgForm);
      }

      /// <summary>(public)
      /// Sync call to set the marked text of rich edit control
      /// </summary>
      /// <param name="control"></param>
      /// <param name="line"></param>
      /// <param name="text"></param>
      public static void setMarkedTextOnRichEdit(GuiMgControl control, int line, string text)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.setMarkedTextOnRichEdit(control, line, text);
      }
#endif

      /// <summary>
      /// Activate the guiMgForm.
      /// </summary>
      /// <param name="guiMgForm"></param>
      public static void ActivateForm(GuiMgForm guiMgForm)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.ActivateForm(guiMgForm);
      }

      ///<summary>
      ///  Check whether the combobox is in dropped down state or not.
      ///</summary>
      ///<param name="control"></param>
      ///<param name="line"></param>
      ///<returns></returns>
      public static bool IsComboBoxInDroppedDownState(GuiMgControl control, int line)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         return guiInteractive.IsComboBoxInDroppedDownState(control, line);
      }

#if !PocketPC
      /// <summary>
      /// Show Context Menu.
      /// </summary>
      /// <param name="guiMgControl"></param>
      /// <param name="guiMgForm"></param>
      /// <param name="left"></param>
      /// <param name="top"></param>
      public static void ShowContextMenu(GuiMgControl guiMgControl, GuiMgForm guiMgForm, int left, int top, int line)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.onShowContextMenu(guiMgControl, guiMgForm, left, top, line);
      }

      /// <summary>
      /// Show Context Menu.
      /// </summary>
      /// <param name="guiMgControl"></param>
      /// <param name="guiMgForm"></param>
      /// <param name="left"></param>
      /// <param name="top"></param>
      public static void OpenFormDesigner(MgFormBase guiMgForm, bool adminMode)
      {
         GuiInteractive guiInteractive = new GuiInteractive();

         Dictionary<object, ControlDesignerInfo> dict = guiMgForm.BuildStudioValuesDictionaryForForm();

         guiInteractive.OnOpenFormDesigner(guiMgForm, dict, adminMode, guiMgForm.GetControlsPersistencyFileName());
      }
#endif

      /// <summary>
      /// Enable/Disable MenuItem
      /// </summary>
      /// <param name="mnuRef"></param>
      /// <param name="enable"></param>
      internal static void EnableMenuEntry(MenuReference mnuRef, bool enable)
      {
         GuiInteractive guiInteractive = new GuiInteractive();
         guiInteractive.EnableMenuEntry(mnuRef, enable);
      }

      /// <summary>
      /// Activates Print Preview form
      /// </summary>
      public static void ActivatePrintPreview()
      {
#if !PocketPC
         PrintPreview printPreview = new PrintPreview();
         printPreview.Activate();
#endif
      }

      #endregion // Commands that are queued and then executed, either synchronously or asynchronously
   }
}
