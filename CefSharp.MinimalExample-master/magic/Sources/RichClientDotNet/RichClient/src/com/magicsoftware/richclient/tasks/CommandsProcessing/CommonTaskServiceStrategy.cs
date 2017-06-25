using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.tasks.CommandsProcessing
{
   /// <summary>
   /// task services strategy for non-main task
   /// </summary>
   internal class CommonTaskServiceStrategy : ITaskServiceStrategy
   {
      TaskServiceBase _taskService;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="isOfflineTask"></param>
      public CommonTaskServiceStrategy(bool isOfflineTask)
      {
         _taskService = (isOfflineTask
                            ? (TaskServiceBase)(new LocalTaskService())
                            : (TaskServiceBase)(new RemoteTaskService()));
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public TaskServiceBase GetTaskService()
      {
         return _taskService;
      }
   }
}
