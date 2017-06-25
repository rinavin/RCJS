using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;

namespace com.magicsoftware.richclient.remote
{
   /// <summary>
   /// command to add the user range, from RangeAdd, to the remote data view 
   /// </summary>
   class AddUserRangeRemoteDataViewCommand : RemoteDataViewCommandBase
   {
      UserRange userRange;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public AddUserRangeRemoteDataViewCommand(AddUserRangeDataviewCommand command)
         : base(command)
      {
         userRange = command.Range;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         if (Task.UserRngs == null)
            Task.UserRngs = new List<UserRange>();

         Task.UserRngs.Add(userRange);

         return new ReturnResult();
      }
   }
}
