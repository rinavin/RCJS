using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.commands.ClientToServer
{
   /// <summary>
   /// interface to identify commands which have the task tag member
   /// </summary>
   interface ICommandTaskTag
   {
      String TaskTag { get; set; }
   }
}
