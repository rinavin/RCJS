using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks.sort;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.remote
{
   /// <summary>
   /// Remote DataView Command for Add user sort.
   /// </summary>
   class AddUserSortRemoteDataViewCommand : RemoteDataViewCommandBase
   {
      Sort sort;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public AddUserSortRemoteDataViewCommand(AddUserSortDataViewCommand command)
         : base(command)
      {
         sort = command.Sort;
      }

      /// <summary>
      /// This will add user sorts on task so that view refresh command will apply this sort.
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         if (Task.UserSorts == null)
            Task.UserSorts = new List<Sort>();

         Task.UserSorts.Add(sort);

         return new ReturnResult();
      }
   }
}
