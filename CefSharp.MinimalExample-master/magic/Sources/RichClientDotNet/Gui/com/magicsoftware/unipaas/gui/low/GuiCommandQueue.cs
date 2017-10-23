using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.unipaas.env;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.win32;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.dotnet;
using Controls.com.magicsoftware;
using com.magicsoftware.controls.utils;
using System.Web.Script.Serialization;

#if !PocketPC
using System.Media;
using System.Globalization;
using GroupBox = System.Windows.Forms.GroupBox;
using System.Threading;
using util.com.magicsoftware.util;
#else
using com.magicsoftware.richclient;
using com.magicsoftware.richclient.mobile.util;
using com.magicsoftware.mobilestubs;
using TextBox = com.magicsoftware.controls.MgTextBox;
using Panel = com.magicsoftware.controls.MgPanel;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using ComboBox = OpenNETCF.Windows.Forms.ComboBox2;
using MainMenu = com.magicsoftware.controls.MgMenu.MgMainMenu;
using MenuItem = com.magicsoftware.controls.MgMenu.MgMenuItem;
using ContextMenu = com.magicsoftware.controls.MgMenu.MgContextMenu;
using GroupBox = OpenNETCF.Windows.Forms.GroupBox;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.gui;
using Monitor = com.magicsoftware.richclient.mobile.util.Monitor;
using util.com.magicsoftware.util;
#endif


namespace com.magicsoftware.unipaas.gui.low
{
#if !PocketPC
    /// <summary>
    ///   Gui command queue
    /// </summary>
    internal sealed class GuiCommandQueue : GuiCommandQueueBase
    {
        private static GuiCommandQueue _instance;

        /// <summary>
        ///   basis Constructor
        /// </summary>
        private GuiCommandQueue()
        {
            init();
        }

        /// <summary>
        ///   end inner class GuiCommand
        /// </summary>
        /// <summary>singleton</summary>
        /// <returns> reference to GuiCommandQueue object</returns>
        internal static GuiCommandQueue getInstance()
        {
            if (_instance == null)
                _instance = new GuiCommandQueue();
            return _instance;
        }

        /// <summary>do not allow to clone singleton</summary>
        internal Object Clone()
        {
            throw new Exception("CloneNotSupportedException");
        }


        /// <summary>
        ///   CREATE_TOOLBAR This method creates a matching toolbar for the passed form. object is the
        ///   MgMenu::toolbar. parentObject is MgMenu.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void createToolbar(GuiCommand guiCommand)
        {
            MenuReference menuReference = (MenuReference)guiCommand.obj;
            GuiMgForm guiMgForm = (GuiMgForm)guiCommand.parentObject;

            Form form = GuiUtils.getForm(ControlsMap.getInstance().object2Widget(guiMgForm));
            ToolStripEx toolBar = new ToolStripEx();
            toolBar.ClickThrough = true;

            // TODO: For MenuBar creation and assignment is done using different commands. Shouldn't we have the same behaviro here.

            if (((TagData)form.Tag).toolBar != null)
                form.Controls.Remove(((TagData)form.Tag).toolBar);

            form.Controls.Add(toolBar);
            //find menustrip
            foreach (Control control in form.Controls)
            {
                if (control is MenuStrip)
                {
                    //QCR #779146, make sure pulldown menu always above the toolbar
                    int idx = form.Controls.IndexOf(control);
                    form.Controls.SetChildIndex(toolBar, idx);
                    break;
                }
            }

            GuiUtils.CreateTagData(toolBar);

            GuiUtils.setVisible(toolBar, false);

            // ToolBar will not be listened. Only Its items.
            MgMenuHandler.getInstance().addHandler(toolBar);

            ((TagData)form.Tag).toolBar = toolBar;
            GuiUtils.setVisible(toolBar, true);

            ControlsMap.getInstance().add(menuReference, toolBar);
        }

        /// <summary>
        /// Sets toolbar for frame window.
        /// </summary>
        /// <param name="guiCommand"></param>
        internal override void setToolBar(GuiCommand guiCommand)
        {
            ToolStrip toolBar = (ToolStrip)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            GuiMgForm guiMgForm = (GuiMgForm)guiCommand.parentObject;
            Form form = GuiUtils.getForm(ControlsMap.getInstance().object2Widget(guiMgForm));

            if (((TagData)form.Tag).toolBar != null)
                form.Controls.Remove(((TagData)form.Tag).toolBar);
            form.Controls.Add(toolBar);

            //find menustrip
            foreach (Control control in form.Controls)
            {
                if (control is MenuStrip)
                {
                    //QCR #779146, make sure pulldown menu always above the toolbar
                    int idx = form.Controls.IndexOf(control);
                    form.Controls.SetChildIndex(toolBar, idx);
                    break;
                }
            }
            ((TagData)form.Tag).toolBar = toolBar;
            GuiUtils.setVisible(toolBar, true);
        }

        /// <summary>
        ///   CREATE_TOOLBAR_ITEM This method creates a matching toolbar item for the passed menuEntry. parentObject
        ///   is the Toolbar.
        /// </summary>
        /// <param name = "guiCommand">parentObject - is the ToolBar to which we add a new item line - is the index of the new object
        ///   in the toolbar menuEntry - is the menuEntry for which we create this toolitem</param>
        internal override void createToolbarItem(GuiCommand guiCommand)
        {
            GuiMgForm form = (GuiMgForm)(guiCommand.obj);
            ToolStrip toolBar = (ToolStrip)ControlsMap.getInstance().object2Widget(guiCommand.parentObject);
            if (toolBar != null)
            {
                ToolStripItem item;

                if (guiCommand.menuEntry.menuType() == GuiMenuEntry.MenuType.SEPARATOR)
                    item = new ToolStripSeparator();
                else
                    item = new ToolStripButton();

                toolBar.Items.Insert(guiCommand.line, item);
                GuiUtils.CreateTagData(item);
                setToolItemPropsByMenuEntry(form, item, guiCommand.menuEntry);
                MgMenuHandler.getInstance().addHandler(item, false);
                MenuReference menuReference = guiCommand.menuEntry.getInstantiatedToolItem(form);
                ControlsMap.getInstance().add(menuReference, item);
            }
        }

        /// <summary>
        ///   DELETE_TOOLBAR_ITEM This method delete a matching toolbar item for the passed menuEntry. parentObject is
        ///   the Toolbar.
        /// </summary>
        /// <param name = "guiCommand">parentObject - is the ToolBar to which we add a new item line - is the index of the new object
        ///   in the toolbar menuEntry - is the menuEntry for which we create this toolitem
        /// </param>
        internal override void deleteToolbarItem(GuiCommand guiCommand)
        {
            GuiMgForm form = (GuiMgForm)(guiCommand.obj);
            ToolStrip toolBar = (ToolStrip)ControlsMap.getInstance().object2Widget(guiCommand.parentObject);
            if (toolBar != null)
            {
                MenuReference menuReference = guiCommand.menuEntry.getInstantiatedToolItem(form);
                ToolStripItem toolItem = (ToolStripItem)(ControlsMap.getInstance().object2Widget(menuReference));
                toolItem.Dispose();
            }
        }

        /// <summary>
        ///   This method will delete a toolbar.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void deleteToolbar(GuiCommand guiCommand)
        {
            ToolStrip toolBar = (ToolStrip)ControlsMap.getInstance().object2Widget(guiCommand.obj);

            if (toolBar != null)
                toolBar.Dispose();
        }

        /// <summary>
        ///   CommandType.CREATE_MENU_ITEM This method creates a menuItem object to match a specific MenuEntry as the
        ///   child of the passed parent Menu object. The leaves of the menu will be created with the SWT_CHECK style
        ///   in order to allow checking and unchecking them (for MnuCheck function).It means the menu itself will be
        ///   a little wider.
        /// </summary>
        /// <menuEntry>  MenuEntry object we mean to create a matching menu to. </menuEntry>
        /// <parentMenu>  a menuItem for which we will add the new menu as child </parentMenu>
        /// <menuStyle>  style of the menu to create </menuStyle>
        /// <parentIsMenu>  - the parent object is a Menu - otherwise it is a MenuItem </parentIsMenu>
        internal override void createMenuItem(GuiMenuEntry menuEntry, Object parentMenu, MenuStyle menuStyle,
            bool parentIsMenu, int index, GuiMgForm form)
        {
            ToolStripItem menuItem;
            int location;
            int count;

            if (parentMenu is MenuReference)
            {
                // We will be here when this method is called from Manager.CrateWindowListMenuEntry.
                Object obj = ControlsMap.getInstance().object2Widget(parentMenu);
                parentMenu = obj;
            }

            if (parentMenu is ToolStrip)
                count = ((ToolStrip)parentMenu).Items.Count;
            else
                count = ((ToolStripMenuItem)parentMenu).DropDownItems.Count;

            location = (index <= count
                ? index
                : count + 1);

            try
            {
                if (menuEntry.menuType() == GuiMenuEntry.MenuType.MENU)
                    menuItem = new MgToolStripMenuItem();
                else if (menuEntry.menuType() == GuiMenuEntry.MenuType.SEPARATOR)
                    menuItem = new ToolStripSeparator();
                else
                    menuItem = new MgToolStripMenuItem();

                // set the menu data according to the MenuEntry entry
                setMenuItemPropsByMenuEntry(form, menuItem, menuEntry, menuStyle);

                if (parentMenu is ToolStrip)
                    ((ToolStrip)parentMenu).Items.Insert(location - 1, menuItem);
                else
                    ((ToolStripMenuItem)parentMenu).DropDownItems.Insert(location - 1, menuItem);

                // save a reference from the menuItem object to the MenuEntry object
                MenuReference menuItemReference = menuEntry.getInstantiatedMenu(form, menuStyle);
                if (menuItemReference != null)
                    ControlsMap.getInstance().add(menuItemReference, menuItem);
                else
                    Debug.Assert(menuItemReference != null);

                // Add handlers
                MgMenuHandler.getInstance().addHandler(menuItem, menuEntry.menuType() == GuiMenuEntry.MenuType.MENU);
            }
            catch (Exception e)
            {
                Misc.WriteStackTrace(e, System.Console.Error);
            }
        }

        internal override void deleteMenuItem(GuiCommand guiCommand)
        {
            Object obj;
            MenuReference mnuRef = (MenuReference)guiCommand.obj;
            obj = ControlsMap.getInstance().object2Widget(mnuRef);

            if (obj is ToolStripItem)
            {
                ToolStripItem menuItem = (ToolStripItem)obj;
                menuItem.Dispose();
            }
            if (obj is ToolStrip)
            {
                ToolStrip menu = (ToolStrip)obj;
                foreach (ToolStrip item in menu.Items)
                    item.Dispose();

                menu.Dispose();
            }
        }

        /// <summary></summary>
        /// <param name = "guiCommand"></param>
        internal override void deleteMenu(GuiCommand guiCommand)
        {
            GuiMgForm guiMgForm = (GuiMgForm)guiCommand.obj;
            MenuReference menuRefernce = guiCommand.menu.getInstantiatedMenu(guiMgForm, guiCommand.menuStyle);
            ToolStrip menu = (ToolStrip)ControlsMap.getInstance().object2Widget(menuRefernce);

            if (guiCommand.Bool3)
            {
                Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);
                Form form = GuiUtils.getForm(obj);

                if (guiCommand.menuStyle == MenuStyle.MENU_STYLE_PULLDOWN || guiCommand.menuStyle == MenuStyle.MENU_STYLE_CONTEXT)
                    menu.Dispose(); // Dispose the menubar.
            }
        }

        /// <summary>
        /// Show the menus if we have at lest one menu and hide if we don't have any items.
        /// We also hide the menuBar and Toolbar if the all the menu items are invisible
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void updateMenuVisibility(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            Form form = GuiUtils.getForm(control);

            MgFormBase mgForm = (MgFormBase)guiCommand.obj;
            MgMenu pullDownMenu = mgForm.getPulldownMenu();
            MenuReference menuRef = pullDownMenu.getInstantiatedMenu(mgForm, MenuStyle.MENU_STYLE_PULLDOWN);
            ToolStrip menuBar = (ToolStrip)ControlsMap.getInstance().object2Widget(menuRef);

            bool isAnyMenuItemVisible = false;

            for (int i = 0; i < menuBar.Items.Count; i++)
            {
                ToolStripItem item = menuBar.Items[i];
                //We are checking the Available property to check Visibility as it is found that the Visible property returns false in this case as the parent form is not yet visible.
                isAnyMenuItemVisible = isAnyMenuItemVisible || item.Available;

                if (isAnyMenuItemVisible)
                    break;
            }

            //Here we handle the Visibility of Menu bar
            if (form.MainMenuStrip != null)
            {
                GuiUtils.setVisible(form.MainMenuStrip, isAnyMenuItemVisible);
            }

            //Here we handle the Visibility of Toolbar
            Object toolbarMenu = pullDownMenu.getToolbar(mgForm);
            if (toolbarMenu != null)
            {
                ToolStrip toolBar = (ToolStrip)ControlsMap.getInstance().object2Widget(toolbarMenu);
                bool isAnyToolbarItemVisible = false;//This flag is used to handle the Visibility of the Toolbar
                bool isAnyToolbarItemVisibleInGroup = false;//This flag is used to handle the Visibility of the ToolStripSeparator of a group in Toolbar

                //We are not breaking this loop whenever anyToolbarItem is Visible because we are handling the visibility of the seperators as well.
                for (int i = 0; i < toolBar.Items.Count; i++)
                {
                    ToolStripItem item = toolBar.Items[i];
                    //We are checking the Available property to check Visibility as it is found that the Visible property returns false in this case as the parent form is not yet visible.
                    if (!(item is ToolStripSeparator)) //The toolbar contains toolStripButtons and toolStripSeperators.
                    {
                        isAnyToolbarItemVisible = isAnyToolbarItemVisible || item.Available;
                        isAnyToolbarItemVisibleInGroup = isAnyToolbarItemVisibleInGroup || item.Available;
                    }
                    else //Here we are handling the visiblity of the ToolStripSeperator
                    {
                        //We should show the ToolStripSeperator only when atleast one item from the group is Visible
                        item.Visible = isAnyToolbarItemVisibleInGroup;
                        isAnyToolbarItemVisibleInGroup = false;
                    }
                }

                if (toolBar != null)
                {
                    GuiUtils.setVisible(toolBar, isAnyToolbarItemVisible);
                }
            }
        }

        /// <summary>
        ///   CommandType.CREATE_MENU This method creates an .Net menu for the passed MenuEntry object.
        /// </summary>
        internal override void createMenu(GuiCommand guiCommand)
        {
            ToolStrip parentMenu = null;
            GuiMgForm guiMgForm = null;
            MenuReference menuReference = null;

            guiMgForm = (GuiMgForm)guiCommand.obj;
            bool shouldShowPullDownMenu = guiCommand.Bool1;

            Boolean createTagData = true;
            // same to form and subform 
            if (guiCommand.menuStyle == MenuStyle.MENU_STYLE_CONTEXT)
                parentMenu = new ContextMenuStrip();
            else
            {
                parentMenu = new MenuStrip();
                //Story 126642 : If the number of menus are larger than the window can occupy, we use Overflow property and let system handle the displaying of menus
                parentMenu.CanOverflow = true;
                createTagData = false;
                GuiUtils.CreateTagData(parentMenu);
                if (!shouldShowPullDownMenu)
                    GuiUtils.setVisible(parentMenu, false);
            }
            if (createTagData)
                GuiUtils.CreateTagData(parentMenu);

            // Only diff btw form and subform is what we send as mgForm in the getInstantiatedMenu.
            menuReference = guiCommand.menu.getInstantiatedMenu(guiMgForm, guiCommand.menuStyle);

            ((TagData)parentMenu.Tag).GuiMgMenu = guiCommand.menu;
            ((TagData)parentMenu.Tag).MenuStyle = guiCommand.menuStyle;
            ((TagData)parentMenu.Tag).ContextCanOpen = false;

            // add the new menu to the map
            ControlsMap.getInstance().add(menuReference, parentMenu);
            MgMenuHandler.getInstance().addHandler(parentMenu);
        }

        /// <summary>
        ///   This method sets the object's menu. When this command arrives the menu object already exists, since we
        ///   take care of it in the MenuManager::GetMenu() method.
        /// </summary>
        /// <commandType>  == PROP_SET_MENU </commandType>
        /// <object>  form / control on which we set the menu </object>
        /// <style>  menu style (PROP_TYPE_PULLDOWN_MENU , PROP_TYPE_CONTEXT_MENU) </style>
        /// <MenuEntry>  by which to set the menu </MenuEntry>
        /// <parentTypeForm>  is the object form or control </parentTypeForm>
        internal override void setMenu(GuiCommand guiCommand)
        {
            try
            {
                GuiMgForm guiMgForm = (GuiMgForm)guiCommand.obj;
                MenuReference menuRefernce = guiCommand.menu.getInstantiatedMenu(guiMgForm, guiCommand.menuStyle);
                ToolStrip menu = (ToolStrip)ControlsMap.getInstance().object2Widget(menuRefernce);

                GuiMgControl guiMgControl = null;
                if (guiCommand.Bool3) // parentTypeForm
                {
                    Object obj = ControlsMap.getInstance().object2Widget(guiCommand.parentObject);
                    Form form = GuiUtils.getForm(obj);

                    if (guiCommand.menuStyle == MenuStyle.MENU_STYLE_PULLDOWN)
                    {
                        // set pull down menu
                        if (form.MainMenuStrip != null)
                            form.Controls.Remove(form.MainMenuStrip);

                        form.Controls.Add(menu);
                        form.MainMenuStrip = (MenuStrip)menu;
                    }
                    else
                    {
                        // set context menu
                        form.ContextMenuStrip = (ContextMenuStrip)menu;
                    }
                }
                else
                {
                    guiMgControl = (GuiMgControl)guiCommand.parentObject;
                    ArrayList arrayControl = ControlsMap.getInstance().object2WidgetArray(guiMgControl, guiCommand.line);
                    for (int i = 0;
                        i < arrayControl.Count;
                        i++)
                    {
                        Object control = arrayControl[i];
                        if (control is TableControl)
                            GuiUtils.getTableManager((TableControl)control).setContextMenu(menu);
                        else if (control is LogicalControl)
                        {
                            Control EditorControl = null;
                            ((LogicalControl)control).ContextMenu = (ContextMenuStrip)menu;
                            EditorControl = ((LogicalControl)control).getEditorControl();
                            if (EditorControl != null)
                                GuiUtils.setContextMenu(EditorControl, menu);
                        }
                        else if (control is Control)
                            GuiUtils.setContextMenu((Control)control, menu);
                    }
                }
            }
            catch (Exception e)
            {
                Misc.WriteStackTrace(e, System.Console.Error);
                throw e;
            }
        }

        /// <summary>
        ///   This method clears the menu of the object. Since we reuse menu objects, we do not want to dispose thw
        ///   menu object. So, we create a dummy menu, set it on the object and dispose of it
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void resetMenu(GuiCommand guiCommand)
        {
            ToolStrip menu;
            ToolStrip dummyMenu = null;
            try
            {
                if (guiCommand.Bool3)
                    // parentTypeForm
                {
                    Object obj = ControlsMap.getInstance().object2Widget(guiCommand.parentObject);
                    Form form = GuiUtils.getForm(obj);

                    if (form != null)
                    {
                        if (guiCommand.menuStyle == MenuStyle.MENU_STYLE_PULLDOWN)
                        {
                            // get pull down menu
                            menu = form.MainMenuStrip;
                            if (menu != null)
                            {
                                dummyMenu = new MenuStrip();
                                form.MainMenuStrip = (MenuStrip)dummyMenu;
                            }
                        }
                        else
                        {
                            // get context menu
                            menu = form.ContextMenuStrip;
                            if (menu != null)
                            {
                                GuiUtils.setContextMenu(form, null);
                                // dummyMenu = new ContextMenuStrip();
                                // form.ContextMenuStrip = (ContextMenuStrip)dummyMenu;
                            }
                        }

                        if (menu != null)
                        {
                            menu.Dispose();

                            if (guiCommand.menuStyle == MenuStyle.MENU_STYLE_PULLDOWN)
                            {
                                // remove all the tool items since we destroy the menu
                                // we do not destroy the toolbar itself in order to be able to add other tool items
                                // to it when a new menu will be set on the form
                                ToolStrip toolbar = ((TagData)(form.Tag)).toolBar;
                                if (toolbar != null)
                                {
                                    GuiUtils.setVisible(toolbar, false);

                                    foreach (ToolStripItem item in toolbar.Items)
                                    {
                                        item.Dispose();
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Object obj = ControlsMap.getInstance().object2Widget(guiCommand.parentObject);
                    if (obj != null && obj is Control)
                    {
                        menu = ((Control)obj).ContextMenuStrip;
                        if (menu != null)
                            GuiUtils.setContextMenu((Control)obj, null);
                    }
                    Debug.Assert(obj != null);
                }
            }
            catch (Exception e)
            {
                Misc.WriteStackTrace(e, System.Console.Error);
            }
        }

        /// <summary>
        /// Register DN control value changed event.
        /// </summary>
        /// <param name="guiCommand"></param>
        internal override void RegisterDNControlValueChangedEvent(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);
            ReflectionServices.AddDNControlValueChangedHandler(obj, guiCommand.str);
        }

        /// <summary>
        ///   This method updates a passed menu object by the sent MenuEntry information text, accelerator, image and
        ///   so on.
        /// </summary>
        private void setMenuItemPropsByMenuEntry(GuiMgForm guiMgForm, ToolStripItem menuItem, GuiMenuEntry menuEntry, MenuStyle menuStyle)
        {
            if (menuEntry.menuType() != GuiMenuEntry.MenuType.SEPARATOR)
            {
                setMenuItemText((ToolStripMenuItem)menuItem, menuEntry);
                if (menuEntry.Imagefor == GuiMenuEntry.ImageFor.MENU_IMAGE_BOTH ||
                    menuEntry.Imagefor == GuiMenuEntry.ImageFor.MENU_IMAGE_MENU)
                {
                    Image image = getImage(menuEntry);
                    menuItem.Image = image;
                }
                ((ToolStripMenuItem)menuItem).Checked = menuEntry.getChecked();
                ((ToolStripMenuItem)menuItem).Enabled = menuEntry.getEnabled();
                ((ToolStripMenuItem)menuItem).Visible = menuEntry.getVisible() ? menuEntry.CheckIfParentItemVisible(menuEntry) : false;
            }

            GuiUtils.CreateTagData(menuItem);
            ((TagData)menuItem.Tag).guiMenuEntry = menuEntry;
            ((TagData)menuItem.Tag).MenuStyle = menuStyle;
        }

        /// <summary>
        ///   This method updates a passed menu object by the sent MenuEntry information text, accelerator, image andso on.
        /// </summary>
        private void setToolItemPropsByMenuEntry(GuiMgForm guiMgform, ToolStripItem toolItem, GuiMenuEntry menuEntry)
        {
            if (toolItem is ToolStripButton)
            {
                Image image = getImage(menuEntry);
                ((ToolStripButton)toolItem).Image = image;
                ((ToolStripButton)toolItem).DisplayStyle = ToolStripItemDisplayStyle.Image;
                ((ToolStripButton)toolItem).Text = menuEntry.TextMLS;
                ((ToolStripButton)toolItem).Checked = menuEntry.getChecked();
                ((ToolStripButton)toolItem).Visible = menuEntry.getVisible() ? menuEntry.CheckIfParentItemVisible(menuEntry) : false;


                // In case the tooltip is null, the tooltip is automatically taken from the Text.
                // The advantage in this is that if the Text has '&' in it, the tooltip will not show it. 
                if (menuEntry.ToolTipMLS != null)
                    ((ToolStripButton)toolItem).ToolTipText = menuEntry.ToolTipMLS;
            }
            ((TagData)toolItem.Tag).guiMenuEntry = menuEntry;
            ((TagData)toolItem.Tag).MenuStyle = MenuStyle.MENU_STYLE_TOOLBAR;
            toolItem.Enabled = menuEntry.getEnabled();
            toolItem.Visible = menuEntry.getVisible() ? menuEntry.CheckIfParentItemVisible(menuEntry) : false;
        }

        /// <summary>
        ///   This method creates an Image object for the passed MenuEntry object it will be created from the image
        ///   file or from the toolbar file + image number. In case the image file property is set, we will ask the
        ///   ImageCache for the image file.
        /// </summary>
        internal new Image getImage(GuiMenuEntry menuEntry)
        {
            Image toolImage = null;

            if (menuEntry.ImageNumber > 0)
            {
                ToolImages toolImages = ToolImages.getInstance();
                toolImage = toolImages.getToolImage(menuEntry.ImageNumber - 1);
            }
            else if (menuEntry.ImageFile != null)
            {
                Image orgImage = ImageLoader.GetImage(menuEntry.ImageFile);
                if (orgImage != null)
                {
                    toolImage = new Bitmap(orgImage);
                    ((Bitmap)toolImage).MakeTransparent(Color.FromArgb(192, 192, 192));
                }
            }

            return toolImage;
        }

        /// <summary>
        ///   execute layout on the form
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void executeLayout(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);

            // Perform layout only for actual controls. 
            // For logical controls, it will be handled by setSpecificControlProperties() of a Logical Control
            Control control = obj as Control;
            if (control != null)
            {

                if (control.Parent is Form)
                    control.Parent.PerformLayout(control.Parent, "");
                ((Control)obj).PerformLayout((Control)obj, "");
            }
        }

        /// <summary>
        ///   Sets the startup position
        /// </summary>
        internal override void setStartupPosition(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            Form form = GuiUtils.getForm(obj);
            WindowPosition mgWindowPosition = (WindowPosition)guiCommand.number;

            form.StartPosition = GuiUtils.WindowPosition2StartupPosition(mgWindowPosition);
        }

        /// <summary>
        /// </summary>
        internal override void setVisibleLines(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            if (obj is MgComboBox)
                GuiUtils.setVisibleLines((ComboBox)obj, guiCommand.number, ((MgComboBox)obj).Font);
            else if (obj is LgCombo)
                ((LgCombo)obj).setVisibleItemsCount(guiCommand.number);
            else
                throw new ApplicationException("in GuiCommandQueue.setVisibleLines()");
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setMenuEnable(GuiCommand guiCommand)
        {
            MenuReference menuItemReference = (MenuReference)guiCommand.obj;
            if (menuItemReference != null)
            {
                Object menuItem = ControlsMap.getInstance().object2Widget(menuItemReference);
                if (menuItem is ToolStripMenuItem)
                {
                    foreach (ToolStripMenuItem item in ((ToolStripMenuItem)menuItem).DropDownItems)
                    {
                        item.Enabled = guiCommand.Bool3;
                    }
                    ((ToolStripMenuItem)menuItem).Enabled = guiCommand.Bool3;
                }
                else if (menuItem is ToolStrip)
                {
                    foreach (ToolStripItem item in ((ToolStrip)menuItem).Items)
                    {
                        item.Enabled = guiCommand.Bool3;
                    }
                    ((ToolStrip)menuItem).Enabled = guiCommand.Bool3;
                }
            }
        }

        /// <summary>
        ///   set default button on form
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setDefaultButton(GuiCommand guiCommand)
        {
            Control ctrl = (Control)ControlsMap.getInstance().object2Widget(guiCommand.parentObject, 0);
            // for subforms get the ancestor shell and set the DefaultButton for it
            Control parentComposite = GuiUtils.FindForm(ctrl);

            if (parentComposite is Form)
            {
                if (((Form)parentComposite).AcceptButton != null)
                    resetDrawAsDefaultButtonAndRefresh(parentComposite);

                if (guiCommand.obj == null)
                    ((Form)parentComposite).AcceptButton = null;
                else
                {
                    Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
                    if (obj is LogicalControl)
                        obj = ((LogicalControl)obj).getEditorControl();

                    if (obj is Control)
                    {
                        Control control = (Control)obj;
                        if (control is MgButtonBase)
                            ((Form)parentComposite).AcceptButton = ((MgButtonBase)control);
                        else if (control is MgLinkLabel)
                            ((Form)parentComposite).AcceptButton = ((MgLinkLabel)control);
                        else
                            throw new ApplicationException(
                                "in GuiCommandQueue.setDefaultButton(): the control isn't a MgButton control");

                        resetDrawAsDefaultButtonAndRefresh(parentComposite);
                    }
                }
            }
        }

        /// <summary>
        ///   reset DrawAsDefaultButton And Refresh
        /// </summary>
        /// <param name = "parentComposite"></param>
        private static void resetDrawAsDefaultButtonAndRefresh(Control parentComposite)
        {
            Control accButton = (Control)(((Form)parentComposite).AcceptButton);
            if ((TagData)accButton.Tag != null)
            {
                TagData tg = (TagData)accButton.Tag;
                tg.DrawAsDefaultButton = false;
                GuiUtils.RefreshButtonImage(accButton);
            }
        }

        /// <summary>
        ///   set tootip timeout 
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setEnvTooltipTimeout(GuiCommand guiCommand)
        {
            GuiUtils.setTooltipTimeout((int)guiCommand.obj1);
        }

        /// <summary>
        /// </summary>
        internal override void setTooltip(GuiCommand guiCommand)
        {
            ArrayList arrayControl = ControlsMap.getInstance().object2WidgetArray(guiCommand.obj, guiCommand.line);

            for (int i = 0;
                i < arrayControl.Count;
                i++)
            {
                Object control = arrayControl[i];

                if (control is LogicalControl)
                    ((LogicalControl)control).Tooltip = guiCommand.str;
                else if (control is ToolStripLabel)
                    GuiUtils.setTooltip((ToolStripLabel)control, guiCommand.str);
                else if (control is Control)
                {
                    ((TagData)((Control)control).Tag).Tooltip = guiCommand.str;

                    if (control is TabControl)
                    {
                        Panel tabControlPanel = ((TagData)((Control)control).Tag).TabControlPanel;
                        ((TagData)(tabControlPanel.Tag)).Tooltip = guiCommand.str;
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name = "object"></param>
        /// <param name = "sentSize"></param>
        /// <param name = "updateMinSizeInfo"></param>
        internal override void setMinSize(Control control, int sentSize, bool updateMinSizeInfo, bool setWidth)
        {
            Control parent = control.Parent;

            // 1. Create (if needed) and update the MinSizeInfo on this object.
            MinSizeInfo msi = GuiUtils.getMinSizeInfo(control);
            if (updateMinSizeInfo)
            {
                Point pt = new Point(setWidth
                    ? sentSize
                    : GuiConstants.DEFAULT_VALUE_INT, !setWidth
                    ? sentSize
                    : GuiConstants.DEFAULT_VALUE_INT);
                GuiUtils.updateFrameSize(ref pt, true);
                if (setWidth)
                    msi.MinWidth = pt.X;
                else
                    msi.MinHeight = pt.Y;
            }

            // The recursion stops when the form is reached or when the frameset is opened inside a subform.
            bool isFramesetInSubForm = false;
            if (control is MgSplitContainer && (parent is MgPanel))
                isFramesetInSubForm = GuiUtils.IsSubformPanel((MgPanel)parent);

            if (!(control is GuiForm) && !isFramesetInSubForm)
            {
                // 2.Add this MinSizeInfo to the parent childrenMinSizeInfo (if not there already)
                MinSizeInfo msiParent = GuiUtils.getMinSizeInfo(parent);
                msiParent.addChildMinSizeInfo(control, msi);

                // 3. Call setMinSize() for the parent object recursively. (when calling recursively send a parameter
                // that tells the callee to use its old min size value)
                setMinSize(parent, 0, false, setWidth);
            }
        }

        /// <summary>
        /// </summary>
        internal override void setIconFileName(GuiCommand guiCommand)
        {
            var control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            Form form = GuiUtilsBase.FindForm(control);
            form.Icon = IconsCache.GetInstance().Get(guiCommand.fileName);
        }

        /// <summary>
        ///   for table control show the lines or not
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setLinesVisible(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (obj is TreeView)
                ((TreeView)obj).ShowLines = guiCommand.Bool3;
        }

        /// <summary>
        ///   set the statusBar Pane width.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setSBPaneWidth(GuiCommand guiCommand)
        {
            ToolStripItem statusPane = (ToolStripItem)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            int width = guiCommand.width;

            if (width == 0)
            {
                if (statusPane is ToolStripStatusLabel)
                    ((ToolStripStatusLabel)statusPane).Spring = true;
            }
            else
                //In case of DPI aware is true then we need to consider scaling factor according to the current resolution.
                //So, get the scaling factor of ToolStripStatusLabel's Parent because ToolStripStatusLabel is not control.
                //We need control to get graphics which consists the scaling factor(graphics.dpiX, graphics.dpiY).
                statusPane.Width = (int)((float)guiCommand.width * ((float)Utils.GetDpiScaleRatioX(statusPane.GetCurrentParent())));
        }

        /// <summary>
        ///   create a StatusBar Pane
        /// </summary>
        /// <param name = "mgControl"></param>
        /// <returns></returns>
        internal override void createSBPane(GuiCommand guiCommand)
        {
            StatusStrip statusBar = (StatusStrip)ControlsMap.getInstance().object2Widget(guiCommand.parentObject);
            Object obj = GuiUtils.createSBPane(statusBar);

            ControlsMap.getInstance().add(guiCommand.obj, obj);
        }

        /// <summary>
        ///   create tree node
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void createTreeNode(GuiCommand guiCommand)
        {
            TreeView tree = (TreeView)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (tree != null)
                GuiUtils.getTreeManager(tree).createNode(guiCommand.number, guiCommand.number1, guiCommand.line);
        }

        /// <summary>
        ///   create tree node
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void moveTreeNode(GuiCommand guiCommand)
        {
            TreeView tree = (TreeView)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (tree != null)
                GuiUtils.getTreeManager(tree).moveNode(guiCommand.number, guiCommand.number1, guiCommand.line);
        }

        /// <summary>
        ///   create tree node
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setExpanded(GuiCommand guiCommand)
        {
            TreeChild treeChild = (TreeChild)ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            treeChild.setExpand(guiCommand.Bool3);
        }

        /// <summary>
        ///   set children retrieved flag
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setChildrenRetrieved(GuiCommand guiCommand)
        {
            TreeChild treeChild = (TreeChild)ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            treeChild.setChildrenRetrieved(guiCommand.Bool3);
        }

        /// <summary>
        ///   delete tree node
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void deleteTreeNode(GuiCommand guiCommand)
        {
            TreeChild treeChild = (TreeChild)ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            treeChild.Dispose();
        }

        /// <summary>
        ///   show full Row
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void showFullRow(GuiCommand guiCommand)
        {
            TreeView tree = (TreeView)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (tree != null)
                tree.FullRowSelect = guiCommand.Bool3;
        }

        /// <summary>
        ///   show buttons
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void showButtons(GuiCommand guiCommand)
        {
            TreeView tree = (TreeView)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            tree.ShowPlusMinus = guiCommand.Bool3;
        }

        /// <summary>
        ///   show buttons
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void hotTrack(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (control is TreeView)
                ((TreeView)control).HotTracking = guiCommand.Bool3;
            else if (control is TabControl)
                ((TabControl)control).HotTrack = guiCommand.Bool3;
        }

        /// <summary>
        ///   show buttons
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void linesAtRoot(GuiCommand guiCommand)
        {
            TreeView tree = (TreeView)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            tree.ShowRootLines = guiCommand.Bool3;
        }

        /// <summary>
        ///   set the horizontal placement on the sub form
        /// </summary>
        internal override void setHorizontalPlacement(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (GuiUtils.shouldHaveMgSplitContainerData(control))
            {
                MgSplitContainerData mgSplitContainerData = GuiUtils.getMgSplitContainerData(control);
                mgSplitContainerData.allowHorPlacement = guiCommand.Bool3;
            }
        }

        /// <summary>
        ///   set the vertical placement on the sub form
        /// </summary>
        internal override void setVerticalPlacement(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (GuiUtils.shouldHaveMgSplitContainerData(control))
            {
                MgSplitContainerData mgSplitContainerData = GuiUtils.getMgSplitContainerData(control);
                mgSplitContainerData.allowVerPlacement = guiCommand.Bool3;
            }
        }

        /// <summary>
        ///   show temporary editor
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void showTmpEditor(GuiCommand guiCommand)
        {
            TreeChild treeChild = (TreeChild)ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            treeChild.TreeManager.showTmpEditor(treeChild);
        }

        /// <summary>
        ///   order the children of splitter container
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void orderMgSplitterContainerChildren(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);

            if (obj is MgSplitContainer)
                ((MgSplitContainer)obj).orderChildren();
            else
                Debug.Assert(false);
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setTabSizeMode(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (control is MgTabControl)
                ((MgTabControl)control).TabsWidth = (TabControlTabsWidth)guiCommand.number;
            else
                Debug.Assert(false);
        }

        /// <summary>
        ///   expanded image index
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setExpandedImageIdx(GuiCommand guiCommand)
        {
            TreeChild treechild = (TreeChild)ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            treechild.ExpandedImageIdx = guiCommand.number;
        }

        /// <summary>
        ///   collapsed image index
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setCollapsedImageIdx(GuiCommand guiCommand)
        {
            TreeChild treechild = (TreeChild)ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            treechild.CollapsedImageIdx = guiCommand.number;
        }

        /// <summary>
        ///   parked collapsed image index
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setParkedCollapsedImageIdx(GuiCommand guiCommand)
        {
            TreeChild treechild = (TreeChild)ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            treechild.ParkedCollapsedImageIdx = guiCommand.number;
        }

        /// <summary>
        ///   parked expanded image index
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setParkedExpandedImageIdx(GuiCommand guiCommand)
        {
            TreeChild treechild = (TreeChild)ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            treechild.ParkedExpanedImageIdx = guiCommand.number;
        }

        /// <summary>
        ///   allow update of the tree
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void allowUpdate(GuiCommand guiCommand)
        {
            TreeView tv = (TreeView)ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            GuiUtils.getTreeManager(tv).allowUpdate(guiCommand.Bool3);
        }


        /// <summary>
        ///   set image list
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setImageListIndexes(GuiCommand guiCommand)
        {
            MgTabControl tab = (MgTabControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            int[] imageListIndexes = guiCommand.intArray;
            for (int i = 0;
                i < imageListIndexes.Length && i < tab.TabPages.Count;
                i++)
            {
                tab.TabPages[i].ImageIndex = imageListIndexes[i] - 1;
            }

            int width = 0;
            if (tab.ImageList != null && imageListIndexes.Length > 0)
                width = tab.ImageList.ImageSize.Width;
            tab.IconWidth = width;
        }

        /// <summary>
        ///   set activate keybord layout
        ///   if bool1(restoreLang) then restore the lang from the tagData, otherwise set heb if needed.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void activateKeyboardLayout(GuiCommand guiCommand)
        {
            if (guiCommand.Bool1) // need to restoreLang(the prev lang)
            {
                // fixed defect#:122063:
                // in ver 2 in c++ the default keyboard was Eng , we need to set it also for runtime.
                // the Application.CurrentInputLanguage = null is return the default lag that define on control panel\Lang and its not correct for us.
                // Application.CurrentInputLanguage = null;
                // this defect Only happens when the default language is Hebrew (i.e. - in 2.x and 3.0, on non H fields, the default language is used and in 1.9 English language is used.)

                Application.CurrentInputLanguage = InputLanguage.FromCulture(new CultureInfo("en-US"));
            }
            else
            {
                if (guiCommand.Bool3) // set heb lang
                    Application.CurrentInputLanguage = InputLanguage.FromCulture(new CultureInfo("he-IL"));
            }
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setAlignment(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);

            if (control is RichTextBox)
            {
                RichTextBox rtb = (RichTextBox)control;
                rtb.SelectionAlignment = ControlUtils.HorAlign2HorAlign((AlignmentTypeHori)guiCommand.number);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setBullet(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);

            if (control is RichTextBox)
            {
                RichTextBox rtb = (RichTextBox)control;
                rtb.SelectionBullet = !rtb.SelectionBullet;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setIndent(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);

            if (control is RichTextBox)
            {
                RichTextBox rtb = (RichTextBox)control;
                rtb.SelectionIndent += 20;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setUnindent(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);

            if (control is RichTextBox)
            {
                RichTextBox rtb = (RichTextBox)control;
                rtb.SelectionIndent -= 20;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void ChangeColor(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);

            if (control is RichTextBox)
                GuiUtils.changeColor((RichTextBox)control);
            else
                Debug.Assert(false);
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void ChangeFont(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);

            if (control is RichTextBox)
                GuiUtils.changeFont((RichTextBox)control);
            else
                Debug.Assert(false);
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void setWindowState(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            Form form = GuiUtils.FindForm(control);

            switch (guiCommand.style)
            {
                case Styles.WINDOW_STATE_MAXIMIZE:
                    form.WindowState = FormWindowState.Maximized;
                    break;

                case Styles.WINDOW_STATE_MINIMIZE:
                    form.WindowState = FormWindowState.Minimized;
                    break;

                case Styles.WINDOW_STATE_RESTORE:
                    form.WindowState = FormWindowState.Normal;
                    break;

                default:
                    break;
            }

            GuiUtils.setLastWindowState(form);
        }

        /// <summary>
        /// Set TopBorderMargin property
        /// </summary>
        /// <param name="guiCommand"></param>
        protected override void SetTopBorderMargin(GuiCommand guiCommand)
        {
            MgGroupBox mgGroupBox = ControlsMap.getInstance().object2Widget(guiCommand.obj) as MgGroupBox;

            Debug.Assert(mgGroupBox != null);

            mgGroupBox.TopBorderMargin = guiCommand.Bool3;
        }

        /// <summary>
        /// Set Title Foreground color 
        /// </summary>
        /// <param name="guiCommand"></param>
        internal override void setTitleFgColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is MgTabControl)
            {
                Color color = Color.Empty;
                if (guiCommand.mgColor != null)
                    color = ControlUtils.MgColor2Color(guiCommand.mgColor, false, false);

                ((MgTabControl)obj).TitleFgColor = color;
            }
        }

        /// <summary>
        /// set hot track color
        /// </summary>
        /// <param name="guiCommand"></param>
        internal override void setHotTrackColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is MgTabControl)
            {
                Color color = Color.Empty;
                if (guiCommand.mgColor != null)
                    color = ControlUtils.MgColor2Color(guiCommand.mgColor, false, false);

                ((MgTabControl)obj).HotTrackColor = color;
            }
        }

        /// <summary>
        /// set Hot track foreground color
        /// </summary>
        /// <param name="guiCommand"></param>
        internal override void setHotTrackFgColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is MgTabControl)
            {
                Color color = Color.Empty;
                if (guiCommand.mgColor != null)
                    color = ControlUtils.MgColor2Color(guiCommand.mgColor, false, false);

                ((MgTabControl)obj).HotTrackFgColor = color;
            }
        }


        /// <summary>
        /// set selected tab color
        /// </summary>
        /// <param name="guiCommand"></param>
        internal override void setSelectedTabColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is MgTabControl)
            {
                Color color = Color.Empty;
                if (guiCommand.mgColor != null)
                    color = ControlUtils.MgColor2Color(guiCommand.mgColor, false, false);

                ((MgTabControl)obj).SelectedTabColor = color;
            }
        }

        /// <summary>
        /// set selected tab foreground color
        /// </summary>
        /// <param name="guiCommand"></param>
        internal override void setSelectedTabFgColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is MgTabControl)
            {
                Color color = Color.Empty;
                if (guiCommand.mgColor != null)
                    color = ControlUtils.MgColor2Color(guiCommand.mgColor, false, false);

                ((MgTabControl)obj).SelectedTabFgColor = color;
            }
        }

        #region DRAG And DROP
        /// <summary>
        ///   We will set TagData / LogicalControl's AllowDrop, and use the
        ///   same to decide whether we should allow drop on a control / form.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void SetAllowDrop(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            // When we have a Form/Subform as a GuiCommand.obj, we will get MgPanel from controls map.
            // We will use Panel.TagData.AllowDrop, to decide whether we should allow drop on form or not.
            if (obj is LogicalControl)
                ((LogicalControl)obj).AllowDrop = guiCommand.Bool3;
            else if (obj is Control)
            {
                if (obj is Panel && ((TagData)((Panel)obj).Tag).IsClientPanel)
                {
                    // Set TagData.AllowDrop of Form, to allow drop on non-client area.
                    Form form = GuiUtils.getForm(obj);
                    if (form != null)
                        ((TagData)(form.Tag)).AllowDrop = guiCommand.Bool3;
                }

                // Set TagData.AllowDrop of controls (i.e : TextBox, Panel controls)
                ((TagData)((Control)obj).Tag).AllowDrop = guiCommand.Bool3;
            }
        }

        /// <summary>
        ///   We will set TagData / LogicalControl's AllowDrag, and use the
        ///   same to decide whether we should allow drag on a control.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void SetAllowDrag(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is LogicalControl)
                ((LogicalControl)obj).AllowDrag = guiCommand.Bool3;
            else if (obj is Control)
                ((TagData)((Control)obj).Tag).AllowDrag = guiCommand.Bool3;
        }

        /// <summary>
        ///   Set data for drag operation.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void SetDataForDrag(GuiCommand guiCommand)
        {
            String draggedData = null;
            String userFormatStr = null;
            ClipFormats clipFormat = ClipFormats.FORMAT_TEXT;

            // Dragged data can be : 
            //       1) Data from function evaluation
            //  OR   2) Data from current control --> When guiCommand.obj != null.
            if (guiCommand.obj == null)
            {
                draggedData = guiCommand.str;
                userFormatStr = guiCommand.userDropFormat;
                clipFormat = (ClipFormats)guiCommand.style;
            }
            else
            {
                Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
                draggedData = GetDefaultDataForDrag(obj);
            }

            if (draggedData != null)
                GuiUtils.DraggedData.SetData(draggedData, clipFormat, userFormatStr);
        }

        /// <summary>
        ///   Initiate a Drag operation by calling DoDragDrop.
        ///   After returning from DoDragDrop, reset the saved properties.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal override void PerformDragDrop(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            Control control = null;

            // If we have a logical control, then we will invoke DoDragDrop on Form.
            if (obj is LogicalControl)
            {
                if (obj is LgText && ((LgText)obj).Modifable)
                    control = GuiUtils.getTextCtrl(obj);
                else
                    control = GuiUtils.getForm(ControlsMap.getInstance().object2Widget(guiCommand.obj1));
            }
            else
                control = (Control)obj;

            GuiUtils.DraggedData.IsBeginDrag = false;
            if (control != null)
            {
                control.DoDragDrop(GuiUtils.DraggedData.DataContent, DragDropEffects.Copy | DragDropEffects.None);
                GuiUtils.ResetDragInfo(control);
            }
        }

        /// <summary>
        /// Get the default data of a control.
        /// </summary>
        /// <param name="obj">A control for which we want to retrieve the data.</param>
        /// <returns></returns>
        private String GetDefaultDataForDrag(Object obj)
        {
            String strData = null;

            if (obj is TextBox || obj is LgText)
            {
                TextBox textBox = (TextBox)GuiUtils.getTextCtrl(obj);
                if (textBox != null)
                {
                    if (textBox.UseSystemPasswordChar)
                        strData = new String('*', textBox.SelectionLength);
                    else
                        strData = textBox.SelectedText;
                }
            }
            else if (obj is TabControl)
            {
                TabControl tab = (TabControl)obj;
                strData = tab.TabPages[tab.SelectedIndex].Text;
            }
            else if (obj is ComboBox)
                strData = ((Control)obj).Text;
            else if (obj is ListBox)
            {
                // Common code for Single & Multiple Selection ListBox.
                ListBox lstBox = (ListBox)obj;
                foreach (String str in lstBox.SelectedItems)
                    strData = strData + str + ",";
                strData = strData.TrimEnd(new char[] { ',' });
            }
            else if (obj is MgRadioPanel)
            {
                MgRadioButton radioButton = (MgRadioButton)GuiUtils.getSelectedRadioControl((MgRadioPanel)obj);
                strData = radioButton.TextToDisplay;
            }

            return strData;
        }
        #endregion    // DRAG And DROP
    }
#else
// PocketPC
/// <summary>
///   Gui command queue
/// </summary>
   internal sealed class GuiCommandQueue : GuiCommandQueueBase
   {
      private static GuiCommandQueue _instance;
      private Boolean _isWindowsMobile6;

      /// <summary>
      ///   Implementing a singleton
      /// </summary>
      /// <returns> reference to GuiCommandQueue object</returns>
      internal static GuiCommandQueue getInstance()
      {
         if (_instance == null)
            _instance = new GuiCommandQueue();
         return _instance;
      }

      /// <summary>Constructor</summary>
      private GuiCommandQueue()
      {
         init();
         initMobile();
      }

      /// <summary>Constructor</summary>
      private void initMobile()
      {
         StringBuilder sb = new StringBuilder(128);
         if (NativeWindowCommon.SystemParametersInfo(NativeWindowCommon.SPI_GETPLATFORMTYPE,
                                                     (uint)sb.Capacity, sb, 0) == 0)
            throw new ApplicationException("Failed to get system parameter");

         if (sb.ToString().Equals("SmartPhone"))
            _isWindowsMobile6 = true;
      }

      /// <summary>
      ///   CommandType.CREATE_MENU This method creates an .Net menu for the passed MenuEntry object.
      /// </summary>
      internal override void createMenu(GuiCommand guiCommand)
      {
         Menu parentMenu = null;
         GuiMgForm guiMgForm = null;
         MenuReference menuReference = null;

         guiMgForm = (GuiMgForm)guiCommand.obj;

         // Only diff btw form and subform is what we send as mgForm in the getInstantiatedMenu.
         menuReference = guiCommand.menu.getInstantiatedMenu(guiMgForm, guiCommand.menuStyle);

         // same to form and subform 
         if (guiCommand.menuStyle == MenuStyle.MENU_STYLE_CONTEXT)
         {
            parentMenu = new ContextMenu();
            GuiUtils.CreateTagData((ContextMenu)parentMenu);
            ((TagData)((ContextMenu)parentMenu).Tag).GuiMgMenu = guiCommand.menu;
            ((TagData)((ContextMenu)parentMenu).Tag).MenuStyle = guiCommand.menuStyle;
            ((TagData)((ContextMenu)parentMenu).Tag).ContextCanOpen = false;
         }
         else
         {
            parentMenu = new MainMenu();
            GuiUtils.CreateTagData((MainMenu)parentMenu);
            ((TagData)((MainMenu)parentMenu).Tag).GuiMgMenu = guiCommand.menu;
            ((TagData)((MainMenu)parentMenu).Tag).MenuStyle = guiCommand.menuStyle;
            ((TagData)((MainMenu)parentMenu).Tag).ContextCanOpen = false;
         }
         // add the new menu to the map
         ControlsMap.getInstance().add(menuReference, parentMenu);
         MgMenuHandler.getInstance().addHandler(parentMenu);
      }

      /// <summary>
      ///   CommandType.CREATE_MENU_ITEM This method creates a menuItem object to match a specific MenuEntry as the
      ///   child of the passed parent Menu object. The leaves of the menu will be created with the SWT_CHECK style
      ///   in order to allow checking and unchecking them (for MnuCheck function).It means the menu itself will be
      ///   a little wider.
      /// </summary>
      /// <menuEntry>  MenuEntry object we mean to create a matching menu to. </menuEntry>
      /// <parentMenu>  a menuItem for which we will add the new menu as child </parentMenu>
      /// <menuStyle>  style of the menu to create </menuStyle>
      /// <parentIsMenu>  - the parent object is a Menu - otherwise it is a MenuItem </parentIsMenu>
      internal override void createMenuItem(GuiMenuEntry menuEntry, Object parentMenu, MenuStyle menuStyle,
                                            bool parentIsMenu, int index, GuiMgForm form)
      {
         MenuItem menuItem;
         int location;
         int count;

         if (parentMenu is MainMenu)
            count = ((MainMenu)parentMenu).MenuItems.Count;
         else if (parentMenu is MenuItem)
            count = ((MenuItem)parentMenu).MenuItems.Count;
         else
            count = ((ContextMenu)parentMenu).MenuItems.Count;

         location = (index <= count
                        ? index
                        : count + 1);

         try
         {
            if (menuEntry.menuType() == GuiMenuEntry.MenuType.MENU)
            {
               // Create the item in the bar menu
               menuItem = new MenuItem();
               menuItem.Text = menuEntry.TextMLS;
               menuItem.Checked = menuEntry.getChecked();
               menuItem.Enabled = menuEntry.getEnabled();

               if (parentMenu is MainMenu)
                  ((MainMenu)parentMenu).Insert(location - 1, menuItem);
               else if (parentMenu is MenuItem)
                  ((MenuItem)parentMenu).Insert(location - 1, menuItem);
               else
                  ((ContextMenu)parentMenu).Insert(location - 1, menuItem);

               // save a reference from the menuItem object to the MenuEntry object
               // No need for extra menu in order to add menu item
               MenuReference menuItemReference = menuEntry.getInstantiatedMenu(form, menuStyle);
               // set the SWT menu data according to the MenuEntry entry
               setMenuItemPropsByMenuEntry(form, menuItem, menuEntry, menuStyle);

               ControlsMap.getInstance().add(menuItemReference, menuItem);

               // each menuitem has 2 refs. 1 in menus (like instantiatedPullDown) and 1 in instantiatedMenuItems.
               // we have to mark both.
               // 7.1.08 : Ori - removing the add to the map since it caused memory leak. 
               // This is the menu item who is menu entry reg, the more important is the menuitemReference.
               // We cannot add them both with menuItem as the key, since 2nd will overrun the 1st , but the reference will
               // be in the hashtable of the controlmap forever and cause a leak.
               // i did not find any use to adding it..i add it on ver 100 (31/7/08) and did not find why.
               //MenuReference menuReference = menuEntry.getInstantiatedMenuItem(form, menuStyle);
               //controlsMap.add(menuReference, menuItem);

               // Add handlers
               MgMenuHandler.getInstance().addHandler(menuItem, true);
            }
            else
            {
               MenuItem Item = new MenuItem();

               if (parentMenu is MainMenu)
                  ((MainMenu)parentMenu).Insert(location - 1, Item);
               else if (parentMenu is MenuItem)
                  ((MenuItem)parentMenu).Insert(location - 1, Item);
               else
                  ((ContextMenu)parentMenu).Insert(location - 1, Item);

               // set the SWT menu data according to the MenuEntry entry
               setMenuItemPropsByMenuEntry(form, Item, menuEntry, menuStyle);
               // save a reference from the menuItem object to the MenuEntry object
               // MgForm form = (MgForm)((Menu) parentMenu).getShell().getData();
               MenuReference menuReference = menuEntry.getInstantiatedMenu(form, menuStyle);
               if (menuReference != null)
                  ControlsMap.getInstance().add(menuReference, Item);
               else
                  Debug.Assert(menuReference != null);

               // Add handlers. also add handler to seperator..just for the dispose.
               MgMenuHandler.getInstance().addHandler(Item, false);
            }
         }
         catch (Exception e)
         {
            Misc.WriteStackTrace(e, Console.Error);
         }
      }

      ///<summary>
      ///   This method updates a passed menu object by the sent MenuEntry information text, accelerator, image and so on.
      ///</summary>
      ///<param name="guiMgform">!!.</param>
      ///<param name="toolItem">!!.</param>
      ///<param name="menuEntry">!!.</param>
      ///<returns>!!.</returns>
      private void setToolItemPropsByMenuEntry(GuiMgForm guiMgform, ToolBarButton toolItem, GuiMenuEntry menuEntry)
      {
         if (toolItem.Style != ToolBarButtonStyle.Separator)
         {
            toolItem.ImageIndex = menuEntry.ImageNumber;
            toolItem.Pushed = menuEntry.getChecked();
         }
         ((TagData)toolItem.Tag).guiMenuEntry = menuEntry;
         ((TagData)toolItem.Tag).MenuStyle = MenuStyle.MENU_STYLE_TOOLBAR;
         toolItem.Enabled = menuEntry.getEnabled();
      }

      /// <summary>
      ///   Sets menu items properties from MenuEntry.
      /// </summary>
      /// <param name="guiMgform">!!.</param>
      /// <param name = "menuItem">MenuItem whose properties is to be set</param>
      /// <param name = "menuEntry">MenuEntry from where the properties were set</param>
      /// <param name = "menuStyle">Menu style CONTEXT,PULLDOWN, TOOLBAR</param>
      private void setMenuItemPropsByMenuEntry(GuiMgForm guiMgForm, MenuItem menuItem, GuiMenuEntry menuEntry, MenuStyle menuStyle)
      {
         setMenuItemText(menuItem, menuEntry);
         if (menuEntry.menuType() != GuiMenuEntry.MenuType.SEPARATOR)
         {
            menuItem.Checked = menuEntry.getChecked();
            menuItem.Enabled = menuEntry.getEnabled();
         }
         GuiUtils.CreateTagData(menuItem);
         ((TagData)menuItem.Tag).guiMenuEntry = menuEntry;
         ((TagData)menuItem.Tag).MenuStyle = menuStyle;
      }

      /// <summary>
      ///   This method sets the pulldown menu or context menu for a form or context menu for control
      /// </summary>
      /// <param name = "guiCommand"></param>
      internal override void setMenu(GuiCommand guiCommand)
      {
         Menu menu;

         MenuReference menuRefernce;
         GuiMgControl mgControl = null;
         GuiMgForm guiMgForm = (GuiMgForm)guiCommand.obj;
         try
         {
            menuRefernce = guiCommand.menu.getInstantiatedMenu(guiMgForm, guiCommand.menuStyle);
            menu = (Menu)ControlsMap.getInstance().object2Widget(menuRefernce);

            if (guiCommand.Bool3 && mgControl == null)
            {
               Object obj = ControlsMap.getInstance().object2Widget(guiCommand.parentObject);
               Form form = GuiUtils.getForm(obj);

               // set pull down menu or context menu
               if (guiCommand.menuStyle == MenuStyle.MENU_STYLE_PULLDOWN)
                  form.Menu = (MainMenu)menu;
               else
                  form.ContextMenu = (ContextMenu)menu;
            }
            else
            {
               if (mgControl == null)
                  mgControl = (GuiMgControl)guiCommand.parentObject;
               ArrayList arrayControl = ControlsMap.getInstance().object2WidgetArray(mgControl, guiCommand.line);
               for (int i = 0;
                    i < arrayControl.Count;
                    i++)
               {
                  Object control = arrayControl[i];
                  if (control is TableControl)
                     GuiUtils.getTableManager((TableControl)control).setContextMenu((ContextMenu)menu);
                  else if (control is LogicalControl)
                     ((LogicalControl)control).ContextMenu = (ContextMenu)menu;
                  else if (control is Control)
                     GuiUtils.setContextMenu((Control)control, menu);
               }
            }
         }
         catch (Exception e)
         {
            Misc.WriteStackTrace(e, Console.Error);
            throw e;
         }
      }

      /// <summary>
      ///   Function returns true if the control on which the command operates is supported.///
      /// </summary>
      /// <param name = "guiCommand">gui commands (control, command, ...)</param>
      /// <returns>whether supported or not</returns>
      internal override bool isSupportedControl(GuiCommand guiCommand)
      {
         bool isSupported = true;

         if (guiCommand.obj is MgControl)
         {
            MgControl control = (MgControl)guiCommand.obj;
            switch (control.Type)
            {
               case MgControlType.CTRL_TYPE_RADIO:
               case MgControlType.CTRL_TYPE_TAB:
               case MgControlType.CTRL_TYPE_LIST:
                  if (!_isWindowsMobile6)
                     break;
                  else
                     goto case MgControlType.CTRL_TYPE_FRAME_FORM;

               case MgControlType.CTRL_TYPE_FRAME_FORM:
               case MgControlType.CTRL_TYPE_GROUP:
               case MgControlType.CTRL_TYPE_FRAME_SET:
               case MgControlType.CTRL_TYPE_STATUS_BAR:
               case MgControlType.CTRL_TYPE_TREE:
               case MgControlType.CTRL_TYPE_RICH_EDIT:
               case MgControlType.CTRL_TYPE_RICH_TEXT:
                  isSupported = false;
                  String error = null;

                  switch (guiCommand.CommandType)
                  {
                     case CommandType.CREATE_FRAME_FORM:
                     case CommandType.CREATE_GROUP:
                     case CommandType.CREATE_FRAME_SET:
                     case CommandType.CREATE_TREE:
                     case CommandType.CREATE_RICH_EDIT:
                     case CommandType.CREATE_RICH_TEXT:
                        error = GuiConstants.RIA_MOBILE_UNSUPPORTED_CONTROL_ERROR;
                        break;
                     case CommandType.CREATE_RADIO_BUTTON:
                     case CommandType.CREATE_TAB:
                     case CommandType.CREATE_LIST_BOX:
                        error = GuiConstants.RIA_MOBILE_UNSUPPORTED_CONTROL_ERROR_WINDOW_6;
                        break;
                  }
                  if (error != null)
                  {
                     String title = String.Format("TaskBase '{0}'", ((Task)control.getForm().getTask()).getName());
                     String errorMsg = control.getControlTypeName() + " " + error;
                     Events.WriteErrorToLog(title + ": " + errorMsg);
                     // display the error message in the current form's title, and continue execution
                     Form form = ((Form)ControlsMap.getInstance().object2Widget(control.getForm()));
                     form.Text = errorMsg;
                  }
                  break;
            }
            if (isSupported && control.getLinkedParent(false) != null)
               if (control.getLinkedParent(false).Type == MgControlType.CTRL_TYPE_GROUP)
                  isSupported = false;
         }

         return isSupported;
      }

      /// <summary>
      /// </summary>
      /// <param name = "guiCommand"></param>
      internal override void setWindowState(GuiCommand guiCommand)
      {
         Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);
         Form form = GuiUtils.FindForm(control);

         switch (guiCommand.style)
         {
            case Styles.WINDOW_STATE_MAXIMIZE:
               form.WindowState = FormWindowState.Maximized;
               if (!form.Visible)
                  GUIManager.Instance.restoreHiddenForms();
               break;

            case Styles.WINDOW_STATE_MINIMIZE:
               GUIMain.getInstance().MainForm.Visible = false;
               List<MgFormBase> forms = MGDataCollection.Instance.GetTopMostForms();
               foreach (MgFormBase mgForm in forms)
               {
                  form = mgFormToForm(mgForm);
                  if (form != null)
                     form.Visible = false;
               }
               GUIManager.Instance.hibernateContext();
               break;

            case Styles.WINDOW_STATE_RESTORE:
               form.WindowState = FormWindowState.Normal;
               if (!form.Visible && ClientManager.Instance.IsHidden)
                  GUIManager.Instance.restoreHiddenForms();
               break;

            default:
               break;
         }
      }

      /// <summary>
      ///   returns the Form corresponding to the MgForm
      /// </summary>
      /// <param name = "mgForm"></param>
      /// <returns>Form object or null</returns>
      internal Form mgFormToForm(MgFormBase mgForm)
      {
         Object obj = ControlsMap.getInstance().object2Widget(mgForm);
         return (obj is Form)
                   ? (Form)obj
                   : (obj is MgPanel
                         ? (Form)((MgPanel)obj).Parent
                         : null);
      }

      /// <summary>
      ///   Show and activate all forms in array list
      /// </summary>
      /// <param name = "guiCommand"></param>
      internal override void onResumeShowForms(GuiCommand guiCommand)
      {
         var forms = (List<MgFormBase>)guiCommand.obj;
         foreach (var mgForm in forms)
         {
            if (mgForm.getTask().isOpenWin())
            {
               Form form = mgFormToForm(mgForm);
               if (form != null)
               {
                  form.Visible = true;
                  form.Activate();
               }
            }
         }
      }
   }
#endif

    /// <summary>
    ///   Gui command queue
    /// </summary>
    internal abstract class GuiCommandQueueBase
    {
        private magicsoftware.util.Queue<GuiCommand> _commandsQueue;
     
      internal int QueueSize { get { return _commandsQueue.Size(); } }

        // Gui thread command processing sync. objects
        internal static Object GuiThreadCommandProcessingStart = new Object();
        internal static Object GuiThreadCommandProcessingEnd = new Object();

        private bool _guiThreadIsAvailableToProcessCommands_DO_NOT_USE_DIRECTLY = false;
        internal bool GuiThreadIsAvailableToProcessCommands
        {
            get { return _guiThreadIsAvailableToProcessCommands_DO_NOT_USE_DIRECTLY; }
            set
            {
                // Only 1 thread should process the GUI commands at a time. When this flag is true,
                // it means that someone is executing the gui commands and so, the other threads go 
                // into the wait state. 
                // So, when setting this flag to false, notify the thread that is waiting for the object.
                // Each time, notify only one thread. This thread will execute the gui command and 
                // in turn, will notify the next waiting thread (by setting this flag to false).
                if (_guiThreadIsAvailableToProcessCommands_DO_NOT_USE_DIRECTLY != value)
                {
                    _guiThreadIsAvailableToProcessCommands_DO_NOT_USE_DIRECTLY = value;

                    Object lockObj = (_guiThreadIsAvailableToProcessCommands_DO_NOT_USE_DIRECTLY ? GuiThreadCommandProcessingStart : GuiThreadCommandProcessingEnd);

                    try
                    {
                        Monitor.Enter(lockObj);
                        Monitor.Pulse(lockObj);
                    }
                    finally
                    {
                        Monitor.Exit(lockObj);
                    }
                }
            }
        }

        private bool _modalShowFormCommandPresentInQueue = false;

        /// <summary>Constructor</summary>
        internal void init()
        {
            _commandsQueue = new magicsoftware.util.Queue<GuiCommand>();
        }

       /// <summary>inner class for the gui command</summary>
       internal class GuiCommand
       {
          //TODO: consolidate to minimal set of members
          public CommandType CommandType { get; set; }

          public String TaskTag { get; set; } // should we use windownum?
          public string Operation { get; set; }
          public bool Bool1 { get; set; }
          public bool Bool3 { get; set; }
          public byte[] ByteArray { get; set; }
          internal List<GuiMgControl> CtrlsList;
          public String fileName { get; set; }
          public int height { get; set; }
          public int[] intArray { get; set; }
          public List<int[]> intArrayList { get; set; }
          public List<int> intList { get; set; }
          public String[] itemsList { get; set; }
          public int layer { get; set; }
          public int line { get; set; }
          public GuiMgMenu menu;
          public GuiMenuEntry menuEntry { get; set; }
          public MenuStyle menuStyle { get; set; }
          public MgColor mgColor { get; set; }
          public MgColor mgColor1 { get; set; }
          public MgFont mgFont { get; set; }
          public int number { get; set; }
          public int number1 { get; set; }
          public int number2 { get; set; }

          public String CtrlName
          {
             get { return obj is MgControlBase ? ((MgControlBase) obj).getName() : null; }
          }

          internal Object obj;
          internal Object parentObject;
          internal Object obj1;
          public String str { get; set; }
          public List<String> stringList { get; set; }
          public int style { get; set; }
          internal Type type;
          public int width { get; set; }
          public WindowType windowType { get; set; }
          public int x { get; set; }
          public int y { get; set; }
          public String userDropFormat { get; set; }
          public bool isHelpWindow { get; set; }
          public bool isParentHelpWindow { get; set; }
          public DockingStyle dockingStyle { get; set; }
          public ListboxSelectionMode listboxSelectionMode { get; set; }
          internal Int64 contextID { get; set; }
          public int? runtimeDesignerXDiff { get; set; }
          public int? runtimeDesignerYDiff { get; set; }
          public bool createInternalFormForMDI { get; set; }

          /// <summary>
          /// </summary>
          internal GuiCommand(CommandType commandType)
          {
             this.CommandType = commandType;
             this.contextID = Manager.GetCurrentContextID();
          }

          /// <summary></summary>
          /// <param name = "commandType"></param>
          /// <param name = "str"></param>
          internal GuiCommand(CommandType commandType, String str)
             : this(commandType)
          {
             this.str = str;
          }

          /// <summary></summary>
          internal GuiCommand(Object obj, CommandType commandType)
             : this(commandType)
          {
             this.obj = obj;
             if (obj is MgControlBase)
               this.TaskTag=((MgControlBase) obj).getForm().getTask().getTaskTag();
             else if (obj is MgFormBase)
                this.TaskTag = ((MgFormBase)obj).getTask().getTaskTag();
         }

          /// <summary>
          ///   this is my comment
          /// </summary>
          /// <param name = "parentObject">the parent object of the object</param>
          internal GuiCommand(Object parentObject, Object obj, CommandType cmmandType)
             : this(obj, cmmandType)
          {
             this.parentObject = parentObject;
          }

          /// <summary>
          /// return true if the command is for showing a modal form
          /// </summary>
          /// <returns></returns>
          internal bool IsModalShowFormCommand()
          {
             return (CommandType == CommandType.SHOW_FORM && Bool3);
          }

          public override string ToString()
          {
             return "{" + CommandType + "}";
          }
       }

       #region DifferentiationConfirmed

        internal virtual void createMenu(GuiCommand guiCommand)
        {
            Events.WriteExceptionToLog("createMenu - Not Implemented Yet");
        }

        /// <summary>
        ///   CommandType.CREATE_MENU_ITEM Translate the passed gui command to a call to the next method.
        /// </summary>
        internal void createMenuItem(GuiCommand guiCommand)
        {
            Object obj = null;
            GuiMgForm guiMgForm = (GuiMgForm)guiCommand.obj;

            if (guiCommand.parentObject is GuiMgMenu)
            {
                MenuReference menuReference = ((GuiMgMenu)guiCommand.parentObject).getInstantiatedMenu(guiMgForm,
                    guiCommand.menuStyle);
                Debug.Assert(menuReference != null);
                obj = ControlsMap.getInstance().object2Widget(menuReference);
            }
            else if (guiCommand.parentObject is MenuReference)
                obj = ControlsMap.getInstance().object2Widget(guiCommand.parentObject);
            else
                Debug.Assert(false);

            if (obj != null)
                createMenuItem(guiCommand.menuEntry, obj, guiCommand.menuStyle, true, guiCommand.line, guiMgForm);
        }

        internal virtual void createMenuItem(GuiMenuEntry menuEntry, object parentMenu, MenuStyle menuStyle,
            bool parentIsMenu, int index, GuiMgForm form)
        {
            Events.WriteExceptionToLog("createMenuItem - Not Implemented Yet");
        }

        internal virtual void SetAllowDrop(GuiCommand guiCommand)
        {
            Events.WriteExceptionToLog("setAllowDrop - Not Implemented Yet");
        }

        internal virtual void SetAllowDrag(GuiCommand guiCommand)
        {
            Events.WriteExceptionToLog("setAllowDrag - Not Implemented Yet");
        }

        internal virtual void SetDataForDrag(GuiCommand guiCommand)
        {
            // We won't get guiCommand.obj when it is called from Expression.
            Events.WriteExceptionToLog("setDataForDrag - Not Implemented Yet");
        }

        internal virtual void PerformDragDrop(GuiCommand guiCommand)
        {
            Events.WriteExceptionToLog("performDragDrop - Not Implemented Yet");
        }

        internal virtual void RegisterDNControlValueChangedEvent(GuiCommand guiCommand)
        {
            Events.WriteExceptionToLog("RegisterDNControlValueChangedEvent - Not Implemented Yet");
        }

        #endregion

        #region Common

        /// <summary>
        /// Adds a command to the queue. If there are already enough commands in the queue then
        /// it will wait till the queue is emptied bu Gui thread.
        /// </summary>
        /// <param name="guiCommand"></param>
        private void put(GuiCommand guiCommand)
        {
            const int QUEUE_SIZE_THRESHOLD = 1200; // a threshold above which commands will not be inserted to the queue until the queue will be empty.
            const int MAX_SLEEP_DURATION = 4; // duration, in ms, between checking that the queue is empty.

            // If worker thread wants to add a command and there are too many commands in the queue pending 
            // to be processed by Gui thread then suspend current thread till the commands are over. If we keep 
            // on adding commands to the queue and Gui thread enters Run(), then sometimes (specially in cases of 
            // batch tasks) Gui thread remains in Run() for a long time and hence it is unable to process user 
            // interactions (as described in QCR#722145)
            // Before entering the loop confirm that the command is NOT being added by Gui thread itself and Gui 
            // thread is already processing the commands. This is to ensure that the commands will be processed by 
            // Gui thread. If Gui thread is not processing commands, then it will never process it again as it is 
            // the worker thread that invokes Gui thread for processing commands
            // Also, suspend worker thread from adding new commands to the queue till SHOW_FORM for modal window 
            // is processed. Problem occurs if SHOW_FORM is followed by few more commands and then a call to 
            // GuiInteractive  that depends on the earlier commands. In such cases, GuiInteractive will not process 
            // commands between SHOW_FORM and interactive command because GuiThreadIsAvailableToProcessCommands
            // was set to false before opening the dialog.
            if (!Misc.IsGuiThread() &&
                ((_commandsQueue.Size() > QUEUE_SIZE_THRESHOLD && GuiThreadIsAvailableToProcessCommands) || _modalShowFormCommandPresentInQueue))
            {
                int sleepDuration = MAX_SLEEP_DURATION;
                do
                {
                    // get current size of the queue and wait for some time
                    int size = _commandsQueue.Size();
                    System.Threading.Thread.Sleep(sleepDuration);

                    // while current thread was sleeping, gui thread should have processed some commands.
                    // get average duration required by gui thread for processing a command and estimate new duration for remaining commands.
                    int newSize = _commandsQueue.Size();
                    if (size > newSize)
                    {
                        float averageDurationPerCommand = sleepDuration / (size - newSize);
                        int newSleepDuration = (int)(newSize * averageDurationPerCommand);
                        if (newSleepDuration > 0)
                            sleepDuration = System.Math.Min(MAX_SLEEP_DURATION, newSleepDuration);
                    }
                } while (GuiThreadIsAvailableToProcessCommands && _commandsQueue.Size() > 0);
            }

            _commandsQueue.put(guiCommand);

            if (guiCommand.CommandType == CommandType.REFRESH_TABLE && guiCommand.Bool3)
            {
                MgControlBase mgControl = (guiCommand.obj as MgControlBase);
                mgControl.refreshTableCommandCount++;
            }
            else if (guiCommand.IsModalShowFormCommand())
                _modalShowFormCommandPresentInQueue = true;
        }

       internal String SerializeCommands()
       {
          string result = null;
          if (_commandsQueue._queueVec.Count > 0)
          {
             JavaScriptSerializer serializer = new JavaScriptSerializer();
             serializer.RegisterConverters(new JavaScriptConverter[] {new NullPropertiesConverter()});
             result = serializer.Serialize(_commandsQueue._queueVec);
             _commandsQueue.clear();
          }
          return result;
       }
      private class NullPropertiesConverter : JavaScriptConverter
      {
         public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
         {
            throw new NotImplementedException();
         }

         public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
         {
            var jsonExample = new Dictionary<string, object>();
            foreach (var prop in obj.GetType().GetProperties())
            {
               //check if decorated with ScriptIgnore attribute
               bool ignoreProp = prop.IsDefined(typeof(ScriptIgnoreAttribute), true);

               var value = prop.GetValue(obj, BindingFlags.Public, null, null, null);

               if (value != null && !ignoreProp)
               {
                  if (prop.PropertyType.IsEnum)
                     value = (int)value;
                  if (value is bool && ((bool)value) == false)
                     continue;
                  if (value is int && ((int)value) == 0)
                     continue;
                
                  jsonExample.Add(prop.Name, value);
               }
            }

            return jsonExample;
         }

         public override IEnumerable<Type> SupportedTypes
         {
            get { return GetType().Assembly.GetTypes(); }
         }
      }

      /// <summary>
      ///   BEEP,
      /// </summary>
      internal void add(CommandType commandType)
        {
            GuiCommand guiCommand = new GuiCommand(commandType);
            put(guiCommand);
        }

        /// <summary>
        ///   DISPOSE_OBJECT REMOVE_CONTROLS EXECUTE_LAYOUT CLOSE_SHELL, REMOVE_ALL_TABLE_ITEMS,
        ///   REMOVE_CONTROLS, INVALIDATE_TABLE, SET_SB_LAYOUT_DATA, SET_WINDOW_ACTIVE
        ///   SET_FRAMESET_LAYOUT_DATA, RESUME_LAYOUT, UPDATE_MENU_VISIBILITY
        ///   ORDER_MG_SPLITTER_CONTAINER_CHILDREN, CLEAR_TABLE_COLUMNS_SORT_MARK, MOVE_ABOVE, START_TIMER
        /// </summary>
        internal void add(CommandType commandType, Object obj)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            put(guiCommand);
        }

#if PocketPC
/// <summary>
///   RESUME_SHOW_FORMS
/// </summary>
      internal void add(CommandType commandType, List<GuiMgForm> obj)
      {
         GuiCommand guiCommand = new GuiCommand(obj, commandType);
         put(guiCommand);
      }
#endif

        /// <summary>
        ///   OPEN_FORM, OPEN HELP FORM.
        /// </summary>
        internal void add(CommandType commandType, Object obj, bool boolVal, String formName)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            guiCommand.Bool3 = boolVal;
            guiCommand.str = formName;
            put(guiCommand);
        }

        /// <summary>
        /// SHOW_FORM
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="obj"></param>
        /// <param name="boolVal"></param>
        /// <param name="isHelpWindow"></param>
        /// <param name="formName"></param>
        internal void add(CommandType commandType, Object obj, bool boolVal, bool isHelpWindow, String formName)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            guiCommand.Bool3 = boolVal;
            guiCommand.Bool1 = isHelpWindow;
            guiCommand.str = formName;
            put(guiCommand);
        }

        /// <summary>
        ///   EXECUTE_LAYOUT, REORDER_FRAME, PROP_SET_SHOW_ICON, SET_FORMSTATE_APPLIED, PROP_SET_FILL_WIDTH
        /// </summary>
        internal void add(CommandType commandType, Object obj, bool boolVal)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            guiCommand.Bool3 = boolVal;
            put(guiCommand);
        }

        /// <summary>
        ///   ADD_DVCONTROL_HANDLER, REMOVE_DVCONTROL_HANDLER
        /// </summary>
        internal void add(CommandType commandType, Object obj, Object obj1)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            guiCommand.obj1 = obj1;
            put(guiCommand);
        }

        /// <summary>
        ///   PROP_SET_DEFAULT_BUTTON style : not relevant PROP_SET_SORT_COLUMN
        /// </summary>
        /// <param name = "line">TODO CREATE_RADIO_BUTTON PROP_SET_SORT_COLUMN layer, line,style isn't relevant parentObject:
        ///   must to be the table control object: must to be the Column control
        /// </param>
        internal void add(CommandType commandType, Object parentObject, Object obj, int layer, int line, int style)
        {
            checkObject(obj);

            if (!((parentObject is GuiMgForm) || (parentObject is GuiMgControl)))
                throw new ApplicationException("in GuiCommandQueue.add(): parent object is not GuiMgForm or GuiMgControl");

            GuiCommand guiCommand = new GuiCommand(parentObject, obj, commandType);
            guiCommand.line = line;
            switch (commandType)
            {
                case CommandType.PROP_SET_DEFAULT_BUTTON:
                    guiCommand.parentObject = parentObject;
                    guiCommand.obj = obj;
                    break;

                default:
                    guiCommand.layer = layer;
                    guiCommand.style = style;
                    break;
            }
            put(guiCommand);
        }

        /// <summary>
        ///   SELECT_TEXT
        /// </summary>
        internal void add(CommandType commandType, Object obj, int line, int num1, int num2, int num3)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            guiCommand.line = line;
            switch (commandType)
            {
                case CommandType.SELECT_TEXT:
                case CommandType.CREATE_TREE_NODE:
                case CommandType.MOVE_TREE_NODE:
                    guiCommand.number = num1; // (0) -unmark all text, (1)- mark all text, (-1)-mark from pos until pos
                    // for unmark\mark(0,1) all text, num1 & num 2 are not relevants
                    guiCommand.layer = num2; // mark from text pos
                    guiCommand.number1 = num3; // mark until text pos
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }
            put(guiCommand);
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
        internal void add(CommandType commandType, Object parentObject, Object obj, WindowType windowType, String formName, bool isHelpWindow, bool createInternalFormForMDI, bool shouldBlock)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(parentObject, obj, commandType);

            guiCommand.windowType = windowType;
            guiCommand.str = formName;
            guiCommand.isHelpWindow = isHelpWindow;
            guiCommand.createInternalFormForMDI = createInternalFormForMDI;
            guiCommand.Bool1 = shouldBlock;
            put(guiCommand);
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
        /// <param name = "bool">TODO</param>
        internal void add(CommandType commandType, Object parentObject, Object obj, int line, int style,
            List<String> stringList, List<GuiMgControl> ctrlList, int columnCount, bool boolVal,
            bool boolVal1,
            int number1, Type type, int number2, Object obj1, bool isParentHelpWindow, DockingStyle dockingStyle)
        {
            checkObject(obj);

            if (!((parentObject is GuiMgForm) || (parentObject is GuiMgControl)))
                throw new ApplicationException("in GuiCommandQueue.add(): parent object is not GuiMgForm or GuiMgControl");

            GuiCommand guiCommand = new GuiCommand(parentObject, obj, commandType);
            guiCommand.line = line;
            guiCommand.style = style;
            guiCommand.stringList = stringList;
            guiCommand.CtrlsList = ctrlList;
            guiCommand.number = columnCount;
            guiCommand.Bool3 = boolVal;
            guiCommand.Bool1 = boolVal1;
            guiCommand.type = type;
            guiCommand.number1 = number1;
            guiCommand.number2 = number2;
            guiCommand.obj1 = obj1;
            guiCommand.isParentHelpWindow = isParentHelpWindow;
            guiCommand.dockingStyle = dockingStyle;

            put(guiCommand);
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
        /// <param name = "bool">TODO</param>
        internal void add(CommandType commandType, Object parentObject, Object obj, int line, int style,
            List<String> stringList, List<GuiMgControl> ctrlList, int columnCount, bool boolVal,
            bool boolVal1, int number1, Type type, int number2, Object obj1)
        {
            checkObject(obj);

            if (!((parentObject is GuiMgForm) || (parentObject is GuiMgControl)))
                throw new ApplicationException("in GuiCommandQueue.add(): parent object is not GuiMgForm or GuiMgControl");

            GuiCommand guiCommand = new GuiCommand(parentObject, obj, commandType);
            guiCommand.line = line;
            guiCommand.style = style;
            guiCommand.stringList = stringList;
            guiCommand.CtrlsList = ctrlList;
            guiCommand.number = columnCount;
            guiCommand.Bool3 = boolVal;
            guiCommand.Bool1 = boolVal1;
            guiCommand.type = type;
            guiCommand.number1 = number1;
            guiCommand.number2 = number2;
            guiCommand.obj1 = obj1;

            put(guiCommand);
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
        internal void add(CommandType commandType, Object obj, int number, bool boolVal)
        {
            add(commandType, obj, number, boolVal, false);
        }

        /// <summary>
        ///   PROP_SET_VISIBLE, SET_ACTIVETE_KEYBOARD_LAYOUT
        /// </summary>
        internal void add(CommandType commandType, Object obj, int number, bool boolVal, bool executeParentLayout)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            guiCommand.Bool3 = boolVal;
            guiCommand.Bool1 = executeParentLayout;
            // //for SET_ACTIVETE_KEYBOARD_LAYOUT guiCommand.bool1 is define if it restore or not

            switch (commandType)
            {
                case CommandType.CHANGE_COLUMN_SORT_MARK:
                case CommandType.START_TIMER:
                case CommandType.STOP_TIMER:
                    guiCommand.number = number;
                    break;
                default:
                    guiCommand.line = number;
                    break;
            }
            put(guiCommand);
        }

        /// <summary>
        ///   PROP_SET_BOUNDS, PROP_SET_COLUMN_WIDTH, PROP_SET_SB_PANE_WIDTH, PROP_SET_PLACEMENT
        ///   subformAsControl isn't relevant, need to be false
        /// </summary>
        /// <param name = "line">TODO CREATE_LAYOUT</param>
        /// <param name = "bool">TODO</param>
        internal void add(CommandType commandType, Object obj, int line, int x, int y, int width, int height, bool boolVal,
            bool bool1)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            guiCommand.line = line;
            guiCommand.Bool3 = boolVal;
            guiCommand.Bool1 = bool1;
            guiCommand.x = x;
            guiCommand.y = y;
            guiCommand.width = width;
            guiCommand.height = height;


            put(guiCommand);
        }



        internal void add(CommandType commandType, Object obj, int line, int x, int y, int width, int height, bool boolVal,
            bool bool1, int? number1, int? number2)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            guiCommand.line = line;
            guiCommand.Bool3 = boolVal;
            guiCommand.Bool1 = bool1;
            guiCommand.x = x;
            guiCommand.y = y;
            guiCommand.width = width;
            guiCommand.height = height;
            guiCommand.runtimeDesignerXDiff = number1;
            guiCommand.runtimeDesignerYDiff = number2;

            put(guiCommand);
        }
        /// <summary>
        /// REGISTER_DN_CTRL_VALUE_CHANGED_EVENT
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="obj"></param>
        /// <param name="eventName"></param>
        internal void add(CommandType commandType, Object obj, string eventName)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            guiCommand.str = eventName;

            put(guiCommand);
        }

        /// <summary>
        /// PROP_SET_SELECTION
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="obj"></param>
        /// <param name="line"></param>
        /// <param name="number"></param>
        /// <param name="prevNumber"></param>
        internal void add(CommandType commandType, Object obj, int line, int number, int prevNumber)
        {
            GuiCommand guiCommand = add(commandType, obj, line, number);
            guiCommand.number1 = prevNumber;
        }

        /// <summary>
        ///   PROP_SET_TEXT_SIZE_LIMIT, PROP_SET_VISIBLE_LINES, PROP_SET_MIN_WIDTH, PROP_SET_MIN_HEIGHT,
        ///   SET_WINDOW_STATE, VALIDATE_TABLE_ROW, SET_ORG_COLUMN_WIDTH, PROP_SET_COLOR_BY,
        ///   PROP_SET_TRANSLATOR, PROP_SET_HORIZANTAL_ALIGNMENT, PROP_SET_MULTILINE_WORDWRAP_SCROLL
        /// </summary>
        /// <param name = "line">TODO</param>
        internal GuiCommand add(CommandType commandType, Object obj, int line, int number)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            guiCommand.line = line;

            switch (commandType)
            {
                case CommandType.PROP_SET_GRADIENT_STYLE:

                case CommandType.SET_WINDOW_STATE:
                    guiCommand.style = number;
                    break;

                case CommandType.PROP_SET_MIN_WIDTH:
                    guiCommand.width = number;
                    break;

                case CommandType.PROP_SET_MIN_HEIGHT:
                    guiCommand.height = number;
                    break;

                case CommandType.PROP_SET_SELECTION_MODE:
                    guiCommand.listboxSelectionMode = (ListboxSelectionMode)number;
                    break;

                default:
                    guiCommand.number = number;
                    break;
            }
            put(guiCommand);
            return guiCommand;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="obj"></param>
        /// <param name="line"></param>
        /// <param name="objectValue1"></param>
        /// <param name="objectValue2"></param>
        internal void add(CommandType commandType, Object obj, int line, Object objectValue1, Object objectValue2)
        {
            add(commandType, obj, line, objectValue1, objectValue2, false);
        }
        /// <summary>
        ///   PROP_SET_GRADIENT_COLOR, SET_DVCONTROL_DATASOURCE, PROP_SET_BACKGOUND_COLOR, PROP_SET_FONT
        /// </summary>
        /// <param name = "commandType"></param>
        /// <param name = "obj"></param>
        /// <param name = "line"></param>
        /// <param name = "objectValue1"></param>
        /// <param name = "objectValue2"></param>
        internal void add(CommandType commandType, Object obj, int line, Object objectValue1, Object objectValue2, bool bool1)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            guiCommand.line = line;

            switch (commandType)
            {
                case CommandType.PROP_SET_GRADIENT_COLOR:
                    guiCommand.mgColor = (MgColor)objectValue1;
                    guiCommand.mgColor1 = (MgColor)objectValue2;
                    break;

                case CommandType.INSERT_ROWS:
                case CommandType.REMOVE_ROWS:
                    guiCommand.number = (int)objectValue1;
                    guiCommand.number1 = (int)objectValue2;
                    guiCommand.Bool1 = bool1;
                    break;

                case CommandType.PROP_SET_CHECKED:
                    guiCommand.line = line;
                    guiCommand.number = (int)objectValue1;
                    guiCommand.Bool3 = (bool)objectValue2;
                    break;

                case CommandType.PROP_SET_SELECTION:
                    guiCommand.str = objectValue1.ToString();
                    guiCommand.intArray = (int[])objectValue2;
                    guiCommand.Bool1 = bool1;
                    break;

                case CommandType.APPLY_CHILD_WINDOW_PLACEMENT:
                    guiCommand.width = (int)objectValue1;
                    guiCommand.height = (int)objectValue2;
                    break;

                case CommandType.UPDATE_DVCONTROL_COLUMN:
                case CommandType.REJECT_DVCONTROL_COLUMN_CHANGES:
                    guiCommand.line = (int)line;
                    guiCommand.number = (int)objectValue1;
                    guiCommand.obj1 = objectValue2;
                    break;

                case CommandType.PROP_SET_BACKGOUND_COLOR:
                    guiCommand.mgColor = (MgColor)objectValue1;
                    guiCommand.number = (int)objectValue2;
                    break;

                case CommandType.PROP_SET_FONT:
                    guiCommand.mgFont = (MgFont)objectValue1;
                    guiCommand.number = (int)objectValue2;
                    break;

                default:
                    throw new ApplicationException("in GuiCommandQueue.add(): command type not handled: " + commandType);
            }
            put(guiCommand);
        }

        /// <summary>
        /// PROP_SET_FOCUS_COLOR, PROP_SET_HOVERING_COLOR, PROP_SET_VISITED_COLOR
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="obj"></param>
        /// <param name="line"></param>
        /// <param name="objectValue1"></param>
        /// <param name="objectValue2"></param>
        /// <param name="number"></param>
        internal void add(CommandType commandType, Object obj, int line, Object objectValue1, Object objectValue2, int number)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            guiCommand.line = line;
            guiCommand.mgColor = (MgColor)objectValue1;
            guiCommand.mgColor1 = (MgColor)objectValue2;
            guiCommand.number = number;

            put(guiCommand);
        }

        /// <summary>
        ///   PROP_SET_BACKGOUND_COLOR, PROP_SET_FOREGROUND_COLOR, PROP_SET_ALTENATING_COLOR
        ///   PROP_SET_STARTUP_POSITION
        /// </summary>
        /// <param name = "line">TODO PROP_SET_ROW_HIGHLIGHT_COLOR, PROP_SET_ROW_HIGHLIGHT_FGCOLOR : line not relevant
        ///   PROP_SET_FORM_BORDER_STYLE,SET_ALIGNMENT, SET_FRAMES_WIDTH, SET_FRAMES_HEIGHT, REORDER_COLUMNS
        /// </param>
        internal void add(CommandType commandType, Object obj, int line, Object objectValue)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            guiCommand.line = line;

            switch (commandType)
            {
                case CommandType.SET_ALIGNMENT:
                case CommandType.PROP_SET_CHECK_BOX_CHECKED:
                case CommandType.PROP_SET_STARTUP_POSITION:
                    guiCommand.number = (int)objectValue;
                    break;

                case CommandType.PROP_SET_FORM_BORDER_STYLE:
                    guiCommand.style = (int)objectValue;
                    break;

                case CommandType.PROP_SET_ROW_HIGHLIGHT_FGCOLOR:
                case CommandType.PROP_SET_ROW_HIGHLIGHT_BGCOLOR:
                case CommandType.PROP_SET_INACTIVE_ROW_HIGHLIGHT_BGCOLOR:
                case CommandType.PROP_SET_INACTIVE_ROW_HIGHLIGHT_FGCOLOR:
                case CommandType.PROP_SET_FOREGROUND_COLOR:
                case CommandType.PROP_SET_BORDER_COLOR:
                case CommandType.PROP_SET_ALTENATING_COLOR:
                case CommandType.PROP_SET_TITLE_COLOR:
                case CommandType.PROP_SET_DIVIDER_COLOR:
                case CommandType.PROP_SET_TITLE_FGCOLOR:
                case CommandType.PROP_SET_HOT_TRACK_COLOR:
                case CommandType.PROP_SET_HOT_TRACK_FGCOLOR:
                case CommandType.PROP_SET_SELECTED_TAB_COLOR:
                case CommandType.PROP_SET_SELECTED_TAB_FGCOLOR:
                case CommandType.PROP_SET_EDIT_HINT_COLOR:
                case CommandType.PROP_SET_ROW_BG_COLOR:
                    guiCommand.mgColor = (MgColor)objectValue;
                    break;

                case CommandType.PROP_SET_IMAGE_LIST_INDEXES:
                    guiCommand.intArray = (int[])objectValue;
                    break;

                case CommandType.SET_FRAMES_WIDTH:
                case CommandType.SET_FRAMES_HEIGHT:
                case CommandType.RESTORE_COLUMNS:
                    guiCommand.intList = (List<int>)objectValue;
                    break;

                case CommandType.REORDER_COLUMNS:
                    guiCommand.intArrayList = (List<int[]>)objectValue;
                    break;

                case CommandType.CREATE_ENTRY_IN_CONTROLS_MAP:
                    guiCommand.obj1 = objectValue;
                    break;

                case CommandType.PERFORM_DRAGDROP:
                case CommandType.UPDATE_DVCONTROL_ROW:
                case CommandType.ADD_DVCONTROL_HANDLER:
                case CommandType.CREATE_ROW_IN_DVCONTROL:
                case CommandType.DELETE_DVCONTROL_ROW:
                case CommandType.SET_DVCONTROL_ROW_POSITION:
                    guiCommand.obj1 = objectValue;
                    break;

                case CommandType.PROP_SET_EDIT_HINT:
                    guiCommand.str = (string)objectValue;
                    break;

                default:
                    throw new ApplicationException("in GuiCommandQueue.add(): command type not handled: " + commandType);
            }
            put(guiCommand);
        }

       internal void add(CommandType commandType, Object obj, int line, String operation, string value)
       {
         checkObject(obj);

         GuiCommand guiCommand = new GuiCommand(obj, commandType);
         guiCommand.Operation = operation;
         guiCommand.str = value;
         guiCommand.line = line;
         put(guiCommand);

      }
      /// <summary>
      ///   PROP_SET_TOOLTIP, PROP_SET_TEXT style: not relevant PROP_SET_WALLPAPER PROP_SET_IMAGE_FILE_NAME
      ///   PROP_SET_URL, PROP_SET_ICON_FILE_NAME : style isn't relevant
      ///   PROP_SET_CONTROL_NAME : style isn't relevant
      /// </summary>
      /// <param name = "line">TODO</param>
      internal void add(CommandType commandType, Object obj, int line, String str, int style)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);

            guiCommand.line = line;

            switch (commandType)
            {
                case CommandType.PROP_SET_ICON_FILE_NAME:
                case CommandType.PROP_SET_WALLPAPER:
                case CommandType.PROP_SET_IMAGE_FILE_NAME:
                case CommandType.PROP_SET_IMAGE_LIST:
                    guiCommand.fileName = str;
                    guiCommand.style = style;
                    break;

                case CommandType.SETDATA_FOR_DRAG:
                case CommandType.PROP_SET_IMAGE_DATA:
                    guiCommand.str = str;
                    guiCommand.style = style;
                    break;

                case CommandType.SET_PROPERTY:
                    guiCommand.Operation = str;
                    guiCommand.number = style;
                    break;

                default:
                    guiCommand.str = str;
                    break;
            }
            put(guiCommand);
        }

        /// <summary>
        /// SETDATA_FOR_DRAG
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="obj"></param>
        /// <param name="line">line no</param>
        /// <param name="str">string</param>
        /// <param name="userDropFormat">user defined format, if any.</param>
        /// <param name="style"></param>
        internal void add(CommandType commandType, Object obj, int line, String str, String userDropFormat, int style)
        {
            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            guiCommand.line = line;
            guiCommand.str = str;
            guiCommand.userDropFormat = userDropFormat;
            guiCommand.style = style;

            put(guiCommand);
        }
  
      internal void add(CommandType commandType, Object obj, String calledTaskTag, String subformControlName, string formName, string inputControls)
      {
         GuiCommand guiCommand = new GuiCommand(obj, commandType);       
         guiCommand.str = calledTaskTag;
         guiCommand.obj1 = subformControlName;
         guiCommand.userDropFormat = formName;
         guiCommand.fileName = inputControls;

         put(guiCommand);
      }

      /// <summary>
      ///   PROP_SET_IMAGE_DATA
      /// </summary>
      /// <param name = "line">TODO</param>
      internal void add(CommandType commandType, Object obj, int line, byte[] byteArray, int style)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);

            guiCommand.line = line;
            guiCommand.ByteArray = byteArray;
            guiCommand.style = style;
            put(guiCommand);
        }

        /// <summary>
        ///   PROP_SET_ITEMS_LIST,
        /// </summary>
        /// <param name = "line">TODO
        /// </param>
        internal void add(CommandType commandType, Object obj, int line, String[] displayList, bool bool1)
        {
            checkObject(obj);

            GuiCommand guiCommand = new GuiCommand(obj, commandType);
            guiCommand.itemsList = displayList;
            guiCommand.line = line;
            guiCommand.Bool1 = bool1;
            put(guiCommand);
        }

        /// <summary>
        ///   PROP_SET_MENU, REFRESH_MENU_ACTIONS
        /// </summary>
        internal void add(CommandType commandType, Object parentObj, GuiMgForm containerForm, MenuStyle menuStyle,
            GuiMgMenu guiMgMenu, bool parentTypeForm)
        {
            GuiCommand guiCommand = new GuiCommand(parentObj, containerForm, commandType);
            guiCommand.menu = guiMgMenu;
            guiCommand.menuStyle = menuStyle;
            guiCommand.Bool3 = parentTypeForm;
            guiCommand.line = GuiConstants.ALL_LINES;
            put(guiCommand);
        }

        /// <summary>
        ///   CREATE_MENU
        /// </summary>
        internal void add(CommandType commandType, Object parentObj, GuiMgForm containerForm, MenuStyle menuStyle,
            GuiMgMenu guiMgMenu, bool parentTypeForm, bool shouldShowPulldownMenu)
        {
            GuiCommand guiCommand = new GuiCommand(parentObj, containerForm, commandType);
            guiCommand.menu = guiMgMenu;
            guiCommand.menuStyle = menuStyle;
            guiCommand.Bool3 = parentTypeForm;
            guiCommand.line = GuiConstants.ALL_LINES;
            guiCommand.Bool1 = shouldShowPulldownMenu;
            put(guiCommand);
        }

        /// <summary>
        ///   CREATE_MENU_ITEM
        /// </summary>
        /// <param name = "commandType"></param>
        /// <param name = "parentObj"></param>
        /// <param name = "menuStyle"></param>
        /// <param name = "menuEntry"></param>
        /// <param name = "form"></param>
        /// <param name = "index"></param>
        internal void add(CommandType commandType, Object parentObj, MenuStyle menuStyle, GuiMenuEntry menuEntry,
            GuiMgForm guiMgForm, int index)
        {
            GuiCommand guiCommand = new GuiCommand(parentObj, guiMgForm, commandType);
            guiCommand.menuEntry = menuEntry;
            guiCommand.menuStyle = menuStyle;
            guiCommand.line = index;
            put(guiCommand);
        }

        /// <summary>
        ///   DELETE_MENU_ITEM
        /// </summary>
        /// <param name = "commandType"></param>
        /// <param name = "parentObj"></param>
        /// <param name = "menuStyle"></param>
        /// <param name = "menuEntry"></param>
        /// <param name = "menuItemReference"></param>
        internal void add(CommandType commandType, Object parentObj, MenuStyle menuStyle, GuiMenuEntry menuEntry)
        {
            GuiCommand guiCommand = new GuiCommand(parentObj, commandType);
            guiCommand.menuEntry = menuEntry;
            guiCommand.menuStyle = menuStyle;
            put(guiCommand);
        }

        /// <summary>
        ///   PROP_SET_CHECKED PROP_SET_ENABLE PROP_SET_VISIBLE PROP_SET_MENU_ENABLE PROP_SET_MENU_VISIBLE Above
        ///   properties for menu entry
        /// </summary>
        /// <param name = "commandType"></param>
        /// <param name = "menuEntry">TODO</param>
        /// <param name = "menuEntry"></param>
        /// <param name = "value"></param>
        internal void add(CommandType commandType, MenuReference mnuRef, GuiMenuEntry menuEntry, Object val)
        {
            GuiCommand guiCommand = new GuiCommand(mnuRef, commandType);

            guiCommand.menuEntry = menuEntry;

            if (val is Boolean)
                guiCommand.Bool3 = ((Boolean)val);
            else
                guiCommand.str = ((String)val);
            put(guiCommand);
        }

        /// <summary>
        ///   CREATE_TOOLBAR
        /// </summary>
        /// <param name = "commandType"></param>
        /// <param name = "form"></param>
        /// <param name = "newToolbar"></param>
        internal void add(CommandType commandType, GuiMgForm form, Object newToolbar)
        {
            GuiCommand guiCommand = new GuiCommand(form, newToolbar, commandType);
            put(guiCommand);
        }

        /// <summary>
        ///   CREATE_TOOLBAR_ITEM, DELETE_TOOLBAR_ITEM
        /// </summary>
        /// <param name = "commandType"></param>
        /// <param name = "toolbar">is the ToolBar to which we add a new item (placed in parentObject)</param>
        /// <param name = "menuEntry">is the menuEntry for which we create this toolitem</param>
        /// <param name = "index">is the index of the new object in the toolbar (placed in line)</param>
        internal void add(CommandType commandType, Object toolbar, GuiMgForm form, GuiMenuEntry menuEntry, int index)
        {
            GuiCommand guiCommand = new GuiCommand(toolbar, form, commandType);
            guiCommand.menuEntry = menuEntry;
            guiCommand.line = index;
            put(guiCommand);
        }

        /// <summary>
        ///   Verifies that the object is either MgForm or MgControl and throws Error if not.
        /// </summary>
        /// <param name = "object">the object to check</param>
        internal void checkObject(Object obj)
        {
            if (!(obj == null || obj is GuiMgForm || obj is GuiMgControl || obj is MenuReference || obj is MgCursors || obj is MgTimer))
#if !PocketPC
                if (!(obj is Console.ConsoleWindow || obj is MGDataTable || obj is Dictionary<MgControlBase, Dictionary<string, object>>))
#else
            if (!(obj is List<MgFormBase>))
#endif
                    throw new ApplicationException(string.Format("in GuiCommandQueue.add(): object {0} is not MgForm or MgControl or MenuReference or MgCursors or MgTimer",
                        obj == null ? obj : obj.GetType()));
        }

        /// <summary>
        ///   Function returns true if control is supported.///
        /// </summary>
        /// <param name = "obj"> MgControl object</param>
        /// <returns> whether supported or not</returns>
        internal virtual bool isSupportedControl(GuiCommand guiCommand)
        {
            return true;
        }

        /// <summary>
        ///   execute the command type
        /// </summary>
        internal void Run()
        {
            GuiThreadIsAvailableToProcessCommands = true;
            GuiCommand guiCommand = null;

            Manager.ContextIDGuard contextIDGuard = new Manager.ContextIDGuard();
            try
            {
                while (!_commandsQueue.isEmpty())
                {
                    guiCommand = (GuiCommand)_commandsQueue.get();
                    // when command belongs to already closed window we should not execute it
                    if (isDisposedObjects(guiCommand) || !isSupportedControl(guiCommand))
                        continue;

                    try
                    {
                        contextIDGuard.SetCurrent(guiCommand.contextID);
                        switch (guiCommand.CommandType)
                        {
                            /**
                            * create form
                            */
                            case CommandType.CREATE_FORM:
                                createForm(guiCommand);
                                break;

                            case CommandType.INITIAL_FORM_LAYOUT:
                                InitialFormLayout(guiCommand);
                                break;

                            case CommandType.SHOW_FORM:
                                ShowForm(guiCommand);
                                break;

                            case CommandType.CREATE_PLACEMENT_LAYOUT:
                                createLayout(guiCommand);
                                break;

                            case CommandType.SUSPEND_LAYOUT:
                                SuspendLayout(guiCommand);
                                break;

                            case CommandType.RESUME_LAYOUT:
                                ResumeLayout(guiCommand);
                                break;

                            case CommandType.SUSPEND_PAINT:
                                SuspendPaint(guiCommand);
                                break;

                            case CommandType.RESUME_PAINT:
                                ResumePaint(guiCommand);
                                break;

                            case CommandType.EXECUTE_LAYOUT:
                                executeLayout(guiCommand);
                                break;

                            /**
                            * create controls
                            */
                            case CommandType.CREATE_LABEL:
                            case CommandType.CREATE_EDIT:
                            case CommandType.CREATE_COMBO_BOX:
                            case CommandType.CREATE_BUTTON:
                            case CommandType.CREATE_LIST_BOX:
                            case CommandType.CREATE_RADIO_CONTAINER:
                            case CommandType.CREATE_RADIO_BUTTON:
                            case CommandType.CREATE_IMAGE:
                            case CommandType.CREATE_CHECK_BOX:
                            case CommandType.CREATE_TAB:
                            case CommandType.CREATE_TABLE:
                            case CommandType.CREATE_COLUMN:
                            case CommandType.CREATE_SUB_FORM:
                            case CommandType.CREATE_BROWSER:
                            case CommandType.CREATE_GROUP:
                            case CommandType.CREATE_TREE:
                            case CommandType.CREATE_FRAME_SET:
                            case CommandType.CREATE_FRAME_FORM:
                            case CommandType.CREATE_CONTAINER:
                            case CommandType.CREATE_STATUS_BAR:
                            case CommandType.CREATE_RICH_EDIT:
                            case CommandType.CREATE_RICH_TEXT:
                            case CommandType.CREATE_LINE:
                            case CommandType.CREATE_DOTNET:
                                createControl(guiCommand);
                                break;

                            /**
                            * dispose object
                            */
                            case CommandType.DISPOSE_OBJECT:
                                disposeObject(guiCommand);
                                break;

                            case CommandType.MOVE_ABOVE:
                                moveAbove(guiCommand);
                                break;

                            /**
                            * update properties
                            */
                            case CommandType.PROP_SET_ENABLE:
                                setEnable(guiCommand);
                                break;

                            case CommandType.PROP_SET_TEXT_SIZE_LIMIT:
                                setTextSizeLimit(guiCommand);
                                break;

                            case CommandType.PROP_SET_CONTROL_NAME:
                                SetControlName(guiCommand);
                                break;

                            case CommandType.PROP_SET_TEXT:
                                setText(guiCommand);
                                break;

                            case CommandType.PROP_SET_READ_ONLY:
                                setReadonly(guiCommand);
                                break;

                            case CommandType.PROP_SET_ITEMS_LIST:
                                setItemsList(guiCommand);
                                break;

                            case CommandType.PROP_SET_VISIBLE_LINES:
                                setVisibleLines(guiCommand);
                                break;

                            case CommandType.PROP_SET_TOOLTIP:
                                setTooltip(guiCommand);
                                break;

                            case CommandType.PROP_SET_DEFAULT_BUTTON:
                                setDefaultButton(guiCommand);
                                break;

                            case CommandType.PROP_SET_STARTUP_POSITION:
                                setStartupPosition(guiCommand);
                                break;

                            case CommandType.PROP_SET_BOUNDS:
                                setBounds(guiCommand);
                                break;

                            case CommandType.PROP_SET_PLACEMENT:
                                setPlacementData(guiCommand);
                                break;

                            case CommandType.PROP_SET_BORDER_COLOR:
                                setBorderColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_BACKGOUND_COLOR:
                                setBackgroundColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_FOREGROUND_COLOR:
                                setForegroundColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_FOCUS_COLOR:
                                setFocusColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_HOVERING_COLOR:
                                setHoveringColor(guiCommand);
                                break;

                            case CommandType.SET_TAG_DATA_LINK_VISITED:
                                SetLinkVisited(guiCommand);
                                break;

                            case CommandType.PROP_SET_VISITED_COLOR:
                                setVisitedColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_ALTENATING_COLOR:
                                setAlternatedColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_TITLE_COLOR:
                                setTitleColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_TITLE_FGCOLOR:
                                setTitleFgColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_HOT_TRACK_COLOR:
                                setHotTrackColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_HOT_TRACK_FGCOLOR:
                                setHotTrackFgColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_SELECTED_TAB_COLOR:
                                setSelectedTabColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_SELECTED_TAB_FGCOLOR:
                                setSelectedTabFgColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_DIVIDER_COLOR:
                                setDividerColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_COLOR_BY:
                                setColorBy(guiCommand);
                                break;

                            case CommandType.PROP_SET_VISIBLE:
                                setVisible(guiCommand);
                                break;

                            case CommandType.PROP_SET_MIN_WIDTH:
                                setMinWidth(guiCommand);
                                break;

                            case CommandType.PROP_SET_MIN_HEIGHT:
                                setMinHeight(guiCommand);
                                break;

                            case CommandType.PROP_SET_WALLPAPER:
                            case CommandType.PROP_SET_IMAGE_FILE_NAME:
                                setImageFileName(guiCommand);
                                break;

                            case CommandType.PROP_SET_GRADIENT_COLOR:
                                setGradientColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_GRADIENT_STYLE:
                                setGradientStyle(guiCommand);
                                break;

                            case CommandType.PROP_SET_IMAGE_DATA:
                                setImageData(guiCommand);
                                break;

                            case CommandType.PROP_SET_IMAGE_LIST:
                                setImageList(guiCommand);
                                break;

                            case CommandType.PROP_SET_ICON_FILE_NAME:
                                setIconFileName(guiCommand);
                                break;

                            case CommandType.PROP_SET_FONT:
                                setFont(guiCommand);
                                break;

                            case CommandType.SET_FOCUS:
                                setFocus(guiCommand);
                                break;

                            case CommandType.UPDATE_TMP_EDITOR_INDEX:
                                updateTmpEditorIndex(guiCommand);
                                break;

                            case CommandType.PROP_SET_RIGHT_TO_LEFT:
                                setRightToLeft(guiCommand);
                                break;

                            case CommandType.START_TIMER:
                                startTimer(guiCommand);
                                break;

                            case CommandType.STOP_TIMER:
                                stopTimer(guiCommand);
                                break;

                            case CommandType.PROP_SET_EDIT_HINT:
                                setEditHint(guiCommand);
                                break;

                            case CommandType.PROP_SET_EDIT_HINT_COLOR:
                                setEditHintColor(guiCommand);
                                break;

                            case CommandType.CLOSE_FORM:
                            {
                                // Defect# 129179
                                //
                                // Whenever a form is closed, framework activates a form based on some logic --- parent of the closed form in normal situation or last active MDI child in case of MDI Frame.
                                // But we always need the parent to be activated and hence we put two commands : CLOSE_FORM,  ACTIVATE_FORM(MODAL TASK's parent) while closing a form.
                                //
                                // After executing CLOSE_FORM of MODAL, it's parent is activated correctly through ACTIVATE_FORM. And then framework start terminating ShowDialog().
                                // While doing so it activate a form as per its own implementation. Hence the activation done by us gets overridden by the framework and the 
                                // desire Form is not seen activated.
                                //
                                // Solution : Parent form should be activated only after control returns from ShowDialog().
                                //   For e.g. :
                                //          RUN_1() --> executing OPEN_FORM - Form.ShowDialog() ----> executing commands in RUN_2() : CLOSE_FORM, ACTIVATE_FORM(MODAL->Parent)
                                //
                                //   After fix : 
                                //
                                //          RUN_1() --> executing OPEN_FORM - Form.ShowDialog() ----> executing commands in RUN_2() : CLOSE_FORM (return/break RUN_2).
                                //          RUN_1() --> after finished handling OPEN_FORM       ----> execute ACTIVATE_FORM(MODAL->Parent).
                                //
                                if (closeForm(guiCommand))
                                    return;
                            }
                                break;

                            case CommandType.ACTIVATE_FORM:
                                activateForm(guiCommand);
                                break;

                            case CommandType.SET_LAST_IN_CONTEXT:
                                setLastInContext(guiCommand);
                                break;

                            case CommandType.REMOVE_SUBFORM_CONTROLS:
                                removeCompositeControls(guiCommand);
                                break;

                            case CommandType.BEEP:
                                beep();
                                break;

                            case CommandType.PROP_SET_CHECK_BOX_CHECKED:
                                setCheckBoxCheckState(guiCommand);
                                break;

                            case CommandType.PROP_SET_CHECKED:
                                setChecked(guiCommand);
                                break;

                            case CommandType.PROP_SET_SELECTION:
                                setSelection(guiCommand);
                                break;

                            case CommandType.PROP_SET_LAYOUT_NUM_COLUMN:
                                setLayoutNumColumns(guiCommand);
                                break;

                            case CommandType.PROP_SET_LINE_VISIBLE:
                                setLinesVisible(guiCommand);
                                break;

                            case CommandType.PROP_SET_RESIZABLE:
                                setResizable(guiCommand);
                                break;

                            case CommandType.PROP_SET_ROW_HEIGHT:
                                setRowHeight(guiCommand);
                                break;

                            case CommandType.PROP_SET_TITLE_HEIGHT:
                                setTitleHeight(guiCommand);
                                break;

                            case CommandType.PROP_SET_BOTTOM_POSITION_INTERVAL:
                                setBottomPositionInterval(guiCommand);
                                break;

                            case CommandType.PROP_SET_ALLOW_REORDER:
                                setAllowReorder(guiCommand);
                                break;

                            case CommandType.PROP_SET_SORTABLE_COLUMN:
                                setSortableColumn(guiCommand);
                                break;

                            case CommandType.PROP_SET_COLUMN_FILTER:
                                setFilterableColumn(guiCommand);
                                break;

                            case CommandType.PROP_SET_RIGHT_BORDER:
                                setColumnRightBorder(guiCommand);
                                break;

                            case CommandType.PROP_SET_TOP_BORDER:
                                setColumnTopBorder(guiCommand);
                                break;


                            case CommandType.PROP_SET_COLUMN_PLACMENT:
                                setPlacement(guiCommand);
                                break;

                            case CommandType.SET_COLUMN_ORG_WIDTH:
                                setOrgWidth(guiCommand);
                                break;

                            case CommandType.SET_COLUMN_START_POS:
                                setStartPos(guiCommand);
                                break;

                            case CommandType.SET_TABLE_ITEMS_COUNT:
                                SetTableItemsCount(guiCommand);
                                break;

                            case CommandType.SET_TABLE_VIRTUAL_ITEMS_COUNT:
                                SetTableVirtualItemsCount(guiCommand);
                                break;

                            case CommandType.SET_TABLE_VSCROLL_THUMB_POS:
                                SetVScrollThumbPos(guiCommand);
                                break;

                            case CommandType.SET_TABLE_VSCROLL_PAGE_SIZE:
                                SetVScrollPageSize(guiCommand);
                                break;

                            case CommandType.SET_RECORDS_BEFORE_CURRENT_VIEW:
                                SetRecordsBeforeCurrentView(guiCommand);
                                break;

                            case CommandType.INSERT_ROWS:
                                InsertRows(guiCommand);
                                break;

                            case CommandType.REMOVE_ROWS:
                                RemoveRows(guiCommand);
                                break;

                            case CommandType.TOGGLE_ALTERNATE_COLOR_FOR_FIRST_ROW:
                                ToggleAlternateColorForFirstRow(guiCommand);
                                break;

                            case CommandType.SET_TABLE_INCLUDES_FIRST:
                                SetTableIncludesFirst(guiCommand);
                                break;

                            case CommandType.SET_TABLE_INCLUDES_LAST:
                                SetTableIncludesLast(guiCommand);
                                break;

                            case CommandType.SET_TABLE_TOP_INDEX:
                                SetTableTopIndex(guiCommand);
                                break;

                            case CommandType.SET_SELECTION_INDEX:
                                SetSelectionIndex(guiCommand);
                                break;

                            case CommandType.REFRESH_TABLE:
                                refreshTable(guiCommand);
                                break;

                            case CommandType.INVALIDATE_TABLE:
                                invalidateTable(guiCommand);
                                break;

                            case CommandType.CREATE_TABLE_ROW:
                                createTableRow(guiCommand);
                                break;

                            case CommandType.UNDO_CREATE_TABLE_ROW:
                                undoCreateTableRow(guiCommand);
                                break;

                            case CommandType.SET_TABLE_ROW_VISIBILITY:
                                SetTableRowVisibility(guiCommand);
                                break;

                            case CommandType.VALIDATE_TABLE_ROW:
                                validateTableRow(guiCommand);
                                break;

                            case CommandType.CLEAR_TABLE_COLUMNS_SORT_MARK:
                                clearTableColumnsSortMark(guiCommand);
                                break;

                            case CommandType.REFRESH_TMP_EDITOR:
                                refreshTmpEditor(guiCommand);
                                break;

                            case CommandType.PROP_SET_URL:
                                setUrl(guiCommand);
                                break;

                            case CommandType.PROP_SET_ROW_HIGHLIGHT_BGCOLOR:
                                setRowHighlightBgColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_ROW_HIGHLIGHT_FGCOLOR:
                                setRowHighlightFgColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_INACTIVE_ROW_HIGHLIGHT_BGCOLOR:
                                setInactiveRowHighlightBgColor(guiCommand);
                                break;

                            case CommandType.PROP_SET_ACTIVE_ROW_HIGHLIGHT_STATE:
                                setActiveRowHighlightState(guiCommand);
                                break;

                            case CommandType.PROP_SET_INACTIVE_ROW_HIGHLIGHT_FGCOLOR:
                                setInactiveRowHighlightFgColor(guiCommand);
                                break;

                            case CommandType.SELECT_TEXT:
                                selectText(guiCommand);
                                break;

                            case CommandType.PROP_SET_MENU:
                                setMenu(guiCommand);
                                break;

                            case CommandType.PROP_RESET_MENU:
                                resetMenu(guiCommand);
                                break;

                            case CommandType.CREATE_MENU:
                                createMenu(guiCommand);
                                break;

                            case CommandType.REFRESH_MENU_ACTIONS:
                                RefreshMenuActions(guiCommand);
                                break;

                            case CommandType.CREATE_MENU_ITEM:
                                createMenuItem(guiCommand);
                                break;

                            case CommandType.DELETE_MENU_ITEM:
                                deleteMenuItem(guiCommand);
                                break;

                            case CommandType.UPDATE_MENU_VISIBILITY:
                                updateMenuVisibility(guiCommand);
                                break;

                            case CommandType.DELETE_MENU:
                                deleteMenu(guiCommand);
                                break;

                            case CommandType.CREATE_TOOLBAR:
                                createToolbar(guiCommand);
                                break;

                            case CommandType.SET_TOOLBAR:
                                setToolBar(guiCommand);
                                break;

                            case CommandType.DELETE_TOOLBAR:
                                deleteToolbar(guiCommand);
                                break;

                            case CommandType.CREATE_TOOLBAR_ITEM:
                                createToolbarItem(guiCommand);
                                break;

                            case CommandType.DELETE_TOOLBAR_ITEM:
                                deleteToolbarItem(guiCommand);
                                break;

                            case CommandType.SET_WINDOW_STATE:
                                setWindowState(guiCommand);
                                break;

                            case CommandType.PROP_SET_SB_PANE_WIDTH:
                                setSBPaneWidth(guiCommand);
                                break;

                            case CommandType.CREATE_SB_IMAGE:
                            case CommandType.CREATE_SB_LABEL:
                                createSBPane(guiCommand);
                                break;

                            case CommandType.PROP_SET_MENU_ENABLE:
                                setMenuEnable(guiCommand);
                                break;

                            case CommandType.PROP_SET_AUTO_WIDE:
                                setAutoWide(guiCommand);
                                break;

                            case CommandType.CREATE_TREE_NODE:
                                createTreeNode(guiCommand);
                                break;

                            case CommandType.MOVE_TREE_NODE:
                                moveTreeNode(guiCommand);
                                break;

                            case CommandType.SET_EXPANDED:
                                setExpanded(guiCommand);
                                break;

                            case CommandType.SET_CHILDREN_RETRIEVED:
                                setChildrenRetrieved(guiCommand);
                                break;

                            case CommandType.DELETE_TREE_NODE:
                                deleteTreeNode(guiCommand);
                                break;

                            case CommandType.PROP_HORIZONTAL_PLACEMENT:
                                setHorizontalPlacement(guiCommand);
                                break;

                            case CommandType.PROP_VERTICAL_PLACEMENT:
                                setVerticalPlacement(guiCommand);
                                break;

                            case CommandType.SHOW_TMP_EDITOR:
                                showTmpEditor(guiCommand);
                                break;

                            case CommandType.PROP_SET_TRANSLATOR:
                                setImeMode(guiCommand);
                                break;

                            case CommandType.PROP_SET_HORIZANTAL_ALIGNMENT:
                                setHorizontalAlignment(guiCommand);
                                break;

                            case CommandType.PROP_SET_VERTICAL_ALIGNMENT:
                                setVerticalAlignment(guiCommand);
                                break;

                            case CommandType.PROP_SET_RADIO_BUTTON_APPEARANCE:
                                setRadioButtonAppearance(guiCommand);
                                break;

                            case CommandType.PROP_SET_THREE_STATE:
                                setThreeState(guiCommand);
                                break;

                            case CommandType.PROP_SET_MULTILINE:
                                setMultiLine(guiCommand);
                                break;

                            case CommandType.PROP_SET_PASSWORD_EDIT:
                                setPasswordEdit(guiCommand);
                                break;

                            case CommandType.PROP_SET_MULTILINE_WORDWRAP_SCROLL:
                                setMultilineWordWrapScroll(guiCommand);
                                break;

                            case CommandType.PROP_SET_MULTILINE_VERTICAL_SCROLL:
                                setMultilineVerticalScroll(guiCommand);
                                break;

                            case CommandType.PROP_SET_MULTILINE_ALLOW_CR:
                                setMultilineAllowCR(guiCommand);
                                break;

                            case CommandType.PROP_SET_BORDER_STYLE:
                                setBorderStyle(guiCommand);
                                break;

                            case CommandType.PROP_SET_STYLE_3D:
                                setStyle3D(guiCommand);
                                break;

                            case CommandType.PROP_SET_CHECKBOX_MAIN_STYLE:
                                setCheckboxMainStyle(guiCommand);
                                break;

                            case CommandType.PROP_SET_BORDER:
                                setBorder(guiCommand);
                                break;

                            case CommandType.PROP_SET_FORM_BORDER_STYLE:
                                setFormBorderStyle(guiCommand);
                                break;

                            case CommandType.PROP_SET_MINBOX:
                                setMinBox(guiCommand);
                                break;

                            case CommandType.PROP_SET_MAXBOX:
                                setMaxBox(guiCommand);
                                break;

                            case CommandType.PROP_SET_SYSTEM_MENU:
                                setSystemMenu(guiCommand);
                                break;

                            case CommandType.PROP_SET_TITLE_BAR:
                                setTitleBar(guiCommand);
                                break;

                            case CommandType.PROP_SHOW_FULL_ROW:
                                showFullRow(guiCommand);
                                break;

                            case CommandType.ORDER_MG_SPLITTER_CONTAINER_CHILDREN:
                                orderMgSplitterContainerChildren(guiCommand);
                                break;

                            case CommandType.PROP_SHOW_BUTTONS:
                                showButtons(guiCommand);
                                break;

                            case CommandType.PROP_HOT_TRACK:
                                hotTrack(guiCommand);
                                break;

                            case CommandType.PROP_LINES_AT_ROOT:
                                linesAtRoot(guiCommand);
                                break;

                            case CommandType.PROP_SHOW_SCROLLBAR:
                                showScrollbar(guiCommand);
                                break;

                            case CommandType.PROP_LINE_DIVIDER:
                                showLineDividers(guiCommand);
                                break;

                            case CommandType.PROP_COLUMN_DIVIDER:
                                showColumnDividers(guiCommand);
                                break;

                            case CommandType.PROP_ROW_HIGHLITING_STYLE:
                                rowHighlitingStyle(guiCommand);
                                break;

                            case CommandType.SET_ENV_ACCESS_TEST:
                                setEnvAccessTest(guiCommand);
                                break;

                            case CommandType.SET_ENV_LAMGUAGE:
                                setLanguage(guiCommand);
                                break;

                            case CommandType.SET_ENV_SPECIAL_TEXT_SIZE_FACTORING:
                                setSpecialTextSizeFactoring(guiCommand);
                                break;

                            case CommandType.SET_ENV_SPECIAL_FLAT_EDIT_ON_CLASSIC_THEME:
                                setSpecialFlatEditOnClassicTheme(guiCommand);
                                break;

                            case CommandType.SET_ENV_SPECIAL_SWIPE_FLICKERING_REMOVAL:
                                setSpecialSwipeFlickeringRemoval(guiCommand);
                                break;

                            case CommandType.SET_ENV_TOOLTIP_TIMEOUT:
                                setEnvTooltipTimeout(guiCommand);
                                break;

                            case CommandType.PROP_SET_LINE_STYLE:
                                setLineStyle(guiCommand);
                                break;

                            case CommandType.PROP_SET_LINE_WIDTH:
                                setLineWidth(guiCommand);
                                break;

                            case CommandType.PROP_SET_LINE_DIRECTION:
                                setLineDirection(guiCommand);
                                break;

                            case CommandType.PROP_TAB_CONTROL_SIDE:
                                setTabControlSide(guiCommand);
                                break;

                            case CommandType.PROP_SET_TAB_SIZE_MODE:
                                setTabSizeMode(guiCommand);
                                break;

                            case CommandType.PROP_SET_EXPANDED_IMAGEIDX:
                                setExpandedImageIdx(guiCommand);
                                break;

                            case CommandType.PROP_SET_COLLAPSED_IMAGEIDX:
                                setCollapsedImageIdx(guiCommand);
                                break;

                            case CommandType.PROP_SET_PARKED_IMAGEIDX:
                                setParkedExpandedImageIdx(guiCommand);
                                break;

                            case CommandType.PROP_SET_PARKED_COLLAPSED_IMAGEIDX:
                                setParkedCollapsedImageIdx(guiCommand);
                                break;

                            case CommandType.PROP_SET_IMAGE_LIST_INDEXES:
                                setImageListIndexes(guiCommand);
                                break;

                            case CommandType.COMBO_DROP_DOWN:
                                comboDropDown(guiCommand);
                                break;

                            case CommandType.SET_ACTIVETE_KEYBOARD_LAYOUT:
                                activateKeyboardLayout(guiCommand);
                                break;

                            case CommandType.ALLOW_UPDATE:
                                allowUpdate(guiCommand);
                                break;


                            case CommandType.SET_ALIGNMENT:
                                setAlignment(guiCommand);
                                break;

                            case CommandType.BULLET:
                                setBullet(guiCommand);
                                break;

                            case CommandType.INDENT:
                                setIndent(guiCommand);
                                break;

                            case CommandType.UNINDENT:
                                setUnindent(guiCommand);
                                break;

                            case CommandType.CHANGE_COLOR:
                                ChangeColor(guiCommand);
                                break;

                            case CommandType.CHANGE_FONT:
                                ChangeFont(guiCommand);
                                break;

                            case CommandType.CHANGE_COLUMN_SORT_MARK:
                                ChangeColumnSortMark(guiCommand);
                                break;
#if PocketPC
                  case CommandType.RESUME_SHOW_FORMS:
                     onResumeShowForms(guiCommand);
                     break;
#endif
                            case CommandType.SET_CURRENT_CURSOR:
                                setCurrentCursor(guiCommand);
                                break;

                            case CommandType.SET_FRAMES_WIDTH:
                                setFramesWidth(guiCommand);
                                break;

                            case CommandType.SET_FRAMES_HEIGHT:
                                setFramesHeight(guiCommand);
                                break;

                            case CommandType.SET_WINDOWSTATE:
                                setFormMaximized(guiCommand);
                                break;

                            case CommandType.REORDER_COLUMNS:
                                reorderColumns(guiCommand);
                                break;

                            case CommandType.RESTORE_COLUMNS:
                                restoreColumns(guiCommand);
                                break;

#if !PocketPC
                            case CommandType.PROP_SET_ALLOW_DRAG:
                                SetAllowDrag(guiCommand);
                                break;

                            case CommandType.PROP_SET_ALLOW_DROP:
                                SetAllowDrop(guiCommand);
                                break;

                            case CommandType.SETDATA_FOR_DRAG:
                                SetDataForDrag(guiCommand);
                                break;

                            case CommandType.PERFORM_DRAGDROP:
                                PerformDragDrop(guiCommand);
                                break;
#endif
                            case CommandType.PROP_SET_SELECTION_MODE:
                                SetSelectionMode(guiCommand);
                                break;

                            case CommandType.ENABLE_XPTHEMES:
                                EnableXPThemes(guiCommand);
                                break;

                            case CommandType.APPLY_CHILD_WINDOW_PLACEMENT:
                                ApplyChildWindowPlacement(guiCommand);
                                break;

                            case CommandType.REGISTER_DN_CTRL_VALUE_CHANGED_EVENT:
                                RegisterDNControlValueChangedEvent(guiCommand);
                                break;

                            case CommandType.PROP_SET_ROW_PLACEMENT:
                                SetRowPlacement(guiCommand);
                                break;

                            case CommandType.SET_TABLE_ORG_ROW_HEIGHT:
                                SetTableOrgRowHeight(guiCommand);
                                break;

                            case CommandType.CREATE_ENTRY_IN_CONTROLS_MAP:
                                CreateControlsMapEntry(guiCommand);
                                break;

                            case CommandType.REMOVE_ENTRY_FROM_CONTROLS_MAP:
                                RemoveControlsMapEntry(guiCommand);
                                break;

                            case CommandType.PROCESS_PRESS_EVENT:
                                ProcessPressEvent(guiCommand);
                                break;

                            case CommandType.SET_MARKED_ITEM_STATE:
                                SetMarkedItemState(guiCommand);
                                break;

                            case CommandType.PROP_SET_TOP_BORDER_MARGIN:
                                SetTopBorderMargin(guiCommand);
                                break;

                            case CommandType.PROP_SET_FILL_WIDTH:
                                SetFillWidth(guiCommand);
                                break;

                            case CommandType.PROP_SET_MULTI_COLUMN_DISPLAY:
                                SetMultiColumnDisplay(guiCommand);
                                break;

                            case CommandType.PROP_SET_SHOW_ELLIPSIS:
                                SetShowEllipsis(guiCommand);
                                break;


                            case CommandType.PROP_SET_TITLE_PADDING:
                                SetTitlePadding(guiCommand);
                                break;

#if !PocketPC
                            case CommandType.UPDATE_DVCONTROL_ROW:
                                OnUpdateDVControlRow(guiCommand);
                                break;

                            case CommandType.CREATE_ROW_IN_DVCONTROL:
                                OnCreateRowInDVControl(guiCommand);
                                break;

                            case CommandType.DELETE_DVCONTROL_ROW:
                                OnDeleteDVControlRow(guiCommand);
                                break;

                            case CommandType.ADD_DVCONTROL_HANDLER:
                                OnAddDVControlHandler(guiCommand);
                                break;

                            case CommandType.REMOVE_DVCONTROL_HANDLER:
                                OnRemoveDVControlHandler(guiCommand);
                                break;

                            case CommandType.UPDATE_DVCONTROL_COLUMN:
                                OnUpdateDVControlColumn(guiCommand);
                                break;

                            case CommandType.SET_DVCONTROL_ROW_POSITION:
                                OnSetDVControlRowPosition(guiCommand);
                                break;

                            case CommandType.REJECT_DVCONTROL_COLUMN_CHANGES:
                                OnRejectDVControlColumnChanges(guiCommand);
                                break;

                            case CommandType.SET_DESIGNER_VALUES:
                                OnSetDesignerValues(guiCommand);
                                break;
#endif
                            case CommandType.RECALCULATE_TABLE_COLORS:
                                OnRecalculateTableColors(guiCommand);
                                break;

                            case CommandType.RECALCULATE_TABLE_FONTS:
                                OnRecalculateTableFonts(guiCommand);
                                break;

                            case CommandType.SET_SHOULD_APPLY_PLACEMENT_TO_HIDDEN_COLUMNS:
                                OnSetShouldApplyPlacementToHiddenColumns(guiCommand);
                                break;

                            case CommandType.PROP_SET_ROW_BG_COLOR:
                                OnSetRowBGColor(guiCommand);
                                break;

                            case CommandType.SET_CARET:
                                SetCaret(guiCommand);
                                break;

                            default:
                                throw new ApplicationException("in GuiCommandQueue.run(): command type not handled: " +
                                                               guiCommand.CommandType);
                        }
                    }
                    catch (Exception ex)
                    {
                        Events.WriteExceptionToLog(ex);
                    }
                }
            }
            finally
            {
                contextIDGuard.Dispose();
                GuiThreadIsAvailableToProcessCommands = false;
            }
        }

        internal delegate void GuiCommandsDelegate();

        /// <summary>
        ///   execute all pending commands, asynchronously
        /// </summary>
        internal void beginInvoke()
        {
            JSBridge.Instance.executeCommands(Commands.GetCommands());
            // If Gui thread is already processing commands, then no need to invoke Run() once again.
            // Existing instance of Run() handles all commands in the queue.
            //if (!GuiThreadIsAvailableToProcessCommands)
            //   GUIMain.getInstance().beginInvoke(new GuiCommandsDelegate(Run));
        }

        /// <summary>
        ///   execute all pending commands, synchronously
        /// </summary>
        internal void invoke()
        {
           JSBridge.Instance.executeCommands(Commands.GetCommands());
         //GUIMain.getInstance().invoke(new GuiCommandsDelegate(Run));
      }

        /// <summary>
        ///   checks if command belongs to disposed window This method performs the check
        /// </summary>
        /// <param name = "guiCommand">guiCommand</param>
        /// <returns> true if object for operation is disposed</returns>
        internal bool isDisposedObjects(GuiCommand guiCommand)
        {
            if (guiCommand.CommandType == CommandType.CREATE_FORM && guiCommand.parentObject == null)
                return false; // open shell without parent
            if (guiCommand.parentObject != null)
                return objectDisposed(guiCommand.parentObject);
            else if (guiCommand.obj != null)
                return objectDisposed(guiCommand.obj);

            return false;
        }

        /// <summary>
        ///   check if object is null or disposed
        /// </summary>
        /// <param name = "object"></param>
        /// <returns></returns>
        internal bool objectDisposed(Object obj)
        {
            return false;
        }

        internal virtual void createSBPane(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("createSBPane - Not Implemented Yet");
        }

        internal virtual void createTreeNode(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("createTreeNode - Not Implemented Yet");
        }

        internal virtual void moveTreeNode(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("moveTreeNode - Not Implemented Yet");
        }

        internal virtual void deleteMenu(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("deleteMenu - Not Implemented Yet");
        }

        internal virtual void updateMenuVisibility(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("updateMenuVisibility - Not Implemented Yet");
        }

        internal virtual void deleteMenuItem(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("deleteMenuItem - Not Implemented Yet");
        }

        internal virtual void deleteToolbar(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("deleteToolbar - Not Implemented Yet");
        }

        internal virtual void deleteToolbarItem(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("deleteToolbarItem - Not Implemented Yet");
        }

        internal virtual void deleteTreeNode(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("deleteTreeNode - Not Implemented Yet");
        }

        internal virtual void executeLayout(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("executeLayout - Not Implemented Yet");
        }

        internal Image getImage(GuiMenuEntry menuEntry)
        {
            Events.WriteDevToLog("getImage - Not Implemented Yet");
            throw new NotImplementedException();
        }

        internal virtual void hotTrack(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("hotTrack - Not Implemented Yet");
        }

        internal virtual void linesAtRoot(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("linesAtRoot - Not Implemented Yet");
        }

        internal virtual void orderMgSplitterContainerChildren(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("orderMgSplitterContainerChildren - Not Implemented Yet");
        }

        internal virtual void resetMenu(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("resetMenu - Not Implemented Yet");
        }

        internal virtual void setChildrenRetrieved(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setChildrenRetrieved - Not Implemented Yet");
        }

        internal virtual void setCollapsedImageIdx(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setCollapsedImageIdx - Not Implemented Yet");
        }

        internal virtual void setDefaultButton(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setDefaultButton - Not supported in the CF");
        }

        internal virtual void setExpanded(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setExpanded - Not Implemented Yet");
        }

        internal virtual void setExpandedImageIdx(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setExpandedImageIdx - Not Implemented Yet");
        }

        internal virtual void setHorizontalPlacement(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setHorizontalPlacement - Not Implemented Yet");
        }

        internal virtual void setIconFileName(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setIconFileName - Not Implemented Yet");
        }

        internal virtual void setImageListIndexes(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setImageListIndexes - Not Implemented Yet");
        }

        internal virtual void setLinesVisible(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setLinesVisible - Not Implemented Yet");
        }

        internal virtual void setMenu(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setMenu - Not Implemented Yet");
        }

        internal virtual void setMenuEnable(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setMenuEnable - Not Implemented Yet");
        }

        internal virtual void setMinSize(Control control, int sentSize, bool updateMinSizeInfo, bool setWidth)
        {
            Events.WriteDevToLog("setMinSize - Not Implemented Yet");
        }

        internal virtual void setParkedCollapsedImageIdx(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setParkedCollapsedImageIdx - Not Implemented Yet");
        }

        internal virtual void setParkedExpandedImageIdx(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setParkedExpandedImageIdx - Not Implemented Yet");
        }

        internal virtual void allowUpdate(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("allowUpdate - Not Implemented Yet");
        }

        internal virtual void setSBPaneWidth(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setSBPaneWidth - Not Implemented Yet");
        }

        internal void setShowIcon(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setShowIcon - Not supported in the CF");
        }

        internal virtual void setStartupPosition(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setStartupPosition - Not supported in the CF");
        }

        internal virtual void setVisibleLines(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setVisibleLines - Not supported in the CF");
        }

        /// <summary>
        ///   set setSystemMenu
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setSystemMenu(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            Form form = GuiUtils.getForm(obj);

            ((TagData)form.Tag).ShowSystemMenu = guiCommand.Bool3;

            form.ControlBox = (((TagData)form.Tag).ShowTitleBar && ((TagData)form.Tag).ShowSystemMenu);
        }

        /// <summary>
        ///   set setTitleBar
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setTitleBar(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            Form form = GuiUtils.getForm(obj);

            ((TagData)form.Tag).ShowTitleBar = guiCommand.Bool3;

            // fixed bug #:305699
            bool showControlBox = (((TagData)form.Tag).ShowTitleBar && ((TagData)form.Tag).ShowSystemMenu);
            if (showControlBox)
            {
                // first show the control box and then set the title text
                form.ControlBox = showControlBox;
                GuiUtils.setText(form, ((TagData)form.Tag).TextToDisplay);
            }
            else
            {
                // first reset the title text and the set the hide the control box 
                GuiUtils.setText(form, ((TagData)form.Tag).TextToDisplay);
                form.ControlBox = showControlBox;
            }
        }

        internal virtual void setTabSizeMode(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setTabSizeMode - Not supported in the CF");
        }

        internal virtual void setEnvTooltipTimeout(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setEnvTooltipTimeout - Not Implemented Yet");
        }

        internal virtual void setTooltip(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setTooltip - Not Implemented Yet");
        }

        internal virtual void setVerticalPlacement(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setVerticalPlacement - Not Implemented Yet");
        }

        internal virtual void showButtons(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("showButtons - Not Implemented Yet");
        }

        internal virtual void showFullRow(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("showFullRow - Not Implemented Yet");
        }

        internal virtual void showTmpEditor(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("showTmpEditor - Not Implemented Yet");
        }

        internal virtual void activateKeyboardLayout(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("activateKeyboardLayout - Not Implemented Yet");
        }

        internal virtual void createToolbar(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("createToolbar - Not Implemented Yet");
        }

        internal virtual void setToolBar(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setToolBar - Not Implemented Yet");
        }

        internal virtual void createToolbarItem(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("createToolbarItem - Not Implemented Yet");
        }

        internal virtual void setAlignment(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setAlignment - Not Implemented Yet");
        }

        internal virtual void setBullet(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setBullet - Not Implemented Yet");
        }

        internal virtual void setIndent(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setIndent - Not Implemented Yet");
        }

        internal virtual void setUnindent(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setUnindent - Not Implemented Yet");
        }

        internal virtual void ChangeColor(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setChangeColor - Not Implemented Yet");
        }

        internal virtual void ChangeFont(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("setChangeFont - Not Implemented Yet");
        }

        protected virtual void SetTopBorderMargin(GuiCommand guiCommand)
        {
            Events.WriteDevToLog("SetTopBorderMargin - Not Implemented Yet");
        }

#if PocketPC
      internal virtual void onResumeShowForms(GuiCommand guiCommand)
      {
      }
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="guiCommand"></param>
        internal void ChangeColumnSortMark(GuiCommand guiCommand)
        {
            LgColumn column = (LgColumn)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            column.SetSortMark(guiCommand.number);
        }

        /// <summary>
        ///   beep
        /// </summary>
        internal void beep()
        {
#if !PocketPC
            SystemSounds.Beep.Play();
#else
         Beep.Play();
#endif
        }

        /// <summary></summary>
        /// <param name = "guiCommand"></param>
        private void createControl(GuiCommand guiCommand)
        {
            bool addDefaultHandlers = true;
            Control parent = getInnerControl(guiCommand.parentObject);
            if (guiCommand.isHelpWindow || guiCommand.isParentHelpWindow)
                addDefaultHandlers = false;

            Object obj = GuiUtils.createControl(guiCommand.CommandType, guiCommand.style, parent,
                (GuiMgControl)guiCommand.obj, guiCommand.stringList, guiCommand.CtrlsList,
                guiCommand.number, guiCommand.Bool3, guiCommand.Bool1, guiCommand.type, guiCommand.number2, guiCommand.obj1, addDefaultHandlers, guiCommand.dockingStyle);

            ControlsMap.getInstance().add(guiCommand.obj, guiCommand.line, obj);
        }

        /// <summary>
        ///   close a form
        /// </summary>
        /// <param name = "guiCommand"></param>
        /// <returns> true - if the closed form is MODAL</returns>
        private bool closeForm(GuiCommand guiCommand)
        {
            bool isModalFormClosed = false;
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);
            GuiForm form = (GuiForm)GuiUtils.getForm(obj);

            if (!GuiUtils.isDisposed(form))
                try
                {
                    form.IsClosing = true;
                    form.Closing -= FormHandler.getInstance().ClosingHandler;
#if !PocketPC
                    // When statusbar & toolbar is getting disposed, it raises the layout event on a form and
                    // it also raises layout event for its childerns. Hence when the form is getting closed 
                    // there is no need to handle the layout event.
                    form.Layout -= FormHandler.getInstance().LayoutHandler;

                    // For child window, ActiveChildWindow is maintained in tagdata of a topmost form (which is not child).
                    // Hence when child window is getting closed, update ActiveChildWindow on tagdata of topmost form.
                    if (((TagData)form.Tag).WindowType == WindowType.ChildWindow)
                    {
                        Form topMostForm = GuiUtils.getTopLevelFormForChildWindow(form);

                        ((TagData)topMostForm.Tag).ActiveChildWindow = (form.ParentForm != topMostForm ? form.ParentForm : null);

                        GuiUtils.restoreFocus(form.ParentForm);
                    }
#endif

                    // remove the minsize info of the form from its parent tabel.
                    if (form.Parent != null)
                    {
                        MinSizeInfo msiParent = GuiUtils.getMinSizeInfo(form.Parent);
                        msiParent.deleteChildMinSizeInfo(form);
                    }

                    Control statusBar = ((TagData)form.Tag).StatusBarControl;
                    if (statusBar != null && !GuiUtils.isDisposed(statusBar))
                    {
                        statusBar.Dispose();
                        ((TagData)form.Tag).StatusBarControl = null;
                    }

                    Control toolBar = ((TagData)form.Tag).toolBar;
                    if (toolBar != null && !GuiUtils.isDisposed(toolBar))
                    {
                        toolBar.Dispose();
                        ((TagData)form.Tag).toolBar = null;
                    }
#if !PocketPC
                    isModalFormClosed = form.Modal;
#endif
                    // When for batch task, Open Form window = Y, it attempts to close form here, which internally
                    // activates caller window or MDI. We need to ensure here that Print Preview is activated. To
                    // manage this, use below flags. For modal window, these flags are not set/reset syncronously; i.e.
                    // form activate is called multiple times and that too after form.Close(). So do not set it as true
                    // as below. It will be managed as described above in call to form.ShowDialog
                    PrintPreviewFocusManager.GetInstance().ShouldResetPrintPreviewInfo = false;
                    OldZorderManager.getInstance().PreventZorderChanges = true;
                    form.Close();
                    OldZorderManager.getInstance().PreventZorderChanges = false;
                    if (!form.Modal)
                        PrintPreviewFocusManager.GetInstance().ShouldResetPrintPreviewInfo = true;
                }
                catch (Exception)
                {
                }

            return isModalFormClosed;
        }
        /// <summary>
        /// set as last window context 
        /// </summary>
        /// <param name="guiCommand"></param>
        private void setLastInContext(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);
            Form form = GuiUtils.getForm(obj);
            ContextForms.MoveToEnd((GuiForm)form);
        }
        /// <summary>
        /// activate the form
        /// </summary>
        /// <param name = "guiCommand"></param>
        private void activateForm(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);
            Form form = GuiUtils.getForm(obj);

#if !PocketPC
            // #714840. If we are activating a form for a context and if the form is 
            // in minimized state then restore it before activating the form.

            bool shouldRestoreState = guiCommand.Bool3;

            if (shouldRestoreState && form.WindowState == FormWindowState.Minimized)
            {
                form.WindowState = FormWindowState.Normal;
                ((TagData)form.Tag).Minimized = false;
            }
#endif
            form.Activate();

            if (!Manager.Environment.SpecialOldZorder || !form.IsMdiChild)
            {
                Form topMostForm = GuiUtils.getTopLevelFormForChildWindow(form); //#76983 When activated form is a child window form than it's parent should be used to call BringToFront().
                topMostForm.BringToFront();
                if (topMostForm != form)
                    form.BringToFront();
            }

        }

        /// <summary>
        ///   Gets the inner control of a Control. 
        ///   If the control is TabControl, it returns the TabControlPanel
        /// </summary>
        /// <param name = "obj">the control</param>
        /// <returns></returns>
        internal Control getInnerControl(Object obj)
        {
            Control parentObject = (Control)ControlsMap.getInstance().object2Widget(obj);
            parentObject = GuiUtils.getInnerControl(parentObject);
            return parentObject;
        }

        ///
        internal void addControl(Object key, Control obj)
        {
            ControlsMap.getInstance().add(key, 0, obj);
        }

        /// <summary>
        ///   Sets the bounds of controls. If the control is a Shell then the requested width and height will be
        ///   considered as if they are the size of the client area.
        /// </summary>
        private void setBounds(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            GuiUtils.setBounds(obj, guiCommand.x, guiCommand.y, guiCommand.width, guiCommand.height, IsIgnorePlacement(guiCommand.obj));
        }

        /// <param name = "guiCommand"></param>
        private void setPlacementData(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            Rectangle rect = new Rectangle(guiCommand.x, guiCommand.y, guiCommand.width, guiCommand.height);

            //If the control is the client panel, get the form and then set the
            //placement data on the form.
            if (obj is Panel && ((TagData)((Panel)obj).Tag).IsClientPanel)
                obj = GuiUtils.getForm(obj);

            if (obj is Control)
                ((TagData)((Control)obj).Tag).PlacementData = new PlacementData(rect);
            else if (obj is LogicalControl)
                ((LogicalControl)obj).PlacementData = new PlacementData(rect);
        }

        /// <summary>
        /// </summary>
        /// <param name="guiCommand"></param>
        private void SetControlName(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is Control)
                GuiUtils.SetControlName(((Control)obj), guiCommand.str);
            else if (obj is LogicalControl)
                ((LogicalControl)obj).Name = guiCommand.str;
        }

        /// <summary>
        /// </summary>
        private void setText(GuiCommand guiCommand)
        {
#if !PocketPC //temp
            if (guiCommand.obj is Console.ConsoleWindow)
                ((Console.ConsoleWindow)guiCommand.obj).addText(guiCommand.str);
            else
#endif
            {
#if !PocketPC //temp
                ArrayList arrayControl = ControlsMap.getInstance().object2WidgetArray(guiCommand.obj, GuiConstants.ALL_LINES);
                Object object0 = arrayControl[0];
                if (object0 is ToolStripMenuItem)
                {
                    foreach (ToolStripMenuItem mnuItem in arrayControl)
                    {
                        setMenuItemText(mnuItem, guiCommand.menuEntry);
                    }
                }
                else
#endif
                {
                    Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
                    GuiUtils.setText(obj, guiCommand.str);
                }
            }
        }

#if !PocketPC
        //temp
        /// <summary>
        ///   This method updates the menuItem's text according to the passed text + access key.
        /// </summary>
        internal static void setMenuItemText(ToolStripMenuItem menuItem, GuiMenuEntry menuEntry)
        {
            String text = menuEntry.TextMLS;
            if (menuEntry.AccessKey != null && menuEntry.ParentMenuEntry != null)
            {
                menuItem.ShortcutKeyDisplayString = menuEntry.AccessKey.ToString();
            }
            menuItem.Text = text;
        }
#else
/// <summary>
///   Updates the menu text according to MenuEntry. Access keys are not supported in mobile.
/// </summary>
/// <param name = "menuItem"></param>
/// <param name = "menuEntry"></param>
      internal static void setMenuItemText(MenuItem menuItem, GuiMenuEntry menuEntry)
      {
         // If the menu type is separator then set the text to "-". Older menu classes
         // differentiate seprator only with text unlike new menu classes.
         if (menuEntry.menuType() == GuiMenuEntry.MenuType.SEPARATOR)
            menuItem.Text = "-";
         else
         {
            String text = menuEntry.TextMLS;
            menuItem.Text = text;
         }
      }

#endif

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        private void setFont(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            //create the font with underline for link label.
            if (obj is MgLinkLabel || obj is LgLinkLabel)
            {
                //Although MgLinkLabel takes care of adding the underline, runtime should explicitly
                //add it in the MgFont. 
                //Because, if 2 LinkLabels have same font, MgLinkLabel will create 2 Fonts for them.
                //But if we add underline to MgFont, FontsCache will ensure using the same Font 
                //for both the LinkLabels. 
                guiCommand.mgFont = new MgFont(guiCommand.mgFont);
                guiCommand.mgFont.addStyle(FontAttributes.FontAttributeUnderline);
            }

            Font font = FontsCache.GetInstance().Get(guiCommand.mgFont);

            GuiUtils.setFont(obj, font, guiCommand.mgFont.Orientation, guiCommand.number);
        }

        /// <summary>
        /// set focus color
        /// </summary>
        /// <param name="guiCommand"></param>
        private void setFocusColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            Color? emptyColor = null;
            Color? FocusFGColor = guiCommand.mgColor != null ? ControlUtils.MgColor2Color(guiCommand.mgColor, false, false) : emptyColor;
            Color? FocusBGColor = guiCommand.mgColor != null ? ControlUtils.MgColor2Color(guiCommand.mgColor1, false, false) : emptyColor;

            if (obj is MgTextBox)
            {
                Control objMgTextBox = obj as Control;
                (((TagData)objMgTextBox.Tag).FocusFGColor) = FocusFGColor;
                (((TagData)objMgTextBox.Tag).FocusBGColor) = FocusBGColor;


                if (GuiUtils.AccessTest)
                {
                    Control controlInFocus = ControlUtils.GetFocusedControl();
                    if (controlInFocus == (Control)obj)
                        GuiUtils.SetFocusColor((Control)obj as TextBox);
                }

            }
            else if (obj is LgText)
            {
                ((LgText)obj).FocusFGColor = FocusFGColor;
                ((LgText)obj).FocusBGColor = FocusBGColor;
                ((LgText)obj).MgFocusColorIndex = guiCommand.number;
            }
            else
                Debug.Assert(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="guiCommand"></param>
        private void setBackgroundColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            Color backgroundColor = Color.Empty;
            // if the color is transparent then set the background color to null
            if (guiCommand.mgColor != null)
            {
                //For logical text, we support alpha values.
                bool supportsAlpha = obj is LgText || GuiUtils.supportTransparency(obj);

                backgroundColor = ControlUtils.MgColor2Color(guiCommand.mgColor, GuiUtils.supportTransparency(obj), supportsAlpha);
            }

            bool isTransparentForLogical = ControlUtils.MgColor2Color(guiCommand.mgColor, true, false) == Color.Transparent;

            GuiUtils.setBackgroundColor(obj, backgroundColor, guiCommand.mgColor, guiCommand.mgColor.IsTransparent, isTransparentForLogical, guiCommand.number);
        }

        /// <summary>
        /// Set the Border color of the control.
        /// </summary>
        /// <param name="guiCommand"></param>
        private void setBorderColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            Color foregroundColor = Color.Empty;
            if (guiCommand.mgColor != null)
                foregroundColor = ControlUtils.MgColor2Color(guiCommand.mgColor, false, false);

            GuiUtils.setBorderColor(obj, foregroundColor);
        }

        /// <summary>
        /// </summary>
        private void setForegroundColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            Color foregroundColor = ControlUtils.MgColor2Color(guiCommand.mgColor, false, false);

            GuiUtils.setForegroundColor(obj, foregroundColor);
        }



        /// <summary>
        /// </summary>
        private void SetLinkVisited(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is MgLinkLabel)
                ((MgLinkLabel)obj).LinkVisited = guiCommand.Bool3;
            else if (obj is LgLinkLabel)
                ((LgLinkLabel)obj).Visited = guiCommand.Bool3;
            else
                Debug.Assert(false);
        }

        /// <summary>
        /// </summary>
        private void setVisitedColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            Color visitedFGColor = ControlUtils.MgColor2Color(guiCommand.mgColor, false, false);
            Color visitedBGColor = ControlUtils.MgColor2Color(guiCommand.mgColor1, true, true);
            if (obj is MgLinkLabel)
                ((MgLinkLabel)obj).SetVisitedColor(visitedFGColor, visitedBGColor);
            else if (obj is LgLinkLabel)
            {
                ((LgLinkLabel)obj).VisitedFGColor = visitedFGColor;
                ((LgLinkLabel)obj).VisitedBGColor = visitedBGColor;
                ((LgLinkLabel)obj).MgVisitedColorIndex = guiCommand.number;
            }
            else
                Debug.Assert(false);
        }

        /// <summary>
        /// </summary>
        private void setGradientColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is TabControl)
            {
                TabControl tabControl = (TabControl)obj;
                obj = ((TagData)tabControl.Tag).TabControlPanel;
            }

            Color fromColor = ControlUtils.MgColor2Color(guiCommand.mgColor, false, false);
            Color toColor = ControlUtils.MgColor2Color(guiCommand.mgColor1, false, false);
            GradientColor gradientColor = new GradientColor(fromColor, toColor);

            if (obj is Control)
                ControlUtils.SetGradientColor((Control)obj, gradientColor);
            else if (obj is LogicalControl)
                ((LogicalControl)obj).GradientColor = gradientColor;
            else
                Debug.Assert(false);
        }

        /// <summary>
        ///   set gradientStyle into TagData
        /// </summary>
        /// <param name = "guiCommand">
        /// </param>
        internal void setGradientStyle(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            GradientStyle gradientStyle = (GradientStyle)guiCommand.style;

            GuiUtils.setGradientStyle(obj, gradientStyle);
        }

        /// <summary>
        /// </summary>
        private void setHoveringColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            Color HovaringFGColor = ControlUtils.MgColor2Color(guiCommand.mgColor, false, false);
            Color HovaringBGColor = ControlUtils.MgColor2Color(guiCommand.mgColor1, true, true);
            if (obj is MgLinkLabel)
                ((MgLinkLabel)obj).SetHoveringColor(HovaringFGColor, HovaringBGColor);
            else if (obj is LgLinkLabel)
            {
                ((LgLinkLabel)obj).HoveringFGColor = HovaringFGColor;
                ((LgLinkLabel)obj).HoveringBGColor = HovaringBGColor;
                ((LgLinkLabel)obj).MgHoveringColorIndex = guiCommand.number;
            }
            else
                Debug.Assert(false);
        }

        /// <summary>
        ///   set the push button image list number on the button (4 or 6)
        /// </summary>
        /// <param name = "guiCommand"></param>
        private void setPBImagesNumber(Object key, int line, int PBImagesNumber)
        {
            Object obj = ControlsMap.getInstance().object2Widget(key, line);

            if (obj is MgImageButton)
                ((MgImageButton)obj).PBImagesNumber = PBImagesNumber;
            else if (obj is LgButton && ((LogicalControl)obj).GuiMgControl.IsImageButton())
                ((LgButton)obj).PBImagesNumber = PBImagesNumber;
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setImageFileName(GuiCommand guiCommand)
        {
            Image image = GuiUtils.getImageFromFile(guiCommand.fileName);

            setImageInfo(guiCommand, image);
        }

        /// <summary>
        ///   set info image on a object
        /// </summary>
        /// <param name = "guiCommand"></param>
        /// <param name = "image"></param>
        internal void setImageInfo(GuiCommand guiCommand, Image image)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            CtrlImageStyle imageStyle = (CtrlImageStyle)guiCommand.style;

            if (obj is Panel || obj is PictureBox || obj is MgCheckBox || obj is MgRadioPanel || ControlUtils.IsMdiClient(obj))
            {
                if (obj is MgRadioPanel)
                    GuiUtils.SetImageToRadio(image, guiCommand.fileName, obj, imageStyle);
                else
                    GuiUtils.SetImageToControl(image, (Control)obj, imageStyle);
            }
            else if (obj is LogicalControl)
            {
                if (obj is LgImage)
                    ((LgImage)obj).SetImage(image, imageStyle);
                else if (obj is LgRadioContainer)
                    ((LgRadioContainer)obj).SetImage(image, guiCommand.fileName);
                else if (obj is LgCheckBox)
                    ((LgCheckBox)obj).SetImage(image);
            }
#if !PocketPC
            else if (obj is ToolStripItem)
                GuiUtils.SetImage((ToolStripItem)obj, image, imageStyle);
#endif
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setImageList(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            Debug.Assert(obj is MgImageButton || obj is TreeView || obj is TabControl ||
                         (obj is LgButton && ((LogicalControl)obj).GuiMgControl.IsImageButton()));

            setPBImagesNumber(guiCommand.obj, guiCommand.line, guiCommand.style);

            if (obj is LgButton)
                ((LgButton)obj).setImageList(guiCommand.fileName);
            else
                GuiUtils.SetImageList((Control)obj, guiCommand.fileName);
        }


        /// <summary>
        /// fixed bug#:800763
        /// when the selection isn't equal to the prev value (from worker thread) ignore the selection
        /// 
        /// </summary>
        /// <param name="guiCommand"></param>
        internal bool ignoreSelection(GuiCommand guiCommand, Control obj)
        {
            bool ignore = false;

            Form form = GuiUtils.FindForm(obj);

            //it is for ComboBox or ListBox
            if (obj is ListControl)
            {
                ListControl listControl = (ListControl)obj;
                //control is in focus
                if (((TagData)form.Tag).LastFocusedControl == listControl)
                {
                    int[] selectedIndice = null;
                    int idx = 0;

                    if (listControl is ListBox)
                    {
                        ListBox listBox = (ListBox)listControl;

#if !PocketPC
                        //Get selected indice list (Don't use listBox.SelectedIndices, Please refer the called method for explanation).
                        selectedIndice = GuiUtils.GetSelectedIndices(listBox);
#else
                  selectedIndice = new int[1];
                  selectedIndice[idx] = listBox.SelectedIndex;
#endif
                    }
                    else
                    {
                        selectedIndice = new int[1];
                        selectedIndice[idx] = listControl.SelectedIndex;
                    }

                    //if the prev value was NULL then don't ignore the control 
                    //because the prev value was reset
                    if (guiCommand.intArray == null)
                        ignore = false;
                    else
                    {
                        // while the value on Worker thread and the value on Gui thread are not identical and we know about it 
                        // force selection
                        if ((((TagData)listControl.Tag).WorkerThreadAndGuiThreadValueAreNotIdentical))
                            ignore = false;
                        else
                            //compare the last and current selections. When the selection isn't equal to the prev value (from worker thread) ignore the selection
                            ignore = !Misc.CompareIntArrays(guiCommand.intArray, selectedIndice);
                    }
                }
            }

            return ignore;
        }
        /// <summary>
        ///   set the selection on the list\combo
        /// </summary>
        /// <param name = "guiCommand">
        /// </param>
        internal void setSelection(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is MgComboBox)
            {
                Control control = (Control)obj;
                TagData tg = control.Tag as TagData;

                // if the fourceSetSelection then set the selection
                if (!ignoreSelection(guiCommand, (Control)obj))
                {
                    // if we are not in "SetRowToDefaultValues", reset the value 
                    if (!guiCommand.Bool1)
                        tg.WorkerThreadAndGuiThreadValueAreNotIdentical = false;

                    int[] indice = Misc.GetIntArray(guiCommand.str);
                    GuiUtils.setSelect((MgComboBox)obj, indice[0]);
                }
            }
            else if (obj is ListBox)
            {
                ListBox listBox = ((ListBox)obj);

                if (!ignoreSelection(guiCommand, (Control)obj))
                {

                    if (guiCommand.str.Length > 0)
                    {
                        //we want the event to pop only when the user makes the change. Remove the handler.
                        listBox.SelectedIndexChanged -= ListHandler.getInstance().SelectedIndexChangedHandler;
#if !PocketPC
                        bool shouldClearAndSetSelection = false;

                        //Get selected indices list from a listbox - (Don't use listBox.SelectedIndices, Please refer the called method for explanation).
                        int[] selectedIndice = GuiUtils.GetSelectedIndices(listBox);

                        //Get selected indice list from guicommand.
                        int[] indice = Misc.GetIntArray(guiCommand.str);

                        //Compare the indices in guicommand and guicontrol. If they are same, no need to clear & set the 
                        //selections else clear & set the selections.

                        if (!Misc.CompareIntArrays(selectedIndice, indice))
                            shouldClearAndSetSelection = true;

                        if (shouldClearAndSetSelection)
                        {
                            // Clear the selected entries.
                            foreach (var index in selectedIndice)
                                listBox.SetSelected(index, false);

                            foreach (var index in indice)
                            {
                                if (index >= 0 && index < listBox.Items.Count)
                                    listBox.SetSelected(index, true);
                            }
                        }
#else

                  int[] indice = Misc.GetIntArray(guiCommand.str);
                  int selectedIndex = indice[0];

                  if (selectedIndex >= 0 && selectedIndex <= listBox.Items.Count)
                     listBox.SelectedIndex = selectedIndex;
                  else
                     listBox.SelectedIndex = -1;

                  GuiUtils.setListControlOriginalValue(listBox, listBox.SelectedIndex.ToString());

#endif
                        // put the handler back.
                        listBox.SelectedIndexChanged += ListHandler.getInstance().SelectedIndexChangedHandler;
                    }
                }
            }
            else if (obj is MgTabControl)
            {
                int[] indice = Misc.GetIntArray(guiCommand.str);
                int selectedIndex = indice[0];

                GuiUtils.setSelectionForTabControl(selectedIndex, (MgTabControl)obj);
            }
            else if (obj is LgCombo)
            {
                int[] indice = Misc.GetIntArray(guiCommand.str);
                int selectedIndex = indice[0];

                ((LgCombo)obj).setSelectionIdx(selectedIndex);
            }
            else
                Debug.Assert(false);
        }

        /// <summary>
        ///   create the items list for choice controls
        /// </summary>
        internal void setItemsList(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (guiCommand.itemsList != null)
            {
                if (obj is LgCombo)
                    ((LgCombo)obj).setItemList(guiCommand.itemsList);
                else if (obj is LgRadioContainer)
                    ((LgRadioContainer)obj).ItemList = guiCommand.itemsList;
                else
                {

                    int saveSelectedIndex = -1;
                    if (obj is ListControl)
                        saveSelectedIndex = ((ListControl)obj).SelectedIndex;

                    GuiUtils.setItemsList((Control)obj, guiCommand.itemsList);

                    // see defect #81498 (tagData.cs explanation on property WorkerThreadAndGuiThreadValueAreNotIdentical)
                    if (guiCommand.Bool1)
                    {
                        if (obj is ListControl)
                        {
                            if (saveSelectedIndex != ((ListControl)obj).SelectedIndex)
                            {
                                // 81498: while the items is set with less items then the selectedIndex, then :
                                //        The selectedIndex is set to -1, and the value on Worker thread and the value on 
                                //        Gui thread are not identical.
                                TagData tg = (((Control)obj)).Tag as TagData;
                                if (tg != null)
                                    tg.WorkerThreadAndGuiThreadValueAreNotIdentical = true;
                            }

                        }
                    }

                }
            }
        }

        /// <summary>
        ///   Open subform, sets minimum size for the form or subform
        /// </summary>
        /// <param name = "guiCommand">
        /// </param>
        internal void createLayout(GuiCommand guiCommand)
        {
            Control realComposite = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            Control innerComposite = getInnerControl(guiCommand.obj);
            Rectangle rect = new Rectangle(guiCommand.x, guiCommand.y, guiCommand.width, guiCommand.height);

            ((TagData)innerComposite.Tag).PlacementLayout = new EditorSupportingPlacementLayout(realComposite, rect, guiCommand.Bool3, guiCommand.runtimeDesignerXDiff, guiCommand.runtimeDesignerYDiff);
            //innerComposite.setLayout(new PlacementLayout(realComposite, rect, guiCommand.boolVal));
        }

        /// <summary>
        ///   set true\false\null for check box
        /// </summary>
        /// <param name = "guiCommand">
        /// </param>
        internal void setCheckBoxCheckState(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            CheckState checkState = GuiUtils.checkBoxCheckStates2CheckBoxCheckState((MgCheckState)guiCommand.number);

            if (obj is MgCheckBox)
                GuiUtils.setCheckBoxCheckState((MgCheckBox)obj, checkState);
            else if (obj is LgCheckBox)
                ((LgCheckBox)obj).CheckState = checkState;
        }

        /// <summary>
        ///   Set Style 3D
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setCheckboxMainStyle(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            Appearance appearance = (guiCommand.number == (int)CheckboxMainStyle.Box || guiCommand.number == (int)CheckboxMainStyle.Switch
                ? Appearance.Normal
                : Appearance.Button);

            if (obj is MgCheckBox)
                ControlUtils.SetCheckboxMainStyle((MgCheckBox)obj, appearance);
            else if (obj is LgCheckBox)
                ((LgCheckBox)obj).Appearance = appearance;
        }

        /// <summary>
        ///   will be seen as grey and the user will not park on it
        /// </summary>
        internal void setEnable(GuiCommand guiCommand)
        {
            ArrayList arrayControl = ControlsMap.getInstance().object2WidgetArray(guiCommand.obj, guiCommand.line);

            if (arrayControl != null)
            {
                Object obj = arrayControl[0];
#if !PocketPC
                //tmp
                if (obj is ToolStripMenuItem)
                {
                    foreach (ToolStripMenuItem mnuItem in arrayControl)
                    {
                        GuiUtils.setEnabled(mnuItem, guiCommand.Bool3);
                    }
                }
                else if (obj is ToolStripButton)
                {
                    foreach (ToolStripButton toolItem in arrayControl)
                    {
                        GuiUtils.setEnabled(toolItem, guiCommand.Bool3);
                    }
                }
#else
            if (obj is MenuItem)
            {
               foreach (MenuItem mnuItem in arrayControl)
               {
                  GuiUtils.setEnabled(mnuItem, guiCommand.Bool3);
               }
            }
#endif
                else
                {
                    for (int i = 0;
                        i < arrayControl.Count;
                        i++)
                    {
                        Object control = arrayControl[i];
                        if (control is Control)
                            GuiUtils.setEnabled((Control)control, guiCommand.Bool3);
                        else if (control is LogicalControl)
                            ((LogicalControl)control).Enabled = guiCommand.Bool3;
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setFormBorderStyle(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            Form form = GuiUtils.getForm(obj);
            BorderType borderType = (BorderType)guiCommand.style;

            form.FormBorderStyle = ControlUtils.BorderTypeToFormBorderStyle(borderType, ((TagData)form.Tag).WindowType, ((TagData)form.Tag).ShowTitleBar);

            //#435503: To hide the title bar but show text in task bar set Form.Text to value of property Form Name 
            // when border style = No Border and title bar = No.
            GuiUtils.setText(form, ((TagData)form.Tag).TextToDisplay);
        }

        /// <summary>
        ///   Set Multiline WordWrap Scroll
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setMultilineWordWrapScroll(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is TextBox)
            {
                MultilineHorizontalScrollBar horizontalScrollBar = (MultilineHorizontalScrollBar)guiCommand.number;
                GuiUtils.setMultilineWordWrapScroll((TextBox)obj, horizontalScrollBar);
            }
            else if (obj is LogicalControl)
            {
                ((LogicalControl)obj).HorizontalScrollBar = (MultilineHorizontalScrollBar)guiCommand.number;
                ((LogicalControl)obj).WordWrap = ((LogicalControl)obj).HorizontalScrollBar == MultilineHorizontalScrollBar.WordWrap;
            }
        }

        /// <summary>
        ///   set vertical alignment
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setVerticalAlignment(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            AlignmentTypeVert alignmentTypeVert = (AlignmentTypeVert)guiCommand.number;

            if (obj is LogicalControl)
            {
                ContentAlignment currentContentAlignment = ((LogicalControl)obj).ContentAlignment;
                ContentAlignment contentAlignment = ControlUtils.GetContentAligmentForSetVerAligment(currentContentAlignment, alignmentTypeVert);
                ((LogicalControl)obj).ContentAlignment = contentAlignment;
            }
            else if (obj is Control)
                ControlUtils.SetVerticalAlignment((Control)obj, alignmentTypeVert);
        }

        /// <summary>
        ///   TODO: RADIO
        /// </summary>
        internal void setVisible(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            if (obj is LogicalControl)
                ((LogicalControl)obj).Visible = guiCommand.Bool3;
            else if (obj is Control)
                GuiUtils.setVisible((Control)obj, guiCommand.Bool3);
#if !PocketPC
            else if (obj is ToolStripMenuItem)
                GuiUtils.setVisible((ToolStripMenuItem)obj, guiCommand.Bool3);
            else if (obj is ToolStripButton)
                GuiUtils.setVisible((ToolStripButton)obj, guiCommand.Bool3);
            else if (obj is ToolStripSeparator)
                GuiUtils.setVisible((ToolStripSeparator)obj, guiCommand.Bool3);
#endif
#if DEBUG && !PocketPC
            /*TODO Rajan Should we using StatusBarPanel*/
            else if (!(obj is ToolStripStatusLabel)) // TODO: check why we get to this point for ToolStripStatusLabel
            {
                if (obj != null)
                    Events.WriteExceptionToLog(string.Format("obj.GetType() = {0}", obj.GetType()));
                else
                    Events.WriteExceptionToLog("obj is null");
            }
#endif
        }

        /// <summary>
        ///   Set Multiline Vertical Scroll
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setMultilineVerticalScroll(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is TextBox)
                GuiUtils.setMultilineVerticalScroll((TextBox)obj, guiCommand.Bool3);
            else if (obj is LogicalControl)
                ((LogicalControl)obj).ShowVerticalScroll = guiCommand.Bool3;
        }

        /// <summary>
        ///   set Multiline
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setMultiLine(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is Control)
                ControlUtils.SetMultiLine((Control)obj, guiCommand.Bool3);
            else if (obj is LogicalControl)
                ((LogicalControl)obj).MultiLine = guiCommand.Bool3;
        }

        /// <summary>
        ///   set the browser control to a new URI
        /// </summary>
        internal void setUrl(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj != null && obj is MgWebBrowser)
            {
                if (string.IsNullOrEmpty(guiCommand.str))
                    ((MgWebBrowser)obj).DocumentText = "";
                else
                {
                    // if the url is relative, prefix it with protocol://server/
                    StringBuilder url = new StringBuilder();
                    if (guiCommand.str.StartsWith("/"))
                        url.Append(Manager.DefaultProtocol + "://" + Manager.DefaultServerName);
                    url.Append(guiCommand.str);

                    try
                    {

                        Uri uri = Misc.createURI(url.ToString());

                        // QCR 782876 - some file types are not opened by the WebBrowser control, but cause the 
                        // launch of mspaint.exe. In order to open them in the control, we wrap them in HTML.
                        // The last segment of the uri is the file name, use FileInfo to break it down and get the extension.
                        string lastseg = uri.Segments[uri.Segments.Length - 1];
                        int extpos = lastseg.LastIndexOf('.');
                        string ext = "";
                        if (extpos >= 0)
                            ext = lastseg.Substring(extpos, lastseg.Length - extpos);

                        if (ext == ".bmp" || ext == ".tif" || ext == ".png")
                            // Wrap problematic image files with html, and set the html text directly
                            ((MgWebBrowser)obj).DocumentText = "<HEAD></HEAD>\r\n<BODY><IMG src=\"" + uri.AbsoluteUri + "\"</BODY>";
                        else
                            // set the url 
                            ((MgWebBrowser)obj).Navigate(uri);
                    }
                    catch (Exception ex)
                    {
                        Events.WriteExceptionToLog(ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setFocus(GuiCommand guiCommand)
        {
            int line = (guiCommand.line >= 0
                ? guiCommand.line
                : 0);
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, line);

            Form form = null;
            Control control = null;
            TableManager tableManager = null;
            LogicalControl lg = null;

            bool actualControlChanged = false;
            bool logicalControlChanged = false;
            bool focusingFirstTimeOnForm = false;

            if (obj is Panel)
            {
                control = (Control)obj;
                form = GuiUtils.FindForm(control);
                ContainerManager.hideTmpEditor(form);
            }
            else if (obj is Control)
            {
                form = GuiUtils.FindForm(((Control)obj));
                //fixed bug #712097, when control is wide control don't hide the 
                if (!isWideControl(guiCommand))
                    ContainerManager.hideTmpEditor(form);
                control = (Control)obj;
            }
            else if (obj is LogicalControl)
            {
                //Debug.Assert(obj is TableChild || obj is StaticText);
                lg = (LogicalControl)obj;
                ContainerManager containerManager = lg.ContainerManager;
                form = GuiUtils.FindForm(containerManager.mainControl);
                //tableChild = lg as TableChild;
                if (!lg.isLastFocussedControl())
                {
                    ContainerManager.hideTmpEditor(form);
                    logicalControlChanged = true;
                }
                tableManager = containerManager as TableManager;
                if (GuiUtils.isOwnerDrawControl(lg.GuiMgControl))
                {
                    if (tableManager != null)
                        control = (tableManager).showTmpEditor(lg);
                    else //containerManager is StaticControlsManager)
                        control = ((BasicControlsManager)containerManager).showTmpEditor(lg);
                }
                else
                {
                    control = lg.getEditorControl();
                    lg.setProperties(control, true, true);
                }
            }
            else
                Debug.Assert(false);


            //If ((TagData)form.Tag).LastFocusedControl is null, it means that we are focusing 
            //on this form for the first time.
            focusingFirstTimeOnForm = (((TagData)form.Tag).LastFocusedControl == null);

            ((TagData)form.Tag).LastFocusedControl = control;
            if (form.IsMdiContainer && ((TagData)form.Tag).MDIClientForm != null)
            {
                ((TagData)((TagData)form.Tag).MDIClientForm.Tag).LastFocusedControl = control;
            }

            Debug.Assert(control != null);
            refreshOldEditor(form);

            //QCR #775670 , paint performance improvement
            // 1. We dispose previous focused temporary editor here, because if we dispose contol that  currently has focus
            // .NET automaticly shifts focus to the next control  (and if the next control a button we can see a blue frame on it for a second)
            // 2.Changing bounds of a child causes paint on the whole client area of the parent. 
            if (tableManager != null)
            {
                ((TagData)form.Tag).LastFocusedMapData = null; //do not return focus to the previous control
                tableManager.refreshTable(false);
            }
            ((TagData)form.Tag).LastFocusedMapData = ControlsMap.getInstance().getMapData(control);

            actualControlChanged = GuiUtils.setFocus(control, guiCommand.Bool3, !guiCommand.Bool3);
            if (GuiUtils.ControlToDispose != null)
            {
                GuiUtils.ControlToDispose.Dispose();
                GuiUtils.ControlToDispose = null;
            }

            //QCR #923332, 980627 : It turns out that performing setFocus on textcontrol whose size is 0, causes textcontrol to scroll. 
            //Changes are applies so that control will always receive its bounds (in layout())  before setFocus is performed.
            //#924272. scrollToControl() should be called only if the focus shifts from 
            //one control to another --- either logical or actual.
            //#727282.  In case of AllowTesting=Yes, since we have actual controls, .Net framework sets the focus on the first control 
            //when the form is opened. So, when GuiCommandQueue.setFocus() is called, because the focus is already there on the control,
            //GuiUtils.setFocus() returns false. And so, GuiUtils.scrollToControl() doesn't get called.
            //The solution is to call GuiUtils.scrollToControl() when GuiCommandQueue.setFocus() is called for a form for the first time.
            if (!(control is Panel) && !(control is Form) && (focusingFirstTimeOnForm || logicalControlChanged || actualControlChanged))
                GuiUtils.scrollToControl(control, lg);

            UtilImeJpn utilImeJpn = Manager.UtilImeJpn; // JPN: IME support
            if (utilImeJpn != null)
            {
                utilImeJpn.ImeAutoOff = Manager.Environment.GetImeAutoOff();

                if (control is MgTextBox)
                {
                    utilImeJpn.controlIme(control, ((TagData)(control).Tag).ImeMode); // JPN: IME support
                    ((MgTextBox)control).ClearCompositionString(); // JPN: ZIMERead function
                    utilImeJpn.StrImeRead = "";
                }
#if !PocketPC
                else if (control is MgRichTextBox)
                {
                    ((MgRichTextBox)control).ClearCompositionString();
                    utilImeJpn.StrImeRead = "";
                }
#endif
                else if (control is MgComboBox || control is ListBox)
                {
                    utilImeJpn.controlIme(control, 0);
                }
            }
        }

        /// <summary>
        ///   refresh previous editor
        /// </summary>
        /// <param name = "form"></param>
        private void refreshOldEditor(Form form)
        {
            MapData mapData = ((TagData)form.Tag).LastFocusedMapData;
            if (mapData != null && mapData.getControl() != null)
            {
                LogicalControl lg = ControlsMap.getInstance().object2Widget(mapData.getControl(), mapData.getIdx()) as LogicalControl;
                if (lg != null)
                {
                    if (lg.getEditorControl() != null)
                        lg.Refresh(true);
                    else
                        lg.ContainerManager.mainControl.Invalidate(lg.getRectangle());

                }
            }
        }

        /// <summary>
        ///   return true if this control is the wide control on the form
        /// </summary>
        /// <param name = "guiCommand"></param>
        /// <returns></returns>
        private static bool isWideControl(GuiCommand guiCommand)
        {
            bool IsWideControl = false;
            if (guiCommand.obj is GuiMgControl)
            {
                GuiMgControl guiMgControl = (GuiMgControl)guiCommand.obj;
                IsWideControl = guiMgControl.IsWideControl;
            }
            return IsWideControl;
        }

        /// <summary>
        ///   return true if this control is the wide control on the form
        /// </summary>
        /// <param name = "guiCommand"></param>
        /// <returns></returns>
        private static bool IsIgnorePlacement(object obj)
        {
            bool isIgnore = false;
            if (obj is GuiMgControl)
            {
                GuiMgControl guiMgControl = (GuiMgControl)obj;
                isIgnore = guiMgControl.IsDirectFrameChild;
            }
            return isIgnore;
        }


        /// <summary>
        ///   set LineStyle
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setLineStyle(GuiCommand guiCommand)
        {
            Line control = (Line)ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            control.LineHelper.LineStyle = (CtrlLineType)guiCommand.number;
        }

        /// <summary>
        ///   set LineWidth
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setLineWidth(GuiCommand guiCommand)
        {
            Line control = (Line)ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            control.LineHelper.LineWidth = guiCommand.number;
        }

        /// <summary>
        ///   set LineDirection
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setLineDirection(GuiCommand guiCommand)
        {
            Line control = (Line)ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            control.LineHelper.LineDir = (CtrlLineDirection)guiCommand.number;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="guiCommand"></param>
        internal void setBorderStyle(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj != null)
            {
                if (obj is MgRadioPanel)
                    ControlUtils.SetBorderStyle((Panel)obj, (BorderType)guiCommand.number);
                else if (obj is LgRadioContainer)
                    ((LgRadioContainer)obj).ControlBorderType = (BorderType)guiCommand.number;
                else if (obj is MgCheckBox)
                    ((MgCheckBox)obj).BorderType = (BorderType)guiCommand.number;
                else if (obj is LgCheckBox)
                    ((LgCheckBox)obj).ControlBorderType = (BorderType)guiCommand.number;
            }
        }


        /// <summary>
        ///   Set Style 3D
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setStyle3D(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj != null)
            {
                if (obj is LogicalControl)
                    ((LogicalControl)obj).Style = (ControlStyle)guiCommand.number;
                else
                    ControlUtils.SetStyle3D((Control)obj, (ControlStyle)guiCommand.number);
            }
        }

        /// <summary>
        ///   Change the Z-Order of the controls by moving one control on the top of another
        /// </summary>
        internal void moveAbove(GuiCommand guiCommand)
        {
            Object higherObject = ControlsMap.getInstance().object2Widget(guiCommand.obj);

            if (higherObject is Control)
            {
                Control higherControl = (Control)higherObject;

                // There is a problem with .NetFramework for Z-Order. While aligning Z-Order, 
                // if a control is not created then its Z-Order is not set by .NetFramework.
                // Hence before aligning Z-Order of a control, access its handle to create a control.
                IntPtr ptr = higherControl.Handle;
                ptr = IntPtr.Zero;

                // BringToFront brings the control to the topmost position. This should work
                // in our case as we receive the control list in the order of the Z-order.
                higherControl.BringToFront();
            }
        }

        /// <summary>
        ///   select text in text control
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void selectText(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            TextBoxBase text = GuiUtils.getTextCtrl(obj);

            if (text != null && !GuiUtils.IsEmptyHintTextBox(text))
            {
                switch ((MarkMode)guiCommand.number)
                {
                    case MarkMode.MARK_ALL_TEXT:
                        text.SelectAll();
                        break;

                    case MarkMode.UNMARK_ALL_TEXT:
                        text.Select(0, 0);
                        break;

                    case MarkMode.MARK_SELECTION_TEXT:
                        // mark text by pos
                        text.Select(guiCommand.layer, guiCommand.number1 - guiCommand.layer);
                        break;
                }

                // check id any actions need to be enable disable after marking change.(same as in guiInteractive).
                GuiUtils.enableDisableEvents(text, (GuiMgControl)guiCommand.obj);
            }
        }

        /// <summary>
        /// </summary>
        internal void setMinHeight(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);
            setMinSize((Control)obj, guiCommand.height, true, false);
        }

        /// <summary>
        /// </summary>
        internal void setMinWidth(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);
            setMinSize((Control)obj, guiCommand.width, true, true);
        }

        /// <summary>
        ///   set parameter is read only
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setReadonly(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is LogicalControl)
                ((LogicalControl)obj).Modifable = !guiCommand.Bool3;
            else if (obj is TextBox)
                GuiUtils.setReadOnly(((TextBox)obj), guiCommand.Bool3);
#if !PocketPC
            else if (obj is RichTextBox)
                GuiUtils.setReadOnly(((RichTextBox)obj), guiCommand.Bool3);
            else if (obj is TreeView)
                GuiUtils.getTreeManager((TreeView)obj).labelEdit(!guiCommand.Bool3);
#endif
        }

        /// <summary></summary>
        internal void setTextSizeLimit(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            if (obj is TextBox)
                GuiUtils.setTextLimit((TextBox)obj, guiCommand.number);
            else if (obj is LogicalControl)
                ((LogicalControl)obj).TextSizeLimit = guiCommand.number;
            else if (!(obj is DateTimePicker)) //TODO:?
                throw new ApplicationException("in GuiCommandQueue.setTextSizeLimit()");
        }

        /// <summary>
        ///   set IME control mode (JPN: IME support)
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setImeMode(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            int mode = guiCommand.number;

            if (obj is TextBox)
                GuiUtils.setImeMode((TextBox)obj, mode);
            else if (obj is LogicalControl)
                ((LogicalControl)obj).ImeMode = mode;
        }

        /// <summary>
        ///   Sets AcceptsReturn property of Multi Line Text Box according to AllowCR prop
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setMultilineAllowCR(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is TextBox)
                GuiUtils.setMultilineAllowCR((TextBox)obj, guiCommand.Bool3);
            else if (obj is LogicalControl)
                ((LogicalControl)obj).MultiLineAllowCR = guiCommand.Bool3;
        }

        /// <summary>
        ///   Set the border
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setBorder(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is LogicalControl)
                ((LogicalControl)obj).ShowBorder = guiCommand.Bool3;
#if !PocketPC
            else if (obj is ToolStripStatusLabel)
            {
                if (guiCommand.Bool3)
                {
                    ((ToolStripStatusLabel)obj).BorderSides = ToolStripStatusLabelBorderSides.All;
                    ((ToolStripStatusLabel)obj).BorderStyle = Border3DStyle.SunkenOuter;
                }
            }
#endif
            else
            {
                ControlUtils.SetBorder((Control)obj, (bool)guiCommand.Bool3);

                TableControlLimitedItems tableControl = obj as TableControlLimitedItems;
                if (tableControl != null)
                {
                    TagData td = (TagData)tableControl.Tag;
                    if (td.Bounds != null)
                        GuiUtils.setBounds(tableControl, (Rectangle)td.Bounds);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setHorizontalAlignment(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj != null)
            {
                AlignmentTypeHori alignmentTypeHori = (AlignmentTypeHori)guiCommand.number;

                if (obj is LogicalControl)
                {
                    ContentAlignment currentContentAlignment = ((LogicalControl)obj).ContentAlignment;

                    if (obj is LgColumn && ((LgColumn)obj).RightToLeftLayout)
                        alignmentTypeHori = GuiUtils.reverseHorizontalAlignment(alignmentTypeHori);

                    ContentAlignment contentAlignment = ControlUtils.GetContentAligmentForSetHorAligment(currentContentAlignment, alignmentTypeHori);

                    ((LogicalControl)obj).ContentAlignment = contentAlignment;
                }
                //  Condition (obj is MgLinkLabel) is added for mobile. QCR #786015
                else if (obj is Control)
                    ControlUtils.SetHorizontalAlignment((Control)obj, alignmentTypeHori);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setImageData(GuiCommand guiCommand)
        {
            Image image = GuiUtils.GetImageFromBinary(guiCommand.ByteArray);

            setImageInfo(guiCommand, image);
        }

        /// <summary>
        ///   set password edit
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setPasswordEdit(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is MgTextBox)
                ControlUtils.SetPasswordEdit((MgTextBox)obj, guiCommand.Bool3);
            else if (obj is LogicalControl)
                ((LogicalControl)obj).PasswordEdit = guiCommand.Bool3;
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setRightToLeft(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is LogicalControl)
                ((LogicalControl)obj).RightToLeft = guiCommand.Bool3;
            else if (obj is Control)
                GuiUtils.setRightToLeft((Control)obj, guiCommand.Bool3);
            else
                Debug.Assert(false);
        }

        /// <summary>
        ///   set setRadioButtonAppearance
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setThreeState(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is MgCheckBox)
                GuiUtils.SetThreeStates((MgCheckBox)obj, guiCommand.Bool3);
            else if (obj is LgCheckBox)
                ((LgCheckBox)obj).ThreeStates = guiCommand.Bool3;
        }

        /// <summary>
        ///   Add controls to a tab control
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void AddControlsToTab(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.parentObject, guiCommand.line);
            TabControl tab = ((TabControl)obj);

            Control child = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (guiCommand.layer > 0)
                tab.Controls[guiCommand.layer - 1].Controls.Add(child);
            else
                tab.Controls[tab.SelectedIndex].Controls.Add(child);
        }

        /// <summary>
        ///   set side property of a tab control
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setTabControlSide(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is MgTabControl)
                ControlUtils.SetTabAlignment((MgTabControl)obj, (SideType)guiCommand.number);
        }

        /// <summary>
        ///   set setMaxBox
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setMaxBox(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            Form form = GuiUtils.getForm(obj);

            form.MaximizeBox = guiCommand.Bool3;
        }

        /// <summary>
        ///   set setMinBox
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setMinBox(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            Form form = GuiUtils.getForm(obj);

            form.MinimizeBox = guiCommand.Bool3;
        }

        /// <summary>
        /// </summary>
        internal void createForm(GuiCommand guiCommand)
        {
            if (Events.ShouldLog(Logger.LogLevels.Gui))
                Events.WriteGuiToLog(String.Format("Creating form \"{0}\"", guiCommand.str));

            GuiForm form = new GuiForm();
            form.Text = "";
#if !PocketPC
            form.AllowMove = guiCommand.windowType != WindowType.FitToMdi;
            form.AllowDrop = true;
            form.MaximumSize = new Size(100000, 100000);
#endif
            GuiUtils.CreateTagData(form);
            form.SuspendLayout();
            ((TagData)form.Tag).WindowType = guiCommand.windowType;
            bool isSDI = guiCommand.windowType == WindowType.Sdi;
            bool isMDIFrame = guiCommand.windowType == WindowType.MdiFrame;
            form.AutoScroll = false;
            form.ShouldBlockAnsestorActivation = guiCommand.Bool1 && guiCommand.windowType == WindowType.MdiChild;

            // create a panel that will fill the area left from the client area after
            // reducing the area that the status bar and the tool bar occupy
            Control clientAreaPanel;
#if !PocketPC
            if (isMDIFrame)
            {
                if (guiCommand.createInternalFormForMDI)
                {
                    GuiUtils.SetMDIFrame(form);
                    GuiForm fitToMdiForm = new GuiForm();
                    GuiUtils.CreateTagData(fitToMdiForm);
                    fitToMdiForm.SuspendLayout();
                    ((TagData)fitToMdiForm.Tag).IsMDIClientForm = true;
                    fitToMdiForm.Text = "";
                    fitToMdiForm.Name = "MDIClientForm";
                    fitToMdiForm.AllowMove = false;
                    fitToMdiForm.AllowDrop = true;
                    fitToMdiForm.MaximumSize = new Size(100000, 100000);
                    fitToMdiForm.ControlBox = false;
                    fitToMdiForm.FormBorderStyle = FormBorderStyle.FixedSingle;
                    fitToMdiForm.MinimizeBox = false;
                    fitToMdiForm.MaximizeBox = false;
                    fitToMdiForm.AutoScroll = false;
                    fitToMdiForm.MdiParent = form;
                    fitToMdiForm.Dock = DockStyle.Fill;
                    FormHandler.getInstance().addMainProgramMDIFormActivatedHandler(fitToMdiForm);

                    clientAreaPanel = GuiUtils.createInnerPanel(fitToMdiForm, false);
                    clientAreaPanel.Name = "FitToMdiPanel";
                    clientAreaPanel.Dock = DockStyle.Fill;
                    fitToMdiForm.Controls.Add(clientAreaPanel);

                    ((TagData)form.Tag).MDIClientForm = fitToMdiForm;
                }
                else
                    clientAreaPanel = GuiUtils.SetMDIFrame(form);
            }
            else
#endif
                clientAreaPanel = GuiUtils.createInnerPanel(form, guiCommand.isHelpWindow);

            //For help window, do not register panel events.
            if (!guiCommand.isHelpWindow)
                SubformPanelHandler.getInstance().addHandler(clientAreaPanel);
            ((TagData)clientAreaPanel.Tag).IsClientPanel = true;
            ControlsMap.getInstance().add(guiCommand.obj, clientAreaPanel);
            ((TagData)form.Tag).ClientPanel = clientAreaPanel;

            //Register default handlers for all the forms except help form.
            if (!guiCommand.isHelpWindow)
                FormHandler.getInstance().addHandler(form);

            if (!isMDIFrame)
            {
                ((Panel)clientAreaPanel).RightToLeft = RightToLeft.Inherit;
                clientAreaPanel.SuspendLayout();
                clientAreaPanel.Dock = (isSDI
                    ? DockStyle.Top
                    : DockStyle.Fill);
                form.Controls.Add(clientAreaPanel);
            }

            // On Mobile, ownership between forms may cause bad behavior - forms may be hidden/closed
            // when they shouldn't, so we leave the mobile forms ownerless.
#if !PocketPC
            Control ownerPanel = guiCommand.parentObject == null
                ? null
                : (Control)ControlsMap.getInstance().object2Widget(guiCommand.parentObject);
            Form ownerForm = GuiUtils.FindForm(ownerPanel);
            bool isModal = guiCommand.windowType == WindowType.Modal;

            if (guiCommand.windowType == WindowType.MdiChild || guiCommand.windowType == WindowType.FitToMdi)
            {
                form.MdiParent = ownerForm;
                if (guiCommand.windowType == WindowType.FitToMdi)
                    form.Dock = DockStyle.Fill;
            }
            // #932179: If Help called from Modal window do not set owner form
            else if (isModal && ownerForm != null && !guiCommand.isHelpWindow)
                form.Owner = ownerForm;
            else if (guiCommand.windowType == WindowType.Floating ||
                     guiCommand.windowType == WindowType.Tool)
            {
                form.Owner = ownerForm;
            }
            else if (guiCommand.windowType == WindowType.ChildWindow)
            {
                form.Owner = ownerForm;

                // If the current window is a child window, add it as a control of its owner.
                // A top level control cannot be added as a child, so set TopLevel as false.
                form.TopLevel = false;
                ownerPanel.Controls.Add(form);
                form.BringToFront();
            }
#endif

#if PocketPC
// Get the key preview - enable us to process the tab key correctly
         form.KeyPreview = true;
#else
            //tmp
            ((TagData)form.Tag).toolBar = null;
#endif

            if (Events.ShouldLog(Logger.LogLevels.Gui))
                Events.WriteGuiToLog(String.Format("Form \"{0}\" created", guiCommand.str));
        }

        /// <summary>
        ///   Opens form and sets it's minimum size
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void InitialFormLayout(GuiCommand guiCommand)
        {
            if (Events.ShouldLog(Logger.LogLevels.Gui))
                Events.WriteGuiToLog(String.Format("Initial form layout \"{0}\"", guiCommand.str));

            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            Form form = GuiUtils.getForm(control);

#if !PocketPC
            int menuHeight = 0;

            if (form.MainMenuStrip != null)
                menuHeight = form.MainMenuStrip.Height;
#endif

            GuiUtils.resumeLayout(form, false);
            GuiUtils.performLayout(form);
#if !PocketPC
            if (form.MainMenuStrip != null)
            {
                //QCR #932716, menu size is determine only after layout, must correct form size according to new menu size
                //might be more than one line
                if (form.MainMenuStrip.Height != menuHeight)
                {
                    Size size = form.ClientSize;
                    size.Height += form.MainMenuStrip.Height - menuHeight;
                    form.ClientSize = size;
                }
            }
#endif

            if (((TagData)form.Tag).WindowType == WindowType.MdiFrame)
            {
                Form internalForm = ((TagData)form.Tag).MDIClientForm;

                if (internalForm != null)
                {
                    IntPtr handle = internalForm.Handle;
                    GuiUtils.resumeLayout(internalForm, false);
                    GuiUtils.performLayout(internalForm);
                }
            }

            if (((TagData)control.Tag).IsClientPanel) //resume layout for the client area panel of sdi
            {
                GuiUtils.resumeLayout(control, false);
                GuiUtils.performLayout(control);
            }

#if PocketPC
// If we have an SDI window, perform the layout now, as we don't get the layout event.
         if (((TagData)form.Tag).WindowType == WindowType.Sdi)
            FormHandler.SDIFormLayout(form);
#endif
        }

        /// <summary>
        /// show form
        /// </summary>
        /// <param name="guiCommand"></param>
        private void ShowForm(GuiCommand guiCommand)
        {


            Control clientPanel = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            Form form = GuiUtils.getForm(clientPanel);
            if (SpashScreenManager.Instance.IsSplashOpen)
            {
                SpashScreenManager.Instance.CloseSplashWindow();
                form.Load += Form_Load;
            }

            // For SHOW_FORM command Bool1 indicates the flag isHelpWindow.
            // For Help window register Deactivate event as if help window is deactivated, then it should be closed.
            if (guiCommand.Bool1)
                FormHandler.getInstance().addDeActivatedHandler(form as GuiForm);

            //Some .NET controls may add a close handler to the form (Syncfusion edit control displays a dialog for saving 
            //changes). .NET framework executes ALL registered event handlers irrespective of whether the event was canceled.
            //If the event was canceled by some other handler, we should not handle CLOSING event. Hence, our handler should
            //be registered last (just before showing the window, after all controls on the form are created)
            FormHandler.getInstance().addClosingHandler(form as GuiForm);

            //bug# 184850, This is .NET behavior, when we have form that it's properties MaximizeBox = false &&	WindowState =Maximized
            //             The TaskBar is hidden by the form size 
            bool orgMaxBox = form.MaximizeBox;
            if (form.WindowState == FormWindowState.Maximized)
                form.MaximizeBox = true;

#if !PocketPC
            // If the startup screen is open, then close it while opening an application form.
            if (GUIMain.getInstance().IsStartupScreenOpen())
                GUIMain.getInstance().HideStartupScreen();

            bool showInTaskbar = false;

            if (((TagData)form.Tag).WindowType == WindowType.Sdi)
                showInTaskbar = true; //show taskbar icon for SDI window 
            else
                showInTaskbar = (Application.OpenForms.Count == 1); //this is the first window, dummy window is not counted

            form.ShowInTaskbar = showInTaskbar;
#endif
            // Check and update IsFirstFormOpened on RuntimeContext when form is opened in GuiThread. It must be done 
            // before calling Show/ShowDialog as it raises FormActivate handler (where contextswitch is handled).
            Events.OnShowForm((GuiMgForm)guiCommand.obj);

            if (Events.ShouldLog(Logger.LogLevels.Gui))
                Events.WriteGuiToLog(String.Format("Showing form \"{0}\"", guiCommand.str));

            //Defect #130747:
            //While showing the form, framework sets the focus on active control (which happens to be the first parkable control at this point).
            //But, until worker thread informs where to park, the focus needs to be on the inner client panel.
            //So, set the client panel to be the ActiveControl and the framework will focus it.
            form.ActiveControl = clientPanel;

            if (guiCommand.Bool3) //show dialog 
            {
                // The instance of Run() that called this function will halt till we come out of ShowDialog()
                // Hence gui thread can not process commands with this instance of Run(). 
                GuiThreadIsAvailableToProcessCommands = false;
                _modalShowFormCommandPresentInQueue = false;

#if !PocketPC
                // ACCORDING to MSDN : 
                // When using the ShowDialog method (without parameter), the currently active window is made the owner of the dialog box.
                // If one needs to specify a specific owner, then the overloaded version of ShowDialog should be used.
                // 
                // Defect# 129179
                // We require this because while closing the showDialog, in some cases (i.e. when xpa application is not active), it internally activates
                // another MDI Child and although we activate the Parent of a MODAL (through ACTIVATE_FORM), it is not getting activated.
                // When owner form is explicitly specified in ShowDialog() then the MDI Child isn't gettign activated while the dialog is closing.
                Form ownerForm = form.Owner;
                form.Owner = null;

                // Set ShouldResetPrintPreviewInfo = false here. This will be change to true in OnFormActivate() when ShowDialog is called.
                // When this modal form is closed, in closeForm(), this will be set to false again and it will be change to true as below after
                // form.ShowDialog() call. This way, focus issues for printPreview can be resolved when Print Preview called from modal window.
                PrintPreviewFocusManager.GetInstance().ShouldResetPrintPreviewInfo = false;
                PrintPreviewFocusManager.GetInstance().IsInModalFormOpening = true;
                form.ShowDialog(ownerForm);
                PrintPreviewFocusManager.GetInstance().ShouldResetPrintPreviewInfo = true;

                // Since it is set to false after handling CLOSE_FORM of Modal, set it to true again to indicate that GuiThread is processing commands.
                GuiThreadIsAvailableToProcessCommands = true;
#else
            form.ShowDialog();
#endif

                form.Dispose(); //QCR #778429
            }
            else
            {
                if (((TagData)form.Tag).WindowType == WindowType.MdiFrame)
                {
                    Form internalForm = ((TagData)form.Tag).MDIClientForm;

                    if (internalForm != null)
                        internalForm.Show();
                }
                // When a form is shown, internally it is activated, which resets PrintPreview flag.
                // Here if Print Preview is already opened, it should remain as foreground. So set
                // flag accordingly and re-activate Print Preview when any other Runtime window is
                // activated. This applies to form.Close() also. TODO: Handle case for modal window
                // i.e. form.ShowDialog(). But there are lot of issues with it.
                using (new PrintPreviewInfoResetGuard())
                {
                    form.Show();
                }
                form.MaximizeBox = orgMaxBox;
                //fix according to defect 132726 and  Story 142377
                if (Manager.Environment.SpecialRestoreMaximizedForm)
                {
                    if (!form.MaximizeBox && ((TagData)form.Tag).WindowType == WindowType.MdiChild && form.WindowState == FormWindowState.Maximized)
                        form.WindowState = FormWindowState.Normal;
                }
            }


            if (Events.ShouldLog(Logger.LogLevels.Gui))
                Events.WriteGuiToLog(String.Format("Form \"{0}\" opened", guiCommand.str));
        }

        private void Form_Load(object sender, EventArgs e)
        {
            ((Form)sender).TopMost = true;
            ((Form)sender).BringToFront();
            ((Form)sender).TopMost = false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="guiCommand"></param>
        private void refreshTmpEditor(GuiCommand guiCommand)
        {
            MgPanel panel = (MgPanel)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (panel != null && panel.Parent != null)
            {
                if (guiCommand.Bool3)
                    refreshOldEditor((Form)panel.Parent);
                ContainerManager.refreshTmpEditor((Form)panel.Parent);
            }
        }

        /// <summary>Calls the start timer function.</summary>
        /// <param name = "guiCommand"></param>
        private void startTimer(GuiCommand guiCommand)
        {
            ((MgTimer)guiCommand.obj).Start();
        }

        /// <summary>Calls the stop timer function.</summary>
        /// <param name="guiCommand"></param>
        private void stopTimer(GuiCommand guiCommand)
        {
            ((MgTimer)guiCommand.obj).Stop();
        }

        /// <summary>
        /// Sets hint text
        /// </summary>
        /// <param name="guiCommand"></param>
        private void setEditHint(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is LgText)
                ((LgText)obj).HintText = guiCommand.str;

            else if (obj is MgTextBox) //For case of AllowTesting = Y
                ControlUtils.SetHint(obj as MgTextBox, guiCommand.str);
        }

        /// <summary>
        /// Sets hint text color
        /// </summary>
        /// <param name="guiCommand"></param>
        private void setEditHintColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if ((obj is LgText || obj is MgTextBox))
            {
                Color color = Color.Empty;
                if (guiCommand.mgColor != null)
                    color = ControlUtils.MgColor2Color(guiCommand.mgColor, false, false);

                if (obj is LgText)
                    ((LgText)obj).HintForeColor = color;
                else if (obj is MgTextBox)
                    ControlUtils.SetHintColor((MgTextBox)obj, color);
            }
        }

        /// <summary>
        ///   create table's row
        /// </summary>
        /// <param name = "guiCommand">
        /// </param>
        internal void createTableRow(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (table != null)
                GuiUtils.getTableManager(table).createRow(guiCommand.number);
        }

        /// <summary>
        ///   create table's row
        /// </summary>
        /// <param name = "guiCommand">
        /// </param>
        internal void undoCreateTableRow(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (table != null)
                GuiUtils.getTableManager(table).undoCreateRow(guiCommand.number);
        }

        /// <summary>
        /// set visibility of the row
        /// </summary>
        /// <param name = "guiCommand">
        /// </param>
        internal void SetTableRowVisibility(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (table != null)
                GuiUtils.getTableManager(table).SetTableRowVisibility(guiCommand.line, guiCommand.Bool3, guiCommand.Bool1);
        }

        /// <summary>
        ///   Set highlighted row background color
        /// </summary>
        internal void setRowHighlightBgColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is TableControl)
            {
                Color rowHighlightColor = ControlUtils.MgColor2Color(guiCommand.mgColor, true, true);
                GuiUtils.getTableManager((TableControl)obj).setHightlightBgColor(rowHighlightColor);
            }
        }

        /// <summary>
        ///   Set highlighted row foreground color
        /// </summary>
        internal void setRowHighlightFgColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is TableControl)
            {
                Color rowHighlightColor = ControlUtils.MgColor2Color(guiCommand.mgColor, false, false);
                GuiUtils.getTableManager((TableControl)obj).setHightlightFgColor(rowHighlightColor);
            }
        }

        internal void setInactiveRowHighlightBgColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is TableControl)
            {
                Color rowHighlightColor = ControlUtils.MgColor2Color(guiCommand.mgColor, true, true);
                GuiUtils.getTableManager((TableControl)obj).setInactiveHightlightBgColor(rowHighlightColor);
            }
        }


        /// <summary>
        ///   Set highlighted row foreground color
        /// </summary>
        internal void setInactiveRowHighlightFgColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is TableControl)
            {
                Color rowHighlightColor = ControlUtils.MgColor2Color(guiCommand.mgColor, false, false);
                GuiUtils.getTableManager((TableControl)obj).setInactiveHightlightFgColor(rowHighlightColor);
            }
        }

        internal void setActiveRowHighlightState(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is TableControl)
                GuiUtils.getTableManager((TableControl)obj).setActiveState(guiCommand.Bool3);
        }

        /// <summary>
        ///   sets table's alternating color
        /// </summary>
        internal void setAlternatedColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is TableControl)
            {
                Color color = ControlUtils.MgColor2Color(guiCommand.mgColor, true, true);
                GuiUtils.getTableManager((TableControl)obj).setAlternateColor(color);
            }
        }


        /// <summary>
        ///   sets table's alternating color
        /// </summary>
        internal void setTitleColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            Color color = Color.Empty;
            if (guiCommand.mgColor != null)
                color = ControlUtils.MgColor2Color(guiCommand.mgColor, !(obj is TableControl), !(obj is TableControl));

            if (obj is TableControl)
            {
                ((TableControl)obj).TitleColor = color;
                ((TableControl)obj).RefreshAllHeaderLabelControls();
            }
#if !PocketPC
            else if (obj is MgTabControl)
            {
                ((MgTabControl)obj).TitleColor = color;
            }
#endif
        }

        /// <summary>
        /// set title foreground color
        /// </summary>
        /// <param name="guiCommand"></param>
        internal virtual void setTitleFgColor(GuiCommand guiCommand)
        {
            Events.WriteExceptionToLog("setTitleFgColor - Not Implemented Yet");
        }

        /// <summary>
        /// set HottrackColor
        /// </summary>
        /// <param name="guiCommand"></param>
        internal virtual void setHotTrackColor(GuiCommand guiCommand)
        {
            Events.WriteExceptionToLog("setHotTrackColor - Not Implemented Yet");
        }

        /// <summary>
        /// set HottrackFgColor
        /// </summary>
        /// <param name="guiCommand"></param>
        internal virtual void setHotTrackFgColor(GuiCommand guiCommand)
        {
            Events.WriteExceptionToLog("setHotTrackFgColor - Not Implemented Yet");
        }

        /// <summary>
        /// set selected tab color
        /// </summary>
        /// <param name="guiCommand"></param>
        internal virtual void setSelectedTabColor(GuiCommand guiCommand)
        {
            Events.WriteExceptionToLog("setSelectedTabColor - Not Implemented Yet");
        }

        /// <summary>
        /// set selected tab Fg color
        /// </summary>
        /// <param name="guiCommand"></param>
        internal virtual void setSelectedTabFgColor(GuiCommand guiCommand)
        {
            Events.WriteExceptionToLog("setSelectedTabFgColor - Not Implemented Yet");
        }

        /// <summary>
        /// Sets table column divider color
        /// </summary>
        /// <param name="guiCommand"></param>
        internal void setDividerColor(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is TableControl)
            {
                Color color = Color.Empty;
                if (guiCommand.mgColor != null)
                    color = ControlUtils.MgColor2Color(guiCommand.mgColor, false, false);
                ((TableControl)obj).DividerColor = color;
            }
        }

        /// <summary>
        ///   sets table's color by property
        /// </summary>
        internal void setColorBy(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is TableControl)
                GuiUtils.getTableManager((TableControl)obj).setColorBy(guiCommand.number);
            else
                Debug.Assert(false);
        }

        /// <summary>
        ///   update index of temporary editor due to adding rows in the begining of the table
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void updateTmpEditorIndex(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (table != null)
                GuiUtils.getTableManager(table).updateTmpEditorIndex();
        }

        /// <summary>
        ///   for column set true\false for the allow resizeable
        /// </summary>
        internal void setResizable(GuiCommand guiCommand)
        {
            Object table = ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (table is TableControl)
                GuiUtils.getTableManager((TableControl)table).setResizable(guiCommand.Bool3);
        }

        /// <summary>
        ///   sets row height
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setRowHeight(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            GuiUtils.getTableManager(table).setRowHeight(guiCommand.number);
        }

        /// <summary>
        ///   sets title height
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setTitleHeight(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            GuiUtils.getTableManager(table).setTitleHeight(guiCommand.number);
        }

        /// <summary> set the bottom position interval of the table </summary>
        /// <param name = "guiCommand"></param>
        internal void setBottomPositionInterval(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            GuiUtils.getTableManager(table).setBottomPositionInterval((BottomPositionInterval)guiCommand.number);

            TagData td = (TagData)table.Tag;
            if (td.Bounds != null)
                GuiUtils.setBounds(table, (Rectangle)td.Bounds);
        }

        /// <summary>
        ///   for column set true\false for the allow moveable
        /// </summary>
        internal void setAllowReorder(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            table.AllowColumnReorder = guiCommand.Bool3;
        }

        /// <summary>
        ///   set Table's item's count
        /// </summary>
        /// <param name = "guiCommand"></param>
        private void SetTableItemsCount(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (table != null)
                GuiUtils.getTableManager(table).SetTableItemsCount(guiCommand.number);
        }

        /// <summary>
        ///   set Table's virtual items count
        /// </summary>
        /// <param name = "guiCommand"></param>
        private void SetTableVirtualItemsCount(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (table != null)
                GuiUtils.getTableManager(table).SetTableVirtualItemsCount(guiCommand.number);
        }

        /// <summary>
        /// set the table vertical scroll's thumb position
        /// </summary>
        /// <param name = "guiCommand"></param>
        private void SetVScrollThumbPos(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            Debug.Assert(table != null);
            GuiUtils.getTableManager(table).SetVScrollThumbPos(guiCommand.number);
        }

        /// <summary>
        /// set the page size for table's vertical scrollbar
        /// </summary>
        /// <param name = "guiCommand"></param>
        private void SetVScrollPageSize(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            Debug.Assert(table != null);
            GuiUtils.getTableManager(table).SetVScrollPageSize(guiCommand.number);
        }

        /// <summary>
        /// set the records before current view
        /// </summary>
        /// <param name = "guiCommand"></param>
        private void SetRecordsBeforeCurrentView(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            Debug.Assert(obj != null && obj is TableControlUnlimitedItems);
            ((TableManagerUnlimitedItems)GuiUtils.getTableManager((TableControlUnlimitedItems)obj)).SetRecordsBeforeCurrentView(guiCommand.line);
        }

        /// <summary>
        /// insert rows into table control
        /// </summary>
        /// <param name="guiCommand"></param>
        private void InsertRows(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            Debug.Assert(table != null);
            GuiUtils.getTableManager(table).InsertRows(guiCommand.number, guiCommand.number1, guiCommand.Bool1);
        }

        /// <summary>
        /// remove rows from table control
        /// </summary>
        /// <param name="guiCommand"></param>
        private void RemoveRows(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            Debug.Assert(table != null);
            GuiUtils.getTableManager(table).RemoveRows(guiCommand.number, guiCommand.number1, guiCommand.Bool1);
        }

        /// <summary>
        /// Toggles color for first row when a table has alternate color. Needed while scrolling tables with
        /// limited items
        /// </summary>
        /// <param name="guiCommand"></param>
        private void ToggleAlternateColorForFirstRow(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            Debug.Assert(table != null && table is TableControlLimitedItems);
            TableManagerLimitedItems tableManager = (TableManagerLimitedItems)GuiUtils.getTableManager(table);
            tableManager.ToggleAlternateColorForFirstRow();
        }

        /// <summary>
        ///   set's table's include first
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void SetTableIncludesFirst(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (table != null)
            {
                TableManager tableManager = GuiUtils.getTableManager(table);
                Debug.Assert(tableManager is TableManagerUnlimitedItems);
                ((TableManagerUnlimitedItems)tableManager).setIncludesFirst(guiCommand.Bool3);
            }
        }

        /// <summary>
        ///   sets Table Include Last
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void SetTableIncludesLast(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (table != null)
            {
                TableManager tableManager = GuiUtils.getTableManager(table);
                Debug.Assert(tableManager is TableManagerUnlimitedItems);
                ((TableManagerUnlimitedItems)tableManager).setIncludesLast(guiCommand.Bool3);
            }
        }

        /// <summary>
        ///   sets table's top index
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void SetTableTopIndex(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            Debug.Assert(table != null);
            GuiUtils.getTableManager(table).SetTopIndex(guiCommand.number);
        }

        /// <summary>
        ///   refresh table
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void refreshTable(GuiCommand guiCommand)
        {
            MgControlBase mgControl = guiCommand.obj as MgControlBase;
            bool refreshEntireTable = guiCommand.Bool3;
            if (refreshEntireTable)
                mgControl.refreshTableCommandCount--;

            // If there are still some more commands in the queue to refresh entire table (mgControl.refreshTableCommandCount > 0), don't execute current command.
            // It is true even if current command is to refresh single row.
            // It may happen that worker thread will keep on adding refresh commands, thereby restricting gui thread to process the commands as the counter will 
            // increase continously. To avoid this starvation, gui thread should execute the command after certain interval (mgControl.refreshTableCommandCount != 15)
            if (mgControl.refreshTableCommandCount > 0 && mgControl.refreshTableCommandCount != 15)
                return;

            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(mgControl, 0);
            Debug.Assert(table != null);
            GuiUtils.getTableManager(table).refreshTable(refreshEntireTable);
        }

        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void invalidateTable(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            Debug.Assert(table != null);
            GuiUtils.getTableManager(table).invalidateTable();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="guiCommand"></param>
        internal void validateTableRow(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            Debug.Assert(table != null);
            GuiUtils.getTableManager(table).validateRow(guiCommand.number);
        }

        /// <summary>
        ///   clear the Sort Mark from all the columns of the table
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void clearTableColumnsSortMark(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            Debug.Assert(table != null);
            GuiUtils.getTableManager(table).clearColumnsSortMark();
        }

        /// <summary>
        ///   showScrollbar
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void showScrollbar(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            table.VerticalScrollBar = guiCommand.Bool3;
        }

        /// <summary>
        ///   showColumnDividers
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void showColumnDividers(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            table.ShowColumnDividers = guiCommand.Bool3;
        }

        /// <summary>
        ///   showLineDividers
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void showLineDividers(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            table.ShowLineDividers = guiCommand.Bool3;
        }

        /// <summary>
        ///   rowHighlitingStyle
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void rowHighlitingStyle(GuiCommand guiCommand)
        {
            TableControl table = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            Debug.Assert(table != null);
            GuiUtils.getTableManager(table).RowHighlitingStyle = (RowHighlightType)guiCommand.number;
        }

        /// <summary>
        ///   for column set true\false for the allow moveable
        /// </summary>
        internal void setSortableColumn(GuiCommand guiCommand)
        {
            LgColumn columnManager = (LgColumn)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            columnManager.IsSortable = guiCommand.Bool3;
        }

        internal void setFilterableColumn(GuiCommand guiCommand)
        {
            LgColumn columnManager = (LgColumn)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            columnManager.AllowFilter = guiCommand.Bool3;
        }

        /// <summary>
        ///   for column set true\false for the Right border
        /// </summary>
        internal void setColumnRightBorder(GuiCommand guiCommand)
        {
            LgColumn columnManager = (LgColumn)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            columnManager.RightBorder = guiCommand.Bool3;
        }

        /// <summary>
        ///   for column set true\false for the Top border
        /// </summary>
        internal void setColumnTopBorder(GuiCommand guiCommand)
        {
            LgColumn columnManager = (LgColumn)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            columnManager.TopBorder = guiCommand.Bool3;
        }

        /// <summary>
        ///   for column set true\false for the allow moveable
        /// </summary>
        internal void setPlacement(GuiCommand guiCommand)
        {
            LgColumn columnManager = (LgColumn)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            columnManager.setPlacement(guiCommand.Bool3);
        }

        /// <summary>
        ///   set original width for column
        /// </summary>
        internal void setOrgWidth(GuiCommand guiCommand)
        {
            LgColumn columnManager = (LgColumn)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            columnManager.setOrgWidth(guiCommand.number);
        }

        /// <summary>
        ///   set start pos for column
        /// </summary>
        internal void setStartPos(GuiCommand guiCommand)
        {
            LgColumn columnManager = (LgColumn)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            columnManager.setStartXPos(guiCommand.number);
        }

        /// <summary>
        ///   set AccessTest property
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setEnvAccessTest(GuiCommand guiCommand)
        {
            GuiUtils.AccessTest = guiCommand.Bool3;
        }


        /// <summary>
        /// save the language
        /// </summary>
        /// <param name="guiCommand"></param>
        internal void setLanguage(GuiCommand guiCommand)
        {
            com.magicsoftware.controls.utils.Utils.Language = (char)guiCommand.number;
        }

        /// <summary>
        ///   set SpecialTextSizeFactoring property
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setSpecialTextSizeFactoring(GuiCommand guiCommand)
        {
            com.magicsoftware.controls.utils.Utils.SpecialTextSizeFactoring = guiCommand.Bool3;
        }

        /// <summary>
        ///   set SpecialFlatEditOnClassicTheme property
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setSpecialFlatEditOnClassicTheme(GuiCommand guiCommand)
        {
            com.magicsoftware.controls.utils.Utils.SpecialFlatEditOnClassicTheme = guiCommand.Bool3;
        }

        /// <summary>
        ///   set SpecialSwipeFlickeringRemoval
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setSpecialSwipeFlickeringRemoval(GuiCommand guiCommand)
        {
            com.magicsoftware.controls.utils.Utils.SpecialSwipeFlickeringRemoval = guiCommand.Bool3;
        }



        /// <summary>
        ///   sets table seletcion index
        /// </summary>
        /// <param name = "guiCommand">
        /// </param>
        internal void SetSelectionIndex(GuiCommand guiCommand)
        {
            Control widget = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (widget != null)
                GuiUtils.getItemsManager(widget).setSelectionIndex(guiCommand.number);
        }

        internal void comboDropDown(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is LgCombo)
                obj = ((LgCombo)obj).getEditorControl();
            if (obj is MgComboBox)
                ((MgComboBox)obj).DroppedDown = true;
        }

        /// <summary>
        ///   dispose an object
        /// </summary>
        internal void disposeObject(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);

            if (obj is ToolStrip)
            {
                ToolStrip menu = (ToolStrip)obj;
                menu.Dispose();
            }
            else if (obj is Control)
            {
                Control control = (Control)obj;
                // fixed bug #:915499, when control is dispose we need to reset the menu so the
                // control.dispose will not dispose the menu that connect to the control
                // when the shell will be dispose the all menus will be dispose also.
                //GuiUtils.setContextMenu(control, null);
                control.Dispose();
            }
            else if (obj is ToolStripItem)
            {
                ToolStripItem menuItem = (ToolStripItem)obj;
                menuItem.Dispose();
            }
            // Applicable only for Mobile but since this class is also supported 
            // in standard framework. Ifdef is avoided.
            else if (obj is MainMenu)
            {
                MainMenu menu = (MainMenu)obj;
                menu.Dispose();
            }
            // Applicable only for Mobile but since this class is also supported 
            // in standard framework. Ifdef is avoided.
            else if (obj is ContextMenu)
            {
                ContextMenu menu = (ContextMenu)obj;
                menu.Dispose();
            }
            // Applicable only for Mobile but since this class is also supported 
            // in standard framework. Ifdef is avoided.
            else if (obj is MenuItem)
            {
                MenuItem menuItem = (MenuItem)obj;
                menuItem.Dispose();
            }
            else
                Debug.Assert(false);
        }

        /// <summary>
        ///   set data autowide
        /// </summary>
        /// <param name = "guiCommand">
        /// </param>
        internal void setAutoWide(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            if (obj is LogicalControl)
            {
                LogicalControl child = (LogicalControl)obj;
                child.AutoWide = guiCommand.Bool3;
            }
            else if (obj is Control)
                ((TagData)((Control)obj).Tag).CheckAutoWide = guiCommand.Bool3;
        }

        /// <summary>
        ///   remove the composie controls of the sub form control
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void removeCompositeControls(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);

            if (obj is Panel)
            {
                Panel panel = (Panel)obj;

#if !PocketPC
                // If current panel contained active control from the form, then set focus to panel 
                // so that we can dispose the control (some controls like Syncfusion's edit control
                // throw exception if tried to dispose while focused).
                Form containerForm = GuiUtils.FindForm(panel);
                if (panel == GuiUtils.getSubformPanel(containerForm.ActiveControl))
                    panel.Focus();
#endif


                for (int i = panel.Controls.Count - 1; i >= 0; i--)
                {
                    Control dispCtrl = panel.Controls[i];
#if PocketPC
               if (dispCtrl == panel.dummy)
                  continue;
#else
                    RemoveTableControlFromContainer(containerForm, dispCtrl);
#endif
                    dispCtrl.Dispose();
                }

                GuiUtils.getContainerManager(panel).Dispose();
                new BasicControlsManager(panel);
                panel.Invalidate();
            }
        }

#if !PocketPC
        /// <summary>
        ///   remove the child table controls of the controls of sub form control
        /// </summary>
        /// <param name = "guiCommand"></param>
        void RemoveTableControlFromContainer(Form containerForm, Control control)
        {
            if (control is TableControl)
            {
                //If a table control is disposed, remove it from the collection of
                //table controls which is maintained on the form's TagData.
                ((TagData)containerForm.Tag).TableControls.Remove(control);
            }
            else
            {
                Control parent = control;
                for (int i = parent.Controls.Count - 1; i >= 0; i--)
                {
                    RemoveTableControlFromContainer(containerForm, parent.Controls[i]);
                }
            }
        }
#endif

        /// <summary>
        ///   set true\false on the check box \ radio button
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setChecked(GuiCommand guiCommand)
        {
            ArrayList arrayControl = ControlsMap.getInstance().object2WidgetArray(guiCommand.obj, GuiConstants.ALL_LINES);
            Object obj = arrayControl[0];

            if (obj is MgRadioPanel)
                GuiUtils.SetChecked((MgRadioPanel)obj, guiCommand.number);
#if !PocketPC
            else if (obj is ToolStripItem)
            {
                for (int i = 0;
                    i < arrayControl.Count;
                    i++)
                {
                    Object mnuItem = arrayControl[i];
                    GuiUtils.setChecked(mnuItem, guiCommand.Bool3);
                }
            }
#endif
            else
            {
                obj = arrayControl[guiCommand.line];
                if (obj is LgCheckBox)
                    ((LgCheckBox)obj).setChecked(guiCommand.Bool3);
                else if (obj is LgRadioContainer)
                    ((LgRadioContainer)obj).SelectionIdx = guiCommand.number;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal abstract void setWindowState(GuiCommand guiCommand);

        /// <summary>
        ///   for radio button set number columns in the control
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setLayoutNumColumns(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            GuiUtils.SetLayoutNumColumns(obj, guiCommand.number);
        }

        /// <summary>
        ///   set setRadioButtonAppearance
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void setRadioButtonAppearance(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);
            RbAppearance radioButtonAppearance = (RbAppearance)guiCommand.number;

            GuiUtils.SetRadioAppearance(obj, ControlUtils.RadioButtonAppearance2Appearance(radioButtonAppearance));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="guiCommand"></param>
        internal void SuspendLayout(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);

            if (IsMagicContainer((Control)obj))
            {
#if !PocketPC
                Form form = GuiUtils.getForm(obj);

                if (form != null && form.IsMdiContainer)
                    form.SuspendLayout();
#endif
                ((Control)obj).SuspendLayout();

                if (obj is MgTabControl)
                {
                    Panel panel = ((TagData)((Control)obj).Tag).TabControlPanel;
                    panel.SuspendLayout();
                }
            }
            else if (ControlUtils.IsMdiClient(obj))
                GuiUtils.getForm(obj).SuspendLayout();
            else if (obj is TableControlLimitedItems)
                ((Control)obj).SuspendLayout();
            else
                Debug.Assert(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="guiCommand"></param>
        internal void ResumeLayout(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj, guiCommand.line);

            // When radio panel is on table control, its resume layout will be 
            // called from LgRadioContainer.setSpecificControlProperties().
            if (obj is LgRadioContainer)
                return;
            Control control = (Control)obj;
            if (IsMagicContainer(control))
            {
                if (obj is MgTabControl)
                {
                    Panel panel = ((TagData)((Control)obj).Tag).TabControlPanel;
                    panel.ResumeLayout();
                }

                control.ResumeLayout();
                ForceFillLayoutForMgSplitter(control);

#if !PocketPC
                Form form = GuiUtils.getForm(control);

                if (form != null && form.IsMdiContainer)
                    form.ResumeLayout();
#endif
            }
            else if (ControlUtils.IsMdiClient(obj))
                GuiUtils.getForm(obj).ResumeLayout();
            else if (obj is TableControlLimitedItems)
                ((Control)obj).ResumeLayout();
            else
                Debug.Assert(false);
        }

        /// <summary>
        /// Force docking on the child
        /// When child is invisible and has Dock.Fill, executing layout on the parent does not change child's size.(default .NET behavior)
        /// This causes wrong behavior in case of MgSplitter (QCR #782379)
        /// Here we force this execution. 
        /// </summary>
        /// <param name="parent"></param>
        private static void ForceFillLayoutForMgSplitter(Control parent)
        {

            foreach (Control control in parent.Controls)
            {
                if (control is MgSplitContainer && !control.Visible)
                {
                    Debug.Assert(control.Dock == DockStyle.Fill);
                    control.Size = ((Control)parent).ClientSize;
                }
            }
        }

        /// <summary>
        /// return true if  control is legal magic container
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        static bool IsMagicContainer(Control control)
        {
            return control is MgRadioPanel
                   || control is Panel
                   || control is MgSplitContainer
                   || control is MgTabControl
                   || control is GroupBox;
        }

        /// <summary>
        /// suspend paint of the control
        /// </summary>
        /// <param name="guiCommand"></param>
        internal void SuspendPaint(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);
            if (obj is TableControlLimitedItems)
            {  //suspend/resume mechanism for table is removed
                //GuiUtils.getTableManager((TableControl)obj).SuspendPaint(); 
            }
            else if (obj is Control && ((Control)obj).Parent != null)
            {
                Control parent = ((Control)obj).Parent;

                if (parent is GuiForm && ((TagData)parent.Tag).IsMDIClientForm)
                {
                    Form mdiframe = ((GuiForm)parent).MdiParent;
                    NativeWindowCommon.SendMessage(mdiframe.Handle, NativeWindowCommon.WM_SETREDRAW, false, 0);
                }
                else
                    NativeWindowCommon.SendMessage(parent.Handle, NativeWindowCommon.WM_SETREDRAW, false, 0);

            }
            else
                Debug.Assert(false);
        }

        /// <summary>
        /// resume paint of the control
        /// </summary>
        /// <param name="guiCommand"></param>
        internal void ResumePaint(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);
            if (obj is TableControlLimitedItems)
            {  //suspend/resume mechanism for table is removed
                //GuiUtils.getTableManager((TableControl)obj).ResumePaint();
            }
            else if (obj is Control && ((Control)obj).Parent != null)
            {
                Control parent = ((Control)obj).Parent;
                if (parent is GuiForm && ((TagData)parent.Tag).IsMDIClientForm)
                {
                    Form mdiframe = ((GuiForm)parent).MdiParent;
                    NativeWindowCommon.SendMessage(mdiframe.Handle, NativeWindowCommon.WM_SETREDRAW, true, 0);
                    mdiframe.Refresh();
                }
                else
                {
                    NativeWindowCommon.SendMessage(parent.Handle, NativeWindowCommon.WM_SETREDRAW, true, 0);
                    parent.Refresh();
                }
            }
            else
                Debug.Assert(false);
        }

        /// <summary>
        ///   set current cursor
        /// </summary>
        private void setCurrentCursor(GuiCommand guiCommand)
        {
            Cursor cursor = GuiInteractive.MgCursorsToCursor((MgCursors)guiCommand.obj);
            if (cursor != null)
                Cursor.Current = cursor;
        }

        /// <summary>
        ///   sets the width of the frames
        /// </summary>
        /// <param name = "guiCommand"></param>
        private void setFramesWidth(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            if (control != null && control is MgSplitContainer)
            {
                MgSplitContainer mgSplitContainer = (MgSplitContainer)control;
                Control[] ctrls = mgSplitContainer.getControls(false);
                List<int> array = guiCommand.intList;
                int width = 0;

                for (int index = 0; index < array.Count; index++)
                {
                    width = array[index];
                    ((MgSplitContainerLayout)mgSplitContainer.LayoutEngine).setOrgSizeBy(ctrls[index], width,
                        mgSplitContainer.Height);
                }
            }
        }

        /// <summary>
        ///   sets the height of the frames
        /// </summary>
        /// <param name = "guiCommand"></param>
        private void setFramesHeight(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            if (control != null && control is MgSplitContainer)
            {
                MgSplitContainer mgSplitContainer = (MgSplitContainer)control;
                Control[] ctrls = mgSplitContainer.getControls(false);
                List<int> array = guiCommand.intList;
                int height = 0;

                for (int index = 0; index < array.Count; index++)
                {
                    height = array[index];
                    ((MgSplitContainerLayout)mgSplitContainer.LayoutEngine).setOrgSizeBy(ctrls[index],
                        mgSplitContainer.Width, height);
                }
            }
        }

        /// <summary>
        ///   set form property maximized
        /// </summary>
        /// <param name = "guiCommand"></param>
        private void setFormMaximized(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);
            Form form = GuiUtils.getForm(obj);

            if (form != null && guiCommand.Bool3)
                form.WindowState = FormWindowState.Maximized;
        }

        /// <summary>
        ///   reorder columns
        /// </summary>
        /// <param name = "guiCommand"></param>
        private void reorderColumns(GuiCommand guiCommand)
        {
            List<int[]> columnsData = guiCommand.intArrayList;

            TableControl tableControl = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            TableManager tableManager = GuiUtils.getTableManager(tableControl);

            int[] orderArray = new int[columnsData.Count];
            int[] widthArray = new int[columnsData.Count];
            int[] widthForFillTablePlacementArray = new int[columnsData.Count];

            for (int i = 0; i < columnsData.Count; i++)
            {
                orderArray[i] = columnsData[i][0];
                widthArray[i] = columnsData[i][1];
                widthForFillTablePlacementArray[i] = columnsData[i][2];
            }

            // 1. reorder column manager
            GuiUtils.reorderColumnManager(tableManager, orderArray);

            // 2. set Widths
            GuiUtils.setWidthColumns(tableManager, widthArray, widthForFillTablePlacementArray);

            // get the sort order to sort tablecontrol, based on sorted column manager
            int[] sortOrder = GuiUtils.getSortOrder(tableManager);

            // 3. sort the table control on 'orderArray'
            tableControl.sort(sortOrder);

            //// 4. sort the nativeHeaders
            //tableControl.sortNativeHeader(sortOrder);
        }

        /// <summary>
        ///   restore columns
        /// </summary>
        /// <param name = "guiCommand"></param>
        private void restoreColumns(GuiCommand guiCommand)
        {
            List<int> columnsData = guiCommand.intList;

            TableControl tableControl = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            TableManager tableManager = GuiUtils.getTableManager(tableControl);

            int[] orderArray = columnsData.ToArray();

            // 1. reorder column manager
            GuiUtils.reorderColumnManager(tableManager, orderArray);

            // get the sort order to sort tablecontrol, based on sorted column manager
            int[] sortOrder = GuiUtils.getSortOrder(tableManager);

            // 2. sort the table control on 'orderArray'
            tableControl.sort(sortOrder);

            // 3. restore the nativeHeaders
            //int[] nativeOrderArray = GuiUtils.getRestoreNativeSortOrder(tableManager.getColumns().Count);
            //tableControl.sortNativeHeader(nativeOrderArray);

            // refresh the page
            tableManager.refreshPage();
        }

        /// <summary>
        /// Sets the selection mode of the list box.
        /// </summary>
        /// <param name="guiCommand"></param>
        private void SetSelectionMode(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            if (control is ListBox)
            {
                //Get list box object.
                ListBox objListBox = (ListBox)control;
#if !PocketPC
                //Set the selection mode.
                GuiUtils.SetSelectionMode(objListBox, guiCommand.listboxSelectionMode == ListboxSelectionMode.Single ? SelectionMode.One : SelectionMode.MultiExtended);
#endif
            }
        }

        /// <summary>
        /// enable/disable XP themes
        /// </summary>
        /// <param name="guiCommand"></param>
        private void EnableXPThemes(GuiCommand guiCommand)
        {
#if !PocketPC
            if (guiCommand.Bool3) //must set EnableVisualStyles if they were not set before
                Application.EnableVisualStyles();
            Application.VisualStyleState = (guiCommand.Bool3
                ? System.Windows.Forms.VisualStyles.VisualStyleState.ClientAndNonClientAreasEnabled
                : System.Windows.Forms.VisualStyles.VisualStyleState.NonClientAreaEnabled);
#else
         throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// mark/unmark items in multimark
        /// </summary>
        /// <param name="guiCommand"></param>
        private void SetMarkedItemState(GuiCommand guiCommand)
        {
            int index = guiCommand.line;
            bool isMarked = guiCommand.Bool3;

            Debug.Assert(index >= 0 || index == GuiConstants.ALL_LINES);
            Control obj = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            if (obj != null)
            {
                ItemsManager itemsManager = GuiUtils.getItemsManager(obj);
                if (isMarked)
                    itemsManager.MarkRow(index);
                else if (index == GuiConstants.ALL_LINES)
                    itemsManager.UnMarkAll();
                else
                    itemsManager.UnMarkRow(index);
            }
        }

#if !PocketPC
        #region DataView Controls Command


        /// <summary>
        ///   Update DataRow of DataTable attached to DataView Control.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void OnUpdateDVControlRow(GuiCommand guiCommand)
        {
            DataTable dataTable = ((MGDataTable)guiCommand.obj).DataTblObj;
            object[] row = (object[])guiCommand.obj1;
            GuiMgControl DVControl = DVDataTableCollection.GetDVControlManager(dataTable).DVControl;
            DVDataTable dvDataTbl = DVDataTableCollection.GetDVControlManager(dataTable).DVDataTableObj;

            DataViewControlHandler.getInstance().RemoveHandler(DVControl, dataTable);

            // Check if primary key value already exists. If exists, update the same row.
            // if not found it means, new row is added.
            DataRow foundRow = dataTable.Rows.Find(row[0]);
            int currRow = dvDataTbl.CurrRow;
            if (foundRow == null)
            {
                currRow = guiCommand.line;
                foundRow = dataTable.Rows[guiCommand.line];
            }

            for (int i = 0; i < dataTable.Columns.Count; i++)
                foundRow[i] = row[i];

            if (dvDataTbl != null && currRow >= 0)
                dataTable.Rows[currRow].AcceptChanges();

            //Set position.
            Control ctrl = (Control)ControlsMap.getInstance().object2Widget(DVControl);
            BindingManagerBase dm = ctrl.BindingContext[dataTable];
            dm.Position = guiCommand.line;

            DataViewControlHandler.getInstance().AddHandler(DVControl, dataTable);
        }

        /// <summary>
        ///   Create row in DataTable attached to DataView Control.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void OnCreateRowInDVControl(GuiCommand guiCommand)
        {
            DataTable dataTable = ((MGDataTable)guiCommand.obj).DataTblObj;
            int rowIdx = guiCommand.line;
            bool check = guiCommand.Bool3;

            if (!check || (rowIdx >= dataTable.Rows.Count))
            {
                DataRow newRow = dataTable.NewRow();
                newRow["Isn"] = 0;
                dataTable.Rows.InsertAt(newRow, rowIdx);
            }
        }

        /// <summary>
        ///   Delete row from DataTable attached to DataView Control.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void OnDeleteDVControlRow(GuiCommand guiCommand)
        {
            DataTable dataTable = ((MGDataTable)guiCommand.obj).DataTblObj;
            int rowIdx = guiCommand.line;

            GuiMgControl DVControl = DVDataTableCollection.GetDVControlManager(dataTable).DVControl;

            dataTable.Rows.RemoveAt(rowIdx);

            //Set position if needed
            Control ctrl = (Control)ControlsMap.getInstance().object2Widget(DVControl);
            BindingManagerBase dm = ctrl.BindingContext[dataTable];
            if (dm.Position == dataTable.Rows.Count)
                dm.Position = dm.Position - 1;
        }

        /// <summary>
        ///   Update Column of DataTable.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void OnUpdateDVControlColumn(GuiCommand guiCommand)
        {
            DataTable dataTable = ((MGDataTable)guiCommand.obj).DataTblObj;

            int rowIdx = guiCommand.line;
            int colIdx = guiCommand.number;
            object colVal = guiCommand.obj1;

            GuiMgControl DVControl = DVDataTableCollection.GetDVControlManager(dataTable).DVControl;

            DataViewControlHandler.getInstance().RemoveHandler(DVControl, dataTable);

            dataTable.Rows[rowIdx][colIdx] = colVal;

            DataViewControlHandler.getInstance().AddHandler(DVControl, dataTable);
        }

        /// <summary>
        ///   Add DataView Control handlers.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void OnAddDVControlHandler(GuiCommand guiCommand)
        {
            GuiMgControl DVControl = (GuiMgControl)guiCommand.obj;
            DataTable dataTable = (DataTable)guiCommand.obj1;

            DataViewControlHandler.getInstance().AddHandler(DVControl, dataTable);
        }

        /// <summary>
        ///   Remove DataView Control handlers.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void OnRemoveDVControlHandler(GuiCommand guiCommand)
        {
            GuiMgControl DVControl = (GuiMgControl)guiCommand.obj;
            DataTable dataTable = (DataTable)guiCommand.obj1;

            DataViewControlHandler.getInstance().RemoveHandler(DVControl, dataTable);
        }

        /// <summary>
        ///   Set Current Row Position of DataTable attached to DataView control.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void OnSetDVControlRowPosition(GuiCommand guiCommand)
        {
            GuiMgControl DVControl = (GuiMgControl)guiCommand.obj;

            Control ctrl = (Control)ControlsMap.getInstance().object2Widget(DVControl);

            DataTable dataTable = (DataTable)guiCommand.obj1;

            BindingManagerBase dm = ctrl.BindingContext[dataTable];

            dm.Position = guiCommand.line;
        }

        /// </summary>
        ///   Reject column changes of DataTable attached to DataView control.
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void OnRejectDVControlColumnChanges(GuiCommand guiCommand)
        {
            GuiMgControl DVControl = (GuiMgControl)guiCommand.obj;

            Control ctrl = (Control)ControlsMap.getInstance().object2Widget(DVControl);

            DataTable dataTable = (DataTable)guiCommand.obj1;

            int rowId = guiCommand.line;
            int colId = guiCommand.number;

            DataViewControlHandler.getInstance().RemoveHandler(DVControl, dataTable);

            BindingManagerBase dm = ctrl.BindingContext[dataTable];

            dm.Position = guiCommand.line;

            //dataTable.RejectChanges();
            dataTable.Rows[rowId].RejectChanges();

            Object val = dataTable.Rows[rowId][colId];

            DataViewControlHandler.getInstance().AddHandler(DVControl, dataTable);

            DNObjectsCollection dnObjectsCollection = DNManager.getInstance().DNObjectsCollection;
            int dnKey = dnObjectsCollection.CreateEntry(val.GetType());
            dnObjectsCollection.Update(dnKey, val);

            Events.OnDVControlColumnValueChangedEvent(dataTable, colId, dnKey);
        }

        #endregion
#endif

        /// <summary>
        ///   Refresh menu actions of a specific form
        /// </summary>
        /// <param name = "guiCommand"></param>
        internal void RefreshMenuActions(GuiCommand guiCommand)
        {
            Events.OnRefreshMenuActions(guiCommand.menu, (GuiMgForm)guiCommand.parentObject);
        }

        /// <summary> Applies the placement to the specified form. </summary>
        /// <param name="guiCommand"></param>
        private void ApplyChildWindowPlacement(GuiCommand guiCommand)
        {
            Control control = (Control)ControlsMap.getInstance().object2Widget(guiCommand.obj);
            Form form = GuiUtils.getForm(control);
            Debug.Assert(form != null);

            // Create dummy object of EditorSupportingPlacementLayout to call ControlPlacement.
            EditorSupportingPlacementLayout editorSupportingPlacementLayout = new EditorSupportingPlacementLayout(control, Rectangle.Empty, false, null, null);
            editorSupportingPlacementLayout.ControlPlacement(form, new Point(guiCommand.width, guiCommand.height));
        }

        /// <summary> Set row placement for the table </summary>
        /// <param name="guiCommand"></param>
        private void SetRowPlacement(GuiCommand guiCommand)
        {
            TableControl tableControl = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            tableControl.HasRowPlacement = guiCommand.Bool3;
        }

        /// <summary> Set table's original row height </summary>
        /// <param name="guiCommand"></param>
        private void SetTableOrgRowHeight(GuiCommand guiCommand)
        {
            TableControl tableControl = (TableControl)ControlsMap.getInstance().object2Widget(guiCommand.obj, 0);
            tableControl.OriginalRowHeight = guiCommand.number;
        }

        /// <summary>
        /// Create a new entry in ControlsMap using existing Widget.
        /// </summary>
        private void CreateControlsMapEntry(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj1);
            ControlsMap.getInstance().add(guiCommand.obj, obj);
        }

        /// <summary>
        /// Removes an entry from a ControlsMap
        /// </summary>
        private void RemoveControlsMapEntry(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj); // TODO : Remove this call, this is just for testing purpose.
            ControlsMap.getInstance().remove(guiCommand.obj);
        }

        // Todo: Implementation for opening context menu will be added 
        // by mobile clients supporting press event
        internal void ProcessPressEvent(GuiCommand guiCommand)
        {
        }

        /// <summary>
        /// Set FillTable on TableManager
        /// </summary>
        /// <param name="guiCommand"></param>
        private void SetFillWidth(GuiCommand guiCommand)
        {
            TableControl table = ControlsMap.getInstance().object2Widget(guiCommand.obj, 0) as TableControl;

            if (table != null)
                GuiUtils.getTableManager(table).FillWidth = guiCommand.Bool3;
        }

        /// <summary>
        /// Set Multi Display Column property on Table Manager
        /// </summary>
        /// <param name="guiCommand"></param>
        private void SetMultiColumnDisplay(GuiCommand guiCommand)
        {
            TableControl table = ControlsMap.getInstance().object2Widget(guiCommand.obj, 0) as TableControl;

            if (table != null)
                GuiUtils.getTableManager(table).SetMultiColumnDisplay(guiCommand.Bool3);
        }

        /// <summary>
        /// Set Multi Display Column property on Table Manager
        /// </summary>
        /// <param name="guiCommand"></param>
        private void SetShowEllipsis(GuiCommand guiCommand)
        {
            TableControl table = ControlsMap.getInstance().object2Widget(guiCommand.obj, 0) as TableControl;

            if (table != null)
                GuiUtils.getTableManager(table).ShowEllipsis(guiCommand.Bool3);
        }

        /// <summary>
        /// Set TitlePadding on Tab Control
        /// </summary>
        /// <param name="guiCommand"></param>
        private void SetTitlePadding(GuiCommand guiCommand)
        {
            MgTabControl mgTabControl = ControlsMap.getInstance().object2Widget(guiCommand.obj, 0) as MgTabControl;

            if (mgTabControl != null)
                ControlUtils.SetTitlePadding(mgTabControl, guiCommand.number);
        }

        /// <summary>
        /// Set ShouldApplyPlacementToHiddenColumns on TablePlacementManager.
        /// </summary>
        /// <param name="guiCommand"></param>
        private void OnSetShouldApplyPlacementToHiddenColumns(GuiCommand guiCommand)
        {
            TableControl table = ControlsMap.getInstance().object2Widget(guiCommand.obj, 0) as TableControl;

            if (table != null)
                GuiUtils.getTableManager(table).TablePlacementManager.ShouldApplyPlacementToHiddenColumns = false;
        }

#if !PocketPC
        /// <summary>
        /// set runtime designer values on all controls
        /// </summary>
        /// <param name="guiCommand"></param>
        private void OnSetDesignerValues(GuiCommand guiCommand)
        {
            Dictionary<MgControlBase, Dictionary<string, object>> dict = (Dictionary<MgControlBase, Dictionary<string, object>>)guiCommand.obj;
            DesignerValuesSetters.SetDesignerValues(dict);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="guiCommand"></param>
        private void OnRecalculateTableColors(GuiCommand guiCommand)
        {
            TableControl table = ControlsMap.getInstance().object2Widget(guiCommand.obj, 0) as TableControl;

            if (table != null)
                GuiUtils.getTableManager(table).RecalculateColors();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="guiCommand"></param>
        private void OnRecalculateTableFonts(GuiCommand guiCommand)
        {
            TableControl table = ControlsMap.getInstance().object2Widget(guiCommand.obj, 0) as TableControl;

            if (table != null)
                GuiUtils.getTableManager(table).RecalculateFonts();
        }
#endif
        /// <summary>
        /// 
        /// </summary>
        /// <param name="guiCommand"></param>
        private void OnSetRowBGColor(GuiCommand guiCommand)
        {
            TableControl table = ControlsMap.getInstance().object2Widget(guiCommand.obj, 0) as TableControl;

            if (table != null)
                GuiUtils.getTableManager(table).SetRowBGColor(guiCommand.line, guiCommand.mgColor);
        }

        /// <summary>
        /// Hide or show caret
        /// </summary>
        /// <param name="guiCommand"></param>
        /// <returns></returns>
        private void SetCaret(GuiCommand guiCommand)
        {
            Object obj = ControlsMap.getInstance().object2Widget(guiCommand.obj);
            GuiForm form = (GuiForm)GuiUtils.getForm(obj);
            Debug.Assert(form != null);
            TagData tagData = (TagData)form.Tag;

            if (guiCommand.Bool3)
            {
                if (!tagData.HasCaret)
                    NativeWindowCommon.ShowCaret(IntPtr.Zero);
            }
            else
                NativeWindowCommon.HideCaret(IntPtr.Zero);

            tagData.HasCaret = guiCommand.Bool3;
        }

        #endregion
    }
}
