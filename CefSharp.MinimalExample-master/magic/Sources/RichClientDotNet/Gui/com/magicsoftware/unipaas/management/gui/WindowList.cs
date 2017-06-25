using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.tasks;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary>
   /// This class will provide interface to maintain list of form that should be shown as a window list menu.
   /// It also create/delete window menu items.
   /// The order of the elements in lit will be decided by the WindowSortBy property(recently used or window creation).
   /// </summary>
   public class WindowList
   {
      private static List<MgFormBase> _windowList = new List<MgFormBase>();
      private static object _windowListLock = new Object();

      private int _currPosInList;            // Will be used by GetNext() & GetPrevious()
      private int _currWinIdx;               // Will be used by GetNext() & GetPrevious()
      private const int NOT_IN_WINDOW_LIST = -1;

      private bool _sortByRecentlyUsed;      // Sort by recently used.

      public int LastProcessedAction { get; set; } // Last processed action Next/Previous Window action from KeyBoard Mapping table.
      public bool SourceIsKbd { get; set; } // keyboard is used for navigation between windows.

      /// <summary>
      /// constructor
      /// </summary>
      internal WindowList()
      {
         LastProcessedAction = 0;
         SourceIsKbd = false;
         _currWinIdx = _currPosInList = NOT_IN_WINDOW_LIST;
      }

      /// <summary>
      /// Set sorting option for items of a list.
      /// </summary>
      /// <param name="sortByRecentlyUsed"></param>
      public void SetSortByRecentlyUsed(bool sortByRecentlyUsed)
      {
         _sortByRecentlyUsed = sortByRecentlyUsed;
      }

      /// <summary>
      /// Add an entry into a list
      /// </summary>
      /// <param name="mgForm"></param>
      public void Add(MgFormBase mgForm)
      {
         lock (_windowListLock)
         {
            if (!_windowList.Contains(mgForm))
            {
               // 1) SortByRecentlyUsed : insertion should be at first place.
               // 2) SortByCreation     : - insertion should be at first place if its a first item.
               //                         - insertion should be at last place.
               int addPos = 0;
               if (!_sortByRecentlyUsed && _windowList.Count > 0)
                  addPos = _windowList.Count;

               _windowList.Insert(addPos, mgForm);
               _currWinIdx = addPos == 0 ? 0 : _windowList.Count - 1;
               _currPosInList = _currWinIdx;
            }
         }
      }

      /// <summary>
      /// Remove an entry form list
      /// </summary>
      /// <param name="mgForm"></param>
      public void Remove(MgFormBase mgForm)
      {
         lock (_windowListLock)
         {
            if (_windowList.Contains(mgForm))
            {
               _windowList.Remove(mgForm);

               if (_currWinIdx > _windowList.Count - 1)
                  _currWinIdx = _windowList.Count - 1;

               if (_currPosInList > _windowList.Count - 1)
                  _currPosInList = _windowList.Count - 1;
            }
         }
      }

      /// <summary>
      /// Remove all entries from list
      /// </summary>
      /// <param name="mgForm"></param>
      public void RemoveAll()
      {
         _windowList.RemoveRange(0, _windowList.Count);
         _currPosInList = _currWinIdx = NOT_IN_WINDOW_LIST;
         _sortByRecentlyUsed = false;
      }

      /// <summary>
      /// get nos of entries from the list
      /// </summary>
      /// <returns></returns>
      public int GetCount()
      {
         int count = 0;
         lock (_windowListLock)
         {
            count = _windowList.Count;
         }

         return count;
      }

      /// <summary>
      /// Set a form as recent/adjust positions maintained.
      /// We will not update/sort list when the window is being activated using keyBoard (i.e. Ctrl + Tab + Tab + Tab...)
      /// The list will be updated whenever the pressed keys will be released.
      /// </summary>
      /// <param name="mgFormBase"></param>
      public void SetRecent(MgFormBase mgFormBase)
      {
         lock (_windowListLock)
         {
            int index = _windowList.IndexOf(mgFormBase);

            if (index > NOT_IN_WINDOW_LIST)
            {
               if (_sortByRecentlyUsed && !SourceIsKbd && index > 0)
               {
                  _windowList.RemoveAt(index);
                  _windowList.Insert(0, mgFormBase);
                  _currPosInList = 0;
                  _currWinIdx = 0;
               }
               else
               {
                  // If window sort is byCreation just set _currWinIdx. 
                  // And if windows are LRU but they are switch using Ctrl is kept pressed and Tab
                  // is key is pressed Ctrl + tab, tab, tab we will set CurrWinIdx_ only  and 
                  // will not update the list. It will be done when Ctrl Key is released.
                  if (index != _currWinIdx)
                  {
                     _currWinIdx = index;
                     _currPosInList = _currWinIdx; // CurrPosInList_ same as currWinIdx_
                  }
               }
            }
            else
            {
               // Since current form is not in the list CurrWinIdx will be NOT_IN_WINDOW_LIST
               // but CurrPosInList_ will represent the idx of the form which was active last
               if (_currWinIdx > NOT_IN_WINDOW_LIST)
               {
                  _currPosInList = _currWinIdx;
                  _currWinIdx = NOT_IN_WINDOW_LIST;
               }
            }
         }
      }

      /// <summary>
      /// Get Next Form to be activated from the list.
      /// </summary>
      /// <returns></returns>
      public MgFormBase GetNext()
      {
         MgFormBase nextForm = null;
         int nextWindowIdx = NOT_IN_WINDOW_LIST;

         lock (_windowListLock)
         {
            if (_windowList.Count > 0)
            {
               // If the window on which we are parked is in window list
               // then previous window will be calculated according to CurrWinIdx_
               if (_currWinIdx > NOT_IN_WINDOW_LIST)
               {
                  if (_currWinIdx == _windowList.Count - 1)
                     nextWindowIdx = 0;
                  else
                     nextWindowIdx = _currWinIdx + 1;
               }
               // If the window on which we are parked is not in window list
               // then previous window will be calculated according to 
               // last window in window list which was active. (i.e. CurrPosInList_)
               else if (_currPosInList > NOT_IN_WINDOW_LIST)
               {
                  if (_currPosInList == _windowList.Count - 1)
                     nextWindowIdx = 0;
                  else
                     nextWindowIdx = _currPosInList + 1;
               }

               Debug.Assert(nextWindowIdx != -1);
               nextForm = _windowList[nextWindowIdx];
            }
         }
         return nextForm;
      }

      /// <summary>
      /// Get Previous Form to be activated from the list.
      /// </summary>
      /// <returns></returns>
      public MgFormBase GetPrevious()
      {
         MgFormBase prevForm = null;
         int prevWindowIdx = NOT_IN_WINDOW_LIST;

         lock (_windowListLock)
         {
            if (_windowList.Count > 0)
            {
               // If the window on which we are parked is in window list
               // then previous window will be calculated according to CurrWinIdx_
               if (_currWinIdx > NOT_IN_WINDOW_LIST)
               {
                  if (_currWinIdx == 0)
                     prevWindowIdx = _windowList.Count - 1;
                  else
                     prevWindowIdx = _currWinIdx - 1;
               }
               // If the window on which we are parked is not in window list
               // then previous window will be calculated according to 
               // last window in window list which was active. (i.e. CurrPosInList_)
               else if (_currPosInList > NOT_IN_WINDOW_LIST)
               {
                  prevWindowIdx = _currPosInList;
               }

               Debug.Assert(prevWindowIdx != -1);
               prevForm = _windowList[prevWindowIdx];
            }
         }
         return prevForm;
      }

      /// <summary>
      /// Create MenuEntries for WindowList Items.
      /// </summary>
      /// <param name="guiMgForm"></param>
      /// <param name="guiMenuEntry"></param>
      /// <param name="menuStyle"></param>
      public void CreateWindowMenuEntries(GuiMgForm guiMgForm, Object guiMenuEntry, MenuStyle menuStyle)
      {
          Debug.Assert(Misc.IsGuiThread());
          bool IsMenuEntryWindowMenu = guiMenuEntry is MenuEntryWindowMenu;
          MenuEntry menuEntry;
          int windowMenuIdx = 0;

          //We get this flag as true only when the menu contains WindowList at first level and it is being used as context menu
          if (IsMenuEntryWindowMenu)
          {
              //Here we add the new WindowMenu in the parent MgMenu
              menuEntry = (MenuEntryWindowMenu)guiMenuEntry;
              windowMenuIdx = menuEntry.getParentMgMenu().GetWindowMenuEntryIndex();
          }
          else
          {
              //We add the WindowMenu in the MenuEntryMenu
              menuEntry = (MenuEntryMenu)guiMenuEntry;
              windowMenuIdx = ((MenuEntryMenu)menuEntry).GetWindowMenuEntryIndex();
          }

          // Enable/Disable MenuEntries for CloseAll, NextWindow & PreviousWindow.
          TaskBase task = ((MgFormBase)guiMgForm).getTask();
          MgMenu mgMenu = menuEntry.getParentMgMenu();
          mgMenu.enableInternalEvent((MgFormBase)guiMgForm, InternalInterface.MG_ACT_CLOSE_ALL_WIN, task.ActionManager.isEnabled(InternalInterface.MG_ACT_CLOSE_ALL_WIN), null);
          mgMenu.enableInternalEvent((MgFormBase)guiMgForm, InternalInterface.MG_ACT_NEXT_RT_WINDOW, task.ActionManager.isEnabled(InternalInterface.MG_ACT_NEXT_RT_WINDOW), null);
          mgMenu.enableInternalEvent((MgFormBase)guiMgForm, InternalInterface.MG_ACT_PREV_RT_WINDOW, task.ActionManager.isEnabled(InternalInterface.MG_ACT_PREV_RT_WINDOW), null);

          lock (_windowListLock)
          {
              if (IsValidIndex(windowMenuIdx))
              {
                  // Delete the existing MenuEntries.
                  DeleteWindowMenuEntries(menuEntry, windowMenuIdx + 1, IsMenuEntryWindowMenu);

                  // Create new MenuEntries for WindowList Items.
                  if (_windowList.Count > 0)
                  {
                      // Create menu item for a form in window list.
                      for (int i = 0; i < _windowList.Count; i++)
                      {
                          MgFormBase mgForm = _windowList[i];
                          menuEntry.CreateMenuEntry(mgForm, GuiMenuEntry.MenuType.WINDOW_MENU_ENTRY, windowMenuIdx++, guiMgForm, menuStyle, _sortByRecentlyUsed ? i == 0 : i == _currWinIdx);
                      }

                      // Create a separator to distinguish the window menu items only for PullDown menu
                      if (menuStyle != MenuStyle.MENU_STYLE_CONTEXT)
                          menuEntry.CreateMenuEntry(null, GuiMenuEntry.MenuType.SEPARATOR, windowMenuIdx++, guiMgForm, menuStyle, false);
                  }
              }
          }
      }

      /// <summary>
      /// Delete MenuEntries created for WindowList
      /// </summary>
      /// <param name="menuEntryMenu">MenuEntry containing window list menu item</param>
      /// <param name="windowMenuIdx">index from where to start deleting menuentries </param>
      private void DeleteWindowMenuEntries(MenuEntry menuEntryMenu, int windowMenuIdx, bool IsMenuEntryWindowMenu)
      {
          Debug.Assert(Misc.IsGuiThread());

          if (windowMenuIdx > 0)
          {
              int offset = windowMenuIdx;
              bool bRemoveFromSubMenu = false; // check whether we need this / not.
              List<MenuEntry> menuEntryList;

              if (IsMenuEntryWindowMenu)
                  menuEntryList = menuEntryMenu.getParentMgMenu().MenuEntries;
              else
                  menuEntryList = ((MenuEntryMenu)menuEntryMenu).subMenus;

              while (offset < menuEntryList.Count)
              {
                  MenuEntry menuEntry = menuEntryList[offset++];
                  if ((menuEntry is MenuEntryWindowMenu) ||
                     (menuEntry.menuType() == GuiMenuEntry.MenuType.SEPARATOR))
                  {
                      // Delete existing ToolStripMenuItem if exists.
                      // Window menu entries are deleted before they are created on the fly(before menu gets opened).
                      // And hence while removing them remove it from PullDown as well as Context menu.
                      ICollection mnuReference = menuEntry.getInstantiatedMenus(MenuStyle.MENU_STYLE_PULLDOWN);
                      DeleteMenuItems(mnuReference);

                      mnuReference = menuEntry.getInstantiatedMenus(MenuStyle.MENU_STYLE_CONTEXT);
                      DeleteMenuItems(mnuReference);

                      bRemoveFromSubMenu = true;
                  }
                  else
                      break;
              }

              // Remove deleted menu entries from a sub-menu.
              if (bRemoveFromSubMenu)
              {
                  menuEntryList.RemoveRange(windowMenuIdx, offset - windowMenuIdx);
                  if (IsMenuEntryWindowMenu)
                      menuEntryMenu.getParentMgMenu().setIndexes(false);
                  else
                      ((MenuEntryMenu)menuEntryMenu).setIndexes(false);
              }
          }
      }

      /// <summary>
      /// Delete all menus (ToolStripItem) corrosponds to menureferences
      /// </summary>
      /// <param name="mnuReference">collection of menuReferences</param>
      private void DeleteMenuItems(ICollection mnuReference)
      {
         Debug.Assert(Misc.IsGuiThread());
         if (mnuReference != null && mnuReference.Count > 0)
         {
            ArrayList mnuReferenceList = new ArrayList(mnuReference);
            foreach (MenuReference mnuRef in mnuReferenceList)
               Manager.DeleteMenuItem(mnuRef);
         }
      }

      /// <summary>
      /// close all the window in windowlist.
      /// </summary>
      public void CloseAllWindow()
      {
         lock (_windowListLock)
         {
            foreach (MgFormBase mgForm in _windowList)
            {
               Manager.EventsManager.addGuiTriggeredEvent(mgForm.getTask(), InternalInterface.MG_ACT_HIT);
               Manager.EventsManager.addGuiTriggeredEvent(mgForm.getTask(), InternalInterface.MG_ACT_EXIT);
            }
         }
      }

      /// <summary>
      /// Check whether the index passed is valid or not. 
      /// </summary>
      /// <param name="index"></param>
      /// <returns></returns>
      private bool IsValidIndex(int index)
      {
         return index != NOT_IN_WINDOW_LIST;
      }

      /// <summary>
      ///  returns the current form in the windowlist
      /// </summary>
      private MgFormBase GetCurrent()
      {
         MgFormBase currentMgForm = null;
         if (_currWinIdx < _windowList.Count && _currWinIdx > NOT_IN_WINDOW_LIST)
            currentMgForm = _windowList[_currWinIdx];
         // If the window on which we are parked is not in window list
         // then previous window will be calculated accoring to 
         // last window in window list which was active. (i.e. CurrPosInList_)
         else if (_currPosInList < _windowList.Count && _currPosInList > NOT_IN_WINDOW_LIST)
            currentMgForm = _windowList[_currPosInList];

         return currentMgForm;
      }

      /// <summary>
      /// Find and return form using formName.
      /// </summary>
      /// <param name="formName"></param>
      /// <returns></returns>
      public MgFormBase GetByName(String formName)
      {
         MgFormBase mgForm = null;

         lock (_windowListLock)
         {
            foreach (MgFormBase mgFormBase in _windowList)
            {
               Property prop = mgFormBase.GetComputedProperty(PropInterface.PROP_TYPE_FORM_NAME);
               if (prop != null && formName == StrUtil.rtrim(prop.GetComputedValue()))
               {
                  mgForm = mgFormBase;
                  break;
               }
            }
         }

         return mgForm;
      }
   }
}
