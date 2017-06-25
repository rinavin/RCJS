using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// command to reset the user ranges on the local dataview
   /// </summary>
   class ResetUserRangeLocalDataviewCommand : LocalDataViewCommandBase
   {
      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public ResetUserRangeLocalDataviewCommand(DataviewCommand command)
         : base(command)
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override util.ReturnResultBase Execute()
      {
         LocalDataviewManager.ResetUserRanges();

         return new ReturnResult();
      }
   }
}
