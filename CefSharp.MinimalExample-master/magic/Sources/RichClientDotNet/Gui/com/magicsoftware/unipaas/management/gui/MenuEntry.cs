using System;
using System.Collections;
using System.Diagnostics;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.management.tasks;

namespace com.magicsoftware.unipaas.management.gui
{
   /*
    * The MenuEntry class describes a specific menu entry (Can be pulldown menu or context menu). 
    * It contains a list of MenuEntry objects.
    * This class holds the specific data we need in order to add an event to the queue (such as 
    * the menu code, program number to execute and so on).
    * We will create a Handler, which will perform the relevant operations for each activated 
    * menu type.
    * (The accelerator exists on the MenuItem (Gui Layer), and we do not need to handle it – 
    * just to define it on the menu)
    */

   /// <summary>
   ///   While adding a new member in this class, please make sure that you want to copy the value of that member
   ///   in new object or not inside clone method.
   /// </summary>
   public class MenuEntry : GuiMenuEntry, ICloneable
   {
      private int _index; // menu index
      private String _name; // menu name (internal)
      private MgMenu _parentMgMenu; // reference to the root parent menu

      /// <summary>
      /// 
      /// </summary>
      /// <param name="type"></param>
      /// <param name="mgMenu"></param>
      public MenuEntry(MenuType type, MgMenu mgMenu)
      {
         setType(type);
         setParentMgMenu(mgMenu);
         //instantiatedMenuItems = new Hashtable();
         _index = -1;
      }

      #region ICloneable Members

      /// <summary>
      ///   returns the clone of the object.
      /// </summary>
      /// <returns></returns>
      public Object Clone()
      {
         MenuEntry menuentry = (MenuEntry) MemberwiseClone();

         //All initiated menus (pulldown, context and tool) references should not be copied in new cloned 
         //object because for creation of new menu in menuAdd function, we need these values diffrent than the actual menu.
         //So, call base.init() to re-initialize them.
         base.init();
         getDeepCopyOfMenuState();
         //menuentry.instantiatedMenuItems = new Hashtable();

         return menuentry;
      }

      #endregion

      public void setIndex(int Idx)
      {
         _index = Idx;
      }

      public int getIndex()
      {
         return _index;
      }

      public void setName(String menuName)
      {
         _name = menuName;
      }

      public String getName()
      {
         return _name;
      }

      internal virtual bool ShouldSetModal(bool isModal, bool mainContextIsModal)
      {
         return false;
      }

      /// <summary>
      ///   sets the text of menu entry.
      /// </summary>
      /// <param name = "menuText">text to be set.
      /// </param>
      /// <param name = "refresh">refresh will decide if gui refresh is to be performed or not.
      /// </param>
      public void setText(String menuText, bool refresh)
      {
         _text = menuText;
         if (refresh)
         {
            TextMLS = Events.Translate(_text);

            ICollection mnuRefs = getInstantiatedMenus(false);
            addAllItemRefsToCmdQueue(mnuRefs, CommandType.PROP_SET_TEXT, TextMLS);
         }
      }

      /// <summary>
      ///   Refresh the entry text.
      ///   The method takes the existing text and put it through another translation (we get here when language data changes).
      ///   If the new translated text is different then the existing one, create set_text command for each instance.
      /// </summary>
      public void refreshText()
      {
         String newTextMls = null;
         if (_text != null)
         {
            newTextMls = Events.Translate(_text);

            // refresh only if the value changes
            if (!(newTextMls.Equals(TextMLS)))
            {
               TextMLS = newTextMls;
               ICollection mnuRefs = null;

               // get all instances not including toolbar. Tooltip is handled elsewhere.
               mnuRefs = getInstantiatedMenus(false);

               if (mnuRefs.Count > 0)
                  addAllItemRefsToCmdQueue(mnuRefs, CommandType.PROP_SET_TEXT, TextMLS);
            }
         }
      }

      public void setParentMgMenu(MgMenu mgMenu)
      {
         _parentMgMenu = mgMenu;
      }

      /// <summary>
      ///   Set ParentRootMgMenu to menuEntry. If menuEntry is MenuEntryMenu, then set ParentRootMgMenu to it's sub menus also.
      /// </summary>
      public void setParentRootMgMenu (MgMenu mgMenu)
      {
         setParentMgMenu(mgMenu);
         if (this is MenuEntryMenu)
         {
            for (int i = 0; i < ((MenuEntryMenu)this).subMenus.Count; i++)
            {
               (((MenuEntryMenu)this).subMenus[i]).setParentRootMgMenu(mgMenu);
               (((MenuEntryMenu)this).subMenus[i]).ParentMenuEntry = this;
            }
         }
      }

      public void setData(MenuType type, String menuName, String menuText, MgMenu mgMenu)
      {
         setType(type);
         setName(menuName);
         setText(menuText, true);
         setParentMgMenu(mgMenu);
      }

      /// <summary>
      ///   check/uncheck menu entry.
      /// </summary>
      /// <param name = "value">check/uncheck value.
      /// </param>
      /// <param name = "refresh">It will decide if gui refresh is to be performed or not.
      /// </param>
      public void setChecked(bool value, bool refresh)
      {
         _menuState.Checked = value;
         if (refresh)
         {
            ICollection mnuRefs = getInstantiatedMenus(true);
            addAllRefsToCmdQueue(mnuRefs, CommandType.PROP_SET_CHECKED, value);
         }
      }

      /// <summary>
      ///   SetModalDisabled().
      /// </summary>
      /// <param name = "val"> </param>
      public override void setModalDisabled(bool val)
      {
         if (this is MenuEntryMenu)
            for (int i = 0; i < ((MenuEntryMenu)this).subMenus.Count; i++)
               (((MenuEntryMenu)this).subMenus[i]).setModalDisabled(val);

         base.setModalDisabled(val);
      }

      public void setVisible(bool visible, bool setPropOnly, bool pullDownMenu, TaskBase topMostTask)
      {
         setVisible(visible, setPropOnly, pullDownMenu, topMostTask, null);
      }

#if !PocketPC
      /// <summary>
      ///   Create/Delete menu item on visibility = true/false
      /// </summary>
      /// <param name = "visible">
      /// </param>
      /// <param name = "setPropOnly">-
      ///   only initialize the property - do not try to create\deleet the matching objects This should be
      ///   true when we initialize the menu entry object data through the
      /// </param>
      public void setVisible(bool visible, bool setPropOnly, bool pullDownMenu, TaskBase topMostTask,
                               MgFormBase sendForm)
      {
         bool prevState = _menuState.Visible;
         _menuState.Visible = visible;
         if (!pullDownMenu)
            return;

         if (!setPropOnly)
         {
            if (topMostTask == null || topMostTask.isMainProg())
            {
               //eventTask can be null, if we are here from TP/TS of MP. In that case, topMostTask should MP.
               TaskBase eventTask = (TaskBase)Events.GetCurrentTask();
               if (eventTask != null)
                  topMostTask = eventTask;
            }

            MgFormBase formOrg = null;
            MgFormBase form = null;
            if (sendForm == null)
            {
               formOrg = topMostTask.getTopMostForm();
               form = topMostTask.getTopMostForm().getTopMostFrameForm();
               // fixed bug#:773382, when there is no SDI\MDI need to get the corg form (for the context menu)
               form = (form != null
                          ? form
                          : formOrg);
            }
            else
               formOrg = form = sendForm;

            if (menuType() == MenuType.MENU)
            {
               for (int i = 0;
                    i < ((MenuEntryMenu)this).subMenus.Count;
                    i++)
               {
                  MenuEntry subMenuEntry = (((MenuEntryMenu)this).subMenus[i]);
                  // Refresh the submenu-entries, in order to show/hide the toolbar icons of submenus when Parent Menu is shown/hidden,
                  // call setVisible() for submenus.
                  subMenuEntry.setVisible(subMenuEntry.getVisible(), setPropOnly, pullDownMenu, topMostTask, sendForm);
               }
            }

            if (visible)
               visible = CheckIfParentItemVisible(this);

            ICollection mnuRefs = getInstantiatedMenus(form, true, true, true);
            addAllRefsToCmdQueue(mnuRefs, CommandType.PROP_SET_VISIBLE, visible);

            if (pullDownMenu)
               Commands.addAsync(CommandType.UPDATE_MENU_VISIBILITY, form);
         }

      }
#else
      /// <summary>
      ///   Create/Delete menu item on visibility = true/false
      /// </summary>
      /// <param name = "visible">
      /// </param>
      /// <param name = "setPropOnly">-
      ///   only initialize the property - do not try to create\deleet the matching objects This should be
      ///   true when we initialize the menu entry object data through the
      /// </param>
      public void setVisible(bool visible, bool setPropOnly, bool pullDownMenu, TaskBase topMostTask,
                               MgFormBase sendForm)
      {
         bool prevState = _menuState.Visible;
         _menuState.Visible = visible;
         if (!pullDownMenu)
            return;
         MenuReference menuReference = null;

         if (!setPropOnly)
         {
            if (topMostTask == null || topMostTask.isMainProg())
            {
               //eventTask can be null, if we are here from TP/TS of MP. In that case, topMostTask should MP.
               TaskBase eventTask = (TaskBase)Events.GetCurrentTask();
               if (eventTask != null)
                  topMostTask = eventTask;
            }

            MgFormBase formOrg = null;
            MgFormBase form = null;
            if (sendForm == null)
            {
               formOrg = topMostTask.getTopMostForm();
               form = topMostTask.getTopMostForm().getTopMostFrameForm();
               // fixed bug#:773382, when there is no SDI\MDI need to get the corg form (for the context menu)
               form = (form != null
                          ? form
                          : formOrg);
            }
            else
               formOrg = form = sendForm;

            if (!prevState && visible)
            {
               resetIndexes();
               if (ParentMenuEntry != null)
               {
                  if (pullDownMenu)
                  {
                     menuReference = ParentMenuEntry.getInstantiatedMenu(form, MenuStyle.MENU_STYLE_PULLDOWN);
                     createMenuEntryObject(menuReference, MenuStyle.MENU_STYLE_PULLDOWN, form, true);
                  }
               }
               else
               {
                  if (pullDownMenu)
                     createMenuEntryObject(_parentMgMenu, MenuStyle.MENU_STYLE_PULLDOWN, form, true);
               }
            }
            else if (prevState && !visible)
            {
               resetIndexes();
               if (pullDownMenu)
                  deleteMenuEntryObject(form, MenuStyle.MENU_STYLE_PULLDOWN);
            }

            if (pullDownMenu)
               Commands.addAsync(CommandType.UPDATE_MENU_VISIBILITY, form);
         }
      }
#endif
      /// <summary>
      ///   reset the index on the menu entry
      /// </summary>
      /// <param name = "resetIndexes"></param>
      private void resetIndexes()
      {
         // refresh the set index to reflect the change
         if (ParentMenuEntry != null && ParentMenuEntry.menuType() == MenuType.MENU)
            ((MenuEntryMenu) ParentMenuEntry).setIndexes(false);
         else
            _parentMgMenu.setIndexes(false);
      }

      /// <summary>
      /// </summary>
      /// <param name = "enabled">
      /// </param>
      /// <param name = "checkValidation:">to check the validation of the set value
      ///   @return : error number if the method was faild.
      /// </param>
      public void setEnabled(bool enabled, bool checkValidation, bool checkEnableSystemAction)
      {
         setEnabled(enabled, checkValidation, checkEnableSystemAction, null, true);
      }

      /// <summary>
      /// </summary>
      /// <param name = "enabled">
      /// </param>
      /// <param name = "checkValidation:">to check the validation of the set value
      ///   @return : error number if the method was faild.
      /// </param>
      /// <param name = "refresh">refresh will decide if gui refresh is to be performed or not.
      /// </param>
      public void setEnabled(bool enabled, bool checkValidation, bool checkEnableSystemAction, String checkForName, bool refresh)
      {
         setEnabled(enabled, checkValidation, checkEnableSystemAction, checkForName, false, refresh);
      }

      /// <summary>
      /// </summary>
      /// <param name = "enabled">
      /// </param>
      /// <param name = "checkValidation:">to check the validation of the set value
      ///   @return : error number if the method was faild.
      /// </param>
      /// <param name = "refresh">refresh will decide if gui refresh is to be performed or not.
      /// </param>
      public void setEnabled(bool enabled, bool checkValidation, bool checkEnableSystemAction, String checkForName,
                              bool IsChildOfCheckForName, bool refresh)
      {
         if (checkValidation)
         {
            //bug #:293070 If MnuEnable () is called on menu, we should disable it's menu entries as well. (in online it is bug #:927017)
            if (menuType() == MenuType.MENU)
            {
               bool NewIsChildOfCheckForName = IsChildOfCheckForName || (checkForName == null) ||
                                               (_name != null && _name.CompareTo(checkForName) == 0);

               for (int i = 0;
                    i < ((MenuEntryMenu) this).subMenus.Count;
                    i++)
               {
                  MenuEntry subMenuEntry = (((MenuEntryMenu) this).subMenus[i]);
                  subMenuEntry.setEnabled(enabled, checkValidation, checkEnableSystemAction, checkForName,
                                                     NewIsChildOfCheckForName, refresh);
               }
            }

            if (checkEnableSystemAction && this is MenuEntryEvent)
            {
               MenuEntryEvent menuEntryEvent = (MenuEntryEvent) this;
               bool notAllowDisable = ((menuEntryEvent.menuType() == MenuType.INTERNAL_EVENT) &&
                                       ((menuEntryEvent.InternalEvent < InternalInterface.MG_ACT_USER_ACTION_1) ||
                                        (menuEntryEvent.InternalEvent > InternalInterface.MG_ACT_USER_ACTION_20)));

               if (notAllowDisable)
                  return;
            }

            if (menuType() == MenuType.PROGRAM)
            {
               MenuEntryProgram mp = (MenuEntryProgram) this;
               if (mp.Idx <= 0)
                  enabled = false;
            }
            else if (menuType() == MenuType.INTERNAL_EVENT)
            {
               MenuEntryEvent me = (MenuEntryEvent) this;
               if (me.InternalEvent == 0)
                  enabled = false;
            }
            else if (menuType() == MenuType.USER_EVENT)
            {
               MenuEntryEvent me = (MenuEntryEvent) this;
               if (me.UserEvtIdx == 0)
                  enabled = false;
            }
         }

         bool activateEanble = (menuType() == MenuType.MENU) ||
                               (checkForName == null) || (_name != null && _name.CompareTo(checkForName) == 0);

         // fixed bugs#:293070 & 754704, (as we doing for online in gui_mnut.cpp \EnableMenu()
         // when try to enale\disable menu entry item we need to set the comman type without to update the member Enable
         if (IsChildOfCheckForName)
         {
            if (refresh)
            {
               ICollection mnuRefs = getInstantiatedMenus(null, true, true, true);

               if (enabled)
               {
                  //try to enable menu that is not enabled
                  if (!_menuState.Enabled)
                     enabled = false;
               }

               addAllRefsToCmdQueue(mnuRefs, CommandType.PROP_SET_ENABLE, enabled);
            }
         }
         else if (activateEanble)
         {
            if (!getModalDisabled())
               _menuState.Enabled = enabled;

            if (refresh)
            {
               ICollection mnuRefs = getInstantiatedMenus(true);
               addAllRefsToCmdQueue(mnuRefs, CommandType.PROP_SET_ENABLE, enabled);
            }
         }
      }

      /// <summary>
      ///   Add menu references to command queue.
      /// </summary>
      /// <param name = "mnuRefs">Collection<MenuReference> containing menu refernces to be added to command queue
      /// </param>
      /// <param name = "cmdType">Command Type can be PROP_SET_CHECKED, PROP_SET_ENABLE, PROP_SET_VISABLE
      /// </param>
      /// <param name = "value">boolean
      /// </param>
      private void addAllRefsToCmdQueue(ICollection mnuRefs, CommandType cmdType, Object val)
      {
         if (mnuRefs != null)
         {
            IEnumerator imnuref = mnuRefs.GetEnumerator();
            while (imnuref.MoveNext())
            {
               MenuReference mnuRef = (MenuReference) imnuref.Current;
               Commands.addAsync(cmdType, mnuRef, this, val);
               Commands.beginInvoke();
            }
         }
      }

      protected internal void createEnableCmd(MgFormBase form, MenuStyle menuStyle, bool enable)
      {
         MenuReference menuReference = getInstantiatedMenu(form, menuStyle);
         Commands.addAsync(CommandType.PROP_SET_ENABLE, menuReference, this, enable);
      }

      /// <summary>
      ///   PROP_SET_MENU_ENABLE DELETE_MENU
      /// </summary>
      /// <param name = "mnuRefs">
      /// </param>
      /// <param name = "cmdType">
      /// </param>
      /// <param name = "value">
      /// </param>
      private void addAllItemRefsToCmdQueue(ICollection mnuRefs, CommandType cmdType, Object val)
      {
         if (mnuRefs != null)
         {
            IEnumerator imnuref = mnuRefs.GetEnumerator();
            while (imnuref.MoveNext())
            {
               MenuReference mnuRef = (MenuReference) imnuref.Current;
               if (mnuRef != null)
               {
                  Commands.addAsync(cmdType, mnuRef, this, val);
                  Commands.beginInvoke();
               }
               else
                  Debug.Assert(false);
            }
         }
      }


      /// <summary>
      ///   This method creates the gui commands in order to create the matching menu object. It creates the whole
      ///   object – with the sub menus.
      /// </summary>
      /// <param name = "menuStyle">
      /// </param>
      /// <param name = "form">
      /// </param>
      public void createMenuEntryObject(Object parentMenuObject, MenuStyle menuStyle, MgFormBase form,
                                          bool callFromMenuShowFunction)
      {
#if  PocketPC
         if (getVisible())
#endif
         {
            int i;
            bool hasTool = false;
            // if the two properties :DisplayContext & DisplayPullDown are false not need to perform the create
            // loop .
            // in case the MenuStyle is context, we perform the loop.
            if (menuStyle == MenuStyle.MENU_STYLE_CONTEXT || form.ShouldShowPullDownMenu || form.ShouldCreateToolbar)
            {
               MenuStyle createMenuStyle = menuStyle;
               bool createSWTmenu = false;

               // when the pulldown menu isn't display the create loop is done for all its children
               // so it mean the toolbar need to be created so we call the method setMenuIsInstantiated with the
               // style MENU_STYLE_TOOLBAR
               if (menuStyle == MenuStyle.MENU_STYLE_PULLDOWN && !form.ShouldShowPullDownMenu)
                  createMenuStyle = MenuStyle.MENU_STYLE_TOOLBAR;
               else
                  createSWTmenu = (menuStyle == MenuStyle.MENU_STYLE_CONTEXT ||
                                   (menuStyle == MenuStyle.MENU_STYLE_PULLDOWN && form.ShouldShowPullDownMenu));
               MenuReference menuReference = null;
               if (!(String.IsNullOrEmpty(TextMLS) && menuType() == MenuType.WINDOW_MENU_ENTRY))
                  menuReference = setMenuIsInstantiated(form, createMenuStyle);

               //After #984822, Gui.low expects the local file path.
               //Since long (even before #984822), if the physical image path was given, the image is 
               //searched in the local file system. I suspect that it should be treated as a server path.
               //A new QCR should be opened for this and till then, only the URLs should be converted to the local path. 
               if (!String.IsNullOrEmpty(ImageFile) && !ImageFile.StartsWith("@")
                   && Misc.isWebURL(ImageFile, Manager.Environment.ForwardSlashUsage))
                  ImageFile = Events.GetLocalFileName(ImageFile, form.getTask());

               // If this is called from menu function then CREATE_MENU_ITEM should use new _index instead of orgIndex.
               // set the form's pulldown and context menus
               if (createSWTmenu)
               {
                  // It may happen that language is changed by SetLang (). So, get translated text before creating menu entry.
                  TextMLS = Events.Translate(_text);
                  if (!(String.IsNullOrEmpty(TextMLS) && menuType() == MenuType.WINDOW_MENU_ENTRY))
                     Commands.addAsync(CommandType.CREATE_MENU_ITEM, parentMenuObject, menuStyle, this, form,
                    getIndex());
               }
               if (menuType() != MenuType.SEPARATOR && menuStyle == MenuStyle.MENU_STYLE_PULLDOWN)
               {
                  if (form.ShouldCreateToolbar)
                     hasTool = createMenuEntryTool(form, callFromMenuShowFunction);
               }
               if (this is MenuEntryMenu)
               {
                  //MenuReference menuItemReference = setInstantiatedMenuItem(form, menuStyle);
                  for (i = 0; i < ((MenuEntryMenu)this).subMenus.Count; i++)
                     (((MenuEntryMenu)this).subMenus[i]).createMenuEntryObject(menuReference, menuStyle, form, callFromMenuShowFunction);
               }
               if (this is MenuEntryEvent)
               {
                  // add the menuentry to the list of objects which appear on the menu bar and or the tool bar
                  if (createSWTmenu)
                     _parentMgMenu.addEntryToInternalEventsOnMenus((MenuEntryEvent)this);
                  if (hasTool)
                     _parentMgMenu.addEntryToInternalEventsOnToolBar((MenuEntryEvent)this);
               }
               if (AccessKey != null)
                  _parentMgMenu.addEntryToMenuEntriesWithAccessKey(this);
            }
         }
      }

#if !PocketPC
      /// <summary>
      ///   Create Menu Objects for Newly added menuentries
      /// </summary>
      /// <param name = "parentMenuObject"></param>
      /// <param name = "form"></param>
      public void CreateNewlyAddedMenus(object parentMenuObject, MgFormBase form)
      {
         resetIndexes();
         createMenuEntryObject(parentMenuObject, MenuStyle.MENU_STYLE_PULLDOWN, form, false);
      }
#endif

      /// <summary>
      ///   This method creates the gui commands in order to delete the matching menu object.
      /// </summary>
      /// <param name = "menuStyle"></param>
      public void deleteMenuEntryObject(MgFormBase form, MenuStyle menuStyle)
      {
         if (this is MenuEntryMenu)
         {
            //MenuReference menuItemReference = getInstantiatedMenuItem(form, menuStyle);
            for (int i = 0;
                 i < ((MenuEntryMenu)this).subMenus.Count;
                 i++)
            {
               MenuEntry subMenuEntry = (((MenuEntryMenu)this).subMenus[i]);
               subMenuEntry.deleteMenuEntryObject(form, menuStyle);
            }

            //if (menuItemReference != null && ((form.ShouldShowPullDownMenu && menuStyle == MenuStyle.MENU_STYLE_PULLDOWN) ||
            //                                  (menuStyle == MenuStyle.MENU_STYLE_CONTEXT)))
            //{
            //   cmdQueue.add(CommandType.DELETE_MENU, menuItemReference, this, true);
            //   cmdQueue.execute();
            //}
         }

         deleteMenuEntryObjectItem(form, menuStyle);
         //      deleteMenuEntryObjectItems(parentMenuObject, MenuStyle.MENU_STYLE_CONTEXT);
         if (menuStyle == MenuStyle.MENU_STYLE_PULLDOWN && form.ShouldCreateToolbar)
            deleteMenuEntryTool(form, true, false);
      }

      /// <summary>
      ///   This method creates the gui commands in order to delete the matching menu object.
      /// </summary>
      /// <param name = "menuStyle"></param>
      public void deleteMenuEntryObjectItem(MgFormBase form, MenuStyle menuStyle)
      {
         //we call this method for menuEntry and MenuStyle(context\pulldown)
         //when create toolbar without pulldown the Reference is null
         MenuReference menuReference = getInstantiatedMenu(form, menuStyle);
         if (menuReference != null)
            Commands.addAsync(CommandType.DELETE_MENU_ITEM, menuReference, menuStyle, this);
      }

      /**
         * get the tool index for method menuShow.
         * we pass all the menu entry in the MgMenu and calculate the index of the tool.
         * @param form : the form that we work on it
         * @param toolGroup: the tool group that this icon need to be added.
         * @forMenuEntry: calculate the tool index for this menu entry
         * @return
         */

      private int calcToolbarIndex(MgFormBase form, int toolGroup, MenuEntry forMenuEntry)
      {
         int count = 0;
         MgMenu mgMenu = form.getMgMenu(MenuStyle.MENU_STYLE_PULLDOWN);
         bool found = false;

         IEnumerator iMenuEntry = mgMenu.iterator();
         while (iMenuEntry.MoveNext())
         {
            MenuEntry menuEntry = (MenuEntry) iMenuEntry.Current;
            //get the count from this menu recursively 
            count += menuEntry.getGroupCount(form, toolGroup, forMenuEntry, ref found);

            if (found)
               break;
         }

         return count;
      }

      /**
         * get the tool index for method menuShow - recursively.
         * we pass all the menu entry in this menu entry calculate the index of the tool.
         * @param form : the form that we work on it
         * @param mgValue TODO
         * @param toolGroup: the tool group that this icon need to be added.
         * @forMenuEntry: calculate the tool index for this menu entry
         * @mgValue: mgValue.bool , will return true if stop the loop
         * @return
         */

      private int getGroupCount(MgFormBase form, int toolGroup, MenuEntry forMenuEntry, ref bool found)
      {
         int count = 0;

         if (this == forMenuEntry)
         {
            found = true;
            return count;
         }

         if (this is MenuEntryMenu)
         {
            MenuEntryMenu menuEntryMenu = ((MenuEntryMenu) this);
            for (int i = 0;
                 i < menuEntryMenu.subMenus.Count;
                 i++)
            {
               MenuEntry subMenuEntry = (menuEntryMenu.subMenus[i]);
               count += subMenuEntry.getGroupCount(form, toolGroup, forMenuEntry, ref found);
               if (found)
                  break;
            }
         }
         else
         {
            //if this menu is on same tool group of the sendMenuEntry and it is before the icon of this menu entry
            if (inSameToolGroup(toolGroup))
            {
               if ((ParentMenuEntry != forMenuEntry.ParentMenuEntry) ||
                   (getIndex() < forMenuEntry.getIndex()))
                  count++;
               else
                  Debug.Assert(false);
            }
         }
         return count;
      }

      /**
         * check if tool is define for menu entry and it is on the same tool group and it is visible 
         * @param toolGroup
         * @return
         */

      private bool inSameToolGroup(int toolGroup)
      {
         return (ImageGroup == toolGroup && toolIsDefined() && getVisible());
      }


      /// <summary>
      ///   This method creates the GUI command for the creation of the menu’s tool, as a child of the MgMenu’s
      ///   toolbar object. Toolbar will be placed as the parentObject.
      /// 
      ///   Note: This method is internal in order to allow the MgMenu object to create the tool for the group
      ///   separator.
      /// </summary>
      /// <param name = "form">form on which we create the tool on</param>
      /// <returns> true if a tool was defined</returns>
      public bool createMenuEntryTool(MgFormBase form, bool calcToolbarIndexParam)
      {
         bool hasTool = false;
         if (toolIsDefined() || menuType() == MenuType.SEPARATOR)
         {
            setMenuIsInstantiated(form, MenuStyle.MENU_STYLE_TOOLBAR);
            // create the gui command for the tool
            Object toolbar = _parentMgMenu.createAndGetToolbar(form);
            // add the tool to the matching group and get the desired index
            bool createSepAtTheEnd = _parentMgMenu.checkStartSepForGroup(form, ImageGroup, menuType());

            int toolbarIndexForFuntion = 0;
            int toolbarIndex = 0;
            if (calcToolbarIndexParam)
            {
               //calc the index of the new tool
               toolbarIndexForFuntion = calcToolbarIndex(form, ImageGroup, this);
               for (int i = 0;
                    i < ImageGroup;
                    i++)
                  toolbarIndexForFuntion += form.getToolbarGroupCount(i);
            }
            //add the new tool to the group
            toolbarIndex = _parentMgMenu.addToolToGroup(form, ImageGroup, menuType());

            //if calcToolbarIndexParam is TRUE then the tool will be add to the calc index 
            // otheriwse we will add the tool to the end if the group
            // toolbarIndex is the last index in the visible group, so if toolbarIndexForFuntion > toolbarIndex we
            // should not set toolbarIndex with toolbarIndexForFuntion. We use the index as an index in the cmd to add the item in the gui
            // and if the index is too big we will get exception. 
            if (calcToolbarIndexParam && toolbarIndexForFuntion < toolbarIndex)
               toolbarIndex = toolbarIndexForFuntion;

            Commands.addAsync(CommandType.CREATE_TOOLBAR_ITEM, toolbar, form, this, toolbarIndex);

            if (createSepAtTheEnd)
               _parentMgMenu.checkEndtSepForGroup(form, ImageGroup, menuType());
            hasTool = true;
         }
         return hasTool;
      }

      /// <summary>
      /// </summary>
      /// <param name = "form">
      /// </param>
      /// <param name = "removeSeperat">TODO
      /// </param>
      public void deleteMenuEntryTool(MgFormBase form, bool removeSeperat, bool fourceDelete)
      {
         if (toolIsDefined() || fourceDelete)
         {
            Object toolbar = _parentMgMenu.createAndGetToolbar(form);
            Commands.addAsync(CommandType.DELETE_TOOLBAR_ITEM, toolbar, form, this, 0);

            form.removeToolFromGroupCount(ImageGroup, removeSeperat);
         }
      }

      private bool toolIsDefined()
      {
         return ((menuType() != MenuType.MENU &&
                  (Imagefor == ImageFor.MENU_IMAGE_TOOLBAR || Imagefor == ImageFor.MENU_IMAGE_BOTH) &&
                  (ImageFile != null || ImageNumber > 0)));
      }

      public MgMenu getParentMgMenu()
      {
         return _parentMgMenu;
      }


      /// <summary>
      /// 
      /// </summary>
      internal virtual void dispose()
      {
         MgFormBase form = null;

         MenuReference menuReference;
         // dispose all pulldown menu objects
         ICollection pulldownKeys = _instantiatedPullDown.Keys;
         IEnumerator pulldownKeysEnumerator = pulldownKeys.GetEnumerator();

         if (menuType() == MenuType.WINDOW_MENU_ENTRY)
            return;

         while (pulldownKeysEnumerator.MoveNext())
         {
            form = (MgFormBase) pulldownKeysEnumerator.Current;
            menuReference = (MenuReference) _instantiatedPullDown[form];
            Commands.addAsync(CommandType.DISPOSE_OBJECT, menuReference);
         }

         // dispose all context menu objects
         ICollection contextKeys = _instantiatedContext.Keys;
         IEnumerator contextKeysEnumerator = contextKeys.GetEnumerator();

         while (contextKeysEnumerator.MoveNext())
         {
            form = (MgFormBase) contextKeysEnumerator.Current;
            menuReference = (MenuReference) _instantiatedContext[form];
            Commands.addAsync(CommandType.DISPOSE_OBJECT, menuReference);
         }

         // dispose all toolbar menu objects
         ICollection toolbarKeys = _instantiatedToolItem.Keys;
         IEnumerator toolbarKeysEnumerator = toolbarKeys.GetEnumerator();

         while (toolbarKeysEnumerator.MoveNext())
         {
            form = (MgFormBase) toolbarKeysEnumerator.Current;
            deleteMenuEntryTool(form, true, false);
         }
      }

      /// <summary>
      ///   Get the menu entry's prompt (relevant for event, program and OSCommand.
      /// </summary>
      /// <returns> - the entry's prompt, null in case of none event,program, os entry.</returns>
      public virtual String getPrompt()
      {
         String prompt = null;
         if (this is MenuEntryEvent)
            prompt = ((MenuEntryEvent)this).Prompt;
         else if (this is MenuEntryOSCommand)
            prompt = ((MenuEntryOSCommand)this).Prompt;
         else if (this is MenuEntryProgram)
            prompt = ((MenuEntryProgram)this).Prompt;

         return (prompt);
      }

      /// <summary>
      /// Create a menuentry for WindowList items.
      /// </summary>
      /// <param name="mgFormBase">Form associated with windowmenu entry</param>
      /// <param name="menuType">WindowMenu/Separator</param>
      /// <param name="guiMgForm"></param>
      /// <param name="menuStyle">pulldown/context</param>
      /// <param name="bChecked">menuentry should be checked or not</param>
      /// <returns></returns>
      public MenuEntry CreateMenuEntryItem(MgFormBase mgFormBase, MenuType menuType, GuiMgForm guiMgForm, MenuStyle menuStyle, bool bChecked)
      {
         MenuEntry menuEntry = null;

         if (menuType == MenuType.WINDOW_MENU_ENTRY)
         {
            menuEntry = new MenuEntryWindowMenu(getParentMgMenu());
            ((MenuEntryWindowMenu)menuEntry).SetForm(mgFormBase);

            //String menuText = mgFormBase.getProp(PropInterface.PROP_TYPE_FORM_NAME).getValue();
            //windowMenuEntry.setText(menuText, false);
            //windowMenuEntry.TextMLS = Events.Translate(menuText);
            menuEntry.setEnabled(true, false, false);
            menuEntry.setChecked(bChecked, false);
         }
         else if (menuType == MenuType.SEPARATOR)
         {
            menuEntry = new MenuEntry(menuType, getParentMgMenu());
         }

         menuEntry.setVisible(true, true, false, null);
         menuEntry.setMenuIsInstantiated(guiMgForm, menuStyle);

         return menuEntry;
      }

      /// <summary>
      /// An empty virtual method. Implemented in MenuEntryWindowMenu and MenuEntryMenu used for ContextMenu and PullDownMenu respectively.
      /// </summary>
      /// <param name="mgFormBase"></param>
      /// <param name="menuType"></param>
      /// <param name="windowMenuIdx"></param>
      /// <param name="guiMgForm"></param>
      /// <param name="menuStyle"></param>
      /// <param name="setChecked"></param>
      public virtual void CreateMenuEntry(MgFormBase mgFormBase, MenuType menuType, int windowMenuIdx, GuiMgForm guiMgForm, MenuStyle menuStyle, bool setChecked)
      {
      }
   }
}
