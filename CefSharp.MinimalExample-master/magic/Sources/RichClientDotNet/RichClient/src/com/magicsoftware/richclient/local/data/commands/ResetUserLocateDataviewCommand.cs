using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// ResetUserLocateDataviewCommand
   /// </summary>
   class ResetUserLocateDataviewCommand : LocalDataViewCommandBase
   {
       /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public ResetUserLocateDataviewCommand(DataviewCommand command)
         : base(command)
      {
      }

      /// <summary>
      /// Reset user locates add by LocateAdd function.
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         LocalDataviewManager.ResetUserLocates();
         return new ReturnResult();
      }
   }
}
