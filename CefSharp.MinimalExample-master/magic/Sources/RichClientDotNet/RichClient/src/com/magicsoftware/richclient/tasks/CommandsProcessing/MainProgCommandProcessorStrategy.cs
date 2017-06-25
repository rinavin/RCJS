using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.tasks.CommandsProcessing
{
   /// <summary>
   /// command processor strategy for main program
   /// </summary>
   internal class MainProgCommandProcessorStrategy : ICommandsProcessorStrategy
   {
      /// <summary>
      /// get the command processor
      /// </summary>
      /// <returns></returns>
      public CommandsProcessorBase GetCommandProcessor()
      {
         // return the command processor from the manager
         return CommandsProcessorManager.GetCommandsProcessor();
      }
   }
}
