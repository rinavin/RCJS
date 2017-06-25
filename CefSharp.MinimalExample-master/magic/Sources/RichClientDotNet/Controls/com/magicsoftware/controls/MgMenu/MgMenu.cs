using System;
using System.Windows.Forms;

namespace com.magicsoftware.controls.MgMenu
{
   /// <summary>MainMenu class does not support some essential functionalities in CF. This class is inheriated from MainMenu and adds those missng functionallity</summary>
   public class MgMainMenu : MainMenu
   {
      //Tag which is not supported in compact framework
      public object Tag;

      /// <summary>
      /// Inserts Menuitem at given position. This is not supported in compact framrwork
      /// </summary>
      /// <param name="index"></param>
      /// <param name="menuItem"></param>
      public void Insert(int index, MenuItem menuItem)
      {
         MenuUtils.Insert(MenuItems, index, menuItem);
      }
   }

   /// <summary>MenuItem class does not support some essential functionalities in CF. This class is inheriated from MenuItem and adds those missng functionallity/// </summary>
   public class MgMenuItem : MenuItem
   {
      //Tag which is not supported in compact framework
      public object Tag;

      /// <summary>Inserts Menuitem at given position. This is not supported in compact framrwork</summary>
      /// <param name="index"></param>
      /// <param name="menuItem"></param>
      public void Insert(int index, MenuItem menuItem)
      {
         MenuUtils.Insert(MenuItems, index, menuItem);
      }
   }

   /// <summary>ContextMenu class does not support some essential functionalities in CF. This class is inheriated from ContextMenu and adds those missng functionallity</summary>
   public class MgContextMenu : ContextMenu
   {
      //Tag which is not supported in compact framework
      public object Tag;

      //Name is not supported in compact framework
      public string Name;

      /// <summary>Inserts Menuitem at given position. This is not supported in compact framrwork</summary>
      /// <param name="index"></param>
      /// <param name="menuItem"></param>
      public void Insert(int index, MenuItem menuItem)
      {
         MenuUtils.Insert(MenuItems, index, menuItem);
      }
   }
   
   /// <summary>Menu utility class.</summary>
   public class MenuUtils
   {
      /// <summary>Inserts MenuItem at given position in given menu item collection.</summary>
      /// <param name="MenuItems"></param>
      /// <param name="index"></param>
      /// <param name="menuItem"></param>
      public static void Insert(Menu.MenuItemCollection MenuItems, int index, MenuItem menuItem)
      {
         // If index is > count add the menuItem
         if (MenuItems.Count <= index + 1)
            MenuItems.Add(menuItem);
         else
         {
            //Otherwise copy all the menu items to an array
            MenuItem[] NewMenuItems = new MenuItem[MenuItems.Count];
            MenuItems.CopyTo(NewMenuItems, 0);

            //Remove all the menuitems where we want to insert the element.
            for (int i = index; i < NewMenuItems.Length; i++)
               MenuItems.RemoveAt(i);

            //Add the menu item
            MenuItems.Add(menuItem);

            // Add remianing menu Items.
            for (int i = index; index < NewMenuItems.Length; index++)
               MenuItems.Add(NewMenuItems[i]);
         }
      }

   }
}

