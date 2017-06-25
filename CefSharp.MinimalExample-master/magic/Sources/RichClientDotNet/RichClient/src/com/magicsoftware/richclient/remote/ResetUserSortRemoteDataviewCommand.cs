using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.remote
{
   /// <summary>
   /// ResetUserSortRemoteDataviewCommand
   /// </summary>
   class ResetUserSortRemoteDataviewCommand : RemoteDataViewCommandBase
   {
       /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public ResetUserSortRemoteDataviewCommand(ClientOriginatedCommand command)
         : base(command)
      {
      }

      /// <summary>
      /// Reset User Sorts added by SortAdd function.
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         if (Task.UserSorts != null)
         {
            Task.UserSorts.Clear();
            Task.UserSorts = null;
         }

         Task.ResetSort = true;

         return new ReturnResult();
      }
   }
}
