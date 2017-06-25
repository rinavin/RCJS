using System;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.util;
using System.Diagnostics;

namespace com.magicsoftware.unipaas
{
   /// <summary>
   /// (1) registers and implements handlers for events that can be served locally by MgGui.dll.
   /// (2) requests subclasses to register their handlers ('Template Method' design pattern).
   /// </summary>
   public abstract class EventsProcessor
   {
      public EventsProcessor()
      {
         // (1) register and implement handlers for events that can be served locally by MgGui.dll.
         RegisterHandlers();

         // (2) request subclasses to register external handlers that they will implement ('Template Method' design pattern).
         RegisterSubclassHandlers();
      }

      /// <summary>register and implement handlers for events that can be served locally by MgGui.dll.</summary>
      private void RegisterHandlers()
      {
         Events.KeyDownEvent += processKeyDown;
         Events.ComboDroppingDownEvent += processComboDroppingDown;
         Events.WindowResizeEvent += processWindowResize;
         Events.WindowMoveEvent += processWindowMove;
         Events.MouseDownEvent += processMouseDown;
         Events.MouseOverEvent += processMouseOver;
         Events.MouseOutEvent += processMouseOut;
         Events.DblClickEvent += processDblClick;
         Events.CloseFormEvent += processFormClose;
         Events.RefreshMenuActionsEvent += refreshMenuActions;
         Events.GetContextMenuEvent += getContextMenu;
         Events.MenuSelectionEvent += onMenuSelection;
         Events.OnMenuPromptEvent += onMenuPrompt;
         Events.GetApplicationMenusEvent += getApplicationMenus;
         Events.ColumnClickEvent += ProcessColumnClick;
         Events.ColumnFilterEvent += ProcessColumnFilter;
         Events.IsContextMenuAllowedEvent += isContextMenuAllowed;
         Events.MultimarkHitEvent += processMultimarkHitEvent;
         Events.ContextMenuCloseEvent += processContextMenuClose;

         #if !PocketPC
         Events.BeginDragEvent += processBeginDrag;
         Events.BeginDropEvent += processBeginDrop;
         Events.GetDropUserFormatsEvent += GetDropUserFormats;
         #endif
      }

      // request subclasses to register handlers that they will implement ('Template Method' design pattern).
      protected abstract void RegisterSubclassHandlers();

      /// <summary> combo is dropping down </summary>
      /// <param name = "guiMgCtrl"></param>
      /// <param name = "line"></param>
      private void processComboDroppingDown(GuiMgControl guiMgCtrl, int line)
      {
         MgControlBase mgControl = (MgControlBase)guiMgCtrl;
         Manager.EventsManager.addGuiTriggeredEvent(mgControl, InternalInterface.MG_ACT_COMBO_DROP_DOWN, line);
      }

      /// <summary>Pass keycode and modifier to bridge, bridge then converts it to keyboard action and adds in MgCore.dll 
      /// events queue</summary>
      /// <param name="form"></param>
      /// <param name="guiMgCtrl"></param>
      /// <param name="modifier"></param>
      /// <param name="keyCode"></param>
      /// <param name="start"></param>
      /// <param name="end"></param>
      /// <param name="caretPos"></param>
      /// <param name="text"></param>
      /// <param name="im"></param>
      /// <param name="isActChar"></param>
      /// <param name="suggestedValue"></param>
      /// <param name="ComboIsDropDown"></param>
      /// <param name="handled">boolean variable event is handled or not.</param>      
      /// <returns> true only if we have handled the KeyDown event (otherwise the CLR should handle). If true magic will handle else CLR will handle.</returns>
      protected virtual bool processKeyDown(GuiMgForm guiMgForm, GuiMgControl guiMgCtrl, Modifiers modifier, int keyCode,
                                            int start, int end, string text, ImeParam im,
                                            bool isActChar, string suggestedValue, bool ComboIsDropDown, bool handled)
      {
         bool eventHandled = handled;
         bool addKeyBoardEvent = true;

         MgControlBase mgControl = (MgControlBase)guiMgCtrl;
         MgFormBase mgForm = (MgFormBase)guiMgForm;

         if (mgControl != null)
         {
            //In case of help window, the events like up arrow\down arrow key, should be handled by the 
            //CLR.So first check if the form is help form and return the value true or false.

            if (mgControl.getForm() != null && mgControl.getForm().IsHelpWindow && keyCode != GuiConstants.KEY_ESC)
               //events related with the help window will NOT be handled by magic.
               return false;

            // DOWN or UP invoked on a SELECT/RADIO control
            if ((mgControl.isRadio() || mgControl.isListBox() ||
                (mgControl.isComboBox() && ComboIsDropDown) ||
                (mgControl.isTabControl() && !suggestedValue.Equals("-1"))))
            {
               if (processKeyForSelectionControl(mgForm, mgControl, modifier, keyCode, suggestedValue))
                  addKeyBoardEvent = false;
            }
         }

         if (addKeyBoardEvent)
            // raise event 
            Manager.EventsManager.AddKeyboardEvent(mgForm, mgControl, modifier, keyCode,
                                                   start, end, text, im,
                                                   isActChar, suggestedValue, InternalInterface.MG_ACT_CTRL_KEYDOWN);

         return eventHandled;
      }

      /// <summary>
      /// put multimark hit event 
      /// </summary>
      /// <param name="guiMgCtrl"></param>
      /// <param name="row"></param>
      void processMultimarkHitEvent(GuiMgControl guiMgCtrl, int row, Modifiers modifier)
      {
         Manager.EventsManager.addGuiTriggeredEvent((MgControlBase)guiMgCtrl, InternalInterface.MG_ACT_MULTI_MARK_HIT, row, modifier);
      }
 
      /// <summary>
      ///   send MG_ACT_ARROW_KEY to worker thread form key down,up,left,right
      ///   we are handling those keys
      /// </summary>
      /// <param name = "mgControl"> </param>
      /// <param name = "modifier"> </param>
      /// <param name = "keyCode"> </param>
      /// <param name = "suggestedValue"> </param>
      /// <returns> true if need to ignore this action key</returns>
      private bool processKeyForSelectionControl(MgFormBase mgForm, MgControlBase mgControl, Modifiers modifier, int keyCode, String suggestedValue)
      {
         bool isSelectionKey = false;

         switch (keyCode)
         {
            case GuiConstants.KEY_DOWN:
            case GuiConstants.KEY_UP:
               isSelectionKey = true;
               break;

            case GuiConstants.KEY_RIGHT:
            case GuiConstants.KEY_LEFT:
               if (!mgControl.isListBox())
                  isSelectionKey = true;
               break;

            case GuiConstants.KEY_SPACE:
               if (mgControl.isRadio())
                  isSelectionKey = true;
               break;

            case GuiConstants.KEY_PG_DOWN:
            case GuiConstants.KEY_PG_UP:
            case GuiConstants.KEY_HOME:
            case GuiConstants.KEY_END:
               if (mgControl.isComboBox())
                  isSelectionKey = true;
               break;
         }

         if (isSelectionKey)
         {
            // raise event 
            Manager.EventsManager.AddKeyboardEvent(mgForm, mgControl, modifier, keyCode,
                                                   0, 0, null, null,
                                                   false, suggestedValue, InternalInterface.MG_ACT_ARROW_KEY);
         }
         return isSelectionKey;
      }

     /// <summary>
     /// Function handling form close event.
     /// </summary>
     /// <param name="guiMgForm"></param>
     /// <returns>true if event is CLR handled else false.</returns>
      protected virtual bool processFormClose(GuiMgForm guiMgForm)
      {
         bool clrHandledEvent = false;
         MgFormBase mgForm = (MgFormBase)guiMgForm;

         if (mgForm.IsMDIFrame)
            Manager.EventsManager.addGuiTriggeredEvent(mgForm.getTask(), InternalInterface.MG_ACT_EXIT_SYSTEM);
         else
         {
            if (!mgForm.IsHelpWindow)
            {
               if (mgForm.ShouldPutActOnFormClose())
                  Manager.EventsManager.addGuiTriggeredEvent(mgForm.getTask(), InternalInterface.MG_ACT_HIT, RaisedBy.CLOSE_SYSTEM_MENU);
               Manager.EventsManager.addGuiTriggeredEvent(mgForm.getTask(), InternalInterface.MG_ACT_CLOSE);
            }
            else
            {
               if (mgForm.Opened)
                  clrHandledEvent = true;
            }
         }

         return clrHandledEvent;
      }

      /// <summary>Process Window Resize</summary>
      /// <param name = "guiMgForm">form</param>
      private void processWindowResize(GuiMgForm guiMgForm)
      {
         MgFormBase mgForm = (MgFormBase)guiMgForm;
         Manager.EventsManager.addGuiTriggeredEvent(mgForm.getTask(), InternalInterface.MG_ACT_WINSIZE);
      }

      /// <summary>Process Window Move</summary>
      /// <param name = "guiMgForm">form</param>
      private void processWindowMove(GuiMgForm guiMgForm)
      {
         MgFormBase mgForm = (MgFormBase)guiMgForm;
         Manager.EventsManager.addGuiTriggeredEvent(mgForm.getTask(), InternalInterface.MG_ACT_WINMOVE);
      }

      /// <summary></summary>
      /// <param name = "guiMgForm"></param>
      /// <param name = "guiMgCtrl"></param>
      /// <param name = "dotNetArgs"></param>
      /// <param name = "leftClickWasPressed"></param>
      /// <param name = "line"></param>
      private void processMouseDown(GuiMgForm guiMgForm, GuiMgControl guiMgCtrl, Object[] dotNetArgs, bool leftClickWasPressed, int line, bool onMultiMark, bool canProduceClick)
      {
         MgControlBase mgControl = (MgControlBase)guiMgCtrl;

         // Click on form or Click on the Subform's form should handle mg_act_hit (not control_hit).
         if (mgControl == null)
         {
            Debug.Assert(guiMgForm != null);
            // As in online. clicking on a form results in ACT_HIT
            Manager.EventsManager.addGuiTriggeredEvent(((MgFormBase)guiMgForm).getTask(), InternalInterface.MG_ACT_HIT, onMultiMark);
            return;
         }
         else if (mgControl.isSubform())
         {
            mgControl.OnSubformClick();
            return;
         }
            
         if (mgControl.RaiseControlHitOnMouseDown(leftClickWasPressed))
            Manager.EventsManager.addGuiTriggeredEvent(mgControl, InternalInterface.MG_ACT_CTRL_HIT, line, null, onMultiMark);

         if (leftClickWasPressed && canProduceClick)
         {
            // fixed bug#927942
            // we never get to this method with the control type button & checkbox.
            // for button & checkbox, we sent the trigger MG_ACT_WEB_CLICK in EventsManager.handleMouseUp() that call 
            // from simulateSelection()
            // the "if" is only for make sure that if we will get it (in the feture) we will not be sent the same trigger twice 
            if (mgControl.Type != MgControlType.CTRL_TYPE_BUTTON && mgControl.Type != MgControlType.CTRL_TYPE_CHECKBOX)
               Manager.EventsManager.addGuiTriggeredEvent(mgControl, InternalInterface.MG_ACT_WEB_CLICK, line, dotNetArgs, false);
         }
      }

      /// <summary>process "mouseover" event</summary>
      ///<param name = "guiMgCtrl">the control</param>
      private void processMouseOver(GuiMgControl guiMgCtrl)
      {
         MgControlBase mgControl = (MgControlBase)guiMgCtrl;
         Manager.EventsManager.addGuiTriggeredEvent(mgControl, InternalInterface.MG_ACT_WEB_MOUSE_OVER, 0);
      }

      /// <summary>process "mouseout" event</summary>
      ///<param name = "guiMgCtrl">the control</param>
      private void processMouseOut(GuiMgControl guiMgCtrl)
      {
         MgControlBase mgControl = (MgControlBase)guiMgCtrl;
         Manager.EventsManager.addGuiTriggeredEvent(mgControl, InternalInterface.MG_ACT_WEB_MOUSE_OUT, 0);
      }

      /// <summary>process "dblclick" event</summary>
      /// <param name = "guiMgCtrl">the control </param>
      /// <param name = "line"> the line of the multiline control </param>
      private void processDblClick(GuiMgControl guiMgCtrl, int line)
      {
         MgControlBase mgControl = (MgControlBase)guiMgCtrl;
         if (!mgControl.GetComputedBooleanProperty(PropInterface.PROP_TYPE_ENABLED, true, line))
            return;

         // raising the internal events : MG_ACT_SELECT and MG_ACT_ZOOM was moved to commonHandler under the
         // MG_ACT_WEB_ON_DBLICK case.
         // This is different then the behavior of online because we fixed QCR #990836 only in rich.
         // In rich, 1st dbclick is raised. Then if there is a handler on it with propogate ='YES', only then the zoom/select
         // will be raised. A Propagate 'NO' will block them.

         Manager.EventsManager.addGuiTriggeredEvent(mgControl, InternalInterface.MG_ACT_WEB_ON_DBLICK, line);
      }

      /// <summary> handle Column Click event on Column </summary>
      /// <param name="guiColumnCtrl"></param>
      /// <param name="direction"></param>
      /// <param name="columnHeader"></param>
      private void ProcessColumnClick(GuiMgControl guiColumnCtrl, int direction, String columnHeader)
      {
         MgControlBase mgColumnControl = (MgControlBase)guiColumnCtrl;
         Manager.EventsManager.AddColumnClickEvent(mgColumnControl, direction, columnHeader);
      }

      /// <summary>
      /// Handle column filter click event
      /// </summary>
      /// <param name="columnCtrl"></param>
      /// <param name="columnHeader"></param>
      /// <param name="index"></param>
      /// <param name="x"></param>
      /// <param name="y"></param>
      private void ProcessColumnFilter(GuiMgControl columnCtrl, String columnHeader, int x, int y, int width, int height)
      {
         MgControlBase mgColumnControl = (MgControlBase)columnCtrl;
         //values that sent to the event, sent in display %. We want that it will be always in 100%.
         Object mapObject = ((MgFormBase)mgColumnControl.GuiMgForm).getMapObject();
         float resolution = ((float)Commands.getResolution(mapObject).x / 96);
         x = ((int)(x / resolution));
         y = ((int)(y / resolution));
         width = ((int)(width / resolution));
         height = ((int)(height / resolution));
         Manager.EventsManager.AddColumnFilterEvent(mgColumnControl, columnHeader, x, y, width, height);
      }

      /// <summary> returns if ContextMenu is allowed </summary>
      /// <param name="guiMgControl"></param>
      private bool isContextMenuAllowed(GuiMgControl guiMgControl)
      {
         MgControlBase mgControl = (MgControlBase)guiMgControl;
         return mgControl.getForm().getTask().IsInteractive;
      }

      /// <summary> returns if ContextMenu is allowed </summary>
      /// <param name="guiMgForm"></param>
      /// <param name="guiMgMenu"></param>
      private void processContextMenuClose(GuiMgForm guiMgForm, GuiMgMenu guiMgMenu)
      {
         Commands.addAsync(CommandType.DELETE_MENU, guiMgForm, guiMgForm, MenuStyle.MENU_STYLE_CONTEXT, guiMgMenu, true);
      }

      /// <summary> Refresh internal actions used in Menus </summary>
      /// <param name = "guiMgMenu"></param>
      /// <param name = "guiMgForm"></param>
      private void refreshMenuActions(GuiMgMenu guiMgMenu, GuiMgForm guiMgForm)
      {
         MgMenu mgMenu = (MgMenu)guiMgMenu;
         MgFormBase mgForm = (MgFormBase)guiMgForm;

         if (mgMenu != null)
            mgMenu.refreshInternalEventMenus(mgForm);
      }

      /// <summary> This method returns the control/form's context menu </summary>
      /// <param name = "guiMgControl"></param>
      /// <returns></returns>
      private GuiMgMenu getContextMenu(Object obj)
      {
         MgMenu mgMenu = null;

         if (obj is MgControlBase)
            mgMenu = ((MgControlBase)obj).getContextMenu(true);
         else if (obj is MgFormBase)
            mgMenu = ((MgFormBase)obj).getContextMenu(true);

         return mgMenu;
      }

      private void onMenuSelection(GuiMenuEntry menuEntry, GuiMgForm activeForm, bool activatedFromMDIFrame)
      {
         Manager.MenuManager.onMenuSelection(menuEntry, activeForm, activatedFromMDIFrame);
      }

      /// <summary> </summary>
      /// <param name = "guiMgForm"></param>
      /// <param name = "prompt"></param>
      private void onMenuPrompt(GuiMgForm guiMgForm, GuiMenuEntry guiMenuEntry)
      {
         MgFormBase mgForm = null;
         TaskBase task = null;

         if (guiMgForm is MgFormBase)
            mgForm = (MgFormBase)guiMgForm;

         if (mgForm != null)
            task = mgForm.getTask();

         if (task != null)
         {
            if (guiMenuEntry == null)
            {
               if( !task.isAborting())
                  Manager.CleanMessagePane(task);
            }
            else
            {
               if (guiMenuEntry is MenuEntry)
               {
                  MenuEntry menuEntry = (MenuEntry)guiMenuEntry;

                  String prompt = menuEntry.getPrompt();
                  if (prompt == null)
                     prompt = "";

                  Manager.WriteToMessagePane(task, StrUtil.makePrintable(prompt), false);
               }
            }
         }
      }

      /// <summary>
      /// returns the application menus
      /// </summary>
      /// <param name="ctlIdx"></param>
      /// <returns></returns>
      private ApplicationMenus getApplicationMenus(Int64 contextID, int ctlIdx)
      {
         TaskBase mainProg = Events.GetMainProgram(contextID, ctlIdx);
         return Manager.MenuManager.getApplicationMenus(mainProg);
      }

      #if !PocketPC
      /// <summary>
      /// Perform action required when object is dragged.
      /// Put ACT_BEGIN_DRAG action.
      /// </summary>
      /// <param name="guiMgCtrl"></param>
      /// <param name="line"></param>
      internal void processBeginDrag(GuiMgControl guiMgCtrl, int line)
      {
         MgControlBase mgControl = (MgControlBase)guiMgCtrl;
         Manager.EventsManager.addGuiTriggeredEvent(mgControl, InternalInterface.MG_ACT_BEGIN_DRAG, line);
      }

      /// <summary>
      /// Perform actions required when a dragged object is dropped (mouse released).
      /// Put actions : ACT_CTRL_HIT or ACT_HIT and MG_ACT_BEGIN_DROP.
      /// </summary>
      /// <param name="guiMgForm"></param>
      /// <param name="guiMgCtrl"></param>
      /// <param name="line"></param>
      protected virtual void processBeginDrop(GuiMgForm guiMgForm, GuiMgControl guiMgCtrl, int line)
      {
         MgControlBase mgControl = (MgControlBase)guiMgCtrl;
         MgFormBase mgForm = (guiMgForm == null) ? mgControl.getForm() : (MgFormBase)guiMgForm;
         bool isCtrlHit = (mgControl != null) ? true : false;  // Drop occurs on a control or a form?

         if (isCtrlHit)
         {
            Manager.EventsManager.addGuiTriggeredEvent(mgControl, InternalInterface.MG_ACT_CTRL_HIT, line);

            if (mgControl.isSubform())
               Manager.EventsManager.addGuiTriggeredEvent(mgControl.GetSubformMgForm().getTask(), InternalInterface.MG_ACT_BEGIN_DROP);
            else
               Manager.EventsManager.addGuiTriggeredEvent(mgControl, InternalInterface.MG_ACT_BEGIN_DROP, line);
         }
         else
         {  // If Drop occurs on FORM.
            Manager.EventsManager.addGuiTriggeredEvent(mgForm.getTask(), InternalInterface.MG_ACT_HIT);
            Manager.EventsManager.addGuiTriggeredEvent(mgForm.getTask(), InternalInterface.MG_ACT_BEGIN_DROP);
         }
      }

      /// <summary>
      ///  Gets the user defined drop formats from the MgCore.
      /// </summary>
      /// <returns></returns>
      private String GetDropUserFormats()
      {
         return Manager.Environment.GetDropUserFormats();
      }
      #endif

      /// <summary> invokes the WindowResize Event</summary>
      /// <param name="form"></param>
      internal static void OnWindowResize(GuiMgForm form)
      {
         Debug.Assert(WindowResizeEvent != null);
         WindowResizeEvent(form);
      }
      internal delegate void WindowResizeDelegate(GuiMgForm form);
      internal static event WindowResizeDelegate WindowResizeEvent;

      /// <summary> invokes the WindowMove Event</summary>
      /// <param name="form"></param>
      internal static void OnWindowMove(GuiMgForm form)
      {
         Debug.Assert(WindowMoveEvent != null);
         WindowMoveEvent(form);
      }
      internal delegate void WindowMoveDelegate(GuiMgForm form);
      internal static event WindowMoveDelegate WindowMoveEvent;
   }
}
