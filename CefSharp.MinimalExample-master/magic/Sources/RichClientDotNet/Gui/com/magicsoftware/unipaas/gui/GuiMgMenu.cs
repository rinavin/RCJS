using System.Collections;
using com.magicsoftware.unipaas.gui.low;

namespace com.magicsoftware.unipaas.gui
{
   public class GuiMgMenu
   {
      protected Hashtable instantiatedContext;
      protected Hashtable instantiatedPullDown;

      public GuiMgMenu()
      {
         init();
      }

      public void init()
      {
         instantiatedPullDown = new Hashtable();
         instantiatedContext = new Hashtable();
      }

      /// <summary> This method updates a menu as instantiated for a specific form and style. It returns a reference to a
      /// menu object - to be used in order to retrieve this menu object, if it is needed. The returned object
      /// should be placed in the controlsMap, with the created menu for future use
      /// 
      /// </summary>
      /// <param name="form">the form for which the menus is instantiated</param>
      /// <param name="menuStyle">the menu style (pulldown, context)</param>
      /// <returns> menu reference object</returns>
      protected MenuReference setMenuIsInstantiated(GuiMgForm form, MenuStyle menuStyle)
      {
         var menuReference = new MenuReference(form);
         if (menuStyle == MenuStyle.MENU_STYLE_PULLDOWN)
            instantiatedPullDown[form] = menuReference;
         else
            instantiatedContext[form] = menuReference;
         return menuReference;
      }

      /// <summary> This method returns a menu object reference for a specific form and menu style (pulldown, context). The
      /// returned reference should be used in order to retrieve the specific instantiated menu object from the
      /// controls map.</summary>
      /// <param name="form">the form for which the menus is instatiated</param>
      /// <param name="menuStyle">the menu style (pulldown, context)</param>
      /// <returns> a menu object reference. In case the menu was not yet instantiated for the specfic form and
      /// style, null is returned.
      /// </returns>
      public MenuReference getInstantiatedMenu(GuiMgForm form, MenuStyle menuStyle)
      {
         MenuReference menuReference = null;
         if (menuStyle == MenuStyle.MENU_STYLE_PULLDOWN)
            menuReference = (MenuReference) instantiatedPullDown[form];
         else
            menuReference = (MenuReference) instantiatedContext[form];
         return menuReference;
      }

      /// <summary> This method removes an instantiated menu from the list</summary>
      /// <param name="form">the form for which the menus is instantiated</param>
      /// <param name="menuStyle">the menu style (pulldown, context)</param>
      /// <returns> menu reference object</returns>
      internal void removeInstantiatedMenu(GuiMgForm form, MenuStyle menuStyle)
      {
         if (menuStyle == MenuStyle.MENU_STYLE_PULLDOWN)
            instantiatedPullDown.Remove(form);
         else
            instantiatedContext.Remove(form);
      }
   }
}