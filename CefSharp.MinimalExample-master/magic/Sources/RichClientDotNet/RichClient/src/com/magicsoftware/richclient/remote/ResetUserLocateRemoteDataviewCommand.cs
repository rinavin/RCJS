using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.remote
{
   /// <summary>
   /// ResetUserLocateRemoteDataviewCommand
   /// </summary>
   class ResetUserLocateRemoteDataviewCommand : RemoteDataViewCommandBase
   {
       /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public ResetUserLocateRemoteDataviewCommand(ClientOriginatedCommand command)
         : base(command)
      {
      }

      /// <summary>
      /// Reset User Locates Added by LocateAdd function
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         if (Task.UserLocs != null)
         {
            Task.UserLocs.Clear();
            Task.UserLocs = null;
         }

         Task.ResetLocate = true;

         return new ReturnResult();
      }
   }
}
