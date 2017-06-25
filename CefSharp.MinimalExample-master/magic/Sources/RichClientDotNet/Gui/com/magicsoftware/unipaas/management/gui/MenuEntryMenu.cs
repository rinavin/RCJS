using System;
using System.Collections;
using System.Collections.Generic;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.gui.low;

namespace com.magicsoftware.unipaas.management.gui
{
   public class MenuEntryMenu : MenuEntry
   {
      internal List<MenuEntry> subMenus;

      public MenuEntryMenu(MgMenu mgMenu) : base(MenuType.MENU, mgMenu)
      {
         subMenus = new List<MenuEntry>();
      }

      public void addSubMenu(MenuEntry newEntry)
      {
         newEntry.ParentMenuEntry = this;
         subMenus.Add(newEntry);
      }

      public void addSubMenu(MenuEntry newEntry, int index)
      {
         newEntry.ParentMenuEntry = this;
         subMenus.Insert(index, newEntry);
      }
      
      public void removeAt (int index, MgFormBase form)
      {
         subMenus[index].deleteMenuEntryObject(form, MenuStyle.MENU_STYLE_PULLDOWN);
         subMenus.RemoveAt(index);
      }
      
      public IEnumerator iterator()
      {
         return subMenus.GetEnumerator();
      }

      internal override void dispose()
      {
         for (int i = 0; i < subMenus.Count; i++)
         {
            MenuEntry menuEntry = subMenus[i];
            menuEntry.dispose();
         }
         base.dispose();
      }

      /// <summary>
      ///   This method updates the indexes of the menu's children
      /// </summary>
      /// <param name = "drillDown">-
      ///   tells us if we need to perform the same for all sub menus, or only for the entries in the
      ///   current level
      /// </param>
      public void setIndexes(bool drillDown)
      {
         int Idx = 0;         
         IEnumerator iInnerMnt = iterator();
         while (iInnerMnt.MoveNext())
         {
            MenuEntry innerMnt = (MenuEntry)iInnerMnt.Current;
#if  !PocketPC
            innerMnt.setIndex(++Idx);
#else
            if (innerMnt.getVisible())
               innerMnt.setIndex(++Idx);
            else
               innerMnt.setIndex(-1);
#endif

            if (drillDown && innerMnt.menuType() == MenuType.MENU)
               ((MenuEntryMenu)innerMnt).setIndexes(drillDown);
         }
      }

      /// <summary>
      /// Find window list menu item and return its index in the List.
      /// </summary>
      /// <returns></returns>
      public int GetWindowMenuEntryIndex()
      {
         int windowMenuIndex = -1;
         for (int i = 0; i < subMenus.Count; i++)
         {
            MenuEntry menuEntry = subMenus[i];
            if (menuEntry is MenuEntryWindowMenu)
            {
               windowMenuIndex = i;
               break;
            }
         }
         return windowMenuIndex;
      }

      /// <summary>
      /// Create a WindowMenuEntry under a MenuEntryMenu and also create a ToolSripMenuItem using Manager.
      /// </summary>
      /// <param name="mgFormBase"></param>
      /// <param name="menuType">WindowMenu / Separator</param>
      /// <param name="windowMenuIdx">Index where new menuentry should be added</param>
      /// <param name="guiMgForm"></param>
      /// <param name="menuStyle">Pulldown / Context</param>
      /// <param name="setChecked"
      public override void CreateMenuEntry(MgFormBase mgFormBase, MenuType menuType, int windowMenuIdx, GuiMgForm guiMgForm, MenuStyle menuStyle, bool setChecked)
      {
         MenuReference menuReference = getInstantiatedMenu(guiMgForm, menuStyle);
         MenuEntry menuEntry = base.CreateMenuEntryItem(mgFormBase, menuType, guiMgForm, menuStyle, setChecked);
         addSubMenu(menuEntry, windowMenuIdx +1);
         setIndexes(false);

         // Create a corresponding ToopStripMenuItem for windowMenuEntry. 
         Manager.CreateMenuItem(menuReference, menuEntry, guiMgForm, menuStyle, menuEntry.getIndex()-1);
      }
   }
}
