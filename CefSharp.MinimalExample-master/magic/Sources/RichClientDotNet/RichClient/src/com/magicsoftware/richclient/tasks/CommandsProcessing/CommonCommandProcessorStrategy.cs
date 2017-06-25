using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local;
using com.magicsoftware.richclient.remote;

namespace com.magicsoftware.richclient.tasks.CommandsProcessing
{
   /// <summary>
   /// commands processor strategy for non-main tasks
   /// </summary>
   internal class CommonCommandProcessorStrategy : ICommandsProcessorStrategy
   {
      CommandsProcessorBase taskService;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="isOfflineTask"></param>
      public CommonCommandProcessorStrategy(bool isOfflineTask)
      {
         if (isOfflineTask)
            taskService = LocalCommandsProcessor.GetInstance();
         else
            taskService = RemoteCommandsProcessor.GetInstance();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public CommandsProcessorBase GetCommandProcessor()
      {
         return taskService;
      }
   }
}
