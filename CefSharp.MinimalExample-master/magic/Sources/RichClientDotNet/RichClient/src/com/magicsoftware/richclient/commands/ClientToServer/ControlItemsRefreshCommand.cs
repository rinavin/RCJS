using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// ControlItemsRefreshCommand class
   /// </summary>
   class ControlItemsRefreshCommand : DataviewCommand
   {
      public MgControlBase Control { get; set; }
      public ControlItemsRefreshCommand()
         : base()
      {
      }
   }
}
