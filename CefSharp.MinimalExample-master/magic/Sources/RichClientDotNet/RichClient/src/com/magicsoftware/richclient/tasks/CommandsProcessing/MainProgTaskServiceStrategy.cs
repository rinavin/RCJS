using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace com.magicsoftware.richclient.tasks.CommandsProcessing
{
   /// <summary>
   /// task services strategy for main program
   /// </summary>
   internal class MainProgTaskServiceStrategy : ITaskServiceStrategy
   {
      private TaskServiceBase _localTaskService = new LocalTaskService();
      private TaskServiceBase _remoteTaskService = new RemoteTaskService();

      /// <summary>
      /// get the task service
      /// </summary>
      /// <returns></returns>
      public TaskServiceBase GetTaskService()
      {
         // return task service according to connection state
         Debug.Assert(CommandsProcessorManager.SessionStatus != CommandsProcessorManager.SessionStatusEnum.Uninitialized);
         return (CommandsProcessorManager.SessionStatus == CommandsProcessorManager.SessionStatusEnum.Remote 
                     ? _remoteTaskService 
                     : _localTaskService);
      }
   }
}
