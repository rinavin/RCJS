using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Collections.Generic;

namespace com.magicsoftware.richclient.remote
{
   /// <summary>
   /// Remote DataView Command for Add User Locates.
   /// </summary>
   class AddUserLocateRemoteDataViewCommand: RemoteDataViewCommandBase
   {
      UserRange userRange;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public AddUserLocateRemoteDataViewCommand(AddUserLocateDataViewCommand command)
         : base(command)
      {
         userRange = command.Range;
      }

      /// <summary>
      /// Add locate values into the task, so that view refresh command will use this values to locate the records.
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         if (Task.UserLocs == null)
            Task.UserLocs = new List<UserRange>();
         Task.UserLocs.Add(userRange);

         return new ReturnResult();
      }
   }
}
