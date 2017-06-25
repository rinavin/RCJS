using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.rt;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// ResetUserSortLocalDataviewCommand
   /// </summary>
   class ResetUserSortLocalDataviewCommand : LocalDataViewCommandBase
   {
      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public ResetUserSortLocalDataviewCommand(DataviewCommand command)
         : base(command)
      {
      }

      /// <summary>
      /// Reset User Sorts added by SortAdd function.
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         if (LocalDataviewManager.UserSorts != null)
         {
            LocalDataviewManager.UserSorts = null;
         }

         return new ReturnResult();
      }
   }
}
