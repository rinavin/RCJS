using System;
using System.Collections;
using System.Collections.Generic;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.tasks;

namespace com.magicsoftware.unipaas.management.gui
{
   public class ApplicationMenus
   {
      internal List<MgMenu> menus;

      /// <summary>
      ///   This method initializes the Menus vector from the menus source file. This method reads the data from the
      ///   menus source file, creates a MenuEntry object for each entry in the menus file, and connects a menu
      ///   entry with its children. In this method we will use the SAX Parser in order to parse the menus file xml.
      ///   menusFileURL – the name of the menus file
      /// </summary>
      /// <param name = "menusData">the menus data buffer</param>
      public ApplicationMenus(byte[] menusData)
      {
         menus = new List<MgMenu>();

         try
         {
            if (menusData != null && menusData.Length > 0)
            {
               ApplicationMenusSaxHandler handler = new ApplicationMenusSaxHandler(menus);
               MgSAXParser mgSAXParser = new MgSAXParser(handler);
               mgSAXParser.parse(menusData);
            }
         }
         catch (Exception ex)
         {
            Events.WriteExceptionToLog(ex);
         }
      }

      /// <summary>
      ///   This method returns an MgMenu object according to passed menuIndex
      /// </summary>
      /// <param name = "menuIndex"></param>
      /// <returns> matching MgMenu object</returns>
      public MgMenu getMgMenu(int menuIndex)
      {
         MgMenu mgMenu = null;
         if (menuIndex > 0 && menus.Count >= menuIndex)
            mgMenu = menus[menuIndex - 1];
         return (mgMenu);
      }

      public IEnumerator iterator()
      {
         return menus.GetEnumerator();
      }

      /// <summary>
      ///   Returns all menu entries matching the name in this applicaiton menu
      /// </summary>
      /// <param name="entryName">Sring Entry name to be searched</param>
      /// <param name="pulldownMenu">Current pulldown menu</param>
      /// <returns>List of MenuEntries matching the entry Name</returns>
      public ArrayList GetMatchingMenuValues(String entryName, MgMenu pulldownMenu)
      {
         ArrayList matchingMnuValues = new ArrayList(); // Can't use List<T> - may hold MenuEntry or MgValue
         IEnumerator iMgMenu = menus.GetEnumerator();

         while (iMgMenu.MoveNext())
         {
            bool isPulldown;

            MgMenu mgmenu = (MgMenu)iMgMenu.Current;
            if (mgmenu == pulldownMenu)
               isPulldown = true;
            else
               isPulldown = false;

            IEnumerator iMenuEntry = mgmenu.iterator();
            BuildMatchingMenuValues(entryName, iMenuEntry, isPulldown, matchingMnuValues);
         }

         matchingMnuValues.TrimToSize();
         return matchingMnuValues;
      }

      /// <summary>
      ///   Returns menu index by its name. Only searchs top level menus (like default pull down etc)
      /// </summary>
      /// <param name="menuName">Name of the menu</param>
      /// <returns> long index of the menu</returns>
      public int menuIdxByName(String menuName, bool isPublic)
      {
         int index = 1;
         IEnumerator iMgMenu = menus.GetEnumerator();
         int menuIdx = 0;

         //internal names are currently not handled.
         if (isPublic)
            return 0;

         while (iMgMenu.MoveNext())
         {
            MgMenu mgmenu = (MgMenu) iMgMenu.Current;
            String mnuName = mgmenu.getName();
            if (mnuName != null && String.CompareOrdinal(mnuName, menuName) == 0)
            {
               menuIdx = index;
               break;
            }
            index++;
         }

         return menuIdx;
      }

      /// <summary>
      ///   Gets the menu entry from its Uid.
      /// </summary>
      /// <param name = "menuUid">Uid whose corresponding menu entry is to be found</param>
      /// <returns></returns>
      public MenuEntry menuByUid(int menuUid)
      {
         MenuEntry menuEntryByUid = null;
         IEnumerator iMgMenu = menus.GetEnumerator();

         while (iMgMenu.MoveNext())
         {
            MgMenu mgmenu = (MgMenu) iMgMenu.Current;
            IEnumerator iMenuEntry = mgmenu.iterator();
            while (iMenuEntry.MoveNext())
            {
               MenuEntry menuEntry = (MenuEntry) iMenuEntry.Current;
               if (menuEntry.menuUid() == menuUid)
               {
                  menuEntryByUid = menuEntry;
                  return menuEntryByUid;
               }
               if (menuEntry.menuType() == GuiMenuEntry.MenuType.MENU)
               {
                  menuEntryByUid = getMenuEntryByUid(menuEntry, menuUid);
                  if (menuEntryByUid != null)
                  {
                     if (menuEntryByUid.menuUid() == menuUid)
                     {
                        return menuEntryByUid;
                     }
                  }
               }
            }
         }
         return menuEntryByUid;
      }

      /// <summary>
      /// </summary>
      /// <param name = "menuEntry"></param>
      /// <param name = "menuUid"></param>
      /// <returns></returns>
      private MenuEntry getMenuEntryByUid(MenuEntry menuEntry, int menuUid)
      {
         MenuEntry menuEntryByUid = null;

         if (menuEntry.menuType() == GuiMenuEntry.MenuType.MENU)
         {
            MenuEntryMenu menuEntryMenu = (MenuEntryMenu) menuEntry;
            IEnumerator iMenuEntryMenu = menuEntryMenu.iterator();
            while (iMenuEntryMenu.MoveNext())
            {
               MenuEntry menuEntryNext = (MenuEntry) iMenuEntryMenu.Current;
               if (menuEntryNext.menuUid() == menuUid)
               {
                  return menuEntryNext;
               }
               if (menuEntryNext.menuType() == GuiMenuEntry.MenuType.MENU)
               {
                  menuEntryByUid = getMenuEntryByUid(menuEntryNext, menuUid);
                  if (menuEntryByUid != null)
                  {
                     if (menuEntryByUid.menuUid() == menuUid)
                     {
                        return menuEntryByUid;
                     }
                  }
               }
            }
         }
         return menuEntryByUid;
      }

      /// <summary>
      ///   Builds actual array list containing menu entries matching the entry name
      /// </summary>
      /// <param name = "EntryName">Sring Entry name to be searched</param>
      /// <param name = "menuEntry">Root menu entry</param>
      /// <param name = "matchingMnuEntries">Out parameter that will have the matching entries.</param>
      /// <param name="isPulldown"></param>
      /// <returns></returns>
      private void BuildMatchingMenuValues(String entryName, IEnumerator iInnerMnt, bool isPulldown, ArrayList matchingMnuEntries)
      {
         while (iInnerMnt.MoveNext())
         {
            MenuEntry innerMnt = (MenuEntry)iInnerMnt.Current;

            String mntName = innerMnt.getName();
            if (mntName != null && (String.CompareOrdinal(mntName, entryName) == 0))
               AddMenuValue(matchingMnuEntries, isPulldown, innerMnt);

            if (innerMnt.menuType() == GuiMenuEntry.MenuType.MENU)
            {
               MenuEntryMenu menuEntMenu = (MenuEntryMenu)innerMnt;
               IEnumerator iMenuEntry = menuEntMenu.iterator();

               BuildMatchingMenuValues(entryName, iMenuEntry, isPulldown, matchingMnuEntries);
            }
         }
      }

      /// <summary>
      ///  add entry to the matchingMnuEntries according to the sent parameters
      /// </summary>
      /// <param name="matchingMnuEntries"></param>
      /// <param name="isPulldown"></param>
      /// <param name="includeInContext"></param>
      /// <param name="innerMnt">the menuEntry</param>
      /// <returns></returns>
      private void AddMenuValue(ArrayList matchingMnuEntries,
                                         bool isPulldown, MenuEntry innerMenuEntry)
      {
         MenuValue menuValue = new MenuValue()
         {
            IsPulldown = isPulldown,
            InnerMenuEntry = innerMenuEntry
         };

         matchingMnuEntries.Add(menuValue);
      }

      /// <summary>
      /// 
      /// </summary>
      public void destroyAndRebuild()
      {
         for (int i = 0; i < menus.Count; i++)
         {
            MgMenu menu = menus[i];
            menu.destroyAndRebuild();
         }
      }

      /// <summary>
      ///   Context menus are not automatically disposed when a form is disposed (unlike the swt version).
      ///   For every menu , dispose all its instances for the disposing form.
      /// </summary>
      /// <param name = "form"></param>
      public void disposeFormContexts(MgFormBase form)
      {
         for (int i = 0; i < menus.Count; i++)
         {
            MgMenu menu = menus[i];
            menu.disposeFormContexts(form);
         }
      }

      /// <summary>
      ///   refresh the internal event menus of form
      /// </summary>
      /// <param name = "form"></param>
      public void refreshInternalEventMenus(MgFormBase form)
      {
         for (int i = 0; i < menus.Count; i++)
         {
            MgMenu menu = menus[i];
            menu.refreshInternalEventMenus(form);
         }
      }

      /// <summary>
      ///   Refresh all the menus text in our menu list.
      /// </summary>
      public void refreshMenuesTextMls()
      {
         IEnumerator iMgMenu = menus.GetEnumerator();
         while (iMgMenu.MoveNext())
         {
            MgMenu mgmenu = (MgMenu) iMgMenu.Current;
            IEnumerator iMenuEntry = mgmenu.iterator();
            while (iMenuEntry.MoveNext())
            {
               MenuEntry menuEntry = (MenuEntry) iMenuEntry.Current;
               refreshRecursiveMenuesEntryMls(menuEntry);
            }
         }
      }

      /// <summary>
      ///   Refresh the text of the menu entry.
      ///   If this menu entry is a menu itself, call this method recursively.
      /// </summary>
      public void refreshRecursiveMenuesEntryMls(MenuEntry menuEntry)
      {
         // 1st refresh the menuentry text
         menuEntry.refreshText();

         // for menu type entry, do a recursive call for each entry.
         if (menuEntry.menuType() == GuiMenuEntry.MenuType.MENU)
         {
            MenuEntryMenu menuEntryMenu = (MenuEntryMenu) menuEntry;
            IEnumerator iMenuEntryMenu = menuEntryMenu.iterator();
            while (iMenuEntryMenu.MoveNext())
            {
               MenuEntry menuEntryNext = (MenuEntry) iMenuEntryMenu.Current;
               // recursive call for each menu entry.
               refreshRecursiveMenuesEntryMls(menuEntryNext);
            }
         }
      }
   }
}
