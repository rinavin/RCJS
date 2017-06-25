using com.magicsoftware.controls;
using com.magicsoftware.support;
using com.magicsoftware.unipaas.management.gui;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <author>  ilanab This class is the menu handler for our menus. All needed operations regarding the display
   /// and activation of menus will be handled here.
   /// </author>
   internal class MgMenuHandler : HandlerBase
   {
      private static MgMenuHandler _instance;
      internal static MgMenuHandler getInstance()
      {
         if (_instance == null)
            _instance = new MgMenuHandler();

         return _instance;
      }

      /// <summary>
      /// menu strip is not opening, what id opening is it's items, by click
      /// context menu however, does open. therefor olny in case of context, we have a handler for opening.
      /// </summary>
      /// <param name="toolStrip"></param>
      internal void addHandler(ToolStrip toolStrip)
      {

         //SWT.Selection, SWT.Arm, SWT.Dispose, SWT.MouseExit, SWT.MouseEnter, SWT.MouseMove, SWT.Show
         toolStrip.Disposed += DisposedHandler;

         if (toolStrip is ContextMenuStrip)
         {
            ((ContextMenuStrip)toolStrip).Opening += MenuOpeningHandler;
            ((ContextMenuStrip)toolStrip).Closed += MenuClosedHandler;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="toolStripMenuItem"></param>
      /// <param name="IsMenu"></param>
      internal void addHandler(ToolStripItem toolStripItem, bool IsMenu)
      {

         //SWT.Selection, SWT.Arm, SWT.Dispose, SWT.MouseExit, SWT.MouseEnter, SWT.MouseMove, SWT.Show
         // Enter/Leave is only for showing the prompt of Toolbar items.

         toolStripItem.Disposed += DisposedHandler;

         if (!(toolStripItem is ToolStripSeparator))
         {
            // In order to show the prompt (same as arm for swt)
            toolStripItem.MouseEnter += MouseEnterHandler;
            toolStripItem.MouseLeave += MouseLeaveHandler;
            
            toolStripItem.Disposed += DisposedHandler;

            if (IsMenu)
            {
               ((ToolStripMenuItem)toolStripItem).DropDownOpening += MenuOpeningHandler;
               ((ToolStripMenuItem)toolStripItem).DropDown.KeyDown += KeyDownHandler;
            }
            else
               toolStripItem.Click += MenuItemSelectedHandler;
         }
      }

      private MgMenuHandler()
      {
      }

      /*
      * (non-Javadoc)
      *  For ContextMenu : MENU_OPENING replaces the SWT's MenuDetect. Since there is no such handler on a control, we needed a way
       *  to block the system's menu and show nothing in case there was no context menu assigned by the user. We also need to decide on 
       *  the correct context menu before it opens.
       *  We do that by assigning a new and empty context menu for each control that is created.
       *  When the user right clicks on a control, the empty context (or a real context) shoots a MENU_OPENING.
       *  In this point do a few things :
       *  1. If this is the 1st opening event for that click, we need to decide if that is the correct menu to be opened.
       *     In case its the 2nd time, the 'ContextCanOpen' flag will be true, verifying that we don't need to check the menu.
       *  2. To check the menu we have separate methods for table controls and other controls.
       *     each type will have its own 'handleContextMenu'.
       *     * In table, if the clicked cell is already in edit mode, then the control we'll get here will be already a regular edit box.
       *  3. In case we have discovered that the correct menu to be showed is not the one that had sent us the 'OPENING' event,
       *     we will cancel its opening and insteed we will set the 'ContextCanOpen' to true and call the ContextMenuStrip.Show ourselves. 
       *     That way the correct menu will be opened.
       */
      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         MapData mapData;
         GuiMgForm guiMgForm = null;
         MenuReference menuRef = null;
         ControlsMap controlsMap;
         Manager.ContextIDGuard contextIDGuard = null;

         mapData = ControlsMap.getInstance().getMapData(sender);
         if (mapData != null)
         {
            menuRef = mapData.getMenuReference();
            guiMgForm = menuRef.GetMgForm();
            if (guiMgForm != null)
               contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(guiMgForm));
         }
         try
         {
            switch (type)
            {
               // Only ContexMenuStrip and ToolStripMenuItem can be opened. A MenuStrip is never opened, only its childs which are items. 
               // *** see also comment above.
               case EventType.MENU_OPENING:
                  // only if the context itself is opening
                  // the purpose is to determine the correct menu to be opened on the control. (substitude to the SWT.MENU_DETECT).
                  if (sender is ContextMenuStrip && !((TagData)((ContextMenuStrip)sender).Tag).ContextCanOpen)
                  {
                     ContextMenuStrip contextMenu = (ContextMenuStrip)sender;
                     Control control = contextMenu.SourceControl;
                     GuiMgControl guiMgControl = null;

                     controlsMap = ControlsMap.getInstance();

                     if (control is Form)
                        // in case of form control, the real control is its panel
                        control = ((TagData)(control.Tag)).ClientPanel;

                     /*Defect 131535 : 
                     After 3.1 the MDIClient is covered with a Panel with FitToMDI.
                     In the defect scenario when user presses the right click on online form and keeping the right click pressed drags the cursor to MDI form and then leaves the button
                     then, somehow we get the SourceControl of the context menu as MDIClient.
                     But the MDIClient is not saved in ControlsMap anymore.
                     In order to fix the problem whenever we get the SourceControl of context menu as MDIClient set it to MgPanel obtained from it's parent's (GuiForm) ClientPanel.*/
                     if (control is System.Windows.Forms.MdiClient)
                     {
                        GuiForm guiForm = (GuiForm)control.Parent;
                        control = ((TagData)guiForm.Tag).ClientPanel;
                     }

                     ContainerManager containerManager = ((TagData)(control.Tag)).ContainerManager;
                     if (containerManager is TableManager || containerManager is TreeManager || containerManager is BasicControlsManager)
                     {

                        // Use the mouse pos to determine in which row and column we are at.
                        // We cannot use the contextMenu.Left\Top, since its not always the point from which the cursor was opened.
                        // If the menu was opened via keyboard, we will not get here , since the control will be not table control but the cell control itself.
                        Point MousePos = Control.MousePosition;

                        // get the relative location on the menu within the container.
                        Point pt = control.PointToClient(new Point(MousePos.X, MousePos.Y));
                        mapData = containerManager.HitTest(pt, true, true);
                        if (mapData == null)
                        {
                           if (containerManager is TableManager)
                              GuiUtils.setTooltip(control, "");
                           else
                           {
                              if (control is Panel && ((TagData)(control.Tag)).ContainerTabControl != null)
                              // in case its the tab panel, the control will be the tab control
                                 control = ((TagData)(control.Tag)).ContainerTabControl;

                              mapData = controlsMap.getMapData(control);
                           }
                        }
                     }
                     else
                        mapData = controlsMap.getMapData(control);

                     // Do not go in here if tableManager.handleContextMenu was executed.

                     if (mapData != null)
                     {
                        bool focusChanged = false;
                        guiMgControl = mapData.getControl();
                        bool onMultiMark = false;
                        if (containerManager == null && ((TagData)control.Tag).IsEditor)
                        {                          
                           Object obj = controlsMap.object2Widget(guiMgControl, mapData.getIdx());
                           if (obj is LogicalControl)
                              containerManager = ((LogicalControl)obj).ContainerManager;
                        }

                        if (containerManager is ItemsManager)
                           onMultiMark = ((ItemsManager)containerManager).IsItemMarked(mapData.getIdx());
                        focusChanged = Events.OnBeforeContextMenu(guiMgControl, mapData.getIdx(), onMultiMark);
                        //If the focus was changed, the control must have been saved as the focussing control.
                        //So, remove it from here.
                        if (focusChanged)
                           GuiUtils.removeFocusingControl(GuiUtils.FindForm(control));

                        GuiUtils.setTooltip(control, "");
                     }

                     // Cancel the opening of the context, as we will be handling it thr' event.
                     ((CancelEventArgs)e).Cancel = true;

                     // Add Event to Open a Context Menu, So, that it should be opened after the event added by 
                     // OnBeforeContextMenu event.
                     int line = mapData != null ? mapData.getIdx() : 0;
                     // If right click in on table control and not any control attached to table control
                     if (control is TableControl && guiMgControl == null)
                     {
                        guiMgControl = controlsMap.getMapData(control).getControl();
                     }
                     // Invoke the event to open a context menu.
                     Events.OnOpenContextMenu(guiMgControl, mapData != null ? mapData.getForm() : null,
                                              contextMenu.Left, contextMenu.Top, line);
                     return;

                  }
                  else
                  {
                     mapData = ControlsMap.getInstance().getMapData(sender);
                     if (mapData == null)
                        break;

                     menuRef = mapData.getMenuReference();
                     guiMgForm = menuRef.GetMgForm();
                     if (sender is MgToolStripMenuItem)
                     {

                        // This change is to improve performance of pulldown menu. When we apply modality, (while opening a batch task).
                        // We just disable menu items on GuiMenuEntry. actual menu is diabled in .ent whe 
                        foreach (ToolStripItem item in ((ToolStripMenuItem)sender).DropDownItems)
                        {
                           if (item is MgToolStripMenuItem)
                           {
                              TagData tagData1 = (TagData)((MgToolStripMenuItem)item).Tag;
                              GuiMenuEntry guiMenuEntry1 = tagData1.guiMenuEntry;

                              if (!(guiMenuEntry1 is MenuEntryEvent ||guiMenuEntry1 is MenuEntryMenu))
                              {

                                 if (guiMenuEntry1.getModalDisabled() && (item.Enabled == guiMenuEntry1.getModalDisabled()))
                                    item.Enabled = false;
                                 else if (!item.Enabled && (guiMenuEntry1.getEnabled() == true))
                                    item.Enabled = guiMenuEntry1.getEnabled();
                              }
                           }
                        }

                        // Create windowMenu items for MgToolStripMenuItem (Only sub-menus can define window menu)
                        TagData tagData = (TagData)((MgToolStripMenuItem)sender).Tag;
                        Events.InitWindowListMenuItems(guiMgForm, tagData.guiMenuEntry, tagData.MenuStyle);
                     }
                  }

                  // we get here in 3 cases :
                  // 1. ContextCanOpen is true. i.e. we have a confirmation to open. we are already in the recursive call.
                  // 2. We are in the original opening, but this is the correct menu.
                  // 3. This is a ToolStripMenuItem (i.e. not the context menu itself.

                  //when opening a menu (pulldown or context), check if paste action 
                  //should be enabled/disabled.
                  //Don't check for dummy, no need. also, if u try : getLastFocusedControl will throw exception since dummy don't have mapData.
                  // Dummy is only relevant in context menu (not in drop down).
                  if (!(sender is ContextMenuStrip && ((ContextMenuStrip)sender).Name == "Dummy"))
                  {
                     //Call to checkPasteEnable() was introduced for QCR# 241365 in Java RC.
                     //This code is added for following situation:
                     //If there are 3 fields a, b, c. Field C has modifiable = No.
                     //If contex menu of fields a and b show paste as enabled, then the modifiable=no field will also show paste enabled...
                     //and it shouldn't.
                     //When we are on 3rd field, tab to another window and get back (to loose and regain focus)
                     //now, the paste is disabled on c as it should...but tab to the other fields, now the paste is disabled for them as well.
                     //to make paste enabled for Modifiable fields, this call is required here.
                     GuiMgControl guiMgControl = getLastFocusedControl(sender);
                     GuiUtils.checkPasteEnable(guiMgControl, true);
                  }

                  return;


               // Select event can happen ONLY on MenuItem, not on Menu. (see addHandler).
               case EventType.MENU_ITEM_SELECTED:
                  onSelection(sender);

                  break;

               case EventType.MOUSE_ENTER:
                  onItemEnterLeave((ToolStripItem)sender, type);
                  break;
               case EventType.MOUSE_LEAVE:
                  onItemEnterLeave((ToolStripItem)sender, type);
                  break;

               case EventType.CLOSED:
                  if (sender is ContextMenuStrip)
                  {
                     ToolStrip menu = (ToolStrip)sender;
                     if (((ContextMenuStrip)sender).Name == "Dummy")
                        break;

                     //Current context menu will be disposed , So, create the dummy context menu for control.
                     Control control = ((TagData)menu.Tag).MouseDownOnControl;
                     GuiUtils.setContextMenu(control, null);

                     //Dispose context Menu.
                     GuiMgMenu mgMenu = ((TagData)menu.Tag).GuiMgMenu;
                     Events.OnContextMenuClose(menuRef.GetMgForm(), mgMenu);
                  }
                  break;
               case EventType.DISPOSED:
                  controlsMap = ControlsMap.getInstance();
                  GuiMenuEntry guiMenuEntry = null;

                  if (sender is ContextMenuStrip && ((ContextMenuStrip)sender).Name == "Dummy")
                     break;

                  mapData = controlsMap.getMapData(sender);

                  if (mapData == null)
                     break;

                  menuRef = mapData.getMenuReference();
                  guiMgForm = menuRef.GetMgForm();

                  if (guiMgForm != null)
                  {
                     if (sender is ToolStripItem)
                     {
                        ToolStripItem menuItem = (ToolStripItem)sender;
                        MenuStyle menuStyle = ((TagData)menuItem.Tag).MenuStyle;

                        guiMenuEntry = ((TagData)menuItem.Tag).guiMenuEntry;

                        guiMenuEntry.removeMenuIsInstantiated(guiMgForm, menuStyle);

                        //if (menuEntry is MenuEntryMenu)
                        //   menuEntry.removeInstantiatedMenuItems(mgForm, menuStyle);
                        if ((guiMenuEntry.ImageFile != null) && menuItem.Image != null)
                           menuItem.Image.Dispose();
                        menuItem.Tag = null;
                     }
                     else if (sender is ToolStrip)
                     {
                        ToolStrip menu = (ToolStrip)sender;
                        MenuStyle menuStyle = ((TagData)menu.Tag).MenuStyle;

                        if (((TagData)menu.Tag).GuiMgMenu != null)
                        {
                           GuiMgMenu mgMenu = ((TagData)menu.Tag).GuiMgMenu;
                           mgMenu.removeInstantiatedMenu(guiMgForm, menuStyle);
                        }
                        //else if (((TagData)menu.Tag).menuEntry != null)
                        //{
                        //   MenuEntry mgMenu = ((TagData)menu.Tag).menuEntry;
                        //   menuEntry.removeInstantiatedMenuItems(mgForm, menuStyle);
                        //}

                        menu.Tag = null;
                     }
                  }

                  if (menuRef != null)
                  {
                     Object fromMap = controlsMap.object2Widget(menuRef);
                     if (fromMap != null)
                        controlsMap.remove(menuRef);
                  }

                  break;

               case EventType.KEY_DOWN:
                  {
                     if (((KeyEventArgs)e).KeyCode == Keys.F1)
                     {
                        ToolStrip menu = (ToolStrip)sender;

                        foreach (ToolStripItem menuItem in menu.Items)
                        {
                           if (menuItem.CanSelect && menuItem.Selected)
                           {
                              guiMenuEntry = ((TagData)menuItem.Tag).guiMenuEntry;

                              if (guiMenuEntry.Help > 0)
                              {
                                 if (menu.IsDropDown)
                                 {
                                    // Defect Id 115414: If help is opened then menu needs to be closed. After closing the menu focus should not be on the menu.
                                    // Same like when menu is clicked.ToolStripDropDownCloseReason.ItemClicked option closes the menu and 
                                    // focus does not remains on the menu.
                                    ((ToolStripDropDownMenu)menu).Close(ToolStripDropDownCloseReason.ItemClicked);
                                 }
                                 Events.OnHelpInVokedOnMenu(guiMenuEntry, ((TagData)menuItem.Tag).MapData.getMenuReference().GetMgForm());
                                 break;
                              }
                           }
                        }
                     }
                     else
                     {
                        if (MnemonicHelper.HandleMnemonicForHebrew((ToolStrip)sender, (char)((KeyEventArgs)e).KeyCode))
                           ((KeyEventArgs)e).Handled = true;
                     }
                  }
                  break;

               default:
                  System.Console.Out.WriteLine(type.ToString());
                  break;

            }
         }
         finally
         {
            if (contextIDGuard != null)
               contextIDGuard.Dispose();
         }
      }    

      /// <summary> This method is activated when a menu was selected. It performs the needed operations in order to
      /// translate the selected menu into the matching operation
      /// 
      /// </summary>
      /// <param name="widget">-
      /// the selected menu entry widget
      /// </param>
      private void onSelection(Object sender)
      {
         //Object object_Renamed = widget.getData();
         GuiMenuEntry guiMenuEntry = null;
         ToolStripItem menuItem = null;
         ToolStrip ts;
         
         menuItem = (ToolStripItem)sender;
         ts = menuItem.GetCurrentParent();

         guiMenuEntry = ((TagData)menuItem.Tag).guiMenuEntry;

         // when a menu with check style is selected, it is automatically checked.
         // we want to check it only if it should be checked - according to the state.
         if (sender is ToolStripButton)
            ((ToolStripButton)sender).Checked = guiMenuEntry.getChecked();
         else
            ((ToolStripMenuItem)sender).Checked = guiMenuEntry.getChecked();

         Form form = menuObjToForm(sender);
         GuiMgForm activeForm = null;
         bool activatedFromMDIFrame = false;

         if (form.IsMdiContainer)
         {
            activatedFromMDIFrame = true;

            Form activeMDIChild = GuiUtils.GetActiveMDIChild(form);
            if (activeMDIChild != null)
               form = activeMDIChild;
         }

         ControlsMap controlsMap = ControlsMap.getInstance();
         Control c = ((TagData)(form.Tag)).ClientPanel;
         activeForm = controlsMap.getControlMapData(c).getForm();

         Events.OnMenuSelection(guiMenuEntry, activeForm, activatedFromMDIFrame);
      }

      /// <summary> according to shell return the last control that was in focus
      /// it can be on subform \ sub sub form....
      /// </summary>
      /// <param name="shell">
      /// </param>
      /// <returns>
      /// </returns>
      private GuiMgControl getLastFocusedControl(Object menuObj)
      {
         Form form = menuObjToForm(menuObj);
         ControlsMap controlsMap = ControlsMap.getInstance();
         GuiMgControl guiMgControl = null;

         if (form.IsMdiContainer)
         {
            Form activeMDIChild = GuiUtils.GetActiveMDIChild(form);
            if (activeMDIChild != null)
               form = activeMDIChild;
         }

         if (form.ActiveControl != null)
         {
            MapData mapData = controlsMap.getMapData(form.ActiveControl);
            if (mapData != null)
               guiMgControl = mapData.getControl();
         }

         return guiMgControl;
      }

      /// <summary> returns the form of the menu which was activated.
      /// </summary>
      /// <param> Object menuObj
      /// </param>
      /// <returns> The form which the menu belongs to.
      /// </returns>
      private Form menuObjToForm(Object menuObj)
      {
         Form form = null;
         GuiMgForm guiMgForm;
         ControlsMap controlsMap = ControlsMap.getInstance();
         MapData mapData = controlsMap.getMapData(menuObj);
         MenuReference menuRef = mapData.getMenuReference();

         guiMgForm = menuRef.GetMgForm();

         form = GuiUtils.FindForm((Control)controlsMap.object2Widget(guiMgForm));

         return form;
      }

      /// <summary>*******************************************************************************************************
      /// **************************** DO NOT DELETE THIS CODE ****************************
      /// *********************************************************************************************** This
      /// method refreshes the enabling of internal event menu entries. It is activated when the menu is
      /// displayed.
      /// 
      /// NOTE: currently not used - we refresh all action menu entries when the action is enabled\disabled. But,
      /// we may decide to use this method instead, due to performance issues. If we decide to use this, we need
      /// to add the SWT.Show to the listeber.
      /// 
      /// </summary>
      /// <param name="widget">-
      /// on which show was received
      /// </param>
      /*
      * private void menuShow (Widget widget) { Menu menu = null; MgMenu mgMenu = null; MenuEntryMenu
      * menuEntryMenu = null; MenuEntry menuEntry = null; MgForm form = null;
      * 
      * if (widget instanceof Menu) { menu = (Menu)widget; Object data = menu.getData(); int style =
      * menu.getStyle(); MenuStyle menuStyle = (style == SWT.POP_UP ? MenuStyle.MENU_STYLE_CONTEXT :
      * MenuStyle.MENU_STYLE_PULLDOWN);
      * 
      * form = (MgForm)menu.getShell().getData(); if (data instanceof MgMenu) { mgMenu = (MgMenu) data;
      * mgMenu.refreshInternalEventMenus(form); } else if (data instanceof MenuEntryMenu) { menuEntryMenu =
      * (MenuEntryMenu)data; menuEntryMenu.refreshActionMenus(form, menuStyle); } } else if (widget instanceof
      * MenuItem) { MenuItem menuItem = (MenuItem)widget; menuEntry = (MenuEntry)menuItem.getData(); } }
      */

      /// <summary> This mdthod takes care of toolitem's prompt - we get the mouse move \ mouse exit event on the toolbar.
      /// 
      /// </summary>
      /// <param name="event">-
      /// the event which occured.
      /// </param>
      private void onItemEnterLeave(ToolStripItem menuItem, EventType type)
      {
         GuiMenuEntry guiMenuEntry = null;
         GuiMgForm guiMgForm = null;
         ControlsMap controlsMap = ControlsMap.getInstance();
         MapData mapData = controlsMap.getMapData(menuItem);
         MenuReference menuRef = mapData.getMenuReference();

         guiMgForm = menuRef.GetMgForm();

         if (type == EventType.MOUSE_ENTER)
         {
            guiMenuEntry = ((TagData)menuItem.Tag).guiMenuEntry;

            // handle tooltip
            if (menuItem is ToolStripButton)
            {
               String tooltipStrMLS = Events.Translate(guiMenuEntry.ToolTip);

               if (tooltipStrMLS == null)
                  tooltipStrMLS = "";

               menuItem.ToolTipText = tooltipStrMLS;
            }
         }

         Events.OnMenuPrompt(guiMgForm, (GuiMenuEntry)guiMenuEntry);
      }

      /// <summary> handle the context of a control
      /// 
      /// </summary>
      /// <param name="widget">:
      /// is the widget of the control \ tab \ table
      /// </param>
      /// <param name="ctrl">
      /// </param>
      public void handleContext(Control control, GuiMgControl guiMgControl, GuiMgForm guiMgForm)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();
         GuiMgMenu contextMenu = null;
         ContextMenuStrip menu = null;
         ContextMenuStrip prevMenu = control.ContextMenuStrip;
         GuiMgForm controlsForm = guiMgForm;
         

         if (guiMgControl != null)
         {
            // save the form that holds the control. 
            if (guiMgControl.isSubform())
               controlsForm = guiMgForm;
            else
               controlsForm = guiMgControl.GuiMgForm;

            contextMenu = Events.OnGetContextMenu(guiMgControl);
            
            Form form = GuiUtils.FindForm(control);
            MapData mapData = controlsMap.getFormMapData(form);
            guiMgForm = mapData.getForm();            
         }
         else
            contextMenu = Events.OnGetContextMenu(guiMgForm);

         if (contextMenu != null)
         {
            MenuReference menuRefernce = contextMenu.getInstantiatedMenu(guiMgForm, MenuStyle.MENU_STYLE_CONTEXT);
            menu = (ContextMenuStrip)controlsMap.object2Widget(menuRefernce);
         }

         
         // Fix bug#:927653, problem #2, when set context menu to the control need to refresh the event on the context menu
         // because on MgForm :instatiatedMenus is one per form per style (the style is the key) and it keeps only the menu 
         // created for the last child of the form.
         // Qcr #909188 : Use the controlsForm to refresh menus action and not the 'form' which might be topmost.
         // The reason is that the form sent to OnRefreshMenuActions has to point to the task that holds the
         // control because it holds the relevant 'Action manager'. An action enabled for the control , might be disabled
         // in the top most form's task and be wrongly disabled in the context menu.
         if (controlsForm != null)
            Events.OnRefreshMenuActions(contextMenu, controlsForm);

         if (menu != prevMenu)
         {
            GuiUtils.setContextMenu(control, menu);
            //Save the control on which context menu is invoked. This is required later for creating dummy context menu.
            if (menu != null)
               ((TagData)menu.Tag).MouseDownOnControl = control;
         }

      }

      /// <summary>
      ///       
      /// </summary>
      /// <param name="Item"></param>
      /// <returns></returns>
      /*       private static Form GetOwnerForm(ToolStripItem Item)
             {
                if (null != Item.Owner)
                {
                   if (Item.Owner is ContextMenuStrip)
                      return ((TagData)(((ContextMenuStrip)Item.Owner).Tag)).ContextMenuForm;
                   else if (null != Item.Owner.FindForm())
                   {
                      return Item.Owner.FindForm();
                   }
                   return GetOwnerForm(((ToolStripDropDown)Item.Owner).OwnerItem);
                }
                return null;
             }*/
   }
}
