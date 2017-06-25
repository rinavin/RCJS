using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;

namespace com.magicsoftware.richclient.remote
{
   /// <summary>
   /// command to reset the user ranges on the remote dataview
   /// </summary>
   class ResetUserRangeRemoteDataviewCommand : RemoteDataViewCommandBase
   {
      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public ResetUserRangeRemoteDataviewCommand(ClientOriginatedCommand command)
         : base(command)
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         Task.UserRngs = null;
         Task.ResetRange = true;

         return new ReturnResult();
      }
   }
}
