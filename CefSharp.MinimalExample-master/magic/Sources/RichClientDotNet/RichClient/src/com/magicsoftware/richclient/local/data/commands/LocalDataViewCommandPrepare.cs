using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// prepare command
   /// </summary>
   internal class LocalDataViewCommandPrepare : LocalDataViewCommandBase
   {
      public LocalDataViewCommandPrepare(DataviewCommand command)
         : base(command)
      { }



      internal override ReturnResultBase Execute()
      {
         return TaskViews.Prepare();
      }
   }
}
