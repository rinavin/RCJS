using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management;
#if !PocketPC
using com.magicsoftware.unipaas.gui.Properties;
using util.com.magicsoftware.util;
#else
using com.magicsoftware.richclient.mobile.util;
using util.com.magicsoftware.util;
#endif

namespace com.magicsoftware.unipaas
{
   /// <summary>events raised by MgGui.dll and handled by MgxpaRIA.exe or MgxpaRuntime.exe.
   /// Important note: these events are NOT related the events handling of Magic (RC or RTE) - 
   ///   the events exposed from this class contain functionality that the current assembly (MgGui.dll) 
   ///   requires from either MgxpaRIA.exe or MgxpaRuntime.exe.
   /// This class contains only the definitions of all delegates and events.
   /// Class 'EventsProcessor' contains handlers for events that can be served locally (by MgGui.dll),
   ///   and registered external handlers (in MgxpaRIA.exe or MgxpaRuntime.exe) for events that can't be served locally.
   /// </summary>
   public static class Events
   {
      #region Raised by Gui.low

      /// <summary>
      /// Invokes DN control value changed event.
      /// </summary>
      /// <param name="mgControl"></param>
      internal static void OnDNControlValueChanged(GuiMgControl mgControl, int line)
      {
         Debug.Assert(Misc.IsGuiThread() && DNControlValueChangedEvent != null);
         DNControlValueChangedEvent(mgControl, line);
      }
      public delegate void DNControlValueChangedDelegate(GuiMgControl mgControl, int line);
      public static event DNControlValueChangedDelegate DNControlValueChangedEvent;

      /// <summary> invokes the focus event</summary>
      /// <param name="ctrl"></param>
      /// <param name="line"></param>
      /// <param name="isProduceClick"></param>
      public static void OnFocus(GuiMgControl ctrl, int line, bool isProduceClick, bool onMultiMark)
      {
         Debug.Assert(FocusEvent != null);
         FocusEvent(ctrl, line, isProduceClick, onMultiMark);
      }
      public delegate void FocusDelegate(GuiMgControl ctrl, int line, bool isProduceClick, bool onMultiMark);
      public static event FocusDelegate FocusEvent;

      /// <summary> invokes the MouseDown Event</summary>
      /// <param name="guiMgForm"></param>
      /// <param name="guiMgCtrl"></param>
      /// <param name="dotNetArgs"></param>
      /// <param name="leftClickWasPressed"></param>
      /// <param name="line"></param>
      /// <param name="onMultiMark">indicates that Multi Mark continues</param>
      public static void OnMouseDown(GuiMgForm guiMgForm, GuiMgControl guiMgCtrl, Object[] dotNetArgs,
                                       bool leftClickWasPressed, int line, bool onMultiMark, bool canProduceClick)
      {
         Debug.Assert(MouseDownEvent != null);
         MouseDownEvent(guiMgForm, guiMgCtrl, dotNetArgs, leftClickWasPressed, line, onMultiMark, canProduceClick);
      }
      internal delegate void MouseDownDelegate(GuiMgForm guiMgForm, GuiMgControl guiMgCtrl, Object[] dotNetArgs,
                                               bool leftClickWasPressed, int line, bool onMultiMark, bool canProduceClick);
      internal static event MouseDownDelegate MouseDownEvent;

      /// <summary> invokes the MouseOver Event</summary>
      /// <param name="ctrl"></param>
      internal static void OnMouseOver(GuiMgControl ctrl)
      {
         Debug.Assert(MouseOverEvent != null);
         MouseOverEvent(ctrl);
      }
      internal delegate void MouseOverDelegate(GuiMgControl ctrl);
      internal static event MouseOverDelegate MouseOverEvent;

      /// <summary> invokes Press Event</summary>
      /// <param name="guiMgForm"></param>
      /// <param name="guiMgCtrl"></param>
      /// <param name="line"></param>
      internal static void OnPress(GuiMgForm guiMgForm, GuiMgControl guiMgCtrl, int line)
      {
         if(PressEvent != null)
            PressEvent(guiMgForm, guiMgCtrl, line);
      }

      public delegate void PressDelegate(GuiMgForm guiMgForm, GuiMgControl guiMgCtrl, int line);
      public static event PressDelegate PressEvent;

      /// <summary> invokes the EditNode Event</summary>
      /// <param name="ctrl"></param>
      /// <param name="line"></param>
      internal static void OnEditNode(GuiMgControl ctrl, int line)
      {
         if (EditNodeEvent != null)
            EditNodeEvent(ctrl, line);
      }
      public delegate void EditNodeDelegate(GuiMgControl ctrl, int line);
      public static event EditNodeDelegate EditNodeEvent;

      /// <summary> invokes the EditNodeExit Event</summary>
      /// <param name="ctrl"></param>
      /// <param name="line"></param>
      internal static void OnEditNodeExit(GuiMgControl ctrl, int line)
      {
         if (EditNodeExitEvent != null)
            EditNodeExitEvent(ctrl, line);
      }
      public delegate void EditNodeExitDelegate(GuiMgControl ctrl, int line);
      public static event EditNodeExitDelegate EditNodeExitEvent;

      /// <summary> invokes the Collapse Event</summary>
      /// <param name="ctrl"></param>
      /// <param name="line"></param>
      internal static void OnCollapse(GuiMgControl ctrl, int line)
      {
         if (CollapseEvent != null)
            CollapseEvent(ctrl, line);
      }
      public delegate void CollapseDelegate(GuiMgControl ctrl, int line);
      public static event CollapseDelegate CollapseEvent;

      /// <summary> invokes the Expand Event</summary>
      /// <param name="ctrl"></param>
      /// <param name="line"></param>
      internal static void OnExpand(GuiMgControl ctrl, int line)
      {
         if (ExpandEvent != null)
            ExpandEvent(ctrl, line);
      }
      public delegate void ExpandDelegate(GuiMgControl ctrl, int line);
      public static event ExpandDelegate ExpandEvent;

      /// <summary> invokes the Expand Event</summary>
      /// <param name="ctrl"></param>
      /// <param name="oldLine"></param>
      /// <param name="newLine"></param>
      internal static bool OnTreeNodeSelectChange(GuiMgControl ctrl, int oldLine, int newLine)
      {
         bool selectChangedHandled = true;

         if (TreeNodeSelectChangeEvent != null)
            TreeNodeSelectChangeEvent(ctrl, oldLine, newLine);
         else
            selectChangedHandled = false;

         return selectChangedHandled;
      }

      public delegate void TreeNodeSelectChangeEventDelegate(GuiMgControl ctrl, int oldLine, int newLine);
      public static event TreeNodeSelectChangeEventDelegate TreeNodeSelectChangeEvent;

      /// <summary> Check if the given key should be handled by us  or
      /// by the default behavior of the tree</summary>
      /// '+' and '-' are handled by the tree for both RC and online.
      /// other keys will be checked for each env.
      /// <param name="keyCode"></param>
      internal static bool ShouldHandleTreeKeyDown(Keys keyCode)
      {
         bool handleKeyDown = true;

         if (keyCode == Keys.Add || keyCode == Keys.Subtract)
            handleKeyDown = false;
         else if (TreeHandleKeyDownEvent != null)
            handleKeyDown = TreeHandleKeyDownEvent(keyCode);

         return handleKeyDown;
      }
      public delegate bool TreeHandleKeyDownDelegate(Keys keyCode);
      public static event TreeHandleKeyDownDelegate TreeHandleKeyDownEvent;

      /// <summary> invokes the VisibilityChanged Event</summary>
      /// <param name="ctrl">control whose visibility is changed</param>
      internal static void OnNonParkableLastParkedCtrl(GuiMgControl ctrl)
      {
         if (NonParkableLastParkedCtrlEvent != null)
            NonParkableLastParkedCtrlEvent(ctrl);
      }
      public delegate void OnNonParkableLastParkedCtrlDelegate(GuiMgControl ctrl);
      public static OnNonParkableLastParkedCtrlDelegate NonParkableLastParkedCtrlEvent;

      /// <summary> invokes the TableReorder Event</summary>
      /// <param name="ctrl"></param>
      /// <param name="tabOrderList"></param>
      internal static void OnTableReorder(GuiMgControl ctrl, List<GuiMgControl> tabOrderList)
      {
         if (TableReorderEvent != null)
            TableReorderEvent(ctrl, tabOrderList);
      }
      public delegate void TableReorderDelegate(GuiMgControl ctrl, List<GuiMgControl> tabOrderList);
      public static event TableReorderDelegate TableReorderEvent;

      /// <summary> invokes the MouseOut Event</summary>
      /// <param name="ctrl"></param>
      internal static void OnMouseOut(GuiMgControl ctrl)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(MouseOutEvent != null);
         MouseOutEvent(ctrl);
      }
      internal delegate void MouseOutDelegate(GuiMgControl ctrl);
      internal static event MouseOutDelegate MouseOutEvent;

      /// <summary> invokes the MouseUp Event</summary>
      /// <param name="ctrl"></param>
      /// <param name="line"></param>
      internal static void OnMouseUp(GuiMgControl ctrl, int line)
      {
         Debug.Assert(Misc.IsGuiThread());
         if (MouseUpEvent != null)
            MouseUpEvent(ctrl, line);
      }
      public delegate void MouseUpDelegate(GuiMgControl ctrl, int line);
      public static event MouseUpDelegate MouseUpEvent;

      /// <summary> invokes the BrowserStatusTxtChange Event</summary>
      /// <param name="browserCtrl"></param>
      /// <param name="text"></param>
      internal static void OnBrowserStatusTxtChange(GuiMgControl browserCtrl, String text)
      {
         Debug.Assert(Misc.IsGuiThread());
         if (BrowserStatusTxtChangeEvent != null)
            BrowserStatusTxtChangeEvent(browserCtrl, text);
      }
      public delegate void BrowserStatusTxtChangeDelegate(GuiMgControl browserCtrl, String text);
      public static event BrowserStatusTxtChangeDelegate BrowserStatusTxtChangeEvent;

      /// <summary> invokes the BrowserExternal Event</summary>
      /// <param name="browserCtrl"></param>
      /// <param name="text"></param>
      internal static void OnBrowserExternalEvent(GuiMgControl browserCtrl, String text)
      {
         Debug.Assert(Misc.IsGuiThread());
         if (BrowserExternalEvent != null)
            BrowserExternalEvent(browserCtrl, text);
      }
      public delegate void BrowserExternalEventDelegate(GuiMgControl browserCtrl, String text);
      public static event BrowserExternalEventDelegate BrowserExternalEvent;

      /// <summary> invokes the DotNet Event</summary>
      /// <param name="sender"></param>
      /// <param name="mgControl"></param>
      /// <param name="eventName"></param>
      /// <param name="parameters"></param>
      internal static void OnDotNetEvent(Object sender, GuiMgControl mgControl, String eventName, object[] parameters)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(DotNetEvent != null);
         DotNetEvent(sender, mgControl, eventName, parameters);
      }
      public delegate void DotNetEventDelegate(Object sender, GuiMgControl mgControl, String eventName, object[] parameters);
      public static event DotNetEventDelegate DotNetEvent;

      /// <summary> invokes When Current active Row of DataView control Changes </summary>
      /// <param name="dataTable">dataTable associated withe DataView Control</param>
      /// <param name="rowIdx">changed row index</param>
      /// <param name="posIsn">Position</param>
      internal static void OnDVControlRowChangedEvent(Object dataTable, int rowIdx, int posIsn)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(DVControlRowChangedEvent != null);
         DVControlRowChangedEvent(dataTable, rowIdx, posIsn);
      }
      public delegate void DVControlRowChangedEventDelegate(Object dataTable, int rowIdx, int posIsn);
      public static event DVControlRowChangedEventDelegate DVControlRowChangedEvent;

      /// <summary> invokes When DataView Control's column value changes</summary>
      /// <param name="dataTable">dataTable associated with DataView Control</param>
      /// <param name="columnId">columnIndex </param>
      /// <param name="dnKey"> dnKey for modified value.</param>
      internal static void OnDVControlColumnValueChangedEvent(Object dataTable, int columnId, int dnKey)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(DVControlColumnValueChangedEvent != null);
         DVControlColumnValueChangedEvent(dataTable, columnId, dnKey);
      }
      public delegate void DVControlColumnValueChangedEventDelegate(Object dataTable, int columnId, int dnKey);
      public static event DVControlColumnValueChangedEventDelegate DVControlColumnValueChangedEvent;

      /// <summary> invokes the ComboDroppingDown Event</summary>
      /// <param name="ctrl"></param>
      /// <param name="line"></param>
      internal static void OnComboDroppingDown(GuiMgControl ctrl, int line)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(ComboDroppingDownEvent != null);
         ComboDroppingDownEvent(ctrl, line);
      }
      internal delegate void ComboDroppingDownDelegate(GuiMgControl ctrl, int line);
      internal static event ComboDroppingDownDelegate ComboDroppingDownEvent;

      /// <summary> invokes the Selection Event</summary>
      /// <param name="val"></param>
      /// <param name="ctrl"></param>
      /// <param name="line"></param>
      /// <param name="produceClick"></param>
      public static void OnSelection(String val, GuiMgControl ctrl, int line, bool produceClick)
      {
        // Debug.Assert(Misc.IsGuiThread());
       //  Debug.Assert(SelectionEvent != null);
         SelectionEvent(val, ctrl, line, produceClick);
      }
      public delegate void SelectionDelegate(String val, GuiMgControl ctrl, int line, bool produceClick);
      public static event SelectionDelegate SelectionEvent;

      /// <summary> invokes the DblClick Event</summary>
      /// <param name="ctrl"></param>
      /// <param name="line"></param>
      internal static void OnDblClick(GuiMgControl ctrl, int line)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(DblClickEvent != null);
         DblClickEvent(ctrl, line);
      }
      internal delegate void DblClickDelegate(GuiMgControl ctrl, int line);
      internal static event DblClickDelegate DblClickEvent;

      /// <summary> delegate for KeyDown Event</summary>
      /// <param name="form"></param>
      /// <param name="guiMgCtrl"></param>
      /// <param name="modifier"></param>
      /// <param name="keyCode"></param>
      /// <param name="start"></param>
      /// <param name="end"></param>
      /// <param name="text"></param>
      /// <param name="im"></param>
      /// <param name="isActChar"></param>
      /// <param name="suggestedValue"></param>
      /// <param name="comboIsDropDown"></param>
      /// <param name="handled">boolean variable event is handled or not. </param>
      /// <returns> true if event is handled else false. If true magic will handle else CLR will handle.</returns>
      internal delegate bool KeyDownHandler(GuiMgForm form, GuiMgControl guiMgCtrl, Modifiers modifier, int keyCode, int start, int end,
                                            String text, ImeParam im, bool isActChar, String suggestedValue, bool comboIsDropDown, bool handled);
      internal static event KeyDownHandler KeyDownEvent;

      /// <summary> invokes the KeyDown Event</summary>
      /// <param name="form"></param>
      /// <param name="guiMgCtrl"></param>
      /// <param name="modifier"></param>
      /// <param name="keyCode"></param>
      /// <param name="start"></param>
      /// <param name="end"></param>
      /// <param name="text"></param>
      /// <param name="im"></param>
      /// <param name="isActChar"></param>
      /// <param name="suggestedValue"></param>
      /// <param name="comboIsDropDown"></param>
      /// <param name="handled">boolean variable event is handled or not. </param>
      internal static void OnKeyDown(GuiMgForm form, GuiMgControl guiMgCtrl, Modifiers modifier, int keyCode, int start, int end,
                                     String text, ImeParam im, bool isActChar, String suggestedValue, bool comboIsDropDown, bool handled)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(KeyDownEvent != null);
         KeyDownEvent(form, guiMgCtrl, modifier, keyCode, start, end, text, im, isActChar,
                      suggestedValue, comboIsDropDown, handled);
      }

      /// <summary>
      /// delegate for the MultiMark hit event
      /// </summary>
      /// <param name="guiMgCtrl"> control </param>
      /// <param name="row"> table row </param>
      internal delegate void MultimakHitHandler(GuiMgControl guiMgCtrl, int row, Modifiers modifier);
      internal static event MultimakHitHandler MultimarkHitEvent;

      /// <summary>
      /// multi mark hit 
      /// </summary>
      /// <param name="ctrl"></param>
      /// <param name="row"></param>
      /// <param name="modifier"> keyboard modifier</param>
      internal static void OnMultiMarkHit(GuiMgControl ctrl, int row, Modifiers modifier)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(MultimarkHitEvent != null);
         MultimarkHitEvent(ctrl, row, modifier);

      }

      /// <summary> invokes the KeyDown Event</summary>
      /// <param name="form"></param>
      /// <param name="ctrl"></param>
      /// <param name="modifier"></param>
      /// <param name="keyCode"></param>
      /// <param name="start"></param>
      /// <param name="end"></param>
      /// <param name="text"></param>
      /// <param name="isActChar"></param>
      /// <param name="suggestedValue"></param>
      /// <param name="handled">boolean variable event is handled or not. </param>
      /// <returns> true only if we have handled the KeyDown event (otherwise the CLR should handle). If true magic will handle else CLR will handle.</returns>
      internal static bool OnKeyDown(GuiMgForm form, GuiMgControl ctrl, Modifiers modifier, int keyCode, int start, int end,
                                     String text, bool isActChar, String suggestedValue, bool handled)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(KeyDownEvent != null);
         return KeyDownEvent(form, ctrl, modifier, keyCode, start, end, text, null, isActChar, suggestedValue, false, handled);
      }

      /// <summary> invokes the KeyDown Event</summary>
      /// <param name="form"></param>
      /// <param name="ctrl"></param>
      /// <param name="modifier"></param>
      /// <param name="keyCode"></param>
      /// <param name="suggestedValue"></param>
      /// <param name="comboIsDropDown"></param>
      /// <param name="handled">boolean variable event is handled or not. </param>
      /// <returns> true if event is handled else false. If true magic will handle else CLR will handle.</returns>
      internal static bool OnKeyDown(GuiMgForm form, GuiMgControl ctrl, Modifiers modifier, int keyCode, String suggestedValue, bool comboIsDropDown, bool handled)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(KeyDownEvent != null);
         return KeyDownEvent(form, ctrl, modifier, keyCode, 0, 0, null, null, false, suggestedValue, comboIsDropDown, handled);
      }

      /// <summary> invokes the Wide Event</summary>
      /// <param name="ctrl"></param>
      internal static void OnWide(GuiMgControl ctrl)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(WideEvent != null);
         WideEvent(ctrl);
      }
      public delegate void WideDelegate(GuiMgControl ctrl);
      public static event WideDelegate WideEvent;

      /// <summary> invokes the Close Event</summary>
      /// <param name="form"></param>
      /// <returns>true if event is CLR handled(closing of the form will be the responsibility of the CLR and no commands will be put 
      /// in to close the form) else false.</returns>
      public static bool OnFormClose(GuiMgForm form)
      {
        // Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(CloseFormEvent != null);
         return CloseFormEvent(form);
      }
      internal delegate bool CloseFormDelegate(GuiMgForm form);
      internal static event CloseFormDelegate CloseFormEvent;

      /// <summary> invokes the Dispose Event</summary>
      /// <param name="form"></param>
      internal static void OnDispose(GuiMgForm form)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(DisposeEvent != null);
         DisposeEvent(form);
      }
      public delegate void DisposeDelegate(GuiMgForm form);
      public static event DisposeDelegate DisposeEvent;

      /// <summary> invokes the Timer Event</summary>
      /// <param name="mgTimer"></param>
      internal static void OnTimer(MgTimer mgTimer)
      {
         // Idle timer can be hit before TimerEvent is hooked during RtDllInit (...)
         Debug.Assert(Misc.IsTimerThread());
         Debug.Assert(TimerEvent != null);
         TimerEvent(mgTimer);
      }
      public delegate void TimerDelegate(MgTimer mgTimer);
      public static event TimerDelegate TimerEvent;

      /// <summary> invokes the WindowResize Event</summary>
      /// <param name="form"></param>
      internal static void OnWindowResize(GuiMgForm form)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(WindowResizeEvent != null);
         WindowResizeEvent(form);
      }
      internal delegate void WindowResizeDelegate(GuiMgForm form);
      internal static event WindowResizeDelegate WindowResizeEvent;

      /// <summary> invokes the WindowMove Event</summary>
      /// <param name="form"></param>
      internal static void OnWindowMove(GuiMgForm form)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(WindowMoveEvent != null);
         WindowMoveEvent(form);
      }
      internal delegate void WindowMoveDelegate(GuiMgForm form);
      internal static event WindowMoveDelegate WindowMoveEvent;

      /// <summary> invokes the Resize Event</summary>
      /// <param name="ctrl"></param>
      /// <param name="newRowsInPage"></param>
      internal static void OnTableResize(GuiMgControl ctrl, int newRowsInPage)
      {
         if (TableResizeEvent != null)
            TableResizeEvent(ctrl, newRowsInPage);
      }
      public delegate void TableResizeDelegate(GuiMgControl ctrl, int newRowsInPage);
      public static event TableResizeDelegate TableResizeEvent;

      /// <summary> invokes the GetRowsData Event</summary>
      /// <param name="ctrl"></param>
      /// <param name="desiredTopIndex"></param>
      /// <param name="sendAll"></param>
      /// <param name="lastFocusedVal"></param>
      internal static void OnGetRowsData(GuiMgControl ctrl, int desiredTopIndex, bool sendAll, LastFocusedVal lastFocusedVal)
      {
         // TODO: MerlinRT Tabcontrol. After re-size handling for OL, add this assert.
         // Debug.Assert(GetRowsDataEvent != null); 
         if (GetRowsDataEvent != null)
            GetRowsDataEvent(ctrl, desiredTopIndex, sendAll, lastFocusedVal);
      }
      public delegate void GetRowsDataDelegate(GuiMgControl ctrl, int desiredTopIndex, bool sendAll, LastFocusedVal lastFocusedVal);
      public static event GetRowsDataDelegate GetRowsDataEvent;

      /// <summary> invokes the EnableCutCopy Event</summary>
      /// <param name="ctrl"></param>
      /// <param name="enable"></param>
      internal static void OnEnableCutCopy(GuiMgControl ctrl, bool enable)
      {
         Debug.Assert(EnableCutCopyEvent != null);
         EnableCutCopyEvent(ctrl, enable);
      }
      public delegate void EnableCutCopyDelegate(GuiMgControl ctrl, bool enable);
      public static event EnableCutCopyDelegate EnableCutCopyEvent;

      /// <summary> invokes the EnablePaste Event</summary>
      /// <param name="ctrl"></param>
      /// <param name="enable"></param>
      internal static void OnEnablePaste(GuiMgControl ctrl, bool enable)
      {
         if (EnablePasteEvent != null)
            EnablePasteEvent(ctrl, enable);
      }
      public delegate void EnablePasteDelegate(GuiMgControl ctrl, bool enable);
      public static event EnablePasteDelegate EnablePasteEvent;

      /// <summary> invokes the BeginDrag Event</summary>
      /// <param name="control"></param>
      /// <param name="line"></param>
      internal static void OnBeginDrag(GuiMgControl control, int line)
      {
         Debug.Assert(BeginDragEvent != null);
         BeginDragEvent(control, line);
      }
      public delegate void BeginDragDelegate(GuiMgControl control, int line);
      public static event BeginDragDelegate BeginDragEvent;

      /// <summary> invokes the BeginDrop Event</summary>
      /// <param name="guiMgForm"></param>
      /// <param name="control"></param>
      /// <param name="line"></param>
      internal static void OnBeginDrop(GuiMgForm guiMgForm, GuiMgControl control, int line)
      {
         Debug.Assert(BeginDropEvent != null);
         BeginDropEvent(guiMgForm, control, line);
      }
      public delegate void BeginDropDelegate(GuiMgForm guiMgForm, GuiMgControl control, int line);
      public static event BeginDropDelegate BeginDropEvent;

      /// <summary> invokes the ColumnClick Event</summary>
      /// <param name="columnCtrl"></param>
      /// <param name="direction"></param>
      /// <param name="columnHeader"></param>
      internal static void OnColumnClick(GuiMgControl columnCtrl, int direction, String columnHeader)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(ColumnClickEvent != null);
         ColumnClickEvent(columnCtrl, direction, columnHeader);
      }
      public delegate void ColumnClickDelegate(GuiMgControl columnCtrl, int direction, String columnHeader);
      public static event ColumnClickDelegate ColumnClickEvent;

      /// <summary> invokes the ColumnFilter Event</summary>
      /// <param name="columnCtrl"></param>
      /// <param name="direction"></param>
      /// <param name="columnHeader"></param>
      internal static void OnColumnFilter(GuiMgControl columnCtrl, String columnHeader, int x, int y, int width, int height)
      {
         Debug.Assert(Misc.IsGuiThread());
         Debug.Assert(ColumnFilterEvent != null);
         ColumnFilterEvent(columnCtrl, columnHeader, x, y, width, height);
      }
      public delegate void ColumnFilterDelegate(GuiMgControl columnCtrl, String columnHeader, int x, int y, int width, int height);
      public static event ColumnFilterDelegate ColumnFilterEvent;

      /// <summary>set the allowFormsLock flag</summary>
      /// <param name="val"></param>
      internal static void AllowFormsLock(bool val)
      {
         if (AllowFormsLockSetEvent != null)
            AllowFormsLockSetEvent(val);
      }
      public delegate void AllowFormsLockSetDelegate(bool val);
      public static event AllowFormsLockSetDelegate AllowFormsLockSetEvent;

      /// <summary>get the allowFormsLock flag</summary>
      internal static bool AllowFormsLock()
      {
         return (AllowFormsLockGetEvent != null && AllowFormsLockGetEvent());
      }
      public delegate bool AllowFormsLockGetDelegate();
      public static event AllowFormsLockGetDelegate AllowFormsLockGetEvent;

      /// <summary>isFormLockAllowed</summary>
      /// <returns></returns>
      internal static bool IsFormLockAllowed()
      {
         return (IsFormLockAllowedEvent != null && IsFormLockAllowedEvent());
      }
      public delegate bool IsFormLockAllowedDelegate();
      public static event IsFormLockAllowedDelegate IsFormLockAllowedEvent;

      /// <summary>IsContextMenuAllowed</summary>
      /// <returns></returns>
      internal static bool IsContextMenuAllowed(GuiMgControl guiMgControl)
      {
         return (IsContextMenuAllowedEvent != null && IsContextMenuAllowedEvent(guiMgControl));
      }
      public delegate bool IsContextMenuAllowedDelegate(GuiMgControl guiMgControl);
      public static event IsContextMenuAllowedDelegate IsContextMenuAllowedEvent;

      /// <summary>invokes refreshTables delegate</summary>
      internal static void RefreshTables()
      {
         if (RefreshTablesEvent != null)
            RefreshTablesEvent();
      }
      public delegate void RefreshTablesDelegate();
      public static event RefreshTablesDelegate RefreshTablesEvent;

      /// <summary>invokes the GetContent delegate</summary>
      /// <param name="requestedURL">URL to be accessed.</param>
      /// <param name="decryptResponse">if true, fresh responses from the server will be decrypted using the 'encryptionKey' passed to 'HttpManager.SetProperties'.</param>
      /// <returns>response (from the server).</returns>
      internal static byte[] GetContent(String requestedURL, bool decryptResponse)
      {
         return (GetContentEvent != null ?
                     GetContentEvent(requestedURL, decryptResponse) :
                     null);
      }
      public delegate byte[] GetContentDelegate(String requestedURL, bool decryptResponse);
      public static event GetContentDelegate GetContentEvent;

      /// <summary>MenuSelection</summary>
      /// <param name="menuEntry"></param>
      /// <param name="activeForm"></param>
      /// <param name="activatedFromMDIFrame"></param>
      internal static void OnMenuSelection(GuiMenuEntry menuEntry, GuiMgForm activeForm, bool activatedFromMDIFrame)
      {
         Debug.Assert(MenuSelectionEvent != null);
         MenuSelectionEvent(menuEntry, activeForm, activatedFromMDIFrame);
      }
      public delegate void MenuSelectionDelegate(GuiMenuEntry menuEntry, GuiMgForm activeForm, bool activatedFromMDIFrame);
      public static event MenuSelectionDelegate MenuSelectionEvent;

      /// <summary>HelpInVokedOnMenu</summary>
      /// <param name="menuEntry"></param>
      /// <param name="activeForm"></param>
      internal static void OnHelpInVokedOnMenu(GuiMenuEntry menuEntry, GuiMgForm activeForm)
      {
         if (HelpInVokedOnMenuEvent != null)
            HelpInVokedOnMenuEvent(menuEntry, activeForm);
      }
      public delegate void HelpInVokedOnMenuDelegate(GuiMenuEntry menuEntry, GuiMgForm activeForm);
      public static event HelpInVokedOnMenuDelegate HelpInVokedOnMenuEvent;

      /// <summary>OnHelpClose</summary>
      /// <param name="activeForm"></param>
      internal static void OnCloseHelp(GuiMgForm activeForm)
      {
         if (HelpCloseEvent != null)
            HelpCloseEvent(activeForm);
      }
      public delegate void HelpCloseDelegate( GuiMgForm activeForm);
      public static event HelpCloseDelegate HelpCloseEvent;


      /// <summary>
      ///   This event is invoked on Program type of menu selection. This event is responsible to 
      ///   translate the selected program menu into the matching operation i.e. program execution here.
      /// </summary>
      /// <param name="contextID">active/target context</param>
      /// <param name="menuEntryProgram">the selected menu \ bar menuEntryProgram object</param>
      /// <param name="activeForm"></param>
      /// <param name="activatedFromMdiFrame"></param>
      /// <returns></returns>
      public static void OnMenuProgramSelection(Int64 contextID, MenuEntryProgram menuEntryProgram, GuiMgForm activeForm, bool activatedFromMdiFrame)
      {
         Debug.Assert(MenuProgramSelectionEvent != null);
         MenuProgramSelectionEvent(contextID, menuEntryProgram, activeForm, activatedFromMdiFrame);
      }
      public delegate void MenuProgramSelectionDelegate(Int64 contextID, MenuEntryProgram menuEntryProgram, GuiMgForm activeForm, bool activatedFromMdiFrame);
      public static event MenuProgramSelectionDelegate MenuProgramSelectionEvent;

      /// <summary>
      ///   This event is invoked on event type of menu selection. This event is responsible to 
      ///   translate the selected event menu into the matching operation i.e. event execution.
      /// </summary>
      /// <param name="contextID">active/target context</param>
      /// <param name="menuEntryEvent">the selected menu \ bar menuEntryEvent object</param>
      /// <param name="activeForm"></param>
      /// <param name="ctlIdx">the index of the ctl which the menu is attached to in toolkit</param>
      internal static void OnMenuEventSelection(Int64 contextID, MenuEntryEvent menuEntryEvent, GuiMgForm activeForm, int ctlIdx)
      {
         Debug.Assert(MenuEventSelectionEvent != null);
         MenuEventSelectionEvent(contextID, menuEntryEvent, activeForm, ctlIdx);
      }
      public delegate void MenuEventSelectionDelegate(Int64 contextID, MenuEntryEvent menuEntryEvent, GuiMgForm activeForm, int ctlIdx);
      public static event MenuEventSelectionDelegate MenuEventSelectionEvent;

      /// <summary>
      ///   This event is invoked on OS type of menu selection. This event is responsible to 
      ///   translate the selected OS command menu into the matching operation i.e. osCommand execution.
      /// </summary>
      /// <param name="contextID"></param>
      /// <param name="osCommandMenuEntry"></param>
      /// <param name="activeForm"></param>
      /// <returns></returns>
      internal static void OnMenuOSCommandSelection(Int64 contextID, MenuEntryOSCommand osCommandMenuEntry, GuiMgForm activeForm)
      {
         Debug.Assert(MenuOSCommandSelectionEvent != null);
         MenuOSCommandSelectionEvent(contextID, osCommandMenuEntry, activeForm);
      }
      public delegate void MenuOSCommandSelectionDelegate(Int64 contextID, MenuEntryOSCommand osCommandMenuEntry, GuiMgForm activeForm);
      public static event MenuOSCommandSelectionDelegate MenuOSCommandSelectionEvent;

      /// <summary>
      /// This event is invoked for Window Menu selection.
      /// </summary>
      /// <param name="menuEntry"></param>
      internal static void OnMenuWindowSelection(MenuEntryWindowMenu menuEntry)
      {
         Debug.Assert(MenuWindowSelectionEvent != null);
         MenuWindowSelectionEvent(menuEntry);
      }
      public delegate void MenuWindowSelectionDelegate(MenuEntryWindowMenu menuEntry);
      public static event MenuWindowSelectionDelegate MenuWindowSelectionEvent;


      /// <summary>RefreshMenuActions </summary>
      /// <param name="guiMgMenu"></param>
      /// <param name="guiMgForm"></param>
      internal static void OnRefreshMenuActions(GuiMgMenu guiMgMenu, GuiMgForm guiMgForm)
      {
         Debug.Assert(RefreshMenuActionsEvent != null);
         RefreshMenuActionsEvent(guiMgMenu, guiMgForm);
      }
      internal delegate void RefreshMenuActionsDelegate(GuiMgMenu guiMgMenu, GuiMgForm guiMgForm);
      internal static event RefreshMenuActionsDelegate RefreshMenuActionsEvent;


      /// <summary>
      /// returns TRUE, if Batch task is running in MAIN context.
      /// </summary>
      internal static bool IsBatchRunningInMainContext()
      {
         bool retValue = false;
         if (IsBatchRunningInMainContextEvent != null)
            retValue = IsBatchRunningInMainContextEvent();
         return retValue;
      }
      public delegate bool IsBatchRunningInMainContextDelegate();
      public static IsBatchRunningInMainContextDelegate IsBatchRunningInMainContextEvent;


      /// <summary> GetContextMenu</summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      internal static GuiMgMenu OnGetContextMenu(Object obj)
      {
         Debug.Assert(GetContextMenuEvent != null && (obj is GuiMgForm || obj is GuiMgControl));
         return GetContextMenuEvent(obj);
      }
      internal delegate GuiMgMenu GetContextMenuDelegate(Object obj);
      internal static event GetContextMenuDelegate GetContextMenuEvent;

      /// <summary>onMenuPrompt </summary>
      /// <param name="guiMgForm"></param>
      /// <param name="guiMenuEntry"></param>
      internal static void OnMenuPrompt(GuiMgForm guiMgForm, GuiMenuEntry guiMenuEntry)
      {
         Debug.Assert(OnMenuPromptEvent != null);
         OnMenuPromptEvent(guiMgForm, guiMenuEntry);
      }
      internal delegate void OnMenuPromptDelegate(GuiMgForm guiMgForm, GuiMenuEntry guiMenuEntry);
      internal static event OnMenuPromptDelegate OnMenuPromptEvent;

      /// <summary>BeforeContextMenu</summary>
      /// <param name="guiMgCtrl"></param>
      /// <param name="line"></param>
      /// <returns></returns>
      internal static bool OnBeforeContextMenu(GuiMgControl guiMgCtrl, int line, bool onMultiMark)
      {
         bool focusChanged = false;

         if (BeforeContextMenuEvent != null)
            focusChanged = BeforeContextMenuEvent(guiMgCtrl, line, onMultiMark);

         return focusChanged;
      }
      public delegate bool BeforeContextMenuDelegate(GuiMgControl guiMgCtrl, int line, bool onMultiMark);
      public static event BeforeContextMenuDelegate BeforeContextMenuEvent;

      /// <summary>BeforeContextMenu</summary>
      /// <param name="guiMgForm"></param>
      /// <param name="guiMgMenu"></param>
      /// <returns></returns>
      internal static void OnContextMenuClose(GuiMgForm guiMgForm, GuiMgMenu guiMgMenu)
      {
         if (ContextMenuCloseEvent != null)
            ContextMenuCloseEvent(guiMgForm, guiMgMenu);

      }
      public delegate void ContextMenuCloseDelegate(GuiMgForm guiMgForm, GuiMgMenu guiMgMenu);
      public static event ContextMenuCloseDelegate ContextMenuCloseEvent;

      /// <summary>OnOpenContextMenu</summary>
      /// <param name="guiMgCtrl"></param>
      /// <param name="guiMgForm"></param>
      /// <param name="left"></param>
      /// <param name="top"></param>
      /// <param name="line"></param>
      internal static void OnOpenContextMenu(GuiMgControl guiMgCtrl, GuiMgForm guiMgForm, int left, int top, int line)
      {

         if (OnOpenContextMenuEvent != null)
            OnOpenContextMenuEvent(guiMgCtrl, guiMgForm, left, top, line);

      }
      public delegate void OnOpenContextMenuDelegate(GuiMgControl guiMgCtrl, GuiMgForm guiMgForm,  int left, int top, int line);
      public static event OnOpenContextMenuDelegate OnOpenContextMenuEvent;

      /// <summary>MLS translation</summary>
      /// <param name="fromString">source string</param>
      /// <returns>translated string</returns>
      internal static String Translate(String fromString)
      {
         Debug.Assert(TranslateEvent != null);
         return (fromString != null && fromString.Length > 0
                   ? TranslateEvent(fromString)
                   : fromString);
      }
      public delegate String TranslateDelegate(String fromString);
      public static TranslateDelegate TranslateEvent;

      /// <summary>
      /// Creates Print Preview Form
      /// </summary>
      /// <param name="contextId">context id</param>
      /// <param name="ioPtr">pointer to current IORT object</param>
      /// <param name="copies">number of copies</param>
      /// <param name="enablePDlg">indicates whether to enable Print dialog</param>
      /// <param name="hWnd">Handle of Print Preview Form</param>
      internal static void OnPrintPreviewStart(Int64 contextID, IntPtr ioPtr, int copies, bool enablePDlg, IntPtr hWnd)
      {
         if (PrintPreviewStartEvent != null)
            PrintPreviewStartEvent(contextID, ioPtr, copies, enablePDlg, hWnd);
      }
      public delegate void PrintPreviewStartDelegate(Int64 contextID, IntPtr ioPtr, int copies, bool enablePDlg, IntPtr hWnd);
      public static PrintPreviewStartDelegate PrintPreviewStartEvent;

      /// <summary>
      /// Set cursor to Print Preview form
      /// </summary>
      /// <param name="prnPrevData">print preview data</param>
      internal static void OnPrintPreviewSetCursor(IntPtr printPreviewData)
      {
         if (PrintPreviewSetCursorEvent != null)
            PrintPreviewSetCursorEvent(printPreviewData);
      }
      public delegate void PrintPreviewSetCursorDelegate(IntPtr printPreviewData);
      public static PrintPreviewSetCursorDelegate PrintPreviewSetCursorEvent;

      ///<summary>
      /// Handles Invoke UDP operation from GUI thread
      ///</summary>
      ///<param name="contextId">Context id</param>
      internal static int InvokeUDP(double contextId)
      {
         int ret = 0;

         if (InvokeUDPEvent != null)
            ret = InvokeUDPEvent(contextId);

         return ret;
      }
      public delegate int InvokeUDPDelegate(double contextId);
      public static InvokeUDPDelegate InvokeUDPEvent;

      /// <summary>
      /// Update Print Preview
      /// </summary>
      /// <param name="prnPrevData">print preview data</param>
      internal static void OnPrintPreviewUpdate(IntPtr prnPrevData)
      {
         if (PrintPreviewUpdateEvent != null)
            PrintPreviewUpdateEvent(prnPrevData);
      }
      public delegate void PrintPreviewUpdateDelegate(IntPtr prnPrevData);
      public static PrintPreviewUpdateDelegate PrintPreviewUpdateEvent;

      /// <summary>
      /// Create Rich Edit
      /// </summary>
      /// <param name="contextId"></param>
      /// <param name="ctrlPtr"></param>
      /// <param name="prmPtr"></param>
      /// <param name="style"></param>
      /// <param name="dwExStyle"></param>
      internal static void OnCreateRichWindow(Int64 contextID, IntPtr ctrlPtr, IntPtr prmPtr, uint style, uint dwExStyle)
      {
         if (CreateRichWindowEvent != null)
            CreateRichWindowEvent(contextID, ctrlPtr, prmPtr, style, dwExStyle);
      }
      public delegate void CreateRichWindowDelegate(Int64 contextID, IntPtr ctrlPtr, IntPtr prmPtr, uint style, uint dwExStyle);
      public static CreateRichWindowDelegate CreateRichWindowEvent;

      ///<summary>
      ///  Creates a window
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
      internal static IntPtr OnCreateGuiWindow(uint exStyle, String className, String windowName, uint style, int x, int y, int width, int height,
                                               IntPtr hwndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lParam)
      {
         IntPtr retValue = IntPtr.Zero;
         if (CreateGuiWindowEvent != null)
            retValue = CreateGuiWindowEvent(exStyle, className, windowName, style, x, y, width, height, hwndParent, hMenu, hInstance, lParam);
         return retValue;
      }
      public delegate IntPtr CreateGuiWindowDelegate(uint exStyle, String className, String windowName, uint style, int x, int y, int width, int height,
                                                     IntPtr hwndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lParam);
      public static CreateGuiWindowDelegate CreateGuiWindowEvent;

      /// <summary>
      /// Destroys a window
      /// </summary>
      /// <param name="hWndPtr">handle of window</param>
      internal static void OnDestroyGuiWindow(IntPtr hWndPtr)
      {
         if (DestroyGuiWindowEvent != null)
            DestroyGuiWindowEvent(hWndPtr);
      }
      public delegate void DestroyGuiWindowDelegate(IntPtr hWndPtr);
      public static DestroyGuiWindowDelegate DestroyGuiWindowEvent;

      /// <summary>
      /// OnClose Print Preview
      /// </summary>
      /// <param name="hWndPtr">print preview data</param>
      internal static void OnPrintPreviewClose(IntPtr printPreviewDataPtr)
      {
         if (PrintPreviewCloseEvent != null)
            PrintPreviewCloseEvent(printPreviewDataPtr);
      }
      public delegate void PrinPreviewCloseDelegate(IntPtr hWndPtr);
      public static PrinPreviewCloseDelegate PrintPreviewCloseEvent;

      /// <summary>
      /// Show Print Dialog
      /// </summary>
      /// <param name="gpd"></param>
      internal static int OnShowPrintDialog(IntPtr gpd)
      {
         int retValue = 0;
         if (ShowPrintDialogEvent != null)
            retValue = ShowPrintDialogEvent(gpd);
         return retValue;
      }
      public delegate int ShowPrintDialogDelegate(IntPtr gpd);
      public static ShowPrintDialogDelegate ShowPrintDialogEvent;

      /// <summary> closeTasksOnParentActivate </summary>
      /// <returns></returns>
      internal static bool CloseTasksOnParentActivate()
      {
         return (CloseTasksOnParentActivateEvent != null && CloseTasksOnParentActivateEvent());
      }
      public delegate bool CloseTasksOnParentActivateDelegate();
      public static event CloseTasksOnParentActivateDelegate CloseTasksOnParentActivateEvent;

      /// <summary> displaySessionStatistics </summary>
      internal static void DisplaySessionStatistics()
      {
         if (DisplaySessionStatisticsEvent != null)
            DisplaySessionStatisticsEvent();
      }
      public delegate void DisplaySessionStatisticsDelegate();
      public static event DisplaySessionStatisticsDelegate DisplaySessionStatisticsEvent;

      /// <summary> InIncrementalLocate 
      /// 
      /// </summary>
      /// <returns></returns>
      internal static bool InIncrementalLocate()
      {
         return (InIncrementalLocateEvent != null && InIncrementalLocateEvent());
      }
      public delegate bool InIncrementalLocateDelegate();
      public static event InIncrementalLocateDelegate InIncrementalLocateEvent;

      /// <summary>
      /// returns true for non interactive task for all controls except a button control
      /// </summary>
      /// <param name="ctrl">clicked control</param>
      /// <returns></returns>
      internal static bool ShouldBlockMouseEvents(GuiMgControl ctrl)
      {
         return (ShouldBlockMouseEventsEvent != null && ShouldBlockMouseEventsEvent(ctrl));
      }
      public delegate bool ShouldBlockMouseEventsDelegate(GuiMgControl ctrl);
      public static event ShouldBlockMouseEventsDelegate ShouldBlockMouseEventsEvent;

      /// <summary>peekEndOfWork</summary>
      /// <returns></returns>
      internal static bool PeekEndOfWork()
      {
         return (PeekEndOfWorkEvent != null && PeekEndOfWorkEvent());
      }
      public delegate bool PeekEndOfWorkDelegate();
      public static event PeekEndOfWorkDelegate PeekEndOfWorkEvent;

      /// <summary>invokes the GetEventTime delegate</summary>
      /// <returns></returns>
      internal static long GetEventTime()
      {
         return (GetEventTimeEvent != null ?
                     GetEventTimeEvent() :
                     0);
      }
      public delegate long GetEventTimeDelegate();
      public static event GetEventTimeDelegate GetEventTimeEvent;

      /// <summary>invokes the getResourceObject delegate</summary>
      /// <param name="resourceName"></param>
      /// <returns></returns>
      internal static Object GetResourceObject(String resourceName)
      {
         Debug.Assert(GetResourceObjectEvent != null);
         Object resource = GetResourceObjectEvent(resourceName);

#if !PocketPC
         if (resource == null)
            resource = Resources.ResourceManager.GetObject(resourceName);
#endif

         return resource;
      }
      public delegate Object GetResourceObjectDelegate(String resourceName);
      public static event GetResourceObjectDelegate GetResourceObjectEvent;

      /// <summary>
      /// returns true iff the url is a request to retrieve a cached resource from the runtime-engine.
      /// </summary>
      /// <param name="urlString"></param>
      /// <returns>true iff the url starts with: /requester?, e.g. /mgrequester19?</returns>
      internal static bool IsRelativeRequestURL(string urlString)
      {
         return (IsRelativeCacheRequestURLEvent != null &&
                 IsRelativeCacheRequestURLEvent(urlString));
      }
      public delegate bool IsRelativeCacheRequestURLDelegate(string urlString);
      public static event IsRelativeCacheRequestURLDelegate IsRelativeCacheRequestURLEvent;

      /// <summary>
      /// invokes the ScrollTable Event
      /// </summary>
      /// <param name="guiMgObject"></param>
      /// <param name="line"></param>
      /// <param name="rowsToScroll"></param>
      /// <param name="isPageScroll">next/prev page?</param>
      /// <param name="isTableScroll">Begin/End Table?</param>
      /// <param name="isRaisedByMouseWheel">indicates whether event is raised by mousewheel or not.</param>
      internal static bool OnScrollTable(Object guiMgObject, int line, int rowsToScroll, bool isPageScroll, bool isTableScroll, bool isRaisedByMouseWheel)
      {
         bool handled = false;

         if (ScrollTableEvent != null)
            handled = ScrollTableEvent(guiMgObject, line, rowsToScroll, isPageScroll, isTableScroll, isRaisedByMouseWheel);

         return handled;
      }
      public delegate bool ScrollTableDelegate(Object guiMgObject, int line, int rowsToScroll, bool isPageScroll, bool isTableScroll, bool isRaisedByMouseWheel);
      public static event ScrollTableDelegate ScrollTableEvent;

      /// <summary>
      /// Gets the user defined Drop formats
      /// </summary>
      /// <param name="ctrl"></param>
      /// <returns></returns>
      internal static string GetDropUserFormats()
      {
         Debug.Assert(GetDropUserFormatsEvent != null);
         return GetDropUserFormatsEvent();
      }
      public delegate String GetDropUserFormatsDelegate();
      public static event GetDropUserFormatsDelegate GetDropUserFormatsEvent;

      /// <summary>
      /// Get the context id from GuiMgForm
      /// </summary>
      /// <param name="guiMgForm"></param>
      /// <returns></returns>
      internal static Int64 GetContextID(GuiMgForm guiMgForm)
      {
         Int64 contextID = -1;

         Debug.Assert(guiMgForm != null);
         if (GetContextIDEvent != null)
            contextID = GetContextIDEvent(guiMgForm);

         return contextID;
      }
      public delegate Int64 GetContextIDDelegate(GuiMgForm guiMgForm);
      public static event GetContextIDDelegate GetContextIDEvent;

      /// <summary>
      /// SetModal event
      /// </summary>
      /// <param name="currForm"></param>
      /// <param name="on"></param>
      internal static void SetModal(MgFormBase mgForm, bool on)
      {
         if (SetModalEvent != null)
            SetModalEvent(mgForm, on);
      }
      public delegate void SetModalDelegate(MgFormBase mgForm, bool on);
      public static SetModalDelegate SetModalEvent;

      /// <summary> </summary>
      /// <param name="guiMgForm"></param>
      /// <returns></returns>
      internal static void OnShowForm(GuiMgForm guiMgForm)
      {
         if (ShowFormEvent != null)
            ShowFormEvent(guiMgForm);
      }
      public delegate void ShowFormDelegate(GuiMgForm guiMgForm);
      public static event ShowFormDelegate ShowFormEvent;

      /// <summary>
      /// invokes OnFormActivateEvent
      /// </summary>
      internal static void OnFormActivate(GuiMgForm guiMgForm)
      {
         if (OnFormActivateEvent != null)
            OnFormActivateEvent(guiMgForm);
      }
      public delegate void OnFormActivateDelegate(GuiMgForm guiMgForm);
      public static event OnFormActivateDelegate OnFormActivateEvent;

      /// <summary>
      /// invokes OnNCActivate
      /// </summary>
      internal static void OnNCActivate(GuiMgForm guiMgForm)
      {
         if (OnNCActivateEvent != null)
            OnNCActivateEvent(guiMgForm);
      }
      public static event OnFormActivateDelegate OnNCActivateEvent;

      /// <summary>
      /// invokes InitWindowListMenuItemsEvent
      /// </summary>
      /// <param name="guiMgMenu"></param>
      /// <param name="menuStyle"></param>
      internal static void InitWindowListMenuItems(GuiMgForm guiMgForm, Object menuObj, MenuStyle menuStyle)
      {
         if (InitWindowListMenuItemsEvent != null)
            InitWindowListMenuItemsEvent(guiMgForm, menuObj, menuStyle);
      }
      public delegate void InitWindowListMenuItemsDelegate(GuiMgForm guiMgForm, Object menuObj, MenuStyle menuStyle);
      public static event InitWindowListMenuItemsDelegate InitWindowListMenuItemsEvent;

      /// <summary>
      /// Handle WM_KEYUP - (required only for ACT_NEXT_RT_WINDOW, ACT_PREV_RT_WINDOW for sorting windowlist
      /// when the key associated with the action is released)
      /// </summary>
      /// <param name="guiMgForm"></param>
      /// <param name="keyCode">keycode of a key which is just released</param>
      internal static void HandleKeyUpMessage(GuiMgForm guiMgForm, int keyCode)
      {
         if (HandleKeyUpMessageEvent != null)
            HandleKeyUpMessageEvent(guiMgForm, keyCode);
      }
      public delegate void HandleKeyUpMessageDelegate(GuiMgForm guiMgForm, int keyCode);
      public static event HandleKeyUpMessageDelegate HandleKeyUpMessageEvent;

      /// <summary>
      /// checks whether we should show Logon window in RTOL format or not.
      /// </summary>
      /// <returns></returns>
      internal static bool IsLogonRTL()
      {
         bool isRTL = false;
         if (OnIsLogonRTLEvent != null)
            isRTL = OnIsLogonRTLEvent();

         return isRTL;
      }
      public delegate bool OnIsLogonRTL();
      public static event OnIsLogonRTL OnIsLogonRTLEvent;

      internal static bool IsSpecialEngLogon()
      {
         bool isEngLogon = false;
         if (OnIsSpecialEngLogonEvent != null)
            isEngLogon = OnIsSpecialEngLogonEvent();

         return isEngLogon;
      }
      public delegate bool OnIsSpecialEngLogon();
      public static event OnIsSpecialEngLogon OnIsSpecialEngLogonEvent;

      internal static bool IsSpecialIgnoreButtonFormat()
      {
         bool isIgnoreButtonFormat = false;
         if (OnIsSpecialIgnoreButtonFormatEvent != null)
            isIgnoreButtonFormat = OnIsSpecialIgnoreButtonFormatEvent();

         return isIgnoreButtonFormat;
      }

      public delegate bool OnIsSpecialIgnoreButtonFormat();
      public static event OnIsSpecialIgnoreButtonFormat OnIsSpecialIgnoreButtonFormatEvent;

      #endregion //Raised by Gui.low


      #region Raised by Management layer

      /// <summary>get the file name in the local file system of a given url.</summary>
      /// <param name="url">a url (absolute or relative) to retrieve thru the web server.</param>
      /// <returns>file name in the local file system.</returns>
      internal static String GetLocalFileName(String url, TaskBase task)
      {
         return (GetLocalFileNameEvent != null
                   ? GetLocalFileNameEvent(url, task)
                   : url);
      }
      public delegate String GetLocalFileNameDelegate(String url, TaskBase task);
      public static event GetLocalFileNameDelegate GetLocalFileNameEvent;

      /// <summary>Get .Net assembly file name specified by url in local file system</summary>
      /// <param name="url">a url (absolute or relative) to retrieve thru the web server.</param>
      /// <returns>file name in the local file system.</returns>
      internal static String GetDNAssemblyFile(String url)
      {
         return (GetDNAssemblyFileEvent != null
                   ? GetDNAssemblyFileEvent(url)
                   : url);
      }
      public delegate String GetDNAssemblyFileEventDelegate(String url);
      public static event GetDNAssemblyFileEventDelegate GetDNAssemblyFileEvent;

      /// <summary> invokes the GetApplicationMenus Event </summary>
      /// <param name="contextID">active/target context</param>
      /// <param name="ctlIdx"></param>
      /// <returns></returns>
      internal static ApplicationMenus GetApplicationMenus(Int64 contextID, int ctlIdx)
      {
         Debug.Assert(GetApplicationMenusEvent != null);
         return GetApplicationMenusEvent(contextID, ctlIdx);
      }
      internal delegate ApplicationMenus GetApplicationMenusDelegate(Int64 contextID, int ctlIdx);
      internal static event GetApplicationMenusDelegate GetApplicationMenusEvent;

      /// <summary> invokes the getMainProgram Event </summary>
      /// <param name="contextID">active/target context</param>
      /// <param name="ctlIdx"></param>
      /// <returns></returns>
      internal static TaskBase GetMainProgram(Int64 contextID, int ctlIdx)
      {
         Debug.Assert(GetMainProgramEvent != null);
         return GetMainProgramEvent(contextID, ctlIdx);
      }
      public delegate TaskBase GetMainProgramDelegate(Int64 contextID, int ctlIdx);
      public static event GetMainProgramDelegate GetMainProgramEvent;

      /// <summary>
      /// Logical name translation
      /// </summary>
      /// <param name="fromString">source string</param>
      /// <returns>translated string</returns>
      internal static string TranslateLogicalName(string fromString)
      {
         Debug.Assert(TranslateLogicalNameEvent != null);
         return TranslateLogicalNameEvent(fromString);
      }
      public static event TranslateDelegate TranslateLogicalNameEvent;

      /// <summary>invokes getMessageString Event</summary>
      /// <param name="msgId"></param>
      /// <returns></returns>
      internal static String GetMessageString(string msgId)
      {
         Debug.Assert(GetMessageStringEvent != null);
         String constString = GetMessageStringEvent(msgId);
         // Story #115578 : the studio need to be translate also by the application mls
         String transString = Translate(constString);

         // Fixed defect#122198: Add Trailing space for message from mgconst that end with ":".
         //                      so it will be the same as v2.5 - it is for automation tests
         if (transString != null && transString.EndsWith(":"))
            transString = transString.Insert(transString.Length, " ");

         return transString;

      }

      public delegate String GetMessageStringDelegate(string msgId);
      public static event GetMessageStringDelegate GetMessageStringEvent;

      /// <summary>raises CtrlFocusEvent</summary>
      /// <returns></returns>
      internal static void OnCtrlFocus(ITask iTask, MgControlBase ctrl)
      {
         if (CtrlFocusEvent != null)
            CtrlFocusEvent(iTask, ctrl);
      }
      public delegate void CtrlFocusDelegate(ITask iTask, MgControlBase ctrl);
      public static CtrlFocusDelegate CtrlFocusEvent;

      /// <summary>get Current processing task</summary>
      /// <returns></returns>
      internal static ITask GetCurrentTask()
      {
         return (GetCurrentTaskEvent != null
                   ? GetCurrentTaskEvent()
                   : null);
      }
      public delegate ITask GetCurrTaskDelegate();
      public static GetCurrTaskDelegate GetCurrentTaskEvent;

      /// <summary>
      /// Gets the RuntimeContextBase that belongs to the contextID
      /// </summary>
      /// <param name="contextID"></param>
      /// <returns></returns>
      internal static RuntimeContextBase GetRuntimeContext(Int64 contextID)
      {
         Debug.Assert(GetRuntimeContextEvent != null);
         return GetRuntimeContextEvent(contextID);
      }
      public delegate RuntimeContextBase GetRuntimeContextDelegate(Int64 contextID);
      public static GetRuntimeContextDelegate GetRuntimeContextEvent;

      /// <summary>
      /// Saves the name of the last clicked control.
      /// </summary>
      /// <param name="guiMgControl">guiMgControl</param>
      /// <param name="controlName">Name of the control</param>
      internal static void SaveLastClickedCtrlName(GuiMgControl guiMgControl, String controlName)
      {
         if (SaveLastClickedCtrlEvent != null)
            SaveLastClickedCtrlEvent(guiMgControl, controlName);
      }
      public delegate void SaveLastClickedCtrlNameDelegate(GuiMgControl guiMgControl, String controlName);
      public static SaveLastClickedCtrlNameDelegate SaveLastClickedCtrlEvent;

      /// <summary>
      /// Saves the last clicked information. (i.e. Co-ordinates and control name.
      /// </summary>
      /// <param name="guiMgForm">guiMgForm</param>
      /// <param name="controlName">name of the control</param>
      /// <param name="x"></param>
      /// <param name="y"></param>
      /// <param name="offsetX"></param>
      /// <param name="offsetY"></param>
      /// <param name="lastClickCoordinatesAreInPixels">co-ordinates are in pixel or not.</param>
      internal static void SaveLastClickInfo(GuiMgForm guiMgForm, String controlName, int x, int y, int offsetX, int offsetY, bool lastClickCoordinatesAreInPixels)
      {
         if (SaveLastClickInfoEvent != null)
            SaveLastClickInfoEvent(guiMgForm, controlName, x, y, offsetX, offsetY, lastClickCoordinatesAreInPixels);
      }
      public delegate void SaveLastClickInfoDelegate(GuiMgForm guiMgForm, String controlName, int x, int y, int offsetX, int offsetY, bool lastClickCoordinatesAreInPixels);
      public static SaveLastClickInfoDelegate SaveLastClickInfoEvent;

      #endregion //Raised by Management layer


      #region Logging events

      public delegate void WriteToLogDelegate(String msg);

      /// <summary>write an error message to the internal log file</summary>
      /// <param name="msg"></param>
      internal static void WriteErrorToLog(String msg)
      {
         Debug.Assert(WriteErrorToLogEvent != null);
         WriteErrorToLogEvent(msg);
      }
      public static WriteToLogDelegate WriteErrorToLogEvent;

      /// <summary>Write an internal error to the log. Also prints stack trace along with the message</summary>
      /// <param name="msg"></param>
      internal static void WriteExceptionToLog(String msg)
      {
         Debug.Assert(WriteExceptionToLogEvent != null);
         WriteExceptionToLogEvent(msg);
      }
      public static WriteToLogDelegate WriteExceptionToLogEvent;

      internal static void WriteExceptionToLog(Exception ex)
      {
         WriteExceptionToLog(string.Format("{0} : {1}{2}{3}{4}",
                                       ex.GetType(), OSEnvironment.EolSeq,
                                       ex.StackTrace, OSEnvironment.EolSeq,
                                       ex.Message));
      }

      internal static String GetDataViewContent(TaskBase task, int generation, String taskVarList)
      {
         Debug.Assert(WriteExceptionToLogEvent != null);
         return GetDataViewContentEvent(task, generation, taskVarList, DataViewOutputType.Xml, 0, string.Empty);
      }
      public delegate String GetDataViewContentDelegate(TaskBase task, int generation, String taskVarList, DataViewOutputType outputType, int destinationDataSourceNumber, string listIndexesOfDestinationSelectedFields);
      public static GetDataViewContentDelegate GetDataViewContentEvent;

      /// <summary>write a warning message to the internal log file</summary>
      /// <param name="msg"></param>
      internal static void WriteWarningToLog(String msg)
      {
         Debug.Assert(WriteWarningToLogEvent != null);
         WriteWarningToLogEvent(msg);
      }
      public static WriteToLogDelegate WriteWarningToLogEvent;

      internal static void WriteWarningToLog(Exception ex)
      {
         WriteWarningToLog(ex.GetType() + " : " + OSEnvironment.EolSeq +
                           ex.StackTrace + OSEnvironment.EolSeq +
                           ex.Message);
      }
      
      /// <summary>
      /// 
      /// </summary>
      /// <param name="logLevel"></param>
      /// <returns></returns>
      internal static bool ShouldLog(Logger.LogLevels logLevel)
      {
         Debug.Assert(ShouldLogEvent != null);
         return ShouldLogEvent(logLevel);
      }
      public delegate bool ShouldLogDelegate(Logger.LogLevels logLevel);
      public static ShouldLogDelegate ShouldLogEvent;

      /// <summary>write a GUI message to the internal log file</summary>
      /// <param name="msg"></param>
      internal static void WriteGuiToLog(String msg)
      {
         Debug.Assert(WriteGuiToLogEvent != null);
         WriteGuiToLogEvent("GUI: " + msg);
      }
      public static WriteToLogDelegate WriteGuiToLogEvent;
      
      /// <summary>write a DEV message to the internal log file</summary>
      /// <param name="msg"></param>
      internal static void WriteDevToLog(String msg)
      {
         Debug.Assert(WriteDevToLogEvent != null);
         WriteDevToLogEvent(msg);
      }
      public static WriteToLogDelegate WriteDevToLogEvent;

      #endregion //Logging events

      /// <summary>
      /// Called when WM_CUT message is received
      /// </summary>
      /// <param name="ctrl"></param>
      internal static void OnCut (GuiMgControl ctrl)
      {
         if (CutEvent != null)
            CutEvent(ctrl);
      }
      public delegate void CutDelegate(GuiMgControl ctrl);
      public static CutDelegate CutEvent;

      /// <summary>
      /// Called when WM_COPY message is received
      /// </summary>
      /// <param name="ctrl"></param>
      internal static void OnCopy(GuiMgControl ctrl)
      {
         if (CopyEvent != null)
            CopyEvent(ctrl);
      }
      public delegate void CopyDelegate(GuiMgControl ctrl);
      public static CopyDelegate CopyEvent;

      /// <summary>
      /// Called when WM_PASTE message is received
      /// </summary>
      /// <param name="ctrl"></param>
      internal static void OnPaste(GuiMgControl ctrl)
      {
         if (PasteEvent != null)
            PasteEvent(ctrl);
      }
      public delegate void PasteDelegate(GuiMgControl ctrl);
      public static PasteDelegate PasteEvent;

      /// <summary>
      /// Called when WM_CLEAR message is received
      /// </summary>
      /// <param name="ctrl"></param>
      internal static void OnClear(GuiMgControl ctrl)
      {
         if (ClearEvent != null)
            ClearEvent(ctrl);
      }
      public delegate void ClearDelegate(GuiMgControl ctrl);
      public static ClearDelegate ClearEvent;

      /// <summary>
      /// Called when WM_UNDO message is received
      /// </summary>
      /// <param name="ctrl"></param>
      internal static void OnUndo(GuiMgControl ctrl)
      {
         if (UndoEvent != null)
            UndoEvent(ctrl);
      }
      public delegate void UndoDelegate(GuiMgControl ctrl);
      public static UndoDelegate UndoEvent;

      /// <summary>
      /// Should Enter be added as keyboard event ? This is important mostly in determining if a click event
      /// will be raised for a default button or not. 
      /// Adding Enter as a keyboard event will not raise click on default button.
      /// </summary>
      /// <returns></returns>
      internal static bool AddEnterAsKeyEvent()
      {
         if (ShouldAddEnterAsKeyEvent != null)
            return ShouldAddEnterAsKeyEvent();
         else
            return false;
      }
      public delegate bool ShouldAddEnterAsKeyEventDelegate();
      public static ShouldAddEnterAsKeyEventDelegate ShouldAddEnterAsKeyEvent;

      /// <summary>
      /// When mouse down even arrives on tree, we produce click. but sometime we need to skip it.
      /// return true if the click event can be skipped.
      /// </summary>
      /// <returns></returns>
      internal static bool CanTreeClickBeSkippedOnMouseDown()
      {
         if (CanTreeClickBeSkippedOnMouseDownEvent != null)
            return CanTreeClickBeSkippedOnMouseDownEvent();
         else
            return false;
      }
      public delegate bool CanTreeClickBeSkippedOnMouseDownDelegate();
      public static CanTreeClickBeSkippedOnMouseDownDelegate CanTreeClickBeSkippedOnMouseDownEvent;

      
#if !PocketPC
      /// <summary>
      /// </summary>
      internal static void ShowSessionStatisticsForm()
      {
         if (ShowSessionStatisticsEvent != null)
            ShowSessionStatisticsEvent();
      }
      public delegate void ShowSessionStatisticsDelegate();
      public static ShowSessionStatisticsDelegate ShowSessionStatisticsEvent;

      /// <summary>
      /// Called when WM_COPYDATA message is received
      /// </summary>
      /// <param name="guiMgForm"></param>
      /// <param name="copyData"></param>
      internal static void OnCopyData(GuiMgForm guiMgForm, IntPtr copyData)
      {
         if (CopyDataEvent != null)
            CopyDataEvent(guiMgForm, copyData);
      }
      public delegate void CopyDataDelegate(GuiMgForm guiMgForm, IntPtr copyData);
      public static CopyDataDelegate CopyDataEvent;
#endif
   }
}
