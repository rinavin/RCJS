using System;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.management.gui
{
   public class MenuEntryOSCommand : MenuEntry
   {
      public String OsCommand { get; internal set; } // OS Command text
      internal String Prompt { get; set; } // prompt
      public CallOsShow Show { get; internal set; } // how to open the window
      public Boolean Wait { get; internal set; } // wait till end of execution		

      /// <summary>
      /// 
      /// </summary>
      /// <param name="mgMenu"></param>
      public MenuEntryOSCommand(MgMenu mgMenu) : base(MenuType.OSCOMMAND, mgMenu)
      {
         Show = CallOsShow.Normal;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="isModal"></param>
      internal override bool ShouldSetModal(bool isModal, bool mainContextIsModal)
      {
         return true;
      }

  }
}