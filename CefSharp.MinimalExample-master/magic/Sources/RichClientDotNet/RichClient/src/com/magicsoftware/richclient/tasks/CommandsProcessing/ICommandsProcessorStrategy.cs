using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.tasks.CommandsProcessing
{
   /// <summary>
   /// interface for commands processor strategy
   /// </summary>
   internal interface ICommandsProcessorStrategy
   {
      CommandsProcessorBase GetCommandProcessor();
   }
}
