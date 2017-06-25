using com.magicsoftware.unipaas.gui.low;
using System;
using System.Collections;
using System.Diagnostics;

namespace com.magicsoftware.unipaas.gui
{
   public class GuiMenuEntry
   {
      private int _menuUid; // menu uid

      protected class MenuState
      {
         public bool Checked { get; set; }

         public bool Visible { get; set; }

         public bool Enabled { get; set; }

         public bool ModalDisabled { get; set; }

         public MenuState() { }

         public MenuState(MenuState menuState)
         {
            Checked = menuState.Checked;
            Visible = menuState.Visible;
            Enabled = menuState.Enabled;
            ModalDisabled = menuState.ModalDisabled;
         }
      }

      public enum MenuType
      {
         MENU,
         PROGRAM,
         OSCOMMAND,
         SEPARATOR,
         SYSTEM_EVENT,
         INTERNAL_EVENT,
         USER_EVENT,
         WINDOW_MENU_ENTRY
      }

      public enum ImageFor
      {
         MENU_IMAGE_TOOLBAR,
         MENU_IMAGE_MENU,
         MENU_IMAGE_BOTH
      }

      private MenuType _menuType;

      // type of the menus
      public GuiMenuEntry ParentMenuEntry { get; set; }

      // immediate parent of the menu entry
      public KeyboardItem AccessKey { get; set; } // access key which activates the menu

      protected Hashtable _instantiatedPullDown;
      protected Hashtable _instantiatedContext;
      protected Hashtable _instantiatedToolItem;
      protected MenuState _menuState; // menu state
      protected String _text; // menu text - displayed
      public String TextMLS { get; set; }
      public ImageFor Imagefor { get; set; } // image and tool data
      public String ImageFile { get; set; } // image and tool data
      public int ImageNumber { get; set; } // full path of image file name
      public int ImageGroup { get; set; } // location of the menu in the toolbar
      internal String ToolTip { get; private set; }
      internal String ToolTipMLS { get; private set; }
      public int Help { get; set; } // used help

      /// <summary>
      /// 
      /// </summary>
      public GuiMenuEntry()
      {
         _menuState = new MenuState();
         init();
      }

      protected void init()
      {
         ParentMenuEntry = null;
         AccessKey = null;

         _instantiatedPullDown = new Hashtable();
         _instantiatedContext = new Hashtable();
         _instantiatedToolItem = new Hashtable();
      }

      /// <summary>
      ///   get Deep Copy of MenuState.
      /// </summary>
      /// <returns></returns>
      public void getDeepCopyOfMenuState()
      {
         MenuState menuState = new MenuState(_menuState);
         _menuState = menuState;
      }

      public int menuUid()
      {
         return _menuUid;
      }

      public void setUid(int uid)
      {
         _menuUid = uid;
      }

      public void setType(MenuType type)
      {
         _menuType = type;
      }

      public MenuType menuType()
      {
         return _menuType;
      }

      /// <summary> This method updates a menu as instantiated for a specific form and style. It returns a reference to a
      /// menu object - to be used in order to retrieve this menu object, if it is needed. The returned object
      /// should be placed in the controlsMap, with the created menu for future use
      /// </summary>
      /// <param name="form">the form for which the menus is instatiated</param>
      /// <param name="menuStyle">the menu style (pulldown, context)</param>
      /// <returns> menu reference object</returns>
      protected MenuReference setMenuIsInstantiated(GuiMgForm form, MenuStyle menuStyle)
      {
         MenuReference menuReference = new MenuReference(form);
         if (menuStyle == MenuStyle.MENU_STYLE_PULLDOWN)
            _instantiatedPullDown[form] = menuReference;
         else if (menuStyle == MenuStyle.MENU_STYLE_CONTEXT)
            _instantiatedContext[form] = menuReference;
         else if (menuStyle == MenuStyle.MENU_STYLE_TOOLBAR)
            _instantiatedToolItem[form] = menuReference;
         else
            Debug.Assert(false);
         return menuReference;
      }

      /// <summary> This method returns a menu object reference for a specific form and menu style (pulldown, context). The
      /// returned reference should be used in order to retrieve the specific instantiated menu object from the
      /// controls map.
      /// </summary>
      /// <param name="form">the form for which the menus is instatiated</param>
      /// <param name="menuStyle">the menu style (pulldown, context)</param>
      /// <returns> a menu object reference. In case the menu was not yet instantiated for the specfic form and
      /// style, null is returned.</returns>
      public MenuReference getInstantiatedMenu(GuiMgForm form, MenuStyle menuStyle)
      {
         MenuReference menuReference = null;
         if (menuStyle == MenuStyle.MENU_STYLE_PULLDOWN)
            menuReference = (MenuReference)_instantiatedPullDown[form];
         else if (menuStyle == MenuStyle.MENU_STYLE_CONTEXT)
            menuReference = (MenuReference)_instantiatedContext[form];
         else if (menuStyle == MenuStyle.MENU_STYLE_TOOLBAR)
            menuReference = (MenuReference)_instantiatedToolItem[form];
         else
            Debug.Assert(false);
         return menuReference;
      }

      internal ICollection getInstantiatedMenus(MenuStyle menuStyle)
      {
         ICollection list = null;
         if (menuStyle == MenuStyle.MENU_STYLE_PULLDOWN)
            list = _instantiatedPullDown.Values;
         else if (menuStyle == MenuStyle.MENU_STYLE_CONTEXT)
            list = _instantiatedContext.Values;
         else if (menuStyle == MenuStyle.MENU_STYLE_TOOLBAR)
            list = _instantiatedToolItem.Values;
         return list;
      }

      /// <summary></summary>
      /// <param name="addToolBar"></param>
      /// <returns>Returns all getInstantiatedMenus menus</returns>
      public ICollection getInstantiatedMenus(bool addToolBar)
      {
         return (getInstantiatedMenus(null, addToolBar, true, true));
      }

      /// <summary> Returns all getInstantiatedMenus menus for one form </summary>
      /// <returns> ArrayList<MenuReference> containing all Instantiated Menus
      /// </returns>
      public ICollection getInstantiatedMenus(GuiMgForm form, bool addToolBar, bool addPullDown, bool addContext)
      {
         ArrayList list = new ArrayList(); // Can't use List<T> - may include MenuReference or ICollection
         if (form == null)
         {
            ICollection listPullDown = null;
            ICollection listContext = null;
            ICollection listToolbar = null;

            if (addPullDown)
            {
               listPullDown = getInstantiatedMenus(MenuStyle.MENU_STYLE_PULLDOWN);
               if (listPullDown != null)
                  list.AddRange(listPullDown);
            }

            if (addContext)
            {
               listContext = getInstantiatedMenus(MenuStyle.MENU_STYLE_CONTEXT);
               if (listContext != null)
                  list.AddRange(listContext);
            }

            if (addToolBar)
            {
               listToolbar = getInstantiatedMenus(MenuStyle.MENU_STYLE_TOOLBAR);
               if (listToolbar != null)
                  list.AddRange(listToolbar);
            }
         }
         else
         {
            MenuReference listPullDown = null;
            MenuReference listContext = null;
            MenuReference listToolbar = null;

            if (addPullDown)
            {
               listPullDown = getInstantiatedMenu(form, MenuStyle.MENU_STYLE_PULLDOWN);
               if (listPullDown != null)
                  list.Add(listPullDown);
            }

            if (addContext)
            {
               listContext = getInstantiatedMenu(form, MenuStyle.MENU_STYLE_CONTEXT);
               if (listContext != null)
                  list.Add(listContext);
            }

            if (addToolBar)
            {
               listToolbar = getInstantiatedMenu(form, MenuStyle.MENU_STYLE_TOOLBAR);
               if (listToolbar != null)
                  list.Add(listToolbar);
            }
         }
         return list;
      }

      /// <summary> Remove the menu reference for the menu entry</summary>
      /// <param name="menuReference"></param>
      /// <param name="style"></param>
      internal void removeMenuIsInstantiated(GuiMgForm guiMgForm, MenuStyle style)
      {
         if (style == MenuStyle.MENU_STYLE_PULLDOWN)
            _instantiatedPullDown.Remove(guiMgForm);
         else if (style == MenuStyle.MENU_STYLE_CONTEXT)
            _instantiatedContext.Remove(guiMgForm);
         else if (style == MenuStyle.MENU_STYLE_TOOLBAR)
            _instantiatedToolItem.Remove(guiMgForm);
      }

      /// <summary> This method updates a toolbar as instantiated for a specific form and style. It returns a reference to a
      /// menu object - to be used in order to retrieve this menu object, if it is needed. The returned object
      /// should be placed in the controlsMap, with the created menu for future use
      /// </summary>
      /// <param name="form">the form for which the menus is instatiated</param>
      /// <param name="menuStyle">the menu style (pulldown, context)</param>
      /// <returns> menu reference object</returns>
      public MenuReference setToolItemInstantiated(GuiMgForm form)
      {
         MenuReference menuReference = new MenuReference(form);
         _instantiatedToolItem[form] = menuReference;
         return menuReference;
      }

      /// <summary> This method returns a menu object reference for a specific form and menu style (pulldown, context). The
      /// returned reference should be used in order to retrieve the specific instantiated menu object from the
      /// controls map.
      /// </summary>
      /// <param name="form">the form for which the menus is instatiated</param>
      /// <param name="menuStyle">the menu style (pulldown, context)</param>
      /// <returns> a menu object reference. In case the menu was not yet instantiated for the specfic form and
      /// style, null is returned.</returns>
      public MenuReference getInstantiatedToolItem(GuiMgForm form)
      {
         return (MenuReference)_instantiatedToolItem[form];
      }

      public ArrayList getInstantiatedToolItems()
      {
         return (ArrayList)_instantiatedToolItem.Values;
      }

      public bool getEnabled()
      {
         return _menuState.Enabled;
      }

      public bool getChecked()
      {
         return _menuState.Checked;
      }

      public bool getVisible()
      {
         return _menuState.Visible;
      }

      public bool getModalDisabled()
      {
         return _menuState.ModalDisabled;
      }

      public virtual void setModalDisabled(bool val)
      {
         _menuState.ModalDisabled = val;
      }

      public void toolTip(String pToolTip)
      {
         ToolTip = pToolTip;
         ToolTipMLS = Events.Translate(ToolTip);
      }

      /// <summary>Check if all ancestors menus entries are visible. If not, return false</summary>
      /// <param name="menuEntry">menu entry.</param>
      public bool CheckIfParentItemVisible(GuiMenuEntry menuEntry)
      {
         bool visible = true;
         if (menuEntry.ParentMenuEntry != null)
         {
            if (!menuEntry.ParentMenuEntry.getVisible())
               return false;
            else
               CheckIfParentItemVisible (menuEntry.ParentMenuEntry);
         }
         return visible;
      }
   }
}
