using System;
using com.magicsoftware.richclient.local.data.gateways.commands;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data.view.fields;
using com.magicsoftware.richclient.local.data.recording;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.local.data.commands
{

   /// <summary>
   /// define init dataview 
   /// </summary>
   internal class LocalInitDataViewCommand : LocalDataViewCommandBase
   {
      internal LocalInitDataViewCommand(DataviewCommand command)
         : base(command)
      { }


      /// <summary>
      /// !!
      /// </summary>
      /// <param name="command"></param>
      internal override ReturnResultBase Execute()
      {
         LocalDataviewManager.TaskViewsBuilder.FieldsBuilder = new FieldsBuilder();
         RecordingManagerBuilder recordingManagerBuilder = new RecordingManagerBuilder();
         LocalDataviewManager.BuildRecorder();
         LocalDataviewManager.BuildViewsCollection(Task);
         LocalDataviewManager.RecordingManager.StartRecording();
         

         GatewayResult retVal = OpenTables();
         
        
         //      if (l != tskr->dbs || !tskRt->AllDCTblsOpened)
         //{
         //   if (!(MAIN_PRG_PRG(tsk) && (BOOLEAN)ENV::backgnd_mode))
         //      tsk_close (l, TRUE, depth, FALSE, calltype, call_idx);
         //   return (FALSE);
         //}

         ///return retVal;
        
         return retVal;

      }      

      /// <summary>
      /// open local tables
      /// </summary>
      /// <returns></returns>
      private GatewayResult OpenTables()
      {
         GatewayResult retVal = new GatewayResult();
         foreach (DataSourceReference dataSourceRef in Task.DataSourceReferences)
         {
            if (dataSourceRef.IsLocal)
            {
               String fileName;
               if (dataSourceRef.NameExpression > 0)
                  Task.EvaluateExpressionAsUnicode(dataSourceRef.NameExpression, out fileName);
               else
                  fileName = dataSourceRef.DataSourceDefinition.Name;

               //create gw open command
               GatewayCommandFileOpen fileOpenCommand = GatewayCommandsFactory.CreateFileOpenCommand(fileName, dataSourceRef.DataSourceDefinition,
                                          dataSourceRef.Access, ClientManager.Instance.LocalManager);
               retVal = fileOpenCommand.Execute();
               if (!retVal.Success)
                  break;
            }
         }
         return retVal;

      }
   }

}
