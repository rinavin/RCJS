using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;

namespace com.magicsoftware.richclient.local.commands
{
   /// <summary>
   /// local command when the client aborts and tells the server to unload
   /// </summary>
   class LocalRunTimeCommandUnload : LocalRunTimeCommandBase
   {
      internal override void Execute()
      {
         MGData firstMgData = MGDataCollection.Instance.getMGData(0);
         Task MainPrg = firstMgData.getMainProg(0);

         while (MainPrg != null)
         {
            MainPrg.handleTaskSuffix(false);

            MainPrg = firstMgData.getNextMainProg(MainPrg.getCtlIdx());
         }
      }
   }
}
