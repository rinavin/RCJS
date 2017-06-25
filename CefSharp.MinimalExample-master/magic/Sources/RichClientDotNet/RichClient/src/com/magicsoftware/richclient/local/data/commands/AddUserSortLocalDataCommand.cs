using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.tasks.sort;

namespace com.magicsoftware.richclient.local.data.commands
{
   class AddUserSortLocalDataCommand : LocalDataViewCommandBase
   {
      Sort sort;
      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public AddUserSortLocalDataCommand(AddUserSortDataViewCommand command)
         : base(command)
      {
         sort = command.Sort;
      }

      internal override ReturnResultBase Execute()
      {
         if (LocalDataviewManager.UserSorts == null)
         {
            LocalDataviewManager.UserSorts = new SortCollection();
         }
         LocalDataviewManager.UserSorts.add(sort);

         return new ReturnResult();
      }
   }
}
