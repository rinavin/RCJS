using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using System.Diagnostics;
using com.magicsoftware.richclient.commands;

namespace com.magicsoftware.richclient.local.commands
{
   /// <summary>
   /// base class for local commands
   /// </summary>
   abstract class LocalRunTimeCommandBase
   {
      abstract internal void Execute();

      /// <summary>
      /// execute the command - add the command to the pending client commands and execute them
      /// </summary>
      /// <param name="command"></param>
      internal void Execute(IClientCommand command)
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;
         mgDataTab.currMgdID = MGDataCollection.Instance.currMgdID;
         MGData mgd = mgDataTab.getCurrMGData();

         // Add the command to the queue 
         mgd.CmdsToClient.Add(command);

         try
         {
            // execute the command
            mgd.CmdsToClient.Execute(null);
         }
         catch (Exception)
         {
            mgd.CmdsToClient.clear();
            throw;
         }
      }
   }
}
