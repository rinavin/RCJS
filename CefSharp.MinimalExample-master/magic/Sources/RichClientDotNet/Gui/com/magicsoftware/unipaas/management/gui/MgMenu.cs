using System;
using System.Collections;
using System.Collections.Generic;
using Console = System.Console;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary>
   ///   While adding a new member in this class, please make sure that you want to copy the value of that member
   ///   in new object or not inside clone method.
   /// </summary>
   public class MgMenu : GuiMgMenu, ICloneable
   {
      private Hashtable _instantiatedToolbar; // for finding the Toolbar of the form
      private Hashtable _internalEventsOnMenus; // list of internal event menu entries which appear on the menu bar
      private Hashtable _internalEventsOnToolBar; // list of internal event menu entries which appear on the tool bar
      private List<MenuEntry> _menuEntries;
      private bool IsModalDisabled { get; set; }
      public List<MenuEntry> MenuEntries
      {
          get
          {
              return _menuEntries;
          }
      }

      //   list of menu entries with access key, not including the internal events
      //   created form performance reasons - not to search all the menu entry form each activated keybord item.
      private Hashtable _menuEntriesWithAccessKey;

      // Performance helpers:
      // HashMap of menu names – for quick search of a menu with a specific name.
      private Hashtable _menuNames; // <String menuName, MenuEntry menuEntry>
      private int _menuUid; // menu uid
      private String _name;
      private String _text;

      /// <summary>
      /// 
      /// </summary>
      public MgMenu()
      {
         _menuEntries = new List<MenuEntry>();
         _instantiatedToolbar = new Hashtable();
         _internalEventsOnMenus = new Hashtable();
         _internalEventsOnToolBar = new Hashtable();
         _menuEntriesWithAccessKey = new Hashtable();

         _menuNames = new Hashtable();
         CtlIdx = 0;
      }

      public int CtlIdx { get; set; }

      #region ICloneable Members

      /// <summary>
      ///   returns the clone of the object.
      /// </summary>
      /// <returns></returns>
      public Object Clone()
      {
         MgMenu mgmenu = (MgMenu)MemberwiseClone();

         //MemeberwiseClone copies the refrences of arraylist but we need new objects so for deep copy of 
         //all elements in menuEntries, we need to copy each of it's element seperately.
         mgmenu._menuEntries = getDeepCopyOfMenuEntries(_menuEntries);

         base.init();
         //Following members(references) should not be copied in new cloned object because for creation of new menu 
         //in menuAdd function, we need these values diffrent than the actual menu.
         _instantiatedToolbar = new Hashtable();
         _internalEventsOnMenus = new Hashtable();
         _internalEventsOnToolBar = new Hashtable();
         _menuEntriesWithAccessKey = new Hashtable();

         return mgmenu;
      }

      #endregion

      /// <summary>
      ///   This function does deep copy of all elements in menuEntries ArrayList.
      /// </summary>
      /// <returns> a new object of Arraylist which contains refrences of new menuEntries objects</returns>
      private List<MenuEntry> getDeepCopyOfMenuEntries(List<MenuEntry> menuEntries)
      {
         List<MenuEntry> clonedMenuEntries = new List<MenuEntry>(menuEntries.Count);
         int i = 0;

         foreach (MenuEntry menuentry in menuEntries)
         {
            clonedMenuEntries.Add((MenuEntry)menuentry.Clone());

            if (menuentry is MenuEntryMenu)
            {
               MenuEntryMenu menu = (MenuEntryMenu)menuentry;
               MenuEntryMenu clonedMenu = (MenuEntryMenu)(clonedMenuEntries[i]);
               clonedMenu.subMenus = getDeepCopyOfMenuEntries(menu.subMenus);
            }
            i++;
         }

         return clonedMenuEntries;
      }

      public void setName(String newName)
      {
         _name = newName;
      }

      public String getName()
      {
         return _name;
      }

      public void addSubMenu(MenuEntry newEntry)
      {
         _menuEntries.Add(newEntry);
      }

      /// <summary>
      ///   Inserts a menu at given position
      /// </summary>
      /// <param name = "newEntry">menu to be added</param>
      /// <param name = "idx">position where menu needs to be inserted</param>
      public void addMenu(MenuEntry newEntry, int idx)
      {
         _menuEntries.Insert(idx, newEntry);
      }

      /// <summary>
      ///   Deletes a menu at given position
      /// </summary>
      /// <param name = "idx">position from where menu to be deleted</param>
      public void removeAt(int idx, MgFormBase form)
      {
         _menuEntries[idx].deleteMenuEntryObject(form, MenuStyle.MENU_STYLE_PULLDOWN);
         _menuEntries.RemoveAt(idx);
         Commands.invoke();
      }

      /// <summary>
      ///   Deletes a menu
      /// </summary>
      public void removeAll(MgFormBase form)
      {
         for (int idx = 0; idx < _menuEntries.Count; idx++)
            _menuEntries[idx].deleteMenuEntryObject(form, MenuStyle.MENU_STYLE_PULLDOWN);

         _menuEntries.Clear();
         Commands.invoke();
      }

      public void setUid(int uid)
      {
         _menuUid = uid;
      }

      /// <summary>
      ///   createMenu This method creates the gui commands in order to create the matching menu object. First we
      ///   create a GUI command which verifies the object will have a menu definition (either he already has one or
      ///   we will create it). Then it calls the CreateSubMenuObject in order to create the actual menu.
      /// </summary>
      /// <param name = "form"></param>
      /// <param name = "menuStyle"></param>
      public void createMenu(MgFormBase form, MenuStyle menuStyle)
      {
         int i;
         MgFormBase actualForm = form;
         if (form.isSubForm())
         {
            MgControlBase subformCtrl = form.getSubFormCtrl();
            actualForm = subformCtrl.getForm().getTopMostForm();
         }
         // #991912. We will create menus anyway even though showMenu on the foem is false. so that when they are
         // later accessed with Access key, we can find the menus. The actual SWT menus will be created only
         // if Showmenu (MGFrom.shouldShowPullDownMenu()) is TRUE.
         setMenuIsInstantiated(actualForm, menuStyle);
         Commands.addAsync(CommandType.CREATE_MENU, null, actualForm, menuStyle, this, true,
                           actualForm.ShouldShowPullDownMenu);
         actualForm.addMgMenuToList(this, menuStyle);

         //We are initializing window list menu items for context menu
         if (menuStyle == MenuStyle.MENU_STYLE_CONTEXT)
         {
             for (i = 0; i < _menuEntries.Count; i++)
             {
                 if (((GuiMenuEntry)_menuEntries[i]).menuType() == GuiMenuEntry.MenuType.WINDOW_MENU_ENTRY)
                 {
                     Events.InitWindowListMenuItems(actualForm, _menuEntries[i], menuStyle);
                     break;//We should initialize the Window List only once
                 }
             }
         }

         for (i = 0; i < _menuEntries.Count; i++)
             _menuEntries[i].createMenuEntryObject(this, menuStyle, actualForm, false);

         refreshMenuAction(form, menuStyle);
         if (menuStyle == MenuStyle.MENU_STYLE_CONTEXT)
            Commands.invoke();
      }

      /// <summary>
      ///   refresh the menu actions
      /// </summary>
      /// <param name = "form"></param>
      /// <param name = "menuStyle"></param>
      public void refreshMenuAction(MgFormBase form, MenuStyle menuStyle)
      {
         Commands.addAsync(CommandType.REFRESH_MENU_ACTIONS, form, null, menuStyle, this, true);
      }

      public void setText(String val)
      {
         _text = val;
      }

      /// <summary>
      ///   This method gives an indication wether a menu was instantiated for a specific form and style.
      /// </summary>
      /// <param name = "form"></param>
      /// <param name = "menuStyle"></param>
      /// <returns> true if the menu already exists, false otherwise.</returns>
      public bool menuIsInstantiated(MgFormBase form, MenuStyle menuStyle)
      {
         return (getInstantiatedMenu(form, menuStyle) != null);
      }

      /// <summary>
      ///   This method updates a menu as instantiated for a specific form and style. It returns a reference to a
      ///   menu object - to be used in order to retrieve this menu object, if it is needed. The returned object
      ///   should be placed in the controlsMap, with the created menu for future use
      /// </summary>
      /// <param name = "form">the form for which the menus is instatiated</param>
      /// <returns> menu reference object</returns>
      public MenuReference setToolBarIsInstantiated(MgFormBase form)
      {
         // toolbar will not save a ref to a form. since we do not handle the toolbar dispose,
         // then there will not be a way to remove the ref of the form and we might have a dengling ref.
         // in any case, there is no use for a form on a menuref for the toolbar itself.
         MenuReference menuReference = new MenuReference(null);
         _instantiatedToolbar[form] = menuReference;
         return menuReference;
      }

      /// <summary>
      ///   This method returns the toolbar object. In case the object is NULL, we allocate it and call the
      ///   createToolbar method in order to instantiate it.
      /// </summary>
      /// <returns>toolbar object</returns>
      public MenuReference createAndGetToolbar(MgFormBase form)
      {
          createToolbar(form);
          return (MenuReference)(_instantiatedToolbar[form]);
      }

      /// <summary>
      /// Get the toolbar. If the toolbar is not already created then we will return null.
      /// </summary>
      /// <param name="form"></param>
      /// <returns>toolbar object</returns>
      public MenuReference getToolbar(MgFormBase form)
      {
          return (MenuReference)_instantiatedToolbar[form];
      }

      /// <summary>
      ///   This method creates the toolbar object. It is done only if it was not instantiated already.
      /// </summary>
      private void createToolbar(MgFormBase form)
      {
         MenuReference newToolbar;
         if (!_instantiatedToolbar.ContainsKey(form))
         {
            if (!form.isSubForm())
            {
               // create a new entry in the toolbar hash map
               newToolbar = setToolBarIsInstantiated(form);
               Console.Out.WriteLine("MgMenu toolbar(" + GetHashCode() + ") menuReference (" + newToolbar + ") ");
               // create the matching gui command
               // form is the parentObject
               // toolbar is the object
               // create toolbar only if form is not a sub-form.
               Commands.addAsync(CommandType.CREATE_TOOLBAR, form, newToolbar);
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="mgForm"></param>
      /// <returns></returns>
      public void deleteToolBar(MgFormBase mgForm)
      {
         MenuReference toolbar = (MenuReference)_instantiatedToolbar[mgForm];

         if (toolbar != null)
         {
            // create the matching gui command
            // form is the parentObject
            // toolbar is the object
            Commands.addAsync(CommandType.DELETE_TOOLBAR, mgForm, toolbar);
            removeInstantiatedToolbar(mgForm);
         }
      }

      /// <summary> This method removes an instantiated toolibar for the form</summary>
      /// <param name="form">the form for which the menus is instantiated</param>
      public void removeInstantiatedToolbar(MgFormBase mgForm)
      {
         _instantiatedToolbar.Remove(mgForm);
      }

      /// <summary>
      ///   This method adds a tool to the passed group and returns the new tool’s index.
      /// </summary>
      /// <param name = "toolGroup">group to which we add the tool</param>
      /// <returns> - new tool index</returns>
      public bool checkStartSepForGroup(MgFormBase form, int toolGroup, GuiMenuEntry.MenuType menuType)
      {
         bool createSepAtTheEnd = false;
         if (menuType != GuiMenuEntry.MenuType.SEPARATOR)
            createSepAtTheEnd = form.createToolGroup(this, toolGroup);

         return createSepAtTheEnd;
      }

      public void checkEndtSepForGroup(MgFormBase form, int toolGroup, GuiMenuEntry.MenuType menuType)
      {
         if (menuType != GuiMenuEntry.MenuType.SEPARATOR)
            form.createSepOnGroup(this, toolGroup);
      }


      /// <summary>
      ///   This method adds a tool to the passed group and returns the new tool’s index.
      /// </summary>
      /// <param name = "toolGroup">group to which we add the tool</param>
      /// <returns>new tool index</returns>
      public int addToolToGroup(MgFormBase form, int toolGroup, GuiMenuEntry.MenuType menuType)
      {
         int i;
         int newToolIndex = 0;
         int count = 0;

         for (i = 0; i <= toolGroup; i++)
         {
            count = form.getToolbarGroupCount(i);
            newToolIndex += count;
         }

         //if we added a new tool and we already have a tool items we need to add the new tool before the seperator
         if (menuType != GuiMenuEntry.MenuType.SEPARATOR &&
             form.getToolbarGroupMenuEntrySep(toolGroup) != null)
            newToolIndex--;

         // each group ends with a separator, so we will increase the counter only after
         // we got the index
         form.setToolbarGroupCount(toolGroup, count + 1);

         return newToolIndex;
      }

      /// <summary>
      ///   This method adds the passed menu entry to the list of internal event menus which 
      ///   appear on the Menu.
      /// </summary>
      /// <param name = "menuEntry">a menu Entry event object to be added to the list</param>
      public void addEntryToInternalEventsOnMenus(MenuEntryEvent menuEntry)
      {
         List<MenuEntryEvent> entries = null;
         if (menuEntry.InternalEvent > 0)
         {
            entries = (List<MenuEntryEvent>)_internalEventsOnMenus[menuEntry.InternalEvent];
            if (entries == null)
               entries = new List<MenuEntryEvent>();
            entries.Add(menuEntry);
            _internalEventsOnMenus[menuEntry.InternalEvent] = entries;
         }
      }

      /// <summary>
      ///   This method adds the passed menu entry to the list of menu entries with access key
      ///   the key will be : access key & modifier
      /// </summary>
      /// <param name = "menuEntry">a menu Entry object to be added to the list</param>
      public void addEntryToMenuEntriesWithAccessKey(MenuEntry menuEntry)
      {
         List<MenuEntry> entries = null;

         //MENU_TYPE_INTERNAL_EVENT will handel from the keybord mapping
         if (menuEntry.AccessKey != null && menuEntry.menuType() != GuiMenuEntry.MenuType.INTERNAL_EVENT)
         {
            String key = "" + menuEntry.AccessKey.getKeyCode() + menuEntry.AccessKey.getModifier();
            entries = (List<MenuEntry>)_menuEntriesWithAccessKey[key];
            if (entries == null)
               entries = new List<MenuEntry>();
            entries.Add(menuEntry);
            _menuEntriesWithAccessKey[key] = entries;
         }
      }


      /// <summary>
      ///   This method returns the list of menu entries which appear on the Menu Bar
      /// </summary>
      public List<MenuEntryEvent> getInternalEventsEntriesOnMenu(int internalEvent)
      {
         return (List<MenuEntryEvent>)_internalEventsOnMenus[internalEvent];
      }

      /// <summary>
      ///   This method returns the list of menu entries with access key
      /// </summary>
      public List<MenuEntry> getMenuEntriesWithAccessKey(KeyboardItem kbdItem)
      {
         String key = "" + kbdItem.getKeyCode() + kbdItem.getModifier();
         return (List<MenuEntry>)_menuEntriesWithAccessKey[key];
      }

      /// <summary>
      ///   This method adds the passed menu entry to the list of entries which appear on the Tool Bar, if it does 
      ///   not exist in the array already
      /// </summary>
      /// <param name = "menuEntry">a menu Entry object to be added to the list</param>
      public void addEntryToInternalEventsOnToolBar(MenuEntryEvent menuEntry)
      {
         List<MenuEntryEvent> entries = null;
         if (menuEntry.InternalEvent > 0)
         {
            entries = (List<MenuEntryEvent>)_internalEventsOnToolBar[menuEntry.InternalEvent];
            if (entries == null)
               entries = new List<MenuEntryEvent>();
            entries.Add(menuEntry);
            _internalEventsOnToolBar[menuEntry.InternalEvent] = entries;
         }
      }

      /// <summary>
      ///   This method returns the list of menu entries which appear on the Tool Bar
      /// </summary>
      public List<MenuEntryEvent> getInternalEventsEntriesOnToolBar(int internalEvent)
      {
         return (List<MenuEntryEvent>)_internalEventsOnToolBar[internalEvent];
      }

      public IEnumerator iterator()
      {
         return _menuEntries.GetEnumerator();
      }

      /// <summary>
      ///   If a menu has a context menu instantiation for the given form :
      ///   Add a command for the gui to dispose the context menu. This will also trigger the disposing of all its items.
      /// </summary>
      /// <param name = "form"></param>
      public void disposeFormContexts(MgFormBase form)
      {
         MenuReference menuReference;

         menuReference = getInstantiatedMenu(form, MenuStyle.MENU_STYLE_CONTEXT);

         if (menuReference != null)
         {
            Commands.addAsync(CommandType.DISPOSE_OBJECT, menuReference);
         }
      }

      /// <summary>
      ///   This method destroys all menu objects of the previous menus definitions file, and
      ///   creates a list of all effected forms. When we finished cleaning the usage of the 
      ///   previous menus file, we will use these list in order to assign the updated menus
      ///   to the effected objects (forms and controls).
      /// </summary>
      public List<MgFormBase> destroy()
      {
         List<MgFormBase> formsToRefresh = new List<MgFormBase>();
         MgFormBase form = null;

         if (instantiatedContext.Count > 0 || instantiatedPullDown.Count > 0)
         {
            for (int i = 0; i < _menuEntries.Count; i++)
            {
               MenuEntry menuEntry = _menuEntries[i];
               menuEntry.dispose();
            }

            MenuReference menuReference;
            // dispose all pulldown menu objects
            IEnumerator pulldownKeysEnumerator = instantiatedPullDown.Keys.GetEnumerator();

            while (pulldownKeysEnumerator.MoveNext())
            {
               form = (MgFormBase)pulldownKeysEnumerator.Current;
               form.toolbarGroupsCountClear();
               if (!formsToRefresh.Contains(form))
                  formsToRefresh.Add(form);
               menuReference = (MenuReference)instantiatedPullDown[form];
               Commands.addAsync(CommandType.DISPOSE_OBJECT, menuReference);
            }

            // dispose all context menu objects
            ICollection contextKeys = instantiatedContext.Keys;
            IEnumerator contextKeysEnumerator = contextKeys.GetEnumerator();

            while (contextKeysEnumerator.MoveNext())
            {
               form = (MgFormBase)contextKeysEnumerator.Current;
               form.toolbarGroupsCountClear();
               if (!formsToRefresh.Contains(form))
                  formsToRefresh.Add(form);
               menuReference = (MenuReference)instantiatedContext[form];
               Commands.addAsync(CommandType.DISPOSE_OBJECT, menuReference);
            }

            // dispose all context menu objects
            ICollection ToolbarKeys = _instantiatedToolbar.Keys;
            IEnumerator ToolbarKeysEnumerator = ToolbarKeys.GetEnumerator();

            while (ToolbarKeysEnumerator.MoveNext())
            {
               form = (MgFormBase)ToolbarKeysEnumerator.Current;
               form.toolbarGroupsCountClear();
               if (!formsToRefresh.Contains(form))
                  formsToRefresh.Add(form);
               menuReference = (MenuReference)_instantiatedToolbar[form];
               if (menuReference != null)
                  Commands.addAsync(CommandType.DISPOSE_OBJECT, menuReference);
            }
            //TODO: MerlinRT. Which form are we using here ? 'form' changes in all the three while-loops above.
            //remove the tollbar
            deleteToolBar(form);

            // we do not dispose the Toolbar objects - they are created once when the form is created
            // and are visible\hiddne according to need.

            _internalEventsOnMenus.Clear();
            _internalEventsOnToolBar.Clear();
            _menuEntriesWithAccessKey.Clear();
         }
         return formsToRefresh;
      }

      /// <summary>
      ///   This method rebuilds all menu objects which were used before the menus file has changed.
      ///   This way we refresh the menus to use the definitions of the updated menus file
      /// </summary>
      public void rebuild(List<MgFormBase> formsToRefresh)
      {
         Property prop = null;

         // refresh the menu properties of all effected forms contents (controls)
         if (formsToRefresh != null)
            for (int i = 0; i < formsToRefresh.Count; i++)
            {
               MgFormBase formToRefresh = formsToRefresh[i];
               prop = formToRefresh.getProp(PropInterface.PROP_TYPE_CONTEXT_MENU);
               if (prop != null)
                  prop.RefreshDisplay(true);

               // It is possible that the controls on the form had different context menus than the form itself. 
               // So under form property we will find only the context menu attached to the form. But we need to refresh the other context menus as well.
               formToRefresh.refreshContextMenuForControls();

               prop = formToRefresh.getProp(PropInterface.PROP_TYPE_PULLDOWN_MENU);
               //In order to skip the computation of pulldown menu , call refreshPulldownMenu() directly instead of refreshDisplay().
               if (prop != null)
                  prop.refreshPulldownMenu();
            }
      }

      /// <summary>
      ///   Destroy old menu, execute pending destroy events, and rebuild the menu
      /// </summary>
      public void destroyAndRebuild()
      {
         List<MgFormBase> formsToRefresh = destroy();
         Commands.invoke();
         rebuild(formsToRefresh);
      }

      /// <summary>
      ///   This method enables\ disables all internal event menu items which match the passed
      ///   action number, on the passed form, to the passed enable state.
      /// </summary>
      /// <param name = "form">on which we refresh this action state</param>
      /// <param name = "action">action whose enable state changed</param>
      /// <param name = "enable">new state of the action</param>
      public void enableInternalEvent(MgFormBase form, int action, bool enable, MgFormBase mdiChildForm)
      {
         List<MenuEntryEvent> entries = getInternalEventsEntriesOnMenu(action);
         MgFormBase frameForm = (form != null
                                    ? form.getTopMostFrameFormForMenuRefresh() :
                                    form);
         if (entries != null)
         {
            for (int i = 0; i < entries.Count; i++)
            {
               MenuEntryEvent actionMenu = entries[i];

               if ((actionMenu.InternalEvent < InternalInterface.MG_ACT_USER_ACTION_1) ||
                   (actionMenu.InternalEvent > InternalInterface.MG_ACT_USER_ACTION_20))
               {
                  if (frameForm != null)
                     EnableMenuEntry(actionMenu, frameForm, MenuStyle.MENU_STYLE_PULLDOWN, enable);
                  MgFormBase contextMenuForm = mdiChildForm != null ? mdiChildForm : form;
                  EnableMenuEntry(actionMenu, contextMenuForm, MenuStyle.MENU_STYLE_CONTEXT, enable);
               }
            }
         }

         entries = getInternalEventsEntriesOnToolBar(action);
         if (entries != null)
         {
            for (int i = 0; i < entries.Count; i++)
            {
               MenuEntryEvent actionMenu = entries[i];
               if ((actionMenu.InternalEvent < InternalInterface.MG_ACT_USER_ACTION_1) ||
                   (actionMenu.InternalEvent > InternalInterface.MG_ACT_USER_ACTION_20))
               {
                  if (frameForm != null)
                     EnableMenuEntry(actionMenu, frameForm, MenuStyle.MENU_STYLE_TOOLBAR, enable);
               }
            }
         }
      }

      /// <summary>
      ///   This method refreshes ALL the action menus of the current menu on a specific form
      /// </summary>
      /// <param name = "form"></param>
      public void refreshInternalEventMenus(MgFormBase form)
      {
         TaskBase task = form.getTask();
         MgFormBase mdiChildForm = null;

         // refresh the action according to the pull down menu
         ICollection actions = _internalEventsOnMenus.Keys;
         IEnumerator actionsEnumerator = actions.GetEnumerator();
         if (form.isSubForm())
         {
            MgControlBase subformCtrl = form.getSubFormCtrl();
            form = subformCtrl.getTopMostForm();
         }

         if (form.IsMDIChild)
         {
            mdiChildForm = form;
            form = form.getTopMostFrameForm();
         }

         while (actionsEnumerator.MoveNext())
         {
            int act = ((Int32)actionsEnumerator.Current);
            bool enable = task.ActionManager.isEnabled(act);
            //The action enable\disable is save on the task but the menu of the subform is on the top most form 
            enableInternalEvent(form, act, enable, mdiChildForm);
         }

         // refresh the action according to the tool bar
         actions = _internalEventsOnToolBar.Keys;
         actionsEnumerator = actions.GetEnumerator();
         while (actionsEnumerator.MoveNext())
         {
            int act = ((Int32)actionsEnumerator.Current);
            bool enable = task.ActionManager.isEnabled(act);
            //The action enable\disable is save on the task but the menu of the subform is on the top most form 

            enableInternalEvent(form, act, enable, mdiChildForm);
         }
      }

      /// <summary>
      ///   creates enable command for a specific action on the menu
      /// </summary>
      /// <param name = "actionMenu">action menu to be enabled\ disabled</param>
      /// <param name = "form">on which form</param>
      /// <param name = "menuStyle">which style</param>
      /// <param name = "enable">enable\ disable</param>
      public void EnableMenuEntry(MenuEntry actionMenu, MgFormBase form, MenuStyle menuStyle, bool enable)
      {
         MenuReference menuReference = actionMenu.getInstantiatedMenu(form, menuStyle);
         if (menuReference != null)
         {
            // When we are in GuiThread, we should enable menuentry synchronously.
            if (Misc.IsGuiThread())
               Commands.EnableMenuEntry(menuReference, enable);
            else
               Commands.addAsync(CommandType.PROP_SET_ENABLE, menuReference, null, enable);
         }
      }

      /// <param name = "drillDown">- tells us if we need to perform the same for all sub menus, or only
      ///   for the entries in the current level
      /// </param>
      public void setIndexes(bool drillDown)
      {
         int Idx = 0;
         IEnumerator iMenuEntry = iterator();
         while (iMenuEntry.MoveNext())
         {
            MenuEntry menuEntry = (MenuEntry)iMenuEntry.Current;
#if  !PocketPC
            menuEntry.setIndex(++Idx);
#else
            if (menuEntry.getVisible())
               menuEntry.setIndex(++Idx);
            else
               menuEntry.setIndex(-1);
#endif

            if (drillDown && menuEntry.menuType() == GuiMenuEntry.MenuType.MENU)
               ((MenuEntryMenu)menuEntry).setIndexes(drillDown);
         }
      }

      /// <summary>
      /// Find window list menu item and return its index in the List.
      /// </summary>
      /// <returns>The index of WindowMenu in current MenuEntries</returns>
      public int GetWindowMenuEntryIndex()
      {
          int windowMenuIndex = -1;
          for (int i = 0; i < _menuEntries.Count; i++)
          {
              MenuEntry menuEntry = _menuEntries[i];
              if (menuEntry is MenuEntryWindowMenu)
              {
                  windowMenuIndex = i;
                  break;
              }
          }
          return windowMenuIndex;
      }


      /// <summary>
      /// returns TRUE , if any entry in menu is found visible.
      /// </summary>
      public bool isAnyMenuEntryVisible()
      {
         for (int i = 0; i < _menuEntries.Count; i++)
         {
            if (_menuEntries[i].getVisible())
               return true;
         }
         return false;
      }

      /// <summary>
      /// set pulldown menu to Modal.
      /// </summary>
      /// <param name = "isModal"></param>
      /// <param name = "mainContextIsModal"></param>
      public void SetModal(bool isModal, bool mainContextIsModal)
      {
         if (!isModal || !IsModalDisabled)
         {
            if (isModal)
               IsModalDisabled = true;
            else
               IsModalDisabled = false;
            setModal(_menuEntries, isModal, mainContextIsModal, true);
         }
      }

      /// <summary>
      /// set menu entries Modal, depedning upon the value of isModal & mainContextIsModal.
      /// </summary>
      /// <param name = "menuEntries"></param>
      /// <param name = "isModal"></param>
      /// <param name = "mainContextIsModal"></param>
      private void setModal(List<MenuEntry> menuEntries, bool isModal, bool mainContextIsModal, bool toplevel)
      {
         foreach (MenuEntry menuEntry in menuEntries)
         {
            bool applyModal = false;
            if (menuEntry is MenuEntryMenu)
            {
               MenuEntryMenu menu = (MenuEntryMenu)menuEntry;
               setModal(menu.subMenus, isModal, mainContextIsModal, false);
            }
            else
               applyModal = menuEntry.ShouldSetModal(isModal, mainContextIsModal);

            if (applyModal)
            {
               if (isModal)
               {
                  // If menuEntry is enabled , then only set ModalDisabled flag & disable the menu.
                  if (menuEntry.getEnabled())
                  {
                     menuEntry.setModalDisabled(true);
                     menuEntry.setEnabled(false, true, true, null, toplevel ? true :false);
                  }
               }
               else
               {
                  // If menuEntry is ModalDisabled, then only enable menuEntry & reset ModalDisabled flag.
                  if (menuEntry.getModalDisabled())
                  {
                     menuEntry.setEnabled(true, true, true, null, toplevel ? true : false);
                     menuEntry.setModalDisabled(false);
                  }
               }
            }
         }
      }
   }
}

   
