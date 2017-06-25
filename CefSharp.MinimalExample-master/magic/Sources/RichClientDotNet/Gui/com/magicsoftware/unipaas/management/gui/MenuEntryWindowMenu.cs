using System;
using System.Collections.Generic;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.gui.low;

namespace com.magicsoftware.unipaas.management.gui
{
   public class MenuEntryWindowMenu : MenuEntry
   {
      public MgFormBase MgForm { get; private set; }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="mgMenu"></param>
      internal MenuEntryWindowMenu (MgMenu mgMenu)
         : base(MenuType.WINDOW_MENU_ENTRY, mgMenu)
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="mgFormbase"></param>
      internal void SetForm(MgFormBase mgFormBase)
      {
         MgForm = mgFormBase;
         String menuText = "";

         Property prop = mgFormBase.GetComputedProperty(PropInterface.PROP_TYPE_FORM_NAME);
         if (prop != null)
            menuText = prop.GetComputedValue();

         setText(menuText, false);
         TextMLS = Events.Translate(menuText);
      }

      /// <summary>
      /// Create a WindowMenuEntry under a MgMenu
      /// </summary>
      /// <param name="mgFormBase"></param>
      /// <param name="menuType">WindowMenu / Separator</param>
      /// <param name="windowMenuIdx">Index where new menuentry should be added</param>
      /// <param name="guiMgForm"></param>
      /// <param name="menuStyle">Pulldown / Context</param>
      /// <param name="setChecked"
      public override void CreateMenuEntry(MgFormBase mgFormBase, MenuType menuType, int windowMenuIdx, GuiMgForm guiMgForm, MenuStyle menuStyle, bool setChecked)
      {
          MenuEntry menuEntry = base.CreateMenuEntryItem(mgFormBase, menuType, guiMgForm, menuStyle, setChecked);
          MgMenu mgMenu = getParentMgMenu();
          mgMenu.addMenu(menuEntry, windowMenuIdx + 1);
          mgMenu.setIndexes(false);
      }
   }
}