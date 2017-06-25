using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient.tasks.CommandsProcessing
{
   /// <summary>
   /// interface for task services strategy
   /// </summary>
   internal interface ITaskServiceStrategy
   {
      TaskServiceBase GetTaskService();
   }
}
