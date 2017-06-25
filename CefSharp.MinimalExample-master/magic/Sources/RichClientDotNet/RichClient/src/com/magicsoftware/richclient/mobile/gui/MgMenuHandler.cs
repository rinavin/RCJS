using System;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.util;
using MenuItem = com.magicsoftware.controls.MgMenu.MgMenuItem;
using ContextMenu = com.magicsoftware.controls.MgMenu.MgContextMenu;

namespace com.magicsoftware.unipaas.gui.low
{
   internal class MgMenuHandler : HandlerBase
   {
      private static MgMenuHandler _instance;
      internal static MgMenuHandler getInstance()
      {
         if (_instance == null)
            _instance = new MgMenuHandler();

         return _instance;
      }

      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         MapData mapData;
         GuiMgForm guiMgForm = null;
         ControlsMap controlsMap;

         switch (type)
         {
            case EventType.MENU_OPENING:
               // only if the context itself is opening
               // the purpose is to determine the correct menu to be opened on the control. (substitude to the SWT.MENU_DETECT).
               if (sender is ContextMenu && !((TagData) ((ContextMenu) sender).Tag).ContextCanOpen)
               {
                  ContextMenu contextMenu = (ContextMenu) sender;
                  Control control = contextMenu.SourceControl;
                  controlsMap = ControlsMap.getInstance();
                  if (control is Form)
                     // in case of form control, the real control is its panel
                     control = ((TagData) (control.Tag)).ClientPanel;
                  else if (control is Panel && ((TagData) (control.Tag)).ContainerTabControl != null)
                     // in case its the tab panel, the control will be the tab control
                     control = ((TagData) (control.Tag)).ContainerTabControl;

                  ContainerManager containerManager = ((TagData) (control.Tag)).ContainerManager;
                  if (containerManager is TableManager || containerManager is BasicControlsManager)
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
                        {
                           // The menu is on a column or the table header.
                           ((TableManager) containerManager).handleContextMenu(control, pt);
                        }
                        else
                           mapData = controlsMap.getMapData(control);
                     }
                  }
                  else
                     mapData = controlsMap.getMapData(control);

                  // Do not go in here if tableManager.handleContextMenu was executed.
                  if (mapData != null)
                  {
                     bool focusChanged = false;
                     GuiMgControl guiMgControl = mapData.getControl();
                     bool onMultiMark = false;
                     if (containerManager is ItemsManager)
                        onMultiMark = ((ItemsManager)containerManager).IsItemMarked(mapData.getIdx());
                     focusChanged = Events.OnBeforeContextMenu(guiMgControl, mapData.getIdx(), onMultiMark);
                     //If the focus was changed, the control must have been saved as the focussing control.
                     //So, remove it from here.
                     if (focusChanged)
                        GuiUtilsBase.removeFocusingControl(GuiUtilsBase.FindForm(control));

                     // set the correct context menu on the control
                     handleContext(control, guiMgControl, mapData.getForm());
                  }
                  // if the context has changed, it means we need to cancel the opening of the context we are in,
                  // and we should open another context that is already on the control.
                  // set its ContextCanOpen to true . open it, and then set it to false.
                  // if menu was not changed (even if the 'ContextCanOpen' is false) then we will continue with the opening.
                  // There is no need for recursive call in that case.
                  if (control.ContextMenu != null && control.ContextMenu != contextMenu)
                  {
                     ((TagData) (((ContextMenu) control.ContextMenu).Tag)).ContextCanOpen = true;
                     // show the new context in the same coordinates as the opening one.
                     Point MousePos = Control.MousePosition;
                     Point pt = control.PointToClient(new Point(MousePos.X, MousePos.Y));
                     control.ContextMenu.Show(control, pt);
                     ((TagData) (((ContextMenu) control.ContextMenu).Tag)).ContextCanOpen = false;
                     return;
                  }
               }
               return;
            case EventType.MENU_ITEM_SELECTED:
               onSelection(sender);
               break;

            case EventType.DISPOSED:
               controlsMap = ControlsMap.getInstance();
               MenuReference menuRef = null;
               GuiMenuEntry guiMenuEntry = null;

               if (sender is ContextMenu && ((ContextMenu) sender).Name == "Dummy")
                  break;


               mapData = controlsMap.getMapData(sender);
               if (mapData == null)
                  break;

               menuRef = mapData.getMenuReference();
               guiMgForm = menuRef.GetMgForm();
               if (guiMgForm == null)
                  break;
               if (sender is MenuItem)
               {
                  MenuItem menuItem = (MenuItem) sender;
                  MenuStyle menuStyle = ((TagData) menuItem.Tag).MenuStyle;
                  guiMenuEntry = ((TagData) menuItem.Tag).guiMenuEntry;
                  guiMenuEntry.removeMenuIsInstantiated(guiMgForm, menuStyle);
                  //if (menuEntry is MenuEntryMenu)
                  //   menuEntry.removeInstantiatedMenuItems(mgForm, menuStyle);
               }
               if (menuRef != null)
               {
                  Object fromMap = controlsMap.object2Widget(menuRef);
                  if (fromMap != null)
                     controlsMap.remove(menuRef);
               }
               break;
            default:
               Console.Out.WriteLine(type.ToString());
               break;
         }
      }

      /// <summary>
      ///   This method is activated when a menu was selected. It performs the needed operations in order to
      ///   translate the selected menu into the matching operation
      /// </summary>
      private void onSelection(Object sender)
      {
         //Object object_Renamed = widget.getData();
         GuiMenuEntry menuEntry = null;
         MenuItem menuItem = null;

         menuItem = (MenuItem) sender;
         menuEntry = ((TagData) (menuItem.Tag)).guiMenuEntry;
         // when a menu with check style is selected, it is automatically checked.
         // we want to check it only if it should be checked - according to the state.
         //In Compact framework it is not allowed to set "Checked" for top level menus.
         if (!(menuItem.Parent is MainMenu))
            ((MenuItem) sender).Checked = menuEntry.getChecked();

         try
         {
            GuiMgForm guiMgForm = menuObjToForm(sender);

            Events.OnMenuSelection(menuEntry, guiMgForm, false);
         }
         catch (ApplicationException e)
         {
            Misc.WriteStackTrace(e, Console.Error);
         }
      }

      /// <summary>
      ///   returns the form of the menu which was activated.
      /// </summary>
      /// <param> Object menuObj
      /// </param>
      /// <returns> The form which the menu belongs to.
      /// </returns>
      private GuiMgForm menuObjToForm(Object menuObj)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();
         MapData mapData = controlsMap.getMapData(menuObj);
         MenuReference menuRef = mapData.getMenuReference();
         GuiMgForm guiMgForm = menuRef.GetMgForm();

         return guiMgForm;
      }

      /// <summary>
      /// </summary>
      internal void addHandler(ToolBar toolbar)
      {
         toolbar.ButtonClick += MenuItemSelectedHandler;
      }

      /// <summary>
      /// </summary>
      internal void addHandler(ToolBarButton button)
      {
         button.Disposed += DisposedHandler;
      }

      /// <summary>
      /// </summary>
      internal void addHandler(Menu menu)
      {
         //SWT.Selection, SWT.Arm, SWT.Dispose, SWT.MouseExit, SWT.MouseEnter, SWT.MouseMove, SWT.Show
         menu.Disposed += DisposedHandler;

         if (menu is ContextMenu)
            ((ContextMenu) menu).Popup += MenuOpeningHandler;
      }

      /// <summary>
      /// </summary>
      /// <param name = "menuItem"></param>
      /// <param name = "IsMenu"></param>
      internal void addHandler(MenuItem menuItem, bool IsMenu)
      {
         menuItem.Disposed += DisposedHandler;

         //Older menus does not have different separator menu type. 
         if (menuItem.Text != "-") //Separator
         {
            menuItem.Disposed += DisposedHandler;
            if (IsMenu)
               menuItem.Popup += MenuOpeningHandler;
            else
               menuItem.Click += MenuItemSelectedHandler;
         }
      }

      /// <summary>
      ///   handle the context of a control
      /// </summary>
      /// <param name = "widget">:
      ///   is the widget of the control \ tab \ table
      /// </param>
      /// <param name = "ctrl">
      /// </param>
      private void handleContext(Control control, GuiMgControl guiMgControl, GuiMgForm guiMgForm)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();
         GuiMgMenu contextMenu = null;
         ContextMenu menu = null;
         ContextMenu prevMenu = (ContextMenu) control.ContextMenu;

         if (guiMgControl != null)
         {
            contextMenu = Events.OnGetContextMenu(guiMgControl);

            Form form = GuiUtilsBase.FindForm(control);
            MapData mapData = controlsMap.getFormMapData(form);
            guiMgForm = mapData.getForm();
         }
         else
            contextMenu = Events.OnGetContextMenu(guiMgForm);

         if (contextMenu != null)
         {
            MenuReference menuRefernce = contextMenu.getInstantiatedMenu(guiMgForm, MenuStyle.MENU_STYLE_CONTEXT);
            menu = (ContextMenu) controlsMap.object2Widget(menuRefernce);
         }

         if (menu != prevMenu)
            GuiUtilsBase.setContextMenu(control, menu);
      }
   }
}