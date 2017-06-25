using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.gateways.commands;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// closing task dataview
   /// </summary>
   internal class LocalClearDataViewCommand : LocalDataViewCommandBase
   {
      public LocalClearDataViewCommand(DataviewCommand command)
         : base(command)
      {

      }
      internal override ReturnResultBase Execute()
      {
         if (TaskViews != null)
            TaskViews.ReleaseCursors();

         //close transaction

         CloseTables();
         if ( LocalDataviewManager.RecordingManager != null)
            LocalDataviewManager.RecordingManager.StopRecording();

         return new GatewayResult();
      }
      



      private void CloseTables()
      {
         foreach (DataSourceReference dataSourceRef in Task.DataSourceReferences)
         {
            if (dataSourceRef.IsLocal)
            {
               //create gw close command
               GatewayCommandFileClose fileCloseCommand = GatewayCommandsFactory.CreateFileCloseCommand(dataSourceRef.DataSourceDefinition, LocalManager);
               fileCloseCommand.Execute();
            }
         }

         if (TaskViews != null)
            TaskViews.CloseTables();

      }
   }
}
