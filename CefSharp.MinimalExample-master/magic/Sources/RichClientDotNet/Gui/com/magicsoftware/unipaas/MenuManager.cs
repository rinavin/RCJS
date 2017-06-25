using System;
using System.Collections;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas.management.data;

namespace com.magicsoftware.unipaas
{
   /*
   * The MenuManager class will handle the project’s menus.
   * It will give services which allow performing functions related operations on the menus.
   * All menu functions will be activated through the MenuManager object.
   * The services it provides include all but the actual creation of the SWT objects.
   * 
   * Keep a reference to the menu toolbar image file.
   * 
   * We will have one instance of the MenuManager object; it will be a member of the 
   * ClientManager object.
   * 
   * Menu references:
   * Since the content of the menu depends also on rights the user has, we will create the 
   * menus file content specifically for the user and it will contain only the menus which 
   * are relevant for him.
   * Menus file name syntax: <Application identification> + <Rights hash code> + “Menus.xml”.
   * Rights hash code – a hash code, which is a combination of the user’s rights, which are 
   * used in the menus.
   * This is a member of the TaskBase’s MgData object.
   * 
   * Menus file name reference: 
   * In order to keep the task independent from the user who executes it, we will not place 
   * the Rights hash code in the task’s xml. We will use a non user specific file name – 
   * meaning <Application identification> + “Menus.xml”.
   * In addition, we will send with the task the Rights Hash code, which we will add to the 
   * sent menus file name when we need to access the menus xml file.
   * In order to obtain a specific menu, we will use the menus file name as it appears in 
   * the reference (without the rights hash code). 
   */
   public class MenuManager
   {
      private readonly Hashtable _applicationMenusMap;
#if !PocketPC
      public WindowList WindowList  { get; private set; }
#endif
      public MenuManager()
      {
         _applicationMenusMap = new Hashtable();

#if !PocketPC
         WindowList = new WindowList();
#endif
      }

      /// <summary>
      /// Clears applicationMenusMap
      /// </summary>
      public void removeApplicationMenus()
      {
         _applicationMenusMap.Clear();
      }

      /// <summary>
      /// Remove specific application menus from applicationMenusMap.
      /// </summary>
      /// <param name="menuFileName"></param>
      public void removeApplicationMenu(String menuFileName)
      {
         _applicationMenusMap.Remove(menuFileName);
      }

      /// <summary>
      /// When menuEntry is selected on Menu, this method gets called. It calls appropriate functions depending 
      /// upon type of menu selected.
      /// </summary>
      /// <param name="menuEntry">selected menu entry</param>
      /// <param name="activeForm">current active form, when menu is selected.</param>
      /// <param name="activatedFromMDIFrame">Is menu selected from MDI Frame. </param>
      public void onMenuSelection(GuiMenuEntry menuEntry, GuiMgForm activeForm, bool activatedFromMDIFrame)
      {
         try
         {
            MgFormBase topMostForm = null;
            TaskBase task = null;

            if (!((MgFormBase)activeForm).IsHelpWindow)
            {
               task = ((MgFormBase)activeForm).getTask();
               //from the menu task get the form of the menu(topmost) and save member of the last menu Id selected.
               topMostForm = (MgFormBase)task.getTopMostForm();
            }
            else
            {
               //In case of help window, get the parent form of help form and get task from that.
               activeForm = topMostForm = ((MgFormBase)activeForm).ParentForm;
               task = topMostForm.getTask();
            }

            topMostForm.LastClickedMenuUid = menuEntry.menuUid();

            if (menuEntry is MenuEntryProgram)
               Events.OnMenuProgramSelection(task.ContextID, (MenuEntryProgram)menuEntry, activeForm, activatedFromMDIFrame);
            else if (menuEntry is MenuEntryOSCommand)
               Events.OnMenuOSCommandSelection(task.ContextID, (MenuEntryOSCommand)menuEntry, activeForm);
            else if (menuEntry is MenuEntryEvent)
            {
               // in order to get the correct ctlidx we take it from the parent MgMenu of the menuentry and then
               // change it if the event is a user event from another component
               int ctlIdx = ((MenuEntryEvent)menuEntry).getParentMgMenu().CtlIdx;
               MenuEntryEvent menuEntryEvent = (MenuEntryEvent)menuEntry;
               if (menuEntryEvent.UserEvtCompIndex > 0)
                  ctlIdx = menuEntryEvent.UserEvtCompIndex;

               Events.OnMenuEventSelection(task.ContextID, (MenuEntryEvent)menuEntry, activeForm, ctlIdx);

            }
            else if (menuEntry is MenuEntryWindowMenu)
            {
               MgFormBase mgFormbase = ((MenuEntryWindowMenu)menuEntry).MgForm;

               // Activate the form associated with the entry
               Commands.addAsync(CommandType.ACTIVATE_FORM, mgFormbase);

               // Put ACT_HIT on form.
               Events.OnMenuWindowSelection((MenuEntryWindowMenu)menuEntry);
            }
         }
         catch (ApplicationException ex)
         {
            Events.WriteExceptionToLog(ex);
         }
      }


      /// <summary>
      ///  This method initializes an application’s menus object, from the passed menusFileName
      ///  (loads the menus xml file to matching data structures).
      /// </summary>
      /// <param name="mainProg"></param>
      /// <returns></returns>
      public ApplicationMenus getApplicationMenus(TaskBase mainProg)
      {
         if (mainProg == null)
            return null;

         // build the menus file URL from the name and the rights hash code
         String menusKey = mainProg.getMenusFileURL();
         ApplicationMenus appMenus = null;

         if (menusKey != null)
         {
            appMenus = (ApplicationMenus) _applicationMenusMap[menusKey];

            if (appMenus == null)
            {
               try
               {
                  byte[] menusFileContent = mainProg.getMenusContent();

                  // build the application menus
                  appMenus = new ApplicationMenus(menusFileContent);

                  // set the indexes of the menu entries relative to their immediate parents.
                  // also set the CtlIdx of the application that contains the menu on the MgMenu,
                  int CtlIdx = mainProg.getCtlIdx();
                  IEnumerator iMgMenu = appMenus.iterator();
                  while (iMgMenu.MoveNext())
                  {
                     MgMenu mgmenu = (MgMenu) iMgMenu.Current;
                     mgmenu.setIndexes(true);
                     mgmenu.CtlIdx = CtlIdx;
                  }

                  _applicationMenusMap[menusKey] = appMenus;
               }
               catch (Exception ex)
               {
                  Events.WriteExceptionToLog(ex);
               }
            }
         }
         return (appMenus);
      }

      /// <summary>
      ///  This method returns a specific menu object, which matches the passed menu index. It checks if the wanted
      ///  menu already exists. If it does not, it calls the CreateMenu method for this entry. The matching MgMenu
      ///  object is returned. This method will be called from the Property::RefreshDisplay. It will provide the
      ///  Property mechanism with a matching MenuEntry to the specified menu identification.
      /// </summary>
      /// <param name="mainProg"></param>
      /// <param name="menuIndex"></param>
      /// <param name="menuStyle">type of the menu: MENU_TYPE_PULLDOWN, MENU_TYPE_CONTEXT</param>
      /// <param name="form"></param>
      /// <param name="createIfNotExist">This will decide if menu is to be created or not.</param>
      /// <returns></returns>
      public MgMenu getMenu(TaskBase mainProg, int menuIndex, MenuStyle menuStyle, MgFormBase form, bool createIfNotExist)
      {
         MgMenu retMenu = null;

         if (mainProg.menusAttached())
         {
            ApplicationMenus appMenus = getApplicationMenus(mainProg);

            if (appMenus != null)
            {
               retMenu = appMenus.getMgMenu(menuIndex);
               if (createIfNotExist && retMenu != null && !retMenu.menuIsInstantiated(form, menuStyle))
               {
                  // this menu does not have a matching java object for this shell – we need to create it
                  // Context menus are created & destroyed on the fly. For context menu, if no visible menu entry
                  // is found at 1st level, then don't create it. Because, if no visible menu entry is found , 
                  // no popup is displayed, So menu gets instantiated but it is not destroyed as we don't get CLOSING message.
                  if (menuStyle == MenuStyle.MENU_STYLE_CONTEXT)
                  {
                     if (retMenu.isAnyMenuEntryVisible())
                        retMenu.createMenu(form, menuStyle);
                     else
                        retMenu = null;
                  }
                  else
                     retMenu.createMenu(form, menuStyle);
               }
            }
         }

         return retMenu;
      }

      /// <summary>
      ///   refresh the actions of all menus belonging to the current task (according to the current task state)
      /// </summary>
      /// <param name = "currentTask"></param>
      public void refreshMenuActionForTask(TaskBase currentTask)
      {
         MgFormBase currentForm = currentTask.getForm();
         if (currentForm != null)
         {
            MgFormBase menusContainerForm; // the form containing the menus to be refreshed
            if (currentForm.isSubForm())
            {
               menusContainerForm = currentForm.getSubFormCtrl().getTopMostForm();
               if (menusContainerForm.IsMDIChild || menusContainerForm.IsFloatingToolOrModal)
                  menusContainerForm = menusContainerForm.getTopMostFrameForm();
            }
            else
               menusContainerForm = currentForm.getTopMostFrameForm();

            if (menusContainerForm == null)
               menusContainerForm = currentForm.getTopMostFrameFormForMenuRefresh();

            if (menusContainerForm != null)
            {
               MenuStyle[] stylesToRefresh = new[]
                                                {
                                                   MenuStyle.MENU_STYLE_PULLDOWN, MenuStyle.MENU_STYLE_CONTEXT,
                                                   MenuStyle.MENU_STYLE_TOOLBAR
                                                };
               foreach (var style in stylesToRefresh)
               {
                  // refresh the actions
                  MgMenu mgMenu = menusContainerForm.getMgMenu(style);
                  if (mgMenu != null)
                     mgMenu.refreshMenuAction(currentTask.getForm(), style);
               }
            }
         }
      }

      /// <summary>
      ///   Refresh the pulldown menu actions.
      /// </summary>
      /// <param name = "frameForm"></param>
      public void RefreshPulldownMenuActions(MgFormBase frameForm)
      {
         MenuStyle[] stylesToRefresh = new[]
                           {
                              MenuStyle.MENU_STYLE_PULLDOWN,
                              MenuStyle.MENU_STYLE_TOOLBAR
                           };
         foreach (var style in stylesToRefresh)
         {
            // refresh the actions
            MgMenu mgMenu = frameForm.getMgMenu(style);
            if (mgMenu != null)
               mgMenu.refreshMenuAction(frameForm, style);
         }
      }

      /// <summary>
      /// destroy and rebuild () all menus.
      /// </summary>
      public void destroyAndRebuild()
      {
         ICollection menusFileNamesKeys = _applicationMenusMap.Keys;
         int size = menusFileNamesKeys.Count;

         if (size > 0)
         {
            String[] ApplicationMenusFileNames = new String[size];
            menusFileNamesKeys.CopyTo(ApplicationMenusFileNames, 0);

            String menusFileName = null;
            for (int i = 0;
                 i < size;
                 i++)
            {
               menusFileName = ApplicationMenusFileNames[i];
               destroyAndRebuild(menusFileName);
            }
         }
      }

      /// <summary>
      /// destroy and rebuild menus for the passed file.
      /// </summary>
      public void destroyAndRebuild(String menusFileName)
      {
         ApplicationMenus appMenus = (ApplicationMenus)_applicationMenusMap[menusFileName];
         if (appMenus != null)
         {
            // remove previous object - no longer needed
            _applicationMenusMap.Remove(menusFileName);

            appMenus.destroyAndRebuild();
         }
      }

      /// <summary>
      ///   check/uncheck menu entry identified by entryName.
      /// </summary>
      /// <param name = "task"></param>
      /// <param name = "entryName">menuentry name </param>
      /// <param name = "check">check/uncheck value</param>
      public bool MenuCheckByName(TaskBase task, String entryName, bool check)
      {
         MgMenu pulldownMenu = GetPulldownMenu (task);

         // Get matching menus from all ctls
         ArrayList menuEntryList = GetMatchingMenuValues(task.ContextID, entryName, pulldownMenu);
         if (menuEntryList != null)
         {
            IEnumerator iMatchingEnt = menuEntryList.GetEnumerator();
            while (iMatchingEnt.MoveNext())
            {
               var menuValue = (MenuValue)iMatchingEnt.Current;
               var mnuEnt = menuValue.InnerMenuEntry;

               bool refresh;
               if (menuValue.IsPulldown)
                  refresh = true;
               else
                  refresh = false;

               mnuEnt.setChecked(check, refresh);
            }
         }

         if (pulldownMenu != null && IsTopLevelMenu(pulldownMenu, entryName))
            Manager.WriteToMessagePane(task, "Check/UnCheck of top level menu item is not allowed", false);

         return true;
      }

      /// <summary>
      ///   show/hide menu entry identified by entryName.
      /// </summary>
      /// <param name = "task"></param>
      /// <param name = "entryName">menuentry name</param>
      /// <param name = "show">show/hide value</param>
      public bool MenuShowByName(TaskBase task, String entryName, bool show)
      {
         bool pulldownMenuModified = false;
         MgMenu pulldownMenu = GetPulldownMenu(task);
         
         // Get matching menus from all ctls
         ArrayList menuEntryList = GetMatchingMenuValues(task.ContextID, entryName, pulldownMenu);
         if (menuEntryList != null)
         {
            IEnumerator iMatchingEnt = menuEntryList.GetEnumerator();
            while (iMatchingEnt.MoveNext())
            {
               var menuValue = (MenuValue)iMatchingEnt.Current;
               var mnuEnt = menuValue.InnerMenuEntry;
               if (menuValue.IsPulldown)
                  pulldownMenuModified = true;
               mnuEnt.setVisible(show, false, menuValue.IsPulldown, task, null);
            }
         }

         // If the menu is being shown, then refresh 'enable' state of internal event menus
         if (show && pulldownMenuModified)
         {
            int ctlIdx = 0;
            TaskBase mainProg = (TaskBase)Manager.MGDataTable.GetMainProgByCtlIdx(task.ContextID, ctlIdx);
            ApplicationMenus menus = getApplicationMenus(mainProg);
            MgFormBase formOrg = (MgFormBase)task.getTopMostForm();
            MgFormBase form = (MgFormBase)formOrg.getTopMostFrameForm();
            // fixed bug#:773382, when there is no SDI\MDI need to get the org form (for the context menu)
            form = (form ?? formOrg);
            menus.refreshInternalEventMenus(form);
         }

          return true;
      }

      /// <summary>
      ///   enable/disable menu entry identified by entryName.
      /// </summary>
      /// <param name = "task"></param>
      /// <param name = "entryName">menuentry name</param>
      /// <param name = "enable">enable/disable value</param>
      public bool MenuEnableByName(TaskBase task, String entryName, bool enable)
      {
         bool internalEventMenuEnabled = false;
         MgMenu pulldownMenu = GetPulldownMenu(task);

         // Get matching menus from all ctls
         ArrayList menuEntryList = GetMatchingMenuValues(task.ContextID, entryName, pulldownMenu);
         
         if (menuEntryList != null)
         {
            IEnumerator iMatchingEnt = menuEntryList.GetEnumerator();
            while (iMatchingEnt.MoveNext())
            {
               var menuValue = (MenuValue)iMatchingEnt.Current;
               var mnuEnt = menuValue.InnerMenuEntry;

               if (mnuEnt.menuType() == GuiMenuEntry.MenuType.INTERNAL_EVENT)
               {
                  MenuEntryEvent menuEntryEvent = (MenuEntryEvent)mnuEnt;
                  if ((menuEntryEvent.InternalEvent < InternalInterface.MG_ACT_USER_ACTION_1) ||
                      (menuEntryEvent.InternalEvent > InternalInterface.MG_ACT_USER_ACTION_20))
                     internalEventMenuEnabled = true;
               }

               bool refresh;
               if (menuValue.IsPulldown)
                  refresh = true;
               else
                  refresh = false;

               //set the ModalDisabled flag, depending upon the value of enable.
               if (!enable)
                  mnuEnt.setModalDisabled(false);

               mnuEnt.setEnabled(enable, true, true, entryName, refresh);
            }

            if (internalEventMenuEnabled)
               Manager.WriteToMessagePanebyMsgId(task, MsgInterface.MENU_STR_ERROR_ENABLE, false);
         }

         return true;
      }

      /// <summary>
      ///   Sets the menu entry text of a menu entry.
      /// </summary>
      /// <param name = "task"></param>
      /// <param name = "entryName">menuentry name</param>
      /// <param name = "entryText">new menuentry text</param>
      public bool SetMenuName(TaskBase task, String entryName, String entryText)
      {
         bool isNameSet = false;
         MgMenu pulldownMenu = GetPulldownMenu(task);

         // Get matching menus from all ctls
         ArrayList menuEntryList = GetMatchingMenuValues(task.ContextID, entryName, pulldownMenu);

         if (menuEntryList != null)
         {
            IEnumerator iMatchingEnt = menuEntryList.GetEnumerator();
            while (iMatchingEnt.MoveNext())
            {
               var menuValue = (MenuValue)iMatchingEnt.Current;
               var mnuEnt = menuValue.InnerMenuEntry;

               bool refresh;
               if (menuValue.IsPulldown)
                  refresh = true;
               else
                  refresh = false;
               mnuEnt.setText(entryText, refresh);
               isNameSet = true;
            }
         }

         return isNameSet;
      }

      /// <summary>
      ///   get Menu's index.
      /// </summary>
      /// <param name = "mainProg">main prog</param>
      /// <param name = "entryName">menuentry name</param>
      /// <param name = "isPublic"></param>
      public int GetMenuIdx(TaskBase mainProg, String entryName, bool isPublic)
      {
         int index = 0;

         ApplicationMenus menus = getApplicationMenus(mainProg);
         if (menus != null)
            index = menus.menuIdxByName(entryName, isPublic);

         return index;
      }

      /// <summary>
      ///   Add menu identified by menuIdx, in current pulldown menu at menuPath specified.
      /// </summary>
      /// <param name = "mainProg">main prog</param>
      /// <param name = "menuIdx">menu to be added</param>
      /// <param name = "menuPath">menu path</param>
      public bool MenuAdd(TaskBase mainProg, TaskBase task, int menuIdx, String menuPath)
      {
         Boolean success = false;

         // Read all menus present inside Menu repository
         ApplicationMenus menus = getApplicationMenus(mainProg);

         int index = menuIdx; // index of the menu inside Menu repository
         MgMenu addMenu = menus.getMgMenu(index); // menu structure to add
         MgMenu pulldownMenu = GetPulldownMenu(task);

         MgFormBase topMostFrameForm = (MgFormBase)task.getForm() == null ? null : (MgFormBase)task.getForm().getTopMostFrameForm();

         if (addMenu == null)
            success = false;
         // Add Menu to Current Pulldown Menu, only if the menu to add is not Current Pulldown Menu
         else if (pulldownMenu != null && topMostFrameForm.getPulldownMenuNumber() > 0 && index != topMostFrameForm.getPulldownMenuNumber())
         {
            // if new menu is to be added, make a new menu by clone instead of using the same reference
            var clonedMenu = (MgMenu)addMenu.Clone();
            MgMenu root = topMostFrameForm.getPulldownMenu();

            bool includeInMenu = topMostFrameForm.isUsedAsPulldownMenu(root);
            bool includeInContext = topMostFrameForm.isUsedAsContextMenu(root);

            // If path, where the menu is to be added, is Null, then add the menu at the end of Current Pulldown Menu.
            if (String.IsNullOrEmpty(menuPath))
            {
               // If menu is not instantiated, it means old menu is deleted, so assign new menu as pulldown menu.
               if (pulldownMenu.getInstantiatedMenu(topMostFrameForm, MenuStyle.MENU_STYLE_PULLDOWN) == null)
               {
                  topMostFrameForm.setPulldownMenuNumber(index, true);
                  success = true;
               }
               else if (!String.IsNullOrEmpty(clonedMenu.getName()))
               {
                  IEnumerator iAddMenu = clonedMenu.iterator();
                  MenuEntry newMenuEntry;

                  // Iterate through all menu entries inside Menu structure to be added, and add it to the current 
                  // Pulldown Menu Structure
                  while (iAddMenu.MoveNext())
                  {
                     newMenuEntry = (MenuEntry)iAddMenu.Current;
                     root.addSubMenu(newMenuEntry);
                     newMenuEntry.setParentRootMgMenu(root);
                     // set orgindex as -1, so that resetIndexes() will recalculate orgIndex
                     newMenuEntry.setIndex(-1);
#if PocketPC
                     // Add menu entry to the Pulldown Menu if menu entry is visible
                     SetAddedMenuVisible(newMenuEntry, includeInMenu, task);
#else
                     // Add menu entry object to the Pulldown Menu for newly added menu entries.
                     newMenuEntry.CreateNewlyAddedMenus(root, topMostFrameForm);
#endif
                  }
                  success = true;
               }
            }
            // Function will fail if consecutive "\\" are found in the menu path 
            else if (menuPath.IndexOf("\\\\") >= 0)
            {
               success = false;
            }
            // Valid Menu Path
            else
            {
               if (!String.IsNullOrEmpty(clonedMenu.getName()))
               {
                  int idx = 0;
                  Object menuPos = null;
                  menuPath = menuPath.TrimEnd(' ');
                  success = FindMenuPath(root, menuPath, ref menuPos, ref idx);
                  if (success)
                  {
                     IEnumerator iAddMenu = clonedMenu.iterator();
                     MenuEntry newMenuEntry;
                     int i = 0;

                     // if menu to be added to root menu i.e Current Pulldown Menu
                     if (menuPos.GetType() == typeof(MgMenu))
                     {
                        while (iAddMenu.MoveNext())
                        {
                           newMenuEntry = (MenuEntry)iAddMenu.Current;
                           root.addMenu(newMenuEntry, idx + i);
                           newMenuEntry.setParentRootMgMenu(root);
                           // set orgindex as -1, so that resetIndexes() will recalculate orgIndex
                           newMenuEntry.setIndex(-1);
#if PocketPC
                           // Add menu entry to the Pulldown Menu if menu entry is visible
                           SetAddedMenuVisible(newMenuEntry, includeInMenu, task);
#else
                           // Add menu entry object to the Pulldown Menu for newly added menu entries.
                           newMenuEntry.CreateNewlyAddedMenus(root, topMostFrameForm);
#endif
                           i++;
                        }
                     }
                     else // if menu to be added to any menu entry
                     {
                        while (iAddMenu.MoveNext())
                        {
                           newMenuEntry = (MenuEntry)iAddMenu.Current;
                           // set the root parent menu 
                           newMenuEntry.setParentRootMgMenu(((MenuEntry)menuPos).getParentMgMenu());
                           // set immediate parent of the menu entry
                           newMenuEntry.ParentMenuEntry = ((MenuEntry)menuPos).ParentMenuEntry;
                           // set orgindex as -1, so that resetIndexes() will recalculate orgIndex
                           newMenuEntry.setIndex(-1);
                           ((MenuEntryMenu)menuPos).addSubMenu(newMenuEntry, idx + i);
#if PocketPC
                           // Add menu entry to the Pulldown Menu if menu entry is visible
                           SetAddedMenuVisible(newMenuEntry, includeInMenu, task);
#else
                           // Add menu entry object to the Pulldown Menu for newly added menu entries.
                           newMenuEntry.CreateNewlyAddedMenus(((MenuEntry)menuPos).getInstantiatedMenu(topMostFrameForm, MenuStyle.MENU_STYLE_PULLDOWN), topMostFrameForm);
#endif
                           i++;
                        }
                     }
                  }
               }
            }
         }
         else
         {
            //If no pulldown menu is attached, menu to be added should be assigned as pulldown menu.
            if (topMostFrameForm != null)
            {
               if (topMostFrameForm.getPulldownMenuNumber() == 0)
                  topMostFrameForm.setPulldownMenuNumber(menuIdx, true);
               else
                  topMostFrameForm.getProp(PropInterface.PROP_TYPE_PULLDOWN_MENU).RefreshDisplay(true, Int32.MinValue, false);
               success = true;
            }
         }
         if (success)
         {
            if (menus != null)
               menus.refreshInternalEventMenus(topMostFrameForm);
         }
         return success;
      }

#if PocketPC
      /// <summary>
      ///   Refresh the menu visiblity.
      /// </summary>
      /// <param name = "newMenuEntry"></param>
      /// <param name = "includeInMenu"></param>
      /// <param name = "task"></param>
      private static void SetAddedMenuVisible(MenuEntry newMenuEntry, bool includeInMenu, TaskBase task)
      {
         //set indexes of all menu entries 
         if (newMenuEntry.getVisible())
         {
            newMenuEntry.setVisible(false, true, false, null);
            newMenuEntry.setVisible(true, false, includeInMenu, task, null);
         }
      }
#endif

      /// <summary>
      ///   Finds the menu entry for the menu path
      /// </summary>
      /// <param name = "root">current pulldown menu structure</param>
      /// <param name = "menuPath">path where the menu structure is to be added</param>
      /// <param name = "menuPos">contains menu entry as out value, where the menu is to be added</param>
      /// <param name = "idx">contains position to add inside menuPos, as out value</param>
      /// <returns>true if the menuPos is found</returns>
      private static Boolean FindMenuPath(Object root, String menuPath, ref Object menuPos, ref int idx)
      {
         Boolean subMenu = false;
         Boolean found = false;
         Object father = null;
         MenuEntry menuEntry = null;
         Boolean ret = false;
         int mnuPos = 0;

         //If MenuPath has trailing slash then add the Menu as SubMenu.
         if (menuPath.EndsWith("\\"))
            subMenu = true;

         String[] tokens = menuPath.Split('\\');
         IEnumerator iMenuEntry;

         foreach (String token in tokens)
         {
            if (String.IsNullOrEmpty(token))
               break;

            mnuPos = 0;

            if (root.GetType() == typeof(MgMenu))
               iMenuEntry = ((MgMenu)root).iterator();
            else if (root.GetType() == typeof(MenuEntryMenu))
               iMenuEntry = ((MenuEntryMenu)root).iterator();
            else
               break;

            while (iMenuEntry.MoveNext())
            {
               menuEntry = (MenuEntry)iMenuEntry.Current;
               if (menuEntry.getName() != null)
               {
                  if (menuEntry.getName().Equals(token))
                  {
                     father = root;
                     root = menuEntry;
                     found = true;
                     break;
                  }
               }
               else
                  found = false;

               mnuPos++;
            }

            if (found == false)
               break;
         }

         if (found)
         {
            if (menuEntry.menuType() == GuiMenuEntry.MenuType.MENU)
            {
               //If Menu type is Menu and have trailing slash in MenuPath
               if (subMenu)
               {
                  idx = 0;
                  menuPos = root;
               }
               // If Menu type is Menu and don't have trailing slash in MenuPath
               else
               {
                  idx = mnuPos + 1;
                  menuPos = father;
               }
               ret = true;
            }
            else
            {
               // If Menu type is not Menu and don't have trailing slash in MenuPath
               if (!subMenu)
               {
                  idx = mnuPos + 1;
                  menuPos = father;
                  ret = true;
               }
            }
         }

         return ret;
      }

      /// <summary>
      ///   gets menu path.
      /// </summary>
      /// <param name = "mainProg"></param>
      /// <param name = "menuUid"></param>
      public String GetMenuPath(TaskBase mainProg, int menuUid)
      {
         String tmpMenuPath = "";
         String MenuPath = "";

         // Read all menus present inside Menu repository
         ApplicationMenus menus = getApplicationMenus(mainProg);


         if (menus != null && menuUid != 0)
         {
            MenuEntry menuEntry = menus.menuByUid(menuUid);
            if (menuEntry != null)
            {
               tmpMenuPath = BuildProgramMenuPath(menuEntry);
               MenuPath = tmpMenuPath.Substring(0, tmpMenuPath.Length - 1);
            }
         }

         return MenuPath;
      }

      /// <summary>
      ///   Build the path of the menu entry from its MgMenu parent.
      /// </summary>
      /// <param name = "menuEntry"></param>
      /// <returns></returns>
      private static String BuildProgramMenuPath(Object menuEntry)
      {
         MenuEntry mnuEntry;
         String menuPath = "";

         if (menuEntry != null)
         {
            mnuEntry = (MenuEntry)menuEntry;
            MenuEntry parent = (MenuEntry)mnuEntry.ParentMenuEntry;
            if (parent != null)
               menuPath = BuildProgramMenuPath(parent);
            menuPath = String.Concat(menuPath, mnuEntry.TextMLS);
            menuPath = String.Concat(menuPath, ";");
         }

         return menuPath;
      }

      /// <summary>
      ///   Remove menu identified by menuIdx, in current pulldown menu at menuPath specified.
      /// </summary>
      /// <param name = "mainProg"></param>
      /// <param name = "menuIdx"></param>
      /// <param name = "menuPath"></param>
      public bool MenuRemove(TaskBase mainProg, TaskBase task, int menuIdx, String menuPath)
      {
         Boolean success = false;

         // Read all menus present inside Menu repository
         ApplicationMenus menus = getApplicationMenus(mainProg);

         int index = menuIdx; // index of the menu inside Menu repository
         MgMenu delMenu = menus.getMgMenu(index); // menu structure to delete

         MgFormBase topMostFrameForm = (MgFormBase)task.getForm() == null ? null : (MgFormBase)task.getForm().getTopMostFrameForm();
         MgMenu pulldownMenu = GetPulldownMenu (task);
         if (delMenu == null || pulldownMenu == null)
            success = false;
         else
         {
            // If path, from where the menu to be deleted is Null and 1.menu to be deleted is pulldown menu. The delete pulldown menu
            // menu to be deleted is not pulldown menu, search the whole menu.
            if (String.IsNullOrEmpty(menuPath))
            {
               success = true;
               if (delMenu == pulldownMenu) //remove all entries from pulldown menu
               {
                  // Set PulldownMenuIdx to 0. this will reset the menu.
                  int saveMenuIdx = topMostFrameForm.getPulldownMenuNumber();
                  topMostFrameForm.setPulldownMenuNumber(0, true);
                  topMostFrameForm.setPulldownMenuNumber(saveMenuIdx, false);
               }
               else
                  SearchAndRemoveMenuEntries(0, delMenu, pulldownMenu, topMostFrameForm);
            }
            // Function will fail if consecutive "\\" are found in the menu path 
            else if (menuPath.IndexOf("\\\\") >= 0)
            {
               success = false;
            }
            // Valid Menu Path
            else
            {
               int idx = 0;
               Object menuPos = null;
               menuPath = menuPath.TrimEnd(' ');
               success = FindMenuPath(pulldownMenu, menuPath, ref menuPos, ref idx);
               if (success)
               {
                  SearchAndRemoveMenuEntries(idx, delMenu, menuPos, topMostFrameForm);
               }
            }
         }

         if (success)
         {
            if (pulldownMenu != null)
               pulldownMenu.refreshInternalEventMenus(topMostFrameForm);
         }
         return success;
      }

      /// <summary>
      ///   Reset Pulldown menu.
      /// </summary>
      /// <param name = "context task"></param>
      public bool MenuReset(TaskBase mainProg, TaskBase task)
      {
         MgFormBase formOrg = task.getTopMostForm();
         MgFormBase form = (formOrg != null)
                              ? formOrg.getTopMostFrameForm()
                              : null;
         form = (form ?? formOrg);

         if (form != null)
         {
            int pulldownMenuIdx = form.getPulldownMenuNumber();

            if (pulldownMenuIdx > 0)
            {
               String menusFileUrl = mainProg.getMenusFileURL();

               //Suspend drawing of frame form till pulldown menu is destroy and rebuilt.
               Commands.addAsync(CommandType.SUSPEND_PAINT, form);
               
               //destroy and rebuild all application menus.
               destroyAndRebuild(menusFileUrl);
               
               Commands.addAsync(CommandType.RESUME_PAINT, form);

               MgMenu pulldownMenu = form.getPulldownMenu();

               //recreate the pulldown menu, only if it is not instantiated.This happens only in case, if MenuRemove()
               //is called to remove the pulldown menu.
               if (!pulldownMenu.menuIsInstantiated(form, MenuStyle.MENU_STYLE_PULLDOWN))
                  form.setPulldownMenuNumber(pulldownMenuIdx, true);
            }
         }

         return true;
      }

      /// <summary>
      ///   Set pulldown menu Modal depending upon value of isModal.
      /// </summary>
      /// <param name = "contextID"></param>
      /// <param name = "isModal"></param>
      public void SetModal(Int64 contextID, bool isModal)
      {
         MgFormBase frameForm = Events.GetRuntimeContext(contextID).FrameForm;

         if (frameForm != null)
         {
            MgMenu pulldownMenu = frameForm.getPulldownMenu();
            if (pulldownMenu != null)
            {
               bool mainContextIsModal = Events.IsBatchRunningInMainContext();
               pulldownMenu.SetModal(isModal, mainContextIsModal);
            }
         }
      }

      /// <summary>
      ///   Get matching menu entries from menus of all ctls opened.
      /// </summary>
      /// <param name = "entryName">name to be matched</param>
      /// <param name = "currentPulldownMenu">current pulldown menu</param>
      private ArrayList GetMatchingMenuValues(Int64 contextID, String entryName, MgMenu currentPulldownMenu)
      {
         entryName = entryName.Trim();

         ArrayList menuEntryList = new ArrayList();

         // Go through all ctls to get matching menus
         int ctlIdx = 0;
         TaskBase mainProg = (TaskBase)Manager.MGDataTable.GetMainProgByCtlIdx(contextID, ctlIdx);
         while (mainProg != null)
         {
            ApplicationMenus menus = getApplicationMenus(mainProg);

            if (menus != null)
            {
               ArrayList tempMenuEntryList = menus.GetMatchingMenuValues(entryName, currentPulldownMenu);

               if (tempMenuEntryList != null)
                  menuEntryList.AddRange(tempMenuEntryList);
            }

            ctlIdx++;
            mainProg = (TaskBase)Manager.MGDataTable.GetMainProgByCtlIdx(contextID, ctlIdx);
         }

         return menuEntryList;
      }

      /// <param name = "mgMenu">EntryName EntryName to be checked</param>
      /// <param name = "entryName">EntryName to be checked</param>
      /// <returns> Returns true if specified entry name is found in top level menu</returns>
      private bool IsTopLevelMenu(MgMenu mgMenu, String entryName)
      {
         bool found = false;

         IEnumerator iMenuEntry = mgMenu.iterator();
         while (!found && iMenuEntry.MoveNext())
         {
            MenuEntry menuEntry = (MenuEntry)iMenuEntry.Current;
            String menuName = menuEntry.getName();

            if (menuName != null && String.CompareOrdinal(menuName, entryName) == 0)
               found = true;
         }

         return found;
      }

      /// <summary>
      ///   Get pulldown Menu.
      /// </summary>
      /// <param name = "task"></param>
      private MgMenu GetPulldownMenu (TaskBase task)
      {
         MgFormBase formOrg = (MgFormBase)task.getTopMostForm();

         MgFormBase form = (formOrg != null) ? (MgFormBase)formOrg.getTopMostFrameForm() : null;
         
         form = (form ?? formOrg);

         MgMenu pulldownMenu = (form != null)  ? form.getPulldownMenu() : null;

         return pulldownMenu;
      }

      /// <summary>
      ///   Remove menu entry at location idx from mgMenu.
      /// </summary>
      /// <param name = "idx">location of menu entry.</param>
      /// <param name = "mgMenu">menu from which menu entry is to be deleted.</param>
      /// <param name = "form">Frame window</param>
      private void RemoveAt(int idx, object mgMenu, MgFormBase form)
      {
         if (mgMenu.GetType() == typeof(MgMenu))
            ((MgMenu)mgMenu).removeAt(idx, form);
         else
            ((MenuEntryMenu)mgMenu).removeAt(idx, form);
      }

      /// <summary>
      ///   Search and remove menu entries that are found in delMenu.
      /// </summary>
      /// <param name = "delMenu">Menu  to be deleted.</param>
      /// <param name = "menuPos">menu from which menu entries to be deleted</param>
      /// <param name = "form">Frame window</param>
      private void SearchAndRemoveMenuEntries(int idx, MgMenu delMenu, object menuPos, MgFormBase form)
      {

         IEnumerator iDelMenuEntry = delMenu.iterator();
         while (iDelMenuEntry.MoveNext())
         {
            IEnumerator iMenuEntry;
            MenuEntry delMenuEntry = (MenuEntry)iDelMenuEntry.Current;
            String delMenuName = delMenuEntry.TextMLS;
            
            if (menuPos is MgMenu)
               iMenuEntry = ((MgMenu)menuPos).iterator();
            else 
               iMenuEntry = ((MenuEntryMenu)menuPos).iterator();

            SearchAndRemoveMenuEntry(idx, iMenuEntry, delMenuName, menuPos, form);
         }
      }

      /// <summary>
      ///   Search and remove menu entries after location idx from pulldown menu.
      /// </summary>
      /// <param name = "idx">location of menu entry.</param>
      /// <param name = "iMenuEntry">menu entry iterator.</param>
      /// <param name = "delMenuName">menuentry name to match menuentry in iMenuEntry enumerator.</param>
      /// <param name = "menuPos">menu from which menu entries to be deleted</param>
      /// <param name = "form">Frame window</param>
      private bool SearchAndRemoveMenuEntry(int idx, IEnumerator iMenuEntry, String delMenuName, object menuPos, MgFormBase form)
      {
         bool removed = false;
         for (int i = 0; i < idx; i++)
            iMenuEntry.MoveNext();

         int menuEntryIdx = idx;
         while (!removed && iMenuEntry.MoveNext())
         {
            MenuEntry menuEntry = (MenuEntry)iMenuEntry.Current;
            String menuName = menuEntry.TextMLS;

            if (menuName != null && String.CompareOrdinal(menuName, delMenuName) == 0)
            {
               RemoveAt(menuEntryIdx, menuPos, form);
               removed = true;
            }
            else
               menuEntryIdx++;
         }

         return removed;
      }

      public TaskDefinitionId TaskCalledByMenu(int menuId, TaskBase mainProg)
      {
         //fixed bug#:431559, the menu need to be take from menuCtlIndex main program.
         ApplicationMenus applicationMenus = getApplicationMenus(mainProg);
         MenuEntryProgram menuEntry = applicationMenus.menuByUid(menuId) as MenuEntryProgram;

         TaskDefinitionId taskDefinitionId = new TaskDefinitionId(menuEntry.CtlIndex, menuEntry.ProgramIsn, 0, true);

         return taskDefinitionId;
      }

      /// <summary>
      ///   The menu texts needs to be refreshed due to a change in the language.
      /// </summary>
      /// <param name = "contextID">The context on which menues will be refreshed.</param>
      public void RefreshMenusText(Int64 contextID)
      {
         // Go through all ctls in the context. Each ctl has its own application menu.
         int ctlIdx = 0;
         TaskBase mainProg = (TaskBase)Manager.MGDataTable.GetMainProgByCtlIdx(contextID, ctlIdx);
         while (mainProg != null)
         {
            ApplicationMenus menus = getApplicationMenus(mainProg);

            // call refreshMenuesTextMls for each application menu of components, starting with the main.
            if (menus != null)
               menus.refreshMenuesTextMls();

            ctlIdx++;
            mainProg = (TaskBase)Manager.MGDataTable.GetMainProgByCtlIdx(contextID, ctlIdx);
         }
      }
   }
}
